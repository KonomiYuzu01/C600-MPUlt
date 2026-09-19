// Explicitly selected macro relationships and variants; no solving or execution authority.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 Dictionary<string,object> BoundMacro(Dictionary<string,object> record){return LocalApi.D("id",record["id"],"version",record["version"],"recipe",api.Parse(api.Json(record["recipe"])));}
 bool SameMacroBinding(Dictionary<string,object> bound,Dictionary<string,object> record){return bound!=null&&record!=null&&Text(Value(bound,"id"))==Text(Value(record,"id"))&&Text(Value(bound,"version"))==Text(Value(record,"version"))&&api.Json(Value(bound,"recipe"))==api.Json(Value(record,"recipe"));}
 bool CurrentMacroBinding(Dictionary<string,object> bound){return Items(Value(work,"library")).Select(Map).Any(record=>SameMacroBinding(bound,record));}
 string MacroPairScope(object ids){var values=Items(ids).Select(Number).ToArray();return values.Length==0?"None":String.Join("\r\n",values.Select(OrbitName).ToArray());}
 bool ComparisonMatches(Dictionary<string,object> result,Dictionary<string,object> a,Dictionary<string,object> b){
  if(result==null||Text(Value(result,"model"))!=Text(Value(work,"model"))||!(Value(result,"full_equal") is bool)||!(Value(result,"inverse") is bool)||Value(result,"equal_active_orbits")==null||Value(result,"both_inactive_orbits")==null||Value(result,"different_orbits")==null)return false;
  return SameMacroBinding(a,Map(Value(Map(Value(result,"a")),"record")))&&SameMacroBinding(b,Map(Value(Map(Value(result,"b")),"record")));
 }
 string MacroComparisonText(Dictionary<string,object> result){
  var a=Map(Value(result,"a"));var b=Map(Value(result,"b"));var ra=Map(Value(a,"record"));var rb=Map(Value(b,"record"));
  int currentOrbit=Number(Workspace["orbit"]);bool inactive=Items(Value(result,"both_inactive_orbits")).Select(Number).Contains(currentOrbit),same=Items(Value(result,"equal_active_orbits")).Select(Number).Contains(currentOrbit);
  var lines=new List<string>{MacroName(ra)+" · revision "+Text(Value(ra,"version")),MacroName(rb)+" · revision "+Text(Value(rb,"version")),"","Whole puzzle: "+(Object.Equals(Value(result,"full_equal"),true)?"same net action":"different net actions"),"Reverse action: "+(Object.Equals(Value(result,"inverse"),true)?"exact inverses":"not inverses"),"This orbit: "+(inactive?"neither macro acts here":same?"same net action":"different net actions"),OrbitName(currentOrbit),"","Turns: "+Text(Value(a,"primitives"))+" / "+Text(Value(b,"primitives")),"Net action only. Intermediate motion and complete-operation protection need their own check.","","Same action on affected orbits:",MacroPairScope(Value(result,"equal_active_orbits")),"","Different action:",MacroPairScope(Value(result,"different_orbits")),"","Neither acts:",MacroPairScope(Value(result,"both_inactive_orbits")),"","First affected orbits:",MacroPairScope(Value(a,"affected_orbits")),"","Second affected orbits:",MacroPairScope(Value(b,"affected_orbits")),"","Scope: "+Text(Value(result,"scope")),Text(Value(result,"note")),"Canonical macros: "+Text(Value(ra,"id"))+" / "+Text(Value(rb,"id")),"Model: "+Text(Value(result,"model"))};
  return String.Join("\r\n",lines.ToArray());
 }
 TabPage MacroComparePage(Form dialog,Dictionary<string,object> source){
  var page=new TabPage("Compare");var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(6),Margin=Padding.Empty};foreach(int height in new[]{38,40,0,42})grid.RowStyles.Add(new RowStyle(height==0?SizeType.Percent:SizeType.Absolute,height==0?100:height));page.Controls.Add(grid);
  var sourceLabel=ToolLabel("Compare "+MacroShortName(source)+" · revision "+Text(Value(source,"version")));sourceLabel.AutoEllipsis=false;grid.Controls.Add(sourceLabel,0,0);FitToolTextRow(grid,sourceLabel,0);
  var other=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Second macro to compare"};other.Items.Add(new Choice("","Choose another macro"));var records=Items(Value(work,"library")).Select(Map).ToDictionary(record=>Text(record["id"]),record=>Map(api.Parse(api.Json(record))),StringComparer.Ordinal);foreach(var pair in records)other.Items.Add(new Choice(pair.Key,MacroName(pair.Value)+" · revision "+Text(Value(pair.Value,"version"))));other.SelectedIndex=0;grid.Controls.Add(other,0,1);
  var resultText=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,AccessibleName="Exact macro comparison",Text="Choose a second saved macro, then check the pair. No operation is inserted or executed."};grid.Controls.Add(resultText,0,2);
  var row=ToolRow();var check=ToolButton("Check comparison");check.Enabled=false;row.Controls.Add(check);grid.Controls.Add(row,0,3);var bound=BoundMacro(source);bool checking=false,stopRequested=false;int request=0;
  other.SelectedIndexChanged+=delegate{request++;var selected=other.SelectedIndex>0?records[((Choice)other.SelectedItem).Id]:null;resultText.Text=selected==null?"Choose a second macro.":MacroName(selected)+" · revision "+Text(Value(selected,"version"))+"\r\nCanonical macro: "+Text(Value(selected,"id"))+"\r\nPair not checked.";tips.SetToolTip(other,resultText.Text);check.Enabled=!checking&&other.SelectedIndex>0;};
  check.Click+=async delegate{
   if(checking){if(!stopRequested){stopRequested=true;check.Enabled=false;check.Text="Stopping…";resultText.Text="Stop requested; waiting for the checked result.";StopAnalysis();}return;}if(other.SelectedIndex<=0)return;if(!IsReady){resultText.Text=StopFailure??"Wait for the current check and stop request to finish.";return;}string id=((Choice)other.SelectedItem).Id;var second=BoundMacro(records[id]);int serial=++request;
   if(!CurrentMacroBinding(bound)||!CurrentMacroBinding(second)){resultText.Text="A saved macro changed. Close and reopen Details to compare its current revision.";return;}
   checking=true;stopRequested=false;check.Text="Stop check";check.Enabled=true;other.Enabled=false;resultText.Text="Checking this exact pair…";
   try{
    bool accepted=await Send(LocalApi.D("action","macro-compare","a",bound,"b",second));var result=lastCommandResult;await WaitForStopCompletion();if(dialog.IsDisposed||dialog.Disposing)return;
    if(serial!=request||other.SelectedIndex<=0||((Choice)other.SelectedItem).Id!=id){resultText.Text="Selection changed; comparison discarded.";return;}
    if(!accepted){resultText.Text=feedback.Text;return;}
    if(!CurrentMacroBinding(bound)||!CurrentMacroBinding(second)||!ComparisonMatches(result,bound,second)){resultText.Text="Comparison no longer matches the named revisions. Reopen Details and check again.";return;}
    resultText.Text=MacroComparisonText(result)+(StopFailure==null?"":"\r\n\r\n"+StopFailure);
   }catch(Exception error){if(!dialog.IsDisposed&&!dialog.Disposing)resultText.Text=error.Message;}
   finally{checking=false;stopRequested=false;if(!dialog.IsDisposed&&!dialog.Disposing){other.Enabled=true;check.Text="Check comparison";check.Enabled=other.SelectedIndex>0&&IsReady;}}
  };
  return page;
 }
 void ShowMacroComparison(){ShowSelectedMacroDetails(true);}
 void ShowMacroVariant(string kind){ShowMacroVariant(kind,SelectedMacroRecord());}
 void ShowMacroVariant(string kind,Dictionary<string,object> source){
  if(kind!="inverse"&&kind!="reference")throw new ArgumentException("Choose inverse or the current explicit reference.");
  var bound=BoundMacro(source);var reference=api.Parse(api.Json(Value(Workspace,"reference")));string referenceKey=api.Json(reference),selectId=null;Dictionary<string,object> savedRecord=null;
  using(var dialog=ToolDialog(kind=="inverse"?"Save inverse macro":"Save macro with R",700,480)){
   dialog.MinimumSize=new Size(610,430);var grid=ToolLayout(dialog,58,38,kind=="reference"?70:0,-1,MacroContextHeight,44);
   var sourceLabel=ToolLabel(MacroShortName(source)+" · revision "+Text(source["version"])+"\n"+(kind=="inverse"?"Reverse the chosen fixed operation.":"Save the explicit sequence R⁻¹ / Macro / R."));sourceLabel.AutoEllipsis=false;grid.Controls.Add(sourceLabel,0,0);FitToolTextRow(grid,sourceLabel,0);tips.SetToolTip(sourceLabel,"Canonical source: "+source["id"]+"\nThe source recipe is retained unchanged.");
   string suffix=kind=="inverse"?" inverse":" with R",sourceName=MacroShortName(source);string suggested=(sourceName.Length+suffix.Length>80?sourceName.Substring(0,80-suffix.Length):sourceName)+suffix;
   var name=new TextBox{Dock=DockStyle.Fill,MaxLength=80,Text=suggested,AccessibleName="New macro name"};grid.Controls.Add(name,0,1);
   var r=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,AccessibleName="Explicit reference word",Text="R = "+referenceKey+"\r\nChosen legal turns; this does not certify a spatial frame."};r.Visible=kind=="reference";grid.Controls.Add(r,0,2);
   var status=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,AccessibleName="Saved macro result",Text="Save creates a separate entry. The selected macro, operation steps and work objects stay in place."};grid.Controls.Add(status,0,3);grid.Controls.Add(MacroWorkContext(),0,4);
   var row=ToolRow();Button save=ToolButton(kind=="inverse"?"Save inverse":"Save with R"),select=ToolButton("Select saved macro"),close=ToolButton("Cancel");select.Enabled=false;close.DialogResult=DialogResult.Cancel;row.Controls.Add(save);row.Controls.Add(select);row.Controls.Add(close);grid.Controls.Add(row,0,5);dialog.CancelButton=close;dialog.AcceptButton=save;bool saving=false,completed=false,stopRequested=false;
   Action requestStop=delegate{if(!saving||stopRequested)return;stopRequested=true;close.Enabled=false;close.Text="Stopping…";status.Text="Stop requested; waiting for the save result. A completed save will still be reported.";StopAnalysis();};
   close.Click+=delegate{if(saving)requestStop();};
   dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(saving&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;requestStop();}};
   save.Click+=async delegate{
    if(saving||completed)return;if(!IsReady){status.Text=StopFailure??"Wait for the current check and stop request to finish.";return;}
    if(!CurrentMacroBinding(bound)){status.Text="The source macro changed. Your name is kept; close and reopen this editor for its current revision.";return;}
    if(kind=="reference"&&referenceKey!=api.Json(Value(Workspace,"reference"))){status.Text="Reference R changed. Your name is kept; reopen this editor to inspect the new R before saving.";return;}
    if(String.IsNullOrWhiteSpace(name.Text)){status.Text="Give the saved macro a name.";name.Focus();return;}
    saving=true;stopRequested=false;name.Enabled=save.Enabled=select.Enabled=false;close.Enabled=true;close.DialogResult=DialogResult.None;close.Text="Stop check";status.Text="Saving the chosen fixed variant…";
    try{
     var payload=LocalApi.D("action","macro-variant","source",bound,"kind",kind,"name",name.Text.Trim());if(kind=="reference")payload["reference"]=reference;
     bool accepted=await Send(payload);var receipt=lastCommandResult;await WaitForStopCompletion();if(dialog.IsDisposed||dialog.Disposing)return;
     if(!accepted){status.Text=feedback.Text;return;}completed=true;close.Text="Close";dialog.AcceptButton=null;
     var record=Map(Value(receipt,"record"));var comparison=Map(Value(receipt,"comparison"));string id=Text(Value(receipt,"created_id"));
     if(id.Length==0||record==null||Text(Value(record,"id"))!=id||!CurrentMacroBinding(BoundMacro(record))||!ComparisonMatches(comparison,bound,BoundMacro(record))){status.Text="The save returned an unexpected receipt. Selection and draft were kept; inspect Macro Base before retrying.";return;}
     savedRecord=record;selectId=id;status.Text="Saved "+MacroName(record)+" · revision "+Text(Value(record,"version"))+"\r\nSelect saved macro to use this entry. It has not been inserted.\r\nCanonical macro: "+id+"\r\n\r\n"+MacroComparisonText(comparison);string warning=Text(Value(receipt,"warning"));if(warning.Length>0)status.Text+="\r\n"+warning;if(StopFailure!=null)status.Text+="\r\n\r\n"+StopFailure;select.Enabled=IsReady;
    }catch(Exception error){if(!dialog.IsDisposed&&!dialog.Disposing)status.Text=error.Message;}
    finally{saving=false;if(!dialog.IsDisposed&&!dialog.Disposing){close.Enabled=true;close.DialogResult=DialogResult.Cancel;close.Text=completed?"Close":"Cancel";save.Enabled=!completed&&IsReady;name.Enabled=!completed;}}
   };
   string chosen=null;select.Click+=delegate{if(saving||savedRecord==null||selectId==null)return;if(!IsReady){status.Text=StopFailure??"Wait for the current check and stop request to finish.";return;}if(!CurrentMacroBinding(BoundMacro(savedRecord))){status.Text="The saved entry changed. Reopen Macro Base before selecting its current revision.";return;}chosen=selectId;dialog.Close();};dialog.Shown+=delegate{name.Focus();name.SelectAll();};ShowOwned(dialog);selectId=chosen;
  }
  if(selectId!=null&&!closing){if(savedRecord==null||!CurrentMacroBinding(BoundMacro(savedRecord)))throw new InvalidOperationException("The saved macro is no longer the inspected revision. Choose it again from Macro Base.");selectedMacro=selectId;selectedEffect=null;selectedApplicability=null;ShowCatalogue(true);Send(LocalApi.D("action","macro-effect","id",selectId,"select",true));}
 }
}
