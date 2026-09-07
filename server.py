"""Local-only C600 Studio server. No public listener, telemetry, CDN or account.
Write requests require an unpredictable launch token and same-origin validation.
"""
from __future__ import annotations
import argparse,base64,gzip,json,os,secrets,sys,threading,time,webbrowser,signal
from concurrent.futures import ThreadPoolExecutor
from http.server import ThreadingHTTPServer,BaseHTTPRequestHandler
from pathlib import Path
from urllib.parse import urlparse,parse_qs
import numpy as np
from core import Model,canonical
from session import Session,NO_CAMERA
from enhanced import Workflow
from native_bridge import verify_native
from grips import grips
from engine_process import PROTOCOL, redact
ROOT=Path(__file__).resolve().parent

def atomic_json(path,payload,timeout=2.0):
 """Publish one complete UTF-8 JSON file despite brief Windows reader locks."""
 path.parent.mkdir(parents=True,exist_ok=True);tmp=path.with_suffix('.tmp')
 text=canonical(payload);deadline=time.monotonic()+timeout
 while True:
  try:
   tmp.write_text(text,encoding='utf-8');tmp.replace(path);return
  except PermissionError:
   # Windows readers/scanners may omit FILE_SHARE_DELETE while reading the
   # previous file. Keep it intact and retry the atomic replace, never unlink it.
   remaining=deadline-time.monotonic()
   if remaining<=0:raise
   time.sleep(min(.025,remaining))

def watch_parent(stopping,request_shutdown):
 """Watch the owned stdin pipe without holding Python's buffered-reader lock.

 Polling lets failed startup and API shutdown join this thread even when the
 parent still owns the write end. A blocked BufferedReader daemon can abort
 CPython during finalization, hiding the original startup failure on Windows.
 """
 try:
  fd=sys.stdin.fileno()
  if os.name=='nt':
   import ctypes,msvcrt
   from ctypes import wintypes
   kernel=ctypes.WinDLL('kernel32',use_last_error=True);peek=kernel.PeekNamedPipe
   peek.argtypes=[wintypes.HANDLE,wintypes.LPVOID,wintypes.DWORD,wintypes.LPDWORD,wintypes.LPDWORD,wintypes.LPDWORD];peek.restype=wintypes.BOOL
   handle=msvcrt.get_osfhandle(fd);available=wintypes.DWORD()
  else:import select
  while not stopping.is_set():
   if os.name=='nt':
    if not peek(handle,None,0,None,ctypes.byref(available),None):break
    if not available.value:stopping.wait(.05);continue
    amount=min(4096,available.value)
   else:
    if not select.select([fd],[],[],.05)[0]:continue
    amount=4096
   if not os.read(fd,amount):break
 except (OSError,ValueError):pass
 if not stopping.is_set():request_shutdown()

