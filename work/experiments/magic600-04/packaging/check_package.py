"""Check the actual frozen package without a GUI or installed Python on PATH."""
import argparse
import base64
from contextlib import contextmanager
import ctypes
import hashlib
import json
import os
from pathlib import Path
import shutil
import sqlite3
import subprocess
import sys
import time
from urllib.request import ProxyHandler, Request, build_opener

from package_contract import EXPERIMENT, sha, inspect_payload, verify_package, write_json

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT))
from engine_process import EngineProcess, local_request, validate_launch, _NoRedirect
from session_lock import SessionLock


def create_check_output(bundle, output):
    bundle, output = Path(bundle).resolve(), Path(output).resolve()
    if output.exists() or output == bundle or output.is_relative_to(bundle):
        raise ValueError('Use a fresh verification directory outside the package')
    output.mkdir(parents=True)
    return output


def profile_hashes(directory):
    directory = Path(directory)
    entries = list(directory.rglob('*'))
    if directory.is_symlink() or any(p.is_symlink() for p in entries):
        raise ValueError('Generated profile must not contain links')
    return {p.relative_to(directory).as_posix(): sha(p) for p in entries if p.is_file()}


def copy_generated_profile(source, destination, owner):
    source, destination, owner = map(lambda p: Path(p).resolve(), (source, destination, owner))
    if (source.parent != owner or destination.parent != owner or source == destination
            or destination.exists() or not (source / '.package-fixture.json').is_file()):
        raise ValueError('Copy only the generated closed fixture into a fresh owned directory')
    before = profile_hashes(source)
    shutil.copytree(source, destination)
    if profile_hashes(destination) != before:
        raise ValueError('Generated profile copy differs')
    assert_original_unchanged(source, before)
    return before


def assert_original_unchanged(source, expected):
    if profile_hashes(source) != expected:
        raise ValueError('The original rollback profile changed')


def retained_profile_records(directory):
    directory = Path(directory)
    wal = directory / 'session.sqlite3-wal'
    if wal.exists() and wal.stat().st_size:
        raise ValueError('Inspect only a closed, checkpointed generated profile')
    # Even mode=ro creates WAL sidecars; these fixtures have no live engine.
    db = sqlite3.connect((directory / 'session.sqlite3').as_uri() + '?mode=ro&immutable=1', uri=True)
    try:
        rows = db.execute('SELECT name,head,hash,prefs,created,labels FROM snapshots').fetchall()
    finally:
        db.close()
    return dict(checkpoints={row[0]: hashlib.sha256(json.dumps(row[:5], ensure_ascii=False,
        separators=(',', ':')).encode('utf-8') + b'\0' + row[5]).hexdigest() for row in rows},
        native_keys_sha256=sha(directory / 'native_keys.json'))


def assert_retained_profile_records(directory, expected):
    actual = retained_profile_records(directory)
    if actual['native_keys_sha256'] != expected['native_keys_sha256']:
        raise ValueError('Copied legacy key file changed')
    if any(actual['checkpoints'].get(name) != value for name, value in expected['checkpoints'].items()):
        raise ValueError('A pre-existing copied checkpoint was changed or removed')
    return actual


def finish_copy_report(source, original, report, path):
    report['original_after'] = profile_hashes(source)
    report['original_unchanged'] = report['original_after'] == original
    report['passed'] = bool(report.get('passed') and report['original_unchanged'])
    write_json(path, report)
    assert_original_unchanged(source, original)


def load_native_export(path, expected_sha256, runtime_sha256):
    path = Path(path)
    if path.stat().st_size > 64 * 1024 * 1024 or sha(path) != expected_sha256:
        raise ValueError('Native geometry witness hash or size differs')
    value = json.loads(path.read_text(encoding='utf-8-sig'))
    if (value.get('format') != 'MPUlt-native600-v1' or value.get('n') != 259800
            or value.get('executable_sha256') != runtime_sha256):
        raise ValueError('Native geometry witness does not bind the packaged MPUlt runtime')
    return value  # The frozen server must still verify every slot and generator.


def api_request(info, path, body=None, raw=False):
    validate_launch(info, info['launch_id'])
    request = Request(info['base'] + path,
        data=None if body is None else json.dumps(body).encode('utf-8'),
        headers={'X-C600-Token': info['token'], 'Content-Type': 'application/json'})
    with build_opener(ProxyHandler({}), _NoRedirect()).open(request, timeout=180) as response:
        content = response.read(32 * 1024 * 1024 + 1)
    if len(content) > 32 * 1024 * 1024:
        raise ValueError('Oversized package acceptance response')
    return content if raw else json.loads(content)


