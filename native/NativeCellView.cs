// Small, independent GDI views of the retained 600-cell geometry. These views
// never own puzzle labels, call the engine, or change the main DirectX camera.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

internal struct NativePoint4 {
 internal readonly double W, X, Y, Z;
 internal NativePoint4(double w, double x, double y, double z) { W=w; X=x; Y=y; Z=z; }
 internal double LengthSquared { get { return W*W+X*X+Y*Y+Z*Z; } }
}

// Parsed once and shared by both controls. No mutable array is exposed.
internal sealed class NativeCellGeometry {
 readonly NativePoint4[] vertices, centers;
 readonly int[][] cells, adjacency;
 readonly Color[] palette;
 internal readonly string ModelId;
 internal readonly double Radius;
 NativeCellGeometry(string model, NativePoint4[] v, NativePoint4[] c, int[][] t, int[][] a, Color[] p, double radius) {
  ModelId=model; vertices=v; centers=c; cells=t; adjacency=a; palette=p; Radius=radius;
 }
 internal NativePoint4 Vertex(int vertex) { return vertices[vertex]; }
 internal NativePoint4 Center(int cell) { return centers[cell]; }
 internal int CellVertex(int cell, int corner) { return cells[cell][corner]; }
 internal int Neighbor(int cell, int side) { return adjacency[cell][side]; }
 internal Color CellColor(int cell) { return palette[cell]; }
 internal static NativeCellGeometry FromStructure(Dictionary<string,object> source, Color[] canonicalPalette) { return Read(source, canonicalPalette); }
 internal static NativeCellGeometry Read(Dictionary<string,object> source, Color[] canonicalPalette) {
  if(source==null) throw new ArgumentNullException("source");
  string model=NativeCellData.Text(source,"model_id");
  var g=NativeCellData.Map(NativeCellData.Required(source,"geometry"));
  if(NativeCellData.Text(g,"format")!="C600-cell-geometry-v1" || NativeCellData.Integer(NativeCellData.Required(g,"vertex_id_base"))!=0 || NativeCellData.Integer(NativeCellData.Required(g,"color_id_base"))!=1) throw new ArgumentException("Unsupported cell geometry format.");
  object[] order=NativeCellData.Array(NativeCellData.Required(g,"coordinate_order"),4);
  string[] expected={"W","X","Y","Z"}; for(int i=0;i<4;i++) if(!(order[i] is string)||(string)order[i]!=expected[i]) throw new ArgumentException("Cell geometry must use W, X, Y, Z coordinates.");
  NativePoint4[] v=Points(NativeCellData.Required(g,"vertices4"),120), c=Points(NativeCellData.Required(g,"centers4"),600);
  int[][] cells=Rows(NativeCellData.Required(g,"cells"),600,4,0,119), a=Rows(NativeCellData.Required(source,"adjacency"),600,4,1,600);
  if(canonicalPalette==null || canonicalPalette.Length!=600) throw new ArgumentException("Exactly 600 canonical cell colors are required.");
  double radius=Math.Sqrt(v[0].LengthSquared); if(radius<1e-10) throw new ArgumentException("Cell geometry has a zero radius.");
  for(int i=0;i<120;i++) if(Math.Abs(Math.Sqrt(v[i].LengthSquared)-radius)>radius*1e-6) throw new ArgumentException("Cell vertices do not have a common radius.");
  var uniqueCells=new HashSet<string>();
  for(int cell=0;cell<600;cell++) {
   int[] sorted=(int[])cells[cell].Clone(); System.Array.Sort(sorted);
   if(!uniqueCells.Add(String.Join(",",sorted))) throw new ArgumentException("Duplicate tetrahedral cell.");
   double w=0,x=0,y=0,z=0; for(int k=0;k<4;k++) { NativePoint4 p=v[cells[cell][k]]; w+=p.W; x+=p.X; y+=p.Y; z+=p.Z; }
   NativePoint4 q=c[cell]; double error=(w/4-q.W)*(w/4-q.W)+(x/4-q.X)*(x/4-q.X)+(y/4-q.Y)*(y/4-q.Y)+(z/4-q.Z)*(z/4-q.Z);
   if(error>radius*radius*1e-12) throw new ArgumentException("A cell center differs from its four vertices.");
   for(int k=0;k<4;k++) {
    int neighbor=a[cell][k]-1; a[cell][k]=neighbor;
    if(neighbor==cell) throw new ArgumentException("A cell cannot be adjacent to itself.");
    int common=0; foreach(int vertex in cells[cell]) if(System.Array.IndexOf(cells[neighbor],vertex)>=0) common++;
    if(common!=3) throw new ArgumentException("Adjacent cells must share exactly three vertices.");
   }
  }
  for(int cell=0;cell<600;cell++) for(int k=0;k<4;k++) if(System.Array.IndexOf(a[a[cell][k]],cell)<0) throw new ArgumentException("Cell adjacency must be symmetric.");
  return new NativeCellGeometry(model,v,c,cells,a,(Color[])canonicalPalette.Clone(),radius);
 }
 static NativePoint4[] Points(object value,int count) {
  object[] rows=NativeCellData.Array(value,count); var result=new NativePoint4[count];
  for(int i=0;i<count;i++) { object[] p=NativeCellData.Array(rows[i],4); result[i]=new NativePoint4(NativeCellData.Number(p[0]),NativeCellData.Number(p[1]),NativeCellData.Number(p[2]),NativeCellData.Number(p[3])); }
  return result;
 }
 static int[][] Rows(object value,int count,int width,int minimum,int maximum) {
  object[] rows=NativeCellData.Array(value,count); var result=new int[count][];
  for(int i=0;i<count;i++) { object[] row=NativeCellData.Array(rows[i],width); result[i]=new int[width]; var unique=new HashSet<int>();
   for(int k=0;k<width;k++) { int n=NativeCellData.Integer(row[k]); if(n<minimum||n>maximum||!unique.Add(n)) throw new ArgumentException("Invalid or repeated cell geometry index."); result[i][k]=n; }
  } return result;
 }
}

