// Draw only the visible meshes while retaining the complete native puzzle for
// input, colors, twists, hit testing and the authoritative journal bridge.
// Verified against the shipped MPUlt CubeObj.Render IL: its only slot-indexed
// read outside Stks is Cube.Field[index], used for the selected outline bit.
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

internal sealed class NativeRenderSubset {
 static readonly ConditionalWeakTable<object,object> focusMeshes=new ConditionalWeakTable<object,object>();
 internal static int FrameworkColor(object mesh){object marker;return focusMeshes.TryGetValue(mesh,out marker)?unchecked((int)0xFF29E3DE):-8355712;}
 readonly Control viewport;
 readonly object cube,puzzle,renderPuzzle;
 readonly Array fullStickers;
 readonly int fullCount,ownerThread;
 readonly FieldInfo stickersField,countField,puzzleField,colorsField,facesField;
 readonly Array fullFaces,emptyFaces,focusFullFaces,focusOnlyFaces;
 int focusedCell=-1;
 object savedFaces;
 byte[] visibility;
 Array drawStickers;
 int[] drawIndices;
 short[] drawColors;
 Array motionStickers;
 int[] motionIndices;
 short[] motionColors;
 bool entered,hideFramework=true;
 int referenceDepth;

 internal const int MotionLimit=1500;
 internal const int MotionThreshold=6000;
 internal int VisibleCount { get; private set; }
 internal int MotionCount { get { return VisibleCount>MotionThreshold?MotionLimit:VisibleCount; } }
 internal int LastDrawCount { get; private set; }
 internal int FullCount { get { return fullCount; } }
 internal bool IsEntered { get { return entered; } }
 internal bool FrameworkVisible { get { return !hideFramework; } }
 internal int FocusedCell {
  get{return focusedCell;}
  set{
   CheckThread();if(entered||referenceDepth>0)throw new InvalidOperationException("Cannot change cell focus during a native draw.");
   if(value< -1||value>=fullFaces.Length)throw new ArgumentOutOfRangeException("value");if(value==focusedCell)return;
   object focused=null;
   if(value>=0){focused=Reflect.Clone(fullFaces.GetValue(value));var projected=Reflect.Get(focused,"Coords3D") as Array;if(projected!=null)Reflect.Set(focused,"Coords3D",projected.Clone());focusMeshes.Add(focused,new object());}
   if(focusedCell>=0){focusFullFaces.SetValue(fullFaces.GetValue(focusedCell),focusedCell);focusOnlyFaces.SetValue(emptyFaces.GetValue(focusedCell),focusedCell);}
   focusedCell=value;
   if(value>=0){focusFullFaces.SetValue(focused,value);focusOnlyFaces.SetValue(focused,value);}
  }
 }
 Array DisplayFaces {get{return focusedCell<0?(hideFramework?emptyFaces:fullFaces):(hideFramework?focusOnlyFaces:focusFullFaces);}}
 internal bool HideFramework {
  get { return hideFramework; }
  set { CheckThread();if(entered||referenceDepth>0)throw new InvalidOperationException("Cannot change the framework policy during a native render.");hideFramework=value; }
 }

 internal NativeRenderSubset(Control viewport,object cube,object puzzle) {
  this.viewport=viewport;this.cube=cube;this.puzzle=puzzle;
  ownerThread=Thread.CurrentThread.ManagedThreadId;
  stickersField=RequireField(cube,"Stks");countField=RequireField(cube,"NStk");
  puzzleField=RequireField(cube,"Cube");colorsField=RequireField(puzzle,"Field");
  facesField=RequireField(cube,"StFaces");
  fullFaces=(Array)facesField.GetValue(cube);emptyFaces=EmptyFaces(fullFaces);
  focusFullFaces=(Array)fullFaces.Clone();focusOnlyFaces=(Array)emptyFaces.Clone();
  fullStickers=(Array)stickersField.GetValue(cube);
  fullCount=Convert.ToInt32(countField.GetValue(cube));
  if(fullStickers==null||fullStickers.Length!=fullCount||fullCount!=259800||
     !Object.ReferenceEquals(puzzleField.GetValue(cube),puzzle)||
     ((short[])colorsField.GetValue(puzzle)).Length!=fullCount)
   throw new InvalidOperationException("The render subset requires the complete 259,800-slot native puzzle.");
  renderPuzzle=Reflect.Clone(puzzle);
  // The shallow clone owns a separate Field from construction onward. Other
  // cloned members are never used or modified by the renderer.
  colorsField.SetValue(renderPuzzle,new short[0]);
  drawStickers=fullStickers;VisibleCount=fullCount;BuildMotionSubset();
 }

