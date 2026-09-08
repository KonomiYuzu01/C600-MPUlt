"""Atomic inspected-cell focus over fresh SQLite and authenticated HTTP.

The shuffled native bridge is synthetic; these checks do not exercise a GPU.
"""
from pathlib import Path
import argparse,base64,hashlib,json,sqlite3,sys,time
from urllib.error import HTTPError
from urllib.request import Request,ProxyHandler,build_opener
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import Model,PuzzleState,canonical
from session import Session
from engine_process import EngineProcess
from test_native_bridge import make_fixture
from test_color_inspection import frozen,mechanics,rejects,commit,apply_delta

WORD=[1,2,7,31,101,600,1200,65,433,812,1198]

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--work-dir',type=Path,required=True);args=ap.parse_args()
 work=args.work_dir;work.mkdir(parents=True,exist_ok=False);started=time.perf_counter();checks=[]
 source_names=['core.py','session.py','server.py','tests/test_inspection_focus.py']
 hashes={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in source_names}
 m=Model();s=Session(m,work/'session')
 try:
  commit(s,WORD);position=int(np.flatnonzero((m.k==20)&(s.st.at!=m.pids))[0]);slots=list(map(int,m.slots(position)))
  selected=int(s.st.at[position]);s.save_prefs(dict(selected=selected,rules=[dict(expr='selected',style='solid')],pin_safety=False));s.preview([dict(kind='word',moves=[2])])
  original=mechanics(s);mask=s.interactive_styles().copy();baseline=dict(s.prefs);baseline.pop('inspection');baseline.pop('focus_color')
  for gesture,slot in zip(('home-centers','required-piece'),(slots[0],slots[-1])):
   trace=[];s.db.set_trace_callback(trace.append)
   try:r=s.inspect(position,slot,gesture)
   finally:s.db.set_trace_callback(None)
   assert s.prefs['focus_color']==r['clicked_color']==slot//433+1
   assert s.status()['focus']['center_slot']==(slot//433)*433
   assert s.prefs['inspection']['clicked_slot']==slot and mechanics(s)==original
   prefs=dict(s.prefs);prefs.pop('inspection');prefs.pop('focus_color');assert prefs==baseline
   assert np.array_equal(s.interactive_styles(),mask)
   assert sum(q.strip().upper().startswith('BEGIN') for q in trace)==1 and sum(q.strip().upper()=='COMMIT' for q in trace)==1
  checks.append('Both valid gestures publish clicked physical-cell focus and annotation in one SQLite transaction, preserving labels, pending work, original selection, rules and interaction')
  before=frozen(s)
  s.db.execute("CREATE TEMP TRIGGER reject_inspection_focus BEFORE INSERT ON meta WHEN NEW.key='prefs' BEGIN SELECT RAISE(FAIL,'atomic focus failure'); END")
  try:rejects(lambda:s.inspect(position,slots[0],'home-centers'),sqlite3.IntegrityError);assert frozen(s)==before
  finally:s.db.execute('DROP TRIGGER reject_inspection_focus')
  for p,slot,gesture in ((True,slots[0],'home-centers'),(position,-1,'home-centers'),(position,slots[0],'unknown')):
   rejects(lambda:s.inspect(p,slot,gesture));assert frozen(s)==before
  checks.append('Invalid inspection data and an injected SQLite write failure roll back both focus and inspection without changing durable or in-memory preferences')
  s.checkpoint('Atomic inspection focus');expected=(s.st.labels.tobytes(),canonical(s.prefs),canonical(s.inspection_info()))
  focus=s.prefs['focus_color'];s.clear_inspection();assert s.prefs['focus_color']==focus and s.inspection_info() is None
  s.restore('Atomic inspection focus');assert (s.st.labels.tobytes(),canonical(s.prefs),canonical(s.inspection_info()))==expected
 finally:s.close()
 s=Session(m,work/'session')
 try:assert (s.st.labels.tobytes(),canonical(s.prefs),canonical(s.inspection_info()))==expected
 finally:s.close()
 checks.append('Checkpoint restore and a new Session reopen retain all 259800 labels and the coupled focus/inspection; clearing inspection leaves the independent focus intact')

 geometry,mapping=make_fixture(m);inverse=np.argsort(mapping)
 with EngineProcess(ROOT,work/'http-data',work/'launch.json',work/'engine.log',timeout=90) as engine:
  opener=build_opener(ProxyHandler({}))
  def api(path,body=None,raw=False):
   req=Request(engine.info['base']+'/api/'+path,data=None if body is None else canonical(body).encode(),headers={'Content-Type':'application/json','X-C600-Token':engine.info['token']})
   with opener.open(req,timeout=120) as response:data=response.read()
   if raw:return data
   result=json.loads(data)
   if 'job' in result:
    until=time.monotonic()+120
    while time.monotonic()<until:
     job=api('job/'+result['job'])
     if job.get('done'):
      assert 'error' not in job,job;return job['result']
     time.sleep(.01)
    raise TimeoutError('Test operation did not complete')
   return result
  api('native/handshake',geometry);p=api('preview',{'recipe':[dict(kind='word',moves=WORD)]});api('commit',{'token':p['token']})
  labels=api('labels',raw=True);state=PuzzleState(m,np.frombuffer(labels,'<u4'));position=int(np.flatnonzero((m.k==20)&(state.at!=m.pids))[0]);selected=int(state.at[position])
  api('prefs',dict(selected=selected,rules=[dict(expr='selected',style='solid')],pin_safety=False,inspection=None,focus_color=None))
  initial=api('status');cached=apply_delta(api('native/snapshot?protocol=2'));interaction=cached[1][2].copy()
  for gesture,slot in zip(('home-centers','required-piece'),(int(m.slots(position)[0]),int(m.slots(position)[-1]))):
   result=api('native/inspect',dict(native_sticker=int(inverse[slot]),pre_state=initial['state_hash'],gesture=gesture,native_since=cached[0]))
   snap=result['native_snapshot'];cached=apply_delta(snap,cached);atomic=snap['state']
   assert atomic['focus']['color']==atomic['prefs']['focus_color']==atomic['cell_status']['focus_color']==result['clicked_color']==slot//433+1
   assert atomic['inspection']['clicked_slot']==slot and atomic['prefs']['inspection']['clicked_slot']==slot
   assert atomic['prefs']['selected']==selected and np.array_equal(cached[1][2],interaction)
   assert api('labels',raw=True)==labels
   for key in ('head','revision','state_hash','transactions'):assert atomic[key]==initial[key]
  checks.append('One authenticated inspect response contains coherent v2 focus, exact clicked annotation and cell-status metadata over shuffled native IDs with unchanged interaction and complete labels')
  def frozen_http():return canonical(api('status')),api('labels',raw=True),api('styles',raw=True),canonical(api('native/snapshot?protocol=2'))
  body=dict(native_sticker=int(inverse[slot]),pre_state=initial['state_hash'],gesture='home-centers',native_since=cached[0])
  hidden=int(np.flatnonzero(interaction==0)[0])
  for change in ({'pre_state':'0'*64},{'profile_sha256':'bad'},{'native_sticker':hidden},{'native_sticker':True},{'gesture':'bad'}):
   before=frozen_http();bad=dict(body,**change)
   try:api('native/inspect',bad)
   except HTTPError as error:assert error.code==400,error.read().decode()
   else:raise AssertionError('Invalid/stale/hidden inspect accepted')
   assert frozen_http()==before
  checks.append('Stale state/profile, hidden hits and malformed authenticated inspect requests leave focus, annotation, preferences, full labels and native snapshot unchanged')
 report=dict(passed=True,checks=checks,seconds=time.perf_counter()-started,source_sha256=hashes,scope='fresh SQLite and actual authenticated HTTP; shuffled synthetic bridge; no GPU')
 assert hashes=={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in source_names}
 (work/'report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('INSPECTION FOCUS PASS',len(checks),flush=True)

if __name__=='__main__':main()
