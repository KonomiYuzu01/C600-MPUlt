// CPU-only production tool logic with real WinForms controls and fixture model.
// Compile together with NativeHostRegression.cs for its FixtureForm definitions.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

internal static class NativeToolRegression {
 static readonly List<object> checks=new List<object>();
 static bool passed=true;
 static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 static void Check(string name,Action action){try{action();checks.Add(new{name=name,passed=true});Console.WriteLine("PASS "+name);}catch(Exception e){passed=false;checks.Add(new{name=name,passed=false,error=e.ToString()});Console.WriteLine("FAIL "+name+" "+e.Message);}}
 static object Call(object value,string name,params object[] args){return Reflect.Call(value,name,args);}
 static T Field<T>(object value,string name){return (T)Reflect.Get(value,name);}
 static Dictionary<string,object> Prefs(object rules){return LocalApi.D("orbit",33,"rules",rules,"pin_safety",false,"protected",new object[0]);}
 static object[] Rules(string expression){return new object[]{LocalApi.D("expr",expression,"style","solid")};}
 [STAThread] static int Main(string[] args){
  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  string output=Path.GetFullPath(args[0]);
  using(var form=new FixtureForm()){
   var wb=new NativeWorkbench(form,"fixture-not-executed","http://127.0.0.1:1","unused-test-token",true);
   var api=new LocalApi("http://127.0.0.1:1","unused");var filter=Field<TextBox>(wb,"filter");var types=Field<CheckedListBox>(wb,"types");var summary=Field<Label>(wb,"filterSummary");var pin=Field<CheckBox>(wb,"pin");
   var state=LocalApi.D("head",7,"transactions",3,"primitives","624752715364","stars",8,"moving_solved",100,"moving_total",176520,"pending",null);
   Reflect.Set(wb,"state",state);
   Check("Applied active orbit and exact selection synchronize independently of unsubmitted text",delegate{
    var prefs=Prefs(Rules("active"));Call(wb,"RefreshToolState",prefs,new byte[]{2,2,0});
    Assert(filter.Text=="active"&&types.GetItemChecked(33)&&types.CheckedItems.Count==1,"Active orbit not reflected");
    Assert(summary.Text.Contains("2 visible stickers")&&summary.Text.Contains("Exact filter"),"Exact summary missing");
    filter.Text="O27";Assert(summary.Text.Contains("Unapplied edits"),"Draft edit was not marked immediately");
    Call(wb,"RefreshToolState",prefs,new byte[]{2,2,0});
    Assert(filter.Text=="O27","Unrelated refresh destroyed draft");
    Assert(types.GetItemChecked(33)&&!types.GetItemChecked(27),"Checklist incorrectly reflects draft instead of actual filter");
    Assert(summary.Text.Contains("Unapplied edits"),"Refresh lost unapplied marker");
    prefs=Prefs(Rules("O27"));Call(wb,"RefreshToolState",prefs,new byte[]{2,0,0});
    Assert(types.GetItemChecked(27)&&types.CheckedItems.Count==1&&!summary.Text.Contains("Unapplied edits"),"Applied edit not synchronized");
   });
   Check("Complex multi-style rules survive load, unrelated refresh, and right-click exclusion",delegate{
    var rules=new object[]{LocalApi.D("expr","O26","style","hide"),LocalApi.D("expr","O26 | O27","style","highlight"),LocalApi.D("expr","everything","style","ghost")};
    var prefs=Prefs(rules);Call(wb,"RefreshToolState",prefs,new byte[]{3,1,0});
    Assert(filter.Text==api.Json(rules),"Complex rules were flattened");
    Assert(types.CheckedItems.Count==0&&summary.Text.Contains("cannot represent"),"Complex rules masquerade as checklist selection");
    Call(wb,"ExcludeOrbit",27);var next=LocalApi.Array(Call(wb,"ReadFilterRules"));Assert(next.Length==3,"Exclusion dropped rules");
    for(int i=0;i<3;i++){var rule=LocalApi.AsDict(next[i]);var old=LocalApi.AsDict(rules[i]);Assert(Convert.ToString(rule["style"])==Convert.ToString(old["style"]),"Exclusion changed style");Assert(Convert.ToString(rule["expr"])=="("+old["expr"]+") & !O27","Exclusion did not apply to every rule");}
    Call(wb,"RefreshToolState",prefs,new byte[]{3,1,0});Assert(api.Json(Call(wb,"ReadFilterRules"))==api.Json(next),"Unrelated refresh discarded complex draft");
   });
   Check("Saved pin state and empty selection are explained without inventing selected orbit rows",delegate{
    var prefs=Prefs(Rules("nothing"));pin.Checked=true;prefs["pin_safety"]=true;Call(wb,"RefreshToolState",prefs,new byte[]{4,5,6});
    Assert(types.CheckedItems.Count==0&&summary.Text.Contains("Extra pinned context is ON"),"Pinned exceptions unclear");
    pin.Checked=false;prefs["pin_safety"]=false;Call(wb,"RefreshToolState",prefs,new byte[]{0,0,0});Assert(summary.Text.Contains("0 visible stickers")&&summary.Text.Contains("Exact filter"),"Empty exact selection unclear");
    var union=Prefs(Rules("O00 | O27 | O34"));Call(wb,"RefreshToolState",union,new byte[]{2});Assert(types.CheckedItems.Count==3&&types.GetItemChecked(0)&&types.GetItemChecked(27)&&types.GetItemChecked(34),"Orbit union not reflected");
   });
   Check("Malformed JSON filter input is contained in the tool status",delegate{
    filter.Text="[broken";Call(wb,"ApplyFilter");Assert(Field<ToolStripStatusLabel>(wb,"message").Text.StartsWith("Invalid filter:"),"Filter parse exception escaped");
   });
   Check("Macro H/T aliases, signed primitives, inverse suffix, whitespace, and boundaries preserve execution order",delegate{
    var recipe=LocalApi.Array(api.Parse(api.Json(Call(wb,"ParseWordRecipe","H0 T599' -1200, 27'\r\nh13\tt0"))));var moves=LocalApi.Array(LocalApi.AsDict(recipe[0])["moves"]);int[] expected={1,-1200,-1200,-27,27,2};Assert(moves.Length==expected.Length,"Word length changed");for(int i=0;i<moves.Length;i++)Assert(Convert.ToInt32(moves[i])==expected[i],"Word order or sign changed");
    foreach(string bad in new[]{"0","1201","-1201","H600","T-1","H","T1''","a","2147483648"}){bool rejected=false;try{Call(wb,"ParseWordRecipe",bad);}catch(TargetInvocationException e){rejected=e.InnerException is FormatException||e.InnerException is OverflowException;}Assert(rejected,"Malformed macro token accepted: "+bad);}
   });
   Check("Certified preview capture retains star recipes without reducing them to a target effect",delegate{
    var recipe=new object[]{LocalApi.D("kind","star","orbit",33,"node",40,"sign",1)};state["pending"]=LocalApi.D("note","full collateral","recipe",recipe);Call(wb,"UsePreviewAsMacroA");Assert(Object.ReferenceEquals(Reflect.Get(wb,"macroARecipe"),recipe),"Certified recipe was flattened or copied incorrectly");Assert(Field<TextBox>(wb,"macroA").Text.Contains("full collateral"),"Certified caption missing");state["pending"]=null;
   });
   Check("Piece and insertion numeric ranges cover all 177120 model positions",delegate{
    var piece=Field<NumericUpDown>(wb,"piecePosition");var target=Field<NumericUpDown>(wb,"target");Assert(piece.Minimum==0&&piece.Maximum==177119&&target.Minimum==0&&target.Maximum==177119,"Position range excludes valid final position");piece.Value=177119;Call(wb,"UsePieceDestination");Assert(target.Value==177119&&!Field<CheckBox>(wb,"autoTarget").Checked,"Selected destination not retained");
   });
   Check("Session report preserves large witnessed totals and labels timer as thinking time",delegate{
    var report=LocalApi.D("scramble_primitives","1000","solution_primitives","624752714364","stars",8,"operations",3,"assisted_transactions",2,"timer",LocalApi.D("seconds",123.45,"running",false));Call(wb,"DisplaySessionReport",report);string text=Field<TextBox>(wb,"sessionReport").Text;Assert(text.Contains("624752714364")&&text.Contains("1000")&&text.Contains("paused")&&text.Contains("thinking time"),"Report loses precision or scope");
   });
   Application.RemoveMessageFilter(wb);
  }
  Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,new JavaScriptSerializer().Serialize(new{passed=passed,scope="CPU-only production tools with real unshown WinForms controls and fixture model; no native MPUlt or GPU execution",checks=checks,os=Environment.OSVersion.ToString(),clr=Environment.Version.ToString()}));
  return passed?0:1;
 }
}
