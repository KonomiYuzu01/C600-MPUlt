"""Focused full-model preparation contracts on fresh isolated Windows data.

No native host, personal data, network, or alternative mechanics are involved.
"""
from pathlib import Path
import argparse
import hashlib
import json
import sys
import threading
import time

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from core import Model, canonical, digest, invrecipe, state_hash
from preparation import PreparationIntent, PreparationService
from session import Session


def frozen(session):
    pending = session.pending
    pending_state = None if pending is None else (
        id(pending), canonical(pending['public']), pending['token'],
        pending['src'].tobytes(), pending['dst'].tobytes(), pending['after'].tobytes(),
    )
    return (session.st.labels.tobytes(), session.head, session.rev,
            canonical(session.prefs), tuple(session.redo_stack), pending_state,
            session.db.total_changes, tuple(session.db.iterdump()))


def rejected_unchanged(session, callback):
    before = frozen(session)
    try:
        callback()
    except (ValueError, InterruptedError):
        pass
    else:
        raise AssertionError('Invalid or stale preparation was accepted')
    assert frozen(session) == before


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--work-dir', type=Path, required=True)
    args = parser.parse_args()
    work = args.work_dir.resolve()
    work.mkdir(parents=True, exist_ok=False)
    started = time.perf_counter()
    source_names = ['core.py', 'session.py', 'preparation.py', 'tests/test_preparation.py']
    hashes = {name: hashlib.sha256((ROOT / name).read_bytes()).hexdigest() for name in source_names}
    model = Model()
    session = Session(model, work / 'session')
    lock = threading.RLock()
    service = PreparationService(session, lock)
    checks = []
    orbit = 33
    destination = int(model.trees[orbit]['third'])
    intent = PreparationIntent(orbit, destination)
    star = [dict(kind='star', orbit=orbit, node=0, sign=1)]
    inverse = invrecipe(star)
    segments = [dict(phase='macro', recipe=inverse)]

    def commit(recipe):
        with lock:
            preview = session.preview(recipe, 'Isolated preparation contract check')
            return session.commit(preview['token'])

    def review(plan=segments, chosen=intent):
        return service.review(chosen, plan, service.inspect(chosen)['context']['context_id'])

    try:
        before = frozen(session)
        initial = service.inspect(intent)
        assert initial['required_identity'] == destination == initial['required_current_position']
        assert len(initial['stages']) == 35
        assert all(row['complete_now'] and not row['protected'] for row in initial['stages'])
        center = int(model.center_piece[0])
        assert service.inspect(PreparationIntent(orbit, center))['unavailable_reasons'] == ['fixed-cell-center']
        buffer = int(model.trees[orbit]['buffers'][0])
        assert service.inspect(PreparationIntent(orbit, buffer))['unavailable_reasons'] == ['fixed-buffer-destination']
        other = int(model.trees[34]['third'])
        assert service.inspect(PreparationIntent(orbit, other))['unavailable_reasons'] == ['destination-orbit-mismatch']
        assert frozen(session) == before
        checks.append('Exact full-model identities, 35 stages and safe unsupported-destination inspection without writes')

        commit(star)
        session.preview([dict(kind='word', moves=[17])], 'Existing user preview must survive')
        before = frozen(session)
        current = service.inspect(intent)
        a, b = model.trees[orbit]['buffers']
        assert current['destination']['piece'] == b
        assert current['required_identity'] == destination and current['required_current_position'] == a
        assert current['buffers']['A']['piece'] == destination
        for row in current['frame_candidates']:
            assert row['ordered_frame_slots'] == model.trees[orbit]['frames'][row['node']]
            assert row['setup_word'] == model.path(orbit, row['node'])
        result = review()
        expected = session.st.labels.copy()
        for move in model.expand(inverse):
            src, dst = model.move(move)
            expected[dst] = expected[src]
        assert result['post_state_hash'] == state_hash(expected)
        assert result['meets_target_and_protection'] and result['target_exactly_solved']
        assert np.array_equal(expected, model.ids)
        assert result['human_preparation_cycles'] is None and result['estimated_human_seconds'] is None
        assert frozen(session) == before
        assert not hasattr(service, 'commit')
        checks.append('Moved buffer occupants and ordered frames are exact; review matches primitive replay and preserves an unrelated pending preview')

        # A complete identity can temporarily move protected orbits during setup.
        session.save_prefs(dict(protected=list(range(35))))
        identity = [dict(phase='prepare', recipe=[dict(kind='word', moves=[1])]),
                    dict(phase='macro', recipe=[dict(kind='word', moves=[2, -2])]),
                    dict(phase='cleanup', recipe=[dict(kind='word', moves=[-1])])]
        before = frozen(session)
        full = review(identity)
        assert full['full_support'] == [] and full['protected_conflicts'] == []
        assert full['post_state_hash'] == session.st.hash
        assert model.support(model.move(1)[0])  # The prefix really moves protected slots.
        assert full['intermediate_motion_checked'] is False
        bad = review([dict(phase='macro', recipe=[dict(kind='word', moves=[1])])])
        assert bad['protected_conflicts'] == model.support(model.word_net([1])[0])
        assert not bad['meets_target_and_protection'] and frozen(session) == before
        checks.append('Whole prepare/macro/cleanup protection uses exact net support; prefix motion is not confused with final damage')

        session.save_prefs(dict(protected=[], rules=[dict(expr='nothing', style='solid')], pin_safety=False))
        x = next(p for p in model.bypos[orbit] if p != destination)
        damage = inverse + [dict(kind='star', orbit=orbit, node=model.bypos[orbit][x][0], sign=1)]
        plan = [dict(phase='macro', recipe=damage)]
        before = frozen(session)
        checked = review(plan)
        assert checked['target_exactly_solved'] and checked['finished_nonbuffer_losses'] > 0
        assert checked['finished_nonbuffer_protection_conflict'] and not checked['meets_target_and_protection']
        explicit = review(plan, PreparationIntent(orbit, destination, False))
        assert explicit['meets_target_and_protection'] and explicit['finished_nonbuffer_losses'] > 0
        assert frozen(session) == before
        checks.append('Hidden collateral and already-finished nonbuffer losses remain visible and guarded; explicit opt-out retains loss evidence')

        old = service.inspect(intent)['context']['context_id']
        session.save_prefs(dict(rules=[dict(expr='active', style='solid')]))
        assert service.inspect(intent, old)['context']['context_id'] == old
        session.save_prefs(dict(protected=[1]))
        rejected_unchanged(session, lambda: service.review(intent, segments, old))
        session.save_prefs(dict(protected=[]))
        old = service.inspect(intent)['context']['context_id']
        session.preview([dict(kind='word', moves=[19])])
        rejected_unchanged(session, lambda: service.review(intent, segments, old))
        old = service.inspect(intent)['context']['context_id']
        session.pending = None
        rejected_unchanged(session, lambda: service.review(intent, segments, old))
        old = service.inspect(intent)['context']['context_id']
        rejected_unchanged(session, lambda: PreparationService(session, lock).review(intent, segments, old))
        commit([dict(kind='word', moves=[1])])
        session.undo()
        rejected_unchanged(session, lambda: service.review(intent, segments, old))
        checks.append('Protection, pending-preview changes, undo and a new service invalidate context; display-only filters do not')

        invalid_intents = [PreparationIntent(True, destination), PreparationIntent(35, destination),
                           PreparationIntent(orbit, -1), PreparationIntent(orbit, model.np),
                           PreparationIntent(orbit, True), PreparationIntent(orbit, destination, 1)]
        for invalid in invalid_intents:
            rejected_unchanged(session, lambda v=invalid: service.inspect(v))
        bad_segments = [
            [], [dict(phase='prepare', recipe=inverse)],
            [dict(phase='macro', recipe=inverse, execute=True)],
            [dict(phase='cleanup', recipe=inverse), dict(phase='macro', recipe=inverse)],
            [dict(phase='macro', recipe=[dict(kind='word', moves=[True])])],
            [dict(phase='macro', recipe=[dict(kind='word', moves=[0])])],
            [dict(phase='macro', recipe=[dict(kind='star', orbit=orbit, node=-1)])],
            [dict(phase='macro', recipe=[dict(kind='word', moves=[1], commit=True)])],
            [dict(phase='prepare', recipe=[dict(kind='word', moves=[1])] * 129),
             dict(phase='macro', recipe=[dict(kind='word', moves=[-1])] * 129)],
        ]
        for invalid in bad_segments:
            rejected_unchanged(session, lambda p=invalid: review(p))
        rejected_unchanged(session, lambda: service.review(intent, segments, '0' * 64))
        event = threading.Event()
        event.set()
        previous_cancel = model.cancel_event
        model.cancel_event = event
        try:
            rejected_unchanged(session, lambda: review())
        finally:
            model.cancel_event = previous_cancel
        output = service.inspect(intent)
        saved = canonical(output)
        output['buffers']['A']['slots'][0] = -1
        output['stages'][0]['protected'] = True
        assert canonical(service.inspect(intent)) == saved
        checks.append('Strict malformed/aggregate/stale/cancel rejection is read-only; returned inspection data cannot corrupt the cache')

        old = service.inspect(intent)['context']['context_id']
        began, done = threading.Event(), threading.Event()
        errors = []
        def concurrent_reader():
            began.set()
            try:
                service.inspect(intent, old)
            except ValueError as error:
                errors.append(str(error))
            finally:
                done.set()
        with lock:
            thread = threading.Thread(target=concurrent_reader)
            thread.start()
            assert began.wait(2) and not done.wait(.05)
            commit([dict(kind='word', moves=[1])])
        thread.join(3)
        assert not thread.is_alive() and errors and 'stale' in errors[0].lower()
        session.undo()
        checks.append('A reader uses the existing writer lock and rejects stale context after a concurrent committed move')

        reviewed = review()
        expected_hash = reviewed['post_state_hash']
        commit(reviewed['plan']['recipe'])
        assert session.st.hash == expected_hash and np.array_equal(session.st.labels, model.ids)
        labels, head, prefs = session.st.labels.tobytes(), session.head, canonical(session.prefs)
        previous_context = service.inspect(intent)['context']['context_id']
        session.close()
        session = Session(model, work / 'session')
        service = PreparationService(session, lock)
        assert session.st.labels.tobytes() == labels and session.head == head and canonical(session.prefs) == prefs
        rejected_unchanged(session, lambda: service.inspect(intent, previous_context))
        checks.append('Explicit execution still uses existing preview/commit; all 259800 labels and preferences survive reopen with old analysis invalidated')
    finally:
        session.close()
    assert hashes == {name: hashlib.sha256((ROOT / name).read_bytes()).hexdigest() for name in source_names}
    report = dict(passed=True, checks=checks, labelled_slots=model.n,
                  seconds=time.perf_counter() - started, source_sha256=hashes,
                  scope='Real retained full-model Python/SQLite contracts on Windows; no HTTP/native UI integration or human-time/performance claim')
    (work / 'report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, indent=2), flush=True)


if __name__ == '__main__':
    main()
