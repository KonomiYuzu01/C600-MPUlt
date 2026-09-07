"""Full-state mechanics, witnessed macros, guarded planning and safe filter parsing.
All permutations are source -> destination. Words execute from left to right.
No visibility information is ever used to update the puzzle state.
"""
from __future__ import annotations
import gzip,hashlib,json,re,time
from collections import OrderedDict
from pathlib import Path
from typing import Any
import numpy as np

ROOT=Path(__file__).resolve().parent
ORDER=[6,0,17,15,2,22,21,8,23,9,29,25,13,11,10,34,28,26,24,16,14,12,3,33,32,31,30,27,20,19,18,7,5,4,1]

def canonical(obj: Any)->str:
 return json.dumps(obj,sort_keys=True,separators=(',',':'),allow_nan=False)
def digest(data: bytes)->str:return hashlib.sha256(data).hexdigest()
def state_hash(a:np.ndarray)->str:return digest(np.asarray(a,dtype='<i4').tobytes())
def invword(w):return [-int(x) for x in reversed(w)]
def invrecipe(r):
 out=[]
 for step in reversed(r):
  if step['kind']=='word':out.append({'kind':'word','moves':invword(step['moves'])})
  else:out.append(dict(step,sign=-step['sign']))
 return out

def cp(a,b):return tuple(b[a[i]] for i in range(len(a)))
def ip(a):
 r=[0]*len(a)
 for i,j in enumerate(a):r[j]=i
 return tuple(r)
def commp(a,b):return cp(cp(cp(a,b),ip(a)),ip(b))

