"""Strict native geometry handshake. Run against an export from the actual MPUlt process.
A model-derived synthetic fixture is useful for tests but is never runtime evidence.
"""
from __future__ import annotations
import json,hashlib,time
from pathlib import Path
import numpy as np
from core import digest,canonical

def permhash(src,dst):
 keep=src!=dst;s,d=src[keep],dst[keep];ix=np.argsort(s)
 return digest(s[ix].astype('<i4').tobytes()+d[ix].astype('<i4').tobytes())

def verify_native(model,data,directory):
 start=time.perf_counter();m=model;n=m.n
 if data.get('format')!='MPUlt-native600-v1' or data.get('n')!=n:raise ValueError('Native handshake requires the full 259,800-sticker puzzle')
 if len(data.get('faces',[]))!=600 or len(data.get('axes',[]))!=300 or len(data.get('bases',[]))!=1:raise ValueError('Unexpected native geometry counts')
 def cell(v):
  v=np.asarray(v,float)
  if v.shape!=(4,) or not np.isfinite(v).all():raise ValueError('Invalid native normal')
  dist=np.linalg.norm(m.normals-v,axis=1);q=np.argsort(dist)[:2]
  if dist[q[0]]>1e-3 or dist[q[1]]<.01:raise ValueError('Unmatched native cell normal')
  return int(q[0])
 host=np.full(n,-1,np.int32);face_map=np.full(600,-1,np.int32);signatures=[0]*n
 for f in data['faces']:
  first,count,idx=int(f['first']),int(f['count']),int(f['id']);c=cell(f['pole'])
  if count!=433 or not 0<=idx<600 or face_map[idx]!=-1 or not 0<=first<=n-count or np.any(host[first:first+count]>=0):raise ValueError('Invalid native cell partition')
  host[first:first+count]=c;face_map[idx]=c
 if np.any(host<0) or len(np.unique(face_map))!=600:raise ValueError('Incomplete cell partition')
 bases={int(b['id']):b for b in data['bases']};caps=[];seen=set()
 if len(bases)!=1 or sorted(int(a['id']) for a in data['axes'])!=list(range(300)):raise ValueError('Invalid native base/axis identifiers')
 for ax in data['axes']:
  b=bases[ax['base']]
  if ax['fixedMask']!=2 or len(b['cuts'])!=2 or not np.allclose(b['cuts'],[.968,-.968],atol=1e-12,rtol=0):raise ValueError('Native cuts or fixed mask differ')
  if len(ax['layers'])!=3:raise ValueError('Native layer count differs')
  for layer in (0,2):
   c=cell(np.asarray(ax['dir'])*(1 if layer==0 else -1));ss=np.asarray(ax['layers'][layer],np.int32)
   if c in seen or np.any(ss<0) or np.any(ss>=n) or len(np.unique(ss))!=len(ss):raise ValueError('Invalid native cap')
   seen.add(c);bit=1<<c
   for st in ss:signatures[int(st)]|=bit
   caps.append((ax,b,layer,c,ss))
 lookup={}
 for p in range(m.np):
  mask=sum(1<<c for c in m.caps(p))
  for h,s in zip(m.hosting(p),m.slots(p)):lookup[(mask,h)]=int(s)
 mapping=np.empty(n,np.int32)
 for s in range(n):
  key=(signatures[s],int(host[s]))
  if key not in lookup:raise ValueError(f'Native sticker {s} has no cap/host match')
  mapping[s]=lookup[key]
 if len(np.unique(mapping))!=n:raise ValueError('Native-to-lab map is not bijective')
 expected={permhash(*m.move(k)):k for k in range(1,1201)};found={};token_words={}
 for ax,b,layer,c,ss in caps:
  m.check_cancel();src=mapping[ss];native_maps=[];orders=[];matched={}
  for tw,t in enumerate(b['twists']):
   order=int(t['order']);pm=np.asarray(t['maps'][layer],np.int32)
   if order not in(2,3) or not np.array_equal(np.sort(pm),np.arange(len(ss))):raise ValueError('Invalid native rotation map')
   native_maps.append(pm);orders.append(order);q=np.arange(len(ss),dtype=np.int32)
   for angle in range(1,order):
    q=pm[q];key=permhash(src,mapping[ss[q]])
    if key in expected:
     k=expected[key]
     if (k-1)//2!=c:raise ValueError('Matched generator belongs to wrong cap')
     found[k]=dict(axis=ax['id'],twist=tw,angle=angle,mask=1<<layer);matched[k]=q.copy()
  h,t=2*c+1,2*c+2
  if h not in matched or t not in matched:raise ValueError(f'Cap {c} lacks both lab generators')
  gens=[(h,matched[h]),(t,matched[t]),(-t,np.argsort(matched[t]).astype(np.int32))]
  identity=np.arange(len(ss),dtype=np.int32);table={identity.tobytes():[]};queue=[identity]
  for p in queue:
   for letter,g in gens:
    q=g[p].astype(np.int32);key=q.tobytes()
    if key not in table:table[key]=table[p.tobytes()]+[letter];queue.append(q)
   if len(table)>12:raise ValueError('Unexpected local rotation group')
  if len(table)!=12:raise ValueError('Incomplete local rotation group')
  for tw,(pm,order) in enumerate(zip(native_maps,orders)):
   q=identity
   for angle in range(order):
    token_words[f"{ax['id']}:{tw}:{angle}:{1<<layer}"]=table[q.tobytes()];q=pm[q]
 if len(found)!=1200:raise ValueError(f'Only {len(found)}/1200 native generators matched')
 # Both outer caps are disjoint and may be turned together with mask 5.
 for ax in data['axes']:
  for tw,t in enumerate(bases[ax['base']]['twists']):
   for angle in range(int(t['order'])):
    a=f"{ax['id']}:{tw}:{angle}:";token_words[a+'5']=token_words[a+'1']+token_words[a+'4']
    token_words[a+'0']=[]
 out=Path(directory);out.mkdir(parents=True,exist_ok=True)
 profile=dict(format='C600-NATIVE-PROFILE-v1',model_id=m.model_id,native_to_lab=mapping.tolist(),native_face_to_lab=face_map.tolist(),translations=found,token_words=token_words,matched_stickers=n,matched_generators=len(found),native_executable_sha256=data.get('executable_sha256','unreported'),evidence='Full cap/host bijection and complete generator comparison against provided native export',seconds=time.perf_counter()-start)
 profile['profile_sha256']=digest(canonical(profile).encode());tmp=out/'native_profile.json.tmp';tmp.write_text(canonical(profile));tmp.replace(out/'native_profile.json')
 return profile
