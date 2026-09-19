/* Isolated review graphics. Positions, identities and geometry remain model data. */
import {formatIdentity,formatPosition,formatCell,formatOrbit,formatSlot,formatLabel,formatFrameNode,cellFromSlot} from './ids.js';
const C = {ink:'#283438', muted:'#62716f', line:'#a8b2af', surface:'#f0f2ef', accent:'#286b94', good:'#356d54', amber:'#946625', bad:'#b0443b'};
const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const number = value => Number.isInteger(value) && value >= 0;
const list = value => Array.isArray(value) ? value : [];
const txt = (x,y,value,options='') => `<text x="${x}" y="${y}" ${/\bfill=/.test(options)?'':`fill="${C.ink}"`} ${/\bfont-size=/.test(options)?'':'font-size="13"'} ${options}>${esc(value)}</text>`;
const note = (x,y,value) => txt(x,y,value,`fill-opacity=".73" font-size="12"`);
const line = (x1,y1,x2,y2,options='') => `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" ${/\bstroke=/.test(options)?'':`stroke="${C.line}"`} ${options}/>`;
const control = (kind,id,label,body) => `<g class="diagram-control" data-${kind}="${id}" tabindex="0" role="button" aria-label="${esc(label)}"><title>${esc(label)}</title>${body}</g>`;
const wrap = (name,width,height,body,kind) => `<svg xmlns="http://www.w3.org/2000/svg" class="diagram-svg ${kind}-diagram" viewBox="0 0 ${width} ${height}" role="group" aria-label="${esc(name)}" style="font-family:system-ui,sans-serif"><style>.diagram-control{cursor:pointer}.diagram-control:focus{outline:none}.diagram-control:focus>rect,.diagram-control:focus>circle,.diagram-control:focus>path{stroke:${C.ink};stroke-width:3}.diagram-control:hover>rect,.diagram-control:hover>circle{stroke:${C.accent}}</style>${body}</svg>`;
const empty = (name,message,kind) => wrap(name,800,220,txt(28,42,name)+note(28,78,message),kind);

function occupantMap(snapshot) {
  const occupants = new Map();
  for (const p of [...list(snapshot?.buffers),snapshot?.target,snapshot?.current,snapshot?.next]) {
    if (p && number(p.position) && number(p.piece)) occupants.set(p.position,p.piece);
  }
  for (const m of list(snapshot?.block)) if (number(m.position) && number(m.actual)) occupants.set(m.position,m.actual);
  return occupants;
}

function socket(x,y,position,identity,role,{selected=false,next=false,satisfied=null}={}) {
  const edge = selected ? C.accent : C.line;
  let body = control('position',position,`Inspect fixed position ${formatPosition(position)}${role ? ', '+role : ''}`,
    `<rect x="${x-95}" y="${y-58}" width="190" height="121" rx="5" fill="none" stroke="${edge}" stroke-width="${selected?2:1}"/>`+
    txt(x-81,y-35,formatPosition(position),`font-family="ui-monospace,monospace"`)+
    txt(x+81,y-35,role || 'Position',`text-anchor="end" fill-opacity=".73" font-size="11"`));
  if (number(identity)) {
    body += control('identity',identity,`Inspect identity ${formatIdentity(identity)}, currently at ${formatPosition(position)}`,
      `<rect x="${x-79}" y="${y-16}" width="158" height="39" rx="3" fill="${C.surface}" stroke="${next?C.amber:edge}"/>`+
      txt(x,y+9,formatIdentity(identity),`text-anchor="middle" font-family="ui-monospace,monospace"`));
  } else body += note(x-78,y+8,'Occupant not captured');
  body += note(x-80,y+45,satisfied===null ? 'Outline = fixed position' : satisfied ? 'Exact requirement met' : 'Requirement unmet');
  if (next) body += txt(x+87,y+57,'NEXT',`text-anchor="end" fill="${C.amber}" font-size="10"`);
  return body;
}

