"""Session controls over the retained Workflow and atomic journal receipts.

The caller holds the existing Session lock. This class owns no puzzle, clock,
solver, history database, or automatic recipe-selection policy.
"""
from copy import deepcopy
import base64
import json
import secrets
import time

import numpy as np

from core import canonical, digest, state_hash
from enhanced import Workflow
from log_io import MAX_LOG_BYTES, MAX_JSON_BYTES, decode_log, validate_record
from transported_frames import FRAME_VERSION, FrameUnavailable

ATTEMPT_KEY = 'magic600_session_attempt_v1'
ATTEMPT_VERSION = 'manual-session-attempt-v1'
COMPLETION_VERSION = 'manual-session-completion-v1'
ACK_PREFIX = 'magic600_completion_ack:'
MAX_LOG_BASE64_BYTES = 4 * ((MAX_LOG_BYTES + 2) // 3)


class SessionWorkflow:
    def __init__(self, session, workflow):
        if not isinstance(workflow, Workflow) or workflow.s is not session:
            raise ValueError('Session controls require its existing shared Workflow instance')
        self.s, self.workflow = session, workflow
        self._log_confirmation = None

    def _attempt(self):
        raw = self.s._get(ATTEMPT_KEY)
        if raw is None:
            return dict(version=ATTEMPT_VERSION, id='legacy', model=self.s.m.model_id,
                        start_head=0, start_hash=state_hash(self.s.m.ids),
                        timer_baseline=0.0, created=None, legacy=True)
        result = json.loads(raw)
        if result.get('version') != ATTEMPT_VERSION or result.get('model') != self.s.m.model_id:
            raise ValueError('Stored attempt metadata belongs to another model or version')
        return result

    def _timer(self, value=None):
        value = deepcopy(self.workflow.timer() if value is None else value)
        value['session_seconds'] = value['seconds']
        value['seconds'] = max(0.0, value['seconds'] - self._attempt()['timer_baseline'])
        value['scope'] = 'Explicit shared session timer since this attempt baseline; includes thinking time'
        return value

    def plan_new_attempt(self):
        """Detached metadata for the caller's existing atomic Session.reset hook."""
        timer = self.workflow.timer()
        return dict(version=ATTEMPT_VERSION, id=secrets.token_hex(12), model=self.s.m.model_id,
                    start_head=0, start_hash=state_hash(self.s.m.ids),
                    timer_baseline=timer['seconds'], created=time.time())

    def _source(self, incoming=None):
        assistance = set()
        head = self.s.head
        while head:
            self.s.m.check_cancel()
            row = self.s.event(head)
            if row is None:
                raise ValueError('Session ancestry is incomplete')
            assistance.add(row['assistance'])
            head = row['parent']
        if incoming is not None:
            assistance.add(incoming)
        if any('synthetic' in item or 'practice' in item for item in assistance):
            label = 'Practice journal'
        elif any(item.startswith('assisted') for item in assistance):
            label = 'Journal includes assisted operations'
        elif any('scramble' in item for item in assistance):
            label = 'Recorded scramble and journal operations'
        else:
            label = 'Existing journal operations; starting provenance not certified'
        return dict(label=label, assistance=sorted(assistance), human_solve_certified=False,
                    scope='Recorded ancestry, not proof of human-only solving or imported provenance')

    def _current_completion(self, include_ack=False):
        row = self.s.event()
        raw = self.s._get('event:' + str(self.s.head) + ':workflow')
        if row is None or raw is None:
            return None
        receipt = json.loads(raw)
        result = receipt.get('session_completion')
        if not isinstance(result, dict):
            return None
        if (result.get('version') != COMPLETION_VERSION or result.get('model') != self.s.m.model_id
                or result.get('attempt_id') != self._attempt()['id']
                or result.get('parent') != row['parent'] or result.get('pre_state') != row['pre']
                or result.get('state_hash') != row['post'] or row['post'] != self.s.st.hash
                or receipt.get('pre_state') != row['pre'] or receipt.get('post_state') != row['post']
                or result.get('recipe_hash') != digest(canonical(json.loads(row['recipe'])).encode())
                or not np.array_equal(self.s.st.labels, self.s.m.ids)):
            return None
        if not include_ack and self.s._get(ACK_PREFIX + result['id']) is not None:
            return None
        return dict(deepcopy(result), head=self.s.head)

    def report(self):
        stats = self.workflow.stats()
        return dict(head=self.s.head, state_hash=self.s.st.hash,
                    raw_full_home=bool(np.array_equal(self.s.st.labels, self.s.m.ids)),
                    attempt_id=self._attempt()['id'], attempt=deepcopy(self._attempt()),
                    timer=self._timer(stats['timer']), stats=stats, source=self._source(),
                    completion=self._current_completion(),
                    recorded_completion=self._current_completion(include_ack=True))

    def resume(self):
        """Reading Resume neither starts a timer nor reloads/reset the shared state."""
        return self.report()

    def timer(self, command):
        if command not in ('start', 'pause'):
            raise ValueError('Timer command must be start or pause')
        self.workflow.timer(command)
        return self.report()

    def scramble_recipe(self, count, seed=None):
        if type(count) is not int or not 1 <= count <= 10000:
            raise ValueError('Scramble length must be an integer in 1..10,000')
        if seed is None:
            seed = secrets.randbits(32)
        if type(seed) is not int:
            raise ValueError('Scramble seed must be an integer')
        previous = self.s.pending
        try:
            # Reuse the retained random-word generator; retain no temporary preview.
            self.workflow.scramble(count, seed, apply=False)
            recipe = deepcopy(self.s.pending['recipe'])
            self.s.m.check_cancel()
            return dict(recipe=recipe, source=dict(kind='recorded-scramble',
                assistance='recorded-scramble', seed=seed, count=count,
                model=self.s.m.model_id, pre_state=self.s.st.hash,
                recipe_hash=digest(canonical(recipe).encode())))
        finally:
            self.s.pending = previous

    def save_log(self, format='c600', native_profile=None):
        if format not in ('c600', 'mpult'):
            raise ValueError('Log format must be c600 or mpult')
        seconds = self._timer()['seconds']
        return self.s.save_log(format, native_profile, timer_ms=int(seconds * 1000))

    def export_log(self, format='c600', native_profile=None):
        """Existing wire bytes; the caller chooses and writes the output file."""
        if format not in ('c600', 'mpult'):
            raise ValueError('Log format must be c600 or mpult')
        return self.s.export_log(format, native_profile, timer_ms=int(self._timer()['seconds'] * 1000))

    def _log_binding(self, format, native_profile):
        pending = self.s.pending
        return dict(model=self.s.m.model_id, head=self.s.head, revision=self.s.rev,
            state_hash=self.s.st.hash, prefs_sha256=digest(canonical(self.s.prefs).encode()),
            pending=None if pending is None else dict(token=pending['token'], head=pending['head'],
                revision=pending['rev'], recipe=deepcopy(pending['recipe']), pre=pending['public']['pre_state'],
                post=pending['public']['post_state']),
            native_profile_sha256=digest(canonical(native_profile).encode()) if format == 'mpult' else None)

    def inspect_log(self, data_base64, format='c600', native_profile=None):
        """Replay privately, then publish one opaque, current-session confirmation.

        The caller supplies only the server's verified native profile. File
        preferences and source claims never become current settings or proof.
        """
        self.s.m.check_cancel()
        binding = self._log_binding(format, native_profile)
        if format == 'c600':
            plan = validate_record(self.s.m, decode_log(data_base64, format))
        elif format == 'mpult':
            from mpult_log import import_native
            plan = import_native(self.s.m, data_base64, native_profile)
        else:
            raise ValueError('Log format must be c600 or mpult')
        raw = base64.b64decode(data_base64, validate=True)  # Codec already enforced size/encoding limits.
        selected = plan.get('selected_count', plan['transactions'])
        public = dict(format=format, model=self.s.m.model_id, file_sha256=digest(raw), bytes=len(raw),
            state_hash=plan['state'].hash, raw_full_home=bool(np.array_equal(plan['state'].labels, self.s.m.ids)),
            transactions=plan['transactions'], selected_transactions=selected,
            redo_transactions=plan['transactions'] - selected, primitive_count=plan['primitive_count'],
            source=dict(assistance=sorted({row['assistance'] for row in plan['events']}),
                human_solve_certified=False, scope='Imported source claims; legal replay verified, provenance not certified'),
            native_log=deepcopy(plan.get('native_log')), confirmation_id=secrets.token_hex(24))
        self.s.m.check_cancel()
        if binding != self._log_binding(format, native_profile):
            raise ValueError('Session changed during log inspection; inspect the file again')
        self.s.m.check_cancel()
        self._log_confirmation = dict(public=deepcopy(public), binding=binding, payload=data_base64)
        return public

    def apply_log(self, data_base64, confirmation_id, format='c600', native_profile=None):
        """Apply only the freshly inspected original bytes using Session's transaction."""
        saved = self._log_confirmation
        if (saved is None or not isinstance(confirmation_id, str)
                or not secrets.compare_digest(confirmation_id, saved['public']['confirmation_id'])):
            raise ValueError('Log confirmation is unavailable; inspect the file before importing')
        if format != saved['public']['format'] or data_base64 != saved['payload']:
            raise ValueError('The log file or format changed; inspect the original file again')
        binding = self._log_binding(format, native_profile)
        if binding != saved['binding']:
            raise ValueError('Session, pending operation, settings or native mapping changed; inspect the file again')
        self.s.m.check_cancel()
        checkpoints = {row['name'] for row in self.s.db.execute('SELECT name FROM snapshots')}
        warning = None
        try:
            result = self.s.import_log(data_base64, format, native_profile)
        except Exception as error:
            # Session may fail in its final status read after a durable import.
            # Reconcile only that exact transition and its atomic recovery record.
            recovery = [row for row in self.s.db.execute('SELECT name,head,hash,prefs FROM snapshots')
                if row['name'] not in checkpoints and row['name'].startswith('Before import ')
                and row['head'] == binding['head'] and row['hash'] == binding['state_hash']
                and digest(canonical(json.loads(row['prefs'])).encode()) == binding['prefs_sha256']]
            if not (self.s.rev == binding['revision'] + 1 and len(recovery) == 1
                    and self.s.st.hash == saved['public']['state_hash']
                    and int(self.s._get('head')) == self.s.head and self.s.event()['post'] == self.s.st.hash
                    and digest(canonical(self.s.prefs).encode()) == binding['prefs_sha256']
                    and digest(canonical(json.loads(self.s._get('prefs'))).encode()) == binding['prefs_sha256']
                    and self.s.pending is None):
                raise
            result = dict(import_checkpoint=recovery[0]['name'], previous_head=binding['head'],
                imported_transactions=saved['public']['transactions'], native_log=saved['public']['native_log'])
            warning = 'The log was imported; its status refresh failed: ' + str(error)
        self._log_confirmation = None
        return dict(import_applied=True, format=format, model=self.s.m.model_id,
            file_sha256=saved['public']['file_sha256'], head=self.s.head, state_hash=self.s.st.hash,
            import_checkpoint=result['import_checkpoint'], previous_head=result['previous_head'],
            imported_transactions=result['imported_transactions'], native_log=result.get('native_log'),
            source_head=binding['head'], source_revision=binding['revision'], source_state_hash=binding['state_hash'],
            prefs_sha256=binding['prefs_sha256'], warning=warning)

    def completion_plan(self, before, after, frames):
        """Cancellable detached analysis; publish only in Session.commit's receipt.

        Raw Home labels do not alone certify the summary. The exact pending
        operation and every fixed orbit frame must be accounted for first.
        """
        model = self.s.m
        model.check_cancel()
        if before is not self.s.st or after.m is not model:
            raise ValueError('Completion must use the authoritative pre-state and its predicted model')
        pending = self.s.pending
        if (pending is None or pending['head'] != self.s.head or pending['rev'] != self.s.rev
                or pending['public']['pre_state'] != before.hash
                or pending['public']['post_state'] != after.hash
                or not np.array_equal(pending['after'], after.labels)):
            raise ValueError('Completion does not match the exact pending operation')
        if np.array_equal(before.labels, model.ids) or not np.array_equal(after.labels, model.ids):
            return None
        if frames is None:
            return None
        certificates = []
        for orbit in range(35):
            model.check_cancel()
            try:
                certificate = frames.ensure(orbit)
            except FrameUnavailable:
                return None
            if (certificate.get('status') != 'Verified' or certificate.get('model') != model.model_id
                    or certificate.get('frame_version') != FRAME_VERSION or not certificate.get('frame_sha256')):
                return None
            certificates.append(dict(orbit=orbit, frame_sha256=certificate['frame_sha256']))
        public = pending['public']
        stats = self.workflow.stats()
        stats['operations'] += 1
        stats['stars'] += public['star_count']
        key = 'scramble_primitives' if 'scramble' in public['assistance'] else 'solution_primitives'
        stats[key] = str(int(stats[key]) + int(public['primitive_count']))
        stats['assisted_transactions'] += int(public['assistance'].startswith('assisted'))
        result = dict(version=COMPLETION_VERSION, model=model.model_id,
            attempt_id=self._attempt()['id'], parent=self.s.head, pre_state=before.hash,
            state_hash=after.hash, recipe_hash=digest(canonical(pending['recipe']).encode()),
            kind='Whole-model completion', predicate='All 259800 labels equal Home; all35 fixed orbit frames verified',
            frame_version=FRAME_VERSION, frame_certificates=certificates,
            timer=self._timer(stats['timer']), stats=stats, source=self._source(public['assistance']))
        result['id'] = digest(canonical({key: result[key] for key in (
            'version', 'model', 'attempt_id', 'parent', 'pre_state', 'state_hash', 'recipe_hash')}).encode())
        model.check_cancel()
        return result

    def acknowledge(self, completion_id):
        current = self._current_completion(include_ack=True)
        if type(completion_id) is not str or current is None or current['id'] != completion_id:
            raise ValueError('Completion acknowledgement is stale or unavailable')
        with self.s.db:
            self.s._put(ACK_PREFIX + completion_id, self.s.head)
        return self.report()
