// Actual Windows / MPUlt / Managed DirectX timings. Run only with a fresh,
// separate engine data directory. Numbers are evidence, not an invented FPS pass.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.DirectX.Direct3D;

internal static class NativePerformanceRegression {
 static Form form;static NativeWorkbench wb;static Control viewport;static object scene,camera,transform;
 static Device device;static LocalApi api;static string output;
 static double[,] initialCameraMatrix;
 static readonly List<object> measurements=new List<object>();
 static readonly List<string> failures=new List<string>();
 static readonly List<string> acceptanceFailures=new List<string>();
 static readonly List<double> heartbeat=new List<double>();
 static System.Threading.Timer pulse;static int pulsePending,heartbeatEpoch;static bool closed,completed;
 static string initialHash,turnedHash;static DateTime started;
 static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 static double Milliseconds(long ticks){return ticks*1000.0/Stopwatch.Frequency;}
 static Dictionary<string,object> Distribution(List<double> source){
  double[] a=source.ToArray();Array.Sort(a);double sum=0;foreach(double n in a)sum+=n;
  return LocalApi.D("samples",a.Length,"mean_ms",a.Length==0?0:sum/a.Length,"p50_ms",Percentile(a,.5),"p95_ms",Percentile(a,.95),"p99_ms",Percentile(a,.99),"max_ms",a.Length==0?0:a[a.Length-1]);
 }
 static double Percentile(double[] a,double p){return a.Length==0?0:a[Math.Min(a.Length-1,(int)Math.Ceiling(a.Length*p)-1)];}
 static Dictionary<string,object> Memory(){using(var p=Process.GetCurrentProcess()){p.Refresh();return LocalApi.D("working_set_bytes",p.WorkingSet64,"private_bytes",p.PrivateMemorySize64,"managed_bytes",GC.GetTotalMemory(false));}}
 static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static Dictionary<string,object> State(){return LocalApi.AsDict(Reflect.Get(wb,"state"));}
 static string StateHash(){return Convert.ToString(State()["state_hash"]);}
 static void ResetHeartbeat(){Interlocked.Increment(ref heartbeatEpoch);heartbeat.Clear();}
 static void SaveProgress(){File.WriteAllText(Path.Combine(output,"native-performance-progress.json"),api.Json(LocalApi.D("started_utc",started.ToString("o"),"measurements",measurements,"failures",failures)));}
 static void Add(object item){measurements.Add(item);SaveProgress();Console.WriteLine(api.Json(item));Console.Out.Flush();}
 static void Pulse(object ignored){
  if(closed||form==null||!form.IsHandleCreated||Interlocked.CompareExchange(ref pulsePending,1,0)!=0)return;
  long queued=Stopwatch.GetTimestamp();int epoch=heartbeatEpoch;
  try{form.BeginInvoke((Action)delegate{if(!closed&&epoch==heartbeatEpoch)heartbeat.Add(Milliseconds(Stopwatch.GetTimestamp()-queued));Interlocked.Exchange(ref pulsePending,0);});}
  catch(InvalidOperationException){Interlocked.Exchange(ref pulsePending,0);}
 }
 static async Task WaitReady(){
  var timer=Stopwatch.StartNew();
  while(!Convert.ToBoolean(Reflect.Get(wb,"connected"))||Convert.ToBoolean(Reflect.Get(wb,"busy"))){Assert(!closed,"Window closed while waiting for bridge");Assert(timer.Elapsed.TotalSeconds<240,"Native bridge/operation timed out");await Task.Delay(30);}
 }
 static async Task Operation(string name,Action action){
  ResetHeartbeat();var elapsed=Stopwatch.StartNew();action();await WaitReady();elapsed.Stop();
  Add(LocalApi.D("kind","operation","name",name,"elapsed_ms",elapsed.Elapsed.TotalMilliseconds,"state_hash",StateHash(),"ui_dispatch_delay",Distribution(heartbeat),"memory",Memory()));
 }
 static async Task Request(string name,string path,Dictionary<string,object> body){
  await Operation(name,delegate{Reflect.Call(wb,"Run",new Action(delegate{api.Post(path,body);}),new Action(delegate{Reflect.Call(wb,"RefreshFromServer");}),false);});
 }
 static void ValidateRender(string name){
  Assert(form.Visible&&viewport.Visible&&viewport.FindForm()==form,"Viewport invisible/unrooted");
  Assert(Convert.ToBoolean(Reflect.Property(scene,"IsReady")),"DirectX scene not ready");
  Assert(!device.Disposed,"DirectX device disposed");
  Reflect.Call(viewport,"SetSceneChanged");Reflect.Call(viewport,"ForceUpdate");
  Assert(Convert.ToInt32(Reflect.Call(scene,"FrameMove"))>=0,"FrameMove failed");
  // Capture before Present, using the same temporary sticker/framework scope
  // as production Draw. An unscoped diagnostic Render restores hidden cell
  // outlines and misrepresents the filtered image even when timed Draw is exact.
  object lifecycle=Reflect.Get(wb,"renderer");var subsetProperty=lifecycle.GetType().GetProperty("Subset",Reflect.Flags);object subset=subsetProperty==null?null:subsetProperty.GetValue(lifecycle,null);
  lock(viewport)using(subset==null?null:(IDisposable)Reflect.Call(subset,"Enter"))Assert(Convert.ToInt32(Reflect.Call(scene,"Render"))>=0,"Render failed");
  string path=Path.Combine(output,name+".png");
  using(var surface=device.GetRenderTarget(0))SurfaceLoader.Save(path,ImageFileFormat.Png,surface);
  device.Present();
  var colors=new HashSet<int>();int colored=0;
  using(var bitmap=new Bitmap(path)){
   Assert(bitmap.Size==viewport.ClientSize,"Render target is clipped or stale");
   for(int y=0;y<bitmap.Height;y+=4)for(int x=0;x<bitmap.Width;x+=4){Color c=bitmap.GetPixel(x,y);colors.Add(c.ToArgb());if(Math.Max(c.R,Math.Max(c.G,c.B))-Math.Min(c.R,Math.Min(c.G,c.B))>25)colored++;}
  }
  Assert(colors.Count>24&&colored>20,"Render target is blank");
 }
 static byte[] CapturePixels(string name){
  string path=Path.Combine(output,name+".png");using(var surface=device.GetRenderTarget(0))SurfaceLoader.Save(path,ImageFileFormat.Png,surface);
  using(var bitmap=new Bitmap(path)){var pixels=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{var bytes=new byte[pixels.Stride*pixels.Height];Marshal.Copy(pixels.Scan0,bytes,0,bytes.Length);return bytes;}finally{bitmap.UnlockBits(pixels);}}
 }
 static async Task FullDetailComparison(){
  var adapterField=Reflect.Field(wb.GetType(),"fullRenderer");if(adapterField==null)return;object adapter=adapterField.GetValue(wb);Assert(adapter!=null,"Full-detail buffer optimization silently fell back to the original renderer");
  var lifecycle=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");var enabled=adapter.GetType().GetProperty("Enabled",Reflect.Flags);bool wasEnabled=Convert.ToBoolean(enabled.GetValue(adapter,null)),smooth=lifecycle.SmoothMotion;
  ((TextBox)Reflect.Get(wb,"filter")).Text="everything";await Operation("filter-full-detail-comparison",delegate{Reflect.Call(wb,"ApplyFilter");});
  Assert(lifecycle.Subset.VisibleCount==259800,"Full-detail comparison must retain every sticker");byte[] labelsBefore=await Task.Run(delegate{return api.Bytes("labels");});
  var originalFrames=new List<double>();var optimizedFrames=new List<double>();int equalPoses=0;lifecycle.Pause();lifecycle.SmoothMotion=false;pulse.Change(Timeout.Infinite,Timeout.Infinite);
  try{
   for(int pose=0;pose<3;pose++){
    Reflect.Set(transform,"M",(double[,])initialCameraMatrix.Clone());if(pose==1)Reflect.Call(transform,"Rotate",3,1,Math.Sin(.18));if(pose==2)Reflect.Call(transform,"Rotate",0,3,Math.Sin(.14));Reflect.Call(camera,"SetChanged");Reflect.Call(viewport,"SetSceneChanged");byte[] reference=null;
    foreach(int phase in new[]{0,1,2}){
     bool optimized=phase==1;enabled.SetValue(adapter,optimized,null);Assert(lifecycle.RenderFrame(),"Full-detail comparison warmup was not submitted");await Task.Delay(100);
     var frames=new List<double>();int[] gc=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};double cpuBefore;using(var process=Process.GetCurrentProcess())cpuBefore=process.TotalProcessorTime.TotalMilliseconds;
     for(int frame=0;frame<6;frame++){long start=Stopwatch.GetTimestamp();Assert(lifecycle.RenderFrame(),"Full-detail comparison frame was not submitted");frames.Add(Milliseconds(Stopwatch.GetTimestamp()-start));Assert(lifecycle.Subset.LastDrawCount==259800,"Full-detail comparison sampled or omitted stickers");await Task.Delay(20);}
     double cpuAfter;using(var process=Process.GetCurrentProcess())cpuAfter=process.TotalProcessorTime.TotalMilliseconds;
     (optimized?optimizedFrames:originalFrames).AddRange(frames);string name="full-detail-pose-"+pose+"-"+(optimized?"optimized":phase==0?"original-before":"original-after");var pixels=CapturePixels(name);int differences=0;
     if(reference==null)reference=pixels;else{Assert(reference.Length==pixels.Length,"Full-detail render target shape differs");for(int i=0;i<pixels.Length;i++)if(reference[i]!=pixels[i])differences++;}
     Add(LocalApi.D("kind","full_detail_comparison","name",name,"pose",pose,"optimized",optimized,"visible_meshes",259800,"submitted_meshes",lifecycle.Subset.LastDrawCount,"frames",Distribution(frames),"cpu_ms",cpuAfter-cpuBefore,"gc_collections",new[]{GC.CollectionCount(0)-gc[0],GC.CollectionCount(1)-gc[1],GC.CollectionCount(2)-gc[2]},"pixel_sha256",Hash(pixels),"different_pixel_bytes",differences,"exact_pixels",differences==0,"viewport_width",viewport.Width,"viewport_height",viewport.Height));Assert(differences==0,"Full-detail buffer optimization changed rendered pixels at pose "+pose);
    }equalPoses++;
   }
   byte[] labelsAfter=await Task.Run(delegate{return api.Bytes("labels");});Assert(Hash(labelsBefore)==Hash(labelsAfter)&&labelsAfter.Length==259800*4,"Full-detail optimization changed full labelled state");
   var before=Distribution(originalFrames);var after=Distribution(optimizedFrames);double ratio=Convert.ToDouble(after["mean_ms"])/Convert.ToDouble(before["mean_ms"]);
   Add(LocalApi.D("kind","full_detail_improvement","name","all-259800-3-poses","original",before,"optimized",after,"optimized_over_original_mean",ratio,"reduction_percent",100*(1-ratio),"exact_pixel_poses",equalPoses,"unchanged_full_label_hash",Hash(labelsAfter),"required_max_ratio",.9,"passed",ratio<=.9));
  }finally{enabled.SetValue(adapter,wasEnabled,null);lifecycle.SmoothMotion=smooth;Reflect.Set(transform,"M",(double[,])initialCameraMatrix.Clone());Reflect.Call(camera,"SetChanged");Reflect.Call(viewport,"SetSceneChanged");lifecycle.Resume();pulse.Change(0,16);}
 }
 static async Task Idle(){
  // CPU and UI responsiveness are separate measurements. The 16 ms test
  // pulse is not part of the product, so exclude it from the idle CPU sample.
  // Production rendering and authenticated engine heartbeat timers remain on.
  // Three seconds of settling matches the independent alternating idle probe;
  // no GC is forced, and its activity is reported rather than hidden.
  pulse.Change(Timeout.Infinite,Timeout.Infinite);await Task.Delay(3000);ResetHeartbeat();
  int[] collections=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};
  double cpu0;using(var p=Process.GetCurrentProcess())cpu0=p.TotalProcessorTime.TotalMilliseconds;
  int idle0=((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).FramesRequested;var elapsed=Stopwatch.StartNew();
  await Task.Delay(5000);elapsed.Stop();double cpu1;using(var p=Process.GetCurrentProcess())cpu1=p.TotalProcessorTime.TotalMilliseconds;
  Add(LocalApi.D("kind","idle","name","active-orbit-5s","measurement","Warm production idle; test heartbeat disabled; production timers and engine heartbeat active","warmup_ms",3000,"test_heartbeat_enabled",false,"elapsed_ms",elapsed.Elapsed.TotalMilliseconds,"cpu_ms",cpu1-cpu0,"cpu_percent_one_core",100*(cpu1-cpu0)/elapsed.Elapsed.TotalMilliseconds,"cpu_percent_machine",100*(cpu1-cpu0)/elapsed.Elapsed.TotalMilliseconds/Environment.ProcessorCount,"cpu_counter_note","A zero CPU delta means below Windows process CPU counter resolution, not literal zero cost","logical_processors",Environment.ProcessorCount,"idle_callbacks",((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).FramesRequested-idle0,"gc_collections",new[]{GC.CollectionCount(0)-collections[0],GC.CollectionCount(1)-collections[1],GC.CollectionCount(2)-collections[2]},"memory",Memory()));
  pulse.Change(0,16);await Task.Delay(500);ResetHeartbeat();elapsed.Restart();await Task.Delay(5000);elapsed.Stop();
  Add(LocalApi.D("kind","idle_ui_responsiveness","name","active-orbit-5s-instrumented","measurement","Separate UI dispatch probe; 16 ms test heartbeat enabled; excluded from idle CPU acceptance","test_heartbeat_enabled",true,"elapsed_ms",elapsed.Elapsed.TotalMilliseconds,"ui_dispatch_delay",Distribution(heartbeat)));
 }
 static async Task Motion(string name,string expression){
  // Every mode and build follows the same bounded trajectory from the same
  // initial pose. A fixed rotation per submitted frame makes faster builds
  // travel farther and can turn the selected work cell away from the camera.
  Reflect.Set(transform,"M",(double[,])initialCameraMatrix.Clone());Reflect.Call(camera,"SetChanged");Reflect.Call(viewport,"SetSceneChanged");
  ((TextBox)Reflect.Get(wb,"filter")).Text=expression;
  await Operation("filter-"+name,delegate{Reflect.Call(wb,"ApplyFilter");});
  ValidateRender(name+"-before");
  // Use the same dirty UpdateHandler/FrameMove/Render/Present path as camera
  // interaction. Disable only background idle competition during controlled
  // frame submission; it is measured separately above. There is no capture in
  // the timed loop and no removal of full puzzle mechanics/geometry.
  var lifecycle=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");lifecycle.Pause();
  var frames=new List<double>();int incompleteFrames=0;double previousAngle=0;ResetHeartbeat();var total=Stopwatch.StartNew();
  List<double> motionHeartbeat=null;object restoreMeasurement=null;
  MethodInfo rotate=transform.GetType().GetMethod("Rotate",Reflect.Flags,null,new[]{typeof(int),typeof(int),typeof(double)},null);
  MethodInfo changed=camera.GetType().GetMethod("SetChanged",Reflect.Flags);
  MethodInfo update=viewport.GetType().GetMethod("ForceUpdate",Reflect.Flags);
  MethodInfo dirty=viewport.GetType().GetMethod("SetSceneChanged",Reflect.Flags);
  MethodInfo renderFrame=lifecycle.GetType().GetMethod("RenderFrame",Reflect.Flags,null,Type.EmptyTypes,null);
  MethodInfo notifyMotion=lifecycle.GetType().GetMethod("NotifyCameraInteraction",Reflect.Flags,null,Type.EmptyTypes,null);
  var subsetProperty=lifecycle.GetType().GetProperty("Subset",Reflect.Flags);
  object subset=subsetProperty==null?null:subsetProperty.GetValue(lifecycle,null);
  int visibleMeshes=0;
  if(subset==null){foreach(byte style in (byte[])Reflect.Get(wb,"previousStyles"))if(style!=0)visibleMeshes++;}
  else visibleMeshes=Convert.ToInt32(Reflect.Property(subset,"VisibleCount"));
  if(wb.GetType().GetMethod("Scramble",Reflect.Flags)!=null){
   Assert(!Convert.ToBoolean(LocalApi.AsDict(State()["prefs"])["pin_safety"]),"Performance filter retained unrequested pinned context");
   int expectedVisible=name=="active-orbit"?2400:name=="work-cell"?849:259800;
   Assert(visibleMeshes==expectedVisible,"Exact filter count differs from the complete model: "+name+" actual "+visibleMeshes+" expected "+expectedVisible);
  }
  int motionMeshes=notifyMotion==null?visibleMeshes:Convert.ToInt32(Reflect.Property(subset,"MotionCount"));
  int declaredMotionLimit=0,declaredMotionThreshold=0;
  if(notifyMotion!=null){
   declaredMotionLimit=Convert.ToInt32(subset.GetType().GetField("MotionLimit",Reflect.Flags).GetRawConstantValue());
   var threshold=subset.GetType().GetField("MotionThreshold",Reflect.Flags);
   declaredMotionThreshold=threshold==null?declaredMotionLimit:Convert.ToInt32(threshold.GetRawConstantValue());
  }
  object puzzle=Reflect.Get(wb,"puz"),cube=Reflect.Get(wb,"cube");
  object fullField=Reflect.Get(puzzle,"Field"),fullStickers=Reflect.Get(cube,"Stks"),cubePuzzle=Reflect.Get(cube,"Cube");
  int fullCount=Convert.ToInt32(Reflect.Get(cube,"NStk"));
  Assert(rotate!=null&&changed!=null&&update!=null&&dirty!=null,"Unsupported real camera/update reflection surface");
  try{
   while(frames.Count<60||total.Elapsed.TotalSeconds<10){
    long begin=Stopwatch.GetTimestamp();
    if(notifyMotion!=null)notifyMotion.Invoke(lifecycle,null);
    double targetAngle=.25*Math.Sin(total.Elapsed.TotalSeconds*.4),deltaAngle=targetAngle-previousAngle;previousAngle=targetAngle;
    rotate.Invoke(transform,new object[]{0,3,Math.Sin(deltaAngle)});changed.Invoke(camera,null);dirty.Invoke(viewport,null);
    bool submitted=true;
    if(renderFrame!=null)submitted=Convert.ToBoolean(renderFrame.Invoke(lifecycle,null));else update.Invoke(viewport,null);
    if(submitted)frames.Add(Milliseconds(Stopwatch.GetTimestamp()-begin));else incompleteFrames++;
    Assert(Object.ReferenceEquals(fullField,Reflect.Get(puzzle,"Field"))&&((Array)fullField).Length==259800,"Renderer replaced full puzzle labels");
    Assert(Object.ReferenceEquals(fullStickers,Reflect.Get(cube,"Stks"))&&Object.ReferenceEquals(cubePuzzle,Reflect.Get(cube,"Cube"))&&Convert.ToInt32(Reflect.Get(cube,"NStk"))==fullCount,"Renderer subset leaked beyond frame scope");
    Assert(Convert.ToBoolean(Reflect.Property(scene,"IsReady")),"DirectX became unready during motion");
    Assert(total.Elapsed.TotalSeconds<360,"Motion sample exceeded six minutes; performance result incomplete");
    await Task.Delay(submitted?1:250);
   }
   total.Stop();motionHeartbeat=new List<double>(heartbeat);
   if(notifyMotion!=null&&renderFrame!=null){
    ResetHeartbeat();var settle=Stopwatch.StartNew();
    int quiet=Convert.ToInt32(lifecycle.GetType().GetField("MotionSettleMilliseconds",Reflect.Flags).GetRawConstantValue());
    await Task.Delay(quiet+20);
    var fullFrame=Stopwatch.StartNew();Assert(Convert.ToBoolean(renderFrame.Invoke(lifecycle,null)),"Full-detail restoration frame was not submitted");fullFrame.Stop();settle.Stop();await Task.Delay(1);
    Assert(!Convert.ToBoolean(Reflect.Property(lifecycle,"IsMotionActive")),"Renderer remained in sampled motion detail after settling");
    var countProperty=subset.GetType().GetProperty("LastDrawCount",Reflect.Flags);
    Assert(countProperty!=null,"Full-detail restoration requires an actual submitted mesh count");
    int restoredMeshes=Convert.ToInt32(countProperty.GetValue(subset,null));
    Assert(restoredMeshes==visibleMeshes,"Full-detail restoration omitted visible native meshes");
    restoreMeasurement=LocalApi.D("kind","full_detail_restoration","name",name,"visible_meshes",visibleMeshes,"restored_meshes",restoredMeshes,"motion_meshes",motionMeshes,"declared_motion_limit",declaredMotionLimit,"declared_motion_threshold",declaredMotionThreshold,"settle_quiet_ms",quiet,"render_ms",fullFrame.Elapsed.TotalMilliseconds,"delay_after_motion_loop_ms",settle.Elapsed.TotalMilliseconds,"legacy_1000ms_budget_met",fullFrame.Elapsed.TotalMilliseconds<=1000,"latency_acceptance","Observed separately: user selected fluid drag with reduced moving detail and full detail restored after stopping, with approximately 1.4-second full restoration disclosed","ui_dispatch_delay",Distribution(heartbeat));
    MethodInfo preparePicking=lifecycle.GetType().GetMethod("PreparePicking",Reflect.Flags,null,Type.EmptyTypes,null);
    if(preparePicking!=null){
     notifyMotion.Invoke(lifecycle,null);Assert(Convert.ToBoolean(renderFrame.Invoke(lifecycle,null)),"Picking probe motion frame did not render");
     if(countProperty!=null)Assert(Convert.ToInt32(countProperty.GetValue(subset,null))==motionMeshes,"Picking probe did not enter sampled motion detail");
     Assert(Convert.ToBoolean(preparePicking.Invoke(lifecycle,null)),"Picking preparation did not restore a ready full-detail frame");
     Assert(!Convert.ToBoolean(Reflect.Property(lifecycle,"IsMotionActive")),"Picking still uses sampled motion coordinates");
     if(countProperty!=null)Assert(Convert.ToInt32(countProperty.GetValue(subset,null))==visibleMeshes,"Picking preparation omitted full visible native coordinates");
     Add(LocalApi.D("kind","correctness","name","picking-restores-full-coordinates-"+name,"passed",true,"motion_meshes",motionMeshes,"restored_meshes",visibleMeshes));
    }
   }
  }finally{lifecycle.Resume();}
  Add(LocalApi.D("kind","camera_motion","name",name,"expression",expression,"elapsed_ms",total.Elapsed.TotalMilliseconds,"frames",Distribution(frames),"incomplete_frame_attempts",incompleteFrames,"motion_meshes",motionMeshes,"declared_motion_limit",declaredMotionLimit,"declared_motion_threshold",declaredMotionThreshold,"visible_meshes",visibleMeshes,"renderer_scan_slots",subset==null?259800:motionMeshes,"adaptive_motion_sampling",motionMeshes<visibleMeshes,"observed_frames_per_second",frames.Count/total.Elapsed.TotalSeconds,"ui_dispatch_delay",Distribution(motionHeartbeat),"memory",Memory(),"viewport_width",viewport.Width,"viewport_height",viewport.Height,"state_hash",StateHash()));
  if(restoreMeasurement!=null)Add(restoreMeasurement);
  ValidateRender(name+"-after");Assert(StateHash()==initialHash,"Rendering changed committed state");
  Assert(Hash(File.ReadAllBytes(Path.Combine(output,name+"-before.png")))!=Hash(File.ReadAllBytes(Path.Combine(output,name+"-after.png"))),"Camera motion did not change the rendered image");
 }
 static async Task FullState(string name,string expected){
  byte[] labels=await Task.Run(delegate{return api.Bytes("labels");});
  Assert(labels.Length==259800*4,"Incomplete labelled full-state snapshot");Assert(Hash(labels)==expected,"Full labels do not match committed hash: "+name);
  Dictionary<string,object> snapshot=await Task.Run(delegate{return api.Get("native/snapshot");});
  Assert(Convert.ToString(LocalApi.AsDict(snapshot["state"])["state_hash"])==expected,"Native snapshot belongs to another committed state");
  byte[] colors=Convert.FromBase64String(Convert.ToString(snapshot["colors"])),styles=Convert.FromBase64String(Convert.ToString(snapshot["styles"]));
  short[] nativeColors=(short[])Reflect.Get(Reflect.Get(wb,"puz"),"Field");byte[] nativeStyles=(byte[])Reflect.Get(wb,"previousStyles");
  Assert(colors.Length==259800*2&&styles.Length==259800&&nativeColors.Length==259800&&nativeStyles.Length==259800,"Native state/style length mismatch");
  for(int i=0;i<259800;i++)if((nativeColors[i]&0x7fff)!=(colors[2*i]|colors[2*i+1]<<8)||nativeStyles[i]!=styles[i])throw new InvalidOperationException("Native colors/styles differ from atomic full snapshot at slot "+i);
  File.WriteAllBytes(Path.Combine(output,name+"-labels.u32"),labels);
  Add(LocalApi.D("kind","full_state","name",name,"labelled_slots",labels.Length/4,"sha256",Hash(labels),"native_color_and_style_slots_checked",259800));
 }
 static void CheckSubsetRecovery(){
  object lifecycle=Reflect.Get(wb,"renderer");var property=lifecycle.GetType().GetProperty("Subset",Reflect.Flags);if(property==null)return;
  object subset=property.GetValue(lifecycle,null);Assert(subset!=null,"Production renderer has no subset adapter");
  object puzzle=Reflect.Get(wb,"puz"),cube=Reflect.Get(wb,"cube");
  object field=Reflect.Get(puzzle,"Field"),stickers=Reflect.Get(cube,"Stks"),cubePuzzle=Reflect.Get(cube,"Cube");int count=Convert.ToInt32(Reflect.Get(cube,"NStk"));
  lock(viewport){
   bool caught=false;
   try{using((IDisposable)Reflect.Call(subset,"Enter")){Assert(Object.ReferenceEquals(field,Reflect.Get(puzzle,"Field")),"Subset entered the authoritative puzzle field");throw new InvalidOperationException("deliberate-subset-scope-probe");}}
   catch(InvalidOperationException e){if(e.Message!="deliberate-subset-scope-probe")throw;caught=true;}
   Assert(caught,"Subset exception probe did not run");
   Assert(Object.ReferenceEquals(field,Reflect.Get(puzzle,"Field"))&&Object.ReferenceEquals(stickers,Reflect.Get(cube,"Stks"))&&Object.ReferenceEquals(cubePuzzle,Reflect.Get(cube,"Cube"))&&Convert.ToInt32(Reflect.Get(cube,"NStk"))==count,"Subset failed to restore full native references after an exception");
  }
  Add(LocalApi.D("kind","correctness","name","actual-subset-exception-restores-full-native-references","passed",true,"full_stickers",count));
 }
 static async Task Run(){
  try{
   await WaitReady();initialHash=StateHash();scene=Reflect.Property(viewport,"Scene");camera=Reflect.Property(scene,"Camera");transform=Reflect.Get(camera,"Trans");initialCameraMatrix=(double[,])((double[,])Reflect.Get(transform,"M")).Clone();device=(Device)Reflect.Property(scene,"Renderer");
   var renderCube=Reflect.Get(wb,"cube");
   Add(LocalApi.D("kind","render_configuration","name","startup","native_device_description",Convert.ToString(Reflect.Get(scene,"m_strDeviceStats")),"native_device_creation_flags",Convert.ToString(Reflect.Get(scene,"m_dwCreateFlags")),"cell_shrink",Reflect.Get(renderCube,"FShr"),"sticker_shrink",Reflect.Get(renderCube,"SShr"),"original_cell_shrink_slider",((TrackBar)Reflect.Get(form,"trk_faceShrink")).Value,"original_sticker_shrink_slider",((TrackBar)Reflect.Get(form,"trk_StickerSize")).Value,"original_light_ambient_slider",((TrackBar)Reflect.Get(form,"trk_LightAmb")).Value,"original_light_diffuse_slider",((TrackBar)Reflect.Get(form,"trk_LightDiff")).Value,"original_light_specular_slider",((TrackBar)Reflect.Get(form,"trk_LightSpec")).Value));
   form.Size=new Size(1440,900);await Task.Delay(500);ValidateRender("startup");pulse=new System.Threading.Timer(Pulse,null,0,16);
   await FullState("initial",initialHash);CheckSubsetRecovery();await Idle();
   await Motion("active-orbit","active");await Motion("work-cell","current(C000)");await Motion("all-pieces","everything");await FullDetailComparison();
   ((TextBox)Reflect.Get(wb,"filter")).Text="active";await Operation("filter-active-restore",delegate{Reflect.Call(wb,"ApplyFilter");});
   if(wb.GetType().GetMethod("Scramble",Reflect.Flags)!=null){
    await Operation("scramble-normal-1000",delegate{Reflect.Call(wb,"Scramble");});Assert(State()["pending"]==null,"Normal scramble left a pending preview");
   }else{
    await Request("scramble-preview-1000","scramble",LocalApi.D("count",1000,"seed",600));
    Assert(StateHash()==initialHash,"Scramble preview mutated committed state");
    var pending=LocalApi.AsDict(State()["pending"]);Assert(Convert.ToString(pending["primitive_count"])=="1000","Scramble did not retain 1000 legal turns");
    await Operation("scramble-commit-1000",delegate{Reflect.Call(wb,"Commit");});
   }
   turnedHash=StateHash();Assert(turnedHash!=initialHash,"Scramble commit did not change puzzle");Assert(Convert.ToString(State()["primitives"])=="1000","Scramble did not retain exactly 1000 legal turns");await FullState("scrambled",turnedHash);ValidateRender("scrambled");
   await Operation("scramble-undo-1000",delegate{Reflect.Call(wb,"Command","undo");});Assert(StateHash()==initialHash,"Undo failed full-state recovery");await FullState("undone",initialHash);
   await Operation("scramble-redo-1000",delegate{Reflect.Call(wb,"Command","redo");});Assert(StateHash()==turnedHash,"Redo failed full-state recovery");await FullState("redone",turnedHash);
   await Request("checkpoint-save","checkpoint",LocalApi.D("name","performance-scramble-1000"));
   await Request("journal-backup","backup",LocalApi.D());
   byte[] exported=await Task.Run(delegate{return api.Bytes("export");});File.WriteAllBytes(Path.Combine(output,"scrambled-session.c600.json.gz"),exported);
   Assert(StateHash()==turnedHash,"Save/export mutated state");ValidateRender("final");
   if(wb.GetType().GetMethod("Scramble",Reflect.Flags)!=null){
    await Request("explicit-scramble-preview-1000","scramble",LocalApi.D("count",1000,"seed",600));Assert(StateHash()==turnedHash,"Explicit preview mutated committed state");
    await Operation("explicit-preview-cancel",delegate{Reflect.Call(wb,"Command","cancel");});Assert(StateHash()==turnedHash,"Cancel mutated committed state");
   }
   completed=true;
  }catch(Exception e){failures.Add(e.ToString());NativeDiagnostics.Write("PERFORMANCE REGRESSION FAILED",e);}
  finally{closed=true;if(pulse!=null)pulse.Dispose();form.Close();}
 }
 static void Accept(bool pass,string message){if(!pass)acceptanceFailures.Add(message);}
 static void Acceptance(){
  int modes=0;bool idle=false,scramble=false,settle=false;
  foreach(object item in measurements){
   var m=LocalApi.AsDict(item);string kind=Convert.ToString(m["kind"]),name=Convert.ToString(m["name"]);
   if(kind=="camera_motion"){
    modes++;var frames=LocalApi.AsDict(m["frames"]);var queue=LocalApi.AsDict(m["ui_dispatch_delay"]);
    Accept(Convert.ToDouble(m["observed_frames_per_second"])>=30,name+": submitted frame rate below 30 FPS");
    Accept(Convert.ToDouble(frames["p95_ms"])<=33.3,name+": p95 frame time exceeds 33.3 ms");
    Accept(Convert.ToDouble(queue["p95_ms"])<=50,name+": p95 UI queue delay exceeds 50 ms");
    Accept(Convert.ToInt32(m["incomplete_frame_attempts"])==0,name+": incomplete frame attempts occurred");
    if(name=="all-pieces")Accept(Convert.ToInt32(m["declared_motion_limit"])>0&&Convert.ToInt32(m["motion_meshes"])==Convert.ToInt32(m["declared_motion_limit"])&&Convert.ToInt32(m["visible_meshes"])==259800,"All-pieces motion must match the source-declared sample limit and retain all 259800 visible meshes for full detail");
    else Accept(Convert.ToInt32(m["motion_meshes"])==Convert.ToInt32(m["visible_meshes"]),name+": exact filtered mode unexpectedly sampled visible meshes");
   }
   if(kind=="idle"){idle=true;Accept(Convert.ToDouble(m["cpu_percent_machine"])<=1,"Idle CPU exceeds 1% of machine capacity");}
   if(kind=="operation"&&name=="scramble-normal-1000"){scramble=true;Accept(Convert.ToDouble(m["elapsed_ms"])<=2000,"Warm atomic 1000-turn scramble exceeds 2000 ms");}
   if(kind=="full_detail_restoration"&&name=="all-pieces"){settle=true;Accept(Convert.ToInt32(m["visible_meshes"])==259800&&Convert.ToInt32(m["restored_meshes"])==259800,"Full-detail restoration must submit all 259800 visible meshes");}
   if(kind=="full_detail_improvement")Accept(Convert.ToBoolean(m["passed"])&&Convert.ToInt32(m["exact_pixel_poses"])==3,"Full-detail optimization must preserve exact pixels and reduce mean full-frame cost by at least 10% across three poses");
  }
  Accept(modes==3&&idle&&scramble&&settle,"Candidate acceptance measurements are incomplete");
 }
 [STAThread] static int Main(string[] args){
  Console.OutputEncoding=new System.Text.UTF8Encoding(false);
  if(args.Length!=4)return 2;output=Path.GetFullPath(args[3]);Directory.CreateDirectory(output);started=DateTime.UtcNow;
  try{
   string exe=Path.GetFullPath(args[0]);Directory.SetCurrentDirectory(Path.GetDirectoryName(exe));
   AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){string path=Path.Combine(Path.GetDirectoryName(exe),new AssemblyName(e.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
   Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
   Application.ThreadException+=delegate(object sender,ThreadExceptionEventArgs e){failures.Add(e.Exception.ToString());closed=true;if(form!=null)form.Close();};
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   form=(Form)Activator.CreateInstance(Assembly.LoadFrom(exe).GetType("_3dedit.Form1",true));
   wb=new NativeWorkbench(form,exe,args[1],args[2]);viewport=(Control)Reflect.Get(wb,"viewport");api=new LocalApi(args[1],args[2]);
   form.Shown+=async delegate{await Run();};Application.Run(form);
  }catch(Exception e){failures.Add(e.ToString());}
  bool candidate=wb!=null&&wb.GetType().GetMethod("Scramble",Reflect.Flags)!=null;
  if(candidate)Acceptance();
  var report=LocalApi.D("correctness_passed",completed&&failures.Count==0,"performance_passed",candidate?(object)(completed&&acceptanceFailures.Count==0):null,"performance_acceptance",candidate?"30 submitted FPS; p95 frame <=33.3 ms; p95 UI queue <=50 ms; zero incomplete frames; exact filtered geometry and source-declared sampled all-pieces motion; idle <=1% machine CPU; atomic 1000-turn scramble <=2000 ms; full all-pieces restoration must submit all 259800 meshes; current full-detail buffer optimization must reduce the mean cost of complete 259800-mesh rendering by at least 10% across 3 pixel-identical fixed poses; restoration latency is observed separately under the user's selected fluid-motion tradeoff":"Baseline observations, before candidate acceptance","full_restoration_policy","User explicitly selected fluid drag with reduced moving detail followed by complete detail after stopping; approximately 1.4-second full restoration was disclosed. The earlier 1000 ms budget is retained as an observation and is not claimed to have passed.","idle_measurement_policy","Idle CPU uses an uninstrumented 5-second window after 3 seconds of settling; production timers remain active. UI queue latency uses a separate 5-second instrumented window. This corrects the previous benchmark's inclusion of its 16 ms pulse in idle CPU; the earlier failed report is preserved and the 1% CPU gate is unchanged. An independent alternating off/on diagnostic did not establish that the pulse caused the earlier spike.","test_assembly_sha256",Hash(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location)),"scope","Actual Windows MPUlt Managed DirectX, dirty camera update including native Present, fresh isolated journal; no screenshots inside timed loops; full static detail and sampled all-pieces motion are measured separately","started_utc",started.ToString("o"),"finished_utc",DateTime.UtcNow.ToString("o"),"process_bits",IntPtr.Size*8,"clr",Environment.Version.ToString(),"initial_hash",initialHash,"scrambled_hash",turnedHash,"measurements",measurements,"failures",failures,"acceptance_failures",acceptanceFailures);
  File.WriteAllText(Path.Combine(output,"native-performance.json"),new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=67108864}.Serialize(report));
  return completed&&failures.Count==0&&(!candidate||acceptanceFailures.Count==0)?0:1;
 }
}


