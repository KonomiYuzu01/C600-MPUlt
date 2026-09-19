// Shared coarse geometry plus a read-only view of the retained 433 cut regions.
// Small, independent GDI views of the retained 600-cell geometry. These views
// never own puzzle labels, call the engine, or change the main DirectX camera.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
 internal string Describe(int canonicalColor,string orbitCaption=null,string cellCaption=null) {
  int c=canonicalColor-1;
  string heading=cellCaption??("C"+canonicalColor);if(orbitCaption!=String.Empty)heading+=" · "+(orbitCaption??Orbit);
  return heading+"\nSolved "+counts[1][c]+"/"+counts[0][c]+" · eligible "+counts[5][c]+"/"+counts[4][c]+" · filter matches "+counts[2][c]+" stickers · unsolved "+counts[3][c];
 }
 internal bool IsBuffer(int cell) { return buffers[cell]; }
 internal bool IsSelectedPiece(int cell) { return selected[cell]; }
 internal bool HasActive(int cell) { return counts[0][cell]>0; }
 internal bool HasFilterMatch(int cell) { return counts[2][cell]>0; }
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
 struct CutTriangle { internal int Region,A; internal double Depth,Shade; }
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
 float[] cutVertices,cutCenters,cutFrames;
 int[] cutOffsets,cutLabels,cutPieces,cutPositions,cutColors,frameVertices;
 bool?[] cutMatches;
 bool[] cutProtected;
 NativePoint4[] framePoints;
 PointF[] cutScreen;
 V3[] cutView;
 readonly PointF[] cornerScreen=new PointF[4];
 readonly List<CutTriangle> cutTriangles=new List<CutTriangle>();
 Bitmap cutContext;
 bool cutContextDirty=true;
 PointF[][] cutOutlines=new PointF[433][];
 string cutModel,cutHash,cutRevision,cutFilterStatus;
 int cutCell,cutCurrent=-1,cutNext=-1;
 int? gripCap;
 int[][] cutAffectingCaps;
 readonly bool[] gripRegions=new bool[433];
 int[] cornerMapping;
 string cornerMappingCaption;
 bool active=true,projectionDirty=true,dragging,moved;
 int selected=1,focus=1,hover,hops=1;
 double qx,qy,qz,qw=1,zoom=1;
 Point mouseDown;
 V3 lastArc;
 Rectangle sceneRect;
 internal event Action<int> SelectionChanged,HoverChanged,CenterRequested;
 internal bool GlobalMode { get { return globalMode; } }
 internal string OrbitCaption;
 // Display only. Selection and picking retain canonical one-based cell IDs.
 internal Func<int,string> CellLabel;
 internal int SelectedColor { get { return selected; } }
 internal int FocusColor { get { return focus; } }
 internal int CenterCell { get { return focus; } }
 internal bool RegistryCameraInput;
 internal int LocalRegionCount { get { return cutOffsets==null?0:cutOffsets.Length-1; } }
 internal string LocalStateHash { get { return cutHash; } }
 internal int GripHighlightedRegionCount {get{int count=0;foreach(bool highlighted in gripRegions)if(highlighted)count++;return count;}}
 internal bool GripHighlightsRegion(int region){if(region<0||region>=433)throw new ArgumentOutOfRangeException("region");return gripRegions[region];}
 internal bool RenderingActive { get { return active; } }
 internal int LocalHops { get { return hops; } set { if(value!=1&&value!=2) throw new ArgumentOutOfRangeException("value"); if(hops!=value) { hops=value; RebuildCells(); } } }
 internal NativeCellView(bool globalMode) {
  this.globalMode=globalMode; SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
  BackColor=Color.FromArgb(19,26,34); ForeColor=Color.FromArgb(221,231,239); TabStop=true; Size=new Size(450,390); MinimumSize=new Size(180,150);
  AccessibleName=globalMode?"Global 600-cell geometry overview":"Focused cell geometry neighborhood";
  AccessibleDescription="True 4D geometry projected to 3D. Drag to rotate this view, wheel to zoom, Home to reset. Click a cell to select it; Center main is a separate action.";
  if(!globalMode)AccessibleDescription+=" Local a, b, c, d mark the ordered cap corners. Cut regions are spaced 2.5% for visibility. Solid outlines follow Current, dashed outlines follow the Next identity bookmark, and dotted outlines mark protected state. A pale inner outline marks filter-included positions affected by the active Grip cap; a thin Current or Next region uses an anchored Grip label instead. A key guide shows correspondence only; it does not execute a turn.";
  var menu=new ContextMenuStrip(); menu.Items.Add("Center main on selected C",null,delegate { RequestCenter(); }); menu.Items.Add("Reset this camera",null,delegate { ResetCamera(); }); ContextMenuStrip=menu;
  ResetCamera();
 }
 internal void Configure(NativeCellGeometry value) { if(value==null) throw new ArgumentNullException("value"); if(Object.ReferenceEquals(geometry,value))return; geometry=value; RebuildCells(); }
 internal void ConfigureStickers(Dictionary<string,object> payload) {
  if(NativeCellData.Text(payload,"format")!="Magic600-local-geometry-v1")throw new ArgumentException("Unsupported local mesh format.");
  string model=NativeCellData.Text(payload,"model");if(geometry==null||geometry.ModelId!=model)throw new ArgumentException("Local mesh model mismatch.");
  object[] rows=NativeCellData.Array(NativeCellData.Required(payload,"offsets"),434);var offsets=new int[434];
  for(int i=0;i<rows.Length;i++){offsets[i]=NativeCellData.Integer(rows[i]);if(offsets[i]<0||(i==0&&offsets[i]!=0)||(i>0&&(offsets[i]<=offsets[i-1]||(offsets[i]-offsets[i-1])%3!=0)))throw new ArgumentException("Invalid local region offsets.");}
  float[] vertices=LocalFloats(payload,"base_vertices_f32",offsets[433]*4),centers=LocalFloats(payload,"base_centers_f32",433*4),frames=LocalFloats(payload,"cell_frames_f32",600*16);
  cutModel=model;cutOffsets=offsets;cutVertices=vertices;cutCenters=centers;cutFrames=frames;cutScreen=new PointF[offsets[433]];cutView=new V3[offsets[433]];Redraw(true);
 }
 static float[] LocalFloats(Dictionary<string,object> source,string key,int length){byte[] bytes=Convert.FromBase64String(NativeCellData.Text(source,key));if(bytes.Length!=length*4||!BitConverter.IsLittleEndian)throw new ArgumentException("Unsupported local geometry byte order or size.");var result=new float[length];Buffer.BlockCopy(bytes,0,result,0,bytes.Length);foreach(float v in result)if(Single.IsNaN(v)||Single.IsInfinity(v))throw new ArgumentException("Non-finite local mesh.");return result;}
 static int[] LocalIntegers(Dictionary<string,object> source,string key,int length,int minimum,int maximum){object[] rows=NativeCellData.Array(NativeCellData.Required(source,key),length);var result=new int[length];for(int i=0;i<length;i++){result[i]=NativeCellData.Integer(rows[i]);if(result[i]<minimum||result[i]>maximum)throw new ArgumentException("Local "+key+" is out of range.");}return result;}
 internal void UpdateCellState(Dictionary<string,object> payload){
  if(NativeCellData.Text(payload,"format")!="Magic600-local-cell-v1"||NativeCellData.Text(payload,"model")!=cutModel)throw new ArgumentException("Local state model mismatch.");
  int cell=NativeCellData.Integer(NativeCellData.Required(payload,"cell"));CheckColor(cell,false);
  int[] slots=LocalIntegers(payload,"slots",433,0,259799),labels=LocalIntegers(payload,"labels",433,0,259799),pieces=LocalIntegers(payload,"pieces",433,0,177119),positions=LocalIntegers(payload,"positions",433,0,177119),colors=LocalIntegers(payload,"color_cells",433,1,600),vertices=LocalIntegers(payload,"frame_vertices",4,1,120);
  object rawCaps;int[][] caps=null;if(payload.TryGetValue("affecting_caps",out rawCaps)){
   object[] capRows=NativeCellData.Array(rawCaps,433);caps=new int[433][];
   for(int r=0;r<433;r++){object[] values=NativeCellData.Array(capRows[r],-1);if(values.Length>600)throw new ArgumentException("Too many affecting caps.");caps[r]=new int[values.Length];var unique=new HashSet<int>();for(int k=0;k<values.Length;k++){int cap=NativeCellData.Integer(values[k]);CheckColor(cap,false);if(!unique.Add(cap))throw new ArgumentException("Repeated affecting cap.");caps[r][k]=cap;}}
  }
  var seen=new HashSet<int>();for(int i=0;i<433;i++)if(slots[i]!=(cell-1)*433+i||colors[i]!=labels[i]/433+1)throw new ArgumentException("Local slot/color correspondence disagrees.");
  object[] points=NativeCellData.Array(NativeCellData.Required(payload,"frame_positions4"),4);var fp=new NativePoint4[4];
  for(int i=0;i<4;i++){if(!seen.Add(vertices[i]))throw new ArgumentException("Repeated local frame vertex.");object[] p=NativeCellData.Array(points[i],4);fp[i]=new NativePoint4(NativeCellData.Number(p[0]),NativeCellData.Number(p[1]),NativeCellData.Number(p[2]),NativeCellData.Number(p[3]));NativePoint4 expected=geometry.Vertex(vertices[i]-1);if(Sub4(fp[i],expected).LengthSquared>1e-16)throw new ArgumentException("Local frame geometry disagrees.");bool inside=false;for(int k=0;k<4;k++)if(geometry.CellVertex(cell-1,k)==vertices[i]-1)inside=true;if(!inside)throw new ArgumentException("Local frame vertex belongs to another cell.");}
  object[] match=NativeCellData.Array(NativeCellData.Required(payload,"filter_match"),433),locked=NativeCellData.Array(NativeCellData.Required(payload,"protected"),433);var matches=new bool?[433];var protection=new bool[433];
  for(int i=0;i<433;i++){if(match[i]!=null&&!(match[i] is bool)||!(locked[i] is bool))throw new ArgumentException("Invalid local filter/protection value.");matches[i]=match[i]==null?(bool?)null:(bool)match[i];protection[i]=(bool)locked[i];}
  object current=NativeCellData.Required(payload,"current"),next=NativeCellData.Required(payload,"next");int currentId=current==null?-1:NativeCellData.Integer(current),nextId=next==null?-1:NativeCellData.Integer(next);if(currentId< -1||currentId>177119||nextId< -1||nextId>177119)throw new ArgumentException("Invalid local tracked identity.");
  bool geometryChanged=cutCell!=cell||frameVertices==null;for(int i=0;!geometryChanged&&i<4;i++)geometryChanged=frameVertices[i]!=vertices[i];if(geometryChanged){cornerMapping=null;cornerMappingCaption=null;}
  if(cutColors==null||cutMatches==null)cutContextDirty=true;else for(int i=0;i<433;i++)if(cutColors[i]!=colors[i]||cutMatches[i]!=matches[i]){cutContextDirty=true;break;}
  cutHash=NativeCellData.Text(payload,"state_hash");cutRevision=NativeCellData.Text(payload,"revision");cutFilterStatus=NativeCellData.Text(NativeCellData.Map(NativeCellData.Required(payload,"filter")),"status");cutCell=cell;cutLabels=labels;cutPieces=pieces;cutPositions=positions;cutColors=colors;cutMatches=matches;cutProtected=protection;cutAffectingCaps=caps;frameVertices=vertices;framePoints=fp;cutCurrent=currentId;cutNext=nextId;UpdateGripRegions();Redraw(geometryChanged);
 }
 internal void SetGripCap(int? canonicalCell){if(canonicalCell.HasValue)CheckColor(canonicalCell.Value,false);if(gripCap==canonicalCell)return;gripCap=canonicalCell;UpdateGripRegions();Redraw(false);}
 void UpdateGripRegions(){for(int r=0;r<433;r++)gripRegions[r]=gripCap.HasValue&&cutCell==focus&&cutAffectingCaps!=null&&cutMatches!=null&&cutFilterStatus=="evaluated"&&cutMatches[r]==true&&Array.IndexOf(cutAffectingCaps[r],gripCap.Value)>=0;}
 internal void SetCenter(int canonicalCell){if(focus!=canonicalCell){cornerMapping=null;cornerMappingCaption=null;}SetFocus(canonicalCell);}
 // The caller supplies the verified zero-based permutation for this exact cap
 // and ordered frame. This guide is input meaning, never a simulated move.
 internal void SetCornerMapping(int[] permutation,string caption){
  if(permutation==null){cornerMapping=null;cornerMappingCaption=null;Redraw(false);return;}
  if(permutation.Length!=4||cutCell!=focus||frameVertices==null)throw new ArgumentException("Twist guide requires the displayed ordered cap frame.");
  var seen=new HashSet<int>();int inversions=0;for(int i=0;i<4;i++){if(permutation[i]<0||permutation[i]>=4||!seen.Add(permutation[i]))throw new ArgumentException("Twist guide is not a corner permutation.");for(int j=0;j<i;j++)if(permutation[j]>permutation[i])inversions++;}
  if(inversions%2!=0)throw new ArgumentException("Twist guide must be a proper tetrahedral rotation.");
  cornerMapping=(int[])permutation.Clone();cornerMappingCaption=caption??"";Redraw(false);
 }
 internal void ResetView(){ResetCamera();}
 internal void RotateView(double horizontal,double vertical){if(Double.IsNaN(horizontal)||Double.IsInfinity(horizontal)||Double.IsNaN(vertical)||Double.IsInfinity(vertical))throw new ArgumentException("Invalid view rotation.");double length=Math.Sqrt(horizontal*horizontal+vertical*vertical+1);Turn(new V3(0,0,1),new V3(horizontal/length,vertical/length,1/length));Redraw(true);}
 internal void ZoomView(double factor){if(Double.IsNaN(factor)||Double.IsInfinity(factor)||factor<=0)throw new ArgumentException("Invalid zoom.");zoom=Math.Max(.4,Math.Min(4,zoom*factor));Redraw(true);}
 internal void UpdateStatus(NativeCellStatus value) { if(value==null) throw new ArgumentNullException("value"); if(status!=null&&status.Revision==value.Revision)return; status=value; Redraw(false); }
 internal void SetSelectedColor(int value) { CheckColor(value,false); if(selected==value)return; selected=value; Redraw(false); }
 internal void SetFocus(int value) { CheckColor(value,false); if(focus==value)return; focus=value;UpdateGripRegions(); if(!globalMode)RebuildCells(); else Redraw(false); }
 internal void SetHoverColor(int value) { CheckColor(value,true); if(hover==value)return; hover=value; Redraw(false); }
 internal void SetRenderingActive(bool value) { if(active==value)return; active=value; if(!value){dragging=false;Capture=false;} else Invalidate(); }
 internal double[] CaptureCamera() { return new[]{qx,qy,qz,qw,zoom}; }
 internal void ResetCamera() { qx=-0.12;qy=0.23;qz=0.04;qw=0.965;NormalizeCamera();zoom=1;Redraw(true); }
 internal void RequestCenter() { if(geometry!=null&&Enabled&&CenterRequested!=null)CenterRequested(selected); }
 static void CheckColor(int value,bool allowNone) { if(value<(allowNone?0:1)||value>600)throw new ArgumentOutOfRangeException("value"); }
 void Redraw(bool projection) { if(projection){projectionDirty=true;cutContextDirty=true;} if(active&&!IsDisposed)Invalidate(); }
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
 static NativePoint4 Sub4(NativePoint4 a,NativePoint4 b){return new NativePoint4(a.W-b.W,a.X-b.X,a.Y-b.Y,a.Z-b.Z);}
 static NativePoint4 Scale4(NativePoint4 a,double n){return new NativePoint4(a.W*n,a.X*n,a.Y*n,a.Z*n);}
 static double Dot4(NativePoint4 a,NativePoint4 b){return a.W*b.W+a.X*b.X+a.Y*b.Y+a.Z*b.Z;}
 static NativePoint4 Unit4(NativePoint4 a){double length=Math.Sqrt(a.LengthSquared);if(length<1e-9)throw new ArgumentException("Degenerate local frame.");return Scale4(a,1/length);}
 NativePoint4 CutWorld(float[] data,int offset){int f=(focus-1)*16;return new NativePoint4(data[offset]*cutFrames[f]+data[offset+1]*cutFrames[f+4]+data[offset+2]*cutFrames[f+8]+data[offset+3]*cutFrames[f+12],data[offset]*cutFrames[f+1]+data[offset+1]*cutFrames[f+5]+data[offset+2]*cutFrames[f+9]+data[offset+3]*cutFrames[f+13],data[offset]*cutFrames[f+2]+data[offset+1]*cutFrames[f+6]+data[offset+2]*cutFrames[f+10]+data[offset+3]*cutFrames[f+14],data[offset]*cutFrames[f+3]+data[offset+1]*cutFrames[f+7]+data[offset+2]*cutFrames[f+11]+data[offset+3]*cutFrames[f+15]);}
 void EnsureCutProjection(){
  if(!projectionDirty||cutVertices==null||cutCell!=focus||framePoints==null)return;
  int footer=Font.Height*2+14;sceneRect=new Rectangle(12,42,Math.Max(1,Width-24),Math.Max(1,Height-footer-48));
  NativePoint4 origin=geometry.Center(focus-1),right=Unit4(Sub4(framePoints[1],framePoints[0]));
  NativePoint4 y=Sub4(framePoints[2],framePoints[0]);NativePoint4 up=Unit4(Sub4(y,Scale4(right,Dot4(y,right))));
  NativePoint4 z=Sub4(framePoints[3],framePoints[0]);NativePoint4 normal=Unit4(Sub4(Sub4(z,Scale4(right,Dot4(z,right))),Scale4(up,Dot4(z,up))));
  Func<NativePoint4,V3> project=delegate(NativePoint4 point){NativePoint4 d=Sub4(point,origin);return Rotate(new V3(Dot4(d,right),Dot4(d,up),Dot4(d,normal)));};
  double extent=0;foreach(NativePoint4 p in framePoints)extent=Math.Max(extent,Math.Sqrt(Sub4(p,origin).LengthSquared));
  double scale=.45*Math.Min(sceneRect.Width,sceneRect.Height)*zoom/extent,sx=sceneRect.Left+sceneRect.Width*.5,sy=sceneRect.Top+sceneRect.Height*.5;
  Func<V3,PointF> screen=delegate(V3 p){return new PointF((float)(sx+p.X*scale),(float)(sy-p.Y*scale));};
  for(int i=0;i<4;i++)cornerScreen[i]=screen(project(framePoints[i]));cutTriangles.Clear();cutOutlines=new PointF[433][];
  for(int r=0;r<433;r++){
   NativePoint4 center=CutWorld(cutCenters,r*4);
   for(int i=cutOffsets[r];i<cutOffsets[r+1];i++){NativePoint4 point=CutWorld(cutVertices,i*4);NativePoint4 d=Scale4(Sub4(point,center),.975);cutView[i]=project(new NativePoint4(center.W+d.W,center.X+d.X,center.Y+d.Y,center.Z+d.Z));cutScreen[i]=screen(cutView[i]);}
   for(int i=cutOffsets[r];i<cutOffsets[r+1];i+=3){V3 n=V3.Cross(V3.Sub(cutView[i+1],cutView[i]),V3.Sub(cutView[i+2],cutView[i])).Unit();double light=.58+.42*Math.Abs(V3.Dot(n,new V3(.24,.42,.875).Unit()));cutTriangles.Add(new CutTriangle{Region=r,A=i,Depth=(cutView[i].Z+cutView[i+1].Z+cutView[i+2].Z)/3,Shade=light});}
  }
  cutTriangles.Sort(delegate(CutTriangle a,CutTriangle b){return a.Depth.CompareTo(b.Depth);});projectionDirty=false;
 }
 void CutPoints(CutTriangle t){trianglePoints[0]=cutScreen[t.A];trianglePoints[1]=cutScreen[t.A+1];trianglePoints[2]=cutScreen[t.A+2];}
 Color CutColor(CutTriangle t){Color color=geometry.CellColor(cutColors[t.Region]-1);return Color.FromArgb((int)(color.R*t.Shade),(int)(color.G*t.Shade),(int)(color.B*t.Shade));}
 void DrawCutContext(Graphics g){
  if(cutContext==null||cutContext.Width!=Width||cutContext.Height!=Height){if(cutContext!=null)cutContext.Dispose();cutContext=new Bitmap(Math.Max(1,Width),Math.Max(1,Height),PixelFormat.Format32bppPArgb);cutContextDirty=true;}
  if(cutContextDirty){using(var layer=Graphics.FromImage(cutContext))using(var fill=new SolidBrush(Color.Black)){
    layer.Clear(Color.Transparent);layer.SetClip(sceneRect);layer.SmoothingMode=SmoothingMode.AntiAlias;
    foreach(CutTriangle t in cutTriangles)if(cutMatches[t.Region]==false){CutPoints(t);fill.Color=CutColor(t);layer.FillPolygon(fill,trianglePoints);}
   }cutContextDirty=false;}
  // Ghost opacity is bounded once for the whole excluded mesh. Per-triangle
  // alpha accumulates through its internal faces and hides matching regions.
  using(var attributes=new ImageAttributes()){var matrix=new ColorMatrix();matrix.Matrix33=.14f;attributes.SetColorMatrix(matrix);g.DrawImage(cutContext,new Rectangle(0,0,Width,Height),0,0,Width,Height,GraphicsUnit.Pixel,attributes);}
 }
 void DrawCutCell(Graphics g){
  Color ink=ForeColor,quiet=Color.FromArgb(159,177,190);TextRenderer.DrawText(g,"Local · "+(CenterCaption??("C"+focus))+" · 433 cut regions",Font,new Rectangle(10,6,Width-20,25),ink,TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
  if(cutCell!=focus||cutLabels==null){TextRenderer.DrawText(g,"Waiting for this cell's committed state…",Font,ClientRectangle,quiet,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
  EnsureCutProjection();GraphicsState saved=g.Save();g.SetClip(sceneRect);g.SmoothingMode=SmoothingMode.AntiAlias;
  DrawCutContext(g);
  using(var fill=new SolidBrush(Color.White))foreach(CutTriangle t in cutTriangles)if(cutMatches[t.Region]!=false){fill.Color=CutColor(t);CutPoints(t);g.FillPolygon(fill,trianglePoints);}
  using(var pen=new Pen(Color.FromArgb(88,157,176,187),1)){
   for(int a=0;a<4;a++)for(int b=a+1;b<4;b++)g.DrawLine(pen,cornerScreen[a],cornerScreen[b]);
   for(int r=0;r<433;r++)if(cutMatches[r]!=false){PointF[] outline=CutOutline(r);if(outline.Length<3)continue;pen.Color=Color.FromArgb(125,BackColor);g.DrawPolygon(pen,outline);}
   for(int r=0;r<433;r++)if(cutProtected[r]){PointF[] outline=CutOutline(r);if(outline.Length<3)continue;pen.Color=Color.FromArgb(166,203,173);pen.Width=3.6f;pen.DashStyle=DashStyle.Dot;g.DrawPolygon(pen,outline);}
   for(int r=0;r<433;r++)if(cutPieces[r]==cutCurrent||cutPieces[r]==cutNext){PointF[] outline=CutOutline(r);if(outline.Length<3)continue;bool current=cutPieces[r]==cutCurrent;pen.Color=BackColor;pen.Width=4.6f;pen.DashStyle=DashStyle.Solid;g.DrawPolygon(pen,outline);pen.Color=current?Color.FromArgb(81,216,201):Color.FromArgb(241,189,91);pen.Width=2.2f;pen.DashStyle=current?DashStyle.Solid:DashStyle.Dash;g.DrawPolygon(pen,outline);if(cutProtected[r]){pen.Width=1.4f;pen.Color=Color.FromArgb(183,216,182);pen.DashStyle=DashStyle.Dot;g.DrawPolygon(pen,outline);}}
   // Current's clearing stroke must precede the independent Grip cue.
   for(int r=0;r<433;r++)if(gripRegions[r])DrawGripRegion(g,pen,r);
  }
  if(cornerMapping!=null)using(var arrow=new AdjustableArrowCap(4,5,true))using(var pen=new Pen(Color.FromArgb(242,191,107),1.7f)){
   pen.DashStyle=DashStyle.Dash;pen.CustomEndCap=arrow;
   for(int i=0;i<4;i++){int j=cornerMapping[i];if(i==j||cornerMapping[j]==i&&i>j)continue;PointF a=cornerScreen[i],b=cornerScreen[j];double dx=b.X-a.X,dy=b.Y-a.Y,length=Math.Sqrt(dx*dx+dy*dy);if(length<38)continue;float ox=(float)(dx*18/length),oy=(float)(dy*18/length);if(cornerMapping[j]==i)pen.CustomStartCap=arrow;else pen.StartCap=LineCap.Flat;g.DrawLine(pen,a.X+ox,a.Y+oy,b.X-ox,b.Y-oy);}
  }g.Restore(saved);
  for(int i=0;i<4;i++){string text=((char)('a'+i)).ToString();Rectangle bounds=new Rectangle((int)cornerScreen[i].X-12,(int)cornerScreen[i].Y-12,24,24);using(var fill=new SolidBrush(BackColor))g.FillEllipse(fill,bounds);TextRenderer.DrawText(g,text,Font,bounds,ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
  string description=cornerMapping==null?"Actual · "+cutFilterStatus+" filter · "+LocalMatchingCount+" / 433 included":"Key guide · "+cornerMappingCaption+" · not executed";
  description+="\nGrip pale · Current solid · Next dashed · Protected dotted";
  int footer=Font.Height*2+14;TextRenderer.DrawText(g,description,Font,new Rectangle(10,Height-footer,Width-20,footer-3),quiet,TextFormatFlags.WordBreak);
  if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(2,2,Math.Max(1,Width-5),Math.Max(1,Height-5)),ink,BackColor);
 }
 void DrawGripRegion(Graphics g,Pen pen,int region){
  PointF[] outline=CutOutline(region);if(outline.Length<3)return;var center=new PointF();foreach(PointF point in outline){center.X+=point.X/outline.Length;center.Y+=point.Y/outline.Length;}
  float clearance=Single.MaxValue;for(int i=0;i<outline.Length;i++){PointF a=outline[i],b=outline[(i+1)%outline.Length];float dx=b.X-a.X,dy=b.Y-a.Y,length=(float)Math.Sqrt(dx*dx+dy*dy);if(length>0)clearance=Math.Min(clearance,Math.Abs(dx*(center.Y-a.Y)-dy*(center.X-a.X))/length);}
  pen.Color=Color.FromArgb(231,241,248);pen.Width=1.6f;pen.DashStyle=DashStyle.Solid;
  if(clearance<8&&(cutPieces[region]==cutCurrent||cutPieces[region]==cutNext)){
   // The callout identifies a thin tracked region without inflating its geometry.
   Size size=TextRenderer.MeasureText("Grip",Font,Size.Empty,TextFormatFlags.NoPadding);bool left=center.X<sceneRect.Left+sceneRect.Width*.5f,above=center.Y<sceneRect.Top+sceneRect.Height*.5f;
   int x=(int)center.X+(left?-size.Width-35:35),y=(int)center.Y+(above?-size.Height-16:16);x=Math.Max(sceneRect.Left,Math.Min(sceneRect.Right-size.Width,x));y=Math.Max(sceneRect.Top,Math.Min(sceneRect.Bottom-size.Height,y));var label=new Rectangle(x,y,size.Width,size.Height);
   g.DrawLine(pen,center,new PointF(left?label.Right+3:label.Left-3,label.Top+label.Height*.5f));using(var fill=new SolidBrush(BackColor))g.FillRectangle(fill,label);TextRenderer.DrawText(g,"Grip",Font,label,pen.Color,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
  }else{var inner=new PointF[outline.Length];for(int i=0;i<inner.Length;i++)inner[i]=new PointF(center.X+(outline[i].X-center.X)*.86f,center.Y+(outline[i].Y-center.Y)*.86f);g.DrawPolygon(pen,inner);}
 }
 internal string CenterCaption;
 internal int LocalMatchingCount{get{if(cutMatches==null)return 0;int n=0;foreach(bool? value in cutMatches)if(value==true)n++;return n;}}
 internal int LocalLabelAt(int region){return cutLabels[region];}
 internal int LocalIdentityAt(int region){return cutPieces[region];}
 PointF[] CutOutline(int region){if(cutOutlines[region]!=null)return cutOutlines[region];var points=new List<PointF>();for(int i=cutOffsets[region];i<cutOffsets[region+1];i++)points.Add(cutScreen[i]);points.Sort(delegate(PointF a,PointF b){int c=a.X.CompareTo(b.X);return c!=0?c:a.Y.CompareTo(b.Y);});var hull=new List<PointF>();foreach(PointF p in points){while(hull.Count>=2&&Cross2(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}int lower=hull.Count;for(int i=points.Count-2;i>=0;i--){PointF p=points[i];while(hull.Count>lower&&Cross2(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}if(hull.Count>1)hull.RemoveAt(hull.Count-1);return cutOutlines[region]=hull.ToArray();}
 static float Cross2(PointF a,PointF b,PointF c){return (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);}
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
  if(!globalMode&&cutVertices!=null){DrawCutCell(e.Graphics);return;}
  EnsureProjection();var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
  string heading=globalMode?"600 canonical cells · true 4D projection":"C"+focus+" · "+hops+" shared-face hop(s) · "+visibleCells.Count+" cells";
  TextRenderer.DrawText(g,heading,Font,new Rectangle(7,5,Width-14,23),ForeColor,TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
  GraphicsState saved=g.Save();g.SetClip(sceneRect);
  using(var brush=new SolidBrush(Color.White))using(var pen=new Pen(Color.FromArgb(130,210,220,235),1)) {
   foreach(Triangle t in triangles){trianglePoints[0]=screenVertices[t.A];trianglePoints[1]=screenVertices[t.B];trianglePoints[2]=screenVertices[t.C];bool faded=status!=null&&!status.HasFilterMatch(t.Cell);brush.Color=Color.FromArgb(faded?7:t.Cell==focus-1?55:27,geometry.CellColor(t.Cell));g.FillPolygon(brush,trianglePoints);}
   if(!globalMode)foreach(int cell in visibleCells){bool faded=status!=null&&!status.HasFilterMatch(cell);pen.Color=Color.FromArgb(faded?28:cell==focus-1?170:90,150,170,187);pen.Width=cell==focus-1?1.8f:1;DrawCell(g,pen,cell);}
   foreach(int cell in visibleCells){PointF p=screenCenters[cell];float r=markerRadius[cell];bool inactive=status!=null&&!status.HasFilterMatch(cell);brush.Color=inactive?BackColor:geometry.CellColor(cell);g.FillEllipse(brush,p.X-r,p.Y-r,2*r,2*r);pen.Color=inactive?Color.FromArgb(70,geometry.CellColor(cell)):Color.FromArgb(180,45,54,65);pen.Width=1;g.DrawEllipse(pen,p.X-r,p.Y-r,2*r,2*r);
    if(status!=null&&status.HasVisibleUnsolved(cell)){brush.Color=Color.FromArgb(42,49,58);float dot=globalMode?1.25f:1.7f;g.FillEllipse(brush,p.X-dot,p.Y-dot,dot*2,dot*2);}
    if(status!=null&&status.IsBuffer(cell)){float d=r+2;g.DrawLine(pen,p.X,p.Y-d,p.X+d,p.Y);g.DrawLine(pen,p.X+d,p.Y,p.X,p.Y+d);g.DrawLine(pen,p.X,p.Y+d,p.X-d,p.Y);g.DrawLine(pen,p.X-d,p.Y,p.X,p.Y-d);}
    if(status!=null&&status.IsSelectedPiece(cell)){float d=r+2;g.DrawRectangle(pen,p.X-d,p.Y-d,2*d,2*d);}
   }
   if(!globalMode)Highlight(g,pen,brush,focus,Color.FromArgb(95,105,117),false);
   Highlight(g,pen,brush,selected,Color.FromArgb(81,216,201),true);if(hover>0&&hover!=selected){pen.DashStyle=DashStyle.Dash;Highlight(g,pen,brush,hover,Color.FromArgb(215,227,238),true);pen.DashStyle=DashStyle.Solid;}
  } g.Restore(saved);
  string footer=status==null?"Drag: rotate · Wheel: zoom · Home: reset\nTeal: selected · Dashed: hover":status.Describe(selected,globalMode?String.Empty:OrbitCaption,CellName(selected));
  footer+="\n• visible unsolved · ○ no filter matches · ◇ buffer · □ picked";
  TextRenderer.DrawText(g,footer,Font,new Rectangle(7,sceneRect.Bottom+6,Math.Max(1,Width-14),Math.Max(1,Height-sceneRect.Bottom-8)),Color.FromArgb(167,184,199),TextFormatFlags.WordBreak|TextFormatFlags.EndEllipsis);
  if(Focused)ControlPaint.DrawFocusRectangle(g,new Rectangle(2,2,Math.Max(1,Width-5),Math.Max(1,Height-5)),ForeColor,BackColor);
 }
 void DrawCell(Graphics g,Pen pen,int cell) { for(int a=0;a<4;a++)for(int b=a+1;b<4;b++)g.DrawLine(pen,screenVertices[geometry.CellVertex(cell,a)],screenVertices[geometry.CellVertex(cell,b)]); }
 string CellName(int canonicalCell){string label=CellLabel==null?null:CellLabel(canonicalCell);return String.IsNullOrEmpty(label)?"Cell C"+canonicalCell:label;}
 void Highlight(Graphics g,Pen pen,SolidBrush brush,int canonicalColor,Color outline,bool label) {
  int cell=canonicalColor-1;if(!visibleCells.Contains(cell))return;PointF p=screenCenters[cell];pen.Color=outline;pen.Width=2;DrawCell(g,pen,cell);float r=markerRadius[cell]+3;g.DrawEllipse(pen,p.X-r,p.Y-r,r*2,r*2);
  if(label){string text=CellName(canonicalColor);Size size=TextRenderer.MeasureText(text,Font);size.Width=Math.Min(size.Width,Math.Max(1,sceneRect.Width-16));var bounds=new Rectangle((int)Math.Max(sceneRect.Left,Math.Min(sceneRect.Right-size.Width,p.X+8)),(int)Math.Max(sceneRect.Top,Math.Min(sceneRect.Bottom-size.Height,p.Y-8)),size.Width,size.Height);brush.Color=Color.FromArgb(235,BackColor);g.FillRectangle(brush,bounds);TextRenderer.DrawText(g,text,Font,bounds,outline,TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);}
 }
 int Hit(Point point) {
  if(!globalMode&&cutVertices!=null)return 0;
  if(!active||geometry==null)return 0;EnsureProjection();if(!sceneRect.Contains(point))return 0;
  int found=0;double best=Double.MaxValue;
  for(int i=visibleCells.Count-1;i>=0;i--){int cell=visibleCells[i];PointF p=screenCenters[cell];double dx=p.X-point.X,dy=p.Y-point.Y,d=dx*dx+dy*dy,r=markerRadius[cell]+4;if(d<=r*r&&d<best){found=cell+1;best=d;}}
  if(found!=0||globalMode)return found;
  for(int i=triangles.Count-1;i>=0;i--){Triangle t=triangles[i];if(InTriangle(point,screenVertices[t.A],screenVertices[t.B],screenVertices[t.C]))return t.Cell+1;}return 0;
 }
 static bool InTriangle(Point p,PointF a,PointF b,PointF c) { double area=(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);if(Math.Abs(area)<1e-3)return false;double u=((b.X-p.X)*(c.Y-p.Y)-(b.Y-p.Y)*(c.X-p.X))/area,v=((c.X-p.X)*(a.Y-p.Y)-(c.Y-p.Y)*(a.X-p.X))/area;return u>=0&&v>=0&&u+v<=1; }
 void UserHover(int value) { if(hover==value)return;SetHoverColor(value);tip.SetToolTip(this,value==0?"":(CellName(value)+" · canonical C"+value+(status!=null&&status.IsBuffer(value-1)?" · buffer destination":"")+(status!=null&&status.IsSelectedPiece(value-1)?" · selected piece position":"")+"\nClick to select; Center main is separate."+(status==null?"":"\n"+status.Describe(value,OrbitCaption,CellName(value))+"\nCounts are matching stickers here; cells can share pieces.")));if(HoverChanged!=null)HoverChanged(value); }
 protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e);if(!active||!Enabled||e.Button!=MouseButtons.Left)return;Focus();if(!globalMode&&cutVertices!=null)EnsureCutProjection();else EnsureProjection();if(!sceneRect.Contains(e.Location))return;UserHover(0);dragging=true;moved=false;mouseDown=e.Location;lastArc=Arc(e.Location);Capture=true; }
 protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e);if(!active||!Enabled)return;if(dragging){if(Math.Abs(e.X-mouseDown.X)+Math.Abs(e.Y-mouseDown.Y)>3)moved=true;if(moved){V3 next=Arc(e.Location);Turn(lastArc,next);lastArc=next;Redraw(true);}}else UserHover(Hit(e.Location)); }
 protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e);if(e.Button!=MouseButtons.Left||!dragging)return;bool click=!moved;dragging=false;Capture=false;if(click){int c=Hit(e.Location);if(c>0){SetSelectedColor(c);if(SelectionChanged!=null)SelectionChanged(c);}} }
 protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e);if(!Capture)dragging=false; }
 protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e);if(!dragging)UserHover(0); }
 protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e);if(!active||!Enabled)return;zoom=Math.Max(.4,Math.Min(4,zoom*Math.Pow(1.12,e.Delta/120.0)));Redraw(true); }
 protected override bool IsInputKey(Keys keyData) { Keys key=keyData&Keys.KeyCode;return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||base.IsInputKey(keyData); }
 protected override void OnKeyDown(KeyEventArgs e) {
  if(RegistryCameraInput){base.OnKeyDown(e);return;}
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
 protected override void Dispose(bool disposing) { if(disposing){tip.Dispose();if(cutContext!=null)cutContext.Dispose();if(ContextMenuStrip!=null)ContextMenuStrip.Dispose();}base.Dispose(disposing); }
}
