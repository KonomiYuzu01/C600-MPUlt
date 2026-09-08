// CPU/WinForms adapter contracts only; actual DirectX performance is separate.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
internal sealed class FullRendererFixtureViewport : Panel {
 internal ArrayList DXObjects {get;private set;}
 internal FullRendererFixtureViewport(object cube){DXObjects=new ArrayList{cube};}
}
internal static class NativeFullRendererRegression {
 sealed class DrawGuard : IDisposable {internal static int Entries,Exits;internal DrawGuard(){Entries++;}public void Dispose(){Exits++;}}
 static readonly List<object> checks=new List<object>();
 static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 static void Check(string name,Action action){action();checks.Add(LocalApi.D("name",name,"passed",true));Console.WriteLine("PASS "+name);}
 static void CheckColors(Type type){
  object cube=FormatterServices.GetUninitializedObject(type);Type puzzleType=Reflect.Field(type,"Cube").FieldType;object puzzle=FormatterServices.GetUninitializedObject(puzzleType);
  Type stickerType=Reflect.Field(type,"Stks").FieldType.GetElementType();Array stickers=Array.CreateInstance(stickerType,600);
  short[] field=new short[600];byte[] styles=new byte[600];int[] palette=new int[600];
  for(int i=0;i<600;i++){stickers.SetValue(FormatterServices.GetUninitializedObject(stickerType),i);field[i]=unchecked((short)(i|((i%4)<<14)));styles[i]=2;palette[i]=unchecked((int)(0xAB000000u+(uint)(i*2796203)));}
  Reflect.Set(cube,"Cube",puzzle);Reflect.Set(cube,"Stks",stickers);Reflect.Set(cube,"NStk",600);Reflect.Set(puzzle,"Field",field);
  var colorsField=Reflect.Field(type,"Colors");object originalPalette=colorsField.GetValue(null);var col=Reflect.Field(stickerType,"Col");var access=new NativeStickerAccess(stickers);
  try{
   colorsField.SetValue(null,palette);
   foreach(int white in new[]{-1,0,1,599,600}){
    Reflect.Set(cube,"WhiteColor",white);type.GetMethod("SetStickerColors",Reflect.Flags).Invoke(cube,null);
    var expected=new int[600];for(int i=0;i<600;i++){expected[i]=(int)col.GetValue(stickers.GetValue(i));col.SetValue(stickers.GetValue(i),0);}
    access.ApplyColors(cube,field,styles,null,false);
    for(int i=0;i<600;i++)Assert(expected[i]==(int)col.GetValue(stickers.GetValue(i)),"Selective palette math differs from original MPUlt at slot "+i+", WhiteColor "+white);
   }
   Reflect.Set(cube,"WhiteColor",-1);access.ApplyColors(cube,field,styles,null,false);
   field[17]=599;field[299]=0;access.ApplyColors(cube,field,styles,new[]{17,299},false);
   var actual=new int[600];for(int i=0;i<600;i++)actual[i]=(int)col.GetValue(stickers.GetValue(i));
   type.GetMethod("SetStickerColors",Reflect.Flags).Invoke(cube,null);
   for(int i=0;i<600;i++)Assert(actual[i]==(int)col.GetValue(stickers.GetValue(i)),"Sparse mesh colors differ from full native color recomputation");
   Assert(Object.ReferenceEquals(field,Reflect.Get(puzzle,"Field"))&&Object.ReferenceEquals(stickers,Reflect.Get(cube,"Stks")),"Selective color update changed authoritative array ownership");
  }finally{colorsField.SetValue(null,originalPalette);}
 }
 [STAThread] static int Main(string[] args){
  try{
   string runtime=Path.GetFullPath(args[0]);AppDomain.CurrentDomain.AssemblyResolve+=delegate(object s,ResolveEventArgs e){string path=Path.Combine(runtime,new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
   var type=Assembly.LoadFrom(Path.Combine(runtime,"MPUlt.exe")).GetType("_3dedit.CubeObj",true);object cube=FormatterServices.GetUninitializedObject(type);
   Check("pinned-original-render-and-upload-instructions",delegate{NativeFullRenderer.Validate(type,type.GetMethod("Render",Reflect.Flags),type.GetMethod("SendVBuf",Reflect.Flags));});
   Check("selective-colors-match-original-all-600-colors-outline-bits-and-white-modes",delegate{CheckColors(type);});
   Check("commit-hold-blocks-explicit-draw-and-picking-before-device-access",delegate{var lifecycle=(NativeRendererLifecycle)FormatterServices.GetUninitializedObject(typeof(NativeRendererLifecycle));lifecycle.HoldUpdates=true;Assert(!lifecycle.RenderFrame()&&!lifecycle.PreparePicking(),"Held renderer accessed or drew uncommitted state");});
   using(var viewport=new FullRendererFixtureViewport(cube)){
    var adapter=NativeFullRenderer.TryInstall(viewport,cube);Assert(adapter!=null,"Pinned native renderer adapter installation failed");var proxy=viewport.DXObjects[0];
    Check("adapter-installed-with-original-cube-retained",delegate{Assert(!Object.ReferenceEquals(proxy,cube)&&Object.ReferenceEquals(proxy.GetType().GetField("Target").GetValue(proxy),cube),"Adapter replaced native geometry ownership");Assert(adapter.Enabled,"Acceleration should start enabled");});
    Check("native-lighting-contract-forwarded",delegate{Assert(Object.Equals(type.GetMethod("get_NeedLightning").Invoke(cube,null),proxy.GetType().GetMethod("get_NeedLightning").Invoke(proxy,null)),"Lighting contract changed");});
    Check("replacement-and-original-IL-can-execute-with-render-policy",delegate{adapter.RenderScope=delegate{return new DrawGuard();};foreach(bool enabled in new[]{true,false}){adapter.Enabled=enabled;int before=DrawGuard.Entries;try{proxy.GetType().GetMethod("Render").Invoke(proxy,new object[]{null});throw new InvalidOperationException("Uninitialized fixture unexpectedly rendered");}catch(TargetInvocationException e){Assert(e.InnerException is NullReferenceException,"Renderer IL did not execute expected native null-scene access: "+e.InnerException);}Assert(DrawGuard.Entries==before+1&&DrawGuard.Exits==DrawGuard.Entries,"Native original/optimized draw bypassed or leaked its policy scope");}adapter.Enabled=true;});
    Check("dispose-restores-original-scene-object",delegate{adapter.Dispose();adapter.Dispose();Assert(Object.ReferenceEquals(viewport.DXObjects[0],cube),"Dispose failed to restore original native cube");});
   }
   Check("unsupported-renderer-falls-back-without-scene-mutation",delegate{object unsupported=new object();using(var viewport=new FullRendererFixtureViewport(unsupported)){Assert(NativeFullRenderer.TryInstall(viewport,unsupported)==null,"Unknown runtime was accepted");Assert(Object.ReferenceEquals(viewport.DXObjects[0],unsupported),"Fallback changed unsupported scene");}});
   File.WriteAllText(args[1],new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(LocalApi.D("passed",true,"scope","CPU/WinForms only; no DirectX device or GPU rendering","checks",checks)));return 0;
  }catch(Exception error){Console.WriteLine(error);if(args.Length>1)File.WriteAllText(args[1],new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(LocalApi.D("passed",false,"checks",checks,"error",error.ToString())));return 1;}
 }
}
