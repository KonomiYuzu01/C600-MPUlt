from pathlib import Path
import sys,time,tempfile,json,platform,resource
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from core import Model,Filters,PuzzleState
from session import Session
m=Model();s=Session(m,Path(tempfile.mkdtemp()));rows={}
def bench(name,fn,n=30):
 values=[]
 for _ in range(n):t=time.perf_counter();fn();values.append(1000*(time.perf_counter()-t))
 rows[name]={'p50_ms':float(np.median(values)),'p95_ms':float(np.percentile(values,95)),'max_ms':max(values),'samples':n}
bench('status_all_35_orbits',s.status)
bench('filter_active_orbit',s.render_styles)
f=Filters(s.st,orbit=27);bench('filter_compound',lambda:f.parse('(O26 | O27) & (unsolved | home(C013))'))
preview=s.preview([{'kind':'star','orbit':15,'node':100,'sign':1}]);rows['cold_O15_preview']={'ms':preview['preview_ms'],'primitive_count':preview['primitive_count']}
bench('warm_O15_preview',lambda:s.preview([{'kind':'star','orbit':15,'node':100,'sign':1}]),10)
bench('single_primitive_preview_commit',lambda:s.commit(s.preview([{'kind':'word','moves':[2]}])['token']),20)
bench('checkpoint_atomic_sqlite',lambda:s.checkpoint('benchmark'),10)
a=m.ids.copy();src,dst=m.star_net(15,100);bench('warm_full_macro_net_update',lambda:a.__setitem__(dst,a[src]),100)
result=dict(scope='Linux container CPU timings, independent of GPU or Windows',python=platform.python_version(),load_seconds=m.load_seconds,peak_rss_mb=resource.getrusage(resource.RUSAGE_SELF).ru_maxrss/1024,measurements=rows)
(Path(__file__).parent/'backend_benchmark.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2));s.close()
