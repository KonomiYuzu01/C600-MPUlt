// Schematic, keyboard-accessible view of immutable cell adjacency.
// Navigation is local: this control has no engine or viewport dependency.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

internal sealed class NativeColorGraph : Control {
 readonly Dictionary<int,PointF> points=new Dictionary<int,PointF>();
 readonly List<int[]> edges=new List<int[]>();
 readonly ToolTip tip=new ToolTip();
 int[] nodes=new int[0],distances=new int[0];
 int selected=1,compare=1,focused=1,hover=-1;
 Color[] palette;
 internal event Action<int> ColorSelected;
 internal int NodeCount {get{return nodes.Length;}}

 internal NativeColorGraph(){
  DoubleBuffered=true;ResizeRedraw=true;TabStop=true;
  BackColor=Color.FromArgb(248,249,251);ForeColor=Color.FromArgb(35,40,48);
  AccessibleName="Color adjacency graph";
  AccessibleDescription="Arrow keys choose a graph node; Enter or Space makes it the selected cell. Edges mean shared triangular faces.";
  MinimumSize=new Size(240,238);Size=new Size(298,258);
 }
 internal void SetGraph(int[][] adjacency,int root,int comparison,int hops,Color[] colors){
  if(adjacency==null||adjacency.Length!=600||root<1||root>600||comparison<1||comparison>600||(hops!=1&&hops!=2))throw new ArgumentException("Invalid color graph input.");
  selected=root;compare=comparison;focused=root;palette=colors;
  var depth=new Dictionary<int,int>();var queue=new Queue<int>();depth[root]=0;queue.Enqueue(root);
  while(queue.Count>0){int c=queue.Dequeue();if(depth[c]==hops)continue;foreach(int n in adjacency[c-1])if(!depth.ContainsKey(n)){depth[n]=depth[c]+1;queue.Enqueue(n);}}
  nodes=depth.Keys.OrderBy(n=>depth[n]).ThenBy(n=>n).ToArray();distances=nodes.Select(n=>depth[n]).ToArray();edges.Clear();
  foreach(int c in nodes)foreach(int n in adjacency[c-1])if(c<n&&depth.ContainsKey(n))edges.Add(new[]{c,n});
  hover=-1;tip.SetToolTip(this,"");LayoutNodes();Invalidate();
 }
 void LayoutNodes(){
  points.Clear();if(nodes.Length==0)return;
  float cx=ClientSize.Width/2f,cy=(ClientSize.Height-26)/2f+2;
  points[selected]=new PointF(cx,cy);
  float outer=Math.Max(40,Math.Min(ClientSize.Width/2f-29,(ClientSize.Height-26)/2f-21));
  for(int depth=1;depth<=2;depth++){
   var row=nodes.Where((n,i)=>distances[i]==depth).ToArray();float radius=depth==1?(distances.Contains(2)?outer*.52f:outer*.78f):outer;
   for(int i=0;i<row.Length;i++){double angle=-Math.PI/2+2*Math.PI*i/Math.Max(1,row.Length);points[row[i]]=new PointF(cx+(float)Math.Cos(angle)*radius,cy+(float)Math.Sin(angle)*radius);}
  }
 }
 internal Point NodePoint(int color){PointF point;if(!points.TryGetValue(color,out point))throw new ArgumentOutOfRangeException("color");return Point.Round(point);}
 protected override void OnResize(EventArgs e){base.OnResize(e);LayoutNodes();}
 protected override bool IsInputKey(Keys keyData){Keys key=keyData&Keys.KeyCode;return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||base.IsInputKey(keyData);}
 protected override void OnPaint(PaintEventArgs e){
  base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
  using(var pen=new Pen(Color.FromArgb(184,190,201),1.2f))foreach(var edge in edges){PointF a,b;if(points.TryGetValue(edge[0],out a)&&points.TryGetValue(edge[1],out b))e.Graphics.DrawLine(pen,a,b);}
  foreach(int n in nodes){
   PointF p=points[n];float radius=n==selected?12:9;
   Color fill=palette!=null&&palette.Length==600?palette[n-1]:Color.LightSteelBlue;
   using(var brush=new SolidBrush(fill))e.Graphics.FillEllipse(brush,p.X-radius,p.Y-radius,radius*2,radius*2);
   using(var pen=new Pen(n==selected?Color.Black:n==compare?Color.DarkOrange:Color.FromArgb(75,80,90),n==selected||n==compare?2.5f:1))e.Graphics.DrawEllipse(pen,p.X-radius,p.Y-radius,radius*2,radius*2);
   string label="C"+n;SizeF size=e.Graphics.MeasureString(label,Font);float x=Math.Max(1,Math.Min(ClientSize.Width-size.Width-1,p.X-size.Width/2)),y=p.Y+radius+1;
   using(var brush=new SolidBrush(Color.FromArgb(230,248,249,251)))e.Graphics.FillRectangle(brush,x-1,y,size.Width+2,size.Height);
   using(var brush=new SolidBrush(ForeColor))e.Graphics.DrawString(label,Font,brush,x,y);
   if(Focused&&n==focused)ControlPaint.DrawFocusRectangle(e.Graphics,Rectangle.Round(new RectangleF(p.X-radius-4,p.Y-radius-4,radius*2+8,radius*2+8)));
  }
  TextRenderer.DrawText(e.Graphics,"Shared-face edges; schematic layout.",Font,new Rectangle(4,ClientSize.Height-20,ClientSize.Width-8,20),Color.DimGray,TextFormatFlags.EndEllipsis);
 }
 int Hit(Point location){int found=-1;double best=16*16;foreach(var pair in points){double dx=pair.Value.X-location.X,dy=pair.Value.Y-location.Y,d=dx*dx+dy*dy;if(d<best){best=d;found=pair.Key;}}return found;}
 protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);int next=Hit(e.Location);if(next==hover)return;hover=next;tip.SetToolTip(this,next<1?"":"C"+next+" — select as the cell-layer origin");Cursor=next<1?Cursors.Default:Cursors.Hand;}
 protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=-1;tip.SetToolTip(this,"");Cursor=Cursors.Default;}
 protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button!=MouseButtons.Left)return;int found=Hit(e.Location);if(found<1)return;Focus();focused=found;ActivateColor(found);}
 protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
 protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
 protected override void OnKeyDown(KeyEventArgs e){
  if(nodes.Length==0){base.OnKeyDown(e);return;}
  int index=Math.Max(0,Array.IndexOf(nodes,focused));
  if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Up)focused=nodes[(index+nodes.Length-1)%nodes.Length];
  else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Down)focused=nodes[(index+1)%nodes.Length];
  else if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){ActivateColor(focused);e.Handled=true;e.SuppressKeyPress=true;return;}
  else{base.OnKeyDown(e);return;}
  e.Handled=true;e.SuppressKeyPress=true;Invalidate();
 }
 void ActivateColor(int color){if(ColorSelected!=null)ColorSelected(color);}
 protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
}
