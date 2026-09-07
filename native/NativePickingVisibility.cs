// Gate the original HWND mouse-up before MPUlt's ProcessClick can change state.
// Native FindFace otherwise hits the complete cell framework, including hidden
// cells, and its empty-click branch can repeat the previously selected axis.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;

internal sealed class NativePickingVisibility : NativeWindow,IDisposable {
 delegate bool ScreenRay(int x,int y,ref double rayX,ref double rayY);
 readonly Control viewport;readonly Form form;readonly object cube,puzzle,camera;
 readonly Func<bool> preparePicking,allowed;
 readonly object[] stickers;readonly Array originalFaces,emptyFaces,restrictedFaces;
 readonly FieldInfo facesField;
 readonly Func<object,double,double,double,double> checkRay;
 readonly Func<object,int> faceIndex,vertices,triangles;
 readonly Func<object,object> getBase;
 readonly ScreenRay screenRay;
 int[] visible=new int[0];int restrictedFace=-1;
 bool disposed,dispatching,suppressCapture;
 internal bool IsDispatching {get{return dispatching;}}
 internal bool SuppressCapture {get{return suppressCapture;}}
 internal int LastHit {get;private set;}
 internal int LastFace {get;private set;}
 internal int BlockedClicks {get;private set;}
 internal int AcceptedClicks {get;private set;}

 internal NativePickingVisibility(Control viewport,Form form,object cube,object puzzle,Func<bool> preparePicking,Func<bool> allowed){
  this.viewport=viewport;this.form=form;this.cube=cube;this.puzzle=puzzle;
  this.preparePicking=preparePicking;this.allowed=allowed;LastHit=LastFace=-1;
  var full=(Array)Reflect.Get(cube,"Stks");
  if(full.Length!=259800||Convert.ToInt32(Reflect.Get(cube,"NStk"))!=full.Length)throw new InvalidOperationException("Picking requires the complete native geometry.");
  stickers=new object[full.Length];for(int i=0;i<full.Length;i++)stickers[i]=full.GetValue(i);
  originalFaces=(Array)Reflect.Get(cube,"StFaces");emptyFaces=NativeRenderSubset.EmptyFaces(originalFaces);restrictedFaces=(Array)emptyFaces.Clone();
  facesField=Reflect.Field(cube.GetType(),"StFaces");
  var scene=Reflect.Property(viewport,"Scene");camera=Reflect.Property(scene,"Camera");
  screenRay=(ScreenRay)Delegate.CreateDelegate(typeof(ScreenRay),scene,scene.GetType().GetMethod("DirAndOrig",Reflect.Flags));
  Type type=full.GetType().GetElementType();var value=Expression.Parameter(typeof(object),"mesh");var item=Expression.Convert(value,type);
  var basis=Expression.Field(item,Reflect.Field(type,"Base"));
  getBase=Expression.Lambda<Func<object,object>>(Expression.Convert(basis,typeof(object)),value).Compile();
  faceIndex=Expression.Lambda<Func<object,int>>(Expression.Field(item,Reflect.Field(type,"NFace")),value).Compile();
  vertices=IntField(basis.Type,"NV");triangles=IntField(basis.Type,"NF");
  var x=Expression.Parameter(typeof(double),"x");var y=Expression.Parameter(typeof(double),"y");var z=Expression.Parameter(typeof(double),"nearest");
  checkRay=Expression.Lambda<Func<object,double,double,double,double>>(Expression.Call(item,type.GetMethod("CheckRay",Reflect.Flags),x,y,z),value,x,y,z).Compile();
  viewport.HandleCreated+=HandleCreated;viewport.HandleDestroyed+=HandleDestroyed;
  if(viewport.IsHandleCreated)AssignHandle(viewport.Handle);
 }
 static Func<object,int> IntField(Type type,string name){var value=Expression.Parameter(typeof(object),"value");return Expression.Lambda<Func<object,int>>(Expression.Field(Expression.Convert(value,type),Reflect.Field(type,name)),value).Compile();}
 void HandleCreated(object sender,EventArgs args){if(!disposed)AssignHandle(viewport.Handle);}
 void HandleDestroyed(object sender,EventArgs args){ReleaseHandle();}

 internal void Update(byte[] styles){
  if(dispatching)throw new InvalidOperationException("Cannot change visibility inside native picking.");
  if(styles==null||styles.Length!=stickers.Length)throw new ArgumentException("Native picking style length mismatch.","styles");
  var indices=new List<int>();
  for(int i=0;i<styles.Length;i++){
   if(styles[i]>6)throw new ArgumentException("Native style outside 0..6.","styles");
   if(styles[i]!=0)indices.Add(i);
   // Original FindSticker ignores NV when searching selected pieces. Verify
   // the original callback cannot use a hidden mesh's old projected triangles.
   else if(triangles(getBase(stickers[i]))!=0)throw new InvalidOperationException("Hidden native sticker still has pickable triangles: "+i);
  }
  visible=indices.ToArray();
 }

