"""Build the portable Windows application. Compiler/fixture are build-time only."""
from __future__ import annotations
import argparse
import hashlib
import importlib.metadata
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[1]
VERSION = '0.3'
NATIVE = ('NativeHost.cs','NativeDockLayout.cs','NativeDiagnostics.cs','NativeRendererLifecycle.cs',
          'NativeSnapshot.cs','NativeStickerAccess.cs','NativeRenderSubset.cs','NativePickingVisibility.cs','NativeFullRenderer.cs','NativeColorGraph.cs','NativeStructureExplorer.cs', 'NativeCellView.cs', 'NativeAuxiliaryViews.cs')
BACKEND = ('core.py','session.py','enhanced.py','server.py','engine_process.py','native_bridge.py','log_io.py','mpult_log.py','grips.py','session_lock.py')
PACKAGING = ('packaging/launcher.py','packaging/engine_entry.py','packaging/build_windows.py','packaging/C600Studio.spec',
             'packaging/README.md','packaging/requirements-build.txt','native/directx_runtime.py')


def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream,'sha256').hexdigest()


def write(path,value):
    Path(path).write_text(json.dumps(value,indent=2),encoding='utf-8')


def compile_native(output, sources, main, log, fixture=False):
    compiler=Path(os.environ['WINDIR'])/'Microsoft.NET/Framework/v4.0.30319/csc.exe'
    if not compiler.is_file():
        raise RuntimeError('The Windows build machine needs .NET Framework csc.exe.')
    command=[str(compiler),'/nologo','/target:exe' if fixture else '/target:winexe','/platform:x86',
             '/debug-','/optimize+','/utf8output','/codepage:65001','/main:'+main,'/out:'+str(output)]
    command += ['/reference:'+name for name in ('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll')]
    command += [str(source) for source in sources]
    result=subprocess.run(command,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    log.write_bytes(result.stdout)
    if result.returncode:
        raise RuntimeError('Native build failed; read '+str(log))
    shutil.copy2(ROOT/'native/NativeHost.exe.config',output.with_suffix('.exe.config'))


def copy_license(source,dest):
    if source.is_dir():
        shutil.copytree(source,dest,dirs_exist_ok=True)
    else:
        dest.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(source,dest)


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',type=Path,required=True,help='Fresh output parent; no user session data belongs here.')
    parser.add_argument('--work',type=Path,required=True,help='Private build work/log directory.')
    parser.add_argument('--toolchain',type=Path,help='Optional directory containing isolated PyInstaller build dependencies.')
    parser.add_argument('--notice',type=Path,action='append',default=[],help='One reviewed notice text to copy into licenses/; may repeat. A matching filename replaces its default notice.')
    parser.add_argument('--no-zip',action='store_true')
    args=parser.parse_args()
    if os.name!='nt' or sys.maxsize<=2**32:
        parser.error('Use 64-bit CPython on Windows to build this package.')
    if args.toolchain:
        sys.path.insert(0,str(args.toolchain.resolve()))
        os.environ['PYTHONPATH']=str(args.toolchain.resolve())+os.pathsep+os.environ.get('PYTHONPATH','')
    import PyInstaller
    from PyInstaller.__main__ import run as freeze
    if PyInstaller.__version__!='6.22.2':
        raise RuntimeError('Install packaging/requirements-build.txt before building.')
    output,work=args.output.resolve(),args.work.resolve()
    bundle=output/('C600Studio-'+VERSION+'-Windows-x64')
    if bundle.exists():
        raise RuntimeError('Choose a fresh output directory; existing distributions are preserved.')
    output.mkdir(parents=True,exist_ok=True)
    work.mkdir(parents=True,exist_ok=True)
    stage=work/'payload'
    if stage.exists():
        raise RuntimeError('Choose a fresh build work directory; stale payloads are never reused.')
    (stage/'native/runtime').mkdir(parents=True,exist_ok=True)
    source_hashes={name:sha(ROOT/'native'/name) for name in NATIVE}
    backend_hashes={name:sha(ROOT/name) for name in BACKEND}
    packaging_hashes={name:sha(ROOT/name) for name in PACKAGING}
    combined=hashlib.sha256(b''.join((ROOT/'native'/name).read_bytes() for name in NATIVE)).hexdigest()
    sources=[ROOT/'native'/name for name in NATIVE]
    host=stage/'native/C600Native.exe'
    compile_native(host,sources,'Program',work/'native-build.log')
    fixture=work/'NativeHostRegression.exe'
    compile_native(fixture,sources+[ROOT/'tests/native/NativeHostRegression.cs'],'NativeHostRegression',work/'fixture-build.log',True)
    fixture_report=work/'build-time-winforms-fixture.json'
    result=subprocess.run([str(fixture),str(fixture_report)],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,timeout=120,cwd=work)
    (work/'fixture.log').write_bytes(result.stdout)
    if result.returncode or not json.loads(fixture_report.read_text(encoding='utf-8-sig')).get('passed'):
        raise RuntimeError('Build-time WinForms fixture failed. No distribution was created.')
    runtime_files=('MPUlt.exe','MPUlt_puzzles.txt','MPUlt_settings.txt')
    for name in runtime_files:
        shutil.copy2(ROOT/'native/runtime'/name,stage/'native/runtime'/name)
    version="""VSVersionInfo(ffi=FixedFileInfo(filevers=(0,3,0,0),prodvers=(0,3,0,0),mask=0x3f,flags=0,OS=0x40004,fileType=0x1,subtype=0,date=(0,0)),kids=[StringFileInfo([StringTable('040904B0',[StringStruct('FileDescription','C600 Studio native Windows application'),StringStruct('FileVersion','0.3'),StringStruct('ProductName','C600 Studio'),StringStruct('ProductVersion','0.3')])]),VarFileInfo([VarStruct('Translation',[1033,1200])])])"""
    (stage/'version.txt').write_text(version,encoding='utf-8')
    os.environ['C600_PACKAGING_STAGE']=str(stage)
    freeze(['--noconfirm','--clean','--distpath',str(output),'--workpath',str(work/'pyinstaller'),str(ROOT/'packaging/C600Studio.spec')])
    licenses=bundle/'licenses'
    copy_license(Path(sys.base_prefix)/'LICENSE.txt',licenses/'Python-LICENSE.txt')
    for distribution,target in (('numpy','NumPy'),('pyinstaller','PyInstaller')):
        package=importlib.metadata.distribution(distribution)
        # Wheel layouts vary: NumPy 2.3 uses .dist-info/LICENSE.txt, while
        # newer wheels use .dist-info/licenses/. Include recorded component
        # notices as well; the main wheel notice contains bundled-library terms.
        license_files=[f for f in (package.files or ()) if 'licenses' in f.parts or f.name.lower().startswith(('license','licence','copying','notice'))]
        if not license_files:
            raise RuntimeError('Missing complete license material for '+distribution)
        for item in license_files:
            if 'licenses' in item.parts:
                relative=Path(*item.parts[item.parts.index('licenses')+1:])
            elif any(part.endswith('.dist-info') for part in item.parts):
                position=next(i for i,part in enumerate(item.parts) if part.endswith('.dist-info'))
                relative=Path(*item.parts[position+1:])
            else:
                relative=Path(*item.parts)
            if relative.is_absolute() or '..' in relative.parts:
                raise RuntimeError('Unsafe license path in '+distribution)
            copy_license(Path(package.locate_file(item)),licenses/target/relative)
        if not any(path.is_file() and path.name.lower().startswith(('license','licence','copying')) for path in (licenses/target).iterdir()):
            raise RuntimeError('Missing main wheel license for '+distribution)
    copy_license(ROOT/'LICENSE',bundle/'LICENSE')
    copy_license(ROOT/'CREDITS.md',bundle/'CREDITS.md')
    copy_license(ROOT/'native/LICENSE.MPUlt.txt',licenses/'MPUlt-MIT.txt')
    copy_license(ROOT/'licenses/Inno-Setup-LICENSE.txt',licenses/'Inno-Setup-LICENSE.txt')
    copy_license(ROOT/'THIRD_PARTY_NOTICES.md',licenses/'THIRD_PARTY_NOTICES.md')
    for notice in args.notice:
        if not notice.is_file() or notice.suffix.lower() not in ('.md','.txt'):
            raise RuntimeError('--notice accepts an individual .md or .txt file, never an evidence directory.')
        copy_license(notice.resolve(),licenses/notice.name)
    shutil.copy2(ROOT/'packaging/README.md',bundle/'README.md')
    if source_hashes!={name:sha(ROOT/'native'/name) for name in NATIVE} or backend_hashes!={name:sha(ROOT/name) for name in BACKEND}:
        raise RuntimeError('Sources changed during packaging. Do not distribute this build.')
    if packaging_hashes!={name:sha(ROOT/name) for name in PACKAGING}:
        raise RuntimeError('Packaging sources changed during the build. Do not distribute this build.')
    entries=[]
    startup_names={'C600Studio.exe','C600Engine.exe','_internal/native/C600Native.exe','_internal/native/C600Native.exe.config',
                   '_internal/assets/manifest.json',*('_internal/native/runtime/'+n for n in runtime_files if n.endswith(('.exe','.dll')))}
    for path in sorted(bundle.rglob('*')):
        if not path.is_file():continue
        if path.name.lower() in ('microsoft.directx.dll','microsoft.directx.direct3d.dll','microsoft.directx.direct3dx.dll'):
            raise RuntimeError('Microsoft Managed DirectX DLLs must not be included in this public package.')
        relative=path.relative_to(bundle).as_posix()
        entries.append({'path':relative,'sha256':sha(path),'bytes':path.stat().st_size,'startup_required':relative in startup_names})
    manifest={'version':VERSION,'layout':'portable one-folder, x64 frozen engine and x86 native host',
              'python':sys.version.split()[0],'numpy':importlib.metadata.version('numpy'),'pyinstaller':PyInstaller.__version__,
              'native_source_sha256':combined,'native_source_files':source_hashes,'backend_source_files':backend_hashes,
              'packaging_source_files':packaging_hashes,
              'asset_manifest_sha256':sha(ROOT/'assets/manifest.json'),'build_time_fixture_passed':True,
              'normal_startup_fixture_run':False,'files':entries}
    write(bundle/'_internal/package-manifest.json',manifest)
    build_report={'passed':True,'bundle':str(bundle),'native_source_sha256':combined,
                  'files':len(entries),'bytes':sum(item['bytes'] for item in entries),'build_time_fixture_report':str(fixture_report)}
    if not args.no_zip:
        archive=output/('C600Studio-'+VERSION+'-Windows-x64.zip')
        with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as zip:
            for path in sorted(bundle.rglob('*')):
                if path.is_file():zip.write(path,path.relative_to(output))
        build_report.update(zip=str(archive),zip_sha256=sha(archive))
    write(work/'build-report.json',build_report)
    print(json.dumps(build_report),flush=True)


if __name__=='__main__':main()
