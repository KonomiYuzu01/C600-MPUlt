"""Full-state scramble and local HTTP regressions; use only isolated sessions.

Run with --work-dir PATH to retain reports and engine diagnostics. This checks
CPU mechanics and HTTP publication, not native GPU frame time.
"""
from pathlib import Path
import argparse,json,platform,sys,tempfile,threading,time
from unittest.mock import patch
from urllib.request import ProxyHandler,Request,build_opener
from urllib.error import HTTPError
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT));sys.path.insert(0,str(ROOT/'oracle'))
from core import Model,PuzzleState,canonical,invword,state_hash
from session import Session
from enhanced import Workflow
from engine_process import EngineProcess
import move_engine

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--work-dir',type=Path);args=ap.parse_args()
 work=args.work_dir or Path(tempfile.mkdtemp(prefix='c600-scramble-'))
 work.mkdir(parents=True,exist_ok=True)
 checks=[];measurements=[];m=Model();started=time.perf_counter()
 def ok(name,**details):
  checks.append(dict(name=name,passed=True,**details));print('PASS',name,flush=True)
 def reject(fn,text):
  try:fn()
  except (ValueError,InterruptedError) as e:assert text in str(e),(text,str(e))
  else:raise AssertionError('Invalid operation accepted: '+text)
 with tempfile.TemporaryDirectory(prefix='mechanics-',dir=work) as td:
  s=Session(m,Path(td));wf=Workflow(s)
  try:
   root=s.st.hash
   p=wf.scramble(1000,600);word=p['recipe'][0]['moves']
   assert len(word)==1000 and p['primitive_count']=='1000'
   assert s.st.hash==root and s.head==0
   first_after=s.pending['after'].copy();repeat=wf.scramble(1000,600)
   assert word==repeat['recipe'][0]['moves'] and p['post_state']==repeat['post_state']
   assert p['token']!=repeat['token']
   reject(lambda:s.commit(p['token']),'expired')
   ok('Seeded previews are deterministic, legal-length, nonmutating, and expire older tokens')

   # Independently reconstruct every used generator from retained geometry,
   # rather than replaying the packed arrays that the production engine uses.
   move_engine.ROOT=ROOT/'assets';oracle=move_engine.Engine();maps={};labels=m.ids.copy();t=time.perf_counter()
   for move in word:
    if move not in maps:
     q=oracle.move(move);maps[move]=(np.fromiter(q.keys(),dtype=np.int32),np.fromiter(q.values(),dtype=np.int32));oracle.cache.clear()
    src,dst=maps[move];labels[dst]=labels[src]
   assert np.array_equal(labels,first_after)
   PuzzleState(m,labels)  # Bijective stickers, intact physical pieces, same orbit.
   ok('Seed-600 1000-turn full-state replay equals independent geometric oracle',stickers=m.n,distinct_signed_generators=len(maps),seconds=time.perf_counter()-t,state_hash=state_hash(labels))
   del oracle,maps

   prior=s.pending
   for count in (True,False,1.9,'12',None,[],0,10001):reject(lambda count=count:wf.scramble(count,600),'integer')
   for seed in (True,1.9,'600',[]):reject(lambda seed=seed:wf.scramble(1000,seed),'integer')
   for apply in (1,None,'true'):reject(lambda apply=apply:wf.scramble(1000,600,apply=apply),'boolean')
   assert s.pending is prior and s.st.hash==root
   ok('Malformed count, seed, and apply inputs cannot replace pending work or change state')

   conflict=p['support'][0]['orbit'];s.save_prefs({'protected':[conflict]})
   reject(lambda:wf.scramble(1000,600,apply=True),'Protected orbit')
   assert s.pending is prior and s.head==0 and s.st.hash==root
   s.save_prefs({'protected':[]})
   ok('Protected orbit rejects atomic scramble and preserves earlier preview')

   cancel=threading.Event();cancel.set();m.cancel_event=cancel
   reject(lambda:wf.scramble(1000,600,apply=True),'cancelled')
   assert s.pending is prior and s.st.hash==root
   cancel.clear();original=s.preview
   def cancel_after_preview(*a,**kw):
    result=original(*a,**kw);cancel.set();return result
   with patch.object(s,'preview',side_effect=cancel_after_preview):reject(lambda:wf.scramble(1000,600,apply=True),'cancelled')
   assert s.pending is prior and s.st.hash==root and s.head==0
   m.cancel_event=None
   ok('Cancellation before generation and immediately before commit preserves full state and earlier preview')

   for expr in ('active','current(C000)'):
    s.pending=None;s.save_prefs({'rules':[{'expr':expr,'style':'solid'}]})
    before=int(np.count_nonzero(s.render_styles()));t=time.perf_counter();r=wf.scramble(1000,600,apply=True);elapsed=1000*(time.perf_counter()-t)
    assert r['pending'] is None and s.pending is None
    assert s.st.hash==p['post_state'] and np.array_equal(s.st.labels,labels)
    assert r['transactions']==1 and r['primitives']=='1000'
    assert int(np.count_nonzero(s.render_styles()))==before
    assert json.loads(s.event()['recipe'])[0]['moves']==word and 'seed 600' in s.event()['note']
    measurements.append(dict(filter=expr,visible_before=before,visible_after=before,scramble_apply_ms=elapsed))
    s.undo();assert s.st.hash==root;s.redo();assert s.st.hash==p['post_state'];s.undo()
   ok('Atomic scramble publishes one undoable transaction, retains exact seed witness, and preserves filtered visibility',measurements=measurements)

   result=wf.scramble(1000,600,apply=True);saved_hash=result['state_hash'];saved_head=s.head
   s.close();s=Session(m,Path(td));wf=Workflow(s)
   assert s.st.hash==saved_hash and s.head==saved_head
   s.undo();assert s.st.hash==root;s.close();s=Session(m,Path(td));wf=Workflow(s);s.redo();assert s.st.hash==saved_hash
   inv=s.preview([dict(kind='word',moves=invword(word))]);s.commit(inv['token']);assert s.st.hash==root
   ok('Committed scramble recovers on restart, durable undo/redo survives restart, and inverse restores all stickers')
  finally:m.cancel_event=None;s.close()

 with tempfile.TemporaryDirectory(prefix='http-',dir=work) as td:
  td=Path(td)
  with EngineProcess(ROOT,td/'session',td/'launch.json',work/'http-engine.log',timeout=45) as engine:
   def request(path,body=None):
    req=Request(engine.info['base']+'/api/'+path,data=None if body is None else json.dumps(body).encode(),headers={'Content-Type':'application/json','X-C600-Token':engine.info['token']})
    try:
     with build_opener(ProxyHandler({})).open(req,timeout=30) as res:return res.status,json.load(res)
    except HTTPError as e:return e.code,json.load(e)
   def finish(reply):
    assert reply[0]==200,reply
    r=reply[1]
    if 'job' not in r:return r
    deadline=time.monotonic()+30
    while time.monotonic()<deadline:
     _,j=request('job/'+r['job'])
     if j['done']:return j
     time.sleep(.01)
    raise AssertionError('Scramble job timeout')
   root=request('status')[1]['state_hash']
   preview=finish(request('scramble',{'count':1000,'seed':600}))
   assert preview['result']['post_state']==p['post_state']
   status=request('status')[1];old_token=status['pending']['token'];assert status['state_hash']==root
   bad=finish(request('scramble',{'count':1.5,'apply':True}));assert 'integer' in bad['error']
   assert request('status')[1]['pending']['token']==old_token
   cancelled=request('scramble',{'count':10000,'seed':600,'apply':True});request('stop-job',{})
   outcome=finish(cancelled);assert 'cancelled' in outcome.get('error',''),outcome
   after=request('status')[1];assert after['state_hash']==root and after['pending']['token']==old_token
   ok('Real Windows HTTP async preview, validation error, and cancellation preserve authoritative state')

   t=time.perf_counter();applied=finish(request('scramble',{'count':1000,'seed':600,'apply':True}));http_ms=1000*(time.perf_counter()-t)
   assert 'error' not in applied,applied
   state=applied['result'];assert state['pending'] is None and state['state_hash']==p['post_state'] and state['transactions']==1
   assert request('undo',{})[1]['state_hash']==root
   assert request('redo',{})[1]['state_hash']==p['post_state']
   ok('Real Windows HTTP atomic scramble commits once and supports exact undo/redo',milliseconds=http_ms)
  assert engine.process.returncode==0 and not engine.cleanup_result['forced']
  ok('Owned HTTP engine shuts down cleanly before scratch session removal')
 report=dict(passed=True,scope='Windows CPU mechanics and actual loopback HTTP; no GPU timing claim',platform=platform.platform(),python=platform.python_version(),seconds=time.perf_counter()-started,checks=checks)
 (work/'scramble-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
 print('ALL SCRAMBLE TESTS PASSED',report['seconds'],flush=True)
if __name__=='__main__':main()