 // Coordinates are exactly those consumed by the original mkPickObject:
 // scene.DirAndOrig produces X, and mkPickObject negates it before mesh queries.
 // Caller first restores a complete, current projection through PreparePicking.
 internal int FindVisibleSticker(int x,int y,bool selectedOnly){
  if(x<0||y<0||x>=viewport.ClientSize.Width||y>=viewport.ClientSize.Height)return -1;
  double rayX=0,rayY=0;if(!screenRay(x,y,ref rayX,ref rayY))return -1;
  var clipped=(bool[])Reflect.Get(cube,"BPln");var field=(short[])Reflect.Get(puzzle,"Field");
  double nearest=Double.MaxValue;int found=-1;
  foreach(int i in visible){
   object sticker=stickers[i],basis=getBase(sticker);
   if(vertices(basis)<=0||triangles(basis)<=0||clipped[faceIndex(sticker)]||(selectedOnly&&(field[i]&0x8000)==0))continue;
   double depth=checkRay(sticker,-rayX,rayY,nearest);
   if(!Double.IsNaN(depth)&&depth<nearest){nearest=depth;found=i;}
  }
  return found;
 }
 bool IsClick(int x,int y){
  double dx=Convert.ToInt32(Reflect.Get(form,"ClickX"))-x,dy=Convert.ToInt32(Reflect.Get(form,"ClickY"))-y;
  return Convert.ToDouble(Reflect.Get(form,"cpath"))+Math.Sqrt(dx*dx+dy*dy)<=3;
 }
 static bool ButtonMessage(int message){return message>=0x201&&message<=0x209;}
 protected override void WndProc(ref Message message){
  if(disposed){base.WndProc(ref message);return;}
  // StartAnimation can pump Windows messages when animated turns are enabled.
  // Reject nested button input before it changes the original press bookkeeping.
  if(dispatching&&ButtonMessage(message.Msg)){message.Result=IntPtr.Zero;return;}
  bool release=message.Msg==0x202||message.Msg==0x205||message.Msg==0x208;
  if(!release){base.WndProc(ref message);return;}
  int x=(short)((long)message.LParam&65535),y=(short)(((long)message.LParam>>16)&65535);
  // Camera drags retain the complete original down/move/up handling.
  if(!IsClick(x,y)){
   // A double-click MouseUp can carry Clicks=2 and bypass MPUlt's drag test.
   // Keep its release bookkeeping, but never reinterpret a drag as a twist.
   bool skipped=Convert.ToBoolean(Reflect.Get(form,"qSkipClick"));Reflect.Set(form,"qSkipClick",true);suppressCapture=true;
   try{base.WndProc(ref message);}finally{suppressCapture=false;if(!skipped)Reflect.Set(form,"qSkipClick",false);}return;
  }
  if(Convert.ToBoolean(Reflect.Get(form,"qSkipClick"))){suppressCapture=true;try{base.WndProc(ref message);}finally{suppressCapture=false;}return;}
  bool primary=message.Msg==0x202||message.Msg==0x205;
  bool selectedOnly=primary&&Control.ModifierKeys==Keys.None&&Convert.ToInt32(Reflect.Get(form,"m_status"))==1;
  int hit=-1;
  if(allowed()&&Convert.ToInt32(Reflect.Get(form,"m_status"))!=2&&preparePicking())hit=FindVisibleSticker(x,y,selectedOnly);
  LastHit=hit;LastFace=hit<0?-1:faceIndex(stickers[hit]);
  if(hit<0){
   BlockedClicks++;
   // Consume only ProcessClick. The original MouseUpEvt must still clear the
   // pressed button, update the camera path, and release WinForms capture.
   Reflect.Set(form,"qSkipClick",true);suppressCapture=true;
   try{base.WndProc(ref message);}finally{suppressCapture=false;Reflect.Set(form,"qSkipClick",false);}
   return;
  }
  object priorFaces=facesField.GetValue(cube);dispatching=true;
  try{
   if(restrictedFace>=0)restrictedFaces.SetValue(emptyFaces.GetValue(restrictedFace),restrictedFace);
   restrictedFace=LastFace;
   object face=originalFaces.GetValue(restrictedFace);
   // Filtered rendering deliberately omits framework projection. Refresh just
   // the selected native face before FindFace computes its native 4D point.
   Reflect.Call(face,"RecalcCoord",camera);
   restrictedFaces.SetValue(face,restrictedFace);facesField.SetValue(cube,restrictedFaces);
   AcceptedClicks++;base.WndProc(ref message);
  }finally{facesField.SetValue(cube,priorFaces);dispatching=false;}
 }
 public void Dispose(){if(disposed)return;disposed=true;viewport.HandleCreated-=HandleCreated;viewport.HandleDestroyed-=HandleDestroyed;ReleaseHandle();}
}
