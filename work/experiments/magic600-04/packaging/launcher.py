"""Isolated 0.4 portable launcher; never compiles or opens fixture windows."""
import argparse
import ctypes
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time
from urllib.request import ProxyHandler, Request, build_opener

from package_contract import VERSION, EXPERIMENT, sha, verify_package, write_json

RESOURCES = Path(getattr(sys, '_MEIPASS', Path(__file__).resolve().parent)).resolve()
BUNDLE = Path(sys.executable).resolve().parent if getattr(sys, 'frozen', False) else RESOURCES.parent
APP = RESOURCES / 'app'
sys.dont_write_bytecode = True
sys.path[:0] = [str(APP), str(APP / 'native')]
from engine_process import EngineProcess, local_request, redact, validate_launch, _NoRedirect
from directx_runtime import find_directx

FAILURE_REPORT = None


def copy_if_changed(source, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists() and sha(source) == sha(destination):
        return
    temp = destination.with_name(destination.name + '.package.tmp')
    shutil.copyfile(source, temp)
    temp.replace(destination)


def prepare_native(data):
    build = data / 'native-host-0.4'
    host = build / 'Magic600Native.exe'
    copy_if_changed(RESOURCES / 'native/Magic600Native.exe', host)
    copy_if_changed(RESOURCES / 'native/Magic600Native.exe.config', host.with_suffix('.exe.config'))
    source = APP / 'native/runtime'
    runtime = build / 'runtime' / sha(source / 'MPUlt.exe')[:16]
    runtime.mkdir(parents=True, exist_ok=True)
    installed = find_directx(preferred=(runtime,))
    copy_if_changed(source / 'MPUlt.exe', runtime / 'MPUlt.exe')
    for name, path in installed.items():
        copy_if_changed(path, runtime / name)
    # Settings are user-owned only after the first launch. Never take them from
    # another live profile or overwrite their changes on an application update.
    for name in ('MPUlt_puzzles.txt', 'MPUlt_settings.txt'):
        if not (runtime / name).exists():
            copy_if_changed(source / name, runtime / name)
    if sha(runtime / 'MPUlt_puzzles.txt') != sha(source / 'MPUlt_puzzles.txt'):
        raise RuntimeError('The local runtime puzzle definition differs from this package. Use a fresh 0.4 data folder.')
    return host, runtime / 'MPUlt.exe'


def request(info, path, body=None, raw=False):
    validate_launch(info, info['launch_id'])
    payload = None if body is None else json.dumps(body).encode('utf-8')
    req = Request(info['base'] + path, data=payload,
        headers={'X-C600-Token': info['token'], 'Content-Type': 'application/json'})
    with build_opener(ProxyHandler({}), _NoRedirect()).open(req, timeout=180) as response:
        value = response.read(32 * 1024 * 1024 + 1)
    if len(value) > 32 * 1024 * 1024:
        raise RuntimeError('Oversized package-test response')
    return value if raw else json.loads(value)


def command(info, path, body):
    pending = request(info, path, body)
    deadline = time.monotonic() + 180
    while not pending.get('done'):
        if 'job' not in pending:
            raise RuntimeError('Package test expected an explicit job receipt')
        job = pending['job']
        while True:
            if time.monotonic() >= deadline:
                raise RuntimeError('Package-test command timed out')
            result = request(info, '/api/job/' + job)
            if result.get('done'):
                pending = result
                break
            time.sleep(.04)
    if 'error' in pending:
        raise RuntimeError(pending['error'])
    return pending['result']


def engine_test(args, data, manifest):
    if args.data is None or data.exists():
        raise ValueError('Engine verification requires --data naming a fresh, nonexistent test directory')
    build = data / 'native-host-0.4'
    engine = EngineProcess(APP, data, build / 'package-test-launch.json', build / 'package-test-engine.log',
        engine_command=[str(BUNDLE / 'Magic600Engine.exe')], hidden_console=True,
        timeout=args.startup_timeout)
    with engine:
        health = local_request(engine.info, '/api/health')
        before = request(engine.info, '/api/labels', raw=True)
        if len(before) != 259800 * 4:
            raise RuntimeError('Frozen engine did not return all 259800 labels')
        result = dict(passed=True, version=VERSION, packaged=bool(getattr(sys, 'frozen', False)),
            model=health['model_id'], engine_pid=engine.info['pid'], engine_executable=health['python_executable'],
            labelled_slots=259800, initial_hash=hashlib.sha256(before).hexdigest(),
            native_host_started=False, startup_fixture_run=False,
            native_sha256=manifest['native']['executable_sha256'])
        if args.parent_eof:
            result['parent_eof_requested'] = True
            write_json(args.engine_test, result)
            os._exit(0)
        snapshot = request(engine.info, '/api/experiment/snapshot')
        if snapshot['hash'] != result['initial_hash'] or snapshot['model'] != result['model']:
            raise RuntimeError('Frozen experiment snapshot does not match the authoritative labels')
        residual = command(engine.info, '/api/experiment/command',
            dict(action='inspect-residual', review_context_id=snapshot['review_context']['id']))
        if residual['current']['invariant_status']['status'] != 'PassNecessary':
            raise RuntimeError('Packaged pinned invariant proof did not load: ' +
                               str(residual['current']['invariant_status']))
        options = command(engine.info, '/api/experiment/command',
                          dict(action='endgame-options', orbit=34, position=17810))
        if len(options['choices']) != 60 or options['certificate']['status'] != 'Verified':
            raise RuntimeError('Packaged explicit A5 endgame data is unavailable')
        route = '/api/experiment/command'
        command(engine.info, route, dict(action='goal', goal='prepare'))
        command(engine.info, route, dict(action='draft', phase='prepare',
                                        recipe=[dict(kind='word', moves=[1])]))
        review = command(engine.info, route, dict(action='review'))['review']
        if review['status'] != 'Ready':
            raise RuntimeError('Explicit package-test Prepare was not permitted')
        command(engine.info, route, dict(action='preview', review_id=review['id']))
        if request(engine.info, '/api/labels', raw=True) != before:
            raise RuntimeError('Preview changed authoritative labels')
        command(engine.info, route, dict(action='commit'))
        committed = request(engine.info, '/api/labels', raw=True)
        if committed == before:
            raise RuntimeError('Explicit legal package-test commit had no effect')
        command(engine.info, route, dict(action='undo'))
        if request(engine.info, '/api/labels', raw=True) != before:
            raise RuntimeError('Package-test undo did not restore all labels')
        result.update(experiment_route=True, invariant_status='PassNecessary',
            endgame_group=options['group'], endgame_choices=len(options['choices']),
            explicit_preview_commit_undo=True, state_hash=hashlib.sha256(before).hexdigest())
    result['cleanup'] = engine.cleanup_result
    if engine.cleanup_result.get('forced') or engine.cleanup_result.get('exit_code') != 0:
        raise RuntimeError('Frozen engine did not shut down cleanly')
    reopened = EngineProcess(APP, data, build / 'package-reopen-launch.json', build / 'package-reopen-engine.log',
        engine_command=[str(BUNDLE / 'Magic600Engine.exe')], hidden_console=True,
        timeout=args.startup_timeout)
    with reopened:
        if request(reopened.info, '/api/labels', raw=True) != before:
            raise RuntimeError('Reopened frozen journal labels changed')
        snapshot = request(reopened.info, '/api/experiment/snapshot')
        if snapshot['hash'] != result['state_hash']:
            raise RuntimeError('Reopened experiment state differs')
    result.update(reopened_labels_byte_identical=True, reopen_cleanup=reopened.cleanup_result)
    if reopened.cleanup_result.get('forced') or reopened.cleanup_result.get('exit_code') != 0:
        raise RuntimeError('Reopened frozen engine did not shut down cleanly')
    write_json(args.engine_test, result)
    return 0


def main():
    global FAILURE_REPORT
    parser = argparse.ArgumentParser(description='Magic 600 Cell 0.4 native workspace (isolated validation package).')
    parser.add_argument('--data', type=Path, help='Separate session folder; no automatic migration from older versions.')
    parser.add_argument('--mode', choices=('g1', 'g2'), default='g2')
    parser.add_argument('--startup-timeout', type=float, default=180)
    parser.add_argument('--verify-package', type=Path, metavar='REPORT', help=argparse.SUPPRESS)
    parser.add_argument('--engine-test', type=Path, metavar='REPORT', help=argparse.SUPPRESS)
    parser.add_argument('--runtime-check', type=Path, metavar='REPORT', help=argparse.SUPPRESS)
    parser.add_argument('--parent-eof', action='store_true', help=argparse.SUPPRESS)
    args = parser.parse_args()
    FAILURE_REPORT = args.engine_test or args.verify_package or args.runtime_check
    if os.name != 'nt' or sys.maxsize <= 2**32:
        raise RuntimeError('This package requires 64-bit Windows')
    if not 1 <= args.startup_timeout <= 3600:
        raise ValueError('Startup timeout must be between 1 and 3600 seconds')
    if args.parent_eof and not args.engine_test:
        raise ValueError('--parent-eof requires isolated engine verification')
    manifest = verify_package(BUNDLE)
    if args.verify_package:
        write_json(args.verify_package, dict(passed=True, version=VERSION,
            files_verified=len(manifest['files']), packaged=bool(getattr(sys, 'frozen', False)),
            native_host_started=False, startup_fixture_run=False))
        return 0
    data = (args.data or Path(os.environ.get('LOCALAPPDATA', str(Path.home()))) / 'Magic600Cell' / '0.4').resolve()
    if data == BUNDLE or data.is_relative_to(BUNDLE):
        raise ValueError('Keep session data outside the application folder')
    if args.engine_test:
        return engine_test(args, data, manifest)
    if args.runtime_check:
        if args.data is None or data.exists():
            raise ValueError('Runtime verification requires a fresh explicit --data directory')
        host, runtime = prepare_native(data)
        write_json(args.runtime_check, dict(passed=True, host_sha256=sha(host),
            runtime_sha256=sha(runtime), installed_directx={name: sha(runtime.parent / name)
                for name in ('Microsoft.DirectX.dll', 'Microsoft.DirectX.Direct3D.dll', 'Microsoft.DirectX.Direct3DX.dll')},
            native_host_started=False, scope='Installed runtime discovery/copy only; no render or GPU claim'))
        return 0
    build = data / 'native-host-0.4'
    with EngineProcess(APP, data, build / 'launch.json', build / 'engine.log',
            engine_command=[str(BUNDLE / 'Magic600Engine.exe')], hidden_console=True,
            timeout=args.startup_timeout) as engine:
        # Session lock is held before updating this profile's native cache.
        host, runtime = prepare_native(data)
        return subprocess.call([str(host), str(runtime), engine.info['base'], engine.info['token'], args.mode],
                               cwd=runtime.parent)


if __name__ == '__main__':
    try:
        raise SystemExit(main())
    except Exception as error:
        message = redact(str(error))
        if FAILURE_REPORT is not None:
            write_json(FAILURE_REPORT, dict(passed=False, error=message, version=VERSION))
        elif os.name == 'nt':
            ctypes.windll.user32.MessageBoxW(None, message, 'Magic 600 Cell could not start', 0x10)
        elif sys.stderr is not None:
            print(message, file=sys.stderr)
        raise SystemExit(1)
