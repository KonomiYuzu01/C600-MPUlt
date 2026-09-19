// Experimental Piece Filter editor. Expressions remain authoritative in the backend.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 internal static string PieceFilterPreset(string preset,int? identity,int? position) {
  if(preset=="active"||preset=="all")return preset;
  int? value=preset=="identity"?identity:preset=="position"?position:null;
  if(!value.HasValue||value.Value<0||value.Value>=177120)throw new ArgumentException("Choose a Current piece before using its identity or position.");
  return (preset=="identity"?"piece(":"position(")+value.Value+")";
 }
 void ShowPieceFilter() {
  if(!connected||busy){Say("Finish the current check before editing Piece Filter. No filter was changed.",true);return;}
  var metadata=Map(Value(work,"piece_filter"));
  if(metadata==null){Say("Current Piece Filter metadata is unavailable. Reconnect before editing its rules.",true);return;}
  var current=Map(Value(work,"current"));int? identity=current==null?(int?)null:Number(current["piece"]),position=current==null?(int?)null:Number(current["position"]);
  Func<string> context=()=>api.Json(new[]{Value(work,"guard"),Value(Map(Value(work,"piece_filter")),"context_hash"),Value(Map(Value(work,"piece_filter")),"rules")});
  string initial=Value(metadata,"expression") as string,originalContext=context();
  using(var dialog=ToolDialog("Piece Filter",650,410)){
   var grid=ToolLayout(dialog,48,46,76,34,64,-1,40);
   var applied=new TextBox{ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,AccessibleName="Applied Piece Filter rules"};
   applied.Text=initial!=null?"Applied: "+initial:"Applied rules: "+api.Json(Value(metadata,"rules"));
   applied.Text+="\r\n"+(Text(Value(metadata,"status"))=="evaluated"?Value(metadata,"matched_pieces")+" matching pieces · "+Value(metadata,"matched_stickers")+" stickers":"Match counts unavailable: "+Text(Value(metadata,"reason")));
   grid.Controls.Add(applied,0,0);
   var presets=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=1,Margin=Padding.Empty};for(int i=0;i<4;i++)presets.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));grid.Controls.Add(presets,0,1);
   var expressionPanel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};expressionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute,20));expressionPanel.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   var expression=new TextBox{Multiline=true,AcceptsReturn=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Text=initial??"",AccessibleName="Piece Filter expression"};expressionPanel.Controls.Add(new Label{Text="&Expression",AutoSize=true},0,0);expressionPanel.Controls.Add(expression,0,1);grid.Controls.Add(expressionPanel,0,2);
   var operation=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Fill,AccessibleName="How to combine Piece Filter"};operation.Items.Add(new Choice("replace","Replace the applied filter"));
   if(initial!=null){operation.Items.Add(new Choice("union","Union · add matches"));operation.Items.Add(new Choice("intersect","Intersect · keep common matches"));operation.Items.Add(new Choice("subtract","Subtract · remove matches"));}operation.SelectedIndex=0;grid.Controls.Add(operation,0,3);
   grid.Controls.Add(ToolLabel("Syntax: &  |  !  ( ) · piece(n) follows identity; position(n) stays fixed.\nPuzzle: exact piece mask. Hub: dimmed non-matches.\nLocal/Global: cell context and matching sticker counts."),0,4);
   var status=ToolLabel(initial==null?"This filter uses styled or multiple rules. Enter an expression to replace them explicitly; the original rules stay applied until then.":"Presets edit the expression only. Tab to Apply filter when ready.");status.AccessibleName="Piece Filter feedback";grid.Controls.Add(status,0,5);
   string[] keys={"identity","position","active","all"};string[] labels={"Current "+(identity.HasValue?"I"+identity:"unassigned"),"At "+(position.HasValue?"P"+position:"unassigned"),"Working orbit","All pieces"};
   string[] names={"Current identity "+(identity.HasValue?"I"+identity:"unassigned"),"Current position "+(position.HasValue?"P"+position:"unassigned"),"Working orbit active","All pieces all"};
   string[] descriptions={"Follow Current's physical identity as it moves.","Keep Current's present fixed position; its occupant may change.","Match pieces in the working orbit.","Match all pieces."};
   for(int i=0;i<keys.Length;i++){string key=keys[i];var button=ToolButton(labels[i]);button.Dock=DockStyle.Fill;button.AutoSize=false;button.Padding=Padding.Empty;button.Margin=new Padding(2);button.AccessibleName="Edit expression: "+names[i];button.Enabled=i>=2||current!=null;tips.SetToolTip(button,descriptions[i]+"\nThis only edits the expression. Apply filter to use it.");button.Click+=delegate{expression.Text=PieceFilterPreset(key,identity,position);expression.Focus();expression.SelectAll();status.Text="Expression changed locally. Apply filter to use it.";status.ForeColor=toolInk;};presets.Controls.Add(button,i,0);}
   var actions=ToolRow();Button apply=ToolButton("&Apply filter"),cancel=ToolButton("Cancel");apply.AccessibleName="Apply Piece Filter explicitly";cancel.DialogResult=DialogResult.Cancel;actions.Controls.Add(apply);actions.Controls.Add(cancel);grid.Controls.Add(actions,0,6);dialog.CancelButton=cancel;
   bool applying=false;
   dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(applying&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;status.Text="Applying the explicit filter. Wait for its result; your expression is kept.";}};
   apply.Click+=async delegate{
    if(applying)return;
    if(String.IsNullOrWhiteSpace(expression.Text)){status.Text="Enter a filter expression, for example active or piece(35778). Your input is kept.";status.ForeColor=Color.FromArgb(238,141,135);expression.Focus();return;}
    if(!connected||busy||originalContext!=context()){status.Text="The work context changed or a check is busy. Your expression is kept; reopen Piece Filter for the current work before applying.";status.ForeColor=Color.FromArgb(238,141,135);return;}
    applying=true;apply.Enabled=cancel.Enabled=expression.Enabled=operation.Enabled=presets.Enabled=false;status.Text="Applying the explicit filter…";
    try{
     bool accepted=await Send(LocalApi.D("action","filter","expression",expression.Text,"operation",((Choice)operation.SelectedItem).Id));
     applying=false;
     if(dialog.IsDisposed)return;
     if(accepted){dialog.Close();return;}
     status.Text=feedback.Text;status.ForeColor=Color.FromArgb(238,141,135);
    }catch(Exception error){if(!dialog.IsDisposed){status.Text=error.Message;status.ForeColor=Color.FromArgb(238,141,135);}}
    finally{applying=false;if(!dialog.IsDisposed){apply.Enabled=cancel.Enabled=expression.Enabled=operation.Enabled=presets.Enabled=true;expression.Focus();}}
   };
   dialog.Shown+=delegate{expression.Focus();expression.SelectAll();};ShowOwned(dialog);
  }
 }
}
