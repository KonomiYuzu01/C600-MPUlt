// Read-only draft inspection is never a staged preview or execution permission.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 string inspectedBoundary="actual";
 long inspectionSerial;
 Dictionary<string,object> phaseInspection;
 string phaseInspectionReason;

 void RegisterPhaseInspection(){
  RegisterCycles();
  foreach(string boundary in new[]{"actual","prepare","macro","cleanup"}){
   string value=boundary;
   Register("inspect-"+value,value=="actual"?"Inspect actual committed pieces":"Inspect pieces after "+value,
    ()=>{if(OperationExecuted&&value!="actual")throw new InvalidOperationException("These steps were executed. Choose New operation or Reuse steps before inspecting a draft result.");cycleMode=value=="actual"?"Current":"Operation";inspectedBoundary=value;Send(LocalApi.D("action","inspect-phase"));});
  }
 }
 static string BoundaryName(string boundary){return boundary=="actual"?"Actual":boundary=="prepare"?"After Prepare":boundary=="macro"?"After Macro":"After Cleanup";}
 void ChooseInputPhase(string value){
  if(mode=="g2"&&Text(Workspace["input"])=="draft"&&!OperationExecuted)inspectedBoundary=value;
  Send(LocalApi.D("action","settings","phase",value));
 }
 void UseSelectedMacro(bool append){
  if(OperationExecuted)throw new InvalidOperationException("Choose New operation or Reuse steps before adding another macro.");
  if(mode=="g2")OpenWorkWindow("operation",false);else ShowOperation(true);
  RequireMacro();
  Send(LocalApi.D("action","insert-macro","id",selectedMacro,"phase",Text(phase),"append",append));
 }
 long PreparePhaseRequest(Dictionary<string,object> body){
  long serial=++inspectionSerial;if(mode!="g2")return serial;
  if(new[]{"commit","cancel-preview","operation-new","operation-reuse"}.Contains(Text(Value(body,"action"))))inspectedBoundary="actual";
  body["inspect_boundary"]=inspectedBoundary;body["inspect_macro"]=selectedMacro;
  PrepareCycleRequest(body);
  // Withdraw forecasts before a request can edit their inputs. Never leave old
  // projected occupants looking current while asynchronous work is in flight.
  phaseInspection=null;phaseInspectionReason="Updating "+BoundaryName(inspectedBoundary)+"; committed pieces remain actual.";hub.ClearPhaseInspection(phaseInspectionReason);
  return serial;
 }
 void AdoptPhaseInspection(Dictionary<string,object> reply,long serial){
  if(mode!="g2"||serial!=inspectionSerial)return;
  AdoptCycleInspection(reply);
  var result=Map(Value(reply,"phase_inspection"));
  if(!PhaseInspectionMatches(result,work,inspectedBoundary,selectedMacro)){
   phaseInspection=null;string error=Text(Value(reply,"phase_inspection_error"));phaseInspectionReason=error.Length>0?"Draft inspection failed: "+error:"Draft inspection unavailable or out of date; no forecast is current.";return;
  }
  phaseInspection=result;phaseInspectionReason=null;
 }
 internal static bool PhaseInspectionMatches(Dictionary<string,object> result,Dictionary<string,object> state,string boundary,string macro){
  if(result==null||state==null||Text(Value(result,"boundary"))!=boundary||Text(Value(result,"macro_id"))!=Text(macro)||Text(Value(result,"source_hash"))!=Text(Value(state,"hash"))||Text(Value(result,"source_guard"))!=Text(Value(state,"guard"))||Text(Value(result,"operation_state"))!=Text(Value(state,"operation_state")))return false;
  var currentFilter=Map(Value(state,"piece_filter"));var sourceFilter=Map(Value(result,"actual_piece_filter"));
  string context=Text(Value(Map(Value(state,"review_context")),"id"));if(context.Length==0||Text(Value(result,"review_context_id"))!=context)return false;
  if((currentFilter==null)!=(sourceFilter==null)||Text(Value(currentFilter,"context_hash"))!=Text(Value(sourceFilter,"context_hash")))return false;
  bool executed=Text(Value(state,"operation_state"))=="executed";if(executed&&(boundary!="actual"||Value(result,"macro_entry")!=null))return false;
  var workspace=Map(Value(state,"workspace"));var expectedNext=Map(Value(workspace,"next"));var sourceNext=Map(Value(result,"source_next"));
  if(macro!=null&&!executed){var entry=Map(Value(result,"macro_entry"));var current=Items(Value(state,"library")).Select(Map).FirstOrDefault(m=>Text(Value(m,"id"))==macro);if(entry==null||current==null||Text(Value(entry,"id"))!=macro||Text(Value(entry,"version"))!=Text(Value(current,"version")))return false;}
  if((expectedNext==null)!=(sourceNext==null))return false;
  if(expectedNext!=null&&(Text(Value(expectedNext,"identity"))!=Text(Value(sourceNext,"identity"))||Text(Value(expectedNext,"target"))!=Text(Value(sourceNext,"target"))))return false;
  var inspected=Map(Value(result,"source_inspected"));
  return inspected!=null&&Text(Value(inspected,"identity"))==Text(Value(workspace,"inspected"))&&Text(Value(inspected,"position"))==Text(Value(workspace,"inspected_position"));
 }
}
