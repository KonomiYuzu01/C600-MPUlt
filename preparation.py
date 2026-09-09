"""Read-only preparation analysis for the next update; not wired into 0.3.

Pass the SAME lock used by all Session readers/writers. This service neither
owns a second state engine nor changes preferences, previews, or the journal.
Its context hashes identify stale analysis; they never authorize execution.
"""
from __future__ import annotations

from dataclasses import dataclass
import json
import re
import secrets

import numpy as np

from core import ORDER, PuzzleState, canonical, digest


@dataclass(frozen=True)
class PreparationIntent:
    """An explicit human-selected destination, never an automatic next target."""
    orbit: int
    destination: int
    preserve_finished_nonbuffers: bool = True

    def validate(self, model):
        if type(self.orbit) is not int or not 0 <= self.orbit < 35:
            raise ValueError('Orbit must be an integer in 0..34')
        if type(self.destination) is not int or not 0 <= self.destination < model.np:
            raise ValueError('Destination must be a valid integer physical position')
        if type(self.preserve_finished_nonbuffers) is not bool:
            raise ValueError('Finished nonbuffer protection must be Boolean')

    def as_dict(self):
        return dict(orbit=self.orbit, destination=self.destination,
                    preserve_finished_nonbuffers=self.preserve_finished_nonbuffers)


