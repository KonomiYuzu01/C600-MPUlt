// Local structure navigation over immutable model metadata. The host alone owns
// HTTP, authoritative filter composition, persistence, and viewport updates.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

internal sealed class NativeStructureFilterRequest {
 internal long Id {get;private set;}
 internal string Predicate {get;private set;}
 internal string Composition {get;private set;}
 internal NativeStructureFilterRequest(long id,string predicate,string composition){Id=id;Predicate=predicate;Composition=composition;}
}

internal sealed class NativeStructureExplorer : UserControl {
 readonly FlowLayoutPanel content=new FlowLayoutPanel();
 readonly TableLayoutPanel layout=new TableLayoutPanel();
 readonly TabControl navigation=new TabControl();
 readonly FlowLayoutPanel filterArea=new FlowLayoutPanel(),filterOptions=new FlowLayoutPanel();
 readonly Label filterSummary=new Label();
 readonly List<FlowLayoutPanel> pages=new List<FlowLayoutPanel>();
 readonly Dictionary<Control,FlowLayoutPanel> owners=new Dictionary<Control,FlowLayoutPanel>();
 readonly TextBox colorSearch=new TextBox(),compareSearch=new TextBox(),expression=new TextBox(),previewInfo=new TextBox();
 readonly Label selectedInfo=new Label(),compareInfo=new Label(),layerInfo=new Label(),feedback=new Label();
 readonly FlowLayoutPanel neighbors=new FlowLayoutPanel(),colorVertices=new FlowLayoutPanel();
 readonly Button[] neighborButtons=new Button[4],vertexButtons=new Button[4];
 readonly ComboBox hops=new ComboBox(),layerChoice=new ComboBox(),predicateChoice=new ComboBox(),compositionChoice=new ComboBox();
 readonly NumericUpDown vertexChoice=new NumericUpDown();
 readonly ListBox layerColors=new ListBox(),vertexColors=new ListBox();
 readonly NativeColorGraph graph=new NativeColorGraph();
 readonly Button centerButton,previewButton,applyButton,insertButton,optionsButton,reviewButton;
 readonly List<Control> wide=new List<Control>();
 int[][] adjacency,vertexIncidence,colorVertexIds;
 Color[] palette;
 int[] distances;
 int selectedColor=1,comparedColor=1,selectedVertex=1;
 bool changing,configured,busy,resizing;
 long nextRequest;
 NativeStructureFilterRequest previewRequest;
 bool previewAccepted;
 internal string ModelId {get;private set;}
 internal int SelectedColor {get{return selectedColor;}}
 internal int SelectedLayer {get{return layerChoice.SelectedIndex<0?0:layerChoice.SelectedIndex;}}
 internal int SelectedVertex {get{return selectedVertex;}}
 internal string SelectedPredicate {get{return expression.Text;}}
 internal event Action<int> CenterRequested;
 internal event Action<string> InsertRequested;
 internal event Action<NativeStructureFilterRequest> PreviewRequested;
 internal event Action<NativeStructureFilterRequest> ApplyRequested;

