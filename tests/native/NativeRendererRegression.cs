// Runs the production host with the real MPUlt assembly and Managed DirectX.
// All journal mutations must use a separate test data directory.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.DirectX.Direct3D;

internal static class NativeRendererRegression {
 static readonly List<object> checks=new List<object>();
 static readonly List<string> failures=new List<string>();
 static Form form;static NativeWorkbench wb;static Control viewport;
 static LocalApi api;static string output;static string initialHash,turnedHash,scrambledHash,nativeTurnHash,protectedHash;
 static long protectedHead;
 static object[] originalShown;static int[] originalVertices;static int reconnectHidden;
 static int recoveriesBeforeInjection;
 static int stage;static DateTime deadline;static System.Windows.Forms.Timer timer;
 static bool comprehensiveComplete;
 static readonly bool keyRestartOnly=Environment.GetEnvironmentVariable("C600_RENDER_KEY_RESTART_TEST")=="1";
 static readonly bool displayKeysOnly=Environment.GetEnvironmentVariable("C600_RENDER_DISPLAY_TEST")=="1";
 static readonly bool pickingOnly=Environment.GetEnvironmentVariable("C600_RENDER_PICKING_TEST")=="1";
 static readonly bool featuresOnly=Environment.GetEnvironmentVariable("C600_RENDER_FEATURE_TEST")=="1";
 static readonly bool frameOnly=Environment.GetEnvironmentVariable("C600_RENDER_FRAME_TEST")=="1";
 static readonly bool frameRestartOnly=Environment.GetEnvironmentVariable("C600_RENDER_FRAME_RESTART_TEST")=="1";
 static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
 static void Check(string name){checks.Add(LocalApi.D("name",name,"passed",true));NativeDiagnostics.Write("DIRECTX PASS "+name);}
 static void EnglishDialogContracts(){
  Assert(System.Threading.Thread.CurrentThread.CurrentUICulture.Name=="en-US"&&System.Globalization.CultureInfo.DefaultThreadCurrentUICulture.Name=="en-US","Native UI culture was not initialized to English");
  var reader=typeof(NativeFullRenderer).GetMethod("Read",Reflect.Flags);int filters=0;
  foreach(string name in new[]{"Export","ImportLog","ExportLog"}){
   var method=typeof(NativeWorkbench).GetMethod(name,Reflect.Flags);var instructions=(System.Collections.IEnumerable)reader.Invoke(null,new object[]{method});var text=new List<string>();
   foreach(object instruction in instructions){var operand=Reflect.Get(instruction,"Operand");if(operand is string)text.Add((string)operand);}
   string title=name=="Export"?"Export puzzle record":name=="ImportLog"?"Import puzzle log":"Export puzzle log";Assert(text.Contains(title),"Actual native dialog method lacks its English title: "+name);
   foreach(string value in text)if(value.Contains("|")){using(var dialog=new OpenFileDialog()){dialog.Filter=value;Assert(dialog.Filter==value,"Native dialog filter assignment changed");filters++;}}
  }
  Assert(filters==2,"Expected two valid actual compiled log dialog filters");Check("Production English UI initialization and actual compiled import/export dialog titles and filter strings are valid");
 }
 static void VisibleLayout(){
  var workspace=(NativeDockLayout)Reflect.Get(wb,"workspace");
  Assert(form.Visible&&viewport.Visible&&workspace.Visible,"Viewport hierarchy is hidden");
  Assert(viewport.FindForm()==form,"Viewport lost its parent form");
  var bounds=viewport.RectangleToScreen(viewport.ClientRectangle);
  var client=form.RectangleToScreen(form.ClientRectangle);
  Assert(bounds.Width>100&&bounds.Height>100&&client.Contains(bounds),"Viewport is clipped outside the form");
  if(workspace.ToolsRequested){var tabs=(TabControl)Reflect.Get(wb,"tabs");Assert(tabs.Visible&&client.Contains(tabs.RectangleToScreen(tabs.ClientRectangle)),"Tools are hidden or clipped");}
 }
 static void Capture(string name){
  VisibleLayout();
  Assert(((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).FramesRequested>0,"Native idle rendering never resumed");
  var scene=Reflect.Property(viewport,"Scene");
  Assert(Convert.ToBoolean(Reflect.Property(scene,"IsReady")),"DirectX scene is not ready");
  var device=(Device)Reflect.Property(scene,"Renderer");
  Assert(device!=null&&!device.Disposed,"No live DirectX device");
  // Exercise normal update/reset first, then save an actual freshly drawn render
  // target before Present can discard the backbuffer contents.
  Reflect.Call(viewport,"SetSceneChanged");Reflect.Call(viewport,"ForceUpdate");
  Assert(Convert.ToInt32(Reflect.Call(scene,"FrameMove"))>=0,"FrameMove failed");
  lock(viewport)using(((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).Subset.Enter())Assert(Convert.ToInt32(Reflect.Call(scene,"Render"))>=0,"Native Render failed");
  string path=Path.Combine(output,name+".png");
  using(var surface=device.GetRenderTarget(0))SurfaceLoader.Save(path,ImageFileFormat.Png,surface);
  device.Present();
  int colored=0;var colors=new HashSet<int>();
  using(var bmp=new Bitmap(path)){
   Assert(bmp.Size==viewport.ClientSize,"DirectX backbuffer size does not match the visible viewport: "+bmp.Size+" versus "+viewport.ClientSize);
   for(int y=0;y<bmp.Height;y+=3)for(int x=0;x<bmp.Width;x+=3){var c=bmp.GetPixel(x,y);colors.Add(c.ToArgb());if(Math.Max(c.R,Math.Max(c.G,c.B))-Math.Min(c.R,Math.Min(c.G,c.B))>25)colored++;}
   Assert(colors.Count>24&&colored>50,"Rendered frame is blank or contains no colored puzzle geometry: colors="+colors.Count+", colored samples="+colored+", file="+path);
   checks.Add(LocalApi.D("name",name,"passed",true,"width",bmp.Width,"height",bmp.Height,"form_width",form.Width,"form_height",form.Height,"window_state",form.WindowState.ToString(),"idle_callbacks",((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).FramesRequested,"sampled_colors",colors.Count,"colored_samples",colored));
  }
  NativeDiagnostics.Write("DIRECTX FRAME "+name+"; colors="+colors.Count+"; colored="+colored);
 }
 static string StateHash(){return Convert.ToString(LocalApi.AsDict(Reflect.Get(wb,"state"))["state_hash"]);}
 static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static void MatchAtomicSnapshot(string name){
  var reply=api.Get("native/snapshot");var snapshot=NativeSnapshot.Read(reply,Convert.ToString(LocalApi.AsDict(Reflect.Get(wb,"profile"))["profile_sha256"]));
  Assert(Convert.ToString(snapshot.State["state_hash"])==StateHash(),"Host status and snapshot differ: "+name);
  var field=(short[])Reflect.Get(Reflect.Get(wb,"puz"),"Field");var styles=(byte[])Reflect.Get(wb,"previousStyles");
  Assert(field.Length==259800&&styles.Length==259800,"Native state is incomplete");
  for(int i=0;i<field.Length;i++)if(field[i]!=(snapshot.Colors[2*i]|snapshot.Colors[2*i+1]<<8)||styles[i]!=snapshot.Styles[i])throw new InvalidOperationException("Atomic native color/style mismatch at "+i+": "+name);
  byte[] labels=api.Bytes("labels");Assert(labels.Length==259800*4&&Hash(labels)==StateHash(),"Full labelled state hash mismatch: "+name);
  Check(name+" matches all 259800 native colors/styles and full labelled hash");
 }
 static byte[] Pixels(string path){
  using(var bitmap=new Bitmap(path)){
   var rect=new Rectangle(0,0,bitmap.Width,bitmap.Height);var data=bitmap.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try{var result=new byte[bitmap.Width*bitmap.Height*4];for(int y=0;y<bitmap.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),result,y*bitmap.Width*4,bitmap.Width*4);return result;}
   finally{bitmap.UnlockBits(data);}
  }
 }
 static void CompareFilteredPixels(){
  var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");var subset=renderer.Subset;var scene=Reflect.Property(viewport,"Scene");var device=(Device)Reflect.Property(scene,"Renderer");
  object puzzle=Reflect.Get(wb,"puz");var field=(short[])Reflect.Get(puzzle,"Field");var styles=(byte[])Reflect.Get(wb,"previousStyles");var selected=new List<int>();
  for(int i=0;i<styles.Length;i++)if(styles[i]!=0)selected.Add(i);
  Assert(selected.Count>2&&selected.Count<259800,"Pixel comparison requires a real filtered scene");
  int a=selected[selected.Count/3],b=selected[2*selected.Count/3];short oldA=field[a],oldB=field[b];
  string compact=Path.Combine(output,"15-filtered-subset-outlines.png"),full=Path.Combine(output,"16-filtered-reference-outlines.png");
  renderer.Pause();try{
   Assert(renderer.PreparePicking(),"Pixel comparison could not prepare the actual device");
   // Exercise native selected-outline indexing at nontrivial full-array
   // positions. Only this isolated in-memory display field is touched, and its
   // original values are restored even when comparison fails.
   field[a]=unchecked((short)((ushort)field[a]|0x8000));field[b]=unchecked((short)((ushort)field[b]|0x8000));
   lock(viewport){
    using(subset.Enter())Assert(Convert.ToInt32(Reflect.Call(scene,"Render"))>=0,"Compact native reference render failed");
    using(var surface=device.GetRenderTarget(0))SurfaceLoader.Save(compact,ImageFileFormat.Png,surface);
    using(subset.EnterFramework())Assert(Convert.ToInt32(Reflect.Call(scene,"Render"))>=0,"Full-array native reference render failed");
    using(var surface=device.GetRenderTarget(0))SurfaceLoader.Save(full,ImageFileFormat.Png,surface);
   }
   var left=Pixels(compact);var right=Pixels(full);Assert(left.Length==right.Length,"Reference frame dimensions differ");
   for(int i=0;i<left.Length;i++)if(left[i]!=right[i])throw new InvalidOperationException("Compact render changed an actual native color/outline pixel at byte "+i);
   checks.Add(LocalApi.D("name","Filtered compact and original-full-array native renders are pixel-identical including selected outlines","passed",true,"pixels",left.Length/4,"visible_stickers",selected.Count,"outline_native_indices",new[]{a,b},"pixel_sha256",Hash(left)));
  }finally{field[a]=oldA;field[b]=oldB;renderer.Resume();}
 }
 static void InjectDeviceLoss(){
  var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");var scene=Reflect.Property(viewport,"Scene");Exception lost=null;
  // Instantiate the real Managed DirectX exception using its available
  // constructor; no Windows display/sleep/device settings are changed.
  foreach(var constructor in typeof(DeviceLostException).GetConstructors(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)){
   var parameters=constructor.GetParameters();var args=new object[parameters.Length];bool supported=true;
   for(int i=0;i<parameters.Length;i++){
    if(parameters[i].ParameterType==typeof(string))args[i]="Regression-injected device interruption";
    else if(parameters[i].ParameterType==typeof(int))args[i]=unchecked((int)0x88760868);
    else if(parameters[i].ParameterType==typeof(Exception))args[i]=null;
    else{supported=false;break;}
   }
   if(supported){lost=(Exception)constructor.Invoke(args);break;}
  }
  Assert(lost!=null&&lost.GetType()==typeof(DeviceLostException),"Could not construct actual DirectX DeviceLostException");
  recoveriesBeforeInjection=renderer.RecoveryCount;Reflect.Set(scene,"qRenderOn",true);
  Assert(wb.TryRecoverRenderer(lost),"Production renderer did not accept the injected device exception");
  Assert(!Convert.ToBoolean(Reflect.Get(scene,"qRenderOn")),"Recovery left original Render recursion latch set");
 }
 static void CheckInjectedRecovery(){
  var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");var scene=Reflect.Property(viewport,"Scene");
  Assert(renderer.RenderFrame(),"Real DirectX draw did not recover after the retry interval");
  Assert(Convert.ToBoolean(Reflect.Property(scene,"IsReady"))&&!Convert.ToBoolean(Reflect.Get(scene,"qRenderOn")),"Recovered real device is unready or recursion latch remains set");
  Assert(renderer.RecoveryCount>recoveriesBeforeInjection,"Injected interruption did not complete recovery");
  Check("Injected DeviceLostException and latched Render recover through the real device without changing display settings");
 }
 static void BeginVisibilityReconnect(){
  originalShown=(object[])Reflect.Get(wb,"shownBases");originalVertices=new int[originalShown.Length];
  var styles=(byte[])Reflect.Get(wb,"previousStyles");reconnectHidden=-1;
  for(int i=0;i<originalShown.Length;i++){originalVertices[i]=Convert.ToInt32(Reflect.Get(originalShown[i],"NV"));if(styles[i]==0&&originalVertices[i]>0)reconnectHidden=i;}
  Assert(reconnectHidden>=0,"Reconnect probe has no hidden nonempty mesh");
  var slots=((NativeStickerAccess)Reflect.Get(wb,"stickerAccess")).Slots;
  Assert(Convert.ToInt32(Reflect.Get(Reflect.Get(slots[reconnectHidden],"Base"),"NV"))==0,"Reconnect probe was not hidden");
  // Exercise the same production visibility preparation used during reconnect,
  // without repeating the unrelated full geometry handshake.
  Reflect.Call(wb,"PrepareVisibility");
  Assert(Object.ReferenceEquals(originalShown,Reflect.Get(wb,"shownBases")),"Reconnect replaced original shown geometry");
  ((TextBox)Reflect.Get(wb,"filter")).Text="everything";Reflect.Call(wb,"ApplyFilter");
 }
 static void CheckVisibilityReconnect(){
  var slots=((NativeStickerAccess)Reflect.Get(wb,"stickerAccess")).Slots;
  for(int i=0;i<slots.Length;i++){
   object mesh=Reflect.Get(slots[i],"Base");if(!Object.ReferenceEquals(mesh,originalShown[i]))throw new InvalidOperationException("Reconnected shown mesh identity changed at "+i);
   if(Convert.ToInt32(Reflect.Get(mesh,"NV"))!=originalVertices[i])throw new InvalidOperationException("Reconnect destroyed geometry at "+i);
  }
  Assert(originalVertices[reconnectHidden]>0,"Previously hidden sticker did not recover its geometry");
  Check("Reconnect preparation preserves all shown meshes and restores previously hidden geometry");
 }
 static Dictionary<int,Microsoft.DirectX.Vector3[]> ProjectedSamples(){
  var slots=((NativeStickerAccess)Reflect.Get(wb,"stickerAccess")).Slots;var samples=new Dictionary<int,Microsoft.DirectX.Vector3[]>();
  // Sample throughout native order, including many indices outside the motion
  // sample; copy actual cached hit-test coordinates, not model-derived fixtures.
  for(int i=0;i<slots.Length;i+=257){var value=(Microsoft.DirectX.Vector3[])Reflect.Get(slots[i],"Coords3D");samples[i]=(Microsoft.DirectX.Vector3[])value.Clone();}
  return samples;
 }
 static bool SameCoords(Microsoft.DirectX.Vector3[] a,Microsoft.DirectX.Vector3[] b){
  if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(!a[i].X.Equals(b[i].X)||!a[i].Y.Equals(b[i].Y)||!a[i].Z.Equals(b[i].Z))return false;return true;
 }
 static void CheckMotionPicking(){
  var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");var subset=renderer.Subset;var scene=Reflect.Property(viewport,"Scene");var camera=Reflect.Property(scene,"Camera");
  var cube=Reflect.Get(wb,"cube");object stickers=Reflect.Get(cube,"Stks"),puzzle=Reflect.Get(cube,"Cube");
  Assert(subset.VisibleCount==259800&&subset.MotionCount==NativeRenderSubset.MotionLimit,"All-piece motion probe has wrong counts");
  renderer.Pause();try{
   Assert(renderer.PreparePicking(),"Could not establish a full frame before motion");var before=ProjectedSamples();
   Reflect.Call(Reflect.Get(camera,"Trans"),"Rotate",0,3,.035);Reflect.Call(camera,"SetChanged");renderer.NotifyCameraInteraction();
   Assert(renderer.RenderFrame(),"Motion sample did not render");
   Assert(Convert.ToBoolean(Reflect.Get(renderer,"restoreDetail")),"Motion path did not leave a pending complete-detail frame");
   Assert(Object.ReferenceEquals(stickers,Reflect.Get(cube,"Stks"))&&Object.ReferenceEquals(puzzle,Reflect.Get(cube,"Cube"))&&Convert.ToInt32(Reflect.Get(cube,"NStk"))==259800&&!subset.IsEntered,"Motion scope leaked compact arrays");
   int frames=renderer.FramesRequested;Assert(renderer.PreparePicking(),"Picking could not restore full detail");
   Assert(renderer.FramesRequested==frames+1&&!renderer.IsMotionActive&&!Convert.ToBoolean(Reflect.Get(renderer,"restoreDetail")),"Picking did not force one complete frame");
   var picked=ProjectedSamples();int changed=0;foreach(var item in picked)if(!SameCoords(before[item.Key],item.Value))changed++;
   Assert(changed>50,"Camera probe did not alter representative cached projections");
   // A fresh direct native full render is an independent path from the
   // scheduler/subset. It must produce the same cached picking coordinates.
   Assert(Convert.ToInt32(Reflect.Call(scene,"Render"))>=0,"Independent native full render failed");
   var full=ProjectedSamples();foreach(var item in picked)Assert(SameCoords(item.Value,full[item.Key]),"Picking used stale projection at native sticker "+item.Key);
   checks.Add(LocalApi.D("name",NativeRenderSubset.MotionLimit+"-mesh motion followed by picking restores full arrays and fresh cached projections","passed",true,"full_slots",259800,"projected_mesh_samples",picked.Count,"changed_samples",changed));
  }finally{renderer.Resume();}
 }
 internal static int[] FindNativeTurn(Dictionary<string,object> certificate){
  var source=LocalApi.Array(certificate["source"]);var destination=LocalApi.Array(certificate["destination"]);var expected=new Dictionary<int,int>();
  for(int i=0;i<source.Length;i++)expected[Convert.ToInt32(source[i])]=Convert.ToInt32(destination[i]);
  int[] nativeToLab=(int[])Reflect.Get(wb,"nativeToLab");var structure=Reflect.Get(Reflect.Get(wb,"puz"),"Str");var axes=(Array)Reflect.Get(structure,"Axes");
  foreach(object axis in axes){var layers=(Array)Reflect.Get(axis,"Layers");var twists=(Array)Reflect.Get(Reflect.Get(axis,"Base"),"Twists");
   foreach(int layer in new[]{0,2}){
    var slots=(int[])layers.GetValue(layer);bool touches=false;foreach(int slot in slots)if(expected.ContainsKey(nativeToLab[slot])){touches=true;break;}if(!touches)continue;
    for(int twist=0;twist<twists.Length;twist++){
     object turn=twists.GetValue(twist);int order=Convert.ToInt32(Reflect.Get(turn,"Order"));var map=(int[])((Array)Reflect.Get(turn,"Map")).GetValue(layer);
     for(int angle=1;angle<order;angle++){
      int changed=0;bool same=true;
      for(int i=0;i<slots.Length;i++){int j=i;for(int k=0;k<angle;k++)j=map[j];if(j==i)continue;changed++;int target;
       if(!expected.TryGetValue(nativeToLab[slots[i]],out target)||target!=nativeToLab[slots[j]]){same=false;break;}
      }
      if(same&&changed==expected.Count)return new[]{Convert.ToInt32(Reflect.Get(axis,"Id")),twist,angle,1<<layer};
     }
    }
   }
  }
  throw new InvalidOperationException("No live native axis/twist/layer matches the retained H0 grip certificate");
 }
 static void StartNativeTurn(){
  Assert(Convert.ToBoolean(Reflect.Get(wb,"instantTurns"))&&Convert.ToInt32(Reflect.Get(form,"TRate"))==1,"Instant turns are not enabled by default");
  var preview=api.Post("preview",LocalApi.D("recipe",new object[]{LocalApi.D("kind","word","moves",new[]{1})},"note","Native regression H0 map witness"));
  nativeTurnHash=Convert.ToString(preview["post_state"]);int[] turn=FindNativeTurn(api.Get("certificate"));api.Post("cancel",LocalApi.D());Reflect.Call(wb,"RefreshFromServer");
  bool unsafeWork=false;int originalStatus=Convert.ToInt32(Reflect.Get(form,"m_status"));
  try{Reflect.Set(form,"m_status",2);Reflect.Call(wb,"Run",new Action(delegate{unsafeWork=true;}),new Action(delegate{}),false);Assert(!Convert.ToBoolean(Reflect.Get(wb,"busy"))&&!unsafeWork,"Host operation entered native animation");}
  finally{Reflect.Set(form,"m_status",originalStatus);}
  bool pumped=false;form.BeginInvoke((Action)delegate{pumped=true;});var elapsed=Stopwatch.StartNew();
  Reflect.Call(form,"StartAnimation",turn[0],turn[1],turn[2],turn[3]);elapsed.Stop();
  Assert(!pumped,"Instant native animation pumped queued UI work before completing");
  var slots=((NativeStickerAccess)Reflect.Get(wb,"stickerAccess")).Slots;foreach(object mesh in slots)Assert(Reflect.Get(mesh,"ExtraTwist")==null,"Instant turn left stale twist geometry");
  Assert(Convert.ToInt32(Reflect.Get(form,"m_status"))==0&&!Convert.ToBoolean(Reflect.Get(form,"m_runUndo")),"Native animation status was not cleared");
  object puzzle=Reflect.Get(wb,"puz");Reflect.Call(puzzle,"Twist",turn[0],turn[1],turn[2],turn[3]);Assert(Convert.ToInt32(Reflect.Get(puzzle,"Ptr"))>0,"Real native twist did not record a native move");
  checks.Add(LocalApi.D("name","Real mapped H0 StartAnimation and Puzzle.Twist execute with no UI pumping or residual ExtraTwist","passed",true,"axis",turn[0],"twist",turn[1],"angle",turn[2],"mask",turn[3],"instant_animation_ms",elapsed.Elapsed.TotalMilliseconds));
  Check("Host refuses operations while native animation status is active");
  Reflect.Call(wb,"CaptureNativeTurns");
 }
 static Dictionary<string,object> State(){return LocalApi.AsDict(Reflect.Get(wb,"state"));}
 static Dictionary<string,object> Prefs(){return LocalApi.AsDict(State()["prefs"]);}
 static Dictionary<string,object> Pending(){return State()["pending"]==null?null:LocalApi.AsDict(State()["pending"]);}
 static string StatusText(){return ((ToolStripStatusLabel)Reflect.Get(wb,"message")).Text;}
 static async Task WaitIdle(){
  var until=DateTime.UtcNow.AddSeconds(150);
  while(Convert.ToBoolean(Reflect.Get(wb,"busy"))){Assert(DateTime.UtcNow<until,"Native operation timed out: "+StatusText());await Task.Delay(30);}
  Assert(Convert.ToBoolean(Reflect.Get(wb,"connected"))&&viewport.Enabled,"Native command left host disconnected or viewport disabled: "+StatusText());
 }
 static async Task Native(string method,params object[] arguments){Reflect.Call(wb,method,arguments);await WaitIdle();}
 static async Task Backend(string path,Dictionary<string,object> data){
  Reflect.Call(wb,"Run",new Action(delegate{api.Post(path,data);}),new Action(delegate{Reflect.Call(wb,"RefreshFromServer");}),false);await WaitIdle();
 }
 static byte[] Styles(){return (byte[])((byte[])Reflect.Get(wb,"previousStyles")).Clone();}
 static int Visible(){int n=0;foreach(byte b in Styles())if(b!=0)n++;return n;}
 static byte[] WorkCellMask(int cell){
  // Decode the retained immutable slot->piece asset independently of Filters.
  // A work cell shows complete physical pieces, including their other stickers.
  byte[] data=api.Bytes("../assets/slot_piece.u32");Assert(data.Length==259800*4,"Unexpected retained slot-piece asset size");var pieces=new HashSet<int>();for(int i=cell*433;i<(cell+1)*433;i++)pieces.Add(BitConverter.ToInt32(data,4*i));
  var mapping=(int[])Reflect.Get(wb,"nativeToLab");var expected=new byte[259800];for(int i=0;i<expected.Length;i++)if(pieces.Contains(BitConverter.ToInt32(data,4*mapping[i])))expected[i]=2;return expected;
 }
 static async Task Apply(string expression){((TextBox)Reflect.Get(wb,"filter")).Text=expression;await Native("ApplyFilter");}
 static void SameBytes(byte[] actual,byte[] expected,string message){Assert(actual.Length==expected.Length,message+" length");for(int i=0;i<actual.Length;i++)if(actual[i]!=expected[i])throw new InvalidOperationException(message+" at native slot "+i);}
 static void ExactVisibility(byte[] expected,string message){
  SameBytes(Styles(),expected,message);var slots=((NativeStickerAccess)Reflect.Get(wb,"stickerAccess")).Slots;
  var shown=(object[])Reflect.Get(wb,"shownBases");var hidden=(object[])Reflect.Get(wb,"hiddenBases");
  for(int i=0;i<slots.Length;i++){
   Assert(Object.ReferenceEquals(Reflect.Get(slots[i],"Base"),expected[i]==0?hidden[i]:shown[i]),message+" mesh "+i);
   if(expected[i]!=0&&expected[i]!=2){int color=expected[i]==1?unchecked((int)0xFF90969C):expected[i]==3?unchecked((int)0xFFF89A35):expected[i]==4?unchecked((int)0xFF29E3DE):expected[i]==5?unchecked((int)0xFFFF5F33):unchecked((int)0xFFFFE866);Assert(Convert.ToInt32(Reflect.Get(slots[i],"Col"))==color,message+" native display color "+i);}
  }
  Assert(((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).Subset.VisibleCount==Visible(),message+" compact draw count");
 }
 static async Task FilterWorkflow(){
  string hash=StateHash();await Apply("O33");Assert(!Convert.ToBoolean(Prefs()["pin_safety"]),"Exact filter retained pins");var orbit33=Styles();Assert(Visible()==2400,"O33 exact filter should contain 2400 stickers");
  await Native("Grip",0,false);Assert(Pending()!=null,"Filter context probe has no preview");SameBytes(Styles(),orbit33,"Preview support leaked through exact filter");
  await Apply("nothing");ExactVisibility(new byte[259800],"Nothing filter leaked buffers or preview geometry");
  ((CheckBox)Reflect.Get(wb,"pin")).Checked=true;await WaitIdle();Assert(Visible()>0,"Opt-in context does not reveal buffer/preview support");
  await Apply("nothing");Assert(!((CheckBox)Reflect.Get(wb,"pin")).Checked,"Applying exact filter did not turn optional context off");ExactVisibility(new byte[259800],"Exact filter leaked context after pin toggle");
  await Native("Cancel");await Apply("O27");var orbit27=Styles();await Apply("O27 | O33");var union=new byte[259800];for(int i=0;i<union.Length;i++)union[i]=(byte)Math.Max(orbit27[i],orbit33[i]);ExactVisibility(union,"Orbit union differs from individual whole orbit masks");
  var list=(CheckedListBox)Reflect.Get(wb,"types");Assert(list.CheckedIndices.Count==2&&list.GetItemChecked(27)&&list.GetItemChecked(33),"Orbit checklist differs from applied union");
  ((TextBox)Reflect.Get(wb,"filter")).Text="O13";await Native("ReportSession");Assert(list.CheckedIndices.Count==2&&list.GetItemChecked(27)&&list.GetItemChecked(33),"Unapplied draft changed applied orbit checklist");Assert(((Label)Reflect.Get(wb,"filterSummary")).Text.Contains("Unapplied edits"),"Unapplied filter draft is not labelled");
  for(int i=0;i<list.Items.Count;i++)list.SetItemChecked(i,i==33);await ClickButton("Use checked orbits");ExactVisibility(orbit33,"Checked-orbit Apply did not select exactly O33");for(int i=0;i<list.Items.Count;i++)list.SetItemChecked(i,false);await ClickButton("Use checked orbits");ExactVisibility(new byte[259800],"Empty orbit checklist did not hide all pieces");
  await Apply("[{\"expr\":\"O33\",\"style\":\"highlight\"},{\"expr\":\"O27\",\"style\":\"ghost\"}]");var layered=new byte[259800];for(int i=0;i<layered.Length;i++)layered[i]=(byte)(orbit33[i]!=0?3:orbit27[i]!=0?1:0);ExactVisibility(layered,"Layered styles changed native filtering");
  await Native("ExcludeOrbit",33);for(int i=0;i<layered.Length;i++)if(orbit33[i]!=0)layered[i]=0;ExactVisibility(layered,"Exclude orbit leaked excluded geometry or lost ghost style");
  var rules=api.Json(Prefs()["rules"]);await Apply("O33 &");Assert(api.Json(Prefs()["rules"])==rules&&StateHash()==hash&&StatusText().Length>0,"Invalid expression changed filter or state");ExactVisibility(layered,"Invalid expression changed visible geometry");
  var presets=(ComboBox)Reflect.Get(wb,"presets");presets.SelectedItem="All pieces";await ClickButton("Apply preset");Assert(Visible()==259800,"All-pieces preset omitted geometry");presets.SelectedItem="Unsolved orbit";await ClickButton("Apply preset");Assert(Visible()==0,"Solved root leaked pieces in Unsolved orbit preset");presets.SelectedItem="Orientation only";await ClickButton("Apply preset");Assert(Visible()==0,"Solved root leaked pieces in Orientation only preset");((NumericUpDown)Reflect.Get(wb,"cell")).Value=13;presets.SelectedItem="Work cell";await ClickButton("Apply preset");ExactVisibility(WorkCellMask(13),"Work-cell preset differs from independently decoded whole-piece model mask");Assert(((TextBox)Reflect.Get(wb,"filter")).Text=="current(C013)","Work-cell preset did not use the focused cell");
  await Apply("active");Assert(StateHash()==hash&&Pending()==null,"Filter workflow mutated mechanics");MatchAtomicSnapshot("Exact filters, optional context, exclusions and malformed-expression recovery");
  Check("Native exact filters, layered styles, orbit checklist, unapplied edits, exclusion and invalid recovery preserve geometry and full state");
 }
 static object Camera(){return Reflect.Property(Reflect.Property(viewport,"Scene"),"Camera");}
 static double[,] Identity(){var m=new double[4,4];for(int i=0;i<4;i++)m[i,i]=1;return m;}
 static void SetMatrix(double[,] matrix){Reflect.Set(Reflect.Get(Camera(),"Trans"),"M",matrix);Reflect.Call(Camera(),"SetChanged");Reflect.Call(viewport,"SetSceneChanged");}
 internal static void NativeDrag(MouseButtons button,int dx,int dy){
  // Invoke the original Form1 event handlers, which instantiate the original
  // KeysProvider and call DXControl -> S3DirectX -> S4Camera. No OS input is sent.
  Reflect.Call(form,"MouseEvt",viewport,new MouseEventArgs(MouseButtons.None,0,200,200,0));
  Reflect.Call(form,"MouseDownEvt",viewport,new MouseEventArgs(button,1,200,200,0));
  Reflect.Call(form,"MouseEvt",viewport,new MouseEventArgs(button,0,200+dx,200+dy,0));
  Reflect.Call(form,"MouseUpEvt",viewport,new MouseEventArgs(button,1,200+dx,200+dy,0));
  Reflect.Call(form,"MouseEvt",viewport,new MouseEventArgs(MouseButtons.None,0,200+dx,200+dy,0));
  Assert(!Convert.ToBoolean(Reflect.Get(form,"qLeftDown"))&&!Convert.ToBoolean(Reflect.Get(form,"qRightDown")),"Original camera handlers left a button latched");
 }
 static void CameraWorkflow(){
  Assert(Control.ModifierKeys==Keys.None,"Camera test requires released physical modifier keys");
  var saved=(double[,])((double[,])Reflect.Get(Reflect.Get(Camera(),"Trans"),"M")).Clone();string hash=StateHash();
  try{
   // S4Camera projection uses native index 0 as W, and 1/2/3 as XYZ.
   SetMatrix(Identity());NativeDrag(MouseButtons.Left,24,17);var three=(double[,])Reflect.Get(Reflect.Get(Camera(),"Trans"),"M");
   Assert(Math.Abs(three[3,1])+Math.Abs(three[3,2])>.005,"Original left drag did not rotate the spatial coordinates");
   for(int i=0;i<4;i++)Assert(Math.Abs(three[0,i]-(i==0?1:0))<1e-10,"Left 3D drag changed the W coordinate");
   ((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).NotifyCameraInteraction();Assert(((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).RenderFrame(),"3D input did not produce real native render");
   SetMatrix(Identity());NativeDrag(MouseButtons.Right,0,28);var four=(double[,])Reflect.Get(Reflect.Get(Camera(),"Trans"),"M");
   Assert(Math.Abs(four[0,3])>.005,"Original right vertical drag did not rotate the 4D WZ plane");
   for(int i=0;i<4;i++)Assert(Math.Abs(four[1,i]-(i==1?1:0))<1e-10&&Math.Abs(four[2,i]-(i==2?1:0))<1e-10,"Vertical 4D drag changed the unrelated XY coordinates");
   ((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).NotifyCameraInteraction();Assert(((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).RenderFrame(),"4D input did not produce real native render");
   Assert(StateHash()==hash&&Pending()==null&&Convert.ToInt32(Reflect.Get(Reflect.Get(wb,"puz"),"Ptr"))==0,"Camera drag was journaled as a twist");
   Check("Original Form1 mouse handlers perform 3D left drag and 4D right vertical drag, with native index 0=W, without mutating labelled state");
  }finally{SetMatrix(saved);((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).NotifyCameraInteraction();}
 }
 static bool KeyMessage(Control control,Keys key,bool repeat){var msg=Message.Create(control.Handle,0x100,(IntPtr)(int)key,(IntPtr)(repeat?1L<<30:0));return wb.PreFilterMessage(ref msg);}
 static async Task KeyWorkflow(){
  form.Activate();viewport.Focus();await Task.Delay(80);Assert(Form.ActiveForm==form&&Control.ModifierKeys==Keys.None,"Native key tests require the active form and released modifiers");
  var current=(Dictionary<Keys,string>)Reflect.Get(wb,"keymap");var saved=new Dictionary<string,object>();foreach(var pair in current)saved[pair.Key.ToString().Replace(", ","+")]=pair.Value;
  try{
   Reflect.Call(wb,"SetKeys",LocalApi.D("P","grip:0","Enter","commit","U","undo","R","redo","F9","checkpoint"));string hash=StateHash();int count=LocalApi.Array(State()["checkpoints"]).Length;
   var text=(TextBox)Reflect.Get(wb,"macroA");Assert(!KeyMessage(text,Keys.P,false)&&!KeyMessage(text,Keys.Enter,false),"Text editing swallowed ordinary input");Assert(KeyMessage(text,Keys.F9,false),"Editing did not suppress original F-key actions");
   Assert(!KeyMessage(text,Keys.F10,false)&&!KeyMessage(viewport,Keys.F10,false),"Standard F10 menu access was swallowed by legacy macro protection");
   Assert(Pending()==null&&StateHash()==hash&&LocalApi.Array(State()["checkpoints"]).Length==count&&!Convert.ToBoolean(Reflect.Get(wb,"busy")),"Editing triggered a puzzle action");
   Assert(KeyMessage(viewport,Keys.P,true)&&Pending()==null&&!Convert.ToBoolean(Reflect.Get(wb,"busy")),"Repeated key fired another grip");
   Assert(KeyMessage(viewport,Keys.P,false),"Remapped grip key was not handled");await WaitIdle();Assert(Pending()!=null&&StateHash()==hash,"Remapped key did not create a safe preview");string post=Convert.ToString(Pending()["post_state"]);
   Assert(KeyMessage(viewport,Keys.Enter,false),"Enter commit key was not handled");await WaitIdle();Assert(StateHash()==post,"Key commit did not apply preview");
   Assert(KeyMessage(viewport,Keys.U,false),"Remapped undo key was not handled");await WaitIdle();Assert(StateHash()==hash,"Remapped key undo did not recover labels");
   Assert(KeyMessage(viewport,Keys.R,false),"Remapped redo key was not handled");await WaitIdle();Assert(StateHash()==post,"Remapped key redo lost labels");await Native("Command","undo");
   bool rejected=false;try{Reflect.Call(wb,"SetKeys",LocalApi.D("P","unknown"));}catch(TargetInvocationException){rejected=true;}Assert(rejected&&current.ContainsKey(Keys.P)&&current[Keys.P]=="grip:0","Invalid binding partially changed keymap");
   Check("Real WinForms key messages exercise remap, preview/commit/undo/redo, repeat suppression and text editing/F-key suppression");
  }finally{Reflect.Call(wb,"SetKeys",saved);}
 }
 static void SameCamera(Dictionary<string,object> expected){
  var actual=LocalApi.AsDict(Reflect.Call(wb,"CaptureCamera"));var a=LocalApi.Array(actual["matrix"] is double[]?api.Parse(api.Json(actual["matrix"])):actual["matrix"]);var b=LocalApi.Array(api.Parse(api.Json(expected["matrix"])));
  for(int i=0;i<16;i++)Assert(Math.Abs(Convert.ToDouble(a[i])-Convert.ToDouble(b[i]))<1e-10,"Checkpoint camera matrix mismatch "+i);
  foreach(string key in new[]{"radius","angle","cell","face_shrink","sticker_shrink"})Assert(Math.Abs(Convert.ToDouble(actual[key])-Convert.ToDouble(expected[key]))<1e-10,"Checkpoint camera setting mismatch: "+key);
 }
 static async Task CheckpointWorkflow(){
  await Apply("O27");await Backend("prefs",LocalApi.D("protected",new[]{33}));((NumericUpDown)Reflect.Get(wb,"cell")).Value=13;Reflect.Call(wb,"Center");
  string hash=StateHash();var camera=LocalApi.AsDict(Reflect.Call(wb,"CaptureCamera"));await Native("SaveCheckpoint");string name=Convert.ToString(((ComboBox)Reflect.Get(wb,"checkpoint")).SelectedItem);Assert(name.StartsWith("Native "),"Native checkpoint was not saved");
  await Backend("prefs",LocalApi.D("protected",new int[0]));await Apply("O33");await Native("Grip",0,false);await Native("Commit");Assert(StateHash()!=hash,"Checkpoint mutation probe did not move state");
  ((NumericUpDown)Reflect.Get(wb,"cell")).Value=44;Reflect.Call(wb,"Center");((TextBox)Reflect.Get(wb,"filter")).Text="unfinished invalid draft &";await Native("RestoreCheckpoint",name);
  Assert(StateHash()==hash&&Pending()==null,"Checkpoint did not restore labels or clear pending");Assert(((TextBox)Reflect.Get(wb,"filter")).Text=="O27"&&((CheckedListBox)Reflect.Get(wb,"types")).GetItemChecked(27),"Checkpoint did not replace draft with restored applied filter");
  Assert(LocalApi.Array(Prefs()["protected"]).Length==1&&Convert.ToInt32(LocalApi.Array(Prefs()["protected"])[0])==33,"Checkpoint did not restore protection");SameCamera(camera);MatchAtomicSnapshot("Native checkpoint restored mechanics, camera and preferences");
  await Backend("prefs",LocalApi.D("protected",new int[0],"orbit",33));await Apply("active");Check("Checkpoint production restore callback restores full state, exact filter, protection, camera and focused cell over unapplied edits");
 }
 static Button FindButton(Control root,string caption){foreach(Control c in root.Controls){var b=c as Button;if(b!=null&&b.Text==caption)return b;var found=FindButton(c,caption);if(found!=null)return found;}return null;}
 static async Task ClickButton(string caption){var button=FindButton(form,caption);Assert(button!=null,"Missing native button: "+caption);for(Control c=button;c!=null;c=c.Parent){var page=c as TabPage;if(page!=null)((TabControl)page.Parent).SelectedTab=page;}button.PerformClick();await WaitIdle();}
 static int[] PendingWord(){var result=new List<int>();Assert(Pending()!=null,"No macro preview: "+StatusText());foreach(var value in LocalApi.Array(Pending()["recipe"])){var part=LocalApi.AsDict(value);Assert(Convert.ToString(part["kind"])=="word","Expected primitive macro witness");foreach(object move in LocalApi.Array(part["moves"]))result.Add(Convert.ToInt32(move));}return result.ToArray();}
 static void WordEquals(int[] expected,string name){var actual=PendingWord();Assert(actual.Length==expected.Length,name+" witness length");for(int i=0;i<actual.Length;i++)Assert(actual[i]==expected[i],name+" chronology or inverse differs at "+i);Assert(Convert.ToInt32(Pending()["primitive_count"])==expected.Length,name+" primitive counter");}
 static async Task MacroWorkflow(){
  string root=StateHash();((TextBox)Reflect.Get(wb,"macroA")).Text="H0";((TextBox)Reflect.Get(wb,"macroB")).Text="T1";
  await ClickButton("[A, B]");WordEquals(new[]{1,4,-1,-4},"Native commutator");await ClickButton("A B A^-1");WordEquals(new[]{1,4,-1},"Native conjugate");await ClickButton("A^-1");WordEquals(new[]{-1},"Native inverse");
  await ClickButton("A B");WordEquals(new[]{1,4},"Native concatenate");string cert=Path.Combine(output,"native-export-certificate.json");await Native("ExportToFile","certificate",cert);Assert(File.Exists(cert),"Native certificate export did not create a file");SameBytes(File.ReadAllBytes(cert),api.Bytes("certificate"),"Native certificate export changed response bytes");await ClickButton("Preview to A");Assert(((TextBox)Reflect.Get(wb,"macroA")).Text.StartsWith("Certified preview:"),"Preview-to-A lost certified recipe");await ClickButton("A^-1");WordEquals(new[]{-4,-1},"Certified recipe inverse");
  ((TextBox)Reflect.Get(wb,"macroName")).Text="Native regression macro";string expected=Convert.ToString(Pending()["post_state"]);await ClickButton("Save preview macro");Assert(((ListBox)Reflect.Get(wb,"macroLibrary")).Items.Contains("Native regression macro"),"Saved macro missing from native library");
  await Native("Cancel");((ListBox)Reflect.Get(wb,"macroLibrary")).SelectedItem="Native regression macro";await ClickButton("Preview saved");WordEquals(new[]{-4,-1},"Saved macro");await Native("Commit");Assert(StateHash()==expected,"Saved native macro commit differs from certificate");await ClickButton("Inverse saved");await Native("Commit");Assert(StateHash()==root,"Inverse saved macro did not recover all labels");
  ((TextBox)Reflect.Get(wb,"macroA")).Text="H900";await ClickButton("A^-1");Assert(Pending()==null&&StateHash()==root&&StatusText().StartsWith("Invalid macro word:"),"Invalid native macro did not preserve state and error");string absent=Path.Combine(output,"must-not-be-created.json");await Native("ExportToFile","certificate",absent);Assert(!File.Exists(absent)&&StateHash()==root,"Certificate export without pending preview created a partial file");string sentinel=Path.Combine(output,"export-error-preserves-existing.txt");File.WriteAllText(sentinel,"KEEP");await Native("ExportToFile","certificate",sentinel);Assert(File.ReadAllText(sentinel)=="KEEP","Failed certificate export truncated an existing file");
  await Native("History");var history=(ListView)Reflect.Get(wb,"history");Assert(history.Items.Count>=2,"Native history omitted macro transactions");long head=Convert.ToInt64(State()["head"]);ListViewItem ancestor=null;foreach(ListViewItem item in history.Items)if(Convert.ToInt64(item.Tag)<head){ancestor=item;break;}Assert(ancestor!=null,"No history ancestor for native checkout");
  ancestor.Selected=true;ancestor.Focused=true;await ClickButton("Checkout");Assert(Convert.ToInt64(State()["head"])==Convert.ToInt64(ancestor.Tag),"Native history selection did not checkout selected head");await Native("RestoreCheckpoint","Solved root");Assert(StateHash()==initialHash,"Restore solved root after macro branch did not recover labels");await Apply("active");
  Check("Native macro composition, certified preview reuse, library save/preview/inverse, invalid input and history checkout retain finite witnesses and reversible full state");
 }
 static async Task UtilitiesAndInsertion(){
  await Backend("prefs",LocalApi.D("orbit",33,"protected",new int[0]));await Apply("active");((CheckBox)Reflect.Get(wb,"autoTarget")).Checked=true;
  await Native("Analyze");var analysis=LocalApi.AsDict(api.Parse(((TextBox)Reflect.Get(wb,"bufferText")).Text));Assert(LocalApi.Array(analysis["candidates"]).Length>0&&Convert.ToInt32(analysis["orbit"])==33,"Solved buffer analyser produced no native candidate list");
  ((ComboBox)Reflect.Get(wb,"node")).SelectedItem="0";Assert(((ComboBox)Reflect.Get(wb,"node")).Text=="0","Native analyser lacks retained root node");await Native("PreviewStar",1);Assert(Pending()!=null&&Convert.ToInt32(Pending()["star_count"])==1,"Manual native star preview missing witness");await Native("Commit");string moved=StateHash();Assert(moved!=initialHash,"Retained native star fixture did not move full state");
  await Native("Analyze");analysis=LocalApi.AsDict(api.Parse(((TextBox)Reflect.Get(wb,"bufferText")).Text));int destination=Convert.ToInt32(analysis["target"]);Assert(Convert.ToString(analysis["target_selection"])=="automatic-unfinished","Automatic analyser chose no unfinished destination");
  ((NumericUpDown)Reflect.Get(wb,"piecePosition")).Value=destination;await ClickButton("Inspect / select");var piece=LocalApi.AsDict(Reflect.Get(wb,"inspectedPiece"));int identity=Convert.ToInt32(piece["piece"]);Assert(Convert.ToInt32(piece["position"])==destination&&((TextBox)Reflect.Get(wb,"pieceText")).Text.Contains("Identity P"+identity),"Native inspector lost selected position/identity");
  await Backend("preview",LocalApi.D("recipe",new object[]{LocalApi.D("kind","star","orbit",33,"node",0,"sign",1)},"note","Tracking identity through an actual second legal star"));await Native("Commit");await ClickButton("Track identity");piece=LocalApi.AsDict(Reflect.Get(wb,"inspectedPiece"));Assert(Convert.ToInt32(piece["piece"])==identity&&Convert.ToInt32(piece["position"])!=destination,"Track identity did not follow the moved physical piece");
  await Native("Command","undo");await ClickButton("Track identity");Assert(StateHash()==moved&&Convert.ToInt32(LocalApi.AsDict(Reflect.Get(wb,"inspectedPiece"))["position"])==destination,"Tracking did not recover identity location after undo");
  await ClickButton("Use destination");Assert(!((CheckBox)Reflect.Get(wb,"autoTarget")).Checked&&Convert.ToInt32(((NumericUpDown)Reflect.Get(wb,"target")).Value)==destination,"Piece destination utility did not configure fixed insertion");
  await ClickButton("Track identity");Assert(Convert.ToInt32(LocalApi.AsDict(Reflect.Get(wb,"inspectedPiece"))["piece"])==identity,"Tracking changed physical identity");
  await ClickButton("Find required piece");piece=LocalApi.AsDict(Reflect.Get(wb,"inspectedPiece"));Assert(Convert.ToInt32(piece["piece"])==destination,"Find required returned wrong identity");
  await ClickButton("Center current");Assert(Convert.ToInt32(((NumericUpDown)Reflect.Get(wb,"cell")).Value)==Convert.ToInt32(LocalApi.Array(piece["current_cells"])[0]),"Center current did not focus the current cell");await ClickButton("Center home");Assert(Convert.ToInt32(((NumericUpDown)Reflect.Get(wb,"cell")).Value)==Convert.ToInt32(LocalApi.Array(piece["home_cells"])[0]),"Center home did not focus identity home");
  await ClickButton("Next unsolved");Assert(!Convert.ToBoolean(LocalApi.AsDict(Reflect.Get(wb,"inspectedPiece"))["solved"]),"Next unsolved selected a solved piece");await ClickButton("Previous unsolved");Assert(!Convert.ToBoolean(LocalApi.AsDict(Reflect.Get(wb,"inspectedPiece"))["solved"]),"Previous unsolved selected a solved piece");
  ((TextBox)Reflect.Get(wb,"setName")).Text="NativePhysical";((TextBox)Reflect.Get(wb,"setExpression")).Text="selected";((ComboBox)Reflect.Get(wb,"setKind")).SelectedItem="identity";await ClickButton("Save set");Assert(LocalApi.AsDict(Prefs()["named_sets"]).ContainsKey("NativePhysical"),"Native named set was not saved");await Apply("set(NativePhysical)");Assert(Visible()>0&&Visible()<=6,"Selected named set does not display one whole piece");await Apply("active");
  ((CheckBox)Reflect.Get(wb,"autoTarget")).Checked=true;await Native("Suggest");Assert(Pending()!=null&&Convert.ToInt32(Pending()["star_count"])>=1,"Native automatic insertion produced no certified preview");string token=Convert.ToString(Pending()["token"]);
  ((CheckBox)Reflect.Get(wb,"autoTarget")).Checked=false;((NumericUpDown)Reflect.Get(wb,"target")).Value=Convert.ToDecimal(LocalApi.AsDict(analysis["A"])["position"]);await Native("Suggest");Assert(Pending()!=null&&Convert.ToString(Pending()["token"])==token&&StateHash()==moved&&StatusText().ToLowerInvariant().Contains("buffer"),"Invalid fixed buffer target replaced prior preview or hid error");
  ((CheckBox)Reflect.Get(wb,"autoTarget")).Checked=true;await Native("Commit");var progress=(ListView)Reflect.Get(wb,"progress");Assert(progress.Items.Count==35,"Native progress table does not contain all 35 orbits");
  foreach(object row in LocalApi.Array(State()["progress"])){var r=LocalApi.AsDict(row);int o=Convert.ToInt32(r["orbit"]);Assert(progress.Items[o].SubItems[1].Text==r["solved"]+" / "+r["pieces"]&&progress.Items[o].SubItems[2].Text==r["position_wrong"].ToString()&&progress.Items[o].SubItems[3].Text==r["orientation_wrong"].ToString(),"Native progress counters differ at orbit "+o);if(o==33)Assert(Convert.ToInt32(r["solved"])==Convert.ToInt32(r["pieces"]),"Native insertion did not solve target orbit");}
  await Native("Grip",0,false);Assert(Pending()!=null,"Solved-orbit replacement probe lacks earlier preview");await Native("Suggest");Assert(Pending()==null&&StatusText().Contains("no insertion preview is pending"),"Solved orbit left unrelated preview available to Apply");
  await Native("SessionTimer","start");await Task.Delay(180);await Native("SessionTimer","pause");await Native("ReportSession");string report=((TextBox)Reflect.Get(wb,"sessionReport")).Text;var stats=api.Get("stats");Assert(report.Contains("Scramble: "+stats["scramble_primitives"]+" primitive turns")&&report.Contains("Solution: "+stats["solution_primitives"]+" primitive turns")&&report.Contains("Assisted transactions: "+stats["assisted_transactions"])&&report.Contains("(paused)")&&Convert.ToDouble(LocalApi.AsDict(stats["timer"])["seconds"])>0,"Native session report or timer differs from backend statistics");
  MatchAtomicSnapshot("Native buffer/insertion/utilities and reports");Check("Native buffer analysis, manual star, identity utilities, named set, automatic insertion, invalid fixed target, solved-orbit preview clearing, 35-orbit progress and timed session statistics");
  string proof=Path.Combine(output,"native-export-session.c600.json.gz");await Native("ExportToFile","export",proof);Assert(File.Exists(proof)&&File.ReadAllBytes(proof).Length>100,"Native proof export did not write a compressed proof session");Check("Native certificate and proof exports write complete files, while missing-preview export preserves absent and existing destinations");
  await Native("RestoreCheckpoint","Solved root");await Apply("active");Assert(StateHash()==initialHash,"Comprehensive workflow did not finish at solved root");
 }
 static void OwnedDialog(string title,Action<Form> exercise,Action open){
  Exception failure=null;bool visited=false;DateTime until=DateTime.UtcNow.AddSeconds(15);
  using(var trigger=new System.Windows.Forms.Timer{Interval=80}){
   trigger.Tick+=delegate{
    Form dialog=null;foreach(Form candidate in Application.OpenForms)if(candidate!=form&&candidate.Text==title){dialog=candidate;break;}
    if(dialog==null){if(DateTime.UtcNow>until){failure=new TimeoutException("Native dialog did not open: "+title);trigger.Stop();}return;}
    trigger.Stop();visited=true;try{exercise(dialog);}catch(Exception error){failure=error;}finally{if(!dialog.IsDisposed)dialog.Close();}
   };
   trigger.Start();open();trigger.Stop();
  }
  Assert(visited,"Native modal was not exercised: "+title);if(failure!=null)throw new InvalidOperationException("Native dialog regression: "+title,failure);
 }
 static void Descendants<T>(Control root,List<T> result) where T:Control {foreach(Control c in root.Controls){var item=c as T;if(item!=null)result.Add(item);Descendants<T>(c,result);}}
 static ToolStripMenuItem Menu(ToolStripItemCollection items,string caption){foreach(ToolStripItem item in items){var m=item as ToolStripMenuItem;if(m==null)continue;if(m.Text==caption)return m;var nested=Menu(m.DropDownItems,caption);if(nested!=null)return nested;}return null;}
 static void CheckDisplayValue(int index){
  object cube=Reflect.Get(wb,"cube"),scene=Reflect.Property(viewport,"Scene");
  if(index==0)Assert(Math.Abs(Convert.ToDouble(Reflect.Get(cube,"FShr"))-Convert.ToDouble(Reflect.Property(form,"CPShrinkFace")))<1e-9,"Cell shrink slider did not change native geometry");
  if(index==1)Assert(Math.Abs(Convert.ToDouble(Reflect.Get(cube,"SShr"))-Convert.ToDouble(Reflect.Property(form,"CPStickerSize")))<1e-9,"Sticker size slider did not change native geometry");
  if(index==2)Assert(Math.Abs(Convert.ToDouble(Reflect.Property(Camera(),"Angle"))-(double)(float)Convert.ToDouble(Reflect.Property(form,"CPViewAngle")))<1e-7,"View-angle slider did not change actual camera");
  if(index==3)Assert(Convert.ToInt32(Reflect.Get(form,"TRate"))==(int)Convert.ToDouble(Reflect.Property(form,"CPUndoSpeed"))&&Convert.ToInt32(Reflect.Get(wb,"animatedTurnRate"))==Convert.ToInt32(Reflect.Get(form,"TRate")),"Animation slider did not update original turn rate");
  if(index>=4){string light=index==4?"Diffuse":index==5?"Specular":"Ambient",setting=index==4?"CPDiffLight":index==5?"CPSpecLight":"CPAmbLight";Assert(Convert.ToInt32(Reflect.Property(Reflect.Property(scene,"Light"),light))==Convert.ToInt32(Reflect.Property(form,setting)),"Native "+light+" slider did not change actual light");}
 }
 static void RecordDisplayState(string name){
  var scene=Reflect.Property(viewport,"Scene");var light=Reflect.Property(scene,"Light");var sliders=new Dictionary<string,object>();foreach(string key in new[]{"trk_faceShrink","trk_StickerSize","trk_ViewAngle","trk_UndoSpeed","trk_LightDiff","trk_LightSpec","trk_LightAmb"}){var slider=(TrackBar)Reflect.Get(form,key);sliders[key]=LocalApi.D("value",slider.Value,"minimum",slider.Minimum,"maximum",slider.Maximum);}
  var data=LocalApi.D("sliders",sliders,"camera",Reflect.Call(wb,"CaptureCamera"),"light_diffuse",Reflect.Property(light,"Diffuse"),"light_specular",Reflect.Property(light,"Specular"),"light_ambient",Reflect.Property(light,"Ambient"),"light_enable",Reflect.Property(light,"Enable"),"fill_mode",Reflect.Property(scene,"RenderFillMode").ToString(),"m_setgeom",Reflect.Get(form,"m_setgeom"),"state_hash",StateHash());
  File.WriteAllText(Path.Combine(output,name+".json"),api.Json(data));Reflect.Call(viewport,"SetSceneChanged");Reflect.Call(viewport,"ForceUpdate");Assert(Convert.ToInt32(Reflect.Call(scene,"FrameMove"))>=0,"Display diagnostic could not prepare frame");lock(viewport)using(((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).Subset.Enter())Assert(Convert.ToInt32(Reflect.Call(scene,"Render"))>=0,"Display diagnostic could not render");var device=(Device)Reflect.Property(scene,"Renderer");using(var surface=device.GetRenderTarget(0))SurfaceLoader.Save(Path.Combine(output,name+".png"),ImageFileFormat.Png,surface);
 }
 static void MatchShownCoordinates(){
  var slots=((NativeStickerAccess)Reflect.Get(wb,"stickerAccess")).Slots;var styles=Styles();double fs=Convert.ToDouble(Reflect.Get(Reflect.Get(wb,"cube"),"FShr")),ss=Convert.ToDouble(Reflect.Get(Reflect.Get(wb,"cube"),"SShr"));int tested=0;
  for(int i=0;i<slots.Length;i++)if(styles[i]!=0){var actual=(float[])Reflect.Get(slots[i],"Coords");var copy=Reflect.Clone(slots[i]);Reflect.Set(copy,"Coords",(float[])actual.Clone());Reflect.Call(copy,"SetCoord",fs,ss);var expected=(float[])Reflect.Get(copy,"Coords");for(int j=0;j<actual.Length;j++)Assert(actual[j].Equals(expected[j]),"Visible native mesh retains old shrink coordinates at slot "+i+" coordinate "+j);tested++;}
  Assert(tested>0,"Shrink verification did not cover visible geometry");Check("All "+tested+" visible native meshes use the current shrink after hide/show and checkpoint restore");
 }
 static async Task DisplayWorkflow(){
  RecordDisplayState("display-before");
  Capture("display-startup");
  string hash=StateHash();string[] names={"trk_faceShrink","trk_StickerSize","trk_ViewAngle","trk_UndoSpeed","trk_LightDiff","trk_LightSpec","trk_LightAmb"};var original=new TrackBar[7];var values=new int[7];for(int i=0;i<7;i++){original[i]=(TrackBar)Reflect.Get(form,names[i]);values[i]=original[i].Value;}
  var instant=Menu(form.MainMenuStrip.Items,"Instant turns (faster on integrated graphics)");Assert(instant!=null&&instant.Checked,"Instant-turn menu was not available");
  Exception displayFailure=null;try{
   OwnedDialog("MPUlt display controls",delegate(Form dialog){var controls=new List<TrackBar>();Descendants(dialog,controls);Assert(controls.Count==7,"Display dialog omitted sliders");Assert(!controls[3].Enabled,"Animation speed must be disabled while instant turns are enabled");for(int i=0;i<7;i++)if(i!=3){var slider=controls[i];slider.Value=slider.Value<slider.Maximum?slider.Value+1:slider.Value-1;Assert(original[i].Value==slider.Value,"Display proxy updated wrong original slider "+i);CheckDisplayValue(i);}},delegate{Reflect.Call(wb,"OriginalViewControls");});
   await WaitIdle();
   var camera=LocalApi.AsDict(Reflect.Call(wb,"CaptureCamera"));int face=original[0].Value,sticker=original[1].Value,angle=original[2].Value;await Native("SaveCheckpoint");string checkpointName=Convert.ToString(((ComboBox)Reflect.Get(wb,"checkpoint")).SelectedItem);await Apply("nothing");await Apply("active");MatchShownCoordinates();
   OwnedDialog("MPUlt display controls",delegate(Form dialog){var controls=new List<TrackBar>();Descendants(dialog,controls);for(int i=0;i<7;i++)if(i!=3)controls[i].Value=values[i];},delegate{Reflect.Call(wb,"OriginalViewControls");});
   await WaitIdle();
   await Native("RestoreCheckpoint",checkpointName);SameCamera(camera);Assert(original[0].Value==face&&original[1].Value==sticker&&original[2].Value==angle,"Checkpoint restored camera but left original display slider values stale");MatchShownCoordinates();
   instant.PerformClick();Assert(!Convert.ToBoolean(Reflect.Get(wb,"instantTurns")),"Instant-turn toggle did not enable animated settings");
   OwnedDialog("MPUlt display controls",delegate(Form dialog){var controls=new List<TrackBar>();Descendants(dialog,controls);var slider=controls[3];Assert(slider.Enabled,"Animation slider stayed disabled after instant mode was switched off");slider.Value=slider.Value<slider.Maximum?slider.Value+1:slider.Value-1;CheckDisplayValue(3);slider.Value=values[3];CheckDisplayValue(3);},delegate{Reflect.Call(wb,"OriginalViewControls");});
   await WaitIdle();
  }catch(Exception error){displayFailure=error;}
  await WaitIdle();if(!instant.Checked)instant.PerformClick();OwnedDialog("MPUlt display controls",delegate(Form dialog){var controls=new List<TrackBar>();Descendants(dialog,controls);for(int i=0;i<7;i++)if(i!=3)controls[i].Value=values[i];},delegate{Reflect.Call(wb,"OriginalViewControls");});await WaitIdle();if(displayFailure!=null)throw displayFailure;
  RecordDisplayState("display-after");Assert(StateHash()==hash&&Convert.ToInt32(Reflect.Get(form,"TRate"))==1,"Display controls changed puzzle state or failed to restore instant turns");SameBytes(Pixels(Path.Combine(output,"display-after.png")),Pixels(Path.Combine(output,"display-before.png")),"Display controls restored values but changed actual native pixels");Assert(((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).RenderFrame(),"Real DirectX renderer failed after display slider changes");Check("Actual native display modal forwards all seven sliders to geometry, camera, turn rate and lights, and restores identical pixels and instant-turn mode without changing state");
 }
 static void KeyPersistenceWorkflow(){
  string path=Convert.ToString(Reflect.Property(wb,"KeyPath"));byte[] prior=File.Exists(path)?File.ReadAllBytes(path):null;var saved=new Dictionary<string,object>();foreach(var pair in (Dictionary<Keys,string>)Reflect.Get(wb,"keymap"))saved[pair.Key.ToString().Replace(", ","+")]=pair.Value;
  try{
   OwnedDialog("Native keybindings",delegate(Form dialog){var edits=new List<TextBox>();Descendants(dialog,edits);Assert(edits.Count==1,"Native key editor textbox missing");edits[0].Text=api.Json(LocalApi.D("P","grip:0","Enter","commit","Escape","cancel"));var save=FindButton(dialog,"Save bindings");Assert(save!=null,"Native key editor save button missing");save.PerformClick();},delegate{Reflect.Call(wb,"EditKeys");});
   Assert(File.Exists(path)&&LocalApi.AsDict(api.Parse(File.ReadAllText(path))).ContainsKey("P"),"Native key editor did not persist actual file");File.Copy(path,Path.Combine(output,"native-keys-saved.json"),true);Reflect.Call(wb,"SetKeys",LocalApi.D("Q","undo"));Reflect.Call(wb,"InitializeKeys");var map=(Dictionary<Keys,string>)Reflect.Get(wb,"keymap");Assert(map.Count==3&&map.ContainsKey(Keys.P)&&map[Keys.P]=="grip:0"&&!map.ContainsKey(Keys.Q),"Production startup key loader did not reload persisted mapping");
   File.WriteAllText(path,"{broken");Reflect.Call(wb,"InitializeKeys");Assert(map.ContainsKey(Keys.N)&&map[Keys.N]=="suggest"&&File.ReadAllText(path)=="{broken","Malformed saved keys did not recover default bindings and preserve the file for diagnosis");
   Check("Actual native key editor saves bindings to disk; production startup loader reloads them and recovers defaults from malformed JSON");
  }finally{if(prior!=null)File.WriteAllBytes(path,prior);else if(File.Exists(path))File.Delete(path);Reflect.Call(wb,"SetKeys",saved);}
 }
 static async void Comprehensive(){
  try{await FilterWorkflow();CameraWorkflow();await KeyWorkflow();await CheckpointWorkflow();await MacroWorkflow();await UtilitiesAndInsertion();await DisplayWorkflow();await NativeSessionLogRegression.Run(wb,form,viewport,api,output,Check);await NativePickingRegression.Run(wb,form,viewport,api,output,Check);await NativeFramePolicyRegression.Run(wb,form,viewport,api,output,Check);KeyPersistenceWorkflow();comprehensiveComplete=true;Capture("17-comprehensive-final");}
  catch(Exception error){failures.Add(error.ToString());NativeDiagnostics.Write("DIRECTX COMPREHENSIVE FAILED",error);}
  if(comprehensiveComplete){try{await CloseThroughMessageWhenActive();}catch(Exception error){failures.Add(error.ToString());if(!form.IsDisposed)form.Close();}}
  else form.Close();
 }
 static async void DisplayOnly(){try{initialHash=StateHash();await DisplayWorkflow();KeyPersistenceWorkflow();comprehensiveComplete=true;Capture("display-controls-final");await CloseThroughMessageWhenActive();}catch(Exception error){failures.Add(error.ToString());NativeDiagnostics.Write("DIRECTX DISPLAY REGRESSION FAILED",error);if(!form.IsDisposed)form.Close();}}
 static async void PickingOnly(){try{initialHash=StateHash();await NativePickingRegression.Run(wb,form,viewport,api,output,Check);Assert(StateHash()==initialHash,"Picking regression did not restore initial full state");comprehensiveComplete=true;Capture("picking-final");await CloseThroughMessageWhenActive();}catch(Exception error){failures.Add(error.ToString());NativeDiagnostics.Write("DIRECTX PICKING REGRESSION FAILED",error);if(!form.IsDisposed)form.Close();}}
 static async void FeaturesOnly(){try{initialHash=StateHash();await NativeFeatureRegression.Run(wb,form,viewport,api,output,Check);comprehensiveComplete=true;Capture("features-final");await CloseThroughMessageWhenActive();}catch(Exception error){failures.Add(error.ToString());NativeDiagnostics.Write("DIRECTX FEATURE REGRESSION FAILED",error);if(!form.IsDisposed)form.Close();}}
 static async void FrameOnly(){try{initialHash=StateHash();if(frameRestartOnly)await NativeFramePolicyRegression.Reopen(wb,form,viewport,api,output,Check);else await NativeFramePolicyRegression.Run(wb,form,viewport,api,output,Check);Assert(StateHash()==initialHash,"Frame regression did not restore initial full state");comprehensiveComplete=true;Capture("frame-policy-final");await CloseThroughMessageWhenActive();}catch(Exception error){failures.Add(error.ToString());NativeDiagnostics.Write("DIRECTX FRAME POLICY REGRESSION FAILED",error);if(!form.IsDisposed)form.Close();}}
 static async Task CloseThroughMessageWhenActive(){var limit=DateTime.UtcNow.AddSeconds(2);do{form.Activate();await Task.Delay(30);if(Form.ActiveForm==form){CloseThroughMessage();return;}}while(DateTime.UtcNow<limit);throw new InvalidOperationException("Alt+F4 fixture precondition failed: the test window did not become the active form within two seconds");}
 static void CloseThroughMessage(){form.Activate();var close=Message.Create(form.Handle,0x104,(IntPtr)(int)Keys.F4,(IntPtr)(1L<<29));Assert(wb.PreFilterMessage(ref close),"Native Alt+F4 context message was not handled");Assert(form.IsDisposed||!form.Visible,"Native Alt+F4 did not close the actual window");Check("Native WM_SYSKEYDOWN Alt+F4 context closes the actual host through its production message filter");}
 static void Tick(object sender,EventArgs e){
  try{
   if(DateTime.UtcNow>deadline)throw new TimeoutException("Real renderer test timed out at stage "+stage);
   if(stage==20&&!Convert.ToBoolean(Reflect.Get(wb,"busy")))Assert(Convert.ToBoolean(Reflect.Get(wb,"connected")),"Protected scramble rejection disconnected the native host");
   if(!Convert.ToBoolean(Reflect.Get(wb,"connected"))||Convert.ToBoolean(Reflect.Get(wb,"busy")))return;
   timer.Stop();
   if(keyRestartOnly&&stage==0){var map=(Dictionary<Keys,string>)Reflect.Get(wb,"keymap");Assert(map.Count==3&&map.ContainsKey(Keys.P)&&map[Keys.P]=="grip:0"&&map[Keys.Enter]=="commit"&&map[Keys.Escape]=="cancel","Fresh actual native process did not load the saved keybindings");Check("Fresh actual native process reloads the three persisted custom keybindings before connecting the live geometry bridge");stage=29;comprehensiveComplete=true;CloseThroughMessage();return;}
   if(displayKeysOnly&&stage==0){stage=29;DisplayOnly();return;}
   if(pickingOnly&&stage==0){stage=29;PickingOnly();return;}
   if(featuresOnly&&stage==0){stage=29;FeaturesOnly();return;}
   if((frameOnly||frameRestartOnly)&&stage==0){stage=29;FrameOnly();return;}
   switch(stage++){
    case 0:initialHash=StateHash();Capture("01-startup");Check("Actual 259800-slot / 1200-generator bridge connected");form.Size=new Size(1000,650);break;
    case 1:Capture("02-small-window");form.WindowState=FormWindowState.Maximized;break;
    case 2:Capture("03-maximized");form.WindowState=FormWindowState.Minimized;break;
    case 3:form.WindowState=FormWindowState.Normal;form.Size=new Size(1440,900);break;
    case 4:Capture("04-restored");Reflect.Call(wb,"ToggleTools");break;
    case 5:Capture("05-tools-hidden");Reflect.Call(wb,"ToggleTools");break;
    case 6:Capture("06-tools-restored");form.Hide();break;
    case 7:form.Show();break;
    case 8:Capture("07-show-restored");Reflect.Call(wb,"Center");break;
    case 9:Capture("08-recentered");Reflect.Call(wb,"Grip",0,false);break;
    case 10:Assert(StateHash()==initialHash,"Preview changed committed state");Assert(LocalApi.AsDict(Reflect.Get(wb,"state"))["pending"]!=null,"Grip produced no preview");Capture("09-preview");Reflect.Call(wb,"Commit");break;
    case 11:turnedHash=StateHash();Assert(turnedHash!=initialHash,"Commit did not change the full puzzle");Capture("10-committed");Reflect.Call(wb,"Command","undo");break;
    case 12:Assert(StateHash()==initialHash,"Undo did not recover every labelled slot");Capture("11-undone");Reflect.Call(wb,"Command","redo");break;
    case 13:Assert(StateHash()==turnedHash,"Redo did not restore the full committed state");Check("Native grip preview/commit/undo/redo full-state hashes");Reflect.Call(wb,"Command","undo");break;
    case 14:Assert(StateHash()==initialHash,"Test state not restored");((TextBox)Reflect.Get(wb,"filter")).Text="everything";Reflect.Call(wb,"ApplyFilter");break;
    case 15:Capture("12-all-pieces");((TextBox)Reflect.Get(wb,"filter")).Text="active";Reflect.Call(wb,"ApplyFilter");break;
    case 16:Capture("13-final");Assert(StateHash()==initialHash,"Rendering/filtering changed puzzle state");Check("Full geometry filter and restore preserve full-state hash");BeginVisibilityReconnect();break;
    case 17:CheckVisibilityReconnect();CheckMotionPicking();((TextBox)Reflect.Get(wb,"filter")).Text="active";Reflect.Call(wb,"ApplyFilter");break;
    case 18:CompareFilteredPixels();MatchAtomicSnapshot("Before scramble");Reflect.Call(wb,"Run",new Action(delegate{api.Post("prefs",LocalApi.D("protected",new[]{33}));}),new Action(delegate{Reflect.Call(wb,"RefreshFromServer");}),false);break;
    case 19:{
     var state=LocalApi.AsDict(Reflect.Get(wb,"state"));var protectedOrbits=LocalApi.Array(LocalApi.AsDict(state["prefs"])["protected"]);
     Assert(protectedOrbits.Length==1&&Convert.ToInt32(protectedOrbits[0])==33,"Protected scramble probe did not protect orbit 33");
     protectedHash=StateHash();protectedHead=Convert.ToInt64(state["head"]);Reflect.Call(wb,"Scramble");break;
    }
    case 20:{
     var state=LocalApi.AsDict(Reflect.Get(wb,"state"));string error=((ToolStripStatusLabel)Reflect.Get(wb,"message")).Text;
     Assert(StateHash()==protectedHash&&Convert.ToInt64(state["head"])==protectedHead&&state["pending"]==null,"Rejected protected scramble changed state, history, or pending work");
     Assert(Convert.ToBoolean(Reflect.Get(wb,"connected"))&&!Convert.ToBoolean(Reflect.Get(wb,"busy"))&&viewport.Enabled,"Protected rejection left native input disconnected or busy");
     Assert(error.StartsWith("Protected orbit would move.",StringComparison.Ordinal),"Snapshot refresh hid the protected-orbit error: "+error);
     MatchAtomicSnapshot("Rejected protected-orbit scramble");checks.Add(LocalApi.D("name","Protected orbit 33 rejects native scramble without changing state/head and preserves visible error after refresh","passed",true,"head",protectedHead,"state_hash",protectedHash,"status_text",error));
     Reflect.Call(wb,"Run",new Action(delegate{api.Post("prefs",LocalApi.D("protected",new int[0]));}),new Action(delegate{Reflect.Call(wb,"RefreshFromServer");}),false);break;
    }
    case 21:Assert(LocalApi.Array(LocalApi.AsDict(LocalApi.AsDict(Reflect.Get(wb,"state"))["prefs"])["protected"]).Length==0,"Protected orbit was not removed before ordinary scramble");Reflect.Call(wb,"Scramble");break;
    case 22:scrambledHash=StateHash();Assert(scrambledHash!=initialHash,"1000-turn scramble did not commit");Assert(LocalApi.AsDict(Reflect.Get(wb,"state"))["pending"]==null,"Scramble published an intermediate preview");MatchAtomicSnapshot("1000-turn scramble");Reflect.Call(wb,"Command","undo");break;
    case 23:Assert(StateHash()==initialHash,"Scramble undo lost labelled state");MatchAtomicSnapshot("Scramble undo");Reflect.Call(wb,"Command","redo");break;
    case 24:Assert(StateHash()==scrambledHash,"Scramble redo changed the recorded state");MatchAtomicSnapshot("Scramble redo");Reflect.Call(wb,"Command","undo");break;
    case 25:Assert(StateHash()==initialHash,"Native turn probe did not start at root");StartNativeTurn();break;
    case 26:Assert(StateHash()==nativeTurnHash,"Captured real native turn differs from the full H0 oracle certificate");MatchAtomicSnapshot("Captured real native turn");Reflect.Call(wb,"Command","undo");break;
    case 27:Assert(StateHash()==initialHash,"Real native turn undo did not recover root");MatchAtomicSnapshot("Final root after native turn undo");InjectDeviceLoss();break;
    case 28:CheckInjectedRecovery();Assert(StateHash()==initialHash,"Injected display recovery changed puzzle state");Capture("14-verified-final");Comprehensive();return;
   }
   timer.Start();
  }catch(Exception error){failures.Add(error.ToString());NativeDiagnostics.Write("DIRECTX TEST FAILED",error);timer.Stop();form.Close();}
 }
 [STAThread] static int Main(string[] args){
  if(args.Length!=4)return 2;
  output=Path.GetFullPath(args[3]);Directory.CreateDirectory(output);
  var started=DateTime.UtcNow;
  try{
   Program.UseEnglishUi();EnglishDialogContracts();
   string exe=Path.GetFullPath(args[0]);Directory.SetCurrentDirectory(Path.GetDirectoryName(exe));
   AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){string p=Path.Combine(Path.GetDirectoryName(exe),new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
   Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
   Application.ThreadException+=delegate(object sender,System.Threading.ThreadExceptionEventArgs e){failures.Add(e.Exception.ToString());NativeDiagnostics.Write("DIRECTX UI THREAD FAILED",e.Exception);if(form!=null)form.Close();};
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   form=(Form)Activator.CreateInstance(Assembly.LoadFrom(exe).GetType("_3dedit.Form1",true));
   wb=new NativeWorkbench(form,exe,args[1],args[2]);viewport=(Control)Reflect.Get(wb,"viewport");api=new LocalApi(args[1],args[2]);
   timer=new System.Windows.Forms.Timer{Interval=700};timer.Tick+=Tick;deadline=DateTime.UtcNow.AddMinutes(10);
   form.Shown+=delegate{timer.Start();};Application.Run(form);timer.Dispose();
   Assert(stage==29&&comprehensiveComplete,"Renderer regression stopped early at stage "+stage);
  }catch(Exception e){failures.Add(e.ToString());NativeDiagnostics.Write("DIRECTX REGRESSION FAILED",e);}
  var report=LocalApi.D("passed",failures.Count==0,"phase",featuresOnly?"focused-features":keyRestartOnly?"saved-key-restart":displayKeysOnly?"display-key-controls":pickingOnly?"visible-picking":frameRestartOnly?"frame-policy-restart":frameOnly?"frame-policy":"all-components","scope","Actual Windows WinForms and MPUlt Managed DirectX render targets; isolated journal", "started_utc",started.ToString("o"),"finished_utc",DateTime.UtcNow.ToString("o"),"process_bits",IntPtr.Size*8,"clr",Environment.Version.ToString(),"checks",checks,"failures",failures);
  File.WriteAllText(Path.Combine(output,"native-renderer-test.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(report));
  return failures.Count==0?0:1;
 }
}
