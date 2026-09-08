"""Complete-model topology, filters, inspection and atomic native snapshots.

Actual isolated SQLite/HTTP on Windows; native geometry is deliberately shuffled
and synthetic. This is not evidence of mouse handling or GPU rendering.
"""
from pathlib import Path
import argparse,base64,hashlib,json,sqlite3,sys,time,traceback
from urllib.error import HTTPError
from urllib.request import Request,ProxyHandler,build_opener
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import Model,PuzzleState,Filters,canonical
from session import Session
from engine_process import EngineProcess
from test_native_bridge import make_fixture

WORD=[1,2,7,31,101,600,1200,65,433,812,1198]
SOURCES=('core.py','session.py','server.py','native_bridge.py','tests/test_color_inspection.py')
BAD_FILTERS=['color(C0)','color(C601)','color(1)','cell(C0)','adjacent(C601)',
 'layer(C0,L0)','layer(C601,L0)','layer(C1,L16)','layer(C1,L-1)',
 'layer(1,L0)','layer(C1,0)','layer(C1 L0)','layer(C1,L0','home_layer(C601,L0)',
 'layer(V1,L0)','home_layer(V1,L0)','color(C1);all','layer(C1,L0) all']

def same(a,b):
 a,b=np.asarray(a),np.asarray(b)
 assert a.shape==b.shape,(a.shape,b.shape)
 assert np.array_equal(a,b),np.flatnonzero(a!=b)[:12].tolist()
def rejects(fn,kind=ValueError):
 try:fn()
 except kind:return
 raise AssertionError('Invalid input accepted')
def commit(s,word):
 p=s.preview([dict(kind='word',moves=word)]);s.commit(p['token'])
