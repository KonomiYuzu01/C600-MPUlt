"""Check the live loopback server's basic request boundary without modifying state.
Usage: python tests/test_http_boundary.py "http://127.0.0.1:6006/?token=..."
"""
import argparse,urllib.request,urllib.error,json
from urllib.parse import urlparse,parse_qs
from pathlib import Path
ap=argparse.ArgumentParser();ap.add_argument('url');a=ap.parse_args();u=urlparse(a.url)
assert u.hostname=='127.0.0.1'
base=f'{u.scheme}://{u.netloc}';token=parse_qs(u.query)['token'][0]
def request(path,headers=None,body=None):
 req=urllib.request.Request(base+path,data=body,headers=headers or {})
 try:
  with urllib.request.urlopen(req) as r:return r.status,r.read()
 except urllib.error.HTTPError as r:return r.code,r.read()
status,_=request('/api/status');assert status==403
status,payload=request('/api/status',{'X-C600-Token':token});assert status==200;before=json.loads(payload)['state_hash']
status,_=request('/api/prefs',{'X-C600-Token':token,'Origin':'https://foreign.invalid','Content-Type':'application/json'},b'{}');assert status==403
status,_=request('/api/status',{'X-C600-Token':token,'Host':'foreign.invalid'});assert status==403
status,_=request('/assets/../session.py');assert status==404
status,payload=request('/api/status',{'X-C600-Token':token});assert json.loads(payload)['state_hash']==before
r={'passed':True,'scope':'Live local HTTP boundary tests; not a complete security audit','checks':['Unauthenticated state access rejected','Foreign write Origin rejected','Wrong Host rejected','Asset traversal rejected','Puzzle state unchanged']}
(Path(__file__).resolve().parent/'http_boundary_report.json').write_text(json.dumps(r,indent=2));print(json.dumps(r,indent=2))
