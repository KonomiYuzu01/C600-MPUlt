// Isolated adapter for the retained MPUlt renderer. The engine owns all state.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

internal sealed class ExperimentBridge : IDisposable {
 readonly Form form; readonly string exe; readonly object puzzle,cube;
 readonly Array stickers; readonly NativeStickerAccess access;
 readonly NativeRendererLifecycle renderer; readonly NativeFullRenderer full;
 readonly NativePickingVisibility picking;
 readonly Func<bool> allowed;
 readonly double[,] defaultCamera;
 readonly double defaultRadius,defaultAngle,defaultFaceShrink,defaultStickerShrink;
 object[] shown,hidden; byte[] previousStyles;
 int[] labToNativeCell;
 bool dirty,disposed,pendingNative;
 internal readonly Control Viewport;
 internal NativeSnapshot Snapshot { get; private set; }
 internal Dictionary<string,object> Profile { get; private set; }
 internal Color[] Palette { get; private set; }
 internal event Action<int,string> Inspect;
 internal event Action<string[]> Turn;

 internal ExperimentBridge(Form form,string exe,Func<bool> allowed){
  this.form=form;this.exe=exe;this.allowed=allowed;
  puzzle=Reflect.Get(form,"Puz");cube=Reflect.Get(form,"CubeView");Viewport=(Control)Reflect.Get(form,"dxControl2");
  var timerField=Reflect.Field(form.GetType(),"m_Timer");
  var timer=timerField==null?null:timerField.GetValue(form) as System.Threading.Timer;
  if(timer!=null)timer.Change(Timeout.Infinite,Timeout.Infinite);
  stickers=(Array)Reflect.Get(cube,"Stks");access=new NativeStickerAccess(stickers);
  // Keep the original mesh dimensions and original sliders in agreement.
  double fs=Convert.ToDouble(Reflect.Get(cube,"FShr")),ss=Convert.ToDouble(Reflect.Get(cube,"SShr"));
  bool geometry=Convert.ToBoolean(Reflect.Get(form,"m_setgeom"));Reflect.Set(form,"m_setgeom",false);
  try{SetSlider("trk_faceShrink",fs);SetSlider("trk_StickerSize",ss);}finally{Reflect.Set(form,"m_setgeom",geometry);}
  Reflect.Call(cube,"SetStickerSize",fs,ss);Reflect.Set(form,"TRate",1);
  var camera=Reflect.Property(Reflect.Property(Viewport,"Scene"),"Camera");
  defaultCamera=(double[,])((double[,])Reflect.Get(Reflect.Get(camera,"Trans"),"M")).Clone();
  defaultRadius=Convert.ToDouble(Reflect.Get(camera,"R"));defaultAngle=Convert.ToDouble(Reflect.Property(camera,"Angle"));
  defaultFaceShrink=fs;defaultStickerShrink=ss;
  renderer=new NativeRendererLifecycle(Viewport);renderer.Subset=new NativeRenderSubset(Viewport,cube,puzzle);
  full=NativeFullRenderer.TryInstall(Viewport,cube,renderer.Subset);
  if(full==null)throw new InvalidOperationException("Retained full native renderer policy could not be installed.");
  picking=new NativePickingVisibility(Viewport,form,cube,puzzle,renderer.PreparePicking,()=>allowed()&&!pendingNative);
  picking.BeforeNativeClick=delegate{renderer.HoldUpdates=true;Reflect.Set(form,"TRate",1);dirty=true;};
  Viewport.MouseUp+=OnMouseUp;Viewport.Enabled=false;renderer.Resume();
 }
 void SetSlider(string name,double value){var slider=(TrackBar)Reflect.Get(form,name);slider.Value=Math.Max(slider.Minimum,Math.Min(slider.Maximum,(int)Math.Round(value*slider.Maximum)));}
 internal bool FrameworkHidden {get{return renderer.Subset.HideFramework;}}
 internal bool AdaptiveMotion {get{return renderer.SmoothMotion;}}
 internal TrackBar DisplaySlider(string name){return (TrackBar)Reflect.Get(form,name);}
 void SyncDisplayDimensions(){Reflect.Set(cube,"FShr",Reflect.Property(form,"CPShrinkFace"));Reflect.Set(cube,"SShr",Reflect.Property(form,"CPStickerSize"));}
 internal void SetDisplayValue(string name,int value){
  if(!allowed()||disposed)throw new InvalidOperationException("Wait for the current operation before changing display settings.");
  var original=DisplaySlider(name);if(value<original.Minimum||value>original.Maximum)throw new ArgumentOutOfRangeException("value");
  original.Value=value;
  if(name=="trk_faceShrink"||name=="trk_StickerSize"){
   SyncDisplayDimensions();
  }
  Reflect.Call(Viewport,"SetSceneChanged");
 }
 internal void ResetView(){
  if(!allowed()||disposed)return;
  SetSlider("trk_faceShrink",defaultFaceShrink);SetSlider("trk_StickerSize",defaultStickerShrink);SetSlider("trk_ViewAngle",defaultAngle/Math.PI);
  SyncDisplayDimensions();
  var camera=Reflect.Property(Reflect.Property(Viewport,"Scene"),"Camera");
  lock(Viewport){Reflect.Set(Reflect.Get(camera,"Trans"),"M",defaultCamera.Clone());Reflect.Set(camera,"R",defaultRadius);Reflect.SetProperty(camera,"Angle",defaultAngle);Reflect.Call(cube,"SetStickerSize",Reflect.Get(cube,"FShr"),Reflect.Get(cube,"SShr"));Reflect.Call(camera,"SetChanged");Reflect.Call(Viewport,"SetSceneChanged");}
 }
 internal Dictionary<string,object> Geometry(){
  var s=Reflect.Get(puzzle,"Str");
  if(Convert.ToBoolean(Reflect.Get(s,"QSimplified"))||Convert.ToInt32(Reflect.Get(s,"NStickers"))!=259800)
   throw new InvalidOperationException("The native bridge requires the immutable 600-cell-Full model.");
  var faces=new List<object>();foreach(object f in (Array)Reflect.Get(s,"Faces"))faces.Add(LocalApi.D("id",Reflect.Get(f,"Id"),"pole",Reflect.Get(f,"Pole"),"first",Reflect.Get(f,"FirstSticker"),"count",Reflect.Get(Reflect.Get(f,"Base"),"NStickers")));
  var bases=new List<object>();foreach(object a in (Array)Reflect.Get(s,"BaseAxes")){var twists=new List<object>();foreach(object t in (Array)Reflect.Get(a,"Twists"))twists.Add(LocalApi.D("order",Reflect.Get(t,"Order"),"maps",Reflect.Get(t,"Map")));bases.Add(LocalApi.D("id",Reflect.Get(a,"Id"),"cuts",Reflect.Get(a,"Cut"),"twists",twists));}
  var axes=new List<object>();foreach(object a in (Array)Reflect.Get(s,"Axes"))axes.Add(LocalApi.D("id",Reflect.Get(a,"Id"),"base",Reflect.Get(Reflect.Get(a,"Base"),"Id"),"dir",Reflect.Get(a,"Dir"),"fixedMask",Reflect.Get(Reflect.Get(a,"Base"),"FixedMask"),"layers",Reflect.Get(a,"Layers")));
  string hash;using(var h=SHA256.Create())using(var f=File.OpenRead(exe))hash=BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();
  return LocalApi.D("format","MPUlt-native600-v1","n",259800,"faces",faces,"axes",axes,"bases",bases,"executable_sha256",hash);
 }
 internal void Configure(Dictionary<string,object> profile){
  if(Convert.ToInt32(profile["matched_stickers"])!=259800||Convert.ToInt32(profile["matched_generators"])!=1200)
   throw new InvalidDataException("Native mapping did not verify the complete model.");
  Profile=profile;var mapping=LocalApi.Array(profile["native_face_to_lab"]);labToNativeCell=new int[600];
  for(int i=0;i<mapping.Length;i++)labToNativeCell[Convert.ToInt32(mapping[i])]=i;
  Palette=new Color[600];var colors=(int[])Reflect.Get(cube,"Colors");
  for(int c=0;c<600;c++)Palette[c]=Color.FromArgb(unchecked((int)0xff000000)|colors[labToNativeCell[c]]);
  var originals=new Dictionary<object,object>();var hiddenOriginals=new Dictionary<object,object>();
  shown=new object[stickers.Length];hidden=new object[stickers.Length];previousStyles=new byte[stickers.Length];
  for(int i=0;i<stickers.Length;i++){
   object b=access.GetBase(access.Slots[i]);
   if(!originals.ContainsKey(b)){
    var visible=Reflect.Clone(b);Reflect.Set(visible,"Rank",0);originals[b]=visible;
    var invisible=Reflect.Clone(b);Reflect.Set(invisible,"Rank",1);Reflect.Set(invisible,"MinBDim",1);
    Reflect.Set(invisible,"NV",0);Reflect.Set(invisible,"NF",0);Reflect.Set(invisible,"NE",0);hiddenOriginals[b]=invisible;
   }
   shown[i]=originals[b];hidden[i]=hiddenOriginals[b];previousStyles[i]=255;
  }
  Reflect.Set(cube,"ShowRank",1);Reflect.Set(form,"qSolved",true);Reflect.Set(form,"m_TRun",false);
 }
 internal void Apply(NativeSnapshot next){
  if(Profile==null||next.ProfileHash!=Convert.ToString(Profile["profile_sha256"]))throw new InvalidDataException("Native snapshot does not match this renderer.");
  lock(Viewport){
   bool initial=Snapshot==null||next.IsFull;byte[] style=next.Styles;
   var field=(short[])Reflect.Get(puzzle,"Field");Buffer.BlockCopy(next.Colors,0,field,0,next.Colors.Length);
   ClearScratch();Reflect.Call(form,"InitStatus");
   double fs=Convert.ToDouble(Reflect.Get(cube,"FShr")),ss=Convert.ToDouble(Reflect.Get(cube,"SShr"));
   int[] changed=initial?null:next.ChangedSlots;int count=changed==null?style.Length:changed.Length;
   for(int j=0;j<count;j++){
    int i=changed==null?j:changed[j];
    if((style[i]!=0)!=(previousStyles[i]!=0)||previousStyles[i]==255){var st=access.Slots[i];access.SetBase(st,style[i]==0?hidden[i]:shown[i]);if(style[i]!=0)access.SetCoord(st,fs,ss);}
    previousStyles[i]=style[i];
   }
   access.ApplyColors(cube,field,style,changed,dirty);
   if(initial||next.VisibilityChanged)renderer.Subset.Update(style);
   if(initial||next.VisibilityChanged||next.InteractionChanged)picking.Update(style,next.Interactive);
   var prefs=LocalApi.AsDict(next.State["prefs"]);object v,h;bool hideFrame=true;
   if(prefs.TryGetValue("view",out v)&&LocalApi.AsDict(v).TryGetValue("native_hide_frame",out h)&&h is bool)hideFrame=(bool)h;
   bool frameChanged=renderer.Subset.HideFramework!=hideFrame;renderer.Subset.HideFramework=hideFrame;
   bool adaptive=true;if(prefs.TryGetValue("view",out v)&&LocalApi.AsDict(v).TryGetValue("native_adaptive_motion",out h)&&h is bool)adaptive=(bool)h;
   if(renderer.SmoothMotion!=adaptive)renderer.SmoothMotion=adaptive;
   if(initial||count>0||frameChanged||dirty)Reflect.Call(Viewport,"SetSceneChanged");
   dirty=false;Snapshot=next;renderer.HoldUpdates=false;
  }
 }
 void ClearScratch(){foreach(string name in new[]{"Ptr","LSeq","LShuffle","NTwists"})Reflect.Set(puzzle,name,0);}
 void OnMouseUp(object sender,MouseEventArgs e){
  if(disposed)return;int hit;string gesture;
  bool inspect=picking.TakeInspection(e.Button,out hit,out gesture);
  if(picking.SuppressCapture&&!inspect||!allowed()||pendingNative)return;
  pendingNative=true;
  // The original native WndProc still owns the picking/turn callback here.
  // Publish only after it returns, never while its temporary face mask is live.
  try{form.BeginInvoke((Action)delegate{
   pendingNative=false;if(disposed||!allowed())return;
   if(inspect){if(Inspect!=null)Inspect(hit,gesture);}else CaptureTurns();
  });}catch(InvalidOperationException){pendingNative=false;}
 }
 void CaptureTurns(){
  int count=Convert.ToInt32(Reflect.Get(puzzle,"Ptr"));var tokens=new List<string>();
  if(count>0){var seq=(long[])Reflect.Get(puzzle,"Seq");for(int i=0;i<count;i++){long c=seq[i];if(c>=0)tokens.Add((c&65535)+":"+((c>>16)&65535)+":"+((c>>32)&65535)+":"+((c>>48)&65535));}}
  ClearScratch();
  // Native click feedback is a scratch result. Restore the committed display
  // before dispatch; draft destinations must never look committed.
  if(Snapshot!=null)Apply(Snapshot);
  if(tokens.Count>0&&Turn!=null)Turn(tokens.ToArray());
 }
 internal void LocateCell(int canonical){
  if(canonical<1||canonical>600||Profile==null)throw new ArgumentOutOfRangeException("canonical");
  var faces=(Array)Reflect.Get(Reflect.Get(puzzle,"Str"),"Faces");var pole=Reflect.Get(faces.GetValue(labToNativeCell[canonical-1]),"Pole");
  Reflect.Call(Reflect.Property(Reflect.Property(Viewport,"Scene"),"Camera"),"Recenter",pole);
  renderer.Subset.FocusedCell=labToNativeCell[canonical-1];Reflect.Call(Viewport,"SetSceneChanged");
 }
 internal void SetBusy(bool busy){Viewport.Enabled=!busy&&Snapshot!=null;if(!busy&&Snapshot!=null)renderer.HoldUpdates=false;}
 internal bool Recover(Exception error){return renderer.TryRecover(error);}
 internal void Ready(){renderer.AssertReady();}
 public void Dispose(){if(disposed)return;disposed=true;Viewport.MouseUp-=OnMouseUp;picking.Dispose();renderer.Dispose();full.Dispose();}
}
