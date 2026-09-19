// Exact cycle display only. The host owns request binding and all mutations.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class ExperimentHub {
 long cycleRingCreated,cycleRingDisposed;
 long[] BeginCycleLifecycleTiming(){return hubTimingEnabled?new[]{Stopwatch.GetTimestamp(),cycleRingCreated,cycleRingDisposed}:null;}
 void WriteCycleLifecycleTiming(string operation,long[] start){
  if(start==null)return;
  try{long completed=Stopwatch.GetTimestamp();NativeDiagnostics.Write(String.Format(System.Globalization.CultureInfo.InvariantCulture,
   "Experiment cycle lifecycle timing: operation={0} total_ms={1:F3} created={2} disposed={3}",
   operation,(completed-start[0])*1000.0/Stopwatch.Frequency,cycleRingCreated-start[1],cycleRingDisposed-start[2]));}catch{}
 }
 Dictionary<string,object> cycleProjection;
 string cycleProjectionError="",cycleScope="selected-macro",cycleMode="Current";
 readonly List<Button> cycleModeButtons=new List<Button>();
 readonly Panel cycleToolbar=new Panel();
 bool cycleDisplayActive,syncingCycleOrbit;
 int? requestedCycleOrbit;
 int pendingCycleFocus=-1;
 Form cycleFocusOwner;
 bool cycleFocusQueued;
 readonly List<CycleChoice> detailedChoices=new List<CycleChoice>();
 readonly Panel cycleOverview=new Panel(),cycleRings=new Panel(),cycleSlotScroll=new Panel();
 readonly ComboBox cycleOrbitPicker=new ComboBox();
 readonly Label cycleBoundary=new Label();
 readonly CyclePaintSurface cycleSlotGraphic=new CyclePaintSurface();
 Dictionary<string,object> correspondenceEdge;

 internal event Action<string> CycleScopeRequested;
 internal event Action<string> CycleModeRequested;
 internal event Action<int?> CycleOrbitRequested;
 internal event Action<int> CycleObjectRequested;
 internal Dictionary<string,object> CycleProjection {get{return cycleProjection;}}
 internal int CycleScopeIndex {get{return cycleScope=="macro-steps"?1:cycleScope=="complete"?2:0;}}
 internal IList DetailedEdges {get{return Items(Get(Map(Get(cycleProjection,"selected")),"edges"));}}
 bool ResidualAfter {get{return cycleMode=="After"&&cycleProjection!=null&&Flag(Get(cycleProjection,"projection_available"));}}
 internal void SetCycleMode(string value){cycleMode=value;foreach(var button in cycleModeButtons){bool active=(string)button.Tag==value;button.BackColor=active?raised:BackColor;button.ForeColor=active?blue:ink;button.FlatAppearance.BorderSize=active?1:0;}}
 void LayoutCycleSelectors(){
  cycleToolbar.Height=U(32);
  int x=8;foreach(var button in cycleModeButtons){button.SetBounds(U(x),U(1),U(76),U(28));x+=78;}
  effectScopePicker.Visible=cycleMode=="Operation";
  if(effectScopePicker.Visible){effectScopePicker.SetBounds(U(x+2),U(1),U(128),U(28));x+=136;}
  cyclePicker.Bounds=new Rectangle(U(x+2),U(1),U(Math.Max(120,OperationWidth-x-120)),U(28));
 }

 sealed class CycleChoice {
  internal int Anchor,Length;
  internal bool Orientation,Unchanged;
  internal string Caption;
  public override string ToString(){return Caption;}
 }
 sealed class CyclePaintSurface : Control {
  internal Action<Graphics> PaintContent;
  internal CyclePaintSurface(){SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint,true);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(PaintContent!=null)PaintContent(e.Graphics);}
 }
 sealed class CycleRingButton : Button {
  internal string PresentationKey;
  internal ExperimentHub Hub;
  internal CycleChoice Choice;
  internal bool Selected;
  protected override bool IsInputKey(Keys keyData){Keys key=keyData&Keys.KeyCode;return key==Keys.Left||key==Keys.Right||base.IsInputKey(keyData);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.Clear(Selected?Hub.raised:BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
   float cx=Width/2f,cy=Hub.U(17),radius=Hub.U(12);Color color=Selected?Hub.blue:Hub.ink;
   if(Choice.Unchanged){using(var pen=new Pen(color,1.2f))g.DrawRectangle(pen,cx-radius/2,cy-radius/2,radius,radius);}
   else {using(var pen=new Pen(color,1.2f))using(var arrow=new AdjustableArrowCap(3,4)){
    pen.CustomEndCap=arrow;g.DrawArc(pen,cx-radius,cy-radius,2*radius,2*radius,-75,305);
   }
   if(Choice.Length<=8&&!Choice.Orientation)for(int n=0;n<Choice.Length;n++){
    double angle=(-90+360d*n/Choice.Length)*Math.PI/180;
    using(var brush=new SolidBrush(color))g.FillEllipse(brush,cx+(float)Math.Cos(angle)*radius-2,cy+(float)Math.Sin(angle)*radius-2,4,4);
   }
   TextRenderer.DrawText(g,Choice.Orientation?"↻":Choice.Length.ToString(),Font,new Rectangle(0,Hub.U(8),Width,Font.Height),color,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);}
   TextRenderer.DrawText(g,Text,Font,new Rectangle(2,Hub.U(31),Width-4,Font.Height),color,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
   if(Selected)using(var pen=new Pen(Hub.blue,1.4f))g.DrawLine(pen,2,Height-2,Width-3,Height-2);
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(2,2,Width-5,Height-5),Hub.blue,BackColor);
  }
 }

 void InitializeCycleUI(){
  cycleToolbar.Dock=DockStyle.Top;cycleToolbar.BackColor=BackColor;Controls.Add(cycleToolbar);Controls.SetChildIndex(cycleToolbar,Controls.GetChildIndex(tracking));
  cycleToolbar.Controls.Add(effectScopePicker);cycleToolbar.Controls.Add(cyclePicker);cycleToolbar.Controls.Add(entryToggle);
  foreach(string name in new[]{"Current","Operation","After"}){string modeName=name;var button=new ResultButton{Text=name,Tag=name,Font=Font,FlatStyle=FlatStyle.Flat,BackColor=BackColor,ForeColor=ink,AccessibleName="Cycle view: "+name};button.FlatAppearance.BorderColor=blue;button.Click+=delegate{if(CycleModeRequested!=null)CycleModeRequested(modeName);};cycleModeButtons.Add(button);cycleToolbar.Controls.Add(button);tip.SetToolTip(button,name=="Current"?"Current location to Home: ownership, not a legal turn.":name=="After"?"Predicted residual after all steps; no turns are executed.":"Exact action of the explicitly chosen operation.");}
  SetCycleMode(cycleMode);
  syncingEffectScope=true;effectScopePicker.Items.Clear();effectScopePicker.Items.AddRange(new object[]{"Selected macro","Macro steps","All steps"});effectScopePicker.SelectedIndex=CycleScopeIndex;syncingEffectScope=false;
  cycleOverview.BackColor=BackColor;cycleOverview.AccessibleName="Exact cycle groups; layout is not physical distance";canvas.Controls.Add(cycleOverview);
  cycleOrbitPicker.DropDownStyle=ComboBoxStyle.DropDownList;cycleOrbitPicker.FlatStyle=FlatStyle.Flat;cycleOrbitPicker.BackColor=surface;cycleOrbitPicker.ForeColor=ink;cycleOrbitPicker.AccessibleName="Cycle inspection orbit";
  cycleOrbitPicker.SelectedIndexChanged+=delegate{if(!syncingCycleOrbit&&CycleOrbitRequested!=null)CycleOrbitRequested(cycleOrbitPicker.SelectedIndex<=0?(int?)null:cycleOrbitPicker.SelectedIndex-1);};
  cycleOverview.Controls.Add(cycleOrbitPicker);
  cycleBoundary.ForeColor=muted;cycleBoundary.BackColor=BackColor;cycleBoundary.AutoEllipsis=true;cycleOverview.Controls.Add(cycleBoundary);
  cycleRings.AutoScroll=true;cycleRings.BackColor=BackColor;cycleRings.AccessibleName="Cycles on the current index page";cycleOverview.Controls.Add(cycleRings);
  cycleSlotScroll.Dock=DockStyle.Top;cycleSlotScroll.AutoScroll=true;cycleSlotScroll.BackColor=surface;cycleSlotScroll.Visible=false;entryPane.Controls.Add(cycleSlotScroll);
  cycleSlotGraphic.BackColor=surface;cycleSlotGraphic.PaintContent=PaintCycleCorrespondence;cycleSlotScroll.Controls.Add(cycleSlotGraphic);
  cycleSlotScroll.Resize+=delegate{LayoutCycleSlots();};
  cycleSlotGraphic.MouseMove+=delegate(object sender,MouseEventArgs e){
   if(InlineCycleIdentities&&e.X>=CycleIdentityLeft){tip.SetToolTip(cycleSlotGraphic,EntryIdentityDetails(e.Y<U(48)?"source":"destination"));return;}
   if(e.Y<CycleIdentityHeight){tip.SetToolTip(cycleSlotGraphic,EntryIdentityDetails(e.Y<Font.Height+U(2)?"source":"destination"));return;}
   int index=(e.X-U(68))/U(46);var slots=Items(Get(correspondenceEdge,"slots"));
   if(e.X>=U(68)&&index>=0&&index<slots.Count)tip.SetToolTip(cycleSlotGraphic,SlotPairText(Map(slots[index])));
  };
  EnabledChanged+=delegate{if(Enabled)QueueCycleFocus();};
 }

 internal void WithdrawCycleProjection(){
  RememberCycleFocus();
  cycleDisplayActive=true;cycleProjection=null;cycleProjectionError="Effect not checked";correspondenceEdge=null;
  ClearCycleHits();cyclePositions.Clear();detailedChoices.Clear();
  syncingCycle=true;cyclePicker.Items.Clear();syncingCycle=false;cyclePicker.Visible=false;
  // Keep only this parent-owned generation; withdrawn choices have no visible or clickable surface.
  cycleRings.Visible=false;cycleRings.AutoScrollMinSize=Size.Empty;cycleRings.AutoScrollPosition=Point.Empty;cycleBoundary.Text="Effect not checked";cycleSlotScroll.Visible=false;
  frameReadout.Text="Cycle correspondence is unavailable until the current effect is checked.";
  cycleOverview.Invalidate();canvas.Invalidate();
 }
 internal void SetCycleProjection(Dictionary<string,object> value,string error,string requestedScope,int? requestedOrbit){
  cycleDisplayActive=true;cycleProjection=value;cycleProjectionError=error??"";
  cycleScope=requestedScope??"selected-macro";
  requestedCycleOrbit=requestedOrbit;
 }
 bool PopulateDetailedCycles(){var timing=BeginCycleLifecycleTiming();try{return PopulateDetailedCyclesCore();}finally{WriteCycleLifecycleTiming("PopulateDetailedCycles",timing);}}
 bool PopulateDetailedCyclesCore(){
  if(!cycleDisplayActive)return false;
  detailedChoices.Clear();cyclePositions.Clear();ClearCycleHits();
  var selected=Map(Get(cycleProjection,"selected"));int anchor=Id(Get(selected,"anchor"));
  int index=0;foreach(object raw in Items(Get(Map(Get(cycleProjection,"cycle_index")),"items"))){
   var row=Map(raw);int p=Id(Get(row,"anchor")),length=Id(Get(row,"length"));if(p<0||length<2)continue;
   detailedChoices.Add(new CycleChoice{Anchor=p,Length=length,Caption="Cycle "+(++index)+" · "+length+" pieces"});
  }
  foreach(object raw in Items(Get(Map(Get(cycleProjection,"orientation_index")),"positions"))){int p=Id(raw);if(p>=0)detailedChoices.Add(new CycleChoice{Anchor=p,Length=1,Orientation=true,Caption="Orientation "+(++index)});}
  bool unchanged=ValueText(Get(selected,"kind"))=="unchanged";
  if(anchor>=0&&!detailedChoices.Any(c=>c.Anchor==anchor))detailedChoices.Add(new CycleChoice{Anchor=anchor,Length=Id(Get(selected,"length")),Orientation=ValueText(Get(selected,"kind"))=="orientation",Unchanged=unchanged,Caption=unchanged?"Inspected position · no net change":"Selected · "+Id(Get(selected,"length"))+" positions"});
  cyclePicker.BeginUpdate();syncingCycle=true;
  try{cyclePicker.Items.Clear();foreach(var choice in detailedChoices)cyclePicker.Items.Add(choice);selectedCycle=detailedChoices.FindIndex(c=>c.Anchor==anchor);cyclePicker.SelectedIndex=selectedCycle;}
  finally{try{cyclePicker.EndUpdate();}finally{syncingCycle=false;}}
  foreach(object raw in DetailedEdges){var edge=Map(raw);foreach(string key in new[]{"source_position","destination_position"}){int p=Id(Get(edge,key));if(p>=0&&!cyclePositions.Contains(p))cyclePositions.Add(p);}}
  if(unchanged&&anchor>=0)cyclePositions.Add(anchor);
  cyclePicker.AccessibleDescription="Current index page. Each choice stores its canonical fixed-position anchor; numbering is display order only. "+(cycleMode=="Operation"?"Edges show the chosen operation.":"Edges link depicted positions to Home; they are not executable moves.")+" Other pages use the Cycles key set.";
  tip.SetToolTip(cyclePicker,cyclePicker.AccessibleDescription);
  PopulateCycleOrbit();BuildCycleRings();return true;
 }
 bool SelectDetailedCycle(int pickerIndex){
  if(!cycleDisplayActive)return false;
  if(pickerIndex>=0&&pickerIndex<detailedChoices.Count){ClearCycleHits();if(CycleObjectRequested!=null)CycleObjectRequested(detailedChoices[pickerIndex].Anchor);}
  return true;
 }
 void MergeCycleActualRecords(Dictionary<int,Dictionary<string,object>> committed){
  AddPiece(committed,Get(Map(Get(cycleProjection,"selected")),"actual_position"));
  foreach(object raw in DetailedEdges){var edge=Map(raw);foreach(string key in new[]{"actual_source","actual_destination"})AddPiece(committed,Get(edge,key));}
 }
 void MergeCycleForecastRecords(Dictionary<int,Dictionary<string,object>> after){if(ResidualAfter)foreach(object raw in DetailedEdges){var edge=Map(raw);foreach(string key in new[]{"source","destination"})AddPiece(after,Get(edge,key));}}
 void PopulateCycleOrbit(){
  int orbit=Orbit(Get(cycleProjection,"orbit"));cycleOrbitPicker.BeginUpdate();syncingCycleOrbit=true;
  try{
   cycleOrbitPicker.Items.Clear();cycleOrbitPicker.Items.Add("Working orbit");
   for(int o=0;o<35;o++){string name=OrbitLabel==null?null:OrbitLabel(o);cycleOrbitPicker.Items.Add(String.IsNullOrEmpty(name)?"Orbit "+o:name);}
   cycleOrbitPicker.SelectedIndex=requestedCycleOrbit.HasValue?requestedCycleOrbit.Value+1:0;
  }finally{try{cycleOrbitPicker.EndUpdate();}finally{syncingCycleOrbit=false;}}
  cycleOrbitPicker.Enabled=cycleMode=="Operation";
  cycleOrbitPicker.DropDownWidth=Math.Max(cycleOrbitPicker.Width,Math.Min(U(600),ClientSize.Width-U(20)));
  tip.SetToolTip(cycleOrbitPicker,orbit>=0&&OrbitLabel!=null?OrbitLabel(orbit):"Select an orbit for effect inspection; this does not change the working orbit.");
 }
 void ClearCycleRings(){var timing=BeginCycleLifecycleTiming();try{ClearCycleRingsCore();}finally{WriteCycleLifecycleTiming("ClearCycleRings",timing);}}
 void ClearCycleRingsCore(){
  foreach(Control control in cycleRings.Controls.Cast<Control>().ToArray())DisposeCycleRing(control);
  cycleRings.AutoScrollMinSize=Size.Empty;
 }
 void DisposeCycleRing(Control control){if(control==null)return;bool count=hubTimingEnabled&&!control.IsDisposed;control.Dispose();if(count&&control.IsDisposed)cycleRingDisposed++;}
 void RememberCycleFocus(){
  if(pendingCycleFocus>=0&&(!cycleRings.Visible||!Enabled))return;
  var button=cycleRings.Controls.OfType<CycleRingButton>().FirstOrDefault(b=>b.Focused);
  if(button!=null){pendingCycleFocus=button.Choice.Anchor;cycleFocusOwner=FindForm();}
 }
 void QueueCycleFocus(){
  if(pendingCycleFocus<0||cycleFocusQueued||!Enabled||!IsHandleCreated||IsDisposed)return;
  cycleFocusQueued=true;BeginInvoke(new Action(delegate{
   cycleFocusQueued=false;if(IsDisposed||!Enabled)return;
   int anchor=pendingCycleFocus;var owner=cycleFocusOwner;pendingCycleFocus=-1;cycleFocusOwner=null;
   if(anchor<0||owner==null||owner.IsDisposed||Form.ActiveForm!=owner)return;
   Control active=owner;while(active is ContainerControl){var child=((ContainerControl)active).ActiveControl;if(child==null)break;active=child;}
   // Never take focus from an editor, another work object or an auxiliary window.
   if(active!=owner&&active!=this&&active!=scroll&&active!=canvas&&active!=cycleOverview&&active!=cycleRings&&!(active is CycleRingButton))return;
   var button=cycleRings.Controls.OfType<CycleRingButton>().FirstOrDefault(b=>b.Choice.Anchor==anchor);
   if(button!=null&&button.CanFocus)button.Focus();
  }));
 }
 void BuildCycleRings(){var timing=BeginCycleLifecycleTiming();try{BuildCycleRingsCore();}finally{WriteCycleLifecycleTiming("BuildCycleRings",timing);}}
 string CycleRingPresentationKey(CycleChoice choice,int ordinal,bool selected){
  return objectKeyJson.Serialize(new object[]{ordinal,choice.Anchor,choice.Length,choice.Orientation,choice.Unchanged,choice.Caption,selected,cycleMode,
   Font.Name,Font.Size,(int)Font.Style,(int)Font.Unit,Font.GdiCharSet,Font.GdiVerticalFont,Font.Height,UiScale,
   BackColor.ToArgb(),ink.ToArgb(),blue.ToArgb(),raised.ToArgb()});
 }
 void BuildCycleRingsCore(){
  int scrollX=-cycleRings.AutoScrollPosition.X;RememberCycleFocus();
  var previous=cycleRings.Controls.OfType<CycleRingButton>().ToArray();foreach(var old in previous)old.TabIndex=0;
  cycleRings.AutoScrollMinSize=Size.Empty;cycleRings.AutoScrollPosition=Point.Empty;int anchor=Id(Get(Map(Get(cycleProjection,"selected")),"anchor")),x=0,index=0;
  try{foreach(var choice in detailedChoices){var item=choice;string key=CycleRingPresentationKey(item,index,item.Anchor==anchor);
   CycleRingButton button=index<previous.Length?previous[index]:null;
   if(button!=null&&(button.IsDisposed||button.PresentationKey!=key)){previous[index]=null;DisposeCycleRing(button);button=null;}
   if(button==null){button=new CycleRingButton{Hub=this,Choice=item,Selected=item.Anchor==anchor,Text=item.Unchanged?"Position":item.Orientation?"Frame":"Cycle",Font=Font,BackColor=BackColor,ForeColor=ink,FlatStyle=FlatStyle.Flat,TabStop=true,AccessibleName=item.Caption,AccessibleDescription="Canonical fixed-position anchor P"+item.Anchor+". "+(item.Unchanged?"No net change at this position; intermediate protection is checked separately.":cycleMode=="Operation"?"Directed operation, not geometric distance or a rigid block.":"Depicted position to Home, not an executable move.")+" Selecting only inspects."};if(hubTimingEnabled)cycleRingCreated++;button.FlatAppearance.BorderSize=0;
    var created=button;
    created.Click+=delegate{if(cycleProjection==null||!created.Visible||!created.Enabled)return;ClearCycleHits();if(CycleObjectRequested!=null)CycleObjectRequested(created.Choice.Anchor);};
    created.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Right){int n=cycleRings.Controls.GetChildIndex(created)+(e.KeyCode==Keys.Left?-1:1);if(n>=0&&n<cycleRings.Controls.Count)cycleRings.Controls[n].Focus();e.Handled=true;e.SuppressKeyPress=true;}};
    created.GotFocus+=delegate{cycleRings.ScrollControlIntoView(created);};cycleRings.Controls.Add(button);
   }
   if(index<previous.Length)previous[index]=null;
   button.Choice=item;button.PresentationKey=key;button.Font=Font;button.TabIndex=index;button.TabStop=true;button.Enabled=true;button.Visible=true;button.SetBounds(x,0,U(70),U(52));cycleRings.Controls.SetChildIndex(button,index);tip.SetToolTip(button,button.AccessibleDescription);button.Invalidate();x+=U(74);index++;
  }}catch{cycleRings.Visible=false;ClearCycleRings();throw;}
  finally{foreach(var unused in previous)DisposeCycleRing(unused);}
  cycleRings.Visible=true;
  cycleRings.AutoScrollMinSize=new Size(x,0);cycleRings.AutoScrollPosition=new Point(scrollX,0);
  string boundary=ValueText(Get(cycleProjection,"source_boundary"));
  string text=cycleProjectionError.Length>0?cycleProjectionError:cycleProjection==null?"Effect not checked":!Flag(Get(cycleProjection,"projection_available"))?"Fixed action · entry unavailable":boundary=="prepare"?"Entry: after Prepare":"Entry: actual state";
  if(cycleProjection!=null&&cycleMode!="Operation")text=!Flag(Get(cycleProjection,"projection_available"))?ValueText(Get(cycleProjection,"projection_reason")):cycleMode=="After"?"Forecast location → Home":"Current location → Home";
  var residual=Map(Get(cycleProjection,"residual"));string detail="";
  if(residual!=null){
   var frame=Map(Get(residual,"frame_status"));bool frameKnown=ValueText(Get(frame,"status"))=="Verified";
   string counts=Id(Get(Map(Get(cycleProjection,"cycle_index")),"total"))+" cycles · "+Id(Get(Map(Get(cycleProjection,"orientation_index")),"total"))+" orientation";
   text+="\n"+(!frameKnown?"Frame unverified · "+Id(Get(residual,"FrameUnknown"))+" unknown":Flag(Get(residual,"ExactSolved"))?"Exact orbit complete":Flag(Get(residual,"raw_exact_labels_solved"))?cycleMode=="After"?"Forecast labels exact · not executed":"Exact labels · completion pending":counts);
   var invariant=Map(Get(residual,"invariant_status"));
   if(ValueText(Get(invariant,"status"))=="Conflict")text=(cycleMode=="After"?"Forecast":"Current")+" · invariant conflict\nInspect Residuals in Solve";
   detail="\n"+ResidualDependencyText(residual)+"\n"+ValueText(Get(invariant,"reason"));
   }else if(ValueText(Get(Map(Get(cycleProjection,"selected")),"kind"))=="unchanged")text+="\nInspected · no net change";
   else if(cycleProjection!=null&&Flag(Get(cycleProjection,"projection_available"))&&detailedChoices.Count==0)text+="\nNo cycles on this page";
  cycleBoundary.Text=text;cycleBoundary.AccessibleDescription=text+detail+". Protection is checked on the complete operation separately.";tip.SetToolTip(cycleBoundary,cycleBoundary.AccessibleDescription);
  QueueCycleFocus();
 }
 string ResidualDependencyText(Dictionary<string,object> residual){
  var reasons=new List<string>();foreach(object raw in Items(Get(residual,"unresolved_dependencies"))){var item=Map(raw);string reason=ValueText(Get(item,"reason"));if(reason.Length>0)reasons.Add(reason);}
  return String.Join("\n",reasons.ToArray());
 }
 void LayoutCycleUI(){
  cycleOverview.SetBounds(U(8),U(4),Math.Max(U(100),canvas.Width-U(16)),U(70));
  int side=Math.Min(U(210),Math.Max(U(150),cycleOverview.Width/3));cycleOrbitPicker.SetBounds(0,0,side,U(27));
  cycleBoundary.SetBounds(0,U(29),side,U(39));cycleRings.SetBounds(side+U(10),0,Math.Max(1,cycleOverview.Width-side-U(10)),U(70));
  cycleOverview.Visible=cycleDisplayActive;LayoutCycleSlots();
 }
 bool RefreshCycleCorrespondence(){
  correspondenceEdge=null;foreach(object raw in DetailedEdges){var edge=Map(raw);if(Id(Get(edge,"source_position"))==inspectedCycleSource&&Id(Get(edge,"destination_position"))==inspectedCycleDestination){correspondenceEdge=edge;break;}}
  if(correspondenceEdge==null){cycleSlotScroll.Visible=false;return false;}
  var reference=Map(Get(correspondenceEdge,"reference"));string boundary=ValueText(Get(cycleProjection,"source_boundary"));
  string head=(cycleMode=="After"?"Predicted ownership":cycleMode=="Current"?"Current ownership":boundary=="prepare"?"Entry after Prepare":"Entry in actual state")+" · "+PositionCaption(inspectedCycleSource)+" → "+PositionCaption(inspectedCycleDestination);
  string status=ValueText(Get(reference,"status"));var lines=new List<string>{head,EntryIdentityDetails("source"),EntryIdentityDetails("destination"),"The source entry piece arrives at the destination. The destination entry occupant is not the arrival. Main canvas tokens remain actual occupants.","Fixed-position reference: "+(status.Length==0?"Unknown":status)+" · "+ValueText(Get(reference,"reason")),"Endpoints use hosting-cell colors; the moving dot uses the entry sticker color."};
  if(cycleMode!="Operation")lines[3]="The arrow links this occupant's depicted position to its Home. It is an ownership relation, not an executable move. Actual occupants remain solid; predicted occupants are outlined separately.";
  foreach(object raw in Items(Get(correspondenceEdge,"slots")))lines.Add(SlotPairText(Map(raw)));
  frameReadout.Text=String.Join("\r\n",lines.ToArray()).Replace("\r\n","\n").Replace("\r","\n").Replace("\n","\r\n");frameReadout.AccessibleDescription=frameReadout.Text;tip.SetToolTip(frameReadout,head+". Full canonical slot transport is available in this read-only text.");
  cycleSlotGraphic.AccessibleName="Exact source-to-destination slot transport";cycleSlotGraphic.AccessibleDescription=frameReadout.Text;
  cycleSlotScroll.Visible=true;entryPane.Height=U(180)+CycleIdentityHeight;LayoutCycleSlots();cycleSlotGraphic.Invalidate();return true;
 }
 int CycleIdentityLeft {get{return U(90+46*Items(Get(correspondenceEdge,"slots")).Count);}}
 bool InlineCycleIdentities {get{return correspondenceEdge!=null&&cycleSlotScroll.ClientSize.Width-CycleIdentityLeft>=U(280);}}
 int CycleIdentityHeight {get{return InlineCycleIdentities?0:Font.Height*2+U(8);}}
 string EntryIdentityCaption(string role){
  var piece=Map(Get(correspondenceEdge,role));var actual=Map(Get(correspondenceEdge,"actual_"+role));
  string title=role=="source"?"Source entry":"Destination entry occupant";
  if(piece==null)return title+" · unavailable";
  string boundary=cycleMode=="After"?"forecast":ValueText(Get(cycleProjection,"source_boundary"))=="prepare"?"after Prepare":"actual";
  return title+" ("+boundary+") · "+Named(piece,"identity_short","I"+Id(Get(piece,"piece")))+(actual==null?" · actual unavailable":Id(Get(piece,"piece"))==Id(Get(actual,"piece"))?" · same identity as actual":" · different from actual");
 }
 string EntryIdentityDetails(string role){
  var piece=Map(Get(correspondenceEdge,role));var actual=Map(Get(correspondenceEdge,"actual_"+role));
  return EntryIdentityCaption(role)+(piece==null?"":"\r\nEntry identity I"+Id(Get(piece,"piece"))+" at fixed position P"+Id(Get(piece,"position"))+" · "+NameDetails(piece))+(actual==null?"\r\nActual occupant unavailable":"\r\nActual identity I"+Id(Get(actual,"piece"))+" · "+Named(actual,"identity_short","I"+Id(Get(actual,"piece"))));
 }
 string SlotPairText(Dictionary<string,object> pair){int s=Id(Get(pair,"source_slot")),d=Id(Get(pair,"destination_slot")),label=Id(Get(pair,"label"));return "S"+s+" → S"+d+" · "+SlotCaption(s)+" → "+SlotCaption(d)+(label>=0?" · sticker L"+label:" · entry label unavailable");}
 void LayoutCycleSlots(){int n=Items(Get(correspondenceEdge,"slots")).Count;cycleSlotScroll.Height=U(102)+CycleIdentityHeight;cycleSlotGraphic.SetBounds(0,0,Math.Max(cycleSlotScroll.ClientSize.Width,U(78+46*n)),U(82)+CycleIdentityHeight);cycleSlotScroll.AutoScrollMinSize=new Size(U(78+46*n),U(82)+CycleIdentityHeight);}
 Color SlotCellColor(int slot){return geometry!=null&&slot>=0&&slot<259800?geometry.CellColor(slot/433):muted;}
 static int FrameOrdinal(IList frame,int slot){for(int i=0;i<frame.Count;i++)if(Id(frame[i])==slot)return i+1;return -1;}
 void PaintCycleCorrespondence(Graphics g){
  g.Clear(surface);if(correspondenceEdge==null)return;g.SmoothingMode=SmoothingMode.AntiAlias;
  int row=0;foreach(string role in new[]{"source","destination"}){int x=InlineCycleIdentities?CycleIdentityLeft:U(2),y=InlineCycleIdentities?U(row==0?24:57):row*(Font.Height+U(2));TextRenderer.DrawText(g,EntryIdentityCaption(role),Font,new Rectangle(x,y,Math.Max(1,cycleSlotScroll.ClientSize.Width-x-U(6)),Font.Height),ink,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);row++;}
  var saved=g.Save();g.TranslateTransform(0,CycleIdentityHeight);
  var slots=Items(Get(correspondenceEdge,"slots"));var reference=Map(Get(correspondenceEdge,"reference"));string status=ValueText(Get(reference,"status"));bool known=status=="Matched"||status=="Mismatch";
  TextRenderer.DrawText(g,known?"Ref slots":"Slot pairs",Font,new Rectangle(U(2),0,U(66),Font.Height),ink,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
  TextRenderer.DrawText(g,"From",Font,new Rectangle(U(2),U(24),U(58),Font.Height),muted,TextFormatFlags.NoPadding);
  TextRenderer.DrawText(g,"To",Font,new Rectangle(U(2),U(57),U(58),Font.Height),muted,TextFormatFlags.NoPadding);
  for(int i=0;i<slots.Count;i++){
   var pair=Map(slots[i]);int s=Id(Get(pair,"source_slot")),d=Id(Get(pair,"destination_slot")),label=Id(Get(pair,"label"));int x=U(68+46*i),center=x+U(17);
   int first=FrameOrdinal(Items(Get(reference,"source_frame")),s),last=FrameOrdinal(Items(Get(reference,"destination_frame")),d);
   string caption=known&&first>0&&last>0?first+"→"+last:(i+1).ToString();
   TextRenderer.DrawText(g,caption,Font,new Rectangle(x,0,U(42),Font.Height),ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
   using(var brush=new SolidBrush(SlotCellColor(s)))g.FillRectangle(brush,x+U(7),U(24),U(22),U(9));
   using(var brush=new SolidBrush(SlotCellColor(d)))g.FillRectangle(brush,x+U(7),U(66),U(22),U(9));
   using(var pen=new Pen(known&&first!=last?amber:blue,1.3f))using(var arrow=new AdjustableArrowCap(3,4)){pen.CustomEndCap=arrow;g.DrawLine(pen,center,U(36),center,U(62));}
   if(label>=0)using(var brush=new SolidBrush(SlotCellColor(label)))g.FillEllipse(brush,center-U(3),U(44),U(6),U(6));
  }
  g.Restore(saved);
 }
}