internal sealed class NativeCellStatus {
 readonly int[][] counts;
 readonly bool[] buffers, selected;
 internal readonly string Revision, StateHash, Orbit;
 internal readonly int FocusColor;
 NativeCellStatus(string revision,string hash,string orbit,int focus,int[][] values,bool[] b,bool[] s) { Revision=revision; StateHash=hash; Orbit=orbit; FocusColor=focus; counts=values; buffers=b; selected=s; }
 internal static NativeCellStatus FromState(Dictionary<string,object> state) { return Read(NativeCellData.Map(NativeCellData.Required(state,"cell_status"))); }
 internal static NativeCellStatus Read(Dictionary<string,object> source) {
  if(source==null) throw new ArgumentNullException("source");
  if(NativeCellData.Text(source,"format")!="C600-cell-status-v1") throw new ArgumentException("Unsupported cell status format.");
  string revision=NativeCellData.Text(source,"revision"),hash=NativeCellData.Text(source,"state_hash");int orbitId=NativeCellData.Integer(NativeCellData.Required(source,"orbit"));if(orbitId<0||orbitId>34)throw new ArgumentException("Invalid cell status orbit.");string orbit="O"+orbitId;
  object focusValue=NativeCellData.Required(source,"focus_color"); int focus=focusValue==null?0:NativeCellData.Integer(focusValue);
  if(focusValue!=null&&(focus<1||focus>600)) throw new ArgumentException("Invalid cell status focus.");
  string[] names={"active_total","active_solved","visible_total","visible_unsolved","eligible_total","eligible_solved"}; var values=new int[6][];
  for(int k=0;k<6;k++) { object[] row=NativeCellData.Array(NativeCellData.Required(source,names[k]),600); values[k]=new int[600]; for(int i=0;i<600;i++) { int n=NativeCellData.Integer(row[i]); if(n<0||n>259800) throw new ArgumentException("Invalid cell position count."); values[k][i]=n; } }
  for(int i=0;i<600;i++) if(values[1][i]>values[0][i]||values[3][i]>values[2][i]||values[5][i]>values[4][i]||values[4][i]>values[0][i]||values[4][i]>values[2][i]) throw new ArgumentException("Inconsistent cell position counts.");
  return new NativeCellStatus(revision,hash,orbit,focus,values,CellSet(NativeCellData.Required(source,"buffer_cells")),CellSet(NativeCellData.Required(source,"selected_cells")));
 }
 static bool[] CellSet(object value) { object[] items=NativeCellData.Array(value,-1); if(items.Length>600) throw new ArgumentException("Too many cell IDs."); var result=new bool[600]; foreach(object item in items) { int n=NativeCellData.Integer(item); if(n<1||n>600||result[n-1]) throw new ArgumentException("Invalid or repeated status cell ID."); result[n-1]=true; } return result; }
 internal string Describe(int canonicalColor) {
  int c=canonicalColor-1;
  return "C"+canonicalColor+" · "+Orbit+" solved "+counts[1][c]+"/"+counts[0][c]+" · eligible "+counts[5][c]+"/"+counts[4][c]+"\nExact visible "+counts[2][c]+" · unsolved "+counts[3][c];
 }
 internal bool IsBuffer(int cell) { return buffers[cell]; }
 internal bool IsSelectedPiece(int cell) { return selected[cell]; }
 internal bool HasActive(int cell) { return counts[0][cell]>0; }
 internal bool HasVisibleUnsolved(int cell) { return counts[3][cell]>0; }
}

