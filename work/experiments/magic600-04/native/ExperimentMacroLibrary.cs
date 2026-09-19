// Native catalogue predicates use only backend-issued classification memberships.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

internal static class NativeMacroFacets {
 static object Get(Dictionary<string,object> record,string key){object value;return record!=null&&record.TryGetValue(key,out value)?value:null;}
 static object[] Items(object value){return value as object[]??new object[0];}
 internal static bool MatchesUse(Dictionary<string,object> record,string filter,int activeOrbit){
  if(filter=="all")return true;
  var use=Get(record,"use") as Dictionary<string,object>;
  bool checkedUse=Object.Equals(Get(use,"status"),"Checked");
  if(filter=="unchecked")return !checkedUse;
  if(!checkedUse)return false;
  if(filter=="other")return Object.Equals(Get(use,"other"),true);
  int orbit=activeOrbit;if(filter!="current"&&!Int32.TryParse(filter,out orbit))return false;
  return Items(Get(use,"memberships")).Concat(Items(Get(use,"candidates"))).Select(value=>value as Dictionary<string,object>).Any(row=>Object.Equals(Get(row,"orbit"),orbit));
 }
 internal static bool Matches(Dictionary<string,object> record,string kind,int? affectedOrbit,string[] tags,bool pinnedOnly){
  if(pinnedOnly&&!Object.Equals(Get(record,"pinned"),true))return false;
  var actualTags=new HashSet<string>(Items(Get(record,"tag_keys")).Select(Convert.ToString),StringComparer.Ordinal);if(!tags.All(actualTags.Contains))return false;
  var classification=Get(record,"classification") as Dictionary<string,object>;object[] kinds=Items(Get(classification,"kinds"));
  if(affectedOrbit.HasValue){var scope=Items(Get(classification,"scoped")).Select(value=>value as Dictionary<string,object>).FirstOrDefault(row=>row!=null&&Object.Equals(Get(row,"orbit"),affectedOrbit.Value));if(scope==null)return false;kinds=Items(Get(scope,"kinds"));}
  else if(classification==null)kinds=new object[]{"unchecked"};
  return String.IsNullOrEmpty(kind)||kinds.Select(Convert.ToString).Contains(kind,StringComparer.Ordinal);
 }
}


