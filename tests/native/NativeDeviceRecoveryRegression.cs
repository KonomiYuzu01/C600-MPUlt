// CPU-only adapter fixture. The exception names intentionally match the native
// API contract; no Managed DirectX assembly or graphics device is loaded.
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace Microsoft.DirectX.Direct3D {
 internal sealed class DeviceLostException : Exception {}
 internal sealed class DeviceNotResetException : Exception {}
}
internal static class NativeDeviceRecoveryRegression {
 internal sealed class Parameters {public int BackBufferWidth{get;set;}public int BackBufferHeight{get;set;}}
 internal sealed class Device {
  public event EventHandler<CancelEventArgs> DeviceResizing;
  internal int Mode,ResetAttempts;internal bool FailReset;internal readonly List<string> Calls=new List<string>();
  public bool Disposed{get{return false;}}
  public void TestCooperativeLevel(){if(Mode==1)throw new Microsoft.DirectX.Direct3D.DeviceLostException();if(Mode==2)throw new Microsoft.DirectX.Direct3D.DeviceNotResetException();}
  public void Present(){Calls.Add("present");}
  public void Reset(Parameters[] parameters){ResetAttempts++;Calls.Add("reset");if(FailReset)throw new Microsoft.DirectX.Direct3D.DeviceLostException();Mode=0;}
  internal bool HasResizeGuard{get{return DeviceResizing!=null;}}
 }
 internal sealed class Scene {
  public Device m_pd3dDevice=new Device();public Parameters m_d3dpp=new Parameters();
  public bool m_bReady=true,qRenderOn;
  public bool SceneChanged{get;set;}public bool Resize{get;set;}public bool IsReady{get{return m_bReady;}}
  public int Render(){m_pd3dDevice.Calls.Add("render");return 0;}
  public int InvalidateDeviceObjects(){m_pd3dDevice.Calls.Add("invalidate");return 0;}
  public int RestoreDeviceObjects(){m_pd3dDevice.Calls.Add("restore");return 0;}
  public int AfterResize(int width,int height){m_pd3dDevice.Calls.Add("after");return 0;}
  public void EnvironmentResized(object sender,CancelEventArgs e){}
 }
 internal sealed class Viewport : UserControl {
  public Scene m_DDeviceX=new Scene();public EventHandler m_hidl=delegate{};
  public void SetSceneChanged(){m_DDeviceX.SceneChanged=true;}
 }
 static int checks;
 static void Require(bool value,string message){if(!value)throw new Exception(message);checks++;}
 static void Ready(NativeRendererLifecycle lifecycle){typeof(NativeRendererLifecycle).GetMethod("EnsureDeviceReady",Reflect.Flags).Invoke(lifecycle,null);}
 [STAThread] static int Main(){try{
  using(var viewport=new Viewport())using(var lifecycle=new NativeRendererLifecycle(viewport)) {
   viewport.ClientSize=new System.Drawing.Size(800,600);var scene=viewport.m_DDeviceX;var device=scene.m_pd3dDevice;
   Require(device.HasResizeGuard,"Resize guard was not installed");
   Ready(lifecycle);Require(device.ResetAttempts==0,"Ready device was needlessly reset");
   scene.Resize=true;Ready(lifecycle);
   Require(String.Join(",",device.Calls.ToArray())=="invalidate,reset,restore,after","Resize order differed from the inspected native sequence");
   Require(scene.m_bReady&&!scene.Resize&&scene.SceneChanged,"Successful reset flags were incorrect");
   Require(scene.m_d3dpp.BackBufferWidth==800&&scene.m_d3dpp.BackBufferHeight==600,"Backbuffer size did not follow viewport");
   device.Mode=2;scene.m_bReady=false;scene.qRenderOn=true;Ready(lifecycle);
   Require(device.ResetAttempts==2&&scene.m_bReady&&!scene.qRenderOn,"NotReady/NotReset recovery stayed blocked");
   device.Mode=2;device.FailReset=true;Exception failure=null;
   try{Ready(lifecycle);}catch(Exception error){failure=error;}
   Require(failure!=null&&!scene.m_bReady,"Reset failure was swallowed or marked ready");
   scene.qRenderOn=true;
   Require(lifecycle.TryRecover(failure)&&!scene.qRenderOn&&!scene.m_bReady&&scene.Resize,"Device failure did not clear the render latch and schedule retry");
   device.FailReset=false;Ready(lifecycle);
   Require(device.ResetAttempts==4&&scene.m_bReady&&!scene.Resize,"Reset did not retry after a failed reset");
   int resets=device.ResetAttempts;device.Mode=1;failure=null;
   try{Ready(lifecycle);}catch(Exception error){failure=error;}
   Require(failure!=null&&device.ResetAttempts==resets,"Lost device was reset before becoming resettable");
   Require(lifecycle.TryRecover(failure),"Wrapped DeviceLost exception was not classified");
   Require(!lifecycle.TryRecover(new InvalidOperationException("unrelated failure")),"Unrelated exception was suppressed");
  }
  Console.WriteLine("PASS: "+checks+" CPU fixture recovery checks; no DirectX device or visible window created");return 0;
 }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