internal static class NativeCellData {
 internal static object Required(Dictionary<string,object> value,string key) { object result; if(value==null||!value.TryGetValue(key,out result)) throw new ArgumentException("Missing cell view data: "+key); return result; }
 internal static Dictionary<string,object> Map(object value) { var result=value as Dictionary<string,object>; if(result==null) throw new ArgumentException("Expected a cell view object."); return result; }
 internal static string Text(Dictionary<string,object> value,string key) { string result=Required(value,key) as string; if(String.IsNullOrEmpty(result)) throw new ArgumentException("Invalid cell view text: "+key); return result; }
 internal static object[] Array(object value,int count) { var list=value as IList; if(list==null||(count>=0&&list.Count!=count)) throw new ArgumentException("Invalid cell view array size."); var result=new object[list.Count]; list.CopyTo(result,0); return result; }
 internal static double Number(object value) {
  if(!(value is byte||value is sbyte||value is short||value is ushort||value is int||value is uint||value is long||value is ulong||value is float||value is double||value is decimal)) throw new ArgumentException("Expected a numeric cell value.");
  double n=Convert.ToDouble(value,CultureInfo.InvariantCulture); if(Double.IsNaN(n)||Double.IsInfinity(n)) throw new ArgumentException("Cell coordinates must be finite."); return n;
 }
 internal static int Integer(object value) { double n=Number(value); if(n!=Math.Truncate(n)||n<Int32.MinValue||n>Int32.MaxValue) throw new ArgumentException("Expected an integral cell value."); return (int)n; }
}

