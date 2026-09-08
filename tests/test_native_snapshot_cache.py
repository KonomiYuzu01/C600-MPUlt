"""Immutable profile-cache and atomic full/delta tests; no native/GPU claims."""
from pathlib import Path
import argparse,base64,hashlib,json,sys
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import Model,canonical,digest
from session import Session
from server import NativeSnapshotCache

def arrays(reply,previous=None):
 values=[np.frombuffer(base64.b64decode(reply[name]),dtype) for name,dtype in [('colors','<u2'),('styles','u1'),('interactive','u1')]]
 if reply.get('mode')!='delta':return [v.copy() for v in values]
 assert previous is not None and reply['base_revision']==previous[0]
 ids=np.frombuffer(base64.b64decode(reply['indices']),'<u4');assert np.all(ids[1:]>ids[:-1])
 out=[v.copy() for v in previous[1]]
 for target,value in zip(out,values):assert len(value)==len(ids);target[ids]=value
 return out

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--work-dir',type=Path,required=True);ap.add_argument('--verified-native-profile',type=Path);args=ap.parse_args()
 work=args.work_dir;work.mkdir(parents=True,exist_ok=False);m=Model();s=Session(m,work/'session');checks=[]
 profile=json.loads(args.verified_native_profile.read_text(encoding='utf-8')) if args.verified_native_profile else dict(native_to_lab=list(range(m.n-1,-1,-1)),native_face_to_lab=list(range(599,-1,-1)),profile_sha256='a'*64)
 mapping=np.array(profile['native_to_lab'],np.int32);inverse=np.argsort(profile['native_face_to_lab']);cache=NativeSnapshotCache()
 def match(values):
  expected=[inverse[s.st.labels[mapping]//433],s.render_styles()[mapping],s.interactive_styles()[mapping]!=0]
  for a,b in zip(values,expected):assert np.array_equal(a,b)
 try:
  s.save_prefs(dict(rules=[dict(expr='active',style='solid')],pin_safety=False));initial=cache.read(s,profile);current=arrays(initial);match(current)
  assert cache.mapping.nbytes+cache.inverse.nbytes==1041600 and not cache.mapping.flags.writeable and not cache.inverse.flags.writeable
  assert cache.mapping.flags.owndata and cache.inverse.flags.owndata
  checks.append('Full snapshot matches an independent native slot/palette oracle; owned immutable maps retain1,041,600bytes')
  identity=(id(cache.mapping),id(cache.inverse));empty=cache.read(s,profile,initial['revision'])
  assert (id(cache.mapping),id(cache.inverse))==identity and empty['mode']=='delta' and base64.b64decode(empty['indices'])==b''
  match(arrays(empty,(initial['revision'],current)));checks.append('Warm and empty-delta requests reuse the same immutable mappings')
  pending=s.preview([dict(kind='word',moves=[1])]);s.commit(pending['token']);changed=cache.read(s,profile,empty['revision']);materialized=arrays(changed,(empty['revision'],current));match(materialized)
  assert changed['mode']=='delta' and changed['state']['state_hash']==s.st.hash
  old=current[0].copy();again=cache.read(s,profile);match(arrays(again));assert np.array_equal(old,current[0])
  checks.append('Legal committed turn produces exact full/delta colors, styles and interaction without modifying its predecessor')
  v1=cache.read(s,profile,protocol=1);match(arrays(v1));assert v1['format']=='C600-native-snapshot-v1';checks.append('Legacy v1 full snapshot remains palette-compatible')
  replacement=dict(profile,native_to_lab=list(reversed(profile['native_to_lab'])),native_face_to_lab=list(reversed(profile['native_face_to_lab'])),profile_sha256='b'*64)
  mapping=np.array(replacement['native_to_lab'],np.int32);inverse=np.argsort(replacement['native_face_to_lab']);reset=cache.read(s,replacement,changed['revision'])
  assert reset['mode']=='full' and len(cache.entries)==1 and (id(cache.mapping),id(cache.inverse))!=identity
  match(arrays(reset));checks.append('Changed verified profile replaces both maps and invalidates all old delta bases')
  labels=s.st.labels.tobytes();head=s.head;s.close();s=Session(m,work/'session');assert s.head==head and s.st.labels.tobytes()==labels
  match(arrays(cache.read(s,replacement)));checks.append('All259800 labelled slots and exact remapped full snapshot survive SQLite reopen')
 finally:s.close()
 report=dict(passed=True,checks=[dict(name=n,passed=True) for n in checks],profile_source='retained verified native profile' if args.verified_native_profile else 'synthetic profile boundary fixture',labelled_slots=m.n,mapping_cache_bytes=1041600,source_sha256={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in ['core.py','session.py','server.py','tests/test_native_snapshot_cache.py']})
 (work/'report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report,indent=2))
if __name__=='__main__':main()