class Model:
 def __init__(self,assets:Path|None=None,verify_assets:bool=True):
  start=time.perf_counter();self.root=assets or ROOT/'assets'
  self.manifest=json.loads((self.root/'manifest.json').read_text())
  if verify_assets:
   for name,h in self.manifest['files'].items():
    p=ROOT/name if assets is None else self.root/Path(name).name
    if digest(p.read_bytes())!=h:raise ValueError('Asset hash mismatch: '+name)
  self.model_id=self.manifest['model_id'];self.z=np.load(self.root/'model.npz',allow_pickle=False)
  self.sp=self.z['slot_piece'];self.oid=self.z['orbit_id'];self.n=len(self.sp);self.np=len(self.oid)
  self.ids=np.arange(self.n,dtype=np.int32);self.pids=np.arange(self.np,dtype=np.int32)
  self.so=self.oid[self.sp];self.fo=self.z['face_offsets'];self.faces=self.z['face_values'];self.mo=self.z['mask_offsets'];self.masks=self.z['mask_values']
  self.psorted=np.argsort(self.sp,kind='stable').astype(np.int32)
  self.first=self.psorted[self.fo[:-1]];self.k=np.diff(self.fo);self.rank=np.diff(self.mo)-1
  self.normals=self.z['normals'];self.cell_frames=self.z['frames']
  zz=np.load(self.root/'primitives.npz',allow_pickle=False);self.src=zz['src'];self.dst=zz['dst'];self.offset=zz['offsets']
  self.inv_moves={};self.atlas=json.loads((self.root/'seed_atlas.json').read_text());self.census=json.loads((self.root/'census.json').read_text())
  with gzip.open(self.root/'execution_trees.json.gz','rt') as f:self.trees=json.load(f)
  self.buffers=json.loads((self.root/'buffer_certificates.json').read_text());self.native=json.loads((self.root/'lab_to_native.json').read_text())
  self.bypos=[];self.base={};self.certified_seeds={};self.star_cache=OrderedDict();self._word_cache={}
  for t in self.trees:
   d={}
   for n,p in enumerate(t['positions']):d.setdefault(p,[]).append(n)
   self.bypos.append(d)
  self.home_cell_cache={};self.cancel_event=None;self.cap_cell_cache={};self.shell_cache={}
  self.adj=[np.argsort(np.linalg.norm(self.normals-self.normals[c],axis=1))[1:5].tolist() for c in range(600)]
  self.load_seconds=time.perf_counter()-start
 def check_cancel(self):
  if self.cancel_event is not None and self.cancel_event.is_set():raise InterruptedError('Analysis cancelled; puzzle state unchanged')
 def slots(self,p:int)->np.ndarray:
  if not 0<=p<self.np:raise ValueError('Piece position is out of range')
  return self.psorted[self.fo[p]:self.fo[p+1]]
 def hosting(self,p):return self.faces[self.fo[p]:self.fo[p+1]].tolist()
 def caps(self,p):return self.masks[self.mo[p]:self.mo[p+1]].tolist()
 def move(self,m:int):
  if type(m)!=int or not 1<=abs(m)<=1200:raise ValueError('Primitive ID must be an integer in ±1..±1200')
  i=abs(m)-1;a,b=self.offset[i:i+2];s,d=self.src[a:b],self.dst[a:b]
  if m>0:return s,d
  if m not in self.inv_moves:
   ix=np.argsort(d);self.inv_moves[m]=(d[ix],s[ix])
  return self.inv_moves[m]
 def word_net(self,w):
  a=self.ids.copy()
  for i,m in enumerate(w):
   if i%128==0:self.check_cancel()
   s,d=self.move(m);a[d]=a[s]
  d=np.flatnonzero(a!=self.ids).astype(np.int32);s=a[d];ix=np.argsort(s)
  return s[ix],d[ix]
 def mapped(self,m,a):
  s,d=self.move(m)
  if len(s)==0:return a.copy()
  ix=np.searchsorted(s,a);valid=ix<len(s);safe=np.minimum(ix,len(s)-1);valid &= s[safe]==a
  out=a.copy();out[valid]=d[ix[valid]];return out
 def path(self,o,n):
  t=self.trees[o];out=[]
  if type(n)!=int or not 0<=n<len(t['parent']):raise ValueError('Frame node out of range')
  while t['parent'][n]>=0:out.append(t['move'][n]);n=t['parent'][n]
  return out[::-1]
 def certify_seed(self,o):
  if o in self.base:return self.certified_seeds[o]
  if type(o)!=int or not 0<=o<35:raise ValueError('Orbit out of range')
  t=self.trees[o];start=time.perf_counter();s,d=self.word_net(t['seed'])
  if t['seed']!=self.atlas[o]['word']:raise ValueError('Seed definition mismatch')
  target=self.so[s]==o;sp=self.sp[s[target]];dp=self.sp[d[target]]
  if len(s[target])!=3*self.census['orbits'][o]['colors'] or len(np.unique(sp))!=3:raise ValueError('Target seed is not a three-piece cycle')
  pm={int(a):int(b) for a,b in zip(sp,dp)}
  if any(pm[pm[pm[x]]]!=x or pm[x]==x for x in pm):raise ValueError('Bad target piece transport')
  actual=sorted(set(map(int,self.so[s]))-{o})
  if actual!=sorted(self.atlas[o]['collateral']):raise ValueError('Collateral orbit mismatch')
  # Relocating a source/destination permutation conjugates both endpoints.
  for m in t['relocation']:s=self.mapped(m,s);d=self.mapped(m,d)
  fa,fb,fc=[np.asarray(x,np.int32) for x in self.atlas[o]['frames']]
  ix=np.argsort(s);s,d=s[ix],d[ix]
  for a,b in [(fa,fb),(fb,fc),(fc,fa)]:
   q=np.searchsorted(s,a)
   if not np.array_equal(s[q],a) or not np.array_equal(d[q],b):raise ValueError('Relocated frame mismatch')
  self.base[o]=(s,d)
  c=dict(orbit=o,scope='complete full-model permutation',legal_seed_replayed=True,seed_length=len(t['seed']),seed_sha256=digest(canonical(t['seed']).encode()),full_support=len(s),collateral=actual,seconds=time.perf_counter()-start)
  self.certified_seeds[o]=c;return c
 def star_net(self,o,n,sign=1):
  if type(o)!=int or not 0<=o<35 or sign not in (-1,1):raise ValueError('Invalid star')
  key=(o,n)
  if key in self.star_cache:
   s,d=self.star_cache.pop(key);self.star_cache[key]=(s,d)
  else:
   self.certify_seed(o);s,d=self.base[o];t=self.trees[o]
   guard=set(self.caps(t['buffers'][0])+self.caps(t['buffers'][1]))
   for m in self.path(o,n):
    if (abs(m)-1)//2 in guard:raise ValueError('Setup moves a guarded buffer position')
    s=self.mapped(m,s);d=self.mapped(m,d)
   ix=np.argsort(s);s,d=s[ix],d[ix]
   fa,fb=[np.asarray(f,np.int32) for f in self.atlas[o]['frames'][:2]];fx=np.asarray(t['frames'][n],np.int32)
   for a,b in ((fa,fb),(fb,fx),(fx,fa)):
    q=np.searchsorted(s,a)
    if np.any(q>=len(s)) or not np.array_equal(s[q],a) or not np.array_equal(d[q],b):raise ValueError('Transported star frame mismatch')
   self.star_cache[key]=(s,d)
   # Bounded memory even after hundreds of thousands of stars.
   while len(self.star_cache)>96:self.star_cache.popitem(last=False)
  return (s,d) if sign>0 else (d,s)
 def normalize(self,recipe):
  if not isinstance(recipe,list) or not 1<=len(recipe)<=256:raise ValueError('Recipe must contain 1..256 steps')
  out=[];length=0
  for x in recipe:
   if not isinstance(x,dict):raise ValueError('Bad recipe step')
   if x.get('kind')=='word':
    w=x.get('moves')
    if not isinstance(w,list) or not 1<=len(w)<=100000:raise ValueError('Word length must be 1..100000')
    for m in w:self.move(m)
    out.append({'kind':'word','moves':w});length+=len(w)
   elif x.get('kind')=='star':
    o=x.get('orbit');n=x.get('node');sg=x.get('sign',1)
    if type(o)!=int or not 0<=o<35 or type(sg)!=int or sg not in(-1,1):raise ValueError('Bad star fields')
    path=self.path(o,n);out.append({'kind':'star','orbit':o,'node':n,'sign':sg});length+=len(self.trees[o]['seed'])+2*len(self.trees[o]['relocation'])+2*len(path)
   else:raise ValueError('Only word and star steps are accepted')
  if length>3000000:raise ValueError('Preview expansion exceeds 3,000,000 primitives; split the batch')
  return out,length
 def net(self,recipe):
  recipe,length=self.normalize(recipe);a=self.ids.copy()
  for x in recipe:
   self.check_cancel()
   if x['kind']=='word':
    for i,m in enumerate(x['moves']):
     if i%128==0:self.check_cancel()
     s,d=self.move(m);a[d]=a[s]
   else:s,d=self.star_net(x['orbit'],x['node'],x['sign']);a[d]=a[s]
  d=np.flatnonzero(a!=self.ids).astype(np.int32);s=a[d];ix=np.argsort(s)
  return s[ix],d[ix],length,recipe
 def expand(self,recipe):
  recipe,_=self.normalize(recipe)
  for x in recipe:
   if x['kind']=='word':yield from x['moves'];continue
   o=x['orbit'];t=self.trees[o];p=self.path(o,x['node']);w=invword(p)+invword(t['relocation'])+t['seed']+t['relocation']+p
   yield from (w if x['sign']>0 else invword(w))
 def support(self,s):
  return [dict(orbit=int(o),stickers=int(np.count_nonzero(self.so[s]==o)),pieces=int(len(np.unique(self.sp[s[self.so[s]==o]])))) for o in np.unique(self.so[s])]
 def cap_mask(self,c):
  if not 0<=c<600:raise ValueError('Cell must be 0..599')
  if c not in self.cap_cell_cache:
   self.cap_cell_cache[c]=np.bincount(np.repeat(self.pids,np.diff(self.mo)),weights=(self.masks==c),minlength=self.np)>0
  return self.cap_cell_cache[c]
 def shell_mask(self,c,distance):
  if not 0<=c<600 or not 0<=distance<=30:raise ValueError('Shell expects a cell 0..599 and depth 0..30')
  key=(c,distance)
  if key not in self.shell_cache:
   current={c};seen={c}
   for _ in range(distance):
    current={q for x in current for q in self.adj[x]}-seen;seen|=current
   result=np.zeros(self.np,bool)
   for x in current:result|=self.cell_mask(x)
   if len(self.shell_cache)>=64:self.shell_cache.pop(next(iter(self.shell_cache)))
   self.shell_cache[key]=result
  return self.shell_cache[key]
 def cell_mask(self,c):
  if not 0<=c<600:raise ValueError('Cell must be 0..599')
  if c not in self.home_cell_cache:
   a=np.zeros(self.np,bool);a[np.unique(self.sp[c*433:(c+1)*433])]=True;self.home_cell_cache[c]=a
  return self.home_cell_cache[c]

