"""Verify a Studio certificate or exported session without the graphical client.
Certificate mode independently expands the recipe and replays its primitives;
it does not use the cached star permutation to establish the claimed effect.
The mechanics model is shared. This is not native MPUlt geometry verification.
"""
from __future__ import annotations
import argparse,gzip,json,sys,time
from pathlib import Path
import numpy as np
from core import Model,PuzzleState,canonical,digest,state_hash

HASH_FIELDS=['format','model_id','recipe','source_to_destination_sha256','primitive_count','star_count','support','pre_state','post_state','evidence','native_windows_equivalence']
def verify_certificate(m,data):
 if data.get('format')!='C600-STUDIO-CERTIFICATE-v1' or data.get('model_id')!=m.model_id:raise ValueError('Certificate format/model mismatch')
 word=list(m.expand(data['recipe']))
 if word!=data['expanded_lab_word']:raise ValueError('Expanded witness does not match the recipe')
 s,d=m.word_net(word)
 if s.tolist()!=data['source'] or d.tolist()!=data['destination']:raise ValueError('Full permutation differs from primitive witness')
 if digest(s.astype('<i4').tobytes()+d.astype('<i4').tobytes())!=data['source_to_destination_sha256']:raise ValueError('Permutation checksum mismatch')
 if len(word)!=int(data['primitive_count']) or m.support(s)!=data['support']:raise ValueError('Length/support claim mismatch')
 if data['star_count']!=sum(x['kind']=='star' for x in data['recipe']):raise ValueError('Star count mismatch')
 if data.get('native_windows_equivalence') is not False:raise ValueError('Native equivalence is outside this verifier scope')
 if digest(canonical({k:data[k] for k in HASH_FIELDS}).encode())!=data['certificate_id']:raise ValueError('Certificate identifier mismatch')
 return dict(passed=True,scope='Full permutation independently recomputed from the primitive witness using the retained model',primitive_count=len(word),moved_stickers=len(s),state_pre_post_checked=False,native_windows_equivalence=False)
def verify_session(m,data,expand=False,limit=2000000):
 if data.get('format')!='C600-STUDIO-SESSION-v1' or data.get('model_id')!=m.model_id:raise ValueError('Session format/model mismatch')
 a=m.ids.copy();total=0;stars=0
 for index,ev in enumerate(data['events']):
  if state_hash(a)!=ev['pre']:raise ValueError(f'Pre-state mismatch at event {index}')
  _,length=m.normalize(ev['recipe']);total+=length
  if expand:
   if total>limit:raise ValueError('Primitive verification budget exceeded. Use compact mode or raise --limit deliberately.')
   s,d=m.word_net(list(m.expand(ev['recipe'])))
  else:s,d,_,_=m.net(ev['recipe'])
  a[d]=a[s]
  if state_hash(a)!=ev['post'] or length!=int(ev['primitive_count']):raise ValueError(f'Event mismatch at {index}')
  stars+=sum(x['kind']=='star' for x in ev['recipe'])
 if state_hash(a)!=data['final_state']:raise ValueError('Final hash mismatch')
 PuzzleState(m,a)
 return dict(passed=True,scope='primitive-expanded replay' if expand else 'compact recipe replay with full collateral',transactions=len(data['events']),stars=stars,primitive_count=str(total),final_state=state_hash(a),solved=bool(np.array_equal(a,m.ids)),native_windows_equivalence=False)
def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('file',type=Path);ap.add_argument('--expand',action='store_true');ap.add_argument('--limit',type=int,default=2000000);args=ap.parse_args();t=time.perf_counter()
 op=gzip.open if args.file.suffix=='.gz' else open
 with op(args.file,'rt',encoding='utf-8') as f:data=json.load(f)
 m=Model();r=verify_certificate(m,data) if data.get('format')=='C600-STUDIO-CERTIFICATE-v1' else verify_session(m,data,args.expand,args.limit);r['seconds']=time.perf_counter()-t;print(json.dumps(r,indent=2))
if __name__=='__main__':
 try:main()
 except Exception as e:print(f'Verification failed: {e}',file=sys.stderr);raise SystemExit(1)
