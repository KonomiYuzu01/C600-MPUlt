// Native .NET Framework 4.x layout. No MPUlt or DirectX dependency.
// Use a traditional docking Splitter rather than imposing SplitContainer panel
// minima on a control that has not yet been given its final client width.
using System;
using System.Drawing;
using System.Windows.Forms;

internal sealed class NativeDockLayout : Panel {
 internal readonly Panel ViewportPanel = new Panel();
 internal readonly Panel ToolsPanel = new Panel();
 readonly Splitter divider = new Splitter();
 bool requested = true;
 bool arranging;
 int preferred = 348;
 internal const int MinimumViewportWidth = 360;
 internal const int MinimumToolsWidth = 320;
 internal const int DividerWidth = 5;

 internal NativeDockLayout() {
  Name = "C600Workspace";
  Margin = Padding.Empty;
  Padding = Padding.Empty;
  ViewportPanel.Name = "C600ViewportPanel";
  ViewportPanel.Dock = DockStyle.Fill;
  ToolsPanel.Name = "C600ToolsPanel";
  ToolsPanel.Dock = DockStyle.Right;
  ToolsPanel.Width = preferred;
  divider.Name = "C600ToolsDivider";
  divider.Dock = DockStyle.Right;
  divider.Width = DividerWidth;
  divider.MinSize = MinimumToolsWidth;
  divider.MinExtra = MinimumViewportWidth;
  // Dock layout walks the children back to front: tools, divider, then viewport.
  Controls.Add(ViewportPanel);
  Controls.Add(divider);
  Controls.Add(ToolsPanel);
  divider.SplitterMoved += delegate {
   if (!arranging) preferred = Math.Max(MinimumToolsWidth, ToolsPanel.Width);
   FitPanels();
  };
  SizeChanged += delegate { FitPanels(); };
  FitPanels();
 }
 internal bool ToolsRequested {
  get { return requested; }
  set { requested = value; FitPanels(); }
 }
 internal int PreferredToolsWidth {
  get { return preferred; }
  set { preferred = Math.Max(MinimumToolsWidth, Math.Min(1600, value)); FitPanels(); }
 }
 internal static int ToolsWidthFor(int clientWidth, int preferredWidth, bool show) {
  if (!show || clientWidth < MinimumViewportWidth + MinimumToolsWidth + DividerWidth) return 0;
  return Math.Min(Math.Max(MinimumToolsWidth, preferredWidth), clientWidth - MinimumViewportWidth - DividerWidth);
 }
 internal void FitPanels() {
  if (arranging || IsDisposed) return;
  arranging = true;
  SuspendLayout();
  try {
   divider.Width = DividerWidth;
   int width = ToolsWidthFor(ClientSize.Width, preferred, requested);
   bool show = width > 0;
   // Tiny construction/minimized widths simply hide the tools. No impossible
   // SplitterDistance or PanelMinSize assignment exists in this layout path.
   if (show) ToolsPanel.Width = width;
   ToolsPanel.Visible = show;
   divider.Visible = show;
   divider.Enabled = show;
  } finally {
   ResumeLayout(true);
   arranging = false;
  }
 }
}
