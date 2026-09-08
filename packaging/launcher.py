"""Portable GUI launcher; normal startup never compiles or runs fixture windows."""
from __future__ import annotations
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

from engine_process import EngineProcess, local_request, redact
from directx_runtime import find_directx

VERSION = '0.3'
RESOURCES = Path(getattr(sys, '_MEIPASS', Path(__file__).resolve().parents[1])).resolve()
BUNDLE = Path(sys.executable).resolve().parent if getattr(sys, 'frozen', False) else RESOURCES
FAILURE_REPORT = None


def digest(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2), encoding='utf-8')


def verify_package(complete=False):
    manifest = json.loads((RESOURCES/'package-manifest.json').read_text(encoding='utf-8'))
    if manifest.get('version') != VERSION:
        raise RuntimeError('This package contains inconsistent application versions. Extract the complete ZIP again.')
    for item in manifest['files']:
        relative = Path(item['path'])
        if relative.is_absolute() or '..' in relative.parts:
            raise RuntimeError('Invalid package file manifest.')
        if not complete and not item.get('startup_required'):
            continue
        target = BUNDLE / relative
        if not target.is_file() or digest(target) != item['sha256']:
            raise RuntimeError('A packaged application file is missing or damaged: '+relative.as_posix()+'. Extract the complete ZIP again.')
    return manifest


def copy_if_changed(source, destination):
    source, destination = Path(source), Path(destination)
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.exists() and digest(source) == digest(destination):
        return
    temp = destination.with_name(destination.name+'.package.tmp')
    shutil.copyfile(source, temp)
    temp.replace(destination)


def prepare_native(data, build, manifest):
    # The engine has acquired this session's lock before any executable/cache
    # replacement. Existing keys/settings remain in the established data folder.
    source = RESOURCES/'native'
    host = build/'C600Native.exe'
    copy_if_changed(source/'C600Native.exe', host)
    copy_if_changed(source/'C600Native.exe.config', host.with_suffix('.exe.config'))
    runtime_source = source/'runtime'
    runtime = build/'runtime'/digest(runtime_source/'MPUlt.exe')[:16]
    runtime.mkdir(parents=True, exist_ok=True)
    installed_directx = find_directx(preferred=(runtime,))
    copy_if_changed(runtime_source/'MPUlt.exe', runtime/'MPUlt.exe')
    for name, installed in installed_directx.items():
        copy_if_changed(installed, runtime/name)
    old_builds = [data/('native-host-'+v) for v in ('0.2.4','0.2.3','0.2.2','0.2.1','0.2')]
    for name in ('MPUlt_puzzles.txt', 'MPUlt_settings.txt'):
        if not (runtime/name).exists():
            prior = next((old/'runtime'/runtime.name/name for old in old_builds if (old/'runtime'/runtime.name/name).exists()), runtime_source/name)
            copy_if_changed(prior, runtime/name)
    if not (build/'native_keys.json').exists():
        prior = next((old/'native_keys.json' for old in old_builds if (old/'native_keys.json').exists()), None)
        if prior is not None:
            copy_if_changed(prior, build/'native_keys.json')
    write_json(build/'diagnostics/build-info.json', {
        'version': VERSION, 'source_sha256': manifest['native_source_sha256'],
        'native_bits': 32, 'process_bits': 64, 'packaged': True,
        'host_sha256': digest(host), 'startup_fixture_run': False,
        'build_time_fixture_passed': manifest['build_time_fixture_passed'],
        'directx_runtime_sha256': {name:digest(path) for name,path in installed_directx.items()},
    })
    return host, runtime/'MPUlt.exe'


def labels(info):
    request = Request(info['base']+'/api/labels', headers={'X-C600-Token': info['token']})
    with build_opener(ProxyHandler({})).open(request, timeout=30) as response:
        raw = response.read(259800*4+1)
    if len(raw) != 259800*4:
        raise RuntimeError('Packaged engine did not return all 259800 labels.')
    return raw


