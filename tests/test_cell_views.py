"""Coarse real geometry, exact cell counts and independent durable focus.

Uses fresh SQLite/HTTP sessions. It does not exercise GDI, native input or a GPU.
"""
from pathlib import Path
from collections import Counter
from itertools import combinations
import argparse,base64,hashlib,json,sqlite3,sys,time
from urllib.error import HTTPError
from urllib.request import Request,ProxyHandler,build_opener
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import Model,canonical
from session import Session
from server import NativeSnapshotCache
from engine_process import EngineProcess
from test_native_snapshot_cache import arrays

FIELDS=('active_total','active_solved','visible_total','visible_unsolved','eligible_total','eligible_solved')
WORD=[1,2,7,31,101,600,1200,65,433,812,1198]
def rejects(fn,kind=ValueError):
 try:fn()
 except kind:return
 raise AssertionError('Invalid input accepted')
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--work-dir',type=Path,required=True);args=ap.parse_args()
 work=args.work_dir;work.mkdir(parents=True,exist_ok=False);started=time.perf_counter();m=Model();checks=[]
 sources=['core.py','session.py','server.py','tests/test_cell_views.py'];hashes={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in sources}
 structure=m.structure();g=structure['geometry'];v=np.asarray(g['vertices4']);cells=np.asarray(g['cells']);centers=np.asarray(g['centers4'])
 assert g['format']=='C600-cell-geometry-v1' and g['coordinate_order']==['W','X','Y','Z'] and g['vertex_id_base']==0 and g['color_id_base']==1
 assert v.shape==(120,4) and cells.shape==(600,4) and centers.shape==(600,4)
 assert np.allclose(centers,m.normals,atol=1e-12,rtol=0) and np.allclose(v[cells].mean(axis=1),centers,atol=1e-9,rtol=0)
 for i,incident in enumerate(m.vertex_cells):
  poles=m.normals[incident];independent=np.linalg.lstsq(poles,np.sum(poles*poles,axis=1),rcond=None)[0]
  assert np.allclose(v[i],independent,atol=1e-9,rtol=0)
 edges=Counter(edge for row in cells for edge in combinations(sorted(map(int,row)),2));faces=Counter(face for row in cells for face in combinations(sorted(map(int,row)),3))
 assert len(edges)==720 and set(edges.values())=={5} and len(faces)==1200 and set(faces.values())=={2}
 lengths=np.array([np.linalg.norm(v[a]-v[b]) for a,b in edges]);assert np.allclose(lengths,lengths[0],atol=1e-9,rtol=0)
 for c,row in enumerate(cells):
  shared=[j+1 for j,other in enumerate(cells) if j!=c and len(set(row)&set(other))==3]
  assert set(shared)==set(structure['adjacency'][c])
 assert np.shares_memory(m.cell_positions,m.sp) and not m.vertices4.flags.writeable and not m.tetrahedra.flags.writeable
 checks.append('120 real 4D vertices and600 tetrahedra match independent plane solves,720 equal edges,1200 shared faces and existing adjacency')
 s=Session(m,work/'session');cache=NativeSnapshotCache();profile=dict(profile_sha256='c'*64,native_to_lab=list(range(m.n-1,-1,-1)),native_face_to_lab=list(range(599,-1,-1)))
 def snapshot(previous=None):return cache.read(s,profile,None if previous is None else previous['revision'])
 def counts(reply):
  status=reply['state']['cell_status'];assert status['state_hash']==s.st.hash and status['orbit']==s.prefs['orbit'] and all(len(status[n])==600 for n in FIELDS)
  visible=s.interactive_styles()!=0
  for c in range(600):
   positions=np.unique(m.sp[c*433:(c+1)*433]);active=m.oid[positions]==s.prefs['orbit'];shown=visible[m.first[positions]];solved=s.st.correct[positions];eligible=active&shown
   expected=(active.sum(),(active&solved).sum(),shown.sum(),(shown&~solved).sum(),eligible.sum(),(eligible&solved).sum())
   assert tuple(status[n][c] for n in FIELDS)==expected,(c,expected)
  assert status['buffer_cells']==sorted({c+1 for p in m.trees[s.prefs['orbit']]['buffers'] for c in m.hosting(p)})
  selected=s.prefs.get('selected');expected=[] if selected is None else [c+1 for c in m.hosting(int(s.st.where[selected]))]
  assert status['selected_cells']==expected
  return status
 def invariant():return (s.st.labels.tobytes(),s.st.hash,s.head,s.rev,canonical(s.pending['public']) if s.pending else None,s.render_styles().tobytes(),s.interactive_styles().tobytes(),canonical({k:v for k,v in s.prefs.items() if k!='focus_color'}))
 try:
  s.save_prefs(dict(rules=[dict(expr='all',style='solid')],pin_safety=False));r=snapshot();cs=counts(r);assert all(x==433 for x in cs['visible_total']) and not any(cs['visible_unsolved'])
  p=s.preview([dict(kind='word',moves=WORD)]);s.commit(p['token']);selected=int(s.st.at[np.flatnonzero(s.st.at!=m.pids)[0]])
  for expr in ('active','selected','cell(C1)','color(C600) & unsolved','nothing'):
   s.save_prefs(dict(rules=[dict(expr=expr,style='solid')],selected=selected));counts(snapshot())
  checks.append('All600 cell summaries match independent distinct-position oracles in solved/scrambled states across five exact filters, including empty denominators')
  s.save_prefs(dict(rules=[dict(expr='nothing',style='solid')],pin_safety=True));s.inspect(int(s.st.where[selected]),int(m.slots(int(s.st.where[selected]))[0]),'home-centers')
  before=snapshot();data=arrays(before);oldcounts=s._cell_counts;old=invariant();result=s.focus(600);after=snapshot(before);cs=counts(after)
  assert result['focus']==dict(color=600,lab_cell=599,center_position=int(m.center_piece[599]),center_slot=599*433) and cs['focus_color']==600
  assert invariant()==old and s._cell_counts is oldcounts and after['mode']=='delta' and base64.b64decode(after['indices'])==b''
  assert after['revisions']['annotation']!=before['revisions']['annotation'] and after['state']['inspection']==before['state']['inspection']
  assert all(np.array_equal(a,b) for a,b in zip(arrays(after,(before['revision'],data)),data)) and not any(cs['visible_total'])
  checks.append('Focus-only update changes annotation/status revisions and canonical metadata, reuses counts, and leaves every sticker array, inspection, filter and interaction unchanged')
  for bad in (True,False,0,601,-1,1.0,'1','C1',[],{}):
   old=(invariant(),canonical(s.prefs),s._get('prefs'));rejects(lambda:s.focus(bad));assert (invariant(),canonical(s.prefs),s._get('prefs'))==old
  s.db.execute("CREATE TRIGGER reject_focus BEFORE INSERT ON meta WHEN NEW.key='prefs' BEGIN SELECT RAISE(ABORT,'injected focus write failure'); END")
  old=(invariant(),canonical(s.prefs),s._get('prefs'));rejects(lambda:s.focus(1),sqlite3.IntegrityError);assert (invariant(),canonical(s.prefs),s._get('prefs'))==old
  s.db.execute('DROP TRIGGER reject_focus');s.db.commit();checks.append('Invalid focus types/ranges and injected SQLite failure preserve durable preferences and full state atomically')
  s.checkpoint('Cell focus');s.focus(1);s.restore('Cell focus');assert s.focus_info()['color']==600
  labels=s.st.labels.tobytes();prefs=canonical(s.prefs);head=s.head;s.close();s=Session(m,work/'session');assert s.st.labels.tobytes()==labels and canonical(s.prefs)==prefs and s.head==head
  assert s.focus_info()['color']==600;counts(snapshot());s.focus(None);assert s.status()['focus'] is None
  checks.append('Focus survives checkpoint restore and full259800-label close/reopen; explicit null clears only focus')
 finally:s.close()
 with EngineProcess(ROOT,work/'http-data',work/'launch.json',work/'engine.log',timeout=90) as engine:
  opener=build_opener(ProxyHandler({}))
  def api(path,body=None,raw=False):
   req=Request(engine.info['base']+'/api/'+path,data=None if body is None else canonical(body).encode(),headers={'Content-Type':'application/json','X-C600-Token':engine.info['token']})
   with opener.open(req,timeout=30) as response:payload=response.read()
   return payload if raw else json.loads(payload)
  original=api('structure');assert original==structure
  base=api('status');labels=api('labels',raw=True);r=api('focus',{'color':1});assert r['focus']['color']==1 and r['state_hash']==base['state_hash'] and r['head']==base['head'] and api('labels',raw=True)==labels
  for body in ({},{'color':False},{'color':601},{'color':1,'selected':0},{'color':2,'native_since':None}):
   frozen=canonical(api('status'));raw=api('styles',raw=True)
   try:api('focus',body)
   except HTTPError as e:assert e.code==400
   else:raise AssertionError('HTTP focus accepted invalid/unavailable atomic request')
   assert canonical(api('status'))==frozen and api('styles',raw=True)==raw and api('labels',raw=True)==labels
  api('focus',{'color':None});assert api('status')['focus'] is None and api('structure')==original
 checks.append('Authenticated HTTP exposes unchanged immutable geometry and strict independent focus; invalid body or unavailable native snapshot cannot partly apply focus')
 assert hashes=={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in sources}
 report=dict(passed=True,scope='Derived real model geometry, exact backend counts, isolated SQLite and authenticated HTTP; no auxiliary-view rendering or input claims',checks=[dict(name=n,passed=True) for n in checks],seconds=time.perf_counter()-started,source_sha256=hashes)
 (work/'report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
if __name__=='__main__':main()