 // Keep NF/FPoles/BPln intact: CubeObj.Render uses those to clip stickers as
 // well as framework faces. Empty renderer-only meshes suppress framework
 // lines without changing the native cell numbering or clipping planes.
 internal static Array EmptyFaces(Array originals) {
  if(originals==null)throw new ArgumentNullException("originals");
  var result=Array.CreateInstance(originals.GetType().GetElementType(),originals.Length);
  for(int i=0;i<originals.Length;i++) {
   object mesh=Reflect.Clone(originals.GetValue(i));
   object basis=Reflect.Clone(Reflect.Get(mesh,"Base"));
   Reflect.Set(basis,"NV",0);Reflect.Set(basis,"NF",0);Reflect.Set(basis,"NE",0);
   Reflect.Set(mesh,"Base",basis);result.SetValue(mesh,i);
  }
  return result;
 }

 // Full-array reference rendering must apply the same framework policy. This
 // changes only StFaces; it is also safe inside the guarded native click scope.
 internal IDisposable EnterFramework() {
  CheckThread();
  if(!Monitor.IsEntered(viewport))throw new InvalidOperationException("Lock the native viewport before rendering.");
  object previous=facesField.GetValue(cube);var desired=DisplayFaces;
  facesField.SetValue(cube,desired);
  referenceDepth++;
  return new FaceScope(this,previous);
 }

 // Every native scene-object draw must use this guard, including original
 // ForceUpdate and animation redraws outside our lifecycle. A lifecycle draw
 // already owns its selected motion/detail scope; do not replace that scope.
 // Monitor is reentrant, and this synchronous scope never pumps UI messages.
 internal IDisposable EnterNativeDraw() {
  CheckThread();
  if(entered||referenceDepth>0)return EmptyScope.Instance;
  Monitor.Enter(viewport);
  try { return new NativeDrawScope(viewport,Enter()); }
  catch { Monitor.Exit(viewport);throw; }
 }

 static FieldInfo RequireField(object value,string name) {
  var field=Reflect.Field(value.GetType(),name);
  if(field==null)throw new MissingFieldException(value.GetType().FullName,name);
  return field;
 }
 void CheckThread() {
  if(Thread.CurrentThread.ManagedThreadId!=ownerThread)
   throw new InvalidOperationException("The native render subset must remain on its viewport UI thread.");
 }

 internal void Update(byte[] styles) {
  CheckThread();
  if(entered)throw new InvalidOperationException("Cannot change visibility during a native render.");
  if(styles==null||styles.Length!=fullCount)throw new ArgumentException("Native style length mismatch.","styles");
  int count=0;bool changed=visibility==null;
  for(int i=0;i<styles.Length;i++) {
   if(styles[i]>6)throw new ArgumentException("Native style outside 0..6.","styles");
   byte shown=(byte)(styles[i]==0?0:1);count+=shown;
   if(visibility!=null&&visibility[i]!=shown)changed=true;
  }
  if(!changed)return;
  var nextVisibility=new byte[fullCount];
  for(int i=0;i<styles.Length;i++)nextVisibility[i]=(byte)(styles[i]==0?0:1);
  Array nextStickers=fullStickers;int[] nextIndices=null;short[] nextColors=null;
  if(count!=fullCount) {
   nextStickers=Array.CreateInstance(fullStickers.GetType().GetElementType(),count);
   nextIndices=new int[count];nextColors=new short[count];
   for(int i=0,j=0;i<styles.Length;i++)if(styles[i]!=0) {
    nextIndices[j]=i;nextStickers.SetValue(fullStickers.GetValue(i),j);j++;
   }
  }
  visibility=nextVisibility;drawStickers=nextStickers;
  drawIndices=nextIndices;drawColors=nextColors;VisibleCount=count;
  BuildMotionSubset();
 }

