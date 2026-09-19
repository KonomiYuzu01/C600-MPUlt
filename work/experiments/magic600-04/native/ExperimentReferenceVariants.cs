// User-selected proper frames compile a new legal macro; no target search or execution.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 sealed class GeometryFrameChoice {
  internal Dictionary<string,object> Binding;
  internal string Caption,Details;
  public override string ToString(){return Caption;}
 }
 ComboBox GeometryCellPicker(string name){
  var picker=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,DropDownWidth=620,AccessibleName=name};
  picker.Items.Add(new Choice("","Choose a cell…"));for(int cell=1;cell<=600;cell++)picker.Items.Add(new Choice(cell.ToString(),CellName(cell)));picker.SelectedIndex=0;return picker;
 }
 bool GeometryProofMatches(Dictionary<string,object> result,Dictionary<string,object> source,Dictionary<string,object> from,Dictionary<string,object> to){
  var reference=Map(Value(result,"reference"));var proof=Map(Value(result,"proof"));
  return result!=null&&SameMacroBinding(source,Map(Value(result,"source")))&&reference!=null&&proof!=null
   &&Text(Value(reference,"model"))==Text(Value(work,"model"))&&Text(Value(reference,"reference_version"))=="exact-incidence-reference-v1"
   &&api.Json(Value(reference,"source_frame"))==api.Json(from)&&api.Json(Value(reference,"destination_frame"))==api.Json(to)
   &&Object.Equals(Value(proof,"complete_action_equal"),true)&&Number(Value(proof,"labels"))==259800&&Value(result,"recipe")!=null;
 }
 string GeometryReviewText(Dictionary<string,object> result){
  var proof=Map(Value(result,"proof"));var reference=Map(Value(result,"reference"));var source=Map(Value(reference,"source_frame"));var target=Map(Value(reference,"destination_frame"));
  var lines=new List<string>{"Net reference effect checked · "+Text(Value(result,"source_primitives"))+" → "+Text(Value(result,"emitted_primitives"))+" turns",
   "Protection not reviewed · intermediate motion may differ.",
   "Review the complete operation before execution.",
   CellName(Number(Value(source,"cell")))+" → "+CellName(Number(Value(target,"cell"))),
   Text(Value(proof,"support_labels"))+" affected labels · all labels checked",
   "Affected orbits:",MacroPairScope(Value(proof,"affected_orbits"))};
  return String.Join("\r\n",lines.ToArray());
 }
 void ShowGeometryVariant(){
  var source=SelectedMacroRecord();var bound=BoundMacro(source);string model=Text(Value(work,"model"));
  int? openingGrip=input.ActiveGripCell;string openingBank=Text(Value(Workspace,"bank"));var openingVertices=openingGrip.HasValue?Items(Value(Map(Value(Bank,"frames")),openingGrip.Value.ToString())).Select(Number).ToArray():new int[0];
  string chosen=null;Dictionary<string,object> savedRecord=null;
  using(var dialog=ToolDialog("Map a macro to another frame",780,590)){
   dialog.MinimumSize=new Size(660,520);var grid=ToolLayout(dialog,54,36,154,-1,MacroContextHeight,46);
   var heading=ToolLabel(MacroShortName(source)+" · revision "+Text(Value(source,"version"))+"\nChoose both ordered references; the original macro stays unchanged.");heading.AutoEllipsis=false;grid.Controls.Add(heading,0,0);FitToolTextRow(grid,heading,0);tips.SetToolTip(heading,"Canonical source: "+Text(Value(source,"id")));
   string suggestion=MacroShortName(source);if(suggestion.Length>66)suggestion=suggestion.Substring(0,66);
   var name=new TextBox{Dock=DockStyle.Fill,Text=suggestion+" mapped frame",MaxLength=80,AccessibleName="New geometry macro name"};grid.Controls.Add(name,0,1);
   var selectors=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=4,Margin=Padding.Empty};selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));foreach(int height in new[]{30,38,38,44})selectors.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
   selectors.Controls.Add(ToolLabel("Source reference"),0,0);selectors.Controls.Add(ToolLabel("Destination reference"),1,0);
   var fromCell=GeometryCellPicker("Geometry source cell");var toCell=GeometryCellPicker("Geometry destination cell");
   var fromFrame=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Geometry source frame",Enabled=false};var toFrame=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="Geometry destination frame",Enabled=false};
   selectors.Controls.Add(fromCell,0,1);selectors.Controls.Add(toCell,1,1);selectors.Controls.Add(fromFrame,0,2);selectors.Controls.Add(toFrame,1,2);
   var sourceHint=ToolLabel("Choose the source explicitly.");sourceHint.AutoEllipsis=false;selectors.Controls.Add(sourceHint,0,3);var gripRow=ToolRow();var useGrip=ToolButton("Use selected Grip");tips.SetToolTip(useGrip,openingGrip.HasValue?"Uses the Grip active when this editor opened: "+CellName(openingGrip.Value)+". Opening an editor releases held input.":"No Grip was selected when this editor opened. Choose a destination cell and frame explicitly.");gripRow.Controls.Add(useGrip);selectors.Controls.Add(gripRow,1,3);grid.Controls.Add(selectors,0,2);
   var status=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,AccessibleName="Geometry reference result",Text="Choose source and destination cells and frames, then check the complete correspondence."};grid.Controls.Add(status,0,3);grid.Controls.Add(MacroWorkContext(),0,4);
   var actions=ToolRow();var check=ToolButton("Check reference");var save=ToolButton("Save new macro");var select=ToolButton("Select saved macro");var close=ToolButton("Cancel");actions.Controls.Add(check);actions.Controls.Add(save);actions.Controls.Add(select);actions.Controls.Add(close);grid.Controls.Add(actions,0,5);close.DialogResult=DialogResult.Cancel;dialog.CancelButton=close;dialog.AcceptButton=check;
   bool running=false,completed=false,stopping=false,syncing=false;Dictionary<string,object> checkedResult=null;string savedId=null;
   Func<Dictionary<string,object>> sourceFrame=()=>fromFrame.SelectedItem is GeometryFrameChoice?((GeometryFrameChoice)fromFrame.SelectedItem).Binding:null;
   Func<Dictionary<string,object>> destinationFrame=()=>toFrame.SelectedItem is GeometryFrameChoice?((GeometryFrameChoice)toFrame.SelectedItem).Binding:null;
   Action refresh=delegate{
    bool idle=!running&&!completed;fromCell.Enabled=toCell.Enabled=name.Enabled=idle;fromFrame.Enabled=idle&&fromFrame.Items.Count>0;toFrame.Enabled=idle&&toFrame.Items.Count>0;useGrip.Enabled=idle&&openingGrip.HasValue&&openingVertices.Length==4;
    bool reload=fromCell.SelectedIndex>0&&fromFrame.Items.Count==0||toCell.SelectedIndex>0&&toFrame.Items.Count==0;
    check.Enabled=!completed&&(!running||!stopping)&&(running||reload||sourceFrame()!=null&&destinationFrame()!=null);check.Text=running?(stopping?"Stopping…":"Stop check"):reload?"Load frames":"Check reference";
    save.Enabled=idle&&IsReady&&checkedResult!=null;select.Enabled=!running&&savedRecord!=null&&IsReady;close.DialogResult=running?DialogResult.None:DialogResult.Cancel;close.Enabled=!stopping;close.Text=running?"Stop check":completed?"Close":"Cancel";
   };
   Action invalidate=delegate{if(syncing)return;checkedResult=null;status.Text="Reference changed. Check the selected pair before saving.";refresh();};
   Action requestStop=delegate{if(!running||stopping)return;stopping=true;status.Text="Stop requested; waiting for the result. A completed save will still be reported.";StopAnalysis();refresh();};
   close.Click+=delegate{if(running)requestStop();};dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(running&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;requestStop();}};
   Func<bool,int,int[],Task> loadFrames=async (isSource,cell,gripVertices)=>{
    var picker=isSource?fromFrame:toFrame;picker.Items.Clear();checkedResult=null;
    if(!IsReady){status.Text=StopFailure??"Wait for the current check to finish, then select the cell again.";refresh();return;}
    running=true;stopping=false;status.Text="Loading the selected cell's proper frames…";refresh();
    try{
     bool accepted=await Send(LocalApi.D("action","reference-frames","cell",cell));var result=lastCommandResult;await WaitForStopCompletion();if(dialog.IsDisposed||dialog.Disposing)return;
     if(!accepted){status.Text=feedback.Text;return;}if(Text(Value(result,"model"))!=model||Number(Value(result,"cell"))!=cell){status.Text="The frame reply does not match the selected cell. Choose it again.";return;}
     var basis=Items(Value(result,"base_vertices")).Select(Number).ToArray();if(basis.Length!=4)throw new InvalidOperationException("The cell has no complete ordered reference.");
     foreach(var item in Items(Value(result,"frames"))){var row=Map(item);var vertices=Items(Value(row,"vertices")).Select(Number).ToArray();if(vertices.Length!=4||vertices.Any(v=>Array.IndexOf(basis,v)<0))throw new InvalidOperationException("An ordered frame is incomplete.");
      string order=new String(vertices.Select(v=>(char)('a'+Array.IndexOf(basis,v))).ToArray());var choice=new GeometryFrameChoice{Binding=LocalApi.D("cell",cell,"ordered_vertices",vertices.Cast<object>().ToArray()),Caption="Frame abcd ← "+order,Details="Canonical cap C"+cell+"; ordered vertices "+String.Join(", ",vertices.Select(v=>v.ToString()).ToArray())+". Selecting a frame does not turn the puzzle."};picker.Items.Add(choice);
     }
     if(picker.Items.Count!=12)throw new InvalidOperationException("The selected cell did not provide all twelve proper frames.");
     if(gripVertices!=null){int found=-1;for(int i=0;i<picker.Items.Count;i++)if(Items(((GeometryFrameChoice)picker.Items[i]).Binding["ordered_vertices"]).Select(Number).SequenceEqual(gripVertices))found=i;if(found<0)throw new InvalidOperationException("The captured Grip frame is no longer one of this cell's proper frames.");picker.SelectedIndex=found;}
     status.Text=gripVertices==null?"Choose an ordered frame for "+CellName(cell)+".":"Destination copied from the explicitly selected Grip. Choose the source, then check.";
    }catch(Exception error){if(!dialog.IsDisposed&&!dialog.Disposing){picker.Items.Clear();status.Text=error.Message;}}
    finally{running=false;stopping=false;if(!dialog.IsDisposed&&!dialog.Disposing)refresh();}
   };
   fromCell.SelectedIndexChanged+=async delegate{if(syncing)return;fromFrame.Items.Clear();invalidate();if(fromCell.SelectedIndex>0)await loadFrames(true,Int32.Parse(((Choice)fromCell.SelectedItem).Id),null);};
   toCell.SelectedIndexChanged+=async delegate{if(syncing)return;toFrame.Items.Clear();invalidate();if(toCell.SelectedIndex>0)await loadFrames(false,Int32.Parse(((Choice)toCell.SelectedItem).Id),null);};
   fromFrame.SelectedIndexChanged+=delegate{invalidate();var selected=fromFrame.SelectedItem as GeometryFrameChoice;if(selected!=null)tips.SetToolTip(fromFrame,selected.Details);};toFrame.SelectedIndexChanged+=delegate{invalidate();var selected=toFrame.SelectedItem as GeometryFrameChoice;if(selected!=null)tips.SetToolTip(toFrame,selected.Details);};
   useGrip.Click+=async delegate{
    if(running||!openingGrip.HasValue)return;int cell=openingGrip.Value;var vertices=Items(Value(Map(Value(Bank,"frames")),cell.ToString())).Select(Number).ToArray();
    if(openingBank!=Text(Value(Workspace,"bank"))||vertices.Length!=4||!vertices.SequenceEqual(openingVertices)){status.Text="The selected Grip set or frame changed. Choose the destination explicitly or reopen the editor.";return;}
    syncing=true;toCell.SelectedIndex=cell;syncing=false;await loadFrames(false,cell,openingVertices);
   };
   check.Click+=async delegate{
    if(running){requestStop();return;}if(!IsReady){status.Text=StopFailure??"Wait for the current check to finish.";return;}
    if(fromCell.SelectedIndex>0&&fromFrame.Items.Count==0){await loadFrames(true,Int32.Parse(((Choice)fromCell.SelectedItem).Id),null);return;}
    if(toCell.SelectedIndex>0&&toFrame.Items.Count==0){await loadFrames(false,Int32.Parse(((Choice)toCell.SelectedItem).Id),null);return;}
    var from=sourceFrame();var to=destinationFrame();if(from==null||to==null)return;
    if(!CurrentMacroBinding(bound)){status.Text="The source revision changed. Your selections are kept; reopen the editor for its current recipe.";return;}
    checkedResult=null;running=true;stopping=false;status.Text="Checking every affected label and the emitted legal turns…";refresh();
    try{
     bool accepted=await Send(LocalApi.D("action","macro-geometry-review","source",bound,"source_frame",from,"destination_frame",to));var result=lastCommandResult;await WaitForStopCompletion();if(dialog.IsDisposed||dialog.Disposing)return;
     if(!accepted){status.Text=feedback.Text;return;}if(!CurrentMacroBinding(bound)||!GeometryProofMatches(result,bound,from,to)){status.Text="The result no longer matches the selected source and frames. Check again.";return;}
     checkedResult=Map(api.Parse(api.Json(result)));status.Text=GeometryReviewText(result);if(StopFailure!=null)status.Text+="\r\n"+StopFailure;
    }catch(Exception error){if(!dialog.IsDisposed&&!dialog.Disposing)status.Text=error.Message;}
    finally{running=false;stopping=false;if(!dialog.IsDisposed&&!dialog.Disposing)refresh();}
   };
   save.Click+=async delegate{
    if(running||completed||checkedResult==null)return;if(!IsReady){status.Text=StopFailure??"Wait for the current check to finish.";return;}var from=sourceFrame();var to=destinationFrame();
    if(!CurrentMacroBinding(bound)||!GeometryProofMatches(checkedResult,bound,from,to)){checkedResult=null;status.Text="The source or references changed. Check the pair again.";refresh();return;}
    if(String.IsNullOrWhiteSpace(name.Text)){status.Text="Give the new macro a name.";name.Focus();return;}
    running=true;stopping=false;status.Text="Saving this checked reference as a separate macro…";refresh();
    try{
     bool accepted=await Send(LocalApi.D("action","macro-variant","kind","geometry","source",bound,"source_frame",from,"destination_frame",to,"name",name.Text.Trim()));var result=lastCommandResult;await WaitForStopCompletion();if(dialog.IsDisposed||dialog.Disposing)return;
     if(!accepted){status.Text=feedback.Text;return;}completed=true;dialog.AcceptButton=null;var record=Map(Value(result,"record"));var proof=Map(Value(result,"reference_proof"));string id=Text(Value(result,"created_id"));
     if(id.Length==0||record==null||Text(Value(record,"id"))!=id||!CurrentMacroBinding(BoundMacro(record))||!GeometryProofMatches(proof,bound,from,to)||api.Json(Value(record,"recipe"))!=api.Json(Value(checkedResult,"recipe"))){status.Text="The save returned an unexpected receipt. Inspect Macro Base before retrying; the operation has not been changed.";return;}
     savedRecord=record;savedId=id;status.Text="Saved "+MacroName(record)+" · revision "+Text(Value(record,"version"))+"\r\nSelect saved macro to use it. It has not been inserted.\r\n\r\n"+GeometryReviewText(proof);string warning=Text(Value(result,"warning"));if(warning.Length>0)status.Text+="\r\n"+warning;if(StopFailure!=null)status.Text+="\r\n"+StopFailure;
    }catch(Exception error){if(!dialog.IsDisposed&&!dialog.Disposing)status.Text=error.Message;}
    finally{running=false;stopping=false;if(!dialog.IsDisposed&&!dialog.Disposing)refresh();}
   };
   select.Click+=delegate{if(running||savedRecord==null||!IsReady)return;if(!CurrentMacroBinding(BoundMacro(savedRecord))){status.Text="The saved macro changed. Select its current revision in Macro Base.";return;}chosen=savedId;dialog.Close();};
   dialog.Shown+=delegate{fromCell.Focus();};refresh();ShowOwned(dialog);
  }
  if(chosen!=null&&!closing){if(savedRecord==null||!CurrentMacroBinding(BoundMacro(savedRecord)))throw new InvalidOperationException("The saved macro changed. Choose its current revision again.");selectedMacro=chosen;selectedEffect=null;selectedApplicability=null;ShowCatalogue(true);Send(LocalApi.D("action","macro-effect","id",chosen,"select",true));}
 }
}
