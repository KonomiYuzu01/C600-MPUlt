using System;
using System.Collections.Generic;
using System.IO;

// Parse and validate the one atomic reply on the worker, before the UI applies it.
internal sealed class NativeSnapshot {
 internal Dictionary<string,object> State;
 internal byte[] Colors,Styles;
 internal static NativeSnapshot Read(Dictionary<string,object> reply,string profileHash){
  if(Convert.ToString(reply["format"])!="C600-native-snapshot-v1"||Convert.ToString(reply["profile_sha256"])!=profileHash)
   throw new InvalidDataException("Native snapshot/profile mismatch");
  var result=new NativeSnapshot{State=LocalApi.AsDict(reply["state"]),Colors=Convert.FromBase64String(Convert.ToString(reply["colors"])),Styles=Convert.FromBase64String(Convert.ToString(reply["styles"]))};
  if(result.State==null)throw new InvalidDataException("Native snapshot state missing");
  if(result.Colors.Length!=259800*2||result.Styles.Length!=259800)throw new InvalidDataException("Native snapshot length mismatch");
  for(int i=0;i<result.Styles.Length;i++){
   if(result.Styles[i]>6)throw new InvalidDataException("Native snapshot style out of range");
   if((result.Colors[2*i]|(result.Colors[2*i+1]<<8))>=600)throw new InvalidDataException("Native snapshot color out of range");
  }
  return result;
 }
}
