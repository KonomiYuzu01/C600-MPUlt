// The physical keyboard is a view of the same effective input map in G1 and G2.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 readonly List<Control> keyboardRows=new List<Control>();
 readonly Dictionary<Button,string> physicalKeyboardKeys=new Dictionary<Button,string>();
 readonly HashSet<string> standardKeyboardCodes=new HashSet<string>(StringComparer.Ordinal);
 readonly List<TableLayoutPanel> additionalKeyboardRows=new List<TableLayoutPanel>();
 Button additionalKeyboardHeading;
 bool keyboardExtrasExpanded,displayedInverseShift;
 string indicatedKeyboardCode,hoveredKeyboardCode;
 internal bool KeyboardExtrasVisible {get{return keyboardExtrasExpanded;}}
 string additionalKeyboardSignature;
 Label keyboardActionDetail;
 string keyboardLayoutTrace;
 int keyboardFitCalls;
 bool fittingKeyboardRows,keyboardLayoutPending;
 int keyboardBatchDepth;
 bool keyboardRefreshPending;
 void BeginKeyboardBatch(){keyboardBatchDepth++;}
 void EndKeyboardBatch(){
  if(--keyboardBatchDepth!=0)return;
  bool refresh=keyboardRefreshPending;keyboardRefreshPending=false;
  if(refresh&&!closing&&keyboardWindow!=null&&!keyboardWindow.IsDisposed)RenderPhysicalKeyboard();
 }
 static readonly Color KeyBackground=Color.FromArgb(17,22,28),KeySurface=Color.FromArgb(25,33,41),KeyRaised=Color.FromArgb(34,45,56),KeyInk=Color.FromArgb(223,230,234),KeyMuted=Color.FromArgb(147,164,173),KeyRule=Color.FromArgb(53,66,76),KeySelection=Color.FromArgb(108,187,212),KeyForecast=Color.FromArgb(228,182,106),KeyError=Color.FromArgb(238,141,135);
 class KeyboardKeyButton:Button {
  bool hovered,pointerPressed;
  internal bool PhysicalPressed,RejectedPress;
  internal bool Pressed { get { return PhysicalPressed||pointerPressed; } }
  protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left&&Enabled){pointerPressed=true;Invalidate();Update();}base.OnMouseDown(e);}
  protected override void OnMouseUp(MouseEventArgs e){pointerPressed=false;Invalidate();base.OnMouseUp(e);}
  protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture){pointerPressed=false;Invalidate();}base.OnMouseCaptureChanged(e);}
  protected override void OnMouseEnter(EventArgs e){hovered=true;base.OnMouseEnter(e);Invalidate();}
  protected override void OnMouseLeave(EventArgs e){hovered=false;base.OnMouseLeave(e);Invalidate();}
  protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
  protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
  protected override void OnPaint(PaintEventArgs e){
   Color surface=RejectedPress?Color.FromArgb(72,38,39):Pressed?Color.FromArgb(46,75,88):hovered&&Enabled?KeyRaised:BackColor;e.Graphics.Clear(surface);if(FlatAppearance.BorderSize>0)using(var pen=new Pen(FlatAppearance.BorderColor))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);
   var lines=Text.Split('\n').Where(line=>line.Length>0).ToArray();int lineHeight=Math.Max(Font.Height,TextRenderer.MeasureText("Ag",Font,Size.Empty,TextFormatFlags.NoPadding).Height),count=lines.Length,y=Math.Max(2,(Height-count*lineHeight)/2);for(int i=0;i<count;i++){Color color=!Enabled?KeyMuted:i==0?ForeColor:KeySelection;TextRenderer.DrawText(e.Graphics,lines[i],Font,new Rectangle(5,y,Width-10,lineHeight),color,TextFormatFlags.HorizontalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);y+=lineHeight;}
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-8,Height-8),ForeColor,surface);
   if(Pressed)using(var brush=new SolidBrush(RejectedPress?KeyError:KeySelection))e.Graphics.FillRectangle(brush,8,Height-5,Math.Max(0,Width-16),2);
   if(RejectedPress)using(var pen=new Pen(KeyError,2)){e.Graphics.DrawLine(pen,Width-12,7,Width-6,13);e.Graphics.DrawLine(pen,Width-6,7,Width-12,13);}
  }
 }
 sealed class GripKeyButton:KeyboardKeyButton {
  internal Color CapColor=Color.Transparent;
  internal bool SelectedGrip;
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(CapColor.A!=0)using(var brush=new SolidBrush(CapColor))e.Graphics.FillRectangle(brush,3,3,3,Math.Max(0,Height-6));if(SelectedGrip)using(var pen=new Pen(KeySelection,2))e.Graphics.DrawRectangle(pen,1,1,Width-3,Height-3);RoundButtonEdges(this,e.Graphics);}
 }
 void FitKeyboardRows(){
  if(fittingKeyboardRows){keyboardLayoutPending=true;return;}
  // A scrollbar resize must not let an outer pass overwrite a newer width.
  fittingKeyboardRows=true;int passes=0;
  try{do{if(++passes>8)throw new InvalidOperationException("Keyboard layout did not settle: client="+keyboard.ClientSize+", minimum="+keyboard.AutoScrollMinSize);keyboardLayoutPending=false;FitKeyboardRowsCore();}while(keyboardLayoutPending);}
  finally{fittingKeyboardRows=false;}
 }
 void FitKeyboardRowsCore(){
  keyboardFitCalls++;TraceKeyboardLayout("fit-before");
  int width=Math.Max(800,keyboard.ClientSize.Width-keyboard.Padding.Horizontal);foreach(Control row in keyboardRows)if(!row.IsDisposed){row.Width=width;var keys=row.Controls.OfType<GripKeyButton>().ToArray();if(keys.Length>0){var table=(TableLayoutPanel)row;float fixedWidth=table.ColumnStyles.Cast<ColumnStyle>().Where(style=>style.SizeType==SizeType.Absolute).Sum(style=>style.Width),percent=table.ColumnStyles.Cast<ColumnStyle>().Where(style=>style.SizeType==SizeType.Percent).Sum(style=>style.Width);foreach(var key in keys)if(key.Tag is string){var style=table.ColumnStyles[table.GetColumn(key)];int columnWidth=style.SizeType==SizeType.Absolute?(int)style.Width:(int)Math.Floor((width-fixedWidth)*style.Width/percent);key.Text=WrapKeyText((string)key.Tag,key.Font,columnWidth-14);}int line=Math.Max(row.Font.Height,TextRenderer.MeasureText("Ag",row.Font,Size.Empty,TextFormatFlags.NoPadding).Height),count=keys.Max(key=>key.Text.Split('\n').Length);row.Height=Math.Max(48,count*line+8);row.PerformLayout();}}
  if(keyboardActionDetail!=null)keyboardActionDetail.Height=Math.Max(25,keyboardActionDetail.GetPreferredSize(new Size(width,0)).Height);
  // TopDown/no-wrap content has a fixed outer height. Bounds can still reflect the prior scroll origin during resize/layout.
  if(keyboard.Visible){int height=keyboard.Padding.Vertical+keyboardRows.Where(row=>!row.IsDisposed&&row.Visible).Sum(row=>row.Height+row.Margin.Vertical);var extent=new Size(0,height);if(keyboard.AutoScrollMinSize!=extent)keyboard.AutoScrollMinSize=extent;}
  TraceKeyboardLayout("fit-after");
  if(width!=Math.Max(800,keyboard.ClientSize.Width-keyboard.Padding.Horizontal))keyboardLayoutPending=true;
 }
 void TraceKeyboardLayout(string reason){
  if(keyboard.IsDisposed)return;var rows=keyboardRows.Where(row=>!row.IsDisposed).ToArray();var visible=rows.Where(row=>row.Visible).ToArray();var last=visible.LastOrDefault();
  string state="visible="+keyboard.Visible+" owner="+(keyboardWindow!=null&&keyboardWindow.Visible)+" parent="+(keyboard.Parent!=null&&keyboard.Parent.Visible)+" rows="+rows.Length+" shown="+visible.Length+" total="+(keyboard.Padding.Vertical+rows.Sum(row=>row.Height+row.Margin.Vertical))+" desired="+(keyboard.Padding.Vertical+visible.Sum(row=>row.Height+row.Margin.Vertical))+" minimum="+keyboard.AutoScrollMinSize.Height+" maximum="+keyboard.VerticalScroll.Maximum+" client="+keyboard.ClientSize+" scroll="+keyboard.AutoScrollPosition+" last="+(last==null?"none":last.Top+":"+last.Height+":"+last.Margin.Vertical)+" flow="+keyboard.FlowDirection+" wrap="+keyboard.WrapContents;
  if(state==keyboardLayoutTrace)return;keyboardLayoutTrace=state;NativeDiagnostics.Write("Keyboard layout "+reason+" fitCalls="+keyboardFitCalls+" "+state);
 }
 TableLayoutPanel KeyboardRow(int columns,int height){var row=new TableLayoutPanel{Height=height,ColumnCount=columns,RowCount=1,BackColor=KeyBackground,Margin=Padding.Empty,Padding=Padding.Empty};row.RowStyles.Add(new RowStyle(SizeType.Percent,100));for(int i=0;i<columns;i++)row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/columns));keyboardRows.Add(row);keyboard.Controls.Add(row);return row;}
 void KeyboardCell(TableLayoutPanel row,Control control,int column){control.Dock=DockStyle.Fill;control.Margin=new Padding(2);control.MinimumSize=new Size(44,44);row.Controls.Add(control,column,0);}
 void RenderKeyboard(){RenderPhysicalKeyboard();}
 void RenderGripFeedback(){if(input==null)return;tips.SetToolTip(inputFeedback,input.HeldStatus);RenderPhysicalKeyboard();}
 void OpenFloatingKeyboard(bool activate){
  if(keyboardWindow==null||keyboardWindow.IsDisposed){
   var content=new Panel{Dock=DockStyle.Fill,BackColor=KeyBackground};keyboard.Dock=DockStyle.Fill;keyboard.FlowDirection=FlowDirection.TopDown;keyboard.WrapContents=false;keyboard.AutoScroll=true;keyboard.Padding=new Padding(8,4,8,4);keyboardContext.Dock=DockStyle.Top;keyboardContext.Height=46;keyboardContext.Padding=new Padding(8,4,8,4);keyboardContext.ForeColor=KeyInk;content.Controls.Add(keyboard);content.Controls.Add(keyboardContext);
   keyboardWindow=EnsureWorkWindow("keyboard",content,new Size(864,451));keyboardWindow.MinimumSize=new Size(880,460);keyboard.SizeChanged+=delegate{TraceKeyboardLayout("size-changed");FitKeyboardRows();};keyboard.VisibleChanged+=delegate{TraceKeyboardLayout("visible-changed");};keyboard.Layout+=delegate(object sender,LayoutEventArgs e){TraceKeyboardLayout("layout:"+e.AffectedProperty);};
  }
  RestoreMinimizedWindow(keyboardWindow);if(!keyboardWindow.Visible){ClampWindowToScreen(keyboardWindow);keyboardWindow.Show();}RenderKeyboard();if(activate){keyboardWindow.Activate();var key=physicalKeyboardKeys.Keys.FirstOrDefault(b=>b.Enabled);if(key!=null)key.Focus();}UpdateWindowReadouts();
 }
 internal static string CompactCommand(string id){
  if(id.StartsWith("review-reason-",StringComparison.Ordinal))return "Reason\n"+id.Substring(14);
  switch(id){
   case "keymap-import":return "Import\nkeys";case "keymap-export":return "Export\nkeys";
   case "session-import-log":return "Open\nlog";case "session-export-log":return "Export\nlog";
   case "solve-macros":return "Solve\nmacros";case "solve-prepare":return "Prep.";case "solve-protection":return "Rules";case "macro-check-library":return "Check\nlibrary";
   case "solve-locate-a":return "Find A";case "solve-locate-b":return "Find B";case "solve-locate-target":return "Find\ntarget";
   case "cycles-current":return "Current";case "cycles-operation":return "Operation";case "cycles-after":return "After";
   case "cycles-macro":return "Macro\ncycles";case "cycles-steps":return "Step\ncycles";case "cycles-complete":return "Full\ncycles";case "cycles-refresh":return "Check";case "cycles-next":return "Next\ncycle";case "cycles-previous":return "Prior\ncycle";case "cycles-more":return "More\nedges";case "cycles-back":return "Prior\nedges";case "functions-toggle":return "Tools";case "bank-Cycles":return "Cycle set";case "bank-Functions":return "Tools set";case "keyboard-extra":return "More";case "help":return "Help";case "index":return "Index";case "bank":return "Sets";case "macro-search":return "Macro";case "operation-focus":return "Steps";case "filter":return "Filter";case "keyboard":return "Keys";case "bank-previous":return "Back";
   case "orbit":return "Orbit";case "focus":return "Pick\npiece";case "set-current":return "Make\nCurr.";case "assign-target":return "Target";case "assign-a":return "Buffer A";case "assign-b":return "Buffer B";case "roles":return "Roles";
   case "next-pin":return "Lock Next";case "next-activate":return "Use Next";case "next-clear":return "Clear\nNext";case "next-locate":return "Find Next";case "next-menu":return "Next…";case "copy-selection":return "Copy name";case "copy-selection-canonical":return "Copy\npiece ID";
   case "block-add":return "Add Home";case "block-capture":return "Keep\nplace";case "block-remove":return "Drop\npiece";case "block-protect":return "Keep\nblock";case "block-unprotect":return "Free\nblock";case "protection":return "Rules";case "block-reference":return "Move goal";
   case "workspace-fullscreen":return "Full\nscreen";case "bank-next":return "Next set";case "bank-prev":return "Back set";case "solve-actions":return "Actions";case "views":return "Views";case "views-close":return "Close\nviews";case "windows-hide":return "Hide\ntools";
   case "local":return "Local";case "global":return "Global";case "puzzle":return "Puzzle";case "local-center":return "Cell";case "local-center-current":return "Curr.\ncenter";case "local-center-grip":return "Grip\ncenter";
   case "local-reset":return "Reset\nLocal";case "global-reset":return "Reset\nGlobal";case "local-left":return "Local ←";case "local-right":return "Local →";case "local-up":return "Local ↑";case "local-down":return "Local ↓";case "global-left":return "Global ←";case "global-right":return "Global →";case "global-up":return "Global ↑";case "global-down":return "Global ↓";
   case "local-zoom-in":return "Local +";case "local-zoom-out":return "Local −";case "global-zoom-in":return "Global +";case "global-zoom-out":return "Global −";case "compare-preview":return "Stage\nview";
   case "inspect-actual":return "Actual";case "inspect-prepare":return "After\nPrep.";case "inspect-macro":return "After\nMacro";case "inspect-cleanup":return "After\nClean";
   case "prefix-on":return "Each\nturn";case "prefix-off":return "Final\nresult";
   case "checkpoint":return "Save\npoint";case "restore":return "Load";case "undo":return "Undo";case "redo":return "Redo";case "reset":return "Reset";case "worksheet":return "Work\nsheets";case "worksheet-save":return "Save\nsheet";case "worksheet-use":return "Use sheet";case "operation-new":return "New steps";case "operation-reuse":return "Reuse\nsteps";
   case "macro-compare":return "Check\npair";case "macro-inverse":return "Invert\nmacro";case "macro-filter":return "Filter\nmacros";case "macro-label":return "Edit\nmacro";case "macro-pin":return "Pin\nmacro";case "macro-export":return "Export\nmacro";case "macro-import":return "Import\nmacros";
   case "macro-new":return "New macro";case "macro-details":return "Macro\ndetails";case "macro-insert":return "Add macro";case "macro-replace":return "Set\nphase";case "macro-close":return "Close\nmacro";case "reference":return "Ref.";case "reference-transform":return "Save R";case "effect-body":return "Macro\neffect";case "effect-complete":return "Full\neffect";case "bank-macro-1":return "M1";case "bank-macro-2":return "M2";
   case "phase-input":return "Edit\nsteps";case "edit-prepare":return "Edit\nPrep.";case "edit-macro":return "Edit\nMacro";case "edit-cleanup":return "Edit\nClean";case "phase-prepare":return "Prep.";case "phase-macro":return "Macro";case "phase-cleanup":return "Clean";case "cleanup-inverse":return "Invert\nprep";
   case "review":return "Check";case "review-details":return "Result";case "review-locate":return "Find";case "preview":return "Stage";case "commit":return "Run";case "cancel-preview":return "Cancel";case "cancel-analysis":return "Stop\ncheck";case "operation-hide":return "Close\nsteps";case "input-draft":return "To draft";case "input-live":return "To puzzle";
   case "goal-prepare":return "Goal:\nprep";case "goal-insert":return "Goal:\ninsert";case "goal-place":return "Place\npiece";case "goal-orient":return "Orient\npiece";case "goal-finish-buffer":return "Finish\nbuffer";case "goal-block":return "Goal:\nbuild";case "goal-endgame":return "Goal:\nfinish";case "target-capture":return "Capture\ngoal";case "target-home":return "Home\ngoal";
   case "key-edit":return "Edit\nkey";case "key-edit-advanced":return "Key\nrecord";case "capture":return "Set grips";case "capture-piece":return "Piece\ngrips";case "grip-hold":return "Hold";case "grip-latch":return "Latch";case "grip-frame":return "Set frame";
   case "bank-A":return "A set";case "bank-B":return "B set";case "bank-I":return "Insert\nset";case "bank-M":return "Macro set";case "bank-E":return "Finish\nset";case "bank-Workspace":return "Work set";case "bank-Views":return "View set";case "bank-Filter":return "Filter set";case "bank-Session":return "Save set";case "bank-Macro":return "Macro\nkeys";case "bank-Operation":return "Steps set";case "bank-Keyboard":return "Keys set";case "fixture-e1":return "E1\nsample";
   default:return "Action";
  }
 }
 void ShowKeyMeaning(string code){indicatedKeyboardCode=code;if(keyboardActionDetail==null)return;var state=InputState();string effective=EffectiveKeyboardCode(code,state);string command,axis;int cap;string meaning;if(state.CommandKeys.TryGetValue(effective,out command))meaning=hints.ContainsKey(command)?hints[command]:command;else if(state.GripKeys.TryGetValue(code,out cap))meaning=(input.InverseShiftHeld?"Release Shift to select Grip ":"Grip ")+CellName(cap)+" · "+FrameCaption(cap);else if(state.TwistKeys.TryGetValue(code,out axis)){bool suffix=axis.EndsWith("-",StringComparison.Ordinal),inverse=suffix^input.InverseShiftHeld;string selectedAxis=suffix?axis.Substring(0,axis.Length-1):axis;var notation=AxisNotation(selectedAxis);var vertices=Items(Value(notation,inverse?"inverse_permutation":"permutation"));string correspondence=vertices.Length==4?String.Join(" ",vertices.Select((vertex,index)=>((char)('a'+index)).ToString()+"→"+(char)('a'+Number(vertex))).ToArray()):"Frame correspondence unavailable";meaning="Twist "+TwistCaption(selectedAxis,inverse)+" · "+correspondence+" · "+state.Destination+" / "+state.Phase;}else meaning="No operation in this set";keyboardActionDetail.Text=ShortCode(code)+"  →  "+meaning;keyboardActionDetail.AccessibleDescription=meaning;tips.SetToolTip(keyboardActionDetail,meaning);}
 Dictionary<string,object> AxisNotation(string axis){return Items(Value(work,"frame_notation")).Select(Map).FirstOrDefault(a=>Text(Value(a,"axis"))==axis);}
 void ShowKeyDirection(string code){
  var state=InputState();string axis;local.SetCornerMapping(null,null);if(state.CommandKeys.ContainsKey(EffectiveKeyboardCode(code,state))||!state.TwistKeys.TryGetValue(code,out axis)||!input.ActiveGripCell.HasValue)return;int cap=input.ActiveGripCell.Value;if(cap!=local.CenterCell||lastLocalCell==null||Text(Value(lastLocalCell,"state_hash"))!=Text(Value(work,"hash")))return;
  var frame=Items(Value(Map(Value(Bank,"frames")),cap.ToString()));if(!frame.Select(Number).SequenceEqual(Items(Value(lastLocalCell,"frame_vertices")).Select(Number)))return;bool suffix=axis.EndsWith("-",StringComparison.Ordinal),reverse=suffix^input.InverseShiftHeld;string action=suffix?axis.Substring(0,axis.Length-1):axis;var notation=AxisNotation(action);if(notation==null)return;local.SetCornerMapping(Items(Value(notation,reverse?"inverse_permutation":"permutation")).Select(Number).ToArray(),ShortCode(code)+" · "+TwistCaption(action,reverse)+" · input meaning, not an executed move");
 }
 string TwistCaption(string axis,bool inverse){var record=AxisNotation(axis);return record==null?axis+(inverse?"⁻¹":""):Text(Value(record,inverse?"inverse":"forward"));}
 void RenderPhysicalKeyboard(){
  if(input==null||work==null||keyboardWindow==null||keyboardWindow.IsDisposed)return;
  if(keyboardBatchDepth>0){keyboardRefreshPending=true;return;}
  long timingStart=sendTimingEnabled?System.Diagnostics.Stopwatch.GetTimestamp():0;
  displayedInverseShift=input.InverseShiftHeld;
  if(physicalKeyboardKeys.Count==0){
   keyboard.SuspendLayout();
   string[][] rows={new[]{"Escape","F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12"},new[]{"Backquote","Digit1","Digit2","Digit3","Digit4","Digit5","Digit6","Digit7","Digit8","Digit9","Digit0","Minus","Equal"},new[]{"Tab","KeyQ","KeyW","KeyE","KeyR","KeyT","KeyY","KeyU","KeyI","KeyO","KeyP","BracketLeft","BracketRight","Backslash"},new[]{"KeyA","KeyS","KeyD","KeyF","KeyG","KeyH","KeyJ","KeyK","KeyL","Semicolon","Quote","Enter"},new[]{"KeyZ","KeyX","KeyC","KeyV","KeyB","KeyN","KeyM","Comma","Period","Slash","Backspace","Space"}};
   foreach(var rowKeys in rows){var row=KeyboardRow(rowKeys.Length,62);if(rowKeys.Contains("Backslash"))for(int column=0;column<rowKeys.Length;column++)row.ColumnStyles[column]=new ColumnStyle(column==0||column>=11?SizeType.Absolute:SizeType.Percent,column==0?52:column==11||column==12?50:column==13?56:10);for(int i=0;i<rowKeys.Length;i++){standardKeyboardCodes.Add(rowKeys[i]);KeyboardCell(row,CreatePhysicalKey(rowKeys[i]),i);}}
   keyboardActionDetail=new Label{Height=25,ForeColor=KeyInk,Padding=new Padding(5,3,5,2),AutoEllipsis=false,AccessibleName="Full meaning of the indicated key"};keyboardRows.Add(keyboardActionDetail);keyboard.Controls.Add(keyboardActionDetail);keyboard.ResumeLayout(true);
  }
  var state=InputState();RefreshAdditionalKeys(state);long timingKeys=sendTimingEnabled?System.Diagnostics.Stopwatch.GetTimestamp():0;foreach(var pair in physicalKeyboardKeys){var button=pair.Key;string code=pair.Value,effective=EffectiveKeyboardCode(code,state),command,axis;int cap;string detail,caption;bool commandKey=state.CommandKeys.TryGetValue(effective,out command);bool grip=!commandKey&&state.GripKeys.TryGetValue(code,out cap);bool twist=!commandKey&&!grip&&state.TwistKeys.TryGetValue(code,out axis);state.GripKeys.TryGetValue(code,out cap);state.TwistKeys.TryGetValue(code,out axis);
   if(commandKey){caption=CompactCommand(command);detail=hints.ContainsKey(command)?hints[command]:command;string reason=ToolCommandUnavailable(command);button.Enabled=reason==null&&connected&&(!OperationInputBlocked||new[]{"cancel-analysis","index","bank","local","global","keyboard","macro-search","operation-focus","solve-macros","solve-prepare","solve-protection","help","keyboard-extra"}.Contains(command));if(reason!=null)detail+="\n"+reason;}
   else if(grip){caption=input.InverseShiftHeld?"Release\nShift":"Grip";detail="Grip "+CellName(cap)+" · "+FrameCaption(cap)+"\nCanonical cap C"+cap+". Click to latch this onscreen grip. Click again to release.";button.Enabled=connected&&!OperationInputBlocked&&!input.InverseShiftHeld;}
   else if(twist){bool suffix=axis.EndsWith("-",StringComparison.Ordinal),reverse=suffix^input.InverseShiftHeld;string baseAxis=suffix?axis.Substring(0,axis.Length-1):axis;caption=TwistCaption(baseAxis,reverse);detail="Twist "+caption+" · "+baseAxis+(reverse?" inverse":" forward")+"\n"+(input.ActiveGripCell.HasValue?"Cap C"+input.ActiveGripCell.Value+" · "+FrameCaption(input.ActiveGripCell.Value):"Choose a Grip first.")+"\nTurns → "+state.Destination+" / "+state.Phase;button.Enabled=input.ActiveGripCell.HasValue&&connected&&!OperationInputBlocked;}
   else{caption=code=="Escape"?"Close":code=="Tab"?"Next":code=="Enter"?"Accept":code=="Space"?"Accept":"—";detail="No operation bound in "+state.BankId+". "+caption;button.Enabled=false;}
   ((GripKeyButton)button).CapColor=grip&&cap>=1&&cap<=bridge.Palette.Length?bridge.Palette[cap-1]:Color.Transparent;((GripKeyButton)button).SelectedGrip=grip&&input.ActiveGripCell==cap;button.Tag=ShortCode(code)+"\n"+caption;button.Text=WrapKeyText(Text(button.Tag),button.Font,Math.Max(44,button.Width)-10);button.AccessibleName=ShortCode(code)+" · "+detail;button.BackColor=grip&&input.ActiveGripCell==cap?KeyRaised:KeySurface;button.FlatAppearance.BorderColor=grip&&input.ActiveGripCell==cap?KeySelection:KeyRule;button.FlatAppearance.BorderSize=grip&&input.ActiveGripCell==cap?2:1;tips.SetToolTip(button,detail+"\nPhysical position: "+ExperimentInput.PhysicalLabel(code));
  }
  long timingFeedback=sendTimingEnabled?System.Diagnostics.Stopwatch.GetTimestamp():0;DrawInputPressFeedback();RenderPhysicalFeedback(state);RefreshIndicatedKey();long timingFit=sendTimingEnabled?System.Diagnostics.Stopwatch.GetTimestamp():0;FitKeyboardRows();
  // First live row insertion can set the minimum inside SizeChanged, before the outer layout publishes its old range.
  // Reconcile only that measured mismatch after both row batches have completed; never pad the content or queue layouts.
  if(keyboard.Visible&&keyboard.VerticalScroll.Visible&&keyboard.VerticalScroll.Maximum+1<keyboard.AutoScrollMinSize.Height){keyboard.PerformLayout();TraceKeyboardLayout("range-reconciled");}
  if(sendTimingEnabled)try{long end=System.Diagnostics.Stopwatch.GetTimestamp();NativeDiagnostics.Write("Experiment keyboard timing: "+api.Json(LocalApi.D("keys",physicalKeyboardKeys.Count,"prepare_ms",SendTimingMs(timingStart,timingKeys),"keys_ms",SendTimingMs(timingKeys,timingFeedback),"feedback_ms",SendTimingMs(timingFeedback,timingFit),"fit_ms",SendTimingMs(timingFit,end),"total_ms",SendTimingMs(timingStart,end))));}catch { }
 }
 void DrawInputPressFeedback(){
  if(input==null)return;
  if(displayedInverseShift!=input.InverseShiftHeld){
   displayedInverseShift=input.InverseShiftHeld;
   if(keyboardBatchDepth>0)keyboardRefreshPending=true;
   else{RenderPhysicalKeyboard();return;}
  }
  foreach(var pair in physicalKeyboardKeys){var key=(GripKeyButton)pair.Key;if(key.IsDisposed)continue;bool pressed=input.IsVisuallyPressed(pair.Value),rejected=input.IsVisuallyRejected(pair.Value);if(key.PhysicalPressed==pressed&&key.RejectedPress==rejected)continue;key.PhysicalPressed=pressed;key.RejectedPress=rejected;key.AccessibleDescription=rejected?"Rejected input; read the input message.":pressed?"Key is physically down. This is not proof that an operation completed.":"Key released.";key.Invalidate();if(key.Visible&&key.IsHandleCreated)key.Update();}
 }
 GripKeyButton CreatePhysicalKey(string code){
  var button=new GripKeyButton{BackColor=KeySurface,ForeColor=KeyInk,FlatStyle=FlatStyle.Flat,Padding=new Padding(3)};button.FlatAppearance.BorderColor=KeyRule;physicalKeyboardKeys.Add(button,code);
  button.MouseEnter+=delegate{hoveredKeyboardCode=code;ShowKeyMeaning(code);ShowKeyDirection(code);};button.MouseLeave+=delegate{hoveredKeyboardCode=null;local.SetCornerMapping(null,null);};button.GotFocus+=delegate{ShowKeyMeaning(code);ShowKeyDirection(code);};button.LostFocus+=delegate{local.SetCornerMapping(null,null);};button.Click+=delegate{ActivateOnscreenKey(code);};
  var menu=new ContextMenuStrip();menu.Opening+=delegate{menu.Items.Clear();var keys=InputState();string command;if(keys.CommandKeys.TryGetValue(code,out command)){menu.Items.Add("Change "+(hints.ContainsKey(command)?hints[command]:command)+" key…",null,delegate{ShowBindingEditor(command);});}else menu.Items.Add("Change command key…",null,delegate{ShowBindingEditor("macro-insert");});};button.ContextMenuStrip=menu;button.Disposed+=delegate{menu.Dispose();};return button;
 }
 internal static string[] AdditionalKeyboardCodes(ExperimentInputState state,IEnumerable<string> standardCodes){
  var standard=new HashSet<string>(standardCodes,StringComparer.Ordinal);return state.GripKeys.Keys.Concat(state.TwistKeys.Keys).Concat(state.CommandKeys.Keys).Where(code=>!standard.Contains(code)).Distinct(StringComparer.Ordinal).OrderBy(code=>code.Contains("+")?1:0).ThenBy(code=>code,StringComparer.Ordinal).ToArray();
 }
 void ToggleKeyboardExtras(){
  if(Text(Value(Workspace,"bank"))=="Functions"){Say("Functions shortcuts are always shown below the main keyboard.",false);return;}
  keyboardExtrasExpanded=!keyboardExtrasExpanded;if(keyboardWindow==null||keyboardWindow.IsDisposed){OpenFloatingKeyboard(true);return;}RefreshAdditionalVisibility();RenderPhysicalKeyboard();
 }
 void RefreshAdditionalVisibility(){
  bool functions=Text(Value(Workspace,"bank"))=="Functions",expanded=functions||keyboardExtrasExpanded;
  bool moveFocus=additionalKeyboardRows.Any(row=>Object.Equals(row.Tag,"chords")&&row.ContainsFocus)&&!expanded;
  foreach(var row in additionalKeyboardRows)row.Visible=!Object.Equals(row.Tag,"chords")||expanded;
  if(additionalKeyboardHeading!=null){int count=additionalKeyboardRows.Where(row=>Object.Equals(row.Tag,"chords")).Sum(row=>row.Controls.Count);additionalKeyboardHeading.Text=functions?"Functions shortcuts ("+count+")":(expanded?"▾":"▸")+" Extra keys ("+count+")"+KeyHint("keyboard-extra");additionalKeyboardHeading.AccessibleName=functions?"Functions combinations are always displayed":"Show or hide additional modifier shortcuts";additionalKeyboardHeading.Enabled=!functions;additionalKeyboardHeading.Visible=count>0;if(moveFocus)additionalKeyboardHeading.Focus();}
 }
 void RefreshAdditionalKeys(ExperimentInputState state){
  string[] codes=AdditionalKeyboardCodes(state,standardKeyboardCodes);string signature=String.Join("|",codes);if(signature==additionalKeyboardSignature){RefreshAdditionalVisibility();return;}additionalKeyboardSignature=signature;keyboard.SuspendLayout();
  try{
   foreach(var row in additionalKeyboardRows){foreach(Button key in row.Controls.OfType<Button>())physicalKeyboardKeys.Remove(key);keyboardRows.Remove(row);keyboard.Controls.Remove(row);row.Dispose();}additionalKeyboardRows.Clear();
   if(additionalKeyboardHeading==null){additionalKeyboardHeading=new Button{Height=28,ForeColor=KeyInk,BackColor=KeySurface,FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(5,0,5,0),Margin=Padding.Empty,AccessibleName="Show or hide additional modifier shortcuts"};additionalKeyboardHeading.FlatAppearance.BorderSize=0;additionalKeyboardHeading.Click+=delegate{ToggleKeyboardExtras();};keyboardRows.Add(additionalKeyboardHeading);keyboard.Controls.Add(additionalKeyboardHeading);}
   foreach(bool chords in new[]{false,true}){var group=codes.Where(code=>code.Contains("+")==chords).ToArray();for(int first=0;first<group.Length;first+=7){var row=KeyboardRow(7,48);row.Tag=chords?"chords":"physical";additionalKeyboardRows.Add(row);for(int column=0;column<7&&first+column<group.Length;column++)KeyboardCell(row,CreatePhysicalKey(group[first+column]),column);}}
   RefreshAdditionalVisibility();
  }finally{keyboard.ResumeLayout(true);}
 }
 void RefreshIndicatedKey(){
  var focused=physicalKeyboardKeys.FirstOrDefault(pair=>pair.Key.Focused&&pair.Key.Visible);
  string code=focused.Key!=null?focused.Value:hoveredKeyboardCode??indicatedKeyboardCode;
  if(code==null)return;ShowKeyMeaning(code);if(focused.Key!=null||hoveredKeyboardCode!=null)ShowKeyDirection(code);else local.SetCornerMapping(null,null);
 }
 internal static string WrapKeyText(string text,Font font,int width){
  var lines=new List<string>();foreach(string line in text.Split('\n')){
   string source=line;if(TextRenderer.MeasureText(source,font,Size.Empty,TextFormatFlags.NoPadding).Width>width&&source.Contains(")("))source=source.Replace(")(",") (");
   string pending="";foreach(string word in source.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries)){string next=pending.Length==0?word:pending+" "+word;if(pending.Length>0&&TextRenderer.MeasureText(next,font,Size.Empty,TextFormatFlags.NoPadding).Width>width){lines.Add(pending);pending=word;}else pending=next;}if(pending.Length>0)lines.Add(pending);
  }return String.Join("\n",lines.ToArray());
 }
 string FrameCaption(int cap){var frame=Items(Value(Map(Value(Bank,"frames")),cap.ToString()));var basis=Items(Value(Map(Value(Bank,"frame_bases")),cap.ToString()));if(frame.Length!=4||basis.Length!=4)return "Frame correspondence unavailable";string full=RelativeFrame(frame,basis);return "Frame abcd ← "+String.Concat(full.Split(new[]{" · "},StringSplitOptions.None).Select(part=>part.Substring(part.Length-1)).ToArray());}
 void RenderPhysicalFeedback(ExperimentInputState state){
  if(keyboardWindow==null||keyboardWindow.IsDisposed)return;keyboardWindow.Text="Keyboard · "+state.BankId+" · Magic 600 Cell";
  string grip=input.ActiveGripCell.HasValue?"Grip "+CellName(input.ActiveGripCell.Value)+" · "+FrameCaption(input.ActiveGripCell.Value):"No grip selected";if(input.ActiveGripCell.HasValue&&input.ActiveGripCell.Value!=local.CenterCell)grip+=" · Local: other cell";if(input.InverseShiftHeld)grip+=" · Shift: reverse";
  string focus=InputFocusCaption();
  string status=inputFeedback.Text;bool routine=status.StartsWith("Grip C",StringComparison.Ordinal)||status.StartsWith("Onscreen grip C",StringComparison.Ordinal)||status.StartsWith("Work controls own the keyboard;",StringComparison.Ordinal)||status.StartsWith("Owned window controls own the keyboard;",StringComparison.Ordinal);
  string toggle=FunctionsShortcutNotice();if(state.BankId=="Functions")grip="Return to "+Text(Value(Workspace,"previous_bank"))+KeyHint("functions-toggle")+" · Commands only";
  keyboardContext.Text="SET "+state.BankId+" · "+Text(Value(Bank,"purpose"))+" · "+state.Destination+(state.Destination=="draft"?" / "+state.Phase:"")+" · "+state.GripMode+" · Focus: "+focus+"\n"+grip+(routine||status.Length==0?"":"\n"+status)+(toggle.Length==0?"":"\n"+toggle);
  if(keyboardContext.Width>0)keyboardContext.Height=Math.Max(46,keyboardContext.GetPreferredSize(new Size(keyboardContext.Width,0)).Height);
  string guide=Object.Equals(Value(Bank,"turns_enabled"),false)?"This is a command set. Keys do not Grip or Twist. Select an orbit set explicitly to turn.":"a b c d name the selected cap's ordered vertices. A cycle such as (abc) means a→b→c→a. Focus any Twist key to read its exact correspondence. Inverse keys reverse that cycle; Shift remains available. Camera and Local center never rebind a Grip.";
  keyboardContext.AccessibleDescription=guide+"\n"+input.HeldStatus;tips.SetToolTip(keyboardContext,keyboardContext.Text+"\n"+guide+"\n"+input.HeldStatus);
 }

 void RegisterKeymapFileCommands(){
  Register("keymap-export","Export saved keybinding overrides to a versioned file",ExportKeymapFile);
  Register("keymap-import","Inspect a keymap file and explicitly replace saved bindings",ImportKeymapFile);
 }
 async void ExportKeymapFile(){
  try{
   string path;using(var picker=new SaveFileDialog{Title="Export keybindings",Filter="Magic 600 Cell keymap (*.json)|*.json",DefaultExt="json",AddExtension=true,OverwritePrompt=true,FileName="Magic600-keymap.json"}){if(MacroFileDialog(picker)!=DialogResult.OK)return;path=picker.FileName;}
   await ExportKeymapFile(path);
  }catch(Exception error){NativeDiagnostics.Write("Keymap export failed",error);if(!closing)Say(error.Message,true);}
 }
 internal async Task<bool> ExportKeymapFile(string path){
  try{
   bool accepted=await Send(LocalApi.D("action","keymap-export"));var result=lastCommandResult;await WaitForStopCompletion();if(!accepted||closing)return false;var document=Map(Value(result,"document"));if(document==null)throw new InvalidOperationException("The engine did not return a validated keymap.");
   byte[] bytes=new UTF8Encoding(false,true).GetBytes(api.Json(document));if(bytes.Length>2000000)throw new InvalidOperationException("The keymap exceeds the 2,000,000-byte limit.");
   string temporary=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)),".magic600-keymap-"+Guid.NewGuid().ToString("N")+".tmp");
   try{File.WriteAllBytes(temporary,bytes);if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
   Say("Exported keybindings · "+Path.GetFileName(path)+". Captures, frames and work are not included."+(StopFailure==null?"":" "+StopFailure),StopFailure!=null);
   return true;
  }catch(Exception error){NativeDiagnostics.Write("Keymap export failed",error);if(!closing)Say(error.Message,true);return false;}
 }
 async void ImportKeymapFile(){
  try{
   string path;using(var picker=new OpenFileDialog{Title="Inspect keybindings",Filter="Magic 600 Cell keymap (*.json)|*.json",CheckFileExists=true,Multiselect=false}){if(MacroFileDialog(picker)!=DialogResult.OK)return;path=picker.FileName;}
   await ShowKeymapFileImport(path);
  }catch(Exception error){NativeDiagnostics.Write("Keymap inspection failed",error);if(!closing)Say(error.Message,true);}
 }
 internal async Task<bool> ShowKeymapFileImport(string path){
  try{
   string content;using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){if(stream.Length>2000000)throw new InvalidOperationException("Choose a keymap no larger than 2,000,000 bytes.");using(var reader=new StreamReader(stream,new UTF8Encoding(false,true),true))content=reader.ReadToEnd();}
   bool accepted=await Send(LocalApi.D("action","keymap-import-check","text",content));var inspection=lastCommandResult;await WaitForStopCompletion();if(!accepted||closing)return false;if(!IsReady){Say(StopFailure??"Keymap inspected; wait for the current operation status before applying.",true);return false;}if(inspection==null)throw new InvalidOperationException("Keymap inspection returned no result.");
   if(!OwnedActiveWindow()){Say("Keymap inspected. Reopen Import to confirm replacement.");return false;}return ShowKeymapImportReview(Path.GetFileName(path),content,inspection);
  }catch(Exception error){NativeDiagnostics.Write("Keymap inspection failed",error);if(!closing)Say(error.Message,true);return false;}
 }
 string KeymapImportDescription(Dictionary<string,object> bindings){
  var lines=new List<string>();Action<string,Dictionary<string,object>> add=delegate(string scope,Dictionary<string,object> group){
   lines.Add(scope);var grips=Items(Value(group,"grips"));for(int index=0;index<grips.Length;index++)lines.Add("  Grip "+(index+1)+": "+ShortCode(Text(grips[index])));
   foreach(string section in new[]{"twists","commands"}){var map=Map(Value(group,section));if(map==null)continue;foreach(var pair in map.OrderBy(p=>p.Key)){string action=pair.Value==null?"Unbound":Text(pair.Value);if(section=="commands"&&hints.ContainsKey(action))action=hints[action];else if(section=="twists"&&action.EndsWith("-",StringComparison.Ordinal))action=action.Substring(0,action.Length-1)+"⁻¹";lines.Add("  "+ShortCode(pair.Key)+" → "+action);}}
   lines.Add("");
  };
  add("Shared overrides",bindings);var banks=Map(Value(bindings,"banks"));if(banks!=null)foreach(var pair in banks.OrderBy(p=>p.Key)){var record=Items(Value(work,"banks")).Select(Map).FirstOrDefault(b=>Text(Value(b,"id"))==pair.Key);string name=Text(Value(record,"name"));add(name.Length==0?pair.Key:name+" · "+pair.Key,Map(pair.Value));}
  return String.Join("\r\n",lines.ToArray());
 }
 internal bool ShowKeymapImportReview(string filename,string content,Dictionary<string,object> inspection){
  var bindings=Map(Value(inspection,"bindings"));string basis=Text(Value(inspection,"basis"));if(bindings==null||basis.Length==0)throw new InvalidOperationException("The keymap inspection is incomplete.");
  bool imported=false;
  using(var dialog=ToolDialog("Import keybindings",690,510)){
   dialog.MinimumSize=new Size(570,450);var grid=ToolLayout(dialog,88,-1,58,MacroContextHeight,44);var counts=Map(Value(inspection,"summary"));
   var summary=ToolLabel(filename+"\n"+Text(Value(counts,"command_bindings"))+" command overrides · "+Text(Value(counts,"twist_bindings"))+" Twist overrides · "+Text(Value(counts,"grip_keys"))+" Grip keys\nReplace all saved bindings. Unlisted bindings return to inherited defaults.");summary.AccessibleName="Keymap replacement scope";summary.AutoEllipsis=false;grid.Controls.Add(summary,0,0);FitToolTextRow(grid,summary,0);
   var detail=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,Text=KeymapImportDescription(bindings),AccessibleName="Validated keymap bindings"};grid.Controls.Add(detail,0,1);
   var status=ToolLabel("Captures, frames, macros, bank names, active set, Current, Next and steps stay unchanged.");status.AutoEllipsis=false;grid.Controls.Add(status,0,2);FitToolTextRow(grid,status,2);grid.Controls.Add(MacroWorkContext(),0,3);
   var actions=ToolRow();Button apply=ToolButton("Replace all bindings"),cancel=ToolButton("Cancel");apply.AccessibleName="Apply inspected keymap";cancel.DialogResult=DialogResult.Cancel;actions.Controls.Add(apply);actions.Controls.Add(cancel);grid.Controls.Add(actions,0,4);dialog.CancelButton=cancel;bool applying=false;
   dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(applying&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;status.Text="Applying the complete keymap. Wait for the authoritative result.";}};apply.Click+=async delegate{
    if(applying)return;if(!IsReady){status.Text=StopFailure??"Finish the current action first.";return;}applying=true;apply.Enabled=cancel.Enabled=false;
    try{bool accepted=await Send(LocalApi.D("action","keymap-import","text",content,"basis",basis));await WaitForStopCompletion();if(dialog.IsDisposed)return;if(accepted){imported=true;applying=false;dialog.Close();Say("Saved keybindings replaced."+(StopFailure==null?"":" "+StopFailure),StopFailure!=null);}else{status.Text=feedback.Text+"\nReopen Import before retrying.";apply.Enabled=false;}}
    catch(Exception error){if(!dialog.IsDisposed){status.Text=error.Message;apply.Enabled=false;}}
    finally{applying=false;if(!dialog.IsDisposed)cancel.Enabled=true;}
   };
   detail.Select(0,0);dialog.ActiveControl=cancel;ShowOwned(dialog);
  }
  return imported;
 }
 string EffectiveKeyboardCode(string code,ExperimentInputState state){return input.InverseShiftHeld&&!code.Contains("+")?"Shift+"+code:code;}
 void ActivateOnscreenKey(string code){var state=InputState();string command,axis;int cap;if(state.CommandKeys.TryGetValue(EffectiveKeyboardCode(code,state),out command)){RunCommand(command);return;}if(state.GripKeys.TryGetValue(code,out cap)){if(input.InverseShiftHeld)return;if(input.ActiveGripCode=="Pointer"&&input.ActiveGripCell==cap)input.ReleasePointerGrip();else input.SetPointerGrip(cap);RenderPhysicalKeyboard();return;}if(state.TwistKeys.TryGetValue(code,out axis)){bool reverse=axis.EndsWith("-",StringComparison.Ordinal);input.Twist(reverse?axis.Substring(0,axis.Length-1):axis,reverse^input.InverseShiftHeld);RenderPhysicalKeyboard();}}
}
