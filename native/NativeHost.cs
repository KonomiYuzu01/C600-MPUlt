// C600 native workbench: hosts the actual MPUlt Form1 and DirectX renderer.
// No DirectX reference is needed to compile this host. Reflection remains confined
// to the isolated user-supplied MPUlt process; the Python engine owns the journal.
// Windows build/runtime validation is required. Build against .NET Framework 4.x, x86.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal static class Reflect {
 internal const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
 internal static FieldInfo Field(Type type,string name){while(type!=null){var f=type.GetField(name,Flags|BindingFlags.DeclaredOnly);if(f!=null)return f;type=type.BaseType;}return null;}
 internal static object Get(object obj,string name){var f=Field(obj.GetType(),name);if(f==null)throw new MissingFieldException(obj.GetType().FullName,name);return f.GetValue(obj);}
 internal static void Set(object obj,string name,object value){var f=Field(obj.GetType(),name);if(f==null)throw new MissingFieldException(obj.GetType().FullName,name);f.SetValue(obj,value!=null && f.FieldType.IsPrimitive?Convert.ChangeType(value,f.FieldType):value);}
 internal static object Call(object obj,string name,params object[] args){foreach(var m in obj.GetType().GetMethods(Flags))if(m.Name==name&&m.GetParameters().Length==args.Length){try{return m.Invoke(obj,args);}catch(ArgumentException){}}throw new MissingMethodException(obj.GetType().FullName,name);}
 internal static object Clone(object obj){return typeof(object).GetMethod("MemberwiseClone",Flags).Invoke(obj,null);}
 internal static void SetProperty(object obj,string name,object value){var p=obj.GetType().GetProperty(name,Flags);if(p!=null&&p.CanWrite)p.SetValue(obj,Convert.ChangeType(value,p.PropertyType),null);else Set(obj,name,value);}
 internal static object Property(object obj,string name){var p=obj.GetType().GetProperty(name,Flags);return p!=null?p.GetValue(obj,null):Get(obj,name);}
}
internal sealed class LocalApi {
 readonly string root,token; readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=67108864,RecursionLimit=128};
 internal LocalApi(string root,string token){this.root=root.TrimEnd('/');this.token=token;}
 internal string Json(object x){lock(json)return json.Serialize(x);}
 internal object Parse(string x){lock(json)return json.DeserializeObject(x);}
 internal byte[] Bytes(string path){return Request(path,null);}
 byte[] Request(string path,object body){
  var r=(HttpWebRequest)WebRequest.Create(root+"/api/"+path);r.Proxy=null;r.Timeout=180000;r.ReadWriteTimeout=180000;r.Headers.Add("X-C600-Token",token);
  if(body!=null){r.Method="POST";r.ContentType="application/json";byte[] b=Encoding.UTF8.GetBytes(Json(body));r.ContentLength=b.Length;using(var s=r.GetRequestStream())s.Write(b,0,b.Length);}
  try{using(var res=r.GetResponse())using(var s=res.GetResponseStream())using(var ms=new MemoryStream()){s.CopyTo(ms);return ms.ToArray();}}
  catch(WebException e){if(e.Response==null)throw;using(var sr=new StreamReader(e.Response.GetResponseStream()))throw new InvalidOperationException(sr.ReadToEnd(),e);}
 }
 internal Dictionary<string,object> Get(string path){return AsDict(Parse(Encoding.UTF8.GetString(Request(path,null))));}
 internal object GetValue(string path){return Parse(Encoding.UTF8.GetString(Request(path,null)));}
 internal Dictionary<string,object> Post(string path,object body){
  var v=AsDict(Parse(Encoding.UTF8.GetString(Request(path,body))));
  if(v.ContainsKey("job")){string id=Convert.ToString(v["job"]);int delay=2;while(true){var j=Get("job/"+id);if(Convert.ToBoolean(j["done"])){if(j.ContainsKey("error"))throw new InvalidOperationException(Convert.ToString(j["error"]));return AsDict(j["result"]);}Thread.Sleep(delay);delay=Math.Min(40,delay*2);}}
  return v;
 }
 internal static Dictionary<string,object> AsDict(object x){return (Dictionary<string,object>)x;}
 internal static object[] Array(object x){return (object[])x;}
 internal static Dictionary<string,object> D(params object[] a){var d=new Dictionary<string,object>();for(int i=0;i<a.Length;i+=2)d[(string)a[i]]=a[i+1];return d;}
}
internal sealed class NativeWorkbench : IMessageFilter {
 readonly Form form;readonly LocalApi api;readonly string exe;readonly Control viewport;
 readonly NativeRendererLifecycle renderer;
 readonly NativePickingVisibility picking;
 readonly NativeFullRenderer fullRenderer;
 readonly NativeStickerAccess stickerAccess;
 NativeSnapshot preparedSnapshot,cachedSnapshot,operationSnapshot;
 readonly NativeStructureExplorer explorer=new NativeStructureExplorer();
 NativeAuxiliaryViews auxiliaries;NativeCellGeometry auxiliaryGeometry;
 int? pendingFocusColor;bool changingCellFocus,focusDispatchQueued;int synchronizedFocusColor=-1;
 string auxiliaryFailure;
 Dictionary<string,object> structureData;
 int[] labToNativeSlot;
 Color[] canonicalPalette;
 int inspectedOrbit=-1;string bufferStateHash;
 Dictionary<string,object> bufferAnalysis;
 readonly List<Dictionary<string,object>> analyzedFrames=new List<Dictionary<string,object>>();
 object explorerRules;string explorerPreviewState,explorerPreviewRules,explorerContext;long explorerRequest;
 readonly NativeDockLayout workspace=new NativeDockLayout();readonly TabControl tabs=new TabControl();readonly StatusStrip statusBar=new StatusStrip();readonly ToolStripStatusLabel message=new ToolStripStatusLabel();
 readonly ComboBox orbit=new ComboBox();readonly NumericUpDown cell=new NumericUpDown(),target=new NumericUpDown(),batch=new NumericUpDown();
 readonly TextBox filter=new TextBox(),word=new TextBox(),bufferText=new TextBox(),certificate=new TextBox(),macroName=new TextBox();
 readonly ComboBox checkpoint=new ComboBox(),presets=new ComboBox(),node=new ComboBox();readonly ListView progress=new ListView(),history=new ListView();
 readonly FlowLayoutPanel gripPanel=new FlowLayoutPanel(); readonly CheckBox pin=new CheckBox(),autoTarget=new CheckBox();
 Dictionary<string,object> state,profile;object puz,cube;Array nativeStickers;int[] nativeToLab,nativeFaces,labToNativeCell;
 object[] shownBases,hiddenBases;object[] baseForSlot;byte[] previousStyles;bool busy=true,connected=false,closing=false,syncing=false;
 readonly FieldInfo colField; readonly FieldInfo baseField;readonly Dictionary<string,object> empty=LocalApi.D();
 readonly List<Control> guarded=new List<Control>(); readonly Panel dock=new Panel();
 readonly Dictionary<Keys,string> keymap=new Dictionary<Keys,string>();
 readonly CheckedListBox types=new CheckedListBox();
 readonly Label filterSummary=new Label();
 readonly CheckBox hideFrame=new CheckBox();
 readonly ToolStripMenuItem frameMenu=new ToolStripMenuItem("Hide 600-cell frame");
 bool updatingFrameControls;
 readonly TextBox macroA=new TextBox(),macroB=new TextBox(),pieceText=new TextBox(),sessionReport=new TextBox(),setName=new TextBox(),setExpression=new TextBox();
 readonly ListBox macroLibrary=new ListBox();
 readonly NumericUpDown piecePosition=new NumericUpDown();
 readonly ComboBox setKind=new ComboBox();
 Dictionary<string,object> inspectedPiece;
 string displayedRules=null,macroACaption=null,appliedFilterText="active",filterStatus="";
 object macroARecipe;
 string stateHash="";Dictionary<string,object>[] gripAxes; int heartbeatInFlight; readonly bool layoutOnly;readonly System.Windows.Forms.Timer clockTimer=new System.Windows.Forms.Timer();
 bool instantTurns=true;
 int animatedTurnRate;bool nativeColorsDirty;
 object defaultCamera;
 readonly ToolTip hints=new ToolTip{AutoPopDelay=12000,InitialDelay=500,ReshowDelay=100};
 readonly Font uiFont=new Font("Segoe UI",9F);
 readonly ToolStripStatusLabel displayState=new ToolStripStatusLabel();
 readonly List<ToolStripItem> stateTools=new List<ToolStripItem>();