export function blockDiagram(snapshot, selectedIdentity=null) {
  const members = list(snapshot?.block);
  if (!members.length) return empty('Working block','Select an identity, then explicitly add its requirement to the block.','block');
  const columns = 3, rows = Math.ceil(members.length/columns), height = 128+rows*179;
  let body = txt(28,31,snapshot?.workspace?.block?.name || 'Working block')+
    note(28,54,'Requirement arrangement · schematic, not a claim of adjacency or rigid motion')+
    `<path d="M 23 81 H 14 V ${height-22} H 23 M 777 81 H 786 V ${height-22} H 777" fill="none" stroke="${C.line}"/>`;
  const nextId = snapshot?.next?.piece;
  members.forEach((m,i) => {
    const x=139+(i%columns)*260,y=143+Math.floor(i/columns)*179;
    body += socket(x,y,m.position,m.actual,'Block',{selected:m.identity===selectedIdentity,next:m.actual===nextId,satisfied:!!m.satisfied});
    body += txt(x-92,y+87,`Required ${formatIdentity(m.identity)}`,`fill="${m.satisfied?C.accent:C.amber}"`);
  });
  return wrap('Block requirements and actual occupants',800,height,body,'block');
}

export function workspaceDiagram(snapshot,effect,selectedIdentity=null,{predicted=false,scope='body'}={}) {
  const w=snapshot?.workspace || {}, members=list(snapshot?.block), before=occupantMap(snapshot), occupants=new Map(before);
  const cycles=list(effect?.cycles), cyclePositions=new Set(cycles.flatMap(c=>list(c.positions)));
  const orientationPositions=new Set(list(effect?.fixed_orientation));
  if(predicted && effect) {
    // A truncated cycle catalogue cannot establish that unlisted occupants stay put.
    if((effect.cycle_count ?? cycles.length)>cycles.length)
      for(const p of occupants.keys())if(!cyclePositions.has(p))occupants.delete(p);
    for(const cycle of cycles) {
      const ps=list(cycle.positions);
      ps.forEach((src,i)=>{const dst=ps[(i+1)%ps.length];if(before.has(src))occupants.set(dst,before.get(src));else occupants.delete(dst);});
    }
  }
  const buffers=list(snapshot?.buffers).filter(p=>p&&number(p.position));
  const roles=list(w.roles), bufferPositions=[0,1].map(i=>number(roles[i])?roles[i]:buffers[i]?.position);
  const target=number(w.target)?w.target:snapshot?.target?.position;
  const current=snapshot?.current?.piece, next=snapshot?.next?.piece;
  const workPositions=[...new Set([...members.map(m=>m.position),target].filter(number))]
    .filter(p=>!bufferPositions.includes(p));
  const memberByPosition=new Map(members.map(m=>[m.position,m]));
  const coords=new Map(), kind=new Map();
  bufferPositions.forEach((p,i)=>{if(number(p)){coords.set(p,[112+i*216,100]);kind.set(p,i?'B':'A');}});
  workPositions.forEach((p,i)=>{coords.set(p,[570+(i%2)*216,100+Math.floor(i/2)*185]);kind.set(p,'work');});
  const blockRows=Math.max(1,Math.ceil(workPositions.length/2)), blockBottom=224+(blockRows-1)*185;
  const unbound=[effect?.star?.a,effect?.star?.b,effect?.star?.target].filter(p=>number(p)&&!coords.has(p));
  const extra=[...new Set(unbound)];
  extra.forEach((p,i)=>{coords.set(p,[135+(i%3)*285,blockBottom+100+Math.floor(i/3)*165]);kind.set(p,'unbound');});
  const height=Math.max(250,blockBottom+26+Math.ceil(extra.length/3)*165);
  const text=(x,y,value,options='')=>txt(x,y,value,`font-size="17" ${options}`);
  const dim=(x,y,value,options='')=>text(x,y,value,`fill-opacity=".73" ${options}`);
  const protectedPositions=new Set(list(w.block?.protected).map(m=>m.position));
  const orbitByPosition=new Map();
  for(const p of [...buffers,snapshot?.target,snapshot?.current,snapshot?.next,...members])
    if(p&&Number.isInteger(p.orbit))orbitByPosition.set(p.position,p.orbit);
  const review=snapshot?.review,matchingReview=review?.effect?.id===effect?.id;
  const conflicts=new Set(review?.status!=='Stale'?list(review?.block_conflicts):[]);
  let body=`<defs><marker id="workspace-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto"><path d="M 0 0 L 10 5 L 0 10 z" fill="${C.accent}"/></marker></defs>`;
  body+=`<path d="M 468 35 H 454 V ${blockBottom} H 468 M 884 35 H 898 V ${blockBottom} H 884" fill="none" stroke="${C.line}" stroke-width="1.5"/>`;
  if(!workPositions.length)body+=dim(480,110,'No destination assigned.');

  for(const cycle of cycles) {
    const positions=list(cycle.positions);
    positions.forEach((src,i)=>{
      const dst=positions[(i+1)%positions.length];
      if(src===dst||!coords.has(src)||!coords.has(dst))return;
      const [sx,sy]=coords.get(src),[dx,dy]=coords.get(dst),vx=dx-sx,vy=dy-sy,length=Math.hypot(vx,vy);
      const inset=Math.min(94/Math.max(Math.abs(vx/length),.001),40/Math.max(Math.abs(vy/length),.001));
      // Offset a two-cycle's opposing edges so both directed effects stay legible.
      const side=positions.length===2?7:0,ox=-vy/length*side,oy=vx/length*side;
      if(sy===dy&&Math.abs(vx)>265){
        // Skip intervening sockets through a separate lane, never through tokens.
        const direction=Math.sign(vx),routeY=sy+(direction>0?-80:134),start=sx+94*direction,end=dx-94*direction;
        const d=`M ${start} ${sy} C ${start+12*direction} ${sy}, ${start+12*direction} ${routeY}, ${start+24*direction} ${routeY} L ${end-24*direction} ${routeY} C ${end-12*direction} ${routeY}, ${end-12*direction} ${dy}, ${end} ${dy}`;
        body+=`<path d="${d}" fill="none" stroke="${C.accent}" stroke-width="2" marker-end="url(#workspace-arrow)"/>`;
      }else body+=line(sx+vx/length*inset+ox,sy+vy/length*inset+oy,dx-vx/length*inset+ox,dy-vy/length*inset+oy,
        `stroke="${C.accent}" stroke-width="2" marker-end="url(#workspace-arrow)"`);
    });
  }
  for(const [position,[x,y]] of coords) {
    const identity=occupants.get(position),member=memberByPosition.get(position),role=kind.get(position);
    const isCurrent=number(current)&&identity===current,isNext=number(next)&&identity===next,isTarget=position===target;
    const knownOrbit=orbitByPosition.has(position),orbit=orbitByPosition.get(position);
    const protectedHere=protectedPositions.has(position)||(knownOrbit&&list(snapshot?.protected).includes(orbit));
    const conflict=conflicts.has(position),socketColor=conflict?C.bad:isTarget?C.accent:C.line;
    const path=`M ${x-77} ${y-36} H ${x-92} V ${y+35} H ${x-77} M ${x+77} ${y-36} H ${x+92} V ${y+35} H ${x+77}`;
    const frameChanged=orientationPositions.has(position);
    body+=control('position',position,`Inspect fixed ${formatPosition(position)}${isTarget?', active destination':''}${conflict?', protection conflict':''}${frameChanged?', exact fixed-position frame change in the supplied effect':''}`,
      `<path d="${path}" fill="none" stroke="${socketColor}" stroke-width="${isTarget?2:1.5}"/>`+
      text(x,y-20,formatPosition(position),`text-anchor="middle" font-family="ui-monospace,monospace"`));
    const heading=role==='A'||role==='B'?`BUFFER ${role}`:role==='unbound'?'MACRO FIXED SOCKET':isTarget?'TARGET':'BLOCK MEMBER';
    body+=dim(x,y-43,heading,'text-anchor="middle"');
    if(number(identity)) {
      body+=control('identity',identity,`Inspect identity ${formatIdentity(identity)}, ${predicted?'predicted':'committed'} position ${formatPosition(position)}`,
        `<rect x="${x-76}" y="${y-8}" width="152" height="32" rx="2" fill="${C.surface}" stroke="${isCurrent?C.accent:isNext?C.amber:C.line}" stroke-width="${isCurrent||isNext?2:1}"/>`+
        txt(x,y+15,formatIdentity(identity),`text-anchor="middle" font-family="ui-monospace,monospace" font-size="20"`));
    }else body+=dim(x,y+7,'Occupant unknown','text-anchor="middle"');
    const tags=[isCurrent?'CURRENT':'',isNext?(isCurrent?'NEXT':'LOCKED NEXT'):'',frameChanged?'FRAME Δ':''].filter(Boolean);
    body+=txt(x,y+49,tags.join(' · '),`text-anchor="middle" font-size="16" fill="${isNext||frameChanged?C.amber:C.accent}"`);
    if(member) {
      body+=dim(x,y+71,`Required ${formatIdentity(member.identity)}`,'text-anchor="middle"');
      body+=text(x,y+93,conflict?(matchingReview?'CONFLICT':'DRAFT CONFLICT'):`${predicted?'Before: ':''}${member.satisfied?'EXACT':'UNMET'}`,
        `text-anchor="middle" fill="${conflict?C.bad:member.satisfied?C.good:C.muted}"`);
    }
    const policy=protectedHere?'PROTECTION LOCKED':knownOrbit?'No protection lock':'Protection scope unknown';
    body+=txt(x,y+(member?112:72),policy,`text-anchor="middle" font-size="16" fill="${protectedHere?C.good:knownOrbit?C.muted:C.amber}"`);
  }
  if(extra.length)body+=dim(25,blockBottom+34,'Unassigned macro sockets · inspection only');
  const frame=list(effect?.star?.frame);
  body+=`<desc>${esc(`${predicted&&effect?'Predicted identities':'Committed identities'}; ${scope==='complete'?'complete operation':'macro body'}. ${effect?.star?formatFrameNode(effect.star.orbit,effect.star.node):'No retained star frame claimed'}. ${frame.length?'Ordered slots ['+frame.map(formatSlot).join(', ')+'].':''} Position sockets remain fixed; occupant tokens track identities.`)}</desc>`;
  return wrap('Unified block, buffer and selected operation workspace',900,height,body,'workspace')
    .replace('style="font-family:','style="min-width:750px;max-width:900px;width:100%;height:auto;font-family:');
}

