using System;
using System.IO;
using System.Text;

internal static class NativeDiagnostics {
 static readonly object gate = new object();
 internal static string DirectoryPath {
  get {
   string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diagnostics");
   try { Directory.CreateDirectory(dir); return dir; }
   catch {
    dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "C600Studio", "diagnostics");
    Directory.CreateDirectory(dir); return dir;
   }
  }
 }
 internal static void Write(string stage, Exception error = null) {
  try {
   lock (gate) {
    string file = Path.Combine(DirectoryPath, "native-debug.log");
    if (File.Exists(file) && new FileInfo(file).Length > 4 * 1024 * 1024) {
     string old = file + ".previous";
     if (File.Exists(old)) File.Delete(old);
     File.Move(file, old);
    }
    File.AppendAllText(file, DateTime.UtcNow.ToString("o") + " " + stage +
     (error == null ? "" : Environment.NewLine + error.ToString()) + Environment.NewLine, Encoding.UTF8);
   }
  } catch { /* Logging must not replace the original error with a new failure. */ }
 }
}
