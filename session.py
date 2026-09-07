"""Branch-preserving SQLite journal and transaction-scoped macro previews."""
from __future__ import annotations
import gzip,json,os,secrets,sqlite3,time,zlib
from pathlib import Path
import numpy as np
from session_lock import SessionLock
from core import Model,PuzzleState,Planner,Filters,ORDER,canonical,digest,state_hash,invrecipe

DEFAULT_RULES=[{'expr':'active','style':'solid'}]
NO_CAMERA=object()
class Session:
 def __init__(self,model:Model,directory:Path):
  self.m=model;self.directory=directory;directory.mkdir(parents=True,exist_ok=True);self.process_lock=SessionLock(directory)
  self.db=sqlite3.connect(directory/'session.sqlite3',check_same_thread=False);self.db.row_factory=sqlite3.Row
  self.db.execute('PRAGMA journal_mode=WAL');self.db.execute('PRAGMA synchronous=FULL')
  self.db.executescript('''CREATE TABLE IF NOT EXISTS meta(key TEXT PRIMARY KEY,value TEXT NOT NULL);
  CREATE TABLE IF NOT EXISTS events(id INTEGER PRIMARY KEY,parent INTEGER,recipe TEXT NOT NULL,pre TEXT NOT NULL,post TEXT NOT NULL,certificate TEXT NOT NULL,primitive_count TEXT NOT NULL,stars INTEGER NOT NULL,total_primitives TEXT NOT NULL,total_stars INTEGER NOT NULL,depth INTEGER NOT NULL,created REAL NOT NULL,note TEXT NOT NULL,assistance TEXT NOT NULL);
  CREATE TABLE IF NOT EXISTS snapshots(name TEXT PRIMARY KEY,head INTEGER NOT NULL,labels BLOB NOT NULL,hash TEXT NOT NULL,prefs TEXT NOT NULL,created REAL NOT NULL);
  ''')
  saved=self._get('model')
  if saved and saved!=model.model_id:raise ValueError('This session belongs to a different asset model. Use another data directory.')
  self.st=PuzzleState(model);self.head=int(self._get('head') or 0);self.pending=None;self.redo_stack=[];self.rev=0
  self.prefs=json.loads(self._get('prefs') or '{}');self.prefs.setdefault('orbit',33);self.prefs.setdefault('rules',DEFAULT_RULES);self.prefs.setdefault('pin_safety',True);self.prefs.setdefault('protected',[]);self.prefs.setdefault('selected',None)
  self.planner=Planner(model)
  if not saved:
   with self.db:
    self._put('model',model.model_id);self._put('head','0')
    self.db.execute('INSERT INTO events VALUES (0,NULL,?,?,?,?,?,?,?,?,?,?,?,?)',('[]',self.st.hash,self.st.hash,'root','0',0,'0',0,0,time.time(),'Solved root','manual'))
    self._snapshot('Solved root')
  else:self._recover()
 def _get(self,key):
  x=self.db.execute('SELECT value FROM meta WHERE key=?',(key,)).fetchone();return x[0] if x else None
 def _put(self,key,val):self.db.execute('INSERT OR REPLACE INTO meta VALUES (?,?)',(key,str(val)))
 def event(self,head=None):return self.db.execute('SELECT * FROM events WHERE id=?',(self.head if head is None else head,)).fetchone()
 def _recover(self):
  chain=[];h=self.head
  while True:
   snap=self.db.execute('SELECT * FROM snapshots WHERE head=? ORDER BY created DESC LIMIT 1',(h,)).fetchone()
   if snap:break
   ev=self.event(h)
   if ev is None or ev['parent'] is None:raise ValueError('Journal ancestry is incomplete')
   chain.append(ev);h=ev['parent']
  raw=zlib.decompress(snap['labels']);a=np.frombuffer(raw,dtype='<i4').copy()
  if state_hash(a)!=snap['hash']:raise ValueError('Checkpoint hash mismatch')
  for ev in reversed(chain):
   if state_hash(a)!=ev['pre']:raise ValueError('Recovery pre-state mismatch')
   s,d,_,_=self.m.net(json.loads(ev['recipe']));a[d]=a[s]
   if state_hash(a)!=ev['post']:raise ValueError('Recovery post-state mismatch')
  self.st=PuzzleState(self.m,a)
  if self.st.hash!=self.event()['post']:raise ValueError('Journal head mismatch')
 def _snapshot(self,name,prefs=None):
  self.db.execute('INSERT OR REPLACE INTO snapshots VALUES (?,?,?,?,?,?)',(name,self.head,zlib.compress(self.st.labels.astype('<i4').tobytes(),3),self.st.hash,canonical(self.prefs if prefs is None else prefs),time.time()))
 def checkpoint(self,name=None):
  name=(str(name or time.strftime('Checkpoint %Y-%m-%d %H-%M-%S')))[:120]
  if name=='Solved root' and self.head!=0:raise ValueError('The solved root checkpoint is immutable')
  with self.db:self._snapshot(name);self._put('prefs',canonical(self.prefs))
  return dict(name=name,head=self.head,state_hash=self.st.hash)
 def restore(self,name):
  x=self.db.execute('SELECT * FROM snapshots WHERE name=?',(name,)).fetchone()
  if not x:raise ValueError('Checkpoint not found')
  labels=np.frombuffer(zlib.decompress(x['labels']),dtype='<i4').copy();st=PuzzleState(self.m,labels)
  if st.hash!=x['hash']:raise ValueError('Checkpoint checksum failed')
  prefs=json.loads(x['prefs'])
  with self.db:self._put('head',x['head']);self._put('prefs',canonical(prefs))
  self.head=x['head'];self.st=st;self.prefs=prefs;self.pending=None;self.redo_stack=[];self.rev+=1
  return self.status()
 def _recovery_name(self,action):
  return 'Before '+action+' '+time.strftime('%Y-%m-%d %H-%M-%S')+' '+secrets.token_hex(4)
 def _prefs_with_camera(self,camera):
  if camera is NO_CAMERA:return self.prefs
  from log_io import validate_camera
  prefs=dict(self.prefs);prefs['camera']=validate_camera(camera)
  if len(canonical(prefs))>500000:raise ValueError('Preferences are too large')
  return prefs
 def reset(self,camera=NO_CAMERA):
  """Return to solved while retaining a recoverable branch and all view prefs."""
  prefs=self._prefs_with_camera(camera);old_head=self.head;name=self._recovery_name('reset');solved=PuzzleState(self.m)
  self.m.check_cancel()
  with self.db:
   self._snapshot(name,prefs);self._put('head',0);self._put('prefs',canonical(prefs));self.m.check_cancel()
  self.head=0;self.st=solved;self.prefs=prefs;self.pending=None;self.redo_stack=[];self.rev+=1
  return dict(self.status(),reset_checkpoint=name,previous_head=old_head,note='Reset to solved. Previous progress is retained in checkpoint '+name)
 def save_prefs(self,changes):
  # User preferences can only mutate view/selection/guard data, never puzzle labels.
  if not isinstance(changes,dict):raise ValueError('Preferences must be an object')
  p=dict(self.prefs)
  allowed={'orbit','rules','pin_safety','protected','selected','camera','keybinds','presets','view','filter_sequence','sequence_index','layout','bookmarks','named_sets','macro_library','assistance_profile','input_profile'}
  for k,v in changes.items():
   if k not in allowed:raise ValueError('Unknown preference: '+k)
   p[k]=v
  if type(p['orbit'])!=int or not 0<=p['orbit']<35:raise ValueError('Bad active orbit')
  if type(p['pin_safety'])!=bool:raise ValueError('Show extra context must be a boolean')
  view=p.get('view',{})
  if isinstance(view,dict) and 'native_hide_frame' in view and type(view['native_hide_frame']) is not bool:raise ValueError('Hide 600-cell frame must be a boolean')
  if not isinstance(p['protected'],list) or any(type(o)!=int or not 0<=o<35 for o in p['protected']):raise ValueError('Bad protected orbit list')
  if p['selected'] is not None and (type(p['selected'])!=int or not 0<=p['selected']<self.m.np):raise ValueError('Bad selected physical identity')
  rules=p['rules']
  if not isinstance(rules,list) or len(rules)>32:raise ValueError('Filters require a list of at most 32 rules')
  for rule in rules:
   if not isinstance(rule,dict) or not isinstance(rule.get('expr'),str) or rule.get('style') not in ('hide','ghost','solid','highlight'):raise ValueError('Each filter rule needs an expression and a valid style')
  sets=p.get('named_sets',{})
  if not isinstance(sets,dict):raise ValueError('Named sets must be an object')
  for name,item in sets.items():
   import re
   if not isinstance(name,str) or not re.fullmatch(r'[A-Za-z_][A-Za-z_0-9]{0,39}',name) or not isinstance(item,dict) or item.get('kind') not in ('identity','position'):raise ValueError('Invalid named set')
   if not isinstance(item.get('ids'),list) or len(item['ids'])>10000 or any(type(x)!=int or not 0<=x<self.m.np for x in item['ids']):raise ValueError('Invalid set members')
  library=p.get('macro_library',{})
  if not isinstance(library,dict):raise ValueError('Macro library must be an object')
  for name,item in library.items():
   if not isinstance(name,str) or len(name)>80:raise ValueError('Macro names must contain at most 80 characters')
   if not isinstance(item,dict) or 'recipe' not in item:raise ValueError('Each saved macro needs a recipe')
   self.m.normalize(item['recipe'])
  # Validate referenced sets before the expression evaluator reads their members.
  Filters(self.st,p['orbit'],p['selected'],p['protected'],sets=sets).styles(rules,p['pin_safety'])
  if len(canonical(p))>500000:raise ValueError('Preferences are too large')
  with self.db:self._put('prefs',canonical(p))
  self.prefs=p
  return self.status()
 def preview(self,recipe,note='',assistance='manual'):
  start=time.perf_counter();s,d,length,recipe=self.m.net(recipe);after=self.st.labels.copy();after[d]=after[s]
  public=dict(format='C600-STUDIO-CERTIFICATE-v1',model_id=self.m.model_id,recipe=recipe,source_to_destination_sha256=digest(s.astype('<i4').tobytes()+d.astype('<i4').tobytes()),primitive_count=str(length),star_count=sum(x['kind']=='star' for x in recipe),support=self.m.support(s),pre_state=self.st.hash,post_state=state_hash(after),evidence='Legal seed replay plus exact conjugation/composition over the retained full model',native_windows_equivalence=False)
  public['certificate_id']=digest(canonical(public).encode())
  public['conflicts']=[r for r in public['support'] if r['orbit'] in self.prefs['protected']]
  public['net_moved_stickers']=len(s);public['net_moved_pieces']=len(np.unique(self.m.sp[s]));public['preview_ms']=1000*(time.perf_counter()-start)
  public['note']=str(note)[:500];public['assistance']=assistance
  self.m.check_cancel()
  self.pending=dict(public=public,src=s,dst=d,after=after,rev=self.rev,head=self.head,token=secrets.token_urlsafe(18),recipe=recipe)
  return dict(public,token=self.pending['token'])
 def commit(self,token):
  p=self.pending
  if not p or not secrets.compare_digest(str(token),p['token']):raise ValueError('Preview token is missing or expired')
  if p['head']!=self.head or p['rev']!=self.rev or p['public']['pre_state']!=self.st.hash:raise ValueError('Stale preview. Preview the operation again.')
  if any(r['orbit'] in self.prefs['protected'] for r in p['public']['support']):raise ValueError('Protected orbit would move. Remove that protection explicitly or choose a different macro.')
  c=p['public'];old=self.event();new_state=PuzzleState(self.m,p['after'],trusted=True);total=int(old['total_primitives'])+int(c['primitive_count']);stars=old['total_stars']+c['star_count'];depth=old['depth']+1
  # Write the durable operation before publishing its state to the UI.
  with self.db:
   cur=self.db.execute('INSERT INTO events(parent,recipe,pre,post,certificate,primitive_count,stars,total_primitives,total_stars,depth,created,note,assistance) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)',(self.head,canonical(p['recipe']),self.st.hash,new_state.hash,c['certificate_id'],c['primitive_count'],c['star_count'],str(total),stars,depth,time.time(),c['note'],c['assistance']))
   nh=cur.lastrowid;self._put('head',nh);self._put('prefs',canonical(self.prefs))
   if depth%50==0:self.db.execute('INSERT OR REPLACE INTO snapshots VALUES (?,?,?,?,?,?)',(f'Auto {nh}',nh,zlib.compress(new_state.labels.astype('<i4').tobytes(),3),new_state.hash,canonical(self.prefs),time.time()))
  self.head=nh;self.st=new_state;self.pending=None;self.redo_stack=[];self.rev+=1
  return self.status()
 def undo(self):
  ev=self.event()
  if ev['parent'] is None:raise ValueError('Already at the solved root')
  s,d,_,_=self.m.net(invrecipe(json.loads(ev['recipe'])));a=self.st.labels.copy();a[d]=a[s]
  if state_hash(a)!=ev['pre']:raise ValueError('Undo hash check failed')
  with self.db:self._put('head',ev['parent']);self._put('redo:'+str(ev['parent']),self.head)
  self.redo_stack.append(self.head);self.head=ev['parent'];self.st=PuzzleState(self.m,a);self.pending=None;self.rev+=1
  return self.status()
 def redo(self):
  if self.redo_stack:h=self.redo_stack[-1]
  else:
   h=self._get('redo:'+str(self.head))
   if h is None:
    children=self.db.execute('SELECT id FROM events WHERE parent=?',(self.head,)).fetchall()
    if len(children)!=1:raise ValueError('Choose a redo branch in History')
    h=children[0]['id']
   h=int(h)
  ev=self.event(h)
  if ev['parent']!=self.head or ev['pre']!=self.st.hash:raise ValueError('Redo is on another branch')
  s,d,_,_=self.m.net(json.loads(ev['recipe']));a=self.st.labels.copy();a[d]=a[s]
  if state_hash(a)!=ev['post']:raise ValueError('Redo hash check failed')
  with self.db:self._put('head',h)
  if self.redo_stack:self.redo_stack.pop()
  self.head=h;self.st=PuzzleState(self.m,a);self.pending=None;self.rev+=1
  return self.status()
 def suggest(self,orbit,batch=1,target=None):
  self.planner.validate_orbit(orbit)
  if type(batch)!=int or not 1<=batch<=50:raise ValueError('Batch must be 1..50 insertions')
  temp=PuzzleState(self.m,self.st.labels,trusted=True);cmds=[];notes=[]
  for i in range(batch):
   self.m.check_cancel()
   cs,note=self.planner.next(temp,orbit,target if i==0 else None)
   if not cs:break
   for c in cs:
    s,d=self.m.star_net(c['orbit'],c['node'],c['sign']);temp.labels[d]=temp.labels[s]
   temp.refresh();cmds.extend(cs);notes.append(note)
  if not cmds:
   self.pending=None
   return dict(complete=True,note='This orbit is solved; no insertion preview is pending.')
  result=self.preview(cmds,notes[0]['explanation']+f' Batch: {len(notes)} insertion(s).','assisted-target-selection')
  result['plan']=notes;result['complete']=False;return result
 def render_styles(self):
  pp=[] if self.pending is None else np.unique(self.m.sp[self.pending['src']]).tolist()
  p=self.prefs;return Filters(self.st,p['orbit'],p.get('selected'),p['protected'],pp,sets=p.get('named_sets')).styles(p['rules'],p['pin_safety'])
 def status(self):
  ev=self.event();rows=self.st.progress();moving=self.m.oid>=0
  return dict(version='0.2.0',head=self.head,revision=self.rev,model_id=self.m.model_id,state_hash=self.st.hash,solved=bool(np.all(self.st.correct)),moving_solved=int(self.st.correct[moving].sum()),moving_total=int(moving.sum()),all_stickers= self.m.n,progress=rows,prefs=self.prefs,primitives=ev['total_primitives'],stars=ev['total_stars'],transactions=ev['depth'],checkpoints=[dict(r) for r in self.db.execute('SELECT name,head,created FROM snapshots ORDER BY created DESC LIMIT 100')],pending=None if self.pending is None else dict(self.pending['public'],token=self.pending['token']),can_undo=self.head!=0,can_redo=bool(self.redo_stack or self._get('redo:'+str(self.head)) or self.db.execute('SELECT 1 FROM events WHERE parent=? LIMIT 1',(self.head,)).fetchone()),order=ORDER)
 def certificate(self):
  if not self.pending:raise ValueError('Preview an operation first')
  p=self.pending
  return dict(p['public'],source=p['src'].tolist(),destination=p['dst'].tolist(),expanded_lab_word=list(self.m.expand(p['recipe'])))
 def export(self):
  events=[];h=self.head
  while h:
   r=self.event(h);events.append({k:r[k] for k in ('id','recipe','pre','post','primitive_count','stars','note','assistance')});h=r['parent']
  for r in events:r['recipe']=json.loads(r['recipe'])
  return dict(format='C600-STUDIO-SESSION-v1',model_id=self.m.model_id,events=list(reversed(events)),final_state=self.st.hash,prefs=self.prefs,root='labelled_identity',native_windows_equivalence=False)
 def import_record(self,record,camera=NO_CAMERA):
  from log_io import validate_record
  prefs=self._prefs_with_camera(camera)
  return self._import_plan(validate_record(self.m,record),prefs)
 def _import_plan(self,plan,prefs=None):
  if prefs is None:prefs=self.prefs
  new_state=plan['state'];name=self._recovery_name('import');old_head=self.head;selected_count=plan.get('selected_count',len(plan['events']))
  # All input has been verified before any persistent session mutation.
  with self.db:
   self._snapshot(name,prefs)
   head=0;total=0;stars=0;heads=[0]
   for depth,ev in enumerate(plan['events'],1):
    self.m.check_cancel();n=ev['length'];ss=ev['stars'];total+=n;stars+=ss
    cur=self.db.execute('INSERT INTO events(parent,recipe,pre,post,certificate,primitive_count,stars,total_primitives,total_stars,depth,created,note,assistance) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?)',(head,canonical(ev['recipe']),ev['pre'],ev['post'],'replayed-import',str(n),ss,str(total),stars,depth,time.time(),ev['note'],ev['assistance']));head=cur.lastrowid;heads.append(head)
   head=heads[selected_count]
   if selected_count+1<len(heads):self._put('redo:'+str(head),heads[selected_count+1])
   self._put('head',head);self._put('prefs',canonical(prefs))
   self.db.execute('INSERT OR REPLACE INTO snapshots VALUES (?,?,?,?,?,?)',(f'Imported {head} '+secrets.token_hex(4),head,zlib.compress(new_state.labels.astype('<i4').tobytes(),3),new_state.hash,canonical(prefs),time.time()))
   self.m.check_cancel()
  self.head=head;self.st=new_state;self.prefs=prefs;self.pending=None;self.redo_stack=[];self.rev+=1
  return dict(self.status(),import_checkpoint=name,previous_head=old_head,imported_transactions=plan['transactions'],native_log=plan.get('native_log'),note='Verified log imported as a new branch; previous progress and current preferences are retained.')
 def export_log(self,format='c600',native_profile=None,timer_ms=0):
  if format=='mpult':
   from mpult_log import export_native
   return export_native(self.m,self.export(),self.st.labels,native_profile,timer_ms)
  if format!='c600':raise ValueError('Unsupported log format')
  from log_io import encode_log
  return encode_log(self.export())
 def import_log(self,payload,format='c600',native_profile=None,camera=NO_CAMERA):
  if format=='mpult':
   from mpult_log import import_native
   prefs=self._prefs_with_camera(camera)
   return self._import_plan(import_native(self.m,payload,native_profile),prefs)
  from log_io import decode_log
  return self.import_record(decode_log(payload,format),camera)
 def save_log(self,format='c600',native_profile=None,timer_ms=0):
  payload=self.export_log(format,native_profile,timer_ms);directory=self.directory/'logs';directory.mkdir(exist_ok=True)
  suffix='.log' if format=='mpult' else '.c600.json.gz'
  name='C600-'+time.strftime('%Y%m%d-%H%M%S')+'-'+secrets.token_hex(4)+suffix;path=directory/name;tmp=directory/(name+'.tmp')
  try:
   with tmp.open('xb') as stream:stream.write(payload);stream.flush();os.fsync(stream.fileno())
   tmp.replace(path)
  finally:tmp.unlink(missing_ok=True)
  return dict(path=str(path),name=name,bytes=len(payload),sha256=digest(payload),state_hash=self.st.hash,head=self.head,format='MPUltimate-v1' if format=='mpult' else 'C600-STUDIO-SESSION-v1')
 def backup(self):
  p=self.directory/('backup_'+time.strftime('%Y%m%d_%H%M%S')+'.sqlite3');target=sqlite3.connect(p)
  self.db.backup(target);target.close();return dict(path=str(p),bytes=p.stat().st_size)
 def close(self):self.db.close();self.process_lock.close()
