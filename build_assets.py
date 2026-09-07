"""Rebuild release assets from the retained reference packages. Build only needs SciPy.
Usage: python build_assets.py REFERENCE_PACKAGE ORIGINAL_TOOLKIT
Only load base_stickers.npz from the trusted retained toolkit: it has legacy object arrays.
The application itself never unpickles user uploads.
"""
from pathlib import Path
import sys,json,hashlib,shutil,time
import numpy as np
from scipy.spatial import ConvexHull
ROOT=Path(__file__).resolve().parent
ref=Path(sys.argv[1]);old=Path(sys.argv[2]);out=ROOT/'assets';out.mkdir(exist_ok=True)
sys.path.insert(0,str(ref/'engine'))
from move_engine import Engine
start=time.perf_counter();e=Engine()
srcs=[];dsts=[];offs=[0]
for m in range(1,1201):
 p=e.move(m);src=np.array(sorted(p),np.int32);dst=np.array([p[int(s)] for s in src],np.int32)
 srcs.append(src);dsts.append(dst);offs.append(offs[-1]+len(src))
np.savez_compressed(out/'primitives.npz',src=np.concatenate(srcs),dst=np.concatenate(dsts),offsets=np.array(offs,np.int32))
shutil.copy2(ref/'engine'/'model.npz',out/'model.npz')
for f in ['execution_trees.json.gz','seed_atlas.json','census.json','lab_to_native.json','buffer_certificates.json','puzzle_definition.txt']:
 shutil.copy2(ref/'algorithms'/f,out/f)
for f in ['move_engine.py']:
 shutil.copy2(ref/'engine'/f,ROOT/'oracle'/f)
# Mesh numbering must coincide with cell*433 + base region index.
z=np.load(old/'base_stickers.npz',allow_pickle=True);ini=np.load(old/'initial.npz');B=ini['basis'];n0=e.N[0];cent=z['centers'];F=e.F
errors=[]
for c in range(600):
 errors.append(float(np.max(np.abs(cent@F[c].T-e.z['slot_centers'][c*433:(c+1)*433]))))
assert max(errors)<1e-6,max(errors)
verts=[];offsets=[0];localids=[]
for i,region in enumerate(z['regions']):
 v=np.asarray(region,dtype=float);h=ConvexHull(v)
 for tri in h.simplices:
  pts=n0+v[tri]@B.T
  verts.extend(pts.tolist());localids.extend([i]*3)
 offsets.append(len(verts))
# Plain data arrays are safe to consume in WebGL and never require pickle.
np.asarray(verts,'<f4').tofile(out/'mesh_vertices.f32')
np.asarray(localids,'<u4').tofile(out/'mesh_sticker.u32')
np.asarray(cent,'<f4').tofile(out/'mesh_centers.f32')
np.asarray(F.transpose(0,2,1),'<f4').tofile(out/'cell_frames.f32')
np.asarray(e.sp,'<u4').tofile(out/'slot_piece.u32')
np.asarray(e.N,'<f4').tofile(out/'cell_normals.f32')
mesh=dict(base_vertices=len(verts),base_stickers=433,cells=600,slots=e.n,offsets=offsets,normal=n0.tolist(),normal_length=float(np.linalg.norm(n0)),max_numbering_error=max(errors),full_triangles=len(verts)//3*600)
(out/'mesh.json').write_text(json.dumps(mesh,separators=(',',':')))
# Test scenes are real legal scrambles; compact reference trace remains available.
with __import__('gzip').open(ref/'logs'/'triangular_solution_0.json.gz','rt') as f:record=json.load(f)
(out/'demo_scramble.json').write_text(json.dumps(record['scramble']))
shutil.copy2(ref/'logs'/'triangular_solution_0.json.gz',ROOT/'tests'/'reference_solve.json.gz')
files={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(out.iterdir()) if p.is_file()}
manifest=dict(format='C600-STUDIO-ASSETS-v1',puzzle='600-cell-Full',geometry_scope='retained ideal-golden-ratio model; native Windows equivalence untested',files=files,mesh=mesh)
manifest['model_id']=hashlib.sha256(json.dumps(files,sort_keys=True,separators=(',',':')).encode()).hexdigest()
(out/'manifest.json').write_text(json.dumps(manifest,indent=2))
print(json.dumps(dict(seconds=time.perf_counter()-start,primitives_entries=offs[-1],mesh=mesh,model_id=manifest['model_id']),indent=2))
