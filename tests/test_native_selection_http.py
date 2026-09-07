"""Real authenticated HTTP hidden-picking guard; synthetic full bridge, no GPU."""
from pathlib import Path
import argparse,base64,hashlib,json,sys,time
from urllib.error import HTTPError
from urllib.request import Request,ProxyHandler,build_opener
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import Model,canonical
from engine_process import EngineProcess
from test_native_bridge import make_fixture

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--work-dir',type=Path,required=True);args=ap.parse_args();work=args.work_dir;work.mkdir(parents=True,exist_ok=True)
    assert not (work/'data/session.sqlite3').exists(),'Use a fresh directory'
    source={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in ('server.py','session.py','core.py')};checks=[]
    model=Model();geometry,mapping=make_fixture(model)
    with EngineProcess(ROOT,work/'data',work/'launch.json',work/'engine.log',timeout=240) as engine:
        def api(path,body=None,raw=False):
            req=Request(engine.info['base']+'/api/'+path,data=None if body is None else canonical(body).encode(),headers={'Content-Type':'application/json','X-C600-Token':engine.info['token']})
            with build_opener(ProxyHandler({})).open(req,timeout=180) as reply:payload=reply.read()
            if raw:return payload
            data=json.loads(payload)
            if 'job' in data:
                until=time.monotonic()+180
                while time.monotonic()<until:
                    value=api('job/'+data['job'])
                    if value.get('done'):
                        if 'error' in value:raise AssertionError(value['error'])
                        return value['result']
                    time.sleep(.01)
                raise TimeoutError('Native synthetic handshake timeout')
            return data
        def reject(index):
            before=api('status');labels=api('labels',raw=True)
            try:api('native/select',{'native_sticker':int(index)})
            except HTTPError as exc:assert exc.code==400 and 'Hidden stickers' in exc.read().decode()
            else:raise AssertionError('Hidden viewport hit selected a piece')
            assert api('status')==before and api('labels',raw=True)==labels
        handshake=api('native/handshake',geometry);assert handshake['matched_stickers']==259800 and handshake['matched_generators']==1200
        api('prefs',{'rules':[{'expr':'O33','style':'solid'}],'pin_safety':False,'selected':None})
        api('preview',{'recipe':[{'kind':'word','moves':[2]}]})
        snap=api('native/snapshot');styles=np.frombuffer(base64.b64decode(snap['styles']),dtype=np.uint8)
        visible=int(np.flatnonzero(styles>0)[0]);hidden=int(np.flatnonzero(styles==0)[0]);reject(hidden)
        checks.append('Hidden native hit is HTTP400 and preserves complete labels, prefs, pending preview, head and revision')
        before=api('status');picked=api('native/select',{'native_sticker':visible});assert picked['position']==int(model.sp[mapping[visible]])
        after=api('status');assert after['prefs']['selected']==picked['piece'] and after['head']==before['head'] and after['state_hash']==before['state_hash'] and after['pending']==before['pending']
        checks.append('Visible native hit selects its mapped physical identity without changing mechanics or preview')
        api('prefs',{'rules':[{'expr':'everything','style':'hide'}],'pin_safety':False})
        assert not any(base64.b64decode(api('native/snapshot')['styles']));reject(visible)
        checks.append('A previously hittable sticker becomes noninteractive immediately after applying hide')
        piece=api('select',{'position':int(model.sp[mapping[hidden]])});assert api('status')['prefs']['selected']==piece['piece']
        assert not any(base64.b64decode(api('native/snapshot')['styles']));reject(hidden)
        checks.append('Explicit Pieces utility selection remains available while viewport hidden hits stay rejected')
    after={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in source};assert after==source
    report=dict(passed=True,scope='Actual Windows EngineProcess and authenticated HTTP; explicitly synthetic full native geometry, no actual MPUlt/GPU claim',checks=checks,source_sha256=source,source_after=after,children_exited=True)
    (work/'native-selection-http-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2))
if __name__=='__main__':main()