internal sealed partial class ExperimentShell {
 string macroUseFilter="all";
 readonly ComboBox macroUsePicker=new ComboBox();
 Control BuildMacroUsePicker(){
  macroUsePicker.DropDownStyle=ComboBoxStyle.DropDownList;macroUsePicker.Width=220;macroUsePicker.FlatStyle=FlatStyle.Flat;macroUsePicker.AccessibleName="Likely solving use";
  PopulateMacroUses(macroUsePicker);macroUsePicker.SelectionChangeCommitted+=delegate{if(!updating&&macroUsePicker.SelectedItem!=null){macroUseFilter=((Choice)macroUsePicker.SelectedItem).Id;DrawMacros();}};
  tips.SetToolTip(macroUsePicker,"Likely solving use is inferred from checked mathematical effects. It does not select a macro or certify this insertion.");return macroUsePicker;
 }
 void PopulateMacroUses(ComboBox picker){
  picker.Items.Clear();picker.Items.Add(new Choice("all","All uses"));picker.Items.Add(new Choice("current","Current orbit"));picker.Items.Add(new Choice("other","Other"));picker.Items.Add(new Choice("unchecked","Not checked"));
  foreach(var profile in Items(Value(structure,"orbit_profiles")).Select(Map)){int id=Number(profile["orbit"]);picker.Items.Add(new Choice(id.ToString(),OrbitName(id)));}
  picker.SelectedIndex=0;for(int i=0;i<picker.Items.Count;i++)if(((Choice)picker.Items[i]).Id==macroUseFilter)picker.SelectedIndex=i;
 }
 string MacroUseSummary(Dictionary<string,object> m){
  var use=Map(Value(m,"use"));if(Text(Value(use,"status"))!="Checked")return Text(Value(use,"status"))=="AwaitingVerification"?"Reference needs verification":"Use not checked";
  var names=Items(Value(use,"memberships")).Select(Map).Select(row=>OrbitName(Number(row["orbit"]))).ToList();
  if(macroUseFilter!="all"&&macroUseFilter!="other"&&macroUseFilter!="unchecked"){
   int scope=Number(Value(Workspace,"orbit"));if(macroUseFilter!="current")Int32.TryParse(macroUseFilter,out scope);
   if(!Items(Value(use,"memberships")).Select(Map).Any(row=>Number(row["orbit"])==scope)&&Items(Value(use,"candidates")).Select(Map).Any(row=>Number(row["orbit"])==scope))return "Possible use · "+OrbitName(scope);
  }
  if(Object.Equals(Value(use,"other"),true))names.Add("Other");return "Likely use · "+String.Join(" / ",names.ToArray());
 }
 string MacroUseReason(string code){switch(code){
  case "pure_position_effect":return "Position cycles; no extra orientation";case "pure_orientation_effect":return "Orientation only";case "mixed_position_orientation":return "Positions and orientation change";case "coherent_frame_unavailable":return "Reference not verified";case "use_score_qualifies":return "Meets use threshold";case "score_below_threshold":return "Below use threshold";case "outside_best_band":return "Outside leading score band";case "membership_limit":return "Other leading uses shown";case "awaiting_reference_for_macro":return "Finish reference verification";case "no_score_qualifies":return "No orbit meets use threshold";
  case "verified_star_on_orbit":return "Verified orbit-local Star";case "reference_pure_cycle_on_orbit":return "Reference-neutral piece cycle";case "only_affected_orbit":return "Only this orbit changes";
  case "fixed_position_orientation_work":return "Orientation work at fixed positions";case "affected_orbit":return "This orbit changes";case "use_not_established":return "Specific solving use is not established";
  case "identity_effect":return "No net effect";case "no_specific_orbit_use_evidence":return "No specific orbit use established";case "exact_effect_not_checked":return "Check the fixed macro first";
  default:return code.Replace('_',' ');
 }}
 string MacroUseDetails(Dictionary<string,object> m){
  var use=Map(Value(m,"use"));var lines=new List<string>{MacroUseSummary(m)};
  lines.AddRange(Items(Value(use,"reasons")).Select(x=>MacroUseReason(Text(x))));
  foreach(var row in Items(Value(use,"memberships")).Concat(Items(Value(use,"candidates"))).Select(Map)){
   var terms=Map(Value(row,"contributions"));var points=Map(Value(terms,"points"));string score=Value(row,"score")==null?"not verified":Convert.ToDouble(row["score"]).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture);
   lines.Add(OrbitName(Number(row["orbit"]))+" · use "+score+": "+String.Join(", ",Items(Value(row,"reasons")).Select(x=>MacroUseReason(Text(x))).ToArray()));
   if(terms!=null)lines.Add("  "+Text(Value(terms,"n_o"))+" / "+Text(Value(terms,"N"))+" affected pieces · "+Text(Value(terms,"L"))+" turns. Points: "+String.Join(" + ",new[]{"T","J","C","E"}.Select(key=>key+" "+(Value(points,key)==null?"?":Convert.ToDouble(points[key]).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture))).ToArray()));
  }
  lines.Add("UseScore: structure 40% · concentration 25% · compactness 20% · cost 15%. At most 3 uses, at least 70 and within 10 of the best. Frame: "+Text(Value(use,"frame_version")));
  var context=Map(Value(m,"use_context"));var conflicts=Items(Value(context,"body_protected_orbits"));if(conflicts.Length>0)lines.Add("Body affects protected orbits: "+String.Join(" / ",conflicts.Select(x=>OrbitName(Number(x))).ToArray()));
  lines.Add("Use is an inference. Check complete Prepare / Macro / Cleanup before execution.");return String.Join("\n",lines.ToArray());
 }
 string macroFacetKind="";
 int? macroFacetOrbit;
 string[] macroFacetTags=new string[0];
 string[] macroFacetTagKeys=new string[0];
 bool macroFacetPinned;
 static readonly string[] MacroKinds={"","star","pure-position","pure-orientation","mixed","pure-piece-cycle","single-three-cycle","orientation","cross-orbit","identity","unchecked"};
 static string MacroKindName(string kind){switch(kind){case "pure-position":return "Position only";case "pure-orientation":return "Orientation only";case "mixed":return "Position + orientation";case "star":return "Verified star";case "pure-piece-cycle":return "One pure position cycle";case "single-three-cycle":return "Three-cycle in an orbit";case "orientation":return "Any orientation change";case "cross-orbit":return "Cross-orbit effect";case "identity":return "No net change";case "unchecked":return "Not checked";default:return "All exact action types";}}
 void RegisterMacroLibraryCommands(){
  Register("macro-check-library","Check existing macro effects for use classification",CheckMacroLibrary);
  Register("macro-filter","Filter macros by exact effects, tags and pins",ShowMacroFilters);
  Register("macro-label","Edit selected macro name, notes, tags and pin",ShowMacroMetadata);
  Register("macro-pin","Pin or unpin the selected macro",ToggleMacroPin);
  Register("macro-export","Export selected fixed macro to a local file",ExportSelectedMacro);
  Register("macro-import","Inspect a local macro file before adding entries",ImportMacros);
 }
 Dictionary<string,object> SelectedMacroRecord(){RequireMacro();var record=Items(Value(work,"library")).Select(Map).FirstOrDefault(m=>Text(Value(m,"id"))==selectedMacro);if(record==null)throw new InvalidOperationException("The selected macro is no longer in this library. Choose an existing entry.");return record;}
 async void CheckMacroLibrary(){
  if(!await Send(LocalApi.D("action","macro-check-library","limit",72))||closing)return;
  var result=lastCommandResult;int checkedCount=Items(Value(result,"checked_ids")).Length,remaining=Number(Value(result,"remaining_count")??0);
  Say(checkedCount+" macro effects checked · "+remaining+" remaining. "+(remaining>0?"Check library again to continue.":"Use groups updated; no macro selected or executed."));
 }
 bool MacroMatchesFacets(Dictionary<string,object> m){return NativeMacroFacets.Matches(m,macroFacetKind,macroFacetOrbit,macroFacetTagKeys,macroFacetPinned);}
 string MacroClassificationSummary(Dictionary<string,object> m){var facts=Map(Value(m,"classification"));var kinds=Items(Value(facts,"kinds")).Select(Text).ToArray();return kinds.Length==0?(Text(Value(Map(Value(m,"use")),"status"))=="Checked"?"Other exact effect":"Not checked"):String.Join(" · ",kinds.Select(MacroKindName).ToArray());}
 string MacroEmptyMessage(){
  var records=Items(Value(work,"library")).Select(Map).ToArray();
  int uncheckedCount=records.Count(m=>Text(Value(Map(Value(m,"use")),"status"))=="Unchecked"),frameCount=records.Count(m=>Text(Value(Map(Value(m,"use")),"status"))=="AwaitingVerification");
  bool checkedFilter=macroUseFilter!="all"&&macroUseFilter!="unchecked"||macroFacetOrbit.HasValue||!String.IsNullOrEmpty(macroFacetKind)&&macroFacetKind!="unchecked";
  string text=checkedFilter?"No verified matches":"No search/filter matches";
  if(uncheckedCount>0)text+="\nLibrary: "+uncheckedCount+" effects not checked · Check library";
  if(frameCount>0)text+="\nLibrary: "+frameCount+" need a verified frame";
  return text;
 }
 static string[] MacroTags(string text){var tags=text.Split(new[]{',','\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(t=>t.Trim()).Where(t=>t.Length>0).ToArray();if(tags.Length>24||tags.Any(t=>t.Length>40||t.IndexOf('\0')>=0))throw new ArgumentException("Use at most 24 tags, each 1–40 characters, separated by commas.");return tags;}
 int MacroLineHeight {get{return TextRenderer.MeasureText("Ag",form.Font).Height;}}
 int MacroContextHeight {get{return MacroLineHeight*4+10;}}
 static void FitToolTextRow(TableLayoutPanel grid,Label label,int row){
  Action fit=delegate{if(grid.IsDisposed||label.IsDisposed)return;bool visible=label.Text.Length>0;int height=visible?label.GetPreferredSize(new Size(Math.Max(1,grid.ClientSize.Width-label.Margin.Horizontal),0)).Height+label.Margin.Vertical:0;grid.RowStyles[row].Height=height;label.Visible=visible;};
  label.TextChanged+=delegate{fit();};label.FontChanged+=delegate{fit();};grid.SizeChanged+=delegate{fit();};fit();
 }
 Control MacroWorkContext(){var context=new WorkReadout{Dock=DockStyle.Fill,Margin=Padding.Empty,AccessibleName="Current, Next bookmark and protection"};context.Current.Margin=context.Next.Margin=Padding.Empty;context.Current.Text=ReadoutPiece(Value(work,"current")==null?"Current":"Current · Home",Map(Value(work,"current")));context.Next.Text=ReadoutPiece(Value(work,"next")==null?"Next":"Next · locked · Home",Map(Value(work,"next")));context.Status.Text=protection.Text+" · "+(Object.Equals(Value(Workspace,"prefix"),true)?"Each turn":"Final result");tips.SetToolTip(context.Current,PieceCaption(Map(Value(work,"current"))));tips.SetToolTip(context.Next,PieceCaption(Map(Value(work,"next"))));tips.SetToolTip(context.Status,"Context readout only; selection and protection are unchanged.\n"+context.Status.Text);return context;}
 static TableLayoutPanel MacroFields(){var fields=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=0,AutoScroll=true,Margin=Padding.Empty};fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,135));fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));return fields;}
 static void MacroField(TableLayoutPanel fields,string label,Control control,int height){int row=fields.RowCount++;fields.RowStyles.Add(new RowStyle(SizeType.Absolute,height));fields.Controls.Add(ToolLabel(label),0,row);control.Dock=DockStyle.Fill;fields.Controls.Add(control,1,row);}
 void ShowMacroFilters(){
  int contentHeight=168+MacroContextHeight+44+24;
  using(var dialog=ToolDialog("Filter Macro Base",650,contentHeight)){
   dialog.MinimumSize=new Size(600,contentHeight+dialog.Height-dialog.ClientSize.Height);var grid=ToolLayout(dialog,0,-1,0,MacroContextHeight,44);
   var fields=MacroFields();grid.Controls.Add(fields,0,1);var kind=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Macro exact action type"};foreach(string id in MacroKinds)kind.Items.Add(new Choice(id,MacroKindName(id)));kind.SelectedIndex=Array.IndexOf(MacroKinds,macroFacetKind);MacroField(fields,"Exact action",kind,36);
   var affected=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Actual affected orbit"};affected.Items.Add(new Choice("","Any affected orbit"));foreach(var profile in Items(Value(structure,"orbit_profiles")).Select(Map)){int id=Number(profile["orbit"]);affected.Items.Add(new Choice(id.ToString(),OrbitName(id)));}affected.SelectedIndex=0;for(int i=1;i<affected.Items.Count;i++)if(macroFacetOrbit.HasValue&&((Choice)affected.Items[i]).Id==macroFacetOrbit.Value.ToString())affected.SelectedIndex=i;MacroField(fields,"Affected orbit",affected,36);
   var tags=new TextBox{Text=String.Join(", ",macroFacetTags),AccessibleName="All required macro tags"};MacroField(fields,"All tags",tags,36);var pinned=new CheckBox{Text="Pinned entries only",Checked=macroFacetPinned,AutoSize=true,AccessibleName="Only pinned macros"};MacroField(fields,"Pin",pinned,30);var uses=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Likely solving use filter"};PopulateMacroUses(uses);MacroField(fields,"Likely use",uses,30);
   tips.SetToolTip(kind,"Exact action types come from checked full-model effects, scoped to the affected orbit when selected.");tips.SetToolTip(uses,"Orbit use includes explicit possible-use candidates. They stay labelled Possible; affected orbits and saved groups are separate facts.");
   var status=ToolLabel("");status.AutoEllipsis=false;status.Margin=Padding.Empty;grid.Controls.Add(status,0,2);FitToolTextRow(grid,status,2);affected.SelectedIndexChanged+=delegate{status.Text=affected.SelectedIndex>0?"Unchecked effects are excluded from this orbit filter.":"";};if(affected.SelectedIndex>0)status.Text="Unchecked effects are excluded from this orbit filter.";grid.Controls.Add(MacroWorkContext(),0,3);var actions=ToolRow();Button apply=ToolButton("Apply filter"),clear=ToolButton("Clear facets"),cancel=ToolButton("Cancel");cancel.DialogResult=DialogResult.Cancel;actions.Controls.Add(apply);actions.Controls.Add(clear);actions.Controls.Add(cancel);grid.Controls.Add(actions,0,4);dialog.AcceptButton=apply;dialog.CancelButton=cancel;
   clear.Click+=delegate{kind.SelectedIndex=affected.SelectedIndex=uses.SelectedIndex=0;tags.Clear();pinned.Checked=false;};
   bool applying=false;dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(applying&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;status.Text="Validating these tags. Wait for the result; your input is kept.";}};
   apply.Click+=async delegate{if(applying)return;try{var parsed=MacroTags(tags.Text);applying=true;fields.Enabled=actions.Enabled=false;bool accepted=await Send(LocalApi.D("action","macro-query","query",LocalApi.D("tags",parsed)));if(dialog.IsDisposed)return;if(!accepted){status.Text=feedback.Text;return;}var normalized=Value(lastCommandResult,"tag_keys") as object[];if(normalized==null||normalized.Length!=parsed.Length||normalized.Any(value=>!(value is string)))throw new InvalidOperationException("The backend did not return validated tag keys. Your filter input is retained; no filter was changed.");var orbit=((Choice)affected.SelectedItem).Id;macroFacetKind=((Choice)kind.SelectedItem).Id;macroFacetOrbit=orbit.Length==0?(int?)null:Int32.Parse(orbit);macroFacetTags=parsed;macroFacetTagKeys=normalized.Select(Text).ToArray();macroFacetPinned=pinned.Checked;macroUseFilter=((Choice)uses.SelectedItem).Id;PopulateMacroUses(macroUsePicker);DrawMacros();applying=false;dialog.Close();}catch(Exception error){if(!dialog.IsDisposed)status.Text=error.Message;}finally{applying=false;if(!dialog.IsDisposed)fields.Enabled=actions.Enabled=true;}};
   dialog.Shown+=delegate{kind.Focus();};ShowOwned(dialog);
  }
 }
 void ShowMacroMetadata(){
  if(selectedMacro==null){Choose("Macro library",new[]{new Choice("macro-import","Import a local library file"),new Choice("macro-new","Create an explicit finite macro")},RunCommand);return;}
  var record=SelectedMacroRecord();string id=Text(record["id"]),nextAction=null;
  var exact=ToolLabel("Revision "+Text(Value(record,"version"))+" · "+MacroClassificationSummary(record)+"\nSaved group: "+OrbitName(Number(record["orbit"])));exact.Font=form.Font;exact.Margin=Padding.Empty;exact.AutoEllipsis=false;int headingHeight=Math.Max(24,exact.GetPreferredSize(new Size(590,0)).Height);int contentHeight=headingHeight+198+MacroContextHeight+44+24;
  using(var dialog=ToolDialog("Edit macro · "+MacroShortName(record),680,contentHeight)){
   dialog.MinimumSize=new Size(630,contentHeight+dialog.Height-dialog.ClientSize.Height);var grid=ToolLayout(dialog,headingHeight,-1,0,MacroContextHeight,44);tips.SetToolTip(exact,"Canonical macro: "+id+"\nMetadata edits leave the fixed recipe and revision unchanged.");grid.Controls.Add(exact,0,0);
   var fields=MacroFields();grid.Controls.Add(fields,0,1);var name=new TextBox{Text=Text(Value(record,"name")),MaxLength=80,AccessibleName="Macro human name"};var note=new TextBox{Text=Text(Value(record,"note")),MaxLength=500,Multiline=true,ScrollBars=ScrollBars.Vertical,AccessibleName="Macro usage notes"};var tags=new TextBox{Text=String.Join(", ",Items(Value(record,"tags")).Select(Text).ToArray()),AccessibleName="Macro tags separated by commas"};var pinned=new CheckBox{Text="Pinned in Macro Base",Checked=Object.Equals(Value(record,"pinned"),true),AutoSize=true,AccessibleName="Macro pinned"};MacroField(fields,"Name",name,36);MacroField(fields,"Use / notes",note,96);MacroField(fields,"Tags",tags,36);MacroField(fields,"Pin",pinned,30);
   var status=ToolLabel("");status.AutoEllipsis=false;status.Margin=Padding.Empty;grid.Controls.Add(status,0,2);FitToolTextRow(grid,status,2);grid.Controls.Add(MacroWorkContext(),0,3);var actions=ToolRow();Button save=ToolButton("Save labels"),export=ToolButton("Export…"),import=ToolButton("Import…"),cancel=ToolButton("Cancel");cancel.DialogResult=DialogResult.Cancel;foreach(var button in new[]{save,export,import,cancel})actions.Controls.Add(button);grid.Controls.Add(actions,0,4);dialog.CancelButton=cancel;bool saving=false;
   dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(saving&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;status.Text="Saving the metadata. Wait for the result; your edits are kept.";}};
   Func<bool> dirty=()=>name.Text!=Text(Value(record,"name"))||note.Text!=Text(Value(record,"note"))||tags.Text!=String.Join(", ",Items(Value(record,"tags")).Select(Text).ToArray())||pinned.Checked!=Object.Equals(Value(record,"pinned"),true);
   Action reflectEdits=delegate{bool changed=dirty();export.Enabled=import.Enabled=!changed;status.Text=changed?"Unsaved labels · save or cancel before export / import.":"";};
   name.TextChanged+=delegate{reflectEdits();};note.TextChanged+=delegate{reflectEdits();};tags.TextChanged+=delegate{reflectEdits();};pinned.CheckedChanged+=delegate{reflectEdits();};
   export.Click+=delegate{if(dirty())return;nextAction="macro-export";dialog.Close();};import.Click+=delegate{if(dirty())return;nextAction="macro-import";dialog.Close();};
   save.Click+=async delegate{if(saving)return;try{string[] parsed=MacroTags(tags.Text);if(String.IsNullOrWhiteSpace(name.Text))throw new ArgumentException("Give this macro a nonempty name.");saving=true;fields.Enabled=actions.Enabled=false;bool accepted=await Send(LocalApi.D("action","macro-note","id",id,"name",name.Text.Trim(),"note",note.Text,"tags",parsed,"pinned",pinned.Checked));saving=false;if(dialog.IsDisposed)return;if(accepted)dialog.Close();else status.Text=feedback.Text;}catch(Exception error){if(!dialog.IsDisposed)status.Text=error.Message;}finally{saving=false;if(!dialog.IsDisposed)fields.Enabled=actions.Enabled=true;}};
   dialog.Shown+=delegate{name.Focus();};ShowOwned(dialog);
  }
  if(nextAction!=null&&!closing)RunCommand(nextAction);
 }
 async void ToggleMacroPin(){try{var record=SelectedMacroRecord();await Send(LocalApi.D("action","macro-note","id",record["id"],"pinned",!Object.Equals(Value(record,"pinned"),true)));}catch(Exception error){if(!closing&&!form.IsDisposed)Say(error.Message,true);}}
 DialogResult MacroFileDialog(FileDialog dialog){var owner=Form.ActiveForm;if(owner==null||owner!=form&&!windows.Contains(owner))owner=form;var prior=FocusedOwnedControl();bool oldModal=modal;modal=true;input.Reset("Local file picker owns input.");try{return dialog.ShowDialog(owner);}finally{modal=oldModal;input.Reset("File picker closed; release held keys.");if(!closing&&!owner.IsDisposed){owner.Activate();if(prior!=null&&!prior.IsDisposed)prior.Focus();}input.RefreshFocusFeedback();}}
 async void ExportSelectedMacro(){
  try{
   var record=SelectedMacroRecord();string id=Text(record["id"]),path;using(var picker=new SaveFileDialog{Title="Export selected fixed macro",Filter="Magic 600 Cell macro library (*.json)|*.json",DefaultExt="json",AddExtension=true,OverwritePrompt=true,FileName=String.Join("_",MacroShortName(record).Split(Path.GetInvalidFileNameChars()))+".json"}){if(MacroFileDialog(picker)!=DialogResult.OK)return;path=picker.FileName;}
   if(!await Send(LocalApi.D("action","macro-export","ids",new[]{id}))||closing||form.IsDisposed)return;var result=lastCommandResult;var document=Map(Value(result,"document"));if(document==null)throw new InvalidOperationException("The export did not return a macro library document.");byte[] data=new UTF8Encoding(false,true).GetBytes(api.Json(document));if(data.Length>2000000)throw new InvalidOperationException("The export exceeds the 2,000,000-byte library limit.");string temporary=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)),".magic600-export-"+Guid.NewGuid().ToString("N")+".tmp");try{File.WriteAllBytes(temporary,data);if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}Say("Exported the selected macro to "+Path.GetFileName(path)+". Recipe and canonical identity were preserved.");
  }catch(Exception error){NativeDiagnostics.Write("Macro export failed",error);if(!closing&&!form.IsDisposed)Say(error.Message,true);}
 }
 async void ImportMacros(){
  try{
   string path;using(var picker=new OpenFileDialog{Title="Inspect a macro library file",Filter="Magic 600 Cell macro library (*.json)|*.json",CheckFileExists=true,Multiselect=false}){if(MacroFileDialog(picker)!=DialogResult.OK)return;path=picker.FileName;}
   string content;using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){if(stream.Length>2000000)throw new InvalidOperationException("Choose a library no larger than 2,000,000 bytes. Nothing was imported.");using(var reader=new StreamReader(stream,new UTF8Encoding(false,true),true))content=reader.ReadToEnd();}var document=Map(api.Parse(content));if(document==null)throw new InvalidOperationException("Expected a Magic 600 Cell macro-library object. Nothing was imported.");
   if(!await Send(LocalApi.D("action","macro-import-check","document",document))||closing||form.IsDisposed)return;var result=lastCommandResult;if(result==null)return;if(!OwnedActiveWindow()){Say("The library file was inspected. Reopen Import to compare it before adding entries.");return;}ShowMacroImportReview(Path.GetFileName(path),document,result);
  }catch(Exception error){NativeDiagnostics.Write("Macro import inspection failed",error);if(!closing&&!form.IsDisposed)Say(error.Message,true);}
 }
 void ShowMacroImportReview(string filename,Dictionary<string,object> document,Dictionary<string,object> inspection){
  using(var dialog=ToolDialog("Import macros · "+filename,770,575)){
   dialog.MinimumSize=new Size(610,460);var grid=ToolLayout(dialog,54,-1,108,105,44);var added=new HashSet<string>(Items(Value(inspection,"added")).Select(Text),StringComparer.Ordinal);var identical=new HashSet<string>(Items(Value(inspection,"identical")).Select(Text),StringComparer.Ordinal);var conflicts=Items(Value(inspection,"conflict")).Select(Map).ToDictionary(row=>Text(Value(row,"id")),row=>Text(Value(row,"reason")),StringComparer.Ordinal);var records=Items(Value(inspection,"records")).Select(Map).ToArray();
   var summary=ToolLabel(added.Count+" new · "+identical.Count+" already present · "+conflicts.Count+" conflicts\nOnly new identities are added. Existing recipes, labels, Current and Next stay unchanged.");summary.AutoEllipsis=false;grid.Controls.Add(summary,0,0);
   var list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false,AccessibleName="Macro import comparison",BackColor=Paper,ForeColor=Ink};list.Columns.Add("Macro",340);list.Columns.Add("Result",170);list.Columns.Add("Revision",80);list.Resize+=delegate{list.Columns[0].Width=Math.Max(210,list.ClientSize.Width-258);};grid.Controls.Add(list,0,1);
   foreach(var record in records){string id=Text(record["id"]);var item=new ListViewItem(Text(record["name"])){Tag=id};item.SubItems.Add(conflicts.ContainsKey(id)?"Conflict":added.Contains(id)?"Add new":"Already present");item.SubItems.Add(Text(Value(record,"version")));list.Items.Add(item);}
   foreach(var conflict in conflicts.Where(pair=>!records.Any(record=>Text(record["id"])==pair.Key))){var item=new ListViewItem("Conflicting entry"){Tag=conflict.Key};item.SubItems.Add("Conflict");item.SubItems.Add("");list.Items.Add(item);}
   var detail=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,AccessibleName="Selected import record and conflict reason"};grid.Controls.Add(detail,0,2);grid.Controls.Add(MacroWorkContext(),0,3);
   list.SelectedIndexChanged+=delegate{if(list.SelectedItems.Count!=1)return;string id=Text(list.SelectedItems[0].Tag);var record=records.FirstOrDefault(row=>Text(row["id"])==id);var lines=new List<string>();if(conflicts.ContainsKey(id))lines.Add("Conflict: "+conflicts[id]+". No entry will be replaced.");else if(identical.Contains(id))lines.Add("Same canonical ID, revision and recipe. Local names, notes, tags and pin are kept.");else lines.Add("New canonical identity. Imported proofs are ignored; only locally checked effects are used.");if(record!=null){lines.Add("Saved group: "+OrbitName(Number(record["orbit"])));lines.Add("Notes: "+Text(Value(record,"note")));if(Value(record,"source")!=null)lines.Add("Source claim (not an action proof): "+Text(record["source"]));if(Value(record,"derived_from")!=null)lines.Add("Derivation claim (not a verified relationship): "+api.Json(record["derived_from"]));}lines.Add("Canonical macro: "+id);detail.Text=String.Join("\r\n",lines.ToArray());};
   var actions=ToolRow();Button apply=ToolButton("Add "+added.Count+" new macros"),cancel=ToolButton("Cancel");cancel.DialogResult=DialogResult.Cancel;actions.Controls.Add(apply);actions.Controls.Add(cancel);grid.Controls.Add(actions,0,4);dialog.CancelButton=cancel;bool applying=false;apply.Enabled=Object.Equals(Value(inspection,"can_import"),true)&&added.Count>0;detail.Text=conflicts.Count>0?"Resolve the listed conflicts in the source file, then reopen Import. No partial import is allowed.":added.Count==0?"Every entry is already present. Nothing needs to be added.":"Select an entry to compare its canonical identity and source claims before adding it.";
   dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(applying&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;detail.Text="Applying the validated import. Wait for the result; no second request was queued.";}};
   apply.Click+=async delegate{if(applying)return;applying=true;actions.Enabled=false;try{bool accepted=await Send(LocalApi.D("action","macro-import","document",document));applying=false;if(dialog.IsDisposed)return;if(accepted)dialog.Close();else{detail.Text=feedback.Text+"\r\nReopen Import to refresh the comparison before retrying.";apply.Enabled=false;}}catch(Exception error){if(!dialog.IsDisposed){detail.Text=error.Message;apply.Enabled=false;}}finally{applying=false;if(!dialog.IsDisposed)actions.Enabled=true;}};
   dialog.Shown+=delegate{cancel.Focus();};ShowOwned(dialog);
  }
 }
}