def mechanics(s):return (s.st.labels.tobytes(),s.st.hash,s.head,s.rev,None if s.pending is None else s.pending['token'],s.db.execute('SELECT COUNT(*) FROM events').fetchone()[0])
def frozen(s):return (mechanics(s),canonical(s.prefs),s._get('prefs'),s.render_styles().tobytes(),s.interactive_styles().tobytes())
def apply_delta(reply,previous=None):
 assert reply['format']=='C600-native-snapshot-v2'
 assert set(reply['revisions'])=={'state','color','visibility','interaction','annotation'}
 assert all(isinstance(v,str) and len(v)==64 for v in reply['revisions'].values())
 values=[np.frombuffer(base64.b64decode(reply[k]),dtype=d) for k,d in [('colors','<u2'),('styles','u1'),('interactive','u1')]]
 if reply['mode']=='full':
  assert all(len(v)==259800 for v in values) and 'base_revision' not in reply
  result=[v.copy() for v in values]
 else:
  assert previous and reply['base_revision']==previous[0]
  indices=np.frombuffer(base64.b64decode(reply['indices']),'<u4')
  assert len(indices)==len(np.unique(indices)) and (not len(indices) or int(indices[-1])<259800)
  assert np.all(indices[1:]>indices[:-1]) and all(len(v)==len(indices) for v in values)
  result=[v.copy() for v in previous[1]]
  for out,new in zip(result,values):out[indices]=new
 assert np.all(result[0]<600) and np.all(result[1]<=6) and np.all(result[2]<=1)
 return reply['revision'],result

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--work-dir',type=Path,required=True);ap.add_argument('--skip-http',action='store_true');args=ap.parse_args()
 work=args.work_dir;work.mkdir(parents=True,exist_ok=True)
 assert not (work/'session').exists() and not (work/'http-data').exists(),'Use a fresh work directory'
 hashes={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in SOURCES};started=time.perf_counter();checks=[]
 def check(name,fn):
  begin=time.perf_counter()
  try:details=fn() or {};checks.append(dict(name=name,passed=True,seconds=time.perf_counter()-begin,**details));print('PASS',name,flush=True)
  except Exception as e:checks.append(dict(name=name,passed=False,seconds=time.perf_counter()-begin,error=type(e).__name__+': '+str(e),traceback=traceback.format_exc()));print('FAIL',name,type(e).__name__,str(e),flush=True)
 m=Model();solved=PuzzleState(m);moved=PuzzleState(m);moved.apply(*m.word_net(WORD))
 pair=np.sum((m.normals[:,None,:]-m.normals[None,:,:])**2,axis=2);pair[np.diag_indices(600)]=np.inf
 adj=[set(map(int,np.flatnonzero(np.isclose(row,row.min(),atol=1e-9,rtol=0)))) for row in pair]
 vertex_cells=[set(map(int,np.flatnonzero(m.sp==p)//433)) for p in m.vertex_positions]
 distances={}
 def bfs(c):
  if c not in distances:
   row=np.full(600,-1,np.int16);row[c]=0;front={c};seen={c};depth=0
   while front:
    front=set.union(*(adj[x] for x in front))-seen;depth+=1
    for x in front:row[x]=depth
    seen|=front
   assert len(seen)==600;distances[c]=row
  return distances[c]
 def mask(slots):
  out=np.zeros(m.np,bool);out[np.unique(m.sp[np.flatnonzero(slots)])]=True;return out
 def topology_oracle(t):
  assert t['format']=='C600-structure-v1' and t['model_id']==m.model_id
  assert t['color_count']==600 and t['vertex_count']==120
  assert len(t['colors'])==len(t['adjacency'])==len(t['color_vertices'])==600
  assert len(t['vertices'])==len(t['vertex_incidence'])==120
  for c,r in enumerate(t['colors']):
   assert r['number']==c+1 and r['lab_cell']==c and r['center_position']==int(m.sp[433*c])
   assert set(r['neighbors'])=={x+1 for x in adj[c]}==set(t['adjacency'][c])
   assert set(r['vertices'])=={v+1 for v,cells in enumerate(vertex_cells) if c in cells}==set(t['color_vertices'][c])
  for v,r in enumerate(t['vertices']):
   assert r['number']==v+1 and r['position']==int(m.vertex_positions[v])
   assert set(r['incident_colors'])=={c+1 for c in vertex_cells[v]}==set(t['vertex_incidence'][v])
 def geometry():
  assert m.n==259800 and m.np==177120 and all(len(a)==4 for a in adj)
  fixed=np.flatnonzero(m.oid[m.sp]==-1);same(fixed,np.arange(600)*433);same(m.sp[fixed],m.center_piece)
  assert int(m.center_piece[2])!=866,'Fixture must catch center slot/piece confusion'
  assert all(len(c)==20 for c in vertex_cells)
  assert all(sum(c in row for row in vertex_cells)==4 for c in range(600))
  for c in range(600):same(m.cell_distances(c),bfs(c));assert np.flatnonzero(bfs(c)==0).tolist()==[c] and int(bfs(c).max())==15
  topology_oracle(m.structure());assert len(m.distance_cache)<=32
  return dict(colors=600,vertices=120,bfs_roots=600,max_layer=15)
 check('Independent geometry verifies 600 fixed centers, all adjacency/BFS tables and complete one-fetch vertex topology',geometry)
 def predicates():
  differences=0
  for st in (solved,moved):
   f=Filters(st)
   for c in range(600):
    current=mask(m.ids//433==c);home=mask(st.labels//433==c);near=mask(np.isin(m.ids//433,list(adj[c])))
    same(f.parse(f'cell(C{c+1})'),current);same(f.parse(f'color(C{c+1})'),home);same(f.parse(f'adjacent(C{c+1})'),near)
    if c in (0,299,599):same(f.parse(f'current(C{c:03d})'),current);same(f.parse(f'home(C{c:03d})'),home)
    differences+=int(np.count_nonzero(current!=home))
  assert differences>0
  return dict(colors=600,states=2,home_current_differences=differences)
 check('Color, cell and adjacent whole-piece predicates match labelled-slot oracles and preserve legacy zero-based numbering',predicates)
 def layers():
  for c in (0,199,399,599):
   overlaps=np.zeros(m.np,int)
   for st in (solved,moved):
    f=Filters(st)
    for layer in range(16):
     expected=mask(bfs(c)[m.ids//433]==layer);same(f.parse(f'layer(C{c+1},L{layer})'),expected);same(f.parse(f'home_layer(C{c+1},L{layer})'),expected[st.at])
     if st is solved:overlaps+=expected
    same(f.parse(f'layer(C{c+1},L0)'),f.parse(f'cell(C{c+1})'));same(f.parse(f'layer(C{c+1},L1)'),f.parse(f'adjacent(C{c+1})'))
   assert np.all(overlaps>=1) and np.any(overlaps>1)
  assert len(m.layer_mask_cache)<=48
  same(Filters(moved).parse('layer(C1,L0)'),mask(m.ids//433==0))
  for expr in BAD_FILTERS:rejects(lambda expr=expr:Filters(moved).parse(expr))
  return dict(roots=4,layers=16,states=2,malformed_rejected=len(BAD_FILTERS))
 check('Center-origin any-touch layers overlap correctly, follow home identities and survive bounded cache eviction',layers)

 s=Session(m,work/'session')
 try:
  commit(s,WORD);s.save_prefs(dict(rules=[dict(expr='nothing',style='solid')],pin_safety=False));s.preview([dict(kind='word',moves=[2])])
  by_size={}
  for p in np.flatnonzero(s.st.at!=m.pids):by_size.setdefault(int(m.k[p]),int(p))
  def left():
   assert max(by_size)==20;before=mechanics(s);clicks=0
   for size,p in by_size.items():
    occupant=int(s.st.at[p]);cells=sorted(set(map(int,np.flatnonzero(m.sp==occupant)//433)));expected=np.zeros(m.n,'u1');expected[np.asarray(cells)*433]=4
    for slot in m.slots(p):
     slot=int(slot);r=s.inspect(p,slot,'home-centers');clicks+=1
     assert r['identity']==occupant and r['source_position']==p and r['clicked_piece']==s.st.piece(p)
     assert r['current_position']==p and r['current_piece']==s.st.piece(p)
     assert sorted(r['home_cells'])==cells and sorted(r['home_colors'])==[c+1 for c in cells]
     same(sorted(r['center_slots']),np.asarray(cells)*433);same(r['center_positions'],m.sp[np.asarray(r['home_cells'])*433])
     assert r['clicked_slot']==slot and r['clicked_cell']==slot//433 and r['clicked_color']==slot//433+1
     assert r['clicked_label']==int(s.st.labels[slot]) and r['clicked_sticker_home_color']==int(s.st.labels[slot]//433)+1
     same(s.render_styles(),expected);same(s.interactive_styles(),np.zeros(m.n,'u1'));assert mechanics(s)==before
   return dict(piece_sizes=sorted(by_size),all_stickers_clicked=clicks)
  check('Shift-left highlights every occupying-identity home center for each sticker on moved pieces up to20 stickers',left)
  def right():
   before=mechanics(s)
   for size,p in by_size.items():
    current=int(s.st.where[p]);assert current!=p;r=s.inspect(p,int(m.slots(p)[-1]),'required-piece')
    assert r['target_identity']==r['target_position']==r['source_position']==p and r['required_position']==current
    assert r['required_piece']==s.st.piece(current) and r['required_piece']['piece']==p
    expected=np.zeros(m.n,'u1');expected[m.sp==current]=6;same(s.render_styles(),expected);same(s.interactive_styles(),np.zeros(m.n,'u1'))
    if r['buffer_analysis_available']:
     b=r['buffer_analysis'];assert b['target']==p and b['required_position']==current and b['required_piece']['piece']==p and b['preview_required']
    assert mechanics(s)==before
   for p in [int(m.center_piece[2]),*map(int,m.trees[33]['buffers'])]:
    r=s.inspect(p,int(m.slots(p)[0]),'required-piece');assert not r['buffer_analysis_available'] and r['buffer_analysis_reason'] and 'buffer_analysis' not in r;assert mechanics(s)==before
   return dict(piece_sizes=sorted(by_size),fixed_buffer_center_exceptions=3)
  check('Shift-right tracks the identity belonging at the clicked destination and uses that destination for guarded analysis',right)
  def persistence():
   p=by_size[20];s.inspect(p,int(m.slots(p)[0]),'required-piece');before=s.inspection_info();labels=s.st.labels.copy();s.checkpoint('Inspection')
   s.undo();assert s.inspection_info()['required_position']==p and s.inspection_info()['target_position']==p
   s.redo();assert s.inspection_info()==before;same(s.st.labels,labels)
   s.clear_inspection();assert s.inspection_info() is None and s.prefs['selected'] is None
   s.restore('Inspection');assert s.inspection_info()==before;same(s.st.labels,labels)
   assert s.status()['inspection']['clicked_label']==before['clicked_label']
  check('Durable annotation preserves exact click metadata across undo, redo, clear and checkpoint restore',persistence)
  def interaction():
   s.save_prefs(dict(rules=[dict(expr='O33',style='solid')],pin_safety=True));s.preview([dict(kind='word',moves=[1])])
   exact=(m.so==33).astype('u1')*2;same(s.interactive_styles(),exact);assert np.any((s.render_styles()!=0)&(exact==0))
   s.save_prefs({'pin_safety':False});same(s.interactive_styles(),exact)
  check('Inspection, preview, buffer and selection context can draw outside the filter but never expand its interaction mask',interaction)
  def selected_filter_invariance():
   p=by_size[20];selected=int(s.st.at[p]);s.save_prefs(dict(selected=selected,rules=[dict(expr='selected',style='solid')],pin_safety=False))
   exact=s.interactive_styles().copy();assert np.any(exact) and selected!=p
   for gesture in ('home-centers','required-piece'):
    s.inspect(p,int(m.slots(p)[0]),gesture);assert s.prefs['selected']==selected;same(s.interactive_styles(),exact)
   s.clear_inspection();assert s.prefs['selected']==selected;same(s.interactive_styles(),exact)
   s.inspect(p,int(m.slots(p)[0]),'required-piece')
  check('Both inspection gestures and clear preserve original selected identity and an active selected-based interaction filter',selected_filter_invariance)
  def rollback():
   before=frozen(s);p=by_size[20];slot=int(m.slots(p)[0])
   for pos,sl,g in ((True,slot,'home-centers'),(p,True,'home-centers'),(-1,slot,'required-piece'),(p,m.n,'required-piece'),(p,0,'home-centers'),(p,slot,'bad'),(p,slot,None)):
    rejects(lambda:s.inspect(pos,sl,g));assert frozen(s)==before
   original=s.prefs['inspection']
   for change in ({'clicked_slot':True},{'clicked_label':m.n},{'source_state':'bad'},{'target_position':m.np},{'extra':1}):
    invalid=dict(original,**change);rejects(lambda:s.save_prefs({'inspection':invalid}));assert frozen(s)==before
   s.db.execute("CREATE TEMP TRIGGER reject_prefs BEFORE INSERT ON meta WHEN NEW.key='prefs' BEGIN SELECT RAISE(FAIL,'injected inspection failure'); END")
   try:
    for gesture in ('home-centers','required-piece'):rejects(lambda:s.inspect(p,slot,gesture),sqlite3.IntegrityError);assert frozen(s)==before
   finally:s.db.execute('DROP TRIGGER reject_prefs')
  check('Malformed clicks, inspection fields and durable write failures preserve complete labels, history, pending work, preferences and masks',rollback)
  def preview_apply():
   rules=[dict(expr='cell(C1)',style='hide'),dict(expr='layer(C1,L1)',style='ghost'),dict(expr='color(C600)',style='highlight')]
   before=frozen(s);r=s.filter_preview({'rules':rules});assert frozen(s)==before
   f=Filters(s.st,s.prefs['orbit']);expected=f.styles(rules,False);old=s.interactive_styles()!=0;shown=expected!=0
   assert r['rules']==rules and r['stickers']==r['visible_stickers']==int(shown.sum()) and r['pieces']==int(shown[m.first].sum())
   assert r['added_stickers']==int(np.count_nonzero(shown&~old)) and r['removed_stickers']==int(np.count_nonzero(old&~shown))
   assert sum(x['stickers'] for x in r['by_orbit'])==r['stickers'] and not r['pins_included'] and not r['annotations_included']
   s.filter_apply(r['rules'],r['context_hash']);same(s.interactive_styles(),expected);assert not s.prefs['pin_safety']
   for mutate in (lambda:s.save_prefs({'orbit':32}),lambda:s.preview([dict(kind='word',moves=[7])]),lambda:commit(s,[1])):
    result=s.filter_preview({'expression':'preview | color(C1)'});mutate();baseline=frozen(s)
    rejects(lambda:s.filter_apply(result['rules'],result['context_hash']));assert frozen(s)==baseline
   for payload in ({},{'expression':'all','rules':[]},{'rules':[{}]},{'expression':'layer(V1,L0)'},{'expression':'all','orbit':True},{'expression':'active','orbit':(s.prefs['orbit']+1)%35}):
    baseline=frozen(s);rejects(lambda:s.filter_preview(payload));assert frozen(s)==baseline
  check('Filter preview counts honor first-match styles; atomic apply binds state, preferences and pending support with full rollback on stale context',preview_apply)
  # Leave no nonpersistent preview support in the persisted comparison.
  s.pending=None;s.save_prefs({'pin_safety':False})
  expected=(s.st.labels.tobytes(),canonical(s.prefs),canonical(s.inspection_info()),s.render_styles().tobytes(),s.interactive_styles().tobytes())
 finally:s.close()
 def reopen():
  s=Session(m,work/'session')
  try:assert (s.st.labels.tobytes(),canonical(s.prefs),canonical(s.inspection_info()),s.render_styles().tobytes(),s.interactive_styles().tobytes())==expected
  finally:s.close()
 check('A new Session reopens all259800 labels and inspection metadata with identical rendering and picking masks',reopen)

 if not args.skip_http:
  geometry,mapping=make_fixture(m);inverse=np.argsort(mapping)
  with EngineProcess(ROOT,work/'http-data',work/'launch.json',work/'engine.log',timeout=90) as engine:
   opener=build_opener(ProxyHandler({}))
   def api(path,body=None,raw=False):
    req=Request(engine.info['base']+'/api/'+path,data=None if body is None else canonical(body).encode(),headers={'Content-Type':'application/json','X-C600-Token':engine.info['token']})
    with opener.open(req,timeout=180) as reply:payload=reply.read()
    if raw:return payload
    data=json.loads(payload)
    if 'job' in data:
     until=time.monotonic()+180
     while time.monotonic()<until:
      result=api('job/'+data['job'])
      if result.get('done'):
       if 'error' in result:raise AssertionError(result['error'])
       return result['result']
      time.sleep(.01)
     raise TimeoutError('Synthetic bridge/operation timed out')
    return data
   def snapshot():return canonical(api('status')),api('labels',raw=True),api('styles',raw=True)
   def reject_http(path,body,contains=None):
    before=snapshot()
    try:api(path,body)
    except HTTPError as e:
     text=e.read().decode();assert e.code==400 and (contains is None or contains in text),(e.code,text)
    else:raise AssertionError('Invalid HTTP request accepted: '+path)
    assert snapshot()==before
   def topology_http():
    before=snapshot();t=api('structure');topology_oracle(t);assert api('structure')==t and snapshot()==before
    reject_http('native/inspect',dict(native_sticker=0,pre_state=api('status')['state_hash'],gesture='home-centers'),'bridge')
   check('Authenticated topology HTTP is immutable and inspection requires the verified native bridge',topology_http)
   handshake=api('native/handshake',geometry);assert handshake['matched_stickers']==259800 and handshake['matched_generators']==1200
   preview=api('preview',{'recipe':[dict(kind='word',moves=WORD)]});api('commit',{'token':preview['token']})
   current=PuzzleState(m,np.frombuffer(api('labels',raw=True),'<u4'));p=int(np.flatnonzero((m.k==20)&(current.at!=m.pids))[0]);slot=int(m.slots(p)[-1]);ns=int(inverse[slot])
   api('prefs',dict(rules=[dict(expr=f'position(P{p})',style='solid')],pin_safety=False,inspection=None));api('preview',{'recipe':[dict(kind='word',moves=[2])]})
   def match_full(cached):
    full=api('native/snapshot');same(cached[1][0],np.frombuffer(base64.b64decode(full['colors']),'<u2'));same(cached[1][1],np.frombuffer(base64.b64decode(full['styles']),'u1'));same(cached[1][2],np.frombuffer(base64.b64decode(full['interactive']),'u1'))
   def gestures_http():
    before=api('status');labels=api('labels',raw=True);cached=apply_delta(api('native/snapshot?protocol=2'))
    for gesture in ('home-centers','required-piece'):
     r=api('native/inspect',dict(native_sticker=ns,pre_state=before['state_hash'],gesture=gesture,native_since=cached[0]))
     assert r['clicked_slot']==slot and r['clicked_piece']==current.piece(p)
     expected=(m.sp==p).astype('u1')*2
     if gesture=='home-centers':expected[np.asarray(r['center_slots'])]=4;assert len(r['center_slots'])==20 and r['identity']==int(current.at[p])
     else:expected[m.sp==int(current.where[p])]=6;assert r['target_position']==p and r['required_position']==int(current.where[p]) and r['buffer_analysis']['target']==p
     cached=apply_delta(r['native_snapshot'],cached);same(cached[1][1],expected[mapping]);same(cached[1][2],(m.sp[mapping]==p).astype('u1'));match_full(cached)
     assert r['native_snapshot']['state']['inspection']['clicked_slot']==slot
     outside=np.flatnonzero((expected!=0)&(m.sp!=p));assert len(outside)
     hidden=int(inverse[outside[0]])
     reject_http('native/inspect',dict(native_sticker=hidden,pre_state=before['state_hash'],gesture=gesture),'Hidden');reject_http('native/select',dict(native_sticker=hidden),'Hidden')
     after=api('status');assert api('labels',raw=True)==labels
     for key in ('head','revision','state_hash','pending','transactions'):assert after[key]==before[key]
    zero=api('native/snapshot?protocol=2&since='+cached[0]);assert zero['mode']=='delta' and base64.b64decode(zero['indices'])==b'';match_full(apply_delta(zero,cached))
   check('Both gestures over shuffled native slots return atomic deltas, preserve mechanics and reject annotation-only hidden hits',gestures_http)
   def rejected_http():
    h=api('status')['state_hash']
    for change in ({'native_sticker':True},{'native_sticker':-1},{'native_sticker':m.n},{'native_sticker':'1'},{'gesture':'bad'},{'gesture':None},{'pre_state':None},{'profile_sha256':'bad'},{'native_since':True}):
     body=dict(native_sticker=ns,pre_state=h,gesture='home-centers');body.update(change);reject_http('native/inspect',body)
    for expr in BAD_FILTERS:reject_http('filter-preview',{'expression':expr})
    preview=api('preview',{'recipe':[dict(kind='word',moves=[1])]});api('commit',{'token':preview['token']});assert api('status')['state_hash']!=h
    reject_http('native/inspect',dict(native_sticker=ns,pre_state=h,gesture='required-piece'),'stale')
    api('prefs',dict(rules=[dict(expr='nothing',style='solid')]))
    reject_http('native/inspect',dict(native_sticker=ns,pre_state=api('status')['state_hash'],gesture='home-centers'),'Hidden')
    r=api('inspection/clear',{});assert r['inspection'] is None and r['prefs']['selected'] is None and not any(api('styles',raw=True))
   check('Malformed, stale and formerly-visible hidden HTTP requests roll back all state; explicit clear removes only annotations',rejected_http)
   def filter_http():
    before=snapshot();r=api('filter-preview',{'expression':'layer(C1,L1) | color(C600)'});assert snapshot()==before
    cached=apply_delta(api('native/snapshot?protocol=2'));result=api('filter-apply',dict(rules=r['rules'],context_hash=r['context_hash'],native_since=cached[0]));cached=apply_delta(result['native_snapshot'],cached);match_full(cached)
    assert np.count_nonzero(cached[1][2])==r['stickers'] and not result['prefs']['pin_safety']
    stale=api('filter-preview',{'expression':'color(C1)'});api('prefs',{'orbit':31});reject_http('filter-apply',dict(rules=stale['rules'],context_hash=stale['context_hash']),'stale')
   check('HTTP filter preview and atomic apply match exact masks/counts and reject stale preference contexts',filter_http)
   def native_deltas():
    cached=apply_delta(api('native/snapshot?protocol=2'));original=cached[0]
    profile=json.loads((work/'http-data/native_profile.json').read_text(encoding='utf-8'));token=next(k for k,v in profile['token_words'].items() if v==[1])
    before=api('status');r=api('native/turn',dict(pre_state=before['state_hash'],tokens=[token],native_since=cached[0]));cached=apply_delta(r['native_snapshot'],cached);match_full(cached)
    assert r['state_hash']==r['native_snapshot']['state']['state_hash']!=before['state_hash']
    assert r['native_snapshot']['revisions']['state']==r['state_hash']
    for orbit in (30,29,28,27,26):
     r=api('prefs',dict(orbit=orbit,native_since=cached[0]));cached=apply_delta(r['native_snapshot'],cached)
    assert api('native/snapshot?protocol=2&since='+original)['mode']=='full'
    assert api('native/snapshot?protocol=2&since=unknown')['mode']=='full'
    r=api('prefs',dict(rules=[dict(expr='everything',style='solid')],native_since=cached[0]));assert r['native_snapshot']['mode']=='full';match_full(apply_delta(r['native_snapshot']))
    # A new verified profile invalidates old revisions even with identical state.
    prior=r['native_snapshot']['revision'];api('native/handshake',geometry)
    assert api('native/snapshot?protocol=2&since='+prior)['mode']=='full'
    assert api('health')['ready']
   check('Atomic native-turn snapshots match full arrays; cache eviction, unknown bases, dense changes and profile changes force safe full snapshots',native_deltas)
  assert engine.process.returncode==0 and not engine.cleanup_result['forced']
 after={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in SOURCES}
 report=dict(passed=all(c['passed'] for c in checks) and hashes==after,scope='Independent full-model oracles, isolated SQLite and authenticated Windows HTTP with synthetic native geometry; no actual mouse/GPU claim',checks=checks,source_sha256=hashes,source_after_sha256=after,sources_unchanged=hashes==after,labelled_slots=m.n,physical_pieces=m.np,seconds=time.perf_counter()-started)
 (work/'color-inspection-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('COLOR INSPECTION',report['passed'],len(checks),flush=True)
 return 0 if report['passed'] else 1
if __name__=='__main__':raise SystemExit(main())
