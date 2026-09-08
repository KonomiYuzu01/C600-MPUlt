// Modeless auxiliary windows and the compact tools drawer. No engine requests,
// puzzle mutations, animation loop, or camera coupling to the main viewport.
using System;
using System.Drawing;
using System.Windows.Forms;

internal sealed class NativeAuxiliaryViews : IDisposable {
 internal const int CompactWidth = 1100;
 readonly Form owner;
 readonly Control drawerHost;
 readonly TabControl drawer = new TabControl();
 readonly TabPage globalPage = new TabPage("Global overview");
 readonly TabPage localPage = new TabPage("Focused neighborhood");
 readonly Panel globalPanel = new Panel();
 readonly Panel localPanel = new Panel();
 readonly Form globalForm;
 readonly Form localForm;
 readonly Label globalClosed = new Label();
 readonly Label localClosed = new Label();
 readonly Label globalContext = new Label();
 readonly Label localContext = new Label();
 readonly ComboBox follow = new ComboBox();
 readonly ComboBox hops = new ComboBox();
 readonly Font uiFont = new Font("Segoe UI", 9F);
 readonly ToolTip help = new ToolTip();
 readonly Button globalCenter;
 readonly Button localCenter;
 bool globalOpen, localOpen, compact, configured, busy, arranging, disposed, changing;
 int selectedColor = 1, localOrigin = 1;
 string failure;
 Rectangle workingArea;
 double workingScale = 1;
 internal readonly NativeCellView GlobalView = new NativeCellView(true);
 internal readonly NativeCellView LocalView = new NativeCellView(false);
 internal event Action<int> SelectionChanged;
 internal event Action<int> CenterRequested;
 internal event Action DrawerRequested;
 internal bool IsCompact { get { return compact; } }
 internal bool GlobalOpen { get { return globalOpen; } }
 internal bool LocalOpen { get { return localOpen; } }
 internal bool LocalPinned { get { return follow.SelectedIndex == 1; } }
 internal int LocalOrigin { get { return localOrigin; } }
 internal int SelectedColor { get { return selectedColor; } }

 internal NativeAuxiliaryViews(Form owner, Control drawerHost) {
  if (owner == null || drawerHost == null) throw new ArgumentNullException("owner");
  this.owner = owner; this.drawerHost = drawerHost;
  drawer.Dock = DockStyle.Fill; drawer.Font = uiFont;
  drawer.AccessibleName = "Auxiliary views";
  drawer.TabPages.Add(globalPage); drawer.TabPages.Add(localPage);
  drawerHost.Controls.Add(drawer);
  globalPage.Padding = localPage.Padding = new Padding(3);
  PrepareClosed(globalClosed, globalPage, "Global overview is closed. Use Open global below.", OpenGlobal);
  PrepareClosed(localClosed, localPage, "Focused neighborhood is closed. Use Open local below.", OpenLocal);
  globalForm = MakeWindow("Global overview — C600 Studio");
  localForm = MakeWindow("Focused neighborhood — C600 Studio");
  globalCenter = BuildPanel(globalPanel, GlobalView, globalContext, false);
  localCenter = BuildPanel(localPanel, LocalView, localContext, true);
  globalForm.FormClosing += GlobalClosing; localForm.FormClosing += LocalClosing;
  globalForm.Resize += VisibilityChanged; localForm.Resize += VisibilityChanged;
  globalForm.VisibleChanged += VisibilityChanged; localForm.VisibleChanged += VisibilityChanged;
  owner.Resize += OwnerResized; owner.LocationChanged += OwnerMoved; owner.VisibleChanged += VisibilityChanged;
  owner.FormClosed += OwnerClosed;
  drawer.SelectedIndexChanged += VisibilityChanged;
  drawerHost.VisibleChanged += VisibilityChanged;
  globalPanel.VisibleChanged += VisibilityChanged; localPanel.VisibleChanged += VisibilityChanged;
  GlobalView.SelectionChanged += UserSelection; LocalView.SelectionChanged += UserSelection;
  GlobalView.CenterRequested += RequestCenter; LocalView.CenterRequested += RequestCenter;
  // Hover only synchronizes auxiliary highlighting. It never selects a color,
  // requests a viewport refresh, or queries the backend.
  GlobalView.HoverChanged += delegate(int color) { LocalView.SetHoverColor(color); };
  LocalView.HoverChanged += delegate(int color) { GlobalView.SetHoverColor(color); };
  ResetPlacement(); Arrange(); UpdateContext();
 }

