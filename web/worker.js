/* Filtered draw-list construction never runs on the browser's input thread. */
onmessage=e=>{
 const t=performance.now(),{styles,offsets,base,generation}=e.data;let visible=0,n=0;
 for(let i=0;i<styles.length;i++)if(styles[i]){visible++;n+=offsets[i%433+1]-offsets[i%433];}
 const mode=e.data.mode==='auto'?(visible>15000?'points':'mesh'):e.data.mode;
 let items=new Uint32Array(mode==='points'?visible:n),j=0;
 for(let i=0;i<styles.length;i++)if(styles[i]){
  if(mode==='points')items[j++]=i;
  else{const local=i%433,cell=Math.floor(i/433),a=offsets[local],b=offsets[local+1];for(let v=a;v<b;v++)items[j++]=cell*base+v;}
 }
 postMessage({generation,mode,visible,indices:items.buffer,ms:performance.now()-t},[items.buffer]);
};