 void BuildMotionSubset() {
  motionStickers=null;motionIndices=null;motionColors=null;
  if(VisibleCount<=MotionThreshold)return;
  motionStickers=Array.CreateInstance(fullStickers.GetType().GetElementType(),MotionLimit);
  motionIndices=new int[MotionLimit];motionColors=new short[MotionLimit];
  for(int i=0;i<MotionLimit;i++) {
   // Include both endpoints and sample throughout the complete visible order,
   // keeping spatial coverage instead of retaining only an initial cell range.
   int rank=(int)((long)i*(VisibleCount-1)/(MotionLimit-1));
   int nativeIndex=drawIndices==null?rank:drawIndices[rank];
   motionIndices[i]=nativeIndex;motionStickers.SetValue(fullStickers.GetValue(nativeIndex),i);
  }
 }

 // Call only around the inspected synchronous scene.Render under lock(viewport).
 // Perform device readiness/reset outside this scope: original ForceUpdate can
 // show a modal reset-error dialog. Never retain the scope across DoEvents,
 // native mouse handlers, or another operation that pumps UI messages.
 internal IDisposable Enter() { return Enter(false); }
 // Motion detail is temporary display sampling only. All chosen meshes retain
 // their original indices in mechanics. The framework follows its own policy.
 internal IDisposable Enter(bool motion) {
  CheckThread();
  if(!Monitor.IsEntered(viewport))throw new InvalidOperationException("Lock the native viewport before rendering a subset.");
  if(entered)throw new InvalidOperationException("A native render subset cannot be nested.");
  if(!Object.ReferenceEquals(stickersField.GetValue(cube),fullStickers)||
     Convert.ToInt32(countField.GetValue(cube))!=fullCount||
     !Object.ReferenceEquals(puzzleField.GetValue(cube),puzzle))
   throw new InvalidOperationException("The native full geometry was not restored before rendering.");
  bool useMotion=motion&&VisibleCount>MotionThreshold;
  var indices=useMotion?motionIndices:drawIndices;
  var stickers=useMotion?motionStickers:drawStickers;
  var colors=useMotion?motionColors:drawColors;
  LastDrawCount=indices==null?fullCount:indices.Length;
  if(indices!=null) {
   var fullColors=(short[])colorsField.GetValue(puzzle);
   if(fullColors.Length!=fullCount)throw new InvalidOperationException("The full native field changed size.");
   for(int i=0;i<indices.Length;i++)colors[i]=fullColors[indices[i]];
   colorsField.SetValue(renderPuzzle,colors);
  }
  entered=true;
  try {
   savedFaces=facesField.GetValue(cube);
   facesField.SetValue(cube,DisplayFaces);
   if(indices!=null) {
    stickersField.SetValue(cube,stickers);
    countField.SetValue(cube,indices.Length);
    puzzleField.SetValue(cube,renderPuzzle);
   }
   return new Scope(this);
  } catch {
   Restore();throw;
  }
 }

 void Restore() {
  // Restore all three members even if a future runtime rejects one assignment.
  // The original Puzzle.Field is never assigned to, even transiently.
  try { puzzleField.SetValue(cube,puzzle); }
  finally {
   try { countField.SetValue(cube,fullCount); }
   finally {
    try { stickersField.SetValue(cube,fullStickers); }
    finally { try { facesField.SetValue(cube,savedFaces); } finally { savedFaces=null;entered=false; } }
   }
  }
 }
 sealed class FaceScope : IDisposable {
  NativeRenderSubset owner;readonly object previous;
  internal FaceScope(NativeRenderSubset owner,object previous){this.owner=owner;this.previous=previous;}
  public void Dispose(){var current=owner;if(current==null)return;owner=null;try{current.facesField.SetValue(current.cube,previous);}finally{current.referenceDepth--;}}
 }
 sealed class NativeDrawScope : IDisposable {
  readonly object viewport;IDisposable inner;
  internal NativeDrawScope(object viewport,IDisposable inner){this.viewport=viewport;this.inner=inner;}
  public void Dispose(){var current=inner;if(current==null)return;inner=null;try{current.Dispose();}finally{Monitor.Exit(viewport);}}
 }
 sealed class Scope : IDisposable {
  NativeRenderSubset owner;
  internal Scope(NativeRenderSubset owner){this.owner=owner;}
  public void Dispose(){var current=owner;if(current==null)return;owner=null;current.Restore();}
 }
 sealed class EmptyScope : IDisposable {
  internal static readonly EmptyScope Instance=new EmptyScope();
  public void Dispose(){}
 }
}
