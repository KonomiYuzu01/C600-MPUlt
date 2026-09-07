// Production commands and the actual original MPUlt Puzzle.Save/Load methods.
// Run only inside the renderer runner's isolated journal and runtime copy.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class NativeSessionLogRegression {
 static NativeWorkbench wb;static LocalApi api;static Control viewport;
 static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 static Dictionary<string,object> State(){return LocalApi.AsDict(Reflect.Get(wb,"state"));}
 static string Hash(){return Convert.ToString(State()["state_hash"]);}
 static string Message(){return ((ToolStripStatusLabel)Reflect.Get(wb,"message")).Text;}
 static async Task Native(string method,params object[] args){
  Reflect.Call(wb,method,args);var until=DateTime.UtcNow.AddMinutes(3);
  while(Convert.ToBoolean(Reflect.Get(wb,"busy"))){Assert(DateTime.UtcNow<until,"Log command timeout: "+Message());await Task.Delay(30);}
  Assert(Convert.ToBoolean(Reflect.Get(wb,"connected"))&&viewport.Enabled,"Log command disconnected native input: "+Message());
 }
 static async Task Backend(string path,Dictionary<string,object> data){
  await Native("Run",new Action(delegate{api.Post(path,data);}),new Action(delegate{Reflect.Call(wb,"RefreshFromServer");}),false);
 }
 static void SameBytes(byte[] a,byte[] b,string message){Assert(a.Length==b.Length,message+": length");for(int i=0;i<a.Length;i++)if(a[i]!=b[i])throw new InvalidOperationException(message+": byte "+i);}
 static void SameColors(object a,object b,string message){var x=(short[])Reflect.Get(a,"Field");var y=(short[])Reflect.Get(b,"Field");Assert(x.Length==259800&&y.Length==259800,message+": slots");for(int i=0;i<x.Length;i++)if((x[i]&16383)!=(y[i]&16383))throw new InvalidOperationException(message+": native sticker "+i);}
 static Dictionary<string,object> Capture(){return LocalApi.AsDict(api.Parse(api.Json(Reflect.Call(wb,"CaptureCamera"))));}
 static void SameCamera(Dictionary<string,object> expected){var actual=Capture();foreach(string key in new[]{"radius","angle","face_shrink","sticker_shrink","cell"})Assert(Math.Abs(Convert.ToDouble(expected[key])-Convert.ToDouble(actual[key]))<1e-6,"Recovered camera differs: "+key);var a=LocalApi.Array(expected["matrix"]);var b=LocalApi.Array(actual["matrix"]);for(int i=0;i<16;i++)Assert(Math.Abs(Convert.ToDouble(a[i])-Convert.ToDouble(b[i]))<1e-10,"Recovered camera matrix differs at "+i);}
 static string PrefsWithoutCamera(){var prefs=LocalApi.AsDict(api.Parse(api.Json(State()["prefs"])));prefs.Remove("camera");return api.Json(prefs);}
 static void Recenter(int cell){((NumericUpDown)Reflect.Get(wb,"cell")).Value=cell;Reflect.Call(wb,"Center");}
 static async Task Restore(string name){((ComboBox)Reflect.Get(wb,"checkpoint")).SelectedItem=name;await Native("RestoreCheckpoint",name);}
 internal static async Task Run(NativeWorkbench workbench,Form form,Control control,LocalApi client,string output,Action<string> check){
  wb=workbench;viewport=control;api=client;string initial=Hash(),backup="Before native reset/log regression";
  await Backend("checkpoint",LocalApi.D("name",backup));
  await Backend("prefs",LocalApi.D("protected",new int[0]));
  await Native("ResetPuzzle");Assert(Convert.ToBoolean(State()["solved"]),"Reset did not solve full puzzle");
  await Native("Scramble");Assert(!Convert.ToBoolean(State()["solved"]),"Scramble remained solved");string scrambled=Hash();byte[] labels=api.Bytes("labels");string prefs=PrefsWithoutCamera();
  Recenter(23);var liveCamera=Capture();Assert(api.Json(liveCamera)!=api.Json(LocalApi.AsDict(State()["prefs"])["camera"]),"Camera fixture must differ from last persisted camera");
  await Native("ResetPuzzle");Assert(Convert.ToBoolean(State()["solved"])&&prefs==PrefsWithoutCamera(),"Reset lost solved state or view preferences");SameCamera(liveCamera);
  Assert(Message().StartsWith("Reset to solved."),"Reset did not report recovery checkpoint");
  var entries=LocalApi.Array(State()["checkpoints"]);string recovery=null;foreach(var entry in entries){var item=LocalApi.AsDict(entry);if(Convert.ToString(item["name"]).StartsWith("Before reset")){recovery=Convert.ToString(item["name"]);break;}}
  Assert(recovery!=null,"Reset checkpoint is missing");Recenter(57);await Restore(recovery);Assert(Hash()==scrambled,"Reset checkpoint did not restore scramble");SameBytes(labels,api.Bytes("labels"),"Reset checkpoint full labels");SameCamera(liveCamera);
  check("Native Reset after 1000-turn scramble restores all labels through its automatic checkpoint, including the current unsaved native camera and focused cell");
  string proof=Path.Combine(output,"024-export.c600.json.gz");await Native("ExportToFile","log/export?format=c600",proof);Assert(File.Exists(proof),"C600 log export missing");
  await Native("SaveLog");Assert(Message().StartsWith("Log saved: "),"Save log failed: "+Message());string saved=Message().Substring("Log saved: ".Length);Assert(File.Exists(saved),"Saved log path does not exist");SameBytes(File.ReadAllBytes(proof),File.ReadAllBytes(saved),"Saved and exported C600 log contents");
  await Native("ResetPuzzle");Recenter(101);var importCamera=Capture();await Native("ImportLogFromFile",proof);Assert(Hash()==scrambled&&Message().StartsWith("Verified log imported."),"Native C600 import failed: "+Message());SameBytes(labels,api.Bytes("labels"),"C600 log roundtrip labels");SameCamera(importCamera);
  string importRecovery=Message().Substring("Verified log imported. Previous state saved as checkpoint: ".Length);long importHead=Convert.ToInt64(State()["head"]);Recenter(201);await Restore(importRecovery);Assert(Convert.ToBoolean(State()["solved"]),"Import recovery checkpoint lost prior state");SameCamera(importCamera);await Backend("checkout",LocalApi.D("head",importHead));
  await Native("Command","undo");Assert(Convert.ToBoolean(State()["solved"]),"Imported C600 transaction undo failed");await Native("Command","redo");Assert(Hash()==scrambled,"Imported C600 transaction redo failed");
  string plain=Path.Combine(output,"024-export-uncompressed.json");using(var input=File.OpenRead(proof))using(var gzip=new GZipStream(input,CompressionMode.Decompress))using(var text=new StreamReader(gzip))File.WriteAllText(plain,text.ReadToEnd(),new System.Text.UTF8Encoding(true));
  await Native("ImportLogFromFile",plain);Assert(Hash()==scrambled&&Message().StartsWith("Verified log imported."),"Native UTF-8 BOM JSON log detection failed: "+Message());SameBytes(labels,api.Bytes("labels"),"Native uncompressed C600 log labels");
  string bad=Path.Combine(output,"024-invalid.log");File.WriteAllText(bad,"MPUltimate invalid");long head=Convert.ToInt64(State()["head"]);string oldPrefs=api.Json(State()["prefs"]);Recenter(333);await Native("ImportLogFromFile",bad);Assert(Hash()==scrambled&&Convert.ToInt64(State()["head"])==head&&oldPrefs==api.Json(State()["prefs"]),"Invalid native log changed current branch or camera prefs");Assert(Message().Contains("Incomplete MPUlt log"),"Invalid log command did not report its parser error: "+Message());
  string sentinel=Path.Combine(output,"024-existing-destination.log");byte[] old={11,22,33};File.WriteAllBytes(sentinel,old);await Native("ExportToFile","log/export?format=invalid",sentinel);SameBytes(old,File.ReadAllBytes(sentinel),"Failed log export overwrote existing destination");Assert(Message().Contains("Unsupported log format"),"Invalid export did not report its format error: "+Message());
  check("Native save/export/import log commands roundtrip full labels, retain undo/redo and reject corrupt files without replacing state or existing output");

  // Build a genuine independent log with the original writer: negative angle,
  // both outer caps and an undone tail. No production native puzzle is changed.
  object live=Reflect.Get(wb,"puz"),structure=Reflect.Get(live,"Str");Type type=live.GetType();
  object original=Activator.CreateInstance(type,Reflect.Flags,null,new[]{structure},null);
  Reflect.Call(original,"Twist",0,0,1,1);Reflect.Call(original,"Twist",1,1,-1,4);Reflect.Call(original,"Twist",2,0,1,5);Reflect.Call(original,"Undo");
  Reflect.Set(original,"CTime",123450000L);string incoming=Path.Combine(output,"024-original-mpult.log");Reflect.Call(original,"Save",incoming);Assert(File.Exists(incoming),"Original MPUlt writer failed");
  File.WriteAllLines(Path.Combine(output,"024-original-model-description.txt"),(string[])Reflect.Call(structure,"GetDescription"));
  await Native("ImportLogFromFile",incoming);Assert(Message().StartsWith("Verified log imported."),"Original MPUlt log rejected: "+Message());SameColors(original,live,"Original MPUlt imported colors");
  Reflect.Call(original,"Redo");await Native("Command","redo");SameColors(original,live,"Original MPUlt undone-tail redo");
  string native=Path.Combine(output,"024-export-mpult.log");await Native("ExportToFile","log/export?format=mpult",native);Assert(File.Exists(native),"MPUlt export failed: "+Message());
  object loaded=Activator.CreateInstance(type,true);Reflect.Call(loaded,"Load",native);SameColors(loaded,live,"Actual MPUlt loader exported colors");Reflect.Call(loaded,"Recalculate");SameColors(loaded,live,"Actual MPUlt independent full sequence replay");
  string roundtrip=Hash();byte[] expected=api.Bytes("labels");await Native("ResetPuzzle");await Native("ImportLogFromFile",native);Assert(Hash()==roundtrip,"MPUlt roundtrip changed full label identities");SameBytes(expected,api.Bytes("labels"),"MPUlt full labelled roundtrip");
  check("Actual original MPUlt Save and Load interoperate with v1 logs, including negative angle, two caps, undone tail, checksum, full colors and independent native sequence replay");
  await Backend("restore",LocalApi.D("name",backup));Assert(Hash()==initial,"Log tests did not restore prior test state");
 }
}
