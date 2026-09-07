// Real render-policy and accessible control regression. All native calls run in
// the isolated test process; no desktop input or user journal is used.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.DirectX.Direct3D;

internal static class NativeFramePolicyRegression {
 [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
 static NativeWorkbench wb;static LocalApi api;static Control viewport;static Form form;
 static NativeRendererLifecycle renderer;static NativeFullRenderer adapter;
 static object cube,puzzle;static Array fullSlots,fullFaces;static object fullField;static string output;
 static readonly List<object> evidence=new List<object>();
 static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 static Dictionary<string,object> State(){return LocalApi.AsDict(Reflect.Get(wb,"state"));}
 static string Hash(){return Convert.ToString(State()["state_hash"]);}
 static async Task Idle(){var deadline=DateTime.UtcNow.AddSeconds(90);while(Convert.ToBoolean(Reflect.Get(wb,"busy"))){Assert(DateTime.UtcNow<deadline,"Frame mode operation timed out");await Task.Delay(25);}Assert(Convert.ToBoolean(Reflect.Get(wb,"connected"))&&viewport.Enabled,"Frame mode disconnected the host");}
 static async Task Native(string method,params object[] args){Reflect.Call(wb,method,args);await Idle();}
 static async Task Backend(string path,Dictionary<string,object> data){await Native("Run",new Action(delegate{api.Post(path,data);}),new Action(delegate{Reflect.Call(wb,"RefreshFromServer");}),false);}
 static CheckBox Checkbox(){return (CheckBox)Reflect.Get(wb,"hideFrame");}
 static ToolStripMenuItem Menu(){return (ToolStripMenuItem)Reflect.Get(wb,"frameMenu");}
 static void Mode(bool hide){var prefs=LocalApi.AsDict(State()["prefs"]);Assert((bool)Reflect.Call(wb,"ReadFrameMode",prefs)==hide,"Stored frame mode mismatch");Assert(Checkbox().Checked==hide&&Menu().Checked==hide&&renderer.Subset.HideFramework==hide,"Frame mode UI/render state is inconsistent");}
 static async Task ToggleCheckbox(){Assert(Checkbox().Enabled,"Frame checkbox is disabled while idle");Reflect.Call(Checkbox(),"OnClick",EventArgs.Empty);await Idle();}
 static async Task ToggleMenu(){Assert(Menu().Enabled,"Frame menu is disabled while idle");Menu().PerformClick();await Idle();}
 static async Task Apply(string expression){((TextBox)Reflect.Get(wb,"filter")).Text=expression;await Native("ApplyFilter");}
 static void Ownership(){Assert(Object.ReferenceEquals(fullSlots,Reflect.Get(cube,"Stks"))&&Convert.ToInt32(Reflect.Get(cube,"NStk"))==259800&&Object.ReferenceEquals(puzzle,Reflect.Get(cube,"Cube"))&&Object.ReferenceEquals(fullField,Reflect.Get(puzzle,"Field"))&&Object.ReferenceEquals(fullFaces,Reflect.Get(cube,"StFaces"))&&Convert.ToInt32(Reflect.Get(cube,"NF"))==600&&!renderer.Subset.IsEntered,"Rendering leaked its temporary geometry, field, or clipping policy");}
 static void RejectFrameClick(string image){
  Point point=Point.Empty;using(var bitmap=new Bitmap(image)){int background=bitmap.GetPixel(0,0).ToArgb();for(int y=1;y<bitmap.Height-1&&point.IsEmpty;y++)for(int x=1;x<bitmap.Width-1;x++)if(bitmap.GetPixel(x,y).ToArgb()!=background){point=new Point(x,y);break;}}
  Assert(!point.IsEmpty&&renderer.Subset.VisibleCount==0,"Frame click requires a visible framework pixel and zero visible stickers");
  var picking=(NativePickingVisibility)Reflect.Get(wb,"picking");int blocked=picking.BlockedClicks;string hash=Hash();long head=Convert.ToInt64(State()["head"]);Assert(Control.ModifierKeys==Keys.None,"Frame click requires released modifiers");
  IntPtr xy=(IntPtr)((point.X&65535)|((point.Y&65535)<<16));SendMessage(viewport.Handle,0x200,IntPtr.Zero,xy);SendMessage(viewport.Handle,0x201,(IntPtr)1,xy);SendMessage(viewport.Handle,0x202,IntPtr.Zero,xy);
  Assert(picking.BlockedClicks==blocked+1&&picking.LastHit<0&&Hash()==hash&&Convert.ToInt64(State()["head"])==head&&!Convert.ToBoolean(Reflect.Get(wb,"busy")),"Visible framework allowed a hidden sticker interaction");
  Assert(!Convert.ToBoolean(Reflect.Get(form,"qLeftDown"))&&!Convert.ToBoolean(Reflect.Get(form,"qRightDown"))&&!Convert.ToBoolean(Reflect.Get(form,"qSkipClick")),"Rejected framework click left mouse state latched");Ownership();
  evidence.Add(LocalApi.D("framework_pixel_click_rejected",true,"x",point.X,"y",point.Y,"hide_frame",renderer.Subset.HideFramework,"state_hash",hash));
 }
 static byte[] Pixels(string path){using(var bitmap=new Bitmap(path)){var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{var bytes=new byte[bitmap.Width*bitmap.Height*4];for(int y=0;y<bitmap.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),bytes,y*bitmap.Width*4,bitmap.Width*4);return bytes;}finally{bitmap.UnlockBits(data);}}}
 static void Same(string a,string b){var left=Pixels(a);var right=Pixels(b);Assert(left.Length==right.Length,"Frame image sizes differ");for(int i=0;i<left.Length;i++)if(left[i]!=right[i])throw new InvalidOperationException("Original and optimized policy pixels differ at byte "+i);}
 static int RawFrame(string name){
  var scene=Reflect.Property(viewport,"Scene");Reflect.Call(Reflect.Property(scene,"Camera"),"SetChanged");Reflect.Call(viewport,"SetSceneChanged");
  Assert(Convert.ToInt32(Reflect.Call(scene,"FrameMove"))>=0,"Raw native FrameMove failed");
  // No outer Subset.Enter: this deliberately exercises the formerly unguarded
  // entry used by MPUlt ForceUpdate, animation, and original display controls.
  lock(viewport)Assert(Convert.ToInt32(Reflect.Call(scene,"Render"))>=0,"Raw native scene.Render failed");
  string path=Path.Combine(output,name+".png");var device=(Device)Reflect.Property(scene,"Renderer");using(var surface=device.GetRenderTarget(0))SurfaceLoader.Save(path,ImageFileFormat.Png,surface);
  int changed=0;using(var bitmap=new Bitmap(path)){int background=bitmap.GetPixel(0,0).ToArgb();for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++)if(bitmap.GetPixel(x,y).ToArgb()!=background)changed++;}
  Ownership();evidence.Add(LocalApi.D("frame",name,"different_pixels",changed,"hide_frame",renderer.Subset.HideFramework,"visible_stickers",renderer.Subset.VisibleCount,"optimized",adapter.Enabled));return changed;
 }
 static void Setup(NativeWorkbench workbench,Form window,Control control,LocalApi client,string directory){wb=workbench;form=window;viewport=control;api=client;output=directory;renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");adapter=(NativeFullRenderer)Reflect.Get(wb,"fullRenderer");cube=Reflect.Get(wb,"cube");puzzle=Reflect.Get(wb,"puz");fullSlots=(Array)Reflect.Get(cube,"Stks");fullFaces=(Array)Reflect.Get(cube,"StFaces");fullField=Reflect.Get(puzzle,"Field");Assert(adapter!=null&&adapter.RenderScope!=null,"Universal native render guard is not installed");}
 static void Report(bool passed,string error=null){File.WriteAllText(Path.Combine(output,"native-frame-policy.json"),api.Json(LocalApi.D("passed",passed,"scope","Actual WinForms control events and original MPUlt/DirectX scene calls; isolated journal","raw_frames",evidence,"full_slots",259800,"native_clipping_planes",600,"error",error)));}
 internal static async Task Run(NativeWorkbench workbench,Form window,Control control,LocalApi client,string directory,Action<string> check){
  Setup(workbench,window,control,client,directory);string initial=Hash();bool originalEnabled=adapter.Enabled;var guard=adapter.RenderScope;
  try{
   Assert(renderer.PreparePicking(),"Frame policy could not initialize real projection");renderer.Pause();
   // Fresh default and checkpoints both use explicit policy; preserve another
   // view preference to catch accidental replacement of the complete view map.
   await Backend("prefs",LocalApi.D("view",LocalApi.D("native_hide_frame",true,"frame_regression_marker","preserved")));Mode(true);
   await Apply("nothing");Assert(RawFrame("frame-nothing-hidden")==0,"Nothing filter leaked the frame in unwrapped scene.Render");
   adapter.RenderScope=null;try{Assert(RawFrame("frame-bypass-fault-demonstration")>0,"Fault demonstration did not reproduce the previous unguarded framework draw");}finally{adapter.RenderScope=guard;}
   Assert(RawFrame("frame-nothing-guard-restored")==0,"Restoring universal guard did not remove leaked frame");
   check("Unwrapped original scene.Render omits the frame; disabling only the guard reproduces and restoring it removes the prior leak");
   await ToggleCheckbox();Mode(false);Assert(RawFrame("frame-nothing-shown")>0,"Explicit frame show produced no framework");RejectFrameClick(Path.Combine(output,"frame-nothing-shown.png"));
   await ToggleMenu();Mode(true);Assert(RawFrame("frame-menu-hidden")==0,"View menu did not hide frame");
   Assert(Convert.ToString(LocalApi.AsDict(LocalApi.AsDict(State()["prefs"])["view"])["frame_regression_marker"])=="preserved","Frame UI erased unrelated saved view preferences");
   using(var release=new ManualResetEvent(false)){
    Reflect.Call(wb,"Run",new Action(delegate{release.WaitOne();}),new Action(delegate{Reflect.Call(wb,"RefreshFromServer");}),false);
    try{Assert(Convert.ToBoolean(Reflect.Get(wb,"busy"))&&!Checkbox().Enabled&&!Menu().Enabled,"Frame controls allow concurrent changes while busy");Reflect.Call(wb,"ChangeFrameMode",false);Mode(true);}finally{release.Set();}await Idle();
   }
   check("Visible checkbox and View menu toggle the same persisted frame mode, preserve other preferences, and recover safely after busy operations; actual framework-pixel clicks cannot interact with hidden stickers");
   var presets=(ComboBox)Reflect.Get(wb,"presets");presets.SelectedIndex=4;Reflect.Call(presets,"OnSelectionChangeCommitted",EventArgs.Empty);await Idle();
   Assert(renderer.Subset.VisibleCount==259800,"Choosing All pieces did not apply the preset immediately");Mode(true);Assert(RawFrame("frame-all-hidden-original")>0,"All pieces are blank");
   adapter.Enabled=false;RawFrame("frame-all-hidden-reference");Same(Path.Combine(output,"frame-all-hidden-original.png"),Path.Combine(output,"frame-all-hidden-reference.png"));adapter.Enabled=originalEnabled;
   await ToggleMenu();Mode(false);RawFrame("frame-all-shown");Assert(!EqualPixels(Path.Combine(output,"frame-all-hidden-original.png"),Path.Combine(output,"frame-all-shown.png")),"All pieces ignored the independent framework setting");
   presets.SelectedIndex=0;Reflect.Call(presets,"OnSelectionChangeCommitted",EventArgs.Empty);await Idle();Assert(renderer.Subset.VisibleCount>0&&renderer.Subset.VisibleCount<259800,"Active orbit preset did not apply");Mode(false);
   await ToggleCheckbox();Mode(true);Assert(RawFrame("frame-filtered-hidden")>0,"Filtered stickers disappeared with framework hide");
   check("Preset selection applies immediately, and independent frame mode works for all 259800 stickers and filtered views with exact original/optimized pixels");
   await Apply("nothing");int calls=0;adapter.RenderScope=delegate{var scope=guard();calls++;try{Assert(Convert.ToInt32(Reflect.Get(cube,"NStk"))==0,"Original ForceUpdate drew hidden stickers");foreach(object face in (Array)Reflect.Get(cube,"StFaces"))Assert(Convert.ToInt32(Reflect.Get(Reflect.Get(face,"Base"),"NE"))==0,"Original ForceUpdate drew framework edges");return scope;}catch{scope.Dispose();throw;}};
   try{Reflect.Call(viewport,"SetSceneChanged");Reflect.Call(viewport,"ForceUpdate");Assert(calls>0,"Original ForceUpdate never reached native drawing");}finally{adapter.RenderScope=guard;}Ownership();
   // Display geometry updates and camera movement retain original mesh records
   // outside draws, but may never restore hidden framework inside a draw.
   Reflect.Call(cube,"SetStickerSize",Convert.ToDouble(Reflect.Get(cube,"FShr")),Convert.ToDouble(Reflect.Get(cube,"SShr")));NativeRendererRegression.NativeDrag(MouseButtons.Right,0,18);Assert(RawFrame("frame-camera-display-hidden")==0,"Camera/display update reintroduced framework");
   check("Original ForceUpdate and camera/display geometry updates pass through the universal policy without leaking full arrays");
   await Apply("active");var preview=api.Post("preview",LocalApi.D("recipe",new object[]{LocalApi.D("kind","word","moves",new[]{1})},"note","Animated frame-policy regression"));int[] turn=NativeRendererRegression.FindNativeTurn(api.Get("certificate"));api.Post("cancel",LocalApi.D());await Native("Run",new Action(delegate{}),new Action(delegate{Reflect.Call(wb,"RefreshFromServer");}),false);
   int rate=Convert.ToInt32(Reflect.Get(form,"TRate"));calls=0;adapter.RenderScope=delegate{var scope=guard();calls++;try{foreach(object face in (Array)Reflect.Get(cube,"StFaces"))Assert(Convert.ToInt32(Reflect.Get(Reflect.Get(face,"Base"),"NE"))==0,"Animated native redraw leaked frame");return scope;}catch{scope.Dispose();throw;}};
   try{Reflect.Set(form,"TRate",125);Reflect.Call(form,"StartAnimation",turn[0],turn[1],turn[2],turn[3]);Assert(calls>0,"Noninstant native animation did not redraw");}finally{Reflect.Set(form,"TRate",rate);adapter.RenderScope=guard;}
   Ownership();Assert(Hash()==initial,"Animation changed the committed journal");foreach(object mesh in fullSlots)Assert(Reflect.Get(mesh,"ExtraTwist")==null,"Animation left transient twist geometry");
   evidence.Add(LocalApi.D("animated_draw_calls",calls,"rate",125));check("Noninstant original StartAnimation redraws obey frame hide and clear transient twist geometry");
   await Native("SaveCheckpoint");var names=LocalApi.Array(State()["checkpoints"]);string checkpoint=Convert.ToString(LocalApi.AsDict(names[0])["name"]);
   await ToggleMenu();Mode(false);await Native("RestoreCheckpoint",checkpoint);Mode(true);await Apply("nothing");Assert(RawFrame("frame-checkpoint-restored")==0,"Checkpoint restoration lost frame mode");
   await ToggleCheckbox();Mode(false);await Native("ResetPuzzle");Mode(false);Assert(RawFrame("frame-reset-retained")>0,"Reset lost explicit frame show");
   await Apply("active");Assert(Hash()==initial,"Frame controls changed full labelled state");Ownership();
   check("Checkpoint restores frame preference and reset retains it, with unchanged full labelled state");Report(true);
  }catch(Exception error){Report(false,error.ToString());throw;}finally{adapter.RenderScope=guard;adapter.Enabled=originalEnabled;renderer.Resume();}
 }
 static bool EqualPixels(string a,string b){var x=Pixels(a);var y=Pixels(b);if(x.Length!=y.Length)return false;for(int i=0;i<x.Length;i++)if(x[i]!=y[i])return false;return true;}
 internal static async Task Reopen(NativeWorkbench workbench,Form window,Control control,LocalApi client,string directory,Action<string> check){Setup(workbench,window,control,client,directory);Mode(false);Assert(renderer.PreparePicking(),"Reopened renderer was not ready");renderer.Pause();try{await Apply("nothing");Assert(RawFrame("frame-reopened-shown")>0,"Reopened session lost explicit frame show");RejectFrameClick(Path.Combine(output,"frame-reopened-shown.png"));await ToggleMenu();Mode(true);Assert(RawFrame("frame-reopened-hidden")==0,"Reopened frame control cannot hide the frame");await Apply("active");check("A new native process reloads the saved explicit frame preference, rejects framework-only pixels, and both mode controls remain functional");Report(true);}finally{renderer.Resume();}}
}
