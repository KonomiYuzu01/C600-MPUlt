using System;
using System.Collections.Generic;
using System.IO;

// Materialize one atomic server reply on the worker. Published arrays are immutable:
// a delta validates completely before it clones any changed predecessor arrays.
internal sealed class NativeSnapshot {
 internal const int SlotCount=259800;
 internal Dictionary<string,object> State;
 internal Dictionary<string,string> Revisions;
 internal byte[] Colors,Styles,Interactive;
 internal string ProfileHash,Revision,BaseRevision;
 internal bool IsFull,ColorsChanged,StylesChanged,VisibilityChanged,InteractionChanged;
 // null means all slots; an empty array is a valid metadata-only delta.
 internal int[] ChangedSlots,ColorSlots,StyleSlots,InteractionSlots;
 internal static NativeSnapshot Read(Dictionary<string,object> reply,string profileHash){return Read(reply,profileHash,null);}
 internal static NativeSnapshot Read(Dictionary<string,object> reply,string profileHash,NativeSnapshot previous){
  if(reply==null)throw new InvalidDataException("Native snapshot missing");
  string format=StringValue(reply,"format");
  if((format!="C600-native-snapshot-v1"&&format!="C600-native-snapshot-v2")||StringValue(reply,"profile_sha256")!=profileHash)
   throw new InvalidDataException("Native snapshot/profile mismatch");
  object state;reply.TryGetValue("state",out state);
  var result=new NativeSnapshot{State=LocalApi.AsDict(state),ProfileHash=profileHash};
  if(result.State==null)throw new InvalidDataException("Native snapshot state missing");
  bool version2=format=="C600-native-snapshot-v2";
  string mode=version2?StringValue(reply,"mode"):"full";
  if(mode!="full"&&mode!="delta")throw new InvalidDataException("Native snapshot mode invalid");
  result.IsFull=mode=="full";
  if(version2){
   result.Revision=Token(reply,"revision");
   object revisionObject;reply.TryGetValue("revisions",out revisionObject);
   var revisions=LocalApi.AsDict(revisionObject);
   if(revisions==null)throw new InvalidDataException("Native snapshot revisions missing");
   result.Revisions=new Dictionary<string,string>();
   foreach(string key in new[]{"state","color","visibility","interaction","annotation"})result.Revisions.Add(key,Token(revisions,key));
  }
  var colors=Bytes(reply,"colors");var styles=Bytes(reply,"styles");
  byte[] interactive;
  if(!version2&&!reply.ContainsKey("interactive")){
   interactive=new byte[styles.Length];for(int i=0;i<styles.Length;i++)interactive[i]=(byte)(styles[i]==0?0:1);
  }else interactive=Bytes(reply,"interactive");
  int[] indices=null;int count=SlotCount;
  if(!result.IsFull){
   result.BaseRevision=Token(reply,"base_revision");
   if(previous==null||previous.Revision!=result.BaseRevision||previous.ProfileHash!=profileHash)
    throw new InvalidDataException("Native snapshot delta base mismatch; request a full snapshot");
   byte[] packed=Bytes(reply,"indices");
   if(packed.Length%4!=0||packed.Length>SlotCount*4)throw new InvalidDataException("Native snapshot index length mismatch");
   count=packed.Length/4;indices=new int[count];int last=-1;
   for(int i=0;i<count;i++){
    int at=4*i;uint slot=(uint)packed[at]|((uint)packed[at+1]<<8)|((uint)packed[at+2]<<16)|((uint)packed[at+3]<<24);
    if(slot>=SlotCount||(int)slot<=last)throw new InvalidDataException("Native snapshot indices must be sorted, unique and in range");
    indices[i]=last=(int)slot;
   }
  }
  if(colors.Length!=count*2||styles.Length!=count||interactive.Length!=count)throw new InvalidDataException("Native snapshot length mismatch");
  for(int i=0;i<count;i++){
   if(styles[i]>6)throw new InvalidDataException("Native snapshot style out of range");
   if((colors[2*i]|(colors[2*i+1]<<8))>=600)throw new InvalidDataException("Native snapshot color out of range");
   if(interactive[i]>1||(interactive[i]!=0&&styles[i]==0))throw new InvalidDataException("Native snapshot interaction out of range or hidden");
  }
  if(result.IsFull){
   result.Colors=colors;result.Styles=styles;result.Interactive=interactive;
   result.ColorsChanged=result.StylesChanged=result.VisibilityChanged=result.InteractionChanged=true;
   return result;
  }
  // Union-index entries need not alter every attribute.
  var colorSlots=new List<int>();var styleSlots=new List<int>();var interactionSlots=new List<int>();
  for(int i=0;i<count;i++){
   int slot=indices[i],at=slot*2;
   if(previous.Colors[at]!=colors[2*i]||previous.Colors[at+1]!=colors[2*i+1])colorSlots.Add(slot);
   if(previous.Styles[slot]!=styles[i]){styleSlots.Add(slot);if((previous.Styles[slot]==0)!=(styles[i]==0))result.VisibilityChanged=true;}
   if(previous.Interactive[slot]!=interactive[i])interactionSlots.Add(slot);
  }
  result.ChangedSlots=indices;result.ColorSlots=colorSlots.ToArray();result.StyleSlots=styleSlots.ToArray();result.InteractionSlots=interactionSlots.ToArray();
  result.ColorsChanged=colorSlots.Count!=0;result.StylesChanged=styleSlots.Count!=0;result.InteractionChanged=interactionSlots.Count!=0;
  result.Colors=result.ColorsChanged?(byte[])previous.Colors.Clone():previous.Colors;
  result.Styles=result.StylesChanged?(byte[])previous.Styles.Clone():previous.Styles;
  result.Interactive=result.InteractionChanged?(byte[])previous.Interactive.Clone():previous.Interactive;
  for(int i=0;i<count;i++){
   int slot=indices[i];
   if(result.ColorsChanged){result.Colors[2*slot]=colors[2*i];result.Colors[2*slot+1]=colors[2*i+1];}
   if(result.StylesChanged)result.Styles[slot]=styles[i];
   if(result.InteractionChanged)result.Interactive[slot]=interactive[i];
  }
  return result;
 }
 static string StringValue(Dictionary<string,object> source,string key){object value;if(!source.TryGetValue(key,out value)||!(value is string))throw new InvalidDataException("Native snapshot "+key+" missing or invalid");return (string)value;}
 static string Token(Dictionary<string,object> source,string key){string value=StringValue(source,key);if(value.Length==0||value.Length>256)throw new InvalidDataException("Native snapshot "+key+" invalid");return value;}
 static byte[] Bytes(Dictionary<string,object> source,string key){try{return Convert.FromBase64String(StringValue(source,key));}catch(FormatException error){throw new InvalidDataException("Native snapshot "+key+" is not base64",error);}}
}
