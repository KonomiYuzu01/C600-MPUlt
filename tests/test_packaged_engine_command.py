"""Real owned engine lifecycle through the portable-command extension, no GPU."""
from pathlib import Path
from contextlib import closing
import hashlib
import json
import os
import sys
import tempfile
from unittest.mock import patch

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from engine_process import EngineProcess, local_request
from session_lock import SessionLock


def main():
    checks=[]
    with tempfile.TemporaryDirectory(prefix='c600-packaged-command-') as temporary:
        directory=Path(temporary)
        for invalid in ([],[''],[1]):
            try:
                EngineProcess(ROOT,directory,directory/'launch.json',directory/'engine.log',engine_command=invalid)
                raise AssertionError('Invalid explicit engine command accepted')
            except ValueError:
                pass
        checks.append('Empty and malformed engine command prefixes are rejected')
        command=[sys.executable,'-X','utf8','-u',str(ROOT/'server.py')]
        with patch.dict(os.environ,{'PYTHONHOME':str(directory/'missing-python'),'PYTHONPATH':str(directory/'missing-modules'),'__PYVENV_LAUNCHER__':str(directory/'missing-python.exe')}):
            engine=EngineProcess(ROOT,directory/'data',directory/'launch.json',directory/'engine.log',
                                 engine_command=command,hidden_console=True,timeout=60)
            with engine:
                health=local_request(engine.info,'/api/health')
                before=local_request(engine.info,'/api/status')
                assert health['ready'] and health['pid']==engine.info['pid']
                assert Path(health['python_executable']).resolve()==Path(sys.executable).resolve()
            assert engine.cleanup_result['graceful_request'] and not engine.cleanup_result['forced']
            assert engine.cleanup_result['exit_code']==0 and not engine.launch_file.exists()
        checks.append('Explicit executable starts despite inherited Python redirection variables, then exits gracefully')
        with EngineProcess(ROOT,directory/'data',directory/'reopen.json',directory/'reopen.log',
                           engine_command=command,hidden_console=True,timeout=60) as reopened:
            after=local_request(reopened.info,'/api/status')
            assert before['state_hash']==after['state_hash'] and before['head']==after['head']
        assert reopened.cleanup_result['exit_code']==0 and not reopened.cleanup_result['forced']
        with closing(SessionLock(directory/'data')):
            pass
        checks.append('Reopen preserves the full model state and releases the session lock')
    report={'passed':True,'checks':checks,'engine_process_sha256':hashlib.sha256((ROOT/'engine_process.py').read_bytes()).hexdigest(),
            'scope':'Real Windows engine command/lifecycle; no frozen interpreter or native renderer in this source fixture'}
    if len(sys.argv)>1:
        Path(sys.argv[1]).write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(json.dumps(report))


if __name__=='__main__':main()