 internal NativeStructureExplorer(){
  AutoScroll=false;DoubleBuffered=true;BackColor=SystemColors.Control;MinimumSize=new Size(285,300);
  AccessibleName="600-cell structure explorer";
  layout.Dock=DockStyle.Fill;layout.ColumnCount=1;layout.RowCount=3;layout.Padding=new Padding(3);layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(layout);
  SetupSection(content);content.Dock=DockStyle.Fill;layout.Controls.Add(content,0,0);
  navigation.Dock=DockStyle.Fill;navigation.AccessibleName="Structure tasks";navigation.Margin=new Padding(0,3,0,4);layout.Controls.Add(navigation,0,1);
  var colorsPage=Page("Colors");var layersPage=Page("Layers");var verticesPage=Page("Vertices");
  SetupSection(filterArea);filterArea.Dock=DockStyle.Fill;filterArea.Padding=new Padding(0,5,0,0);layout.Controls.Add(filterArea,0,2);
  colorSearch.Text="C1";colorSearch.Width=70;colorSearch.AccessibleName="Find color C1 to C600";
  var find=Button("Find color",FindColor);centerButton=Button("Center viewport",delegate{if(configured&&!busy&&CenterRequested!=null)CenterRequested(selectedColor);});
  Add(content,Row(colorSearch,find,centerButton));
  colorSearch.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter){FindColor();e.Handled=true;e.SuppressKeyPress=true;}};
  selectedInfo.AutoSize=true;selectedInfo.MinimumSize=new Size(0,20);Add(content,selectedInfo);
  hops.DropDownStyle=ComboBoxStyle.DropDownList;hops.Items.AddRange(new object[]{"1 hop","2 hops"});hops.SelectedIndex=1;hops.Width=82;hops.AccessibleName="Graph radius";
  Add(colorsPage,Row(hops,new Label{Text="Local navigation only",AutoSize=true,ForeColor=Color.DimGray,Margin=new Padding(6,6,3,3)}));
  hops.SelectedIndexChanged+=delegate{if(configured&&!changing)RefreshGraph();};
  Add(colorsPage,graph);graph.ColorSelected+=NavigateColor;
  SetupButtonRow(neighbors);neighbors.AccessibleName="Four shared-face neighbors";Add(colorsPage,neighbors);
  Add(colorsPage,Row(Button("Use color",delegate{predicateChoice.SelectedIndex=0;}),Button("Use cell",delegate{predicateChoice.SelectedIndex=1;}),Button("Use neighbors",delegate{predicateChoice.SelectedIndex=2;})));
  compareSearch.Text="C1";compareSearch.Width=82;compareSearch.AccessibleName="Compare color C1 to C600";
  Add(colorsPage,Row(compareSearch,Button("Compare",CompareColor)));compareInfo.AutoSize=true;compareInfo.MinimumSize=new Size(0,30);Add(colorsPage,compareInfo);
  compareSearch.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter){CompareColor();e.Handled=true;e.SuppressKeyPress=true;}};
  Add(layersPage,Heading("Exact cell-centered layers"));
  layerChoice.DropDownStyle=ComboBoxStyle.DropDownList;layerChoice.Width=82;layerChoice.AccessibleName="Cell graph layer";Add(layersPage,Row(layerChoice));
  layerChoice.SelectedIndexChanged+=delegate{if(configured&&!changing){RefreshLayer();RefreshExpression();}};
  layerInfo.AutoSize=true;layerInfo.MinimumSize=new Size(0,48);Add(layersPage,layerInfo);layerColors.Height=170;layerColors.AccessibleName="Colors in the exact cell layer";Add(layersPage,layerColors);
  layerColors.DoubleClick+=delegate{NavigateListColor(layerColors);};
  Add(layersPage,Row(Button("Go to listed color",delegate{NavigateListColor(layerColors);}))); 
  Add(layersPage,Row(Button("Use current layer",delegate{predicateChoice.SelectedIndex=3;}),Button("Use home layer",delegate{predicateChoice.SelectedIndex=4;})));
  Add(layersPage,Note("These buttons prepare the filter below. Preview counts before applying. Multi-cell pieces can belong to more than one layer."));
  Add(verticesPage,Heading("Vertices of the selected cell"));SetupButtonRow(colorVertices);Add(verticesPage,colorVertices);
  for(int i=0;i<4;i++){
   int index=i;
   neighborButtons[i]=NavigationButton("C1",delegate{if(configured)NavigateColor(adjacency[selectedColor-1][index]);});neighbors.Controls.Add(neighborButtons[i]);
   vertexButtons[i]=NavigationButton("V1",delegate{if(configured)SelectVertex(colorVertexIds[selectedColor-1][index]);});colorVertices.Controls.Add(vertexButtons[i]);
  }
  Add(verticesPage,Note("Choose one of the four vertices above, or find any vertex. Vertex lookup does not change the cell-layer origin."));
  vertexChoice.Minimum=1;vertexChoice.Maximum=120;vertexChoice.Value=1;vertexChoice.Width=90;vertexChoice.AccessibleName="Vertex number V1 to V120";
  Add(verticesPage,Row(new Label{Text="Vertex V",AutoSize=true,Margin=new Padding(3,7,3,3)},vertexChoice));
  vertexChoice.ValueChanged+=delegate{if(configured&&!changing)SelectVertex((int)vertexChoice.Value);};
  vertexColors.Height=190;vertexColors.AccessibleName="Twenty incident colors at the selected vertex";Add(verticesPage,vertexColors);
  vertexColors.DoubleClick+=delegate{NavigateListColor(vertexColors);};Add(verticesPage,Row(Button("Go to incident color",delegate{NavigateListColor(vertexColors);}))); 
  expression.ReadOnly=true;expression.AccessibleName="Generated filter expression";Add(filterArea,expression);
  filterSummary.AutoSize=true;filterSummary.MinimumSize=new Size(0,18);filterSummary.ForeColor=Color.DimGray;filterSummary.AccessibleName="Filter mode and preview summary";Add(filterArea,filterSummary);
  SetupSection(filterOptions);Add(filterArea,filterOptions);
  predicateChoice.DropDownStyle=ComboBoxStyle.DropDownList;predicateChoice.Items.AddRange(new object[]{"Color identities","Current cell positions","Adjacent cell positions","Current cell layer","Home identities in layer"});predicateChoice.SelectedIndex=0;predicateChoice.AccessibleName="Structure predicate";Add(filterOptions,predicateChoice);
  predicateChoice.SelectedIndexChanged+=delegate{if(!changing)RefreshExpression();};
  compositionChoice.DropDownStyle=ComboBoxStyle.DropDownList;compositionChoice.Items.AddRange(new object[]{"Replace current filter","Intersect current filter","Union with current filter"});compositionChoice.SelectedIndex=0;compositionChoice.AccessibleName="Filter composition";Add(filterOptions,compositionChoice);
  compositionChoice.SelectedIndexChanged+=delegate{if(!changing)InvalidatePreview();};
  filterOptions.Visible=false;
  previewInfo.Multiline=true;previewInfo.ReadOnly=true;previewInfo.ScrollBars=ScrollBars.Vertical;previewInfo.Height=145;previewInfo.AccessibleName="Filter preview counts and complete composed rules";previewInfo.Visible=false;Add(filterArea,previewInfo);
  previewButton=Button("Preview counts",RequestPreview);applyButton=Button("Apply filter",ApplyPreview);insertButton=Button("Insert expression",delegate{if(configured&&!busy&&InsertRequested!=null)InsertRequested(expression.Text);});
  optionsButton=Button("Options",delegate{filterOptions.Visible=!filterOptions.Visible;optionsButton.Text=filterOptions.Visible?"Hide options":"Options";ResizeContent();});
  reviewButton=Button("Review",delegate{previewInfo.Visible=!previewInfo.Visible;reviewButton.Text=previewInfo.Visible?"Hide review":"Review";ResizeContent();});reviewButton.AccessibleName="Show or hide complete filter preview rules";
  Add(filterArea,Row(previewButton,applyButton));Add(filterArea,Row(insertButton,optionsButton,reviewButton));
  feedback.AutoSize=true;feedback.ForeColor=Color.FromArgb(140,35,25);feedback.AccessibleName="Structure explorer message";feedback.Visible=false;Add(filterArea,feedback);
  Resize+=delegate{ResizeContent();};navigation.SizeChanged+=delegate{ResizeContent();};ResizeContent();RefreshExpression();SetBusy(false);
 }
 static Label Heading(string text){return new Label{Text=text,AutoSize=true,MinimumSize=new Size(0,25),Font=new Font(SystemFonts.MessageBoxFont,FontStyle.Bold),Margin=new Padding(3,10,3,0)};}
 static Label Note(string text){return new Label{Text=text,AutoSize=true,MinimumSize=new Size(0,34),ForeColor=Color.DimGray};}
 static Button Button(string text,Action action){var b=new Button{Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(70,28),Margin=new Padding(3)};b.Click+=delegate{action();};return b;}
 static Button NavigationButton(string text,Action action){var b=Button(text,action);b.AutoSize=false;b.MinimumSize=new Size(52,28);b.Size=new Size(52,28);return b;}
 static FlowLayoutPanel Row(params Control[] controls){var row=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=true,Margin=new Padding(0)};row.Controls.AddRange(controls);return row;}
 static void SetupButtonRow(FlowLayoutPanel row){row.AutoSize=true;row.AutoSizeMode=AutoSizeMode.GrowAndShrink;row.WrapContents=true;row.Margin=new Padding(0);}
 static void SetupSection(FlowLayoutPanel panel){panel.AutoSize=true;panel.AutoSizeMode=AutoSizeMode.GrowAndShrink;panel.FlowDirection=FlowDirection.TopDown;panel.WrapContents=false;panel.Margin=Padding.Empty;}
 FlowLayoutPanel Page(string name){var page=new TabPage(name){Padding=new Padding(3)};var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Margin=Padding.Empty};page.Controls.Add(panel);navigation.TabPages.Add(page);pages.Add(panel);panel.SizeChanged+=delegate{ResizeContent();};return panel;}
 void Add(FlowLayoutPanel owner,Control control){owner.Controls.Add(control);wide.Add(control);owners[control]=owner;}
 void ResizeContent(){
  if(resizing)return;resizing=true;
  try{
   int outer=Math.Max(260,ClientSize.Width-12);content.Width=outer;filterArea.Width=outer;content.MaximumSize=new Size(outer,0);filterArea.MaximumSize=new Size(outer,0);
   foreach(var control in wide){FlowLayoutPanel owner=owners[control];int width=pages.Contains(owner)?Math.Max(240,owner.ClientSize.Width-SystemInformation.VerticalScrollBarWidth-8):outer-(owner==filterOptions?12:6);control.Width=width;if(control is FlowLayoutPanel||control is Label)control.MaximumSize=new Size(width,0);}
   graph.Height=Math.Max(238,Math.Min(360,graph.Width-10));layerColors.Height=Math.Max(125,Math.Min(260,navigation.ClientSize.Height-220));vertexColors.Height=Math.Max(125,Math.Min(280,navigation.ClientSize.Height-175));
  }finally{resizing=false;}
 }

 // The host converts the validated /api/structure arrays to this typed boundary.
 // Copies prevent later caller mutation from changing navigation semantics.
 internal void Configure(string modelId,int[][] canonicalAdjacency,int[][] incidentColors,Color[] canonicalPalette){
  if(String.IsNullOrWhiteSpace(modelId))throw new ArgumentException("A model ID is required.");
  int[][] nextAdjacency=CopyRows(canonicalAdjacency,600,4,600,"cell adjacency"),nextIncidence=CopyRows(incidentColors,120,20,600,"vertex incidence");
  for(int c=1;c<=600;c++)foreach(int neighbor in nextAdjacency[c-1])if(neighbor==c||!nextAdjacency[neighbor-1].Contains(c))throw new ArgumentException("Cell adjacency must be symmetric without self edges.");
  var inverse=Enumerable.Range(0,600).Select(i=>new List<int>()).ToArray();
  for(int v=0;v<120;v++)foreach(int c in nextIncidence[v])inverse[c-1].Add(v+1);
  if(inverse.Any(row=>row.Count!=4))throw new ArgumentException("Every cell must meet exactly four vertices.");
  int[] reachable=Distances(nextAdjacency,1);if(reachable.Any(d=>d<0))throw new ArgumentException("Cell graph must be connected.");
  if(canonicalPalette!=null&&canonicalPalette.Length!=600)throw new ArgumentException("The color palette must contain 600 entries.");
  ModelId=modelId;adjacency=nextAdjacency;vertexIncidence=nextIncidence;colorVertexIds=inverse.Select(row=>row.ToArray()).ToArray();palette=canonicalPalette==null?null:(Color[])canonicalPalette.Clone();configured=true;
  NavigateColor(selectedColor);SelectVertex(selectedVertex);SetBusy(busy);
 }
 static int[][] CopyRows(int[][] input,int rows,int columns,int maximum,string name){
  if(input==null||input.Length!=rows)throw new ArgumentException("Invalid "+name+" row count.");var result=new int[rows][];
  for(int i=0;i<rows;i++){if(input[i]==null||input[i].Length!=columns||input[i].Any(n=>n<1||n>maximum)||input[i].Distinct().Count()!=columns)throw new ArgumentException("Invalid "+name+" row.");result[i]=(int[])input[i].Clone();Array.Sort(result[i]);}return result;
 }
 static int[] Distances(int[][] graphRows,int root){
  var result=Enumerable.Repeat(-1,600).ToArray();var queue=new Queue<int>();result[root-1]=0;queue.Enqueue(root);
  while(queue.Count>0){int c=queue.Dequeue();foreach(int n in graphRows[c-1])if(result[n-1]<0){result[n-1]=result[c-1]+1;queue.Enqueue(n);}}return result;
 }
 static int ParseColor(string text){int value;string input=(text??"").Trim();if(input.StartsWith("C",StringComparison.OrdinalIgnoreCase))input=input.Substring(1);if(!Int32.TryParse(input,out value)||value<1||value>600)throw new ArgumentException("Enter a color from C1 to C600.");return value;}
 void FindColor(){if(!configured)return;try{NavigateColor(ParseColor(colorSearch.Text));}catch(ArgumentException e){ShowError(e.Message);}}
 void CompareColor(){if(!configured)return;try{comparedColor=ParseColor(compareSearch.Text);compareSearch.Text="C"+comparedColor;RefreshComparison();RefreshGraph();ClearFeedback();}catch(ArgumentException e){ShowError(e.Message);}}
 internal void NavigateColor(int color){
  if(!configured)return;if(color<1||color>600)throw new ArgumentOutOfRangeException("color");
  // Preserve navigation HWNDs and unchanged layer items. Rebuilding these
  // controls caused repeated native handle creation and autosize layout.
   selectedColor=color;distances=Distances(adjacency,color);colorSearch.Text="C"+color;
   selectedInfo.Text="Layer origin: C"+color+" · lab cell "+(color-1);
   int previous=SelectedLayer,count=distances.Max()+1;bool wasChanging=changing;changing=true;
   layerChoice.BeginUpdate();
   try{
    if(layerChoice.Items.Count!=count){layerChoice.Items.Clear();for(int k=0;k<count;k++)layerChoice.Items.Add("L"+k);}
    layerChoice.SelectedIndex=Math.Min(previous,count-1);
   }finally{layerChoice.EndUpdate();changing=wasChanging;}
   for(int i=0;i<4;i++){neighborButtons[i].Text="C"+adjacency[color-1][i];vertexButtons[i].Text="V"+colorVertexIds[color-1][i];}
   RefreshComparison();RefreshGraph();RefreshLayer();RefreshExpression();ClearFeedback();
 }
 internal void SelectVertex(int vertex){
  if(!configured)return;if(vertex<1||vertex>120)throw new ArgumentOutOfRangeException("vertex");selectedVertex=vertex;changing=true;vertexChoice.Value=vertex;changing=false;
  vertexColors.BeginUpdate();try{vertexColors.Items.Clear();foreach(int c in vertexIncidence[vertex-1])vertexColors.Items.Add("C"+c);}finally{vertexColors.EndUpdate();}
 }
 internal void SelectLayer(int layer){if(!configured)return;if(layer<0||layer>=layerChoice.Items.Count)throw new ArgumentOutOfRangeException("layer");layerChoice.SelectedIndex=layer;}
 void NavigateListColor(ListBox list){if(list.SelectedItem!=null){NavigateColor(ParseColor(list.SelectedItem.ToString()));navigation.SelectedIndex=0;}}
 void RefreshComparison(){compareInfo.Text="C"+selectedColor+" → C"+comparedColor+": "+distances[comparedColor-1]+" face-adjacency step(s).\r\n"+(selectedColor==comparedColor?"Same cell.":adjacency[selectedColor-1].Contains(comparedColor)?"These cells share a triangular face.":"These cells do not share a triangular face.");}
 void RefreshGraph(){graph.SetGraph(adjacency,selectedColor,comparedColor,hops.SelectedIndex+1,palette);}
 void RefreshLayer(){
  int layer=SelectedLayer;var cells=Enumerable.Range(1,600).Where(c=>distances[c-1]==layer).ToArray();
  layerInfo.Text="L"+layer+": "+cells.Length+" cells at exactly "+layer+" shared-face step(s) from C"+selectedColor+".\r\nCell shells are disjoint; whole-piece selections can overlap.";
  layerColors.BeginUpdate();try{layerColors.Items.Clear();foreach(int c in cells)layerColors.Items.Add("C"+c);}finally{layerColors.EndUpdate();}
 }
 internal string GeneratePredicate(string kind){
  string c="C"+selectedColor;if(kind=="color"||kind=="cell"||kind=="adjacent")return kind+"("+c+")";
  if(kind=="layer"||kind=="home_layer")return kind+"("+c+",L"+SelectedLayer+")";
  throw new ArgumentException("Unknown structure predicate.");
 }
 void RefreshExpression(){string[] names={"color","cell","adjacent","layer","home_layer"};expression.Text=GeneratePredicate(names[Math.Max(0,predicateChoice.SelectedIndex)]);InvalidatePreview();}
 internal void InvalidatePreview(){previewRequest=null;previewAccepted=false;previewInfo.Text="Preview counts before applying a filter. Counts exclude optional pins and inspection annotations.";previewInfo.Visible=false;filterSummary.Text=compositionChoice.Text+" · preview required";if(reviewButton!=null)reviewButton.Text="Review";if(applyButton!=null)applyButton.Enabled=false;}
 void RequestPreview(){
  if(!configured||busy)return;string[] modes={"replace","intersect","union"};previewRequest=new NativeStructureFilterRequest(++nextRequest,expression.Text,modes[compositionChoice.SelectedIndex]);previewAccepted=false;applyButton.Enabled=false;previewInfo.Text="Counting matching whole pieces…";filterSummary.Text="Counting matching whole pieces…";ClearFeedback();
  if(PreviewRequested!=null)PreviewRequested(previewRequest);else ShowError("The filter preview service is not connected.");
 }
 internal void ShowPreview(long requestId,int pieces,int stickers,string detail){
  if(previewRequest==null||previewRequest.Id!=requestId)return;
  if(pieces<0||pieces>177120||stickers<0||stickers>259800)throw new ArgumentOutOfRangeException("pieces","Invalid filter preview counts.");
  previewAccepted=true;string counts=pieces.ToString("N0")+" pieces · "+stickers.ToString("N0")+" sticker slots";previewInfo.Text=counts+"\r\n"+(detail??"Counts exclude pins and inspection annotations.");filterSummary.Text=counts;filterOptions.Visible=false;optionsButton.Text="Options";previewInfo.Visible=ClientSize.Height>=680;reviewButton.Text=previewInfo.Visible?"Hide review":"Review";applyButton.Enabled=configured&&!busy;ClearFeedback();ResizeContent();
 }
 void ApplyPreview(){if(!configured||busy||!previewAccepted||previewRequest==null)return;NativeStructureFilterRequest request=previewRequest;previewAccepted=false;applyButton.Enabled=false;if(ApplyRequested!=null)ApplyRequested(request);}
 void ClearFeedback(){feedback.Text="";feedback.Visible=false;}
 internal void ShowError(string message){feedback.Text=message??"";feedback.Visible=feedback.Text.Length>0;previewRequest=null;previewAccepted=false;applyButton.Enabled=false;filterSummary.Text="Preview unavailable · check the message below";}
 internal void SetBusy(bool value){busy=value;layout.Enabled=configured&&!busy;if(applyButton!=null)applyButton.Enabled=configured&&!busy&&previewAccepted;}
}
