// CPU-only fixtures for snapshot validation and compiled sticker member access.
// No native puzzle, DirectX device or visible window is created by this test.
using System;
using System.Collections.Generic;

internal static class NativeDataRegression {
 sealed class Mesh { internal int Id; }
 sealed class Sticker {
  public Mesh Base;public int Col;internal double FShr,SShr;internal int Calls;
  public void SetCoord(double fs,double ss){FShr=fs;SShr=ss;Calls++;}
 }
 sealed class MissingCoordSticker { public Mesh Base;public int Col; }
 sealed class MissingBaseSticker { public int Col; }
 sealed class PaletteOwner {public static int[] Colors;public int WhiteColor=-1;}
 static int checks;
 static void Require(bool value,string message){if(!value)throw new Exception(message);checks++;}
 static void Reject(Action action,string description){
  bool rejected=false;try{action();}catch{rejected=true;}
  Require(rejected,"Malformed input was accepted: "+description);
 }
 static Dictionary<string,object> Reply(byte[] colors,byte[] styles){
  return LocalApi.D("format","C600-native-snapshot-v1","profile_sha256","profile-fixture",
   "colors",Convert.ToBase64String(colors),"styles",Convert.ToBase64String(styles),
   "state",LocalApi.D("state_hash",new string('a',64),"prefs",LocalApi.D("orbit",33,"pin_safety",true),
    "progress",new object[0],"checkpoints",new object[0],"pending",null,"moving_solved",0,"moving_total",259800,"primitives",0,"head",0));
 }
 static void SnapshotTests(){
  var colors=new byte[519600];var styles=new byte[259800];
  colors[2]=87;colors[3]=2;colors[colors.Length-2]=87;colors[colors.Length-1]=2;
  styles[1]=6;styles[styles.Length-1]=6;
  var valid=Reply(colors,styles);var parsed=NativeSnapshot.Read(valid,"profile-fixture");
  Require(parsed.Colors.Length==519600&&parsed.Styles.Length==259800,"Valid full snapshot length mismatch");
  Require(parsed.Colors[2]==87&&parsed.Colors[3]==2&&parsed.Styles[1]==6,"Valid boundary color/style changed");
  Require(Object.ReferenceEquals(parsed.State,valid["state"]),"State metadata did not come from the same reply");
  Require(!Object.ReferenceEquals(parsed.Colors,colors)&&!Object.ReferenceEquals(parsed.Styles,styles),"Decoded arrays alias fixture inputs");
  Reject(delegate{NativeSnapshot.Read(valid,"different-profile");},"profile mismatch");
  var wrongFormat=Reply(colors,styles);wrongFormat["format"]="C600-native-snapshot-v999";
  Reject(delegate{NativeSnapshot.Read(wrongFormat,"profile-fixture");},"unknown snapshot format");
  var wrongColor=Reply(colors,styles);var invalidColors=(byte[])colors.Clone();invalidColors[colors.Length-2]=88;
  wrongColor["colors"]=Convert.ToBase64String(invalidColors);
  Reject(delegate{NativeSnapshot.Read(wrongColor,"profile-fixture");},"color 600 at the final slot");
  var wrongStyle=Reply(colors,styles);var invalidStyles=(byte[])styles.Clone();invalidStyles[styles.Length-1]=7;
  wrongStyle["styles"]=Convert.ToBase64String(invalidStyles);
  Reject(delegate{NativeSnapshot.Read(wrongStyle,"profile-fixture");},"style 7 at the final slot");
  Reject(delegate{NativeSnapshot.Read(Reply(new byte[519599],styles),"profile-fixture");},"truncated colors");
  Reject(delegate{NativeSnapshot.Read(Reply(colors,new byte[259799]),"profile-fixture");},"truncated styles");
  var badBase64=Reply(colors,styles);badBase64["colors"]="this is not base64!";
  Reject(delegate{NativeSnapshot.Read(badBase64,"profile-fixture");},"invalid base64");
  var nullState=Reply(colors,styles);nullState["state"]=null;
  Reject(delegate{NativeSnapshot.Read(nullState,"profile-fixture");},"null atomic state");
  var badState=Reply(colors,styles);badState["state"]=new object[0];
  Reject(delegate{NativeSnapshot.Read(badState,"profile-fixture");},"incorrect state type");
  var missing=Reply(colors,styles);missing.Remove("styles");
  Reject(delegate{NativeSnapshot.Read(missing,"profile-fixture");},"missing styles");
  Require(colors[colors.Length-2]==87&&styles[styles.Length-1]==6,"Validation mutated source bytes");
 }
 static Dictionary<string,object> V2(string mode,string revision,int[] indices,byte[] colors,byte[] styles,byte[] interactive){
  var reply=Reply(colors,styles);reply["format"]="C600-native-snapshot-v2";reply["mode"]=mode;reply["revision"]=revision;
  reply["revisions"]=LocalApi.D("state","state-"+revision,"color","color-"+revision,"visibility","visibility-"+revision,"interaction","interaction-"+revision,"annotation","annotation-"+revision);
  reply["interactive"]=Convert.ToBase64String(interactive);
  if(mode=="delta"){
   var packed=new byte[indices.Length*4];for(int i=0;i<indices.Length;i++){uint n=(uint)indices[i];packed[i*4]=(byte)n;packed[i*4+1]=(byte)(n>>8);packed[i*4+2]=(byte)(n>>16);packed[i*4+3]=(byte)(n>>24);}
   reply["indices"]=Convert.ToBase64String(packed);reply["base_revision"]="r1";
  }
  return reply;
 }
 static void DeltaTests(){
  var colors=new byte[519600];var styles=new byte[259800];var interactive=new byte[259800];styles[10]=2;interactive[10]=1;
  var first=NativeSnapshot.Read(V2("full","r1",null,colors,styles,interactive),"profile-fixture");
  Require(first.IsFull&&first.ChangedSlots==null&&first.Revision=="r1"&&first.Revisions.Count==5,"Full v2 metadata missing");
  var delta=V2("delta","r2",new[]{10,259799},new byte[]{87,2,0,0},new byte[]{6,1},new byte[]{1,0});
  var next=NativeSnapshot.Read(delta,"profile-fixture",first);
  Require(!next.IsFull&&next.BaseRevision=="r1"&&next.Revision=="r2"&&next.ChangedSlots.Length==2,"Delta identity mismatch");
  Require(next.Colors[20]==87&&next.Colors[21]==2&&next.Styles[259799]==1&&next.Interactive[10]==1,"Sparse bytes mapped to wrong slots");
  Require(next.ColorsChanged&&next.StylesChanged&&next.VisibilityChanged&&!next.InteractionChanged,"Delta flags do not describe actual changes");
  Require(next.ColorSlots.Length==1&&next.ColorSlots[0]==10&&next.StyleSlots.Length==2&&next.InteractionSlots.Length==0,"Per-attribute indices include unchanged data");
  Require(first.Colors[20]==0&&first.Styles[10]==2&&first.Styles[259799]==0,"Delta mutated its predecessor");
  Require(Object.ReferenceEquals(next.Interactive,first.Interactive)&&!Object.ReferenceEquals(next.Colors,first.Colors),"Delta copy-on-write arrays wrong");
  Require(Object.ReferenceEquals(next.State,delta["state"]),"Delta lost its atomic state object");
  var empty=V2("delta","r3",new int[0],new byte[0],new byte[0],new byte[0]);empty["base_revision"]="r2";
  var metadata=NativeSnapshot.Read(empty,"profile-fixture",next);
  Require(metadata.ChangedSlots.Length==0&&!metadata.ColorsChanged&&!metadata.StylesChanged&&!metadata.InteractionChanged,"Empty delta was not metadata-only");
  Require(Object.ReferenceEquals(metadata.Colors,next.Colors)&&Object.ReferenceEquals(metadata.Styles,next.Styles)&&Object.ReferenceEquals(metadata.Interactive,next.Interactive),"Empty delta copied large arrays");
  var colorOnly=V2("delta","r4",new[]{10},new byte[]{1,0},new byte[]{2},new byte[]{1});
  var colorChange=NativeSnapshot.Read(colorOnly,"profile-fixture",first);
  Require(colorChange.ColorsChanged&&!colorChange.StylesChanged&&!colorChange.VisibilityChanged&&!colorChange.InteractionChanged,"Color-only update invalidates visibility");
  var tint=V2("delta","r4",new[]{10},new byte[]{0,0},new byte[]{6},new byte[]{1});
  Require(!NativeSnapshot.Read(tint,"profile-fixture",first).VisibilityChanged,"Visible style change rebuilds geometry");
  Reject(delegate{NativeSnapshot.Read(delta,"profile-fixture");},"delta without predecessor");
  Reject(delegate{NativeSnapshot.Read(delta,"profile-fixture",next);},"stale delta base");
  foreach(int[] malformed in new[]{new[]{10,10},new[]{11,10},new[]{259800},new[]{-1}}){
   var bad=V2("delta","r2",malformed,new byte[malformed.Length*2],new byte[malformed.Length],new byte[malformed.Length]);
   Reject(delegate{NativeSnapshot.Read(bad,"profile-fixture",first);},"duplicate, unsorted or invalid indices");
  }
  var badLength=V2("delta","r2",new[]{10},new byte[1],new byte[1],new byte[1]);
  Reject(delegate{NativeSnapshot.Read(badLength,"profile-fixture",first);},"delta value length mismatch");
  var badPacked=V2("delta","r2",new int[0],new byte[0],new byte[0],new byte[0]);badPacked["indices"]=Convert.ToBase64String(new byte[3]);
  Reject(delegate{NativeSnapshot.Read(badPacked,"profile-fixture",first);},"partial uint32 index");
  var hidden=V2("delta","r2",new[]{10},new byte[2],new byte[]{0},new byte[]{1});
  Reject(delegate{NativeSnapshot.Read(hidden,"profile-fixture",first);},"hidden interactive slot");
  var badInteractive=V2("delta","r2",new[]{10},new byte[2],new byte[]{2},new byte[]{2});
  Reject(delegate{NativeSnapshot.Read(badInteractive,"profile-fixture",first);},"interaction byte outside boolean range");
  var badRevision=V2("full","r1",null,colors,styles,interactive);badRevision["revisions"]=LocalApi.D("state","s");
  Reject(delegate{NativeSnapshot.Read(badRevision,"profile-fixture");},"missing attribute revisions");
  var invalidAtEnd=V2("delta","r2",new[]{10,259799},new byte[]{1,0,88,2},new byte[]{2,2},new byte[]{1,1});
  Reject(delegate{NativeSnapshot.Read(invalidAtEnd,"profile-fixture",first);},"invalid last union value");
  Require(first.Colors[20]==0&&first.Styles[259799]==0,"Rejected delta partially published earlier valid values");
 }
 static void ColorTests(){
  PaletteOwner.Colors=new int[600];for(int i=0;i<600;i++)PaletteOwner.Colors[i]=i*65537;
  var owner=new PaletteOwner();var stickers=new[]{new Sticker(),new Sticker(),new Sticker(),new Sticker()};var access=new NativeStickerAccess(stickers);
  var field=new short[]{1,2,3,599};var styles=new byte[]{2,6,2,0};
  access.ApplyColors(owner,field,styles,null,false);
  Require(stickers[0].Col==unchecked((int)0xFF010001)&&stickers[1].Col==unchecked((int)0xFFFFE866),"Native palette or style override math changed");
  field[0]=4;int before=stickers[2].Col;access.ApplyColors(owner,field,styles,new[]{0},false);
  Require(stickers[0].Col==unchecked((int)0xFF040004)&&stickers[2].Col==before,"Sparse color updates touched unrelated native meshes");
  stickers[1].Col=0;access.ApplyColors(owner,field,styles,new int[0],true);
  Require(stickers[1].Col==unchecked((int)0xFFFFE866),"Native ShowCube style reset was not restored");
  owner.WhiteColor=4;access.ApplyColors(owner,field,styles,new int[0],false);
  Require(stickers[0].Col==-1&&stickers[2].Col==unchecked((int)0x80010001),"WhiteColor change did not refresh exact original dim/white colors");
  var mesh=stickers[0].Base;Reject(delegate{access.ApplyColors(owner,field,styles,new[]{0,0},false);},"duplicate mesh indices");
  Require(Object.ReferenceEquals(mesh,stickers[0].Base)&&stickers[0].Calls==0,"Color application changed geometry");
 }
 static void AccessTests(){
  var original=new Mesh{Id=1};var replacement=new Mesh{Id=2};
  var sticker=new Sticker{Base=original};var access=new NativeStickerAccess(new[]{sticker});
  Require(access.Slots.Length==1&&Object.ReferenceEquals(access.Slots[0],sticker),"Sticker reference was copied or changed");
  Require(Object.ReferenceEquals(access.GetBase(sticker),original),"Compiled mesh getter mismatch");
  access.SetBase(sticker,replacement);
  Require(Object.ReferenceEquals(sticker.Base,replacement)&&original.Id==1,"Compiled mesh setter changed the original mesh");
  access.SetColor(sticker,unchecked((int)0xFFABCDEF));
  Require(unchecked((uint)sticker.Col)==0xFFABCDEFu,"Compiled setter did not preserve signed ARGB bits");
  access.SetCoord(sticker,0.321,0.654);
  Require(sticker.Calls==1&&sticker.FShr==0.321&&sticker.SShr==0.654,"Compiled coordinate call changed argument precision or order");
  Reject(delegate{access.SetBase(sticker,new object());},"incompatible renderer mesh type");
  Require(Object.ReferenceEquals(sticker.Base,replacement),"Failed mesh assignment changed the current mesh");
  var missing=new MissingCoordSticker{Base=original,Col=1};var noCoord=new NativeStickerAccess(new[]{missing});
  Reject(delegate{noCoord.SetCoord(missing,0.3,0.6);},"missing coordinate API");
  Reject(delegate{new NativeStickerAccess(new[]{new MissingBaseSticker{Col=1}});},"missing Base API");
 }
 [STAThread] static int Main(){try{
  SnapshotTests();DeltaTests();AccessTests();ColorTests();Console.WriteLine("PASS: "+checks+" CPU fixture snapshot/member-access checks; no DirectX device created");return 0;
 }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
