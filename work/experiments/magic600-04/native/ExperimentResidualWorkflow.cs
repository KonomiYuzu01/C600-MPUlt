// Read-only residual and journal details inside the existing Solve context.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 string ResidualStage(Dictionary<string,object> residual){
  var intent=Map(Value(residual,"suggested_intent"));string kind=Text(Value(intent,"kind"));
  return kind=="PlacePiece"?"Position cleanup":kind=="OrientPiece"?"Orientation cleanup":kind=="FinishBuffer"?"Buffer "+Text(Value(intent,"role"))+" cleanup":kind=="FinishOrbit"?"Exact completion check":"Check reference";
 }
 async void ShowResidualDetails(){
  try{
   if(!IsReady)throw new InvalidOperationException("Finish or stop the current check first.");
   if(!await Send(LocalApi.D("action","inspect-residual")))return;
   var bundle=lastCommandResult;await WaitForStopCompletion();if(closing||form.IsDisposed)return;
   if(StopFailure!=null)throw new InvalidOperationException(StopFailure);
   var residual=Map(Value(bundle,"current"));if(residual==null)throw new InvalidOperationException("No current residual result is available.");
   var origin=Map(Value(residual,"source"));if(Text(Value(bundle,"review_context_id"))!=Text(Value(Map(Value(work,"review_context")),"id"))||Text(Value(residual,"model"))!=Text(Value(work,"model"))||Text(Value(residual,"state_hash"))!=Text(Value(work,"hash"))||Number(Value(residual,"orbit"))!=Number(Workspace["orbit"])||Text(Value(origin,"kind"))!="Current")
    throw new InvalidOperationException("The residual reply is out of date. Inspect the current state again.");
   string context=Text(Value(bundle,"review_context_id"));var invariant=Map(Value(residual,"invariant_status"));
   var lines=new List<string>{OrbitName(Number(residual["orbit"])),ResidualStage(residual),"",
    "Position · nonbuffer "+Text(Value(residual,"P_nonbuffer"))+" / buffer "+Text(Value(residual,"P_buffer")),
    "At Home, orientation · nonbuffer "+Text(Value(residual,"O_nonbuffer"))+" / buffer "+Text(Value(residual,"O_buffer")),
    "Unknown frames · "+Text(Value(residual,"FrameUnknown")),"Exact orbit complete · "+Text(Value(residual,"ExactSolved")),"",
    "Invariant check · "+Text(Value(invariant,"status")),Text(Value(invariant,"reason")),
    "Necessary conditions do not certify a protection-safe solution. No macro or correction is selected."};
   foreach(var buffer in Items(Value(residual,"buffer_occupants")).Select(Map)){
    int position=Number(buffer["position"]);lines.Add("");lines.Add("Buffer "+Text(buffer["role"])+" · "+SolvePositionName(position));
    lines.Add("Identity at Home · "+Text(buffer["position_correct"])+" / exact labels · "+Text(buffer["exact_labels_correct"]));
    var row=Items(Value(residual,"orientation_residuals")).Concat(Items(Value(residual,"displaced_orientation"))).Select(Map).FirstOrDefault(r=>Number(r["position"])==position);
    if(row!=null){lines.Add("Frame · "+Text(row["orientation_status"]));if(Value(row,"orientation_element")!=null)lines.Add("Current · "+EndgameElementCaption(row["orientation_element"])+" / inverse · "+EndgameElementCaption(row["correction_element"]));}
   }
   var completion=Map(Value(residual,"completion"));lines.Add("");lines.Add("Completion checks · "+String.Join(", ",Items(Value(completion,"reasons")).Select(Text).ToArray()));
   string requested=null;
   using(var dialog=ToolDialog("Current residual",690,480)){
    var grid=ToolLayout(dialog,-1,42);grid.Controls.Add(new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,Text=String.Join("\r\n",lines.Where(x=>x!=null).ToArray()),AccessibleName="Current residual and necessary invariant checks"},0,0);
    var actions=ToolRow();foreach(string role in new[]{"a","b"}){string chosen=role;var button=ToolButton("Locate "+role.ToUpperInvariant());button.Click+=delegate{requested=chosen;dialog.Close();};actions.Controls.Add(button);}var close=ToolButton("Close");close.DialogResult=DialogResult.Cancel;actions.Controls.Add(close);dialog.CancelButton=close;grid.Controls.Add(actions,0,1);ShowOwned(dialog);
   }
   if(requested!=null){if(context!=Text(Value(Map(Value(work,"review_context")),"id")))throw new InvalidOperationException("Work changed; inspect the residual again.");InspectSolveRole(requested);HideWorkWindow("solve");}
  }catch(Exception error){if(!closing&&!form.IsDisposed)Say(error.Message,true);}
 }
 void ShowJournalDelta(){
  var receipt=Map(Value(work,"journal_delta"));var delta=Map(Value(receipt,"residual_delta"));
  if(delta==null)throw new InvalidOperationException("This journal event has no solver delta receipt.");
  var before=Map(delta["before"]);var after=Map(delta["after"]);
  string summary=OrbitName(Number(delta["orbit"]))+"\nPosition "+before["position"]+" → "+after["position"]+" · orientation at Home "+before["orientation"]+" → "+after["orientation"]+"\nProtected damage "+delta["protected_damage"];
  string head=Text(receipt["head"]),selected=null;
  using(var dialog=ToolDialog("Last committed step",700,390)){
   var grid=ToolLayout(dialog,78,-1,42);grid.Controls.Add(ToolLabel(summary),0,0);var list=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false,HorizontalScrollbar=true,AccessibleName="Changed fixed positions from the journal"};foreach(var row in Items(delta["changed_positions"]).Select(Map))list.Items.Add(new Choice(Text(row["position"]),Text(Value(row,"position_name"))));grid.Controls.Add(list,0,1);
   var actions=ToolRow();var locate=ToolButton("Locate position");locate.Enabled=false;list.SelectedIndexChanged+=delegate{locate.Enabled=list.SelectedItem!=null;};locate.Click+=delegate{selected=((Choice)list.SelectedItem).Id;dialog.Close();};actions.Controls.Add(locate);var close=ToolButton("Close");close.DialogResult=DialogResult.Cancel;actions.Controls.Add(close);dialog.CancelButton=close;grid.Controls.Add(actions,0,2);tips.SetToolTip(list,"First "+list.Items.Count+" of "+delta["changed_count"]+" changed positions. Full action remains in the journal; this is a compact endpoint summary.");ShowOwned(dialog);
  }
  if(selected!=null){if(head!=Text(Value(Map(Value(work,"journal_delta")),"head")))throw new InvalidOperationException("Journal head changed; inspect its current result again.");Send(LocalApi.D("action","inspect-position","position",Int32.Parse(selected)));HideWorkWindow("solve");}
 }
 void ChooseFollowingOrbit(){
  var suggestions=Items(Value(Workspace,"orbit_suggestions")).Select(Map).ToArray();
  if(suggestions.Length==0)throw new InvalidOperationException("No next-orbit suggestion is available from the last exact completion. Choose an orbit explicitly.");
  Choose("Last completion suggestions · inspect and choose",suggestions.Select(r=>new Choice(Text(r["orbit"]),OrbitName(Number(r["orbit"])))),id=>Send(LocalApi.D("action","orbit","orbit",Int32.Parse(id))));
 }
}