def api_command(info, body, route='/api/experiment/command'):
    result = api_request(info, route, body)
    if 'job' in result:
        deadline = time.monotonic() + 180
        while True:
            reply = api_request(info, '/api/job/' + result['job'])
            if reply.get('done'):
                if 'error' in reply:
                    raise RuntimeError(reply['error'])
                result = reply['result']
                break
            if time.monotonic() >= deadline:
                raise TimeoutError('Frozen acceptance job did not finish')
            time.sleep(.04)
    return result['result'] if route == '/api/experiment/session-log' else result


def require(value, message):
    if not value:
        raise ValueError(message)


def exercise_session_files(read, command, files, expected_labels, *, mpult):
    """Small fixed route scenario; caller owns an already-copied test Session."""
    files = Path(files)
    files.mkdir()  # Existing output is never overwritten.
    result = dict(passed=False, formats=[], native_window_started=False)
    label_hash = hashlib.sha256(expected_labels).hexdigest()
    require(read('/api/labels', raw=True) == expected_labels, 'Copied labels differ at route entry')
    command(dict(action='session-timer', command='pause'))
    before = command(dict(action='session-report'))
    resumed = command(dict(action='session-resume'))
    require(resumed['attempt_id'] == before['attempt_id'] and resumed['timer'] == before['timer'],
            'Resume changed the explicit attempt or paused timer')
    command(dict(action='checkpoint', name='Package before New'))
    command(dict(action='session-new'))
    home = read('/api/labels', raw=True)
    report = command(dict(action='session-report'))
    require(report['raw_full_home'] and report['completion'] is None, 'New fabricated completion or was not Home')
    command(dict(action='session-timer', command='pause'))
    home_log = command(dict(action='session-log-export', format='c600'))
    command(dict(action='restore', name='Package before New'))
    require(read('/api/labels', raw=True) == expected_labels, 'New recovery lost original labels')
    command(dict(action='session-scramble', count=3, seed=1729))
    require(read('/api/labels', raw=True) == expected_labels, 'Scramble staging executed its recipe')
    require(command(dict(action='session-report'))['completion'] is None, 'Staging fabricated completion')
    command(dict(action='operation-new'))

    document = command(dict(action='keymap-export'))['document']
    document['bindings'] = {'commands': {'F9': 'checkpoint'}}
    text = json.dumps(document, ensure_ascii=False, sort_keys=True)
    keyfile = files / 'chosen-keymap.json'; keyfile.write_text(text, encoding='utf-8')
    original_map = command(dict(action='keymap-export'))['document']
    checked = command(dict(action='keymap-import-check', text=keyfile.read_text(encoding='utf-8')))
    require(command(dict(action='keymap-export'))['document'] == original_map,
            'Keymap inspection changed bindings')
    command(dict(action='keymap-import', text=text, basis=checked['basis']))
    require(command(dict(action='keymap-export'))['document'] == document, 'Keymap roundtrip differs')
    require(read('/api/labels', raw=True) == expected_labels, 'Keymap import changed the puzzle')

    route = '/api/experiment/session-log' if mpult else '/api/experiment/command'
    for format in (('c600', 'mpult') if mpult else ('c600',)):
        exported = command(dict(action='session-log-export', format=format), route)
        payload = base64.b64decode(exported['data_base64'], validate=True)
        require(len(payload) == exported['bytes'] and hashlib.sha256(payload).hexdigest() == exported['file_sha256'],
                'Export bytes differ from their receipt')
        path = files / ('chosen.log' if format == 'mpult' else 'chosen.c600.json.gz')
        path.write_bytes(payload)
        saved = command(dict(action='session-save-log', format=format))
        require(saved['state_hash'] == label_hash, 'Saved log has another state')
        snapshot = read('/api/experiment/snapshot')
        command(dict(action='native-word', moves=[3], destination='live', state_hash=snapshot['hash']))
        changed = read('/api/labels', raw=True)
        require(changed != expected_labels, 'Fixed legal import fixture did not change')
        body = dict(format=format, data_base64=base64.b64encode(path.read_bytes()).decode('ascii'))
        checked = command(dict(action='session-log-inspect', **body), route)
        require(read('/api/labels', raw=True) == changed, 'Log inspection changed labels')
        require(checked['state_hash'] == label_hash, 'Checked log has another state')
        receipt = command(dict(action='session-log-apply', confirmation_id=checked['confirmation_id'], **body), route)
        require(receipt['import_applied'] and read('/api/labels', raw=True) == expected_labels,
                'Confirmed log import did not restore all labels')
        require(command(dict(action='keymap-export'))['document'] == document, 'Log import replaced current keybindings')
        require(command(dict(action='session-report'))['completion'] is None, 'Import fabricated completion')
        command(dict(action='restore', name=receipt['import_checkpoint']))
        require(read('/api/labels', raw=True) == changed, 'Import recovery lost previous labels')
        # Apply the same fixed file only after a new inspection at this context.
        checked = command(dict(action='session-log-inspect', **body), route)
        command(dict(action='session-log-apply', confirmation_id=checked['confirmation_id'], **body), route)
        result['formats'].append(format)

    body = dict(format='c600', data_base64=home_log['data_base64'])
    checked = command(dict(action='session-log-inspect', **body), route)
    receipt = command(dict(action='session-log-apply', confirmation_id=checked['confirmation_id'], **body), route)
    require(read('/api/labels', raw=True) == home, 'Solved log did not import exact Home labels')
    report = command(dict(action='session-report'))
    require(report['completion'] is None and report['recorded_completion'] is None,
            'Solved import created a solve summary')
    command(dict(action='restore', name=receipt['import_checkpoint']))
    require(read('/api/labels', raw=True) == expected_labels, 'Solved-import recovery changed the source state')
    result.update(passed=True, state_hash=label_hash, keybindings=document['bindings'],
                  files=profile_hashes(files), mpult_verified=mpult,
                  scope='Explicit known files and legal words; no automatic solving or native UI claim')
    return result


