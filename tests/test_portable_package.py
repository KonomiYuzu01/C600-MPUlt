"""Execute a built portable package with no Python/compiler locations on PATH.

This verifies resources and full frozen-engine ownership/persistence. Actual
DirectX UI/component regressions run separately against the same built engine.
"""
from __future__ import annotations
import argparse
from contextlib import closing
import ctypes
import hashlib
import json
import os
from pathlib import Path
import struct
import subprocess
import sys
import time

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from session_lock import SessionLock
from engine_process import local_request


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('bundle',type=Path)
    parser.add_argument('output',type=Path)
    args=parser.parse_args()
    bundle,output=args.bundle.resolve(),args.output.resolve()
    if output.exists():raise RuntimeError('Use a fresh verification output directory.')
    output.mkdir(parents=True)
    app=bundle/'C600Studio.exe'
    manifest=json.loads((bundle/'_internal/package-manifest.json').read_text(encoding='utf-8'))
    version=manifest['version']
    before={p.relative_to(bundle).as_posix():sha(p) for p in bundle.rglob('*') if p.is_file()}
    checks=[]
    names=set(before)
    for required in ('LICENSE','CREDITS.md','licenses/MPUlt-MIT.txt','licenses/Inno-Setup-LICENSE.txt','licenses/Python-LICENSE.txt','licenses/NumPy/LICENSE.txt'):
        assert required in names and (bundle/required).stat().st_size>100,required
    assert any(name.startswith('licenses/PyInstaller/') for name in names)
    assert 'licenses/MPUlt-LICENSE.txt' not in names
    assert 'Andrey Astrelin' in (bundle/'README.md').read_text(encoding='utf-8')
    assert 'MIT License' in (bundle/'LICENSE').read_text(encoding='utf-8')
    checks.append({'name':'Prominent Andrey attribution, C600 MIT license, upstream MIT filename, Python/NumPy/PyInstaller/Inno notices are present','passed':True})
    assert not any(Path(name).name.lower() in ('microsoft.directx.dll','microsoft.directx.direct3d.dll','microsoft.directx.direct3dx.dll') for name in names)
    assert not any(Path(name).name.lower().endswith('regression.exe') or Path(name).name.lower()=='csc.exe' for name in names)
    assert not any(Path(name).suffix.lower() in ('.sqlite3','.log','.pdb') for name in names)
    for relative,machine in (('C600Studio.exe',0x8664),('C600Engine.exe',0x8664),('_internal/native/C600Native.exe',0x14c)):
        raw=(bundle/relative).read_bytes();offset=struct.unpack_from('<I',raw,0x3c)[0]
        assert raw[offset:offset+4]==b'PE\0\0' and struct.unpack_from('<H',raw,offset+4)[0]==machine
    checks.append({'name':'Expected x64 launcher/engine and x86 host; no Microsoft DLL redistribution, compiler, fixture EXE, personal logs or databases','passed':True})
    env=os.environ.copy()
    env['PATH']=str(Path(os.environ['WINDIR'])/'System32')
    env.update(PYTHONHOME=str(output/'absent-python'),PYTHONPATH=str(output/'absent-modules'))
    def run(arguments,report,timeout=120):
        result=subprocess.run([str(app),*arguments],env=env,cwd=output,timeout=timeout,
                              stdin=subprocess.DEVNULL,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,
                              creationflags=subprocess.CREATE_NO_WINDOW)
        if result.returncode:
            detail=report.read_text(encoding='utf-8') if report.exists() else result.stdout.decode('utf-8','replace')
            raise RuntimeError('Packaged verification failed: '+detail)
        return json.loads(report.read_text(encoding='utf-8'))
    verification=output/'resources.json'
    resource_result=run(['--verify-package',str(verification)],verification)
    assert resource_result['passed'] and resource_result['packaged'] and not resource_result['startup_fixture_run']
    assert resource_result['version']==version
    checks.append({'name':'Frozen launcher verifies all packaged files without Python/compiler PATH or fixture windows','passed':True,'files':resource_result['files_verified']})
    data=output/'isolated session Unicode 测试'
    report=output/'frozen-engine.json'
    result=run(['--engine-test',str(report),'--data',str(data)],report)
    assert result['passed'] and result['packaged'] and result['labelled_slots']==259800
    assert result['version']==version
    assert result['reopened_labels_byte_identical'] and not result['startup_fixture_run'] and not result['native_host_started']
    assert Path(result['engine_executable']).resolve()==(bundle/'C600Engine.exe').resolve()
    for cleanup in (result['cleanup'],result['reopen_cleanup']):
        assert cleanup['graceful_request'] and not cleanup['forced'] and cleanup['exit_code']==0
    with closing(SessionLock(data)):pass
    checks.append({'name':'Actual frozen engine authenticates, serves all259800 labels, closes gracefully, and reopens identical labels using a Unicode test path','passed':True,'state_hash':result['state_hash']})
    eof_data=output/'parent-eof-session'
    eof_report=output/'parent-eof.json'
    eof=run(['--engine-test',str(eof_report),'--data',str(eof_data),'--parent-eof'],eof_report)
    assert eof['version']==version
    kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    kernel.OpenProcess.argtypes=[ctypes.c_uint32,ctypes.c_int,ctypes.c_uint32];kernel.OpenProcess.restype=ctypes.c_void_p
    kernel.WaitForSingleObject.argtypes=[ctypes.c_void_p,ctypes.c_uint32]
    kernel.CloseHandle.argtypes=[ctypes.c_void_p]
    handle=kernel.OpenProcess(0x100000,False,eof['engine_pid'])
    if handle:
        try:
            exited=kernel.WaitForSingleObject(handle,30000)==0
            if not exited:
                # Preserve failure but clean up this test's authenticated child.
                launch=eof_data/f'native-host-{version}'/'package-test-launch.json'
                info=json.loads(launch.read_text(encoding='utf-8'))
                assert info['pid']==eof['engine_pid']
                local_request(info,'/api/shutdown',{'launch_id':info['launch_id']})
                assert kernel.WaitForSingleObject(handle,15000)==0,'Owned engine cleanup did not finish'
            assert exited,'Owned frozen engine survived parent pipe EOF'
        finally:kernel.CloseHandle(handle)
    with closing(SessionLock(eof_data)):pass
    checks.append({'name':'Hard packaged launcher exit triggers parent pipe EOF, child exit, and session-lock release without force killing','passed':True})
    asset_manifest=bundle/'_internal/assets/manifest.json'
    original_manifest=asset_manifest.read_bytes()
    damaged_report=output/'damaged-resource.json'
    try:
        asset_manifest.write_bytes(original_manifest+b'\n')
        damaged=subprocess.run([str(app),'--verify-package',str(damaged_report)],env=env,cwd=output,timeout=30,
                               stdout=subprocess.PIPE,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW)
        failure=json.loads(damaged_report.read_text(encoding='utf-8'))
        assert damaged.returncode==1 and not failure['passed'] and 'missing or damaged' in failure['error']
    finally:
        asset_manifest.write_bytes(original_manifest)
    checks.append({'name':'Damaged resource is rejected before opening a session or UI, with an actionable error','passed':True})
    after={p.relative_to(bundle).as_posix():sha(p) for p in bundle.rglob('*') if p.is_file()}
    assert before==after,'Portable runtime wrote into its distribution directory'
    checks.append({'name':'All packaged files remain byte-identical after isolated runs; session writes stay outside the distribution','passed':True})
    summary={'passed':True,'version':version,'checks':checks,'native_source_sha256':manifest['native_source_sha256'],
             'engine_process_sha256':manifest['backend_source_files']['engine_process.py'],
             'scope':'Real frozen Windows executable/resource/engine/parent-EOF verification with Python/compiler paths excluded; DirectX rendering is tested separately.'}
    (output/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
    print(json.dumps(summary))


if __name__=='__main__':main()
