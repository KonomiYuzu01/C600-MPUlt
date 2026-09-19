// Session controls use the same authoritative engine and existing Solve transaction path.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 Form sessionDialog;
 TextBox sessionReportText;
 readonly HashSet<string> shownSessionCompletions=new HashSet<string>();
 bool sessionCompletionOpening;
 Dictionary<string,object> pendingSessionCompletion;

 void RegisterSessionCommands(){
  Register("session","Open Session, timer and recovery",()=>ShowSessionReport(false));
  Register("session-report","Read the authoritative session report",()=>ShowSessionReport(false));
  Register("session-summary","Reopen the recorded completion summary",()=>ShowSessionReport(true));
  Register("session-new","Start a solved session; keep previous progress recoverable",()=>ShowSessionReset("new"));
  Register("session-resume","Resume saved work without resetting it",()=>RunSessionQuick("session-resume",null));
  Register("scramble","Stage a recorded scramble for explicit review and execution",ShowSessionScramble);
  Register("session-timer-start","Start the explicit session timer",()=>RunSessionQuick("session-timer","start"));
  Register("session-timer-pause","Pause the explicit session timer",()=>RunSessionQuick("session-timer","pause"));
  Register("session-save-log","Save the current verified session log",SaveSessionLog);
  Register("session-import-log","Inspect and explicitly import a saved session log",ChooseSessionLogImport);
  Register("session-export-log","Export a verified C600 or MPUlt session log",ChooseSessionLogExport);
  Register("reset-workspace","Reset work presentation; retain history, banks and macros",()=>ShowSessionReset("workspace"));
  Register("reset-view","Reset cameras and projection; retain puzzle and work",()=>{ResetPuzzleView();Say("Views reset. Puzzle state, work and history are unchanged.");});
 }
 Dictionary<string,object> SessionData {get{return Map(Value(work,"session"));}}
 static string SessionValue(Dictionary<string,object> data,string key){object value=Value(data,key);return value==null?"Unavailable":Text(value);}
 string SessionSource(Dictionary<string,object> data){
  object source=Value(data,"source");var details=Map(source);if(details==null)return source==null?"Not recorded":Text(source);
  string kind=Text(Value(details,"label"));if(kind.Length==0)kind=Text(Value(details,"kind"));if(kind.Length==0)kind=Text(Value(details,"type"));
  var parts=new List<string>();if(kind.Length>0)parts.Add(kind);if(Value(details,"seed")!=null)parts.Add("seed "+Text(details["seed"]));if(Value(details,"count")!=null)parts.Add(Text(details["count"])+" legal turns");
  return parts.Count==0?"Recorded source":String.Join(" · ",parts.ToArray());
 }
 string SessionReportBody(Dictionary<string,object> data,bool completion){
  if(data==null)return "Session information is unavailable. Refresh after the engine is ready.";
  var timer=Map(Value(data,"timer"));var stats=Map(Value(data,"stats"));var lines=new List<string>();
  if(completion){lines.Add(SessionValue(data,"kind"));lines.Add(SessionValue(data,"predicate"));lines.Add("Saved result · journal step "+SessionValue(data,"head"));}
  else lines.Add("Journal step "+SessionValue(data,"head")+(Object.Equals(Value(data,"raw_full_home"),true)?" · labels currently at Home":""));
  lines.Add("Source · "+SessionSource(data));
  double seconds;string elapsed=Double.TryParse(Convert.ToString(Value(timer,"seconds"),CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out seconds)&&!Double.IsNaN(seconds)&&!Double.IsInfinity(seconds)&&seconds>=0?seconds.ToString("F1",CultureInfo.InvariantCulture)+" s":"Unavailable";
  lines.Add("Timer · "+elapsed+(Value(timer,"running")==null?"":Object.Equals(Value(timer,"running"),true)?" · running":" · paused"));
  lines.Add("Scramble · "+SessionValue(stats,"scramble_primitives")+" primitive turns");
  lines.Add("Solution · "+SessionValue(stats,"solution_primitives")+" primitive turns");
  lines.Add("Stars · "+SessionValue(stats,"stars")+"    Operations · "+SessionValue(stats,"operations"));
  lines.Add("Assisted operations · "+SessionValue(stats,"assisted_transactions"));
  string scope=Text(Value(timer,"scope"));if(scope.Length>0){lines.Add("");lines.Add(scope);}lines.Add("");
  lines.Add(completion?"This is the recorded result, not a new completion. Reopening it does not execute any steps.":"Home labels alone do not certify completion.");
  return String.Join("\r\n",lines.ToArray());
 }
 void DrawSessionState(){if(sessionDialog!=null&&!sessionDialog.IsDisposed&&sessionReportText!=null&&!sessionReportText.IsDisposed)sessionReportText.Text=SessionReportBody(SessionData,false);}
 bool SessionHasWork(){return Value(work,"pending")!=null||Map(Value(Workspace,"draft"))!=null&&Map(Workspace["draft"]).Values.Any(value=>Items(value).Length>0);}
 async Task<bool> SendSession(Dictionary<string,object> payload){if(!IsReady){Say(StopFailure??"Finish the current action before changing Session.",true);return false;}bool accepted=await Send(payload);if(!await WaitForStopCompletion())Say((accepted?"Action accepted. ":"")+(StopFailure??"Stop acknowledgement is unavailable; further operations remain blocked."),true);return accepted;}
 async void RunSessionQuick(string action,string command){
  try{var body=LocalApi.D("action",action);if(command!=null)body["command"]=command;if(await SendSession(body))DrawSessionState();}catch(Exception error){if(!closing)Say(error.Message,true);}
 }
 async void SaveSessionLog(){
  try{if(!await SendSession(LocalApi.D("action","session-save-log","format","c600"))||closing)return;string path=Text(Value(lastCommandResult,"path"));string saved=path.Length>0?"Saved session log · "+Path.GetFileName(path):"The engine saved the current session log.";Say(saved+(StopFailure==null?"":" "+StopFailure),StopFailure!=null);}catch(Exception error){if(!closing)Say(error.Message,true);}
 }
 const int SessionLogFileLimit=16*1024*1024;
 static byte[] ReadSessionLogFile(string path){
  using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
   if(file.Length<1||file.Length>SessionLogFileLimit)throw new InvalidDataException("Choose a log between 1 byte and 16 MiB.");
   using(var data=new MemoryStream()){var buffer=new byte[65536];int count;while((count=file.Read(buffer,0,buffer.Length))>0){if(data.Length+count>SessionLogFileLimit)throw new InvalidDataException("The log grew beyond 16 MiB; choose it again.");data.Write(buffer,0,count);}return data.ToArray();}
  }
 }
 async Task<bool> SendSessionLog(Dictionary<string,object> body){
  if(!IsReady){Say(StopFailure??"Finish the current action before checking a log.",true);return false;}
  bool accepted=await SendRoute("experiment/session-log",body);if(!await WaitForStopCompletion())Say((accepted?"Log action accepted. ":"")+(StopFailure??"Stop acknowledgement is unavailable."),true);return accepted;
 }
 void ChooseSessionLogImport(){
  try{using(var picker=new OpenFileDialog{Title="Inspect a saved session log",Filter="C600 proof log (*.c600.json.gz;*.json;*.gz)|*.c600.json.gz;*.json;*.gz|MPUlt log (*.log)|*.log",CheckFileExists=true,Multiselect=false}){if(MacroFileDialog(picker)!=DialogResult.OK)return;ShowSessionLogImport(picker.FileName,picker.FilterIndex==2?"mpult":"c600");}}
  catch(Exception error){if(!closing)Say(error.Message,true);}
 }
 string SessionLogInspection(Dictionary<string,object> receipt){
  return "Verified "+SessionValue(receipt,"format")+" log · "+SessionValue(receipt,"bytes")+" bytes\r\n"+
   "Selected transactions · "+SessionValue(receipt,"selected_transactions")+"    Redo · "+SessionValue(receipt,"redo_transactions")+"\r\n"+
   "Primitive turns · "+SessionValue(receipt,"primitive_count")+"\r\n"+
   "Recorded source claims · "+String.Join(", ",Items(Value(Map(Value(receipt,"source")),"assistance")).Select(Text).ToArray())+"\r\n"+
   (Object.Equals(Value(receipt,"raw_full_home"),true)?"All labels at Home. Import is not a completed solve.":"Unfinished puzzle state.")+"\r\n"+
   "State · "+SessionValue(receipt,"state_hash")+"\r\nFile · "+SessionValue(receipt,"file_sha256")+"\r\n\r\n"+
   "Import creates a verified journal branch and a recovery checkpoint. Current preferences, bindings and work are retained. Imported provenance is not a human-solve certificate.";
 }
 internal void ShowSessionLogImport(string path,string format){
  using(var dialog=ToolDialog("Import session log",660,520)){
   dialog.MinimumSize=new Size(570,470);var grid=ToolLayout(dialog,38,38,-1,MacroContextHeight,44);
   var file=new TextBox{Text=Path.GetFullPath(path),ReadOnly=true,Dock=DockStyle.Fill,AccessibleName="Log file"};grid.Controls.Add(file,0,0);
   var formats=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Log format"};formats.Items.AddRange(new object[]{new Choice("c600","C600 proof log · compressed or JSON"),new Choice("mpult","MPUlt native log")});formats.SelectedIndex=format=="mpult"?1:0;grid.Controls.Add(formats,0,1);
   var details=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,AccessibleName="Log inspection",Text="Check the file before importing. The check does not change the puzzle, work or journal."};grid.Controls.Add(details,0,2);grid.Controls.Add(MacroWorkContext(),0,3);
   var row=ToolRow();var inspect=ToolButton("Check file");var apply=ToolButton("Import log");var cancel=ToolButton("Cancel");apply.Enabled=false;cancel.DialogResult=DialogResult.Cancel;row.Controls.Add(inspect);row.Controls.Add(apply);row.Controls.Add(cancel);grid.Controls.Add(row,0,4);dialog.CancelButton=cancel;dialog.ActiveControl=inspect;
   Dictionary<string,object> receipt=null;bool sending=false,stopping=false;
   formats.SelectedIndexChanged+=delegate{receipt=null;apply.Enabled=false;details.Text="Format changed. Check the file again.";};
   Action stop=delegate{if(!sending||stopping)return;stopping=true;cancel.Enabled=false;details.Text="Stop requested; waiting for the authoritative import/check result.";StopAnalysis();};
   cancel.Click+=delegate{if(sending)stop();};dialog.FormClosing+=delegate(object sender,FormClosingEventArgs args){if(sending&&args.CloseReason==CloseReason.UserClosing){args.Cancel=true;stop();}};
   Func<bool,Task> run=async importing=>{
    if(sending)return;if(!IsReady){details.Text=StopFailure??"Wait for the current action to finish.";return;}if(importing&&receipt==null)return;
    sending=true;stopping=false;inspect.Enabled=apply.Enabled=formats.Enabled=false;cancel.Text="Stop check";cancel.DialogResult=DialogResult.None;
    try{
     var bytes=await Task.Factory.StartNew(()=>ReadSessionLogFile(path));if(stopping)return;
     var body=LocalApi.D("action",importing?"session-log-apply":"session-log-inspect","format",((Choice)formats.SelectedItem).Id,"data_base64",Convert.ToBase64String(bytes));if(importing)body["confirmation_id"]=receipt["confirmation_id"];
     bool accepted=await SendSessionLog(body);if(dialog.IsDisposed)return;
     if(!accepted){receipt=null;details.Text=feedback.Text+"\r\nCheck the current file and state again before importing.";return;}
     receipt=lastCommandResult;if(importing){if(!Object.Equals(Value(receipt,"import_applied"),true))throw new InvalidDataException("No authoritative import receipt was returned; inspect Session before retrying.");string warning=Text(Value(receipt,"warning"));if(StopFailure!=null)warning+=" "+StopFailure;Say("Log imported. Previous work: "+Text(Value(receipt,"import_checkpoint"))+(warning.Length>0?" · "+warning:""),warning.Length>0);sending=false;dialog.Close();}
     else details.Text=SessionLogInspection(receipt);
    }catch(Exception error){receipt=null;if(!dialog.IsDisposed)details.Text=error.Message;}
    finally{sending=false;if(!dialog.IsDisposed){inspect.Enabled=formats.Enabled=true;apply.Enabled=IsReady&&receipt!=null&&Value(receipt,"confirmation_id")!=null;cancel.Text="Cancel";cancel.Enabled=true;cancel.DialogResult=DialogResult.Cancel;details.Select(0,0);}}
   };
   inspect.Click+=async delegate{await run(false);};apply.Click+=async delegate{await run(true);};ShowOwned(dialog);
  }
 }
 async void ChooseSessionLogExport(){
  try{using(var picker=new SaveFileDialog{Title="Export session log",Filter="C600 proof log (*.c600.json.gz)|*.c600.json.gz|MPUlt log (*.log)|*.log",DefaultExt="c600.json.gz",AddExtension=true,OverwritePrompt=true,FileName="Magic600-session.c600.json.gz"}){if(MacroFileDialog(picker)!=DialogResult.OK)return;await ExportSessionLog(picker.FileName,picker.FilterIndex==2?"mpult":"c600");}}
  catch(Exception error){if(!closing)Say(error.Message,true);}
 }
 internal async Task<bool> ExportSessionLog(string path,string format){
  if(!await SendSessionLog(LocalApi.D("action","session-log-export","format",format))||closing)return false;
  var result=lastCommandResult;byte[] bytes=Convert.FromBase64String(Text(Value(result,"data_base64")));string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
  if(hash!=Text(Value(result,"file_sha256"))||bytes.Length!=Convert.ToInt64(Value(result,"bytes")))throw new InvalidDataException("Export payload does not match the verified engine result.");
  string target=Path.GetFullPath(path),temporary=Path.Combine(Path.GetDirectoryName(target),".magic600-log-"+Guid.NewGuid().ToString("N")+".tmp");
  try{using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}if(File.Exists(target))File.Replace(temporary,target,null);else File.Move(temporary,target);}finally{if(File.Exists(temporary))File.Delete(temporary);}
  Say("Exported "+format+" log · "+Path.GetFileName(target)+(StopFailure==null?"":" · "+StopFailure),StopFailure!=null);return true;
 }
 async void ShowSessionReport(bool completion){
  if(sessionDialog!=null&&!sessionDialog.IsDisposed){sessionDialog.Activate();return;}
  try{
   if(!await SendSession(LocalApi.D("action","session-report"))||closing)return;
   var data=SessionData;if(completion){var saved=Map(Value(data,"recorded_completion"))??Map(Value(data,"completion"));if(saved==null){Say("No recorded whole-solve completion is available at this journal step.",true);return;}ShowRecordedSessionCompletion(saved);return;}
   string nextAction=null;
   using(var dialog=ToolDialog("Session",630,520)){
    dialog.MinimumSize=new Size(570,480);sessionDialog=dialog;var grid=ToolLayout(dialog,42,-1,88,MacroContextHeight,44);
    var title=ToolLabel("Continue saved work, prepare a scramble, or start a new solve.");title.AutoEllipsis=false;grid.Controls.Add(title,0,0);
    sessionReportText=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,Text=SessionReportBody(data,false),AccessibleName="Authoritative session report"};grid.Controls.Add(sessionReportText,0,1);
    var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true,Margin=Padding.Empty};
    foreach(var item in new[]{new Choice("session-resume","Resume"),new Choice("session-new","New solve…"),new Choice("scramble","Scramble…"),new Choice("reset","Reset puzzle…"),new Choice("reset-view","Reset view"),new Choice("reset-workspace","Reset workspace…"),new Choice("session-timer-start","Start timer"),new Choice("session-timer-pause","Pause timer"),new Choice("session-summary","Last completion")}){
     string id=item.Id;var button=ToolButton(item.Label);button.AccessibleName=item.Label;button.Click+=delegate{nextAction=id;dialog.Close();};actions.Controls.Add(button);
    }
    grid.Controls.Add(actions,0,2);actions.SizeChanged+=delegate{grid.RowStyles[2].Height=actions.GetPreferredSize(new Size(actions.ClientSize.Width,0)).Height;};grid.Controls.Add(MacroWorkContext(),0,3);
    var row=ToolRow();Button refresh=ToolButton("Refresh"),save=ToolButton("Save log"),importLog=ToolButton("Import…"),exportLog=ToolButton("Export…"),close=ToolButton("Close");close.DialogResult=DialogResult.Cancel;row.Controls.Add(refresh);row.Controls.Add(save);row.Controls.Add(importLog);row.Controls.Add(exportLog);row.Controls.Add(close);grid.Controls.Add(row,0,4);dialog.CancelButton=close;
    importLog.Click+=delegate{nextAction="session-import-log";dialog.Close();};exportLog.Click+=delegate{nextAction="session-export-log";dialog.Close();};
    bool refreshing=false;refresh.Click+=async delegate{if(refreshing)return;refreshing=true;refresh.Enabled=false;try{await SendSession(LocalApi.D("action","session-report"));if(!dialog.IsDisposed)DrawSessionState();}catch(Exception error){if(!dialog.IsDisposed)sessionReportText.Text=error.Message;}finally{refreshing=false;if(!dialog.IsDisposed)refresh.Enabled=true;}};
    save.Click+=delegate{nextAction="session-save-log";dialog.Close();};sessionReportText.Select(0,0);dialog.ActiveControl=refresh;dialog.Shown+=delegate{sessionReportText.Select(0,0);sessionReportText.ScrollToCaret();bool focused=refresh.Focus();NativeDiagnostics.Write("Session report Shown: selection="+sessionReportText.SelectionStart+"/"+sessionReportText.SelectionLength+" refresh.Focus="+focused+" focused="+refresh.Focused+" active="+(dialog.ActiveControl==null?"null":dialog.ActiveControl.GetType().Name));};ShowOwned(dialog);
   }
   sessionDialog=null;sessionReportText=null;if(nextAction!=null&&!closing)RunCommand(nextAction);
  }catch(Exception error){sessionDialog=null;sessionReportText=null;if(!closing)Say(error.Message,true);}
 }
 void ShowSessionReset(string scope){
  if(scope!="new"&&scope!="puzzle"&&scope!="workspace")throw new ArgumentException("Unknown reset scope.");
  string title=scope=="new"?"New solve":scope=="puzzle"?"Reset puzzle":"Reset workspace";
  string description=scope=="new"?"Start solved with the documented default view and a new timer attempt. Previous progress stays recoverable in a checkpoint. Current work, steps and pending preview are cleared.":scope=="puzzle"?"Return all puzzle labels to Home. Save a recovery checkpoint first. Keep view preferences, personal key sets and macros; clear the pending operation. This is not a completed solve.":"Clear Current, Next, working steps and work presentation. Keep the puzzle, journal, personal key sets and macros. This does not reset or solve the puzzle.";
  using(var dialog=ToolDialog(title,610,350)){
   dialog.MinimumSize=new Size(550,350);var grid=ToolLayout(dialog,105,-1,MacroContextHeight,44);var label=ToolLabel(description);label.AccessibleName="Session action scope";label.AutoEllipsis=false;grid.Controls.Add(label,0,0);FitToolTextRow(grid,label,0);
   var status=ToolLabel("Apply explicitly, or Cancel to keep everything as it is.");status.AutoEllipsis=false;grid.Controls.Add(status,0,1);grid.Controls.Add(MacroWorkContext(),0,2);
   var row=ToolRow();Button apply=ToolButton(scope=="new"?"Start solved":"Apply reset"),cancel=ToolButton("Cancel");cancel.DialogResult=DialogResult.Cancel;row.Controls.Add(apply);row.Controls.Add(cancel);grid.Controls.Add(row,0,3);dialog.CancelButton=cancel;bool saving=false,stopping=false;
   Action stop=delegate{if(!saving||stopping)return;stopping=true;cancel.Enabled=false;status.Text="Stop requested; waiting for the authoritative result.";StopAnalysis();};cancel.Click+=delegate{if(saving)stop();};dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(saving&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;stop();}};
   apply.Click+=async delegate{
    if(saving)return;if(!IsReady){status.Text=StopFailure??"Wait for the current action to finish.";return;}saving=true;apply.Enabled=false;cancel.Text="Stop check";cancel.DialogResult=DialogResult.None;
    try{bool accepted=await SendSession(scope=="new"?LocalApi.D("action","session-new"):LocalApi.D("action","session-reset","scope",scope));if(dialog.IsDisposed)return;if(accepted){saving=false;dialog.Close();}else status.Text=feedback.Text;}
    catch(Exception error){if(!dialog.IsDisposed)status.Text=error.Message;}
    finally{saving=false;if(!dialog.IsDisposed){stopping=false;apply.Enabled=IsReady;cancel.Enabled=true;cancel.Text="Cancel";cancel.DialogResult=DialogResult.Cancel;}}
   };ShowOwned(dialog);
  }
 }
 void ShowSessionScramble(){
  bool staged=false;
  using(var dialog=ToolDialog("Stage scramble",610,480)){
   dialog.MinimumSize=new Size(550,440);var grid=ToolLayout(dialog,50,44,44,36,-1,MacroContextHeight,44);
   var intro=ToolLabel("Random legal turns with a recorded seed. Stage first; inspect and execute through Solve. This is not uniform random-state sampling.");intro.AutoEllipsis=false;grid.Controls.Add(intro,0,0);FitToolTextRow(grid,intro,0);
   var lengthRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,Margin=Padding.Empty};lengthRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));lengthRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90));lengthRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
   var mode=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Scramble size"};mode.Items.AddRange(new object[]{"Short · 100 turns","Full · 1000 turns","Custom"});mode.SelectedIndex=0;var count=new NumericUpDown{Dock=DockStyle.Fill,Minimum=1,Maximum=10000,Value=100,AccessibleName="Scramble length"};lengthRow.Controls.Add(mode,0,0);lengthRow.Controls.Add(ToolLabel("Turns"),1,0);lengthRow.Controls.Add(count,2,0);grid.Controls.Add(lengthRow,0,1);mode.SelectedIndexChanged+=delegate{if(mode.SelectedIndex<2)count.Value=mode.SelectedIndex==0?100:1000;};count.ValueChanged+=delegate{if(mode.SelectedIndex<2&&count.Value!=(mode.SelectedIndex==0?100:1000))mode.SelectedIndex=2;};
   var seedRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty};seedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,90));seedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));seedRow.Controls.Add(ToolLabel("Seed"),0,0);var seed=new TextBox{Dock=DockStyle.Fill,AccessibleName="Scramble seed"};tips.SetToolTip(seed,"Leave blank for a recorded generated seed, or enter an integer to reproduce a scramble.");seedRow.Controls.Add(seed,1,0);grid.Controls.Add(seedRow,0,2);
   var replace=new CheckBox{Text="Replace current steps and pending preview",AutoSize=true,Dock=DockStyle.Fill,Checked=false,AccessibleName="Replace current work for scramble"};grid.Controls.Add(replace,0,3);
   var status=ToolLabel(SessionHasWork()?"Current work exists. Keep it, or explicitly allow replacement before staging.":"A blank seed is generated and recorded by the engine.");status.AutoEllipsis=false;grid.Controls.Add(status,0,4);grid.Controls.Add(MacroWorkContext(),0,5);
   var row=ToolRow();Button stage=ToolButton("Stage in Prepare"),cancel=ToolButton("Cancel");stage.AccessibleName="Stage scramble in Prepare";cancel.DialogResult=DialogResult.Cancel;row.Controls.Add(stage);row.Controls.Add(cancel);grid.Controls.Add(row,0,6);dialog.CancelButton=cancel;bool saving=false,stopping=false;
   Action stop=delegate{if(!saving||stopping)return;stopping=true;cancel.Enabled=false;status.Text="Stop requested; waiting for the staging result.";StopAnalysis();};cancel.Click+=delegate{if(saving)stop();};dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(saving&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;stop();}};
   stage.Click+=async delegate{
    if(saving)return;if(!IsReady){status.Text=StopFailure??"Wait for the current action to finish.";return;}long parsedSeed=0;bool hasSeed=seed.Text.Trim().Length>0;if(hasSeed&&!Int64.TryParse(seed.Text.Trim(),NumberStyles.Integer,CultureInfo.InvariantCulture,out parsedSeed)){status.Text="Enter an integer seed, or leave it blank. Your input is kept.";seed.Focus();return;}
    if(SessionHasWork()&&!replace.Checked){status.Text="Current steps or a preview exist. Explicitly allow replacement, or Cancel to retain them.";return;}
    saving=true;stage.Enabled=mode.Enabled=count.Enabled=seed.Enabled=replace.Enabled=false;cancel.Text="Stop check";cancel.DialogResult=DialogResult.None;
    try{bool accepted=await SendSession(LocalApi.D("action","session-scramble","count",Decimal.ToInt32(count.Value),"seed",hasSeed?(object)parsedSeed:null,"replace",replace.Checked));if(dialog.IsDisposed)return;if(accepted){staged=true;saving=false;dialog.Close();}else status.Text=feedback.Text;}
    catch(Exception error){if(!dialog.IsDisposed)status.Text=error.Message;}
    finally{saving=false;if(!dialog.IsDisposed){stopping=false;stage.Enabled=IsReady;mode.Enabled=count.Enabled=seed.Enabled=replace.Enabled=true;cancel.Enabled=true;cancel.Text="Cancel";cancel.DialogResult=DialogResult.Cancel;}}
   };dialog.Shown+=delegate{mode.Focus();};ShowOwned(dialog);
  }
  if(staged&&!closing){if(!IsReady){Say("Scramble staged. "+(StopFailure??"Wait for the current operation status before continuing."),true);return;}RunCommand("solve-prepare");Say("Scramble staged. Check the complete operation, preview it, then execute explicitly.");}
 }
 bool SessionCompletionMatches(Dictionary<string,object> receipt,Dictionary<string,object> current){
  return receipt!=null&&current!=null&&Text(Value(receipt,"id")).Length>0&&Text(Value(receipt,"id"))==Text(Value(current,"id"))&&Text(Value(receipt,"state_hash"))==Text(Value(work,"hash"))&&Text(Value(receipt,"state_hash"))==Text(Value(current,"state_hash"))&&Text(Value(receipt,"head"))==Text(Value(SessionData,"head"))&&Text(Value(receipt,"head"))==Text(Value(current,"head"));
 }
 internal void RequestSessionCompletion(){
  var receipt=Map(Value(SessionData,"completion"));if(closing||!SessionCompletionMatches(receipt,receipt)||shownSessionCompletions.Contains(Text(Value(receipt,"id"))))return;
  pendingSessionCompletion=Map(api.Parse(api.Json(receipt)));OnUi(()=>ShowSessionCompletion());
 }
 internal async void ShowSessionCompletion(){
  if(closing||modal||sessionCompletionOpening||!IsReady||pendingSessionCompletion==null)return;
  var completion=Map(Value(SessionData,"recorded_completion"));string id=Text(Value(pendingSessionCompletion,"id"));
  if(!SessionCompletionMatches(pendingSessionCompletion,completion)||shownSessionCompletions.Contains(id)){pendingSessionCompletion=null;return;}
  sessionCompletionOpening=true;
  try{
   var captured=Map(api.Parse(api.Json(completion)));
   if(Value(SessionData,"completion")!=null&&!await SendSession(LocalApi.D("action","session-completion-ack","id",id))){if(!closing)Say("Completion summary is pending. "+feedback.Text,true);return;}
   if(closing||modal||!IsReady)return;
   if(!SessionCompletionMatches(captured,Map(Value(SessionData,"recorded_completion")))){pendingSessionCompletion=null;return;}
   pendingSessionCompletion=null;shownSessionCompletions.Add(id);ShowRecordedSessionCompletion(captured);
  }
  catch(Exception error){if(!closing)Say(error.Message,true);}finally{sessionCompletionOpening=false;}
 }
 void ShowRecordedSessionCompletion(Dictionary<string,object> completion){
  string nextAction=null;
  using(var dialog=ToolDialog("Solve summary",640,500)){
   dialog.MinimumSize=new Size(570,450);var grid=ToolLayout(dialog,40,-1,MacroContextHeight,44);var title=ToolLabel("Recorded whole-model completion");title.AutoEllipsis=false;grid.Controls.Add(title,0,0);
   var detail=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Text=SessionReportBody(completion,true),AccessibleName="Certified completion report"};grid.Controls.Add(detail,0,1);grid.Controls.Add(MacroWorkContext(),0,2);
   var row=ToolRow();Button save=ToolButton("Save log"),review=ToolButton("Review work"),close=ToolButton("Close");save.Click+=delegate{nextAction="session-save-log";dialog.Close();};review.Click+=delegate{nextAction="solve-prepare";dialog.Close();};close.DialogResult=DialogResult.Cancel;row.Controls.Add(save);row.Controls.Add(review);row.Controls.Add(close);grid.Controls.Add(row,0,3);dialog.CancelButton=close;detail.Select(0,0);dialog.ActiveControl=close;dialog.Shown+=delegate{detail.Select(0,0);detail.ScrollToCaret();close.Focus();};ShowOwned(dialog);
  }
  if(nextAction!=null&&!closing)RunCommand(nextAction);
 }
}
