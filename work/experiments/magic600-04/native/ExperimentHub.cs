// Isolated native workspace. This control owns display and inspection only.
// All role changes, state mutations and transactions belong to the host.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal sealed partial class ExperimentHub : Control {
 readonly JavaScriptSerializer objectKeyJson=new JavaScriptSerializer{MaxJsonLength=Int32.MaxValue};
 [DllImport("user32.dll",EntryPoint="SendMessageW")]static extern IntPtr HubSendMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
 [DllImport("user32.dll",EntryPoint="IsWindowVisible")]static extern bool HubIsWindowVisible(IntPtr window);
 [DllImport("user32.dll",EntryPoint="RedrawWindow")]static extern bool HubRedrawWindow(IntPtr window,IntPtr updateRect,IntPtr updateRegion,uint flags);
 static void CollectVisibleHubHandles(Control control,List<KeyValuePair<Control,IntPtr>> handles){
  if(control.IsHandleCreated&&HubIsWindowVisible(control.Handle))handles.Add(new KeyValuePair<Control,IntPtr>(control,control.Handle));
  foreach(Control child in control.Controls)CollectVisibleHubHandles(child,handles);
 }
 internal void SetInteractionEnabled(bool value){
  if(Enabled==value){Enabled=value;return;}
  // Only batch the Enabled transition: scene rebuilds can change visibility.
  // Snapshot every visible HWND before WM_SETREDRAW removes WS_VISIBLE.
  var handles=new List<KeyValuePair<Control,IntPtr>>();CollectVisibleHubHandles(this,handles);
  try{
   foreach(var pair in handles)HubSendMessage(pair.Value,0x000B,IntPtr.Zero,IntPtr.Zero);
   Enabled=value;
  }finally{
   foreach(var pair in handles)if(!pair.Key.IsDisposed&&pair.Key.IsHandleCreated&&pair.Key.Handle==pair.Value)HubSendMessage(pair.Value,0x000B,new IntPtr(1),IntPtr.Zero);
   if(IsHandleCreated&&!IsDisposed)HubRedrawWindow(Handle,IntPtr.Zero,IntPtr.Zero,0x0485); // Invalidate, erase, frame, all children; paint remains observable.
  }
 }
 readonly bool hubTimingEnabled=Environment.GetEnvironmentVariable("MAGIC600_SEND_TIMING")=="1";
 long hubLayoutCalls,hubLayoutTicks,hubSceneCalls,hubSceneTicks,hubIdentityCalls,hubIdentityTicks,hubStickerCalls,hubStickerTicks,hubResumeCalls,hubResumeTicks;
 long[] BeginHubTiming(){return hubTimingEnabled?new[]{Stopwatch.GetTimestamp(),hubLayoutCalls,hubLayoutTicks,hubSceneCalls,hubSceneTicks,hubIdentityCalls,hubIdentityTicks,hubStickerCalls,hubStickerTicks,hubResumeCalls,hubResumeTicks}:null;}
 void WriteHubTiming(string operation,long[] start){
  if(start==null)return;
  long completed=Stopwatch.GetTimestamp();
  try{double ms=1000.0/Stopwatch.Frequency;NativeDiagnostics.Write(String.Format(System.Globalization.CultureInfo.InvariantCulture,
   "Experiment hub timing: operation={0} total_ms={1:F3} layout_calls={2} layout_ms={3:F3} scene_calls={4} scene_ms={5:F3} identity_calls={6} identity_ms={7:F3} sticker_calls={8} sticker_ms={9:F3} resume_calls={10} resume_ms={11:F3}",
   operation,(completed-start[0])*ms,hubLayoutCalls-start[1],(hubLayoutTicks-start[2])*ms,hubSceneCalls-start[3],(hubSceneTicks-start[4])*ms,hubIdentityCalls-start[5],(hubIdentityTicks-start[6])*ms,hubStickerCalls-start[7],(hubStickerTicks-start[8])*ms,hubResumeCalls-start[9],(hubResumeTicks-start[10])*ms));}catch{}
 }
 internal Rectangle[] PersistentContextBounds {get{return new Control[]{caption,tracking,reviewStatus}.Where(c=>c.Visible&&c.IsHandleCreated&&c.Width>0&&c.Height>0).Select(c=>c.RectangleToScreen(c.ClientRectangle)).ToArray();}}
 readonly Label heading = new ReadoutLabel();
 readonly Panel caption = new Panel();
 readonly FlowLayoutPanel phaseBar = new FlowLayoutPanel();
 readonly List<Button> phaseButtons = new List<Button>();
 readonly ComboBox cyclePicker = new ComboBox();
 readonly ComboBox effectScopePicker = new ComboBox();
 readonly TextBox frameReadout = new TextBox();
 readonly Panel entryPane=new Panel();
 readonly Button entryToggle=new ResultButton(),entryClose=new Button();
 readonly ToolStripMenuItem entryMenu=new ToolStripMenuItem("&Entry slots");
 readonly List<Button> forecastObjects = new List<Button>();
 readonly List<Button> blockObjects = new List<Button>();
 readonly List<int> cyclePositions = new List<int>();
 readonly List<CycleSegment> cycleSegments = new List<CycleSegment>();
 readonly Dictionary<int,PointF> socketCenters=new Dictionary<int,PointF>();
 float workGroupY,sceneBottom;
 int inspectedCycleSource=-1,inspectedCycleDestination=-1;
 readonly Panel tracking = new Panel();
 readonly LinkLabel currentTrace=new ReadoutLinkLabel(),nextTrace=new ReadoutLinkLabel();
 readonly CheckBox comparison = new ComparisonCheckBox();
 readonly Panel scroll = new Panel();
 readonly Panel canvas = new Panel();
 readonly ContextMenuStrip objectMenu = new ContextMenuStrip();
 readonly Button objectActions = new Button();
 readonly Button activateNext = new ResultButton();
 readonly Button browseMacro = new Button();
 readonly ToolStripMenuItem addHomeMenu = new ToolStripMenuItem("Add &Home");
 readonly Button reviewStatus = new ResultButton();
 readonly ToolTip tip = new ToolTip();
 readonly List<Socket> sockets = new List<Socket>();
 readonly List<Button> objects = new List<Button>();
 readonly HashSet<int> reviewEvidence = new HashSet<int>();
 string reviewEvidenceContext;
 readonly List<ToolStripMenuItem> actionItems = new List<ToolStripMenuItem>();
 NativeCellGeometry geometry;
 Dictionary<string,object> snapshot, effect;
 Dictionary<string,object> phaseInspection;
 string phaseReason="";
 int selectedCycle;
 bool syncingCycle;
 bool syncingEffectScope;
 string selectedKind, scope = "body";
 int selectedId = -1;
 int? gripCap;
 bool requestedPrediction;
 bool selectedForecast;
 bool withdrawingPhase;
 int pendingForecastIdentity=-1;
 string pendingForecastBoundary="";
 bool syncingComparison,menuPosted,rebuilding;
 ObjectTag pendingMenu;
 int workSocketCount;
 readonly Color ink = Color.FromArgb(223,230,234), muted = Color.FromArgb(147,164,173), line = Color.FromArgb(53,66,76);
 readonly Color blue = Color.FromArgb(108,187,212), green = Color.FromArgb(134,198,165), amber = Color.FromArgb(228,182,106), red = Color.FromArgb(238,141,135);
 readonly Color surface=Color.FromArgb(25,33,41),raised=Color.FromArgb(34,45,56);

 sealed class Socket {
  internal int Position, Identity = -1, Orbit = -2, Index;
  internal string Role;
  internal Dictionary<string,object> Member;
  internal int[] Hosting = new int[0];
  internal int[] Home = new int[0];
  internal int AfterIdentity=-1;
  internal int[] AfterHome=new int[0];
  internal bool AfterKnown;
  internal string MacroRole;
  internal string MacroBinding="";
  internal int TrackedIndex=-1;
  internal int CycleIndex=-1, RailIndex=-1;
  internal int BlockIndex=-1,CollateralIndex=-1;
  internal bool? RequirementMet;
  internal bool Current, Next, Protected, Conflict, FrameChanged;
  internal bool? BoundaryMet;
  internal Dictionary<string,object> Actual, Forecast;
  internal Dictionary<string,object> ActualFilter,ForecastFilter;
 }
 sealed class CycleSegment {internal GraphicsPath Path;internal int Source,Destination;}
 sealed class ObjectTag {
  internal string Kind;
  internal int Id;
  internal bool Forecast;
  internal ObjectTag(string kind,int id,bool forecast=false) { Kind=kind; Id=id;Forecast=forecast; }
 }
 static void PaintDisabledReadout(Control control,Graphics graphics) {
  graphics.Clear(control.BackColor);var area=control.ClientRectangle;area.X+=control.Padding.Left;area.Y+=control.Padding.Top;area.Width-=control.Padding.Horizontal;area.Height-=control.Padding.Vertical;
  TextRenderer.DrawText(graphics,control.Text,control.Font,area,Color.FromArgb(147,164,173),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
 }
 sealed class ReadoutLabel : Label {protected override void OnPaint(PaintEventArgs e){if(Enabled)base.OnPaint(e);else PaintDisabledReadout(this,e.Graphics);}}
 sealed class ReadoutLinkLabel : LinkLabel {protected override void OnPaint(PaintEventArgs e){if(Enabled)base.OnPaint(e);else PaintDisabledReadout(this,e.Graphics);}}
 sealed class ComparisonCheckBox : CheckBox {
  protected override void OnPaint(PaintEventArgs e) {
   var g=e.Graphics;g.Clear(BackColor);using(var pen=new Pen(Checked?Color.FromArgb(108,187,212):Color.FromArgb(53,66,76)))g.DrawRectangle(pen,0,0,Width-1,Height-1);
   TextRenderer.DrawText(g,Text,Font,new Rectangle(2,Math.Max(2,(Height-Font.Height)/2),Width-4,Font.Height),Enabled?ForeColor:Color.FromArgb(147,164,173),TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(3,3,Width-6,Height-6),ForeColor,BackColor);
  }
 }
 sealed class ResultButton : Button {
  protected override void OnPaint(PaintEventArgs e) {
   var g=e.Graphics;g.Clear(BackColor);if(FlatAppearance.BorderSize>0)using(var pen=new Pen(FlatAppearance.BorderColor))g.DrawRectangle(pen,0,0,Width-1,Height-1);
   string[] lines=Text.Split('\n');int y=Math.Max(2,(Height-lines.Length*Font.Height)/2);
   foreach(string text in lines){TextRenderer.DrawText(g,text,Font,new Rectangle(2,y,Width-4,Font.Height),Enabled?ForeColor:Color.FromArgb(147,164,173),(TextAlign==ContentAlignment.MiddleLeft?TextFormatFlags.Left:TextFormatFlags.HorizontalCenter)|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);y+=Font.Height;}
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(3,3,Width-6,Height-6),ForeColor,BackColor);
  }
 }
 sealed class ObjectButton : Button {
  internal string PresentationKey;
  internal bool IdentityToken;
  internal bool Forecast;
  internal string RoleMark;
  internal Color RoleColor;
  internal string PolicyMark;
  internal Color PolicyColor;
  internal string BindingMark;
  internal bool? FilterMatch;
  internal bool GripHighlighted;
  internal bool EvidenceHighlighted;
  protected override void OnPaint(PaintEventArgs e) {
   var g=e.Graphics;g.Clear(IdentityToken&&FilterMatch==false?Color.FromArgb(20,27,34):BackColor);int bindingHeight=String.IsNullOrEmpty(BindingMark)?0:Font.Height+2;
   int border=FlatAppearance.BorderSize;
   if(border>0)using(var pen=new Pen(FlatAppearance.BorderColor,border))g.DrawRectangle(pen,border*.5f,border*.5f,Width-border,Height-border);
   string[] nameRows=Text.Split('\n');int roleHeight=String.IsNullOrEmpty(RoleMark)?0:Font.Height;
   int textTop=IdentityToken?8:2;
   if(Forecast)using(var pen=new Pen(ForeColor,1.4f)){pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,1,1,Width-3,Height-3);}
   if(IdentityToken&&Image!=null)DrawFilteredImage(g,Image,new Point((Width-Image.Width)/2,2),FilterMatch);
   if(roleHeight>0)TextRenderer.DrawText(g,RoleMark,Font,new Rectangle(2,textTop,Width-4,Font.Height),IdentityToken?ForeColor:RoleColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
   textTop+=roleHeight;
   foreach(string row in nameRows){TextRenderer.DrawText(g,row,Font,new Rectangle(3,textTop,Width-6,Font.Height),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);textTop+=Font.Height;}
   if(!IdentityToken&&Image!=null){using(var pen=new Pen(FlatAppearance.BorderColor,Focused?2:1)){g.DrawLine(pen,1,1,1,Height-2);g.DrawLine(pen,Width-2,1,Width-2,Height-2);}int imageTop=textTop+3;g.DrawImage(Image,new Rectangle(3,imageTop,Width-6,Math.Max(1,Height-imageTop-3-bindingHeight)));}
   if(GripHighlighted)using(var pen=new Pen(Color.FromArgb(231,241,248),1.5f))g.DrawRectangle(pen,4,4,Math.Max(1,Width-9),Math.Max(1,Height-9));
   if(EvidenceHighlighted)using(var pen=new Pen(Color.FromArgb(231,241,248),2)){int x=Width-6,y=Height-6;g.DrawLine(pen,5,13,5,5);g.DrawLine(pen,5,5,13,5);g.DrawLine(pen,x-8,y,x,y);g.DrawLine(pen,x,y,x,y-8);}
   if(IdentityToken&&FilterMatch!=true)TextRenderer.DrawText(g,FilterMatch==false?"×":"?",Font,new Rectangle(2,Height-Font.Height-3,12,Font.Height+2),FilterMatch==false?Color.FromArgb(147,164,173):Color.FromArgb(228,182,106),TextFormatFlags.Left|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
   if(bindingHeight>0)TextRenderer.DrawText(g,BindingMark,Font,new Rectangle(1,Height-bindingHeight,Width-2,bindingHeight),Color.FromArgb(228,182,106),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
   if(!IdentityToken&&!Forecast&&!String.IsNullOrEmpty(PolicyMark)) {float scale=Math.Max(1,Font.Height/19f);using(var pen=new Pen(PolicyColor,1.5f*scale)){if(PolicyMark=="locked"){g.DrawArc(pen,4*scale,4*scale,7*scale,8*scale,180,180);g.DrawRectangle(pen,3*scale,8*scale,9*scale,8*scale);}else if(PolicyMark=="conflict"){g.DrawLine(pen,3*scale,5*scale,12*scale,15*scale);g.DrawLine(pen,12*scale,5*scale,3*scale,15*scale);}else TextRenderer.DrawText(g,"?",Font,new Rectangle(0,0,(int)(16*scale),Font.Height+3),PolicyColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding);}}
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(3,3,Width-6,Height-6),ForeColor,BackColor);
  }
  protected override bool IsInputKey(Keys keyData) {
   Keys key=keyData&Keys.KeyCode;
   return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||base.IsInputKey(keyData);
  }
 }

 internal event Action<string,int> ObjectSelected;
 internal event Action<string,string,int> ActionRequested;
 internal event Action<string> CommandRequested;
 internal event Action<string> PhaseRequested;
 internal event Action<string> EffectScopeRequested;
 internal Func<string,string> KeyLabel {get;set;}
 internal Func<int,string> OrbitLabel {get;set;}
 internal Func<int,string> CellLabel {get;set;}
 internal Func<int,string> SlotLabel {get;set;}
 internal string SelectedKind { get { return selectedKind; } }
 internal int SelectedId { get { return selectedId; } }
 internal bool SelectionIsForecast {get{return selectedForecast&&PhaseForecast;}}
 internal string EffectScope {
  get { return scope; }
  set { if(value!="body"&&value!="complete") throw new ArgumentException("Effect scope must be body or complete."); if(scope!=value){scope=value;Rebuild();} }
 }
 internal bool Predicted {
  get { string state=ValueText(Get(Map(Get(snapshot,"review")),"status"));return cycleMode=="Operation" && !OperationExecuted && !withdrawingPhase && requestedPrediction && (state=="Ready"||state=="Staged") && Map(Get(snapshot,"predicted"))!=null; }
  set { requestedPrediction=value; Rebuild(); }
 }
 internal void SetPhaseInspection(Dictionary<string,object> result) {
  SetPhaseData(result);Rebuild();
 }
 void SetPhaseData(Dictionary<string,object> result) {
  string boundary=ValueText(Get(result,"boundary"));
  if(boundary!="actual"&&boundary!="prepare"&&boundary!="macro"&&boundary!="cleanup")throw new ArgumentException("Unknown phase inspection boundary.");
  if(OperationExecuted&&boundary!="actual")throw new ArgumentException("Executed steps have no draft forecast. Choose New or Reuse steps first.");
  phaseInspection=result;phaseReason="";withdrawingPhase=false;
  if(boundary!="actual"&&boundary==pendingForecastBoundary&&pendingForecastIdentity>=0&&SelectionAcknowledged("identity",pendingForecastIdentity)) {
   bool present=false;foreach(var pair in PieceMap(result))if(Id(Get(pair.Value,"piece"))==pendingForecastIdentity){present=true;break;}
   selectedForecast=present;selectedKind="identity";selectedId=pendingForecastIdentity;if(!present)pendingForecastIdentity=-1;
  }else if(boundary=="actual"||pendingForecastIdentity>=0){pendingForecastIdentity=-1;selectedForecast=false;}
 }
 internal void ClearPhaseInspection(string reason) {var timing=BeginHubTiming();try{ClearPhaseInspectionCore(reason);}finally{WriteHubTiming("ClearPhaseInspection",timing);}}
 void ClearPhaseInspectionCore(string reason) {WithdrawCycleProjection();phaseInspection=null;phaseReason=reason??"";withdrawingPhase=true;selectedForecast=false;ClearCycleHits();foreach(Button button in forecastObjects){if(button.Image!=null)button.Image.Dispose();button.Dispose();}forecastObjects.Clear();foreach(Socket socket in sockets){socket.Conflict=false;socket.FrameChanged=false;}cyclePicker.Visible=false;RefreshTraces();RefreshScene();}
 void ClearCycleHits(){foreach(var segment in cycleSegments)segment.Path.Dispose();cycleSegments.Clear();inspectedCycleSource=inspectedCycleDestination=-1;}
 bool OperationExecuted {get{return ValueText(Get(snapshot,"operation_state"))=="executed";}}
 string Boundary {get{return OperationExecuted||cycleMode=="Current"?"actual":ResidualAfter?"cleanup":ValueText(Get(phaseInspection,"boundary"));}}
 bool PhaseForecast {get{return !OperationExecuted&&(cycleMode=="After"?ResidualAfter:cycleMode=="Operation"&&phaseInspection!=null&&Boundary!="actual");}}
 Dictionary<string,object> MacroEntry {get{return OperationExecuted||cycleMode!="Operation"?null:Map(Get(phaseInspection,"macro_entry"));}}

 internal ExperimentHub() {
  Font = new Font("Segoe UI",10.5f);
  BackColor = Color.FromArgb(17,22,28); ForeColor=ink;
  AccessibleName="Block, buffers and operation workspace";
  AccessibleDescription="Fixed position sockets contain physical identity tokens. Selection inspects; role changes require an explicit action.";
  TabStop=false;
  caption.Dock=DockStyle.Top;heading.Padding=new Padding(5,0,3,0);heading.TextAlign=ContentAlignment.MiddleLeft;heading.AutoEllipsis=true;
  comparison.Appearance=Appearance.Button;comparison.TextAlign=ContentAlignment.MiddleCenter;comparison.AccessibleName="Show exact complete operation result";
  comparison.FlatStyle=FlatStyle.Flat;comparison.BackColor=raised;
  comparison.CheckedChanged+=delegate {if(!syncingComparison)Predicted=comparison.Checked;};
  caption.Controls.Add(heading);caption.Controls.Add(comparison);
  phaseBar.WrapContents=false;phaseBar.Padding=new Padding(2,0,2,0);phaseBar.BackColor=surface;caption.Controls.Add(phaseBar);
  foreach(string boundary in new[]{"actual","prepare","macro","cleanup"}) {
   string requested=boundary;var button=new ResultButton{Text=boundary=="actual"?"Actual":"After "+Char.ToUpperInvariant(boundary[0])+boundary.Substring(1),Tag=boundary,FlatStyle=FlatStyle.Flat,Margin=new Padding(2,0,2,0),AccessibleName="Inspect "+boundary+" boundary"};button.FlatAppearance.BorderSize=0;
   button.Click+=delegate{if(PhaseRequested!=null)PhaseRequested(requested);};phaseButtons.Add(button);phaseBar.Controls.Add(button);
  }
  scroll.Dock=DockStyle.Fill; scroll.AutoScroll=true; scroll.TabStop=false;
  canvas.TabStop=false;canvas.Paint+=PaintCanvas;canvas.MouseDown+=CanvasSelect;scroll.Controls.Add(canvas);
  tracking.Dock=DockStyle.Top;tracking.Visible=true;
  foreach(var trace in new[]{currentTrace,nextTrace}){trace.TextAlign=ContentAlignment.MiddleLeft;trace.LinkBehavior=LinkBehavior.HoverUnderline;trace.LinkColor=blue;trace.ActiveLinkColor=amber;trace.VisitedLinkColor=blue;trace.LinkClicked+=delegate(object sender,LinkLabelLinkClickedEventArgs e){var tag=e.Link.LinkData as ObjectTag;if(tag!=null){CancelPendingInteraction();InspectTag(tag);}};tracking.Controls.Add(trace);}
  Controls.Add(scroll);Controls.Add(tracking);Controls.Add(caption);
  cyclePicker.DropDownStyle=ComboBoxStyle.DropDownList;cyclePicker.FlatStyle=FlatStyle.Flat;cyclePicker.BackColor=surface;cyclePicker.ForeColor=ink;cyclePicker.AccessibleName="Inspect an exact directed macro cycle";cyclePicker.SelectedIndexChanged+=delegate{if(!syncingCycle){if(SelectDetailedCycle(cyclePicker.SelectedIndex))return;selectedCycle=cyclePicker.SelectedIndex;inspectedCycleSource=inspectedCycleDestination=-1;Rebuild();}};cyclePicker.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter&&DetailedEdges.Count>0){var edges=DetailedEdges.Cast<object>().Select(Map).ToList();int at=edges.FindIndex(x=>Id(Get(x,"source_position"))==inspectedCycleSource&&Id(Get(x,"destination_position"))==inspectedCycleDestination);var edge=edges[(at+1)%edges.Count];inspectedCycleSource=Id(Get(edge,"source_position"));inspectedCycleDestination=Id(Get(edge,"destination_position"));entryPane.Visible=true;RefreshFrameReadout();LayoutScene();e.Handled=true;e.SuppressKeyPress=true;return;}if(e.KeyCode==Keys.Enter&&!cycleDisplayActive&&cyclePositions.Count>1){int index=cyclePositions.IndexOf(inspectedCycleSource);index=(index+1)%cyclePositions.Count;inspectedCycleSource=cyclePositions[index];inspectedCycleDestination=cyclePositions[(index+1)%cyclePositions.Count];RefreshFrameReadout();var target=objects.Find(b=>((ObjectTag)b.Tag).Kind=="position"&&((ObjectTag)b.Tag).Id==inspectedCycleSource);if(target!=null)target.Focus();e.Handled=true;e.SuppressKeyPress=true;}};canvas.Controls.Add(cyclePicker);
  cyclePicker.DrawMode=DrawMode.OwnerDrawFixed;cyclePicker.ItemHeight=Font.Height+4;cyclePicker.DrawItem+=delegate(object sender,DrawItemEventArgs e){using(var brush=new SolidBrush((e.State&DrawItemState.Selected)!=0?raised:surface))e.Graphics.FillRectangle(brush,e.Bounds);string text=e.Index>=0?ValueText(cyclePicker.Items[e.Index]):cyclePicker.Text;TextRenderer.DrawText(e.Graphics,text,Font,e.Bounds,ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);if((e.State&DrawItemState.Focus)!=0)ControlPaint.DrawFocusRectangle(e.Graphics,e.Bounds,blue,surface);};
  effectScopePicker.Items.AddRange(new object[]{"Macro body","All steps"});effectScopePicker.DropDownStyle=ComboBoxStyle.DropDownList;effectScopePicker.FlatStyle=FlatStyle.Flat;effectScopePicker.BackColor=surface;effectScopePicker.ForeColor=ink;effectScopePicker.DrawMode=DrawMode.OwnerDrawFixed;effectScopePicker.ItemHeight=Font.Height+4;effectScopePicker.AccessibleName="Displayed effect scope";
  effectScopePicker.DrawItem+=delegate(object sender,DrawItemEventArgs e){using(var brush=new SolidBrush((e.State&DrawItemState.Selected)!=0?raised:surface))e.Graphics.FillRectangle(brush,e.Bounds);string text=e.Index>=0?ValueText(effectScopePicker.Items[e.Index]):effectScopePicker.Text;TextRenderer.DrawText(e.Graphics,text,Font,e.Bounds,effectScopePicker.Enabled?ink:muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);if((e.State&DrawItemState.Focus)!=0)ControlPaint.DrawFocusRectangle(e.Graphics,e.Bounds,blue,surface);};
  effectScopePicker.SelectedIndexChanged+=delegate{if(syncingEffectScope)return;if(CycleScopeRequested!=null){string graphScope=effectScopePicker.SelectedIndex==0?"selected-macro":effectScopePicker.SelectedIndex==1?"macro-steps":"complete";CycleScopeRequested(graphScope);return;}string requested=effectScopePicker.SelectedIndex==0?"body":"complete";syncingEffectScope=true;effectScopePicker.SelectedIndex=CycleScopeRequested!=null?CycleScopeIndex:scope=="body"?0:1;syncingEffectScope=false;if(requested!=scope&&EffectScopeRequested!=null)EffectScopeRequested(requested);};canvas.Controls.Add(effectScopePicker);
  frameReadout.ReadOnly=true;frameReadout.Multiline=true;frameReadout.WordWrap=true;frameReadout.ScrollBars=ScrollBars.Vertical;frameReadout.BorderStyle=BorderStyle.None;frameReadout.BackColor=surface;frameReadout.ForeColor=ink;frameReadout.AccessibleName="Selected ordered slot correspondence";frameReadout.Dock=DockStyle.Fill;
  entryPane.Dock=DockStyle.Bottom;entryPane.Height=120;entryPane.Visible=false;entryPane.Padding=new Padding(12,8,8,8);entryPane.BackColor=surface;entryPane.Controls.Add(frameReadout);entryClose.Text="×";entryClose.AccessibleName="Close entry correspondence";entryClose.FlatStyle=FlatStyle.Flat;entryClose.FlatAppearance.BorderSize=0;entryClose.Dock=DockStyle.Right;entryClose.Width=32;entryClose.Click+=delegate{entryPane.Visible=false;LayoutScene();FocusWork();};entryPane.Controls.Add(entryClose);Controls.Add(entryPane);
  entryToggle.Text="Entry slots";entryToggle.AccessibleName="Open exact macro entry correspondence";entryToggle.FlatStyle=FlatStyle.Flat;entryToggle.FlatAppearance.BorderSize=0;entryToggle.Click+=delegate{entryPane.Visible=!entryPane.Visible;RefreshFrameReadout();LayoutScene();if(entryPane.Visible)frameReadout.Focus();};canvas.Controls.Add(entryToggle);
  entryMenu.Click+=delegate{entryToggle.PerformClick();};objectMenu.Items.Add(entryMenu);objectMenu.BackColor=surface;objectMenu.ForeColor=ink;
  activateNext.FlatStyle=FlatStyle.Flat;activateNext.FlatAppearance.BorderSize=0;activateNext.AccessibleName="Activate the locked Next identity";activateNext.Click+=delegate{if(CommandRequested!=null)CommandRequested("next-activate");};tracking.Controls.Add(activateNext);
  browseMacro.FlatStyle=FlatStyle.Flat;browseMacro.FlatAppearance.BorderSize=1;browseMacro.AccessibleName="Choose an existing macro for this work";browseMacro.Click+=delegate{if(CommandRequested!=null)CommandRequested("macro-search");};canvas.Controls.Add(browseMacro);
  reviewStatus.FlatStyle=FlatStyle.Flat;reviewStatus.FlatAppearance.BorderSize=0;reviewStatus.TextAlign=ContentAlignment.MiddleCenter;reviewStatus.AccessibleName="Whole-operation protection check";reviewStatus.Click+=delegate{if(CommandRequested!=null)CommandRequested("review-details");};reviewStatus.Dock=DockStyle.Bottom;reviewStatus.BackColor=surface;Controls.Add(reviewStatus);
  objectActions.Text="⋮";objectActions.Visible=false;objectActions.FlatStyle=FlatStyle.Flat;objectActions.FlatAppearance.BorderSize=0;objectActions.BackColor=raised;objectActions.ForeColor=blue;
  objectActions.AccessibleName="Actions for inspected object";objectActions.AccessibleDescription="Same actions as Shift+F10 on a position or identity.";
  objectActions.Click+=delegate { var b=SelectedButton();if(b!=null)RequestMenu(b); };canvas.Controls.Add(objectActions);
  AddAction("Use as A","assign-a"); AddAction("Use as B","assign-b"); AddAction("Use as Target","assign-target");
  AddAction("Make Current","set-current"); AddAction("Pin Next","pin-next"); AddAction("Protect position","protect-position");
  addHomeMenu.AccessibleName="Add inspected identity's Home requirement to the block";addHomeMenu.Click+=delegate{if(selectedKind=="identity"&&SelectionAcknowledged(selectedKind,selectedId)&&CommandRequested!=null)CommandRequested("block-add");};objectMenu.Items.Add(addHomeMenu);
  Resize+=delegate { LayoutScene(); };
  FontChanged+=delegate { Rebuild(); };
  objectMenu.Opening+=delegate(object sender,System.ComponentModel.CancelEventArgs e) {
   var source=objectMenu.SourceControl as Button;
   if(source!=null && source.Tag is ObjectTag){e.Cancel=true;RequestMenu(source);return;}
   foreach(var item in actionItems)item.Enabled=SelectionAcknowledged(selectedKind,selectedId)&&ActionRequested!=null&&(!SelectionIsForecast||(string)item.Tag=="set-current"||(string)item.Tag=="pin-next");
  };
  EnabledChanged+=delegate {ScheduleMenu();};
  InitializeCycleUI();RefreshScene();
 }
 internal void UpdateContext(Dictionary<string,object> value,Dictionary<string,object> exactEffect) {
  snapshot=value; effect=exactEffect; Rebuild();
 }
 internal void UpdateContext(Dictionary<string,object> value,Dictionary<string,object> exactEffect,Dictionary<string,object> inspection,string reason,string effectScope=null,bool? previewRequested=null) {
  if(effectScope!=null&&effectScope!="body"&&effectScope!="complete")throw new ArgumentException("Effect scope must be body or complete.");
  snapshot=value;effect=exactEffect;
  var projection=CycleProjection;if(projection!=null&&(ValueText(Get(projection,"source_hash"))!=ValueText(Get(value,"hash"))||ValueText(Get(projection,"source_guard"))!=ValueText(Get(value,"guard"))))WithdrawCycleProjection();
  if(effectScope!=null)scope=effectScope;if(previewRequested.HasValue)requestedPrediction=previewRequested.Value;
  if(inspection!=null)SetPhaseData(inspection);else{phaseInspection=null;phaseReason=reason??"";withdrawingPhase=false;selectedForecast=false;}
  Rebuild();
 }
 internal void SetGeometry(NativeCellGeometry value) { geometry=value;Rebuild(); }
 internal bool FocusWork() {var button=SelectedButton()??objects.Find(b=>b.Parent==canvas)??objects.Find(b=>b.CanFocus);return button!=null&&button.Focus();}
 internal void CancelPendingInteraction() {pendingMenu=null;objectMenu.Close();PlaceObjectActions();}
 void AddAction(string label,string action) {
  var b=new ToolStripMenuItem { Text=label, Enabled=false, AccessibleName=label+" for inspected object", Tag=action };
  b.Click+=delegate { if(selectedId>=0 && ActionRequested!=null) ActionRequested(action,selectedKind,selectedId); };
  objectMenu.Items.Add(b); actionItems.Add(b);
 }
 static object Get(Dictionary<string,object> value,string key) { object result; return value!=null&&value.TryGetValue(key,out result)?result:null; }
 static Dictionary<string,object> Map(object value) { return value as Dictionary<string,object>; }
 static IList Items(object value) { return value as IList ?? new object[0]; }
 static int Id(object value) {
  if(value is int) return (int)value;
  if(value is long && (long)value>=Int32.MinValue && (long)value<=Int32.MaxValue) return (int)(long)value;
  return -1;
 }
 static string ValueText(object value) { return value==null?"":Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture); }
 static string Named(Dictionary<string,object> piece,string key,string fallback) {string value=ValueText(Get(Map(Get(piece,"names")),key));return value.Length==0?fallback:value;}
 static string NameLines(Dictionary<string,object> piece,string key,string fallback) {var rows=new List<string>();foreach(object row in Items(Get(Map(Get(piece,"names")),key)))rows.Add(ValueText(row));return rows.Count==0?fallback:String.Join("\n",rows.ToArray());}
 static string IdentityCaption(Dictionary<string,object> piece) {return NameLines(piece,"identity_lines","I"+Id(Get(piece,"piece")));}
 static string PositionCaption(Dictionary<string,object> piece) {return NameLines(piece,"current_lines","P"+Id(Get(piece,"position")));}
 string PositionCaption(int position) {var socket=sockets.Find(s=>s.Position==position);return socket==null||socket.Actual==null?"P"+position:Named(socket.Actual,"current_short","P"+position);}
 string CellCaption(int cell) {string value=CellLabel==null?null:CellLabel(cell);return String.IsNullOrEmpty(value)?"C"+cell:value;}
 string SlotCaption(int slot) {string value=SlotLabel==null?null:SlotLabel(slot);return String.IsNullOrEmpty(value)?"S"+slot:value;}
 static string NameDetails(Dictionary<string,object> piece) {return Named(piece,"identity_name","")+"\n"+Named(piece,"current_name","")+"\n"+Named(piece,"detail","");}
 static bool Flag(object value) { return value is bool && (bool)value; }
 static bool? PieceFilterMatch(Dictionary<string,object> record,Dictionary<string,object> provenance){object value=Get(record,"filter_match");return ValueText(Get(provenance,"status"))=="evaluated"&&value is bool?(bool?)value:null;}
 static string FilterDescription(Dictionary<string,object> record,Dictionary<string,object> provenance,string boundary){bool? match=PieceFilterMatch(record,provenance);string text=boundary+" Piece Filter: "+(match.HasValue?match.Value?"included":"excluded (dimmed; still selectable)":"unknown (not treated as excluded)")+".";string expression=ValueText(Get(provenance,"expression")),reason=ValueText(Get(provenance,"reason"));if(expression.Length>0)text+=" Expression: "+expression+".";if(reason.Length>0)text+=" "+reason;return text;}
 static void DrawFilteredImage(Graphics g,Image image,Point position,bool? match){if(match!=false){g.DrawImageUnscaled(image,position);return;}using(var attributes=new System.Drawing.Imaging.ImageAttributes()){var matrix=new System.Drawing.Imaging.ColorMatrix();matrix.Matrix33=.24f;attributes.SetColorMatrix(matrix);g.DrawImage(image,new Rectangle(position,image.Size),0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);}}
 static int Orbit(object value) { return (value is int || value is long) && Id(value)>=-1 && Id(value)<35?Id(value):-2; }
 static HashSet<int> Ids(object value) { var r=new HashSet<int>(); foreach(object p in Items(value)) if(Id(p)>=0)r.Add(Id(p)); return r; }
 static void AddPiece(Dictionary<int,Dictionary<string,object>> pieces, object value) {
  var p=Map(value); int position=Id(Get(p,"position")); if(position>=0) pieces[position]=p;
 }
 Dictionary<int,Dictionary<string,object>> PieceMap(Dictionary<string,object> context) {
  var result=new Dictionary<int,Dictionary<string,object>>();
  foreach(object p in Items(Get(context,"buffers"))) AddPiece(result,p);
  foreach(object p in Items(Get(context,"block")))if(Map(p)!=null&&Get(Map(p),"piece")!=null)AddPiece(result,p);
  foreach(object p in Items(Get(context,"positions")))AddPiece(result,p);
  foreach(string name in new[]{"current","next","target","inspected"}) AddPiece(result,Get(context,name));
  return result;
 }
 Dictionary<string,object> DisplayEffect {get{return cycleMode!="Operation"||OperationExecuted||withdrawingPhase?null:scope=="complete"?effect:MacroEntry!=null?Map(Get(MacroEntry,"facts")):effect;}}
 void Rebuild() {var timing=BeginHubTiming();try{RebuildCore();}finally{WriteHubTiming("Rebuild",timing);}}
 void ResumeSceneLayout(){long started=hubTimingEnabled?Stopwatch.GetTimestamp():0;try{canvas.ResumeLayout();ResumeLayout();}finally{if(hubTimingEnabled){hubResumeCalls++;hubResumeTicks+=Stopwatch.GetTimestamp()-started;}}}
 void RebuildCore() {
  if(scroll==null)return;
  ClearCycleHits();
  rebuilding=true;
  bool restoreFocus=scroll.ContainsFocus; var focused=objects.Find(delegate(Button b){return b.Focused;});
  var focusTag=focused==null?null:focused.Tag as ObjectTag;
  SuspendLayout(); canvas.SuspendLayout();
  var authoritative=Map(Get(snapshot,"workspace"));int inspectedPosition=Id(Get(authoritative,"inspected_position"));
  int previousSelected=selectedId;selectedKind=inspectedPosition>=0?"position":"identity";selectedId=inspectedPosition>=0?inspectedPosition:Id(Get(authoritative,"inspected"));if(previousSelected!=selectedId||selectedKind!="identity"||!PhaseForecast)selectedForecast=false;
  if(selectedId<0)selectedKind=null;
  if(objectMenu.Visible)objectMenu.Close();
  if(objectActions.Parent!=canvas&&objectActions.Parent!=tracking)canvas.Controls.Add(objectActions);
  var previousObjects=objects.ToArray();
  try{
  // Future retained buttons must not inflate auto TabIndex assignment of new siblings.
  foreach(Button button in previousObjects)button.TabIndex=0;
  objects.Clear(); sockets.Clear();
  foreach(Button b in forecastObjects){if(b.Image!=null)b.Image.Dispose();b.Dispose();}forecastObjects.Clear();foreach(Button b in blockObjects)b.Dispose();blockObjects.Clear();
  var w=Map(Get(snapshot,"workspace"));
  var committed=PieceMap(snapshot);var actualFilterSources=new Dictionary<int,Dictionary<string,object>>();foreach(object p in Items(Get(phaseInspection,"actual_positions"))){AddPiece(committed,p);actualFilterSources[Id(Get(Map(p),"position"))]=Map(Get(phaseInspection,"actual_piece_filter"));}
  MergeCycleActualRecords(committed);
  var displayed=committed;var after=PhaseForecast?PieceMap(phaseInspection):Predicted?PieceMap(Map(Get(snapshot,"predicted"))):new Dictionary<int,Dictionary<string,object>>();
  MergeCycleForecastRecords(after);
  var members=new Dictionary<int,Dictionary<string,object>>();
  foreach(object raw in Items(Get(snapshot,"block"))) {
   var m=Map(raw); int p=Id(Get(m,"position")); if(p<0)continue;
   members[p]=m;
   if(!displayed.ContainsKey(p)) displayed[p]=new Dictionary<string,object>{{"piece",Get(m,"actual")},{"position",p},{"orbit",Get(m,"orbit")},{"current_cells",Get(m,"current_cells")}};
  }
  var protectedPositions=new HashSet<int>();
  object locks=Get(snapshot,"position_locks")??Get(Map(Get(w,"block")),"protected");
  foreach(object raw in Items(locks)) {int p=Map(raw)!=null?Id(Get(Map(raw),"position")):Id(raw);if(p>=0)protectedPositions.Add(p);}
  var protectedOrbits=Ids(Get(snapshot,"protected"));
  var review=Map(Get(snapshot,"review"));
  var conflicts=ReviewCurrent?Ids(Get(review,"block_conflicts")):new HashSet<int>();
  var shownEffect=DisplayEffect;var frame=cycleDisplayActive?Ids(Get(Map(Get(CycleProjection,"orientation_index")),"positions")):Ids(Get(shownEffect,"fixed_orientation"));
  if(!PopulateDetailedCycles()){
  var cycles=Items(Get(shownEffect,"cycles"));selectedCycle=Math.Max(0,Math.Min(selectedCycle,cycles.Count-1));
  syncingCycle=true;cyclePicker.Items.Clear();for(int i=0;i<cycles.Count;i++)cyclePicker.Items.Add(CycleCaption(Map(cycles[i]),i,shownEffect));if(cycles.Count>0)cyclePicker.SelectedIndex=selectedCycle;syncingCycle=false;
  int dropWidth=cyclePicker.Width;foreach(object item in cyclePicker.Items)dropWidth=Math.Max(dropWidth,TextRenderer.MeasureText(ValueText(item),Font).Width+U(28));cyclePicker.DropDownWidth=Math.Min(Math.Max(200,ClientSize.Width-U(16)),dropWidth);
  cyclePositions.Clear();if(cycles.Count>0)foreach(object p in Items(Get(Map(cycles[selectedCycle]),"positions")))if(Id(p)>=0&&!cyclePositions.Contains(Id(p)))cyclePositions.Add(Id(p));
  int inspectedIndex=cyclePositions.IndexOf(inspectedCycleSource);if(inspectedIndex<0||cyclePositions[(inspectedIndex+1)%cyclePositions.Count]!=inspectedCycleDestination)inspectedCycleSource=inspectedCycleDestination=-1;
  cyclePicker.AccessibleDescription=ValueText(cyclePicker.SelectedItem)+". "+(scope=="complete"?"Complete Prepare / Macro / Cleanup effect. ":"Selected macro body only; Prepare and Cleanup are excluded. ")+"Dashed arrows describe movement, not protection boundaries. Exact directed positions: "+String.Join(" → P",cyclePositions.ConvertAll(p=>p.ToString()).ToArray())+". Enter focuses and scrolls to the next source position. Arrow keys then navigate exact positions and pieces; Enter inspects the focused object. No role or operation changes occur on focus.";
  tip.SetToolTip(cyclePicker,cyclePicker.AccessibleDescription);
  if(cyclePositions.Count>6)cyclePicker.AccessibleDescription+=" Positions follow the exact directed cycle order. Scroll to inspect further positions.";
  }
  var roles=Items(Get(w,"roles")); var buffers=Items(Get(snapshot,"buffers"));
  var positions=new List<int>(); var names=new List<string>();
  for(int i=0;i<2;i++) {
   int p=roles.Count>i?Id(roles[i]):buffers.Count>i?Id(Get(Map(buffers[i]),"position")):-1;
   positions.Add(p); names.Add(i==0?"BUFFER A":"BUFFER B");
  }
  int target=Id(Get(w,"target"));
  foreach(object raw in Items(Get(snapshot,"block"))) { int p=Id(Get(Map(raw),"position")); if(p>=0&&!positions.Contains(p)){positions.Add(p);names.Add(p==target?"TARGET":"BLOCK MEMBER");} }
  if(target>=0&&!positions.Contains(target)){positions.Add(target);names.Add("TARGET");}
  var star=CycleProjection!=null&&CycleScopeIndex!=0?null:Map(Get(shownEffect,"star"));
  foreach(string role in new[]{"a","b","target"}) { int p=Id(Get(star,role));if(p>=0&&!positions.Contains(p)){positions.Add(p);names.Add("MACRO "+role.ToUpperInvariant()+" (UNBOUND)");} }
  foreach(int p in cyclePositions)if(!positions.Contains(p)){positions.Add(p);names.Add("MACRO POSITION");}
  if(cyclePositions.Count==0)foreach(int p in positions)if(p>=0&&cyclePositions.Count<3)cyclePositions.Add(p);
  int current=Id(Get(Map(Get(snapshot,"current")),"piece")), next=Id(Get(Map(Get(snapshot,"next")),"piece"));
  workSocketCount=positions.Count;
  // Current and Next have permanent canonical traces, including locations outside the cycle.
  int inspectedIdentity=Id(Get(w,"inspected")),inspectionPosition=Id(Get(w,"inspected_position"));
  if(inspectionPosition<0&&inspectedIdentity>=0)foreach(var pair in displayed)if(Id(Get(pair.Value,"piece"))==inspectedIdentity){inspectionPosition=pair.Key;break;}
  if(inspectionPosition>=0&&!positions.Contains(inspectionPosition)){positions.Add(inspectionPosition);names.Add(inspectedPosition>=0?"INSPECTED POSITION":"INSPECTED IDENTITY");}
  else if(inspectionPosition<0&&inspectedIdentity>=0){positions.Add(-1);names.Add("INSPECTED IDENTITY");}
  int railIndex=0,blockIndex=0,collateralIndex=0;
  var phaseMembers=new Dictionary<int,Dictionary<string,object>>();foreach(object raw in Items(Get(phaseInspection,"block"))){var m=Map(raw);int p=Id(Get(m,"position"));if(p>=0)phaseMembers[p]=m;}
  for(int i=0;i<positions.Count;i++) {
   int p=positions[i]; var s=new Socket {Position=p,Index=i,Role=names[i],TrackedIndex=i>=workSocketCount?i-workSocketCount:-1};
   s.CycleIndex=cyclePositions.IndexOf(p);if(s.CycleIndex>=6)s.CycleIndex=-1;
   if(s.TrackedIndex<0&&s.CycleIndex<0)s.RailIndex=railIndex++;
   Dictionary<string,object> actual,member,original,prediction;
   if(displayed.TryGetValue(p,out actual)) {
    s.Actual=actual;
    Dictionary<string,object> filterSource;s.ActualFilter=actualFilterSources.TryGetValue(p,out filterSource)?filterSource:Map(Get(snapshot,"piece_filter"));
    s.Identity=Id(Get(actual,"piece"));var cells=new List<int>();
    foreach(object cell in Items(Get(actual,"current_cells"))) {int c=Id(cell);if(c>=1&&c<=600&&!cells.Contains(c))cells.Add(c);}
    s.Hosting=cells.ToArray();
    s.Home=CanonicalCells(Get(actual,"home_cells"));
   }
   if(after.TryGetValue(p,out prediction)){s.Forecast=prediction;s.ForecastFilter=Map(Get(ResidualAfter?cycleProjection:PhaseForecast?phaseInspection:Map(Get(snapshot,"predicted")),"piece_filter"));s.AfterKnown=true;s.AfterIdentity=Id(Get(prediction,"piece"));s.AfterHome=CanonicalCells(Get(prediction,"home_cells"));}
   // A piece's Home structure is the hosting structure of its same-numbered fixed position.
   if(s.Home.Length==0&&committed.TryGetValue(s.Identity,out original))s.Home=CanonicalCells(Get(original,"current_cells"));
   if(s.AfterHome.Length==0&&s.AfterIdentity>=0) {
    foreach(var entry in committed)if(Id(Get(entry.Value,"piece"))==s.AfterIdentity){s.AfterHome=CanonicalCells(Get(entry.Value,"home_cells"));break;}
    if(s.AfterHome.Length==0&&committed.TryGetValue(s.AfterIdentity,out original))s.AfterHome=CanonicalCells(Get(original,"current_cells"));
   }
   if(p<0&&s.Role=="INSPECTED IDENTITY")s.Identity=inspectedIdentity;
   var flags=new List<string>();bool unbound=false;foreach(string role in new[]{"a","b","target"})if(p>=0&&p==Id(Get(star,role))){flags.Add(role=="target"?"Target":role.ToUpperInvariant());int workPosition=role=="target"?target:roles.Count>(role=="a"?0:1)?Id(roles[role=="a"?0:1]):-1;unbound|=p!=workPosition;}
   s.MacroRole=String.Join("/",flags);
   if(unbound)s.MacroBinding="Macro "+s.MacroRole+": unbound";
   if(committed.TryGetValue(p,out original))s.Orbit=Orbit(Get(original,"orbit"));
   if(members.TryGetValue(p,out member)){
    s.Member=member;s.Orbit=Orbit(Get(member,"orbit"));
    s.RequirementMet=Get(member,"satisfied") is bool?(bool?)Flag(Get(member,"satisfied")):null;
   }
   if(s.Member!=null||s.Role=="TARGET")s.BlockIndex=blockIndex++;else if(s.Index>=2&&s.TrackedIndex<0)s.CollateralIndex=collateralIndex++;
   Dictionary<string,object> phaseMember;if(phaseMembers.TryGetValue(p,out phaseMember)){s.BoundaryMet=Get(phaseMember,"requirement_met") is bool?(bool?)Flag(Get(phaseMember,"requirement_met")):null;}
   s.Current=s.Identity>=0&&s.Identity==current; s.Next=s.Identity>=0&&s.Identity==next;
   s.Protected=protectedPositions.Contains(p)||(s.Orbit>=0&&protectedOrbits.Contains(s.Orbit));
   s.Conflict=conflicts.Contains(p);s.FrameChanged=frame.Contains(p); sockets.Add(s);
   if(p>=0) AddObject("position",p,s.Role+", fixed position P"+p,s.Actual==null?"P"+p:PositionCaption(s.Actual),s,previousObjects);
   if(s.Identity>=0)AddObject("identity",s.Identity,"Inspect identity I"+s.Identity+", committed"+(p>=0?" at P"+p:" position unavailable"),s.Actual==null?"I"+s.Identity:IdentityCaption(s.Actual),s,previousObjects);
   if(PhaseForecast&&s.AfterKnown&&s.AfterIdentity>=0&&s.TrackedIndex<0) {
    var forecast=new ObjectButton{Text=IdentityCaption(s.Forecast),Tag=new ObjectTag("identity",s.AfterIdentity,true),IdentityToken=true,Forecast=true,FilterMatch=PieceFilterMatch(s.Forecast,s.ForecastFilter),RoleMark=s.AfterIdentity==current?"After Current":s.AfterIdentity==next?"After Next":"After",FlatStyle=FlatStyle.Flat,BackColor=BackColor,ForeColor=amber,AccessibleName="Inspect identity I"+s.AfterIdentity+", forecast after "+Boundary+" at P"+p+(s.AfterIdentity==current?"; Current":s.AfterIdentity==next?"; locked Next":""),AccessibleDescription="Canonical physical identity. Its projected position is an observation; explicit role actions still use the authoritative command context. "+FilterDescription(s.Forecast,s.ForecastFilter,"After "+Boundary),UseVisualStyleBackColor=false};forecast.FlatAppearance.BorderColor=amber;forecast.FlatAppearance.BorderSize=1;forecast.Click+=delegate{InspectObject(forecast);};forecast.KeyDown+=ObjectKey;tip.SetToolTip(forecast,forecast.AccessibleName+"\n"+forecast.AccessibleDescription);canvas.Controls.Add(forecast);forecastObjects.Add(forecast);
   }
   if(s.Member!=null){var requirement=new ResultButton{FlatStyle=FlatStyle.Flat,Tag=new ObjectTag("position",p),AccessibleName="Inspect block requirement at fixed position P"+p,Text="",BackColor=BackColor,TextAlign=ContentAlignment.MiddleLeft};requirement.FlatAppearance.BorderSize=0;requirement.Click+=delegate{InspectObject(requirement);};requirement.KeyDown+=ObjectKey;canvas.Controls.Add(requirement);blockObjects.Add(requirement);}
  }
  DisposePreviousObjects(previousObjects);
  ResumeSceneLayout(); LayoutScene(); RefreshScene();RefreshGripHighlights();
  if(restoreFocus && focusTag!=null)foreach(Button b in objects){var t=(ObjectTag)b.Tag;if(t.Kind==focusTag.Kind&&t.Id==focusTag.Id){b.Focus();break;}}
  rebuilding=false;
  ScheduleMenu();
  }finally{DisposePreviousObjects(previousObjects);}
 }
 static int[] CanonicalCells(object raw){var cells=new List<int>();foreach(object value in Items(raw)){int c=Id(value);if(c>=1&&c<=600&&!cells.Contains(c))cells.Add(c);}cells.Sort();return cells.ToArray();}
 internal void SetGripCap(int? canonicalCell){if(canonicalCell.HasValue&&(canonicalCell.Value<1||canonicalCell.Value>600))throw new ArgumentOutOfRangeException("canonicalCell");if(gripCap==canonicalCell)return;gripCap=canonicalCell;RefreshGripHighlights();}
 internal int GripHighlightedObjectCount {get{return objects.OfType<ObjectButton>().Count(button=>button.GripHighlighted);}}
 void RefreshGripHighlights(){foreach(ObjectButton button in objects){var tag=(ObjectTag)button.Tag;var socket=tag.Kind=="identity"?sockets.Find(item=>item.Identity==tag.Id):null;bool highlighted=gripCap.HasValue&&socket!=null&&socket.Actual!=null&&PieceFilterMatch(socket.Actual,socket.ActualFilter)==true&&Items(Get(socket.Actual,"cap_cells")).Cast<object>().Any(value=>Id(value)==gripCap.Value);if(button.GripHighlighted!=highlighted){button.GripHighlighted=highlighted;button.Invalidate();}}}
 static void DisposePreviousObjects(Button[] previous){for(int i=0;i<previous.Length;i++){var button=previous[i];previous[i]=null;if(button!=null){if(button.Image!=null)button.Image.Dispose();button.Dispose();}}}
 string ObjectPresentationKey(string kind,int id,string accessible,string label,Socket s){
  // Exact presentation dependencies; semantic sockets and projections are rebuilt every time.
  return objectKeyJson.Serialize(new object[]{kind,id,accessible,label,objects.Count,
   s.Position,s.Identity,s.Orbit,s.Index,s.Role,s.Hosting,s.Home,s.AfterIdentity,s.AfterHome,s.AfterKnown,s.MacroRole,s.MacroBinding,s.TrackedIndex,s.CycleIndex,s.RailIndex,s.BlockIndex,s.CollateralIndex,s.RequirementMet,s.Current,s.Next,s.Protected,s.Conflict,s.FrameChanged,s.BoundaryMet,s.Actual,s.Forecast,s.ActualFilter,s.ForecastFilter,s.Member,
   PhaseForecast,Boundary,ValueText(Get(Map(Get(CycleProjection,"selected")),"kind")),
   Font.Name,Font.Size,(int)Font.Style,(int)Font.Unit,Font.GdiCharSet,Font.GdiVerticalFont,Font.Height,BackColor.ToArgb(),ink.ToArgb(),muted.ToArgb(),line.ToArgb(),blue.ToArgb(),green.ToArgb(),amber.ToArgb(),red.ToArgb(),surface.ToArgb(),raised.ToArgb(),
   kind=="identity"?s.Hosting.Select(CellCaption).ToArray():null,
   kind=="identity"&&geometry!=null?s.Home.Select(cell=>geometry.CellColor(cell-1).ToArgb()).ToArray():null});
 }
 void AddObject(string kind,int id,string accessible,string label,Socket socket,Button[] previous) {
  string key=ObjectPresentationKey(kind,id,accessible,label,socket);
  int index=objects.Count;
  if(index<previous.Length){
   var retained=previous[index] as ObjectButton;var tag=retained==null?null:retained.Tag as ObjectTag;
   if(retained!=null&&!retained.IsDisposed&&retained.Parent==(socket.TrackedIndex<0?canvas:tracking)&&tag!=null&&tag.Kind==kind&&tag.Id==id&&!tag.Forecast&&retained.PresentationKey==key){retained.TabIndex=index;retained.Parent.Controls.SetChildIndex(retained,retained.Parent.Controls.Count-1);previous[index]=null;objects.Add(retained);return;}
   var unused=previous[index];previous[index]=null;if(unused!=null){if(unused.Image!=null)unused.Image.Dispose();unused.Dispose();}
  }
  var button=new ObjectButton {Text=label,Tag=new ObjectTag(kind,id),FlatStyle=FlatStyle.Flat,AccessibleName=accessible,AccessibleDescription="Enter or Space inspects. Arrow keys move focus. Shift+F10 opens explicit object actions.",TabIndex=objects.Count,UseVisualStyleBackColor=false,ContextMenuStrip=objectMenu};
  button.AccessibleDescription+="\n"+NameDetails(socket.Actual);
  button.IdentityToken=kind=="identity";button.RoleMark=socket.TrackedIndex>=0?"":socket.Role=="BUFFER A"?"A":socket.Role=="BUFFER B"?"B":socket.Role=="TARGET"?"Target":socket.Member!=null?"Block":ValueText(Get(Map(Get(CycleProjection,"selected")),"kind"))=="unchanged"?"Position":"Macro";button.RoleColor=socket.Index<2?ink:green;
  if(kind=="position"&&socket.MacroBinding.Length>0){button.BindingMark=socket.MacroBinding;if(socket.Role.StartsWith("MACRO"))button.RoleMark="";button.AccessibleDescription+=" "+socket.MacroBinding+". This fixed macro role differs from the corresponding working role; no role transport or setup is inferred.";}
  if(kind=="position"&&socket.TrackedIndex<0){button.PolicyMark=socket.Conflict?"conflict":socket.Protected?"locked":socket.Orbit< -1?"unknown":"";button.PolicyColor=socket.Conflict?red:socket.Protected?green:amber;}
  button.BackColor=BackColor;button.FlatAppearance.MouseOverBackColor=Color.FromArgb(235,240,239);
  button.FlatAppearance.BorderColor=kind=="position"?(socket.Conflict?red:line):socket.Current?blue:socket.Next?amber:line;
  button.FlatAppearance.BorderSize=0;
  if(kind=="identity") {
   button.FilterMatch=PieceFilterMatch(socket.Actual,socket.ActualFilter);button.AccessibleDescription+=" "+FilterDescription(socket.Actual,socket.ActualFilter,"Actual");
   button.AccessibleDescription+=" A pale inner outline marks a filter-included actual position affected by the active Grip cap; it does not certify a chosen Twist or protection.";
   button.RoleMark=socket.Current&&socket.Next?"Current · Next":socket.Current?"Current":socket.Next?"Next":"";
   button.Image=IdentityImage(socket.Home,U(socket.TrackedIndex<0?148:82),U(socket.TrackedIndex<0?20:3));button.ImageAlign=ContentAlignment.TopCenter;button.TextAlign=ContentAlignment.BottomCenter;button.Padding=new Padding(U(5));
   if(socket.TrackedIndex>=0)button.Padding=Padding.Empty;
   var cells=new List<string>();foreach(int cell in socket.Hosting)cells.Add(CellCaption(cell));
   button.AccessibleDescription+=" Hosting cells: "+(cells.Count>0?String.Join(", ",cells):"unavailable")+". Color segments show occupying sticker home-cell colors in this fixed position's exact supplied slot order. Entry shows the paired slot and Sticker IDs. These are not the structural cell colors.";
   if(socket.Current)button.AccessibleName+="; Current";if(socket.Next)button.AccessibleName+="; locked Next bookmark";
  }else {
   button.AccessibleDescription+=" Fixed position socket. Its contained token is the actual occupant; use Local for the real hosting-cell geometry. Placement in this diagram conveys operation order, not physical distance.";
   if(socket.MacroRole.Length>0)button.AccessibleName+="; selected macro role "+socket.MacroRole;
   if(socket.Protected)button.AccessibleName+="; protected position; policy set, see the complete operation check";
   if(socket.Conflict)button.AccessibleName+="; complete operation CONFLICT";
   button.AccessibleDescription+=" "+FilterDescription(socket.Actual,socket.ActualFilter,"Actual occupant");if(socket.AfterKnown)button.AccessibleDescription+=" "+FilterDescription(socket.Forecast,socket.ForecastFilter,PhaseForecast?"After "+Boundary+" occupant":"Complete preview occupant");
  }
  button.Click+=delegate { InspectObject(button); };
  button.KeyDown+=ObjectKey; button.GotFocus+=delegate { KeepVisible(button); };(socket.TrackedIndex<0?canvas:tracking).Controls.Add(button); objects.Add(button);
  tip.SetToolTip(button,(kind=="identity"?"Piece · ":"Position · ")+label.Replace("\n"," / ")+"\nEnter: inspect · Shift+F10: actions"+(socket.Conflict?" · Protection conflict":""));
  button.PresentationKey=key;
 }
 void InspectObject(Button button,bool keepMenu=false) {
  if(!keepMenu)CancelPendingInteraction();InspectTag((ObjectTag)button.Tag);
 }
 void InspectTag(ObjectTag tag) {
  inspectedCycleSource=inspectedCycleDestination=-1;
  selectedKind=tag.Kind;selectedId=tag.Id;selectedForecast=tag.Forecast;pendingForecastIdentity=tag.Forecast?tag.Id:-1;pendingForecastBoundary=tag.Forecast?Boundary:"";RefreshScene();
  if(ObjectSelected!=null)ObjectSelected(tag.Kind,tag.Id);
 }
 bool SelectionAcknowledged(string kind,int id) {
  var w=Map(Get(snapshot,"workspace"));return id>=0&&(kind=="position"?Id(Get(w,"inspected_position"))==id:Id(Get(w,"inspected_position"))<0&&Id(Get(w,"inspected"))==id);
 }
 Button SelectedButton() {var list=SelectionIsForecast?forecastObjects:objects;var found=list.Find(delegate(Button b){var t=(ObjectTag)b.Tag;return t.Kind==selectedKind&&t.Id==selectedId;});return found??objects.Find(b=>((ObjectTag)b.Tag).Kind==selectedKind&&((ObjectTag)b.Tag).Id==selectedId);}
 void RequestMenu(Button button) {
  var tag=(ObjectTag)button.Tag;pendingMenu=new ObjectTag(tag.Kind,tag.Id);
  selectedForecast=tag.Forecast;pendingForecastIdentity=tag.Forecast?tag.Id:-1;pendingForecastBoundary=tag.Forecast?Boundary:"";
  if(!SelectionAcknowledged(tag.Kind,tag.Id))InspectObject(button,true);
  else {selectedKind=tag.Kind;selectedId=tag.Id;RefreshScene();}
  ScheduleMenu();
 }
 void ScheduleMenu() {
  if(pendingMenu==null||menuPosted||!IsHandleCreated||!Enabled||!SelectionAcknowledged(pendingMenu.Kind,pendingMenu.Id))return;
  menuPosted=true;BeginInvoke(new Action(delegate {
   menuPosted=false;if(IsDisposed||pendingMenu==null||!Enabled||!SelectionAcknowledged(pendingMenu.Kind,pendingMenu.Id))return;
   selectedKind=pendingMenu.Kind;selectedId=pendingMenu.Id;pendingMenu=null;PlaceObjectActions();
   if(objectActions.Visible)objectMenu.Show(objectActions,new Point(0,objectActions.Height));
  }));
 }
 float UiScale { get { return Math.Max(1f,Font.Height/19f); } }
 int U(double value) { return (int)Math.Ceiling(value*UiScale); }
 float LogicalWidth {get{return Math.Max(630,(ClientSize.Width-SystemInformation.VerticalScrollBarWidth)/UiScale);}}
 float OperationWidth {get{return LogicalWidth;}}
 float SceneHeight {get{return Math.Max(321,scroll.ClientSize.Height/UiScale);}}
 int GeometryWidth(Socket s){int width=s.Index<2?146:158;foreach(string text in new[]{IdentityCaption(s.Actual),PositionCaption(s.Actual),IdentityCaption(s.Forecast),s.MacroBinding})foreach(string row in text.Split('\n'))width=Math.Max(width,(int)Math.Ceiling((TextRenderer.MeasureText(row,Font,Size.Empty,TextFormatFlags.NoPadding).Width+U(16))/UiScale));return width;}
 int BindingHeight(Socket s){return s.MacroBinding.Length>0?Font.Height+2:0;}
 int GeometryHeight(Socket s){return 64+BindingHeight(s);}
 float SocketX(Socket s) {
  if(s.TrackedIndex>=0)return LogicalWidth*2/3+100;
  PointF point;return socketCenters.TryGetValue(s.Index,out point)?point.X:LogicalWidth*.5f;
 }
 float SocketY(Socket s) {
  if(s.TrackedIndex>=0)return 0;
  PointF point;return socketCenters.TryGetValue(s.Index,out point)?point.Y:90;
 }
 PointF Center(Socket s) { return new PointF(U(SocketX(s)),U(SocketY(s))); }
 void BuildCycleLayout(){
  socketCenters.Clear();var visible=sockets.Where(s=>s.TrackedIndex<0).ToList();
  int width=visible.Count==0?180:visible.Max(s=>GeometryWidth(s)),columns=Math.Max(1,Math.Min(4,(int)((LogicalWidth-24)/(width+64f))));
  float stride=(LogicalWidth-24)/columns,rowHeight=visible.Count==0?180:visible.Max(s=>GeometryHeight(s)+106+(PhaseForecast||Predicted?70:0)+(s.Member==null?0:28));
  var cycle=CycleProjection==null&&DisplayEffect==null?new List<Socket>():cyclePositions.Select(p=>visible.Find(s=>s.Position==p)).Where(s=>s!=null).Distinct().ToList();
  var other=visible.Where(s=>!cycle.Contains(s)).ToList();int index=0;
  foreach(Socket s in cycle){socketCenters[s.Index]=new PointF(12+stride*(index%columns+.5f),138+(index/columns)*rowHeight);index++;}
  int rows=(index+columns-1)/columns;workGroupY=cycle.Count>0&&other.Count>0?112+rows*rowHeight:0;
  float start=cycle.Count>0?138+rows*rowHeight+(other.Count>0?24:0):138;
  index=0;foreach(Socket s in other){socketCenters[s.Index]=new PointF(12+stride*(index%columns+.5f),start+(index/columns)*rowHeight);index++;}
  sceneBottom=Math.Max(220,other.Count>0?start+((index+columns-1)/columns)*rowHeight-24:138+rows*rowHeight-24);
 }
 void LayoutHeader() {
  int row=Font.Height+4,width=Math.Max(1,ClientSize.Width),phaseWidth=phaseBar.Padding.Horizontal;
  foreach(Button button in phaseButtons){button.Size=new Size(TextRenderer.MeasureText(button.Text,Font).Width+12,row);phaseWidth+=button.Width+button.Margin.Horizontal;}
  int compareWidth=!OperationExecuted&&Map(Get(snapshot,"predicted"))!=null?TextRenderer.MeasureText(comparison.Text,Font).Width+16:0;
  int minimumCaption=TextRenderer.MeasureText("FORECAST · After Prepare",Font).Width+10;
  bool stacked=width<minimumCaption+phaseWidth+compareWidth+8;
  caption.Height=row*(stacked?2:1);
  phaseBar.Bounds=new Rectangle(stacked?0:width-compareWidth-phaseWidth,stacked?row:0,phaseWidth,row);
  heading.Bounds=new Rectangle(0,0,Math.Max(1,stacked?width-compareWidth:width-compareWidth-phaseWidth),row);
  comparison.Bounds=new Rectangle(width-compareWidth,0,compareWidth,row);
  activateNext.Text="Activate Next"+(width>=900?Hint("next-activate"):"");
  tip.SetToolTip(activateNext,"Explicitly activate the locked Next identity."+Hint("next-activate"));
  int activateWidth=Get(snapshot,"next")!=null?TextRenderer.MeasureText(activateNext.Text,Font).Width+12:0;
  activateNext.Bounds=new Rectangle(width-activateWidth,0,activateWidth,row);
  int available=Math.Max(1,width-12),currentWidth=TextRenderer.MeasureText(currentTrace.Text,Font).Width+6,nextWidth=TextRenderer.MeasureText(nextTrace.Text,Font).Width+6;
  bool traceStack=currentWidth+nextWidth+activateWidth+8>available;
  tracking.Height=row*(traceStack?4:2);
  if(traceStack){currentTrace.Bounds=new Rectangle(5,0,available-activateWidth,row*2);nextTrace.Bounds=new Rectangle(5,row*2,available,row*2);}
  else {currentWidth=Math.Max(currentWidth,(available-activateWidth-8)/2);currentTrace.Bounds=new Rectangle(5,0,currentWidth,row*2);nextTrace.Bounds=new Rectangle(5+currentWidth+8,0,Math.Max(1,available-currentWidth-activateWidth-8),row*2);}
  if(sockets.Exists(s=>s.TrackedIndex>=0))tracking.Height+=row*2;
 }
 void LayoutScene() {long started=hubTimingEnabled?Stopwatch.GetTimestamp():0;try{LayoutSceneCore();}finally{if(hubTimingEnabled){hubLayoutCalls++;hubLayoutTicks+=Stopwatch.GetTimestamp()-started;}}}
 void LayoutSceneCore() {
  if(scroll==null||canvas==null)return;
  LayoutHeader();
  // Outside-work bookmarks stay above scrolling work, without moving fixed work sockets.
  BuildCycleLayout();canvas.Size=new Size(U(LogicalWidth),Math.Max(scroll.ClientSize.Height,U(sceneBottom)));
  browseMacro.Visible=false;
  cyclePicker.Visible=!withdrawingPhase&&cyclePicker.Items.Count>0;
  LayoutCycleSelectors();
  entryToggle.Bounds=new Rectangle(U(OperationWidth-112),U(1),U(108),U(28));entryToggle.Enabled=MacroEntry!=null||DetailedEdges.Count>0;entryMenu.Enabled=entryToggle.Enabled;
  LayoutCycleUI();
  int b=0;
  foreach(Socket s in sockets) {
   PointF c=Center(s);
   if(s.TrackedIndex>=0) {
    int x=U(5+s.TrackedIndex*210),h=(Font.Height+4)*2,y=tracking.Height-h;
    if(s.Position>=0){var position=objects[b++];position.Text="Position";position.Bounds=new Rectangle(x+U(100),y,U(95),h);}
    if(s.Identity>=0){var identity=objects[b++];identity.Text="Piece";identity.Bounds=new Rectangle(x,y,U(95),h);}
    continue;
   }
   int width=GeometryWidth(s),height=GeometryHeight(s);
   if(s.Position>=0){var position=objects[b++];position.Bounds=new Rectangle((int)c.X-U(width*.5),(int)c.Y-U(31),U(width),U(height));if(position.Image!=null){position.Image.Dispose();position.Image=null;}}
   if(s.Identity>=0){var identity=objects[b++];identity.Bounds=new Rectangle((int)c.X-U(width*.5),(int)c.Y+U(height-29),U(width),U(66));if(identity.Image!=null)identity.Image.Dispose();identity.Image=StickerImage(s.Actual,U(width-12),U(4));}
  }
  foreach(Button forecast in forecastObjects){var t=(ObjectTag)forecast.Tag;Socket s=sockets.Find(x=>x.AfterIdentity==t.Id&&x.TrackedIndex<0);if(s!=null){forecast.Bounds=new Rectangle(U(SocketX(s)-GeometryWidth(s)*.5),U(SocketY(s)+GeometryHeight(s)+39),U(GeometryWidth(s)),U(66));if(forecast.Image!=null)forecast.Image.Dispose();forecast.Image=StickerImage(s.Forecast,U(GeometryWidth(s)-12),U(4));}}
  foreach(Button requirement in blockObjects){int p=((ObjectTag)requirement.Tag).Id;var s=sockets.Find(x=>x.Position==p);bool? met=PhaseForecast?s.BoundaryMet:s.RequirementMet;requirement.Text=(met==true?"✓ ":"Needs ")+(Id(Get(s.Member,"identity"))==p?"Home":"captured piece");requirement.ForeColor=met==true?green:ink;requirement.Bounds=new Rectangle(U(SocketX(s)-GeometryWidth(s)*.5),U(SocketY(s)+GeometryHeight(s)+((PhaseForecast||Predicted)?108:39)),U(GeometryWidth(s)),U(25));requirement.AccessibleDescription="Declared exact block requirement for I"+Id(Get(s.Member,"identity"))+" at P"+p+". Actual: "+(s.RequirementMet.HasValue?s.RequirementMet.Value?"met":"not met":"unknown")+"; "+(PhaseForecast?"after "+Boundary+": "+(met.HasValue?met.Value?"met":"not met":"unknown"):"actual state")+". Protection is a separate policy.";tip.SetToolTip(requirement,requirement.AccessibleDescription);}
  RefreshFrameReadout();
  RefreshTraces();
  PlaceObjectActions();
   RefreshReviewEvidence();RefreshProtection();
  canvas.Invalidate();tracking.Invalidate();
 }
 void RefreshScene() {long started=hubTimingEnabled?Stopwatch.GetTimestamp():0;try{RefreshSceneCore();}finally{if(hubTimingEnabled){hubSceneCalls++;hubSceneTicks+=Stopwatch.GetTimestamp()-started;}}}
 void RefreshSceneCore() {
  if(heading==null)return;
  syncingComparison=true;comparison.Visible=cycleMode=="Operation"&&!OperationExecuted&&Map(Get(snapshot,"predicted"))!=null;comparison.Checked=Predicted;comparison.Text=Predicted?"Hide after overlay":"Show after overlay";syncingComparison=false;
  syncingEffectScope=true;effectScopePicker.SelectedIndex=CycleScopeRequested!=null?CycleScopeIndex:scope=="body"?0:1;syncingEffectScope=false;effectScopePicker.Enabled=!OperationExecuted&&EffectScopeRequested!=null;effectScopePicker.AccessibleDescription="Selected macro: that fixed library action. Macro steps: the exact composed Macro segment. All steps: complete Prepare / Macro / Cleanup. This changes inspection only, never executes or replaces steps.";tip.SetToolTip(effectScopePicker,effectScopePicker.AccessibleDescription);
  string state=Predicted?"Actual pieces + PREVIEW": "Actual pieces";
  string operation=DisplayEffect==null?"Effect not checked":(scope=="complete"?"Whole operation":"Macro")+" effect";
  if(requestedPrediction&&!Predicted)operation="Preview unavailable: check the whole operation first.";
  heading.Text=PhaseForecast?"FORECAST · After "+Char.ToUpperInvariant(Boundary[0])+Boundary.Substring(1)+" · actual pieces remain solid":phaseInspection!=null?"Actual state · committed":phaseReason.Length>0?"Actual state · "+phaseReason:requestedPrediction&&!Predicted?"Preview unavailable · check all steps":state;
  if(cycleMode=="Current")heading.Text=cycleProjection!=null&&Flag(Get(cycleProjection,"projection_available"))?"Current residual · committed state":"Current residual · not checked";else if(cycleMode=="After")heading.Text=ResidualAfter?"After · predicted residual · not executed":"After · prediction unavailable";
  if(OperationExecuted)heading.Text="Executed · choose New or Reuse steps";
  activateNext.Visible=Get(snapshot,"next")!=null;activateNext.Enabled=CommandRequested!=null;
  browseMacro.Text="Choose macro"+Hint("macro-search");browseMacro.Enabled=CommandRequested!=null;
  var star=Map(Get(DisplayEffect,"star"));
  if(star!=null) {
   string binding="Macro A=P"+Id(Get(star,"a"))+" · B=P"+Id(Get(star,"b"))+" · Target=P"+Id(Get(star,"target"))+" · O"+Id(Get(star,"orbit")).ToString("00")+"/node"+Id(Get(star,"node"));
   var ordered=new List<string>();foreach(object slot in Items(Get(star,"frame")))ordered.Add("S"+Id(slot));
   heading.AccessibleDescription=heading.Text+". "+operation+". "+binding+". Exact ordered frame: "+String.Join(", ",ordered);
   tip.SetToolTip(heading,heading.AccessibleDescription);
  }else { heading.AccessibleDescription=heading.Text+". "+operation+". No retained star frame is claimed.";tip.SetToolTip(heading,heading.AccessibleDescription); }
  heading.ForeColor=Predicted||PhaseForecast?amber:ink;
  foreach(Button phase in phaseButtons){bool active=(Boundary.Length==0?"actual":Boundary)==(string)phase.Tag;phase.BackColor=active?raised:phaseBar.BackColor;phase.ForeColor=active?blue:ink;phase.Enabled=PhaseRequested!=null&&(!OperationExecuted||(string)phase.Tag=="actual");}
  foreach(var item in actionItems)item.ShortcutKeyDisplayString=Hint((string)item.Tag).Trim();addHomeMenu.ShortcutKeyDisplayString=Hint("block-add").Trim();
  foreach(Button b in objects){var t=(ObjectTag)b.Tag;var token=(ObjectButton)b;bool selected=!SelectionIsForecast&&t.Kind==selectedKind&&t.Id==selectedId;b.ForeColor=selected?blue:ink;b.FlatAppearance.BorderSize=selected?2:t.Kind=="identity"?1:0;b.FlatAppearance.BorderColor=selected||t.Kind=="identity"&&token.RoleMark=="Current"?blue:t.Kind=="identity"&&token.RoleMark=="Next"?amber:line;b.BackColor=selected?Color.FromArgb(31,58,69):t.Kind=="identity"?raised:BackColor;}
  foreach(ObjectButton button in objects){var tag=(ObjectTag)button.Tag;if(tag.Kind!="position")continue;var socket=sockets.Find(s=>s.Position==tag.Id);if(socket==null||socket.TrackedIndex>=0)continue;button.PolicyMark=socket.Conflict?"conflict":socket.Protected?"locked":socket.Orbit< -1?"unknown":"";button.PolicyColor=socket.Conflict?red:socket.Protected?green:amber;}
  foreach(Button b in forecastObjects){bool selected=SelectionIsForecast&&((ObjectTag)b.Tag).Id==selectedId;b.ForeColor=selected?blue:amber;b.BackColor=BackColor;}
  RefreshProtection();
  PlaceObjectActions();
  RefreshFrameReadout();
  RefreshTraces();
  canvas.Invalidate();
 }
 bool ReviewCurrent {get{string value=ValueText(Get(Map(Get(snapshot,"review")),"status"));return !withdrawingPhase&&(value=="Ready"||value=="Staged"||value=="Conflict");}}
 internal void SetReviewEvidence(string context,IEnumerable<int> identities){reviewEvidenceContext=context;reviewEvidence.Clear();foreach(int identity in identities)reviewEvidence.Add(identity);RefreshReviewEvidence();}
 internal int ReviewEvidenceVisibleCount {get{return objects.Concat(forecastObjects).OfType<ObjectButton>().Where(b=>b.EvidenceHighlighted).Select(b=>((ObjectTag)b.Tag).Id).Distinct().Count();}}
 void RefreshReviewEvidence(){
  bool current=ReviewCurrent&&reviewEvidenceContext==ValueText(Get(Map(Get(snapshot,"review_context")),"id"));
  foreach(ObjectButton button in objects.Concat(forecastObjects)){var tag=(ObjectTag)button.Tag;bool marked=current&&tag.Kind=="identity"&&reviewEvidence.Contains(tag.Id);if(button.EvidenceHighlighted!=marked){button.EvidenceHighlighted=marked;button.Invalidate();}}
 }
 void RefreshProtection() {
  var r=Map(Get(snapshot,"review"));var records=Items(Get(r,"conflicts"));var positions=Items(Get(r,"block_conflicts"));var prefix=Map(Get(r,"prefix"));string prefixState=ValueText(Get(prefix,"status"));int first=Id(Get(prefix,"first_violation"));
  bool current=ReviewCurrent,knownNet=Get(r,"conflicts") is IList&&Get(r,"block_conflicts") is IList,netConflict=records.Count>0||positions.Count>0,prefixConflict=prefixState=="Violation";
  var lines=new List<string>();var details=new List<string>();
  if(!current){lines.Add("Unchecked"+(r!=null?" · old":""));details.Add(r==null?"The complete operation has not been checked.":"Historical results only. Current protection is unchecked; review all steps again.");}
  else {lines.Add(!knownNet?"End: unchecked":netConflict?"End: CONFLICT":"End: preserved");details.Add("Complete Prepare / Macro / Cleanup, including hidden regions. Protection set on a socket is a policy, not a check result.");}
  if(records.Count>0) {
   var firstRecord=Map(records[0]);int orbit=Orbit(Get(firstRecord,"orbit")),pieces=Id(Get(firstRecord,"pieces")),labels=Id(Get(firstRecord,"stickers"));
   string caption=orbit>=0&&OrbitLabel!=null?OrbitLabel(orbit):null;
   if(!String.IsNullOrEmpty(caption)) {
    if(!current)lines[0]="Unchecked · old";
    if(records.Count>1)lines[0]+=" +"+(records.Count-1)+" orbits";
    lines.AddRange(CaptionLines(caption));
    lines.Add((pieces>=0?pieces.ToString():"?")+" pieces / "+(labels>=0?labels.ToString():"?")+" labels");
   }else {
    lines.Add((current?"":"Old ")+(orbit>=0?"O"+orbit.ToString("00"):"Orbit ?")+": "+(pieces>=0?pieces.ToString():"?")+" pieces");
    lines.Add((labels>=0?labels.ToString():"?")+" labels"+(positions.Count>0?"; "+positions.Count+" positions":""));
    if(records.Count>1)lines.Add("+"+(records.Count-1)+" more orbits");
   }
  }else if(positions.Count>0)lines.Add(positions.Count+" positions change");
  else if(r!=null&&!current&&knownNet)lines.Add("Old end: preserved");
  foreach(object raw in records){var item=Map(raw);int orbit=Orbit(Get(item,"orbit"));string caption=orbit>=0&&OrbitLabel!=null?OrbitLabel(orbit):null;details.Add((current?"":"Historical ")+(!String.IsNullOrEmpty(caption)?caption+" (O"+orbit.ToString("00")+")":orbit>=0?"O"+orbit.ToString("00"):"Unknown orbit")+": "+ValueText(Get(item,"pieces"))+" pieces / "+ValueText(Get(item,"stickers"))+" labels affected. Aggregate counts do not identify individual conflicting positions.");}
  foreach(object p in positions)details.Add((current?"":"Historical ")+"protected position P"+Id(p)+" changes at the complete-operation boundary.");
  if(prefixConflict){lines.Add((current?"First change: ":"Old prefix: ")+"turn "+(first>0?first.ToString():"?"));details.Add((current?"":"Historical ")+"strict-prefix violation at turn "+(first>0?first.ToString():"unknown")+" (1-based index in the expanded primitive operation).");}
  else if(current&&prefixState=="Preserved"){lines.Add("Each turn: preserved");details.Add("Protected state is preserved at every verified primitive prefix.");}
  else if(current){lines.Add("During: unchecked");details.Add("Intermediate movement has not been checked. Net preservation does not certify each intermediate turn.");}
  var protectedOrbits=new HashSet<int>();foreach(object raw in Items(Get(snapshot,"protected"))){int orbit=Orbit(raw);if(orbit>=0)protectedOrbits.Add(orbit);}
  var declaredPositions=new HashSet<int>();var lockDetails=new List<string>();object lockSource=Get(snapshot,"position_locks")??Get(Map(Get(Map(Get(snapshot,"workspace")),"block")),"protected");
  foreach(object raw in Items(lockSource)){var record=Map(raw);int position=record==null?Id(raw):Id(Get(record,"position"));if(position<0)continue;declaredPositions.Add(position);var labels=new List<string>();foreach(object label in Items(Get(record,"labels")))labels.Add("Sticker"+Id(label));lockDetails.Add("P"+position+": "+(labels.Count>0?String.Join(", ",labels.ToArray()):"captured labels unavailable"));}
  string declared="Protection set: "+protectedOrbits.Count+" orbits · "+declaredPositions.Count+" positions";
  var visible=new List<string>{declared};visible.AddRange(WrappedLines("All steps · "+String.Join(" · ",lines.ToArray()),Math.Max(100,ClientSize.Width-U(20))));
  var scopeDetails=new List<string>{declared+". Declared scope is not a verification result; includes offscreen and inactive-work requirements."};foreach(int orbit in protectedOrbits){string caption=OrbitLabel==null?null:OrbitLabel(orbit);scopeDetails.Add("Protected orbit: "+(String.IsNullOrEmpty(caption)?"O"+orbit.ToString("00"):caption+" (O"+orbit.ToString("00")+")"));}scopeDetails.AddRange(lockDetails);scopeDetails.AddRange(details);
  reviewStatus.Text=String.Join("\n",visible.ToArray());reviewStatus.AccessibleDescription=String.Join("\n",scopeDetails.ToArray());tip.SetToolTip(reviewStatus,reviewStatus.AccessibleDescription+"\nOpen read-only check results."+Hint("review-details"));
  reviewStatus.Height=visible.Count*Font.Height+U(8);
  reviewStatus.ForeColor=!current||!knownNet?amber:netConflict||prefixConflict?red:prefixState=="Preserved"?green:ink;reviewStatus.FlatAppearance.BorderColor=reviewStatus.ForeColor;reviewStatus.Enabled=r!=null&&CommandRequested!=null;
 }
 string CycleCaption(Dictionary<string,object> cycle,int index,Dictionary<string,object> exactEffect) {
  int orbit=Orbit(Get(cycle,"orbit"));string caption=OrbitLabel==null?null:OrbitLabel(orbit);if(String.IsNullOrEmpty(caption))caption=orbit>=0?"O"+orbit.ToString("00"):"Unknown orbit";
  var star=Map(Get(exactEffect,"star"));var certificate=Map(Get(star,"certificate"));string relation="Cycle";
  if(Flag(Get(certificate,"legal_seed_replayed"))) {var roleSet=new HashSet<int>();foreach(string role in new[]{"a","b","target"}){int p=Id(Get(star,role));if(p>=0)roleSet.Add(p);}var positions=Ids(Get(cycle,"positions"));if(roleSet.Count==3&&positions.SetEquals(roleSet))relation="A/B/Target";else relation=orbit==Orbit(Get(star,"orbit"))?"Other same-orbit cycle":"Collateral";}
  return relation+" · "+caption+" · #"+(index+1);
 }
 IEnumerable<string> WrappedLines(string text,int width) {string line="";foreach(string word in text.Split(' ')){string candidate=line.Length==0?word:line+" "+word;if(line.Length>0&&TextRenderer.MeasureText(candidate,Font).Width>width){yield return line;line=word;}else line=candidate;}if(line.Length>0)yield return line;}
 IEnumerable<string> CaptionLines(string caption) {
  int width=Math.Max(80,reviewStatus.ClientSize.Width-8);string remaining=caption.Trim();
  for(int lineIndex=0;lineIndex<2&&remaining.Length>0;lineIndex++) {
   if(TextRenderer.MeasureText(remaining,Font).Width<=width){yield return remaining;yield break;}
   int length=remaining.Length;while(length>1&&TextRenderer.MeasureText(remaining.Substring(0,length)+(lineIndex==1?"…":""),Font).Width>width)length--;
   int space=remaining.LastIndexOf(' ',Math.Max(0,length-1),length);if(space>0)length=space;
   yield return remaining.Substring(0,length).TrimEnd()+(lineIndex==1?"…":"");remaining=remaining.Substring(length).TrimStart();
  }
 }
 string Hint(string command){string value=KeyLabel==null?"":KeyLabel(command);return String.IsNullOrEmpty(value)?"":value.StartsWith(" ")?value:" "+value;}
 void ObjectKey(object sender,KeyEventArgs e) {
  if(e.KeyCode==Keys.Escape){CancelPendingInteraction();e.Handled=true;return;}
  if(e.KeyCode==Keys.F10&&e.Shift) {RequestMenu((Button)sender);e.Handled=true;e.SuppressKeyPress=true;return;}
  if(e.KeyCode!=Keys.Left&&e.KeyCode!=Keys.Right&&e.KeyCode!=Keys.Up&&e.KeyCode!=Keys.Down)return;
  var from=(Button)sender; Button best=null; double bestCost=Double.MaxValue;
  double fx=from.Left+from.Width*.5,fy=from.Top+from.Height*.5;
  var navigable=new List<Button>(objects);navigable.AddRange(forecastObjects);navigable.AddRange(blockObjects);
  foreach(Button candidate in navigable) {
   if(candidate==from)continue; double dx=candidate.Left+candidate.Width*.5-fx,dy=candidate.Top+candidate.Height*.5-fy;
   double along=e.KeyCode==Keys.Left?-dx:e.KeyCode==Keys.Right?dx:e.KeyCode==Keys.Up?-dy:dy;
   double across=e.KeyCode==Keys.Left||e.KeyCode==Keys.Right?Math.Abs(dy):Math.Abs(dx);
   if(along<=1)continue;double cost=along+across*4;if(cost<bestCost){bestCost=cost;best=candidate;}
  }
  if(best!=null)best.Focus(); e.Handled=true;e.SuppressKeyPress=true;
 }
 void PlaceObjectActions() {
  var selected=SelectedButton();objectActions.Visible=selected!=null;objectActions.Enabled=selected!=null&&SelectionAcknowledged(selectedKind,selectedId);
  addHomeMenu.Enabled=selectedKind=="identity"&&SelectionAcknowledged(selectedKind,selectedId)&&CommandRequested!=null;
  if(selected==null)return;
  Socket socket=sockets.Find(s=>selectedKind=="position"?s.Position==selectedId:SelectionIsForecast?s.AfterIdentity==selectedId:s.Identity==selectedId);
  Button anchor=socket==null?selected:objects.Find(b=>((ObjectTag)b.Tag).Kind=="position"&&((ObjectTag)b.Tag).Id==socket.Position)??selected;
  if(objectActions.Parent!=anchor)anchor.Controls.Add(objectActions);
  objectActions.Bounds=new Rectangle(anchor.Width-U(25),anchor.Image!=null?Font.Height+U(5):0,U(24),U(25));objectActions.TabIndex=selected.TabIndex+1;objectActions.BringToFront();
  tip.SetToolTip(objectActions,(selectedKind=="position"?"P":"I")+selectedId+" actions · Shift+F10. Selection does not execute an operation.");
 }
 void KeepVisible(Button button) {
  if(button.Parent!=canvas)return;
  Point point=scroll.PointToClient(button.PointToScreen(Point.Empty));
  int x=-scroll.AutoScrollPosition.X,y=-scroll.AutoScrollPosition.Y;
  if(point.X<0)x+=point.X;else if(point.X+button.Width>scroll.ClientSize.Width)x+=point.X+button.Width-scroll.ClientSize.Width;
  if(point.Y<0)y+=point.Y;else if(point.Y+button.Height>scroll.ClientSize.Height)y+=point.Y+button.Height-scroll.ClientSize.Height;
  scroll.AutoScrollPosition=new Point(Math.Max(0,x),Math.Max(0,y));
 }
 void LabelAt(Graphics g,string value,float x,float y,Color color,bool centered=true) {
  using(var brush=new SolidBrush(color)) using(var format=new StringFormat {Alignment=centered?StringAlignment.Center:StringAlignment.Near})
   g.DrawString(value,Font,brush,new PointF(U(x),U(y)),format);
 }
 void CanvasSelect(object sender,MouseEventArgs e) {
  using(var hit=new Pen(Color.Black,U(12)))foreach(var segment in cycleSegments)if(segment.Path.IsOutlineVisible(e.Location,hit)){inspectedCycleSource=segment.Source;inspectedCycleDestination=segment.Destination;entryPane.Visible=true;RefreshFrameReadout();LayoutScene();return;}
 }
 Image StickerImage(Dictionary<string,object> record,int width,int height) {long started=hubTimingEnabled?Stopwatch.GetTimestamp():0;try{return StickerImageCore(record,width,height);}finally{if(hubTimingEnabled){hubStickerCalls++;hubStickerTicks+=Stopwatch.GetTimestamp()-started;}}}
 Image StickerImageCore(Dictionary<string,object> record,int width,int height) {
  var image=new Bitmap(Math.Max(1,width),Math.Max(1,height));using(var g=Graphics.FromImage(image)){g.Clear(BackColor);var slots=Items(Get(record,"slots"));var labels=Items(Get(record,"labels"));if(geometry==null||slots.Count==0||slots.Count!=labels.Count)return image;
   for(int i=0;i<labels.Count;i++){int label=Id(labels[i]);if(label<0||label>=259800)continue;using(var brush=new SolidBrush(geometry.CellColor(label/433)))g.FillRectangle(brush,i*width/(float)labels.Count,0,(i+1)*width/(float)labels.Count-i*width/(float)labels.Count,height);}}
  return image;
 }
 Image IdentityImage(int[] cells,int width,int height) {long started=hubTimingEnabled?Stopwatch.GetTimestamp():0;try{return IdentityImageCore(cells,width,height);}finally{if(hubTimingEnabled){hubIdentityCalls++;hubIdentityTicks+=Stopwatch.GetTimestamp()-started;}}}
 Image IdentityImageCore(int[] cells,int width,int height) {
  var bitmap=new Bitmap(Math.Max(1,width),Math.Max(1,height));
  using(var g=Graphics.FromImage(bitmap)) {
   g.Clear(BackColor);
   if(geometry==null||cells.Length==0) {using(var p=new Pen(line))g.DrawLine(p,0,height/2,width,height/2);return bitmap;}
   for(int i=0;i<cells.Length;i++)using(var brush=new SolidBrush(geometry.CellColor(cells[i]-1)))g.FillRectangle(brush,i*width/(float)cells.Length,0,(i+1)*width/(float)cells.Length-i*width/(float)cells.Length,height);
  }
  return bitmap;
 }
 void RefreshFrameReadout() {
  if(RefreshCycleCorrespondence())return;
  var entry=MacroEntry;var roles=Map(Get(entry,"roles"));Dictionary<string,object> selectedRole=null;string roleName="";
  if(inspectedCycleSource>=0){frameReadout.Text="Selected fixed action: "+PositionCaption(inspectedCycleSource)+" → "+PositionCaption(inspectedCycleDestination)+"\nSelect a position for its ordered entry slots.";frameReadout.AccessibleDescription=frameReadout.Text+" This is an exact source-to-destination action, not a setup path. Cycle picker Enter advances through the directed links.";tip.SetToolTip(frameReadout,frameReadout.AccessibleDescription);return;}
  if(roles!=null)foreach(string role in new[]{"a","b","target"}){var record=Map(Get(roles,role));if(record!=null&&(selectedKind=="position"?Id(Get(record,"position"))==selectedId:Id(Get(record,"piece"))==selectedId)){selectedRole=record;roleName=role.ToUpperInvariant();break;}}
  if(selectedRole!=null){var pairs=new List<string>();foreach(object raw in Items(Get(selectedRole,"correspondence"))){var pair=Map(raw);pairs.Add(SlotCaption(Id(Get(pair,"slot")))+" ← sticker from "+SlotCaption(Id(Get(pair,"label"))));}frameReadout.Text="Macro entry · after Prepare · "+roleName+" at "+PositionCaption(selectedRole)+"\r\n"+String.Join("\r\n",pairs.ToArray());frameReadout.AccessibleDescription=frameReadout.Text+"\r\n"+ValueText(Get(entry,"frame_status"));}
  else if(entry!=null){frameReadout.Text="Select a macro position to inspect its entry after Prepare.";frameReadout.AccessibleDescription=ValueText(Get(entry,"frame_status"));}
  else {frameReadout.Text=phaseInspection==null?"Choose a phase to inspect your entered steps.":"No fixed macro entry selected.";frameReadout.AccessibleDescription="No ordered frame is inferred from geometry or the work target.";}
  tip.SetToolTip(frameReadout,frameReadout.AccessibleDescription);
 }
 void RefreshTraces() {
  RefreshTrace(currentTrace,"Current","current");RefreshTrace(nextTrace,"Next","next");
  LayoutHeader();
 }
 void RefreshTrace(LinkLabel trace,string title,string key,bool compact=false) {
  var actual=Map(Get(snapshot,key));var shown=PhaseForecast?Map(Get(phaseInspection,key)):actual;
  int identity=Id(Get(actual,"piece")),position=Id(Get(shown,"position")),actualPosition=Id(Get(actual,"position"));trace.Links.Clear();
  if(identity<0){trace.Text=title+" not set";trace.AccessibleName=trace.Text;return;}
  string identityText=Named(actual,"identity_short","I"+identity),positionText=position>=0?Named(shown,"current_short","P"+position):"position unknown";
  trace.Text=title+" · Home "+identityText+"\n"+(PhaseForecast?"After ":"At ")+positionText;
  trace.AccessibleName=title+" identity "+identityText+(PhaseForecast?", forecast after "+Boundary:", actual")+" at "+positionText;
  trace.AccessibleDescription=NameDetails(actual)+"\nCanonical identity I"+identity+" is at P"+actualPosition+". The first link tracks identity; the second inspects the fixed position. Its actual occupant is not inferred from the forecast.";
  trace.AccessibleDescription+=" "+FilterDescription(actual,Map(Get(snapshot,"piece_filter")),"Actual");if(PhaseForecast)trace.AccessibleDescription+=" "+FilterDescription(shown,Map(Get(phaseInspection,"piece_filter")),"After "+Boundary);
  trace.Links.Add(trace.Text.IndexOf(identityText),identityText.Length,new ObjectTag("identity",identity,PhaseForecast));
  if(position>=0)trace.Links.Add(trace.Text.LastIndexOf(positionText),positionText.Length,new ObjectTag("position",position));tip.SetToolTip(trace,trace.AccessibleDescription);
 }
 void DrawIdentityGhost(Graphics g,Socket s) {
  float x=SocketX(s),y=SocketY(s)+GeometryHeight(s)+39;
  using(var p=new Pen(s.Conflict?red:amber,UiScale)){p.DashStyle=DashStyle.Dash;g.DrawRectangle(p,U(x-GeometryWidth(s)*.5),U(y),U(GeometryWidth(s)),U(66));}
  if(s.AfterHome.Length>0)using(var colors=IdentityImage(s.AfterHome,U(148),U(3)))DrawFilteredImage(g,colors,new Point(U(x-74),U(y+2)),PieceFilterMatch(s.Forecast,s.ForecastFilter));
  string text=s.AfterKnown&&s.AfterIdentity>=0?IdentityCaption(s.Forecast):"unknown";
  LabelAt(g,"After",x,y+5,s.Conflict?red:amber);LabelAt(g,text,x,y+5+Font.Height/UiScale,s.Conflict?red:amber);
  bool? match=PieceFilterMatch(s.Forecast,s.ForecastFilter);if(match!=true)LabelAt(g,match==false?"×":"?",x-72,y+5,match==false?muted:amber);
 }
 string Topology(Socket a,Socket b) {
  if(geometry==null)return null;
  foreach(int ca in a.Hosting)foreach(int cb in b.Hosting) {
   if(ca==cb)return "shared C"+ca;
   int count=0;for(int i=0;i<4;i++)for(int j=0;j<4;j++)if(geometry.CellVertex(ca-1,i)==geometry.CellVertex(cb-1,j))count++;
   if(count==3)return "C"+ca+" / C"+cb+" shared face";
  }
  return null;
 }
 void PaintCanvas(object sender,PaintEventArgs e) {
  var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
  foreach(var segment in cycleSegments)segment.Path.Dispose();cycleSegments.Clear();
  var block=Map(Get(Map(Get(snapshot,"workspace")),"block"));string title=ValueText(Get(block,"name"));
   string selectedKind=ValueText(Get(Map(Get(CycleProjection,"selected")),"kind"));int cycleLength=Id(Get(Map(Get(CycleProjection,"selected")),"length"));LabelAt(g,selectedKind=="unchanged"?"Inspected position · no net change":cycleLength>0?(selectedKind=="orientation"?"Orientation at a fixed position":cycleLength+"-piece cycle"):title.Length>0?title:"Work positions",12,84,ink,false);
  if(workGroupY>0){using(var pen=new Pen(line))g.DrawLine(pen,U(12),U(workGroupY-8),U(LogicalWidth-12),U(workGroupY-8));LabelAt(g,title.Length>0?title:"Other work positions",12,workGroupY,muted,false);}
  var indexed=new Dictionary<int,Socket>();foreach(Socket s in sockets)if(s.TrackedIndex<0&&s.Position>=0)indexed[s.Position]=s;
  var drawnEdges=DetailedEdges.Cast<object>().Select(Map).ToList();

  foreach(var edge in drawnEdges) {
   Socket a,b;if(!indexed.TryGetValue(Id(Get(edge,"source_position")),out a)||!indexed.TryGetValue(Id(Get(edge,"destination_position")),out b))continue;
   float ax=SocketX(a),ay=SocketY(a)-7,bx=SocketX(b),by=SocketY(b)-7;
   float sx=ax+GeometryWidth(a)*.5f+5,ex=bx-GeometryWidth(b)*.5f-5;
   var path=new GraphicsPath();
   if(a==b){path.AddBezier(new PointF(U(sx),U(ay)),new PointF(U(sx+34),U(ay-40)),new PointF(U(sx+34),U(ay+40)),new PointF(U(sx),U(ay+18)));}
   else if(Math.Abs(ay-by)<1&&sx<ex){path.AddLine(new PointF(U(sx),U(ay)),new PointF(U(ex),U(by)));}
   else {
    float lower=Math.Max(SocketY(a)+GeometryHeight(a),SocketY(b)+GeometryHeight(b))+85+(PhaseForecast||Predicted?70:0);
    float lane=Math.Min(LogicalWidth-10,Math.Max(sx,bx+GeometryWidth(b)*.5f)+30),left=Math.Max(10,Math.Min(ex,ax-GeometryWidth(a)*.5f)-30);
    path.AddBezier(new PointF(U(sx),U(ay)),new PointF(U(lane),U(ay)),new PointF(U(lane),U(lower)),new PointF(U(lane-10),U(lower)));
    path.AddLine(new PointF(U(lane-10),U(lower)),new PointF(U(left+10),U(lower)));
    path.AddBezier(new PointF(U(left+10),U(lower)),new PointF(U(left),U(lower)),new PointF(U(left),U(by)),new PointF(U(ex),U(by)));
   }
   cycleSegments.Add(new CycleSegment{Path=path,Source=a.Position,Destination=b.Position});
   bool selected=inspectedCycleSource==a.Position&&inspectedCycleDestination==b.Position;
   using(var pen=new Pen(cycleMode=="After"?amber:blue,Math.Max(selected?3:1.8f,UiScale*1.5f)))using(var arrow=new AdjustableArrowCap(4,5)){pen.DashStyle=cycleMode=="Operation"?DashStyle.Solid:DashStyle.Dash;pen.CustomEndCap=arrow;g.DrawPath(pen,path);}
   string reference=ValueText(Get(Map(Get(edge,"reference")),"status"));
   if(reference!="Matched")LabelAt(g,reference=="Mismatch"?"Frame Δ":"Frame ?",sx+4,ay+8,reference=="Mismatch"?amber:muted,false);
  }
  foreach(Socket socket in sockets) {
   if(socket.TrackedIndex>=0)continue;float x=SocketX(socket),y=SocketY(socket);
   if(socket.Identity<0)LabelAt(g,"Occupant unavailable",x,y+GeometryHeight(socket)-27,amber);
   if(socket.Position<0)LabelAt(g,"Unassigned",x,y-27,amber);
   if(Predicted&&!PhaseForecast)DrawIdentityGhost(g,socket);
   if(socket.FrameChanged)LabelAt(g,cycleMode=="Operation"?"Orientation changes":"Orientation residual",x,y-47,amber);
  }
   string relation=selectedKind=="unchanged"?"The inspected fixed position has no net change; intermediate protection is checked separately.":cycleMode=="Operation"?"Solid arrows: exact action of the chosen operation.":cycleMode=="After"?"Dashed arrows: predicted position to Home; not executable moves.":"Dashed arrows: current position to Home; not executable moves.";
   canvas.AccessibleDescription="Fixed position sockets contain actual piece tokens; outlined tokens are predictions. "+relation+(selectedKind=="unchanged"?" No effect edge is drawn for this position; use Local for real structure.":" Select an arrow for exact slot correspondence; use Local for real structure.");
   tip.SetToolTip(canvas,selectedKind=="unchanged"?"No net change. Strict protection checks intermediate turns.":cycleMode=="Operation"?"Solid arrows: operation effect.\nSelect an edge to inspect slots.":"Dashed arrows: depicted position → Home.\nOwnership only; not executable moves.");
 }
 protected override void OnLeave(EventArgs e){if(!rebuilding&&!objectMenu.Visible)CancelPendingInteraction();base.OnLeave(e);}
 protected override void Dispose(bool disposing) { if(disposing){tip.Dispose();objectMenu.Dispose();foreach(Button button in forecastObjects)if(button.Image!=null)button.Image.Dispose();foreach(var segment in cycleSegments)segment.Path.Dispose();foreach(Button b in objects)if(b.Image!=null)b.Image.Dispose();}base.Dispose(disposing); }
}
