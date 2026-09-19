// Explicit comparison of existing bodies with the current Prepare and Cleanup.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 bool candidateChecking;
 string candidateBaseText,candidateRenderedText,candidateBaseTip,candidateRenderedTip,candidateBaseDescription;

 Dictionary<string,object> CurrentCandidates(){
  var batch=Map(Value(work,"candidates"));string context=Text(Value(Map(Value(work,"review_context")),"id"));
  if(batch==null||context.Length==0||Text(Value(batch,"version"))!="fixed-phase-candidates-v1"||Text(Value(batch,"model"))!=Text(Value(work,"model"))||Text(Value(batch,"base_review_context_id"))!=context)return null;
  var rows=Items(Value(batch,"candidates")).Select(Map).ToArray();if(rows.Length==0||rows.Length>12)return null;
  var ids=new HashSet<string>();foreach(var row in rows){var binding=Map(Value(row,"candidate"));var recommendation=Map(Value(row,"recommendation"));var rowContext=Map(Value(row,"review_context"));string id=Text(Value(binding,"id"));if(id.Length==0||!ids.Add(id)||!CurrentMacroBinding(binding)||recommendation==null||rowContext==null||Text(Value(recommendation,"review_context_id"))!=Text(Value(rowContext,"id")))return null;}
  var order=Items(Value(batch,"order")).Select(Text).ToArray();if(order.Length!=ids.Count||order.Distinct().Count()!=ids.Count||order.Any(id=>!ids.Contains(id)))return null;
  return batch;
 }
 Dictionary<string,object> CandidateRow(Dictionary<string,object> record){
  var batch=CurrentCandidates();if(batch==null||record==null)return null;
  return Items(Value(batch,"candidates")).Select(Map).FirstOrDefault(row=>SameMacroBinding(Map(Value(row,"candidate")),record));
 }
 IEnumerable<Dictionary<string,object>> CandidateOrder(IEnumerable<Dictionary<string,object>> records){
  var source=records.ToArray();var batch=CurrentCandidates();if(batch==null)return source;
  var order=Items(Value(batch,"order")).Select(Text).Select((id,index)=>new{Id=id,Index=index}).ToDictionary(x=>x.Id,x=>x.Index);
  return source.OrderBy(record=>order.ContainsKey(Text(Value(record,"id")))?order[Text(Value(record,"id"))]:Int32.MaxValue).ToArray();
 }
 string CandidateCaption(Dictionary<string,object> record){
  var row=CandidateRow(record);return row==null?MacroUseSummary(record):"Current · "+RecommendationCaption(Map(Value(row,"recommendation")));
 }
 string CandidateDetails(Dictionary<string,object> row){
  if(row==null)return "";var result=Map(Value(row,"recommendation"));
  var lines=new List<string>{"Candidate comparison · "+RecommendationCaption(result),"Current Prepare + this alternate Macro + current Cleanup.","The draft, selected macro and staged preview were not replaced."};
  lines.AddRange(Items(Value(result,"reasons")).Take(3).Select(reason=>Text(Value(Map(reason),"text"))));
  lines.Add("Choose and Add explicitly, then review the new complete operation before execution.");return String.Join("\n",lines.ToArray());
 }
 void DrawCandidateCaption(){
  if(effectText==null||effectText.IsDisposed)return;
  // Draw and list filtering may both call this; never accumulate repeated text.
  if(effectText.Text==candidateRenderedText)effectText.Text=candidateBaseText??"";
  if(tips.GetToolTip(effectText)==candidateRenderedTip)tips.SetToolTip(effectText,candidateBaseTip??"");
  if(effectText.AccessibleDescription==candidateRenderedTip)effectText.AccessibleDescription=candidateBaseDescription??"";
  candidateBaseText=effectText.Text;candidateBaseTip=tips.GetToolTip(effectText);candidateBaseDescription=effectText.AccessibleDescription;candidateRenderedText=candidateRenderedTip=null;
  var record=Items(Value(work,"library")).Select(Map).FirstOrDefault(m=>Text(Value(m,"id"))==selectedMacro);
  var row=selectedEffectScope=="body"?CandidateRow(record):null;
  if(row!=null){var recommendation=Map(Value(row,"recommendation"));string explanation=CandidateDetails(row);candidateRenderedText=candidateBaseText+"\n\n"+RecommendationCaption(recommendation)+"\nCandidate · fixed Prepare / Cleanup";candidateRenderedTip=(candidateBaseTip.Length==0?"":candidateBaseTip+"\n\n")+explanation;effectText.Text=candidateRenderedText;tips.SetToolTip(effectText,candidateRenderedTip);effectText.AccessibleDescription=candidateRenderedTip;}
  var batch=CurrentCandidates();int checkedCount=batch==null?0:Items(Value(batch,"candidates")).Length;
  string listTip=(checkedCount==0?"Compare up to 12 visible entries.":"Last "+checkedCount+" checked; others unchecked.")+"\nSelect inspects; Add changes the draft.\nFilter to compare another set.";
  if(row!=null)listTip+="\n\n"+CandidateDetails(row);tips.SetToolTip(macros,listTip);
 }
 async void CheckVisibleCandidates(){
  try{
   if(!IsReady||candidateChecking)throw new InvalidOperationException(StopFailure??"Finish or stop the current check first. No comparison was queued.");
   var visible=macros.Items.Cast<Choice>().Select(choice=>choice.Id).ToArray();if(visible.Length==0)throw new InvalidOperationException("No visible macros match. Change the search or filter before comparing.");
   var library=Items(Value(work,"library")).Select(Map).ToDictionary(record=>Text(Value(record,"id")));
   var chosen=visible.Take(12).Select(id=>{Dictionary<string,object> record;if(!library.TryGetValue(id,out record))throw new InvalidOperationException("A visible macro changed. Refresh Macro Base before comparing.");return BoundMacro(record);}).ToArray();
   string context=Text(Value(Map(Value(work,"review_context")),"id"));candidateChecking=true;
   Say("Comparing "+chosen.Length+" of "+visible.Length+" visible macros with fixed Prepare / Cleanup. Stop remains available.");
   bool accepted=await Send(LocalApi.D("action","macro-candidates","candidates",chosen));var receipt=lastCommandResult;await WaitForStopCompletion();
   if(closing||form.IsDisposed)return;if(!accepted){Say(StopFailure??feedback.Text,true);return;}
   var batch=CurrentCandidates();var returned=Items(Value(receipt,"candidates")).Select(Map).ToArray();
   if(batch==null||Text(Value(receipt,"base_review_context_id"))!=context||Text(Value(batch,"cache_key"))!=Text(Value(receipt,"cache_key"))||returned.Length!=chosen.Length||returned.Where((row,index)=>!SameMacroBinding(chosen[index],Map(Value(row,"candidate")))).Any())throw new InvalidOperationException("The work or macro revisions changed. Comparison was not adopted; compare the current entries again.");
   DrawMacros();DrawCandidateCaption();QueueSolveLayout();
   Say("Compared "+chosen.Length+" of "+visible.Length+" visible macros · fixed Prepare / Cleanup. Select and Add explicitly; draft and preview unchanged."+(visible.Length>chosen.Length?" Filter to another set for the next batch.":""));
   if(StopFailure!=null)Say(StopFailure,true);
  }catch(Exception error){if(!closing&&!form.IsDisposed){Say(error.Message,true);NativeDiagnostics.Write("Candidate comparison rejected",error);}}
  finally{candidateChecking=false;}
 }
}
