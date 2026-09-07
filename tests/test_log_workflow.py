"""Real Windows Python/loopback reset and durable proof-log regressions; no GPU."""
from pathlib import Path
import argparse, base64, copy, gzip, hashlib, json, platform, sqlite3, sys, tempfile, time, traceback
from urllib.error import HTTPError
from urllib.request import Request, ProxyHandler, build_opener
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import Model, canonical, state_hash
from session import Session
from enhanced import Workflow
from engine_process import EngineProcess
from log_io import decode_log, encode_log, validate_record, MAX_JSON_BYTES
from verify import verify_session

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--work-dir',type=Path);args=ap.parse_args()
    work=args.work_dir or Path(tempfile.mkdtemp(prefix='c600-logs-'));work.mkdir(parents=True,exist_ok=True)
    assert not (work/'direct/session.sqlite3').exists() and not (work/'http/session.sqlite3').exists(),'Use a fresh directory'
    hashes={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in ('core.py','session.py','enhanced.py','server.py','log_io.py')}
    checks=[];failures=[];m=Model();s=Session(m,work/'direct');record=None
    def check(name,fn):
        start=time.perf_counter()
        try:detail=fn() or {};checks.append(dict(name=name,passed=True,seconds=time.perf_counter()-start,**detail));print('PASS',name,flush=True)
        except Exception as exc:failures.append(dict(name=name,error=str(exc),traceback=traceback.format_exc()));print('FAIL',name,str(exc),flush=True)
    def commit(moves):p=s.preview([dict(kind='word',moves=moves)]);return s.commit(p['token'])
    def fingerprint():
        return (s.head,s.st.labels.tobytes(),canonical(s.prefs),s.rev,copy.deepcopy(s.pending),list(s.redo_stack),list(s.db.iterdump()))
    def same(before):
        after=fingerprint();assert before[:4]==after[:4] and before[5:]==after[5:]
        assert (before[4]['token'] if before[4] else None)==(after[4]['token'] if after[4] else None)
    def rejected(fn,types=(ValueError,)):
        before=fingerprint()
        try:fn()
        except types:pass
        else:raise AssertionError('Invalid operation was accepted')
        same(before)
    try:
        def reset_recovery():
            Workflow(s).scramble(1000,47,apply=True);s.save_prefs(dict(orbit=2,rules=[dict(expr='everything',style='solid')],pin_safety=False,camera={'probe':'preserve'},protected=[1]))
            old=s.head;labels=s.st.labels.copy();prefs=canonical(s.prefs);Workflow(s).timer('pause');timer=s._get('timer_seconds')
            result=s.reset();assert result['solved'] and s.head==0 and s.pending is None and canonical(s.prefs)==prefs and s._get('timer_seconds')==timer
            assert s.event(old) is not None;s.restore(result['reset_checkpoint']);assert np.array_equal(labels,s.st.labels) and s.head==old and canonical(s.prefs)==prefs
            return dict(labelled_slots=m.n,recovery_checkpoint=result['reset_checkpoint'],retained_head=old)
        check('1000-turn reset returns solved and durable checkpoint restores all labels/preferences',reset_recovery)
        def create_record():
            nonlocal record
            s.save_prefs({'protected':[]});commit([2,4,6]);p=s.preview([dict(kind='star',orbit=33,node=0,sign=1)]);s.commit(p['token'])
            record=s.export();assert verify_session(m,record,expand=True)['passed']
            saved=s.save_log();raw=Path(saved['path']).read_bytes();assert saved['sha256']==hashlib.sha256(raw).hexdigest()
            assert decode_log(base64.b64encode(raw).decode())==record and json.loads(gzip.decompress(raw))==record
            again=s.save_log();assert again['path']!=saved['path'] and Path(again['path']).read_bytes()==raw
            assert not list((s.directory/'logs').glob('*.tmp'))
            (work/'saved-proof.c600.json.gz').write_bytes(raw)
            return dict(transactions=len(record['events']),proof_sha256=saved['sha256'])
        check('Save log is unique, atomic and independently primitive-expanded verifiable',create_record)
        def imported_branch():
            old=s.head;commit([8]);old=s.head;before=s.st.labels.copy();prefs=canonical(s.prefs);imported=copy.deepcopy(record);imported['prefs']={'orbit':34,'rules':[]}
            result=s.import_log(base64.b64encode(encode_log(imported)).decode());assert s.st.hash==record['final_state'] and canonical(s.prefs)==prefs and s.head!=old
            assert result['previous_head']==old and s.event(old) is not None and result['imported_transactions']==len(record['events'])
            imported_head=s.head;s.undo();s.redo();assert s.st.hash==record['final_state'];s.restore(result['import_checkpoint']);assert s.head==old and np.array_equal(before,s.st.labels)
            Workflow(s).checkout(imported_head);assert verify_session(m,s.export(),expand=True)['passed']
            return dict(imported_head=imported_head,preserved_head=old,prefs_ignored=True)
        check('Verified import adds a branch with full undo/redo and prior-progress recovery',imported_branch)
        def malformed_records():
            s.preview([dict(kind='word',moves=[10])]);cases=[]
            for key,value in [('format','foreign'),('model_id','wrong'),('root','unlabelled'),('native_windows_equivalence',True),('prefs',[]),('final_state','0'*64),('events',[{}]),('events',[None])]:
                value_record=copy.deepcopy(record);value_record[key]=value;cases.append(value_record)
            for key,value in [('pre','0'*64),('post','0'*64),('primitive_count',True),('primitive_count','1'),('stars',True),('stars',99),('note',[]),('assistance','x'*81),('recipe',[dict(kind='word',moves=[True])]),('recipe',[dict(kind='word',moves=[1201])]),('recipe',[dict(kind='star',orbit=-1,node=0)])]:
                value_record=copy.deepcopy(record);value_record['events'][0][key]=value;cases.append(value_record)
            cases.extend([[],None,{},dict(record,prefs={'not_json':float('nan')})])
            for bad in cases:rejected(lambda:s.import_record(bad))
            return dict(invalid_cases=len(cases),pending_and_journal_unchanged=True)
        check('Malformed models, roots, counts, legal recipes and claims do not mutate journal or preview',malformed_records)
        def payload_failures():
            raw=encode_log(record);duplicate=b'{"format":1,"format":2}';bomb=gzip.compress(b' '*(MAX_JSON_BYTES+1),compresslevel=1)
            for payload in ('%%%','',base64.b64encode(raw[:-5]).decode(),base64.b64encode(duplicate).decode(),base64.b64encode(b'\xffbad').decode(),base64.b64encode(bomb).decode()):
                rejected(lambda:s.import_log(payload))
            rejected(lambda:s.import_log(base64.b64encode(raw).decode(),'unknown'))
            return dict(corrupt_cases=7,gzip_expansion_limit=MAX_JSON_BYTES)
        check('Bounded decoding rejects corrupt gzip, duplicate JSON, UTF8 errors and decompression overflow',payload_failures)
        def budget():
            bad=copy.deepcopy(record);bad['events']=[bad['events'][0]]*10001;rejected(lambda:s.import_record(bad))
            bad=copy.deepcopy(record);bad['events']=[dict(bad['events'][0],recipe=[dict(kind='word',moves=[2]*100000)],primitive_count='100000',stars=0)]*21
            rejected(lambda:s.import_record(bad));return dict(max_transactions=10000,max_primitives=2000000)
        check('Oversize transaction and primitive budgets reject before replay or persistent writes',budget)
        def transaction_fault():
            def authorizer(action,table,*_):return sqlite3.SQLITE_DENY if action==sqlite3.SQLITE_INSERT and table=='meta' else sqlite3.SQLITE_OK
            for fn in (s.reset,lambda:s.import_record(record)):
                before=fingerprint();s.db.set_authorizer(authorizer)
                try:
                    try:fn()
                    except sqlite3.DatabaseError:pass
                    else:raise AssertionError('Injected write failure accepted')
                finally:s.db.set_authorizer(None)
                same(before)
            return dict(rollback_includes_auto_checkpoints=True)
        check('SQLite write failures roll back reset/import and their recovery checkpoints',transaction_fault)
        def cancelled_import():
            class Cancel:
                def is_set(self):return True
            m.cancel_event=Cancel()
            try:rejected(lambda:s.import_record(record),(InterruptedError,));rejected(s.reset,(InterruptedError,))
            finally:m.cancel_event=None
        check('Cancellation leaves state, preview, preferences and durable journal unchanged',cancelled_import)
        expected=dict(head=s.head,hash=s.st.hash,prefs=canonical(s.prefs),labels=s.st.labels.tobytes());s.close();s=Session(m,work/'direct')
        check('Reopening imported branch restores exact full labels and view preferences',lambda:dict(passed_reopen=(s.head==expected['head'] and s.st.hash==expected['hash'] and canonical(s.prefs)==expected['prefs'] and s.st.labels.tobytes()==expected['labels'])) if (s.head==expected['head'] and s.st.hash==expected['hash'] and canonical(s.prefs)==expected['prefs'] and s.st.labels.tobytes()==expected['labels']) else (_ for _ in ()).throw(AssertionError('Reopen mismatch')))
    finally:s.close()
    engine=None
    def api(path,body=None,raw=False):
        req=Request(engine.info['base']+'/api/'+path,data=None if body is None else json.dumps(body).encode(),headers={'Content-Type':'application/json','X-C600-Token':engine.info['token']})
        try:
            with build_opener(ProxyHandler({})).open(req,timeout=180) as reply:payload=reply.read()
        except HTTPError as exc:raise ValueError('HTTP '+str(exc.code)+': '+exc.read().decode())
        if raw:return payload
        data=json.loads(payload)
        if 'job' in data:
            until=time.monotonic()+180
            while time.monotonic()<until:
                result=api('job/'+data['job'])
                if result.get('done'):
                    if 'error' in result:raise ValueError(result['error'])
                    return result['result']
                time.sleep(.01)
            raise TimeoutError('HTTP job timeout')
        return data
    def http_workflow():
        nonlocal engine
        with EngineProcess(ROOT,work/'http',work/'launch.json',work/'engine.log',timeout=240) as engine:
            api('prefs',{'orbit':2,'pin_safety':False});api('scramble',{'count':1000,'seed':9,'apply':True});old=api('status');labels=api('labels',raw=True)
            saved=api('log/save',{});payload=api('log/export?format=c600',raw=True);assert Path(saved['path']).read_bytes()==payload
            assert verify_session(m,json.loads(gzip.decompress(payload)),expand=True)['passed']
            reset=api('reset',{});assert reset['solved'] and reset['prefs']==old['prefs'];api('restore',{'name':reset['reset_checkpoint']});assert api('labels',raw=True)==labels
            api('reset',{});result=api('log/import',{'format':'c600','data_base64':base64.b64encode(payload).decode()});assert result['state_hash']==old['state_hash'] and api('labels',raw=True)==labels
            for body in ({'data_base64':'%%%','format':'c600'},{'data_base64':base64.b64encode(payload).decode(),'format':'wrong'}):
                before=api('status')
                try:api('log/import',body)
                except ValueError as exc:assert '500' not in str(exc)
                else:raise AssertionError('Malformed HTTP log accepted')
                assert api('status')==before
            api('import',{'record':json.loads(gzip.decompress(payload))});assert api('labels',raw=True)==labels
            head=api('status')['head']
        with EngineProcess(ROOT,work/'http',work/'launch-reopen.json',work/'engine-reopen.log',timeout=240) as engine:
            assert api('status')['head']==head and api('labels',raw=True)==labels and api('status')['prefs']==old['prefs']
        return dict(labelled_slots=259800,real_engine_reopened=True,graceful_children=True)
    check('Actual authenticated Windows HTTP reset/save/export/import and process restart preserve full state',http_workflow)
    after={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in hashes}
    if hashes!=after:failures.append(dict(name='Source freeze',error='Production sources changed during regression'))
    report=dict(passed=not failures,scope='Actual local Windows Python/SQLite and authenticated HTTP; no GUI/GPU claim',platform=platform.platform(),python=sys.version,source_sha256=hashes,source_after=after,checks=checks,failures=failures)
    (work/'log-workflow-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(dict(passed=report['passed'],checks=len(checks),failures=len(failures),report=str(work/'log-workflow-report.json')),ensure_ascii=False))
    return 0 if report['passed'] else 1
if __name__=='__main__':raise SystemExit(main())
