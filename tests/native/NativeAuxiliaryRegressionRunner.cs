// Standalone entry point for the bounded structure and auxiliary WinForms fixtures.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal static class NativeAuxiliaryRegressionRunner {
 [STAThread] static int Main(string[] args) {
  if (args.Length != 3) return 2;
  Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
  Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
  var json = new JavaScriptSerializer { MaxJsonLength = 10000000 };
  try {
   var structure = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(args[0]));
   var status = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(args[1]));
   var explorer = NativeStructureExplorerRegression.Run(structure);
   var control = NativeCellViewRegression.Run(structure, status);
   var palette = Enumerable.Range(1, 600).Select(i => Color.FromArgb(80 + i * 13 % 176, 80 + i * 29 % 176, 80 + i * 37 % 176)).ToArray();
   var manager = NativeAuxiliaryViewsRegression.Run(NativeCellGeometry.Read(structure, palette));
   File.WriteAllText(args[2], json.Serialize(new {
    passed = true,
    scope = "Isolated Windows GDI/WinForms controls and owned forms, retained model geometry, generated test palette and production status from a fresh solved session. No MPUlt, HTTP engine, personal session or performance acceptance.",
    structure_checks = explorer, control_checks = control, manager_checks = manager
   }));
   Console.WriteLine("PASS " + explorer.Count + " structure, " + control.Count + " cell-view and " + manager.Count + " auxiliary-manager checks"); return 0;
  } catch (Exception error) {
   File.WriteAllText(args[2], json.Serialize(new { passed = false, error = error.ToString() }));
   Console.Error.WriteLine(error); return 1;
  }
 }
}
