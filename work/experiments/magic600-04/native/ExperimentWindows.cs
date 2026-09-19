// Owned work surfaces share one adopted snapshot and one keyboard router.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 sealed class WorkToolForm:Form {protected override bool ShowWithoutActivation {get{return true;}}}
 [System.Runtime.InteropServices.DllImport("user32.dll",EntryPoint="ShowWindow")]static extern bool ShowToolWindow(System.Runtime.InteropServices.HandleRef window,int command);
 static void RestoreMinimizedWindow(Form window){
  if(window.WindowState!=FormWindowState.Minimized)return;
  const int showNormalWithoutActivation=4;var handle=new System.Runtime.InteropServices.HandleRef(window,window.Handle);
  // Let native messages update WindowState and RestoreBounds together.
  ShowToolWindow(handle,showNormalWithoutActivation);
  // Windows can first restore the maximized state that preceded minimization.
  if(window.WindowState==FormWindowState.Maximized)ShowToolWindow(handle,showNormalWithoutActivation);
 }
 sealed class CellCenterButton:Button { }
 sealed class WorkReadout:TableLayoutPanel {
  internal readonly Label Current=new Label(),Next=new Label(),Status=new Label();
  bool measuring,layoutQueued;
  internal WorkReadout(){Dock=DockStyle.Bottom;ColumnCount=2;RowCount=2;Padding=new Padding(8,4,8,4);BackColor=Color.FromArgb(25,33,41);ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));RowStyles.Add(new RowStyle(SizeType.Percent,100));RowStyles.Add(new RowStyle(SizeType.Absolute,38));foreach(var label in new[]{Current,Next,Status}){label.Dock=DockStyle.Fill;label.ForeColor=Ink;label.AutoEllipsis=false;label.Padding=new Padding(0,0,6,0);label.Margin=Padding.Empty;label.TextChanged+=delegate{PerformLayout();};}Controls.Add(Current,0,0);Controls.Add(Next,1,0);Controls.Add(Status,0,1);SetColumnSpan(Status,2);}
  int PreferredContentHeight(out int status){
   int a=Current.GetPreferredSize(new Size(Current.Width,0)).Height,b=Next.GetPreferredSize(new Size(Next.Width,0)).Height;status=Status.GetPreferredSize(new Size(Status.Width,0)).Height;return Math.Max(a,b)+status+Padding.Vertical+2;
  }
  void QueueMeasuredLayout(TableLayoutPanel host){
   var dispatcher=FindForm();if(layoutQueued||dispatcher==null||!dispatcher.IsHandleCreated||dispatcher.IsDisposed||dispatcher.Disposing)return;
   // A row changed during its parent's layout is not arranged until another pass.
   layoutQueued=true;dispatcher.BeginInvoke((MethodInvoker)delegate{layoutQueued=false;if(IsDisposed||host.IsDisposed||Parent!=host)return;host.PerformLayout();PerformLayout();});
  }
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);PerformLayout();}
  protected override void OnLayout(LayoutEventArgs e){
   base.OnLayout(e);if(measuring||RowStyles.Count<2||Controls.Count<3||Current.Width<24||Status.Width<24)return;
   var host=Parent as TableLayoutPanel;int row=host==null?-1:host.GetRow(this);bool bottom=Dock==DockStyle.Bottom;
   if(!bottom&&(Dock!=DockStyle.Fill||host==null||row<0||row>=host.RowStyles.Count))return;
   bool needsLayout=false;measuring=true;try{
    int status,height=PreferredContentHeight(out status);needsLayout=Height!=height||Status.Height!=status;if(RowStyles[1].Height!=status){RowStyles[1].Height=status;needsLayout=true;}
    if(bottom){if(Height!=height)Height=height;}
    else{var style=host.RowStyles[row];height+=Margin.Vertical;if(style.SizeType!=SizeType.Absolute)style.SizeType=SizeType.Absolute;if(style.Height!=height)style.Height=height;}
   }finally{measuring=false;}
   if(!bottom&&needsLayout)QueueMeasuredLayout(host);
  }
 }
 readonly Dictionary<string,Form> workWindows=new Dictionary<string,Form>();
 readonly Dictionary<string,WorkReadout> windowReadouts=new Dictionary<string,WorkReadout>();
 readonly Dictionary<string,Control> windowContents=new Dictionary<string,Control>();
 readonly Timer windowSaveTimer=new Timer{Interval=600};
 bool windowLayoutDirty,restoringWindows,windowsRestored,fullscreen,windowSaveInFlight;
 long windowLayoutGeneration,windowLayoutSavedGeneration;
 string windowLayoutFailure;
 internal bool WindowLayoutPending {get{return windowLayoutDirty||windowSaveInFlight;}}
 internal string WindowLayoutFailure {get{return windowLayoutFailure;}}
 Rectangle priorWindowBounds; FormBorderStyle priorWindowBorder;

 bool OwnedActiveWindow(){var active=Form.ActiveForm;return active==form||active!=null&&windows.Contains(active);}
 Control FocusedOwnedControl(){var active=Form.ActiveForm;if(active==null||active!=form&&!windows.Contains(active))return null;Control control=active;while(control is ContainerControl){var child=((ContainerControl)control).ActiveControl;if(child==null)break;control=child;}return control;}
 string WindowCaption(string id){return id=="local"?"Local cell":id=="global"?"Global structure":id=="solve"||id=="macro"||id=="operation"?"Solve":id=="keyboard"?"Keyboard":id;}
 Form EnsureWorkWindow(string id,Control content,Size size){
  Form existing;if(workWindows.TryGetValue(id,out existing))return existing;
  var window=new WorkToolForm{Text=WindowCaption(id)+" · Magic 600 Cell",Font=form.Font,BackColor=Paper,ForeColor=Ink,ClientSize=size,MinimumSize=new Size(400,260),StartPosition=FormStartPosition.Manual,ShowInTaskbar=false,KeyPreview=false};
  window.MinimumSize=WorkWindowMinimum(id);
  var readout=new WorkReadout{Height=Math.Max(105,form.Font.Height*5+17),AccessibleName=WindowCaption(id)+" shared work and keyboard context"};
  if(id=="keyboard"){int statusHeight=form.Font.Height+2;readout.Height-=(int)readout.RowStyles[1].Height-statusHeight;readout.RowStyles[1].Height=statusHeight;}
  if(content.Parent!=null)content.Parent.Controls.Remove(content);content.Dock=DockStyle.Fill;content.Visible=true;window.Controls.Add(content);window.Controls.Add(readout);workWindows.Add(id,window);windowReadouts.Add(id,readout);windowContents.Add(id,content);windows.Add(window);input.RegisterWindow(window);
  window.Activated+=delegate{UpdateWindowReadouts();input.RefreshFocusFeedback();};
  window.Deactivate+=delegate{UpdateWindowReadouts();};
  window.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(closing||e.CloseReason!=CloseReason.UserClosing)return;e.Cancel=true;bool owned=OwnedActiveWindow();window.Hide();input.Reset("Tool closed; release held keys.");MarkWindowLayout();UpdateViewRendering();if(owned)FocusWorkspace();};
  window.LocationChanged+=delegate{MarkWindowLayout();};window.SizeChanged+=delegate{MarkWindowLayout();UpdateViewRendering();};window.VisibleChanged+=delegate{MarkWindowLayout();UpdateViewRendering();};
  if(workWindows.Count==1)windowSaveTimer.Tick+=delegate{SaveWindowLayout();};
  bool restored=RestoreWindowBounds(id,window);if(!restored)PlaceWorkWindow(id,window);window.Shown+=delegate{if(!restored)PlaceWorkWindow(id,window);else ClampWindowToScreen(window);};UpdateWindowReadouts();return window;
 }
 Size WorkWindowMinimum(string id){return id=="keyboard"?new Size(880,460):id=="solve"?new Size(780,570):new Size(470,420);}
 Rectangle[] WorkContextBounds(){var bands=new List<Rectangle>();if(hub.Visible&&hub.IsHandleCreated)bands.AddRange(hub.PersistentContextBounds);if(feedback.Visible&&feedback.IsHandleCreated)bands.Add(feedback.RectangleToScreen(feedback.ClientRectangle));return bands.ToArray();}
 void FitWindowMinimum(Form window,Rectangle area){window.MinimumSize=new Size(Math.Min(window.MinimumSize.Width,area.Width),Math.Min(window.MinimumSize.Height,area.Height));}
 void ClampWindowToScreen(Form window){if(window.WindowState!=FormWindowState.Normal)return;var area=Screen.FromRectangle(window.Bounds).WorkingArea;FitWindowMinimum(window,area);window.Bounds=ExperimentWindowPlacement.Clamp(window.Bounds,area,window.MinimumSize);}
 void PlaceWorkWindow(string id,Form window){var area=Screen.FromControl(form).WorkingArea;FitWindowMinimum(window,area);window.Bounds=ExperimentWindowPlacement.Place(id,area,form.Bounds,window.Size,window.MinimumSize,workWindows.Values.Where(w=>w!=window&&w.Visible&&w.WindowState!=FormWindowState.Minimized).Select(w=>w.Bounds),WorkContextBounds(),SystemInformation.CaptionHeight+SystemInformation.FrameBorderSize.Height);}
 void PlaceOwnedDialog(Form dialog,Form owner){var area=Screen.FromControl(owner).WorkingArea;FitWindowMinimum(dialog,area);dialog.StartPosition=FormStartPosition.Manual;dialog.Bounds=ExperimentWindowPlacement.Place("dialog",area,owner.Bounds,dialog.Size,dialog.MinimumSize,workWindows.Values.Where(w=>w.Visible&&w!=owner&&w.WindowState!=FormWindowState.Minimized).Select(w=>w.Bounds),WorkContextBounds(),SystemInformation.CaptionHeight+SystemInformation.FrameBorderSize.Height);dialog.Shown+=delegate{ClampWindowToScreen(dialog);};}
 bool RestoreWindowBounds(string id,Form window){
  var records=Map(Value(Map(Value(Workspace,"view")),"windows"));var entry=Map(Value(records,id));
  // Old independent tools become one surface. Read their location only; its larger content has one minimum.
  if(entry==null&&id=="solve"){entry=Map(Value(records,"operation"))??Map(Value(records,"macro"));}
  if(entry==null)return false;
  try{var bounds=new Rectangle(Number(entry["x"]),Number(entry["y"]),Number(entry["width"]),Number(entry["height"]));var area=Screen.FromRectangle(bounds).WorkingArea;FitWindowMinimum(window,area);window.Bounds=ExperimentWindowPlacement.Clamp(bounds,area,window.MinimumSize);return true;}catch(KeyNotFoundException){return false;}
 }
 void MarkWindowLayout(){if(restoringWindows||!connected||closing)return;windowLayoutGeneration++;windowLayoutDirty=true;windowSaveTimer.Stop();windowSaveTimer.Interval=600;windowSaveTimer.Start();}
 void SaveWindowLayout(){
  if(!windowLayoutDirty||windowSaveInFlight||restoringWindows||!connected||busy||closing)return;
  var records=new Dictionary<string,object>();foreach(var pair in workWindows){var w=pair.Value;if(w.IsDisposed)continue;var b=w.WindowState==FormWindowState.Normal?w.Bounds:w.RestoreBounds;records[pair.Key]=LocalApi.D("x",b.X,"y",b.Y,"width",b.Width,"height",b.Height,"visible",w.Visible);}
  long generation=windowLayoutGeneration;windowSaveTimer.Stop();windowSaveInFlight=true;
  NativeDiagnostics.Write("Window layout background save: generation "+generation);
  Task.Factory.StartNew(()=>api.Post("experiment/window-layout",LocalApi.D("generation",generation,"windows",records))).ContinueWith(task=>OnUi(delegate{
   windowSaveInFlight=false;
   Exception failure=task.IsFaulted?task.Exception.GetBaseException():null;
   if(failure==null){var result=task.Result;if(Convert.ToInt64(Value(result,"generation"))!=generation)failure=new InvalidOperationException("Window layout acknowledgment did not match the sent generation.");else if(Object.Equals(Value(result,"saved"),true)){windowLayoutSavedGeneration=generation;windowLayoutFailure=null;string warning=Text(Value(result,"warning"));if(warning.Length>0)NativeDiagnostics.Write("Window layout saved with warning: "+warning);}}
   if(failure!=null){string message="Window layout was not saved; it remains pending and will retry. "+failure.Message;if(windowLayoutFailure!=message){NativeDiagnostics.Write(message,failure);if(!busy)Say(message,true);}windowLayoutFailure=message;}
   windowLayoutDirty=windowLayoutSavedGeneration<windowLayoutGeneration;
   if(windowLayoutDirty&&!closing){windowSaveTimer.Interval=failure==null?600:2000;windowSaveTimer.Start();}
  },()=>{windowSaveInFlight=false;}));
 }
 void OpenWorkWindow(string id,bool activate=true){
  string requested=id;Control content;Size size;
  if(id=="local"){content=local;size=new Size(680,650);}else if(id=="global"){content=global;size=new Size(660,600);}else if(id=="macro"||id=="operation"||id=="solve"){id="solve";content=BuildSolveContent();size=new Size(824,581);}else throw new ArgumentException("Unknown work window: "+id);
  var window=EnsureWorkWindow(id,content,size);content.Visible=true;if(requested=="macro")SelectSolvePage("macros",false);RestoreMinimizedWindow(window);if(!window.Visible){ClampWindowToScreen(window);window.Show();}if(activate){window.Activate();if(requested=="macro")search.Focus();else if(requested=="operation"||requested=="solve")FocusOperationControls();else content.Focus();}UpdateViewRendering();UpdateWindowReadouts();
 }
 void HideWorkWindow(string id){if(id=="macro"||id=="operation")id="solve";Form window;if(!workWindows.TryGetValue(id,out window))return;bool active=Form.ActiveForm==window;window.Hide();input.Reset("Tool hidden; release held keys.");UpdateViewRendering();if(active)FocusWorkspace();}
 void FocusOperationControls(){var button=commandButtons.FirstOrDefault(e=>e.Value==(OperationExecuted?"operation-new":"review")&&e.Key.Parent==operationStrip).Key;if(button!=null&&button.Visible&&button.Enabled){button.FindForm().Activate();button.Focus();}}
 void UpdateViewRendering(){Form window;local.SetRenderingActive(workWindows.TryGetValue("local",out window)?window.Visible&&window.WindowState!=FormWindowState.Minimized:views.SelectedIndex==1&&!viewSplit.Panel2Collapsed);global.SetRenderingActive(workWindows.TryGetValue("global",out window)?window.Visible&&window.WindowState!=FormWindowState.Minimized:views.SelectedIndex==2&&!viewSplit.Panel2Collapsed);}
 string ReadoutPiece(string role,Dictionary<string,object> piece){if(piece==null)return role+" · unassigned";var names=Map(Value(piece,"names"));var lines=Items(Value(names,"identity_lines")).Select(Text).ToArray();return role+" · "+Text(Value(names,"category"))+"\n"+(lines.Length>0?String.Join("\n",lines):PieceCaption(piece));}
 string KeyboardReadoutPiece(string role,Dictionary<string,object> piece){if(piece==null)return role+" · unassigned";var names=Map(Value(piece,"names"));string home=Text(Value(names,"home_short"));return home.Length>0?role+" · Home "+home:ReadoutPiece(role,piece);}
 string InputFocusCaption(){
  var active=Form.ActiveForm;if(active==form)return "Workspace";
  string named=workWindows.Where(pair=>pair.Value==active).Select(pair=>WindowCaption(pair.Key)).FirstOrDefault();if(named!=null)return named;
  if(active!=null&&windows.Contains(active))return active.Text;
  return "Outside application";
 }
 void UpdateWindowReadouts(){if(work==null)return;var keys=InputState();string focus=InputFocusCaption();string context="Set "+keys.BankId+" · "+keys.Destination+(keys.Destination=="draft"?" / "+keys.Phase:"")+" · Focus: "+focus;var current=Map(Value(work,"current"));var next=Map(Value(work,"next"));foreach(var pair in windowReadouts){var readout=pair.Value;bool compact=pair.Key=="keyboard"||pair.Key=="solve";readout.Current.Text=compact?KeyboardReadoutPiece("Current",current):ReadoutPiece("Current",current);readout.Next.Text=compact?KeyboardReadoutPiece(next==null?"Next":"Next · locked",next):ReadoutPiece(next==null?"Next":"Next · locked",next);readout.Status.Text=pair.Key=="keyboard"?protection.Text+" · "+(Object.Equals(Value(Workspace,"prefix"),true)?"Each turn":"Final result"):protection.Text+"\n"+context;tips.SetToolTip(readout.Current,PieceCaption(current));tips.SetToolTip(readout.Next,PieceCaption(next)+"\nIdentity bookmark; does not mechanically protect it.");tips.SetToolTip(readout.Status,Text(Value(Bank,"purpose"))+"\n"+inputFeedback.Text);}RenderPhysicalFeedback(keys);DrawSolveWindow();}
 void RestoreWorkWindows(){if(windowsRestored||mode!="g2"||!connected)return;windowsRestored=true;var records=Map(Value(Map(Value(Workspace,"view")),"windows"));if(records==null)return;restoringWindows=true;try{foreach(var pair in records){var entry=Map(pair.Value);if(!Object.Equals(Value(entry,"visible"),true))continue;if(new[]{"macro","operation"}.Contains(pair.Key))continue;if(new[]{"local","global","solve"}.Contains(pair.Key))OpenWorkWindow(pair.Key,false);else if(pair.Key=="keyboard")OpenFloatingKeyboard(false);}if(Value(records,"solve")==null){bool macro=Object.Equals(Value(Map(Value(records,"macro")),"visible"),true),operation=Object.Equals(Value(Map(Value(records,"operation")),"visible"),true);if(macro||operation)OpenWorkWindow(macro?"macro":"operation",false);}}finally{restoringWindows=false;}}
 void CycleBank(int direction){var records=Items(Value(work,"banks")).Select(Map).Where(r=>!Object.Equals(Value(r,"default_cycle"),false)||Text(r["id"])==Text(Workspace["bank"])).ToArray();int index=Array.FindIndex(records,r=>Text(r["id"])==Text(Workspace["bank"]));if(index<0||records.Length==0)return;Send(LocalApi.D("action","bank","id",records[(index+direction+records.Length)%records.Length]["id"]));}
 void ToggleFullscreen(){if(!fullscreen){priorWindowBounds=form.Bounds;priorWindowBorder=form.FormBorderStyle;form.WindowState=FormWindowState.Normal;form.FormBorderStyle=FormBorderStyle.None;form.WindowState=FormWindowState.Maximized;}else{form.WindowState=FormWindowState.Normal;form.FormBorderStyle=priorWindowBorder;form.Bounds=priorWindowBounds;}fullscreen=!fullscreen;FocusWorkspace();}
 void ShowLocalCenter(){
  int candidate=local.CenterCell;using(var dialog=ToolDialog("Local center",620,530)){
   var grid=ToolLayout(dialog,42,34,-1,48,42);var label=ToolLabel("Choose the cell to inspect. This changes only Local's center; grips, Current and Next stay unchanged.");grid.Controls.Add(label,0,0);
   var query=new TextBox{Dock=DockStyle.Fill,AccessibleName="Find a Local center by canonical C1 to C600"};grid.Controls.Add(query,0,1);var colors=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,WrapContents=true};grid.Controls.Add(colors,0,2);var selected=ToolLabel("Current center: "+CellName(candidate));grid.Controls.Add(selected,0,3);var actions=ToolRow();Button apply=ToolButton("Use selected center"),cancel=ToolButton("Cancel");cancel.DialogResult=DialogResult.Cancel;actions.Controls.Add(apply);actions.Controls.Add(cancel);grid.Controls.Add(actions,0,4);dialog.CancelButton=cancel;dialog.AcceptButton=apply;bool applying=false;dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(applying&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;selected.Text="Saving the selected center. Wait for the result; your selection is kept.";}};
   Action fill=delegate{colors.SuspendLayout();while(colors.Controls.Count>0){var child=colors.Controls[0];colors.Controls.Remove(child);child.Dispose();}string term=query.Text.Trim();for(int cell=1;cell<=600;cell++){int c=cell;if(term.Length>0&&("C"+c+" "+CellName(c)).IndexOf(term,StringComparison.OrdinalIgnoreCase)<0)continue;Color color=bridge.Palette[c-1];var b=new CellCenterButton{Text=CellKeyName(c),Tag=c,Width=100,Height=54,BackColor=color,ForeColor=color.GetBrightness()<.48?Color.White:Color.Black,FlatStyle=FlatStyle.Flat,AccessibleName="Local center "+CellName(c)+"; canonical C"+c,Margin=new Padding(3)};b.FlatAppearance.BorderSize=c==candidate?3:1;b.FlatAppearance.BorderColor=c==candidate?Accent:Rule;b.Click+=delegate{candidate=c;selected.Text="Selected "+CellName(c)+" · Apply explicitly; keyboard grips stay fixed.";foreach(Button button in colors.Controls)button.FlatAppearance.BorderSize=Number(button.Tag)==candidate?3:1;};tips.SetToolTip(b,CellName(c)+" · canonical C"+c);colors.Controls.Add(b);}colors.ResumeLayout(true);};query.TextChanged+=delegate{fill();};apply.Click+=async delegate{if(applying)return;applying=true;apply.Enabled=cancel.Enabled=query.Enabled=colors.Enabled=false;try{bool accepted=await Send(LocalApi.D("action","settings","view",LocalApi.D("local_center",candidate)));applying=false;if(dialog.IsDisposed)return;if(accepted)dialog.Close();else selected.Text=feedback.Text;}finally{applying=false;if(!dialog.IsDisposed)apply.Enabled=cancel.Enabled=query.Enabled=colors.Enabled=true;}};dialog.Shown+=delegate{query.Focus();};fill();ShowOwned(dialog);
  }
 }
 void RegisterWindowCommands(){
  Register("workspace-fullscreen","Toggle fullscreen workspace",ToggleFullscreen);Register("windows-hide","Hide supporting windows",()=>{foreach(var window in workWindows.Values)window.Hide();input.Reset("Supporting windows hidden.");UpdateViewRendering();FocusWorkspace();});
  Register("bank-next","Select next keyboard set",()=>CycleBank(1));Register("bank-prev","Select previous keyboard set",()=>CycleBank(-1));foreach(string name in new[]{"Workspace","Views","Filter","Session","Macro","Operation","Keyboard"}){string id=name;Register("bank-"+id,"Use "+id+" keyboard set",()=>Send(LocalApi.D("action","bank","id",id)));}
  Register("local-center","Choose Local center",ShowLocalCenter);Register("local-center-current","Center Local on Current's first hosting cell",()=>{var current=Map(Value(work,"current"));if(current==null)throw new InvalidOperationException("Choose Current first.");Send(LocalApi.D("action","settings","view",LocalApi.D("local_center",Items(current["current_cells"])[0])));});
  Register("local-center-grip","Center Local on selected Grip",()=>{if(!input.ActiveGripCell.HasValue)throw new InvalidOperationException("Select a Grip first.");Send(LocalApi.D("action","settings","view",LocalApi.D("local_center",input.ActiveGripCell.Value)));});
  Register("grip-frame","Choose the selected Grip's ordered frame",ShowGripFrame);
  foreach(string name in new[]{"local","global"}){string id=name;NativeCellView view=id=="local"?local:global;Register(id+"-reset","Reset "+id+" view",view.ResetView);Register(id+"-zoom-in","Zoom "+id+" in",()=>view.ZoomView(1.12));Register(id+"-zoom-out","Zoom "+id+" out",()=>view.ZoomView(1/1.12));Register(id+"-left","Rotate "+id+" left",()=>view.RotateView(-.1,0));Register(id+"-right","Rotate "+id+" right",()=>view.RotateView(.1,0));Register(id+"-up","Rotate "+id+" up",()=>view.RotateView(0,.1));Register(id+"-down","Rotate "+id+" down",()=>view.RotateView(0,-.1));}
 }
 void CloseWorkWindows(){windowSaveTimer.Stop();windowSaveTimer.Dispose();foreach(var window in workWindows.Values.ToArray())if(!window.IsDisposed)window.Close();}
 string CellName(int cap){var names=Items(Value(structure,"cell_names"));return cap>=1&&cap<=names.Length?Text(Value(Map(names[cap-1]),"name")):"Cell C"+cap;}
 string CellKeyName(int cap){var names=Items(Value(structure,"cell_names"));var pole=cap>=1&&cap<=names.Length?Items(Value(Map(names[cap-1]),"pole")):new object[0];return pole.Length==4?"("+pole[0]+","+pole[1]+";\n"+pole[2]+","+pole[3]+")":"Grip C"+cap;}
 static string RelativeFrame(object[] vertices,object[] basis){if(vertices.Length!=4||basis.Length!=4)return "Ordered frame unavailable";var old=basis.Select(Number).ToArray();return String.Join(" · ",vertices.Select((v,i)=>{int index=Array.IndexOf(old,Number(v));return "abcd"[i]+"←"+(index<0?"?":"abcd"[index].ToString());}).ToArray());}
 async void ShowGripFrame(){
  if(!input.ActiveGripCell.HasValue){Say("Select one Grip before changing its ordered frame.",true);return;}int cap=input.ActiveGripCell.Value;string bank=Text(Workspace["bank"]);if(!await Send(LocalApi.D("action","grip-frames","cell",cap)))return;var result=lastCommandResult;if(result==null)return;if(!OwnedActiveWindow()){Say("Grip frames loaded. Return to the application and reopen the frame editor.");return;}
  var old=Items(Value(result,"selected"));var basis=Items(Value(result,"base_vertices"));var frames=Items(Value(result,"frames")).Select(Map).ToArray();
  using(var dialog=ToolDialog("Grip frame · "+CellName(cap),700,470)){
   var grid=ToolLayout(dialog,82,-1,62,44);grid.Controls.Add(ToolLabel("Key set "+bank+" · "+CellName(cap)+"\nOld: "+RelativeFrame(old,basis)+"\nSymbols on the right are the retained base corners. This changes input meaning; it does not turn the puzzle."),0,0);
   var list=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false,AccessibleName="Twelve legal ordered frames relative to base corners"};foreach(var frame in frames){var vertices=Items(frame["vertices"]);list.Items.Add(new Choice(api.Json(vertices),RelativeFrame(vertices,basis)));}grid.Controls.Add(list,0,1);
   var status=ToolLabel("Select a frame, inspect the new corner correspondence, then Apply explicitly.");grid.Controls.Add(status,0,2);list.SelectedIndexChanged+=delegate{if(list.SelectedItem!=null){status.Text="New: "+((Choice)list.SelectedItem).Label+"\nLocal labels and Twist cycle symbols will use this same ordered frame.";tips.SetToolTip(list,"Canonical old vertices: "+api.Json(old)+"\nCanonical new vertices: "+((Choice)list.SelectedItem).Id);}};
   var actions=ToolRow();Button apply=ToolButton("Apply selected frame"),cancel=ToolButton("Cancel");cancel.DialogResult=DialogResult.Cancel;actions.Controls.Add(apply);actions.Controls.Add(cancel);grid.Controls.Add(actions,0,3);dialog.CancelButton=cancel;bool applying=false;
   dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(applying&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;status.Text="Saving the selected frame. Wait for the result; your selection is kept.";}};
   apply.Click+=async delegate{
    if(applying)return;if(list.SelectedItem==null){status.Text="Select one of the twelve proper frames.";return;}if(Text(Workspace["bank"])!=bank){status.Text="The key set changed. Close and reopen the frame editor for the intended set.";return;}
    applying=true;apply.Enabled=cancel.Enabled=list.Enabled=false;var vertices=api.Parse(((Choice)list.SelectedItem).Id);
    try{bool accepted=await Send(LocalApi.D("action","capture-frame","bank",bank,"cell",cap,"vertices",vertices,"previous_vertices",old));applying=false;if(dialog.IsDisposed)return;if(accepted)dialog.Close();else status.Text=feedback.Text;}finally{applying=false;if(!dialog.IsDisposed)apply.Enabled=cancel.Enabled=list.Enabled=true;}
   };ShowOwned(dialog);
  }
 }
}
