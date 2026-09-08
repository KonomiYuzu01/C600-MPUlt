// Reuse the original renderer's managed upload array. The native projection,
// packing, colors, draw order, scene, lighting and picking caches remain intact.
// Runtime files are never rewritten. Unknown binaries keep the original path.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using Label=System.Reflection.Emit.Label;

internal sealed class NativeFullRenderer : IDisposable {
 const string RuntimeHash="228b3145460a399e2accc17cb5b891568ec8ddd33c011f1f2a32a00c59743327";
 const string RenderHash="03ce5fd2f71e4d18dc3689ca19124b31e223c3880f9b7a794cf3c2488fc0ea6f";
 const string UploadHash="ace9f87bc3cab12798602bf7ddca98fb4ad8f04cd5004931993d451ca357d23e";
 readonly IList objects;
 readonly object cube,proxy;
 readonly Action<object,object> original,accelerated;
 bool disposed;
 internal bool Enabled {get;set;}
 internal Func<IDisposable> RenderScope {get;set;}
 internal static int StagingArraysCreated {get;private set;}
 static readonly Dictionary<short,OpCode> opcodes=new Dictionary<short,OpCode>();
 static readonly ConditionalWeakTable<object,Staging> staging=new ConditionalWeakTable<object,Staging>();
 static readonly ConditionalWeakTable<object,Staging>.CreateValueCallback createStaging=CreateStaging;
 static int serial;
 static NativeFullRenderer(){foreach(var field in typeof(OpCodes).GetFields())if(field.FieldType==typeof(OpCode)){var op=(OpCode)field.GetValue(null);opcodes[op.Value]=op;}}
 internal static NativeFullRenderer TryInstall(Control viewport,object cube){
  return TryInstall(viewport,cube,null);
 }
 internal static NativeFullRenderer TryInstall(Control viewport,object cube,NativeRenderSubset subset){
  try{return new NativeFullRenderer(viewport,cube,subset);}
  catch(Exception error){NativeDiagnostics.Write("Native render policy adapter could not be installed",error);return null;}
 }
 NativeFullRenderer(Control viewport,object cube,NativeRenderSubset subset){
  if(viewport==null||cube==null||viewport.InvokeRequired)throw new InvalidOperationException("Renderer adapter requires its UI thread and native cube.");
  this.cube=cube;objects=(IList)Reflect.Property(viewport,"DXObjects");int index=objects.IndexOf(cube);if(index<0)throw new NotSupportedException("Original cube is not a scene object.");
  MethodInfo render=cube.GetType().GetMethod("Render",Reflect.Flags),upload=cube.GetType().GetMethod("SendVBuf",Reflect.Flags);
  if(render==null||render.ReturnType!=typeof(void)||render.GetParameters().Length!=1)throw new NotSupportedException("Unrecognized native scene render signature.");
  var c=Expression.Parameter(typeof(object),"cube");var s=Expression.Parameter(typeof(object),"scene");var sceneType=render.GetParameters()[0].ParameterType;
  original=Expression.Lambda<Action<object,object>>(Expression.Call(Expression.Convert(c,cube.GetType()),render,Expression.Convert(s,sceneType)),c,s).Compile();
  if(subset!=null)RenderScope=subset.EnterNativeDraw;
  try {
   Validate(cube.GetType(),render,upload);
   var replacementUpload=Clone(upload,null);var replacementRender=Clone(render,replacementUpload);
   var callbackType=typeof(Action<,>).MakeGenericType(cube.GetType(),sceneType);var callback=replacementRender.CreateDelegate(callbackType);
   accelerated=Expression.Lambda<Action<object,object>>(Expression.Invoke(Expression.Constant(callback),Expression.Convert(c,cube.GetType()),Expression.Convert(s,sceneType)),c,s).Compile();
   Enabled=true;
  } catch(Exception error) {
   NativeDiagnostics.Write("Reusable renderer buffer unavailable; original MPUlt rendering retains the frame and visibility policy",error);
  }
  proxy=MakeProxy(cube,Draw);objects[index]=proxy;
  NativeDiagnostics.Write(Enabled?"Reusable full-detail upload buffer installed; native projection, vertex packing and all geometry retained":"Original native render policy adapter installed");
 }
 void Draw(object target,object scene){using(RenderScope==null?null:RenderScope()){if(Enabled&&accelerated!=null)accelerated(target,scene);else original(target,scene);}}
 static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 internal static void Validate(Type cubeType,MethodInfo render,MethodInfo upload){
  if(cubeType.FullName!="_3dedit.CubeObj"||render==null||upload==null||Hash(File.ReadAllBytes(cubeType.Assembly.Location))!=RuntimeHash)throw new NotSupportedException("Unrecognized MPUlt renderer binary.");
  if(render.GetMethodBody()==null||upload.GetMethodBody()==null||Hash(render.GetMethodBody().GetILAsByteArray())!=RenderHash||Hash(upload.GetMethodBody().GetILAsByteArray())!=UploadHash)throw new NotSupportedException("Unrecognized MPUlt render/upload instructions.");
  if(render.ReturnType!=typeof(void)||render.GetParameters().Length!=1||upload.ReturnType!=typeof(void)||upload.GetParameters().Length!=3)throw new NotSupportedException("Unrecognized MPUlt renderer signature.");
 }
 sealed class Staging {internal Array Vertices;internal Action Upload;}
 static Staging CreateStaging(object buffer){
  var type=buffer.GetType();var vertexType=type.Assembly.GetType("Microsoft.DirectX.Direct3D.CustomVertex+PositionNormalColored",true);
  int stride=Convert.ToInt32(vertexType.GetProperty("StrideSize",BindingFlags.Public|BindingFlags.Static).GetValue(null,null));int size=Convert.ToInt32(type.GetProperty("SizeInBytes").GetValue(buffer,null));
  if(stride!=28||size<=0||size%stride!=0)throw new NotSupportedException("Unexpected native vertex buffer layout.");
  var vertices=Array.CreateInstance(vertexType,size/stride);var flags=type.Assembly.GetType("Microsoft.DirectX.Direct3D.LockFlags",true);
  var setData=type.GetMethod("SetData",new[]{typeof(object),typeof(int),flags});if(setData==null)throw new MissingMethodException("Native VertexBuffer.SetData is unavailable.");
  var upload=Expression.Lambda<Action>(Expression.Call(Expression.Constant(buffer),setData,Expression.Constant(vertices,typeof(object)),Expression.Constant(0),Expression.Constant(Enum.ToObject(flags,0),flags))).Compile();
  StagingArraysCreated++;return new Staging{Vertices=vertices,Upload=upload};
 }
 // Called only by the verified synchronous native Render. SetData locks and
 // unlocks the same original buffer while uploading this reusable array.
 static Array ReuseLock(object buffer,int offset,int flags){if(offset!=0||flags!=0)throw new InvalidOperationException("Unexpected native lock arguments.");return staging.GetValue(buffer,createStaging).Vertices;}
 static void ReuseUpload(object buffer){Staging entry;if(!staging.TryGetValue(buffer,out entry))throw new InvalidOperationException("Native upload has no matching staging array.");entry.Upload();}
 sealed class Instruction {internal int Offset;internal OpCode Op;internal object Operand;}
 static List<Instruction> Read(MethodInfo method){
  var bytes=method.GetMethodBody().GetILAsByteArray();var result=new List<Instruction>();int p=0;
  while(p<bytes.Length){var item=new Instruction{Offset=p};short code=bytes[p++];if(code==254)code=(short)(0xfe00|bytes[p++]);var op=opcodes[code];item.Op=op;int count=0;object value=null;
   switch(op.OperandType){
    case OperandType.InlineNone:break;
    case OperandType.ShortInlineI:value=(sbyte)bytes[p];count=1;break;
    case OperandType.ShortInlineVar:value=bytes[p];count=1;break;
    case OperandType.InlineVar:value=BitConverter.ToInt16(bytes,p);count=2;break;
    case OperandType.ShortInlineBrTarget:value=p+1+(sbyte)bytes[p];count=1;break;
    case OperandType.InlineBrTarget:value=p+4+BitConverter.ToInt32(bytes,p);count=4;break;
    case OperandType.InlineI:value=BitConverter.ToInt32(bytes,p);count=4;break;
    case OperandType.InlineI8:value=BitConverter.ToInt64(bytes,p);count=8;break;
    case OperandType.ShortInlineR:value=BitConverter.ToSingle(bytes,p);count=4;break;
    case OperandType.InlineR:value=BitConverter.ToDouble(bytes,p);count=8;break;
    case OperandType.InlineSwitch:{int n=BitConverter.ToInt32(bytes,p);count=4+n*4;var targets=new int[n];for(int i=0;i<n;i++)targets[i]=p+count+BitConverter.ToInt32(bytes,p+4+i*4);value=targets;break;}
    case OperandType.InlineString:count=4;value=method.Module.ResolveString(BitConverter.ToInt32(bytes,p));break;
    case OperandType.InlineSig:throw new NotSupportedException("Indirect renderer calls are unsupported.");
    default:count=4;value=method.Module.ResolveMember(BitConverter.ToInt32(bytes,p));break;
   }
   p+=count;item.Operand=value;result.Add(item);
  }return result;
 }
 static DynamicMethod Clone(MethodInfo method,DynamicMethod upload){
  var body=method.GetMethodBody();if(body.ExceptionHandlingClauses.Count!=0)throw new NotSupportedException("Unexpected renderer exception clauses.");var parameters=new List<Type>{method.DeclaringType};foreach(var parameter in method.GetParameters())parameters.Add(parameter.ParameterType);
  var dm=new DynamicMethod("C600Reusable"+method.Name+(serial++),method.ReturnType,parameters.ToArray(),method.DeclaringType,true);dm.InitLocals=body.InitLocals;var il=dm.GetILGenerator();foreach(var local in body.LocalVariables)il.DeclareLocal(local.LocalType,local.IsPinned);
  var instructions=Read(method);var labels=new Dictionary<int,Label>();foreach(var item in instructions)labels[item.Offset]=il.DefineLabel();labels[body.GetILAsByteArray().Length]=il.DefineLabel();int locks=0,unlocks=0,sends=0,frameworkColors=0;
  foreach(var item in instructions){
   il.MarkLabel(labels[item.Offset]);var op=item.Op;object value=item.Operand;var called=value as MethodInfo;
   if(upload!=null&&item.Offset==0x329&&op==OpCodes.Ldc_I4&&(int)value==-8355712){
    // The hash-verified original passes a constant gray to framework lines;
    // StkMesh.Col only affects fills. Read the renderer-owned focus mesh here.
    // Geometry, clipping and the original sticker selection colors stay intact.
    il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Ldfld,Reflect.Field(method.DeclaringType,"StFaces"));il.Emit(OpCodes.Ldloc,(short)11);il.Emit(OpCodes.Ldelem_Ref);
    il.Emit(OpCodes.Call,typeof(NativeRenderSubset).GetMethod("FrameworkColor",BindingFlags.NonPublic|BindingFlags.Static));frameworkColors++;
   }else if(called!=null&&called.DeclaringType.FullName=="Microsoft.DirectX.Direct3D.VertexBuffer"&&called.Name=="Lock"){
    if(called.ReturnType!=typeof(Array)||called.GetParameters().Length!=2)throw new NotSupportedException("Unrecognized native lock overload.");il.Emit(OpCodes.Call,typeof(NativeFullRenderer).GetMethod("ReuseLock",BindingFlags.NonPublic|BindingFlags.Static));locks++;
   }else if(called!=null&&called.DeclaringType.FullName=="Microsoft.DirectX.Direct3D.VertexBuffer"&&called.Name=="Unlock"){
    il.Emit(OpCodes.Call,typeof(NativeFullRenderer).GetMethod("ReuseUpload",BindingFlags.NonPublic|BindingFlags.Static));unlocks++;
   }else if(upload!=null&&called!=null&&called.Name=="SendVBuf"&&called.DeclaringType==method.DeclaringType){il.Emit(OpCodes.Call,upload);sends++;}
   else switch(op.OperandType){
    case OperandType.InlineNone:il.Emit(op);break;
    case OperandType.ShortInlineI:il.Emit(op,(sbyte)value);break;
    case OperandType.ShortInlineVar:il.Emit(op,(byte)value);break;
    case OperandType.InlineVar:il.Emit(op,(short)value);break;
    case OperandType.ShortInlineBrTarget:{var longOp=(OpCode)typeof(OpCodes).GetField(op.Name.Replace('.','_').Substring(0,op.Name.Length-2),BindingFlags.Public|BindingFlags.Static|BindingFlags.IgnoreCase).GetValue(null);il.Emit(longOp,labels[(int)value]);break;}
    case OperandType.InlineBrTarget:il.Emit(op,labels[(int)value]);break;
    case OperandType.InlineI:il.Emit(op,(int)value);break;
    case OperandType.InlineI8:il.Emit(op,(long)value);break;
    case OperandType.ShortInlineR:il.Emit(op,(float)value);break;
    case OperandType.InlineR:il.Emit(op,(double)value);break;
    case OperandType.InlineSwitch:{var targets=(int[])value;var destination=new Label[targets.Length];for(int i=0;i<targets.Length;i++)destination[i]=labels[targets[i]];il.Emit(op,destination);break;}
    case OperandType.InlineString:il.Emit(op,(string)value);break;
    default:if(value is FieldInfo)il.Emit(op,(FieldInfo)value);else if(value is ConstructorInfo)il.Emit(op,(ConstructorInfo)value);else if(value is MethodInfo)il.Emit(op,(MethodInfo)value);else if(value is Type)il.Emit(op,(Type)value);else throw new NotSupportedException("Unexpected renderer instruction operand.");break;
   }
  }
  il.MarkLabel(labels[body.GetILAsByteArray().Length]);
  if(upload==null?(locks!=0||unlocks!=1||sends!=0||frameworkColors!=0):(locks!=5||unlocks!=0||sends!=5||frameworkColors!=1))throw new NotSupportedException("Native buffer call structure changed.");return dm;
 }
 static object MakeProxy(object cube,Action<object,object> render){
  var iface=cube.GetType().GetInterface("_3dedit.IDXObject");var assembly=AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("C600FullRenderAdapter"+(serial++)),AssemblyBuilderAccess.Run);var module=assembly.DefineDynamicModule("adapter");var type=module.DefineType("NativeRenderAdapter",TypeAttributes.Public|TypeAttributes.Sealed);type.AddInterfaceImplementation(iface);
  var target=type.DefineField("Target",cube.GetType(),FieldAttributes.Public);var callback=type.DefineField("Callback",typeof(Action<object,object>),FieldAttributes.Public);
  foreach(var method in iface.GetMethods()){
   var parameters=Array.ConvertAll(method.GetParameters(),p=>p.ParameterType);var implementation=type.DefineMethod(method.Name,MethodAttributes.Public|MethodAttributes.Virtual|MethodAttributes.Final|MethodAttributes.HideBySig|MethodAttributes.NewSlot,method.ReturnType,parameters);var il=implementation.GetILGenerator();
   if(method.Name=="Render"){il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Ldfld,callback);il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Ldfld,target);il.Emit(OpCodes.Ldarg_1);il.Emit(OpCodes.Callvirt,typeof(Action<object,object>).GetMethod("Invoke"));}
   else{il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Ldfld,target);for(int i=0;i<parameters.Length;i++)il.Emit(OpCodes.Ldarg,i+1);il.Emit(OpCodes.Callvirt,cube.GetType().GetMethod(method.Name,Reflect.Flags,null,parameters,null));}
   il.Emit(OpCodes.Ret);type.DefineMethodOverride(implementation,method);
  }
  var created=type.CreateType();var proxy=Activator.CreateInstance(created);created.GetField("Target").SetValue(proxy,cube);created.GetField("Callback").SetValue(proxy,render);return proxy;
 }
 public void Dispose(){if(disposed)return;disposed=true;int index=objects.IndexOf(proxy);if(index>=0)objects[index]=cube;}
}
