"""All 11 nonidentity rotations of the focused tetrahedral cap, expressed as legal H/T words."""
import numpy as np

def grips(model,cell):
 c=int(cell)
 if not 0<=c<600:raise ValueError('Cell must be 0..599')
 h,t=2*c+1,2*c+2;rot=model.z['rotperms'];generators=[(h,rot[h-1]),(t,rot[t-1]),(-t,np.argsort(rot[t-1]))]
 identity=np.arange(600,dtype=np.int32);table={identity.tobytes():[]};queue=[identity];items=[]
 for perm in queue:
  word=table[perm.tobytes()]
  if word:
   order=2 if np.array_equal(perm[perm],identity) else 3
   R=np.linalg.lstsq(model.normals,model.normals[perm],rcond=None)[0].T
   u,sv,vh=np.linalg.svd(R-np.eye(4));a=vh[0];b=vh[1];angle=float(np.arctan2(b@R@a,a@R@a))
   items.append(dict(id=len(items),word=word,order=order,label=('H' if order==2 else 'T')+str(len(items)+1),u=a.tolist(),v=b.tolist(),angle=angle,cell=c))
  for letter,g in generators:
   q=g[perm].astype(np.int32);key=q.tobytes()
   if key not in table:table[key]=word+[letter];queue.append(q)
 if len(table)!=12:raise ValueError('Unexpected cap rotation closure')
 # Group inverse pairs into seven visible axes, each with forward/inverse controls.
 axes=[];used=set()
 for i,it in enumerate(items):
  p=queue[i+1]
  if i in used:continue
  j=next(j for j,q in enumerate(queue[1:]) if np.array_equal(q[p],identity))
  used|={i,j};it=dict(it);it['label']=('H' if it['order']==2 else 'T')+str(1+sum(x['order']==it['order'] for x in axes));it['inverse']=items[j];axes.append(it)
 return dict(cell=c,axes=axes,all_rotations=len(items),basis='World-coordinate cap rotations from the retained normal permutation tables')
