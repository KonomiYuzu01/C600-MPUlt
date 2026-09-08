// Focused in-process WinForms control test. No MPUlt process, GPU timing, global
// keyboard injection, HTTP service, or personal session is used by this fixture.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

internal static class NativeStructureExplorerRegression {
 static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static T Get<T>(object target,string name){return (T)target.GetType().GetField(name,Fields).GetValue(target);}
 static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
 static void Invoke(object target,string name,object value){target.GetType().GetMethod(name,Fields).Invoke(target,new[]{value});}
 static IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(Control next in Descendants(child))yield return next;}}
 static void Click(Control parent,string text){var button=Descendants(parent).OfType<Button>().Single(b=>b.Text==text);button.PerformClick();}
 static int[][] Rows(object source){return ((IEnumerable)source).Cast<object>().Select(row=>((IEnumerable)row).Cast<object>().Select(Convert.ToInt32).ToArray()).ToArray();}
 static void Reject(Action action){try{action();}catch(ArgumentException){return;}throw new InvalidOperationException("Invalid topology was accepted.");}

 internal static List<string> Run(IDictionary<string,object> topology,string captureDirectory=null){
  var checks=new List<string>();int[][] adjacency=Rows(topology["adjacency"]),vertices=Rows(topology["vertex_incidence"]);string model=Convert.ToString(topology["model_id"]);
  using(var host=new Form{Text="Isolated structure explorer control fixture",Font=new Font("Segoe UI",9F),ClientSize=new Size(330,720),StartPosition=FormStartPosition.Manual,Location=new Point(80,80)})
  using(var explorer=new NativeStructureExplorer{Dock=DockStyle.Fill}){
   host.Controls.Add(explorer);host.Show();Application.DoEvents();
   int centers=0,inserts=0,applies=0,previews=0;string inserted=null;NativeStructureFilterRequest last=null;
   explorer.CenterRequested+=delegate(int color){centers++;Require(color==explorer.SelectedColor,"Center event used a noncanonical ID.");};
   explorer.InsertRequested+=delegate(string value){inserts++;inserted=value;};
   explorer.PreviewRequested+=delegate(NativeStructureFilterRequest value){previews++;last=value;};explorer.ApplyRequested+=delegate(NativeStructureFilterRequest value){applies++;Require(value==last,"Apply changed the preview request.");};
   explorer.Configure(model,adjacency,vertices,null);
   Require(explorer.ModelId==model&&explorer.SelectedColor==1&&centers==0&&previews==0,"Configure performed an external action.");
   var graph=Get<NativeColorGraph>(explorer,"graph");var neighborPanel=Get<FlowLayoutPanel>(explorer,"neighbors");var vertexList=Get<ListBox>(explorer,"vertexColors");
   var vertexPanel=Get<FlowLayoutPanel>(explorer,"colorVertices");var initialNeighbors=neighborPanel.Controls.Cast<Control>().ToArray();var initialVertices=vertexPanel.Controls.Cast<Control>().ToArray();
   Require(neighborPanel.Controls.Count==4&&vertexList.Items.Count==20,"Neighbor or vertex incidence display is incomplete.");checks.Add("Immutable topology, four cell neighbors, and twenty incident vertex colors");
   int original=adjacency[0][0];adjacency[0][0]=600;Require(neighborPanel.Controls[0].Text!="C600"||original==600,"Caller mutation changed configured topology.");adjacency[0][0]=original;
   var malformed=adjacency.Select(row=>(int[])row.Clone()).ToArray();malformed[0][0]=1;Reject(delegate{explorer.Configure("bad",malformed,vertices,null);});
   Require(explorer.ModelId==model&&explorer.SelectedColor==1,"Rejected topology replaced current navigation state.");checks.Add("Topology copies and validation preserve the previous configuration on failure");
   var hops=Get<ComboBox>(explorer,"hops");hops.SelectedIndex=0;Require(graph.NodeCount==5,"One-hop graph must show root and four neighbors.");hops.SelectedIndex=1;Require(graph.NodeCount>5,"Two-hop graph omitted its second ring.");
   int before=explorer.SelectedColor;Point point=graph.NodePoint(original);Invoke(graph,"OnMouseMove",new MouseEventArgs(MouseButtons.None,0,point.X,point.Y,0));
   Require(explorer.SelectedColor==before&&previews==0&&centers==0,"Hover performed an external or navigation action.");
   graph.Focus();var right=new KeyEventArgs(Keys.Right);Invoke(graph,"OnKeyDown",right);Require(right.Handled&&explorer.SelectedColor==before,"Arrow key did not retain explicit activation semantics.");Invoke(graph,"OnKeyDown",new KeyEventArgs(Keys.Enter));Require(explorer.SelectedColor!=before&&centers==0&&previews==0,"Keyboard navigation refreshed the viewport or preview.");
   explorer.NavigateColor(1);point=graph.NodePoint(original);Invoke(graph,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0));Require(explorer.SelectedColor==original&&centers==0&&previews==0,"Graph click failed local navigation.");
   explorer.NavigateColor(600);Require(neighborPanel.Controls.Cast<Control>().SequenceEqual(initialNeighbors)&&vertexPanel.Controls.Cast<Control>().SequenceEqual(initialVertices),"Navigation recreated controls instead of preserving keyboard focus and HWNDs.");
   Require(neighborPanel.Controls.Cast<Control>().Select(c=>c.Text).SequenceEqual(adjacency[599].OrderBy(n=>n).Select(n=>"C"+n)),"Reused neighbor buttons show stale targets.");
   int nextNeighbor=Int32.Parse(initialNeighbors[0].Text.Substring(1));((Button)initialNeighbors[0]).PerformClick();Require(explorer.SelectedColor==nextNeighbor,"Reused neighbor button retained its previous navigation target.");
   var taskTabs=Get<TabControl>(explorer,"navigation");taskTabs.SelectedIndex=2;int nextVertex=Int32.Parse(initialVertices[0].Text.Substring(1));((Button)initialVertices[0]).PerformClick();Require(explorer.SelectedVertex==nextVertex&&explorer.SelectedColor==nextNeighbor,"Reused vertex button changed the origin or retained its old target.");taskTabs.SelectedIndex=0;
   checks.Add("One/two-hop graph, inert hover, real control keyboard handlers, and reused local navigation controls");
   explorer.NavigateColor(1);var layers=Get<ComboBox>(explorer,"layerChoice");var colors=Get<ListBox>(explorer,"layerColors");var seen=new HashSet<string>();int sum=0;
   for(int layer=0;layer<layers.Items.Count;layer++){explorer.SelectLayer(layer);foreach(object color in colors.Items){Require(seen.Add(color.ToString()),"Cell appeared in more than one BFS layer.");sum++;}}
   Require(sum==600,"Cell layers do not cover all six hundred cells.");explorer.SelectLayer(0);Require(colors.Items.Count==1&&colors.Items[0].ToString()=="C1","Layer zero is not the selected cell center.");
   explorer.SelectLayer(1);Require(colors.Items.Count==4,"Layer one is not the four shared-face neighbors.");
   Require(explorer.GeneratePredicate("color")=="color(C1)"&&explorer.GeneratePredicate("cell")=="cell(C1)"&&explorer.GeneratePredicate("adjacent")=="adjacent(C1)"&&explorer.GeneratePredicate("layer")=="layer(C1,L1)"&&explorer.GeneratePredicate("home_layer")=="home_layer(C1,L1)","Generated predicate changed canonical color or layer semantics.");
   for(int vertex=1;vertex<=120;vertex++){explorer.SelectVertex(vertex);Require(vertexList.Items.Count==20&&explorer.SelectedColor==1,"Vertex lookup changed the layer origin or lost incident cells.");}
   checks.Add("Cell-centered BFS partition, exact canonical predicates, and all 120 vertices as navigation aids");
   var search=Get<TextBox>(explorer,"colorSearch");foreach(string invalid in new[]{"C0","C601","V1","1.5",""}){search.Text=invalid;Click(explorer,"Find color");Require(explorer.SelectedColor==1,"Invalid color search changed selection.");}
   search.Text="C600";Click(explorer,"Find color");Require(explorer.SelectedColor==600,"Canonical endpoint color cannot be found.");Click(explorer,"Center viewport");Require(centers==1,"Center must require its explicit button.");checks.Add("Invalid search preserves selection; C600 and explicit viewport-center event");
   Click(explorer,"Insert expression");Require(inserts==1&&inserted=="color(C600)"&&previews==0,"Insert should transfer only the expression.");
   var apply=Get<Button>(explorer,"applyButton");Require(!apply.Enabled,"Apply is enabled before a count preview.");Click(explorer,"Preview counts");Require(previews==1&&last.Predicate=="color(C600)"&&last.Composition=="replace","Preview request contract is incorrect.");
   explorer.ShowPreview(last.Id+1,1,1,null);Require(!apply.Enabled,"Stale response enabled Apply.");string detailed="Complete rules\r\n"+String.Join("\r\n",Enumerable.Repeat("{expr: color(C600), style: solid}",20));explorer.ShowPreview(last.Id,20,30,detailed);Require(apply.Enabled,"Current counts did not enable Apply.");
   var review=Get<TextBox>(explorer,"previewInfo");Require(review.Multiline&&review.ReadOnly&&review.Height>=140&&review.ScrollBars==ScrollBars.Vertical&&review.Text.Contains(detailed),"Complete composed rules are not scrollable and reviewable.");
   explorer.SetBusy(true);Click(explorer,"Apply filter");Require(applies==0,"Busy control applied a filter.");explorer.SetBusy(false);Click(explorer,"Apply filter");Require(applies==1&&!apply.Enabled,"Apply was not tied to one explicit preview.");
   Click(explorer,"Preview counts");long old=last.Id;explorer.NavigateColor(1);explorer.ShowPreview(old,20,30,null);Require(!apply.Enabled,"Navigation reused stale counts.");
   var composition=Get<ComboBox>(explorer,"compositionChoice");composition.SelectedIndex=1;Click(explorer,"Preview counts");Require(last.Composition=="intersect","Intersection composition missing.");explorer.ShowPreview(last.Id,20,30,null);composition.SelectedIndex=2;Require(!apply.Enabled,"Composition change retained prior counts.");Click(explorer,"Preview counts");Require(last.Composition=="union","Union composition missing.");
   long failed=last.Id;explorer.ShowError("Fixture rejection");explorer.ShowPreview(failed,20,30,null);Require(!apply.Enabled,"Failed preview was later resurrected.");
   var retainedLayers=layers.Items.Cast<object>().ToArray();int oldLayer=explorer.SelectedLayer;explorer.SelectLayer(5);explorer.NavigateColor(42);
   Require(explorer.SelectedLayer==5&&composition.SelectedIndex==2&&explorer.SelectedPredicate=="color(C42)"&&!apply.Enabled,"Batched navigation lost layer/composition state or reused preview counts.");
   Require(layers.Items.Cast<object>().Zip(retainedLayers,(a,b)=>Object.ReferenceEquals(a,b)).All(same=>same),"Unchanged layer options were rebuilt during navigation.");explorer.SelectLayer(oldLayer);explorer.NavigateColor(1);
   checks.Add("Explicit insert, count-before-apply, busy guard, replace/intersect/union, and stale-response rejection");
   var tasks=Get<TabControl>(explorer,"navigation");Require(tasks.TabPages.Cast<TabPage>().Select(p=>p.Text).SequenceEqual(new[]{"Colors","Layers","Vertices"}),"Task tabs are missing.");int origin=explorer.SelectedColor,chosenLayer=explorer.SelectedLayer;
   foreach(int tab in new[]{1,2,0}){tasks.SelectedIndex=tab;Require(explorer.SelectedColor==origin&&explorer.SelectedLayer==chosenLayer,"Task switching changed the selected layer origin.");}
   tasks.SelectedIndex=1;Click(explorer,"Use current layer");Require(explorer.SelectedPredicate==explorer.GeneratePredicate("layer"),"Layer task did not prepare its current-position predicate.");Click(explorer,"Use home layer");Require(explorer.SelectedPredicate==explorer.GeneratePredicate("home_layer"),"Layer task did not prepare its home-identity predicate.");
   Get<ComboBox>(explorer,"predicateChoice").SelectedIndex=0;tasks.SelectedIndex=0;checks.Add("Colors/Layers/Vertices tasks preserve the shared origin and prepare explicit layer filters");
   explorer.InvalidatePreview();Click(explorer,"Preview counts");explorer.ShowPreview(last.Id,20,30,detailed);
   foreach(int width in new[]{320,330,600}){
    host.ClientSize=new Size(width,720);Application.DoEvents();var flow=Get<FlowLayoutPanel>(explorer,"content");Require(flow.Right<=explorer.ClientSize.Width,"Explorer origin bar causes horizontal overflow.");
    foreach(int tab in new[]{0,1,2}){
     tasks.SelectedIndex=tab;Application.DoEvents();var panel=(FlowLayoutPanel)tasks.SelectedTab.Controls[0];Require(!panel.HorizontalScroll.Visible,"A task page requires horizontal scrolling: window="+width+", task="+tasks.SelectedTab.Text+", client="+panel.ClientSize+", display="+panel.DisplayRectangle+", controls="+String.Join("; ",panel.Controls.Cast<Control>().Select(c=>c.GetType().Name+" "+c.Bounds)));
     foreach(Button action in new[]{Get<Button>(explorer,"previewButton"),apply,Get<Button>(explorer,"insertButton"),Get<Button>(explorer,"optionsButton"),Get<Button>(explorer,"reviewButton")}){
      Rectangle bounds=explorer.RectangleToClient(action.RectangleToScreen(action.ClientRectangle));Require(action.Visible&&bounds.Top>=0&&bounds.Bottom<=explorer.ClientSize.Height&&bounds.Left>=0&&bounds.Right<=explorer.ClientSize.Width,"Persistent filter action is outside the visible explorer: "+action.Text+" at "+bounds);
     }
    }
    Require(neighborPanel.Controls.Cast<Control>().Select(c=>c.Top).Distinct().Count()==1&&Get<FlowLayoutPanel>(explorer,"colorVertices").Controls.Cast<Control>().Select(c=>c.Top).Distinct().Count()==1,"Four numeric navigation buttons do not fit on one row.");
   }
   host.ClientSize=new Size(330,720);tasks.SelectedIndex=0;Click(explorer,"Options");Application.DoEvents();Require(Get<FlowLayoutPanel>(explorer,"filterOptions").Visible,"Filter options cannot be opened.");Click(explorer,"Hide options");
   var reviewToggle=Get<Button>(explorer,"reviewButton");reviewToggle.PerformClick();Application.DoEvents();Require(!review.Visible,"Expanded review cannot be collapsed.");reviewToggle.PerformClick();Application.DoEvents();Require(review.Visible&&review.Height>=140,"Complete preview review cannot be reopened.");
   Require(!explorer.AutoScroll&&graph.TabStop&&graph.Height>=238&&Get<TextBox>(explorer,"colorSearch").AccessibleName.Length>0,"Compact task layout or keyboard accessibility is absent.");checks.Add("Persistent filter actions, substantial graph, local scrolling, and collapsible review at 320/330/600px");
   if(captureDirectory!=null){Directory.CreateDirectory(captureDirectory);foreach(int tab in new[]{0,1,2}){tasks.SelectedIndex=tab;Application.DoEvents();using(var bitmap=new Bitmap(explorer.Width,explorer.Height)){explorer.DrawToBitmap(bitmap,explorer.ClientRectangle);bitmap.Save(Path.Combine(captureDirectory,"control-"+tasks.SelectedTab.Text.ToLowerInvariant()+".png"));}}tasks.SelectedIndex=0;reviewToggle.PerformClick();Application.DoEvents();using(var bitmap=new Bitmap(explorer.Width,explorer.Height)){explorer.DrawToBitmap(bitmap,explorer.ClientRectangle);bitmap.Save(Path.Combine(captureDirectory,"control-colors-compact.png"));}}
   host.Close();Application.DoEvents();
  }
  return checks;
 }
}