export function operationDiagram(snapshot,effect,predicted=false) {
  if (!effect) return empty('Operation effect','Select an existing macro or inspect the explicitly composed draft.','operation');
  const cycles = list(effect.cycles), before = occupantMap(snapshot), occupants = new Map(before);
  if (predicted) for (const cycle of cycles) {
    const positions=list(cycle.positions);
    positions.forEach((src,i) => {
      const dst=positions[(i+1)%positions.length];
      if (before.has(src)) occupants.set(dst,before.get(src)); else occupants.delete(dst);
    });
  }
  const marker='effect-arrow';
  let body=`<defs><marker id="${marker}" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M 0 0 L 10 5 L 0 10 z" fill="${C.accent}"/></marker></defs>`;
  body+=txt(28,31,effect.category || 'Exact operation effect')+
    note(28,55,`${predicted?'Predicted identities':'Current identities'} · ${effect.pieces ?? '?'} pieces / ${effect.slots ?? '?'} slots · source → destination`);
  if (effect.star) {
    const star=effect.star, positions=[star.a,star.b,star.target], coords=[[205,148],[595,148],[400,365]];
    const displayed=new Set(positions);
    for (const c of cycles) list(c.positions).forEach((src,i,a) => {
      const dst=a[(i+1)%a.length];
      if (!displayed.has(src)||!displayed.has(dst)) return;
      const [sx,sy]=coords[positions.indexOf(src)],[dx,dy]=coords[positions.indexOf(dst)];
      const vx=dx-sx,vy=dy-sy,length=Math.hypot(vx,vy);
      // The graphic follows the supplied permutation, never the macro name/sign.
      const from=Math.max(102/Math.max(Math.abs(vx/length),.01),0);
      const vertical=72/Math.max(Math.abs(vy/length),.01), inset=Math.min(from,vertical);
      body+=line(sx+vx/length*inset,sy+vy/length*inset,dx-vx/length*inset,dy-vy/length*inset,
        `stroke="${C.accent}" stroke-width="2" marker-end="url(#${marker})"`);
    });
    positions.forEach((p,i)=>body+=socket(...coords[i],p,occupants.get(p),['A','B','Target'][i],{selected:p===snapshot?.target?.position,next:occupants.get(p)===snapshot?.next?.piece}));
    body+=note(28,472,`${formatFrameNode(star.orbit,star.node)} · ordered slots [${list(star.frame).map(formatSlot).join(', ')}]`);
    const other=cycles.filter(c=>list(c.positions).some(p=>!displayed.has(p))).length;
    if (other) body+=txt(28,496,`${other} further cycle(s): inspect complete collateral below.`,`fill="${C.amber}"`);
    if (effect.fixed_orientation_count) body+=txt(28,518,`${effect.fixed_orientation_count} fixed-position orientation change(s).`,`fill="${C.amber}"`);
    return wrap('Verified operation positions, occupants and cycle direction',800,542,body,'operation');
  }
  const shown=cycles.slice(0,8);
  shown.forEach((c,row)=>{
    const positions=list(c.positions), y=99+row*67, visible=positions.slice(0,6);
    body+=note(28,y+2,formatOrbit(c.orbit));
    visible.forEach((p,i)=>{
      const x=104+i*106;
      if(i) body+=line(x-26,y-2,x-5,y-2,`stroke="${C.accent}" marker-end="url(#${marker})"`);
      body+=control('position',p,`Inspect ${formatPosition(p)} in directed cycle`,
        `<rect x="${x}" y="${y-20}" width="78" height="38" rx="3" fill="none" stroke="${C.line}"/>`+
        txt(x+39,y-2,formatPosition(p),`text-anchor="middle" font-size="11"`)+
        txt(x+39,y+12,occupants.has(p)?formatIdentity(occupants.get(p)):'I unknown',`text-anchor="middle" font-size="10" fill-opacity=".7"`));
    });
    body+=note(104,y+36,positions.length>6?`Continues · ${positions.length} positions in full effect table`:positions.length?`${formatPosition(positions[positions.length-1])} → ${formatPosition(positions[0])} closes this cycle`:'No cycle positions');
  });
  const y=105+shown.length*67, fixed=list(effect.fixed_orientation);
  body+=txt(28,y,`${effect.fixed_orientation_count ?? fixed.length} fixed-position orientation changes`);
  fixed.slice(0,16).forEach((p,i)=>{
    const x=28+(i%8)*96,yy=y+34+Math.floor(i/8)*34;
    body+=control('position',p,`Inspect orientation change at fixed ${formatPosition(p)}`,
      `<rect x="${x}" y="${yy-18}" width="88" height="27" fill="none" stroke="${C.amber}"/>`+txt(x+44,yy,formatPosition(p),`text-anchor="middle" font-size="11"`));
  });
  body+=note(28,y+116,`${effect.cycle_count ?? cycles.length} total cycles; ${shown.length} shown. Full slot effects remain in the exact effect table.`);
  return wrap('Directed cycle lanes and fixed-position orientation changes',800,y+140,body,'operation');
}