internal sealed class NativeCellView : Control {
 struct V3 {
  internal double X,Y,Z;
  internal V3(double x,double y,double z) { X=x;Y=y;Z=z; }
  internal static V3 Sub(V3 a,V3 b) { return new V3(a.X-b.X,a.Y-b.Y,a.Z-b.Z); }
  internal static double Dot(V3 a,V3 b) { return a.X*b.X+a.Y*b.Y+a.Z*b.Z; }
  internal static V3 Cross(V3 a,V3 b) { return new V3(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X); }
  internal V3 Unit() { double n=Math.Sqrt(X*X+Y*Y+Z*Z);return n<1e-10?new V3(0,0,1):new V3(X/n,Y/n,Z/n); }
 }
 struct Triangle { internal int Cell,A,B,C; internal double Depth; }
 readonly bool globalMode;
 readonly ToolTip tip=new ToolTip();
 readonly V3[] vertices3=new V3[120], centers3=new V3[600];
 readonly PointF[] screenVertices=new PointF[120], screenCenters=new PointF[600];
 readonly float[] markerRadius=new float[600];
 readonly List<int> visibleCells=new List<int>();
 readonly List<Triangle> triangles=new List<Triangle>();
 readonly PointF[] trianglePoints=new PointF[3];
 NativeCellGeometry geometry;
 NativeCellStatus status;
 bool active=true,projectionDirty=true,dragging,moved;
 int selected=1,focus=1,hover,hops=1;
 double qx,qy,qz,qw=1,zoom=1;
 Point mouseDown;
 V3 lastArc;
 Rectangle sceneRect;
 internal event Action<int> SelectionChanged,HoverChanged,CenterRequested;
 internal bool GlobalMode { get { return globalMode; } }
 internal int SelectedColor { get { return selected; } }
 internal int FocusColor { get { return focus; } }
 internal bool RenderingActive { get { return active; } }
 internal int LocalHops { get { return hops; } set { if(value!=1&&value!=2) throw new ArgumentOutOfRangeException("value"); if(hops!=value) { hops=value; RebuildCells(); } } }
 internal NativeCellView(bool globalMode) {
  this.globalMode=globalMode; SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
  BackColor=Color.FromArgb(246,247,249); ForeColor=Color.FromArgb(39,47,55); TabStop=true; Size=new Size(450,390); MinimumSize=new Size(180,150);
  AccessibleName=globalMode?"Global 600-cell geometry overview":"Focused cell geometry neighborhood";
  AccessibleDescription="True 4D geometry projected to 3D. Drag to rotate this view, wheel to zoom, Home to reset. Click a cell to select it; Center main is a separate action.";
  var menu=new ContextMenuStrip(); menu.Items.Add("Center main on selected C",null,delegate { RequestCenter(); }); menu.Items.Add("Reset this camera",null,delegate { ResetCamera(); }); ContextMenuStrip=menu;
  ResetCamera();
 }
 internal void Configure(NativeCellGeometry value) { if(value==null) throw new ArgumentNullException("value"); if(Object.ReferenceEquals(geometry,value))return; geometry=value; RebuildCells(); }
 internal void UpdateStatus(NativeCellStatus value) { if(value==null) throw new ArgumentNullException("value"); if(status!=null&&status.Revision==value.Revision)return; status=value; Redraw(false); }
 internal void SetSelectedColor(int value) { CheckColor(value,false); if(selected==value)return; selected=value; Redraw(false); }
 internal void SetFocus(int value) { CheckColor(value,false); if(focus==value)return; focus=value; if(!globalMode)RebuildCells(); else Redraw(false); }
 internal void SetHoverColor(int value) { CheckColor(value,true); if(hover==value)return; hover=value; Redraw(false); }
 internal void SetRenderingActive(bool value) { if(active==value)return; active=value; if(!value){dragging=false;Capture=false;} else Invalidate(); }
 internal double[] CaptureCamera() { return new[]{qx,qy,qz,qw,zoom}; }
 internal void ResetCamera() { qx=-0.12;qy=0.23;qz=0.04;qw=0.965;NormalizeCamera();zoom=1;Redraw(true); }
 internal void RequestCenter() { if(geometry!=null&&Enabled&&CenterRequested!=null)CenterRequested(selected); }
 static void CheckColor(int value,bool allowNone) { if(value<(allowNone?0:1)||value>600)throw new ArgumentOutOfRangeException("value"); }
 void Redraw(bool projection) { if(projection)projectionDirty=true; if(active&&!IsDisposed)Invalidate(); }
 void RebuildCells() {
  visibleCells.Clear(); triangles.Clear(); if(geometry!=null) {
   if(globalMode)for(int i=0;i<600;i++)visibleCells.Add(i);
   else { var depth=new int[600]; for(int i=0;i<600;i++)depth[i]=-1; var queue=new Queue<int>(); queue.Enqueue(focus-1);depth[focus-1]=0;
    while(queue.Count>0) { int c=queue.Dequeue(); visibleCells.Add(c); if(depth[c]==hops)continue;for(int k=0;k<4;k++){int n=geometry.Neighbor(c,k);if(depth[n]<0){depth[n]=depth[c]+1;queue.Enqueue(n);}} }
   }
  } Redraw(true);
 }
 V3 Project4(NativePoint4 p) { double w=p.W/geometry.Radius, factor=2.4/(2.4-w); return new V3(p.X/geometry.Radius*factor,p.Y/geometry.Radius*factor,p.Z/geometry.Radius*factor); }
 V3 Rotate(V3 p) { double tx=2*(qy*p.Z-qz*p.Y),ty=2*(qz*p.X-qx*p.Z),tz=2*(qx*p.Y-qy*p.X); return new V3(p.X+qw*tx+qy*tz-qz*ty,p.Y+qw*ty+qz*tx-qx*tz,p.Z+qw*tz+qx*ty-qy*tx); }
 void EnsureProjection() {
  if(!projectionDirty||geometry==null||!active)return;
  int footer=Font.Height*3+8; sceneRect=new Rectangle(8,25,Math.Max(1,Width-16),Math.Max(1,Height-footer-31));
  V3 origin=new V3(),right=new V3(1,0,0),up=new V3(0,1,0),normal=new V3(0,0,1);double extent=.05;
  if(!globalMode) { double minX=Double.MaxValue,minY=minX,minZ=minX,maxX=Double.MinValue,maxY=maxX,maxZ=maxX;
   foreach(int cell in visibleCells) for(int k=0;k<4;k++){V3 p=Project4(geometry.Vertex(geometry.CellVertex(cell,k)));minX=Math.Min(minX,p.X);minY=Math.Min(minY,p.Y);minZ=Math.Min(minZ,p.Z);maxX=Math.Max(maxX,p.X);maxY=Math.Max(maxY,p.Y);maxZ=Math.Max(maxZ,p.Z);}
   origin=new V3((minX+maxX)/2,(minY+maxY)/2,(minZ+maxZ)/2);
   // A cell-relative orthonormal frame presents the neighborhood's tangent
   // plane clearly. User rotation/zoom remains independent and is not reset
   // when Follow changes the origin; all coordinates remain actual geometry.
   normal=Project4(geometry.Center(focus-1)).Unit();right=V3.Cross(Math.Abs(normal.Y)<.9?new V3(0,1,0):new V3(1,0,0),normal).Unit();up=V3.Cross(normal,right).Unit();
  }
  foreach(int cell in visibleCells)for(int k=0;k<4;k++){V3 p=V3.Sub(Project4(geometry.Vertex(geometry.CellVertex(cell,k))),origin);extent=Math.Max(extent,Math.Sqrt(V3.Dot(p,p)));}
  double scale=.47*Math.Min(sceneRect.Width,sceneRect.Height)*zoom/extent;double sx=sceneRect.Left+sceneRect.Width/2.0,sy=sceneRect.Top+sceneRect.Height/2.0;
  for(int i=0;i<120;i++){V3 raw=V3.Sub(Project4(geometry.Vertex(i)),origin);V3 p=Rotate(new V3(V3.Dot(raw,right),V3.Dot(raw,up),V3.Dot(raw,normal)));vertices3[i]=p;screenVertices[i]=new PointF((float)(sx+p.X*scale),(float)(sy-p.Y*scale));}
  foreach(int cell in visibleCells){V3 raw=V3.Sub(Project4(geometry.Center(cell)),origin);V3 p=Rotate(new V3(V3.Dot(raw,right),V3.Dot(raw,up),V3.Dot(raw,normal)));centers3[cell]=p;screenCenters[cell]=new PointF((float)(sx+p.X*scale),(float)(sy-p.Y*scale));markerRadius[cell]=globalMode?(float)Math.Max(2.1,Math.Min(3.4,2.7+p.Z*.45)):4.2f;}
  visibleCells.Sort(delegate(int a,int b){int c=centers3[a].Z.CompareTo(centers3[b].Z);return c!=0?c:a.CompareTo(b);});triangles.Clear();
  if(!globalMode)foreach(int cell in visibleCells)for(int omitted=0;omitted<4;omitted++){int a=-1,b=-1,c=-1;for(int k=0;k<4;k++)if(k!=omitted){int v=geometry.CellVertex(cell,k);if(a<0)a=v;else if(b<0)b=v;else c=v;}triangles.Add(new Triangle{Cell=cell,A=a,B=b,C=c,Depth=(vertices3[a].Z+vertices3[b].Z+vertices3[c].Z)/3});}
  triangles.Sort(delegate(Triangle a,Triangle b){return a.Depth.CompareTo(b.Depth);});projectionDirty=false;
 }
 protected override void OnResize(EventArgs e) { base.OnResize(e);Redraw(true); }
 protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e);Redraw(true); }
 protected override void OnPaint(PaintEventArgs e) {
  base.OnPaint(e); if(!active)return;
  if(geometry==null){TextRenderer.DrawText(e.Graphics,"Waiting for immutable cell geometry…",Font,ClientRectangle,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
  EnsureProjection();var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
  string heading=globalMode?"600 canonical cells · true 4D projection":"C"+focus+" · "+hops+" shared-face hop(s) · "+visibleCells.Count+" cells";
  TextRenderer.DrawText(g,heading,Font,new Rectangle(7,5,Width-14,23),ForeColor,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
  GraphicsState saved=g.Save();g.SetClip(sceneRect);
  using(var brush=new SolidBrush(Color.White))using(var pen=new Pen(Color.FromArgb(130,210,220,235),1)) {
   foreach(Triangle t in triangles){trianglePoints[0]=screenVertices[t.A];trianglePoints[1]=screenVertices[t.B];trianglePoints[2]=screenVertices[t.C];brush.Color=Color.FromArgb(t.Cell==focus-1?55:27,geometry.CellColor(t.Cell));g.FillPolygon(brush,trianglePoints);}
   if(!globalMode)foreach(int cell in visibleCells){pen.Color=Color.FromArgb(cell==focus-1?170:90,55,65,76);pen.Width=cell==focus-1?1.8f:1;DrawCell(g,pen,cell);}
   foreach(int cell in visibleCells){PointF p=screenCenters[cell];float r=markerRadius[cell];bool inactive=status!=null&&!status.HasActive(cell);brush.Color=inactive?BackColor:geometry.CellColor(cell);g.FillEllipse(brush,p.X-r,p.Y-r,2*r,2*r);pen.Color=inactive?geometry.CellColor(cell):Color.FromArgb(180,45,54,65);pen.Width=1;g.DrawEllipse(pen,p.X-r,p.Y-r,2*r,2*r);
    if(status!=null&&status.HasVisibleUnsolved(cell)){brush.Color=Color.FromArgb(42,49,58);float dot=globalMode?1.25f:1.7f;g.FillEllipse(brush,p.X-dot,p.Y-dot,dot*2,dot*2);}
    if(status!=null&&status.IsBuffer(cell)){float d=r+2;g.DrawLine(pen,p.X,p.Y-d,p.X+d,p.Y);g.DrawLine(pen,p.X+d,p.Y,p.X,p.Y+d);g.DrawLine(pen,p.X,p.Y+d,p.X-d,p.Y);g.DrawLine(pen,p.X-d,p.Y,p.X,p.Y-d);}
    if(status!=null&&status.IsSelectedPiece(cell)){float d=r+2;g.DrawRectangle(pen,p.X-d,p.Y-d,2*d,2*d);}
   }
   if(!globalMode)Highlight(g,pen,brush,focus,Color.FromArgb(95,105,117),false);
   Highlight(g,pen,brush,selected,Color.FromArgb(0,124,131),true);if(hover>0&&hover!=selected){pen.DashStyle=DashStyle.Dash;Highlight(g,pen,brush,hover,Color.FromArgb(55,65,76),true);pen.DashStyle=DashStyle.Solid;}
  } g.Restore(saved);
  string footer=status==null?"Drag: rotate · Wheel: zoom · Home: reset\nTeal: selected · Dashed: hover":status.Describe(selected);
  footer+="\n• visible unsolved · ○ no active · ◇ buffer · □ picked";
  TextRenderer.DrawText(g,footer,Font,new Rectangle(7,sceneRect.Bottom+6,Math.Max(1,Width-14),Math.Max(1,Height-sceneRect.Bottom-8)),Color.FromArgb(76,86,98),TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);
  if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(2,2,Math.Max(1,Width-5),Math.Max(1,Height-5)),ForeColor,BackColor);
 }
 void DrawCell(Graphics g,Pen pen,int cell) { for(int a=0;a<4;a++)for(int b=a+1;b<4;b++)g.DrawLine(pen,screenVertices[geometry.CellVertex(cell,a)],screenVertices[geometry.CellVertex(cell,b)]); }
 void Highlight(Graphics g,Pen pen,SolidBrush brush,int canonicalColor,Color outline,bool label) {
  int cell=canonicalColor-1;if(!visibleCells.Contains(cell))return;PointF p=screenCenters[cell];pen.Color=outline;pen.Width=2;DrawCell(g,pen,cell);float r=markerRadius[cell]+3;g.DrawEllipse(pen,p.X-r,p.Y-r,r*2,r*2);
  if(label){string text="C"+canonicalColor;Size size=TextRenderer.MeasureText(text,Font);var bounds=new Rectangle((int)Math.Max(sceneRect.Left,Math.Min(sceneRect.Right-size.Width,p.X+8)),(int)Math.Max(sceneRect.Top,Math.Min(sceneRect.Bottom-size.Height,p.Y-8)),size.Width,size.Height);brush.Color=Color.FromArgb(235,BackColor);g.FillRectangle(brush,bounds);TextRenderer.DrawText(g,text,Font,bounds,outline,TextFormatFlags.SingleLine);}
 }
 int Hit(Point point) {
  if(!active||geometry==null)return 0;EnsureProjection();if(!sceneRect.Contains(point))return 0;
  int found=0;double best=Double.MaxValue;
  for(int i=visibleCells.Count-1;i>=0;i--){int cell=visibleCells[i];PointF p=screenCenters[cell];double dx=p.X-point.X,dy=p.Y-point.Y,d=dx*dx+dy*dy,r=markerRadius[cell]+4;if(d<=r*r&&d<best){found=cell+1;best=d;}}
  if(found!=0||globalMode)return found;
  for(int i=triangles.Count-1;i>=0;i--){Triangle t=triangles[i];if(InTriangle(point,screenVertices[t.A],screenVertices[t.B],screenVertices[t.C]))return t.Cell+1;}return 0;
 }
 static bool InTriangle(Point p,PointF a,PointF b,PointF c) { double area=(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);if(Math.Abs(area)<1e-3)return false;double u=((b.X-p.X)*(c.Y-p.Y)-(b.Y-p.Y)*(c.X-p.X))/area,v=((c.X-p.X)*(a.Y-p.Y)-(c.Y-p.Y)*(a.X-p.X))/area;return u>=0&&v>=0&&u+v<=1; }
 void UserHover(int value) { if(hover==value)return;SetHoverColor(value);tip.SetToolTip(this,value==0?"":("C"+value+" · canonical cell"+(status!=null&&status.IsBuffer(value-1)?" · buffer destination":"")+(status!=null&&status.IsSelectedPiece(value-1)?" · selected piece position":"")+"\nClick to select; Center main is separate."+(status==null?"":"\n"+status.Describe(value)+"\nCounts are physical positions; cells can overlap.")));if(HoverChanged!=null)HoverChanged(value); }
 protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e);if(!active||!Enabled||e.Button!=MouseButtons.Left)return;Focus();EnsureProjection();if(!sceneRect.Contains(e.Location))return;UserHover(0);dragging=true;moved=false;mouseDown=e.Location;lastArc=Arc(e.Location);Capture=true; }
 protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e);if(!active||!Enabled)return;if(dragging){if(Math.Abs(e.X-mouseDown.X)+Math.Abs(e.Y-mouseDown.Y)>3)moved=true;if(moved){V3 next=Arc(e.Location);Turn(lastArc,next);lastArc=next;Redraw(true);}}else UserHover(Hit(e.Location)); }
 protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e);if(e.Button!=MouseButtons.Left||!dragging)return;bool click=!moved;dragging=false;Capture=false;if(click){int c=Hit(e.Location);if(c>0){SetSelectedColor(c);if(SelectionChanged!=null)SelectionChanged(c);}} }
 protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e);if(!Capture)dragging=false; }
 protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e);if(!dragging)UserHover(0); }
 protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e);if(!active||!Enabled)return;zoom=Math.Max(.4,Math.Min(4,zoom*Math.Pow(1.12,e.Delta/120.0)));Redraw(true); }
 protected override bool IsInputKey(Keys keyData) { Keys key=keyData&Keys.KeyCode;return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||base.IsInputKey(keyData); }
 protected override void OnKeyDown(KeyEventArgs e) {
  if(!DispatchNavigationKey(e.KeyData)){base.OnKeyDown(e);return;}e.Handled=true;e.SuppressKeyPress=true;
 }
 internal bool DispatchNavigationKey(Keys keyData) {
  if(!active||!Enabled||(keyData&Keys.Modifiers)!=Keys.None)return false;Keys key=keyData&Keys.KeyCode;
  if(key==Keys.Home)ResetCamera();
  else if(key==Keys.Add||key==Keys.Oemplus){zoom=Math.Min(4,zoom*1.12);Redraw(true);}
  else if(key==Keys.Subtract||key==Keys.OemMinus){zoom=Math.Max(.4,zoom/1.12);Redraw(true);}
  else if(key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down){double dx=key==Keys.Left?-.1:key==Keys.Right?.1:0,dy=key==Keys.Up?.1:key==Keys.Down?-.1:0;Turn(new V3(0,0,1),new V3(dx,dy,Math.Sqrt(1-dx*dx-dy*dy)));Redraw(true);}
  else return false;return true;
 }
 V3 Arc(Point p) { double r=Math.Max(1,Math.Min(sceneRect.Width,sceneRect.Height)/2.0),x=(p.X-sceneRect.Left-sceneRect.Width/2.0)/r,y=-(p.Y-sceneRect.Top-sceneRect.Height/2.0)/r,d=x*x+y*y;if(d>1){double n=Math.Sqrt(d);return new V3(x/n,y/n,0);}return new V3(x,y,Math.Sqrt(1-d)); }
 void Turn(V3 from,V3 to) { double x=from.Y*to.Z-from.Z*to.Y,y=from.Z*to.X-from.X*to.Z,z=from.X*to.Y-from.Y*to.X,w=1+from.X*to.X+from.Y*to.Y+from.Z*to.Z;if(w<1e-9){x=-from.Y;y=from.X;z=0;if(x*x+y*y<1e-9){x=0;y=-from.Z;z=from.Y;}}double length=Math.Sqrt(x*x+y*y+z*z+w*w);x/=length;y/=length;z/=length;w/=length;double nx=w*qx+x*qw+y*qz-z*qy,ny=w*qy-x*qz+y*qw+z*qx,nz=w*qz+x*qy-y*qx+z*qw,nw=w*qw-x*qx-y*qy-z*qz;qx=nx;qy=ny;qz=nz;qw=nw;NormalizeCamera(); }
 void NormalizeCamera() { double n=Math.Sqrt(qx*qx+qy*qy+qz*qz+qw*qw);qx/=n;qy/=n;qz/=n;qw/=n; }
 protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e);Redraw(false); }
 protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e);Redraw(false); }
 protected override void Dispose(bool disposing) { if(disposing){tip.Dispose();if(ContextMenuStrip!=null)ContextMenuStrip.Dispose();}base.Dispose(disposing); }
}
