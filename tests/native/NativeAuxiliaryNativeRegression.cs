// Actual MPUlt/DirectX host plus its two owned GDI views. Only the explicit
// developer fixture invokes this against a fresh isolated engine session.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class NativeAuxiliaryNativeRegression {
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
 [DllImport("user32.dll")]static extern IntPtr GetLastActivePopup(IntPtr owner);
 [DllImport("user32.dll")]static extern IntPtr GetWindow(IntPtr window,uint command);
 [DllImport("user32.dll")]static extern IntPtr GetDlgItem(IntPtr dialog,int item);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr window,StringBuilder name,int count);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr window,StringBuilder name,int count);
 static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
 static Dictionary<string,object> State(NativeWorkbench wb){return LocalApi.AsDict(Reflect.Get(wb,"state"));}
 static string Hash(NativeWorkbench wb){return Convert.ToString(State(wb)["state_hash"]);}
 static bool Pending(NativeWorkbench wb){return Convert.ToBoolean(Reflect.Get(wb,"busy"))||Reflect.Get(wb,"pendingFocusColor")!=null||Convert.ToBoolean(Reflect.Get(wb,"focusDispatchQueued"));}
 internal static async Task Settled(NativeWorkbench wb){
  var until=DateTime.UtcNow.AddSeconds(60);int quiet=0;
  while(quiet<2){await Task.Delay(20);if(DateTime.UtcNow>until)throw new TimeoutException("Auxiliary focus/commit queue did not settle");quiet=Pending(wb)?0:quiet+1;}
  Assert(Convert.ToBoolean(Reflect.Get(wb,"connected")),"Auxiliary operation disconnected the host");
 }
 static async Task Posted(NativeWorkbench wb,LocalApi api,string path,Dictionary<string,object> body,ManualResetEventSlim entered=null,ManualResetEventSlim release=null){
  var snapshot=(NativeSnapshot)Reflect.Get(wb,"cachedSnapshot");body["native_since"]=snapshot==null?null:snapshot.Revision;
  Action work=delegate{
   if(entered!=null){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(10)))throw new TimeoutException("Auxiliary fixture work gate expired");}
   var result=api.Post(path,body);Reflect.Call(wb,"ReadOperationSnapshot",result);
  };
  Reflect.Call(wb,"Run",work,(Action)delegate{Reflect.Call(wb,"RefreshFromServer");},false);await Settled(wb);
 }
 static string Preferences(NativeWorkbench wb,LocalApi api){var p=LocalApi.AsDict(api.Parse(api.Json(State(wb)["prefs"])));p.Remove("focus_color");return api.Json(p);}
 static string Pose(NativeWorkbench wb,LocalApi api){var c=LocalApi.AsDict(api.Parse(api.Json(Reflect.Call(wb,"CaptureCamera"))));c.Remove("cell");return api.Json(c);}
 static bool Same(double[] a,double[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(Math.Abs(a[i]-b[i])>1e-12)return false;return true;}
 static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static bool Same(short[] a,short[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
 static IntPtr Position(Point p){return (IntPtr)((p.X&65535)|((p.Y&65535)<<16));}
 static void Click(NativeCellView view,Point point){SendMessage(view.Handle,0x200,IntPtr.Zero,Position(point));SendMessage(view.Handle,0x201,(IntPtr)1,Position(point));SendMessage(view.Handle,0x202,IntPtr.Zero,Position(point));}
 static Button FindButton(Control parent,string text){
  foreach(Control child in parent.Controls){var button=child as Button;if(button!=null&&button.Text==text)return button;button=FindButton(child,text);if(button!=null)return button;}return null;
 }
 static async Task EnterButton(NativeWorkbench wb,Form owner,Button button){
  Assert(button!=null&&button.Visible&&button.Enabled,"Compact auxiliary button is unavailable");
  owner.Activate();button.Focus();await Task.Delay(30);Assert(Form.ActiveForm==owner&&button.Focused&&Control.ModifierKeys==Keys.None,"Compact key fixture did not focus its button without modifiers");
  // Queue messages to the real control HWND so Application's production message
  // filter and inherited form KeyPreview both participate in keyboard routing.
  Assert(PostMessage(button.Handle,0x100,(IntPtr)(int)Keys.Enter,(IntPtr)1),"Could not queue auxiliary Enter");
  Assert(PostMessage(button.Handle,0x101,(IntPtr)(int)Keys.Enter,(IntPtr)unchecked((int)0xC0000001)),"Could not queue auxiliary Enter release");
  await Settled(wb);
 }
 static void CancelOwnedClose(NativeWorkbench wb,Form owner){
  object puzzle=Reflect.Get(owner,"Puz");Assert(puzzle!=null&&!Convert.ToBoolean(Reflect.Get(owner,"m_Closing"))&&!Convert.ToBoolean(Reflect.Get(wb,"closing")),"Close-cancel regression requires a live original puzzle");
  bool clicked=false;string failure=null;IntPtr ownerHandle=owner.Handle;var until=DateTime.UtcNow.AddSeconds(4);
  using(var timer=new System.Windows.Forms.Timer{Interval=25}){
   timer.Tick+=delegate{
    IntPtr dialog=GetLastActivePopup(ownerHandle);if(dialog==ownerHandle||dialog==IntPtr.Zero||GetWindow(dialog,4)!=ownerHandle)return;
    var kind=new StringBuilder(64);GetClassName(dialog,kind,kind.Capacity);if(kind.ToString()!="#32770")return;
    var title=new StringBuilder(128);GetWindowText(dialog,title,title.Capacity);
    if(title.ToString()!="Close")failure="Unexpected owned close-dialog title: "+title;
    IntPtr cancel=GetDlgItem(dialog,2);
    if(cancel!=IntPtr.Zero){timer.Stop();clicked=true;SendMessage(cancel,0xF5,IntPtr.Zero,IntPtr.Zero);}
    else if(DateTime.UtcNow>until){timer.Stop();failure="Owned close dialog has no Cancel button";PostMessage(dialog,0x10,IntPtr.Zero,IntPtr.Zero);}
   };
   timer.Start();try{owner.Close();}finally{timer.Stop();}
  }
  Assert(clicked&&failure==null,"Close-cancel dialog was not exercised: "+failure);
  Assert(!owner.IsDisposed&&owner.Visible&&Object.ReferenceEquals(puzzle,Reflect.Get(owner,"Puz"))&&!Convert.ToBoolean(Reflect.Get(owner,"m_Closing"))&&!Convert.ToBoolean(Reflect.Get(wb,"closing")),"Canceling a busy close disposed/nullified the native puzzle or left a closing flag set");
 }
 static void Drag(NativeCellView view,int dx){
  view.Refresh();var area=(Rectangle)Reflect.Get(view,"sceneRect");var a=new Point(area.Left+area.Width/2,area.Top+area.Height/2);var b=new Point(a.X+dx,a.Y+17);
  SendMessage(view.Handle,0x201,(IntPtr)1,Position(a));SendMessage(view.Handle,0x200,(IntPtr)1,Position(b));SendMessage(view.Handle,0x202,IntPtr.Zero,Position(b));
 }
 static Point Pick(NativeCellView view,int exclude,out int color){
  view.Refresh();var points=(PointF[])Reflect.Get(view,"screenCenters");
  foreach(PointF value in points){Point p=Point.Round(value);int hit=Convert.ToInt32(Reflect.Call(view,"Hit",p));if(hit>0&&hit!=exclude){color=hit;return p;}}
  throw new InvalidOperationException("No distinct projected cell can receive a real HWND click");
 }
 static void Status(NativeWorkbench wb,NativeAuxiliaryViews manager){
  var global=(NativeCellStatus)Reflect.Get(manager.GlobalView,"status");var local=(NativeCellStatus)Reflect.Get(manager.LocalView,"status");
  Assert(global!=null&&Object.ReferenceEquals(global,local),"Auxiliary views do not share one atomic cell status");
  Assert(global.StateHash==Hash(wb)&&global.Orbit=="O"+Convert.ToInt32(LocalApi.AsDict(State(wb)["prefs"])["orbit"]),"Auxiliary status is stale or misreads the numeric active orbit");
  var raw=LocalApi.AsDict(State(wb)["cell_status"]);Assert(global.Revision==Convert.ToString(raw["revision"]),"Auxiliary count revision differs from the applied native snapshot");
  Assert(Object.ReferenceEquals(Reflect.Get(manager.GlobalView,"geometry"),Reflect.Get(manager.LocalView,"geometry"))&&Object.ReferenceEquals(Reflect.Get(manager.GlobalView,"geometry"),Reflect.Get(wb,"auxiliaryGeometry")),"Auxiliary views duplicated their immutable geometry");
  Assert(Reflect.Get(wb,"auxiliaryFailure")==null,"Auxiliary view failure was hidden during integration");
 }
 static void Focus(NativeWorkbench wb,NativeAuxiliaryViews manager,int color){
  var data=LocalApi.AsDict(State(wb)["focus"]);var subset=((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).Subset;
  Assert(data!=null&&Convert.ToInt32(data["color"])==color&&Convert.ToInt32(data["lab_cell"])==color-1,"Canonical focus metadata does not match selection");
  Assert(manager.SelectedColor==color&&manager.GlobalView.SelectedColor==color&&manager.LocalView.SelectedColor==color&&Convert.ToInt32(((NumericUpDown)Reflect.Get(wb,"cell")).Value)==color-1,"Canonical focus was not synchronized across the main control and two views");
  var explorer=(NativeStructureExplorer)Reflect.Get(wb,"explorer");Assert(explorer.SelectedColor==color&&explorer.GeneratePredicate("color")=="color(C"+color+")","Auxiliary canonical selection did not synchronize the Structure color origin and generated predicate");
  Assert(subset.FocusedCell==((int[])Reflect.Get(wb,"labToNativeCell"))[color-1],"Main outline uses a canonical index as a native face index");Status(wb,manager);
 }
 static void Faces(NativeWorkbench wb,Control viewport,Array originalFaces,object[] originals,int[] colors,short[] originalField){
  object cube=Reflect.Get(wb,"cube"),puz=Reflect.Get(wb,"puz");var subset=((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).Subset;int at=subset.FocusedCell;bool hide=subset.HideFramework;
  Assert(at>=0,"Focus fixture has no selected native face");
  try{
   subset.HideFramework=false;lock(viewport)using(subset.EnterFramework()){
    var drawn=(Array)Reflect.Get(cube,"StFaces");object focused=drawn.GetValue(at);
    Assert(!Object.ReferenceEquals(drawn,originalFaces)&&!Object.ReferenceEquals(focused,originals[at]),"Focus modifies the original native face or array");
    Assert(NativeRenderSubset.FrameworkColor(focused)==unchecked((int)0xFF29E3DE)&&Convert.ToInt32(Reflect.Get(focused,"Col"))==colors[at],"Focus does not isolate its cyan outline from the original fill color");
    object coordinates=Reflect.Get(originals[at],"Coords3D");if(coordinates!=null)Assert(!Object.ReferenceEquals(coordinates,Reflect.Get(focused,"Coords3D")),"Focus clone shares mutable projected coordinates");
    for(int i=0;i<originals.Length;i++)if(i!=at)Assert(Object.ReferenceEquals(drawn.GetValue(i),originals[i]),"Focusing one cell replaced an unrelated visible framework face");
   }
  }finally{subset.HideFramework=hide;}
  Assert(Object.ReferenceEquals(Reflect.Get(cube,"StFaces"),originalFaces),"Focus draw scope failed to restore native StFaces");
  for(int i=0;i<originals.Length;i++)Assert(Object.ReferenceEquals(originalFaces.GetValue(i),originals[i])&&Convert.ToInt32(Reflect.Get(originals[i],"Col"))==colors[i],"Focus mutated an original native face/color");
  Assert(Same((short[])Reflect.Get(puz,"Field"),originalField),"Focus changed authoritative native sticker colors or selection bits");
 }
 static async Task Frame(NativeRendererLifecycle renderer){var until=DateTime.UtcNow.AddSeconds(10);while(!renderer.RenderFrame()){if(DateTime.UtcNow>until)throw new TimeoutException("No actual native frame after auxiliary update");await Task.Delay(30);}}
 static void Capture(NativeWorkbench wb,Control viewport,NativeAuxiliaryViews manager,string output){
  foreach(var entry in new[]{new KeyValuePair<string,NativeCellView>("global",manager.GlobalView),new KeyValuePair<string,NativeCellView>("local",manager.LocalView)}){
   if(!entry.Value.RenderingActive)continue;entry.Value.Refresh();using(var bitmap=new Bitmap(entry.Value.Width,entry.Value.Height)){entry.Value.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(output,"feature-auxiliary-"+entry.Key+".png"),ImageFormat.Png);}
  }
  var save=typeof(NativePickingRegression).GetMethod("SaveFrame",Reflect.Flags);save.Invoke(null,new object[]{wb,viewport,Path.Combine(output,"feature-auxiliary-main-focus.png")});
 }
 static void CyanPixels(NativeWorkbench wb,Control viewport,string output){
  // Compare actual render targets at the same camera/filter. Merely setting a
  // cloned mesh's Col is insufficient: MPUlt's original line call uses a
  // constant color, independently from the mesh fill color.
  var subset=((NativeRendererLifecycle)Reflect.Get(wb,"renderer")).Subset;int focused=subset.FocusedCell;
  var save=typeof(NativePickingRegression).GetMethod("SaveFrame",Reflect.Flags);
  string on=Path.Combine(output,"feature-auxiliary-main-focus.png"),off=Path.Combine(output,"feature-auxiliary-main-unfocused.png");
  try{subset.FocusedCell=-1;save.Invoke(null,new object[]{wb,viewport,off});}
  finally{subset.FocusedCell=focused;Reflect.Call(viewport,"SetSceneChanged");}
  int cyan=0,nonCyan=0;using(var before=new Bitmap(off))using(var after=new Bitmap(on)){
   Assert(before.Size==after.Size,"Focus comparison changed the render target size");
   for(int y=0;y<after.Height;y++)for(int x=0;x<after.Width;x++){
    Color color=after.GetPixel(x,y);if(color.ToArgb()==before.GetPixel(x,y).ToArgb())continue;
    // Original lighting can brighten/saturate the requested 0x29E3DE. Verify
    // its cyan hue, rather than assuming the vertex color is the final RGB.
    if(color.G>color.R+40&&color.B>color.R+40&&color.G>160&&color.B>160)cyan++;else nonCyan++;
   }
  }
  Assert(cyan>0,"Actual native frame has no added cyan focus-outline pixels; a cloned mesh color alone is not proof");
  Assert(nonCyan==0,"Cell focus changed non-cyan pixels at an otherwise identical camera and filter: "+nonCyan);
  NativeDiagnostics.Write("Auxiliary focus actual render-target cyan pixels added="+cyan+"; non-cyan differences="+nonCyan);
 }
 internal static async Task Run(NativeWorkbench wb,Form form,Control viewport,LocalApi api,string output,Action<string> check){
  await Settled(wb);var manager=(NativeAuxiliaryViews)Reflect.Get(wb,"auxiliaries");var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");
  Assert(manager!=null&&State(wb)["focus"]==null&&renderer.Subset.FocusedCell==-1,"Auxiliary fixture requires fresh startup with no focus annotation");
  Status(wb,manager);string initial=Hash(wb),prefs=Preferences(wb,api),inspection=api.Json(State(wb)["inspection"]);byte[] labels=api.Bytes("labels");var field=(short[])Reflect.Get(Reflect.Get(wb,"puz"),"Field");short[] originalField=(short[])field.Clone();object fieldReference=field;
  var snapshot=(NativeSnapshot)Reflect.Get(wb,"cachedSnapshot");byte[] styles=(byte[])snapshot.Styles.Clone(),interaction=(byte[])snapshot.Interactive.Clone();object cube=Reflect.Get(wb,"cube");var originalFaces=(Array)Reflect.Get(cube,"StFaces");var originals=new object[originalFaces.Length];var colors=new int[originals.Length];for(int i=0;i<originals.Length;i++){originals[i]=originalFaces.GetValue(i);colors[i]=Convert.ToInt32(Reflect.Get(originals[i],"Col"));}
  var camera=LocalApi.AsDict(api.Parse(api.Json(Reflect.Call(wb,"CaptureCamera"))));string pose=Pose(wb,api);Rectangle bounds=form.Bounds;FormWindowState windowState=form.WindowState;var tabs=(TabControl)Reflect.Get(wb,"tabs");int tab=tabs.SelectedIndex;
  Exception failure=null;try{
   Rectangle area=Screen.FromControl(form).WorkingArea;double scale;using(Graphics graphics=form.CreateGraphics())scale=graphics.DpiX/96.0;
   form.WindowState=FormWindowState.Normal;form.Location=new Point(area.Left+10,area.Top+10);form.ClientSize=new Size(Math.Min(area.Width-40,(int)Math.Ceiling(1240*scale)),Math.Min(area.Height-90,(int)Math.Ceiling(760*scale)));await Task.Delay(120);
   Reflect.Call(wb,"OpenAuxiliary",false);Reflect.Call(wb,"OpenAuxiliary",true);await Settled(wb);
   var global=(Form)Reflect.Get(manager,"globalForm");var local=(Form)Reflect.Get(manager,"localForm");bool wideExpected=form.ClientSize.Width/scale>=NativeAuxiliaryViews.CompactWidth&&area.Height/scale>=704;
   Assert(manager.GlobalOpen&&manager.LocalOpen&&manager.IsCompact!=wideExpected,"Auxiliary window/drawer mode differs from the actual display bounds");
   if(wideExpected)Assert(global.Visible&&local.Visible&&global.Owner==form&&local.Owner==form&&manager.GlobalView.RenderingActive&&manager.LocalView.RenderingActive,"The real host did not show both owned auxiliary windows");
   else Assert(!global.Visible&&!local.Visible&&tabs.SelectedIndex==8&&manager.LocalView.RenderingActive,"Compact display did not expose the selected auxiliary drawer");
   check(wideExpected?"Actual native host opens two modeless auxiliary HWNDs with shared immutable geometry and atomic backend counts":"Actual native host uses its compact auxiliary drawer for this display, with shared geometry and atomic backend counts");

   foreach(int color in new[]{1,17,600}){
    Reflect.Call(wb,"SelectAuxiliaryColor",color,false);await Settled(wb);Focus(wb,manager,color);await Frame(renderer);Faces(wb,viewport,originalFaces,originals,colors,originalField);
    snapshot=(NativeSnapshot)Reflect.Get(wb,"cachedSnapshot");Assert(Hash(wb)==initial&&Preferences(wb,api)==prefs&&api.Json(State(wb)["inspection"])==inspection&&Pose(wb,api)==pose&&Same(snapshot.Styles,styles)&&Same(snapshot.Interactive,interaction),"Cell focus changed mechanics, original preferences/inspection, masks or camera");
   }
   Assert(Same(api.Bytes("labels"),labels)&&Object.ReferenceEquals(Reflect.Get(Reflect.Get(wb,"puz"),"Field"),fieldReference),"Canonical focus changed full labels or replaced the native field");
   check("C1/C17/C600 selection synchronizes both views, main cell and bridge-mapped cyan face clone without changing full labels, original StFaces, filters, inspection or picking masks");

   Reflect.Call(wb,"SelectAuxiliaryColor",17,false);await Settled(wb);manager.FollowLocal(false);Reflect.Call(wb,"SelectAuxiliaryColor",600,false);await Settled(wb);
   Assert(manager.LocalPinned&&manager.LocalOrigin==17&&manager.LocalView.FocusColor==17,"Pinned neighborhood followed a later selection");
   manager.OpenGlobal();await Task.Delay(50);double[] gc=manager.GlobalView.CaptureCamera(),lc=manager.LocalView.CaptureCamera();Drag(manager.GlobalView,29);Assert(!Same(gc,manager.GlobalView.CaptureCamera())&&Same(lc,manager.LocalView.CaptureCamera())&&Pose(wb,api)==pose,"Auxiliary drag changed another camera");
   gc=manager.GlobalView.CaptureCamera();manager.OpenLocal();await Task.Delay(50);Drag(manager.LocalView,-27);Assert(!Same(lc,manager.LocalView.CaptureCamera())&&Same(gc,manager.GlobalView.CaptureCamera())&&Pose(wb,api)==pose,"Local camera is not independent");
   manager.FollowLocal(true);Assert(manager.LocalOrigin==600,"Follow did not restore the shared selected origin");manager.OpenGlobal();await Task.Delay(50);
   int clicked;Point pixel=Pick(manager.GlobalView,600,out clicked);Click(manager.GlobalView,pixel);await Settled(wb);Focus(wb,manager,clicked);Assert(Pose(wb,api)==pose,"An auxiliary HWND click recentered the main camera");
   Reflect.Call(wb,"SelectAuxiliaryColor",17,false);await Settled(wb);manager.GlobalView.RequestCenter();await Settled(wb);Assert(Pose(wb,api)!=pose,"Explicit Center main did not change the native camera");await Frame(renderer);Capture(wb,viewport,manager,output);CyanPixels(wb,viewport,output);await Frame(renderer);
   check("Actual auxiliary HWND clicks/drags preserve independent cameras and Follow/Pin; explicit Center main changes only the main camera, and real render-target pixels prove its cyan focus outline");

   string centeredPose=Pose(wb,api);manager.OpenGlobal();await Task.Delay(30);
   using(var entered=new ManualResetEventSlim())using(var release=new ManualResetEventSlim()){
    Task blocked=Posted(wb,api,"prefs",LocalApi.D("orbit",Convert.ToInt32(LocalApi.AsDict(State(wb)["prefs"])["orbit"])),entered,release);
    Exception gateFailure=null;int desired=0;
    try{
     var until=DateTime.UtcNow.AddSeconds(5);while(!entered.IsSet){if(DateTime.UtcNow>until)throw new TimeoutException("Main operation did not enter its test gate");await Task.Delay(10);}
     Assert(Convert.ToBoolean(Reflect.Get(wb,"busy"))&&manager.GlobalView.Enabled,"Busy main work disabled read-only auxiliary navigation");
     CancelOwnedClose(wb,form);Assert(Convert.ToBoolean(Reflect.Get(wb,"busy")),"Canceled close released the intentionally blocked main operation");
     Point pick=Pick(manager.GlobalView,manager.SelectedColor,out desired);Click(manager.GlobalView,pick);
     Assert(manager.SelectedColor==desired&&Reflect.Get(wb,"pendingFocusColor")!=null&&Convert.ToInt32(Reflect.Get(wb,"pendingFocusColor"))==desired,"Busy main work discarded the newest auxiliary selection");
     manager.GlobalView.RequestCenter();Assert(Pose(wb,api)==centeredPose,"Busy explicit center changed the main camera");
    }catch(Exception error){gateFailure=error;}finally{release.Set();}
    await blocked;if(gateFailure!=null)ExceptionDispatchInfo.Capture(gateFailure).Throw();Focus(wb,manager,desired);
   }
   string beforeError=api.Json(State(wb)["focus"]);int faceBefore=renderer.Subset.FocusedCell;await Posted(wb,api,"focus",LocalApi.D("color",601));
   Assert(Hash(wb)==initial&&api.Json(State(wb)["focus"])==beforeError&&renderer.Subset.FocusedCell==faceBefore&&!renderer.HoldUpdates,"Rejected focus left stale state, outline or a renderer hold");Status(wb,manager);
   ((TextBox)Reflect.Get(wb,"word")).Text="1";Reflect.Call(wb,"PreviewWord");await Settled(wb);Reflect.Call(wb,"Commit");await Settled(wb);Assert(Hash(wb)!=initial,"Auxiliary integration turn did not commit");Status(wb,manager);
   Reflect.Call(wb,"Command","undo");await Settled(wb);Assert(Hash(wb)==initial&&Same(api.Bytes("labels"),labels),"Auxiliary turn/undo changed complete recovery state");Status(wb,manager);
   check("Actual busy-close Cancel retains the native puzzle and usable host; queued auxiliary focus, rejection recovery and a later real commit/undo preserve shared atomic cell status");

   gc=manager.GlobalView.CaptureCamera();lc=manager.LocalView.CaptureCamera();int selection=manager.SelectedColor;
   form.Size=new Size(1000,650);await Task.Delay(120);Assert(form.Size==new Size(1000,650),"The shown native form restored an obsolete oversized MinimumSize");manager.OpenGlobal();manager.OpenLocal();await Settled(wb);
   Assert(manager.IsCompact&&!global.Visible&&!local.Visible&&tabs.SelectedIndex==8&&manager.LocalView.RenderingActive&&!manager.GlobalView.RenderingActive,"1000x650 compact window did not retain a single active drawer view");
   ((TextBox)Reflect.Get(wb,"word")).Text="1";Reflect.Call(wb,"PreviewWord");await Settled(wb);
   Assert(State(wb)["pending"]!=null,"Compact button shortcut regression lacks a committable preview");
   string keyHash=Hash(wb),token=Convert.ToString(LocalApi.AsDict(State(wb)["pending"])["token"]),keyPose=Pose(wb,api);long keyHead=Convert.ToInt64(State(wb)["head"]);
   short[] keyField=(short[])((short[])Reflect.Get(Reflect.Get(wb,"puz"),"Field")).Clone();var panel=(Control)Reflect.Get(manager,"localPanel");
   await EnterButton(wb,form,FindButton(panel,"Reset camera"));
   Assert(!Same(lc,manager.LocalView.CaptureCamera())&&Same(gc,manager.GlobalView.CaptureCamera())&&Pose(wb,api)==keyPose,"Compact Reset camera Enter did not reset only its local auxiliary camera");lc=manager.LocalView.CaptureCamera();
   Assert(Hash(wb)==keyHash&&Convert.ToInt64(State(wb)["head"])==keyHead&&State(wb)["pending"]!=null&&Convert.ToString(LocalApi.AsDict(State(wb)["pending"])["token"])==token&&Same(keyField,(short[])Reflect.Get(Reflect.Get(wb,"puz"),"Field")),"Compact Reset camera Enter committed a preview or changed native state");
   await EnterButton(wb,form,FindButton(panel,"Close view"));
   Assert(!manager.LocalOpen&&!manager.LocalView.RenderingActive,"Compact Close view Enter did not close its auxiliary view");
   Assert(Hash(wb)==keyHash&&Convert.ToInt64(State(wb)["head"])==keyHead&&State(wb)["pending"]!=null&&Convert.ToString(LocalApi.AsDict(State(wb)["pending"])["token"])==token&&Same(keyField,(short[])Reflect.Get(Reflect.Get(wb,"puz"),"Field"))&&Same(api.Bytes("labels"),labels),"Compact Close view Enter committed a preview or changed complete state");
   Reflect.Call(wb,"Cancel");await Settled(wb);Assert(State(wb)["pending"]==null&&Hash(wb)==initial,"Compact shortcut fixture did not cancel its unchanged preview");manager.OpenLocal();await Settled(wb);
   form.WindowState=FormWindowState.Minimized;await Task.Delay(100);Assert(!manager.GlobalView.RenderingActive&&!manager.LocalView.RenderingActive,"Minimized native owner left auxiliary painting active");form.WindowState=FormWindowState.Normal;await Task.Delay(100);
   form.Hide();await Task.Delay(70);Assert(!global.Visible&&!local.Visible&&!manager.GlobalView.RenderingActive&&!manager.LocalView.RenderingActive,"Hidden native owner left an auxiliary renderer active");form.Show();await Task.Delay(100);
   manager.CloseGlobal();manager.CloseLocal();manager.OpenGlobal();manager.OpenLocal();await Settled(wb);Assert(manager.SelectedColor==selection&&Same(gc,manager.GlobalView.CaptureCamera())&&Same(lc,manager.LocalView.CaptureCamera()),"Hide/minimize/close/reopen lost auxiliary cameras or selection");
   form.ClientSize=new Size(Math.Min(area.Width-40,(int)Math.Ceiling(1240*scale)),Math.Min(area.Height-90,(int)Math.Ceiling(760*scale)));await Task.Delay(120);Assert(manager.IsCompact!=wideExpected,"Wide/compact round trip did not restore the display policy");await Frame(renderer);Status(wb,manager);
   check("Actual native resize/compact drawer and queued Reset camera/Close view Enter preserve pending preview and full state; minimize/hide/reopen retain independent cameras, selection and atomic status");
  }catch(Exception error){failure=error;}
  // The .NET Framework compiler does not support await inside finally.
  // Await cleanup outside the catch, then preserve the original failure stack.
  try{
   await Settled(wb);manager.CloseGlobal();manager.CloseLocal();manager.FollowLocal(true);
   Reflect.Call(wb,"ApplyCamera",camera);await Settled(wb);await Posted(wb,api,"focus",LocalApi.D("color",null));
   form.WindowState=FormWindowState.Normal;form.Bounds=bounds;form.WindowState=windowState;tabs.SelectedIndex=tab;await Task.Delay(100);await Settled(wb);
  }catch(Exception cleanup){if(failure!=null)throw new AggregateException("Auxiliary integration and cleanup both failed",failure,cleanup);throw;}
  if(failure!=null)ExceptionDispatchInfo.Capture(failure).Throw();
  Assert(State(wb)["focus"]==null&&renderer.Subset.FocusedCell==-1&&Preferences(wb,api)==prefs&&api.Json(State(wb)["inspection"])==inspection&&Hash(wb)==initial&&Same(api.Bytes("labels"),labels)&&Pose(wb,api)==pose,"Auxiliary fixture did not restore focus, camera, preferences and complete labelled state before older regressions");
 }
}