export function localDiagram(piece) {
  if (!piece) return empty('Local correspondence','Select a piece to inspect its actual hosting structure and sticker correspondence.','local');
  const slots=list(piece.slots), labels=list(piece.labels), count=Math.max(slots.length,labels.length);
  if (!count) return empty('Local correspondence','Exact sticker correspondence has not been captured for this piece.','local');
  let body=txt(24,30,`${formatIdentity(piece.piece)} at ${formatPosition(piece.position)} · ${list(piece.current_cells).length}-cell structure`)+
    note(24,53,`Exact sticker correspondence · ${piece.orientation_group || 'orientation group unspecified'} · ${piece.solved?'exactly solved':piece.position_correct?'at Home; frame differs':'away from Home'}`)+
    txt(25,89,'Current physical slot')+txt(431,89,'Sticker label / its Home cell')+
    line(24,100,776,100);
  for(let i=0;i<count;i++) {
    const slot=slots[i],label=labels[i],y=124+i*38;
    if (!number(slot)||!number(label)) { body+=note(26,y,'Incomplete correspondence row');continue; }
    const cell=cellFromSlot(slot),home=cellFromSlot(label),exact=slot===label;
    body+=control('cell',cell,`Inspect current hosting cell ${formatCell(cell)}, slot ${formatSlot(slot)}`,
      `<rect x="24" y="${y-18}" width="312" height="29" rx="2" fill="${C.surface}" stroke="${C.line}"/>`+
      txt(36,y,formatCell(cell))+txt(322,y,formatSlot(slot),`text-anchor="end" font-size="12" fill-opacity=".7"`));
    body+=line(349,y-5,415,y-5,`stroke-dasharray="4 4"`);
    body+=control('cell',home,`Inspect Home cell ${formatCell(home)}, label ${formatLabel(label)}`,
      `<rect x="430" y="${y-18}" width="344" height="29" rx="2" fill="none" stroke="${exact?C.accent:C.line}"/>`+
      txt(442,y,formatCell(home))+txt(760,y,formatLabel(label),`text-anchor="end" font-size="12" fill-opacity=".7"`));
  }
  let y=145+count*38;
  body+=note(24,y,'Dashed lines match actual sticker labels; they do not specify a reference transform.');
  const caps=list(piece.cap_cells);y+=32;
  body+=txt(24,y,`${caps.length} affecting caps · ${list(piece.current_cells).length} hosting cells`);
  caps.forEach((cell,i)=>{
    const x=24+(i%10)*75,cy=y+32+Math.floor(i/10)*31;
    body+=control('cell',cell,`Inspect affecting cap ${formatCell(cell)}`,
      `<rect x="${x}" y="${cy-18}" width="66" height="26" fill="none" stroke="${C.line}"/>`+
      txt(x+33,cy,formatCell(cell),`text-anchor="middle" font-size="11"`));
  });
  return wrap('Exact Local hosting cells and sticker correspondence',800,y+34+Math.ceil(caps.length/10)*31,body,'local');
}

