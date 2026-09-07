"""Hard-process-exit recovery checks, not a power-loss simulation."""
from pathlib import Path
import sys,tempfile,subprocess,json,time
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import Model
from session import Session
m=Model();results=[]
with tempfile.TemporaryDirectory() as td:
 p=Path(td);s=Session(m,p);root=s.st.hash;s.close()
 code="""import os,sys
from pathlib import Path
from core import Model
from session import Session
s=Session(Model(),Path(sys.argv[1]));p=s.preview([{'kind':'word','moves':[2]}]);s.commit(p['token']);os._exit(77)
"""
 r=subprocess.run([sys.executable,'-c',code,str(p)],cwd=ROOT);assert r.returncode==77
 s=Session(m,p);assert s.head>0 and s.st.hash!=root;expected=s.st.hash;head=s.head;s.close();results.append('Hard exit after durable commit restores exact committed state')
 code="""import os,sys
from pathlib import Path
from core import Model
from session import Session
s=Session(Model(),Path(sys.argv[1]));s.db.execute('BEGIN IMMEDIATE');s.db.execute(\"UPDATE meta SET value='999999' WHERE key='head'\");os._exit(78)
"""
 r=subprocess.run([sys.executable,'-c',code,str(p)],cwd=ROOT);assert r.returncode==78
 s=Session(m,p);assert s.head==head and s.st.hash==expected;s.close();results.append('Hard exit during uncommitted transaction discards partial head update')
out={'passed':True,'checks':results,'scope':'Process termination and SQLite transaction recovery; no hardware power-loss testing'}
(ROOT/'tests/crash_report.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