@contextmanager
def frozen_engine(bundle, data, output, name):
    engine = EngineProcess(bundle / '_internal/app', data, output / (name + '-launch.json'),
        output / (name + '-engine.log'), engine_command=[str(bundle / 'Magic600Engine.exe')], hidden_console=True)
    old_path = os.environ.get('PATH')
    try:
        os.environ['PATH'] = str(Path(os.environ['WINDIR']) / 'System32')
        engine.start()
    finally:
        if old_path is None: os.environ.pop('PATH', None)
        else: os.environ['PATH'] = old_path
    try:
        health = local_request(engine.info, '/api/health')
        require(Path(health['python_executable']).resolve() == (bundle / 'Magic600Engine.exe').resolve(),
                'Acceptance used an external interpreter')
        yield engine.info
    finally:
        engine.close()
        require(not engine.cleanup_result.get('forced') and engine.cleanup_result.get('exit_code') == 0,
                'Frozen acceptance engine did not shut down cleanly')


def copied_profile_check(bundle, output, native_export=None):
    owner = output / 'copied-profile-check'; owner.mkdir()
    source, copied = owner / 'generated-original', owner / 'copied-profile'
    with frozen_engine(bundle, source, owner, 'generate') as info:
        snapshot = api_request(info, '/api/experiment/snapshot')
        api_command(info, dict(action='native-word', moves=[1], destination='live', state_hash=snapshot['hash']))
        api_command(info, dict(action='checkpoint', name='Generated legacy unfinished'))
        expected = api_request(info, '/api/labels', raw=True)
        expected_head = api_request(info, '/api/status')['head']
    # Only this newly generated, closed database is reshaped to retained legacy
    # preference storage. No mechanical labels, recipes or journal are injected.
    legacy = dict(keybinds={'F9': 'checkpoint'}, macro_library={
        'Generated legacy method': {'recipe': [{'kind': 'word', 'moves': [1]}]}})
    with sqlite3.connect(source / 'session.sqlite3') as db:
        prefs = json.loads(db.execute("SELECT value FROM meta WHERE key='prefs'").fetchone()[0])
        prefs.update(legacy); prefs['layout'].pop('magic600_experiment', None)
        db.execute("UPDATE meta SET value=? WHERE key='prefs'", (json.dumps(prefs),))
    db.close()
    (source / 'native_keys.json').write_text(json.dumps({'F9': 'checkpoint'}), encoding='utf-8')
    write_json(source / '.package-fixture.json', dict(scope='Generated legacy-shaped storage, not a historical application migration',
        head=expected_head, state_hash=hashlib.sha256(expected).hexdigest()))
    original = copy_generated_profile(source, copied, owner)
    retained = retained_profile_records(source)
    report = dict(passed=False, scope='Generated closed legacy-shaped copy and byte-identical original rollback artifact; not historical-version UI migration',
                  original_before=original, retained_records_before=retained, geometry_verified=False)
    try:
        with frozen_engine(bundle, copied, owner, 'copy') as info:
            read = lambda path, raw=False: api_request(info, path, raw=raw)
            command = lambda body, route='/api/experiment/command': api_command(info, body, route)
            status = read('/api/status'); snapshot = read('/api/experiment/snapshot')
            require(read('/api/labels', raw=True) == expected and status['head'] == expected_head, 'Copied profile lost journal state')
            require(all(status['prefs'].get(k) == v for k, v in legacy.items()), 'Copied profile lost legacy preferences')
            require(not snapshot['workspace']['keybinds'] and not snapshot['workspace']['personal_macros'],
                    'Legacy preferences were silently adopted as new workspace bindings/macros')
            if native_export is not None:
                verified = api_command(info, native_export, '/api/native/handshake')
                require(verified['matched_stickers'] == 259800 and verified['matched_generators'] == 1200,
                        'Frozen native handshake is incomplete')
                report['geometry_verified'] = True
            report['routes'] = exercise_session_files(read, command, owner / 'files', expected, mpult=report['geometry_verified'])
        report['retained_records_after_routes'] = assert_retained_profile_records(copied, retained)
        with frozen_engine(bundle, copied, owner, 'reopen') as info:
            require(api_request(info, '/api/labels', raw=True) == expected, 'Copied profile reopen changed labels')
            status = api_request(info, '/api/status')
            require(all(status['prefs'].get(k) == v for k, v in legacy.items()), 'Reopen lost legacy preferences')
            document = api_command(info, dict(action='keymap-export'))['document']
            require(document['bindings'] == report['routes']['keybindings'], 'Imported keymap did not survive reopen')
        report['retained_records_after_reopen'] = assert_retained_profile_records(copied, retained)
        report.update(passed=report['geometry_verified'], copied_reopen=True,
                      missing=[] if report['geometry_verified'] else ['Actual native geometry witness: MPUlt file routes not verified'])
    finally:
        finish_copy_report(source, original, report, output / 'copied-profile.json')
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('bundle', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--native-export', type=Path, help='Actual saved native Geometry export; never a synthetic fixture')
    parser.add_argument('--native-export-sha256', help='Hash recorded with that native export')
    args = parser.parse_args()
    if bool(args.native_export) != bool(args.native_export_sha256):
        parser.error('Supply both --native-export and --native-export-sha256')
    bundle, output = args.bundle.resolve(), args.output.resolve()
    output = create_check_output(bundle, output)
    before = {p.relative_to(bundle).as_posix(): sha(p) for p in bundle.rglob('*') if p.is_file()}
    checks = []
    report = dict(passed=False, final_acceptance=False, checks=checks,
        scope='Actual frozen Windows resources, source-bound invariant loader, explicit engine transaction, '
              'reopen and parent EOF. No native window, GPU, clean-machine or human-solving acceptance.')
    env = os.environ.copy()
    env['PATH'] = str(Path(os.environ['WINDIR']) / 'System32')
    env.update(PYTHONHOME=str(output / 'absent-python'), PYTHONPATH=str(output / 'absent-modules'))
    app = bundle / 'Magic600Cell.exe'
    def run(arguments, name, expected=0, timeout=240):
        path = output / name
        process = subprocess.run([str(app), *arguments, str(path)], env=env, cwd=output,
            stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
            creationflags=subprocess.CREATE_NO_WINDOW, timeout=timeout)
        detail = json.loads(path.read_text(encoding='utf-8')) if path.exists() else {
            'error': process.stdout.decode('utf-8', 'replace')}
        if process.returncode != expected:
            raise RuntimeError('Frozen check failed: ' + json.dumps(detail))
        return detail
    try:
        manifest = verify_package(bundle)
        inspect_payload(bundle)
        report.update(version=manifest['version'], model=manifest['model_id'],
                      manifest_sha256=sha(bundle / '_internal/package-manifest.json'),
                      native_sha256=manifest['native']['executable_sha256'])
        for required in ('LICENSE', 'CREDITS.md', 'licenses/MPUlt-MIT.txt',
                         'licenses/Python-LICENSE.txt', 'licenses/NumPy/LICENSE.txt',
                         'licenses/PyInstaller/COPYING.txt'):
            if (bundle / required).stat().st_size < 100:
                raise ValueError('Missing license material: ' + required)
        if 'Andrey Astrelin' not in (bundle / 'README.md').read_text(encoding='utf-8'):
            raise ValueError('Prominent MPUlt attribution is missing')
        checks.append(dict(name='All allowlisted files, expected PE architectures, licenses and clean payload', passed=True))
        resources = run(['--verify-package'], 'resources.json')
        if not resources['passed'] or not resources['packaged'] or resources['native_host_started']:
            raise ValueError('Resource check is not the expected frozen non-GUI invocation')
        checks.append(dict(name='Frozen launcher works with Python/compiler excluded from PATH', passed=True))
        data = output / 'isolated Unicode session 测试'
        engine = run(['--data', str(data), '--engine-test'], 'engine.json')
        if not (engine['passed'] and engine['packaged'] and engine['experiment_route']
                and engine['explicit_preview_commit_undo'] and engine['reopened_labels_byte_identical']
                and engine['labelled_slots'] == 259800 and engine['invariant_status'] == 'PassNecessary'
                and engine['endgame_choices'] == 60 and not engine['native_host_started']):
            raise ValueError('Frozen experiment engine contract is incomplete')
        if Path(engine['engine_executable']).resolve() != (bundle / 'Magic600Engine.exe').resolve():
            raise ValueError('Test used an external interpreter')
        lock = SessionLock(data)
        lock.close()
        checks.append(dict(name='Frozen 0.4 HTTP routes, exact proof loading, all labels, explicit preview/commit/undo and reopen', passed=True,
                           result='engine.json', sha256=sha(output / 'engine.json')))
        eof_data = output / 'parent-eof-session'
        eof = run(['--data', str(eof_data), '--parent-eof', '--engine-test'], 'parent-eof.json')
        kernel = ctypes.WinDLL('kernel32', use_last_error=True)
        kernel.OpenProcess.argtypes = [ctypes.c_uint32, ctypes.c_int, ctypes.c_uint32]
        kernel.OpenProcess.restype = ctypes.c_void_p
        kernel.WaitForSingleObject.argtypes = [ctypes.c_void_p, ctypes.c_uint32]
        kernel.CloseHandle.argtypes = [ctypes.c_void_p]
        handle = kernel.OpenProcess(0x100000, False, eof['engine_pid'])
        if handle:
            try:
                ended = kernel.WaitForSingleObject(handle, 30000) == 0
                if not ended:
                    info = json.loads((eof_data / 'native-host-0.4/package-test-launch.json').read_text())
                    if info['pid'] != eof['engine_pid']:
                        raise ValueError('Owned EOF fixture PID changed')
                    local_request(info, '/api/shutdown', {'launch_id': info['launch_id']})
                    kernel.WaitForSingleObject(handle, 15000)
                    raise RuntimeError('Owned frozen engine survived parent EOF')
            finally:
                kernel.CloseHandle(handle)
        lock = SessionLock(eof_data)
        lock.close()
        checks.append(dict(name='Hard launcher exit releases the owned engine and session lock', passed=True))
        runtime = run(['--data', str(output / 'runtime-check'), '--runtime-check'], 'runtime.json')
        if not runtime['passed'] or runtime['native_host_started']:
            raise ValueError('Runtime preparation unexpectedly launched a native window')
        checks.append(dict(name='Installed MDX discovery and local cache copy; no renderer invoked', passed=True))
        geometry = None
        if args.native_export:
            runtime_hash = next(row['sha256'] for row in manifest['files']
                if row['path'] == '_internal/app/native/runtime/MPUlt.exe')
            geometry = load_native_export(args.native_export, args.native_export_sha256, runtime_hash)
            report['native_export_sha256'] = args.native_export_sha256
        compatibility = copied_profile_check(bundle, output, geometry)
        checks.append(dict(name='Generated copied profile, Session/log/keymap routes and reopen',
            passed=compatibility['passed'], result='copied-profile.json',
            sha256=sha(output / 'copied-profile.json')))
        audit = bundle / '_internal/app' / EXPERIMENT / 'evidence/orbit-invariants-20260916-generators.json'
        original = audit.read_bytes()
        try:
            audit.write_bytes(original + b'\n')
            damaged = run(['--verify-package'], 'damaged-proof.json', expected=1)
            if damaged['passed'] or 'missing or damaged' not in damaged['error']:
                raise ValueError('Altered proof did not fail before opening a session')
        finally:
            audit.write_bytes(original)
        checks.append(dict(name='Changed invariant artifact refused before engine or native launch', passed=True))
        after = {p.relative_to(bundle).as_posix(): sha(p) for p in bundle.rglob('*') if p.is_file()}
        if before != after:
            raise ValueError('Portable runs changed files in the application folder')
        verify_package(bundle)
        require(compatibility['passed'], 'Package acceptance incomplete: provide an actual native Geometry export for MPUlt checks')
        report.update(passed=True, packaged_files_unchanged=True)
    except BaseException as error:
        report['error'] = type(error).__name__ + ': ' + str(error)
        raise
    finally:
        write_json(output / 'summary.json', report)
    print(json.dumps(report), flush=True)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