export function globalDiagram(structure,groupA,groupB,angle=0) {
  const geometry=structure?.geometry,vertices=list(geometry?.vertices4),centers=list(geometry?.centers4),cells=list(geometry?.cells);
  if(vertices.length!==120 || centers.length!==600 || cells.length!==600 || list(geometry?.coordinate_order).join(',')!=='W,X,Y,Z')
    return empty('Global cell relationship','Waiting for the immutable W/X/Y/Z cell geometry. No substitute geometry is drawn.','global');
  const clean=group=>[...new Set(list(group).filter(c=>Number.isInteger(c)&&c>=1&&c<=600))];
  const a=clean(groupA),b=clean(groupB),as=new Set(a),bs=new Set(b),chosen=[...new Set([...a,...b])];
  const radius=Math.hypot(...vertices[0]),theta=Number.isFinite(angle)?angle:0,co=Math.cos(theta),si=Math.sin(theta);
  const project=p=>{
    const f=2.4/(2.4-p[0]/radius),x=p[1]/radius*f,y=p[2]/radius*f,z=p[3]/radius*f;
    const rx=co*x+si*z,rz=-si*x+co*z;
    return [400+rx*225,260-(y*.91-rz*.414)*225];
  };
  const projected=vertices.map(project),cp=centers.map(project);
  let body=txt(24,30,'Global · two actual cell groups')+
    note(24,53,'W perspective → XYZ · rotate XZ → projected XY · exact model vertices and cell centers');
  body+=line(720,433,761,433)+line(720,433,720,396)+note(761,450,'X')+note(707,391,'Y');
  cp.forEach(([x,y])=>body+=`<circle cx="${x.toFixed(2)}" cy="${y.toFixed(2)}" r="1.2" fill="${C.muted}" opacity=".23"/>`);
  chosen.forEach(cell=>{
    const color=as.has(cell)&&bs.has(cell)?C.ink:as.has(cell)?C.accent:C.amber,indices=cells[cell-1];
    if(!Array.isArray(indices)||indices.length!==4)return;
    for(let i=0;i<4;i++)for(let j=i+1;j<4;j++){
      const p=projected[indices[i]],q=projected[indices[j]];
      if(p&&q)body+=line(...p,...q,`stroke="${color}" stroke-opacity=".65" stroke-width="1.4"`);
    }
    const [x,y]=cp[cell-1];
    body+=control('cell',cell,`Inspect ${formatCell(cell)}, group ${as.has(cell)&&bs.has(cell)?'A and B':as.has(cell)?'A':'B'}`,
      `<circle cx="${x}" cy="${y}" r="7" fill="${C.surface}" stroke="${color}" stroke-width="2"/>`);
  });
  let y=500;
  for(const [name,group,color]of[['A',a,C.accent],['B',b,C.amber]]){
    body+=txt(24,y,`Group ${name} · ${group.length} cells`,`fill="${color}"`);y+=28;
    if(!group.length){body+=note(24,y,'No cells assigned');y+=32;continue;}
    group.forEach((cell,i)=>{
      const x=24+(i%10)*75,cy=y+Math.floor(i/10)*31;
      body+=control('cell',cell,`Inspect group ${name} cell ${formatCell(cell)}`,
        `<rect x="${x}" y="${cy-18}" width="66" height="26" fill="none" stroke="${color}"/>`+
        txt(x+33,cy,formatCell(cell),`text-anchor="middle" font-size="11"`));
    });y+=Math.ceil(group.length/10)*31+12;
  }
  const shared=a.filter(c=>bs.has(c)),facePairs=[];
  for(const ca of a)for(const cb of b)if(ca!==cb&&list(structure.adjacency?.[ca-1]).includes(cb))facePairs.push(`${formatCell(ca)}/${formatCell(cb)}`);
  body+=note(24,y,`Shared cells: ${shared.length?shared.map(formatCell).join(', '):'none'} · shared-face pairs: ${facePairs.length}`);
  body+=note(24,y+24,'Wire segments are tetrahedral edges; projection and adjacency do not describe legal turns.');
  return wrap('Real 4D geometry of selected cell groups',800,y+48,body,'global');
}
