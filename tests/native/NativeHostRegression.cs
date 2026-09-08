// Runs the actual NativeWorkbench.Build() with real Windows Forms controls.
// Only the unavailable MPUlt renderer/model is replaced by a small fixture.
// This is a UI/startup regression test, NOT native geometry/DirectX evidence.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
 internal FixtureForm(bool legacyFontScaling=false) {
  if(legacyFontScaling){Font=new Font("Courier New",8.25F);AutoScaleMode=AutoScaleMode.Font;AutoScaleDimensions=CurrentAutoScaleDimensions;}
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
 static IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(Control next in Descendants(child))yield return next;}}
 static Button FindButton(Control parent,string text){return Descendants(parent).OfType<Button>().Single(button=>button.Text==text);}
 static FlowLayoutPanel SelectToolsPage(TabControl tabs,string name){tabs.SelectedTab=tabs.TabPages.Cast<TabPage>().Single(page=>page.Text==name);Application.DoEvents();return tabs.SelectedTab.Controls.OfType<FlowLayoutPanel>().Single();}
 static void SetSections(FlowLayoutPanel flow,bool expanded){
  foreach(var button in flow.Controls.OfType<Button>().ToArray()){
   bool closed=button.Text.StartsWith("+ ",StringComparison.Ordinal),open=button.Text.StartsWith("− ",StringComparison.Ordinal);
   if((expanded&&closed)||(!expanded&&open)){flow.ScrollControlIntoView(button);button.PerformClick();}
  }
  flow.AutoScrollPosition=Point.Empty;flow.PerformLayout();Application.DoEvents();
 }
 static void AssertHorizontalBounds(FlowLayoutPanel flow,string phase){
  Assert(!flow.HorizontalScroll.Visible,phase+": horizontal scrollbar appeared; client="+flow.ClientSize+", display="+flow.DisplayRectangle);
  // Vertical overflow is intentional. Check only X bounds, including controls
  // below the current viewport, instead of rejecting valid scrolling content.
  Rectangle viewport=flow.RectangleToScreen(flow.ClientRectangle);
  foreach(Control control in Descendants(flow))if(control.Visible&&control.Width>0){
   Rectangle bounds=control.RectangleToScreen(control.ClientRectangle);
   Assert(bounds.Left>=viewport.Left-1&&bounds.Right<=viewport.Right+1,phase+": horizontal clipping in "+control.GetType().Name+" '"+control.Text+"', bounds="+bounds+", parent="+viewport);
  }
 }
 static void AssertVerticalSiblings(FlowLayoutPanel flow,string phase){
  Control previous=null;foreach(Control child in flow.Controls)if(child.Visible){
   Assert(child.Height>0,phase+": zero-height "+child.GetType().Name+" '"+child.Text+"', maximum="+child.MaximumSize+", autosize="+child.AutoSize);
   if(previous!=null)Assert(child.Top>=previous.Bottom,phase+": vertical overlap between "+previous.GetType().Name+" '"+previous.Text+"' "+previous.Bounds+" and "+child.GetType().Name+" '"+child.Text+"' "+child.Bounds);
   var row=child as FlowLayoutPanel;if(row!=null){foreach(Control nested in row.Controls)if(nested.Visible)Assert(nested.Bottom<=row.ClientSize.Height,phase+": wrapped row has insufficient height for '"+nested.Text+"': child="+nested.Bounds+", row="+row.ClientSize);}
   previous=child;
  }
 }
 static void AssertReachable(FlowLayoutPanel flow,Control control,string phase){
  flow.ScrollControlIntoView(control);Application.DoEvents();Assert(control.Visible&&control.IsHandleCreated,phase+": target is hidden or has no handle");
  Rectangle viewport=flow.RectangleToScreen(flow.ClientRectangle),bounds=control.RectangleToScreen(control.ClientRectangle);
  Assert(bounds.Left>=viewport.Left&&bounds.Right<=viewport.Right,phase+": target is horizontally clipped");
  Assert(bounds.Height>0&&bounds.Width>0&&bounds.IntersectsWith(viewport),phase+": target cannot be reached by vertical scrolling");
  // Small inputs/buttons should fit entirely after scrolling; a tall report is
  // permitted to remain partially below the viewport and use vertical scroll.
  if(bounds.Height<=viewport.Height)Assert(bounds.Top>=viewport.Top-1&&bounds.Bottom<=viewport.Bottom+1,phase+": target remains vertically clipped after ScrollControlIntoView");
 }
 static void WaitUntil(Func<bool> ready,string message){var timer=Stopwatch.StartNew();while(!ready()){if(timer.ElapsedMilliseconds>5000)throw new InvalidOperationException(message);Application.DoEvents();Thread.Sleep(5);}Application.DoEvents();}
 static void ExplorerPreviewOrderingChecks(){
  Check("Out-of-order structure preview preserves newer counts while changed context invalidates",delegate{
   using(var f=new FixtureForm()){
    var wb=new NativeWorkbench(f,"fixture-not-executed","http://127.0.0.1:1","unused-test-token",true);
    f.Text="C600 structure preview ordering fixture (no puzzle loaded)";f.Show();Application.DoEvents();
    try{
     var explorer=(NativeStructureExplorer)Reflect.Get(wb,"explorer");var api=(LocalApi)Reflect.Get(wb,"api");
     var adjacency=Enumerable.Range(0,600).Select(c=>new[]{(c+599)%600+1,(c+598)%600+1,(c+1)%600+1,(c+2)%600+1}).ToArray();
     var vertices=Enumerable.Range(0,120).Select(v=>Enumerable.Range(0,20).Select(c=>(5*v+c)%600+1).ToArray()).ToArray();explorer.Configure("synthetic-preview-topology",adjacency,vertices,null);
     object currentRules=new object[]{LocalApi.D("expr","O33","style","solid")};string initialRules=api.Json(currentRules),initialState="fixture-state";
     var prefs=LocalApi.D("rules",currentRules);Reflect.Set(wb,"state",LocalApi.D("prefs",prefs));Reflect.Set(wb,"stateHash",initialState);
     // Request creation and the actual response handler execute synchronously.
     // The workbench stays disconnected, so no HTTP task is dispatched.
     Func<NativeStructureFilterRequest> request=delegate{
      Call(explorer,"RequestPreview");var value=(NativeStructureFilterRequest)Reflect.Get(explorer,"previewRequest");Assert(value!=null,"Control did not create a preview request");Reflect.Set(wb,"explorerRequest",value.Id);return value;
     };
     var a=request();explorer.NavigateColor(2);var b=request();Assert(b.Id>a.Id,"Requests did not receive ordered identifiers");
     object rulesB=new object[]{LocalApi.D("expr",b.Predicate,"style","solid")};var resultB=LocalApi.D("context_hash","context-B","pieces",12,"stickers",24,"added_pieces",5,"removed_pieces",7);
     Call(wb,"CompleteExplorerPreview",b.Id,initialState,initialRules,rulesB,resultB);
     var apply=(Button)Reflect.Get(explorer,"applyButton");var review=(TextBox)Reflect.Get(explorer,"previewInfo");string acceptedText=review.Text;
     Assert(apply.Enabled&&acceptedText.Contains("12 pieces")&&Object.ReferenceEquals(Reflect.Get(wb,"explorerRules"),rulesB),"Newer response was not accepted");
     Call(wb,"CompleteExplorerPreview",a.Id,initialState,initialRules,new object[0],LocalApi.D("context_hash","context-A","pieces",1,"stickers",1,"added_pieces",1,"removed_pieces",0));
     Assert(apply.Enabled&&review.Text==acceptedText&&Object.ReferenceEquals(Reflect.Get(explorer,"previewRequest"),b)&&Object.ReferenceEquals(Reflect.Get(wb,"explorerRules"),rulesB)&&Convert.ToString(Reflect.Get(wb,"explorerContext"))=="context-B","Older response cleared or replaced the newer accepted preview");
     var stateChangedRequest=request();Reflect.Set(wb,"stateHash","changed-state");Call(wb,"CompleteExplorerPreview",stateChangedRequest.Id,initialState,initialRules,rulesB,resultB);
     Assert(!apply.Enabled&&Reflect.Get(explorer,"previewRequest")==null,"Matching response survived a state change");Reflect.Set(wb,"stateHash",initialState);
     var d=request();prefs["rules"]=new object[]{LocalApi.D("expr","O34","style","solid")};Call(wb,"CompleteExplorerPreview",d.Id,initialState,initialRules,rulesB,resultB);
     Assert(!apply.Enabled&&Reflect.Get(explorer,"previewRequest")==null,"Matching response survived a filter-rule change");
    }finally{Reflect.Set(wb,"busy",false);Reflect.Set(wb,"connected",false);f.Close();GC.KeepAlive(wb);}
   }
  });
 }
 static void ResponsiveToolsChecks(){
  Check("Dedicated UI font preserves the legacy form font and requested 1000px window",delegate{
   using(var legacy=new FixtureForm(true)){
    string originalName=legacy.Font.Name;float originalSize=legacy.Font.SizeInPoints;var wb=new NativeWorkbench(legacy,"fixture-not-executed","http://127.0.0.1:1","unused-test-token",true);
    try{
     legacy.Size=new Size(1000,650);legacy.Show();legacy.PerformLayout();Application.DoEvents();
     Assert(legacy.Font.Name==originalName&&Math.Abs(legacy.Font.SizeInPoints-originalSize)<.01F,"Workbench changed the legacy Form.Font and can trigger designer autoscaling");
     Assert(legacy.Width==1000&&legacy.MinimumSize.Width==1000,"Requested width was altered by font autoscaling: size="+legacy.Size+", minimum="+legacy.MinimumSize);
    }finally{Reflect.Set(wb,"busy",false);Reflect.Set(wb,"connected",false);legacy.Close();Application.DoEvents();GC.KeepAlive(wb);}
   }
  });
  using(var f=new FixtureForm())using(var release=new ManualResetEvent(false))using(var entered=new ManualResetEvent(false)){
   var wb=new NativeWorkbench(f,"fixture-not-executed","http://127.0.0.1:1","unused-test-token",true);
   f.Text="C600 responsive controls fixture (no puzzle loaded)";f.Size=new Size(1200,760);f.Show();Application.DoEvents();
   var workspace=(NativeDockLayout)Reflect.Get(wb,"workspace");var tabs=(TabControl)Reflect.Get(wb,"tabs");var guarded=(List<Control>)Reflect.Get(wb,"guarded");
   try{
    Check("Responsive tools inherit the production Segoe UI font",delegate{
     var dock=(Control)Reflect.Get(wb,"dock");Assert(dock.Font.Name=="Segoe UI"&&Math.Abs(dock.Font.SizeInPoints-9F)<.01F,"Dedicated native tools font differs from Segoe UI 9");
     foreach(string name in new[]{"tabs","filter","presets","hideFrame","pin","target","autoTarget","word","certificate","piecePosition","pieceText"}){var control=(Control)Reflect.Get(wb,name);Assert(control.Font.Name==dock.Font.Name&&Math.Abs(control.Font.SizeInPoints-dock.Font.SizeInPoints)<.01F,"Font did not inherit in "+name);}
     workspace.PreferredToolsWidth=240;workspace.FitPanels();Application.DoEvents();Assert(workspace.ToolsPanel.Width==320,"Sidebar shrank below the readable320px minimum");
     tabs.SelectedTab=tabs.TabPages.Cast<TabPage>().Single(page=>page.Text=="Structure");Application.DoEvents();var explorer=(NativeStructureExplorer)Reflect.Get(wb,"explorer");AssertOnScreen(explorer,tabs.SelectedTab.RectangleToScreen(tabs.SelectedTab.ClientRectangle),"Structure at minimum sidebar width");
     // A small synthetic regular graph supplies real label/button counts for
     // layout only; this fixture does not certify the600-cell topology.
     var adjacency=Enumerable.Range(0,600).Select(c=>new[]{(c+599)%600+1,(c+598)%600+1,(c+1)%600+1,(c+2)%600+1}).ToArray();
     var vertices=Enumerable.Range(0,120).Select(v=>Enumerable.Range(0,20).Select(c=>(5*v+c)%600+1).ToArray()).ToArray();explorer.Configure("synthetic-layout-topology",adjacency,vertices,null);
     var tasks=(TabControl)Reflect.Get(explorer,"navigation");foreach(TabPage task in tasks.TabPages){tasks.SelectedTab=task;Application.DoEvents();AssertHorizontalBounds(task.Controls.OfType<FlowLayoutPanel>().Single(),"Structure "+task.Text+" at 320px sidebar");}
     tasks.SelectedIndex=0;FindButton(explorer,"Use cell").PerformClick();Assert(explorer.SelectedPredicate=="cell(C1)","Direct current-cell filter preparation is missing");FindButton(explorer,"Use neighbors").PerformClick();Assert(explorer.SelectedPredicate=="adjacent(C1)","Direct neighbor filter preparation is missing");FindButton(explorer,"Use color").PerformClick();Assert(explorer.SelectedPredicate=="color(C1)","Direct color filter preparation is missing");
    });
    Check("Primary editors and selectors retain usable heights after responsive fitting",delegate{
     foreach(int width in new[]{320,348}){
      workspace.PreferredToolsWidth=width;workspace.FitPanels();Application.DoEvents();
      foreach(string pageName in new[]{"Filters","Solve","Pieces"}){
       var flow=SelectToolsPage(tabs,pageName);string[] fields=pageName=="Filters"?new[]{"filter","presets"}:pageName=="Solve"?new[]{"word","certificate"}:new[]{"pieceText"};
       foreach(string field in fields){var control=(Control)Reflect.Get(wb,field);int minimum=field=="presets"?control.Font.Height+4:field=="pieceText"?180:field=="certificate"?120:48;
        Assert(control.Height>=minimum,pageName+" "+field+" collapsed at sidebar="+width+": bounds="+control.Bounds+", client="+control.ClientSize+", maximum="+control.MaximumSize+", minimum="+control.MinimumSize+", autosize="+control.AutoSize+", expected height >= "+minimum);
       }
       AssertVerticalSiblings(flow,pageName+" at "+width);
      }
     }
    });
    foreach(string pageName in new[]{"Filters","Solve","Pieces"}){
     string name=pageName;
     Check(name+" has no horizontal overflow at 320/348px, with reachable controls",delegate{
      foreach(int width in new[]{320,348}){
       workspace.PreferredToolsWidth=width;workspace.FitPanels();Application.DoEvents();Assert(workspace.ToolsPanel.Width==width,"Requested sidebar width was not used");var flow=SelectToolsPage(tabs,name);
       if(name=="Filters")SetSections(flow,false);AssertHorizontalBounds(flow,name+" collapsed at "+width);AssertVerticalSiblings(flow,name+" collapsed at "+width);
       string[] fields=name=="Filters"?new[]{"hideFrame","presets","filter","pin"}:name=="Solve"?new[]{"autoTarget","target","batch","word","certificate"}:new[]{"piecePosition","pieceText"};
       foreach(string field in fields)AssertReachable(flow,(Control)Reflect.Get(wb,field),name+" "+field+" at "+width);
       if(name=="Filters"){SetSections(flow,true);AssertHorizontalBounds(flow,name+" expanded at "+width);AssertVerticalSiblings(flow,name+" expanded at "+width);foreach(string field in new[]{"types","setName","setExpression","setKind"})AssertReachable(flow,(Control)Reflect.Get(wb,field),name+" expanded "+field+" at "+width);SetSections(flow,false);}
       flow.AutoScrollPosition=Point.Empty;
      }
     });
    }
    Check("Narrow wrapped checkboxes retain their full text height",delegate{
     workspace.PreferredToolsWidth=320;workspace.FitPanels();Application.DoEvents();
     foreach(string name in new[]{"hideFrame","pin","autoTarget"}){
      SelectToolsPage(tabs,name=="autoTarget"?"Solve":"Filters");var box=(CheckBox)Reflect.Get(wb,name);int textHeight=TextRenderer.MeasureText(box.Text,box.Font,new Size(Math.Max(20,box.Width-22),0),TextFormatFlags.WordBreak).Height;
      Assert(!box.AutoSize&&box.Height>=textHeight+2,"Wrapped checkbox text clips in "+name+": box="+box.Size+", required text height="+textHeight);
     }
    });
    Check("Filter sections expand locally without altering their drafts",delegate{
     var flow=SelectToolsPage(tabs,"Filters");var editor=(TextBox)Reflect.Get(wb,"filter");var setExpression=(TextBox)Reflect.Get(wb,"setExpression");editor.Text="color(C17) & unsolved";setExpression.Text="layer(C17,L3)";
     SetSections(flow,false);Assert(!((Control)Reflect.Get(wb,"types")).Visible&&!setExpression.Visible,"Collapsed optional sections remain shown");
     SetSections(flow,true);Assert(((Control)Reflect.Get(wb,"types")).Visible&&setExpression.Visible,"Optional sections cannot be opened while disconnected");
     SetSections(flow,false);Assert(editor.Text=="color(C17) & unsolved"&&setExpression.Text=="layer(C17,L3)","Section toggle changed a filter draft");
    });
    Check("Disconnected ordinary actions stay disabled while local navigation remains available",delegate{
     Assert(!f.dxControl2.Enabled&&guarded.All(control=>!control.Enabled),"Disconnected fixture enabled a mutation control");
     var flow=SelectToolsPage(tabs,"Filters");Assert(FindButton(flow,"Structure…").Enabled,"Local Structure navigation is disabled before connection");
     bool ran=false;Reflect.Set(wb,"busy",false);Call(wb,"Run",new Action(delegate{ran=true;}),new Action(delegate{}),false);Application.DoEvents();Assert(!ran,"Disconnected ordinary operation ran");
    });
    Check("Production Run restores connected button states and keeps cancellation available while busy",delegate{
     // Only model connection is synthesized. Production Run and its asynchronous
     // completion control-state logic execute; profile stays null, so no HTTP.
     Reflect.Set(wb,"busy",false);Call(wb,"Run",new Action(delegate{}),new Action(delegate{Reflect.Set(wb,"connected",true);}),true);
     WaitUntil(delegate{return !Convert.ToBoolean(Reflect.Get(wb,"busy"));},"Fixture connection completion did not settle");Assert(guarded.All(control=>control.Enabled)&&f.dxControl2.Enabled,"Connected completion did not enable ordinary actions");
     Call(wb,"Run",new Action(delegate{entered.Set();release.WaitOne(5000);}),new Action(delegate{}),false);
     WaitUntil(delegate{return entered.WaitOne(0);},"Fixture worker did not begin");Assert(guarded.All(control=>!control.Enabled)&&!f.dxControl2.Enabled,"Busy operation left ordinary mutation controls enabled");
     var flow=SelectToolsPage(tabs,"Solve");Assert(FindButton(flow,"Cancel").Enabled,"Cancel is disabled during a busy operation");release.Set();
     WaitUntil(delegate{return !Convert.ToBoolean(Reflect.Get(wb,"busy"));},"Fixture worker did not finish");Assert(guarded.All(control=>control.Enabled)&&f.dxControl2.Enabled,"Idle completion did not restore controls");
    });
   }finally{release.Set();Reflect.Set(wb,"busy",false);Reflect.Set(wb,"connected",false);f.Close();Application.DoEvents();GC.KeepAlive(wb);}
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
      Assert(names.SetEquals(new[]{"Filters","Solve","Buffers","Progress","Session","Macros","Pieces","Structure","Views"}),"Missing native tools components");
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
   ResponsiveToolsChecks();
   ExplorerPreviewOrderingChecks();
   passed=true;
  } catch(Exception e){failure=e.ToString();Console.Error.WriteLine(failure);}
  var report=new {passed=passed,scope="Actual WinForms controls and production NativeWorkbench.Build; fixture replaces MPUlt model/DirectX; explicit Scale tests are not multi-monitor DPI evidence",os=Environment.OSVersion.ToString(),clr=Environment.Version.ToString(),process_bits=IntPtr.Size*8,original_splitter_exception=oldException,checks=checks,error=failure,utc=DateTime.UtcNow.ToString("o")};
  Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,new JavaScriptSerializer().Serialize(report));
  return passed?0:1;
 }
}
