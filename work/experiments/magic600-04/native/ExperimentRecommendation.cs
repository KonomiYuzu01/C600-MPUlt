// Presentation of the existing complete-operation review; never a solver or permit.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 FlowLayoutPanel solveRecommendation;
 Label recommendationCaption;
 readonly List<Button> recommendationReasons=new List<Button>();
 Dictionary<string,object> CurrentRecommendation(){
  var review=Map(Value(work,"review"));var result=Map(Value(review,"recommendation"));
  string context=Text(Value(Map(Value(work,"review_context")),"id"));
  return review!=null&&Text(Value(review,"status"))!="Stale"&&result!=null&&context.Length>0&&Text(Value(result,"review_context_id"))==context?result:null;
 }
 static string RecommendationCaption(Dictionary<string,object> result){
  if(result==null)return "Current use · check complete operation";
  string category=Text(Value(result,"category"));string caption=category=="Direct"?"Direct benefit":category=="Blocked"?"Blocked by protection":category=="NeedsVerification"?"Needs verification":Object.Equals(Value(result,"related"),true)?"Potential preparation":"No direct benefit";
  var score=Value(result,"score");return caption+(score==null?"":" · score "+Convert.ToDouble(score).ToString("0.0",CultureInfo.InvariantCulture));
 }
 FlowLayoutPanel BuildRecommendation(){
  solveRecommendation=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=Padding.Empty};
  recommendationCaption=new Label{AutoSize=false,Padding=new Padding(4,2,4,2),Margin=Padding.Empty,AccessibleName="Current operation use, separate from execution permission"};solveRecommendation.Controls.Add(recommendationCaption);
  for(int i=0;i<3;i++){var button=Button("Reason "+(i+1),"review-reason-"+(i+1));button.AutoSize=false;button.TextAlign=ContentAlignment.MiddleLeft;button.Margin=new Padding(2);button.Padding=new Padding(5,2,5,2);button.AccessibleName="Inspect computed reason "+(i+1);recommendationReasons.Add(button);solveRecommendation.Controls.Add(button);}
  return solveRecommendation;
 }
 int FitRecommendation(int width){
  if(solveRecommendation==null)return 0;int total=0;foreach(Control control in solveRecommendation.Controls){if(!control.Visible)continue;control.Width=Math.Max(100,width-control.Margin.Horizontal);control.Height=control.GetPreferredSize(new Size(control.Width,Int32.MaxValue)).Height;total+=control.Height+control.Margin.Vertical;}return total;
 }
 void DrawRecommendation(){
  if(solveRecommendation==null)return;var result=CurrentRecommendation();recommendationCaption.Text=RecommendationCaption(result);tips.SetToolTip(recommendationCaption,"Heuristic for the explicitly chosen complete operation. Protection and Preview/Execute are checked separately.");
  var reasons=Items(Value(result,"reasons"));for(int i=0;i<recommendationReasons.Count;i++){var button=recommendationReasons[i];var reason=i<reasons.Length?Map(reasons[i]):null;button.Visible=reason!=null;button.Enabled=reason!=null&&IsReady;string hint=KeyHint("review-reason-"+(i+1));button.Text=reason==null?"":""+(i+1)+" · "+Text(Value(reason,"text"))+hint;button.AccessibleDescription=reason==null?"":button.Text;tips.SetToolTip(button,reason==null?"":Text(Value(reason,"text"))+"\nInspect measured evidence; Current and locked Next stay unchanged.");}
 }
 bool HasReviewReason(int index){return CurrentRecommendation()!=null&&Items(Value(CurrentRecommendation(),"reasons")).Length>index;}
 async void InspectReviewReason(int index){
  try{
   if(!IsReady)throw new InvalidOperationException("Finish or stop the current check first.");var result=CurrentRecommendation();var reasons=Items(Value(result,"reasons"));if(result==null||index<0||index>=reasons.Length)throw new InvalidOperationException("This reason is out of date. Check the complete operation again.");
   var reason=Map(reasons[index]);var records=Items(Value(reason,"objects")).Select(Map).Where(x=>x!=null).ToArray();if(records.Length==0){ShowReview();return;}
   string context=Text(Value(result,"review_context_id"));var first=records[0];
   if(!await InspectOperationEvidence(Number(first["orbit"]),Number(first["position"])))return;
   if(context!=Text(Value(Map(Value(work,"review_context")),"id")))throw new InvalidOperationException("The work changed before this evidence could be shown. Check again.");
   hub.SetReviewEvidence(context,records.Select(x=>Number(x["identity"])));
   HideWorkWindow("solve");
   int total=Value(reason,"objects_total")==null?records.Length:Number(reason["objects_total"]);Say(Text(Value(reason,"text"))+" · "+hub.ReviewEvidenceVisibleCount+" of "+total+" evidence identities loaded. Return to Solve"+KeyHint("operation-focus")+"; no turn executed.");FocusWorkspace();
  }catch(Exception error){Say(error.Message,true);NativeDiagnostics.Write("Review evidence inspection rejected",error);}
 }
 void ShowReviewFindings(List<string> lines){
  var result=CurrentRecommendation();string context=Text(Value(result,"review_context_id"));var reasons=Items(Value(result,"reasons"));
  if(result!=null){lines.Add("");lines.Add(RecommendationCaption(result));foreach(var raw in reasons)lines.Add(Text(Value(Map(raw),"text")));lines.Add("Heuristic only; protection decides whether the operation can be staged.");var terms=Map(Value(result,"terms"));if(terms!=null)lines.Add("Computed terms: "+String.Join("; ",terms.Select(x=>x.Key+"="+(x.Value==null?"unknown":Convert.ToDouble(x.Value).ToString("0.###",CultureInfo.InvariantCulture))).ToArray()));}
  var match=Map(Value(result,"match"));if(match!=null&&Text(Value(match,"status"))=="Unknown")lines.Add(Text(Value(match,"reason")));
  int chosen=-1;using(var dialog=ToolDialog("Operation check",710,430)){
   var grid=ToolLayout(dialog,-1,42);var text=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=Paper,ForeColor=Ink,BorderStyle=BorderStyle.None,Text=String.Join("\r\n",lines.ToArray()),AccessibleName="Read-only operation facts and score terms"};grid.Controls.Add(text,0,0);var row=ToolRow();
   for(int i=0;i<reasons.Length;i++){int index=i;var button=ToolButton("Evidence "+(i+1)+KeyHint("review-reason-"+(i+1)));button.Enabled=Items(Value(Map(reasons[i]),"objects")).Length>0;button.Click+=delegate{chosen=index;dialog.Close();};row.Controls.Add(button);}
   var close=ToolButton("Close");close.DialogResult=DialogResult.Cancel;row.Controls.Add(close);dialog.CancelButton=close;grid.Controls.Add(row,0,1);ShowOwned(dialog);
  }
  if(chosen>=0){if(CurrentRecommendation()==null||context!=Text(Value(CurrentRecommendation(),"review_context_id"))){Say("Work changed while findings were open. Check the complete operation again.",true);return;}RunCommand("review-reason-"+(chosen+1));}
 }
}
