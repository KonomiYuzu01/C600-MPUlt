// Focused production workflow over the real MPUlt HWND and DirectX device.
// Invoked only by the explicit developer renderer regression launcher.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

internal static class NativeFeatureRegression {
 static void Assert(bool value,string text){if(!value)throw new InvalidOperationException(text);}
 static Dictionary<string,object> State(NativeWorkbench wb){return LocalApi.AsDict(Reflect.Get(wb,"state"));}
 static string Hash(NativeWorkbench wb){return Convert.ToString(State(wb)["state_hash"]);}
 static async Task Idle(NativeWorkbench wb){
  await NativeAuxiliaryNativeRegression.Settled(wb);
 }
 static async Task Wait(Func<bool> ready){var limit=DateTime.UtcNow.AddSeconds(30);while(!ready()){if(DateTime.UtcNow>limit)throw new TimeoutException("Read-only explorer did not complete");await Task.Delay(10);}}
 static async Task Command(NativeWorkbench wb,string command){Reflect.Call(wb,"Command",command);await Idle(wb);}
 static async Task Apply(NativeWorkbench wb,string expression){((TextBox)Reflect.Get(wb,"filter")).Text=expression;Reflect.Call(wb,"ApplyFilter");await Idle(wb);}
 static void Match(NativeWorkbench wb,LocalApi api){
  var snapshot=NativeSnapshot.Read(api.Get("native/snapshot"),Convert.ToString(LocalApi.AsDict(Reflect.Get(wb,"profile"))["profile_sha256"]));
  var field=(short[])Reflect.Get(Reflect.Get(wb,"puz"),"Field");var style=(byte[])Reflect.Get(wb,"previousStyles");
  Assert(field.Length==259800&&style.Length==259800,"Full state is incomplete");
  for(int i=0;i<field.Length;i++)Assert(field[i]==(snapshot.Colors[2*i]|snapshot.Colors[2*i+1]<<8)&&style[i]==snapshot.Styles[i],"Atomic native data mismatch at "+i);
  byte[] labels=api.Bytes("labels");Assert(labels.Length==259800*4,"Full labels missing");
  using(var sha=System.Security.Cryptography.SHA256.Create())Assert(BitConverter.ToString(sha.ComputeHash(labels)).Replace("-","").ToLowerInvariant()==Hash(wb),"Full label hash mismatch");
 }
 static async Task InspectionContext(NativeWorkbench wb,LocalApi api,Action<string> check){
  string initial=Hash(wb);var orbit=(ComboBox)Reflect.Get(wb,"orbit");orbit.SelectedIndex=33;await Idle(wb);await Apply(wb,"O34");
  ((TextBox)Reflect.Get(wb,"word")).Text="1 2 7 31 101 600 1200 65 433 812 1198";Reflect.Call(wb,"PreviewWord");await Idle(wb);Reflect.Call(wb,"Commit");await Idle(wb);Assert(Hash(wb)!=initial,"Inspection tracking fixture did not move state");
  var analysis=api.Post("buffers",LocalApi.D("orbit",34));int destination=Convert.ToInt32(analysis["target"]);
  var clicked=api.Post("piece",LocalApi.D("position",destination));int labSlot=Convert.ToInt32(LocalApi.Array(clicked["slots"])[0]);
  int hit=Array.IndexOf((int[])Reflect.Get(wb,"nativeToLab"),labSlot);Assert(hit>=0,"Inspection tracking fixture lacks native mapping");
  Reflect.Call(wb,"InspectNative",hit,"required-piece",Hash(wb));await Idle(wb);
  var context=LocalApi.AsDict(State(wb)["inspection"]);int located=Convert.ToInt32(context["required_position"]);
  Assert(located!=destination&&orbit.SelectedIndex==33,"Required inspection changed the active filter orbit or did not locate a moved piece");
  string prefsBefore=api.Json(State(wb)["prefs"]),hashBefore=Hash(wb);var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");renderer.HoldUpdates=true;
  Reflect.Call(wb,"InspectNative",hit,"home-centers",new string('0',64));await Idle(wb);
  Assert(Hash(wb)==hashBefore&&api.Json(State(wb)["prefs"])==prefsBefore&&!renderer.HoldUpdates,"Stale inspection changed preferences/state or left rendering held");Match(wb,api);
  Reflect.Call(wb,"Analyze");await Idle(wb);var buffer=LocalApi.AsDict(Reflect.Get(wb,"bufferAnalysis"));
  Assert(Convert.ToInt32(buffer["orbit"])==34&&Convert.ToInt32(buffer["target"])==destination&&orbit.SelectedIndex==33,"Analyze used the global filter orbit instead of the inspected destination orbit");
  int selectedTab=((TabControl)Reflect.Get(wb,"tabs")).SelectedIndex;
  await Command(wb,"undo");var shown=LocalApi.AsDict(Reflect.Get(wb,"inspectedPiece"));
  Assert(Convert.ToInt32(shown["piece"])==destination&&Convert.ToInt32(shown["position"])==destination,"Inspector did not follow required identity after undo");
  Assert(((TextBox)Reflect.Get(wb,"bufferText")).Text.Contains("State changed")&&((TabControl)Reflect.Get(wb,"tabs")).SelectedIndex==selectedTab,"Undo omitted stale-analysis warning or changed the user's tab");
  Reflect.Call(wb,"PreviewStar",1);await Idle(wb);Assert(State(wb)["pending"]==null,"Stale buffer frame was previewed after undo");
  await Command(wb,"redo");shown=LocalApi.AsDict(Reflect.Get(wb,"inspectedPiece"));
  Assert(Convert.ToInt32(shown["position"])==located&&Convert.ToInt32(((NumericUpDown)Reflect.Get(wb,"target")).Value)==destination,"Redo lost the tracked required location or fixed destination");
  Reflect.Call(wb,"Suggest");await Idle(wb);var pending=LocalApi.AsDict(State(wb)["pending"]);Assert(pending!=null,"Inspected O34 insertion failed while global orbit remained O33");
  foreach(object item in LocalApi.Array(pending["recipe"]))Assert(Convert.ToInt32(LocalApi.AsDict(item)["orbit"])==34,"Insertion used the global filter orbit");
  Assert(orbit.SelectedIndex==33,"Insertion changed the original active orbit");Reflect.Call(wb,"Cancel");await Idle(wb);await Command(wb,"undo");
  Reflect.Call(wb,"InspectNative",hit,"home-centers",Hash(wb));await Idle(wb);await Command(wb,"redo");shown=LocalApi.AsDict(Reflect.Get(wb,"inspectedPiece"));
  Assert(Convert.ToInt32(shown["piece"])==destination&&Convert.ToInt32(shown["position"])==located,"Home-center inspector did not track its identity after redo");
  await Command(wb,"undo");Reflect.Call(wb,"ClearInspection");await Idle(wb);
  // Restore annotation using only the atomic status path, as reconnect does.
  await Apply(wb,"cell(C1)");int centerHit=Array.IndexOf((int[])Reflect.Get(wb,"nativeToLab"),0);int restoreTab=((TabControl)Reflect.Get(wb,"tabs")).SelectedIndex;
  api.Post("native/inspect",LocalApi.D("native_sticker",centerHit,"gesture","required-piece","pre_state",Hash(wb)));
  Reflect.Call(wb,"RefreshFromServer");
  Assert(((TextBox)Reflect.Get(wb,"bufferText")).Text.Contains("fixed cell center")&&((ComboBox)Reflect.Get(wb,"node")).Items.Count==0,"Snapshot-only inspection restore retained unrelated candidates or omitted unavailable destination reason");
  Assert(((TabControl)Reflect.Get(wb,"tabs")).SelectedIndex==restoreTab,"Snapshot restore changed the current inspection tab");
  Reflect.Call(wb,"ClearInspection");await Idle(wb);await Apply(wb,"active");Assert(Hash(wb)==initial,"Inspection context regression did not restore full state");
  check("Snapshot inspection recovery and undo/redo track current identities without tab changes; O34 destination analysis/insertion preserves the global O33 filter orbit");
 }
 internal static async Task Run(NativeWorkbench wb,Form form,Control viewport,LocalApi api,string output,Action<string> check){
  await Idle(wb);string initial=Hash(wb);Match(wb,api);
  check("Focused startup retains every native color, style and labelled slot");
  await NativeAuxiliaryNativeRegression.Run(wb,form,viewport,api,output,check);
  await NativePickingRegression.Run(wb,form,viewport,api,output,check);
  Reflect.Call(wb,"ClearInspection");await Idle(wb);Assert(State(wb)["inspection"]==null,"Clear inspection did not clear annotation");
  await InspectionContext(wb,api,check);
  await Apply(wb,"active");await Task.Delay(250);
  var renderer=(NativeRendererLifecycle)Reflect.Get(wb,"renderer");var explorer=(NativeStructureExplorer)Reflect.Get(wb,"explorer");var tabs=(TabControl)Reflect.Get(wb,"tabs");
  tabs.SelectedIndex=7;form.Activate();explorer.Focus();await Task.Delay(50);renderer.RenderFrame();int frames=renderer.FramesRequested;
  foreach(int color in new[]{1,600,17,42,17}){explorer.NavigateColor(color);explorer.SelectLayer(3);explorer.SelectVertex(120);}
  await Task.Delay(100);Assert(renderer.FramesRequested==frames&&Hash(wb)==initial,"Explorer navigation requested a DirectX frame or changed puzzle state");
  Assert(explorer.GeneratePredicate("layer")=="layer(C17,L3)","Layer origin is not the selected cell");
  var graph=(NativeColorGraph)Reflect.Get(explorer,"graph");graph.Focus();var key=Message.Create(graph.Handle,0x100,(IntPtr)(int)Keys.Enter,IntPtr.Zero);
  Assert(!wb.PreFilterMessage(ref key),"Host stole graph Enter for macro commit");
  check("Local color/layer/vertex navigation requests zero DirectX frames and retains graph keyboard activation");
  Reflect.Call(explorer,"RequestPreview");await Wait(delegate{return Convert.ToBoolean(Reflect.Get(explorer,"previewAccepted"));});
  string before=Hash(wb);Reflect.Call(explorer,"ApplyPreview");await Idle(wb);Assert(Hash(wb)==before,"Explorer filter changed labels");
  var rules=LocalApi.Array(LocalApi.AsDict(State(wb)["prefs"])["rules"]);Assert(Convert.ToString(LocalApi.AsDict(rules[0])["expr"])=="color(C17)","Explorer did not apply reviewed canonical expression");
  Match(wb,api);check("Explorer previews authoritative counts before applying its canonical color filter");
  string ruleText=api.Json(rules);await Apply(wb,"layer(C0,L3)");Assert(api.Json(LocalApi.AsDict(State(wb)["prefs"])["rules"])==ruleText&&Hash(wb)==before,"Invalid layer changed filter or state");
  ((ComboBox)Reflect.Get(wb,"presets")).SelectedItem="Selected center layer";Reflect.Call(wb,"LoadPreset");await Task.Delay(150);
  Assert(((TextBox)Reflect.Get(wb,"filter")).Text=="layer(C17,L3)"&&api.Json(LocalApi.AsDict(State(wb)["prefs"])["rules"])==ruleText,"Parameterized preset applied before review");
  Reflect.Call(wb,"ApplyFilter");await Idle(wb);check("Invalid canonical IDs roll back and parameterized layer presets remain unapplied until reviewed");
  var hide=(CheckBox)Reflect.Get(wb,"hideFrame");hide.Checked=false;await Idle(wb);Assert(!renderer.Subset.HideFramework,"Frame menu/checkbox did not show frame");hide.Checked=true;await Idle(wb);Assert(renderer.Subset.HideFramework,"Frame checkbox did not hide frame");
  await Apply(wb,"active");
  ((TextBox)Reflect.Get(wb,"macroA")).Text="H0";((TextBox)Reflect.Get(wb,"macroB")).Text="T1";Reflect.Call(wb,"ComposeMacro","commutator");await Idle(wb);Assert(State(wb)["pending"]!=null&&Hash(wb)==initial,"Macro preview changed state or failed");
  Reflect.Call(wb,"Commit");await Idle(wb);string changed=Hash(wb);Assert(changed!=initial,"Macro did not commit");await Command(wb,"undo");Assert(Hash(wb)==initial,"Undo failed");await Command(wb,"redo");Assert(Hash(wb)==changed,"Redo failed");
  Reflect.Call(wb,"SaveCheckpoint");await Idle(wb);var checkpoints=LocalApi.Array(State(wb)["checkpoints"]);string name=Convert.ToString(LocalApi.AsDict(checkpoints[0])["name"]);
  Reflect.Call(wb,"ResetPuzzle");await Idle(wb);Assert(Hash(wb)==initial,"Reset failed");Reflect.Call(wb,"RestoreCheckpoint",name);await Idle(wb);Assert(Hash(wb)==changed,"Checkpoint failed to restore complete state");
  check("Native frame controls, macro witness preview/commit, undo/redo, reset and checkpoint restore retain exact state");
  ((ComboBox)Reflect.Get(wb,"orbit")).SelectedIndex=33;await Idle(wb);((CheckBox)Reflect.Get(wb,"autoTarget")).Checked=true;
  Reflect.Call(wb,"Analyze");await Idle(wb);string buffer=((TextBox)Reflect.Get(wb,"bufferText")).Text;
  Assert(buffer.Contains("Destination P")&&buffer.Contains("Required identity")&&buffer.Contains("Buffer A"),"Buffer analyzer lacks readable identity/location/cost summary");
  Reflect.Call(wb,"Suggest");await Idle(wb);Assert(State(wb)["pending"]!=null||((ToolStripStatusLabel)Reflect.Get(wb,"message")).Text.Contains("solved"),"Insertion neither previews nor explains completion");
  Reflect.Call(wb,"Cancel");await Idle(wb);Reflect.Call(wb,"ReportSession");await Idle(wb);
  Assert(((TextBox)Reflect.Get(wb,"sessionReport")).Text.Contains("Solution:")&&((ListView)Reflect.Get(wb,"progress")).Items.Count==35,"Progress/session summaries failed");
  check("Buffer analysis, certified insertion preview and all35 orbit/session reports remain available");
  Reflect.Call(wb,"ResetPuzzle");await Idle(wb);Assert(Hash(wb)==initial,"Focused test failed to recover solved root");Match(wb,api);
  // Controlled device-loss exception exercises recovery without changing OS settings.
  Exception lost=null;var type=Type.GetType("Microsoft.DirectX.Direct3D.DeviceLostException, Microsoft.DirectX.Direct3D",true);
  foreach(var constructor in type.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)){
   var parameters=constructor.GetParameters();var args=new object[parameters.Length];bool supported=true;
   for(int i=0;i<args.Length;i++){if(parameters[i].ParameterType==typeof(string))args[i]="Focused device interruption";else if(parameters[i].ParameterType==typeof(int))args[i]=unchecked((int)0x88760868);else if(parameters[i].ParameterType!=typeof(Exception)){supported=false;break;}}
   if(supported){lost=(Exception)constructor.Invoke(args);break;}
  }
  Assert(lost!=null,"No device loss exception fixture");int recoveries=renderer.RecoveryCount;renderer.HoldUpdates=true;Assert(wb.TryRecoverRenderer(lost),"Recovery rejected actual DirectX exception");await Task.Delay(300);Match(wb,api);renderer.HoldUpdates=false;renderer.AssertReady();Assert(renderer.RecoveryCount>recoveries,"Actual DirectX device did not recover through reconnect readiness");Assert(Hash(wb)==initial,"Display recovery changed puzzle");
  check("Actual DirectX device-loss recovery resumes rendering without changing puzzle state");
 }
}