class PuzzleState:
 def __init__(self,m,labels=None,*,trusted=False):
  self.m=m;self.labels=m.ids.copy() if labels is None else np.asarray(labels,np.int32).copy();self.refresh(validate=labels is not None and not trusted)
 def refresh(self,validate=False):
  m=self.m
  if self.labels.shape!=(m.n,):raise ValueError('Wrong sticker count')
  if validate and not np.array_equal(np.sort(self.labels),m.ids):raise ValueError('Sticker labels are not bijective')
  self.at=m.sp[self.labels[m.first]];self.where=np.empty(m.np,np.int32);self.where[self.at]=m.pids
  if validate:
   if not np.array_equal(m.sp[self.labels],self.at[m.sp]):raise ValueError('Physical piece split')
   if len(np.unique(self.at))!=m.np or not np.array_equal(m.oid[self.at],m.oid):raise ValueError('Piece or orbit identity error')
  self.position_correct=self.at==m.pids;bad=self.labels!=m.ids;self.correct=np.ones(m.np,bool);self.correct[m.sp[bad]]=False
  self.color_correct=np.ones(m.np,bool);self.color_correct[m.sp[self.labels//433!=m.ids//433]]=False
  self.orientation_wrong=self.position_correct & ~self.correct;self.hash=state_hash(self.labels);self._progress=None
 def apply(self,s,d):self.labels[d]=self.labels[s];self.refresh()
 def progress(self):
  if self._progress is not None:return self._progress
  m=self.m;moving=m.oid>=0;o=m.oid[moving]
  counts=np.bincount(o,minlength=35);done=np.bincount(o,weights=self.correct[moving],minlength=35)
  pos=np.bincount(o,weights=~self.position_correct[moving],minlength=35)
  ori=np.bincount(o,weights=self.orientation_wrong[moving],minlength=35)
  color=np.bincount(o,weights=self.color_correct[moving],minlength=35)
  self._progress=[dict(orbit=i,pieces=int(counts[i]),solved=int(done[i]),position_wrong=int(pos[i]),orientation_wrong=int(ori[i]),color_solved=int(color[i]),rank=r['rank'],stickers=r['colors'],orientation=r['orientation_group']) for i,r in enumerate(m.census['orbits'])]
  return self._progress
 def piece(self,p):
  if type(p)!=int or not 0<=p<self.m.np:raise ValueError('Position must be an integer in 0..'+str(self.m.np-1))
  m=self.m;s=m.slots(p);occupant=int(self.at[p]);return dict(position=p,piece=occupant,orbit=int(m.oid[p]),home_cells=m.hosting(occupant),current_cells=m.hosting(p),cap_cells=m.caps(p),slots=s.tolist(),labels=self.labels[s].tolist(),sticker_home_cells=(self.labels[s]//433).tolist(),position_correct=bool(self.position_correct[p]),solved=bool(self.correct[p]),orientation_wrong=bool(self.orientation_wrong[p]))

class Planner:
 def __init__(self,m):self.m=m
 def validate_orbit(self,o):
  if type(o)!=int or not 0<=o<35:raise ValueError('Orbit must be an integer in 0..34')
 def validate_target(self,o,target):
  self.validate_orbit(o)
  if type(target)!=int or not 0<=target<self.m.np:raise ValueError('Destination must be a valid integer piece position')
  if self.m.oid[target]!=o:raise ValueError('Destination must belong to the selected orbit')
  if target in self.m.trees[o]['buffers']:raise ValueError('Destination is a fixed buffer; choose a nonbuffer position')
 @staticmethod
 def star(o,n,sign=1):return dict(kind='star',orbit=o,node=int(n),sign=sign)
 def next(self,st,o,target=None):
  self.validate_orbit(o)
  if target is not None:
   self.validate_target(o,target)
   if st.correct[target]:raise ValueError('The selected destination is already solved; choose an unfinished position')
  m=self.m;t=m.trees[o];A,B=t['buffers'];frames=t['frames'];by=m.bypos[o];positions=np.flatnonzero(m.oid==o)
  candidates=[int(x) for x in positions if x not in(A,B) and not st.position_correct[x]]
  if candidates:
   if target is not None and target not in candidates:raise ValueError('Finish displaced positions before choosing an orientation-only destination')
   x=target if target is not None else candidates[0];y=int(st.where[x]);nx=by[x][0]
   if y==A:cmd=[self.star(o,nx,-1)]
   elif y==B:cmd=[self.star(o,nx)]
   else:cmd=[self.star(o,by[y][0],-1),self.star(o,nx)]
   return cmd,dict(phase='position insertion',target=x,source=y,explanation=f'Move the piece whose home is P{x} from P{y} into P{x}. Other finished nonbuffer positions in this orbit are preserved.')
  if not np.all(st.position_correct[positions]):raise ValueError('Odd buffer swap or invalid labels: no legal parity patch is assumed')
  k=m.census['orbits'][o]['colors']
  if k>1:
   candidates=[int(x) for x in positions if x not in(A,B) and st.orientation_wrong[x]]
   if candidates:
    x=target if target is not None else candidates[0];n0=by[x][0];f0=np.array(frames[n0]);n=next((i for i in by[x] if np.array_equal(st.labels[frames[i]],f0)),None)
    if n is None:raise ValueError('Orientation is outside the transported legal frame group')
    return [self.star(o,n),self.star(o,n0,-1)],dict(phase='orientation transfer',target=x,explanation='Correct this stationary piece and transfer the inverse orientation effect to buffer B.')
   fa,fb,fc=[np.array(f,np.int32) for f in m.atlas[o]['frames']]
   if st.orientation_wrong[A]:
    ix={int(s):i for i,s in enumerate(fa)};desired=fc[[ix[int(s)] for s in st.labels[fa]]].tolist();n=next(i for i in by[t['third']] if frames[i]==desired)
    return [self.star(o,0),self.star(o,n),self.star(o,0)],dict(phase='buffer A orientation',target=A,explanation='Three-star orientation correction. Full collateral is retained.')
   if st.orientation_wrong[B]:
    ix={int(s):i for i,s in enumerate(fb)};targetq=tuple(ix[int(s)] for s in st.labels[fb]);X=t['third'];Y=next(int(x) for x in positions if x not in(A,B,X))
    def omap(x):
     f=frames[by[x][0]];di={s:i for i,s in enumerate(f)};return {tuple(di[s] for s in frames[n]):n for n in by[x]}
    gx,gy=omap(X),omap(Y);pair=next(((q,r) for q in gx for r in gy if commp(q,r)==targetq),None)
    if pair is None:raise ValueError('Residual orientation violates this orbit\'s abelian invariant')
    q,r=pair;p=[self.star(o,gx[q]),self.star(o,by[X][0],-1)];v=[self.star(o,gy[r]),self.star(o,by[Y][0],-1)]
    return p+v+invrecipe(p)+invrecipe(v),dict(phase='final-buffer commutator',target=B,explanation='Eight stars correct the nonabelian final-buffer residual.')
  return [],dict(phase='complete',target=None,explanation='All labelled stickers in this orbit are solved.')
 def buffer_info(self,st,o,target=None):
  self.validate_orbit(o)
  m=self.m;t=m.trees[o];a,b=t['buffers'];rows=[]
  selection='explicit'
  if target is None:
   unfinished=[int(p) for p in np.flatnonzero((m.oid==o)&~st.correct) if p not in(a,b)]
   displaced=[p for p in unfinished if not st.position_correct[p]]
   target=(displaced or unfinished or [int(t['third'])])[0]
   selection='automatic-unfinished' if unfinished else 'automatic-reference'
  else:self.validate_target(o,target)
  for n in m.bypos[o][target]:rows.append(dict(node=n,depth=t['depth'][n],length=len(t['seed'])+2*len(t['relocation'])+2*t['depth'][n],slots=t['frames'][n],setup=self.m.path(o,n)))
  return dict(orbit=o,A=st.piece(a),B=st.piece(b),third=t['third'],target=target,target_selection=selection,target_piece=st.piece(target),guard_cells=sorted(set(m.caps(a)+m.caps(b))),reachable=len(t['positions']),expected=(m.census['orbits'][o]['pieces']-2)*m.census['orbits'][o]['orientation_order'],max_depth=max(t['depth']),mean_depth=float(np.mean(t['depth'])),candidates=rows,arbitrary_buffer_relocation_implemented=False)

class Filters:
 """Safe recursive-descent Boolean expressions. First matching style rule wins."""
 token=re.compile(r'\s*(>=|<=|!=|[()&|!<>=,]|[A-Za-z_][A-Za-z_0-9]*|[0-9]+)')
 def __init__(self,st,orbit=33,selected=None,protected=None,preview=None,sets=None):
  self.st=st;self.m=st.m;self.orbit=orbit;self.selected=selected;self.protected=protected or [];self.preview=preview or [];self.sets=sets or {}
 def parse(self,text):
  if not isinstance(text,str) or len(text)>2048:raise ValueError('Filter exceeds 2048 characters')
  self.ts=[];pos=0
  while pos<len(text.rstrip()):
   x=self.token.match(text,pos)
   if not x:raise ValueError('Invalid filter character near '+text[pos:pos+20])
   self.ts.append(x[1]);pos=x.end()
  if len(self.ts)>512:raise ValueError('Too many filter tokens')
  self.i=0;self.nesting=0;result=self._or()
  if self.i!=len(self.ts):raise ValueError('Unexpected filter token: '+self.ts[self.i])
  return result
 def peek(self):return self.ts[self.i] if self.i<len(self.ts) else ''
 def pop(self):
  a=self.peek()
  if not a:raise ValueError('Incomplete filter')
  self.i+=1;return a
 def _or(self):
  a=self._and()
  while self.peek().lower() in ('|','or') and self.peek():self.pop();a=a|self._and()
  return a
 def _and(self):
  a=self._unary()
  while self.peek().lower() in ('&','and') and self.peek():self.pop();a=a&self._unary()
  return a
 def _unary(self):
  self.nesting+=1
  if self.nesting>40:raise ValueError('Filter nesting too deep')
  if self.peek().lower() in('!','not'):self.pop();a=~self._unary()
  elif self.peek()=='(':
   self.pop();a=self._or()
   if self.pop()!=')':raise ValueError('Missing closing parenthesis')
  else:a=self._atom()
  self.nesting-=1;return a
 def _atom(self):
  s=self.pop().lower();m=self.m;st=self.st
  if s.startswith('o') and s[1:].isdigit():return m.oid==int(s[1:])
  if s in('rank','stickers','orbit'):
   op=self.pop();v=self.pop().lower();v=int(v[1:] if v.startswith('o') else v);a={'rank':m.rank,'stickers':m.k,'orbit':m.oid}[s]
   ops={'=':np.equal,'!=':np.not_equal,'<':np.less,'>':np.greater,'<=':np.less_equal,'>=':np.greater_equal}
   if op not in ops:raise ValueError('Expected comparison')
   return ops[op](a,v)
  if s=='set':
   if self.pop()!='(':raise ValueError('Expected (')
   name=self.pop()
   if self.pop()!=')' or name not in self.sets:raise ValueError('Unknown named set: '+name)
   item=self.sets[name]
   return np.isin(st.at if item.get('kind')=='identity' else m.pids,item['ids'])
  if s in ('cap','shell'):
   if self.pop()!='(':raise ValueError('Expected (')
   x=self.pop().lower();c=int(x[1:] if x.startswith('c') else x)
   if s=='shell':
    if self.pop()!=',':raise ValueError('shell(cell, depth) needs a comma')
    depth=int(self.pop())
   if self.pop()!=')':raise ValueError('Expected )')
   return m.cap_mask(c) if s=='cap' else m.shell_mask(c,depth)
  if s in('home','current','home_has','current_has','piece','position'):
   if self.pop()!='(':raise ValueError('Expected (')
   value=self.pop().lower();n=int(value[1:] if value.startswith(('c','p')) else value)
   if self.pop()!=')':raise ValueError('Expected )')
   if s in('home','home_has'):return m.cell_mask(n)[st.at]
   if s in('current','current_has'):return m.cell_mask(n)
   return (st.at if s=='piece' else m.pids)==n
  if s in ('all','everything'):return np.ones(m.np,bool)
  if s in ('none','nothing'):return np.zeros(m.np,bool)
  if s=='active':return m.oid==self.orbit
  if s=='unsolved':return ~st.correct
  if s=='solved':return st.correct
  if s=='position_correct':return st.position_correct
  if s=='position_wrong':return ~st.position_correct
  if s=='orientation_wrong':return st.orientation_wrong
  if s=='color_wrong':return ~st.color_correct
  if s=='selected':return st.at==self.selected if self.selected is not None else np.zeros(m.np,bool)
  if s=='buffer':return np.isin(m.pids,m.trees[self.orbit]['buffers'])
  if s=='protected':return np.isin(m.oid,self.protected)
  if s in('preview','preview_support'):return np.isin(m.pids,self.preview)
  raise ValueError('Unknown filter predicate: '+s)
 def styles(self,rules,pin=True):
  if not isinstance(rules,list) or len(rules)>32:raise ValueError('At most 32 rules')
  out=np.zeros(self.m.np,np.uint8);done=np.zeros(self.m.np,bool)
  codes={'hide':0,'ghost':1,'solid':2,'highlight':3}
  for r in rules:
   a=self.parse(r['expr'])&~done;done|=a
   if r['style'] not in codes:raise ValueError('Unknown style')
   out[a]=codes[r['style']]
  if pin:
   out[self.parse('buffer')]=4
   if self.preview:out[self.parse('preview')]=5
   if self.selected is not None:out[self.parse('selected')]=6
  return out[self.m.sp]
