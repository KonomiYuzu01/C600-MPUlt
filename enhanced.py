"""0.2 workflow additions. Declarative data only; no user scripts are executed."""
from __future__ import annotations
import json,time,re,zlib,random
import numpy as np
from core import PuzzleState,Filters,canonical,state_hash,invrecipe

class Workflow:
 def __init__(self,session):
  self.s=session;self.timer_total=float(session._get('timer_seconds') or 0);self.timer_start=None;self.timer_saved=time.monotonic()
 def timer(self,action='status'):
  now=time.monotonic()
  if action=='start' and self.timer_start is None:self.timer_start=now
  elif action=='pause' and self.timer_start is not None:self.timer_total+=now-self.timer_start;self.timer_start=None
  elif action not in('start','pause','status','heartbeat'):raise ValueError('Unknown timer action')
  value=self.timer_total+(now-self.timer_start if self.timer_start is not None else 0)
  if action in('pause','start') or now-self.timer_saved>=5:
   with self.s.db:self.s._put('timer_seconds',value)
   self.timer_saved=now
  return dict(seconds=value,running=self.timer_start is not None,scope='Explicitly running session timer, includes thinking time; not an inferred active-work metric')
 def history(self,before=None,limit=100):
  limit=min(200,max(1,int(limit)));before=int(before) if before is not None else 2**63-1
  rows=[dict(x) for x in self.s.db.execute('SELECT id,parent,depth,note,assistance,primitive_count,stars,post,created FROM events WHERE id<? ORDER BY id DESC LIMIT ?',(before,limit))]
  return dict(rows=rows,head=self.s.head,next_before=rows[-1]['id'] if len(rows)==limit else None)
 def checkout(self,head):
  s=self.s;head=int(head)
  if s.event(head) is None:raise ValueError('Unknown journal head')
  chain=[];h=head
  while True:
   snap=s.db.execute('SELECT * FROM snapshots WHERE head=? ORDER BY created DESC LIMIT 1',(h,)).fetchone()
   if snap:break
   ev=s.event(h)
   if ev is None or ev['parent'] is None:raise ValueError('Broken ancestry')
   chain.append(ev);h=ev['parent']
  a=np.frombuffer(zlib.decompress(snap['labels']),dtype='<i4').copy()
  if state_hash(a)!=snap['hash']:raise ValueError('Snapshot checksum mismatch')
  for ev in reversed(chain):
   s.m.check_cancel()
   if state_hash(a)!=ev['pre']:raise ValueError('Branch replay pre-state mismatch')
   src,dst,_,_=s.m.net(json.loads(ev['recipe']));a[dst]=a[src]
   if state_hash(a)!=ev['post']:raise ValueError('Branch replay mismatch')
  st=PuzzleState(s.m,a)
  with s.db:s._put('head',head)
  s.head=head;s.st=st;s.pending=None;s.redo_stack=[];s.rev+=1
  return s.status()
 def stats(self):
  s=self.s;scramble=0;solution=0;assisted=0;h=s.head;operations=0;stars=0
  while h:
   e=s.event(h);n=int(e['primitive_count']);operations+=1;stars+=e['stars']
   if 'scramble' in e['assistance']:scramble+=n
   else:solution+=n
   if e['assistance'].startswith('assisted'):assisted+=1
   h=e['parent']
  return dict(scramble_primitives=str(scramble),solution_primitives=str(solution),stars=stars,operations=operations,assisted_transactions=assisted,timer=self.timer())
 def save_set(self,name,expression,kind):
  s=self.s
  if not re.fullmatch('[A-Za-z_][A-Za-z_0-9]{0,39}',name):raise ValueError('Set name: letters, digits, underscores; first character cannot be a digit')
  if kind not in('identity','position'):raise ValueError('Choose identity or position')
  p=s.prefs;preview=[] if s.pending is None else np.unique(s.m.sp[s.pending['src']]).tolist()
  f=Filters(s.st,p['orbit'],p['selected'],p['protected'],preview,sets=p.get('named_sets'));positions=np.flatnonzero(f.parse(expression))
  if len(positions)>10000:raise ValueError('Saved set maximum is 10,000 members; use a predicate for larger groups')
  ids=s.st.at[positions] if kind=='identity' else positions
  sets=dict(p.get('named_sets',{}));sets[name]=dict(kind=kind,ids=ids.tolist(),source=expression)
  s.save_prefs({'named_sets':sets});return dict(name=name,members=len(ids),kind=kind)
 def compose(self,a,b,operation):
  s=self.s;s.m.normalize(a)
  if operation=='inverse':recipe=invrecipe(a)
  else:
   s.m.normalize(b)
   if operation=='commutator':recipe=a+b+invrecipe(a)+invrecipe(b)
   elif operation=='conjugate':recipe=a+b+invrecipe(a)
   elif operation=='concat':recipe=a+b
   else:raise ValueError('Unknown composition')
  return s.preview(recipe,operation+' composed in the macro editor','manual-composition')
 def scramble(self,count,seed=None,*,apply=False):
  if type(count)!=int or not 1<=count<=10000:raise ValueError('Scramble length must be an integer in 1..10,000')
  if type(apply)!=bool:raise ValueError('Scramble apply must be a boolean')
  if seed is None:seed=random.SystemRandom().randrange(2**32)
  if type(seed)!=int:raise ValueError('Scramble seed must be an integer')
  rng=random.Random(seed);word=[]
  for i in range(count):
   if i%128==0:self.s.m.check_cancel()
   m=rng.randint(1,1200);word.append(m if m%2 else m*rng.choice((-1,1)))
  previous=self.s.pending;previous_head=self.s.head;previous_hash=self.s.st.hash
  try:
   preview=self.s.preview([dict(kind='word',moves=word)],f'Random scramble, seed {seed}, {count} moves','recorded-scramble')
   if not apply:return preview
   # The HTTP worker holds the session lock across both stages. A native
   # scramble publishes only the committed snapshot, so its near-full-puzzle
   # support never overrides a filtered viewport through preview safety pins.
   self.s.m.check_cancel()
   return self.s.commit(preview['token'])
  except BaseException:
   # Restore earlier work only while the authoritative state is unchanged.
   # A late UI/status error after a durable commit must not revive a stale token.
   if self.s.head==previous_head and self.s.st.hash==previous_hash:self.s.pending=previous
   raise
 def save_macro(self,name):
  s=self.s
  if not s.pending:raise ValueError('Preview a word or a star before saving it')
  name=str(name).strip()
  if not name or len(name)>80:raise ValueError('Macro name must be 1..80 characters')
  lib=dict(s.prefs.get('macro_library',{}));c=s.pending['public']
  lib[name]=dict(recipe=s.pending['recipe'],effect_hash=c['source_to_destination_sha256'],primitive_count=c['primitive_count'],support=c['support'],model_id=s.m.model_id)
  return s.save_prefs({'macro_library':lib})
