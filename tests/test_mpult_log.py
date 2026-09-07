"""MPUlt v1 codec and branch tests using a retained actual native bridge profile.

Original MPUlt Save/Load is tested separately by NativeSessionLogRegression.
"""
from pathlib import Path
import argparse,base64,copy,hashlib,json,sys,time,traceback
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import Model,PuzzleState,canonical
from session import Session
from mpult_log import export_native,import_native,profile_data,native_colors,serialize,description
from verify import verify_session

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--profile',type=Path,required=True);ap.add_argument('--work-dir',type=Path,required=True);ap.add_argument('--original-log',type=Path);args=ap.parse_args()
    work=args.work_dir;work.mkdir(parents=True,exist_ok=True);assert not (work/'data/session.sqlite3').exists(),'Use a fresh directory'
    model=Model();profile=json.loads(args.profile.read_text(encoding='utf-8-sig'));s=Session(model,work/'data');checks=[];failures=[]
    source={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in ('session.py','log_io.py','mpult_log.py','server.py')}
    def check(name,fn):
        start=time.perf_counter()
        try:
            detail=fn() or {}
            if 'passed' in detail:assert detail.pop('passed') is True
            checks.append(dict(name=name,passed=True,seconds=time.perf_counter()-start,**detail));print('PASS',name,flush=True)
        except Exception as exc:failures.append(dict(name=name,error=str(exc),traceback=traceback.format_exc()));print('FAIL',name,str(exc),flush=True)
    def b64(data):return base64.b64encode(data).decode('ascii')
    def snapshot():return s.head,s.st.labels.tobytes(),canonical(s.prefs),s.rev,list(s.db.iterdump()),s.pending['token'] if s.pending else None
    def reject(payload,which=profile):
        before=snapshot()
        try:s.import_log(b64(payload),'mpult',which)
        except ValueError:pass
        else:raise AssertionError('Invalid native log accepted')
        assert snapshot()==before
    def color_for(tokens):
        labels=model.ids.copy()
        for token in tokens:
            if token in ('m[','m]'):continue
            a,t,angle,mask=map(int,token.split(':'));word=table[f'{a}:{t}:{angle%orders[a,t]}:{mask}']
            for move in word:src,dst=model.move(move);labels[dst]=labels[src]
        return labels,native_colors(labels,mapping,inverse)
    mapping,inverse,table,orders=profile_data(model,profile)
    tokens=['0:0:1:1','1:1:-1:4','2:0:1:5'];labels,colors=color_for(tokens)
    try:
        if args.original_log:
            def original_file():
                raw=args.original_log.read_bytes();plan=import_native(model,b64(raw),profile)
                assert plan['native_log']['all_colors_verified']==259800 and plan['native_log']['checksum_verified']
                result=s.import_log(b64(raw),'mpult',profile);assert s.st.hash==plan['state'].hash
                return dict(file_sha256=hashlib.sha256(raw).hexdigest(),native_log=plan['native_log'],state_hash=s.st.hash)
            check('Actual original MPUlt Save file exact decimals, CRC, colors and active pointer import',original_file)
        def negative_mask():
            raw=serialize(colors,tokens,timer=12345);plan=import_native(model,b64(raw),profile)
            assert np.array_equal(plan['state'].labels,labels) and plan['native_log']['checksum_verified']
            assert plan['native_log']['timer_milliseconds']==12345
            (work/'negative-and-combined.log').write_bytes(raw)
            return dict(labelled_slots=model.n,negative_angle=True,opposite_cap_mask=5)
        check('Signed inverse angle and combined outer-cap mask preserve all labelled stickers and CRC',negative_mask)
        def partial_redo():
            grouped=['m[',tokens[0],tokens[1],'m]',tokens[2]];active,active_colors=color_for(grouped[:2])
            raw=serialize(active_colors,grouped,shuffle=2,ptr=2,timer=12345)
            s.save_prefs(dict(orbit=2,pin_safety=False));p=s.preview([dict(kind='word',moves=[24])]);s.commit(p['token']);old=snapshot()
            result=s.import_log(b64(raw),'mpult',profile);assert s.st.labels.tobytes()==active.tobytes() and result['native_log']['redo_transactions']==2 and s.status()['can_redo']
            s.redo();s.redo();assert s.st.labels.tobytes()==labels.tobytes();s.undo();s.undo();assert s.st.labels.tobytes()==active.tobytes()
            assert canonical(s.prefs)==old[2];s.restore(result['import_checkpoint']);assert s.head==old[0] and s.st.labels.tobytes()==old[1]
            (work/'partial-redo.log').write_bytes(raw)
            return dict(saved_pointer=2,retained_redo_transactions=2,original_progress_recoverable=True)
        check('Macro markers and saved Ptr preserve active state, redo tail and previous progress',partial_redo)
        def export_roundtrip():
            s.reset();p=s.preview([dict(kind='word',moves=[1,2,-4]),dict(kind='star',orbit=33,node=0,sign=1)]);s.commit(p['token']);expected=s.st.labels.tobytes()
            proof=s.export();assert verify_session(model,proof,expand=True)['passed'];raw=s.export_log('mpult',profile,12000)
            plan=import_native(model,b64(raw),profile);assert plan['state'].labels.tobytes()==expected
            saved=s.save_log('mpult',profile,12000);assert Path(saved['path']).read_bytes()==raw
            s.import_log(b64(raw),'mpult',profile);assert s.st.labels.tobytes()==expected and verify_session(model,s.export(),expand=True)['passed']
            (work/'backend-export.log').write_bytes(raw)
            return dict(native_moves=plan['native_log']['sequence_entries'],full_witness_expanded=True)
        check('Studio primitive/star witness exports native v1 and reimports exact full labels',export_roundtrip)
        def corruption():
            s.preview([dict(kind='word',moves=[2])]);raw=serialize(colors,tokens,timer=12345);text=raw.decode()
            cases=[text.replace('MPUltimate v1','MPUltimate v0',1),text.replace('600-cell-Full','foreign',1),text.replace('Cuts 0.968 -0.968','Cuts 0.9 -0.9',1),text.replace('2 259800','2 259799',1),text.replace('#timer 12345','#timer 12346',1),text.replace('0:0:1:1','0:0:1:2',1),text.replace('0:0:1:1','300:0:1:1',1),text.replace('0:0:1:1','0:0:32768:1',1),text[:-3],text+'garbage']
            for bad in cases:reject(bad.encode())
            wrong=colors.copy();wrong[0]=(int(wrong[0])+1)%600;reject(serialize(wrong,tokens,timer=12345))
            reject(text.replace('Cuts 0.968 -0.968','Cuts 0.9680000000000000000000000000000000000001 -0.968',1).encode())
            reject(text.replace('Puzzle 600-cell-Full','Puzzle 600.0-cell-Full',1).encode())
            reject(text.replace('2 259800','2.0 259800',1).encode())
            reject(serialize(colors,['m[',*tokens],timer=12345));reject(raw,None)
            return dict(corrupt_cases=len(cases)+6,includes_valid_crc_wrong_colors=True,exact_decimal_comparison=True,journal_unchanged=True)
        check('Wrong model, colors, CRC, masks, dimensions, markers and absent bridge reject transactionally',corruption)
        def empty():
            previous=s.head;previous_labels=s.st.labels.tobytes();raw=serialize(native_colors(model.ids,mapping,inverse),[]);result=s.import_log(b64(raw),'mpult',profile)
            assert result['solved'] and s.head==0 and result['native_log']['sequence_entries']==0
            assert s.event(previous) is not None;s.restore(result['import_checkpoint']);assert s.head==previous and s.st.labels.tobytes()==previous_labels
        check('Empty original-format log selects solved without deleting earlier branches',empty)
    finally:s.close()
    s=Session(model,work/'data')
    try:check('Native imported journal remains independently proof-verifiable after reopen',lambda:verify_session(model,s.export(),expand=True))
    finally:s.close()
    after={name:hashlib.sha256((ROOT/name).read_bytes()).hexdigest() for name in source}
    if after!=source:failures.append(dict(name='source-freeze',error='Production sources changed during regression'))
    result=dict(passed=not failures,scope='Actual Windows Python with a retained actual full native bridge profile; codec-generated input, separate original-MPUlt Save/Load required',profile_sha256=hashlib.sha256(args.profile.read_bytes()).hexdigest(),source_sha256=source,source_after=after,checks=checks,failures=failures)
    (work/'mpult-log-report.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(dict(passed=result['passed'],checks=len(checks),failures=len(failures))))
    return 0 if result['passed'] else 1
if __name__=='__main__':raise SystemExit(main())
