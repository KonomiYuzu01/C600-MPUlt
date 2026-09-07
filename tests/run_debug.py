"""Cross-platform regression runner with owned subprocesses and durable reports.
Every engine test uses a fresh scratch session, never the user's solve database.
A startup failure and a cleanup failure are reported separately.
"""
from __future__ import annotations
from pathlib import Path
import argparse, ast, contextlib, json, os, platform, shutil, subprocess, sys, tempfile, time, traceback
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from engine_process import EngineProcess, python_child_command, redact, stop_owned_process


def remove_scratch(path: Path, attempts: int = 10, delay: float = .2) -> dict:
    """Called only after owned children have stopped. Preserve on failure."""
    last=None
    for i in range(attempts):
        try:
            shutil.rmtree(path)
            return dict(removed=True,attempts=i+1)
        except FileNotFoundError:
            return dict(removed=True,attempts=i+1)
        except OSError as exc:
            last=f'{type(exc).__name__}: {exc}'
            if i+1<attempts:time.sleep(delay)
    return dict(removed=False,retained_directory=str(path),error=last,attempts=attempts)


def main():
    ap=argparse.ArgumentParser()
    ap.add_argument('--browser',action='store_true')
    ap.add_argument('--native-bridge',action='store_true')
    ap.add_argument('--engine-only',action='store_true',help='Fast Python lifecycle/encoding checks; no WinForms or DirectX')
    ap.add_argument('--startup-timeout',type=float,default=180)
    ap.add_argument('--output',type=Path,default=ROOT/'tests/v022')
    args=ap.parse_args()
    out=args.output.resolve();out.mkdir(parents=True,exist_ok=True)
    report=dict(scope='C600 Studio 0.2.2 regression suite; all session inputs are temporary',
                platform=platform.platform(),python=sys.version,checks=[],
                started=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()))
    def save():
        temp=out/'DEBUG_RESULTS.tmp'
        temp.write_text(json.dumps(report,indent=2),encoding='utf-8');temp.replace(out/'DEBUG_RESULTS.json')
    def record(name,status,**details):
        report['checks'].append(dict(name=name,status=status,**details));save();print(status.upper(),name,flush=True)
    def run(name,cmd,timeout=300):
        started=time.perf_counter();log_path=out/(name+'.log')
        env=os.environ.copy();env.update(PYTHONUTF8='1',PYTHONIOENCODING='utf-8',PYTHONUNBUFFERED='1')
        if cmd[0]==sys.executable:cmd,env=python_child_command(cmd[1:])
        proc=None;code=None;reason=None
        with log_path.open('wb') as log:
            try:
                proc=subprocess.Popen(cmd,cwd=ROOT,env=env,stdout=log,stderr=subprocess.STDOUT,
                    stdin=subprocess.DEVNULL,start_new_session=os.name!='nt',
                    creationflags=subprocess.CREATE_NEW_PROCESS_GROUP if os.name=='nt' else 0)
                code=proc.wait(timeout=timeout)
            except subprocess.TimeoutExpired:
                reason=f'Test timed out after {timeout}s'
            except OSError as exc:
                reason=str(exc)
            finally:
                if proc is not None and proc.poll() is None:
                    try:stop_owned_process(proc,grace=.2,group_owned=os.name!='nt')
                    except Exception as exc:reason=(reason or '')+'; cleanup: '+str(exc)
        text=log_path.read_bytes().decode('utf-8-sig','replace');log_path.write_text(redact(text),encoding='utf-8')
        record(name,'passed' if code==0 and reason is None else 'failed',exit_code=code,
               reason=reason,seconds=time.perf_counter()-started,log=log_path.name)
        return code==0 and reason is None
    def cleanup(name,path):
        result=remove_scratch(path)
        record(name,'passed' if result['removed'] else 'failed',**result)
    try:
        try:
            sources=list(ROOT.glob('*.py'))+list((ROOT/'native').glob('*.py'))+list((ROOT/'tests').glob('*.py'))
            for p in sources:ast.parse(p.read_text(encoding='utf-8-sig'),filename=str(p))
            record('python_syntax','passed',files=len(sources))
        except Exception as exc:record('python_syntax','failed',reason=str(exc))
        for name in ('native_startup_contract','engine_lifecycle','debug_cleanup'):
            run(name,[sys.executable,'tests/test_'+name+'.py'])
        if not args.engine_only:
            node=shutil.which('node')
            if node:
                for p in (ROOT/'web').glob('*.js'):run('js_'+p.stem,[node,'--check',str(p)])
            else:record('javascript_syntax','skipped',reason='Node.js is optional and is not installed')
            for name in ('reference_maps','core','v02','crash'):
                run(name,[sys.executable,'tests/test_'+name+'.py'])
            run('native_metadata',[sys.executable,'native/inspect_runtime.py','native/runtime/MPUlt.exe','--report',str(out/'native_metadata.json')])
            scratch=Path(tempfile.mkdtemp(prefix='c600-022-http-'))
            engine=EngineProcess(ROOT,scratch/'session',scratch/'launch.json',out/'http_engine.log',
                                 timeout=args.startup_timeout,progress=lambda s:print(s,flush=True))
            try:
                with engine:
                    record('http_engine_startup','passed',launcher_pid=engine.process.pid,engine_pid=engine.info['pid'])
                    url=engine.info['url']
                    run('http_boundary',[sys.executable,'tests/test_http_boundary.py',url])
                    if args.native_bridge:run('native_snapshot',[sys.executable,'tests/test_native_snapshot.py',url],600)
                    else:record('native_snapshot','skipped',reason='Enable --native-bridge for synthetic complete mapping and snapshot checks')
                    if args.browser:run('browser_ui',[sys.executable,'tests/test_browser_ui.py',url],300)
                    else:record('browser_ui','skipped',reason='Enable --browser with Playwright/Chromium and a display')
            except Exception as exc:
                record('http_engine_or_workflow','failed',reason=redact(str(exc)),log='http_engine.log')
            finally:
                engine.close()
                result=engine.cleanup_result or {}
                record('http_engine_shutdown','failed' if result.get('cleanup_error') else 'passed',**result)
                for name in ('launch.lifecycle.json','launch.status.json'):
                    p=scratch/name
                    if p.exists():(out/name).write_text(redact(p.read_text(encoding='utf-8-sig',errors='replace')),encoding='utf-8')
                if engine.log_file.exists():
                    engine.log_file.write_text(redact(engine.log_file.read_text(encoding='utf-8-sig',errors='replace')),encoding='utf-8')
                cleanup('http_scratch_cleanup',scratch)
            # A Python startup failure must not prevent producing a complete report.
            if os.name=='nt':
                scratch=Path(tempfile.mkdtemp(prefix='c600-022-winforms-'))
                try:
                    run('winforms_runtime',[sys.executable,'native/bootstrap.py','--self-test-only','--data',str(scratch)],240)
                    for p in scratch.glob('native-host-0.2.2/diagnostics/*'):
                        if p.suffix in ('.json','.log'):shutil.copy2(p,out/p.name)
                finally:cleanup('winforms_scratch_cleanup',scratch)
            else:record('winforms_runtime','not_run',reason='No Windows/.NET Framework runtime in this test environment')
            record('directx_live_bridge','not_run',reason='Requires actual Windows MPUlt/DirectX; synthetic mapping is not native execution')
    except KeyboardInterrupt:
        record('runner_interrupted','failed',reason='Interrupted by user; owned engine cleanup was requested')
    except Exception as exc:
        (out/'runner_error.log').write_text(redact(traceback.format_exc()),encoding='utf-8')
        record('runner_internal_error','failed',reason=redact(str(exc)),log='runner_error.log')
    finally:
        report['executed_checks_passed']=all(x['status']!='failed' for x in report['checks'])
        report['windows_native_release_validated']=False
        report['finished']=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime());save()
        print('Report: '+str(out/'DEBUG_RESULTS.json'),flush=True)
    return 0 if report['executed_checks_passed'] else 1

if __name__=='__main__':sys.exit(main())
