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
  var wrongFormat=Reply(colors,styles);wrongFormat["format"]="C600-native-snapshot-v2";
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
  SnapshotTests();AccessTests();Console.WriteLine("PASS: "+checks+" CPU fixture snapshot/member-access checks; no DirectX device created");return 0;
 }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
