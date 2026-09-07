"""Atomic reset/import recovery-camera checks, real SQLite and authenticated HTTP."""
from pathlib import Path
import argparse,base64,copy,hashlib,json,math,sqlite3,sys,time,traceback
from urllib.error import HTTPError
from urllib.request import Request,ProxyHandler,build_opener
import numpy as np
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT))
from core import Model,canonical
from session import Session
from log_io import NATIVE_CAMERA_RADIUS,NATIVE_MAX_ANGLE,encode_log
from engine_process import EngineProcess

def camera(cell=42):
    return dict(format='C600-native-camera-v1',matrix=np.eye(4).ravel().tolist(),radius=NATIVE_CAMERA_RADIUS*.75,angle=NATIVE_MAX_ANGLE,cell=cell,face_shrink=.5,sticker_shrink=.8)

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--work-dir',type=Path,required=True);ap.add_argument('--native-profile',type=Path,required=True);ap.add_argument('--original-log',type=Path,required=True);args=ap.parse_args()
    work=args.work_dir;work.mkdir(parents=True,exist_ok=True);assert not (work/'direct/session.sqlite3').exists() and not (work/'http/session.sqlite3').exists()
    names=('session.py','server.py','log_io.py','mpult_log.py');source={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in names}
    m=Model();s=Session(m,work/'direct');checks=[];failures=[]
    def check(name,fn):
        start=time.perf_counter()
        try:detail=fn() or {};checks.append(dict(name=name,passed=True,seconds=time.perf_counter()-start,**detail));print('PASS',name,flush=True)
        except Exception as exc:failures.append(dict(name=name,error=str(exc),traceback=traceback.format_exc()));print('FAIL',name,str(exc),flush=True)
    def commit():p=s.preview([dict(kind='word',moves=[2,4,6])]);s.commit(p['token'])
    def snapshot():return s.head,s.st.labels.tobytes(),canonical(s.prefs),s.rev,list(s.db.iterdump()),None if s.pending is None else s.pending['token']
    def reject(fn,exceptions=(ValueError,)):
        before=snapshot()
        try:fn()
        except exceptions:pass
        else:raise AssertionError('Invalid operation accepted')
        assert snapshot()==before
    def stored(name):return json.loads(s.db.execute('SELECT prefs FROM snapshots WHERE name=?',(name,)).fetchone()[0])
    try:
        commit();record=s.export();c=camera();old=camera(1);s.save_prefs(dict(camera=old,orbit=2,pin_safety=False))
        def reset_snapshot():
            before_labels=s.st.labels.tobytes();before_head=s.head;r=s.reset(c)
            assert r['solved'] and s.prefs['camera']==c and stored(r['reset_checkpoint'])['camera']==c and s.prefs['orbit']==2
            s.save_prefs({'camera':old});s.restore(r['reset_checkpoint']);assert s.head==before_head and s.st.labels.tobytes()==before_labels and s.prefs['camera']==c
            return dict(single_precision_pi_accepted=True,labelled_slots=m.n)
        check('Reset records the current UI camera in both live preferences and exact recovery checkpoint',reset_snapshot)
        def imports():
            profile=json.loads(args.native_profile.read_text(encoding='utf-8-sig'));raw=args.original_log.read_bytes();bad_file_prefs=dict(record,prefs={'camera':camera(599),'orbit':34})
            for kind in ('record','c600','mpult'):
                requested=camera(20+len(kind));before=s.st.labels.tobytes()
                if kind=='record':result=s.import_record(bad_file_prefs,requested)
                elif kind=='c600':result=s.import_log(base64.b64encode(encode_log(bad_file_prefs)).decode(),'c600',camera=requested)
                else:result=s.import_log(base64.b64encode(raw).decode(),'mpult',profile,requested)
                assert s.prefs['camera']==requested and stored(result['import_checkpoint'])['camera']==requested and s.prefs['orbit']==2
                s.save_prefs({'camera':old});s.restore(result['import_checkpoint']);assert s.st.labels.tobytes()==before and s.prefs['camera']==requested
            return dict(paths=['record','c600','mpult'],file_preferences_ignored=True)
        check('All import paths use explicit current UI camera and retain it in the prior-state checkpoint',imports)
        def bad_camera():
            cases=[None,[],{},dict(c,format='other'),dict(c,matrix=[0.]*16),dict(c,matrix=[float('nan')]+c['matrix'][1:]),dict(c,matrix=[10**500]+c['matrix'][1:]),dict(c,radius=True),dict(c,radius=NATIVE_CAMERA_RADIUS*1.011),dict(c,radius=-NATIVE_CAMERA_RADIUS*.911),dict(c,angle=0),dict(c,angle=NATIVE_MAX_ANGLE+1e-8),dict(c,face_shrink=-.1),dict(c,sticker_shrink=1.1),dict(c,cell=True),dict(c,cell=600),dict(c,unexpected=1)]
            s.preview([dict(kind='word',moves=[10])])
            for bad in cases:reject(lambda:s.reset(bad));reject(lambda:s.import_record(record,bad))
            bad_record=copy.deepcopy(record);bad_record['final_state']='0'*64;reject(lambda:s.import_record(bad_record,camera(77)))
            reject(lambda:s.import_log('%%%','c600',camera=camera(77)))
            return dict(invalid_cameras=len(cases),bad_logs_keep_old_prefs=True)
        check('Invalid camera, extreme numbers and corrupt logs leave prefs, preview and journal untouched',bad_camera)
        def rollback():
            def deny(action,table,*_):return sqlite3.SQLITE_DENY if action==sqlite3.SQLITE_INSERT and table=='meta' else sqlite3.SQLITE_OK
            for action in (lambda:s.reset(c),lambda:s.import_record(record,c)):
                before=snapshot();s.db.set_authorizer(deny)
                try:
                    try:action()
                    except sqlite3.DatabaseError:pass
                    else:raise AssertionError('Injected SQL error did not fail')
                finally:s.db.set_authorizer(None)
                assert snapshot()==before
            class DuringTransaction:
                def is_set(self):return s.db.in_transaction
            m.cancel_event=DuringTransaction()
            try:reject(lambda:s.reset(c),(InterruptedError,));reject(lambda:s.import_record(record,c),(InterruptedError,))
            finally:m.cancel_event=None
            return dict(sql_rollback=True,cancel_after_snapshot_write=True)
        check('Database failure and cancellation inside write transaction roll back new camera and snapshots',rollback)
        result=s.import_record(record,camera(123));expected=canonical(s.prefs);s.close();s=Session(m,work/'direct')
        check('Committed recovery camera survives Session reopen',lambda: {} if canonical(s.prefs)==expected and s.prefs['camera']['cell']==123 else (_ for _ in ()).throw(AssertionError('Camera reopen mismatch')))
    finally:s.close()
    def http():
        with EngineProcess(ROOT,work/'http',work/'launch.json',work/'engine.log',timeout=240) as engine:
            def api(path,body=None):
                req=Request(engine.info['base']+'/api/'+path,data=None if body is None else canonical(body).encode(),headers={'Content-Type':'application/json','X-C600-Token':engine.info['token']})
                with build_opener(ProxyHandler({})).open(req,timeout=180) as reply:data=json.load(reply)
                if 'job' in data:
                    until=time.monotonic()+180
                    while time.monotonic()<until:
                        result=api('job/'+data['job'])
                        if result.get('done'):
                            if 'error' in result:raise ValueError(result['error'])
                            return result['result']
                        time.sleep(.01)
                    raise TimeoutError('HTTP camera job timeout')
                return data
            imported=api('log/import',dict(format='c600',data_base64=base64.b64encode(encode_log(record)).decode(),camera=camera(90)));assert imported['prefs']['camera']==camera(90)
            reset=api('reset',dict(camera=camera(91)));assert reset['solved'] and reset['prefs']['camera']==camera(91)
            restored=api('restore',dict(name=reset['reset_checkpoint']));assert restored['prefs']['camera']==camera(91) and restored['state_hash']==record['final_state']
            for path,body in [('reset',dict(camera=None)),('reset',dict(camera=dict(camera(),radius=True))),('log/import',dict(format='c600',data_base64='%%%',camera=camera(99))),('import',dict(record=record,camera=dict(camera(),matrix=[])))]:
                before=api('status')
                try:api(path,body)
                except (ValueError,HTTPError) as error:
                    if isinstance(error,HTTPError):assert error.code==400
                else:raise AssertionError('Invalid HTTP camera accepted')
                assert api('status')==before
        return dict(explicit_camera_survives_http=True,invalid_requests=4,owned_engine_exited=True)
    check('Actual authenticated HTTP carries explicit camera and rejects invalid camera/log without mutation',http)
    after={n:hashlib.sha256((ROOT/n).read_bytes()).hexdigest() for n in names}
    if source!=after:failures.append(dict(name='source-freeze',error='Source changed during camera regression'))
    report=dict(passed=not failures,scope='Actual Windows Python/SQLite and authenticated HTTP; native view restoration requires separate real GUI regression',source_sha256=source,source_after=after,native_camera_radius=NATIVE_CAMERA_RADIUS,max_angle_single_pi=NATIVE_MAX_ANGLE,checks=checks,failures=failures)
    (work/'recovery-camera-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(dict(passed=report['passed'],checks=len(checks),failures=len(failures))))
    return 0 if report['passed'] else 1
if __name__=='__main__':raise SystemExit(main())
