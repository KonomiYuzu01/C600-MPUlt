using System;
using System.Linq.Expressions;
using System.Reflection;

// Compile the inspected member access once, instead of reflecting per sticker.
internal sealed class NativeStickerAccess {
 internal readonly object[] Slots;
 internal readonly Func<object,object> GetBase;
 internal readonly Action<object,object> SetBase;
 internal readonly Action<object,int> SetColor;
 readonly Action<object,double,double> setCoord;
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
}
