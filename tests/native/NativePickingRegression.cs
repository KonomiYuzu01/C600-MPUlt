// In-process Windows messages exercise the actual viewport HWND and its original
// MPUlt event handlers. No global keyboard/mouse injection or user session is used.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

internal static class NativePickingRegression {
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
 [DllImport("user32.dll")]static extern bool GetKeyboardState(byte[] state);
 [DllImport("user32.dll")]static extern bool SetKeyboardState(byte[] state);
 sealed class ThreadKeys : IDisposable {
  readonly byte[] prior=new byte[256];
  internal ThreadKeys(Keys modifiers,MouseButtons buttons){
   Assert(GetKeyboardState(prior),"Could not read test-thread key state");var state=(byte[])prior.Clone();
   foreach(int key in new[]{1,2,4,16,17,18,160,161,162,163,164,165})state[key]=0;
   if((modifiers&Keys.Shift)!=0)state[16]=state[160]=128;if((modifiers&Keys.Control)!=0)state[17]=state[162]=128;
   if((buttons&MouseButtons.Left)!=0)state[1]=128;if((buttons&MouseButtons.Right)!=0)state[2]=128;
   Assert(SetKeyboardState(state),"Could not establish controlled test-thread key state");
  }
  public void Dispose(){Assert(SetKeyboardState(prior),"Could not restore test-thread key state");}
 }
 static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
 static Dictionary<string,object> State(NativeWorkbench wb){return LocalApi.AsDict(Reflect.Get(wb,"state"));}
 static string Hash(NativeWorkbench wb){return Convert.ToString(State(wb)["state_hash"]);}
 static async Task Idle(NativeWorkbench wb){await Task.Delay(80);var limit=DateTime.UtcNow.AddSeconds(45);while(Convert.ToBoolean(Reflect.Get(wb,"busy"))){if(DateTime.UtcNow>limit)throw new TimeoutException("Native picking operation stayed busy");await Task.Delay(30);}Assert(Convert.ToBoolean(Reflect.Get(wb,"connected")),"Picking disconnected native host");}
 static async Task Apply(NativeWorkbench wb,string expression){((TextBox)Reflect.Get(wb,"filter")).Text=expression;Reflect.Call(wb,"ApplyFilter");await Idle(wb);}
 static IntPtr Position(Point point){return (IntPtr)((point.X&65535)|((point.Y&65535)<<16));}
 static void Click(Control viewport,Point point){SendMessage(viewport.Handle,0x200,IntPtr.Zero,Position(point));SendMessage(viewport.Handle,0x201,(IntPtr)1,Position(point));SendMessage(viewport.Handle,0x202,IntPtr.Zero,Position(point));}
 static void ChordedClick(Control viewport,Point point){SendMessage(viewport.Handle,0x200,IntPtr.Zero,Position(point));SendMessage(viewport.Handle,0x204,(IntPtr)2,Position(point));SendMessage(viewport.Handle,0x201,(IntPtr)3,Position(point));SendMessage(viewport.Handle,0x202,(IntPtr)2,Position(point));SendMessage(viewport.Handle,0x205,IntPtr.Zero,Position(point));}
 static Point FindPixel(NativeWorkbench wb,Control viewport,NativePickingVisibility picking,bool selectedOnly=false,HashSet<int> excluded=null){
  var slots=((NativeStickerAccess)Reflect.Get(wb,"stickerAccess")).Slots;var styles=(byte[])Reflect.Get(wb,"previousStyles");var scene=Reflect.Property(viewport,"Scene");var projection=(Matrix)Reflect.Get(scene,"matProj");
  var field=(short[])Reflect.Get(Reflect.Get(wb,"puz"),"Field");
  // Project original triangle centroids into native client coordinates, then
  // verify each rounded pixel through the actual current ray/visibility helper.
  for(int i=0;i<slots.Length;i++)if(styles[i]!=0&&(!selectedOnly||(field[i]&0x8000)!=0)){
   object basis=Reflect.Get(slots[i],"Base");var coords=(Vector3[])Reflect.Get(slots[i],"Coords3D");var faces=(int[])Reflect.Get(basis,"Faces");int count=Convert.ToInt32(Reflect.Get(basis,"NF"));
   for(int t=0;t<count;t++){
    Vector3 a=coords[faces[t*3]],b=coords[faces[t*3+1]],c=coords[faces[t*3+2]];
    if(a.Z==-1e10f||b.Z==-1e10f||c.Z==-1e10f)continue;
    double x=(a.X+b.X+c.X)/3.0,y=(a.Y+b.Y+c.Y)/3.0;
    var point=new Point((int)Math.Round((1-x*projection.M11)*viewport.ClientSize.Width/2),(int)Math.Round((1-y*projection.M22)*viewport.ClientSize.Height/2));
    int hit=picking.FindVisibleSticker(point.X,point.Y,selectedOnly);if(hit>=0&&(excluded==null||!excluded.Contains(hit)))return point;
   }
  }
  throw new InvalidOperationException("No actual visible "+(selectedOnly?"selected ":"")+"sticker pixel can be picked");
 }
 static int SaveFrame(NativeWorkbench wb,Control viewport,string path){
  var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");Assert(renderer.PreparePicking(),"Could not restore full detail before picking image");var scene=Reflect.Property(viewport,"Scene");var device=(Device)Reflect.Property(scene,"Renderer");
  // Deliberately call the original scene without an outer subset scope: native
  // ForceUpdate and animation use this entry point and must obey the policy.
  lock(viewport)Assert(Convert.ToInt32(Reflect.Call(scene,"Render"))>=0,"Filtered native rendering failed");
  using(var surface=device.GetRenderTarget(0))SurfaceLoader.Save(path,ImageFileFormat.Png,surface);
  int different=0;using(var image=new Bitmap(path)){int background=image.GetPixel(0,0).ToArgb();for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(image.GetPixel(x,y).ToArgb()!=background)different++;}return different;
 }
 static void ReleaseState(Form form){Assert(!Convert.ToBoolean(Reflect.Get(form,"qLeftDown"))&&!Convert.ToBoolean(Reflect.Get(form,"qRightDown")),"Native mouse button remained pressed");Assert(!Convert.ToBoolean(Reflect.Get(form,"qSkipClick")),"Rejected click left native skip flag set");}
 static double[,] CameraMatrix(Control viewport){return (double[,])Reflect.Get(Reflect.Get(Reflect.Property(Reflect.Property(viewport,"Scene"),"Camera"),"Trans"),"M");}
 static void Drag(Control viewport,bool right,Point start,Point end){int down=right?0x204:0x201,up=right?0x205:0x202,mask=right?2:1;SendMessage(viewport.Handle,0x200,IntPtr.Zero,Position(start));using(new ThreadKeys(Control.ModifierKeys,right?MouseButtons.Right:MouseButtons.Left)){SendMessage(viewport.Handle,down,(IntPtr)mask,Position(start));SendMessage(viewport.Handle,0x200,(IntPtr)mask,Position(end));SendMessage(viewport.Handle,up,IntPtr.Zero,Position(end));}}
 static string Selected(NativeWorkbench wb,LocalApi api){return api.Json(LocalApi.AsDict(State(wb)["prefs"])["inspection"]);}
 static bool SameMatrix(double[,] a,double[,] b){for(int i=0;i<4;i++)for(int j=0;j<4;j++)if(Math.Abs(a[i,j]-b[i,j])>1e-9)return false;return true;}
 static string LabelHash(byte[] labels){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(labels)).Replace("-","").ToLowerInvariant();}
 static async Task FullRayBenchmark(NativeWorkbench wb,Control viewport,LocalApi api,string output,Action<string> check){
  await Apply(wb,"everything");var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");var picking=(NativePickingVisibility)Reflect.Get(wb,"picking");Assert(renderer.Subset.VisibleCount==259800&&renderer.PreparePicking(),"Dense picking benchmark could not prepare all native geometry");
  Point hit=FindPixel(wb,viewport,picking),empty=new Point(1,1);Assert(picking.FindVisibleSticker(empty.X,empty.Y,false)<0,"Dense picking benchmark empty pixel is not empty");
  object cube=Reflect.Get(wb,"cube"),puzzle=Reflect.Get(wb,"puz");object slots=Reflect.Get(cube,"Stks"),faces=Reflect.Get(cube,"StFaces"),field=Reflect.Get(puzzle,"Field");var original=(short[])((short[])field).Clone();string hash=Hash(wb);long head=Convert.ToInt64(State(wb)["head"]);
  // Warm both a real visible pixel and an empty full-scan pixel. Timed work is
  // only the production ray query, after complete static projection, with no
  // rendering, HTTP, screenshots, or synthetic geometry in the timing interval.
  picking.FindVisibleSticker(hit.X,hit.Y,false);picking.FindVisibleSticker(empty.X,empty.Y,false);
  var timings=new double[8];var samples=new List<object>();double sum=0;
  for(int i=0;i<timings.Length;i++){
   Point point=i%2==0?hit:empty;var watch=Stopwatch.StartNew();int found=picking.FindVisibleSticker(point.X,point.Y,false);watch.Stop();timings[i]=watch.Elapsed.TotalMilliseconds;sum+=timings[i];
   Assert(i%2==0?found>=0:found<0,"Dense static ray changed its hit result");samples.Add(LocalApi.D("kind",i%2==0?"visible":"empty-full-scan","x",point.X,"y",point.Y,"native_hit",found,"milliseconds",timings[i]));
  }
  Assert(Object.ReferenceEquals(slots,Reflect.Get(cube,"Stks"))&&Object.ReferenceEquals(faces,Reflect.Get(cube,"StFaces"))&&Object.ReferenceEquals(field,Reflect.Get(puzzle,"Field"))&&Convert.ToInt32(Reflect.Get(cube,"NStk"))==259800,"Dense ray query changed native full-array ownership");
  var current=(short[])field;for(int i=0;i<current.Length;i++)Assert(current[i]==original[i],"Dense ray query changed a native field label");
  byte[] labels=api.Bytes("labels");Assert(labels.Length==259800*4&&LabelHash(labels)==hash&&Hash(wb)==hash&&Convert.ToInt64(State(wb)["head"])==head,"Dense ray query changed authoritative full labels or journal");
  Array.Sort(timings);double p95=timings[(int)Math.Ceiling(.95*timings.Length)-1],max=timings[timings.Length-1];bool responsive=max<=100;
  File.WriteAllText(Path.Combine(output,"native-full-picking-performance.json"),api.Json(LocalApi.D("passed",responsive,"scope","actual MPUlt cached full projection; production visible-only ray query","native_slots",259800,"queries",samples,"mean_ms",sum/timings.Length,"p95_ms",p95,"max_ms",max,"provisional_max_ms",100,"full_array_ownership_unchanged",true,"full_label_hash",hash,"camera_static",true)));
  NativeDiagnostics.Write("Dense visible-only picking: mean="+(sum/timings.Length).ToString("F3")+" ms; p95/max="+max.ToString("F3")+" ms; provisional 100 ms target="+responsive);
  Assert(responsive,"All-visible ray exceeded the provisional 100 ms response target: "+max.ToString("F3")+" ms; see native-full-picking-performance.json");
  check("Eight warmed production ray queries over all 259800 slots meet the 100 ms provisional response target with exact full-state and array ownership checks");
 }
 static async Task CheckInitialButtonDown(NativeWorkbench wb,Form form,Control viewport,LocalApi api,Action<string> check){
  var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");object scene=Reflect.Property(viewport,"Scene"),camera=Reflect.Property(scene,"Camera"),puzzle=Reflect.Get(wb,"puz");
  string before=Hash(wb);long head=Convert.ToInt64(State(wb)["head"]);var field=(short[])Reflect.Get(puzzle,"Field");var original=(short[])field.Clone();
  byte[] labels=await Task.Run(delegate{return api.Bytes("labels");});Assert(labels.Length==259800*4&&LabelHash(labels)==before,"Button-down probe has an incomplete initial labelled state");
  renderer.Pause();try{
   foreach(bool right in new[]{false,true})foreach(bool alreadyDirty in new[]{false,true}){
    Point point=new Point(40,40);int down=right?0x204:0x201,up=right?0x205:0x202,mask=right?2:1;
    using(new ThreadKeys(Keys.None,MouseButtons.None))SendMessage(viewport.Handle,0x200,IntPtr.Zero,Position(point));
    Assert(Convert.ToInt32(Reflect.Get(scene,"m_lastAction"))==-1,"No-button native move did not reset the previous mouse action");
    Assert(renderer.PreparePicking(),"Initial button-down probe could not prepare the native projection");
    Assert(!Convert.ToBoolean(Reflect.Property(scene,"SceneChanged")),"Initial button-down probe did not start from a clean rendered scene");
    if(alreadyDirty)Reflect.Call(viewport,"SetSceneChanged");
    var matrix=(double[,])CameraMatrix(viewport).Clone();double radius=Convert.ToDouble(Reflect.Get(camera,"R")),radius0=Convert.ToDouble(Reflect.Get(camera,"R0"));int frames=renderer.FramesRequested;
    try{
     using(new ThreadKeys(Keys.None,right?MouseButtons.Right:MouseButtons.Left))SendMessage(viewport.Handle,down,(IntPtr)mask,Position(point));
     Assert(Convert.ToBoolean(Reflect.Property(scene,"SceneChanged"))==alreadyDirty,alreadyDirty?"Initial mouse-down discarded a pre-existing redraw request":"Initial mouse-down dirtied the scene without camera movement");
     Assert(SameMatrix(matrix,CameraMatrix(viewport))&&radius==Convert.ToDouble(Reflect.Get(camera,"R"))&&radius0==Convert.ToDouble(Reflect.Get(camera,"R0")),"Initial mouse-down changed the native camera transform or radius");
     Assert(renderer.FramesRequested==frames,"Initial mouse-down submitted an unexpected frame");
    }finally{
     // Release through the original HWND bookkeeping while explicitly skipping
     // ProcessClick: this probe tests down transitions, not a synthetic twist.
     Reflect.Set(form,"qSkipClick",true);using(new ThreadKeys(Keys.None,MouseButtons.None))SendMessage(viewport.Handle,up,IntPtr.Zero,Position(point));
    }
    ReleaseState(form);
   }
  }finally{renderer.Resume();}
  await Idle(wb);Assert(Hash(wb)==before&&Convert.ToInt64(State(wb)["head"])==head,"Button-down probe changed the committed state or journal");
  Assert(Object.ReferenceEquals(field,Reflect.Get(puzzle,"Field")),"Button-down probe replaced the native field");for(int i=0;i<field.Length;i++)Assert(field[i]==original[i],"Button-down probe changed a native label/selection flag at "+i);
  byte[] after=await Task.Run(delegate{return api.Bytes("labels");});Assert(after.Length==259800*4&&LabelHash(after)==before,"Button-down probe changed complete authoritative labels");
  check("Real initial left/right HWND button-down preserves clean projection and camera, retains pre-existing dirtiness, and leaves all 259800 native and authoritative labels unchanged");
 }
 internal static async Task Run(NativeWorkbench wb,Form form,Control viewport,LocalApi api,string output,Action<string> check){
  var picking=(NativePickingVisibility)Reflect.Get(wb,"picking");Assert(picking!=null,"Native picking gate is not installed");var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");object cube=Reflect.Get(wb,"cube"),puzzle=Reflect.Get(wb,"puz");object fullSlots=Reflect.Get(cube,"Stks"),fullFaces=Reflect.Get(cube,"StFaces"),fullField=Reflect.Get(puzzle,"Field");
  Assert(Control.ModifierKeys==Keys.None,"Picking regression requires released physical modifiers");form.Activate();viewport.Focus();await Idle(wb);string before=Hash(wb);long head=Convert.ToInt64(State(wb)["head"]);
  double cameraR0=Convert.ToDouble(Reflect.Get(Reflect.Property(Reflect.Property(viewport,"Scene"),"Camera"),"R0"));NativeDiagnostics.Write("Native picking actual camera.R0="+cameraR0.ToString("R",System.Globalization.CultureInfo.InvariantCulture));File.WriteAllText(Path.Combine(output,"native-camera-radius.json"),api.Json(LocalApi.D("R0",cameraR0,"radius",Reflect.Get(Reflect.Property(Reflect.Property(viewport,"Scene"),"Camera"),"R"))));
  await Apply(wb,"O33");await CheckInitialButtonDown(wb,form,viewport,api,check);Assert(renderer.PreparePicking(),"Visible pick projection failed");Point pixel=FindPixel(wb,viewport,picking);int visibleHit=picking.FindVisibleSticker(pixel.X,pixel.Y,false);
  Assert(SaveFrame(wb,viewport,Path.Combine(output,"filtered-no-framework.png"))>0,"Filtered native render has no visible geometry");
  lock(viewport)using(renderer.Subset.Enter()){
   Assert(Convert.ToInt32(Reflect.Get(cube,"NF"))==600,"Framework suppression changed clipping-plane count");foreach(object face in (Array)Reflect.Get(cube,"StFaces"))Assert(Convert.ToInt32(Reflect.Get(Reflect.Get(face,"Base"),"NE"))==0,"Filtered render retains framework edges");
  }
  Assert(Object.ReferenceEquals(Reflect.Get(cube,"StFaces"),fullFaces),"Filtered draw did not restore native framework records");check("Filtered actual DirectX frame omits all 600 framework meshes while retaining native clipping and full mechanics");
  // Keep old Coords3D, hide every mesh, then click the exact formerly visible
  // pixel. This explicitly exercises stale projection rather than empty space.
  await Apply(wb,"nothing");var stale=(Vector3[])Reflect.Get(((Array)fullSlots).GetValue(visibleHit),"Coords3D");Assert(stale.Length>0,"Hidden stale projection fixture is missing");
  Assert(SaveFrame(wb,viewport,Path.Combine(output,"nothing-no-framework.png"))==0,"Nothing filter still draws hidden stickers or framework");int blocked=picking.BlockedClicks;Click(viewport,pixel);await Idle(wb);
  Assert(picking.BlockedClicks==blocked+1&&picking.LastHit<0&&Hash(wb)==before&&Convert.ToInt64(State(wb)["head"])==head,"Hidden cached geometry changed the journal or was accepted");ReleaseState(form);
  Assert(picking.FindVisibleSticker(pixel.X,pixel.Y,true)<0,"Selected-only pick accepted hidden cached geometry");
  check("Real viewport HWND rejects formerly visible pixels after hiding, including stale Coords3D, without native twist, selection, or journal change");
  await Apply(wb,"O33");Assert(renderer.PreparePicking(),"Restored visible pick projection failed");pixel=FindPixel(wb,viewport,picking);int accepted=picking.AcceptedClicks;ChordedClick(viewport,pixel);int committedAxis=Convert.ToInt32(Reflect.Get(form,"m_curAx")),committedTwist=Convert.ToInt32(Reflect.Get(form,"m_tw"));await Idle(wb);ReleaseState(form);
  Assert(picking.AcceptedClicks==accepted+1&&picking.LastHit>=0,"Visible native click was rejected");Assert(Object.ReferenceEquals(Reflect.Get(cube,"Stks"),fullSlots)&&Object.ReferenceEquals(Reflect.Get(cube,"StFaces"),fullFaces)&&Object.ReferenceEquals(Reflect.Get(puzzle,"Field"),fullField)&&Convert.ToInt32(Reflect.Get(cube,"NStk"))==259800,"Visible native click changed full-array ownership");
  bool twisted=Hash(wb)!=before;int clickStatus=Convert.ToInt32(Reflect.Get(form,"m_status"));Assert(!twisted&&clickStatus==1,"Chorded visible native click did not enter the original multi-click axis-selection path");
  var followupHits=new HashSet<int>();var sequence=new List<object>();sequence.Add(LocalApi.D("native_hit",picking.LastHit,"x",pixel.X,"y",pixel.Y,"native_status",clickStatus));
  for(int n=0;n<8&&!twisted;n++){
   Assert(Convert.ToInt32(Reflect.Get(form,"m_status"))==1,"Original multi-click twist stopped without a committed native word");Assert(renderer.PreparePicking(),"Native multi-click projection failed");
   Point next=FindPixel(wb,viewport,picking,true,followupHits);int nextHit=picking.FindVisibleSticker(next.X,next.Y,true);Assert(nextHit>=0&&followupHits.Add(nextHit),"Native multi-click fixture repeated a selected sticker");
   Click(viewport,next);committedAxis=Convert.ToInt32(Reflect.Get(form,"m_curAx"));committedTwist=Convert.ToInt32(Reflect.Get(form,"m_tw"));await Idle(wb);ReleaseState(form);twisted=Hash(wb)!=before;sequence.Add(LocalApi.D("native_hit",picking.LastHit,"x",next.X,"y",next.Y,"native_status",Convert.ToInt32(Reflect.Get(form,"m_status")),"committed",twisted));
  }
  File.WriteAllText(Path.Combine(output,"native-multiclick-twist.json"),api.Json(LocalApi.D("passed",twisted,"clicks",sequence,"before_hash",before,"after_hash",Hash(wb))));
  Assert(twisted&&Convert.ToInt32(Reflect.Get(form,"m_status"))==0&&State(wb)["pending"]==null,"Visible native multi-click sequence failed to commit within eight selected clicks");
  var snapshot=NativeSnapshot.Read(api.Get("native/snapshot"),Convert.ToString(LocalApi.AsDict(Reflect.Get(wb,"profile"))["profile_sha256"]));var colors=(short[])Reflect.Get(puzzle,"Field");var nativeStyles=(byte[])Reflect.Get(wb,"previousStyles");
  for(int i=0;i<259800;i++)Assert(colors[i]==(snapshot.Colors[2*i]|snapshot.Colors[2*i+1]<<8)&&nativeStyles[i]==snapshot.Styles[i],"Actual clicked native turn differs from atomic backend colors/styles at "+i);
  Assert(LabelHash(api.Bytes("labels"))==Hash(wb)&&Convert.ToString(snapshot.State["state_hash"])==Hash(wb),"Actual clicked native turn differs from complete authoritative labels");
  Reflect.Call(wb,"Command","undo");await Idle(wb);Assert(Hash(wb)==before&&LabelHash(api.Bytes("labels"))==before,"Visible native turn did not undo to exact full state");
  check("Real viewport HWND completes original selected-piece multi-click twist, journals its finite legal word, matches all 259800 native colors/styles and labels, and undoes exactly");
  // Seed a previous axis through the real visible path above, then click an
  // empty corner. The original empty-face branch must not repeat that axis.
  Point empty=new Point(1,1);Assert(renderer.PreparePicking(),"Empty click projection failed");Assert(picking.FindVisibleSticker(empty.X,empty.Y,false)<0,"Chosen empty pixel unexpectedly contains visible geometry");blocked=picking.BlockedClicks;Click(viewport,empty);await Idle(wb);ReleaseState(form);Assert(picking.BlockedClicks==blocked+1&&Hash(wb)==before,"Empty native click repeated a previous axis");
  // Reinstall only the stale native UI axis obtained from that actual turn.
  // A chorded left/right click selects MPUlt action 30, whose original empty
  // FindFace branch repeats this axis. It must be rejected before that branch.
  int savedAxis=Convert.ToInt32(Reflect.Get(form,"m_curAx")),savedTwist=Convert.ToInt32(Reflect.Get(form,"m_tw"));Assert(committedAxis>=0,"Actual turn did not expose its native axis");
  try{Reflect.Set(form,"m_curAx",committedAxis);Reflect.Set(form,"m_tw",committedTwist);blocked=picking.BlockedClicks;SendMessage(viewport.Handle,0x204,(IntPtr)2,Position(empty));SendMessage(viewport.Handle,0x201,(IntPtr)3,Position(empty));SendMessage(viewport.Handle,0x202,(IntPtr)2,Position(empty));SendMessage(viewport.Handle,0x205,IntPtr.Zero,Position(empty));await Idle(wb);ReleaseState(form);Assert(picking.BlockedClicks==blocked+2&&Hash(wb)==before,"Empty chorded native click repeated the cached twist axis");}
  finally{Reflect.Set(form,"m_curAx",savedAxis);Reflect.Set(form,"m_tw",savedTwist);}
  check("Empty plain/chorded native HWND clicks cannot repeat a stale native axis obtained from the real completed turn");
  var camera=Reflect.Property(Reflect.Property(viewport,"Scene"),"Camera");var saved=(double[,])CameraMatrix(viewport).Clone();
  try{
   foreach(bool right in new[]{false,true}){
    var initial=(double[,])CameraMatrix(viewport).Clone();Drag(viewport,right,new Point(200,200),new Point(right?200:225,228));await Idle(wb);ReleaseState(form);double delta=0;var current=CameraMatrix(viewport);for(int i=0;i<4;i++)for(int j=0;j<4;j++)delta+=Math.Abs(current[i,j]-initial[i,j]);Assert(delta>.001&&Hash(wb)==before,"Native picking gate swallowed camera drag or created a twist");
   }
  }finally{Reflect.Set(Reflect.Get(camera,"Trans"),"M",saved);Reflect.Call(camera,"SetChanged");Reflect.Call(viewport,"SetSceneChanged");}
  check("Real HWND press/move/release retains 3D left and 4D right-vertical camera drags without committing twists");
  int oldStatus=Convert.ToInt32(Reflect.Get(form,"m_status"));try{Reflect.Set(form,"m_status",2);blocked=picking.BlockedClicks;Click(viewport,empty);Assert(picking.BlockedClicks==blocked+1,"Animation status did not reject native click");}finally{Reflect.Set(form,"m_status",oldStatus);}await Idle(wb);ReleaseState(form);
  Reflect.Set(picking,"dispatching",true);try{accepted=picking.AcceptedClicks;Click(viewport,pixel);Assert(picking.AcceptedClicks==accepted&&!Convert.ToBoolean(Reflect.Get(form,"qLeftDown")),"Reentrant animated click changed native button state");}finally{Reflect.Set(picking,"dispatching",false);}await Idle(wb);Assert(Hash(wb)==before,"Reentrant animation click changed full state");
  check("Native animation-status and reentrant message guards reject extra clicks before original button bookkeeping or twist mutation");
  await Apply(wb,"O33");Assert(renderer.PreparePicking(),"Modified click projection failed");pixel=FindPixel(wb,viewport,picking);
  using(new ThreadKeys(Keys.Shift,MouseButtons.Left)){Assert(Control.ModifierKeys==Keys.Shift,"Controlled Shift did not reach WinForms");Click(viewport,pixel);}await Idle(wb);ReleaseState(form);
  Assert(Selected(wb,api)!="null","Visible Shift-left click did not create an inspection");
  var home=LocalApi.AsDict(State(wb)["inspection"]);int homeHit=picking.LastHit;var map=(int[])Reflect.Get(wb,"nativeToLab");
  Assert(Convert.ToString(home["kind"])=="home-centers"&&Convert.ToInt32(home["clicked_slot"])==map[homeHit],"Shift-left lost its exact clicked sticker");
  Assert(LocalApi.Array(home["center_slots"]).Length==LocalApi.Array(home["home_colors"]).Length&&LocalApi.Array(home["home_colors"]).Length>0,"Home centers are incomplete");
  string selection=Selected(wb,api);await Apply(wb,"nothing");string prefs=api.Json(LocalApi.AsDict(State(wb)["prefs"]));blocked=picking.BlockedClicks;
  using(new ThreadKeys(Keys.Shift,MouseButtons.Left))Click(viewport,pixel);await Idle(wb);ReleaseState(form);Assert(picking.BlockedClicks==blocked+1&&Selected(wb,api)==selection&&api.Json(LocalApi.AsDict(State(wb)["prefs"]))==prefs&&Hash(wb)==before,"Hidden Shift-click changed selection/preferences or full state");
  check("Actual HWND Shift-left resolves the exact sticker and home Cell Centers; hidden annotations cannot be inspected");
  await Apply(wb,"O33");Assert(renderer.PreparePicking(),"Shift-right projection failed");pixel=FindPixel(wb,viewport,picking);
  using(new ThreadKeys(Keys.Shift,MouseButtons.Right)){SendMessage(viewport.Handle,0x200,IntPtr.Zero,Position(pixel));SendMessage(viewport.Handle,0x204,(IntPtr)2,Position(pixel));SendMessage(viewport.Handle,0x205,IntPtr.Zero,Position(pixel));}await Idle(wb);ReleaseState(form);
  var required=LocalApi.AsDict(State(wb)["inspection"]);int destination=Convert.ToInt32(required["target_position"]);
  Assert(Convert.ToString(required["kind"])=="required-piece"&&Convert.ToInt32(required["clicked_slot"])==map[picking.LastHit],"Shift-right lost its exact hit");
  Assert(Convert.ToInt32(required["target_identity"])==destination&&Convert.ToInt32(LocalApi.AsDict(required["required_piece"])["piece"])==destination,"Shift-right located the wrong identity");
  Assert((int)((NumericUpDown)Reflect.Get(wb,"target")).Value==destination&&!((CheckBox)Reflect.Get(wb,"autoTarget")).Checked,"Buffer destination was replaced by the source");
  Assert(Hash(wb)==before,"Inspection changed authoritative labels");selection=Selected(wb,api);
  check("Actual HWND Shift-right locates the destination identity and keeps the clicked destination in the Buffer Analyzer");
  await Apply(wb,"O33");Assert(renderer.PreparePicking(),"Ctrl recenter projection failed");pixel=FindPixel(wb,viewport,picking);int hit=picking.FindVisibleSticker(pixel.X,pixel.Y,false);int faceId=Convert.ToInt32(Reflect.Get(((Array)fullSlots).GetValue(hit),"NFace"));var originalMatrix=(double[,])CameraMatrix(viewport).Clone();var nativeFaces=(Array)Reflect.Get(Reflect.Get(puzzle,"Str"),"Faces");
  Reflect.Call(camera,"Recenter",Reflect.Get(nativeFaces.GetValue(faceId),"Pole"));var expected=(double[,])CameraMatrix(viewport).Clone();Reflect.Set(Reflect.Get(camera,"Trans"),"M",originalMatrix);Reflect.Call(camera,"SetChanged");Assert(renderer.PreparePicking(),"Ctrl recenter baseline projection failed");
  using(new ThreadKeys(Keys.Control,MouseButtons.Left))Click(viewport,pixel);await Idle(wb);ReleaseState(form);Assert(picking.LastFace==faceId&&SameMatrix(CameraMatrix(viewport),expected)&&Hash(wb)==before,"Ctrl recenter chose hidden framework instead of the visible hit cell");
  await Apply(wb,"nothing");var centered=(double[,])CameraMatrix(viewport).Clone();using(new ThreadKeys(Keys.Control,MouseButtons.Left))Click(viewport,pixel);await Idle(wb);Assert(SameMatrix(centered,CameraMatrix(viewport))&&Hash(wb)==before&&Selected(wb,api)==selection,"Hidden Ctrl-click changed camera, selection or state");
  check("Actual HWND with controlled per-thread Ctrl recenters exactly the visible hit cell; hidden Ctrl-click changes neither camera nor selection");
  var beforeShift=(double[,])CameraMatrix(viewport).Clone();using(new ThreadKeys(Keys.Shift,MouseButtons.None))Drag(viewport,false,new Point(200,200),new Point(223,228));await Idle(wb);ReleaseState(form);Assert(!SameMatrix(beforeShift,CameraMatrix(viewport))&&Selected(wb,api)==selection&&Hash(wb)==before,"Shift 4D drag was swallowed or bridged as piece selection");
  check("Actual HWND with controlled per-thread Shift retains 4D camera drag without a selection bridge side effect");
  Reflect.Set(Reflect.Get(camera,"Trans"),"M",saved);Reflect.Call(camera,"SetChanged");Reflect.Call(viewport,"SetSceneChanged");
  await FullRayBenchmark(wb,viewport,api,output,check);
  await Apply(wb,"active");File.WriteAllText(Path.Combine(output,"native-picking.json"),api.Json(LocalApi.D("passed",true,"camera_R0",cameraR0,"visible_pixel",new[]{pixel.X,pixel.Y},"original_visible_slot",visibleHit,"visible_click_committed_twist",twisted,"visible_click_native_status",clickStatus,"accepted_clicks",picking.AcceptedClicks,"blocked_clicks",picking.BlockedClicks,"native_slots",259800,"state_hash",Hash(wb),"input","in-process Windows messages to the actual native viewport HWND; controlled per-thread modifier/button state restored immediately; no global input injection")));
 }
}
