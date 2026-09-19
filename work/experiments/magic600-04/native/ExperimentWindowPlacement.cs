// Pure screen-rectangle policy. Does not move existing tools or own application state.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

internal static class ExperimentWindowPlacement {
 internal static Rectangle Clamp(Rectangle bounds,Rectangle area,Size minimum){
  int width=Math.Min(area.Width,Math.Max(Math.Min(minimum.Width,area.Width),bounds.Width));
  int height=Math.Min(area.Height,Math.Max(Math.Min(minimum.Height,area.Height),bounds.Height));
  return new Rectangle(Math.Max(area.Left,Math.Min(bounds.Left,area.Right-width)),Math.Max(area.Top,Math.Min(bounds.Top,area.Bottom-height)),width,height);
 }
 static double Overlap(Rectangle first,Rectangle second){var overlap=Rectangle.Intersect(first,second);return Math.Max(0,overlap.Width)*(double)Math.Max(0,overlap.Height);}
 internal static Rectangle Place(string role,Rectangle area,Rectangle owner,Size preferred,Size minimum,IEnumerable<Rectangle> occupied,IEnumerable<Rectangle> protectedContext,int titleHeight=30){
  const int gap=12;var tools=occupied.Where(r=>r.Width>0&&r.Height>0).ToArray();var contexts=protectedContext.Where(r=>r.Width>0&&r.Height>0&&r.IntersectsWith(area)).ToArray();
  int top=area.Top+gap,bottom=area.Bottom-gap;
  // Leave the actual broad Current/Next and protection bands exposed when size permits.
  foreach(var context in contexts){if(context.Width<Math.Min(owner.Width,area.Width)/2)continue;if(context.Top<owner.Top+owner.Height/3)top=Math.Max(top,context.Bottom+gap);if(context.Bottom>owner.Top+owner.Height*2/3)bottom=Math.Min(bottom,context.Top-gap);}
  int width=Math.Min(preferred.Width,area.Width-gap*2);if(role=="local"||role=="global")width=Math.Min(width,Math.Max(minimum.Width,(area.Width-gap*3)/2));
  int height=Math.Min(preferred.Height,Math.Max(role=="keyboard"?Math.Max(540,minimum.Height):minimum.Height,bottom-top));
  Size size=Clamp(new Rectangle(0,0,width,height),area,minimum).Size;
  int centerX=owner.Left+(owner.Width-size.Width)/2,centerY=owner.Top+(owner.Height-size.Height)/2;
  bool left=role=="local"||role=="macro"||role=="solve",right=role=="global",lower=role=="keyboard"||role=="operation";
  var ideal=new Point(left?owner.Left+gap:right?owner.Right-size.Width-gap:centerX,lower?bottom-size.Height:role=="dialog"?centerY:top);
  var points=new List<Point>{ideal,new Point(owner.Right+gap,top),new Point(owner.Left-size.Width-gap,top),new Point(centerX,owner.Bottom+gap),new Point(centerX,owner.Top-size.Height-gap),new Point(area.Left+gap,top),new Point(area.Right-size.Width-gap,top),new Point(area.Left+gap,bottom-size.Height),new Point(area.Right-size.Width-gap,bottom-size.Height),new Point(centerX,top),new Point(centerX,bottom-size.Height),new Point(centerX,centerY)};
  foreach(var tool in tools){points.Add(new Point(tool.Right+gap,tool.Top));points.Add(new Point(tool.Left-size.Width-gap,tool.Top));points.Add(new Point(tool.Left,tool.Bottom+gap));points.Add(new Point(tool.Left,tool.Top-size.Height-gap));points.Add(new Point(ideal.X,tool.Top+titleHeight+gap));points.Add(new Point(tool.Left,tool.Top+titleHeight+gap));}
  Rectangle best=Rectangle.Empty;double bestScore=Double.PositiveInfinity;
  foreach(var point in points){var candidate=Clamp(new Rectangle(point,size),area,minimum);double score=contexts.Sum(r=>Overlap(candidate,r))*60+tools.Sum(r=>Overlap(candidate,r))*3+tools.Sum(r=>Overlap(candidate,new Rectangle(r.Left,r.Top,r.Width,Math.Min(titleHeight,r.Height))))*20+Overlap(candidate,owner)*.10+(Math.Abs(candidate.Left-ideal.X)+Math.Abs(candidate.Top-ideal.Y))*8;if(score<bestScore){bestScore=score;best=candidate;}}
  return best;
 }
}
