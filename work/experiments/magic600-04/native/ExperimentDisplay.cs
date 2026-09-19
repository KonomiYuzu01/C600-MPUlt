// Retained native observation controls; no mathematical state or input remapping.
using System;
using System.Drawing;
using System.Windows.Forms;

internal sealed partial class ExperimentShell {
 void RegisterDisplayCommands(){
  Register("display","Display and projection",ShowDisplayControls);
  Register("frame-toggle","Show or hide the 600-cell framework",()=>Send(LocalApi.D("action","display-settings","settings",LocalApi.D("hide_frame",!bridge.FrameworkHidden))));
  Register("detail-toggle","Toggle adaptive motion detail; full detail when still",()=>Send(LocalApi.D("action","display-settings","settings",LocalApi.D("adaptive_motion",!bridge.AdaptiveMotion))));
 }
 void ShowDisplayControls(){
  using(var dialog=ToolDialog("Puzzle display",560,550)){
   var grid=ToolLayout(dialog,88,-1,42);
   var options=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=Padding.Empty};
   var frame=new CheckBox{Text="Hide 600-cell framework"+KeyHint("frame-toggle"),AutoSize=true,Checked=bridge.FrameworkHidden,AccessibleName="Hide 600-cell framework"};
   var adaptive=new CheckBox{Text="Low detail while moving"+KeyHint("detail-toggle"),AutoSize=true,Checked=bridge.AdaptiveMotion,AccessibleName="Adaptive motion detail"};
   tips.SetToolTip(adaptive,"Reduce display detail during camera motion. Full detail returns when still; exact picking and the full mathematical state are retained.");
   options.Controls.Add(frame);options.Controls.Add(adaptive);options.Controls.Add(new Label{Text="Instant turns · committed result, without a turn animation",AutoSize=true});grid.Controls.Add(options,0,0);
   var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Margin=Padding.Empty};grid.Controls.Add(panel,0,1);
   bool saving=false;
   Action save=async delegate{
    if(saving)return;saving=true;frame.Enabled=adaptive.Enabled=panel.Enabled=false;
    try{await Send(LocalApi.D("action","display-settings","settings",LocalApi.D("hide_frame",frame.Checked,"adaptive_motion",adaptive.Checked)));}
    finally{if(!dialog.IsDisposed){frame.Checked=bridge.FrameworkHidden;adaptive.Checked=bridge.AdaptiveMotion;frame.Enabled=adaptive.Enabled=panel.Enabled=IsReady;}saving=false;}
   };
   frame.CheckedChanged+=delegate{if(!saving)save();};adaptive.CheckedChanged+=delegate{if(!saving)save();};
   string[] names={"trk_faceShrink","trk_StickerSize","trk_ViewAngle","trk_LightDiff","trk_LightSpec","trk_LightAmb"};
   string[] labels={"Cell size","Sticker size","Field of view","Diffuse light","Specular light","Ambient light"};
   for(int i=0;i<names.Length;i++){
    string name=names[i],label=labels[i];var original=bridge.DisplaySlider(name);bool angle=name=="trk_ViewAngle";
    var row=new TableLayoutPanel{Width=490,Height=54,ColumnCount=2,RowCount=1,Margin=new Padding(0)};
    row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,150));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
    var caption=ToolLabel("");var slider=new TrackBar{Dock=DockStyle.Fill,Minimum=original.Minimum,Maximum=original.Maximum,Value=original.Value,TickFrequency=original.TickFrequency,SmallChange=original.SmallChange,LargeChange=original.LargeChange,AccessibleName=label};
    Action show=delegate{caption.Text=label+"  "+((angle?180.0:100.0)*slider.Value/slider.Maximum).ToString("F0")+(angle?"°":"%");};
    bool restoring=false;slider.ValueChanged+=delegate{if(restoring)return;try{bridge.SetDisplayValue(name,slider.Value);}catch(Exception error){restoring=true;try{slider.Value=original.Value;}finally{restoring=false;}Say(error.Message,true);}show();};show();row.Controls.Add(caption,0,0);row.Controls.Add(slider,1,0);panel.Controls.Add(row);
   }
   panel.Resize+=delegate{foreach(Control row in panel.Controls)row.Width=Math.Max(320,panel.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-4);};
   var footer=ToolRow();var done=ToolButton("Done");done.DialogResult=DialogResult.OK;footer.Controls.Add(done);grid.Controls.Add(footer,0,2);dialog.AcceptButton=done;dialog.CancelButton=done;
   dialog.FormClosing+=delegate(object sender,FormClosingEventArgs e){if(saving)e.Cancel=true;};ShowOwned(dialog);
  }
 }
}
