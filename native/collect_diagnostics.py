"""Collect bounded startup/debug evidence, excluding puzzle histories and tokens."""
from pathlib import Path
import argparse,json,os,sys,zipfile
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from engine_process import redact

def text_tail(path,limit=4*1024*1024):
 with path.open('rb') as f:
  f.seek(0,2);f.seek(max(0,f.tell()-limit));return redact(f.read().decode('utf-8-sig','replace'))

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--data',type=Path);ap.add_argument('--out',type=Path);a=ap.parse_args()
 data=a.data or Path(os.getenv('LOCALAPPDATA',str(Path.home())))/'C600Studio'
 out=a.out or data/'C600-native-diagnostics.zip';out.parent.mkdir(parents=True,exist_ok=True)
 names=['engine.log','launch.lifecycle.json','launch.status.json','native_start_error.txt',
        'diagnostics/native-debug.log','diagnostics/native-debug.log.previous','diagnostics/build-info.json',
        'diagnostics/host-build.log','diagnostics/self-test-build.log','diagnostics/winforms-self-test.json',
        'diagnostics/winforms-self-test.log']
 names += ['diagnostics/renderer-test-build.log', 'diagnostics/native-renderer-test.json', 'diagnostics/performance-test-build.log', 'diagnostics/native-performance.json']
 count=0
 with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED) as z:
  for version in ('0.2.3','0.2.2','0.2.1'):
   build=data/('native-host-'+version)
   for name in names:
    p=build/name
    if p.is_file():z.writestr('native-host-'+version+'/'+name,text_tail(p));count+=1
   # Capture only non-secret facts from legacy readiness metadata. This helps
   # diagnose redirector PIDs without disclosing the local API capability.
   p=build/'launch.json'
   if p.is_file():
    try:
     value=json.loads(p.read_text(encoding='utf-8-sig'))
     safe={k:value[k] for k in ('pid','protocol','model_id') if k in value}
     z.writestr('native-host-'+version+'/launch-safe.json',json.dumps(safe,indent=2));count+=1
    except (OSError,ValueError):pass
  for folder in ('v022','v021'):
   p=ROOT/'tests'/folder
   if p.is_dir():
    for item in sorted(p.iterdir()):
     if item.suffix in ('.json','.log'):
      z.writestr('tests/'+folder+'/'+item.name,text_tail(item));count+=1
  z.writestr('CONTENTS.txt',
   'Startup/build/debug diagnostics only. No puzzle database, proof history, API token, launch secret or native geometry profile. '
   'Paths, OS/runtime versions and test-state hashes can appear. v021 files may be historical. Review before sharing.\n')
 print(str(out)+'\nIncluded '+str(count)+' diagnostic files.')
if __name__=='__main__':main()
