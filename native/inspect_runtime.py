"""Minimal read-only CLI metadata inspection of the exact supplied MPUlt assembly.
Validates the reflection surface; does not load/execute the assembly or validate DX.
"""
from pathlib import Path
import struct,hashlib,json
REQUIRED={
 '_3dedit.Form1':(['Puz','CubeView','dxControl2','panel1','splitter1','panel3','splitter2','menuStrip1','qSolved','m_TRun'],['InitStatus']),
 '_3dedit.Puzzle':(['Str','Field','Seq','Ptr','LSeq','LShuffle','NTwists'],[]),
 '_3dedit.PuzzleStructure':(['QSimplified','NStickers','Faces','BaseAxes','Axes'],[]),
 '_3dedit.PBaseAxis':(['Id','Cut','Twists','FixedMask'],[]),
 '_3dedit.PAxis':(['Id','Base','Dir','Layers'],[]),
 '_3dedit.PBaseTwist':(['Order','Map'],[]),
 '_3dedit.PFace':(['Id','Pole','FirstSticker','Base'],[]),
 '_3dedit.PBaseFace':(['NStickers'],[]),
 '_3dedit.CubeObj':(['Stks','ShowRank','FShr','SShr'],['SetStickerColors','SetStickerSize']),
 '_3dedit.StkMesh':(['Base','Col'],['SetCoord']),
 '_3dedit.PMesh':(['Rank','MinBDim','NV','NF','NE'],[]),
 '_3dedit.DXControl':(['m_DDeviceX','m_hidl'],['SetSceneChanged','get_Scene','ForceUpdate']),
 '_3dedit.DxBase':(['m_pd3dDevice'],['EnvironmentResized','get_IsReady','set_Resize']),
}
def inspect(path):
 b=Path(path).read_bytes();u16=lambda p:struct.unpack_from('<H',b,p)[0];u32=lambda p:struct.unpack_from('<I',b,p)[0]
 pe=u32(0x3c)
 if b[pe:pe+4]!=b'PE\0\0':raise ValueError('Not a PE assembly')
 nsec=u16(pe+6);op=pe+24;size=u16(pe+20);sections=[]
 for i in range(nsec):
  s=op+size+i*40;sections.append((u32(s+12),max(u32(s+8),u32(s+16)),u32(s+20)))
 def offset(rva):
  for base,length,file in sections:
   if base<=rva<base+length:return file+rva-base
  raise ValueError('PE RVA outside sections')
 dd=op+(96 if u16(op)==0x10b else 112);cli=offset(u32(dd+14*8));md=offset(u32(cli+8));p=md+16+u32(md+12);p=(p+3)&~3;streams=u16(p+2);p+=4;st={}
 for _ in range(streams):
  off,sz=u32(p),u32(p+4);end=b.index(0,p+8);name=b[p+8:end].decode('ascii');st[name]=(md+off,sz);p=(end+4)&~3
 t=st.get('#~',st.get('#-'))[0];ss=st['#Strings'][0];heap=b[t+6];valid=struct.unpack_from('<Q',b,t+8)[0];p=t+24;rows={}
 for i in range(64):
  if valid>>i&1:rows[i]=u32(p);p+=4
 si=4 if heap&1 else 2;gi=4 if heap&2 else 2;bi=4 if heap&4 else 2
 ix=lambda tb:4 if rows.get(tb,0)>=65536 else 2
 coded=lambda tabs,bits:4 if max(rows.get(x,0) for x in tabs)>=(1<<(16-bits)) else 2
 sizes={0:2+si+gi*3,1:coded([0,26,35,1],2)+si*2,2:4+si*2+coded([2,1,27],2)+ix(4)+ix(6),3:ix(4),4:2+si+bi,5:ix(6),6:8+si+bi+ix(8)}
 starts={}
 for i in range(7):starts[i]=p;p+=rows.get(i,0)*sizes[i]
 def readi(p,size):return u32(p) if size==4 else u16(p)
 def text(i):q=ss+i;return b[q:b.index(0,q)].decode('utf-8')
 types=[]
 for i in range(rows.get(2,0)):
  q=starts[2]+i*sizes[2];name=text(readi(q+4,si));namespace=text(readi(q+4+si,si));q+=4+2*si+coded([2,1,27],2);fl=readi(q,ix(4));ml=readi(q+ix(4),ix(6));types.append((namespace+'.'+name,fl,ml))
 members={}
 for i,(name,fl,ml) in enumerate(types):
  fe=types[i+1][1] if i+1<len(types) else rows.get(4,0)+1;me=types[i+1][2] if i+1<len(types) else rows.get(6,0)+1
  fields=[text(readi(starts[4]+(j-1)*sizes[4]+2,si)) for j in range(fl,fe)]
  methods=[text(readi(starts[6]+(j-1)*sizes[6]+8,si)) for j in range(ml,me)];members[name]=(fields,methods)
 missing=[]
 for typ,(fields,methods) in REQUIRED.items():
  a,z=members.get(typ,([],[]));missing.extend(typ+'.'+x for x in fields if x not in a);missing.extend(typ+'.'+x+'()' for x in methods if x not in z)
 return dict(sha256=hashlib.sha256(b).hexdigest(),required_types=len(REQUIRED),missing=missing,reflection_names_present=not missing,scope='Read-only CLI metadata names. Not a compile, load, geometry or Windows test')
if __name__=='__main__':
 import argparse
 ap=argparse.ArgumentParser();ap.add_argument('runtime');ap.add_argument('--report');x=ap.parse_args();r=inspect(x.runtime);print(json.dumps(r,indent=2))
 if x.report:Path(x.report).write_text(json.dumps(r,indent=2))
 raise SystemExit(1 if r['missing'] else 0)