class PreparationService:
    """A bounded, synchronous domain service for an existing engine worker.

    No HTTP route, UI callback, timer, execution token or durable stage ledger
    is installed. Caller integration must keep work off the native UI thread.
    """
    def __init__(self, session, lock):
        self.session = session
        self.lock = lock
        self.epoch = secrets.token_hex(16)
        self._inspection_key = None
        self._inspection_json = None

    def _context(self):
        session = self.session
        pending = session.pending
        # Bind another pending operation without returning its execution token.
        pending_id = None if pending is None else digest(canonical({
            'token': pending['token'], 'head': pending['head'], 'rev': pending['rev'],
        }).encode())
        value = dict(format='C600-preparation-context-v1', epoch=self.epoch,
                     model_id=session.m.model_id, head=session.head,
                     revision=session.rev, state_hash=session.st.hash,
                     protected_orbits=sorted(session.prefs['protected']),
                     pending_preview_identity=pending_id)
        value['context_id'] = digest(canonical(value).encode())
        return value

    @staticmethod
    def _expect(context, expected):
        if not isinstance(expected, str) or not re.fullmatch('[0-9a-f]{64}', expected):
            raise ValueError('Expected a preparation context ID')
        if not secrets.compare_digest(context['context_id'], expected):
            raise ValueError('Preparation is stale; inspect the current state again')

    def _capture(self, intent, context):
        session, model = self.session, self.session.m
        state, destination, orbit = session.st, intent.destination, intent.orbit
        table = model.trees[orbit]
        a, b = table['buffers']
        source = int(state.where[destination])
        destination_orbit = int(model.oid[destination])
        reasons = []
        if destination_orbit == -1:
            reasons.append('fixed-cell-center')
        elif destination_orbit != orbit:
            reasons.append('destination-orbit-mismatch')
        elif destination in (a, b):
            reasons.append('fixed-buffer-destination')
        progress = state.progress()
        protected = set(context['protected_orbits'])
        order_rank = {o: i for i, o in enumerate(ORDER)}
        stage_rows = []
        for o in ORDER:
            row = progress[o]
            stage_rows.append(dict(
                orbit=o, prescribed_rank=order_rank[o], pieces=row['pieces'],
                exact_solved=row['solved'], complete_now=row['solved'] == row['pieces'],
                protected=o in protected, earlier_in_order=order_rank[o] < order_rank[orbit],
            ))
        candidates = []
        if not reasons:
            for node in model.bypos[orbit][destination]:
                path = model.path(orbit, node)
                candidates.append(dict(
                    node=node, ordered_frame_slots=list(table['frames'][node]),
                    setup_word=path, setup_depth=table['depth'][node],
                    star_primitive_count=len(table['seed']) + 2 * len(table['relocation']) + 2 * len(path),
                ))
        return dict(
            format='C600-preparation-inspection-v1', intent=intent.as_dict(),
            context=context, analysis_only=True, insertion_available=not reasons,
            unavailable_reasons=reasons, destination=state.piece(destination),
            required_identity=destination, required_current_position=source,
            required_piece=state.piece(source),
            buffers=dict(A=state.piece(a), B=state.piece(b)),
            buffer_guard_lab_cells=sorted(set(model.caps(a) + model.caps(b))),
            frame_candidates=candidates, stages=stage_rows,
            seed_collateral_reference=list(model.atlas[orbit]['collateral']),
            protection_boundary='complete-operation-net-effect',
            reference_is_execution_certificate=False,
            arbitrary_buffer_relocation_supported=False,
        )

    def inspect(self, intent, expected_context=None):
        """Return coherent target/buffer/frame/stage data, with one cached result.

        Filters/cameras do not invalidate this mechanical analysis. Commits,
        protection edits, pending-preview changes and service reopen do.
        """
        with self.lock:
            if not isinstance(intent, PreparationIntent):
                raise ValueError('Expected an explicit PreparationIntent')
            intent.validate(self.session.m)
            context = self._context()
            if expected_context is not None:
                self._expect(context, expected_context)
            key = (intent, context['context_id'])
            if key != self._inspection_key:
                encoded = canonical(self._capture(intent, context))
                self._inspection_key, self._inspection_json = key, encoded
            # Cached output is serialized, so callers cannot mutate future reads.
            return json.loads(self._inspection_json)

    def _segments(self, segments):
        phases = ('prepare', 'macro', 'cleanup')
        if not isinstance(segments, list) or not 1 <= len(segments) <= 3:
            raise ValueError('Use 1..3 explicit prepare/macro/cleanup segments')
        seen, combined, boundaries = [], [], []
        for segment in segments:
            if not isinstance(segment, dict) or set(segment) != {'phase', 'recipe'}:
                raise ValueError('Each segment requires only phase and recipe')
            phase = segment['phase']
            if phase not in phases or phase in seen:
                raise ValueError('Segment phases must be distinct')
            recipe = segment['recipe']
            if not isinstance(recipe, list) or not 1 <= len(recipe) <= 256:
                raise ValueError('Each segment requires a nonempty legal recipe')
            for step in recipe:
                if not isinstance(step, dict):
                    raise ValueError('Invalid recipe step')
                fields = {'kind', 'moves'} if step.get('kind') == 'word' else {'kind', 'orbit', 'node', 'sign'}
                if step.get('kind') not in ('word', 'star') or not set(step) <= fields:
                    raise ValueError('Unknown recipe kind or extra step fields')
            normalized, length = self.session.m.normalize(recipe)
            seen.append(phase)
            boundaries.append(dict(phase=phase, first_step=len(combined),
                                   step_count=len(normalized), primitive_count=str(length)))
            combined.extend(normalized)
        if 'macro' not in seen or seen != sorted(seen, key=phases.index):
            raise ValueError('Use chronological prepare, macro, cleanup order with one macro')
        # Revalidate aggregate limits; per-segment validity alone is insufficient.
        normalized, _ = self.session.m.normalize(combined)
        return json.loads(canonical(normalized)), boundaries

    def review(self, intent, segments, expected_context):
        """Evaluate an explicit COMPLETE operation without replacing a preview.

        A successful review is not a commit capability or a proof of human cost.
        Intermediate no-motion protection is intentionally not implemented.
        """
        with self.lock:
            inspection = self.inspect(intent, expected_context)
            if not inspection['insertion_available']:
                raise ValueError('Destination is inspectable but unsupported for insertion review: '
                                 + ', '.join(inspection['unavailable_reasons']))
            context = inspection['context']
            recipe, boundaries = self._segments(segments)
            model, state = self.session.m, self.session.st
            src, dst, primitive_count, recipe = model.net(recipe)
            labels = state.labels.copy()
            labels[dst] = labels[src]
            after = PuzzleState(model, labels)  # Validate the complete labelled state.
            support = model.support(src)
            protected = set(context['protected_orbits'])
            conflicts = [row for row in support if row['orbit'] in protected]
            required_location = int(after.where[intent.destination])
            a, b = model.trees[intent.orbit]['buffers']
            finished = (model.oid == intent.orbit) & state.correct
            finished[[a, b]] = False
            lost_finished = np.flatnonzero(finished & ~after.correct)
            finished_conflict = intent.preserve_finished_nonbuffers and len(lost_finished) != 0
            plan = dict(intent=intent.as_dict(), segments=boundaries, recipe=recipe)
            plan_id = digest(canonical(plan).encode())
            result = dict(
                format='C600-preparation-review-v1', analysis_only=True,
                context=context, plan=plan, plan_id=plan_id,
                post_state_hash=after.hash, full_support=support,
                protected_conflicts=conflicts,
                protection_boundary='complete-operation-net-effect',
                intermediate_motion_checked=False,
                target_exactly_solved=bool(after.correct[intent.destination]),
                meets_target_and_protection=bool(after.correct[intent.destination]) and not conflicts and not finished_conflict,
                finished_nonbuffer_losses=int(len(lost_finished)),
                finished_nonbuffer_loss_examples=lost_finished[:32].tolist(),
                finished_nonbuffer_protection_conflict=bool(finished_conflict),
                destination_after=after.piece(intent.destination),
                required_current_position_after=required_location,
                buffers_after=dict(A=after.piece(a), B=after.piece(b)),
                exact_gains=int(np.count_nonzero(after.correct & ~state.correct)),
                exact_losses=int(np.count_nonzero(state.correct & ~after.correct)),
                primitive_count=str(primitive_count),
                star_count=sum(step['kind'] == 'star' for step in recipe),
                preparation_segment_steps=sum(row['step_count'] for row in boundaries if row['phase'] == 'prepare'),
                cleanup_segment_steps=sum(row['step_count'] for row in boundaries if row['phase'] == 'cleanup'),
                human_preparation_cycles=None, estimated_human_seconds=None,
                next_action='Request an existing Session preview for this exact recipe; revalidate this context first.',
            )
            result['review_id'] = digest(canonical({
                'context_id': context['context_id'], 'plan_id': plan_id,
                'post_state_hash': after.hash,
            }).encode())
            return result
