"""Restricted-environment harness: real HTML/modules/GPU, HTTP via Python binding.
Normal navigation is blocked by a managed browser policy in this container.
Only the transport and module loading paths are adapted, not puzzle mechanics.
"""
from pathlib import Path
import json,re,base64,urllib.request,urllib.error,sys,os
from urllib.parse import urlparse,parse_qs
ROOT=Path(__file__).resolve().parents[1]
url=sys.argv[1] if len(sys.argv)>1 else os.environ.get('C600_TEST_URL','')
if not url:raise SystemExit('Pass the running local token-bearing URL as the first argument')
u=urlparse(url)
if u.hostname!='127.0.0.1':raise SystemExit('Test harness accepts only 127.0.0.1')
INFO={'base':f'{u.scheme}://{u.netloc}','token':parse_qs(u.query)['token'][0]}
def boot(page):
 def remote(req):
  path=req['url'];path=path if path.startswith('/') else '/'+path
  body=req.get('body');data=body.encode() if body is not None else None
  headers=req.get('headers',{})
  r=urllib.request.Request(INFO['base']+path,data=data,headers=headers,method=req.get('method','GET'))
  try:
   with urllib.request.urlopen(r,timeout=120) as x:return {'status':x.status,'headers':dict(x.headers),'body':base64.b64encode(x.read()).decode()}
  except urllib.error.HTTPError as e:return {'status':e.code,'headers':dict(e.headers),'body':base64.b64encode(e.read()).decode()}
 page.expose_function('__http600',remote)
 text=(ROOT/'web/index.html').read_text();text=re.sub(r'<script[^>]*>.*?</script>','',text,flags=re.S);text=re.sub(r'<link[^>]*stylesheet[^>]*>','',text)
 page.set_content(text)
 page.add_style_tag(content=(ROOT/'web/style.css').read_text())
 page.evaluate('''()=>{window.fetch=async(url,opt={})=>{let h={};if(opt.headers instanceof Headers)opt.headers.forEach((v,k)=>h[k]=v);else h=opt.headers||{};const r=await window.__http600({url:String(url),method:opt.method||'GET',headers:h,body:opt.body});const bin=Uint8Array.from(atob(r.body),c=>c.charCodeAt(0));return new Response(bin,{status:r.status,headers:r.headers});};}''')
 worker=(ROOT/'web/worker.js').read_text();renderer=(ROOT/'web/renderer.js').read_text().replace("new Worker('/web/worker.js')","new Worker(window.__workerURL)")
 page.evaluate('(s)=>window.__workerURL=URL.createObjectURL(new Blob([s],{type:"text/javascript"}))',worker)
 rendurl=page.evaluate('(s)=>URL.createObjectURL(new Blob([s],{type:"text/javascript"}))',renderer)
 app=(ROOT/'web/app.js').read_text().replace("'./renderer.js'",json.dumps(rendurl)).replace("new URLSearchParams(location.search).get('token')",json.dumps(INFO['token']))
 work=(ROOT/'web/workbench.js').read_text().replace("new URLSearchParams(location.search).get('token')",json.dumps(INFO['token']))
 print('BEFOREMODULE',flush=True);page.evaluate('async ([app,work])=>{const w=URL.createObjectURL(new Blob([work],{type:"text/javascript"}));await import(w);const a=URL.createObjectURL(new Blob([app],{type:"text/javascript"}));await import(a);}',[app,work])
 page.wait_for_function('window.c600?.shellReady',timeout=60000)
 return page