def main():
 parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=6006);parser.add_argument('--data',type=Path);parser.add_argument('--no-browser',action='store_true');parser.add_argument('--launch-info',type=Path);args=parser.parse_args()
 if sys.maxsize<=2**32 or sys.version_info<(3,11):raise SystemExit('Use 64-bit Python 3.11 or newer')
 data=args.data or (Path(os.getenv('LOCALAPPDATA',str(Path.home())))/'C600Studio')
 launch_id=os.environ.get('C600_LAUNCH_ID') or secrets.token_hex(32)
 started=time.monotonic()
 def startup(stage,detail=''):
  print('State engine: '+stage+(' ('+detail+')' if detail else ''),flush=True)
  if args.launch_info:
   try:atomic_json(args.launch_info.with_suffix('.status.json'),dict(launch_id=launch_id,pid=os.getpid(),stage=stage,seconds=time.monotonic()-started,detail=redact(detail)))
   except OSError as error:
    # Progress is diagnostic only. The separately authenticated health reply
    # and mandatory launch metadata determine readiness, not this status file.
    print('State engine: progress file update unavailable: '+redact(str(error)),flush=True)
 startup('loading_assets')
 model=Model()
 startup('opening_session')
 session=Session(model,data)
 workflow=Workflow(session);cancel_event=threading.Event();native_profile=[None];lock=threading.RLock()
 token=secrets.token_urlsafe(32);pool=ThreadPoolExecutor(max_workers=1);jobs={};address=f'127.0.0.1:{args.port}'
 server=None;stopping=threading.Event();serving=threading.Event()
 def request_shutdown():
  if stopping.is_set():return
  stopping.set();cancel_event.set()
  def stop_listener():
   # BaseServer.shutdown must run in another thread, after serve_forever starts.
   serving.wait()
   if server is not None:server.shutdown()
  threading.Thread(target=stop_listener,name='c600-shutdown',daemon=True).start()
 parent_thread=None
 if os.environ.get('C600_WATCH_PARENT_STDIN')=='1':
  parent_thread=threading.Thread(target=watch_parent,args=(stopping,request_shutdown),name='c600-parent-watch',daemon=True);parent_thread.start()
 old_signal={}
 if threading.current_thread() is threading.main_thread():
  for sig in (signal.SIGINT,signal.SIGTERM):
   old_signal[sig]=signal.getsignal(sig);signal.signal(sig,lambda *_:request_shutdown())
 try:
  class Handler(BaseHTTPRequestHandler):
   def setup(self):
    super().setup();self.connection.settimeout(20)
   def log_message(self,*_):pass
   def send(self,status,content,ctype='application/json',filename=None):
    self.send_response(status);self.send_header('Content-Type',ctype);self.send_header('Content-Length',str(len(content)));self.send_header('Cache-Control','no-store');self.send_header('X-Content-Type-Options','nosniff');self.send_header('Referrer-Policy','no-referrer')
    self.send_header('Content-Security-Policy',"default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self'; worker-src 'self'; img-src 'self' data:; frame-ancestors 'none'")
    if filename:self.send_header('Content-Disposition',f'attachment; filename="{filename}"')
    self.end_headers();self.wfile.write(content)
   def js(self,obj,status=200):self.send(status,canonical(obj).encode())
   def valid_host(self):return self.headers.get('Host')==address
   def authenticated(self):return secrets.compare_digest(self.headers.get('X-C600-Token',''),token)
   def do_GET(self):
    try:
     if not self.valid_host():return self.js({'error':'Invalid Host'},403)
     u=urlparse(self.path);path=u.path
     if path=='/':
      supplied=parse_qs(u.query).get('token',[''])[0]
      if not secrets.compare_digest(supplied,token):return self.js({'error':'Open the launch URL printed by server.py'},403)
      return self.send(200,(ROOT/'web'/'index.html').read_bytes(),'text/html; charset=utf-8')
     if path.startswith('/web/'):
      name=path[5:]
      if '/' in name or name not in ('app.js','renderer.js','worker.js','style.css','workbench.js'):return self.js({'error':'Not found'},404)
      return self.send(200,(ROOT/'web'/name).read_bytes(),'text/css' if name.endswith('.css') else 'text/javascript')
     if path.startswith('/assets/'):
      name=path[8:]
      allowed={'mesh.json','mesh_vertices.f32','mesh_sticker.u32','mesh_centers.f32','cell_frames.f32','slot_piece.u32','cell_normals.f32'}
      if name not in allowed:return self.js({'error':'Not found'},404)
      return self.send(200,(ROOT/'assets'/name).read_bytes(),'application/json' if name.endswith('.json') else 'application/octet-stream')
     if not self.authenticated():return self.js({'error':'Unauthorized'},403)
     if path=='/api/health':
      return self.js(dict(protocol=PROTOCOL,ready=not stopping.is_set(),pid=os.getpid(),launch_id=launch_id,model_id=model.model_id,python_prefix=sys.prefix,python_executable=sys.executable))
     if path.startswith('/api/job/'):
      j=jobs.get(path.rsplit('/',1)[1])
      if j is None:return self.js({'error':'Unknown job'},404)
      if not j.done():return self.js({'done':False})
      try:return self.js({'done':True,'result':j.result()})
      except Exception as exc:return self.js({'done':True,'error':str(exc)})
     with lock:
      if path=='/api/status':return self.js(session.status())
      if path=='/api/history':return self.js(workflow.history(parse_qs(u.query).get('before',[None])[0]))
      if path=='/api/stats':return self.js(workflow.stats())
      if path=='/api/catalog':return self.js([dict(orbit=x['orbit'],rank=x['rank'],stickers=x['stickers'],pieces=x['pieces'],orientation=x['orientation'],length=x['seed_length'],formula=x['formula'],collateral=x['collateral']) for x in model.atlas])
      if path=='/api/grips':return self.js(grips(model,int(parse_qs(u.query).get('cell',['0'])[0])))
      if path=='/api/cap':return self.send(200,model.cap_mask(int(parse_qs(u.query).get('cell',['0'])[0]))[model.sp].astype('u1').tobytes(),'application/octet-stream')
      if path=='/api/native/map':
       if native_profile[0] is None:raise ValueError('Native bridge has not passed')
       p=native_profile[0];return self.js(dict(native_to_lab=p['native_to_lab'],native_face_to_lab=p['native_face_to_lab'],profile_sha256=p['profile_sha256'],matched_stickers=p['matched_stickers'],matched_generators=p['matched_generators']))
      if path=='/api/native/snapshot':
       if native_profile[0] is None:raise ValueError('Native bridge has not passed')
       # One lock and one response bind status, colors, and visibility to the same
       # committed state. Three separate GETs could mix concurrent browser edits.
       p=native_profile[0];mapping=np.asarray(p['native_to_lab'],np.int32)
       inv=np.argsort(p['native_face_to_lab'])
       colors=inv[session.st.labels[mapping]//433].astype('<i2').tobytes()
       styles=session.render_styles()[mapping].tobytes()
       return self.js(dict(format='C600-native-snapshot-v1',profile_sha256=p['profile_sha256'],state=session.status(),colors=base64.b64encode(colors).decode('ascii'),styles=base64.b64encode(styles).decode('ascii')))
      if path in ('/api/native/colors','/api/native/styles'):
       if native_profile[0] is None:raise ValueError('Native bridge has not passed')
       p=native_profile[0];mapping=np.asarray(p['native_to_lab'],np.int32)
       if path.endswith('colors'):
        inv=np.argsort(p['native_face_to_lab']);arr=inv[session.st.labels[mapping]//433].astype('<i2')
       else:arr=session.render_styles()[mapping]
       return self.send(200,arr.tobytes(),'application/octet-stream')
      if path=='/api/labels':return self.send(200,session.st.labels.astype('<u4').tobytes(),'application/octet-stream')
      if path=='/api/styles':return self.send(200,session.render_styles().tobytes(),'application/octet-stream')
      if path=='/api/certificate':return self.send(200,canonical(session.certificate()).encode(),'application/json','macro_certificate.c600.json')
      if path=='/api/export':return self.send(200,gzip.compress(canonical(session.export()).encode(),compresslevel=5),'application/gzip','session.c600.json.gz')
      if path=='/api/log/export':
       formats=parse_qs(u.query).get('format',['c600'])
       if len(formats)!=1 or formats[0] not in ('c600','mpult'):raise ValueError('Unsupported log format; choose c600 or mpult')
       format=formats[0];payload=session.export_log(format,native_profile[0],int(workflow.timer()['seconds']*1000))
       return self.send(200,payload,'text/plain; charset=utf-8' if format=='mpult' else 'application/gzip','session.log' if format=='mpult' else 'session.c600.json.gz')
      return self.js({'error':'Not found'},404)
    except (ValueError,KeyError,TypeError) as exc:self.js({'error':str(exc)},400)
    except Exception as exc:self.js({'error':f'{type(exc).__name__}: {exc}'},500)
   def do_POST(self):
    try:
     if not self.valid_host() or not self.authenticated():return self.js({'error':'Unauthorized'},403)
     origin=self.headers.get('Origin')
     if origin and origin!='http://'+address:return self.js({'error':'Foreign origin rejected'},403)
     size=int(self.headers.get('Content-Length','0'))
     if not 0<size<=(64 if urlparse(self.path).path=='/api/native/handshake' else 24)*1024*1024:return self.js({'error':'Invalid request size'},413)
     if 'application/json' not in self.headers.get('Content-Type',''):return self.js({'error':'JSON required'},415)
     try:body=json.loads(self.rfile.read(size))
     except RecursionError:raise ValueError('JSON request nesting is too deep')
     path=urlparse(self.path).path
     if not isinstance(body,dict):return self.js({'error':'Request body must be a JSON object'},400)
     if path=='/api/shutdown':
      if not isinstance(body,dict) or not secrets.compare_digest(str(body.get('launch_id','')),launch_id):return self.js({'error':'Wrong launch ID'},403)
      request_shutdown();return self.js({'stopping':True})
     if stopping.is_set():return self.js({'error':'State engine is stopping'},503)
     if path=='/api/stop-job':
      cancel_event.set();return self.js({'cancel_requested':True})
     def work():
      with lock:
       if stopping.is_set():raise ValueError('State engine is stopping')
       if path=='/api/prefs':return session.save_prefs(body)
       if path=='/api/reset':return session.reset(body.get('camera',NO_CAMERA))
       if path=='/api/log/save':return session.save_log(body.get('format','c600'),native_profile[0],int(workflow.timer()['seconds']*1000))
       if path=='/api/log/import':return session.import_log(body['data_base64'],body.get('format','c600'),native_profile[0],body.get('camera',NO_CAMERA))
       if path=='/api/checkout':return workflow.checkout(body['head'])
       if path=='/api/timer':return workflow.timer(body.get('action','status'))
       if path=='/api/save-set':return workflow.save_set(body['name'],body['expression'],body['kind'])
       if path=='/api/compose':return workflow.compose(body['a'],body.get('b',[]),body['operation'])
       if path=='/api/save-macro':return workflow.save_macro(body['name'])
       if path=='/api/scramble':return workflow.scramble(body.get('count',1000),body.get('seed'),apply=body.get('apply',False))
       if path=='/api/native/handshake':
        p=verify_native(model,body,session.directory);native_profile[0]=p
        return dict(matched_stickers=p['matched_stickers'],matched_generators=p['matched_generators'],profile_sha256=p['profile_sha256'],seconds=p['seconds'])
       if path=='/api/native/select':
        if native_profile[0] is None:raise ValueError('Native bridge has not passed')
        ns=body.get('native_sticker')
        if type(ns)!=int or not 0<=ns<model.n:raise ValueError('Invalid native sticker')
        lab=native_profile[0]['native_to_lab'][ns]
        if session.render_styles()[lab]==0:raise ValueError('Hidden stickers cannot be selected through the native viewport')
        pos=int(model.sp[lab]);piece=session.st.piece(pos);session.save_prefs({'selected':piece['piece']});return piece
       if path=='/api/native/turn':
        if native_profile[0] is None:raise ValueError('Native bridge has not passed')
        if body.get('pre_state')!=session.st.hash:raise ValueError('Native view is stale; state resynchronization required')
        tokens=body.get('tokens',[])
        if not isinstance(tokens,list) or not 1<=len(tokens)<=10000:raise ValueError('Native turn batch must contain 1..10,000 records')
        words=[];table=native_profile[0]['token_words']
        for tok in tokens:
         if tok not in table:raise ValueError('Unsupported native move '+str(tok))
         words.extend(table[tok])
        if not words:return session.status()
        p=session.preview([dict(kind='word',moves=words)],'Native MPUlt turn','manual-native')
        return session.commit(p['token'])
       if path=='/api/preview':return session.preview(body['recipe'],body.get('note',''),'manual')
       if path=='/api/suggest':return session.suggest(body['orbit'],body.get('batch',1),body.get('target'))
       if path=='/api/commit':return session.commit(body['token'])
       if path=='/api/cancel':session.pending=None;return session.status()
       if path=='/api/undo':return session.undo()
       if path=='/api/redo':return session.redo()
       if path=='/api/checkpoint':return session.checkpoint(body.get('name'))
       if path=='/api/restore':return session.restore(body['name'])
       if path=='/api/backup':return session.backup()
       if path=='/api/buffers':return session.planner.buffer_info(session.st,body['orbit'],body.get('target'))
       if path=='/api/piece':return session.st.piece(body['position'])
       if path=='/api/track-piece':
        p=body['piece']
        if type(p)!=int or not 0<=p<model.np:raise ValueError('Piece identity must be an integer in range')
        return session.st.piece(int(session.st.where[p]))
       if path=='/api/find-required':
        p=body['position']
        if type(p)!=int or not 0<=p<model.np:raise ValueError('Position must be an integer in range')
        return session.st.piece(int(session.st.where[p]))
       if path=='/api/next-piece':
        o=body['orbit'];pos=body['position'];direction=body['direction']
        if type(o)!=int or not 0<=o<35 or type(direction)!=int or direction not in(-1,1):raise ValueError('Invalid orbit/direction')
        if type(pos)!=int or not 0<=pos<model.np:raise ValueError('Position must be an integer in range')
        choices=np.flatnonzero((model.oid==o)&~session.st.correct)
        if not len(choices):raise ValueError('This orbit is fully solved')
        i=np.searchsorted(choices,pos,side='right') if direction>0 else np.searchsorted(choices,pos,side='left')-1
        return session.st.piece(int(choices[i%len(choices)]))
       if path=='/api/select':
        p=body['position'];self_piece=session.st.piece(p);session.save_prefs({'selected':self_piece['piece']});return self_piece
       if path=='/api/demo':
        if session.head!=0:raise ValueError('Create a checkpoint, then restore Solved root before adding the demo scramble')
        w=json.loads((ROOT/'assets'/'demo_scramble.json').read_text());return session.preview([{'kind':'word','moves':w}],'Verified seed-600 1,000-move scramble','recorded-scramble')
       if path=='/api/import':return session.import_record(body['record'],body.get('camera',NO_CAMERA))
       raise ValueError('Unknown command')
     if path in('/api/preview','/api/suggest','/api/import','/api/log/import','/api/demo','/api/checkout','/api/compose','/api/scramble','/api/native/handshake'):
      # Long preview work runs away from the browser rendering/event loop.
      if any(not f.done() for f in jobs.values()):return self.js({'error':'An analysis job is already running'},409)
      for k in list(jobs):
       if jobs[k].done() and len(jobs)>20:del jobs[k]
      jid=secrets.token_hex(10);cancel_event.clear()
      def cancelable_work():
       model.cancel_event=cancel_event
       try:return work()
       finally:model.cancel_event=None
      jobs[jid]=pool.submit(cancelable_work);return self.js({'job':jid})
     return self.js(work())
    except (ValueError,KeyError,TypeError) as exc:self.js({'error':str(exc)},400)
    except Exception as exc:self.js({'error':f'{type(exc).__name__}: {exc}'},500)
  class EngineHTTPServer(ThreadingHTTPServer):
   # Finish outstanding request handlers before closing SQLite on Windows.
   daemon_threads=False
   block_on_close=True
  startup('binding_loopback')
  server=EngineHTTPServer(('127.0.0.1',args.port),Handler)
  address=f'127.0.0.1:{server.server_address[1]}'
  url=f'http://{address}/?token={token}'
  if args.launch_info:
   args.launch_info.parent.mkdir(parents=True,exist_ok=True)
   atomic_json(args.launch_info,dict(protocol=PROTOCOL,launch_id=launch_id,model_id=model.model_id,
                                   url=url,token=token,base='http://'+address,pid=os.getpid()))
  startup('ready')
  print(f'C600 Studio: {url}\nSession directory: {data}\nModel loaded in {model.load_seconds:.3f}s. Press Ctrl+C to stop.',flush=True)
  if not args.no_browser:webbrowser.open(url)
  serving.set()
  server.serve_forever(poll_interval=.1)
 finally:
  stopping.set();cancel_event.set()
  if parent_thread is not None:
   parent_thread.join(timeout=1.0)
   if parent_thread.is_alive():print('State engine: parent watcher did not stop within 1 second',flush=True)
  try:
   if server is not None:server.server_close()
   pool.shutdown(wait=True,cancel_futures=True)
  finally:
   try:workflow.timer('pause')
   finally:session.close()
   for sig,previous in old_signal.items():signal.signal(sig,previous)
   if args.launch_info:
    try:
     if json.loads(args.launch_info.read_text(encoding='utf-8-sig')).get('launch_id')==launch_id:args.launch_info.unlink(missing_ok=True)
    except (OSError,ValueError):pass
   startup('stopped')
if __name__=='__main__':main()
