import {Renderer} from '/web/renderer.js';
import {KeyboardRouter} from './keys.js';
import {workspaceDiagram,localDiagram,globalDiagram} from './graphics.js';

import {formatIdentity,formatPosition,formatCell,formatOrbit,parseId,labCellIndex} from './ids.js';

const $=id=>document.getElementById(id);
const esc=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const params=new URLSearchParams(location.search), token=params.get('token'), mode=params.get('mode')==='g1'?'g1':'g2';
document.body.classList.add(mode);$('modeTag').textContent=mode.toUpperCase()+' · REVIEW EXPERIMENT';
let state=null, structure=null, renderer=null, arrays=null, selectedMacro=null, selectedEffect=null;
let busy=false, tab=mode==='g1'?'keys':'block', showingDraft=false, pickReady=false, grip=null, globalView=false;
let selectedCell=null, returnFocus=null, viewReturnFocus=null, orbitChoice=null;
let draftUndo=[], lastDuration=0, bankChoice=null, pendingRender=null, lastRender=null, activeJob=null;
const defaultCommands={F1:'index',F2:'bank',F3:'macro-search',F4:'phase-input',F5:'filter',F6:'next-tab',F7:'local',F8:'global',F9:'keyboard',F10:'worksheet',F11:'puzzle-layout',F12:'review',
 'Control+Shift+KeyN':'next-pin','Control+Alt+KeyN':'next-activate','Control+Shift+KeyL':'next-locate','Control+Shift+Backspace':'next-clear',
 'Alt+Digit1':'bank-A','Alt+Digit2':'bank-B','Alt+Digit3':'bank-I','Alt+Digit4':'bank-M','Alt+Digit5':'bank-E'};
const defaultGripKeys=['Digit1','Digit2','Digit3','Digit4','Digit5','Digit6','Digit7','Digit8','Digit9','Digit0',...'QWERTYUIOP'].map(x=>x.length===1?'Key'+x:x);
const defaultTwists={KeyA:'H1',KeyS:'H2',KeyD:'H3',KeyF:'T1',KeyG:'T2',KeyH:'T3',KeyJ:'T4'};
const keyLabel=k=>k.replace('Key','').replace('Digit','');
const orbitProfile=o=>structure?.orbit_profiles?.find(x=>x.orbit===o);
const orbitName=o=>{const r=orbitProfile(o);return r?`${r.hosting_count}-cell · ${r.affecting_cap_count}-cap`:formatOrbit(o);};
const orbitDefinition=o=>{const r=orbitProfile(o);return r?`${orbitName(o)} · ${r.orientation_group==='trivial'?'no orientation':r.orientation_group+' orientation'}`:formatOrbit(o);};
const macroName=m=>m.name.replace(/^O\d+\s*·\s*/,orbitName(m.orbit)+' · ');
const bank=()=>state?.banks.find(x=>x.id===state.workspace.bank);
const inspected=()=>state?.inspected||state?.current;
const selection=()=>inspected()?.piece;
function message(text,error=false){$('status').textContent=text;$('status').classList.toggle('error',error);}
function bindings(){
 const saved=state?.workspace.keybinds||{}, b=saved.banks?.[state?.workspace.bank]||{};
 return {commands:{...defaultCommands,...saved.commands,...b.commands},twists:{...defaultTwists,...saved.twists,...b.twists},gripCodes:b.grips||saved.grips||defaultGripKeys};
}
const router=new KeyboardRouter({getState:()=>({enabled:!!state,busy,bankId:state?.workspace.bank,
 gripMode:$('gripMode').value,gripKeys:Object.fromEntries(bindings().gripCodes.map((k,i)=>[k,bank()?.slots[i]]).filter(([,c])=>c)),
 twistKeys:bindings().twists,commandKeys:bindings().commands,destination:$('inputDestination').value,phase:$('inputPhase').value,modal:$('dialog').open}),
 onGrip:cell=>{grip=cell;drawKeyboard();},onTwist:input=>{if(input.destination==='draft'){rememberDraft();showingDraft=true;}return run(()=>send('twist',{...input,state_hash:state.hash})).catch(report);},
 onCommand:name=>dispatch(name),onFeedback:text=>{$('gripFeedback').textContent=text;}});
