using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

// Compile the inspected member access once, instead of reflecting per sticker.
internal sealed class NativeStickerAccess {
 internal readonly object[] Slots;
 internal readonly Func<object,object> GetBase;
 internal readonly Action<object,object> SetBase;
 internal readonly Action<object,int> SetColor;
 readonly Action<object,double,double> setCoord;
 Type colorOwnerType;
 Func<int[]> getPalette;
 Func<object,int> getWhiteColor;
 int[] lastPalette,overrideSlots;
 int lastWhiteColor;
 byte[] overrideStyles;
 internal NativeStickerAccess(Array stickers){
  Slots=new object[stickers.Length];for(int i=0;i<Slots.Length;i++)Slots[i]=stickers.GetValue(i);
  Type type=stickers.GetType().GetElementType();
  var obj=Expression.Parameter(typeof(object),"sticker");var item=Expression.Convert(obj,type);
  var baseInfo=Reflect.Field(type,"Base");var baseExpr=Expression.Field(item,baseInfo);
  GetBase=Expression.Lambda<Func<object,object>>(Expression.Convert(baseExpr,typeof(object)),obj).Compile();
  var value=Expression.Parameter(typeof(object),"mesh");
  SetBase=Expression.Lambda<Action<object,object>>(Expression.Block(Expression.Assign(baseExpr,Expression.Convert(value,baseInfo.FieldType)),Expression.Empty()),obj,value).Compile();
  var color=Expression.Parameter(typeof(int),"color");
  SetColor=Expression.Lambda<Action<object,int>>(Expression.Block(Expression.Assign(Expression.Field(item,Reflect.Field(type,"Col")),color),Expression.Empty()),obj,color).Compile();
  var method=type.GetMethod("SetCoord",Reflect.Flags,null,new[]{typeof(double),typeof(double)},null);
  if(method!=null){var fs=Expression.Parameter(typeof(double),"faceShrink");var ss=Expression.Parameter(typeof(double),"stickerShrink");setCoord=Expression.Lambda<Action<object,double,double>>(Expression.Call(item,method,fs,ss),obj,fs,ss).Compile();}
 }
 internal void SetCoord(object item,double fs,double ss){if(setCoord==null)throw new MissingMethodException("Native sticker SetCoord is unavailable");setCoord(item,fs,ss);}
 // The pinned MPUlt SetStickerColors has no geometry/cache side effects. Reproduce
 // its exact packed ARGB math for changed slots, then apply the host's styling.
 internal void ApplyColors(object cube,short[] field,byte[] styles,int[] changedSlots,bool restoreAllOverrides){
  if(cube==null||field==null||styles==null||field.Length!=Slots.Length||styles.Length!=Slots.Length)throw new InvalidDataException("Native sticker color input length mismatch");
  if(colorOwnerType!=cube.GetType()){
   Type type=cube.GetType();var palette=Reflect.Field(type,"Colors");var white=Reflect.Field(type,"WhiteColor");
   if(palette==null||!palette.IsStatic||palette.FieldType!=typeof(int[])||white==null||white.FieldType!=typeof(int))throw new MissingFieldException("Unsupported native sticker palette API");
   getPalette=Expression.Lambda<Func<int[]>>(Expression.Field(null,palette)).Compile();
   var obj=Expression.Parameter(typeof(object),"cube");getWhiteColor=Expression.Lambda<Func<object,int>>(Expression.Field(Expression.Convert(obj,type),white),obj).Compile();
   colorOwnerType=type;lastPalette=null;
  }
  int[] colors=getPalette();int whiteColor=getWhiteColor(cube);
  if(colors==null||colors.Length<600)throw new InvalidDataException("Native sticker palette is incomplete");
  if(!Object.ReferenceEquals(colors,lastPalette)||whiteColor!=lastWhiteColor)changedSlots=null;
  int count=changedSlots==null?Slots.Length:changedSlots.Length,last=-1;
  // Validate before touching any mesh. Snapshot parsing also checks the complete
  // authoritative arrays, but this boundary protects callers outside that path.
  for(int i=0;i<count;i++){
   int slot=changedSlots==null?i:changedSlots[i];
   if(slot<=last||slot<0||slot>=Slots.Length)throw new InvalidDataException("Native sticker color indices must be sorted, unique and in range");
   if(styles[slot]>6||(field[slot]&16383)>=600)throw new InvalidDataException("Native sticker color/style out of range");last=slot;
  }
  if(!Object.ReferenceEquals(styles,overrideStyles)){
   var indices=new List<int>();for(int i=0;i<styles.Length;i++){if(styles[i]>6)throw new InvalidDataException("Native sticker style out of range");if(styles[i]!=0&&styles[i]!=2)indices.Add(i);}
   overrideSlots=indices.ToArray();overrideStyles=styles;
  }
  for(int i=0;i<count;i++){
   int slot=changedSlots==null?i:changedSlots[i],color=field[slot]&16383,rgb=colors[color],alpha=255;
   if(whiteColor>=0){if(color==whiteColor)rgb=16777215;else{alpha=128;rgb=(rgb>>1)&8355711;}}
   int value=rgb|(alpha<<24);if(styles[slot]!=0&&styles[slot]!=2)value=StyleColor(styles[slot]);
   SetColor(Slots[slot],value);
  }
  if(restoreAllOverrides&&changedSlots!=null)foreach(int slot in overrideSlots)SetColor(Slots[slot],StyleColor(styles[slot]));
  lastPalette=colors;lastWhiteColor=whiteColor;
 }
 static int StyleColor(byte style){
  if(style==3)return unchecked((int)0xFFF89A35);if(style==4)return unchecked((int)0xFF29E3DE);
  if(style==5)return unchecked((int)0xFFFF5F33);if(style==6)return unchecked((int)0xFFFFE866);
  return unchecked((int)0xFF90969C);
 }
}
