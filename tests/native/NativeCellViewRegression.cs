// GDI/WinForms control checks with retained geometry supplied by the caller.
// This is not a DirectX performance test and owns no engine or puzzle session.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

internal static class NativeCellViewRegression {
 const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static T Field<T>(object value,string name) { return (T)value.GetType().GetField(name,Members).GetValue(value); }
 static void Invoke(object value,string name,object argument) { value.GetType().GetMethod(name,Members).Invoke(value,new[]{argument}); }
 static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
 static void Reject(Action action) { try{action();}catch(ArgumentException){return;}throw new InvalidOperationException("Malformed cell data was accepted."); }
 static object[] Row(object value) { return ((IList)value).Cast<object>().ToArray(); }
 static bool Same(double[] a,double[] b) { return a.SequenceEqual(b); }
 static void Paint(NativeCellView view,string path=null) { using(var image=new Bitmap(view.Width,view.Height)){view.DrawToBitmap(image,view.ClientRectangle);if(path!=null)image.Save(path);} }
 static Dictionary<string,object> SyntheticStatus() {
  var result=new Dictionary<string,object>{{"format","C600-cell-status-v1"},{"revision","synthetic-status-1"},{"state_hash","synthetic-no-puzzle-state"},{"orbit",33},{"focus_color",null},{"buffer_cells",new object[]{1,17}},{"selected_cells",new object[]{42}}};
  foreach(string key in new[]{"active_total","active_solved","visible_total","visible_unsolved","eligible_total","eligible_solved"})result[key]=Enumerable.Repeat((object)(key=="visible_unsolved"?0:4),600).ToArray();
  return result;
 }
 internal static List<string> Run(Dictionary<string,object> structure,Dictionary<string,object> actualStatus=null,string captureDirectory=null) {
  var checks=new List<string>();var palette=Enumerable.Range(0,600).Select(c=>Color.FromArgb(255,(c*53)%240+8,(c*97)%240+8,(c*137)%240+8)).ToArray();
  NativeCellGeometry geometry=NativeCellGeometry.FromStructure(structure,palette);
  Require(geometry.ModelId==(string)structure["model_id"]&&geometry.Radius>0,"Geometry identity or radius lost.");
  var data=(Dictionary<string,object>)structure["geometry"];var vertices=(IList)data["vertices4"];var first=(IList)vertices[0];object original=first[0];NativePoint4 retained=geometry.Vertex(0);
  first[0]=Double.NaN;Require(geometry.Vertex(0).W==retained.W,"Shared geometry aliases a mutable JSON array.");Reject(delegate{NativeCellGeometry.Read(structure,palette);});first[0]=original;
  Color firstColor=geometry.CellColor(0);palette[0]=Color.Empty;Require(geometry.CellColor(0)==firstColor,"Caller palette mutation leaked into shared geometry.");palette[0]=firstColor;
  object center=data["centers4"];data["centers4"]=new object[0];Reject(delegate{NativeCellGeometry.Read(structure,palette);});data["centers4"]=center;
  var adjacency=(IList)structure["adjacency"];var neighbor=(IList)adjacency[0];object previousNeighbor=neighbor[0];neighbor[0]=1;Reject(delegate{NativeCellGeometry.Read(structure,palette);});neighbor[0]=previousNeighbor;
  checks.Add("Real 120-vertex/600-tetra geometry validates finite coordinates, centers, shared faces, symmetric adjacency and immutable copies");
  Dictionary<string,object> statusData=actualStatus??SyntheticStatus();NativeCellStatus status=NativeCellStatus.Read(statusData);
  var invalid=SyntheticStatus();((object[])invalid["active_solved"])[0]=5;Reject(delegate{NativeCellStatus.Read(invalid);});invalid=SyntheticStatus();invalid["focus_color"]=601;Reject(delegate{NativeCellStatus.Read(invalid);});invalid=SyntheticStatus();invalid["buffer_cells"]=new object[]{1,1};Reject(delegate{NativeCellStatus.Read(invalid);});
  Require(status.Describe(1).Contains("Exact visible")&&status.Describe(600).Contains("eligible"),"Status omitted exact visible and eligible counts.");
  var encoded=SyntheticStatus();((object[])encoded["active_total"])[0]=0;((object[])encoded["active_solved"])[0]=0;((object[])encoded["eligible_total"])[0]=0;((object[])encoded["eligible_solved"])[0]=0;((object[])encoded["visible_unsolved"])[1]=3;var encoding=NativeCellStatus.Read(encoded);
  Require(!encoding.HasActive(0)&&encoding.HasActive(1)&&encoding.HasVisibleUnsolved(1)&&!encoding.HasVisibleUnsolved(0),"Marker semantics do not follow actual active/unsolved counts.");
  checks.Add(actualStatus==null?"Synthetic status schema rejects inconsistent counts, invalid focus and duplicate IDs; immutable revision updates":"Actual backend cell-status payload and malformed status validation");
  using(var form=new Form{Text="Isolated cell geometry control fixture",ClientSize=new Size(920,500),StartPosition=FormStartPosition.Manual,Location=new Point(30,30)})
  using(var global=new NativeCellView(true){Bounds=new Rectangle(0,0,460,500),Font=new Font("Segoe UI",9F)})
  using(var local=new NativeCellView(false){Bounds=new Rectangle(460,0,460,500),Font=global.Font}) {
   form.Controls.Add(global);form.Controls.Add(local);global.Configure(geometry);local.Configure(geometry);global.UpdateStatus(status);local.UpdateStatus(status);form.Show();Application.DoEvents();Paint(global);Paint(local);
   Require(Object.ReferenceEquals(Field<NativeCellGeometry>(global,"geometry"),Field<NativeCellGeometry>(local,"geometry")),"Views do not share immutable geometry.");
   Require(Object.ReferenceEquals(Field<NativeCellStatus>(global,"status"),Field<NativeCellStatus>(local,"status")),"Views do not share one status revision.");
   Require(Field<List<int>>(global,"visibleCells").Count==600&&Field<List<int>>(local,"visibleCells").Count==5,"Global or one-hop cell count is wrong.");
   local.LocalHops=2;Paint(local);var expected=new HashSet<int>{0};for(int k=0;k<4;k++){int n=geometry.Neighbor(0,k);expected.Add(n);for(int j=0;j<4;j++)expected.Add(geometry.Neighbor(n,j));}
   Require(expected.SetEquals(Field<List<int>>(local,"visibleCells")),"Two-hop neighborhood differs from actual shared-face adjacency.");
   foreach(PointF projected in Field<PointF[]>(global,"screenCenters"))Require(!Single.IsNaN(projected.X)&&!Single.IsInfinity(projected.Y),"Projection produced a nonfinite cell point.");
   checks.Add("Global all-600 projection and exact one/two-hop tetrahedral neighborhoods share one geometry/status object");
   int selections=0,hovers=0,centers=0;global.SelectionChanged+=delegate(int c){selections++;Require(c>=1&&c<=600,"Noncanonical selection event.");};global.HoverChanged+=delegate{hovers++;};global.CenterRequested+=delegate{centers++;};
   global.SetSelectedColor(17);global.SetHoverColor(42);global.SetFocus(600);Require(selections==0&&hovers==0&&centers==0,"Programmatic update emitted external action.");global.SetHoverColor(0);
   var rect=Field<Rectangle>(global,"sceneRect");int target=Field<List<int>>(global,"visibleCells").Last(c=>rect.Contains(Point.Round(Field<PointF[]>(global,"screenCenters")[c])));Point point=Point.Round(Field<PointF[]>(global,"screenCenters")[target]);
   Invoke(global,"OnMouseMove",new MouseEventArgs(MouseButtons.None,0,point.X,point.Y,0));Require(hovers==1&&selections==0&&centers==0,"Hover changed selection or requested centering.");
   Invoke(global,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0));Invoke(global,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,point.X,point.Y,0));Require(selections==1&&centers==0,"Actual control click failed isolated canonical selection.");global.RequestCenter();Require(centers==1,"Explicit centering did not require its own action.");
   checks.Add("Canonical hover/click and explicit centering are separate; setters do not re-emit events");
   double[] before=global.CaptureCamera(),other=local.CaptureCamera();Require(global.DispatchNavigationKey(Keys.Right),"Camera key not handled.");Require(!Same(before,global.CaptureCamera())&&Same(other,local.CaptureCamera()),"Auxiliary camera rotation leaked to the other view.");
   Require(!global.DispatchNavigationKey(Keys.Enter)&&!global.DispatchNavigationKey(Keys.F8)&&!global.DispatchNavigationKey(Keys.Control|Keys.Right),"View consumed an unowned shortcut.");
   double[] rotated=global.CaptureCamera();Invoke(global,"OnMouseWheel",new MouseEventArgs(MouseButtons.None,0,100,100,120));Require(global.CaptureCamera()[4]>rotated[4],"Wheel zoom failed.");
   Point origin=new Point(rect.Left+rect.Width/2,rect.Top+rect.Height/2);before=global.CaptureCamera();Invoke(global,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,origin.X,origin.Y,0));Invoke(global,"OnMouseMove",new MouseEventArgs(MouseButtons.Left,0,origin.X+30,origin.Y+15,0));Invoke(global,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,origin.X+30,origin.Y+15,0));Require(!Same(before,global.CaptureCamera())&&selections==1,"Arcball drag selected a cell or failed rotation.");global.ResetCamera();Require(Same(global.CaptureCamera(),local.CaptureCamera()),"Reset camera differs between independent controls.");
   checks.Add("Independent arcball, wheel, keyboard, reset and strict auxiliary shortcut ownership");
   Paint(global);global.SetRenderingActive(false);before=global.CaptureCamera();global.Width=330;global.SetSelectedColor(600);Paint(global);Require(Field<bool>(global,"projectionDirty")&&!global.DispatchNavigationKey(Keys.Left)&&Same(before,global.CaptureCamera()),"Inactive view projected geometry or changed camera.");global.SetRenderingActive(true);Paint(global);Require(!Field<bool>(global,"projectionDirty"),"Reactivated view did not consume its pending projection.");
   global.UpdateStatus(status);Require(Object.ReferenceEquals(status,Field<NativeCellStatus>(global,"status")),"Same revision was replaced.");checks.Add("Inactive view defers projection and input; reactivation renders the latest state without a timer");
   if(captureDirectory!=null){Directory.CreateDirectory(captureDirectory);global.Width=460;Paint(global,Path.Combine(captureDirectory,"cell-global.png"));local.SetFocus(17);local.LocalHops=2;Paint(local,Path.Combine(captureDirectory,"cell-local.png"));global.Width=330;Paint(global,Path.Combine(captureDirectory,"cell-global-narrow.png"));
    global.Size=new Size(397,274);Paint(global,Path.Combine(captureDirectory,"cell-global-compact.png"));local.Size=new Size(397,242);local.LocalHops=1;Paint(local,Path.Combine(captureDirectory,"cell-local-compact.png"));
   }
   form.Close();Application.DoEvents();
  } return checks;
 }
}
