"""Apply narrowly scoped renderer fixes to an inspected ivan216/MPUlt checkout.
Pinned source blob, no network, backup preserved. Build the checkout in Visual Studio
as x86 afterward. The host's renderer-only hidden clones work without this patch.
"""
from pathlib import Path
import argparse,hashlib
EXPECTED='3ff0ad4c9262471e0b7e7ec0e7f1443ef9036881'
def git_blob(raw):return hashlib.sha1(b'blob '+str(len(raw)).encode()+b'\0'+raw).hexdigest()
def patch(root):
 p=Path(root)/'src'/'MeshObj.cs';raw=p.read_bytes();normalized=raw.replace(b'\r\n',b'\n')
 if git_blob(normalized)!=EXPECTED and git_blob(raw)!=EXPECTED:raise ValueError('Source differs from the inspected commit. Do not patch blindly. Expected MeshObj.cs blob '+EXPECTED)
 s=normalized.decode('utf-8-sig')
 edits=[('if(v.Y>=y) yl=true;','if(v.Y<=y) yl=true;'),
 ('''            for(int ns=0;ns<NStk;ns++) {
                if(Stks[ns].ExtraTwist==null && BPln[Stks[ns].NFace]) continue;
                int l0=Stks[ns].L2D();''','''            for(int ns=0;ns<NStk;ns++) {
                int m=1<<Math.Min(11,Stks[ns].Base.Rank);
                if((m&ShowRank)==0) continue; // C600: reject before vertex projection
                if(Stks[ns].ExtraTwist==null && BPln[Stks[ns].NFace]) continue;
                int l0=Stks[ns].L2D();'''),
 ('''                Stks[ns].RecalcCoord((S4Camera)(d3dDevice.Camera));
                int m=1<<Math.Min(11,Stks[ns].Base.Rank);
                if((m&ShowRank)==0) continue;''','''                Stks[ns].RecalcCoord((S4Camera)(d3dDevice.Camera));'''),
 ('''                    if((m&ShowRank)==0 && Stks[ns].Base.MinBDim!=0) continue;
                    ptr=Stks[ns].SetBuffer1D''','''                    if((m&ShowRank)==0 && Stks[ns].Base.MinBDim!=0) continue;
                    if((m&ShowRank)==0) Stks[ns].RecalcCoord((S4Camera)(d3dDevice.Camera));
                    ptr=Stks[ns].SetBuffer1D''')]
 for old,new in edits:
  if s.count(old)!=1:raise ValueError('Patch anchor missing or ambiguous; no file written')
  s=s.replace(old,new)
 backup=p.with_suffix('.cs.pre-c600')
 if backup.exists():raise FileExistsError('Backup exists; no file written')
 backup.write_bytes(raw);p.write_text(s,encoding='utf-8-sig',newline='\n');return p
if __name__=='__main__':
 ap=argparse.ArgumentParser();ap.add_argument('checkout');a=ap.parse_args();print(patch(a.checkout))
