// Fixture test for renderer scope isolation; this is not a DirectX performance test.
using System;
using System.Threading;
using System.Windows.Forms;

internal static class NativeRenderSubsetRegression {
 sealed class Basis {internal int NV=4,NF=4,NE=6;}
 sealed class Mesh { internal int Id;internal Basis Base=new Basis(); }
 sealed class Puzzle { internal short[] Field; }
 sealed class CubeFixture { internal Mesh[] Stks;internal int NStk;internal Puzzle Cube;internal Mesh[] StFaces=new Mesh[600];internal int NF=600; }
 static Mesh[] originalFaces;
 static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
 [STAThread] static int Main(){try {
  using(var viewport=new Control()) {
   var field=new short[259800];var meshes=new Mesh[259800];
   for(int i=0;i<field.Length;i++){field[i]=(short)(i%600);meshes[i]=new Mesh{Id=i};}
   field[12345]=unchecked((short)0x8123);
   var puzzle=new Puzzle{Field=field};var cube=new CubeFixture{Stks=meshes,NStk=meshes.Length,Cube=puzzle};
   for(int i=0;i<cube.StFaces.Length;i++)cube.StFaces[i]=new Mesh{Id=i};originalFaces=cube.StFaces;
   var subset=new NativeRenderSubset(viewport,cube,puzzle);var styles=new byte[field.Length];
   Require(subset.HideFramework&&!subset.FrameworkVisible,"Framework should be hidden by default independently of filter mode");
   styles[0]=2;styles[12345]=6;styles[259799]=3;subset.Update(styles);
   Require(subset.VisibleCount==3,"Visible subset count mismatch");
   lock(viewport)using(subset.Enter()) {
    Require(cube.NStk==3&&cube.Stks.Length==3,"Subset arrays not applied");
    Require(Object.ReferenceEquals(cube.Stks[1],meshes[12345]),"Mesh identity was not retained");
    Require(!Object.ReferenceEquals(cube.Cube,puzzle)&&!Object.ReferenceEquals(cube.Cube.Field,field),"Render field aliases the puzzle");
    Require(cube.Cube.Field[1]==field[12345],"Selected outline bits mapped to the wrong sticker");
    Require(Object.ReferenceEquals(puzzle.Field,field)&&puzzle.Field.Length==259800,"Authoritative field changed during render");
    Require(!subset.FrameworkVisible&&cube.NF==600,"Filtered framework changed native clipping-plane count");
    foreach(var face in cube.StFaces)Require(face.Base.NV==0&&face.Base.NF==0&&face.Base.NE==0,"Filtered framework remains drawable or pickable");
    foreach(var face in originalFaces)Require(face.Base.NV==4&&face.Base.NF==4&&face.Base.NE==6,"Framework suppression mutated original meshes");
   }
   CheckRestored(cube,puzzle,meshes,field,subset);
   using(subset.EnterNativeDraw()) {
    Require(Monitor.IsEntered(viewport)&&cube.NStk==3&&cube.StFaces[0].Base.NE==0,"Unwrapped native draw bypassed the visibility or framework policy");
    using(subset.EnterNativeDraw())Require(cube.NStk==3&&subset.IsEntered,"Nested native draw replaced the active render scope");
   }
   Require(!Monitor.IsEntered(viewport),"Native draw leaked the viewport lock");CheckRestored(cube,puzzle,meshes,field,subset);
   subset.HideFramework=false;
   lock(viewport)using(subset.Enter())Require(subset.FrameworkVisible&&cube.NStk==3&&Object.ReferenceEquals(cube.StFaces,originalFaces),"Explicit framework show depends on filter mode");
   CheckRestored(cube,puzzle,meshes,field,subset);subset.HideFramework=true;
   var pickFaces=(Mesh[])originalFaces.Clone();cube.StFaces=pickFaces;
   lock(viewport)using(subset.Enter())Require(!Object.ReferenceEquals(cube.StFaces,pickFaces),"Render did not hide an active native picking face view");
   Require(Object.ReferenceEquals(cube.StFaces,pickFaces),"Render did not restore the active native picking face view");cube.StFaces=originalFaces;
   lock(viewport)using(subset.EnterFramework())Require(Object.ReferenceEquals(cube.Stks,meshes)&&cube.NStk==259800&&cube.StFaces[0].Base.NE==0,"Reference framework scope changed sticker geometry or retained framework");
   CheckRestored(cube,puzzle,meshes,field,subset);
   try {lock(viewport)using(subset.Enter()){throw new InvalidOperationException("intentional render failure");}}
   catch(InvalidOperationException e){Require(e.Message=="intentional render failure","Unexpected scope exception");}
   CheckRestored(cube,puzzle,meshes,field,subset);
   field[12345]=unchecked((short)0x8234);
   lock(viewport)using(subset.Enter())Require(cube.Cube.Field[1]==field[12345],"Selection bits were cached across frames");
   CheckRestored(cube,puzzle,meshes,field,subset);
   subset.Update(new byte[field.Length]);
   lock(viewport)using(subset.Enter())Require(cube.NStk==0&&cube.Stks.Length==0,"Empty filter did not draw an empty subset");
   CheckRestored(cube,puzzle,meshes,field,subset);
   for(int i=0;i<styles.Length;i++)styles[i]=2;subset.Update(styles);
   lock(viewport)using(subset.Enter())Require(Object.ReferenceEquals(cube.Stks,meshes)&&Object.ReferenceEquals(cube.Cube,puzzle)&&cube.StFaces[0].Base.NE==0,"All-visible view changed the full puzzle or ignored explicit framework hide");
   CheckRestored(cube,puzzle,meshes,field,subset);
   var fullFaces=cube.StFaces;
   int motionLimit=NativeRenderSubset.MotionLimit;
   Require(subset.MotionCount==motionLimit,"Motion detail limit mismatch");
   int chosen=(int)((long)111*(field.Length-1)/(NativeRenderSubset.MotionLimit-1));
   field[chosen]=unchecked((short)0x8345);
   lock(viewport)using(subset.Enter(true)) {
    Require(cube.NStk==motionLimit&&cube.Stks.Length==motionLimit&&subset.LastDrawCount==motionLimit,"Motion draw exceeded its limit");
    Require(cube.Stks[0].Id==0&&cube.Stks[motionLimit-1].Id==259799&&cube.Stks[motionLimit/2].Id>129000,"Motion subset did not cover the whole visible scene");
    for(int i=1;i<cube.Stks.Length;i++)Require(cube.Stks[i].Id>cube.Stks[i-1].Id,"Motion subset contains duplicates or changed order");
    Require(Object.ReferenceEquals(cube.Stks[111],meshes[chosen])&&cube.Cube.Field[111]==field[chosen],"Motion mesh identity or outline mapping changed");
    Require(!Object.ReferenceEquals(cube.StFaces,fullFaces)&&cube.StFaces.Length==600&&cube.StFaces[0].Base.NE==0,"Motion detail ignored explicit framework hide");
    using(subset.EnterNativeDraw())Require(cube.NStk==motionLimit,"Native draw guard replaced the active motion scope");
    Require(Object.ReferenceEquals(puzzle.Field,field)&&puzzle.Field.Length==259800,"Motion detail changed authoritative state");
   }
   CheckRestored(cube,puzzle,meshes,field,subset);
   subset.HideFramework=false;cube.StFaces=pickFaces;
   using(subset.EnterNativeDraw())Require(Object.ReferenceEquals(cube.StFaces,originalFaces),"Explicit show used the restricted picking face view during a native draw");
   Require(Object.ReferenceEquals(cube.StFaces,pickFaces),"Universal draw did not restore the picking face view");cube.StFaces=originalFaces;subset.HideFramework=true;
   try{using(subset.EnterNativeDraw()){throw new InvalidOperationException("intentional unwrapped failure");}}catch(InvalidOperationException e){Require(e.Message=="intentional unwrapped failure","Unexpected unwrapped scope exception");}
   Require(!Monitor.IsEntered(viewport),"Failed native draw leaked the viewport lock");CheckRestored(cube,puzzle,meshes,field,subset);
   try {lock(viewport)using(subset.Enter(true)){throw new InvalidOperationException("intentional motion failure");}}
   catch(InvalidOperationException e){Require(e.Message=="intentional motion failure","Unexpected motion scope exception");}
   CheckRestored(cube,puzzle,meshes,field,subset);
   lock(viewport)using(subset.Enter())Require(Object.ReferenceEquals(cube.Stks,meshes)&&cube.NStk==259800&&subset.LastDrawCount==259800,"Full detail did not return after motion");
   styles=new byte[field.Length];for(int i=0;i<10000;i++)styles[i*20]=2;subset.Update(styles);
   lock(viewport)using(subset.Enter(true)) {
    Require(cube.NStk==motionLimit&&cube.Stks[motionLimit-1].Id==199980,"Filtered motion coverage mismatch");
    foreach(var mesh in cube.Stks)Require(mesh.Id%20==0&&Object.ReferenceEquals(mesh,meshes[mesh.Id]),"Motion detail revealed a filtered sticker");
   }
   CheckRestored(cube,puzzle,meshes,field,subset);
   lock(viewport)using(subset.Enter())Require(cube.NStk==10000,"Normal filtered detail was reduced");
   CheckRestored(cube,puzzle,meshes,field,subset);
   foreach(int filteredCount in new[]{851,2400,NativeRenderSubset.MotionThreshold}) {
    styles=new byte[field.Length];for(int i=0;i<filteredCount;i++)styles[i]=2;subset.Update(styles);
    Require(subset.MotionCount==filteredCount,"Small filtered view was marked for motion sampling");
    lock(viewport)using(subset.Enter(true))Require(cube.NStk==filteredCount&&subset.LastDrawCount==filteredCount,"Motion reduced a view below the activation threshold");
    CheckRestored(cube,puzzle,meshes,field,subset);
   }
   bool rejected=false;try{subset.Update(new byte[3]);}catch(ArgumentException){rejected=true;}
   Require(rejected,"Invalid style length was accepted");
  }
  Console.WriteLine("PASS: fixture render subset identity, outline bits, framework omission/clipping/restore, picking scope nesting, exception restoration, empty/full filters, motion coverage/limit and validation");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
 static void CheckRestored(CubeFixture cube,Puzzle puzzle,Mesh[] meshes,short[] field,NativeRenderSubset subset){
  Require(Object.ReferenceEquals(cube.Stks,meshes)&&cube.NStk==259800&&Object.ReferenceEquals(cube.Cube,puzzle),"Full native geometry was not restored");
  Require(Object.ReferenceEquals(puzzle.Field,field)&&field.Length==259800&&!subset.IsEntered,"Full native field or scope state changed");
  Require(Object.ReferenceEquals(cube.StFaces,originalFaces)&&cube.NF==600,"Native framework array or clipping-plane count was not restored");
 }
}
