"""Whole-piece filters, exact visibility, and preference failure recovery.

Uses isolated full-model sessions and actual authenticated loopback HTTP. It does
not open MPUlt or measure GPU performance. A JSON report retains each outcome.
"""
from pathlib import Path
import argparse,hashlib,json,platform,sqlite3,sys,tempfile,time,traceback
from urllib.error import HTTPError
from urllib.request import ProxyHandler,Request,build_opener
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import Model,PuzzleState,Filters,canonical
from session import Session
from enhanced import Workflow
from engine_process import EngineProcess


def main():
 ap=argparse.ArgumentParser();ap.add_argument('--work-dir',type=Path);ap.add_argument('--skip-http',action='store_true');args=ap.parse_args()
 work=args.work_dir or Path(tempfile.mkdtemp(prefix='c600-filters-'));work.mkdir(parents=True,exist_ok=True)
 checks=[];started=time.perf_counter();m=Model()
 def check(name,fn):
  t=time.perf_counter()
  try:
   details=fn() or {};checks.append(dict(name=name,passed=True,seconds=time.perf_counter()-t,**details));print('PASS',name,flush=True)
  except Exception as e:
   checks.append(dict(name=name,passed=False,seconds=time.perf_counter()-t,error=type(e).__name__+': '+str(e),traceback=traceback.format_exc()));print('FAIL',name,type(e).__name__,str(e),flush=True)
 def same(actual,expected):
  assert actual.shape==expected.shape,(actual.shape,expected.shape)
  assert np.array_equal(actual,expected),'Array mismatch at '+str(np.flatnonzero(actual!=expected)[:10].tolist())
 def rejects(fn):
  try:fn()
  except ValueError:return
  raise AssertionError('Malformed input was accepted')
 def styles(f,expr,code=2):return f.styles([dict(expr=expr,style={0:'hide',1:'ghost',2:'solid',3:'highlight'}[code])],False)
 word=[1,7,31,101,600,1200,65,433,812,1198]
 source,destination=m.word_net(word);solved=PuzzleState(m);moved=PuzzleState(m);moved.apply(source,destination)

 def exact_orbits():
  original=moved.labels.copy();before=moved.hash;counts=[]
  for st in (solved,moved):
   for orbit in range(35):
    f=Filters(st,orbit=orbit,selected=int(m.pids[m.oid!=orbit][0]),preview=m.pids.tolist())
    expected=(m.oid[m.sp]==orbit).astype('u1')*2
    same(styles(f,'active'),expected);same(styles(f,'O%02d'%orbit),expected)
    same(styles(f,'orbit = O%02d'%orbit),expected)
    if st is solved:counts.append(int(np.count_nonzero(expected)))
   same(styles(f,'nothing'),np.zeros(m.n,'u1'));same(styles(f,'everything'),np.full(m.n,2,'u1'))
  same(moved.labels,original);assert moved.hash==before
  return dict(orbits=35,states=2,slots=m.n,orbit_sticker_counts=counts)
 check('Exact mode retains only matching whole pieces across all 35 orbits before and after legal moves',exact_orbits)

 def predicates():
  st=moved;f=Filters(st,orbit=33,protected=[0,26,33]);at=m.sp[st.labels[m.first]]
  for expr,mask in [('rank = 14 & stickers = 1',(m.rank==14)&(m.k==1)),('O26 | O27',np.isin(m.oid,[26,27])),('!O26 & O27',m.oid==27),('not (O26 or O27)',~np.isin(m.oid,[26,27])),('protected',np.isin(m.oid,[0,26,33]))]:same(f.parse(expr),mask)
  exact=np.logical_and.reduceat((st.labels==m.ids)[m.psorted],m.fo[:-1]);position=at==m.pids
  color=np.logical_and.reduceat((st.labels//433==m.ids//433)[m.psorted],m.fo[:-1])
  for expr,mask in [('solved',exact),('unsolved',~exact),('position_correct',position),('position_wrong',~position),('orientation_wrong',position&~exact),('color_wrong',~color)]:same(f.parse(expr),mask)
  differences=0
  for cell in (0,13,299,599):
   current=np.isin(m.pids,np.unique(m.sp[cell*433:(cell+1)*433]));home=current[at]
   same(f.parse(f'current(C{cell:03d})'),current);same(f.parse(f'current_has(C{cell:03d})'),current)
   same(f.parse(f'home(C{cell:03d})'),home);same(f.parse(f'home_has(C{cell:03d})'),home)
   same(styles(f,f'current(C{cell:03d})'),current[m.sp].astype('u1')*2);differences+=np.count_nonzero(current!=home)
   cap_ids=np.repeat(m.pids,np.diff(m.mo))[m.masks==cell];same(f.parse(f'cap(C{cell:03d})'),np.isin(m.pids,cap_ids))
   same(f.parse(f'shell(C{cell:03d}, 0)'),current)
   shell_ids=np.unique(np.concatenate([m.sp[x*433:(x+1)*433] for x in m.adj[cell]]));same(f.parse(f'shell(C{cell:03d}, 1)'),np.isin(m.pids,shell_ids))
  assert differences>0,'Moves did not exercise home/current differences'
  return dict(home_current_differences=int(differences),whole_piece_workcell_stickers=int(np.count_nonzero(styles(f,'current(C000)'))))
 check('Boolean precedence, rank, correctness, home/current, cap, and shell masks match independent array expectations',predicates)

 def precedence_and_pins():
  selected=int(np.flatnonzero(moved.at!=m.pids)[0]);identity=int(moved.at[selected]);preview=[selected,int(m.trees[33]['buffers'][0]),int(m.pids[m.oid==27][0])]
  f=Filters(moved,33,identity,[27],preview)
  rules=[dict(expr='O26',style='hide'),dict(expr='O26 | O27',style='highlight'),dict(expr='O33',style='ghost'),dict(expr='everything',style='solid')]
  expected=np.full(m.np,2,'u1');expected[m.oid==26]=0;expected[m.oid==27]=3;expected[m.oid==33]=1
  same(f.styles(rules,False),expected[m.sp]);same(f.styles([],False),np.zeros(m.n,'u1'))
  pinned=np.zeros(m.np,'u1');pinned[m.trees[33]['buffers']]=4;pinned[preview]=5;pinned[moved.at==identity]=6
  same(f.styles([dict(expr='nothing',style='solid')],True),pinned[m.sp])
  same(f.styles([dict(expr='nothing',style='solid')],False),np.zeros(m.n,'u1'))
  return dict(exact_hidden_stickers=0,context_stickers=int(np.count_nonzero(pinned[m.sp])),priority='selected > preview > buffer > first matching rule')
 check('First matching rule includes explicit hide; optional pins have exact documented override union and priority',precedence_and_pins)

 def malformed_parser():
  f=Filters(moved)
  for text in ('','O33 &','O33 O26','(O33','O33)','O33;everything','__import__(os)','O33 xor O27','current(C600)','home(C600)','cap(C600)','shell(C000,31)','shell(C000 1)','set(unknown)','!'*41+'O33','('*41+'O33'+')'*41,'a'*2049,' | '.join(['O33']*260)):
   rejects(lambda text=text:f.parse(text))
  rejects(lambda:f.styles([dict(expr='all',style='bogus')],False));rejects(lambda:f.styles([dict(expr='all',style='solid')]*33,False))
 check('Malformed expressions, invalid spatial boundaries, excessive rules/tokens/nesting are rejected',malformed_parser)

 with tempfile.TemporaryDirectory(prefix='session-',dir=work) as td:
  s=Session(m,Path(td));wf=Workflow(s)
  try:
   def identity_sets():
    pos=int(np.flatnonzero(moved.at!=m.pids)[0]);identity=int(moved.at[pos]);s.st=PuzzleState(m)
    wf.save_set('Physical',f'piece(P{identity})','identity');wf.save_set('Location',f'position(P{identity})','position')
    p=s.preview([dict(kind='word',moves=word)]);s.commit(p['token'])
    f=Filters(s.st,sets=s.prefs['named_sets'],selected=identity)
    same(f.parse('set(Physical)'),s.st.at==identity);same(f.parse('piece(P%d)'%identity),s.st.at==identity);same(f.parse('selected'),s.st.at==identity)
    same(f.parse('set(Location)'),m.pids==identity);same(f.parse('position(P%d)'%identity),m.pids==identity)
    assert not np.array_equal(f.parse('set(Physical)'),f.parse('set(Location)'))
    return dict(identity=identity,new_position=int(s.st.where[identity]))
   check('Saved identity sets and selection follow moving pieces while position sets stay fixed',identity_sets)
   def preview_sets():
    pending=s.preview([dict(kind='word',moves=[1])]);positions=np.unique(m.sp[s.pending['src']]);assert len(positions)>0
    head=s.head;before=s.st.hash;labels=s.st.labels.copy()
    p=wf.save_set('PreviewPositions','preview','position');i=wf.save_set('PreviewIdentities','preview_support','identity')
    assert p['members']==len(positions) and i['members']==len(positions),'Saved preview set lost pending support'
    same(np.asarray(s.prefs['named_sets']['PreviewPositions']['ids']),positions)
    same(np.asarray(s.prefs['named_sets']['PreviewIdentities']['ids']),s.st.at[positions])
    f=Filters(s.st,sets=s.prefs['named_sets']);same(f.parse('set(PreviewPositions)'),np.isin(m.pids,positions));same(f.parse('set(PreviewIdentities)'),np.isin(s.st.at,s.st.at[positions]))
    assert s.pending['token']==pending['token'] and s.head==head and s.st.hash==before;same(s.st.labels,labels)
    return dict(preview_support_pieces=len(positions),position_and_identity_sets_match=True)
   check('Named sets created from preview and preview_support retain the actual complete pending support',preview_sets)
   def preview_visibility():
    s.save_prefs({'rules':[dict(expr='O33',style='solid')],'pin_safety':False,'selected':int(m.pids[m.oid==27][0])})
    before=s.st.hash;head=s.head;labels=s.st.labels.copy();p=s.preview([dict(kind='word',moves=word)])
    exact=(m.so==33).astype('u1')*2;same(s.render_styles(),exact)
    s.save_prefs({'pin_safety':True});pinned=s.render_styles();assert np.any((pinned!=0)&(m.so!=33))
    s.save_prefs({'pin_safety':False});same(s.render_styles(),exact);same(s.st.labels,labels)
    assert s.st.hash==before and s.head==head and s.pending['token']==p['token']
    s.checkpoint('exact filter');s.save_prefs({'rules':[dict(expr='everything',style='solid')],'pin_safety':True});s.restore('exact filter')
    same(s.render_styles(),exact);assert s.prefs['pin_safety'] is False
    return dict(exact_count=int(np.count_nonzero(exact)),optional_context_count=int(np.count_nonzero(pinned)),restored_rules=s.prefs['rules'])
   check('Preview support cannot leak through exact mode; checkpoint restores the saved actual filter and pin mode',preview_visibility)
   malformed=[None,[],{'pin_safety':'false'},{'pin_safety':0},{'pin_safety':None},{'named_sets':None},{'named_sets':[]},{'named_sets':{'Bad':[]}},{'named_sets':{'Bad':dict(kind='identity',ids=[True])}},{'named_sets':{'Bad':dict(kind='other',ids=[])}},{'macro_library':None},{'macro_library':[]},{'macro_library':{'Bad':[]}},{'rules':None},{'rules':[None]},{'rules':[{}]},{'rules':[dict(expr='all',style=[])]},{'rules':[dict(expr=None,style='solid')]},{'orbit':True},{'selected':True},{'protected':[True]}]
   def preference_validation():
    s.save_prefs({'pin_safety':False});old=canonical(s.prefs);old_db=s._get('prefs');head=s.head;before=s.st.hash;p=s.preview([dict(kind='word',moves=[1])])
    errors=[]
    for change in malformed:
     try:s.save_prefs(change)
     except ValueError:pass
     except Exception as e:errors.append(dict(input=change,error=type(e).__name__+': '+str(e)))
     else:errors.append(dict(input=change,error='accepted'))
     if canonical(s.prefs)!=old or s._get('prefs')!=old_db:errors.append(dict(input=change,error='published invalid prefs'));s.prefs=json.loads(old);s._put('prefs',old_db);s.db.commit()
     assert s.head==head and s.st.hash==before and s.pending['token']==p['token']
    assert not errors,canonical(errors)
    return dict(rejected_inputs=len(malformed))
   check('Malformed preference payloads reject before state, pending token, view, or database publication',preference_validation)
   def durable_failure():
    s.save_prefs({'rules':[dict(expr='O33',style='solid')],'pin_safety':False});old=canonical(s.prefs);old_db=s._get('prefs');old_styles=s.render_styles().copy();head=s.head;before=s.st.hash;pending=s.pending
    s.db.execute("CREATE TEMP TRIGGER reject_prefs BEFORE INSERT ON meta WHEN NEW.key='prefs' BEGIN SELECT RAISE(FAIL,'fixture write rejection'); END")
    try:
     try:s.save_prefs({'rules':[dict(expr='nothing',style='solid')]})
     except sqlite3.IntegrityError:pass
     else:raise AssertionError('Injected durable write failure did not fire')
     assert canonical(s.prefs)==old,'Memory published new filter before durable preference write succeeded'
     assert s._get('prefs')==old_db;same(s.render_styles(),old_styles);assert s.pending is pending and s.head==head and s.st.hash==before
    finally:s.db.execute('DROP TRIGGER reject_prefs');s.prefs=json.loads(old)
   check('Failed durable preference write keeps the prior visible filter, state, pending work, and saved preferences',durable_failure)
  finally:s.close()

 if not args.skip_http:
  def http_boundary():
   with tempfile.TemporaryDirectory(prefix='http-',dir=work) as td:
    td=Path(td)
    with EngineProcess(ROOT,td/'session',td/'launch.json',work/'http-engine.log',timeout=45) as engine:
     def request(path,body=None,raw=False):
      req=Request(engine.info['base']+'/api/'+path,data=None if body is None else json.dumps(body).encode(),headers={'Content-Type':'application/json','X-C600-Token':engine.info['token']})
      try:
       with build_opener(ProxyHandler({})).open(req,timeout=30) as r:return r.status,r.read() if raw else json.load(r)
      except HTTPError as e:return e.code,json.load(e)
     code,baseline=request('prefs',{'rules':[dict(expr='O27',style='solid')],'pin_safety':False});assert code==200
     old=baseline['prefs'];before=baseline['state_hash'];errors=[]
     for change in malformed[1:]:
      status,result=request('prefs',change)
      if status!=400 or 'error' not in result:errors.append(dict(input=change,status=status,error=result.get('error','accepted')))
      now=request('status')[1]
      if now['prefs']!=old:errors.append(dict(input=change,error='invalid preferences published'));request('prefs',old)
      assert now['state_hash']==before and now['head']==0
     assert not errors,canonical(errors)
     data=request('styles',raw=True)[1];same(np.frombuffer(data,'u1'),(m.so==27).astype('u1')*2)
     assert request('health')[1]['ready']
    assert engine.process.returncode==0 and not engine.cleanup_result['forced']
   return dict(rejected_http_inputs=len(malformed)-1,style_slots=len(data))
  check('Actual Windows HTTP rejects malformed filter preferences with 400 and stays healthy with exact styles',http_boundary)
 source_hashes={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in ('core.py','session.py','enhanced.py','server.py','tests/test_piece_filters.py')}
 report=dict(passed=all(c['passed'] for c in checks),scope='Full-model CPU and authenticated local HTTP, isolated sessions; no GPU timing claim',platform=platform.platform(),python=platform.python_version(),numpy=np.__version__,slots=m.n,pieces=m.np,seconds=time.perf_counter()-started,source_sha256=source_hashes,checks=checks)
 (work/'piece-filter-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
 print('ALL PIECE FILTER TESTS PASSED' if report['passed'] else 'PIECE FILTER FAILURES RECORDED',report['seconds'],flush=True)
 return 0 if report['passed'] else 1

if __name__=='__main__':raise SystemExit(main())