 Form MakeWindow(string title) {
  return new Form { Text = title, Font = uiFont, StartPosition = FormStartPosition.Manual,
   Size = new Size(420, 360), MinimumSize = new Size(350, 340), ShowInTaskbar = false,
   FormBorderStyle = FormBorderStyle.Sizable, KeyPreview = false, AutoScaleMode = AutoScaleMode.Dpi };
 }
 void PrepareClosed(Label label, TabPage page, string text, Action open) {
  label.Dock = DockStyle.Fill; label.TextAlign = ContentAlignment.MiddleCenter;
  label.Text = text; label.Padding = new Padding(12); label.AccessibleName = text;
  var button = new Button { Text = page == globalPage ? "Open global" : "Open local", Dock = DockStyle.Bottom, Height = 34 };
  button.Click += delegate { open(); };
  page.Controls.Add(label); page.Controls.Add(button);
 }
 Button BuildPanel(Panel panel, NativeCellView view, Label context, bool local) {
  panel.Dock = DockStyle.Fill; panel.Font = uiFont;
  var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(4) };
  layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
  layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
  layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
  layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
  var tools = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = Padding.Empty };
  if (local) {
   follow.DropDownStyle = ComboBoxStyle.DropDownList; follow.Items.AddRange(new object[] { "Follow selection", "Pin this origin" }); follow.SelectedIndex = 0;
   follow.Width = 138; follow.AccessibleName = "Neighborhood origin mode";
   help.SetToolTip(follow, "Follow selection moves this neighborhood's origin. Pin this origin retains the current origin while other cells are selected.");
   follow.SelectedIndexChanged += delegate { if (!changing) { if (!LocalPinned) SetLocalOrigin(selectedColor); UpdateContext(); } };
   hops.DropDownStyle = ComboBoxStyle.DropDownList; hops.Items.AddRange(new object[] { "1 hop", "2 hops" }); hops.SelectedIndex = 0; hops.Width = 72; hops.AccessibleName = "Neighborhood radius";
   hops.SelectedIndexChanged += delegate { LocalView.LocalHops = hops.SelectedIndex + 1; UpdateContext(); };
   tools.Controls.Add(follow); tools.Controls.Add(hops);
  }
  var center = AddButton(tools, "Center main", delegate { RequestCenter(selectedColor); });
  help.SetToolTip(center, "Center the main puzzle camera on the selected canonical cell. Selecting a cell alone does not move that camera.");
  AddButton(tools, "Reset camera", view.ResetCamera);
  AddButton(tools, "Close view", local ? (Action)CloseLocal : CloseGlobal);
  view.Dock = DockStyle.Fill; view.Margin = Padding.Empty; view.Font = uiFont;
  context.AutoSize = true; context.Dock = DockStyle.Fill; context.Margin = new Padding(2, 4, 2, 2);
  context.AccessibleName = local ? "Neighborhood context" : "Global context";
  context.AccessibleDescription = "Auxiliary view availability and recovery information.";
  help.SetToolTip(context, context.AccessibleDescription);
  // Explicit width bounds let the context wrap without expanding the drawer.
  panel.SizeChanged += delegate { context.MaximumSize = new Size(Math.Max(200, panel.ClientSize.Width - 12), 0); };
  layout.Controls.Add(tools, 0, 0); layout.Controls.Add(view, 0, 1); layout.Controls.Add(context, 0, 2); panel.Controls.Add(layout);
  return center;
 }
 static Button AddButton(FlowLayoutPanel parent, string text, Action action) {
  var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(74, 28), Margin = new Padding(2), Padding = new Padding(3, 0, 3, 0) };
  button.Click += delegate { action(); }; parent.Controls.Add(button); return button;
 }
 internal void Configure(NativeCellGeometry geometry) {
  if (geometry == null) throw new ArgumentNullException("geometry");
  GlobalView.Configure(geometry); LocalView.Configure(geometry); configured = true; failure = null;
  GlobalView.Visible = LocalView.Visible = true;
  globalClosed.Text = "Global overview is closed. Use Open global below."; localClosed.Text = "Focused neighborhood is closed. Use Open local below.";
  SetSelectedColor(selectedColor); SyncActivity();
 }
 internal void UpdateStatus(NativeCellStatus status) { GlobalView.UpdateStatus(status); LocalView.UpdateStatus(status); }
 internal void ShowError(string message) {
  configured = false; failure = "Auxiliary view unavailable. " + (String.IsNullOrWhiteSpace(message) ? "Geometry or status could not be loaded." : message) + "\r\nRetry after reconnect.";
  GlobalView.Visible = LocalView.Visible = false; globalClosed.Text = localClosed.Text = failure;
  UpdateContext(); SyncActivity();
 }
 internal void SetSelectedColor(int color) {
  if (color < 1 || color > 600) throw new ArgumentOutOfRangeException("color");
  selectedColor = color; GlobalView.SetSelectedColor(color); LocalView.SetSelectedColor(color);
  if (!LocalPinned) SetLocalOrigin(color); UpdateContext();
 }
 void SetLocalOrigin(int color) { localOrigin = color; LocalView.SetFocus(color); }
 internal void FollowLocal(bool value) {
  changing = true; try { follow.SelectedIndex = value ? 0 : 1; } finally { changing = false; }
  if (value) SetLocalOrigin(selectedColor); UpdateContext();
 }
 void UserSelection(int color) { if (!configured || disposed) return; SetSelectedColor(color); if (SelectionChanged != null) SelectionChanged(color); }
 void RequestCenter(int color) { if (configured && !busy && !disposed && CenterRequested != null) CenterRequested(color); }
 internal void SetBusy(bool value) { busy = value; SyncActivity(); }
 void UpdateContext() {
  // The renderer already labels actual selection, origin and counts. Only
  // errors need another line; Follow/Pin is explicit in the toolbar itself.
  globalContext.Visible = localContext.Visible = failure != null;
  globalContext.Text = localContext.Text = failure ?? "";
 }
 internal void OpenGlobal() { if (disposed) return; globalOpen = true; Open(false); }
 internal void OpenLocal() { if (disposed) return; localOpen = true; Open(true); }
 void Open(bool local) {
  Arrange();
  if (compact) { drawer.SelectedTab = local ? localPage : globalPage; if (DrawerRequested != null) DrawerRequested(); }
  else { Form window = local ? localForm : globalForm; if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal; window.Activate(); }
  SyncActivity();
 }
 internal void CloseGlobal() { if (disposed) return; globalOpen = false; Arrange(); }
 internal void CloseLocal() { if (disposed) return; localOpen = false; Arrange(); }
 void GlobalClosing(object sender, FormClosingEventArgs e) { if (disposed || e.CloseReason == CloseReason.FormOwnerClosing || e.CloseReason == CloseReason.ApplicationExitCall || e.CloseReason == CloseReason.WindowsShutDown) return; e.Cancel = true; CloseGlobal(); }
 void LocalClosing(object sender, FormClosingEventArgs e) { if (disposed || e.CloseReason == CloseReason.FormOwnerClosing || e.CloseReason == CloseReason.ApplicationExitCall || e.CloseReason == CloseReason.WindowsShutDown) return; e.Cancel = true; CloseLocal(); }
 void OwnerClosed(object sender, FormClosedEventArgs e) { Dispose(); }
 void OwnerResized(object sender, EventArgs e) { if (disposed) return; if (owner.WindowState != FormWindowState.Minimized) RefreshDisplayPolicy(true); Arrange(); }
 void OwnerMoved(object sender, EventArgs e) { if (disposed || arranging || owner.WindowState == FormWindowState.Minimized) return; if (RefreshDisplayPolicy(true)) Arrange(); }
 void VisibilityChanged(object sender, EventArgs e) { if (sender == owner) Arrange(); else SyncActivity(); }
 double DisplayScale() { if (!owner.IsHandleCreated) return 1; using (Graphics graphics = owner.CreateGraphics()) return graphics.DpiX / 96.0; }
 internal static bool ShouldUseDrawer(int ownerClientWidth, int workingAreaHeight, double scale) {
  if (scale <= 0 || Double.IsNaN(scale) || Double.IsInfinity(scale)) throw new ArgumentOutOfRangeException("scale");
  return ownerClientWidth / scale < CompactWidth || workingAreaHeight / scale < 704;
 }
 bool RefreshDisplayPolicy(bool relocateChangedDisplay) {
  Rectangle area = Screen.FromControl(owner).WorkingArea; double scale = DisplayScale();
  bool displayChanged = area != workingArea || Math.Abs(scale - workingScale) > .001;
  bool previous = compact; workingArea = area; workingScale = scale;
  compact = ShouldUseDrawer(owner.ClientSize.Width, area.Height, scale);
  // Preserve manual placements while moving within one working area. A changed
  // monitor, DPI or taskbar area must not strand a retained window off-screen.
  if (displayChanged && relocateChangedDisplay) PlaceDefaultWindows();
  return displayChanged || previous != compact;
 }
 void Arrange() {
  if (disposed || arranging) return; arranging = true;
  try {
   Place(globalPanel, globalPage, globalForm, globalOpen); Place(localPanel, localPage, localForm, localOpen);
   globalClosed.Visible = !globalOpen; localClosed.Visible = !localOpen;
   foreach (Control child in globalPage.Controls) if (child is Button) child.Visible = !globalOpen;
   foreach (Control child in localPage.Controls) if (child is Button) child.Visible = !localOpen;
  } finally { arranging = false; SyncActivity(); }
 }
 void Place(Panel panel, TabPage page, Form window, bool opened) {
  Control destination = compact ? (Control)page : window;
  if (panel.Parent != destination) { panel.Parent = destination; panel.Dock = DockStyle.Fill; panel.BringToFront(); }
  panel.Visible = opened;
  // Owned modeless windows are deliberately hidden in compact mode. Hide/close
  // never disposes a view or resets its independent camera and pinned origin.
  if (compact || !opened || !owner.Visible || owner.WindowState == FormWindowState.Minimized) window.Hide();
  else if (!window.Visible) window.Show(owner);
 }
 void SyncActivity() {
  if (disposed || arranging) return;
  bool ownerReady = owner.Visible && owner.WindowState != FormWindowState.Minimized;
  bool globalActive = configured && ownerReady && globalOpen && (compact ? drawer.Visible && drawer.SelectedTab == globalPage : globalForm.Visible && globalForm.WindowState != FormWindowState.Minimized);
  bool localActive = configured && ownerReady && localOpen && (compact ? drawer.Visible && drawer.SelectedTab == localPage : localForm.Visible && localForm.WindowState != FormWindowState.Minimized);
  if ((GlobalView.RenderingActive && !globalActive) || (LocalView.RenderingActive && !localActive)) { GlobalView.SetHoverColor(0); LocalView.SetHoverColor(0); }
  GlobalView.SetRenderingActive(globalActive); LocalView.SetRenderingActive(localActive);
  // Auxiliary cameras and selection remain responsive while the main puzzle
  // commits. The host queues selection context; only explicit centering waits.
  GlobalView.Enabled = LocalView.Enabled = configured;
  globalCenter.Enabled = localCenter.Enabled = configured && !busy;
 }
 internal void ResetPlacement() {
  if (disposed) return;
  RefreshDisplayPolicy(false); PlaceDefaultWindows(); Arrange();
 }
 void PlaceDefaultWindows() {
  Rectangle area = workingArea; double scale = workingScale;
  int gap = Math.Max(8, (int)Math.Round(8 * scale));
  int width = Math.Min((int)Math.Round(420 * scale), area.Width);
  int height = Math.Min((int)Math.Round(450 * scale), (area.Height - 3 * gap) / 2);
  // Two fully visible, non-overlapping windows along the screen's right edge
  // leave most of the main viewport clear. Short working areas use the drawer.
  height = Math.Max(340, height);
  int x = area.Right - width - gap, y = area.Top + gap;
  globalForm.Bounds = new Rectangle(Math.Max(area.Left, x), y, width, height);
  localForm.Bounds = new Rectangle(Math.Max(area.Left, x), y + height + gap, width, height);
 }
 public void Dispose() {
  if (disposed) return; disposed = true;
  owner.Resize -= OwnerResized; owner.LocationChanged -= OwnerMoved; owner.VisibleChanged -= VisibilityChanged; owner.FormClosed -= OwnerClosed; drawerHost.VisibleChanged -= VisibilityChanged;
  if (!GlobalView.IsDisposed) GlobalView.SetRenderingActive(false); if (!LocalView.IsDisposed) LocalView.SetRenderingActive(false);
  // Detach both panels first: one may currently belong to the drawer, the
  // other to an owned window. Each is disposed exactly once below.
  if (!globalPanel.IsDisposed) globalPanel.Parent = null; if (!localPanel.IsDisposed) localPanel.Parent = null;
  globalForm.Dispose(); localForm.Dispose(); globalPanel.Dispose(); localPanel.Dispose(); drawer.Dispose(); help.Dispose(); uiFont.Dispose();
 }
}
