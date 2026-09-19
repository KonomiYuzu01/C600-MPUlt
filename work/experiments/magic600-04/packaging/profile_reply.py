"""Bounded diagnostic of current paired-reply components; never a GUI benchmark."""
from pathlib import Path
import argparse
import cProfile
import hashlib
import json
import pstats
import statistics
import sys
import threading
import time

HERE = Path(__file__).resolve().parents[1]
ROOT = HERE.parents[2]
sys.path[:0] = [str(HERE), str(ROOT)]
from adapter import Workbench
from core import Model, canonical, digest
from cycle_projection import inspect_cycle_display
from draft_inspection import inspect_phase
from local_geometry import cell_state
from server import NativeSnapshotCache
from session import Session


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--profile', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=False)
    sources = [p for p in HERE.glob('*.py') if not p.name.startswith(('native_', 'record_'))]
    sources += list(ROOT.glob('*.py')) + [Path(__file__)]
    before = {str(p.relative_to(ROOT)): sha(p) for p in sources}
    native_profile = json.loads(args.profile.read_text(encoding='utf-8'))
    receipt = dict(native_profile)
    claimed = receipt.pop('profile_sha256')
    receipt['translations'] = {int(k): v for k, v in receipt['translations'].items()}
    assert digest(canonical(receipt).encode()) == claimed
    assert native_profile['native_executable_sha256'] == sha(ROOT / 'native/runtime/MPUlt.exe')
    m = Model()
    assert native_profile['model_id'] == m.model_id
    assert native_profile['matched_stickers'] == m.n and native_profile['matched_generators'] == 1200
    s = Session(m, args.output / 'isolated-session')
    wb = Workbench(s, threading.RLock())
    native = NativeSnapshotCache()
    rows, profiles = [], {}
    try:
        wb.command(dict(action='fixture', name='e1'), response_snapshot=False)
        wb.command(dict(action='draft', phase='macro', recipe=[dict(kind='star', orbit=33, node=0, sign=-1)]), response_snapshot=False)
        wb.command(dict(action='draft', phase='cleanup', recipe=[dict(kind='star', orbit=33, node=11, sign=-1)]), response_snapshot=False)
        wb.command(dict(action='review'), response_snapshot=False)
        initial_hash, initial_head = s.st.hash, s.head
        since = native.read(s, native_profile, None, 2)['revision']
        metadata = dict(mode='Current', scope='selected-macro', orbit=None, position=None,
                        cycle_after=None, residual_after=None, edge_offset=0)

        def pipeline(body):
            nonlocal since
            times = {}
            def phase(name, function):
                start = time.perf_counter()
                value = function()
                times[name] = (time.perf_counter() - start) * 1000
                return value
            with wb.lock:
                result = phase('command_ms', lambda: wb.command(body, response_snapshot=False) if body else None)
                inspection = phase('phase_ms', lambda: inspect_phase(wb, 'actual', 'o33-n0-inverse'))
                cycles, note = phase('cycles_ms', lambda: inspect_cycle_display(wb, metadata, 'o33-n0-inverse'))
                work = phase('snapshot_ms', lambda: wb.snapshot(prediction=True))
                cell = wb.w['view'].get('local_center', 1)
                local = phase('cell_ms', lambda: cell_state(wb, cell, wb.selected_frame(wb.w['bank'], cell)))
                arrays = phase('native_ms', lambda: native.read(s, native_profile, since, 2))
                since = arrays['revision']
                reply = dict(result=result, work=work, local_cell=local, phase_inspection=inspection,
                    phase_inspection_error=None, cycle_projection=cycles, cycle_projection_error=None,
                    cycle_projection_note=note, native_snapshot=arrays)
                payload = phase('json_ms', lambda: canonical(reply).encode('utf-8'))
                assert s.st.hash == initial_hash and s.head == initial_head
                assert work['hash'] == arrays['state']['state_hash'] == initial_hash
                times.update(total_ms=sum(times.values()), reply_bytes=len(payload),
                    work_bytes=len(canonical(work).encode()), cell_bytes=len(canonical(local).encode()),
                    cycles_bytes=len(canonical(cycles).encode()), phase_bytes=len(canonical(inspection).encode()),
                    native_bytes=len(canonical(arrays).encode()), result_bytes=len(canonical(result).encode()),
                    native_mode=arrays['mode'], predicted='predicted' in work)
                return times, payload

        cases = [('snapshot', lambda i: None),
                 ('bank', lambda i: dict(action='bank', id='33-I' if i % 2 else '33-A')),
                 ('inspect', lambda i: dict(action='inspect', identity=35778 if i % 2 else 26789)),
                 ('grips', lambda i: dict(action='grips', cell=55 if i % 2 else 17))]
        for name, body in cases:
            # Warm both explicit alternatives before recording five samples.
            for i in (-2, -1):
                pipeline(body(i))
            for i in range(5):
                row, payload = pipeline(body(i))
                row.update(case=name, sample=i)
                rows.append(row)
            profiler = cProfile.Profile()
            profiler.enable()
            _, payload = pipeline(body(1))
            profiler.disable()
            profiler.dump_stats(str(args.output / (name + '.prof')))
            with (args.output / (name + '-profile.txt')).open('w', encoding='utf-8') as stream:
                stats = pstats.Stats(profiler, stream=stream).strip_dirs().sort_stats('cumulative')
                stats.print_stats()
            (args.output / (name + '-reply.json')).write_bytes(payload)
            profiles[name] = [dict(file=file, line=line, function=function, calls=calls,
                own_ms=own * 1000, cumulative_ms=cumulative * 1000)
                for (file, line, function), (primitive, calls, own, cumulative, callers)
                in sorted(pstats.Stats(profiler).stats.items(), key=lambda item: item[1][3], reverse=True)[:20]]
            print(name, round(statistics.median(row['total_ms'] for row in rows if row['case'] == name), 3),
                  'ms; payload', len(payload), flush=True)
        after = {str(p.relative_to(ROOT)): sha(p) for p in sources}
        summary = []
        for name, _ in cases:
            selected = [row for row in rows if row['case'] == name]
            summary.append(dict(case=name, samples=5,
                median={key: statistics.median(row[key] for row in selected)
                    for key in selected[0] if key.endswith('_ms') or key.endswith('_bytes')},
                predicted=all(row['predicted'] for row in selected)))
        report = dict(scope='Pure isolated current paired-reply components with recorded verified native mapping. '
            'Root native work may share CPU. Relative-phase diagnosis, not quiet benchmark, HTTP, '
            'native adoption, physical input, rendering or GPU latency.',
            fixture='E1; explicit O33 n0 inverse plus n11 inverse Cleanup, fresh reviewed draft; no subsequent commit',
            model=m.model_id, profile_sha256=claimed, profile_file_sha256=sha(args.profile),
            native_executable_sha256=native_profile['native_executable_sha256'],
            sources_before=before, sources_after=after, sources_unchanged=before == after,
            committed_hash=initial_hash, head=initial_head, summary=summary, rows=rows, profiles=profiles)
        (args.output / 'report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
        print('Source hashes unchanged:', before == after, flush=True)
    finally:
        s.close()


if __name__ == '__main__':
    main()
