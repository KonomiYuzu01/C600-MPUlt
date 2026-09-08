// Actual isolated WinForms containers with NativeCellView. No original MPUlt
// window, global input, backend, personal data, or timing acceptance is involved.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

internal static class NativeAuxiliaryViewsRegression {
 const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
 static T Field<T>(object value, string name) { return (T)value.GetType().GetField(name, Members).GetValue(value); }
 static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
 static void Pump() { Application.DoEvents(); }
 static bool Active(NativeCellView view) { return (bool)view.GetType().GetProperty("RenderingActive", Members).GetValue(view, null); }
 static string Camera(NativeCellView view) { return String.Join(",", ((double[])view.GetType().GetMethod("CaptureCamera", Members).Invoke(view, null)).Select(x => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture))); }
 static void Click(Control root, string text) { var button = Descendants(root).OfType<Button>().FirstOrDefault(value => value.Text == text); if (button == null) throw new ArgumentException("Button not found: " + text); button.PerformClick(); }
 static IEnumerable<Control> Descendants(Control root) { foreach (Control child in root.Controls) { yield return child; foreach (Control nested in Descendants(child)) yield return nested; } }
 static void DragCamera(NativeCellView view, int delta) {
  view.Refresh(); Pump(); var scene = Field<Rectangle>(view, "sceneRect");
  int x = scene.Left + scene.Width / 2, y = scene.Top + scene.Height / 2;
  view.GetType().GetMethod("OnMouseDown", Members).Invoke(view, new object[] { new MouseEventArgs(MouseButtons.Left, 1, x, y, 0) });
  view.GetType().GetMethod("OnMouseMove", Members).Invoke(view, new object[] { new MouseEventArgs(MouseButtons.Left, 0, x + delta, y + 13, 0) });
  view.GetType().GetMethod("OnMouseUp", Members).Invoke(view, new object[] { new MouseEventArgs(MouseButtons.Left, 1, x + delta, y + 13, 0) }); Pump();
 }
 static void SelectProjectedCell(NativeCellView view) {
  view.Refresh(); Pump(); var centers = Field<PointF[]>(view, "screenCenters");
  foreach (PointF center in centers) {
   Point p = Point.Round(center); int hit = (int)view.GetType().GetMethod("Hit", Members).Invoke(view, new object[] { p });
   if (hit < 1 || hit == view.SelectedColor) continue;
   view.GetType().GetMethod("OnMouseDown", Members).Invoke(view, new object[] { new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0) });
   view.GetType().GetMethod("OnMouseUp", Members).Invoke(view, new object[] { new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0) }); Pump(); return;
  }
  throw new InvalidOperationException("No distinct projected cell was pickable in the fixture.");
 }

 internal static List<string> Run(NativeCellGeometry geometry) {
  var checks = new List<string>();
  Require(!NativeAuxiliaryViews.ShouldUseDrawer(1100, 704, 1) && NativeAuxiliaryViews.ShouldUseDrawer(1099, 900, 1) && NativeAuxiliaryViews.ShouldUseDrawer(1600, 703, 1), "Width or short-screen drawer boundary changed.");
  Require(!NativeAuxiliaryViews.ShouldUseDrawer(1375, 880, 1.25) && NativeAuxiliaryViews.ShouldUseDrawer(1374, 1200, 1.25) && NativeAuxiliaryViews.ShouldUseDrawer(2000, 879, 1.25), "Drawer policy failed to use logical width/height at125% scale.");
  checks.Add("Pure drawer policy covers exact width/short-screen boundaries and125% logical scaling; this is not a multi-monitor desktop test");
  using (var owner = new Form { Text = "Isolated auxiliary view lifecycle fixture", ClientSize = new Size(1220, 760), StartPosition = FormStartPosition.Manual, Location = new Point(40, 40) })
  using (var host = new Panel { Dock = DockStyle.Right, Width = 348 }) {
   owner.Controls.Add(host); owner.Show(); Pump();
   using (var manager = new NativeAuxiliaryViews(owner, host)) {
    int centers = 0, selections = 0, drawerRequests = 0;
    manager.CenterRequested += delegate(int color) { centers++; Require(color == manager.SelectedColor, "Center used a different canonical selection."); };
    manager.SelectionChanged += delegate { selections++; };
    manager.DrawerRequested += delegate { drawerRequests++; host.Visible = true; };
    manager.Configure(geometry); Pump();
    var global = Field<Form>(manager, "globalForm"); var local = Field<Form>(manager, "localForm"); var drawer = Field<TabControl>(manager, "drawer");
    Require(!manager.GlobalOpen && !manager.LocalOpen && !Active(manager.GlobalView) && !Active(manager.LocalView), "Closed views were active after configure.");
    manager.OpenGlobal(); manager.OpenLocal(); Pump();
    Require(!manager.IsCompact && global.Visible && local.Visible && global.Owner == owner && local.Owner == owner, "Wide layout did not open simultaneous owned modeless windows.");
    Require(Active(manager.GlobalView) && Active(manager.LocalView), "Open windows were not active.");
    Require(global.Font.Name == "Segoe UI" && local.Font.Name == "Segoe UI", "Auxiliary windows lost the application UI font.");
    checks.Add("Wide mode opens two simultaneous owned, resizable English WinForms windows");

    manager.SetSelectedColor(17); Require(manager.LocalOrigin == 17 && manager.LocalView.FocusColor == 17, "Follow did not track selection.");
    manager.FollowLocal(false); manager.SetSelectedColor(42);
    Require(manager.LocalPinned && manager.LocalOrigin == 17 && manager.LocalView.FocusColor == 17 && manager.GlobalView.SelectedColor == 42 && manager.LocalView.SelectedColor == 42, "Pin changed origin or prevented shared selection.");
    Require(centers == 0 && selections == 0, "Programmatic selection emitted a camera or selection callback.");
    Click(global, "Center main"); Require(centers == 1, "Explicit Center main did not emit exactly once.");
    checks.Add("Follow/Pin separates local origin, shared canonical selection and explicit main-camera requests");

    string initialCamera = Camera(manager.GlobalView); DragCamera(manager.GlobalView, 31); DragCamera(manager.LocalView, -23);
    string globalCamera = Camera(manager.GlobalView), localCamera = Camera(manager.LocalView);
    Require(globalCamera != initialCamera && localCamera != initialCamera && globalCamera != localCamera && selections == 0, "In-process actual camera drags did not create independent cameras or emitted selection.");
    global.Close(); Pump();
    Require(!manager.GlobalOpen && !global.Visible && !global.IsDisposed && !manager.GlobalView.IsDisposed && !Active(manager.GlobalView), "Closing global disposed its retained view or kept drawing.");
    manager.GlobalView.Invalidate(); manager.GlobalView.Refresh(); Pump();
    Require(!Active(manager.GlobalView), "Closed global view became active after invalidation.");
    manager.OpenGlobal(); Pump();
    Require(global.Visible && Camera(manager.GlobalView) == globalCamera && Camera(manager.LocalView) == localCamera && manager.LocalOrigin == 17, "Reopening reset a camera or pinned origin.");
    checks.Add("Close/reopen retains both cameras and pin; invalidation does not reactivate a closed renderer");

    local.WindowState = FormWindowState.Minimized; Pump();
    Require(!Active(manager.LocalView) && Active(manager.GlobalView), "Minimizing one view did not suspend only its renderer.");
    manager.LocalView.Refresh(); Pump();
    Require(!Active(manager.LocalView), "Minimized view became active after invalidation.");
    manager.OpenLocal(); Pump(); Require(local.WindowState == FormWindowState.Normal && Active(manager.LocalView), "Reopen did not restore a minimized view.");
    checks.Add("Minimize stops scene draws independently; opening the view restores its window");

    var globalInstance = manager.GlobalView; var localInstance = manager.LocalView;
    owner.ClientSize = new Size(1000, 720); Pump();
    Require(manager.IsCompact && !global.Visible && !local.Visible && manager.GlobalView.FindForm() == owner && manager.LocalView.FindForm() == owner, "Compact layout did not reparent both views into the tools drawer.");
    manager.OpenLocal(); Pump(); Require(drawerRequests == 1 && drawer.SelectedIndex == 1 && !Active(manager.GlobalView) && Active(manager.LocalView), "Compact open failed to select only the local drawer renderer.");
    manager.OpenGlobal(); Pump(); Require(drawer.SelectedIndex == 0 && Active(manager.GlobalView) && !Active(manager.LocalView), "Inactive drawer page continued rendering.");
    Require(Camera(globalInstance) == globalCamera && Camera(localInstance) == localCamera && manager.LocalOrigin == 17, "Reparenting reset cameras or origin.");
    Require(!Descendants(host).OfType<HScrollBar>().Any(x => x.Visible), "Compact drawer created a horizontal scrollbar.");
    checks.Add("Narrow windows use the Views drawer, retain camera state and suspend the inactive inner tab");

    host.Visible = false; Pump(); Require(!Active(manager.GlobalView) && !Active(manager.LocalView), "Hidden tools drawer kept rendering.");
    manager.OpenGlobal(); Pump(); Require(host.Visible && Active(manager.GlobalView), "Open did not request the hidden drawer.");
    manager.CloseGlobal(); Pump(); Require(!Active(manager.GlobalView) && !manager.GlobalOpen, "Compact close kept the global view active.");
    Click(Field<TabPage>(manager, "globalPage"), "Open global"); Pump(); Require(manager.GlobalOpen, "Closed drawer placeholder did not reopen its view.");
    checks.Add("Hidden tools and closed drawer views stop rendering and remain discoverable through Open actions");

    manager.SetBusy(true); Pump(); int oldCenters = centers; Click(Field<Panel>(manager, "globalPanel"), "Center main");
    Require(manager.GlobalView.Enabled && manager.LocalView.Enabled && centers == oldCenters, "Busy state froze auxiliary cameras or permitted main centering.");
    DragCamera(manager.GlobalView, 19); Require(Camera(manager.GlobalView) != globalCamera, "Busy state blocked independent camera input."); globalCamera = Camera(manager.GlobalView);
    SelectProjectedCell(manager.GlobalView); Require(selections == 1 && manager.SelectedColor != 42 && manager.LocalOrigin == 17 && centers == oldCenters, "Busy selection failed to synchronize locally or changed pin/main camera.");
    manager.SetBusy(false); Require(manager.GlobalView.Enabled && manager.LocalView.Enabled, "Ready state failed to restore interaction.");
    manager.FollowLocal(true); Require(manager.LocalOrigin == manager.SelectedColor, "Returning to Follow did not use the latest shared selection.");
    checks.Add("Busy state preserves actual camera and cell-selection input while gating main centering; Follow resumes from the latest selection");

    owner.ClientSize = new Size(1220, 760); Pump();
    Require(!manager.IsCompact && global.Visible && local.Visible && ReferenceEquals(globalInstance, manager.GlobalView) && ReferenceEquals(localInstance, manager.LocalView), "Returning wide lost an open view or its instance.");
    global.Location = new Point(120, 90); global.Size = new Size(430, 390); local.Location = new Point(170, 120); local.Size = new Size(450, 410); Pump();
    Require(global.Size.Width == 430 && local.Size.Width == 450, "Auxiliary windows did not retain independent user resizing.");
    manager.ResetPlacement(); Pump(); Rectangle area = Screen.FromControl(owner).WorkingArea;
    Require(area.Contains(global.Bounds) && area.Contains(local.Bounds), "Reset placement put a window outside the working area.");
    Require(!global.Bounds.IntersectsWith(local.Bounds), "Reset placement overlapped the two auxiliary windows.");
    Require(Camera(manager.GlobalView) == globalCamera && Camera(manager.LocalView) == localCamera, "Reset placement changed an independent view camera.");
    checks.Add("Wide return, independent resize/drag placement and on-screen placement reset preserve cameras");

    manager.ShowError("Cell geometry is unavailable."); Pump();
    Require(!Active(manager.GlobalView) && !Active(manager.LocalView) && !manager.GlobalView.Visible && !manager.LocalView.Visible, "Unavailable auxiliary geometry retained active or misleading view content.");
    Require(Field<Label>(manager, "globalContext").Text.Contains("Retry after reconnect") && Field<Label>(manager, "localContext").Text.Contains("Cell geometry is unavailable"), "Auxiliary errors were not explained in both windows.");
    manager.Configure(geometry); Pump(); Require(Active(manager.GlobalView) && Active(manager.LocalView) && !Field<Label>(manager, "globalContext").Text.Contains("unavailable"), "Successful reconfiguration failed to clear error state.");
    checks.Add("Geometry/status errors stop auxiliary drawing and explain reconnect; successful configure restores usable views");

    owner.Hide(); Pump(); Require(!Active(manager.GlobalView) && !Active(manager.LocalView), "Hidden owner kept auxiliary rendering active.");
    owner.Show(); Pump(); Require(global.Visible && local.Visible && Active(manager.GlobalView) && Active(manager.LocalView), "Showing the owner failed to restore open windows.");
    owner.Close(); Pump(); Require(global.IsDisposed && local.IsDisposed && manager.GlobalView.IsDisposed && manager.LocalView.IsDisposed, "Owner close left an auxiliary form or control alive.");
    checks.Add("Owner hide/show suspends/restores views; owner close disposes all owned UI and render controls");
   }
  }
  return checks;
 }
}