router.attach(window);
const inputLabel=$('inputDestination').parentElement, phaseLabel=$('inputPhase').parentElement;
const deskHead=document.querySelector('.deskhead'), keyMeta=document.querySelector('.keymeta');
function fitCompactInput(){const compact=innerWidth<=650;if(compact){keyMeta.append(inputLabel,phaseLabel);}else{deskHead.prepend(inputLabel,phaseLabel);}document.body.style.setProperty('--input-dock-height',compact?$('keyboardStrip').offsetHeight+'px':'0px');}
new ResizeObserver(fitCompactInput).observe($('keyboardStrip'));
window.addEventListener('resize',fitCompactInput);
fitCompactInput();
$('canvas').addEventListener('focusin',event=>{const object=event.target.closest('[data-position],[data-identity],[data-cell]');if(!object)return;requestAnimationFrame(()=>{const outer=$('canvas').getBoundingClientRect(),inner=object.getBoundingClientRect(),margin=12;let delta=0;if(inner.left<outer.left+margin)delta=inner.left-outer.left-margin;else if(inner.right>outer.right-margin)delta=inner.right-outer.right+margin;$('canvas').scrollLeft+=delta;});});
function report(error){console.error(error);message(error.message||String(error),true);}
async function raw(path,body){
 const options={headers:{'X-C600-Token':token}};
 if(body!==undefined){options.method='POST';options.headers['Content-Type']='application/json';options.body=JSON.stringify(body);}
 const response=await fetch(path,options), data=await response.json();
 if(!response.ok||data.error)throw Error(data.error||response.statusText);
 return data;
}
async function api(body){
 let data=await raw('/api/experiment/command',body);
 if(data.job){activeJob=data.job;while(true){await new Promise(r=>setTimeout(r,45));const polled=await raw('/api/job/'+activeJob);if(polled.done){activeJob=null;if(polled.error)throw Error(polled.error);data=polled.result;break;}}}
 return data;
}
async function send(action,body={}){const result=await api({action,...body});if(result.workspace)adopt(result);else if(result.snapshot)adopt(result.snapshot);return result;}
async function run(fn){
 if(busy){message('An operation is busy. Input was not queued.',true);return;}
 busy=true;document.body.classList.add('busy');$('connection').textContent='Computing · input not queued';$('cancelAnalysis').hidden=false;
 const begin=performance.now();try{return await fn();}finally{lastDuration=performance.now()-begin;busy=false;document.body.classList.remove('busy');$('cancelAnalysis').hidden=true;$('connection').textContent=state?`${state.build} · isolated session`:'Disconnected';}
}
function adopt(next){
 const previous=state;state=next;
 if(previous&&previous.workspace.bank!==state.workspace.bank)router.reset('Bank switched; release held keys before using the new mapping.');
 if(next.render)lastRender=next;
 draw();
 if(next.render)installRender(next);else if(previous?.hash!==next.hash)refresh().catch(report);
}
async function refresh(){const next=await raw('/api/experiment/snapshot');adopt(next);}
function draw(){
 if(!state)return;const w=state.workspace;
 $('orbitButton').textContent=orbitName(w.orbit);$('orbitButton').title=orbitDefinition(w.orbit);
 $('currentButton').textContent=state.current?`${formatIdentity(state.current.piece)} at ${formatPosition(state.current.position)}`:'Choose a piece';
 $('nextButton').textContent=state.next?`${formatIdentity(state.next.piece)} at ${formatPosition(state.next.position)}`:'Not assigned';
 $('bankButton').innerHTML=`${esc(w.bank)} <kbd>F2</kbd>`;
 $('bankPurpose').textContent=`${w.bank} · ${bank()?.purpose||''}`;
 $('sourceTag').textContent=w.source;
 $('protectionSummary').textContent=`${state.protected.length} orbit locks · ${w.block.protected.length} position locks · ${w.prefix?'strict prefix':'net boundary'}`;
 document.querySelector('.protection').classList.toggle('conflict',state.review?.status==='Conflict');
 document.body.classList.toggle('draft-view',showingDraft);
 const p=inspected();
 $('selectionInfo').textContent=selectedCell?`${formatCell(selectedCell)} selected · camera unchanged`:p?`${formatIdentity(p.piece)} at ${formatPosition(p.position)} · Home ${formatPosition(p.piece)} · ${orbitName(p.orbit)}`:'Select a piece token or fixed position.';
 $('currentButton').title=state.current?`Identity ${formatIdentity(state.current.piece)} · current ${formatPosition(state.current.position)} · Home ${formatPosition(state.current.piece)}`:'Choose Current';
 $('nextButton').title=state.next?`Bookmark ${formatIdentity(state.next.piece)} · current ${formatPosition(state.next.position)} · Home ${formatPosition(state.next.piece)}`:'No bookmarked identity';
 document.querySelector('[data-command="next-activate"]').disabled=!state.next;
 $('inputDestination').value=w.input;$('inputPhase').value=w.phase;
 $('stateIdentity').textContent=`HEAD ${state.head} · ${state.hash.slice(0,10)} · ${Math.round(lastDuration)} ms`;
 for(const phase of ['prepare','macro','cleanup']){$(phase+'Count').textContent=phaseSummary(w.draft[phase]);document.querySelector(`[data-phase="${phase}"]`).classList.toggle('active',w.phase===phase);}
 $('previewButton').disabled=state.review?.status!=='Ready';$('commitButton').disabled=!state.executable;
 $('beforeButton').classList.toggle('active',!showingDraft);$('afterButton').classList.toggle('active',showingDraft);
 document.querySelectorAll('[data-tab]').forEach(button=>button.classList.toggle('active',button.dataset.tab===tab));
 drawLibrary();drawCanvas();drawLocal();drawKeyboard();drawEffect();
 if(state.review)message(`${state.review.status} · ${state.review.goal_note} · Prefix ${state.review.prefix.status}`,(state.review.status==='Conflict'));
}
function phaseSummary(recipe){if(!recipe.length)return 'Empty';return recipe.map(x=>x.kind==='star'?`O${x.orbit} star ${x.node}${x.sign<0?'⁻¹':''}`:`${x.moves.length} turn${x.moves.length===1?'':'s'}`).join(' · ');}
function drawLibrary(){
 const focusId=document.activeElement?.dataset?.macro;
 const query=$('macroSearch').value.toLowerCase(),scope=$('macroScope').value,kind=$('macroKind').value;
 const rows=state.library.filter(x=>(scope==='all'||x.orbit===state.workspace.orbit)&&(!query||(x.name+' '+x.note+' '+x.id).toLowerCase().includes(query))&&(kind==='all'||kind==='personal'&&x.tags.includes('personal')||kind==='star'&&x.recipe.some(y=>y.kind==='star')||kind==='orientation'&&x.effect?.fixed_orientation_count));
 $('macroList').innerHTML=rows.map(m=>`<button class="macro-row ${m.id===selectedMacro?'selected':''}" role="option" aria-selected="${m.id===selectedMacro}" data-macro="${esc(m.id)}" draggable="true"><span class="glyph">${m.recipe[0]?.sign===-1?'↶':'↷'}</span><span class="name">${esc(macroName(m))}</span><span class="detail">${m.effect?esc(m.effect.category)+' · '+m.effect.primitives+' turns':'Unverified · select to compute exact effect'}</span></button>`).join('')||'<div class="empty">No existing entries match these facets.</div>';
 if(focusId)$('macroList').querySelector(`[data-macro="${focusId}"]`)?.focus({preventScroll:true});
}
function drawCanvas(){
 const w=state.workspace;
 $('canvasTitle').textContent=tab==='keys'?`${w.bank} · Grip / Twist`:tab==='reference'?'Reference / cell correspondence':w.block.name;
 $('canvasEyebrow').textContent=`${orbitDefinition(w.orbit)} / ${tab==='keys'?'KEYBOARD WORKSPACE':tab==='reference'?'EXPLICIT REFERENCE':'WORKING BLOCK'}`;
 const effect=selectedEffect||state.review?.effect;
 $('effectScope').textContent=effect?`${selectedEffect?'MACRO BODY':'COMPLETE OPERATION'} · ${effect.category} · ${effect.primitives} primitives · ${effect.fixed_orientation_count?effect.fixed_orientation_count+' frame changes · ':''}${showingDraft?'Predicted occupants; committed state unchanged':'Actual occupants'}`:'Actual occupants · choose a macro to overlay its verified action.';
 if(tab==='keys'){
  const p=inspected();$('canvas').innerHTML=`<div class="key-workspace"><small>GRIP / TWIST · EXPERIMENTAL MECHANICS</small><h3>${esc(bank()?.purpose)}</h3><div class="bigstate">${grip?'C'+grip+' · '+esc($('gripMode').value)+' Grip':'Choose a grip to turn'}</div><p class="hint">The cap stays fixed while you change camera or filter. A Grip chooses the cap; a Twist appends an explicit legal word. Shift inverts a Twist.</p><div class="samplebuttons"><button data-sample="35778">1-cell · I35778</button><button data-sample="14056">2-cell / 18 caps</button><button data-sample="five">5-cell example</button><button data-sample="17810">20-cell · I17810</button></div><div class="note">${p?`Selected ${formatIdentity(p.piece)} at ${formatPosition(p.position)}\nHosting: ${p.current_cells.map(c=>'C'+c).join(' ')}\nAffecting caps: ${p.cap_cells.map(c=>'C'+c).join(' ')}\nOrientation: ${p.orientation_group}`:'Choose a structural example or inspect an identity.'}</div><div class="formrow"><button data-command="capture">Capture selected structure</button><button data-command="bank">Choose bank by ID</button><button data-command="phase-input">Inspect entered word</button></div><p class="hint">${esc(bank()?.requirements)}. Draft preview changes no committed labels. Live mode applies each accepted turn through the existing Session transaction.</p></div>`;
 }else if(tab==='reference'){
  $('canvas').innerHTML=structure?globalDiagram(structure,state.current?.current_cells||[],state.target?.current_cells||[]):'<div class="empty">Loading actual topology…</div>';
 }else $('canvas').innerHTML=workspaceDiagram(state,selectedEffect||state.review?.effect,selection(),{predicted:showingDraft,scope:selectedEffect?'body':'complete'});
}
function drawLocal(){const p=inspected();$('localTitle').textContent=globalView?'Global · two cell groups':'Local · '+(p?formatIdentity(p.piece):'selection');$('localContent').innerHTML=globalView&&structure?globalDiagram(structure,p?.current_cells||[],state.target?.current_cells||[]):p?localDiagram(p):'<div class="empty">Select a piece to inspect its actual structure.</div>';requestAnimationFrame(()=>{$('canvasOverflow').hidden=$('canvas').scrollWidth<=$('canvas').clientWidth+2;});}
function drawKeyboard(){
 if(!state)return;const b=bank(), keys=bindings();
 $('gripFeedback').textContent=grip?`C${grip} · retained world cap frame · ${$('inputDestination').value}/${$('inputPhase').value}`:b?.needs_capture?'Insertion mapping needs an explicit Current/Target capture.':'No held grip · choose a labelled cap';
 $('gripKeys').innerHTML=keys.gripCodes.map((code,i)=>`<button data-grip="${b?.slots[i]||''}" class="${grip===b?.slots[i]?'held':''}" ${!b?.slots[i]?'disabled':''} title="${b?.slots[i]?'Explicit fixed cap C'+b.slots[i]:'Unassigned slot; use Capture / edit'}">${esc(keyLabel(code))}<span>${b?.slots[i]?'C'+b.slots[i]:'—'}</span></button>`).join('');
 $('twistKeys').innerHTML=Object.entries(keys.twists).map(([code,axis])=>`<button data-twist="${esc(axis)}" title="${esc(axis)} in the current cap. Hold Shift or use the inverse toggle.">${esc(keyLabel(code))}<span>${esc(axis)}</span></button>`).join('')+`<button data-command="inverse-toggle" id="inverseToggle" class="${inversePointer?'active':''}" aria-pressed="${inversePointer}">Shift <span>Inverse</span></button>`;
}
let inversePointer=false;
function drawEffect(){
 const r=state.review,e=selectedEffect||r?.effect;
 if(!e){$('macroDetail').innerHTML='<span class="hint">Choose a macro, or enter an explicit operation in the phase strip.</span>';return;}
 const match=e.star?`Fixed target ${formatPosition(e.star.target)}${e.star.target===state.workspace.target?' matches destination.':' differs from destination; no retargeting.'}`:'Exact cycles and orientation changes remain inspectable.';
 const conflicts=(r?.block_conflicts||[]).map(p=>`<button data-conflict-position="${p}">Conflict at ${formatPosition(p)} · inspect protected position</button>`).join('')+(r?.conflicts||[]).map(x=>`<button data-command="protection">Conflict in ${orbitName(x.orbit)} · ${x.pieces} affected pieces</button>`).join('');
 $('macroDetail').innerHTML=`<div class="effect-summary"><strong>${esc(e.category)}</strong> · ${e.pieces} pieces / ${e.slots} slots<small>${esc(match)} Full support: ${e.support.map(x=>orbitName(x.orbit)).join(', ')||'identity'}.</small></div><div class="effect-actions">${selectedEffect?'<button data-command="insert-prepare">→ Prepare</button><button class="primary" data-command="insert-macro">Use as Macro</button><button data-command="insert-cleanup">→ Cleanup</button>':''}<button data-command="effect-details">Exact effects</button></div>${conflicts?'<div class="review-findings">'+conflicts+'</div>':''}`;
}
function decode(data,Type){const bytes=Uint8Array.from(atob(data),c=>c.charCodeAt(0));return new Type(bytes.buffer);}
function installRender(snapshot){
 lastRender=snapshot;if(!renderer){pendingRender=snapshot;return;}
 const render=showingDraft&&snapshot.predicted?snapshot.predicted:snapshot.render;
 if(!render)return;pickReady=false;
 $('viewportTitle').textContent=showingDraft&&snapshot.predicted?'Actual puzzle · proposed draft result':'Actual puzzle · committed';
 $('viewportMessage').textContent='Updating coherent full-model frame…';$('viewportMessage').hidden=false;
 const generation=renderer.generation;
 renderer.setState(decode(render.labels,Uint32Array),decode(render.styles,Uint8Array));
 const displayedHash=render.state_hash;
 const ready=()=>{if(state!==snapshot&&lastRender!==snapshot)return;renderer.draw();pickReady=!showingDraft&&displayedHash===state.hash;$('viewportMessage').textContent=showingDraft?'Read-only proposed state · no committed moves':renderer.actualMode==='points'?'Point glyph view · exact slot identities; mesh geometry picking is separate':'';$('viewportMessage').hidden=!$('viewportMessage').textContent;};
 renderer.experimentReady=ready;
 if(renderer.generation===generation)requestAnimationFrame(ready);
}
async function initializeRenderer(){
 const meta=await fetch('/assets/mesh.json').then(r=>r.json());
 const paths={vertices:'mesh_vertices.f32',local:'mesh_sticker.u32',centers:'mesh_centers.f32',frames:'cell_frames.f32',normals:'cell_normals.f32',sp:'slot_piece.u32'};
 arrays={};await Promise.all(Object.entries(paths).map(async([key,path])=>{const buffer=await fetch('/assets/'+path).then(r=>r.arrayBuffer());arrays[key]=['local','sp'].includes(key)?new Uint32Array(buffer):new Float32Array(buffer);}));
 renderer=new Renderer($('puzzle'),meta,arrays,(slot,event)=>{
  if(!pickReady||busy||showingDraft)return message('Wait for a current committed frame before picking.',true);
  if(slot<0||slot>=arrays.sp.length)return;
  const labels=renderer.labelsTex.pad;const identity=arrays.sp[labels[slot]];
  run(()=>send('inspect',{identity})).then(()=>{if(event.shiftKey)return dispatch('focus');}).catch(report);
 },info=>{if(info.error){pickReady=false;$('viewportMessage').hidden=false;$('viewportMessage').textContent=info.error;}else if(info.mode)$('renderInfo').textContent=`${info.mode} · ${Number(info.visible).toLocaleString()} slots`;});
 renderer.background=[.065,.085,.095];renderer.pixelScale=Math.min(2,window.devicePixelRatio||1);
 const originalMessage=renderer.worker.onmessage;
 renderer.worker.onmessage=event=>{const valid=event.data.generation===renderer.generation;originalMessage(event);if(valid)requestAnimationFrame(()=>renderer.experimentReady?.());};
 document.addEventListener('visibilitychange',()=>{if(document.hidden){cancelAnimationFrame(renderer.raf);router.reset('Window hidden');}else{renderer.dirty=true;renderer.animate();}});
 window.addEventListener('beforeunload',()=>{cancelAnimationFrame(renderer.raf);renderer.worker.terminate();router.dispose();});
 if(pendingRender)installRender(pendingRender);
}
function openDialog(title,html,focus){if(!$('dialog').open)returnFocus=document.activeElement;router.reset('Dialog owns input');$('dialogTitle').textContent=title;$('dialogBody').innerHTML=html;if(!$('dialog').open)$('dialog').showModal();setTimeout(()=>$(focus||'dialogBody')?.focus(),0);}
function closeDialog(){$('dialog').close();}
$('dialog').addEventListener('close',()=>{router.reset('Dialog closed; release held keys');(returnFocus?.isConnected?returnFocus:$('canvas')).focus({preventScroll:true});});
function showViews(puzzle=false){viewReturnFocus=document.activeElement;document.body.classList.add('views-open');document.body.classList.toggle('puzzle-open',puzzle);if(puzzle)requestAnimationFrame(()=>renderer?.fit());}
function rememberDraft(){if(state){draftUndo.push(JSON.parse(JSON.stringify(state.workspace.draft)));if(draftUndo.length>30)draftUndo.shift();}}
function requireSelection(){const p=selection();if(p===undefined||p===null)throw Error('Select a piece identity first.');return p;}
const commands={
 async 'stop-experiment'(){if(busy)throw Error('Wait for the current operation or cancel its analysis before stopping.');const health=await raw('/api/health');await raw('/api/shutdown',{launch_id:health.launch_id});closeDialog();router.dispose();cancelAnimationFrame(renderer?.raf);renderer?.worker.terminate();message('This isolated engine has stopped. Relaunch the same entry to resume.');$('connection').textContent='Stopped';},
 orbit(){orbitChoice=state.workspace.orbit;openDialog('Choose an orbit by structure',`<p class="hint">Hosting cells, affecting caps and orientation come from the retained model. Equal counts can describe different orbits: inspect their cap arrangements before choosing.</p><input id="orbitSearch" placeholder="Search mathematical structure" aria-label="Orbit structure search"><div id="orbitList" class="banklist"></div><div id="orbitProfile" class="orbit-profile"></div><button data-command="orbit-activate">Use this orbit</button>`,'orbitSearch');drawOrbits();},
 async 'orbit-activate'(){await run(()=>send('orbit',{orbit:Number(orbitChoice)}));closeDialog();},

 'library-toggle'(){document.body.classList.toggle('library-open');if(document.body.classList.contains('library-open'))$('macroSearch').focus();},
 'close-views'(){document.body.classList.remove('views-open','puzzle-open');(viewReturnFocus?.isConnected?viewReturnFocus:$('canvas')).focus({preventScroll:true});},
 'next-menu'(){openDialog('Locked Next · identity bookmark',`<p class="hint">${state.next?formatIdentity(state.next.piece)+' at '+formatPosition(state.next.position):'Not assigned'}. A bookmark does not protect mechanical state.</p><div class="formrow"><button data-command="next-locate">Locate</button><button data-command="next-pin">Replace with inspected identity</button><button data-command="next-clear">Clear bookmark</button><button data-command="next-activate">Activate explicitly</button></div>`);},
 'copy-selection'(){const p=inspected();if(p)navigator.clipboard.writeText(formatIdentity(p.piece)).then(()=>message('Copied '+formatIdentity(p.piece))).catch(report);},
 index(){openDialog('Command index',`<input id="commandSearch" placeholder="Search an action" aria-label="Search commands"><div id="commandList" class="banklist"></div>`,'commandSearch');drawCommands();},
 bank(){bankChoice=state.workspace.bank;openDialog('Keyboard banks · choose by ID',`<input id="bankSearch" placeholder="33-I, orbit, or purpose" aria-label="Bank ID or purpose"><div id="bankList" class="banklist"></div><div id="bankDescription" class="bankdescription note"></div><div class="formrow"><button data-command="bank-activate">Activate selected bank</button><button data-command="bank-previous">Previous bank</button><button data-command="bank-rename">Rename current</button></div>`,'bankSearch');drawBanks();},
 async 'bank-activate'(){const id=bankChoice;router.reset('Bank switch');await run(()=>send('bank',{id}));closeDialog();message(`${id} · ${bank()?.purpose}. ${bank()?.requirements}`);},
 async 'bank-previous'(){await run(()=>send('bank',{id:state.workspace.previous_bank}));closeDialog();},
 'bank-rename'(){openDialog('Rename current bank',`<input id="bankName" value="${esc(bank().name)}"><div class="formrow"><button data-command="bank-save-name">Save name</button></div>`,'bankName');},
 async 'bank-save-name'(){await run(()=>send('bank-name',{name:$('bankName').value}));closeDialog();},
 focus(){const p=inspected();openDialog('Current piece and destination',`<p class="hint">Identity follows the piece. Destination remains a fixed position. Choosing a piece does not choose a macro.</p><div class="formrow"><label>Piece identity (I)</label><input id="focusIdentity" type="text" value="${p?formatIdentity(p.piece):''}" placeholder="I35778"><label>Fixed position (P)</label><input id="focusTarget" type="text" value="${p?formatPosition(p.piece):''}" placeholder="P35778"></div><button data-command="focus-apply">Make Current</button>`,'focusIdentity');},
 async 'focus-apply'(){await run(()=>send('focus',{identity:parseId($('focusIdentity').value,'identity'),target:parseId($('focusTarget').value,'position')}));closeDialog();},
 async 'next-pin'(){const identity=requireSelection();if(state.workspace.next){openDialog('Replace locked Next?',`<p class="hint">Next tracks ${formatIdentity(state.workspace.next.identity)}. Replace it with ${formatIdentity(identity)}? This does not protect or move the piece.</p><button data-command="next-replace">Replace Next explicitly</button>`);return;}await run(()=>send('next-pin',{identity}));},
 async 'next-replace'(){await run(()=>send('next-pin',{identity:requireSelection(),replace:true}));closeDialog();},
 async 'next-clear'(){await run(()=>send('next-clear'));},
 async 'next-activate'(){rememberDraft();await run(()=>send('next-activate'));selectedEffect=null;draw();},
 async 'next-locate'(){if(!state.next)throw Error('No Next piece is locked.');await run(()=>send('inspect',{identity:state.next.piece}));renderer?.focus(state.next.current_cells[0]-1);message('Located Next; Current and destination remain unchanged.');},
 async 'block-add'(){await run(()=>send('block-add',{identity:requireSelection()}));},
 async 'block-protect'(){const p=inspected();if(!p)throw Error('Select a position first.');await run(()=>send('block-protect',{position:p.position}));message(`Preserving exact labels at ${formatPosition(p.position)} at the complete-operation boundary.`);},
 roles(){openDialog('Explicit A / B buffer positions',`<div class="formrow"><label>A</label><input id="roleA" type="text" value="${formatPosition(state.workspace.roles[0])}"><label>B</label><input id="roleB" type="text" value="${formatPosition(state.workspace.roles[1])}"></div><p class="hint">Role assignment edits the work context. It does not relocate a macro or move an occupant.</p><button data-command="roles-apply">Assign roles</button>`,'roleA');},
 async 'roles-apply'(){await run(()=>send('roles',{positions:[parseId($('roleA').value,'position'),parseId($('roleB').value,'position')]}));closeDialog();},
 reference(){openDialog('Explicit reference transform',`<p class="hint">Canonical model reference uses an empty word. To transform a selected macro, enter the exact legal reference word R. The result is R⁻¹ / Macro / R. No setup is searched and no frame is chosen by target success.</p><textarea id="referenceWord" aria-label="Explicit reference primitive word">${JSON.stringify(state.workspace.reference)}</textarea><div class="formrow"><button data-command="reference-save">Use this reference word</button><button data-command="reference-transform">Save selected macro under reference</button></div>`,'referenceWord');},
 async 'reference-save'(){await run(()=>send('reference',{word:JSON.parse($('referenceWord').value)}));closeDialog();},
 async 'reference-transform'(){if(!selectedMacro)throw Error('Select an existing macro first.');const word=JSON.parse($('referenceWord').value);await run(async()=>{await send('reference',{word});await send('transform-macro',{id:selectedMacro});});closeDialog();},
 'macro-search'(){document.body.classList.add('library-open');$('macroSearch').focus();},
 'macro-new'(){openDialog('Input and save a concrete macro',`<input id="macroName" placeholder="Your macro name"><p class="hint">Supply native JSON recipe steps: word with moves, or an explicitly chosen star with orbit/node/sign. No target solving occurs.</p><textarea id="macroRecipe" aria-label="Concrete macro recipe">[{"kind":"word","moves":[1]}]</textarea><input id="macroNote" placeholder="Purpose / notes"><div class="formrow"><button data-command="macro-save">Validate and save</button></div>`,'macroName');},
 async 'macro-save'(){await run(()=>send('save-macro',{name:$('macroName').value,recipe:JSON.parse($('macroRecipe').value),note:$('macroNote').value}));closeDialog();},
 'macro-notes'(){const m=state.library.find(x=>x.id===selectedMacro);if(!m)throw Error('Select a macro first.');openDialog('Macro name and purpose',`<input id="macroName" value="${esc(m.name)}"><textarea id="macroNote">${esc(m.note)}</textarea><button data-command="macro-save-notes">Save notes</button>`,'macroName');},
 async 'macro-save-notes'(){await run(()=>send('macro-note',{id:selectedMacro,name:$('macroName').value,note:$('macroNote').value}));closeDialog();},
 'macro-compare'(){const options=state.library.map(x=>`<option value="${esc(x.id)}">${esc(macroName(x))}</option>`).join('');openDialog('Compare exact macro actions',`<div class="formrow"><select id="compareA">${options}</select><select id="compareB">${options}</select></div><button data-command="macro-run-compare">Compare full model</button><div id="compareResult" class="note">Full equality and orbit-local equality remain distinct.</div>`);if(selectedMacro)$('compareA').value=selectedMacro;},
 async 'macro-run-compare'(){const r=await run(()=>send('macro-compare',{a:$('compareA').value,b:$('compareB').value}));if(r)$('compareResult').textContent=`Full net equality: ${r.full_equal}\nInverse: ${r.inverse}\nEqual locally on orbits: ${r.locally_equal.join(', ')}\n${r.note}`;},
 'phase-input'(){phaseDialog($('inputPhase').value);},
 async 'phase-save'(){rememberDraft();await run(()=>send('draft',{phase:$('phaseName').value,recipe:JSON.parse($('phaseRecipe').value)}));closeDialog();},
 async 'draft-undo'(){const previous=draftUndo.pop();if(!previous)throw Error('No previous draft edit in this window.');await run(async()=>{for(const phase of ['prepare','macro','cleanup'])await send('draft',{phase,recipe:previous[phase]});});},
 async 'cleanup-inverse'(){rememberDraft();await run(()=>send('inverse-cleanup'));},
 async review(){await run(()=>send('review'));selectedEffect=null;tab='block';draw();},
 async preview(){if(!state.review)throw Error('Review the complete operation first.');await run(()=>send('preview',{review_id:state.review.id}));},
 async commit(){await run(()=>send('commit'));showingDraft=false;await refresh();message('Committed through Session. Inspect the block and locked Next before choosing the next operation.');},
 async 'cancel-preview'(){await run(()=>send('cancel-preview'));},
 async 'cancel-analysis'(){await raw('/api/stop-job',{});message('Cancellation requested; committed state is preserved.');},
 before(){showingDraft=false;draw();if(lastRender)installRender(lastRender);},
 async after(){if(!Object.values(state.workspace.draft).some(p=>p.length))throw Error('The draft is empty. Choose a macro or enter turns before viewing its result.');await run(()=>send('review'));selectedEffect=null;showingDraft=true;draw();if(lastRender)installRender(lastRender);},
 'next-tab'(){const tabs=['block','reference','keys'];tab=tabs[(tabs.indexOf(tab)+1)%tabs.length];draw();},
 local(){globalView=false;drawLocal();showViews();},
 global(){globalView=true;drawLocal();showViews();},
 keyboard(){$('keyboardKeys').hidden=!$('keyboardKeys').hidden;$('keyboardToggle').setAttribute('aria-expanded',String(!$('keyboardKeys').hidden));},
 'puzzle-layout'(){showViews(true);},
 'camera-home'(){renderer?.focus(0);renderer?.fit();message('View reset; puzzle and work context unchanged.');},
 'locate-current'(){if(!state.current)throw Error('Choose Current first.');renderer?.focus(state.current.current_cells[0]-1);},
 'locate-inspected'(){const p=inspected();if(selectedCell||p){showViews(true);renderer?.focus(labCellIndex(selectedCell||p.current_cells[0]));}},
 'inverse-toggle'(){inversePointer=!inversePointer;const b=$('inverseToggle');b?.classList.toggle('active',inversePointer);},
 capture(){captureDialog();},
 async 'capture-apply'(){const fields=[...$('dialogBody').querySelectorAll('[data-capture-slot]')].map(x=>x.value.trim());while(fields.length&&!fields.at(-1))fields.pop();if(fields.some(x=>!x))throw Error('An empty Grip slot between mapped slots would shift the keys. Fill the gap or clear every later slot. Current mapping preserved.');const cells=fields.map(x=>parseId(x,'cell'));await run(()=>send('capture',{cells}));closeDialog();router.reset('Explicit new cap capture applied.');},
 'keys-edit'(){openDialog('Editable key bindings',`<p class="hint">Mappings use physical codes. Edit commands, twists and the twenty Grip codes globally or under banks[bank ID]. Text/IME and scoped preview/commit rules remain enforced.</p><textarea id="keysJson" aria-label="Keyboard configuration">${esc(JSON.stringify(state.workspace.keybinds,null,2))}</textarea><div class="formrow"><button data-command="keys-save">Save mappings</button><button data-command="keys-defaults">Show editable defaults</button><button data-command="keys-export">Export</button></div>`,'keysJson');},
 'keys-defaults'(){$('keysJson').value=JSON.stringify({commands:defaultCommands,twists:defaultTwists,grips:defaultGripKeys,banks:{}},null,2);},
 async 'keys-save'(){const config=JSON.parse($('keysJson').value);if(config.grips&&(!Array.isArray(config.grips)||config.grips.length!==20))throw Error('Grip mapping requires twenty physical key codes.');await run(()=>send('settings',{keybinds:config}));closeDialog();router.reset('Bindings changed');},
 'keys-export'(){download('magic600-keys.json',$('keysJson').value);},
 protection(){const set=new Set(state.protected);openDialog('Orbit and block protection',`<p class="hint">Protection always checks the full model, including hidden pieces. Whole-orbit locks preserve the current exact labelled state.</p><div class="gridchecks">${state.progress.map(p=>`<label title="${esc(orbitDefinition(p.orbit))}" class="${set.has(p.orbit)?'checked':''}"><input type="checkbox" data-protect-orbit="${p.orbit}" ${set.has(p.orbit)?'checked':''}>${esc(orbitName(p.orbit))}</label>`).join('')}</div><div class="formrow"><label><input id="prefixPolicy" type="checkbox" ${state.workspace.prefix?'checked':''}> Require preservation at every intermediate primitive</label></div><p class="hint">Position locks: ${state.workspace.block.protected.map(p=>formatPosition(p.position)).join(', ')||'none'}</p><div class="formrow"><button data-command="protection-save">Apply policy</button><button data-command="protection-clear-positions">Clear position locks explicitly</button></div><div class="note">${state.review?esc(JSON.stringify({net:state.review.conflicts,positions:state.review.block_conflicts,prefix:state.review.prefix},null,2)):'No current complete-operation review.'}</div>`);},
 async 'protection-save'(){const orbits=[...$('dialogBody').querySelectorAll('[data-protect-orbit]:checked')].map(x=>Number(x.dataset.protectOrbit)),strict=$('prefixPolicy').checked;await run(async()=>{await send('protect',{orbits});await send('prefix',{strict});});closeDialog();},
 async 'protection-clear-positions'(){const positions=state.workspace.block.protected.map(p=>p.position);await run(async()=>{for(const position of positions)await send('block-protect',{position,enabled:false});});closeDialog();},
 filter(){openDialog('Piece Filter workbench',`<p class="hint">Work membership controls display and picking. It never narrows mechanical analysis or protection.</p><input id="filterExpr" aria-label="Filter expression" value="${esc(state.workspace.filter)}"><div class="formrow"><select id="filterOperation"><option value="replace">Replace</option><option value="intersect">Intersect</option><option value="union">Union</option><option value="subtract">Subtract</option></select><button data-command="filter-preview">Preview counts</button><button data-command="filter-apply">Apply</button></div><div class="cellbuttons"><button data-filter-preset="active">Active orbit</button><button data-filter-preset="active & unsolved">Unsolved active</button><button data-filter-preset="everything">All pieces</button><button data-filter-preset="orientation_wrong">Orientation only</button></div><div class="formrow"><input id="filterName" placeholder="Saved filter name"><button data-command="filter-save">Save current</button><select id="savedFilter"><option value="">Saved filters</option>${Object.keys(state.workspace.saved_filters).map(n=>`<option>${esc(n)}</option>`).join('')}</select></div><div id="filterResult" class="note">Preview before applying a compound expression.</div>`,'filterExpr');},
 async 'filter-preview'(){const result=await run(()=>send('filter',{expression:$('filterExpr').value,operation:$('filterOperation').value,preview:true}));if(result)$('filterResult').textContent=JSON.stringify(result,null,2);},
 async 'filter-apply'(){await run(()=>send('filter',{expression:$('filterExpr').value,operation:$('filterOperation').value}));closeDialog();},
 async 'filter-save'(){await run(()=>send('save-filter',{name:$('filterName').value}));message('Saved current filter.');},
 worksheet(){openDialog('Reusable manual work sheets',`<p class="hint">A work sheet keeps fixed concrete steps and your explicit reference. Current/Next remain yours. Reuse never chooses another macro or restores old safety results.</p><div class="formrow"><input id="sheetName" placeholder="Work sheet name"><button data-command="sheet-save">Save current</button></div><div class="formrow"><select id="sheetSelect">${Object.keys(state.workspace.templates).map(x=>`<option>${esc(x)}</option>`).join('')}</select><button data-command="sheet-use">Reuse fixed steps</button></div><div class="note">${esc(phaseSummary(state.workspace.draft.prepare)+' / '+phaseSummary(state.workspace.draft.macro)+' / '+phaseSummary(state.workspace.draft.cleanup))}</div>`,'sheetName');},
 async 'sheet-save'(){await run(()=>send('template-save',{name:$('sheetName').value}));closeDialog();},
 async 'sheet-use'(){rememberDraft();await run(()=>send('template-use',{name:$('sheetSelect').value}));closeDialog();},
 'effect-details'(){const e=selectedEffect||state.review?.effect;if(!e)throw Error('Select or review an explicit operation first.');openDialog('Exact operation effects',`<p class="hint">${esc(e.category)} · complete scope: ${e.slots} labelled slots. ${e.slot_pairs_truncated?'First 160 slot pairs shown; complete effect was checked.':''}</p><div class="note">${esc(JSON.stringify({star:e.star,reference:state.workspace.reference,support:e.support,cycles:e.cycles,fixed_orientation:e.fixed_orientation,slot_pairs:e.slot_pairs},null,2))}</div>`);},
 session(){openDialog('Isolated experiment session',`<p class="hint">${esc(state.workspace.source)}<br>${esc(state.build)} · model ${state.model.slice(0,16)}…<br>G1 and G2 are both pending independent user approval.</p><div class="formrow"><button data-command="fixture-e1">Load explicit E1 practice</button><button data-command="undo">Undo committed</button><button data-command="redo">Redo committed</button></div><div class="formrow"><input id="checkpointName" placeholder="Checkpoint name"><button data-command="checkpoint">Save checkpoint</button></div><div class="formrow"><select id="checkpointSelect">${state.checkpoints.map(x=>`<option value="${esc(x.name)}">${esc(x.name)} · #${x.head}</option>`).join('')}</select><button data-command="restore">Restore checkpoint</button></div><div class="formrow"><button data-command="reset-puzzle">Reset puzzle with recovery point</button><button data-command="export-work">Export work definitions</button><button data-command="stop-experiment">Stop experiment</button></div><p class="hint">New starts solved. Resume retains this experiment's isolated journal. E1 is synthetic practice, not evidence of a human complete solve. Browser interaction is an experiment, not native Windows/DirectX certification.</p>`);},
 async 'fixture-e1'(){await run(()=>send('fixture',{name:'e1'}));selectedMacro=null;selectedEffect=null;showingDraft=false;closeDialog();message('E1 loaded through a legal explicit recipe. Choose node 0 inverse in Macro Base.');},
 async undo(){await run(()=>send('undo'));closeDialog();},async redo(){await run(()=>send('redo'));closeDialog();},
 async checkpoint(){await run(()=>send('checkpoint',{name:$('checkpointName').value||undefined}));closeDialog();},
 async restore(){await run(()=>send('restore',{name:$('checkpointSelect').value}));closeDialog();selectedEffect=null;},
 async 'reset-puzzle'(){await run(()=>send('reset'));closeDialog();showingDraft=false;message('Puzzle reset. The previous solve remains in a recovery checkpoint.');},
 'export-work'(){download('magic600-experimental-work.json',JSON.stringify(state.workspace,null,2));}
};
for(const suffix of ['A','B','I','M','E'])commands['bank-'+suffix]=()=>run(()=>send('bank',{id:bank().orbit+'-'+suffix}));
for(const phase of ['prepare','macro','cleanup'])commands['insert-'+phase]=async()=>{if(!selectedMacro)throw Error('Select a macro first.');rememberDraft();await run(()=>send('insert-macro',{id:selectedMacro,phase,append:phase!=='macro'}));};
async function dispatch(name){try{if(!state)return;const fn=commands[name];if(!fn)throw Error('Command is not available: '+name);await fn();}catch(error){report(error);}}
const formOnly=new Set(['orbit-activate','bank-activate','bank-save-name','focus-apply','next-replace','roles-apply','reference-save','reference-transform','macro-save','macro-save-notes','macro-run-compare','phase-save','capture-apply','keys-save','keys-defaults','keys-export','protection-save','filter-preview','filter-apply','filter-save','sheet-save','sheet-use','checkpoint','restore']);
function drawCommands(){const query=$('commandSearch').value.toLowerCase();$('commandList').innerHTML=Object.keys(commands).filter(name=>!formOnly.has(name)&&name.includes(query)).map(name=>`<button class="bankrow" data-index-command="${esc(name)}">${esc(name.replaceAll('-',' '))}</button>`).join('');}
function drawOrbits(){const query=$('orbitSearch').value.toLowerCase(),rows=(structure?.orbit_profiles||[]).filter(r=>orbitDefinition(r.orbit).toLowerCase().includes(query));$('orbitList').innerHTML=rows.map(r=>`<button class="bankrow ${r.orbit===orbitChoice?'selected':''}" data-orbit-choice="${r.orbit}">${esc(orbitDefinition(r.orbit))}<small>${r.pieces.toLocaleString()} pieces · select to compare cap geometry</small></button>`).join('')||'<p class="hint">No mathematical descriptions match.</p>';const r=orbitProfile(orbitChoice);$('orbitProfile').innerHTML=r?`<p class="hint">Selected: ${esc(orbitDefinition(r.orbit))}. Group A = hosting cells; Group B = affecting caps. This is the actual representative arrangement, not an orbit number.</p>`+globalDiagram(structure,r.hosting_cells,r.affecting_caps):'';}
function drawBanks(){const query=$('bankSearch').value.toLowerCase();const rows=state.banks.filter(b=>(b.id+' '+b.purpose+' '+b.name).toLowerCase().includes(query));if(rows.length===1)bankChoice=rows[0].id;$('bankList').innerHTML=rows.map(b=>`<button class="bankrow ${b.id===bankChoice?'selected':''}" data-bank-choice="${b.id}"><strong>${b.id}</strong>${esc(b.name)}<small>${esc(orbitDefinition(b.orbit))} · A ${formatPosition(b.buffers[0])} · B ${formatPosition(b.buffers[1])} · ${b.slots.length} captured caps · ${esc(b.requirements)}</small></button>`).join('');const selected=state.banks.find(x=>x.id===bankChoice);$('bankDescription').textContent=selected?`${selected.id} · ${selected.purpose}\n${selected.requirements}\nMacros: ${selected.macros.join(' / ')}\nCaps: ${selected.slots.map(c=>'C'+c).join(' ')||'explicit capture needed'}`:'Choose a matching bank.';}
function phaseDialog(phase){openDialog('Edit '+phase+' · explicit chronological recipe',`<input id="phaseName" type="hidden" value="${phase}"><p class="hint">An empty phase is []. Editing changes only the draft. Raw finite word/star recipes remain inspectable.</p><textarea id="phaseRecipe" aria-label="${phase} recipe">${esc(JSON.stringify(state.workspace.draft[phase],null,2))}</textarea><div class="formrow"><button data-command="phase-save">Apply draft edit</button></div>`,'phaseRecipe');}
function captureDialog(){const p=inspected(),old=bank()?.slots||[];openDialog('Capture fixed cap mappings',`<p class="hint">Choose hosting cells or affecting caps, then inspect and Apply. Mapping remains fixed until you explicitly capture again. Frames use the retained world cap basis.</p><div class="formrow"><button data-capture-source="hosting">Selected hosting cells</button><button data-capture-source="caps">Selected affecting caps</button><button data-capture-source="A">Buffer A caps</button><button data-capture-source="B">Buffer B caps</button></div><div class="note">Current mapping: ${old.map(c=>'C'+c).join(' ')||'unassigned'}\nSelected ${p?formatIdentity(p.piece)+' · '+p.current_cells.length+' hosting / '+p.cap_cells.length+' affecting':'none'}\nEnter any C1–C600; additional cap pages can be captured explicitly.</div><div class="capture-grid">${bindings().gripCodes.map((code,i)=>`<label>${esc(keyLabel(code))}<input type="number" min="1" max="600" data-capture-slot="${i}" value="${old[i]||''}" aria-label="Grip ${esc(keyLabel(code))} canonical cell"></label>`).join('')}</div><button data-command="capture-apply">Apply explicit capture</button>`);}
function download(name,text){const a=document.createElement('a'),url=URL.createObjectURL(new Blob([text],{type:'application/json'}));a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
document.addEventListener('click',event=>{
 const target=event.target.closest('button,[data-identity],[data-cell],[data-position]');if(!target)return;
 if(target.dataset.command){dispatch(target.dataset.command);return;}
 if(target.dataset.indexCommand){const name=target.dataset.indexCommand;closeDialog();dispatch(name);return;}
 if(target.dataset.tab){tab=target.dataset.tab;drawCanvas();document.querySelectorAll('[data-tab]').forEach(b=>b.classList.toggle('active',b===target));return;}
 if(target.dataset.phase){phaseDialog(target.dataset.phase);return;}
 if(target.dataset.macro){selectedMacro=target.dataset.macro;selectedEffect=null;drawLibrary();run(async()=>{const result=await send('macro-effect',{id:selectedMacro});selectedEffect=result.effect;if(tab==='reference'||tab==='keys')tab='block';showingDraft=false;if(lastRender)installRender(lastRender);draw();message(result.applicability.reasons.join(' · ')||'Exact effect loaded. Choose its role in the draft.');}).catch(report);return;}
 if(target.dataset.orbitChoice){orbitChoice=Number(target.dataset.orbitChoice);drawOrbits();return;}
 if(target.dataset.bankChoice){bankChoice=target.dataset.bankChoice;drawBanks();return;}
 if(target.dataset.grip){const cell=Number(target.dataset.grip);if(grip===cell)router.releasePointerGrip();else router.setPointerGrip(cell);return;}
 if(target.dataset.twist){router.twist(target.dataset.twist,inversePointer||event.shiftKey);return;}
 if(target.dataset.identity){selectedCell=null;run(()=>send('inspect',{identity:Number(target.dataset.identity)})).catch(report);return;}
 if(target.dataset.position||target.dataset.conflictPosition){selectedCell=null;run(()=>send('inspect-position',{position:Number(target.dataset.position||target.dataset.conflictPosition)})).catch(report);return;}
 if(target.dataset.cell){selectedCell=Number(target.dataset.cell);$('selectionInfo').textContent=`${formatCell(selectedCell)} selected · use Locate selection to move the camera`;message('Cell selection is inspect-only; Grip mapping and camera unchanged.');return;}
 if(target.dataset.sample){const p=target.dataset.sample==='five'?structure?.examples?.five:Number(target.dataset.sample);if(!Number.isInteger(p))return message('Five-cell fixture is loading.',true);run(()=>send('focus',{identity:p})).catch(report);return;}
 if(target.dataset.captureSource){const p=inspected(),source=target.dataset.captureSource;let cells=source==='hosting'?p?.current_cells:source==='caps'?p?.cap_cells:state.buffers[source==='A'?0:1]?.cap_cells;cells=cells||[];$('dialogBody').querySelectorAll('[data-capture-slot]').forEach((input,i)=>input.value=cells[i]||'');return;}
 if(target.dataset.filterPreset){$('filterExpr').value=target.dataset.filterPreset;return;}
});
document.addEventListener('input',event=>{if(event.target.id==='orbitSearch')drawOrbits();if(event.target.id==='macroSearch')drawLibrary();if(event.target.id==='bankSearch')drawBanks();if(event.target.id==='commandSearch')drawCommands();});
document.addEventListener('change',event=>{
 const id=event.target.id;
 if(['macroScope','macroKind'].includes(id))drawLibrary();
 if(id==='savedFilter'&&event.target.value)$('filterExpr').value=state.workspace.saved_filters[event.target.value];
 if(id==='renderMode'&&renderer){renderer.mode=event.target.value;pickReady=false;renderer.rebuild();}
 if(['inputDestination','inputPhase'].includes(id)){router.reset('Input destination / phase changed');run(()=>send('settings',{input:$('inputDestination').value,phase:$('inputPhase').value})).catch(report);}
 if(id==='gripMode')router.reset('Grip behavior changed');
});
document.addEventListener('keydown',event=>{
 if(event.isComposing)return;
 if(event.target.id==='bankSearch'&&['ArrowDown','ArrowUp'].includes(event.key)){event.preventDefault();const rows=[...$('bankList').querySelectorAll('[data-bank-choice]')];const i=rows.findIndex(x=>x.dataset.bankChoice===bankChoice),next=rows[Math.max(0,Math.min(rows.length-1,i+(event.key==='ArrowDown'?1:-1)))];if(next){bankChoice=next.dataset.bankChoice;drawBanks();$('bankList').querySelector(`[data-bank-choice="${bankChoice}"]`)?.scrollIntoView({block:'nearest'});}}
 if(event.key==='Escape'&&!$('dialog').open){document.body.classList.remove('views-open','puzzle-open','library-open');$('canvas').focus();}
 if(event.target.id==='bankSearch'&&event.key==='Enter'){event.preventDefault();dispatch('bank-activate');}
 if((event.key==='Enter'||event.key===' ')&&event.target.matches('[data-identity],[data-cell],[data-position]')){event.preventDefault();event.target.dispatchEvent(new MouseEvent('click',{bubbles:true}));}
});
document.addEventListener('dragstart',event=>{const macro=event.target.closest('[data-macro]');if(macro)event.dataTransfer.setData('text/magic600-macro',macro.dataset.macro);});
document.querySelectorAll('[data-phase]').forEach(element=>{element.addEventListener('dragover',event=>event.preventDefault());element.addEventListener('drop',event=>{event.preventDefault();const id=event.dataTransfer.getData('text/magic600-macro');if(id){selectedMacro=id;dispatch('insert-'+element.dataset.phase);}});});
new ResizeObserver(()=>{$('canvasOverflow').hidden=$('canvas').scrollWidth<=$('canvas').clientWidth+2;}).observe($('canvas'));
window.addEventListener('error',event=>message('Frontend error: '+event.message,true));
async function start(){
 try{structure=await raw('/api/structure');$('orbitButton').dataset.command='orbit';await refresh();$('connection').textContent=state.build+' · isolated session';if(mode==='g2'){$('keyboardKeys').hidden=true;$('keyboardToggle').setAttribute('aria-expanded','false');}await initializeRenderer();message('New or resumed isolated session. Load E1 practice from Session, or select a piece.');
 }catch(error){report(error);$('viewportMessage').textContent=error.message;}
}
start();
