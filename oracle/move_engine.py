"""Sparse sticker-permutation engine. Words are executed left to right."""
import numpy as np,json,time
from pathlib import Path
from collections import Counter,defaultdict
ROOT=Path(__file__).resolve().parent
class Engine:
 def __init__(self):
  z=np.load(ROOT/'model.npz',allow_pickle=False)
  mv=z['mask_values'];mo=z['mask_offsets'];fv=z['face_values'];fo=z['face_offsets']
  self.z=z; self.N=z['normals'];self.F=z['frames'];self.rot=z['rotperms'];self.masks=[tuple(map(int,mv[a:b])) for a,b in zip(mo[:-1],mo[1:])];self.faces=[list(map(int,fv[a:b])) for a,b in zip(fo[:-1],fo[1:])];self.sp=z['slot_piece'];self.oid=z['orbit_id'];self.so=self.oid[self.sp];self.n=len(self.sp)
  self.ps=[[] for _ in self.masks]
  for s,p in enumerate(self.sp):self.ps[p].append(s)
  self.lookup=[dict(zip(f,s)) for f,s in zip(self.faces,self.ps)]
  self.cache={};self.arrays={};self.rawsrc=z['move_src'].astype(object);self.rawdst=z['move_dst'].astype(object)
 def move(self,m):
  """Signed one-based id: +(2*cell+type+1), negative inverse."""
  m=int(m)
  if not 1<=abs(m)<=1200:raise ValueError('Move ID must be in -1200..-1 or 1..1200')
  if m in self.cache:return self.cache[m]
  if m<0:
   p=self.move(-m);q={b:a for a,b in p.items()}
  else:
   i=m-1;r=self.rot[i];q={}
   for a,b in zip(self.rawsrc[i],self.rawdst[i]):
    for f,s in zip(self.faces[a],self.ps[a]):
     dst=self.lookup[b][int(r[f])]
     if dst!=s:q[s]=dst
  self.cache[m]=q;return q
 def word(self,w):
  state=np.arange(self.n,dtype=np.int32)
  for m in w:
   if m not in self.arrays:
    p=self.move(m);self.arrays[m]=(np.fromiter(p.keys(),dtype=np.int32),np.fromiter(p.values(),dtype=np.int32))
   src,dst=self.arrays[m];state[dst]=state[src]
  d=np.flatnonzero(state!=np.arange(self.n))
  return dict(zip(state[d].tolist(),d.tolist()))
 def signature(self,p):
  out={}
  affected=defaultdict(set)
  for s in p:affected[int(self.so[s])].add(int(self.sp[s]))
  for o,pp in affected.items():
   # Get physical piece permutation, with in-place twists counted separately.
   perm={a:int(self.sp[p.get(self.ps[a][0],self.ps[a][0])]) for a in pp}
   cyc=[];seen=set()
   for a in pp:
    if a in seen:continue
    x=a;k=0
    while x not in seen:seen.add(x);k+=1;x=perm[x]
    cyc.append(k)
   out[o]=dict(pieces=len(pp),stickers=sum(int(self.so[s])==o for s in p),cycles=dict(Counter(cyc)))
  return out

def compose(p,q):
 """Chronological composition: execute p, then q. Sparse dicts omit fixes."""
 out={}
 for a,b in p.items():
  c=q.get(b,b)
  if a!=c:out[a]=c
 for a,b in q.items():
  if a not in p:out[a]=b
 return out

def inverse(p):return {b:a for a,b in p.items()}
def invword(w):return [-m for m in w[::-1]]
def comm(a,b):return compose(compose(compose(a,b),inverse(a)),inverse(b))
def cycles(p):
 seen=set();out=[]
 for a in sorted(p):
  if a in seen:continue
  x=a;cyc=[]
  while x not in seen:
   seen.add(x);cyc.append(x);x=p[x]
  out.append(cyc)
 return out
if __name__=='__main__':
 import argparse
 a=argparse.ArgumentParser();a.add_argument('moves',type=int,nargs='*');args=a.parse_args()
 e=Engine();p=e.word(args.moves);print(json.dumps(e.signature(p),indent=2));print('moved stickers',len(p))
