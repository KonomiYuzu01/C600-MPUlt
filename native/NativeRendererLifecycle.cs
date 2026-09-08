// Host-side lifecycle adaptation for the inspected MPUlt Managed DirectX control.
// Its original resize/idle callbacks dereference ParentForm during reparenting;
// OnHandleDestroyed also removes the original Application.Idle subscription.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;

internal sealed class NativeRendererLifecycle : IDisposable {
 readonly Control viewport;
 readonly object scene;
 object device;
 EventInfo resizingEvent;
 Delegate originalResize,guardedResize;
 readonly EventHandler nativeIdle;
 readonly System.Windows.Forms.Timer frameTimer=new System.Windows.Forms.Timer{Interval=16};
 readonly Stopwatch elapsed=Stopwatch.StartNew();
 Func<bool> dirty,resize,ready;
 Func<int> renderScene,invalidateDevice,restoreDevice;
 Func<int,int,int> afterResize;
 Action<bool> setDirty,setResize,setReady,setRendering;
 Action markChanged;
 Action checkDevice,present;
 Action<int> setBackBufferWidth,setBackBufferHeight;
 MethodInfo resetDevice;
 Array resetParameters;
 bool recovering;
 long retryAt;
 internal const int MotionSettleMilliseconds=180;
 long motionUntil;
 bool smoothMotion=true,restoreDetail;
 internal bool SmoothMotion {get{return smoothMotion;}set{smoothMotion=value;markChanged();}}
 internal bool IsMotionActive {get{return smoothMotion&&elapsed.ElapsedMilliseconds<motionUntil;}}
 internal void NotifyCameraInteraction(){motionUntil=elapsed.ElapsedMilliseconds+MotionSettleMilliseconds;markChanged();}
 internal NativeRenderSubset Subset {get;set;}
 internal int RecoveryCount {get;private set;}
 internal double LastFrameMilliseconds {get;private set;}
 bool paused=true,disposed,idleAttached;
 internal int FramesRequested { get; private set; }
 // Hold authoritative input publication without changing device readiness or
 // forcing a resize. Existing dirty state is rendered after commit/rollback.
 internal bool HoldUpdates {get;set;}
 internal NativeRendererLifecycle(Control viewport){
  this.viewport=viewport;
  scene=Reflect.Get(viewport,"m_DDeviceX");
  if(scene==null)throw new InvalidOperationException("MPUlt did not create a DirectX scene.");
  nativeIdle=(EventHandler)Reflect.Get(viewport,"m_hidl");
  if(nativeIdle==null)throw new InvalidOperationException("MPUlt idle renderer is unavailable.");
  dirty=(Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>),scene,scene.GetType().GetProperty("SceneChanged",Reflect.Flags).GetGetMethod(true));
  resize=(Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>),scene,scene.GetType().GetProperty("Resize",Reflect.Flags).GetGetMethod(true));
  ready=(Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>),scene,scene.GetType().GetProperty("IsReady",Reflect.Flags).GetGetMethod(true));
  setDirty=(Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>),scene,scene.GetType().GetProperty("SceneChanged",Reflect.Flags).GetSetMethod(true));
  setResize=(Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>),scene,scene.GetType().GetProperty("Resize",Reflect.Flags).GetSetMethod(true));
  setReady=BooleanFieldSetter(scene,"m_bReady");setRendering=BooleanFieldSetter(scene,"qRenderOn");
  renderScene=(Func<int>)Delegate.CreateDelegate(typeof(Func<int>),scene,scene.GetType().GetMethod("Render",Reflect.Flags, null,Type.EmptyTypes,null));
  invalidateDevice=(Func<int>)Delegate.CreateDelegate(typeof(Func<int>),scene,scene.GetType().GetMethod("InvalidateDeviceObjects",Reflect.Flags,null,Type.EmptyTypes,null));
  restoreDevice=(Func<int>)Delegate.CreateDelegate(typeof(Func<int>),scene,scene.GetType().GetMethod("RestoreDeviceObjects",Reflect.Flags,null,Type.EmptyTypes,null));
  afterResize=(Func<int,int,int>)Delegate.CreateDelegate(typeof(Func<int,int,int>),scene,scene.GetType().GetMethod("AfterResize",Reflect.Flags,null,new[]{typeof(int),typeof(int)},null));
  markChanged=(Action)Delegate.CreateDelegate(typeof(Action),viewport,viewport.GetType().GetMethod("SetSceneChanged",Reflect.Flags));
  frameTimer.Tick+=delegate{Draw(false,false);};
  InstallResizeGuard();
  Application.Idle-=nativeIdle;
  viewport.HandleCreated+=OnHandleCreated;
  viewport.HandleDestroyed+=OnHandleDestroyed;
  ((UserControl)viewport).Load+=OnLoad;
  viewport.MouseMove+=OnMouseMove;
  viewport.MouseWheel+=OnMouseWheel;
  viewport.KeyDown+=OnKeyDown;
  NativeDiagnostics.Write("DirectX lifecycle adapter installed; native idle paused for layout");
 }
 static Action<bool> BooleanFieldSetter(object owner,string name){
  var value=Expression.Parameter(typeof(bool),"value");
  var member=Expression.Field(Expression.Constant(owner),Reflect.Field(owner.GetType(),name));
  return Expression.Lambda<Action<bool>>(Expression.Block(Expression.Assign(member,value),Expression.Empty()),value).Compile();
 }
 void InstallResizeGuard(){
  if(disposed||device!=null)return;
  var created=Reflect.Get(scene,"m_pd3dDevice");
  // Original MPUlt creates the device in DXControl.OnLoad, after Form1's
  // constructor. Do not force Load while the destination hierarchy is unrooted.
  if(created==null)return;
  device=created;
  checkDevice=(Action)Delegate.CreateDelegate(typeof(Action),device,device.GetType().GetMethod("TestCooperativeLevel",Type.EmptyTypes));
  present=(Action)Delegate.CreateDelegate(typeof(Action),device,device.GetType().GetMethod("Present",Type.EmptyTypes));
  var parameters=Reflect.Get(scene,"m_d3dpp");var parameterType=parameters.GetType();
  setBackBufferWidth=(Action<int>)Delegate.CreateDelegate(typeof(Action<int>),parameters,parameterType.GetProperty("BackBufferWidth").GetSetMethod());
  setBackBufferHeight=(Action<int>)Delegate.CreateDelegate(typeof(Action<int>),parameters,parameterType.GetProperty("BackBufferHeight").GetSetMethod());
  resetParameters=Array.CreateInstance(parameterType,1);resetParameters.SetValue(parameters,0);
  resetDevice=device.GetType().GetMethod("Reset",new[]{resetParameters.GetType()});
  if(resetDevice==null)throw new MissingMethodException("Unsupported MPUlt DirectX device reset API.");
  resizingEvent=device.GetType().GetEvent("DeviceResizing");
  var method=scene.GetType().GetMethod("EnvironmentResized",Reflect.Flags);
  if(resizingEvent==null||method==null)throw new MissingMemberException("Unsupported MPUlt device resize lifecycle.");
  originalResize=Delegate.CreateDelegate(resizingEvent.EventHandlerType,scene,method);
  guardedResize=Delegate.CreateDelegate(resizingEvent.EventHandlerType,this,GetType().GetMethod("OnDeviceResizing",Reflect.Flags));
  resizingEvent.RemoveEventHandler(device,originalResize);
  resizingEvent.AddEventHandler(device,guardedResize);
  NativeDiagnostics.Write("DirectX device created; parent/resize guard installed");
 }
 bool CanRender(bool allowPaused=false){
  var parent=viewport.FindForm();
  return !disposed&&(allowPaused||!paused)&&!viewport.IsDisposed&&viewport.IsHandleCreated&&
   parent!=null&&parent.Visible&&viewport.Visible&&parent.WindowState!=FormWindowState.Minimized&&
   viewport.ClientSize.Width>0&&viewport.ClientSize.Height>0;
 }
 void OnDeviceResizing(object sender,CancelEventArgs e){if(!CanRender(true))e.Cancel=true;}
 static Exception Unwrap(Exception error){while(error is TargetInvocationException&&error.InnerException!=null)error=error.InnerException;return error;}
 static void CheckResult(int result,string operation){if(result<0)throw new InvalidOperationException("MPUlt "+operation+" failed (HRESULT "+result+").");}
 void EnsureDeviceReady(){
  bool needsReset=resize()||!ready();
  try{checkDevice();}
  catch(Exception error){if(Unwrap(error).GetType().FullName!="Microsoft.DirectX.Direct3D.DeviceNotResetException")throw;needsReset=true;}
  if(!needsReset)return;
  // Follow the inspected Resize3DEnvironment sequence without its modal error
  // dialog and swallowed failure. Reset occurs before compact arrays are used;
  // a failed Reset leaves ready=false and is retried independently of that flag.
  setReady(false);setRendering(false);
  setBackBufferWidth(viewport.ClientSize.Width);setBackBufferHeight(viewport.ClientSize.Height);
  CheckResult(invalidateDevice(),"InvalidateDeviceObjects");
  resetDevice.Invoke(device,new object[]{resetParameters});
  CheckResult(restoreDevice(),"RestoreDeviceObjects");
  CheckResult(afterResize(viewport.ClientSize.Width,viewport.ClientSize.Height),"AfterResize");
  setReady(true);setResize(false);setDirty(true);
 }
 // A dirty frame is submitted at most once per UI timer tick. Idle callbacks are
 // not frames, and repeatedly invoking them needlessly consumes a CPU core.
 bool Draw(bool force,bool allowPaused){
  if(HoldUpdates)return false;
  if(!CanRender(allowPaused)||elapsed.ElapsedMilliseconds<retryAt)return false;
  InstallResizeGuard();
  if(device==null)return false;
  bool motion=IsMotionActive&&Subset!=null&&Subset.VisibleCount>NativeRenderSubset.MotionThreshold;
  if(force||resize()||recovering||(restoreDetail&&!motion))markChanged();
  if(!dirty())return false;
  var frame=Stopwatch.StartNew();
  try{
   lock(viewport){
    EnsureDeviceReady();setDirty(false);
    // Call only the original synchronous Render while the temporary mesh view
    // is installed. Device readiness/reset and Present use complete native data.
    using(Subset==null?null:Subset.Enter(motion)){CheckResult(renderScene(),"Render");}
    present();
   }
   restoreDetail=motion;
   LastFrameMilliseconds=frame.Elapsed.TotalMilliseconds;FramesRequested++;
   if(recovering){recovering=false;RecoveryCount++;NativeDiagnostics.Write("DirectX rendering recovered after a display/device interruption");}
   return true;
  }catch(Exception error){if(TryRecover(error))return false;throw;}
 }
 // Synchronous production draw used by diagnostics and benchmarks. Pausing the
 // automatic scheduler does not disable an explicitly requested frame.
 internal bool RenderFrame(){return Draw(true,true);}
 // MPUlt's picking uses projected coordinates cached by Render. Refresh all
 // visible meshes before dispatching a click after a sampled camera frame.
 internal bool PreparePicking(){
  if(HoldUpdates)return false;
  bool needsFrame=restoreDetail||IsMotionActive||dirty()||resize();motionUntil=0;
  return !needsFrame||Draw(true,true);
 }
 void OnMouseMove(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.None)NotifyCameraInteraction();}
 void OnMouseWheel(object sender,MouseEventArgs e){NotifyCameraInteraction();}
 void OnKeyDown(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Right||e.KeyCode==Keys.Up||e.KeyCode==Keys.Down||e.KeyCode==Keys.PageUp||e.KeyCode==Keys.PageDown)NotifyCameraInteraction();}
 internal bool TryRecover(Exception error){
  error=Unwrap(error);
  string type=error.GetType().FullName;
  if(type!="Microsoft.DirectX.Direct3D.DeviceLostException"&&type!="Microsoft.DirectX.Direct3D.DeviceNotResetException")return false;
  if(!recovering)NativeDiagnostics.Write("DirectX device temporarily unavailable; rendering will retry",error);
  recovering=true;retryAt=elapsed.ElapsedMilliseconds+250;
  // The original Render does not clear its recursion latch when it throws.
  // Otherwise a successful device reset would still leave every future frame
  // returning immediately with the previous image on screen.
  setRendering(false);setReady(false);setResize(true);markChanged();return true;
 }
 void OnLoad(object sender,EventArgs e){InstallResizeGuard();if(!paused&&!disposed)AttachIdle();}
 void OnHandleCreated(object sender,EventArgs e){if(!paused&&!disposed)AttachIdle();}
 void OnHandleDestroyed(object sender,EventArgs e){DetachIdle();}
 void AttachIdle(){
  // Remove before adding so handle recreation cannot accumulate subscriptions.
  Application.Idle-=nativeIdle;
  frameTimer.Start();idleAttached=true;
 }
 void DetachIdle(){Application.Idle-=nativeIdle;frameTimer.Stop();idleAttached=false;}
 internal void Pause(){paused=true;DetachIdle();}
 internal void Resume(){
  if(disposed)throw new ObjectDisposedException("NativeRendererLifecycle");
  if(viewport.FindForm()==null)throw new InvalidOperationException("DirectX viewport must be rooted in its Form before resuming.");
  InstallResizeGuard();paused=false;AttachIdle();Reflect.SetProperty(scene,"Resize",true);Reflect.Call(viewport,"SetSceneChanged");
  NativeDiagnostics.Write("DirectX viewport rooted; dirty-frame scheduler resumed");
 }
 internal void AssertReady(){
  InstallResizeGuard();
  if(!CanRender()||!idleAttached||Reflect.Property(scene,"Camera")==null)
   throw new InvalidOperationException("DirectX renderer is not ready or its viewport is not visible.");
  // A reconnect may follow a failed device frame. Draw performs the validated
  // reset/recovery path before testing readiness; a held or deferred frame must
  // not be reported as a successful visible connection.
  if(!RenderFrame())throw new InvalidOperationException("DirectX has not presented a frame yet. Reconnect after display recovery.");
  NativeDiagnostics.Write("Actual DirectX scene/camera/device and visible viewport verified");
 }
 public void Dispose(){
  if(disposed)return;disposed=true;DetachIdle();frameTimer.Dispose();
  viewport.HandleCreated-=OnHandleCreated;viewport.HandleDestroyed-=OnHandleDestroyed;
  ((UserControl)viewport).Load-=OnLoad;
  viewport.MouseMove-=OnMouseMove;viewport.MouseWheel-=OnMouseWheel;viewport.KeyDown-=OnKeyDown;
  if(device!=null&&!Convert.ToBoolean(Reflect.Property(device,"Disposed"))){resizingEvent.RemoveEventHandler(device,guardedResize);resizingEvent.AddEventHandler(device,originalResize);}
 }
}
