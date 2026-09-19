// Read-only graphical inspection of an explicitly chosen finite operation.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

internal sealed partial class ExperimentShell {
 string cycleScope="selected-macro",cycleRecipeKey,cycleMode="Current";
 int? cycleOrbit,cyclePosition,cycleAfter,cycleResidualAfter;
 int cycleEdgeOffset;
 readonly Stack<Tuple<int?,int?>> cyclePages=new Stack<Tuple<int?,int?>>();
 void RegisterCycles(){
  Register("bank-Cycles","Open the cycle inspection key set",()=>Send(LocalApi.D("action","bank","id","Cycles")));
  foreach(string modeName in new[]{"Current","Operation","After"}){string value=modeName;Register("cycles-"+value.ToLowerInvariant(),"Show "+value+" in the shared cycle view",()=>ChooseCycleMode(value));}
  Register("cycles-macro","Inspect selected macro cycles",()=>ChooseCycleScope("selected-macro"));
  Register("cycles-steps","Inspect composed Macro steps",()=>ChooseCycleScope("macro-steps"));
  Register("cycles-complete","Inspect all operation cycles",()=>ChooseCycleScope("complete"));
  Register("cycles-refresh","Refresh the chosen exact cycle",()=>Send(LocalApi.D("action","inspect-phase")));
  Register("cycles-next","Next cycle index page",()=>PageCycles(true));
  Register("cycles-previous","Previous cycle index page",()=>PageCycles(false));
  Register("cycles-more","Next part of the selected cycle",()=>PageCycleEdges(true));
  Register("cycles-back","Previous part of the selected cycle",()=>PageCycleEdges(false));
  hub.CycleScopeRequested+=ChooseCycleScope;
  hub.CycleModeRequested+=ChooseCycleMode;
  hub.CycleOrbitRequested+=orbit=>{cycleOrbit=orbit;ResetCyclePage();Send(LocalApi.D("action","inspect-phase"));};
  hub.CycleObjectRequested+=position=>{cyclePosition=position;cycleEdgeOffset=0;Send(LocalApi.D("action","inspect-phase"));};
 }
 void ResetCyclePage(){cyclePosition=null;cycleAfter=null;cycleResidualAfter=null;cycleEdgeOffset=0;cyclePages.Clear();}
 void ChooseCycleMode(string value){cycleMode=value;cycleOrbit=null;ResetCyclePage();if(value!="Operation")inspectedBoundary=value=="After"?"cleanup":"actual";Send(LocalApi.D("action","inspect-phase"));}
 void ChooseCycleScope(string value){cycleMode="Operation";cycleScope=value;ResetCyclePage();Send(LocalApi.D("action","inspect-phase"));}
 async Task<bool> InspectOperationEvidence(int orbit,int position){
  if(mode!="g2"||!IsReady){Say("Evidence inspection is unavailable while input is busy; nothing was queued.",true);return false;}
  if(orbit<0||orbit>=35||position<0||position>=177120)throw new ArgumentException("Evidence needs a canonical orbit and position.");
  string oldMode=cycleMode,oldScope=cycleScope,oldKey=cycleRecipeKey,oldBoundary=inspectedBoundary,oldContext=Text(Value(Map(Value(work,"review_context")),"id"));
  int? oldOrbit=cycleOrbit,oldPosition=cyclePosition,oldAfter=cycleAfter,oldResidualAfter=cycleResidualAfter;int oldOffset=cycleEdgeOffset;var oldPages=cyclePages.ToArray();
  cycleMode="Operation";cycleScope="complete";cycleOrbit=orbit;inspectedBoundary="actual";ResetCyclePage();cyclePosition=position;string requestedKey=cycleRecipeKey=CycleContextKey();
  bool accepted=await Send(LocalApi.D("action","inspect-position","position",position));
  var shown=hub.CycleProjection;accepted=accepted&&shown!=null&&Object.Equals(Value(shown,"projection_available"),true)&&Object.Equals(Value(shown,"requested_position"),position);
  if(!accepted&&!closing&&cycleMode=="Operation"&&cycleScope=="complete"&&cycleOrbit==orbit&&cyclePosition==position&&cycleRecipeKey==requestedKey){
   cycleMode=oldMode;cycleScope=oldScope;cycleOrbit=oldOrbit;cycleRecipeKey=oldKey;inspectedBoundary=oldBoundary;ResetCyclePage();
   if(Text(Value(Map(Value(work,"review_context")),"id"))==oldContext){cyclePosition=oldPosition;cycleAfter=oldAfter;cycleResidualAfter=oldResidualAfter;cycleEdgeOffset=oldOffset;foreach(var page in oldPages.Reverse())cyclePages.Push(page);}
   hub.SetCycleMode(cycleMode);hub.SetCycleProjection(null,"Evidence inspection was not accepted; refresh this scope.",cycleScope,cycleOrbit);
  }
  return accepted;
 }
 void PageCycles(bool forward){
  var detail=hub.CycleProjection;var index=Map(Value(detail,"cycle_index"));var residual=Map(Value(detail,"orientation_index"));
  if(forward){object next=Value(index,"next_after"),nextResidual=Value(residual,"next_after");if(next==null&&nextResidual==null){Say("No further cycles or orientation changes in this orbit.");return;}cyclePages.Push(Tuple.Create(cycleAfter,cycleResidualAfter));if(next!=null)cycleAfter=Number(next);if(nextResidual!=null)cycleResidualAfter=Number(nextResidual);}
  else {if(cyclePages.Count==0){Say("This is the first cycle page.");return;}var previous=cyclePages.Pop();cycleAfter=previous.Item1;cycleResidualAfter=previous.Item2;}
  cyclePosition=null;cycleEdgeOffset=0;Send(LocalApi.D("action","inspect-phase"));
 }
 void PageCycleEdges(bool forward){
  var selected=Map(Value(hub.CycleProjection,"selected"));if(selected==null){Say("Choose a cycle first.");return;}
  object next=Value(selected,"next_edge_offset");if(forward&&next==null){Say("This is the end of the cycle.");return;}
  cyclePosition=Number(selected["anchor"]);cycleEdgeOffset=forward?Number(next):Math.Max(0,Number(selected["edge_offset"])-32);Send(LocalApi.D("action","inspect-phase"));
 }
 object CycleRecipe(){
  if(cycleMode=="Current")return null;
  if(cycleMode=="Operation"&&cycleScope=="selected-macro"){var macro=Items(Value(work,"library")).Select(Map).FirstOrDefault(m=>Text(Value(m,"id"))==selectedMacro);return Value(macro,"recipe");}
  var draft=Map(Value(Workspace,"draft"));return cycleMode=="Operation"&&cycleScope=="macro-steps"?Value(draft,"macro"):new[]{"prepare","macro","cleanup"}.SelectMany(p=>Items(Value(draft,p))).ToArray();
 }
 string CycleContextKey(){return cycleMode+"|"+cycleScope+"|"+(cycleMode=="Operation"?selectedMacro:"")+"|"+(cycleOrbit??Number(Value(Workspace,"orbit")))+"|"+(cycleMode=="Current"?"":api.Json(CycleRecipe()));}
 void PrepareCycleRequest(Dictionary<string,object> body){
  if(mode!="g2")return;
  string action=Text(Value(body,"action"));if(action=="macro-effect"){cycleMode="Operation";cycleScope="selected-macro";}else if(action=="review"){cycleMode="Operation";cycleScope="complete";}else if(new[]{"commit","undo","redo","restore","reset","fixture"}.Contains(action)){cycleMode="Current";cycleOrbit=null;}
  if(cycleMode!="Operation"){inspectedBoundary=cycleMode=="After"?"cleanup":"actual";body["inspect_boundary"]=inspectedBoundary;}
  string key=CycleContextKey();
  if(cycleRecipeKey!=key||new[]{"orbit","next-activate","restore","reset","fixture","draft","insert-macro","inverse-cleanup","template-use","operation-new","operation-reuse","commit"}.Contains(action)||(action=="twist"||action=="native-word"||action=="turn")&&Text(Value(body,"destination"))!="live"){ResetCyclePage();cycleRecipeKey=key;}
  body["inspect_cycle"]=LocalApi.D("mode",cycleMode,"scope",cycleScope,"orbit",cycleOrbit,"position",cyclePosition,"cycle_after",cycleAfter,"residual_after",cycleResidualAfter,"edge_offset",cycleEdgeOffset);
  hub.SetCycleMode(cycleMode);
  hub.WithdrawCycleProjection();
 }
 void AdoptCycleInspection(Dictionary<string,object> reply){
  if(mode!="g2")return;var result=Map(Value(reply,"cycle_projection"));string error=Text(Value(reply,"cycle_projection_error"));if(error.Length==0)error=Text(Value(reply,"cycle_projection_note"));
  if(result!=null){
   var macro=Map(Value(result,"macro"));var chosen=Items(Value(work,"library")).Select(Map).FirstOrDefault(m=>Text(Value(m,"id"))==selectedMacro);
   bool current=Text(Value(result,"model"))==Text(Value(work,"model"))&&Text(Value(result,"source_hash"))==Text(Value(work,"hash"))&&Text(Value(result,"source_guard"))==Text(Value(work,"guard"))&&Text(Value(result,"operation_state"))==Text(Value(work,"operation_state"))&&Text(Value(result,"scope"))==cycleScope&&Number(Value(result,"orbit"))==(cycleOrbit??Number(Workspace["orbit"]));
   string context=Text(Value(Map(Value(work,"review_context")),"id"));current=current&&context.Length>0&&Text(Value(result,"review_context_id"))==context;
   current=current&&Text(Value(result,"mode"))==cycleMode;
   var actualFilter=Map(Value(result,"actual_piece_filter"));var currentFilter=Map(Value(work,"piece_filter"));current=current&&(actualFilter==null)==(currentFilter==null)&&Text(Value(actualFilter,"context_hash"))==Text(Value(currentFilter,"context_hash"));
   if(cycleMode=="Operation"&&cycleScope=="selected-macro")current=current&&chosen!=null&&macro!=null&&Text(Value(macro,"id"))==selectedMacro&&Text(Value(macro,"version"))==Text(Value(chosen,"version"));
   current=current&&result.ContainsKey("requested_position")&&Object.Equals(Value(result,"requested_position"),cyclePosition.HasValue?(object)cyclePosition.Value:null);
   var selected=Map(Value(result,"selected"));if(cyclePosition.HasValue)current=current&&selected!=null&&Number(Value(selected,"edge_offset"))==cycleEdgeOffset;
   if(!current){result=null;error="Cycle view is out of date; refresh this scope.";}
  }
  if(result!=null)cycleRecipeKey=CycleContextKey();
  hub.SetCycleProjection(result,error,cycleScope,cycleOrbit);
 }
}
