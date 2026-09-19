// Explicit retained families inside Solve. Parameters are never selected from a residual.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 TableLayoutPanel endgamePanel;
 FlowLayoutPanel endgameHost;
 ComboBox endgameFamily,endgameQ,endgameR;
 Label endgameXName,endgameYName,endgameRoles,endgameStatus;
 TextBox endgameName;
 Button endgameCheck,endgameSave,endgameSelect,endgameDetails;
 Dictionary<string,object> endgameX,endgameY,endgameProof,endgameSaved;
 string endgameCheckedKey,endgameMessage="Choose an auxiliary position in the scene, then use it as X.";
 bool endgameExpanded,endgameUpdating,endgamePending,endgameSavedOnce;
 int endgameRequest;

 void BuildKnownEndgame(FlowLayoutPanel target){
  if(endgamePanel!=null)return;endgameHost=target;
  var toggle=Button("Endgame","endgame-choices");target.Controls.Add(toggle);target.SetFlowBreak(toggle,true);
  endgamePanel=new TableLayoutPanel{ColumnCount=2,RowCount=9,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Margin=new Padding(2,3,2,3),Padding=new Padding(2),Visible=false,AccessibleName="Explicit known endgame families"};
  endgamePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));endgamePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int i=0;i<9;i++)endgamePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
  endgameFamily=EndgameCombo("Known operation family");foreach(var item in new[]{new[]{"","Choose operation"},new[]{"placement-star","Place · A → B → X"},new[]{"transfer","Transfer orientation · X / B"},new[]{"buffer-a","Transfer orientation · A / B"},new[]{"final-b","Last buffer B · commutator"}})endgameFamily.Items.Add(new Choice(item[0],item[1]));endgameFamily.SelectedIndex=0;endgamePanel.Controls.Add(endgameFamily,0,0);endgamePanel.SetColumnSpan(endgameFamily,2);
  endgamePanel.Controls.Add(Button("Use inspected as X","endgame-x"),0,1);endgameXName=EndgameLabel("X · unassigned");endgamePanel.Controls.Add(endgameXName,1,1);
  endgamePanel.Controls.Add(Button("Use inspected as Y","endgame-y"),0,2);endgameYName=EndgameLabel("Y · only for last buffer B");endgamePanel.Controls.Add(endgameYName,1,2);
  endgamePanel.Controls.Add(Button("q · slot frame","endgame-q"),0,3);endgameQ=EndgameCombo("Explicit q permutation of the fixed ordered slots");endgamePanel.Controls.Add(endgameQ,1,3);
  endgamePanel.Controls.Add(Button("r · slot frame","endgame-r"),0,4);endgameR=EndgameCombo("Explicit r permutation of the fixed ordered slots");endgamePanel.Controls.Add(endgameR,1,4);
  endgameRoles=EndgameLabel("");endgamePanel.Controls.Add(endgameRoles,0,5);endgamePanel.SetColumnSpan(endgameRoles,2);
  var actions=Strip();actions.AutoSize=true;actions.WrapContents=true;endgameCheck=Button("Check effect","endgame-check");endgameDetails=Button("Details","endgame-details");actions.Controls.Add(endgameCheck);actions.Controls.Add(endgameDetails);endgamePanel.Controls.Add(actions,0,6);endgamePanel.SetColumnSpan(actions,2);
  endgameStatus=EndgameLabel(endgameMessage);endgameStatus.AccessibleName="Endgame effect and validation result";endgamePanel.Controls.Add(endgameStatus,0,7);endgamePanel.SetColumnSpan(endgameStatus,2);
  var saveRow=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,AutoSize=true,Margin=Padding.Empty};saveRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));saveRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));saveRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));saveRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
  endgameName=new TextBox{Dock=DockStyle.Fill,MaxLength=80,AccessibleName="Name for the new endgame macro",Margin=new Padding(2)};tips.SetToolTip(endgameName,"New macro name. Saving does not insert or execute this operation.");saveRow.Controls.Add(EndgameLabel("Name"),0,0);saveRow.Controls.Add(endgameName,1,0);var saves=Strip();saves.AutoSize=true;saves.WrapContents=true;endgameSave=Button("Save macro","endgame-save");endgameSelect=Button("Select saved","endgame-select-saved");saves.Controls.Add(endgameSave);saves.Controls.Add(endgameSelect);saveRow.Controls.Add(saves,0,1);saveRow.SetColumnSpan(saves,2);endgamePanel.Controls.Add(saveRow,0,8);endgamePanel.SetColumnSpan(saveRow,2);
  endgameFamily.SelectedIndexChanged+=delegate{if(!endgameUpdating)InvalidateEndgame("Choose q explicitly, then check the complete effect.");};endgameQ.SelectedIndexChanged+=delegate{if(!endgameUpdating)InvalidateEndgame("Parameters changed; check the complete effect.");};endgameR.SelectedIndexChanged+=delegate{if(!endgameUpdating)InvalidateEndgame("Parameters changed; check the complete effect.");};endgameName.TextChanged+=delegate{DrawKnownEndgame();};
  FillEndgameElements(endgameQ,null);FillEndgameElements(endgameR,null);target.Controls.Add(endgamePanel);target.SetFlowBreak(endgamePanel,true);target.ClientSizeChanged+=delegate{SizeKnownEndgame();};StyleTool(endgamePanel);SizeKnownEndgame();DrawKnownEndgame();
 }
 ComboBox EndgameCombo(string accessible){return new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DropDownWidth=650,Margin=new Padding(2,4,2,4),AccessibleName=accessible};}
 Label EndgameLabel(string text){return new Label{Text=text,AutoSize=true,AutoEllipsis=false,Margin=new Padding(3,4,3,4),Padding=Padding.Empty,Anchor=AnchorStyles.Left|AnchorStyles.Right};}
 void SizeKnownEndgame(){if(endgamePanel==null||endgamePanel.IsDisposed)return;int width=Math.Max(260,endgameHost.ClientSize.Width-endgameHost.Padding.Horizontal-endgamePanel.Margin.Horizontal-4);if(endgamePanel.MinimumSize.Width!=width){endgamePanel.MinimumSize=new Size(width,0);endgamePanel.MaximumSize=new Size(width,0);endgamePanel.Width=width;}foreach(var label in new[]{endgameRoles,endgameStatus})label.MaximumSize=new Size(width-12,0);int side=Math.Max(80,width-175);endgameXName.MaximumSize=new Size(side,0);endgameYName.MaximumSize=new Size(side,0);QueueSolveLayout();}
 void ToggleKnownEndgame(){OpenSolvePage("macros");if(endgamePanel==null)return;endgameExpanded=!endgameExpanded;endgamePanel.Visible=endgameExpanded;SizeKnownEndgame();if(endgameExpanded)endgameFamily.Focus();else search.Focus();}
 void EnsureKnownEndgame(){OpenSolvePage("macros");if(endgamePanel==null)throw new InvalidOperationException("Open Solve to use the known endgame families.");endgameExpanded=true;endgamePanel.Visible=true;SizeKnownEndgame();}
 string EndgameFamilyId(){return endgameFamily==null||endgameFamily.SelectedItem==null?"":((Choice)endgameFamily.SelectedItem).Id;}
 bool EndgameFinal(){return EndgameFamilyId()=="final-b";}
 string EndgameFrameVersion(){return Text(Value(Map(Value(work,"review_context")),"frame_version"));}
 bool EndgameOptionsCurrent(Dictionary<string,object> options){return options!=null&&work!=null&&Text(Value(options,"model"))==Text(Value(work,"model"))&&Number(Value(options,"orbit"))==Number(Workspace["orbit"])&&Text(Value(options,"frame_version"))==EndgameFrameVersion();}
 string EndgamePositionName(Dictionary<string,object> options){var record=Map(Value(options,"position_record"));string name=Text(Value(Map(Value(record,"names")),"current_short"));if(name.Length==0)name=Text(Value(Map(Value(options,"position_name")),"short_name"));return name;}
 string EndgameChoiceSummary(){
  if(EndgameFamilyId().Length==0)return "";
  string summary=Text(endgameFamily.SelectedItem)+"\nX · "+(endgameX==null?"unassigned":EndgamePositionName(endgameX));
  if(EndgameFinal())summary+="\nY · "+(endgameY==null?"unassigned":EndgamePositionName(endgameY));return summary;
 }
 void InvalidateEndgame(string message){endgameRequest++;endgameProof=null;endgameCheckedKey=null;endgameSaved=null;endgameSavedOnce=false;endgameMessage=message;DrawKnownEndgame();}
 static string EndgameElementCaption(object raw){
  int[] element=Items(raw).Select(Number).ToArray();if(element.Length==0||element.Length>26||!element.OrderBy(x=>x).SequenceEqual(Enumerable.Range(0,element.Length)))throw new InvalidOperationException("The fixed-slot permutation is malformed.");
  var seen=new bool[element.Length];var cycles=new List<string>();for(int i=0;i<element.Length;i++){if(seen[i])continue;var cycle=new List<string>();int j=i;do{seen[j]=true;cycle.Add(((char)('a'+j)).ToString());j=element[j];}while(j!=i);if(cycle.Count>1)cycles.Add("("+String.Join(" ",cycle.ToArray())+")");}return cycles.Count==0?"e · frame identity":String.Join(" ",cycles.ToArray());
 }
 void FillEndgameElements(ComboBox combo,Dictionary<string,object> options){bool old=endgameUpdating;endgameUpdating=true;try{combo.Items.Clear();combo.Items.Add(new Choice("","Choose rotation"));foreach(var item in Items(Value(options,"choices")).Select(Map)){object element=Value(item,"element");combo.Items.Add(new Choice(api.Json(element),EndgameElementCaption(element)));}combo.SelectedIndex=0;}finally{endgameUpdating=old;}}
 object EndgameElement(ComboBox combo){return combo.SelectedIndex>0?api.Parse(((Choice)combo.SelectedItem).Id):null;}
 Dictionary<string,object> EndgameParameters(){
  if(!EndgameOptionsCurrent(endgameX))return null;string family=EndgameFamilyId();object q=EndgameElement(endgameQ);if(family.Length==0||q==null)return null;
  var body=LocalApi.D("model",endgameX["model"],"frame_version",endgameX["frame_version"],"orbit",endgameX["orbit"],"family",family,"x",endgameX["position"],"q",q);
  if(family=="final-b"){object r=EndgameElement(endgameR);if(!EndgameOptionsCurrent(endgameY)||r==null||Number(endgameX["position"])==Number(endgameY["position"]))return null;body["y"]=endgameY["position"];body["r"]=r;}return body;
 }
 bool EndgameProofMatches(Dictionary<string,object> proof,Dictionary<string,object> parameters){
  if(proof==null||parameters==null||Value(proof,"recipe")==null||Map(Value(proof,"effect"))==null)return false;
  foreach(string key in new[]{"model","frame_version","orbit","family"})if(Text(Value(proof,key))!=Text(Value(parameters,key)))return false;
  var actual=Map(Value(proof,"parameters"));foreach(string key in new[]{"x","q","y","r"})if(api.Json(Value(actual,key))!=api.Json(Value(parameters,key)))return false;
  return api.Json(Value(proof,"roles"))==api.Json(Value(endgameX,"roles"))&&api.Json(Value(proof,"certificate"))==api.Json(Value(endgameX,"certificate"));
 }
 void DrawKnownEndgame(){
  if(endgamePanel==null||endgamePanel.IsDisposed||work==null)return;
  if((endgameX!=null&&!EndgameOptionsCurrent(endgameX))||(endgameY!=null&&!EndgameOptionsCurrent(endgameY))){endgameX=null;endgameY=null;FillEndgameElements(endgameQ,null);FillEndgameElements(endgameR,null);endgameProof=null;endgameCheckedKey=null;endgameSaved=null;endgameSavedOnce=false;endgameRequest++;endgameMessage="Orbit or fixed reference changed. Choose X again; no work object was changed.";}
  bool ready=IsReady&&!endgamePending;endgameFamily.Enabled=ready;endgameQ.Enabled=ready&&endgameX!=null;endgameR.Enabled=ready&&EndgameFinal()&&endgameY!=null;endgameName.Enabled=!endgamePending&&!endgameSavedOnce;
  endgameXName.Text=endgameX==null?"X · unassigned":EndgamePositionName(endgameX);endgameYName.Text=endgameY==null?"Y · only for last buffer B":EndgamePositionName(endgameY);
  string roles="Fixed retained A / B · choose X to inspect";if(endgameX!=null){var records=Items(Value(endgameX,"buffer_records")).Select(Map).ToArray();roles="Fixed retained buffers · orientation "+Text(Value(endgameX,"group"));for(int i=0;i<records.Length;i++)roles+="\n"+(i==0?"A · ":"B · ")+Text(Value(Map(Value(records[i],"names")),"current_short"));bool match=api.Json(Value(endgameX,"roles"))==api.Json(Value(Workspace,"roles"));roles+="\n"+(match?"A / B match current roles.":"A / B differ from current roles; work bindings stay unchanged.");}endgameRoles.Text=roles;
  var parameters=EndgameParameters();endgameCheck.Enabled=ready&&parameters!=null;endgameSave.Enabled=ready&&!endgameSavedOnce&&parameters!=null&&endgameProof!=null&&endgameCheckedKey==api.Json(parameters)&&endgameName.Text.Trim().Length>0;endgameSelect.Enabled=ready&&endgameSaved!=null&&CurrentMacroBinding(BoundMacro(endgameSaved));endgameDetails.Enabled=!endgamePending&&endgameProof!=null;string choice=EndgameChoiceSummary();endgameStatus.Text=(choice.Length==0?"":choice+"\n")+endgameMessage;
  string choiceDetail=endgameStatus.Text;if(endgameQ.SelectedIndex>0)choiceDetail+="\nq · "+Text(endgameQ.SelectedItem);if(EndgameFinal()&&endgameR.SelectedIndex>0)choiceDetail+="\nr · "+Text(endgameR.SelectedItem);tips.SetToolTip(endgameStatus,choiceDetail);endgameStatus.AccessibleDescription=choiceDetail;
  tips.SetToolTip(endgameQ,endgameQ.SelectedIndex>0?"q = "+Text(endgameQ.SelectedItem)+"\nFixed ordered slots: "+api.Json(Value(endgameX,"frame"))+"\nSlot letters are not Grip axes. Full recipe effects are checked separately.":"Choose q; no correction is selected from the current residual.");tips.SetToolTip(endgameR,endgameR.SelectedIndex>0?"r = "+Text(endgameR.SelectedItem)+"\nFixed ordered slots: "+api.Json(Value(endgameY,"frame")):"Choose r for a distinct auxiliary Y.");SizeKnownEndgame();
 }
 void EndgameUseInspected(bool second){
  EnsureKnownEndgame();if(!IsReady||endgamePending)throw new InvalidOperationException(StopFailure??"Finish or stop the current check first.");if(hub.SelectionIsForecast)throw new InvalidOperationException("Select an actual token or fixed position for the auxiliary; this token is a forecast.");
  LoadEndgameOptions(second,SelectedPosition());
 }
 async void LoadEndgameOptions(bool second,int position){
  int orbitId=Number(Workspace["orbit"]);string model=Text(Value(work,"model")),frame=EndgameFrameVersion();int serial=++endgameRequest;endgamePending=true;endgameMessage="Checking fixed-frame choices for "+(second?"Y":"X")+"…";DrawKnownEndgame();
  try{bool accepted=await Send(LocalApi.D("action","endgame-options","orbit",orbitId,"position",position));var receipt=lastCommandResult;await WaitForStopCompletion();if(closing||endgamePanel.IsDisposed)return;if(!accepted){endgameMessage=feedback.Text;return;}if(serial!=endgameRequest||!EndgameOptionsCurrent(receipt)||Text(Value(receipt,"model"))!=model||Text(Value(receipt,"frame_version"))!=frame||Number(Value(receipt,"position"))!=position){endgameMessage="The auxiliary choices no longer match this orbit and reference. Choose the position again.";return;}
   if(Map(Value(receipt,"position_record"))==null||Items(Value(receipt,"buffer_records")).Length!=2)throw new InvalidOperationException("The auxiliary reply omitted its canonical position or buffer names.");
   if(second){endgameY=receipt;FillEndgameElements(endgameR,receipt);}else{endgameX=receipt;FillEndgameElements(endgameQ,receipt);}InvalidateEndgame((second?"Y":"X")+" fixed. Choose its slot rotation explicitly.");
  }catch(Exception error){if(!closing&&!endgamePanel.IsDisposed)endgameMessage=error.Message;}finally{endgamePending=false;if(!closing&&!endgamePanel.IsDisposed)DrawKnownEndgame();}
 }
 void FocusEndgameElement(bool second){EnsureKnownEndgame();var combo=second?endgameR:endgameQ;if(!combo.Enabled)throw new InvalidOperationException(second?"Choose the last-buffer family and an explicit Y first.":"Choose an explicit X first.");combo.Focus();combo.DroppedDown=true;}
 void CheckKnownEndgame(){
  EnsureKnownEndgame();if(!IsReady||endgamePending)throw new InvalidOperationException(StopFailure??"Finish or stop the current check first.");var parameters=EndgameParameters();if(parameters==null)throw new InvalidOperationException("Choose the family, X and q; last buffer B also needs a distinct Y and r.");CheckEndgameParameters(parameters);
 }
 async void CheckEndgameParameters(Dictionary<string,object> parameters){
  string key=api.Json(parameters);int serial=++endgameRequest;endgameProof=null;endgameCheckedKey=null;endgamePending=true;endgameMessage="Checking the complete legal recipe, including other orbits…";DrawKnownEndgame();parameters["action"]="endgame-compose";
  try{bool accepted=await Send(parameters);var receipt=lastCommandResult;await WaitForStopCompletion();if(closing||endgamePanel.IsDisposed)return;if(!accepted){endgameMessage=feedback.Text;return;}var current=EndgameParameters();if(serial!=endgameRequest||current==null||api.Json(current)!=key||!EndgameProofMatches(receipt,current)){endgameMessage="Parameters or reference changed; the returned effect was not adopted.";return;}endgameProof=receipt;endgameCheckedKey=key;endgameMessage=EndgameEffectSummary(receipt)+"\nNet effect only · check the full operation's protection before execution.";if(StopFailure!=null)endgameMessage+="\n"+StopFailure;
  }catch(Exception error){if(!closing&&!endgamePanel.IsDisposed)endgameMessage=error.Message;}finally{endgamePending=false;if(!closing&&!endgamePanel.IsDisposed)DrawKnownEndgame();}
 }
 string EndgameEffectSummary(Dictionary<string,object> proof){var effect=Map(Value(proof,"effect"));int collateral=Items(Value(proof,"collateral")).Length;string local=Object.Equals(Value(proof,"target_identity"),true)?"No net action on this orbit":Object.Equals(Value(proof,"target_positions_preserved"),true)?"This orbit: positions retained":"This orbit: piece positions move";return local+" · "+Text(Value(proof,"cost"))+" turns\n"+Text(Value(effect,"pieces"))+" affected pieces · "+collateral+" other orbits";}
 void SaveKnownEndgame(){
  EnsureKnownEndgame();if(!IsReady||endgamePending)throw new InvalidOperationException(StopFailure??"Finish or stop the current check first.");var parameters=EndgameParameters();string name=endgameName.Text.Trim();if(endgameSavedOnce)throw new InvalidOperationException("This entry was saved. Select it explicitly from the result.");if(parameters==null||endgameProof==null||endgameCheckedKey!=api.Json(parameters)||name.Length==0)throw new InvalidOperationException("Check these exact parameters and enter a new macro name first.");SaveEndgameParameters(parameters,name);
 }
 async void SaveEndgameParameters(Dictionary<string,object> parameters,string name){
  string key=api.Json(parameters);int serial=++endgameRequest;endgamePending=true;endgameMessage="Saving the explicitly checked family…";DrawKnownEndgame();parameters["action"]="endgame-save";parameters["name"]=name;
  try{bool accepted=await Send(parameters);var receipt=lastCommandResult;await WaitForStopCompletion();if(closing||endgamePanel.IsDisposed)return;if(!accepted){endgameMessage=feedback.Text;return;}endgameSavedOnce=true;var record=Map(Value(receipt,"record"));var proof=Map(Value(receipt,"proof"));string id=Text(Value(receipt,"created_id"));var current=EndgameParameters();if(serial!=endgameRequest||current==null||api.Json(current)!=key||!EndgameProofMatches(proof,current)||record==null||id.Length==0||Text(Value(record,"id"))!=id||!CurrentMacroBinding(BoundMacro(record))||api.Json(Value(record,"recipe"))!=api.Json(Value(proof,"recipe"))){endgameMessage="The save completed but its receipt no longer matches this view. Inspect Macro Base before saving again.";return;}endgameSaved=record;endgameProof=proof;endgameMessage="Saved "+MacroName(record)+". Select saved to inspect or insert it; no steps were added.";string warning=Text(Value(receipt,"warning"));if(warning.Length>0)endgameMessage+="\n"+warning;if(StopFailure!=null)endgameMessage+="\n"+StopFailure;
  }catch(Exception error){if(!closing&&!endgamePanel.IsDisposed)endgameMessage=error.Message;}finally{endgamePending=false;if(!closing&&!endgamePanel.IsDisposed)DrawKnownEndgame();}
 }
 void SelectSavedEndgame(){EnsureKnownEndgame();if(!IsReady||endgamePending||endgameSaved==null)throw new InvalidOperationException("Save a checked entry first.");if(!CurrentMacroBinding(BoundMacro(endgameSaved)))throw new InvalidOperationException("The saved revision changed. Choose its current entry in Macro Base.");selectedMacro=Text(endgameSaved["id"]);selectedEffect=null;selectedApplicability=null;Send(LocalApi.D("action","macro-effect","id",selectedMacro,"select",true));}
 static string EndgameFamilyMapping(string family){switch(family){case "placement-star":return "A → B → X → A; B → X carries q, X → A carries q⁻¹ in the fixed slot frames.";case "transfer":return "Fixed positions: X carries q⁻¹; B carries q.";case "buffer-a":return "Fixed positions: A carries q; B carries q⁻¹.";case "final-b":return "Fixed positions: B carries the chronological commutator q / r / q⁻¹ / r⁻¹.";default:return "";}}
 void ShowEndgameDetails(){
  if(endgameProof==null)throw new InvalidOperationException("Check the explicitly selected family first.");var proof=endgameProof;var parameters=Map(Value(proof,"parameters"));var lines=new List<string>{OrbitName(Number(proof["orbit"])),EndgameEffectSummary(proof),EndgameFamilyMapping(Text(proof["family"])),"","Fixed retained A / B (not rebound by this selector):",endgameRoles.Text,"","X · "+EndgamePositionName(endgameX),"q · "+EndgameElementCaption(parameters["q"]),"q maps each fixed slot letter to the next letter in its cycle.","X fixed slot order: "+api.Json(Value(endgameX,"frame"))};if(Value(parameters,"y")!=null){lines.Add("Y · "+EndgamePositionName(endgameY));lines.Add("r · "+EndgameElementCaption(parameters["r"]));lines.Add("Y fixed slot order: "+api.Json(Value(endgameY,"frame")));lines.Add("Result at B · "+EndgameElementCaption(parameters["result"]));}lines.Add("");lines.Add("Other affected orbits:");foreach(var row in Items(Value(proof,"collateral")).Select(Map))lines.Add(OrbitName(Number(row["orbit"]))+" · "+Text(Value(row,"pieces"))+" pieces / "+Text(Value(row,"stickers"))+" labels");if(Items(Value(proof,"collateral")).Length==0)lines.Add("None");lines.Add("");lines.Add("A frame identity or an unchanged chosen orbit does not imply a whole-puzzle identity. Protection and intermediate steps require the existing full operation review.");
  using(var dialog=ToolDialog("Known endgame effect",730,540)){var grid=ToolLayout(dialog,-1,MacroContextHeight,42);var tabs=new TabControl{Dock=DockStyle.Fill};foreach(var item in new[]{new[]{"Effect",String.Join("\r\n",lines.ToArray())},new[]{"Recipe and proof",api.Json(proof)}}){var page=new TabPage(item[0]);page.Controls.Add(new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=false,BorderStyle=BorderStyle.None,Text=item[1],AccessibleName="Read-only known endgame "+item[0]});tabs.TabPages.Add(page);}grid.Controls.Add(tabs,0,0);grid.Controls.Add(MacroWorkContext(),0,1);var actions=ToolRow();var close=ToolButton("Close");close.DialogResult=DialogResult.Cancel;actions.Controls.Add(close);grid.Controls.Add(actions,0,2);dialog.CancelButton=close;ShowOwned(dialog);}
 }
}