def engine_test(args, data, build, manifest):
    if args.data is None or (data/'session.sqlite3').exists():
        raise ValueError('Engine verification requires --data naming a fresh test directory.')
    engine = EngineProcess(RESOURCES, data, build/'package-test-launch.json', build/'package-test-engine.log',
                           timeout=args.startup_timeout, engine_command=[str(BUNDLE/'C600Engine.exe')], hidden_console=True)
    with engine:
        health = local_request(engine.info, '/api/health')
        status = local_request(engine.info, '/api/status')
        before = labels(engine.info)
        state_hash = hashlib.sha256(before).hexdigest()
        if state_hash != status['state_hash']:
            raise RuntimeError('Packaged engine state/label hashes disagree.')
        result = {'passed': True, 'version': VERSION, 'packaged': bool(getattr(sys, 'frozen', False)),
                  'engine_pid': engine.info['pid'], 'engine_executable': health['python_executable'],
                  'labelled_slots': 259800, 'state_hash': state_hash,
                  'native_source_sha256': manifest['native_source_sha256'],
                  'startup_fixture_run': False, 'native_host_started': False}
        if args.parent_eof:
            result.update(parent_eof_test=True, cleanup='External harness must verify child exits after this launcher terminates.')
            write_json(args.engine_test, result)
            # Deliberately bypass Python cleanup, exercising the original parent
            # pipe EOF safety with a real packaged process and fresh test data.
            os._exit(0)
    result['cleanup'] = engine.cleanup_result
    if engine.cleanup_result.get('forced') or engine.cleanup_result.get('exit_code') != 0:
        raise RuntimeError('Packaged engine did not shut down cleanly.')
    with EngineProcess(RESOURCES, data, build/'package-reopen-launch.json', build/'package-reopen-engine.log',
                       timeout=args.startup_timeout, engine_command=[str(BUNDLE/'C600Engine.exe')], hidden_console=True) as reopened:
        if labels(reopened.info) != before:
            raise RuntimeError('Reopened packaged journal labels changed.')
    result['reopened_labels_byte_identical'] = True
    result['reopen_cleanup'] = reopened.cleanup_result
    write_json(args.engine_test, result)
    return 0


def main():
    global FAILURE_REPORT
    parser = argparse.ArgumentParser(description=f'C600 Studio {VERSION} portable native Windows application.')
    parser.add_argument('--data', type=Path, help='Alternate local session folder.')
    parser.add_argument('--startup-timeout', type=float, default=180)
    parser.add_argument('--verify-package', type=Path, metavar='REPORT', help=argparse.SUPPRESS)
    parser.add_argument('--engine-test', type=Path, metavar='REPORT', help=argparse.SUPPRESS)
    parser.add_argument('--parent-eof', action='store_true', help=argparse.SUPPRESS)
    args = parser.parse_args()
    FAILURE_REPORT = args.engine_test or args.verify_package
    if os.name != 'nt' or sys.maxsize <= 2**32:
        raise RuntimeError('This package requires 64-bit Windows.')
    if not 1 <= args.startup_timeout <= 3600:
        raise ValueError('--startup-timeout must be between 1 and 3600 seconds.')
    if args.parent_eof and not args.engine_test:
        raise ValueError('--parent-eof requires the isolated engine verification mode.')
    manifest = verify_package(complete=bool(args.verify_package or args.engine_test))
    if args.verify_package:
        write_json(args.verify_package, {'passed':True, 'version':VERSION,
                  'files_verified':len(manifest['files']), 'packaged':bool(getattr(sys,'frozen',False)),
                  'startup_fixture_run':False, 'native_host_started':False,
                  'native_source_sha256':manifest['native_source_sha256']})
        return 0
    data = (args.data or Path(os.environ.get('LOCALAPPDATA', str(Path.home())))/'C600Studio').resolve()
    build = data/('native-host-'+VERSION)
    diagnostics = build/'diagnostics'
    diagnostics.mkdir(parents=True, exist_ok=True)
    if args.engine_test:
        return engine_test(args, data, build, manifest)
    with (diagnostics/'packaged-launch.log').open('w', encoding='utf-8') as log:
        with EngineProcess(RESOURCES, data, build/'launch.json', build/'engine.log',
                           timeout=args.startup_timeout, progress=lambda value: print(redact(value),file=log,flush=True),
                           engine_command=[str(BUNDLE/'C600Engine.exe')], hidden_console=True) as engine:
            host, runtime = prepare_native(data, build, manifest)
            return subprocess.call([str(host), str(runtime), engine.info['base'], engine.info['token']], cwd=runtime.parent)


if __name__ == '__main__':
    try:
        raise SystemExit(main())
    except Exception as error:
        message = redact(str(error))
        if FAILURE_REPORT is not None:
            write_json(FAILURE_REPORT, {'passed':False,'error':message,'version':VERSION})
        elif os.name == 'nt':
            ctypes.windll.user32.MessageBoxW(None, message, 'C600 Studio could not start', 0x10)
        elif sys.stderr is not None:
            print(message, file=sys.stderr)
        raise SystemExit(1)
