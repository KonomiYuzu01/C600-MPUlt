/* Dedicated full-600-cell GPU renderer. Mechanics are never inferred from pixels. */
export class Renderer {
 constructor(canvas,meta,arrays,onPick,onFrame){
  this.canvas=canvas;this.meta=meta;this.arrays=arrays;this.onPick=onPick;this.onFrame=onFrame;
  this.gl=canvas.getContext('webgl2',{alpha:false,antialias:true,preserveDrawingBuffer:true});
  if(!this.gl)throw Error('WebGL 2 is unavailable. Enable hardware acceleration or use another browser.');
  this.background=[.13,.145,.16];this.pixelScale=1;this.wireframe=true;this.turn=null;this.animating=false;this.displayHost=null;
  this.q=Array.from({length:16},(_,i)=>i%5===0?1:0);this.cellScale=.76;this.stickerScale=.82;this.distance4=1.18;this.zoom=1.15;this.reference=false;this.mode='auto';this.actualMode='mesh';this.count=0;this.lastFrames=[];this.dirty=true;this.rotating=false;
  const gl=this.gl;
  const vs=`#version 300 es
  precision highp float;precision highp int;
  layout(location=0) in uint item;
  uniform sampler2D uVertices,uCenters,uFrames;
  uniform highp usampler2D uLocal,uLabels,uStyles,uTurnMask;
  uniform mat4 uQ;uniform int uBaseCount;uniform vec4 uN0;
  uniform float uRadius,uCellScale,uStickerScale,uD4,uZoom,uAspect;
  uniform bool uPoints,uReference,uWire;uniform vec4 uTurnU,uTurnV;uniform float uTurnAngle;
  out vec3 vPosition;flat out uint vSlot,vStyle,vColor;out float vPoint;
  ivec2 at(uint n){return ivec2(int(n%1024u),int(n/1024u));}
  void main(){
   uint slot,local,cell;vec4 v;
   if(uPoints){slot=item;local=slot%433u;cell=slot/433u;v=texelFetch(uCenters,at(local),0);}
   else{cell=item/uint(uBaseCount);uint vi=item%uint(uBaseCount);local=texelFetch(uLocal,at(vi),0).r;slot=cell*433u+local;v=texelFetch(uVertices,at(vi),0);}
   vec4 center=texelFetch(uCenters,at(local),0);
   v=uWire?(uN0+uCellScale*(v-uN0))/uRadius:(uN0+uCellScale*(center-uN0)+uCellScale*uStickerScale*(v-center))/uRadius;
   mat4 f=mat4(texelFetch(uFrames,at(cell*4u),0),texelFetch(uFrames,at(cell*4u+1u),0),texelFetch(uFrames,at(cell*4u+2u),0),texelFetch(uFrames,at(cell*4u+3u),0));
   vec4 world=f*v;
   if(uTurnAngle!=0.0 && !uWire && texelFetch(uTurnMask,at(slot),0).r>0u){
    float x=dot(world,uTurnU),y=dot(world,uTurnV);float cs=cos(uTurnAngle),sn=sin(uTurnAngle);
    world+=((cs-1.0)*x-sn*y)*uTurnU+(sn*x+(cs-1.0)*y)*uTurnV;
   }
   world=uQ*world;vec3 p=uD4*world.xyz/(uD4-world.w);vPosition=p;
   float z=5.0-p.z;float near=.05,far=100.0;
   gl_Position=vec4(p.x*uZoom/uAspect,p.y*uZoom,(far+near)/(far-near)*z-2.0*far*near/(far-near),z);
   vSlot=slot;vStyle=uWire?2u:texelFetch(uStyles,at(slot),0).r;vColor=uReference?cell:texelFetch(uLabels,at(slot),0).r/433u;
   gl_PointSize=vStyle>=4u?8.0:3.2;vPoint=uPoints?1.0:0.0;
  }`;
  const fs=`#version 300 es
  precision highp float;precision highp int;
  in vec3 vPosition;flat in uint vSlot,vStyle,vColor;in float vPoint;uniform bool uPicking,uWire;out vec4 outColor;
  vec3 hsv(vec3 c){vec3 p=abs(fract(c.xxx+vec3(0,2.0/3.0,1.0/3.0))*6.0-3.0);return c.z*mix(vec3(1),clamp(p-1.0,0.0,1.0),c.y);}
  void main(){
   if(vStyle==0u)discard;
   if(uWire){outColor=vec4(.43,.47,.52,1.);return;}
   if(vStyle==1u && mod(floor(gl_FragCoord.x)+2.0*floor(gl_FragCoord.y),4.0)>0.1)discard;
   if(vPoint>.5 && length(gl_PointCoord-vec2(.5))>.5)discard;
   if(uPicking){if(vStyle==1u)discard;uint id=vSlot+1u;outColor=vec4(float(id&255u),float((id>>8u)&255u),float((id>>16u)&255u),255.0)/255.0;return;}
   vec3 color=hsv(vec3(fract(float(vColor)*.61803398875),.61,.90));
   if(vStyle==1u)color=vec3(.45,.48,.52);
   if(vStyle==3u)color=vec3(.98,.60,.24);
   if(vStyle==4u)color=vec3(.16,.89,.87);
   if(vStyle==5u)color=vec3(1.,.37,.20);
   if(vStyle==6u)color=vec3(1.,.91,.42);
   float shade=1.0;
   if(vPoint<.5){vec3 n=normalize(cross(dFdx(vPosition),dFdy(vPosition)));shade=.50+.50*abs(dot(n,normalize(vec3(.3,.5,1.))));}
   outColor=vec4(color*shade,1.0);
  }`;
  const compile=(type,src)=>{const s=gl.createShader(type);gl.shaderSource(s,src);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw Error(gl.getShaderInfoLog(s));return s;};
  this.program=gl.createProgram();gl.attachShader(this.program,compile(gl.VERTEX_SHADER,vs));gl.attachShader(this.program,compile(gl.FRAGMENT_SHADER,fs));gl.linkProgram(this.program);if(!gl.getProgramParameter(this.program,gl.LINK_STATUS))throw Error(gl.getProgramInfoLog(this.program));
  gl.useProgram(this.program);this.uniforms={};const U=n=>this.uniforms[n]??(this.uniforms[n]=gl.getUniformLocation(this.program,n));this.U=U;
  this.texture('uVertices',arrays.vertices,4,false,0);this.texture('uCenters',arrays.centers,4,false,1);this.texture('uFrames',arrays.frames,4,false,2);this.texture('uLocal',arrays.local,1,true,3);
  this.labelsTex=this.texture('uLabels',new Uint32Array(meta.slots),1,true,4);this.stylesTex=this.texture('uStyles',new Uint32Array(meta.slots),1,true,5);
  this.turnTex=this.texture('uTurnMask',new Uint32Array(meta.slots),1,true,6);
  gl.uniform1i(U('uBaseCount'),meta.base_vertices);gl.uniform4fv(U('uN0'),meta.normal);gl.uniform1f(U('uRadius'),meta.normal_length);
  this.vao=gl.createVertexArray();gl.bindVertexArray(this.vao);this.buffer=gl.createBuffer();gl.bindBuffer(gl.ARRAY_BUFFER,this.buffer);gl.enableVertexAttribArray(0);gl.vertexAttribIPointer(0,1,gl.UNSIGNED_INT,4,0);
  this.wireBuffer=gl.createBuffer();this.wireCount=0;this.cornerIndices=this.findCorners();
  gl.enable(gl.DEPTH_TEST);gl.disable(gl.BLEND);gl.disable(gl.CULL_FACE);
  const ext=gl.getExtension('WEBGL_debug_renderer_info');this.gpu=ext?gl.getParameter(ext.UNMASKED_RENDERER_WEBGL):gl.getParameter(gl.RENDERER);
  this.worker=new Worker('/web/worker.js');this.generation=0;
  this.worker.onmessage=e=>{const d=e.data;if(d.generation!==this.generation)return;this.indices=new Uint32Array(d.indices);this.count=this.indices.length;this.actualMode=d.mode;this.visible=d.visible;gl.bindBuffer(gl.ARRAY_BUFFER,this.buffer);gl.bufferData(gl.ARRAY_BUFFER,this.indices,gl.DYNAMIC_DRAW);this.rebuildWires();this.dirty=true;this.onFrame?.({visible:this.visible,vertices:this.count,mode:this.actualMode,build_ms:d.ms,gpu:this.gpu});};
  this.attachInput();this.focus(0);
  this.animate=()=>{const now=performance.now();if(this.rotating)this.rotate(0,3,.002);if(this.dirty){const begin=performance.now();this.draw();this.lastFrames.push(performance.now()-begin);if(this.lastFrames.length>180)this.lastFrames.shift();this.dirty=false;}this.raf=requestAnimationFrame(this.animate);};this.animate();
  canvas.addEventListener('webglcontextlost',e=>{e.preventDefault();cancelAnimationFrame(this.raf);this.onFrame?.({error:'Graphics context lost. Puzzle state is safe on the server. Reload this page.'});});
 }
 texture(name,data,channels,integer,unit){
  const gl=this.gl,h=Math.ceil(data.length/channels/1024),pad=integer?new Uint32Array(1024*h*channels):new Float32Array(1024*h*channels);pad.set(data);const t=gl.createTexture();gl.activeTexture(gl.TEXTURE0+unit);gl.bindTexture(gl.TEXTURE_2D,t);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.NEAREST);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.CLAMP_TO_EDGE);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.CLAMP_TO_EDGE);gl.texImage2D(gl.TEXTURE_2D,0,integer?gl.R32UI:gl.RGBA32F,1024,h,0,integer?gl.RED_INTEGER:gl.RGBA,integer?gl.UNSIGNED_INT:gl.FLOAT,pad);gl.uniform1i(this.U(name),unit);return {t,h,unit,pad};
 }
 findCorners(){
  const a=this.arrays.vertices,n=this.meta.normal;let best=0,rows=[];
  for(let i=0;i<a.length;i+=4){let d=0;for(let j=0;j<4;j++)d+=(a[i+j]-n[j])**2;best=Math.max(best,d);rows.push(d);}
  const seen=new Set(),out=[];
  for(let i=0;i<rows.length;i++)if(rows[i]>best-1e-5){const key=Array.from(a.subarray(i*4,i*4+4)).map(x=>x.toFixed(5)).join(',');if(!seen.has(key)){seen.add(key);out.push(i);}}
  return out.length===4?out:[];
 }
 rebuildWires(){
  if(!this.styles||!this.cornerIndices.length)return;
  const a=[];for(let c=0;c<600;c++){let any=false;for(let j=c*433;j<(c+1)*433;j++)if(this.styles[j]){any=true;break;}if(!any)continue;for(let i=0;i<4;i++)for(let j=i+1;j<4;j++)a.push(c*this.meta.base_vertices+this.cornerIndices[i],c*this.meta.base_vertices+this.cornerIndices[j]);}
  const gl=this.gl;this.wireCount=a.length;gl.bindBuffer(gl.ARRAY_BUFFER,this.wireBuffer);gl.bufferData(gl.ARRAY_BUFFER,new Uint32Array(a),gl.DYNAMIC_DRAW);gl.bindBuffer(gl.ARRAY_BUFFER,this.buffer);
 }
 updateTexture(x,a){
  let first=-1,last=-1;for(let i=0;i<a.length;i++)if(x.pad[i]!==a[i]){if(first<0)first=i;last=i;x.pad[i]=a[i];}
  if(first<0)return false;
  const y=Math.floor(first/1024),end=Math.floor(last/1024)+1,g=this.gl;
  g.activeTexture(g.TEXTURE0+x.unit);g.bindTexture(g.TEXTURE_2D,x.t);g.texSubImage2D(g.TEXTURE_2D,0,0,y,1024,end-y,g.RED_INTEGER,g.UNSIGNED_INT,x.pad.subarray(y*1024,end*1024));this.dirty=true;return true;
 }
 setState(labels,styles){
  this.unrestrictedStyles=styles;
  if(this.displayHost!==null){styles=styles.slice();for(let i=0;i<styles.length;i++)if(Math.floor(i/433)!==this.displayHost)styles[i]=0;}
  this.updateTexture(this.labelsTex,labels);let rebuild=!this.styles;
  if(this.styles)for(let i=0;i<styles.length;i++)if(Boolean(styles[i])!==Boolean(this.styles[i])){rebuild=true;break;}
  this.updateTexture(this.stylesTex,styles);this.styles=styles;
  if(rebuild)this.rebuild();
 }
 async animateTurn(info,mask,duration=190){
  this.updateTexture(this.turnTex,mask);this.turn={...info,progress:0};this.animating=true;
  const start=performance.now();await new Promise(resolve=>{const tick=now=>{let t=Math.min(1,(now-start)/duration);this.turn.progress=t*t*(3-2*t);this.dirty=true;if(t<1)requestAnimationFrame(tick);else resolve();};requestAnimationFrame(tick);});this.turn=null;this.animating=false;this.dirty=true;
 }
 fit(){this.zoom=this.displayHost!==null?1.6:1.1;this.dirty=true;}
 setCellView(enabled){this.displayHost=enabled?this.focusCell:null;if(this.unrestrictedStyles)this.setState(this.labelsTex.pad.subarray(0,this.meta.slots),this.unrestrictedStyles);this.fit();}
 rebuild(){if(!this.styles)return;this.worker.postMessage({styles:this.styles,offsets:this.meta.offsets,base:this.meta.base_vertices,mode:this.mode,generation:++this.generation});}
 focus(cell){
  if(!Number.isInteger(cell)||cell<0||cell>=600)return;
  const f=this.arrays.frames.subarray(cell*16,(cell+1)*16);const n=Array.from(this.arrays.normals.subarray(cell*4,cell*4+4));let len=Math.hypot(...n);for(let i=0;i<4;i++)n[i]/=len;
  const basis=[];
  for(let col=0;col<4 && basis.length<3;col++){
   let v=Array.from(f.subarray(col*4,col*4+4));for(const u of [n,...basis]){let dot=v.reduce((a,x,i)=>a+x*u[i],0);v=v.map((x,i)=>x-dot*u[i]);}let l=Math.hypot(...v);if(l>1e-5)basis.push(v.map(x=>x/l));
  }
  this.q=[...basis.flat(),...n];this.dirty=true;this.focusCell=cell;if(this.displayHost!==null)this.setCellView(true);
 }
 rotate(a,b,theta){let c=Math.cos(theta),s=Math.sin(theta);for(let j=0;j<4;j++){const x=this.q[a*4+j],y=this.q[b*4+j];this.q[a*4+j]=c*x-s*y;this.q[b*4+j]=s*x+c*y;}this.dirty=true;}
 attachInput(){let drag=null;
  this.canvas.addEventListener('pointerdown',e=>{drag={x:e.clientX,y:e.clientY,startX:e.clientX,startY:e.clientY,button:e.button,moved:false};this.canvas.setPointerCapture(e.pointerId);});
  this.canvas.addEventListener('pointermove',e=>{if(!drag||this.animating)return;let dx=e.clientX-drag.x,dy=e.clientY-drag.y;drag.x=e.clientX;drag.y=e.clientY;drag.moved ||= Math.hypot(e.clientX-drag.startX,e.clientY-drag.startY)>4;if(!drag.moved)return;
   if(e.ctrlKey){this.zoom=Math.max(.2,Math.min(20,this.zoom*Math.exp(-dy*.01)));this.dirty=true;}else if(e.altKey||e.shiftKey||drag.button===2){this.rotate(0,3,dx*.005);this.rotate(e.shiftKey?2:1,3,dy*.005);}else{this.rotate(0,2,dx*.005);this.rotate(1,2,dy*.005);}
  });
  this.canvas.addEventListener('pointerup',e=>{if(!this.animating&&drag&&!drag.moved&&drag.button===0){const s=this.pick(e.clientX,e.clientY);if(s>=0)this.onPick?.(s,e);}drag=null;});
  this.canvas.addEventListener('pointercancel',()=>drag=null);this.canvas.addEventListener('contextmenu',e=>e.preventDefault());
  this.canvas.addEventListener('wheel',e=>{e.preventDefault();this.zoom=Math.max(.2,Math.min(20,this.zoom*Math.exp(-e.deltaY*.001)));this.dirty=true;},{passive:false});
  new ResizeObserver(()=>this.dirty=true).observe(this.canvas);
 }
 draw(picking=false){
  const g=this.gl,c=this.canvas,ratio=this.pixelScale,w=Math.max(1,Math.round(c.clientWidth*ratio)),h=Math.max(1,Math.round(c.clientHeight*ratio));if(c.width!==w||c.height!==h){c.width=w;c.height=h;}
  g.useProgram(this.program);g.bindVertexArray(this.vao);g.viewport(0,0,w,h);g.clearColor(picking?0:this.background[0],picking?0:this.background[1],picking?0:this.background[2],1);g.clear(g.COLOR_BUFFER_BIT|g.DEPTH_BUFFER_BIT);
  const qt=new Float32Array(16);for(let i=0;i<4;i++)for(let j=0;j<4;j++)qt[j*4+i]=this.q[i*4+j];g.uniformMatrix4fv(this.U('uQ'),false,qt);
  g.bindBuffer(g.ARRAY_BUFFER,this.buffer);g.vertexAttribIPointer(0,1,g.UNSIGNED_INT,4,0);g.uniform1i(this.U('uWire'),false);
  g.uniform1f(this.U('uTurnAngle'),this.turn?this.turn.angle*this.turn.progress:0);g.uniform4fv(this.U('uTurnU'),this.turn?.u||[0,0,0,0]);g.uniform4fv(this.U('uTurnV'),this.turn?.v||[0,0,0,0]);
  g.uniform1f(this.U('uCellScale'),this.cellScale);g.uniform1f(this.U('uStickerScale'),this.stickerScale);g.uniform1f(this.U('uD4'),this.distance4);g.uniform1f(this.U('uZoom'),this.zoom);g.uniform1f(this.U('uAspect'),w/h);g.uniform1i(this.U('uPoints'),this.actualMode==='points');g.uniform1i(this.U('uReference'),this.reference);g.uniform1i(this.U('uPicking'),picking);
  if(picking)g.disable(g.DITHER);else g.enable(g.DITHER);
  g.drawArrays(this.actualMode==='points'?g.POINTS:g.TRIANGLES,0,this.count);
  if(!picking&&this.wireframe&&this.wireCount&&!this.animating){g.uniform1i(this.U('uWire'),true);g.uniform1i(this.U('uPoints'),false);g.bindBuffer(g.ARRAY_BUFFER,this.wireBuffer);g.vertexAttribIPointer(0,1,g.UNSIGNED_INT,4,0);g.drawArrays(g.LINES,0,this.wireCount);g.bindBuffer(g.ARRAY_BUFFER,this.buffer);g.vertexAttribIPointer(0,1,g.UNSIGNED_INT,4,0);}
 }
 pick(x,y){
  // Non-multisampled offscreen ID buffer avoids edge blends producing false IDs.
  this.draw(false);const g=this.gl,w=this.canvas.width,h=this.canvas.height;
  if(!this.pickTarget||this.pickTarget.w!==w||this.pickTarget.h!==h){
   if(this.pickTarget){g.deleteFramebuffer(this.pickTarget.fb);g.deleteTexture(this.pickTarget.tex);g.deleteRenderbuffer(this.pickTarget.depth);}
   g.activeTexture(g.TEXTURE0+this.stylesTex.unit);const fb=g.createFramebuffer(),tex=g.createTexture(),depth=g.createRenderbuffer();g.bindTexture(g.TEXTURE_2D,tex);g.texImage2D(g.TEXTURE_2D,0,g.RGBA8,w,h,0,g.RGBA,g.UNSIGNED_BYTE,null);g.texParameteri(g.TEXTURE_2D,g.TEXTURE_MIN_FILTER,g.NEAREST);g.texParameteri(g.TEXTURE_2D,g.TEXTURE_MAG_FILTER,g.NEAREST);g.bindRenderbuffer(g.RENDERBUFFER,depth);g.renderbufferStorage(g.RENDERBUFFER,g.DEPTH_COMPONENT24,w,h);g.bindFramebuffer(g.FRAMEBUFFER,fb);g.framebufferTexture2D(g.FRAMEBUFFER,g.COLOR_ATTACHMENT0,g.TEXTURE_2D,tex,0);g.framebufferRenderbuffer(g.FRAMEBUFFER,g.DEPTH_ATTACHMENT,g.RENDERBUFFER,depth);if(g.checkFramebufferStatus(g.FRAMEBUFFER)!==g.FRAMEBUFFER_COMPLETE)throw Error('Picking framebuffer incomplete');this.pickTarget={fb,tex,depth,w,h};
   // Restore the sampler binding overwritten while allocating the picking texture.
   g.activeTexture(g.TEXTURE0+this.stylesTex.unit);g.bindTexture(g.TEXTURE_2D,this.stylesTex.t);
  }
  g.bindFramebuffer(g.FRAMEBUFFER,this.pickTarget.fb);this.draw(true);const p=new Uint8Array(4),r=this.canvas.getBoundingClientRect();g.readPixels(Math.max(0,Math.min(w-1,Math.floor((x-r.left)*w/r.width))),Math.max(0,Math.min(h-1,h-1-Math.floor((y-r.top)*h/r.height))),1,1,g.RGBA,g.UNSIGNED_BYTE,p);g.bindFramebuffer(g.FRAMEBUFFER,null);this.dirty=true;return p[0]+p[1]*256+p[2]*65536-1;
 }
 view(){return {q:this.q,cellScale:this.cellScale,stickerScale:this.stickerScale,distance4:this.distance4,zoom:this.zoom,mode:this.mode,focusCell:this.focusCell,pixelScale:this.pixelScale,wireframe:this.wireframe,displayHost:this.displayHost};}
 restore(v){if(!v)return;for(const k of ['cellScale','stickerScale','distance4','zoom'])if(Number.isFinite(v[k]))this[k]=v[k];if(Array.isArray(v.q)&&v.q.length===16&&v.q.every(Number.isFinite))this.q=v.q;this.pixelScale=[1,1.5,2].includes(v.pixelScale)?v.pixelScale:1;this.wireframe=v.wireframe!==false;this.mode=['auto','mesh','points'].includes(v.mode)?v.mode:'auto';this.focusCell=v.focusCell||0;this.displayHost=Number.isInteger(v.displayHost)&&v.displayHost>=0&&v.displayHost<600?v.displayHost:null;if(this.unrestrictedStyles)this.setState(this.labelsTex.pad.subarray(0,this.meta.slots),this.unrestrictedStyles);else this.rebuild();this.dirty=true;}
 async benchmark(frames=90){
  const samples=[],intervals=[],start=performance.now();let previous=null;
  for(let i=0;i<frames;i++){
   const now=await new Promise(requestAnimationFrame);if(previous!==null)intervals.push(now-previous);previous=now;
   this.rotate(0,3,.001);const t=performance.now();this.draw();this.gl.finish();samples.push(performance.now()-t);
  }
  const wall=performance.now()-start;samples.sort((a,b)=>a-b);intervals.sort((a,b)=>a-b);
  const percentile=(a,x)=>a[Math.min(a.length-1,Math.floor(a.length*x))];
  return {mode:this.actualMode,visible_stickers:this.visible,vertices:this.count,canvas:[this.canvas.width,this.canvas.height],gpu:this.gpu,frames,p50_ms:percentile(samples,.5),p95_ms:percentile(samples,.95),max_ms:Math.max(...samples),raf_interval_p50_ms:percentile(intervals,.5),raf_interval_p95_ms:percentile(intervals,.95),wall_ms:wall,draw_timer_resolution_warning:percentile(samples,.5)<.01,measurement:'Draw CPU wall time around gl.finish, plus frame intervals including browser scheduling. Low-resolution/zero draw timings must not be interpreted as GPU execution speed. This is not end-to-end input latency.'};
 }
}
