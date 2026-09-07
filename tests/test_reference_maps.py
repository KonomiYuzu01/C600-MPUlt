"""Compare packed app maps against the retained independent map constructor.
The mathematical geometry data is shared; this is not native MPUlt verification.
"""
from pathlib import Path
import sys,json,time
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT));sys.path.insert(0,str(ROOT/'oracle'))
import move_engine
from core import Model
move_engine.ROOT=ROOT/'assets'
t=time.perf_counter();m=Model();e=move_engine.Engine()
for i in range(1,1201):
 p=e.move(i);s,d=m.move(i)
 assert p==dict(zip(s.tolist(),d.tolist())),i
 e.cache.clear()
r={'passed':True,'generators_compared':1200,'scope':'All packed full sticker maps equal the separately implemented retained engine constructor. Shared model geometry; no native Windows equivalence claim.','seconds':time.perf_counter()-t}
(ROOT/'tests/reference_map_report.json').write_text(json.dumps(r,indent=2));print(json.dumps(r,indent=2))
