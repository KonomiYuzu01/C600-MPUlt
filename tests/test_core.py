from __future__ import annotations
import sys,tempfile,time,json,random,platform,sqlite3,gzip
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import *
from session import Session
from verify import verify_certificate,verify_session

def main():
 start=time.perf_counter();m=Model();report={'scope':'C600 Studio core tests on '+platform.system()+'; all full sticker slots retained','python':platform.python_version(),'numpy':np.__version__,'load_seconds':m.load_seconds,'checks':[],'orbit_tests':[]}
 def check(name,fn):
  t=time.perf_counter();fn();report['checks'].append(dict(name=name,passed=True,seconds=time.perf_counter()-t));print('PASS',name,flush=True)
 def generators():
  for i in range(1,1201):
   s,d=m.move(i);a=m.ids.copy();a[d]=a[s];u,v=m.move(-i);a[v]=a[u];assert np.array_equal(a,m.ids)
 check('All 1200 generators followed by inverse restore all 259800 labels',generators)
 def filters():
  st=PuzzleState(m);f=Filters(st)
  assert f.parse('rank = 14 & stickers = 1').sum()==14400
  assert f.parse('O26').sum()==7200 and f.parse('O27').sum()==7200
  assert not np.any(f.parse('O26 & O27'))
  assert f.parse('home(C013) & !current(C013)').sum()==0
  for text in ['__import__(os)','everything;delete','O33 &', '(' * 50+'O33'+')'*50]:
   try:f.parse(text)
   except ValueError:pass
   else:raise AssertionError('Unsafe/malformed filter accepted: '+text)
  before=st.hash;f.styles([{'expr':'everything','style':'hide'}],False);assert st.hash==before
 check('True orbit separation, home/current predicates, malformed input rejection',filters)
 for o in range(35):
  t=time.perf_counter();cert=m.certify_seed(o);tree=m.trees[o];nodes=[0,len(tree['positions'])//2,len(tree['positions'])-1]
  for n in nodes:
   recipe=[{'kind':'star','orbit':o,'node':n,'sign':1}];s,d=m.star_net(o,n);u,v=m.word_net(list(m.expand(recipe)))
   assert np.array_equal(s,u) and np.array_equal(d,v)
  # Mix oriented targets, then complete this orbit using real full-collateral steps.
  st=PuzzleState(m);rng=random.Random(4400+o)
  for _ in range(8):
   n=rng.randrange(len(tree['positions']));s,d=m.star_net(o,n,rng.choice((-1,1)));st.apply(s,d)
  steps=0;phases=[];planner=Planner(m)
  while True:
   cs,note=planner.next(st,o)
   if not cs:break
   for c in cs:s,d=m.star_net(c['orbit'],c['node'],c['sign']);st.labels[d]=st.labels[s]
   st.refresh();steps+=len(cs);phases.append(note['phase']);assert steps<160
  assert np.all(st.correct[m.oid==o])
  report['orbit_tests'].append(dict(orbit=o,seed_verified=True,expanded_stars_checked=3,oriented_local_completion=True,stars=steps,phases=sorted(set(phases)),seconds=time.perf_counter()-t));print('PASS orbit',o,flush=True)
 def journal():
  directory=Path(tempfile.mkdtemp());session=Session(m,directory);root=session.st.hash
  session.save_prefs({'protected':[33]});p=session.preview([{'kind':'star','orbit':33,'node':0,'sign':1}]);assert p['conflicts']
  try:session.commit(p['token'])
  except ValueError:pass
  else:raise AssertionError('Protected macro was committed')
  assert session.head==0 and session.st.hash==root
  session.save_prefs({'protected':[]});session.commit(p['token']);h1=session.st.hash
  session.checkpoint('one star');p=session.preview([{'kind':'word','moves':[2,4,6]}]);data=session.certificate();assert verify_certificate(m,data)['passed'];session.commit(p['token']);h2=session.st.hash
  session.undo();assert session.st.hash==h1;session.redo();assert session.st.hash==h2
  r=session.export();assert verify_session(m,r,True)['passed'];session.close()
  session=Session(m,directory);assert session.st.hash==h2;session.restore('one star');assert session.st.hash==h1
  # A new branch does not delete the prior one.
  p=session.preview([{'kind':'word','moves':[12]}]);session.commit(p['token']);assert session.db.execute('SELECT COUNT(*) FROM events').fetchone()[0]==4
  rec=session.export();copy=Session(m,Path(tempfile.mkdtemp()));copy.import_record(rec);assert copy.st.hash==session.st.hash
  corrupt=json.loads(canonical(rec));corrupt['events'][0]['post']='0'*64
  old=copy.st.hash
  try:copy.import_record(corrupt)
  except ValueError:pass
  else:raise AssertionError('Tampered import accepted')
  assert copy.st.hash==old;copy.close();session.close()
 check('Protection, witness verification, journal recovery, undo/redo, branches, import tamper rejection',journal)
 report['total_seconds']=time.perf_counter()-start;report['passed']=True
 (ROOT/'tests'/'core_report.json').write_text(json.dumps(report,indent=2));print('ALL TESTS PASSED',report['total_seconds'],flush=True)
if __name__=='__main__':main()