 internal NativeWorkbench(Form form,string exe,string root,string token,bool layoutOnly=false){
  Program.UseEnglishUi();
  this.layoutOnly=layoutOnly;
  this.form=form;this.exe=exe;api=new LocalApi(root,token);puz=Reflect.Get(form,"Puz");cube=Reflect.Get(form,"CubeView");viewport=(Control)Reflect.Get(form,"dxControl2");
  nativeStickers=(Array)Reflect.Get(cube,"Stks");var stType=nativeStickers.GetValue(0).GetType();colField=stType.GetField("Col",Reflect.Flags);baseField=stType.GetField("Base",Reflect.Flags);
  stickerAccess=new NativeStickerAccess(nativeStickers);
  if(!layoutOnly){StopLegacyClock();InitializeStickerDimensions();animatedTurnRate=Convert.ToInt32(Reflect.Get(form,"TRate"));Reflect.Set(form,"TRate",1);}
  if(!layoutOnly){renderer=new NativeRendererLifecycle(viewport);renderer.Subset=new NativeRenderSubset(viewport,cube,puz);fullRenderer=NativeFullRenderer.TryInstall(viewport,cube,renderer.Subset);if(fullRenderer==null)throw new InvalidOperationException("The native display policy could not be installed. Input remains disabled.");picking=new NativePickingVisibility(viewport,form,cube,puz,renderer.PreparePicking,delegate{return connected&&!busy&&!syncing&&!closing;});}
  if(picking!=null)picking.BeforeNativeClick=delegate{renderer.HoldUpdates=true;Reflect.Set(form,"TRate",1);nativeColorsDirty=true;};
  InitializeKeys();Build();if(renderer!=null)renderer.Resume();viewport.Enabled=false;form.Text="Full 600-cell · C600 Native "+Program.Version+" / MPUlt";
  if(!layoutOnly)form.Shown+=delegate{StopLegacyClock();form.MinimumSize=new Size(1000,650);Connect();};
  FormClosingEventHandler legacyClose=null;
  if(!layoutOnly){var method=form.GetType().GetMethod("Form1_FormClosing",Reflect.Flags);if(method==null)throw new MissingMethodException("Original MPUlt close handler is unavailable.");legacyClose=(FormClosingEventHandler)Delegate.CreateDelegate(typeof(FormClosingEventHandler),form,method);form.FormClosing-=legacyClose;}
  form.FormClosing+=delegate(object sender,FormClosingEventArgs e){
   if(e.Cancel)return;
   if(busy&&connected&&MessageBox.Show(form,"An operation is in progress. Close and recover the last committed state on restart?","Close",MessageBoxButtons.OKCancel)!=DialogResult.OK){e.Cancel=true;return;}
   // The original handler clears Form1.Puz even when e.Cancel is true. Run it
   // only after our recovery/close decision, so Cancel leaves MPUlt usable.
   closing=true;clockTimer.Stop();if(legacyClose!=null)legacyClose(sender,e);
  };
  viewport.MouseUp+=delegate(object sender,MouseEventArgs e){int hit;string gesture;if(picking!=null&&picking.TakeInspection(e.Button,out hit,out gesture)){string pre=stateHash;if(connected&&!busy&&!syncing)form.BeginInvoke((Action)delegate{InspectNative(hit,gesture,pre);});return;}if(picking!=null&&picking.SuppressCapture)return;if(connected&&!busy&&!syncing)form.BeginInvoke((Action)delegate{CaptureNativeTurns();});};
  Application.AddMessageFilter(this);
  form.Disposed+=delegate{hints.Dispose();uiFont.Dispose();};
  form.FormClosed+=delegate{closing=true;clockTimer.Stop();clockTimer.Dispose();if(auxiliaries!=null)auxiliaries.Dispose();if(picking!=null)picking.Dispose();if(renderer!=null)renderer.Dispose();if(fullRenderer!=null)fullRenderer.Dispose();Application.RemoveMessageFilter(this);NativeDiagnostics.Write("Window closed; message filter, renderer lifecycle and heartbeat disposed");};
 }
 Control FieldControl(string name){return Reflect.Get(form,name) as Control;}
 void StopLegacyClock(){var field=Reflect.Field(form.GetType(),"m_Timer");var timer=field==null?null:field.GetValue(form) as System.Threading.Timer;if(timer!=null)timer.Change(Timeout.Infinite,Timeout.Infinite);}
 internal bool TryRecoverRenderer(Exception error){return renderer!=null&&renderer.TryRecover(error);}
 void Hide(string name){var x=FieldControl(name);if(x!=null)x.Hide();}
 TabPage Page(string name){var t=new TabPage(name){Padding=new Padding(8),BackColor=Color.FromArgb(246,247,249)};tabs.TabPages.Add(t);return t;}
 FlowLayoutPanel Flow(TabPage t){var p=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Margin=Padding.Empty};t.Controls.Add(p);bool fitting=false;Action fit=delegate{if(fitting)return;fitting=true;try{FitFlow(p,Math.Max(80,p.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-2));}finally{fitting=false;}};p.SizeChanged+=delegate{fit();};p.ControlAdded+=delegate{fit();};return p;}
 static void FitFlow(FlowLayoutPanel panel,int width){
  foreach(Control child in panel.Controls){
   int w=Math.Max(40,width-child.Margin.Horizontal);var row=child as FlowLayoutPanel;
   if(row!=null){row.MaximumSize=row.AutoSize?new Size(w,0):Size.Empty;row.Width=w;continue;}
   var label=child as Label;if(label!=null){label.MaximumSize=new Size(w,0);continue;}
   var box=child as CheckBox;if(box!=null){box.AutoSize=false;box.Width=w;box.Height=Math.Max(26,TextRenderer.MeasureText(box.Text,box.Font,new Size(Math.Max(20,w-22),0),TextFormatFlags.WordBreak).Height+8);continue;}
   if(child is NumericUpDown)continue;
   // Native edit/combo controls can report a zero preferred height when given
   // a width-only maximum. Keep their requested height and only fit the width.
   var text=child as TextBox;if(text!=null&&text.Multiline)text.AutoSize=false;
   var combo=child as ComboBox;if(combo!=null)child.MinimumSize=new Size(0,combo.PreferredHeight);
   child.MaximumSize=Size.Empty;child.Width=w;
  }
 }
 Label Label(string text){return new Label{Text=text,AutoSize=true,MaximumSize=new Size(310,0),Margin=new Padding(3,8,3,3)};}
 Button Button(string text,Action action,bool mutation=true){var b=new Button{Text=text,AutoSize=true,MinimumSize=new Size(95,29),Margin=new Padding(3),UseVisualStyleBackColor=true};b.Click+=delegate{if(!busy||!mutation)action();};if(mutation)guarded.Add(b);return b;}
 FlowLayoutPanel Row(params Control[] controls){var r=new FlowLayoutPanel{AutoSize=true,MaximumSize=new Size(320,0),WrapContents=true};r.Controls.AddRange(controls);return r;}
 void Add(FlowLayoutPanel p,params Control[] controls){p.Controls.AddRange(controls);}
 void Section(FlowLayoutPanel parent,string title,params Control[] controls){var toggle=Button("+ "+title,delegate{},false);toggle.TextAlign=ContentAlignment.MiddleLeft;toggle.FlatStyle=FlatStyle.Flat;toggle.FlatAppearance.BorderSize=0;parent.Controls.Add(toggle);foreach(var c in controls){c.Visible=false;parent.Controls.Add(c);}toggle.Click+=delegate{bool show=toggle.Text.StartsWith("+",StringComparison.Ordinal);toggle.Text=(show?"− ":"+ ")+title;foreach(var c in controls)c.Visible=show;};}
 void Build(){
  NativeDiagnostics.Write("Building native workbench controls");
  form.SuspendLayout();try { form.MinimumSize=new Size(1000,650);form.Size=new Size(1440,900);dock.Font=uiFont;tabs.Font=uiFont;explorer.Font=uiFont;
  Hide("panel1");Hide("splitter1");Hide("menuStrip1");Hide("panel3");Hide("splitter2");
  var menu=new MenuStrip{Dock=DockStyle.Top,Font=uiFont};
  var file=new ToolStripMenuItem("File");file.DropDownItems.Add("Save checkpoint",null,delegate{SaveCheckpoint();});file.DropDownItems.Add("Backup journal",null,delegate{Run(delegate{api.Post("backup",empty);},RefreshFromServer);});file.DropDownItems.Add("Export proof session…",null,delegate{Export("export","session.c600.json.gz");});file.DropDownItems.Add("Export certificate…",null,delegate{Export("certificate","macro.c600.json");});file.DropDownItems.Add(new ToolStripSeparator());file.DropDownItems.Add("Close",null,delegate{form.Close();});
  var edit=new ToolStripMenuItem("Edit");edit.DropDownItems.Add("Undo  Ctrl+Z",null,delegate{Command("undo");});edit.DropDownItems.Add("Redo  Ctrl+Shift+Z",null,delegate{Command("redo");});
  edit.DropDownItems.Add("Keybindings…",null,delegate{EditKeys();});
  file.DropDownItems.Insert(0,new ToolStripMenuItem("Reconnect native bridge",null,delegate{if(!busy&&!connected)Connect();}));
  file.DropDownItems.Insert(1,new ToolStripMenuItem("Save log  Ctrl+S",null,delegate{SaveLog();}));
  file.DropDownItems.Insert(2,new ToolStripMenuItem("Import log…  Ctrl+O",null,delegate{ImportLog();}));
  file.DropDownItems.Insert(3,new ToolStripMenuItem("Export log…",null,delegate{ExportLog();}));
  var view=new ToolStripMenuItem("View");view.DropDownItems.Add("Tools panel  F8",null,delegate{ToggleTools();});view.DropDownItems.Add("Display and projection…",null,delegate{OriginalViewControls();});view.DropDownItems.Add("Reset view",null,delegate{ResetView();});view.DropDownItems.Add("Center focused cell",null,delegate{Center();});
  frameMenu.CheckOnClick=true;frameMenu.Checked=true;frameMenu.Enabled=false;frameMenu.CheckedChanged+=delegate{ChangeFrameMode(frameMenu.Checked);};view.DropDownItems.Add(frameMenu);
  var smooth=new ToolStripMenuItem("Smooth camera motion (full detail when still)"){CheckOnClick=true,Checked=true};smooth.CheckedChanged+=delegate{if(renderer!=null)renderer.SmoothMotion=smooth.Checked;};view.DropDownItems.Add(smooth);
  view.DropDownItems.Add(new ToolStripMenuItem("Instant turns after durable commit"){Enabled=false,Checked=instantTurns});
  view.DropDownItems.Add(new ToolStripSeparator());view.DropDownItems.Add("Global cell overview",null,delegate{OpenAuxiliary(false);});view.DropDownItems.Add("Focused cell neighborhood",null,delegate{OpenAuxiliary(true);});view.DropDownItems.Add("Reset auxiliary window positions",null,delegate{if(auxiliaries!=null)auxiliaries.ResetPlacement();});
  var puzzle=new ToolStripMenuItem("Puzzle");puzzle.DropDownItems.Add("Scramble 1,000 turns (undoable)",null,delegate{Scramble();});
  puzzle.DropDownItems.Add("Reset to solved (keep checkpoint)",null,delegate{ResetPuzzle();});
  var help=new ToolStripMenuItem("Help");help.DropDownItems.Add("Controls and validation",null,delegate{MessageBox.Show(form,"MPUlt provides the actual viewport, camera and mouse twisting.\n\nLeft drag: 3D rotation\nShift+left drag: 4D rotation\nRight drag: horizontal roll, vertical 4D slide\nCtrl+left/right drag: radius / field of view\n\nCtrl+click: native cell recenter\nShift+left click: home Cell Centers\nShift+right click: required piece / buffer destination\nNative multi-click corner grips: unchanged\nN: preview assisted insertion\nEnter: commit preview\nEscape: cancel\nF9: checkpoint\nCtrl+S: save log\nCtrl+O: import log\nReset: solved state with a recovery checkpoint\nCtrl+Z / Ctrl+Shift+Z: exact undo/redo\n1..7: preview focused-cell grip; Shift: inverse\nF8: tools panel\n\nState changes go through the labelled backend. File and Session provide verified C600 / MPUlt v1 log import and export. Hidden pieces cannot be picked. Legacy setup/macro commands use the journal and certified operations here.\n\nThe native bridge must match all 259,800 slots and 1,200 generators before input is enabled. This host is an experimental integration, not a Windows-certified release.","C600 Native");});
  menu.Items.AddRange(new ToolStripItem[]{file,edit,view,puzzle,help});form.Controls.Add(menu);menu.BringToFront();form.MainMenuStrip=menu;
  dock.Dock=DockStyle.Fill;dock.Padding=new Padding(2);tabs.Dock=DockStyle.Fill;tabs.Multiline=true;dock.Controls.Add(tabs);
  message.Spring=true;message.AutoToolTip=true;message.TextAlign=ContentAlignment.MiddleLeft;message.Text="Loading full native geometry. Controls are disabled until the bridge passes.";statusBar.Items.Add(message);displayState.BorderSides=ToolStripStatusLabelBorderSides.Left;displayState.Text="Connecting";displayState.ToolTipText="Only the visibility filter changes what can be picked. Motion detail changes drawing only.";statusBar.Items.Add(displayState);form.Controls.Add(statusBar);statusBar.BringToFront();
  statusBar.Font=uiFont;var tools=new ToolStrip{Dock=DockStyle.Top,GripStyle=ToolStripGripStyle.Hidden,Font=uiFont};orbit.Font=uiFont;cell.Font=uiFont;
  stateTools.Add(tools.Items.Add("Undo",null,delegate{Command("undo");}));stateTools.Add(tools.Items.Add("Redo",null,delegate{Command("redo");}));stateTools.Add(tools.Items.Add("Checkpoint",null,delegate{SaveCheckpoint();}));tools.Items.Add(new ToolStripSeparator());
  orbit.DropDownStyle=ComboBoxStyle.DropDownList;orbit.Width=65;for(int o=0;o<35;o++)orbit.Items.Add("O"+o.ToString("00"));orbit.SelectedIndex=33;orbit.SelectedIndexChanged+=delegate{if(connected&&!syncing&&!busy){int selectedOrbit=orbit.SelectedIndex;Run(delegate{api.Post("prefs",LocalApi.D("orbit",selectedOrbit));},RefreshFromServer);}};
  tools.Items.Add(new ToolStripLabel("Orbit"));tools.Items.Add(new ToolStripControlHost(orbit));cell.Minimum=0;cell.Maximum=599;cell.Width=58;cell.AccessibleName="Focused lab cell, zero-based";tools.Items.Add(new ToolStripLabel("Lab cell"));tools.Items.Add(new ToolStripControlHost(cell));stateTools.Add(tools.Items.Add("Center",null,delegate{Center();LoadGrips();}));tools.Items.Add(new ToolStripSeparator());stateTools.Add(tools.Items.Add("Preview insertion",null,delegate{Suggest();}));stateTools.Add(tools.Items.Add("Apply preview",null,delegate{Commit();}));tools.Items.Add(new ToolStripSeparator());tools.Items.Add("Display…",null,delegate{OriginalViewControls();});tools.Items.Add("Reset view",null,delegate{ResetView();});var toggleTools=tools.Items.Add("Tools · F8",null,delegate{ToggleTools();});toggleTools.Alignment=ToolStripItemAlignment.Right;form.Controls.Add(tools);tools.BringToFront();menu.BringToFront();
  var fp=Flow(Page("Filters"));presets.Width=308;presets.DropDownStyle=ComboBoxStyle.DropDownList;presets.Items.AddRange(new object[]{"Active orbit","Unsolved orbit","Orientation only","Work cell","All pieces","Selected color","Selected current cell","Adjacent colors","Selected center layer","Selected home layer"});presets.SelectedIndex=0;
  presets.AccessibleName="Piece filter preset";presets.SelectionChangeCommitted+=delegate{if(connected&&!busy&&!syncing)LoadPreset();};
  hideFrame.Text="Hide 600-cell frame";hideFrame.AccessibleName="Hide 600-cell frame";hideFrame.AutoSize=true;hideFrame.Checked=true;hideFrame.CheckedChanged+=delegate{ChangeFrameMode(hideFrame.Checked);};
  guarded.Add(hideFrame);guarded.Add(presets);guarded.Add(filter);guarded.Add(pin);guarded.Add(types);
  filter.Multiline=true;filter.Width=306;filter.Height=65;filter.Text="active";pin.Text="Show buffers/selection/preview outside filter";pin.Checked=false;pin.AutoSize=true;
  pin.CheckedChanged+=delegate{if(connected&&!busy&&!syncing){bool value=pin.Checked;Run(delegate{api.Post("prefs",LocalApi.D("pin_safety",value));},RefreshFromServer);}};
  filterSummary.AutoSize=true;filterSummary.MaximumSize=new Size(307,0);
  filter.TextChanged+=delegate{if(!syncing)UpdateFilterSummary();};
  Add(fp,Label("Visible pieces"),filterSummary,hideFrame,Label("Quick preset"),presets,Row(Button("Use preset",LoadPreset),Button("Structure…",delegate{tabs.SelectedIndex=7;},false)),Label("Filter expression"),filter,Row(Button("Preview counts",PreviewFilterEditor),Button("Apply filter",ApplyFilter)),pin);
  hints.SetToolTip(presets,"The first five quick presets apply immediately. Color and layer presets prepare an expression and count preview.");hints.SetToolTip(filter,"Whole-piece filters never change puzzle state. Use Structure to preview Replace, Intersect or Union with your existing rules.");
  types.Width=307;types.Height=210;types.CheckOnClick=true;for(int o=0;o<35;o++)types.Items.Add("O"+o.ToString("00"));
  Section(fp,"Moving orbits",Label("Check orbit rows, then apply. Right-click a row to exclude that orbit immediately."),types,Row(Button("Apply checked",delegate{var s=new List<string>();foreach(int i in types.CheckedIndices)s.Add("O"+i.ToString("00"));filter.Text=s.Count==0?"nothing":String.Join(" | ",s.ToArray());ApplyFilter();}),Button("Protect orbit",Protect)));
  types.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Right&&!busy){int i=types.IndexFromPoint(e.Location);if(i>=0)ExcludeOrbit(i);}};
  Section(fp,"Filter syntax and motion detail",Label("C1 = lab cell 0. Try color(C17), cell(C17), adjacent(C17), layer(C17,L3) or home_layer(C17,L3). Legacy home(C013), current(C013), cap and shell keep zero-based IDs. Hidden pieces remain in the full puzzle state."),Label("Smooth camera motion samples large views while moving. Every visible sticker returns when motion stops. Exact picking uses complete visible geometry. Change this in View."));
  setName.Width=306;setName.Text="my_set";setExpression.Width=306;setExpression.Text="selected";setKind.Width=306;setKind.DropDownStyle=ComboBoxStyle.DropDownList;setKind.Items.AddRange(new object[]{"identity","position"});setKind.SelectedIndex=0;
  Section(fp,"Frozen piece sets",Label("Save the current matches as identities that move, or fixed positions. This is a frozen set, not a live query."),setName,setExpression,setKind,Button("Save set",SaveSet));
  var sp=Flow(Page("Solve"));batch.Minimum=1;batch.Maximum=50;batch.Value=1;batch.Width=60;target.Minimum=0;target.Maximum=177119;target.Width=100;autoTarget.Text="Choose destination automatically";autoTarget.Checked=true;autoTarget.AutoSize=true;
  Add(sp,Label("Certified insertion"),autoTarget,Row(Label("Destination P"),target),Row(Label("Batch"),batch),Row(Button("Preview",Suggest),Button("Apply",Commit),Button("Cancel",Cancel,false)));
  gripPanel.Width=310;gripPanel.Height=72;Add(sp,Label("Focused-cell grips (right-click for inverse)"),gripPanel);
  word.Width=306;word.Height=60;word.Multiline=true;word.Text="H0 T1 H0 T1'";Add(sp,Label("Primitive word, e.g. H0 T1 H0 T1'"),word,Button("Preview word",PreviewWord));
  certificate.Multiline=true;certificate.ReadOnly=true;certificate.ScrollBars=ScrollBars.Vertical;certificate.Width=307;certificate.Height=240;Add(sp,certificate);
  macroName.Width=306;macroName.Text="My macro";Add(sp,macroName,Button("Save preview macro",delegate{string name=macroName.Text;Run(delegate{api.Post("save-macro",LocalApi.D("name",name));},RefreshFromServer);}));
  var bp=Flow(Page("Buffers"));node.Width=307;node.DropDownStyle=ComboBoxStyle.DropDownList;bufferText.Multiline=true;bufferText.ReadOnly=true;bufferText.ScrollBars=ScrollBars.Vertical;bufferText.Width=307;bufferText.Height=410;bufferText.Text="No destination analyzed. Shift+right click a visible piece, or choose a destination in Solve and select Analyze.";
  node.SelectedIndexChanged+=delegate{UpdateBufferText();};
  Add(bp,Label("Use the fixed destination from the Solve tab."),Button("Analyze",Analyze),node,Row(Button("Star",delegate{PreviewStar(1);}),Button("Inverse star",delegate{PreviewStar(-1);})),bufferText);
  var pp=Page("Progress");progress.Dock=DockStyle.Fill;progress.View=View.Details;progress.FullRowSelect=true;progress.Columns.Add("Orbit",55);progress.Columns.Add("Solved",100);progress.Columns.Add("Position",75);progress.Columns.Add("Orient",65);pp.Controls.Add(progress);progress.DoubleClick+=delegate{if(progress.SelectedItems.Count>0)orbit.SelectedIndex=Convert.ToInt32(progress.SelectedItems[0].Tag);};
  var hp=Flow(Page("Session"));checkpoint.Width=307;checkpoint.DropDownStyle=ComboBoxStyle.DropDownList;Add(hp,Row(Button("Checkpoint",SaveCheckpoint),Button("Backup",delegate{Run(delegate{api.Post("backup",empty);},RefreshFromServer);})),checkpoint,Button("Restore checkpoint",Restore));
  Add(hp,Button("Reset to solved",ResetPuzzle),Label("Reset keeps a checkpoint of your current progress and retains your view settings."),Row(Button("Save log",SaveLog),Button("Import log…",ImportLog),Button("Export log…",ExportLog)),Label("Save log writes a new file in your session's logs folder. Import verifies the record and preserves your current branch."));
  history.Width=307;history.Height=310;history.View=View.Details;history.FullRowSelect=true;history.Columns.Add("ID",50);history.Columns.Add("Parent",50);history.Columns.Add("Operation",195);Add(hp,Label("Recent journal branches"),history,Row(Button("Refresh history",History),Button("Checkout",Checkout)));
  Add(hp,Row(Button("Start timer",delegate{SessionTimer("start");}),Button("Pause timer",delegate{SessionTimer("pause");})));
  sessionReport.Multiline=true;sessionReport.ReadOnly=true;sessionReport.ScrollBars=ScrollBars.Vertical;sessionReport.Width=307;sessionReport.Height=180;Add(hp,Button("Refresh session report",ReportSession),sessionReport);
  var mp=Flow(Page("Macros"));macroA.Multiline=true;macroA.Width=306;macroA.Height=65;macroB.Multiline=true;macroB.Width=306;macroB.Height=65;macroA.Text="H0";macroB.Text="T1";
  Add(mp,Label("Word A"),macroA,Label("Word B"),macroB,Row(Button("[A, B]",delegate{ComposeMacro("commutator");}),Button("A B A^-1",delegate{ComposeMacro("conjugate");}),Button("A^-1",delegate{ComposeMacro("inverse");}),Button("A B",delegate{ComposeMacro("concat");})),Button("Preview to A",UsePreviewAsMacroA),Label("Saved certified macros"));
  macroLibrary.Width=307;macroLibrary.Height=180;Add(mp,macroLibrary,Row(Button("Preview saved",delegate{PreviewSavedMacro(false);}),Button("Inverse saved",delegate{PreviewSavedMacro(true);})));
  var ip=Flow(Page("Pieces"));piecePosition.Minimum=0;piecePosition.Maximum=177119;piecePosition.Width=170;pieceText.Multiline=true;pieceText.ReadOnly=true;pieceText.ScrollBars=ScrollBars.Vertical;pieceText.Width=307;pieceText.Height=330;pieceText.Text="No inspection yet.\r\n\r\nShift+left click shows the occupying identity's home Cell Centers.\r\n\r\nShift+right click finds the identity required at the clicked destination.\r\n\r\nDrag beyond the normal movement threshold to rotate instead.";
  Add(ip,Label("Shift+left click: home Cell Centers. Shift+right click: required piece and buffer destination. Shift+left drag: 4D rotation."),Button("Clear inspection",ClearInspection),Label("Inspect position P"),piecePosition,Row(Button("Inspect / select",InspectPiece),Button("Track identity",TrackPiece)),Row(Button("Previous unsolved",delegate{NextPiece(-1);}),Button("Next unsolved",delegate{NextPiece(1);})),Row(Button("Use destination",UsePieceDestination),Button("Find required piece",FindRequiredPiece)),Row(Button("Center current",delegate{CenterInspected(false);}),Button("Center home",delegate{CenterInspected(true);})),pieceText);
  var ep=Page("Structure");explorer.Dock=DockStyle.Fill;ep.Controls.Add(explorer);var auxiliaryPage=Page("Views");
  explorer.CenterRequested+=delegate(int color){if(connected&&!busy){cell.Value=color-1;Center();LoadGrips();}};
  explorer.PreviewRequested+=PreviewExplorer;
  explorer.ApplyRequested+=ApplyExplorer;
  explorer.InsertRequested+=delegate(string expression){filter.Text=expression;tabs.SelectedIndex=0;filter.Focus();};
  // A single layout root prevents old designer Dock/Z-order from covering the viewport.
  foreach(Control child in form.Controls)child.Hide();
  var layout=new TableLayoutPanel{Size=form.ClientSize,Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Margin=Padding.Empty,Padding=Padding.Empty};
  layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  layout.RowStyles.Add(new RowStyle(SizeType.Absolute,26));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,34));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,24));
  workspace.Size=new Size(Math.Max(0,form.ClientSize.Width),Math.Max(0,form.ClientSize.Height-84));workspace.Dock=DockStyle.Fill;
  menu.Parent=layout;tools.Parent=layout;statusBar.Parent=layout;menu.Dock=DockStyle.Fill;tools.Dock=DockStyle.Fill;statusBar.Dock=DockStyle.Fill;menu.Show();tools.Show();statusBar.Show();
  layout.Controls.Add(menu,0,0);layout.Controls.Add(tools,0,1);layout.Controls.Add(workspace,0,2);layout.Controls.Add(statusBar,0,3);form.Controls.Add(layout);layout.BringToFront();
  // Root every destination in the live Form before moving the DirectX control.
  // MPUlt's device resize and idle callbacks expect ParentForm to be available.
  layout.Show();workspace.Show();workspace.ViewportPanel.Show();workspace.FitPanels();
  viewport.Parent=workspace.ViewportPanel;viewport.Dock=DockStyle.Fill;viewport.Show();dock.Parent=workspace.ToolsPanel;dock.Dock=DockStyle.Fill;dock.Show();
  auxiliaries=new NativeAuxiliaryViews(form,auxiliaryPage);
  auxiliaries.SelectionChanged+=delegate(int color){SelectAuxiliaryColor(color,false);};
  auxiliaries.CenterRequested+=delegate(int color){SelectAuxiliaryColor(color,true);};
  auxiliaries.DrawerRequested+=delegate{workspace.ToolsRequested=true;tabs.SelectedIndex=8;};
  var auxiliaryTools=new ToolStripDropDownButton("Cell views");auxiliaryTools.DropDownItems.Add("Global overview · 600 cells",null,delegate{OpenAuxiliary(false);});auxiliaryTools.DropDownItems.Add("Focused neighborhood",null,delegate{OpenAuxiliary(true);});auxiliaryTools.DropDownItems.Add("Reset window positions",null,delegate{auxiliaries.ResetPlacement();});tools.Items.Insert(tools.Items.Count-1,auxiliaryTools);
  cell.ValueChanged+=delegate{if(!changingCellFocus&&!syncing&&connected){int color=(int)cell.Value+1;auxiliaries.SetSelectedColor(color);if(auxiliaryGeometry!=null){explorer.NavigateColor(color);synchronizedFocusColor=color;}QueueCellFocus(color);}};
  foreach(var ctl in guarded)ctl.Enabled=false;
  foreach(var item in stateTools)item.Enabled=false;orbit.Enabled=false;cell.Enabled=false;
  } finally { form.ResumeLayout(true); }
  workspace.FitPanels();
  clockTimer.Interval=5000;clockTimer.Tick+=delegate{if(connected&&!busy&&!closing&&Interlocked.CompareExchange(ref heartbeatInFlight,1,0)==0)Task.Factory.StartNew(delegate{try{api.Post("timer",LocalApi.D("action","heartbeat"));}catch(Exception e){NativeDiagnostics.Write("Heartbeat failed",e);}finally{Interlocked.Exchange(ref heartbeatInFlight,0);}});};
  NativeDiagnostics.Write("Native workbench layout constructed");
 }
 void ToggleTools(){workspace.ToolsRequested=!workspace.ToolsRequested;}
 void InitializeStickerDimensions(){
  // MPUlt's saved sliders and CubeObj's dimensions can disagree: its original
  // SetStickerSize updates coordinates without updating FShr/SShr. Start with
  // the dimensions this host used for newly visible meshes, and make both the
  // controls and every original mesh agree before filtering begins.
  double fs=Convert.ToDouble(Reflect.Get(cube,"FShr")),ss=Convert.ToDouble(Reflect.Get(cube,"SShr"));
  bool update=Convert.ToBoolean(Reflect.Get(form,"m_setgeom"));Reflect.Set(form,"m_setgeom",false);
  try{SetDisplaySlider("trk_faceShrink",fs);SetDisplaySlider("trk_StickerSize",ss);}finally{Reflect.Set(form,"m_setgeom",update);}
  SyncStickerDimensions();Reflect.Call(cube,"SetStickerSize",Reflect.Get(cube,"FShr"),Reflect.Get(cube,"SShr"));
  Reflect.Call(Reflect.Property(Reflect.Property(viewport,"Scene"),"Camera"),"SetChanged");
 }
 void SyncStickerDimensions(){if(layoutOnly)return;Reflect.Set(cube,"FShr",Reflect.Property(form,"CPShrinkFace"));Reflect.Set(cube,"SShr",Reflect.Property(form,"CPStickerSize"));}
 void SetDisplaySlider(string name,double ratio){var slider=(TrackBar)FieldControl(name);slider.Value=Math.Max(slider.Minimum,Math.Min(slider.Maximum,(int)Math.Round(ratio*slider.Maximum)));}
 void OriginalViewControls(){
  if(busy||!connected)return;
  using(var dialog=new Form{Text="Display and projection · MPUlt",ClientSize=new Size(420,600),MinimumSize=new Size(360,480),Font=uiFont,StartPosition=FormStartPosition.CenterParent}){
   var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};dialog.Controls.Add(panel);
   panel.Controls.Add(Label("View controls change only the camera and display. Ctrl+left drag adjusts radius; Ctrl+right drag adjusts field of view."));
   var motion=new CheckBox{Text="Adaptive motion; full detail when still",AutoSize=true,Checked=renderer.SmoothMotion};motion.CheckedChanged+=delegate{renderer.SmoothMotion=motion.Checked;};panel.Controls.Add(motion);
   string[] names={"trk_faceShrink","trk_StickerSize","trk_ViewAngle","trk_LightDiff","trk_LightSpec","trk_LightAmb"};
   string[] titles={"Cell size","Sticker size","Field of view","Diffuse light","Specular light","Ambient light"};
   string[] descriptions={"Separate tetrahedral cells for structural reading.","Separate stickers within each cell.","Perspective angle; this does not change the puzzle.","Directional surface shading.","Surface highlights.","Uniform light for dark-facing stickers."};
   for(int i=0;i<names.Length;i++){
    var original=FieldControl(names[i]) as TrackBar;if(original==null)continue;string title=titles[i];bool angle=names[i]=="trk_ViewAngle",geometry=names[i]=="trk_faceShrink"||names[i]=="trk_StickerSize";
    var caption=Label("");panel.Controls.Add(caption);var slider=new TrackBar{Minimum=original.Minimum,Maximum=original.Maximum,Value=original.Value,TickFrequency=original.TickFrequency,SmallChange=original.SmallChange,LargeChange=original.LargeChange,Width=360,Height=36,AccessibleName=title};
    Action show=delegate{caption.Text=title+"  "+(angle?(180.0*slider.Value/slider.Maximum).ToString("F0")+"°":(100.0*slider.Value/slider.Maximum).ToString("F0")+"%");};show();
    slider.ValueChanged+=delegate{original.Value=slider.Value;if(geometry)SyncStickerDimensions();show();};hints.SetToolTip(slider,descriptions[i]);panel.Controls.Add(slider);
   }
   var close=new Button{Text="Done",Dock=DockStyle.Bottom,Height=34,DialogResult=DialogResult.OK};dialog.Controls.Add(close);dialog.AcceptButton=close;dialog.CancelButton=close;dialog.ShowDialog(form);
  }
 }

 void Connect(){
  busy=false;Run(delegate{
   NativeDiagnostics.Write("Exporting live native geometry");var geometry=Geometry();NativeDiagnostics.Write("Checking native geometry against full model");api.Post("native/handshake",geometry);profile=api.Get("native/map");nativeToLab=ToInts(profile["native_to_lab"]);nativeFaces=ToInts(profile["native_face_to_lab"]);labToNativeCell=new int[600];for(int i=0;i<600;i++)labToNativeCell[nativeFaces[i]]=i;
   labToNativeSlot=new int[nativeToLab.Length];for(int i=0;i<nativeToLab.Length;i++)labToNativeSlot[nativeToLab[i]]=i;structureData=api.Get("structure");cachedSnapshot=null;
  },delegate{PrepareVisibility();connected=true;Reflect.Set(form,"qSolved",true);Reflect.Set(form,"m_TRun",false);ClearScratch();RefreshFromServer();ConfigureExplorer();if(defaultCamera==null)defaultCamera=api.Parse(api.Json(CaptureCamera()));RestoreCamera();LoadGrips();if(renderer!=null){renderer.HoldUpdates=false;renderer.AssertReady();}message.Text="Ready · full model verified · drag to rotate; Shift+drag for 4D";NativeDiagnostics.Write("Live native bridge passed and state synchronized");clockTimer.Start();},true);
 }
 Dictionary<string,object> Geometry(){
  var s=Reflect.Get(puz,"Str");if(Convert.ToBoolean(Reflect.Get(s,"QSimplified"))||Convert.ToInt32(Reflect.Get(s,"NStickers"))!=259800)throw new InvalidOperationException("Open 600-cell-Full before connecting. Simplified geometry cannot pass the full bridge.");
  var faces=new List<object>();foreach(object f in (Array)Reflect.Get(s,"Faces"))faces.Add(LocalApi.D("id",Reflect.Get(f,"Id"),"pole",Reflect.Get(f,"Pole"),"first",Reflect.Get(f,"FirstSticker"),"count",Reflect.Get(Reflect.Get(f,"Base"),"NStickers")));
  var bases=new List<object>();foreach(object a in (Array)Reflect.Get(s,"BaseAxes")){var twists=new List<object>();foreach(object t in (Array)Reflect.Get(a,"Twists"))twists.Add(LocalApi.D("order",Reflect.Get(t,"Order"),"maps",Reflect.Get(t,"Map")));bases.Add(LocalApi.D("id",Reflect.Get(a,"Id"),"cuts",Reflect.Get(a,"Cut"),"twists",twists));}
  var axes=new List<object>();foreach(object a in (Array)Reflect.Get(s,"Axes"))axes.Add(LocalApi.D("id",Reflect.Get(a,"Id"),"base",Reflect.Get(Reflect.Get(a,"Base"),"Id"),"dir",Reflect.Get(a,"Dir"),"fixedMask",Reflect.Get(Reflect.Get(a,"Base"),"FixedMask"),"layers",Reflect.Get(a,"Layers")));
  string sha;using(var h=SHA256.Create())using(var fs=File.OpenRead(exe))sha=BitConverter.ToString(h.ComputeHash(fs)).Replace("-","").ToLowerInvariant();
  return LocalApi.D("format","MPUlt-native600-v1","n",259800,"faces",faces,"axes",axes,"bases",bases,"executable_sha256",sha);
 }
 static int[] ToInts(object x){var a=LocalApi.Array(x);int[] n=new int[a.Length];for(int i=0;i<n.Length;i++)n[i]=Convert.ToInt32(a[i]);return n;}
 static int[][] ToRows(object x){return Array.ConvertAll(LocalApi.Array(x),ToInts);}
 void ConfigureExplorer(){
  if(structureData==null)return;
  explorer.Configure(Convert.ToString(structureData["model_id"]),ToRows(structureData["adjacency"]),ToRows(structureData["vertex_incidence"]),canonicalPalette);
  try{auxiliaryGeometry=NativeCellGeometry.FromStructure(structureData,canonicalPalette);auxiliaries.Configure(auxiliaryGeometry);auxiliaryFailure=null;RefreshAuxiliaryState();}
  catch(Exception error){AuxiliaryError(error);}
 }
 void AuxiliaryError(Exception error){auxiliaryFailure=error.Message;NativeDiagnostics.Write("Auxiliary views unavailable",error);if(auxiliaries!=null)auxiliaries.ShowError(error.Message);}
 void RefreshAuxiliaryState(){
  if(auxiliaries==null||auxiliaryGeometry==null||state==null)return;
  try{auxiliaries.UpdateStatus(NativeCellStatus.FromState(state));int color=(int)cell.Value+1;object raw;if(state.TryGetValue("focus",out raw)&&raw!=null)color=Convert.ToInt32(LocalApi.AsDict(raw)["color"]);if(pendingFocusColor.HasValue)color=pendingFocusColor.Value;changingCellFocus=true;try{cell.Value=color-1;auxiliaries.SetSelectedColor(color);if(color!=synchronizedFocusColor){explorer.NavigateColor(color);synchronizedFocusColor=color;}}finally{changingCellFocus=false;}}
  catch(Exception error){AuxiliaryError(error);}
 }
 bool SynchronizeCellOutline(){
  if(renderer==null||state==null)return false;int nativeCell=-1;object raw;
  if(state.TryGetValue("focus",out raw)&&raw!=null)nativeCell=labToNativeCell[Convert.ToInt32(LocalApi.AsDict(raw)["color"])-1];
  if(renderer.Subset.FocusedCell==nativeCell)return false;
  try{renderer.Subset.FocusedCell=nativeCell;return true;}catch(Exception error){AuxiliaryError(error);return false;}
 }
 void SelectAuxiliaryColor(int color,bool center){
  if(!connected||color<1||color>600)return;
  changingCellFocus=true;try{cell.Value=color-1;auxiliaries.SetSelectedColor(color);}finally{changingCellFocus=false;}
  if(auxiliaryGeometry!=null){explorer.NavigateColor(color);synchronizedFocusColor=color;}
  if(center&&!busy){Center();LoadGrips();}QueueCellFocus(color);
 }
 void OpenAuxiliary(bool local){if(auxiliaries==null)return;if(local)auxiliaries.OpenLocal();else auxiliaries.OpenGlobal();if(connected&&state!=null&&state["focus"]==null)QueueCellFocus((int)cell.Value+1);}
 void QueueCellFocus(int color){pendingFocusColor=color;if(focusDispatchQueued||closing||!form.IsHandleCreated)return;focusDispatchQueued=true;form.BeginInvoke((Action)FlushCellFocus);}
 void FlushCellFocus(){
  focusDispatchQueued=false;if(closing||!connected||busy||!pendingFocusColor.HasValue)return;int color=pendingFocusColor.Value;pendingFocusColor=null;
  if(state!=null&&state["focus"]!=null&&Convert.ToInt32(LocalApi.AsDict(state["focus"])["color"])==color)return;
  Run(delegate{var result=api.Post("focus",LocalApi.D("color",color,"native_since",cachedSnapshot==null?null:cachedSnapshot.Revision));ReadOperationSnapshot(result);},delegate{RefreshFromServer();message.Text="Focused C"+color+" · cyan cell outline · Center main changes the camera";});
 }
 void ReadOnlyRequest(Func<Dictionary<string,object>> read,Action<Dictionary<string,object>> finish,Action<string> fail){
  Task.Factory.StartNew(read).ContinueWith(task=>{
   Exception error=task.IsFaulted?task.Exception.GetBaseException():null;
   if(closing||form.IsDisposed||!form.IsHandleCreated)return;
   try{form.BeginInvoke((Action)delegate{if(closing||form.IsDisposed)return;try{if(error!=null)fail(error.Message);else finish(task.Result);}catch(Exception e){fail(e.Message);}});}
   catch(ObjectDisposedException){}catch(InvalidOperationException){}
  });
 }
 object ComposeExplorerRules(string predicate,string composition){
  if(composition=="replace")return new object[]{LocalApi.D("expr",predicate,"style","solid")};
  if(composition!="intersect"&&composition!="union")throw new InvalidOperationException("Unknown filter composition.");
  var next=new List<object>();var existing=LocalApi.Array(LocalApi.AsDict(state["prefs"])["rules"]);
  foreach(var item in existing){
   var rule=LocalApi.AsDict(item);string expression=Convert.ToString(rule["expr"]),style=Convert.ToString(rule["style"]);
   if(composition=="intersect")expression="("+expression+") & ("+predicate+")";
   else if(style=="hide")expression="("+expression+") & !("+predicate+")";
   next.Add(LocalApi.D("expr",expression,"style",style));
  }
  if(composition=="union")next.Add(LocalApi.D("expr",predicate,"style","solid"));
  return next.ToArray();
 }
 void PreviewExplorer(NativeStructureFilterRequest request){
  if(!connected||busy)return;
  object rules;try{rules=ComposeExplorerRules(request.Predicate,request.Composition);}catch(Exception e){explorer.ShowError(e.Message);return;}
  long id=request.Id;explorerRequest=id;string initialState=stateHash,initialRules=api.Json(LocalApi.AsDict(state["prefs"])["rules"]);
  ReadOnlyRequest(delegate{return api.Post("filter-preview",LocalApi.D("rules",rules));},delegate(Dictionary<string,object> result){
   CompleteExplorerPreview(id,initialState,initialRules,rules,result);
  },delegate(string error){if(id==explorerRequest)explorer.ShowError(error);});
 }
 void CompleteExplorerPreview(long id,string initialState,string initialRules,object rules,Dictionary<string,object> result){
  // An earlier response must not clear a newer request's reviewed counts.
  if(id!=explorerRequest)return;
  if(stateHash!=initialState||api.Json(LocalApi.AsDict(state["prefs"])["rules"])!=initialRules){explorer.InvalidatePreview();return;}
  explorerRules=rules;explorerPreviewState=initialState;explorerPreviewRules=initialRules;explorerContext=Convert.ToString(result["context_hash"]);
  explorer.ShowPreview(id,Convert.ToInt32(result["pieces"]),Convert.ToInt32(result["stickers"]),"Add "+result["added_pieces"]+" / remove "+result["removed_pieces"]+" pieces. Whole pieces may touch several layers. Result: "+api.Json(rules));
 }
 void ApplyExplorer(NativeStructureFilterRequest request){
  if(!connected||busy||request.Id!=explorerRequest||explorerRules==null)return;
  if(stateHash!=explorerPreviewState||api.Json(LocalApi.AsDict(state["prefs"])["rules"])!=explorerPreviewRules){explorer.InvalidatePreview();explorer.ShowError("State or filter changed. Preview again.");return;}
  object rules=explorerRules;string context=explorerContext;
  Run(delegate{api.Post("filter-apply",LocalApi.D("rules",rules,"context_hash",context));},delegate{RefreshFromServer();explorer.InvalidatePreview();});
 }
 void PreviewFilterEditor(){
  if(!connected||busy)return;object rules;string draft=filter.Text;
  try{rules=ReadFilterRules();}catch(Exception e){message.Text="Invalid filter: "+e.Message;return;}
  ReadOnlyRequest(delegate{return api.Post("filter-preview",LocalApi.D("rules",rules));},delegate(Dictionary<string,object> result){
   if(filter.Text==draft)message.Text=result["pieces"]+" pieces / "+result["stickers"]+" stickers. Review the expression, then choose Apply exact filter.";
  },delegate(string error){if(filter.Text==draft)message.Text="Invalid filter: "+error;});
 }
 void PrepareVisibility(){
  SyncStickerDimensions();
  // Clone renderer-only PMesh records. Never mutate PuzzleStructure's base meshes.
  // Zero geometry counts on hidden clones avoid the original projection work.
  if(shownBases!=null&&hiddenBases!=null){
   // Reconnect must reuse the original geometry, never clone an already hidden
   // mesh whose vertex/face/edge counts have intentionally been zeroed.
   Reflect.Set(cube,"ShowRank",1);for(int i=0;i<previousStyles.Length;i++)previousStyles[i]=255;return;
  }
  var shown=new Dictionary<object,object>();var hidden=new Dictionary<object,object>();baseForSlot=new object[nativeStickers.Length];shownBases=new object[nativeStickers.Length];hiddenBases=new object[nativeStickers.Length];
  for(int i=0;i<nativeStickers.Length;i++){object st=stickerAccess.Slots[i],b=stickerAccess.GetBase(st);baseForSlot[i]=b;if(!shown.ContainsKey(b)){var v=Reflect.Clone(b);Reflect.Set(v,"Rank",0);shown[b]=v;var h=Reflect.Clone(b);Reflect.Set(h,"Rank",1);Reflect.Set(h,"MinBDim",1);Reflect.Set(h,"NV",0);Reflect.Set(h,"NF",0);Reflect.Set(h,"NE",0);hidden[b]=h;}shownBases[i]=shown[b];hiddenBases[i]=hidden[b];}
  Reflect.Set(cube,"ShowRank",1);previousStyles=new byte[nativeStickers.Length];for(int i=0;i<previousStyles.Length;i++)previousStyles[i]=255;
 }
 void Run(Action work,Action finish,bool allowDisconnected=false){
  if(busy||closing||(!connected&&!allowDisconnected))return;
  if(!layoutOnly&&Convert.ToInt32(Reflect.Get(form,"m_status"))==2)return;
  busy=true;viewport.Enabled=false;frameMenu.Enabled=false;foreach(var c in guarded)c.Enabled=false;foreach(var item in stateTools)item.Enabled=false;orbit.Enabled=false;cell.Enabled=false;explorer.SetBusy(true);if(auxiliaries!=null)auxiliaries.SetBusy(true);message.Text="Working… previous committed state remains visible";operationSnapshot=null;
  NativeSnapshot ready=null;
  Task.Factory.StartNew(delegate{
   try{work();}catch{if(profile!=null)try{ready=FetchSnapshot(true);}catch(Exception refreshError){NativeDiagnostics.Write("Could not refresh after failed command",refreshError);}throw;}
   if(profile!=null)ready=operationSnapshot??FetchSnapshot();
  }).ContinueWith(task=>{
   // Closing may destroy the handle after the worker finishes. Observe the
   // exception before checking the form, and never BeginInvoke a disposed form.
   Exception fault=task.IsFaulted?task.Exception.GetBaseException():null;
   if(closing||form.IsDisposed||!form.IsHandleCreated){if(fault!=null)NativeDiagnostics.Write("Worker finished after close",fault);return;}
   try{form.BeginInvoke((Action)delegate{
    if(closing||form.IsDisposed)return;
    preparedSnapshot=ready;
    try{
     if(fault!=null){
      NativeDiagnostics.Write("Native operation failed",fault);
      if(connected)RefreshFromServer();
      else MessageBox.Show(form,fault.Message+"\n\nInput remains disabled. See diagnostics/native-debug.log. File > Reconnect native bridge retries without resetting your session.","Native connection failed");
      message.Text=fault.Message;
     }else finish();
    }catch(Exception e){NativeDiagnostics.Write("Native UI completion failed",e);message.Text=e.Message;connected=false;clockTimer.Stop();MessageBox.Show(form,e.ToString(),"Native host error");}
    finally{preparedSnapshot=null;operationSnapshot=null;busy=false;viewport.Enabled=connected;frameMenu.Enabled=connected;foreach(var c in guarded)c.Enabled=connected;foreach(var item in stateTools)item.Enabled=connected;orbit.Enabled=connected;cell.Enabled=connected;explorer.SetBusy(!connected);if(auxiliaries!=null)auxiliaries.SetBusy(!connected);if(renderer!=null&&connected)renderer.HoldUpdates=false;if(pendingFocusColor.HasValue)QueueCellFocus(pendingFocusColor.Value);}
   });}catch(ObjectDisposedException){NativeDiagnostics.Write("Window disposed while dispatching completion");}
   catch(InvalidOperationException e){NativeDiagnostics.Write("Window handle unavailable while dispatching completion",e);}
  });
 }
 NativeSnapshot FetchSnapshot(bool full=false){
  string path="native/snapshot?protocol=2";if(!full&&cachedSnapshot!=null&&cachedSnapshot.Revision!=null)path+="&since="+Uri.EscapeDataString(cachedSnapshot.Revision);
  try{return NativeSnapshot.Read(api.Get(path),Convert.ToString(profile["profile_sha256"]),full?null:cachedSnapshot);}
  catch(InvalidDataException){if(full)throw;return NativeSnapshot.Read(api.Get("native/snapshot?protocol=2"),Convert.ToString(profile["profile_sha256"]),null);}
 }
 void ReadOperationSnapshot(Dictionary<string,object> result){
  object reply;if(!result.TryGetValue("native_snapshot",out reply))return;
  try{operationSnapshot=NativeSnapshot.Read(LocalApi.AsDict(reply),Convert.ToString(profile["profile_sha256"]),cachedSnapshot);}
  catch(InvalidDataException){operationSnapshot=FetchSnapshot(true);}
 }
 void RefreshFromServer(){if(!connected)return;var ready=preparedSnapshot??FetchSnapshot();preparedSnapshot=null;lock(viewport){syncing=true;try{
  var nextState=ready.State;byte[] bytes=ready.Colors,style=ready.Styles;bool initial=cachedSnapshot==null||ready.IsFull,wasNative=nativeColorsDirty;
  string oldStateHash=stateHash;state=nextState;stateHash=Convert.ToString(state["state_hash"]);var fld=(short[])Reflect.Get(puz,"Field");Buffer.BlockCopy(bytes,0,fld,0,bytes.Length);ClearScratch();Reflect.Call(form,"InitStatus");
  if(style.Length!=fld.Length)throw new InvalidDataException("Native style response length");double fs=Convert.ToDouble(Reflect.Get(cube,"FShr")),ss=Convert.ToDouble(Reflect.Get(cube,"SShr"));
  int[] changes=initial?null:ready.ChangedSlots;int changedCount=changes==null?style.Length:changes.Length;
  for(int j=0;j<changedCount;j++){int i=changes==null?j:changes[j];if((style[i]!=0)!=(previousStyles[i]!=0)||previousStyles[i]==255){var st=stickerAccess.Slots[i];stickerAccess.SetBase(st,style[i]==0?hiddenBases[i]:shownBases[i]);if(style[i]!=0)stickerAccess.SetCoord(st,fs,ss);}previousStyles[i]=style[i];}
  stickerAccess.ApplyColors(cube,fld,style,changes,wasNative);nativeColorsDirty=false;
  if(canonicalPalette==null){canonicalPalette=new Color[600];var nativePalette=(int[])Reflect.Get(cube,"Colors");for(int c=0;c<600;c++)canonicalPalette[c]=Color.FromArgb(unchecked((int)0xFF000000)|nativePalette[labToNativeCell[c]]);}
  if(renderer!=null&&(initial||ready.VisibilityChanged))renderer.Subset.Update(style);
  if(picking!=null&&(initial||ready.VisibilityChanged||ready.InteractionChanged))picking.Update(style,ready.Interactive);
  var prefs=LocalApi.AsDict(state["prefs"]);bool frameHidden=ReadFrameMode(prefs);bool frameChanged=renderer!=null&&renderer.Subset.HideFramework!=frameHidden;if(renderer!=null)renderer.Subset.HideFramework=frameHidden;SyncFrameControls(frameHidden);
  bool focusChanged=SynchronizeCellOutline();if(initial||changedCount>0||frameChanged||wasNative||focusChanged)Reflect.Call(viewport,"SetSceneChanged");
  cachedSnapshot=ready;orbit.SelectedIndex=Convert.ToInt32(prefs["orbit"]);pin.Checked=Convert.ToBoolean(prefs["pin_safety"]);RefreshToolState(prefs,style);
  SynchronizeInspection();RefreshAuxiliaryState();if(oldStateHash!=stateHash){explorer.InvalidatePreview();UpdateBufferText();}
  progress.BeginUpdate();progress.Items.Clear();foreach(object o in LocalApi.Array(state["progress"])){var r=LocalApi.AsDict(o);var it=new ListViewItem("O"+Convert.ToInt32(r["orbit"]).ToString("00")){Tag=r["orbit"]};it.SubItems.Add(r["solved"]+" / "+r["pieces"]);it.SubItems.Add(r["position_wrong"].ToString());it.SubItems.Add(r["orientation_wrong"].ToString());progress.Items.Add(it);}progress.EndUpdate();
  checkpoint.Items.Clear();foreach(var c in LocalApi.Array(state["checkpoints"]))checkpoint.Items.Add(LocalApi.AsDict(c)["name"]);if(checkpoint.Items.Count>0)checkpoint.SelectedIndex=0;
  if(state["pending"]!=null){var c=LocalApi.AsDict(state["pending"]);certificate.Text=c["note"]+"\r\n"+c["primitive_count"]+" primitive turns; "+c["star_count"]+" stars\r\n\r\nComplete support:\r\n"+api.Json(c["support"])+"\r\n\r\nCertificate "+c["certificate_id"];}
  else certificate.Text="No operation preview. Manual native twists are journaled immediately; assisted operations require Apply.";
  message.Text=state["moving_solved"]+" / "+state["moving_total"]+" moving pieces solved · "+state["primitives"]+" primitive turns · Head "+state["head"];
 }finally{syncing=false;}}}
 void ClearScratch(){foreach(string n in new[]{"Ptr","LSeq","LShuffle","NTwists"})Reflect.Set(puz,n,0);}
 void CaptureNativeTurns(){
  if(!connected||busy||syncing)return;
  int ptr=Convert.ToInt32(Reflect.Get(puz,"Ptr"));if(ptr==0){if(nativeColorsDirty&&cachedSnapshot!=null)stickerAccess.ApplyColors(cube,(short[])Reflect.Get(puz,"Field"),cachedSnapshot.Styles,new int[0],true);nativeColorsDirty=false;if(renderer!=null)renderer.HoldUpdates=false;return;}
  var seq=(long[])Reflect.Get(puz,"Seq");var tokens=new List<string>();
  for(int i=0;i<ptr;i++){long c=seq[i];if(c<0)continue;tokens.Add((c&65535)+":"+((c>>16)&65535)+":"+((c>>32)&65535)+":"+((c>>48)&65535));}
  ClearScratch();string pre=stateHash;
  if(tokens.Count>0)Run(delegate{var result=api.Post("native/turn",LocalApi.D("pre_state",pre,"tokens",tokens,"native_since",cachedSnapshot==null?null:cachedSnapshot.Revision));ReadOperationSnapshot(result);},RefreshFromServer);
  else Run(delegate{},RefreshFromServer);
 }
 void InspectNative(int hit,string gesture,string pre){
  Dictionary<string,object> result=null;
  Run(delegate{result=api.Post("native/inspect",LocalApi.D("native_sticker",hit,"gesture",gesture,"pre_state",pre,"native_since",cachedSnapshot==null?null:cachedSnapshot.Revision));ReadOperationSnapshot(result);},
      delegate{RefreshFromServer();DisplayInspection(result);});
 }
 void ClearInspection(){Run(delegate{api.Post("inspection/clear",empty);},delegate{RefreshFromServer();pieceText.Text="Inspection cleared. Shift+left click shows home Cell Centers; Shift+right click locates the required piece.";});}
 void Center(){if(!connected||busy)return;var s=Reflect.Get(puz,"Str");var faces=(Array)Reflect.Get(s,"Faces");var pole=Reflect.Get(faces.GetValue(labToNativeCell[(int)cell.Value]),"Pole");var scene=Reflect.Property(viewport,"Scene");var camera=Reflect.Property(scene,"Camera");Reflect.Call(camera,"Recenter",pole);Reflect.Call(viewport,"SetSceneChanged");}
 void LoadGrips(){if(!connected)return;var g=api.Get("grips?cell="+cell.Value);var arr=LocalApi.Array(g["axes"]);gripAxes=new Dictionary<string,object>[arr.Length];while(gripPanel.Controls.Count>0){Control old=gripPanel.Controls[0];guarded.Remove(old);gripPanel.Controls.RemoveAt(0);old.Dispose();}for(int i=0;i<arr.Length;i++){int j=i;gripAxes[i]=LocalApi.AsDict(arr[i]);var b=Button(gripAxes[i]["label"].ToString(),delegate{Grip(j,false);});b.MinimumSize=new Size(37,25);b.AutoSize=false;b.Width=38;b.MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Right)Grip(j,true);};gripPanel.Controls.Add(b);}}
 void Grip(int id,bool inverse){if(!connected||busy)return;if(gripAxes==null||Convert.ToInt32(gripAxes[0]["cell"])!=(int)cell.Value)LoadGrips();if(id>=gripAxes.Length)return;var g=inverse?LocalApi.AsDict(gripAxes[id]["inverse"]):gripAxes[id];int c=(int)cell.Value;var recipe=new object[]{LocalApi.D("kind","word","moves",g["word"])};Run(delegate{api.Post("preview",LocalApi.D("recipe",recipe,"note","Native grip C"+c));},RefreshFromServer);}
 void Command(string c){if(connected)Run(delegate{api.Post(c,empty);},RefreshFromServer);}
 void Scramble(){Run(delegate{api.Post("scramble",LocalApi.D("count",1000,"apply",true));},RefreshFromServer);}
 void ResetPuzzle(){if(!connected||busy)return;object camera=CaptureCamera();Dictionary<string,object> result=null;Run(delegate{result=api.Post("reset",LocalApi.D("camera",camera));},delegate{RefreshFromServer();message.Text="Reset to solved. Previous state saved as checkpoint: "+Convert.ToString(result["reset_checkpoint"]);});}
 void Commit(){if(!connected||busy||state==null||state["pending"]==null)return;string token=Convert.ToString(LocalApi.AsDict(state["pending"])["token"]);Run(delegate{api.Post("commit",LocalApi.D("token",token));},RefreshFromServer);}
 void Cancel(){if(busy){Task.Factory.StartNew(delegate{try{api.Post("stop-job",empty);}catch{}});return;}Command("cancel");}
 void Suggest(){bool automatic=autoTarget.Checked;int destination=(int)target.Value,o;if(!TryDestinationOrbit(automatic,destination,out o))return;int n=(int)batch.Value;object p=automatic?null:(object)destination;Dictionary<string,object> result=null;Run(delegate{result=api.Post("suggest",LocalApi.D("orbit",o,"batch",n,"target",p));},delegate{RefreshFromServer();if(result.ContainsKey("complete")&&Convert.ToBoolean(result["complete"]))message.Text=Convert.ToString(result["note"]);});}
 object ReadFilterRules(){string expr=filter.Text.Trim();return expr.StartsWith("[",StringComparison.Ordinal)?api.Parse(expr):new object[]{LocalApi.D("expr",expr,"style","solid")};}
 void ApplyFilter(){object rules;try{rules=ReadFilterRules();}catch(Exception e){message.Text="Invalid filter: "+e.Message;return;}Run(delegate{api.Post("prefs",LocalApi.D("rules",rules,"pin_safety",false));},RefreshFromServer);}
 static bool ReadFrameMode(Dictionary<string,object> prefs){object value,setting;if(prefs!=null&&prefs.TryGetValue("view",out value)){var view=value as Dictionary<string,object>;if(view!=null&&view.TryGetValue("native_hide_frame",out setting)&&setting is bool)return (bool)setting;}return true;}
 void SyncFrameControls(bool hide){updatingFrameControls=true;try{hideFrame.Checked=hide;frameMenu.Checked=hide;}finally{updatingFrameControls=false;}}
 void ChangeFrameMode(bool hide){
  if(updatingFrameControls||syncing)return;
  var prefs=state==null?null:LocalApi.AsDict(state["prefs"]);
  if(!connected||busy||closing){SyncFrameControls(ReadFrameMode(prefs));return;}
  object oldView;var view=new Dictionary<string,object>();
  if(prefs.TryGetValue("view",out oldView)){var values=oldView as Dictionary<string,object>;if(values!=null)foreach(var entry in values)view[entry.Key]=entry.Value;}
  view["native_hide_frame"]=hide;Run(delegate{api.Post("prefs",LocalApi.D("view",view));},RefreshFromServer);
 }
 void ExcludeOrbit(int index){try{var rules=LocalApi.Array(ReadFilterRules());var next=new List<object>();foreach(var item in rules){var r=LocalApi.AsDict(item);next.Add(LocalApi.D("expr","("+Convert.ToString(r["expr"])+") & !O"+index.ToString("00"),"style",r["style"]));}filter.Text=next.Count==1&&Convert.ToString(LocalApi.AsDict(next[0])["style"])=="solid"?Convert.ToString(LocalApi.AsDict(next[0])["expr"]):api.Json(next);ApplyFilter();}catch(Exception e){message.Text="Invalid filter: "+e.Message;}}
 void LoadPreset(){
  string n=presets.Text;int c=explorer.SelectedColor,l=explorer.SelectedLayer;
  string expression=n=="Selected color"?"color(C"+c+")":n=="Selected current cell"?"cell(C"+c+")":n=="Adjacent colors"?"adjacent(C"+c+")":n=="Selected center layer"?"layer(C"+c+",L"+l+")":n=="Selected home layer"?"home_layer(C"+c+",L"+l+")":null;
  if(expression!=null){filter.Text=expression;PreviewFilterEditor();return;}
  filter.Text=n=="Unsolved orbit"?"active & unsolved":n=="Orientation only"?"active & orientation_wrong":n=="Work cell"?"current(C"+((int)cell.Value).ToString("000")+")":n=="All pieces"?"everything":"active";ApplyFilter();
 }
 void Protect(){if(state==null)return;var prefs=LocalApi.AsDict(state["prefs"]);var list=new List<int>(ToInts(prefs["protected"]));int o=orbit.SelectedIndex;if(list.Contains(o))list.Remove(o);else list.Add(o);Run(delegate{api.Post("prefs",LocalApi.D("protected",list));},RefreshFromServer);}
 object CaptureCamera(){SyncStickerDimensions();var camera=Reflect.Property(Reflect.Property(viewport,"Scene"),"Camera");var matrix=(double[,])Reflect.Get(Reflect.Get(camera,"Trans"),"M");var data=new double[16];for(int i=0;i<4;i++)for(int j=0;j<4;j++)data[4*i+j]=matrix[i,j];return LocalApi.D("format","C600-native-camera-v1","matrix",data,"radius",Reflect.Get(camera,"R"),"angle",Reflect.Property(camera,"Angle"),"cell",(int)cell.Value,"face_shrink",Reflect.Get(cube,"FShr"),"sticker_shrink",Reflect.Get(cube,"SShr"));}
 void ResetView(){if(!connected||busy||defaultCamera==null)return;ApplyCamera(LocalApi.AsDict(defaultCamera));LoadGrips();message.Text="View reset. Puzzle state, filter and journal are unchanged.";}
 void RestoreCamera(){if(state==null)return;var prefs=LocalApi.AsDict(state["prefs"]);if(!prefs.ContainsKey("camera")||prefs["camera"]==null)return;var data=prefs["camera"] as Dictionary<string,object>;if(data==null||!data.ContainsKey("format")||Convert.ToString(data["format"])!="C600-native-camera-v1")return;ApplyCamera(data);}
 void ApplyCamera(Dictionary<string,object> data){
  var flat=LocalApi.Array(data["matrix"]);if(flat.Length!=16)throw new InvalidDataException("Invalid native camera");var matrix=new double[4,4];for(int i=0;i<16;i++){double v=Convert.ToDouble(flat[i]);if(Double.IsNaN(v)||Double.IsInfinity(v))throw new InvalidDataException("Nonfinite camera");matrix[i/4,i%4]=v;}
  for(int i=0;i<4;i++)for(int j=0;j<4;j++){double dot=0;for(int k=0;k<4;k++)dot+=matrix[i,k]*matrix[j,k];if(Math.Abs(dot-(i==j?1:0))>1e-5)throw new InvalidDataException("Camera is not orthogonal");}
  double r=Convert.ToDouble(data["radius"]),angle=Convert.ToDouble(data["angle"]),fs=Convert.ToDouble(data["face_shrink"]),ss=Convert.ToDouble(data["sticker_shrink"]);var camera=Reflect.Property(Reflect.Property(viewport,"Scene"),"Camera");double r0=Convert.ToDouble(Reflect.Get(camera,"R0"));
  if(!(r>=-0.91*r0&&r<=1.01*r0&&angle>0&&angle<=(double)(float)Math.PI&&fs>=0&&fs<=1&&ss>=0&&ss<=1))throw new InvalidDataException("Native camera range invalid");
  // The original setters can recenter the camera. Synchronize their controls
  // first, then restore the saved camera matrix/radius/angle below.
  SetDisplaySlider("trk_faceShrink",fs);SetDisplaySlider("trk_StickerSize",ss);SetDisplaySlider("trk_ViewAngle",angle/Math.PI);
  SyncStickerDimensions();fs=Convert.ToDouble(Reflect.Get(cube,"FShr"));ss=Convert.ToDouble(Reflect.Get(cube,"SShr"));
  lock(viewport){Reflect.Set(Reflect.Get(camera,"Trans"),"M",matrix);Reflect.Set(camera,"R",r);Reflect.SetProperty(camera,"Angle",angle);Reflect.Set(cube,"FShr",fs);Reflect.Set(cube,"SShr",ss);Reflect.Call(cube,"SetStickerSize",fs,ss);Reflect.Call(camera,"SetChanged");Reflect.Call(viewport,"SetSceneChanged");}
  int restoredCell=Math.Max(0,Math.Min(599,Convert.ToInt32(data["cell"])));object focused;
  if(state!=null&&state.TryGetValue("focus",out focused)&&focused!=null)restoredCell=Convert.ToInt32(LocalApi.AsDict(focused)["color"])-1;
  changingCellFocus=true;try{cell.Value=restoredCell;}finally{changingCellFocus=false;}
 }
 void SaveCheckpoint(){if(!connected||busy)return;string name="Native "+DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff");object camera=CaptureCamera();Run(delegate{api.Post("prefs",LocalApi.D("camera",camera));api.Post("checkpoint",LocalApi.D("name",name));},RefreshFromServer);}
 void Restore(){if(checkpoint.SelectedItem==null||!connected)return;string name=checkpoint.SelectedItem.ToString();if(MessageBox.Show(form,"Restore "+name+"? Other journal branches remain.","Restore checkpoint",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;RestoreCheckpoint(name);}
 void RestoreCheckpoint(string name){Run(delegate{api.Post("restore",LocalApi.D("name",name));},delegate{displayedRules=null;RefreshFromServer();RestoreCamera();LoadGrips();});}
 bool TryDestinationOrbit(bool automatic,int destination,out int resolved){
  resolved=orbit.SelectedIndex;object value;
  if(automatic||state==null||!state.TryGetValue("inspection",out value)||value==null)return true;
  var inspection=LocalApi.AsDict(value);
  if(Convert.ToString(inspection["kind"])!="required-piece"||Convert.ToInt32(inspection["target_position"])!=destination)return true;
  if(!Convert.ToBoolean(inspection["buffer_analysis_available"])){message.Text=Convert.ToString(inspection["buffer_analysis_reason"]);return false;}
  resolved=Convert.ToInt32(inspection["target_orbit"]);return true;
 }
 void Analyze(){int p=(int)target.Value,o;bool automatic=autoTarget.Checked;if(!TryDestinationOrbit(automatic,p,out o))return;Dictionary<string,object> result=null;Run(delegate{result=api.Post("buffers",LocalApi.D("orbit",o,"target",automatic?null:(object)p));},delegate{RefreshFromServer();DisplayBuffer(result);});}
 void DisplayBuffer(Dictionary<string,object> result){
  bufferAnalysis=result;bufferStateHash=stateHash;inspectedOrbit=Convert.ToInt32(result["orbit"]);
  analyzedFrames.Clear();node.Items.Clear();
  foreach(var x in LocalApi.Array(result["candidates"])){var frame=LocalApi.AsDict(x);analyzedFrames.Add(frame);node.Items.Add("Frame "+frame["node"]+" | depth "+frame["depth"]+" | "+frame["length"]+" turns");}
  if(node.Items.Count>0)node.SelectedIndex=0;UpdateBufferText();
 }
 void UpdateBufferText(){
  if(bufferAnalysis==null)return;var r=bufferAnalysis;var a=LocalApi.AsDict(r["A"]);var b=LocalApi.AsDict(r["B"]);var occupant=LocalApi.AsDict(r["target_piece"]);
  var text=new StringBuilder();text.AppendLine("Destination P"+r["target"]+" · Orbit O"+inspectedOrbit.ToString("00"));
  text.AppendLine("Required identity: P"+r["target"]);
  if(r.ContainsKey("required_position"))text.AppendLine("Required identity is now at P"+r["required_position"]);
  text.AppendLine("Destination currently contains P"+occupant["piece"]);
  text.AppendLine();text.AppendLine("Buffer A: P"+a["position"]+" contains P"+a["piece"]);text.AppendLine("Buffer B: P"+b["position"]+" contains P"+b["piece"]);
  text.AppendLine("Reference destination: P"+r["third"]);text.AppendLine("Reachable frames: "+r["reachable"]+" / "+r["expected"]);
  text.AppendLine("All-frame setup depth: mean "+Convert.ToDouble(r["mean_depth"]).ToString("F2")+", max "+r["max_depth"]);
  if(node.SelectedIndex>=0&&node.SelectedIndex<analyzedFrames.Count){var f=analyzedFrames[node.SelectedIndex];text.AppendLine();text.AppendLine("Selected frame "+f["node"]+": "+f["length"]+" primitive turns");text.AppendLine("Setup depth: "+f["depth"]);text.AppendLine("Setup word: "+api.Json(f["setup"]));}
  if(r.ContainsKey("seed_collateral"))text.AppendLine("Seed collateral orbits: "+api.Json(r["seed_collateral"]));
  if(r.ContainsKey("protected_orbits"))text.AppendLine("Protected orbits: "+api.Json(r["protected_orbits"]));
  text.AppendLine();text.AppendLine("Preview computes complete support and rechecks protection before Apply. Buffer positions are fixed; their occupants move.");
  if(bufferStateHash!=stateHash)text.AppendLine("State changed since analysis. Analyze again before previewing a frame.");
  bufferText.Text=text.ToString();
 }
 void PreviewStar(int sign){
  if(node.SelectedIndex<0||node.SelectedIndex>=analyzedFrames.Count||inspectedOrbit<0){message.Text="Analyze a nonbuffer destination and select a frame first.";return;}
  if(bufferStateHash!=stateHash){message.Text="State changed. Analyze the destination again.";return;}
  int nd=Convert.ToInt32(analyzedFrames[node.SelectedIndex]["node"]),o=inspectedOrbit;
  Run(delegate{api.Post("preview",LocalApi.D("recipe",new object[]{LocalApi.D("kind","star","orbit",o,"node",nd,"sign",sign)},"note","Native manually selected star"));},RefreshFromServer);
 }
 void PreviewWord(){var parts=word.Text.Split(new[]{' ','\r','\n','\t',','},StringSplitOptions.RemoveEmptyEntries);var moves=new List<int>();try{foreach(string raw in parts){string t=raw.Trim();bool inverse=t.EndsWith("'");if(inverse)t=t.Substring(0,t.Length-1);int v;if(t.StartsWith("H",StringComparison.OrdinalIgnoreCase)||t.StartsWith("T",StringComparison.OrdinalIgnoreCase)){int c=Int32.Parse(t.Substring(1));if(c<0||c>599)throw new FormatException("Cell outside 0..599");v=2*c+(Char.ToUpperInvariant(t[0])=='H'?1:2);}else v=Int32.Parse(t);moves.Add(inverse?-v:v);}}catch(Exception e){MessageBox.Show(form,e.Message,"Invalid word");return;}Run(delegate{api.Post("preview",LocalApi.D("recipe",new object[]{LocalApi.D("kind","word","moves",moves)},"note","Native manually entered word"));},RefreshFromServer);}
 void History(){Dictionary<string,object> r=null;Run(delegate{r=api.Get("history");},delegate{RefreshFromServer();history.Items.Clear();foreach(var x in LocalApi.Array(r["rows"])){var row=LocalApi.AsDict(x);var it=new ListViewItem(row["id"].ToString()){Tag=row["id"]};it.SubItems.Add(Convert.ToString(row["parent"]));it.SubItems.Add(Convert.ToString(row["note"]));history.Items.Add(it);}});}
 void Checkout(){if(history.SelectedItems.Count==0)return;int id=Convert.ToInt32(history.SelectedItems[0].Tag);Run(delegate{api.Post("checkout",LocalApi.D("head",id));},RefreshFromServer);}
 void RefreshToolState(Dictionary<string,object> prefs,byte[] styles){
  string encoded=api.Json(prefs["rules"]);
  var rules=LocalApi.Array(prefs["rules"]);appliedFilterText=rules.Length==1&&Convert.ToString(LocalApi.AsDict(rules[0])["style"])=="solid"?Convert.ToString(LocalApi.AsDict(rules[0])["expr"]):encoded;
  // Preserve unfinished edits while unrelated operations refresh the same rules.
  if(encoded!=displayedRules){filter.Text=appliedFilterText;displayedRules=encoded;}
  string normalized=appliedFilterText.Trim();var selected=new HashSet<int>();bool exact=true;
  if(normalized=="active")selected.Add(Convert.ToInt32(prefs["orbit"]));
  else if(normalized!="nothing")foreach(string part in normalized.Split('|')){string token=part.Trim();int index;if(token.Length<2||Char.ToUpperInvariant(token[0])!='O'||!Int32.TryParse(token.Substring(1),out index)||index<0||index>=35){exact=false;break;}selected.Add(index);}
  for(int i=0;i<types.Items.Count;i++)types.SetItemChecked(i,exact&&selected.Contains(i));
  int visible=0,interactiveCount=0;foreach(byte value in styles)if(value!=0)visible++;foreach(byte value in cachedSnapshot==null?styles:cachedSnapshot.Interactive)if(value!=0)interactiveCount++;
  filterStatus=visible.ToString("N0")+" visible · "+interactiveCount.ToString("N0")+" interactive"+(pin.Checked?"\r\nPinned context is shown outside the filter.":"")+(exact?"":"\r\nCustom expression; orbit checklist is only a shortcut.");displayState.Text=visible.ToString("N0")+" / 259,800 visible";UpdateFilterSummary();
  string chosen=macroLibrary.SelectedItem as string;macroLibrary.Items.Clear();if(prefs.ContainsKey("macro_library"))foreach(string name in LocalApi.AsDict(prefs["macro_library"]).Keys)macroLibrary.Items.Add(name);if(chosen!=null&&macroLibrary.Items.Contains(chosen))macroLibrary.SelectedItem=chosen;else if(macroLibrary.Items.Count>0)macroLibrary.SelectedIndex=0;
  sessionReport.Text="Head "+state["head"]+" · "+state["transactions"]+" transactions\r\n"+state["primitives"]+" witnessed primitive turns\r\n"+state["stars"]+" stars\r\n"+state["moving_solved"]+" / "+state["moving_total"]+" moving pieces solved\r\nUse Refresh session report for timer and scramble/solution totals.";
 }
 void UpdateFilterSummary(){bool edited=filter.Text.Trim()!=appliedFilterText.Trim();filterSummary.Text=filterStatus+(edited?"\r\nEdited expression — not applied.":"");filterSummary.ForeColor=edited?Color.FromArgb(138,79,0):Color.FromArgb(43,58,75);}
 object ParseWordRecipe(string text){var moves=new List<int>();foreach(string raw in text.Split(new[]{' ','\r','\n','\t',','},StringSplitOptions.RemoveEmptyEntries)){string token=raw;bool inverse=token.EndsWith("'",StringComparison.Ordinal);if(inverse)token=token.Substring(0,token.Length-1);int value;if(token.StartsWith("H",StringComparison.OrdinalIgnoreCase)||token.StartsWith("T",StringComparison.OrdinalIgnoreCase)){int c=Int32.Parse(token.Substring(1));if(c<0||c>599)throw new FormatException("Cell outside 0..599");value=2*c+(Char.ToUpperInvariant(token[0])=='H'?1:2);}else value=Int32.Parse(token);if(value==0||value<-1200||value>1200)throw new FormatException("Primitive outside +/-1..1200");moves.Add(inverse?-value:value);}return new object[]{LocalApi.D("kind","word","moves",moves)};}
 void ComposeMacro(string operation){object a,b;try{a=macroARecipe!=null&&macroA.Text==macroACaption?macroARecipe:ParseWordRecipe(macroA.Text);b=operation=="inverse"?new object[0]:ParseWordRecipe(macroB.Text);}catch(Exception e){message.Text="Invalid macro word: "+e.Message;return;}Run(delegate{api.Post("compose",LocalApi.D("a",a,"b",b,"operation",operation));},RefreshFromServer);}
 void UsePreviewAsMacroA(){if(state==null||state["pending"]==null){message.Text="Preview a macro first.";return;}var pending=LocalApi.AsDict(state["pending"]);macroARecipe=pending["recipe"];macroACaption="Certified preview: "+Convert.ToString(pending["note"]);macroA.Text=macroACaption;}
 void PreviewSavedMacro(bool inverse){if(state==null||macroLibrary.SelectedItem==null)return;string name=macroLibrary.SelectedItem.ToString();var library=LocalApi.AsDict(LocalApi.AsDict(state["prefs"])["macro_library"]);var recipe=LocalApi.AsDict(library[name])["recipe"];Run(delegate{if(inverse)api.Post("compose",LocalApi.D("a",recipe,"operation","inverse"));else api.Post("preview",LocalApi.D("recipe",recipe,"note",name));},RefreshFromServer);}
 void SaveSet(){string name=setName.Text,expr=setExpression.Text,kind=setKind.Text;Run(delegate{api.Post("save-set",LocalApi.D("name",name,"expression",expr,"kind",kind));},RefreshFromServer);}
 void ReportSession(){Dictionary<string,object> report=null;Run(delegate{report=api.Get("stats");},delegate{RefreshFromServer();DisplaySessionReport(report);});}
 void SessionTimer(string action){Dictionary<string,object> report=null;Run(delegate{api.Post("timer",LocalApi.D("action",action));report=api.Get("stats");},delegate{RefreshFromServer();DisplaySessionReport(report);});}
 void DisplaySessionReport(Dictionary<string,object> report){var t=LocalApi.AsDict(report["timer"]);sessionReport.Text="Head "+state["head"]+"\r\nScramble: "+report["scramble_primitives"]+" primitive turns\r\nSolution: "+report["solution_primitives"]+" primitive turns\r\nStars: "+report["stars"]+"\r\nTransactions: "+report["operations"]+"\r\nAssisted transactions: "+report["assisted_transactions"]+"\r\nTimer: "+Convert.ToDouble(t["seconds"]).ToString("F1")+" s ("+(Convert.ToBoolean(t["running"])?"running":"paused")+")\r\nIncludes thinking time; refresh for current totals.";}
 static string ColorNames(object cells){return String.Join(" ",Array.ConvertAll(LocalApi.Array(cells),x=>"C"+(Convert.ToInt32(x)+1)));}
 void SetPieceDetails(Dictionary<string,object> result){
  inspectedPiece=result;piecePosition.Value=Convert.ToDecimal(result["position"]);int o=Convert.ToInt32(result["orbit"]);
  pieceText.Text="Position P"+result["position"]+" · Identity P"+result["piece"]+"\r\n"+(o<0?"Fixed Cell Center":"Orbit O"+o.ToString("00"))+"\r\nHome colors: "+ColorNames(result["home_cells"])+"\r\nCurrent cells: "+ColorNames(result["current_cells"])+"\r\nSolved: "+result["solved"]+"\r\nPosition correct: "+result["position_correct"]+"\r\nOrientation wrong: "+result["orientation_wrong"];
 }
 void DisplayPiece(Dictionary<string,object> result){RefreshFromServer();SetPieceDetails(result);}
 string displayedInspectionKey;
 string InspectionKey(Dictionary<string,object> result){return Convert.ToString(result["kind"])+":"+result["source_state"]+":"+result["clicked_slot"];}
 void SynchronizeInspection(){
  object value;if(state==null||!state.TryGetValue("inspection",out value)||value==null){
   if(displayedInspectionKey!=null){displayedInspectionKey=null;inspectedPiece=null;pieceText.Text="Inspection cleared. Shift+left click shows home Cell Centers; Shift+right click locates the required piece.";}
   return;
  }
  var inspection=LocalApi.AsDict(value);string key=InspectionKey(inspection);
  SetInspectionDetails(inspection,key!=displayedInspectionKey);displayedInspectionKey=key;
 }
 void SetInspectionDetails(Dictionary<string,object> result,bool restoreDestination){
  string kind=Convert.ToString(result["kind"]);
  if(kind=="home-centers"){
   object current;if(!result.TryGetValue("current_piece",out current))current=result["clicked_piece"];
   SetPieceDetails(LocalApi.AsDict(current));pieceText.Text="Home Cell Centers highlighted in cyan.\r\nOut-of-filter annotations remain noninteractive.\r\nOriginally clicked P"+result["source_position"]+", sticker color C"+result["clicked_sticker_home_color"]+".\r\n\r\n"+pieceText.Text;
  }else{
   SetPieceDetails(LocalApi.AsDict(result["required_piece"]));if(restoreDestination){target.Value=Convert.ToDecimal(result["target_position"]);autoTarget.Checked=false;}
   pieceText.Text="Destination P"+result["target_position"]+" needs identity P"+result["target_identity"]+", now at P"+result["required_position"]+".\r\nHighlighted in yellow; the clicked destination is retained for insertion.\r\n\r\n"+pieceText.Text;
   if(!Convert.ToBoolean(result["buffer_analysis_available"])){bufferAnalysis=null;bufferStateHash="";analyzedFrames.Clear();node.Items.Clear();bufferText.Text=Convert.ToString(result["buffer_analysis_reason"]);}
   else if(restoreDestination){bufferAnalysis=null;bufferStateHash="";analyzedFrames.Clear();node.Items.Clear();bufferText.Text="Destination P"+result["target_position"]+" · Orbit O"+Convert.ToInt32(result["target_orbit"]).ToString("00")+"\r\nRequired identity is now at P"+result["required_position"]+".\r\nAnalyze this destination to refresh its frame candidates.";}
  }
 }
 void DisplayInspection(Dictionary<string,object> result){
  SetInspectionDetails(result,true);displayedInspectionKey=InspectionKey(result);
  if(Convert.ToString(result["kind"])=="home-centers"){tabs.SelectedIndex=6;message.Text="Highlighted the solved home Cell Centers of identity P"+result["identity"]+".";}
  else if(result.ContainsKey("buffer_analysis")&&result["buffer_analysis"]!=null){DisplayBuffer(LocalApi.AsDict(result["buffer_analysis"]));tabs.SelectedIndex=2;message.Text="Required piece highlighted; buffer analysis uses destination P"+result["target_position"]+".";}
  else{tabs.SelectedIndex=6;message.Text=Convert.ToString(result["buffer_analysis_reason"]);}
 }
 void InspectPiece(){int p=(int)piecePosition.Value;Dictionary<string,object> result=null;Run(delegate{result=api.Post("select",LocalApi.D("position",p));},delegate{DisplayPiece(result);});}
 void TrackPiece(){if(inspectedPiece==null){message.Text="Inspect a piece first.";return;}int id=Convert.ToInt32(inspectedPiece["piece"]);Dictionary<string,object> result=null;Run(delegate{result=api.Post("track-piece",LocalApi.D("piece",id));},delegate{DisplayPiece(result);});}
 void UsePieceDestination(){target.Value=piecePosition.Value;autoTarget.Checked=false;message.Text="Fixed insertion destination P"+target.Value+" selected.";}
 void FindRequiredPiece(){int p=(int)piecePosition.Value;UsePieceDestination();Dictionary<string,object> result=null;Run(delegate{result=api.Post("find-required",LocalApi.D("position",p));api.Post("select",LocalApi.D("position",result["position"]));},delegate{DisplayPiece(result);});}
 void NextPiece(int direction){int o=orbit.SelectedIndex,p=(int)piecePosition.Value;Dictionary<string,object> result=null;Run(delegate{result=api.Post("next-piece",LocalApi.D("orbit",o,"position",p,"direction",direction));api.Post("select",LocalApi.D("position",result["position"]));},delegate{DisplayPiece(result);});}
 void CenterInspected(bool home){if(inspectedPiece==null)return;var cells=LocalApi.Array(inspectedPiece[home?"home_cells":"current_cells"]);if(cells.Length==0)return;cell.Value=Convert.ToDecimal(cells[0]);Center();LoadGrips();}
 void Export(string path,string name){if(!connected||busy)return;using(var d=new SaveFileDialog{Title="Export puzzle record",FileName=name,OverwritePrompt=true}){if(d.ShowDialog(form)!=DialogResult.OK)return;ExportToFile(path,d.FileName);}}
 void ExportToFile(string path,string file){if(!connected||busy)return;Run(delegate{byte[] b=api.Bytes(path);WriteAtomic(file,b);},delegate{RefreshFromServer();message.Text="Saved "+file;});}
 static void WriteAtomic(string file,byte[] bytes){
  string target=Path.GetFullPath(file),temporary=target+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}if(File.Exists(target))File.Replace(temporary,target,null);else File.Move(temporary,target);}
  finally{if(File.Exists(temporary))File.Delete(temporary);}
 }
 void SaveLog(){if(!connected||busy)return;Dictionary<string,object> result=null;Run(delegate{result=api.Post("log/save",empty);},delegate{RefreshFromServer();message.Text="Log saved: "+Convert.ToString(result["path"]);});}
 void ExportLog(){if(!connected||busy)return;using(var dialog=new SaveFileDialog{Title="Export puzzle log",FileName="session.c600.json.gz",Filter="C600 verified session (*.c600.json.gz)|*.c600.json.gz|MPUlt v1 log (*.log)|*.log",AddExtension=true,OverwritePrompt=true}){if(dialog.ShowDialog(form)!=DialogResult.OK)return;ExportToFile("log/export?format="+(dialog.FilterIndex==2?"mpult":"c600"),dialog.FileName);}}
 void ImportLog(){if(!connected||busy)return;using(var dialog=new OpenFileDialog{Title="Import puzzle log",Filter="Puzzle logs (*.log;*.gz;*.json)|*.log;*.gz;*.json|All files (*.*)|*.*",CheckFileExists=true,Multiselect=false}){if(dialog.ShowDialog(form)!=DialogResult.OK)return;ImportLogFromFile(dialog.FileName);}}
 void ImportLogFromFile(string file){if(!connected||busy)return;object camera=CaptureCamera();Dictionary<string,object> result=null;Run(delegate{
   byte[] bytes;using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read)){if(stream.Length<2||stream.Length>16*1024*1024)throw new InvalidDataException("Log must contain between 2 bytes and 16 MiB.");bytes=new byte[(int)stream.Length];int read=0;while(read<bytes.Length){int n=stream.Read(bytes,read,bytes.Length-read);if(n==0)throw new EndOfStreamException("Log changed while reading.");read+=n;}}
   string text=Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF',' ','\t','\r','\n');string format=(bytes[0]==0x1f&&bytes[1]==0x8b)||text.StartsWith("{",StringComparison.Ordinal)?"c600":"mpult";
   result=api.Post("log/import",LocalApi.D("format",format,"data_base64",Convert.ToBase64String(bytes),"camera",camera));
  },delegate{RefreshFromServer();message.Text="Verified log imported. Previous state saved as checkpoint: "+Convert.ToString(result["import_checkpoint"]);});}
 static Keys ParseKey(string text){Keys k=Keys.None;foreach(string raw in text.Split('+')){string part=raw.Trim();if(part=="Control"||part=="Ctrl")k|=Keys.Control;else if(part=="Shift")k|=Keys.Shift;else if(part=="Alt")k|=Keys.Alt;else{if(part.StartsWith("Key"))part=part.Substring(3);if(part.StartsWith("Digit"))part="D"+part.Substring(5);k|=(Keys)Enum.Parse(typeof(Keys),part,true);}}return k;}
 static bool ValidAction(string a){if(a.StartsWith("grip:")||a.StartsWith("inverseGrip:")){int i;return Int32.TryParse(a.Split(':')[1],out i)&&i>=0&&i<7;}return new List<string>{"suggest","commit","cancel","undo","redo","checkpoint","toggleTools","center","buffers","protect","filter","reset","saveLog","importLog","exportLog"}.Contains(a);}
 string KeyPath{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"native_keys.json");}}
 void InitializeKeys(){var defs=LocalApi.D("KeyN","suggest","Enter","commit","Escape","cancel","Control+KeyZ","undo","Control+Shift+KeyZ","redo","F9","checkpoint","F8","toggleTools","KeyB","buffers","Control+KeyS","saveLog","Control+KeyO","importLog");for(int i=0;i<7;i++){defs["Digit"+(i+1)]="grip:"+i;defs["Shift+Digit"+(i+1)]="inverseGrip:"+i;}SetKeys(defs);if(!layoutOnly&&File.Exists(KeyPath)){try{SetKeys(LocalApi.AsDict(api.Parse(File.ReadAllText(KeyPath))));}catch(Exception e){NativeDiagnostics.Write("Invalid native keybindings; using defaults and preserving file",e);}}}
 void SetKeys(Dictionary<string,object> values){if(values.Count>100)throw new InvalidOperationException("Too many bindings");var next=new Dictionary<Keys,string>();foreach(var pair in values){string action=Convert.ToString(pair.Value);if(!ValidAction(action))throw new InvalidOperationException("Unknown native action: "+action);Keys k=ParseKey(pair.Key);if(next.ContainsKey(k))throw new InvalidOperationException("Duplicate key combination: "+pair.Key);next[k]=action;}keymap.Clear();foreach(var pair in next)keymap[pair.Key]=pair.Value;}
 void EditKeys(){if(busy)return;var dialog=new Form{Text="Native keybindings",Size=new Size(580,600),StartPosition=FormStartPosition.CenterParent};var edit=new TextBox{Multiline=true,ScrollBars=ScrollBars.Both,Dock=DockStyle.Fill,Font=new Font(FontFamily.GenericMonospace,10)};var d=new Dictionary<string,object>();foreach(var x in keymap)d[x.Key.ToString().Replace(", ","+")]=x.Value;edit.Text=api.Json(d).Replace(",",","+Environment.NewLine);var help=new Label{Dock=DockStyle.Top,Height=76,Text="Windows key names and actions. Grip indices are 0..6.\r\nActions: suggest, commit, cancel, undo, redo, checkpoint, toggleTools, center, buffers, protect, filter, reset, saveLog, importLog, exportLog, grip:0, inverseGrip:0.\r\nBindings are disabled while editing text."};var save=new Button{Text="Save bindings",Dock=DockStyle.Bottom,Height=32};save.Click+=delegate{try{var values=LocalApi.AsDict(api.Parse(edit.Text));SetKeys(values);File.WriteAllText(KeyPath,api.Json(values));dialog.Close();}catch(Exception e){MessageBox.Show(dialog,e.Message,"Invalid bindings");}};dialog.Controls.Add(edit);dialog.Controls.Add(help);dialog.Controls.Add(save);dialog.ShowDialog(form);}
 void KeyAction(string action){if(action=="toggleTools"){ToggleTools();return;}if(action=="cancel"){Cancel();return;}if(busy||!connected)return;if(action.StartsWith("grip:")||action.StartsWith("inverseGrip:")){Grip(Int32.Parse(action.Split(':')[1]),action.StartsWith("inverse"));return;}switch(action){case "suggest":Suggest();break;case "commit":Commit();break;case "undo":Command("undo");break;case "redo":Command("redo");break;case "checkpoint":SaveCheckpoint();break;case "reset":ResetPuzzle();break;case "saveLog":SaveLog();break;case "importLog":ImportLog();break;case "exportLog":ExportLog();break;case "center":Center();break;case "buffers":Analyze();break;case "protect":Protect();break;case "filter":tabs.SelectedIndex=0;filter.Focus();break;}}
 public bool PreFilterMessage(ref Message m){
  if(closing||Form.ActiveForm!=form)return false;
  if((m.Msg==0x201||m.Msg==0x202||m.Msg==0x204||m.Msg==0x205)&&(Control.ModifierKeys&Keys.Alt)!=0)return true;
  if(renderer!=null&&connected&&!busy&&m.HWnd==viewport.Handle&&m.Msg>=0x201&&m.Msg<=0x209&&m.Msg!=0x200){
   if(!renderer.PreparePicking())return true;
  }
  if(m.Msg!=0x100&&m.Msg!=0x104)return false;Keys key=(Keys)m.WParam.ToInt32(),mods=Control.ModifierKeys;
  // Keep standard window close and the visible menu accessible. These are not
  // inherited MPUlt F-key macros and must also work while an editor has focus.
  if(key==Keys.F4&&((mods&Keys.Alt)!=0||(m.Msg==0x104&&((long)m.LParam&(1L<<29))!=0))){form.Close();return true;}
  if(key==Keys.F10&&mods==Keys.None)return false;
  Control focused=Control.FromHandle(m.HWnd);bool editing=false;
  for(Control c=focused;c!=null;c=c.Parent){var view=c as NativeCellView;if(view==null)continue;
   if(view.DispatchNavigationKey(key|mods))return true;
   // Keep auxiliary input away from the inherited MPUlt form's KeyPreview.
   if(key==Keys.Enter&&mods==Keys.None){view.RequestCenter();return true;}
   if(key!=Keys.Tab&&key!=Keys.Escape&&key!=Keys.F8)return true;
   break;
  }
  for(Control c=focused;c!=null;c=c.Parent)if(tabs.TabPages.Count>8&&c==tabs.TabPages[8]){
   // Compact observation controls own their keyboard input, including siblings
   // of the canvas. Never let Enter or inherited MPUlt keys commit a preview.
   if(key==Keys.Tab)return false;
   if(key==Keys.F8){KeyAction("toggleTools");return true;}
   var button=focused as Button;if(button!=null&&mods==Keys.None&&(key==Keys.Enter||key==Keys.Space)){if(button.Enabled)button.PerformClick();return true;}
   var selector=focused as ComboBox;if(selector!=null&&mods==Keys.None){
    if(key==Keys.Down||key==Keys.Right)selector.SelectedIndex=Math.Min(selector.Items.Count-1,selector.SelectedIndex+1);
    else if(key==Keys.Up||key==Keys.Left)selector.SelectedIndex=Math.Max(0,selector.SelectedIndex-1);
    else if(key==Keys.Home)selector.SelectedIndex=0;else if(key==Keys.End)selector.SelectedIndex=selector.Items.Count-1;
    else if(key==Keys.F4||key==Keys.Space)selector.DroppedDown=!selector.DroppedDown;
    else if(key==Keys.Enter||key==Keys.Escape)selector.DroppedDown=false;
   }
   return true;
  }
  for(Control c=focused;c!=null;c=c.Parent)if(c==explorer){
   // Explorer navigation belongs to its controls, including graph activation.
   if(key==Keys.Enter||key==Keys.Space||key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||key==Keys.Home||key==Keys.End||key==Keys.PageUp||key==Keys.PageDown)return false;
   break;
  }
  for(Control c=focused;c!=null;c=c.Parent)if(c is TextBoxBase||c is NumericUpDown||c is ComboBox){editing=true;break;}
  if(editing){
   // Do not let hidden original F-key macros or save/setup accelerators escape
   // through a textbox to the inherited MPUlt form.
   if(key>=Keys.F1&&key<=Keys.F12)return true;
   if((mods&Keys.Control)!=0){
    if(key==Keys.Z){var tb=focused as TextBoxBase;if(tb!=null&&tb.CanUndo)tb.Undo();return true;}
    if(key!=Keys.A&&key!=Keys.C&&key!=Keys.V&&key!=Keys.X&&key!=Keys.Left&&key!=Keys.Right&&key!=Keys.Home&&key!=Keys.End&&key!=Keys.Back&&key!=Keys.Delete&&key!=Keys.Insert&&key!=Keys.ControlKey)return true;
   }
   return false;
  }
  string action;if(keymap.TryGetValue(key|mods,out action)){if(((long)m.LParam&(1L<<30))==0)KeyAction(action);return true;}
  // Original history/setup/macro shortcuts are intentionally not allowed to
  // bypass the authoritative backend or its protected-orbit checks.
  if(key>=Keys.F1&&key<=Keys.F12)return true;
  if((mods&Keys.Control)!=0&&key!=Keys.ControlKey)return true;
  if((mods&Keys.Alt)!=0&&key==Keys.M)return true;
  return false;
 }

}
internal static class Program {
 internal const string Version="0.3";
 internal static void UseEnglishUi(){var language=CultureInfo.GetCultureInfo("en-US");CultureInfo.DefaultThreadCurrentUICulture=language;Thread.CurrentThread.CurrentUICulture=language;}
 [STAThread] static int Main(string[] args){
  UseEnglishUi();
  if(args.Length!=3){MessageBox.Show("Open C600Studio.exe to start this application, or use the source launch script.","C600 Studio");return 2;}
  try{
   string exe=Path.GetFullPath(args[0]);Directory.SetCurrentDirectory(Path.GetDirectoryName(exe));
   AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){string name=new AssemblyName(e.Name).Name+".dll";foreach(string dir in new[]{Path.GetDirectoryName(exe),Path.Combine(Path.GetDirectoryName(exe),"v9.02.2904")}){string p=Path.Combine(dir,name);if(File.Exists(p))return Assembly.LoadFrom(p);}return null;};
   NativeDiagnostics.Write("Starting C600 Native "+Version+"; CLR "+Environment.Version+"; process bits "+(IntPtr.Size*8)+"; OS "+Environment.OSVersion);
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   NativeWorkbench wb=null;
   Application.ThreadException+=delegate(object sender,ThreadExceptionEventArgs e){if(wb!=null&&wb.TryRecoverRenderer(e.Exception))return;NativeDiagnostics.Write("Unhandled native UI exception",e.Exception);MessageBox.Show(e.Exception.ToString()+"\n\nDetails were saved to diagnostics/native-debug.log.","C600 native UI error");Application.Exit();};
   NativeDiagnostics.Write("Loading original MPUlt form and geometry");var assembly=Assembly.LoadFrom(exe);var type=assembly.GetType("_3dedit.Form1",true);var form=(Form)Activator.CreateInstance(type);NativeDiagnostics.Write("Original MPUlt form constructed");wb=new NativeWorkbench(form,exe,args[1],args[2]);Application.Run(form);GC.KeepAlive(wb);return 0;
  }catch(Exception e){while(e is TargetInvocationException&&e.InnerException!=null)e=e.InnerException;NativeDiagnostics.Write("Native startup failed",e);try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"native_start_error.txt"),e.ToString());}catch{}MessageBox.Show(e.ToString()+"\n\nNo antivirus/security changes are required. Keep using the browser client if the native runtime is unavailable.","C600 native start failed");return 1;}
 }
}
