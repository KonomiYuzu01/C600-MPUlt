// One presentation surface for the existing solve commands and authoritative work context.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 TableLayoutPanel solveContent,solveMacroBody,solveMacroSelection,solvePrepare,solveRoleGrid,solveProtection;
 FlowLayoutPanel solveMacroDetailActions;
 Panel solvePages;
 readonly Dictionary<string,Control> solvePageControls=new Dictionary<string,Control>();
 readonly Dictionary<string,Button> solvePageButtons=new Dictionary<string,Button>();
 readonly Dictionary<string,Button> solveRoles=new Dictionary<string,Button>();
 readonly Label solveTitle=new Label(),solveInspection=new Label(),solveGrip=new Label(),solveReference=new Label(),solveProtectionSummary=new Label();
 ListBox solveProtectionList;
 string solvePage="macros";
 bool fittingSolve,solveLayoutQueued;
 internal string ActiveSolvePage {get{return solvePage;}}

 Control BuildSolveContent(){
  if(solveContent!=null)return solveContent;
  solveContent=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty,Padding=new Padding(8,4,8,0),AccessibleName="Unified Solve workspace"};
  solveContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  solveContent.RowStyles.Add(new RowStyle(SizeType.Absolute,36));solveContent.RowStyles.Add(new RowStyle(SizeType.Absolute,34));solveContent.RowStyles.Add(new RowStyle(SizeType.Percent,100));solveContent.RowStyles.Add(new RowStyle(SizeType.Absolute,104));
  var top=Strip();top.Controls.Add(workGoal);top.Controls.Add(new Label{Text="Turns to",AutoSize=true,Padding=new Padding(5,5,0,0)});top.Controls.Add(destination);top.Controls.Add(Button("Work sheet","worksheet"));top.Controls.Add(Button("Filter","filter"));solveTitle.AutoSize=false;solveTitle.Size=new Size(180,30);solveTitle.AutoEllipsis=true;solveTitle.Padding=new Padding(8,5,0,0);top.Controls.Add(solveTitle);solveContent.Controls.Add(top,0,0);
  var navigation=Strip();foreach(string page in new[]{"macros","prepare","protection"}){string key=page;var button=Button(page=="macros"?"Macros":page=="prepare"?"Prepare":"Protection","solve-"+page);button.AccessibleName="Solve "+page+" section";solvePageButtons.Add(key,button);navigation.Controls.Add(button);}solveContent.Controls.Add(navigation,0,1);
  solvePages=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};solveContent.Controls.Add(solvePages,0,2);
  // Move the existing controls; selection, drafts, effect inspection and registry stay singular.
  var macroUse=catalogue.GetControlFromPosition(0,6);catalogue.Controls.Remove(macroState);catalogue.Controls.Remove(effectText);catalogue.Controls.Remove(macroUse);
  ((FlowLayoutPanel)catalogue.GetControlFromPosition(0,2)).WrapContents=true;
  for(int row=4;row<7;row++)catalogue.RowStyles[row].Height=0;
  solveMacroBody=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty,AutoScroll=true};solveMacroBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,51));solveMacroBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,49));solveMacroBody.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  catalogue.Padding=new Padding(0,4,8,4);catalogue.Visible=true;solveMacroBody.Controls.Add(catalogue,0,0);
  var selected=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(8,4,0,4),Margin=Padding.Empty};solveMacroSelection=selected;selected.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));selected.RowStyles.Add(new RowStyle(SizeType.Absolute,54));selected.RowStyles.Add(new RowStyle(SizeType.Percent,100));selected.RowStyles.Add(new RowStyle(SizeType.Absolute,38));selected.RowStyles.Add(new RowStyle(SizeType.Absolute,38));
  macroState.Margin=Padding.Empty;effectText.Margin=Padding.Empty;macroState.AutoEllipsis=true;macroState.AccessibleDescription="Selected macro; complete name and canonical record remain in Details.";effectText.AutoEllipsis=true;selected.Controls.Add(macroState,0,0);selected.Controls.Add(effectText,0,1);selected.Controls.Add(macroUse,0,2);var details=Strip();solveMacroDetailActions=details;details.WrapContents=true;details.Controls.Add(Button("Details","macro-details"));details.Controls.Add(Button("Compare","macro-compare"));details.Controls.Add(Button("New macro","macro-new"));details.Controls.Add(Button("Map frame","macro-geometry"));details.Controls.Add(Button("Compare current use","macro-candidates"));BuildKnownEndgame(details);selected.Controls.Add(details,0,3);solveMacroBody.Controls.Add(selected,1,0);AddSolvePage("macros",solveMacroBody);
  AddSolvePage("prepare",BuildSolvePrepare());AddSolvePage("protection",BuildSolveProtection());
  workDock.Visible=true;solveContent.Controls.Add(workDock,0,3);solveContent.SizeChanged+=delegate{FitSolveWindow();};operationStrip.Layout+=delegate{FitSolveWindow();};foreach(var panel in new[]{solveMacroBody,solveMacroSelection,solvePrepare,solveRoleGrid,solveProtection})panel.ClientSizeChanged+=delegate{QueueSolveLayout();};
  SelectSolvePage(solvePage,false);DrawSolveWindow();return solveContent;
 }
 void AddSolvePage(string id,Control content){content.Dock=DockStyle.Fill;content.Visible=false;solvePageControls.Add(id,content);solvePages.Controls.Add(content);}
 FlowLayoutPanel SolveActions(params string[] items){var row=Strip();row.WrapContents=true;foreach(string item in items){var pair=item.Split('|');row.Controls.Add(Button(pair[0],pair[1]));}return row;}
 Control BuildSolvePrepare(){
  var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Margin=Padding.Empty,AutoScroll=true};solvePrepare=grid;grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(int height in new[]{40,84,38,48,38,38})grid.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
  solveInspection.Dock=DockStyle.Fill;solveInspection.Margin=Padding.Empty;solveInspection.Padding=new Padding(4);solveInspection.AutoEllipsis=true;solveInspection.AccessibleName="Inspected source for explicit role assignment";grid.Controls.Add(solveInspection,0,0);
  var roles=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=2,Margin=Padding.Empty};solveRoleGrid=roles;roles.RowStyles.Add(new RowStyle(SizeType.Percent,100));roles.RowStyles.Add(new RowStyle(SizeType.Absolute,34));
  foreach(string role in new[]{"a","b","target"}){int column=solveRoles.Count;roles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));string key=role;var inspect=Button(role=="target"?"Target":role.ToUpperInvariant(),"solve-locate-"+role);inspect.AutoSize=false;inspect.AutoEllipsis=false;inspect.Dock=DockStyle.Fill;inspect.Margin=new Padding(2);inspect.TextAlign=ContentAlignment.TopLeft;inspect.Padding=new Padding(5);inspect.FlatAppearance.BorderSize=1;inspect.FlatAppearance.BorderColor=Rule;inspect.AccessibleName="Inspect fixed "+(role=="target"?"Target":role.ToUpperInvariant())+" position";solveRoles.Add(role,inspect);roles.Controls.Add(inspect,column,0);roles.Controls.Add(Button("Use inspected","assign-"+key),column,1);}
  grid.Controls.Add(roles,0,1);grid.Controls.Add(SolveActions("Current|set-current","Add Home|block-add","Capture|block-capture","Place only|block-capture-position","Remove|block-remove","Next|next-menu"),0,2);
  var frame=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};frame.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));frame.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));solveGrip.Dock=DockStyle.Fill;solveGrip.Margin=Padding.Empty;solveGrip.Padding=new Padding(4);solveGrip.AutoEllipsis=true;solveGrip.AccessibleName="Active Grip and exact ordered frame";solveReference.Dock=DockStyle.Fill;solveReference.Margin=Padding.Empty;solveReference.Padding=new Padding(4);solveReference.AutoEllipsis=true;solveReference.AccessibleName="Explicit reference word";frame.Controls.Add(solveGrip,0,0);frame.Controls.Add(solveReference,1,0);grid.Controls.Add(frame,0,3);
  grid.Controls.Add(SolveActions("Capture grips|capture","Frame|grip-frame","Reference|reference","Local center|local-center-grip","Keyboard|keyboard"),0,4);
  grid.Controls.Add(SolveActions("Edit A / B|roles","Capture goal|target-capture","Home goal|target-home","Move block goal|block-reference","Invert prepare|cleanup-inverse","Residuals|residual-details","Last step|journal-delta","Next orbit|orbit-following"),0,5);return grid;
 }
 Control BuildSolveProtection(){
  var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty,AutoScroll=true};solveProtection=grid;grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,48));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,30));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,72));grid.RowStyles.Add(new RowStyle(SizeType.Percent,100));solveProtectionSummary.Dock=DockStyle.Fill;solveProtectionSummary.Margin=Padding.Empty;solveProtectionSummary.Padding=new Padding(4);solveProtectionSummary.AutoEllipsis=true;solveProtectionSummary.AccessibleName="Declared protection and exact check scope";grid.Controls.Add(solveProtectionSummary,0,0);
  solveProtectionList=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false,BorderStyle=BorderStyle.None,BackColor=Paper,ForeColor=Ink,AccessibleName="Declared protected orbits and positions",HorizontalScrollbar=true};grid.Controls.Add(BuildRecommendation(),0,1);grid.Controls.Add(solveProtectionList,0,3);
  var actions=SolveActions("Orbit boundary|protection","Hold exact|block-protect","Hold place|block-protect-position","Free exact|block-unprotect-exact","Free place|block-unprotect-position","Free both|block-unprotect","Locate conflict|review-locate","Findings|review-details");actions.Controls.Add(strict);grid.Controls.Add(actions,0,2);return grid;
 }
  void EditOrbitProtection(){
   var declared=new HashSet<int>(Items(work["protected"]).Select(Number));
   string captured=api.Json(declared.OrderBy(id=>id).ToArray());int current=Number(Workspace["orbit"]);
   using(var dialog=ToolDialog("Orbit protection",680,470)){
    var grid=ToolLayout(dialog,44,-1,36,42);
    grid.Controls.Add(ToolLabel("Working orbit · "+OrbitName(current)),0,0);
    var list=new CheckedListBox{Dock=DockStyle.Fill,IntegralHeight=false,BorderStyle=BorderStyle.None,CheckOnClick=true,HorizontalScrollbar=true,AccessibleName="Protected orbits by mathematical name",AccessibleDescription="Space toggles the selected orbit. Apply commits the complete checked set; unchecked orbits remain unprotected."};
    foreach(var profile in Items(Value(structure,"orbit_profiles")).Select(Map)){
     int id=Number(profile["orbit"]);int row=list.Items.Add(new Choice(id.ToString(),OrbitName(id)),declared.Contains(id));if(id==current)list.SelectedIndex=row;
    }
    grid.Controls.Add(list,0,1);var status=ToolLabel("Space: toggle orbit · Apply: change boundary · Esc: cancel");grid.Controls.Add(status,0,2);
    var actions=ToolRow();Button apply=ToolButton("Apply boundary"),cancel=ToolButton("Cancel");cancel.DialogResult=DialogResult.Cancel;actions.Controls.Add(apply);actions.Controls.Add(cancel);grid.Controls.Add(actions,0,3);dialog.CancelButton=cancel;dialog.AcceptButton=apply;
    bool applying=false;Action<string> reject=message=>{status.Text=message;tips.SetToolTip(status,message);};
    apply.Click+=async delegate{
     if(applying)return;
     if(!IsReady){reject("Finish or stop the current operation first. Your choices are kept.");return;}
     if(captured!=api.Json(Items(work["protected"]).Select(Number).OrderBy(id=>id).ToArray())){reject("Protection changed. Cancel and reopen the current boundary; your choices were not applied.");return;}
     int[] chosen=list.CheckedItems.Cast<Choice>().Select(choice=>Int32.Parse(choice.Id)).OrderBy(id=>id).ToArray();
     applying=true;list.Enabled=apply.Enabled=cancel.Enabled=false;
     try{bool accepted=await Send(LocalApi.D("action","protect","orbits",chosen));applying=false;if(dialog.IsDisposed)return;if(accepted)dialog.Close();else reject(feedback.Text);}
     catch(Exception error){reject(error.Message);}
     finally{applying=false;if(!dialog.IsDisposed)list.Enabled=apply.Enabled=cancel.Enabled=true;}
    };
    dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(applying)e.Cancel=true;};dialog.Shown+=delegate{list.Focus();};ShowOwned(dialog);
   }
  }
 void OpenSolvePage(string page){if(mode!="g2")return;OpenWorkWindow("solve");SelectSolvePage(page,true);}
 void InspectSolveRole(string role){var roles=Items(Value(Workspace,"roles"));object raw=role=="target"?Value(Workspace,"target"):role=="a"&&roles.Length>0?roles[0]:role=="b"&&roles.Length>1?roles[1]:null;if(raw==null)throw new InvalidOperationException("Assign the "+role.ToUpperInvariant()+" fixed position first.");Send(LocalApi.D("action","inspect-position","position",Number(raw)));}
 void SelectSolvePage(string page,bool focus){
  if(!solvePageControls.ContainsKey(page))return;solvePage=page;foreach(var pair in solvePageControls)pair.Value.Visible=pair.Key==page;foreach(var pair in solvePageButtons){pair.Value.BackColor=pair.Key==page?Color.FromArgb(34,45,56):Paper;pair.Value.FlatAppearance.BorderSize=pair.Key==page?1:0;pair.Value.FlatAppearance.BorderColor=Accent;}solvePageControls[page].BringToFront();
  if(focus){if(page=="macros")search.Focus();else solvePageButtons[page].Focus();}FitSolveWindow();
 }
 void FitSolveWindow(){
  if(solveContent==null||fittingSolve||solveContent.IsDisposed)return;fittingSolve=true;try{
   int line=form.Font.Height;solveTitle.Width=Math.Max(80,solveContent.Width-530);
   foreach(var table in new[]{solveContent,catalogue,solveMacroSelection,solvePrepare,solveProtection,workDock})MeasureSolveActionRows(table);
   SetSolveRowHeight(workDock,0,line*2+21);SetSolveRowHeight(solveContent,3,workDock.RowStyles[0].Height+workDock.RowStyles[1].Height);
   if(solveRoleGrid!=null){int width=Math.Max(100,solveRoleGrid.ClientSize.Width/3-4),height=0,assignment=0;foreach(var button in solveRoles.Values)height=Math.Max(height,SolveRoleTextHeight(button,width)+button.Margin.Vertical);foreach(Control child in solveRoleGrid.Controls)if(solveRoleGrid.GetRow(child)==1)assignment=Math.Max(assignment,child.PreferredSize.Height+child.Margin.Vertical);SetSolveRowHeight(solveRoleGrid,1,assignment);SetSolveRowHeight(solvePrepare,1,height+assignment);}
   if(solvePrepare!=null){SetSolveRowHeight(solvePrepare,0,solveInspection.GetPreferredSize(new Size(Math.Max(100,solvePrepare.ClientSize.Width),0)).Height);int frameWidth=Math.Max(100,solvePrepare.ClientSize.Width);SetSolveRowHeight(solvePrepare,3,Math.Max(solveGrip.GetPreferredSize(new Size((int)(frameWidth*.6),0)).Height,solveReference.GetPreferredSize(new Size((int)(frameWidth*.4),0)).Height));solvePrepare.AutoScrollMinSize=new Size(0,(int)Math.Ceiling(solvePrepare.RowStyles.Cast<RowStyle>().Sum(row=>row.Height)));}
   if(solveProtection!=null){SetSolveRowHeight(solveProtection,0,solveProtectionSummary.GetPreferredSize(new Size(Math.Max(100,solveProtection.ClientSize.Width),0)).Height);SetSolveRowHeight(solveProtection,1,FitRecommendation(Math.Max(100,solveProtection.ClientSize.Width)));solveProtection.AutoScrollMinSize=new Size(0,(int)Math.Ceiling(solveProtection.RowStyles[0].Height+solveProtection.RowStyles[1].Height+solveProtection.RowStyles[2].Height)+solveProtectionList.ItemHeight*3);}
   if(solveMacroSelection!=null){int details=(int)Math.Ceiling(solveMacroSelection.RowStyles.Cast<RowStyle>().Where(row=>row.SizeType==SizeType.Absolute).Sum(row=>row.Height))+effectText.GetPreferredSize(new Size(Math.Max(100,solveMacroSelection.ClientSize.Width-solveMacroSelection.Padding.Horizontal),0)).Height+solveMacroSelection.Padding.Vertical;int library=(int)Math.Ceiling(catalogue.RowStyles.Cast<RowStyle>().Where(row=>row.SizeType==SizeType.Absolute).Sum(row=>row.Height))+macros.ItemHeight*2+catalogue.Padding.Vertical;solveMacroBody.AutoScrollMinSize=new Size(0,Math.Max(details,library));}
  }finally{fittingSolve=false;}
 }
 static void SetSolveRowHeight(TableLayoutPanel table,int index,float height){var row=table.RowStyles[index];if(row.SizeType!=SizeType.Absolute)row.SizeType=SizeType.Absolute;if(row.Height!=height)row.Height=height;}
 void QueueSolveLayout(){if(solveLayoutQueued||closing||solveContent==null||solveContent.IsDisposed||solveContent.Disposing||!solveContent.IsHandleCreated)return;solveLayoutQueued=true;try{solveContent.BeginInvoke((MethodInvoker)delegate{solveLayoutQueued=false;if(!closing&&!solveContent.IsDisposed)FitSolveWindow();});}catch(InvalidOperationException){solveLayoutQueued=false;}}
 void MeasureSolveActionRows(TableLayoutPanel table){if(table==null)return;foreach(Control child in table.Controls){var row=child as FlowLayoutPanel;if(row==null)continue;int index=table.GetRow(row);if(index>=0&&table.GetRowSpan(row)==1)SetSolveRowHeight(table,index,SolveWrappedHeight(row,Math.Max(100,table.ClientSize.Width-table.Padding.Horizontal-row.Margin.Horizontal))+row.Margin.Vertical);}}
 int SolveWrappedHeight(FlowLayoutPanel row,int width){int total=row.Padding.Vertical,line=0,x=row.Padding.Left,available=Math.Max(1,width-row.Padding.Right);foreach(Control child in row.Controls){if(child==endgamePanel&&!endgameExpanded)continue;if(row==operationStrip){string command;if(child==operationNotice&&!OperationExecuted&&Map(Value(Map(Value(work,"review")),"goal_result"))==null)continue;if(child is Button&&commandButtons.TryGetValue((Button)child,out command)){bool after=command=="operation-new"||command=="operation-reuse";if(command!="review-details"&&command!="cancel-analysis"&&after!=OperationExecuted)continue;}}var size=child.AutoSize?child.GetPreferredSize(Size.Empty):child.Size;int w=size.Width+child.Margin.Horizontal,h=size.Height+child.Margin.Vertical;if(row.WrapContents&&x>row.Padding.Left&&x+w>available){total+=line;line=0;x=row.Padding.Left;}x+=w;line=Math.Max(line,h);}return total+line;}
 internal static int SolveRoleTextHeight(Button button,int width){return button.GetPreferredSize(new Size(Math.Max(1,width),Int32.MaxValue)).Height;}
 string SolvePositionName(int position){
  var records=Items(Value(work,"buffers")).Select(Map).Concat(new[]{Map(Value(work,"current")),Map(Value(work,"next")),Map(Value(work,"inspected")),Map(Value(work,"target"))});var record=records.FirstOrDefault(p=>p!=null&&Number(Value(p,"position")??-1)==position);var names=Map(Value(record,"names"));string value=Text(Value(names,"current_short"));return value.Length>0?value:"Position P"+position;
 }
 void DrawSolveWindow(){
  if(solveContent==null||work==null)return;solveTitle.Text=Text(Value(Map(Value(Workspace,"block")),"name"));tips.SetToolTip(solveTitle,OrbitName(Number(Workspace["orbit"]))+" · "+solveTitle.Text);
  foreach(var entry in commandButtons.Where(pair=>!pair.Key.IsDisposed&&solveContent.Contains(pair.Key))){string key=KeyHint(entry.Value);if(key.Contains("+"))key="";entry.Key.Text=Text(entry.Key.Tag)+key;}
  foreach(var entry in commandButtons.Where(pair=>!pair.Key.IsDisposed&&solveContent.Contains(pair.Key)&&pair.Value=="macro-insert")){string key=KeyHint(entry.Value);entry.Key.Text="Add to "+Text(Workspace["phase"])+(key.Contains("+")?"":key);}
  var inspected=Selection;solveInspection.Text=inspected==null?"Inspect a piece or fixed position in the scene.":"Inspected · "+PieceCaption(inspected);tips.SetToolTip(solveInspection,solveInspection.Text);
  var roles=Items(Value(Workspace,"roles"));foreach(var pair in solveRoles){object raw=pair.Key=="target"?Value(Workspace,"target"):roles.Length>(pair.Key=="a"?0:1)?roles[pair.Key=="a"?0:1]:null;int id=raw==null?-1:Number(raw);pair.Value.Tag=id;pair.Value.Enabled=id>=0;string hint=KeyHint("solve-locate-"+pair.Key);pair.Value.Text=(pair.Key=="target"?"Target":pair.Key.ToUpperInvariant())+" · fixed position"+(hint.Contains("+")?"":hint)+"\n"+(id<0?"Unassigned":SolvePositionName(id));tips.SetToolTip(pair.Value,pair.Value.Text+(id<0?"":"\nCanonical P"+id)+"\nInspect only; Use inspected assigns explicitly."+hint);}
  RefreshSolveGrip();var reference=Items(Value(Workspace,"reference"));solveReference.Text="Reference R · "+reference.Length+" turns\n"+(reference.Length==0?"Identity":String.Join(" ",reference.Take(12).Select(Text).ToArray())+(reference.Length>12?" …":""));tips.SetToolTip(solveReference,solveReference.Text+"\nOpen Reference for the complete retained word. Explicit legal word; no spatial-frame certification.");
  var review=Map(Value(work,"review"));var prefix=Map(Value(review,"prefix"));bool stale=Text(Value(review,"status"))=="Stale";solveProtectionSummary.Text="Complete operation · "+(review==null?"Not checked":Text(Value(review,"status")))+"\nDuring turns · "+(stale?"Stale — check again":prefix==null?"Not checked":Text(Value(prefix,"status")));tips.SetToolTip(solveProtectionSummary,"Prepare, Macro and Cleanup; hidden pieces are included. The declared boundary alone is not a safety result.");
  var lines=new List<string>();foreach(object raw in Items(Value(work,"protected")))lines.Add("Orbit · "+OrbitName(Number(raw)));foreach(var item in Items(Value(work,"position_locks")??Value(Map(Value(Workspace,"block")),"protected")).Select(Map)){if(item!=null)lines.Add((Text(Value(item,"mode"))=="position"?"Keep occupant · ":"Keep exact · ")+SolvePositionName(Number(item["position"])));}if(lines.Count==0)lines.Add("No protection declared");if(!solveProtectionList.Items.Cast<string>().SequenceEqual(lines)){solveProtectionList.BeginUpdate();solveProtectionList.Items.Clear();solveProtectionList.Items.AddRange(lines.Cast<object>().ToArray());solveProtectionList.EndUpdate();}DrawRecommendation();DrawKnownEndgame();FitSolveWindow();
 }
 void RefreshSolveGrip(){if(solveContent==null||input==null)return;solveGrip.Text=input.ActiveGripCell.HasValue?"Grip · "+CellName(input.ActiveGripCell.Value)+"\n"+FrameCaption(input.ActiveGripCell.Value):"Grip · unassigned";tips.SetToolTip(solveGrip,solveGrip.Text);}
}
