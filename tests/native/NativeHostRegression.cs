// Runs the actual NativeWorkbench.Build() with real Windows Forms controls.
// Only the unavailable MPUlt renderer/model is replaced by a small fixture.
// This is a UI/startup regression test, NOT native geometry/DirectX evidence.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed class FixtureMesh {
 internal int NV=4, NF=4, NE=6, Rank=19, MinBDim=0;
}
internal sealed class FixtureSticker {
 internal FixtureMesh Base;
 internal int Col;
 internal FixtureSticker(FixtureMesh m){ Base=m; Col=0; }
}
internal sealed class FixtureCube {
 internal FixtureSticker[] Stks;
 internal int ShowRank=0xfff;
 internal FixtureCube(){ var m=new FixtureMesh(); Stks=new[]{new FixtureSticker(m),new FixtureSticker(m)}; }
}
internal sealed class FixturePuzzle { }
internal sealed class FixtureForm : Form {
 internal readonly FixturePuzzle Puz=new FixturePuzzle();
 internal readonly FixtureCube CubeView=new FixtureCube();
 internal readonly Control dxControl2=new Panel();
 internal readonly Panel panel1=new Panel(),panel3=new Panel();
 internal readonly Splitter splitter1=new Splitter(),splitter2=new Splitter();
 internal readonly MenuStrip menuStrip1=new MenuStrip();
 internal FixtureForm() {
  ClientSize=new Size(800,600);
  Controls.Add(dxControl2);Controls.Add(panel1);Controls.Add(panel3);
  Controls.Add(splitter1);Controls.Add(splitter2);Controls.Add(menuStrip1);
 }
}
internal static class NativeHostRegression {
 static readonly List<object> checks=new List<object>();
 static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 static void Check(string name,Action action){action();checks.Add(new {name=name,passed=true});Console.WriteLine("PASS "+name);}
 static object Call(object value,string name,params object[] args){return Reflect.Call(value,name,args);}
 static void AssertOnScreen(Control control,Rectangle parentScreen,string phase){
  // Control.Visible includes every ancestor. A positive ClientSize alone also
  // passes for hidden controls, which was the old fixture's central blind spot.
  Assert(control.Visible,phase+": hidden "+control.GetType().Name+" "+control.Name);
  Assert(control.IsHandleCreated,phase+": missing handle for "+control.Name);
  Assert(control.ClientSize.Width>0&&control.ClientSize.Height>0,phase+": empty "+control.Name);
  Rectangle screen=control.RectangleToScreen(control.ClientRectangle);
  Assert(parentScreen.Contains(screen),phase+": control outside its visible parent: "+control.Name+" "+screen+" parent "+parentScreen);
 }
 static void AssertVisibleLayout(FixtureForm form,NativeWorkbench workbench,bool toolsRequested,string phase){
  var workspace=(NativeDockLayout)Reflect.Get(workbench,"workspace");
  var dock=(Panel)Reflect.Get(workbench,"dock");
  var tabs=(TabControl)Reflect.Get(workbench,"tabs");
  Assert(form.Visible&&form.WindowState!=FormWindowState.Minimized,phase+": form is not shown");
  Assert(workspace.ToolsRequested==toolsRequested,phase+": tools request was lost");
  AssertOnScreen(workspace,form.RectangleToScreen(form.ClientRectangle),phase);
  Rectangle workspaceScreen=workspace.RectangleToScreen(workspace.ClientRectangle);
  AssertOnScreen(workspace.ViewportPanel,workspaceScreen,phase);
  AssertOnScreen(form.dxControl2,workspace.ViewportPanel.RectangleToScreen(workspace.ViewportPanel.ClientRectangle),phase);
  Assert(form.dxControl2.ClientSize==workspace.ViewportPanel.ClientSize,phase+": viewport does not fill its panel");
  int expectedTools=NativeDockLayout.ToolsWidthFor(workspace.ClientSize.Width,workspace.PreferredToolsWidth,toolsRequested);
  if(expectedTools>0){
   AssertOnScreen(workspace.ToolsPanel,workspaceScreen,phase);
   AssertOnScreen(dock,workspace.ToolsPanel.RectangleToScreen(workspace.ToolsPanel.ClientRectangle),phase);
   AssertOnScreen(tabs,dock.RectangleToScreen(dock.ClientRectangle),phase);
   Assert(tabs.SelectedTab!=null&&tabs.SelectedTab.Visible,phase+": selected tools tab is hidden");
   Assert(workspace.ToolsPanel.Width==expectedTools,phase+": incorrect visible tools width");
   Assert(workspace.ViewportPanel.Right<=workspace.ToolsPanel.Left,phase+": tools overlap viewport");
  }else{
   Assert(!workspace.ToolsPanel.Visible&&!dock.Visible&&!tabs.Visible,phase+": hidden tools still visible");
   Assert(workspace.ViewportPanel.ClientSize==workspace.ClientSize,phase+": hidden tools still consume viewport space");
  }
 }
 [STAThread] static int Main(string[] args){
  string output=args.Length>0?Path.GetFullPath(args[0]):Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"winforms-self-test.json");
  bool passed=false;string failure=null;string oldException=null;
  try {
   Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   // Record the original failure independently. Different runtimes may validate
   // minima differently, so the old reproduction is not itself a pass criterion.
   try{using(var bad=new SplitContainer()){bad.Panel1MinSize=600;bad.Panel2MinSize=305;bad.Size=new Size(1400,740);bad.SplitterDistance=1050;}}
   catch(Exception e){oldException=e.GetType().FullName+": "+e.Message;}
   Check("Production dock before parenting, tiny widths, and hide/restore",delegate{
    using(var d=new NativeDockLayout()){
     foreach(int width in new[]{0,1,4,50,149,150,320,604,605,640,900,1000,1440,1920,3840}){
      d.ClientSize=new Size(width,600);d.FitPanels();
      int expected=NativeDockLayout.ToolsWidthFor(width,348,true);
      if(expected>0)Assert(d.ToolsPanel.Width==expected,"Unexpected tools width "+width);
      d.ToolsRequested=false;d.ToolsRequested=true;d.PerformLayout();
     }
    }
   });
   foreach(float scale in new[]{0.75f,1f,1.25f,1.5f,2f,3f}){
    float factor=scale;
    Check("Full NativeWorkbench.Build and lifecycle, scale "+scale,delegate{
     using(var f=new FixtureForm()){
      var wb=new NativeWorkbench(f,"fixture-not-executed","http://127.0.0.1:1","unused-test-token",true);
      f.Text="C600 layout self-test (temporary fixture; no puzzle loaded)";
      var workspace=(NativeDockLayout)Reflect.Get(wb,"workspace");
      Assert(f.dxControl2.Parent==workspace.ViewportPanel,"Viewport not parented into production workspace");
      Assert(((ComboBox)Reflect.Get(wb,"orbit")).Items.Count==35,"Orbit selector incomplete");
      var toolsTabs=(TabControl)Reflect.Get(wb,"tabs");var names=new HashSet<string>();foreach(TabPage page in toolsTabs.TabPages)names.Add(page.Text);
      Assert(names.SetEquals(new[]{"Filters","Solve","Buffers","Progress","Session","Macros","Pieces"}),"Missing native tools components");
      Assert(!f.dxControl2.Enabled,"Input enabled without verified bridge");
      f.Scale(new SizeF(factor,factor));f.Show();f.PerformLayout();Application.DoEvents();
      workspace.FitPanels();Application.DoEvents();AssertVisibleLayout(f,wb,true,"Shown at scale "+factor);
      foreach(var size in new[]{new Size(1000,650),new Size(1440,900),new Size(1920,1080),new Size(1100,700)}){
       f.Size=size;f.PerformLayout();workspace.FitPanels();Application.DoEvents();
       AssertVisibleLayout(f,wb,true,"Resized to "+size);
       Call(wb,"ToggleTools");f.PerformLayout();Application.DoEvents();AssertVisibleLayout(f,wb,false,"Tools hidden at "+size);
       Call(wb,"ToggleTools");f.PerformLayout();Application.DoEvents();AssertVisibleLayout(f,wb,true,"Tools restored at "+size);
      }
      f.WindowState=FormWindowState.Minimized;Application.DoEvents();
      f.WindowState=FormWindowState.Normal;f.PerformLayout();Application.DoEvents();
      workspace.FitPanels();Application.DoEvents();AssertVisibleLayout(f,wb,true,"Restored from minimized");
      // Startup failure must never unlock mutation via a toolbar/menu path.
      Reflect.Set(wb,"busy",false);bool ran=false;
      Call(wb,"Run",new Action(delegate{ran=true;}),new Action(delegate{}),false);
      Thread.Sleep(10);Application.DoEvents();Assert(!ran,"Disconnected operation ran");
      var original=f.CubeView.Stks[0].Base;Call(wb,"PrepareVisibility");
      Assert(original.NV==4&&original.NF==4&&original.NE==6&&original.Rank==19,"Renderer preparation mutated puzzle mesh");
      var hidden=(object[])Reflect.Get(wb,"hiddenBases");
      Assert(Convert.ToInt32(Reflect.Get(hidden[0],"NV"))==0,"Hidden renderer clone retained vertices");
      Assert(!Object.ReferenceEquals(hidden[0],original),"Hidden clone aliases puzzle mesh");
      var km=(Dictionary<Keys,string>)Reflect.Get(wb,"keymap");int count=km.Count;
      try{Call(wb,"SetKeys",LocalApi.D("KeyN","invalid_action"));throw new InvalidOperationException("Invalid keys accepted");}
      catch(TargetInvocationException){ }
      Assert(km.Count==count,"Bad bindings destroyed defaults");
      f.Close();Application.DoEvents();Assert(Convert.ToBoolean(Reflect.Get(wb,"closing")),"Close lifecycle not observed");
      GC.KeepAlive(wb);
     }
    });
   }
   Check("Large native JSON/base64 payload roundtrip",delegate{
    var api=new LocalApi("http://127.0.0.1:1","unused");var colors=new byte[259800*2];var styles=new byte[259800];
    var doc=LocalApi.D("colors",Convert.ToBase64String(colors),"styles",Convert.ToBase64String(styles),"primitive_count","624752715364");
    var copy=LocalApi.AsDict(api.Parse(api.Json(doc)));
    Assert(Convert.FromBase64String((string)copy["colors"]).Length==519600,"Color decode length mismatch");
    Assert(Convert.FromBase64String((string)copy["styles"]).Length==259800,"Style decode length mismatch");
    Assert((string)copy["primitive_count"]=="624752715364","Large count lost precision");
   });
   passed=true;
  } catch(Exception e){failure=e.ToString();Console.Error.WriteLine(failure);}
  var report=new {passed=passed,scope="Actual WinForms controls and production NativeWorkbench.Build; fixture replaces MPUlt model/DirectX; explicit Scale tests are not multi-monitor DPI evidence",os=Environment.OSVersion.ToString(),clr=Environment.Version.ToString(),process_bits=IntPtr.Size*8,original_splitter_exception=oldException,checks=checks,error=failure,utc=DateTime.UtcNow.ToString("o")};
  Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,new JavaScriptSerializer().Serialize(report));
  return passed?0:1;
 }
}
