"""Actual Windows loopback workflow regression; isolated journal, no GPU claim.

Run with --work-dir PATH to retain source hashes, HTTP engine logs and report.
The model below independently checks API data against all 259800 label bytes.
"""
from pathlib import Path
import argparse, gzip, hashlib, json, platform, sys, tempfile, time, traceback
from urllib.error import HTTPError
from urllib.request import ProxyHandler, Request, build_opener
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT))
from core import Model, PuzzleState, invword, state_hash
from engine_process import EngineProcess
from verify import verify_certificate, verify_session


def main():
    ap=argparse.ArgumentParser();ap.add_argument('--work-dir',type=Path);args=ap.parse_args()
    work=args.work_dir or Path(tempfile.mkdtemp(prefix='c600-workflow-'))
    work.mkdir(parents=True,exist_ok=True)
    assert not (work/'data/session.sqlite3').exists(),'Use a fresh work directory'
    m=Model();checks=[];failures=[];started=time.time();engine=None;saved_macro={}
    def check(name,fn):
        begin=time.perf_counter()
        try:
            detail=fn() or {};checks.append(dict(name=name,passed=True,seconds=time.perf_counter()-begin,**detail));print('PASS',name,flush=True)
        except Exception as exc:
            failures.append(dict(name=name,error=str(exc),traceback=traceback.format_exc()));print('FAIL',name,repr(exc),flush=True)
    def api(path,body=None,raw=False):
        req=Request(engine.info['base']+'/api/'+path,data=None if body is None else json.dumps(body).encode('utf-8'),headers={'Content-Type':'application/json','X-C600-Token':engine.info['token']})
        try:
            with build_opener(ProxyHandler({})).open(req,timeout=120) as r:code,payload=r.status,r.read()
        except HTTPError as exc:code,payload=exc.code,exc.read()
        if raw:
            assert code==200,(code,payload[:200]);return payload
        data=json.loads(payload)
        if code!=200:raise ValueError(f'HTTP {code}: '+data.get('error',str(data)))
        if 'job' in data:
            until=time.monotonic()+120
            while time.monotonic()<until:
                reply=api('job/'+data['job'])
                if reply.get('done'):
                    if 'error' in reply:raise ValueError('Analysis job: '+reply['error'])
                    return reply['result']
                time.sleep(.01)
            raise TimeoutError('Analysis job did not finish')
        return data
    def labels():
        value=api('labels',raw=True);assert len(value)==259800*4
        return np.frombuffer(value,dtype='<i4').copy()
    def status():return api('status')
    def commit(recipe):
        p=api('preview',{'recipe':recipe});r=api('commit',{'token':p['token']});assert r['state_hash']==p['post_state'];return r
    def word(moves):return [{'kind':'word','moves':moves}]
    def reset():return api('restore',{'name':'Solved root'})
    def reject_without_mutation(path,body):
        before=status();token=before['pending']['token'] if before['pending'] else None
        try:api(path,body)
        except ValueError as exc:
            assert 'HTTP 500:' not in str(exc),'Malformed user input produced server error: '+str(exc)
        else:raise AssertionError(f'Invalid request accepted: {path} {body}')
        after=status();assert after['state_hash']==before['state_hash'] and after['head']==before['head']
        assert (after['pending']['token'] if after['pending'] else None)==token
    try:
        with EngineProcess(ROOT,work/'data',work/'launch.json',work/'engine.log',timeout=240) as engine:
            def macro_algebra():
                reset();a=[2,4];b=[6]
                sequences={'inverse':invword(a),'concat':a+b,'commutator':a+b+invword(a)+invword(b),'conjugate':a+b+invword(a)}
                for operation,moves in sequences.items():
                    before=status()['state_hash'];p=api('compose',{'a':word(a),'b':word(b),'operation':operation});cert=api('certificate')
                    assert verify_certificate(m,cert)['passed'];assert cert['expanded_lab_word']==moves
                    s,d=m.word_net(moves);expected=labels();expected[d]=expected[s]
                    assert state_hash(expected)==p['post_state'] and status()['state_hash']==before
                    assert p['primitive_count']==str(len(moves))
                api('save-macro',{'name':'Windows full effect 宏'});saved=status()['prefs']['macro_library']['Windows full effect 宏']
                assert saved['recipe']==p['recipe'] and saved['effect_hash']==p['source_to_destination_sha256'] and saved['support']==p['support']
                saved_macro.update(saved)
                assert len(p['support'])>1,'Witness should exercise full collateral across multiple orbits'
                api('prefs',{'protected':[p['support'][0]['orbit']]})
                reject_without_mutation('commit',{'token':p['token']})
                api('prefs',{'protected':[]})
                r=api('commit',{'token':p['token']});api('undo',{});assert status()['state_hash']==before
                replay=api('preview',{'recipe':saved['recipe']});assert replay['post_state']==r['state_hash']
                assert replay['source_to_destination_sha256']==saved['effect_hash'];api('cancel',{})
                (work/'macro-certificate.json').write_text(json.dumps(cert,ensure_ascii=False,indent=2),encoding='utf-8')
                return {'operations':list(sequences),'full_support_orbits':len(saved['support'])}
            check('Macro inverse, concatenation, commutator, conjugation and saved full-effect witness',macro_algebra)

            def buffer_integrity():
                reset();snap=PuzzleState(m,labels());rows=[]
                for o in range(35):
                    t=m.trees[o];target=int(t['third']);r=api('buffers',{'orbit':o,'target':target})
                    assert [r['A']['position'],r['B']['position']]==t['buffers']
                    assert r['A']==snap.piece(t['buffers'][0]) and r['B']==snap.piece(t['buffers'][1])
                    assert r['reachable']==r['expected']==len(t['positions'])
                    assert len(r['candidates'])==len(m.bypos[o][target])
                    for c in r['candidates']:
                        n=c['node'];assert c['slots']==t['frames'][n] and c['setup']==m.path(o,n)
                        assert c['length']==len(t['seed'])+2*len(t['relocation'])+2*t['depth'][n]
                    rows.append({'orbit':o,'candidate_frames':len(r['candidates'])})
                    automatic=api('buffers',{'orbit':o})
                    assert automatic['target']==target and automatic['target_selection']=='automatic-reference'
                    assert automatic['candidates']==r['candidates'] and automatic['target_piece']==snap.piece(target)
                commit([{'kind':'star','orbit':33,'node':0,'sign':1}]);after=PuzzleState(m,labels());r=api('buffers',{'orbit':33,'target':int(m.trees[33]['third'])})
                assert r['A']==after.piece(m.trees[33]['buffers'][0]) and r['B']==after.piece(m.trees[33]['buffers'][1])
                assert r['A']['position']==m.trees[33]['buffers'][0] and r['A']['piece']!=r['A']['position']
                automatic=api('buffers',{'orbit':33});assert automatic['target_selection']=='automatic-unfinished'
                assert not after.correct[automatic['target']] and automatic['candidates']
                return {'orbits':rows,'fixed_locations_track_changed_occupants':True}
            check('All 35 buffer analyses expose exact fixed locations, live occupants and legal setup frames',buffer_integrity)

            def insertion():
                reset();o=33;nodes=[0,len(m.trees[o]['positions'])//3,len(m.trees[o]['positions'])//2]
                commit([{'kind':'star','orbit':o,'node':n,'sign':1} for n in nodes])
                before=labels();st=PuzzleState(m,before);buffers=set(m.trees[o]['buffers'])
                target=next(int(p) for p in np.flatnonzero((m.oid==o)&~st.position_correct) if p not in buffers)
                p=api('suggest',{'orbit':o,'batch':1,'target':target});assert not p['complete'] and p['plan'][0]['target']==target
                assert status()['state_hash']==state_hash(before),'Insertion preview mutated committed state'
                assert p['assistance']=='assisted-target-selection';assert p['support']==m.support(m.net(p['recipe'])[0])
                api('commit',{'token':p['token']});after=PuzzleState(m,labels());assert after.position_correct[target]
                protected_finished=(m.oid==o)&st.position_correct
                for b in buffers:protected_finished[b]=False
                assert np.all(after.position_correct[protected_finished]),'Insertion disturbed a finished nonbuffer position'
                api('undo',{});assert np.array_equal(labels(),before);api('redo',{});hash_after=status()['state_hash']
                p=api('suggest',{'orbit':o,'batch':5})
                assert 1<=len(p['plan'])<=5
                api('commit',{'token':p['token']});assert np.all(PuzzleState(m,labels()).correct[m.oid==o])
                assert api('suggest',{'orbit':o,'batch':1})['complete'] is True
                api('undo',{});assert status()['state_hash']==hash_after
                return {'explicit_target':target,'batch_insertions':len(p['plan']),'full_sticker_labels':m.n}
            check('Explicit-target and batched insertions preserve preview, solve orbit, and undo/redo exactly',insertion)
            def complete_clears_preview():
                old_head=status()['head'];reset();before=status()['state_hash']
                prior=api('preview',{'recipe':word([2])});assert status()['pending']['token']==prior['token']
                result=api('suggest',{'orbit':33,'batch':1})
                assert result['complete'] is True and 'no insertion preview is pending' in result['note']
                assert status()['pending'] is None and status()['state_hash']==before
                reject_without_mutation('commit',{'token':prior['token']})
                api('checkout',{'head':old_head})
            check('Solved-orbit insertion clears an unrelated older preview and explains that nothing is pending',complete_clears_preview)

            def reports():
                api('scramble',{'count':7,'seed':123,'apply':True})
                st=PuzzleState(m,labels());r=status();assert r['progress']==st.progress()
                for row in r['progress']:assert row['solved']+row['position_wrong']+row['orientation_wrong']==row['pieces']
                assert r['moving_solved']==sum(x['solved'] for x in r['progress']) and r['moving_total']==sum(x['pieces'] for x in r['progress'])
                exported=json.loads(gzip.decompress(api('export',raw=True)));verified=verify_session(m,exported)
                assert verified['passed'] and verified['final_state']==r['state_hash']
                stats=api('stats');events=exported['events'];total=sum(int(e['primitive_count']) for e in events)
                assert int(stats['scramble_primitives'])+int(stats['solution_primitives'])==total==int(r['primitives'])
                assert stats['scramble_primitives']=='7' and int(stats['solution_primitives'])>0
                assert stats['operations']==len(events)==r['transactions'] and stats['stars']==r['stars']==sum(e['stars'] for e in events)
                assert stats['assisted_transactions']==sum(e['assistance'].startswith('assisted') for e in events)
                first=api('history');ids=[x['id'] for x in first['rows']];assert ids==sorted(ids,reverse=True)
                cutoff=ids[len(ids)//2];older=api('history?before='+str(cutoff));assert all(x['id']<cutoff for x in older['rows'])
                for row in first['rows']:
                    if row['id'] in [e['id'] for e in events]:assert row['post']==next(e['post'] for e in events if e['id']==row['id'])
                (work/'session-report.json').write_text(json.dumps({'stats':stats,'status':r,'history':first},ensure_ascii=False,indent=2),encoding='utf-8')
                return {'progress_orbits':35,'verified_transactions':len(events),'primitive_count':str(total)}
            check('Progress, assistance/session accounting, branch history and exported replay match full labels',reports)

            def checkpoint():
                pref={'orbit':33,'keybinds':{'KeyN':'suggest','F9':'checkpoint'},'camera':{'test':'checkpoint-camera'},'bookmarks':{'work':13},'macro_library':{'Windows full effect 宏':saved_macro}}
                api('prefs',pref);before=status();saved=labels();api('checkpoint',{'name':'Windows 完整检查点'})
                old_head=before['head'];commit(word([12]));other_head=status()['head'];api('prefs',{'orbit':2,'keybinds':{}})
                restored=api('restore',{'name':'Windows 完整检查点'});assert restored['head']==old_head and np.array_equal(labels(),saved)
                for k,v in pref.items():assert restored['prefs'][k]==v
                assert any(x['id']==other_head for x in api('history')['rows']),'Restore deleted the newer branch'
                api('checkout',{'head':other_head});assert status()['head']==other_head;api('checkout',{'head':old_head});assert np.array_equal(labels(),saved)
                api('timer',{'action':'start'});time.sleep(.08);paused=api('timer',{'action':'pause'});assert paused['seconds']>=.07 and not paused['running']
                time.sleep(.03);assert api('stats')['timer']['seconds']==paused['seconds']
                backup=api('backup',{});assert Path(backup['path']).parent.resolve()==(work/'data').resolve() and backup['bytes']>0
                (work/'expected-reopen.json').write_text(json.dumps({'state_hash':state_hash(saved),'head':old_head,'prefs':pref,'timer_seconds':paused['seconds']},indent=2),encoding='utf-8')
                return {'checkpoint':'Windows 完整检查点','retained_branch':other_head,'timer_seconds':paused['seconds']}
            check('Checkpoint restores labels and preferences, preserves branches, pauses timer and backs up journal',checkpoint)

            check('Invalid negative buffer orbit is rejected without replacing preview',lambda:reject_without_mutation('buffers',{'orbit':-1}))
            check('Invalid out-of-range buffer orbit is a user error rather than HTTP 500',lambda:reject_without_mutation('buffers',{'orbit':35}))
            check('Fractional insertion batch is rejected before int coercion',lambda:reject_without_mutation('suggest',{'orbit':33,'batch':1.5}))
            check('Boolean insertion batch is rejected before int coercion',lambda:reject_without_mutation('suggest',{'orbit':33,'batch':True}))
            def invalid_target():
                reset();commit([{'kind':'star','orbit':33,'node':0,'sign':1}])
                # A valid earlier preview must survive every rejected request.
                api('preview',{'recipe':word([2])})
                invalid=[-1,m.np,True,1.5,'1',m.trees[33]['buffers'][0],int(np.flatnonzero(m.oid==0)[0])]
                st=PuzzleState(m,labels());invalid.append(next(int(p) for p in np.flatnonzero((m.oid==33)&st.correct) if p not in m.trees[33]['buffers']))
                for target in invalid:reject_without_mutation('suggest',{'orbit':33,'batch':1,'target':target})
                return {'invalid_targets_checked':len(invalid)}
            check('Explicit invalid insertion destination is rejected rather than silently substituted',invalid_target)
            def utilities():
                before=labels();st=PuzzleState(m,before);choices=np.flatnonzero((m.oid==33)&~st.correct);pos=int(choices[0])
                assert api('piece',{'position':pos})==st.piece(pos)
                assert api('track-piece',{'piece':pos})==st.piece(int(st.where[pos]))
                assert api('find-required',{'position':pos})==st.piece(int(st.where[pos]))
                selected=api('select',{'position':pos});assert selected==st.piece(pos) and status()['prefs']['selected']==selected['piece']
                assert api('next-piece',{'orbit':33,'position':pos,'direction':1})==st.piece(int(choices[1%len(choices)]))
                assert api('next-piece',{'orbit':33,'position':pos,'direction':-1})==st.piece(int(choices[-1]))
                assert np.array_equal(labels(),before)
                return {'inspected_position':pos,'full_labels_unchanged':True}
            check('Piece inspection, identity tracking, required-piece lookup and wrapped next/previous navigation are exact',utilities)
            def utility_boundaries():
                for path,key in [('piece','position'),('select','position'),('find-required','position'),('track-piece','piece')]:
                    for value in (-1,m.np,True,1.5,'2'):reject_without_mutation(path,{key:value})
                for body in ({'orbit':33,'position':0,'direction':True},{'orbit':33,'position':-.5,'direction':1},{'orbit':True,'position':0,'direction':1}):reject_without_mutation('next-piece',body)
                for target in (-1,m.trees[33]['buffers'][0],True):reject_without_mutation('buffers',{'orbit':33,'target':target})
                return {'rejected_inputs':26}
            check('Piece inspection, tracking, selection and buffer utility inputs reject malformed positions without mutation',utility_boundaries)
            expected=json.loads((work/'expected-reopen.json').read_text(encoding='utf-8'))
            api('restore',{'name':'Windows 完整检查点'})
        assert engine.process.returncode==0 and not engine.cleanup_result['forced']
        with EngineProcess(ROOT,work/'data',work/'reopen-launch.json',work/'reopen-engine.log',timeout=240) as engine:
            def reopen():
                r=status();assert r['head']==expected['head'] and r['state_hash']==expected['state_hash']==state_hash(labels())
                for k,v in expected['prefs'].items():assert r['prefs'][k]==v
                stats=api('stats');assert stats['timer']['seconds']==expected['timer_seconds'] and not stats['timer']['running']
                assert 'Windows full effect 宏' in r['prefs'].get('macro_library',{})
                return {'labelled_slots':m.n,'sha256':r['state_hash'],'timer_running':False}
            check('Restart recovers full labels, checkpoint preferences, saved macro and paused timer',reopen)
        assert engine.process.returncode==0 and not engine.cleanup_result['forced']
    except Exception as exc:failures.append({'name':'harness','error':str(exc),'traceback':traceback.format_exc()})
    report={'passed':not failures,'scope':'Actual Windows CPU/model and authenticated loopback HTTP workflows; no native GUI/GPU claim','platform':platform.platform(),'python':sys.version,'seconds':time.time()-started,'checks':checks,'failures':failures,'source_sha256':{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in ('core.py','session.py','enhanced.py','server.py','tests/test_workflow_windows.py')}}
    (work/'workflow-windows.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'passed':report['passed'],'checks':len(checks),'failures':len(failures),'report':str(work/'workflow-windows.json')},ensure_ascii=False),flush=True)
    return 0 if report['passed'] else 1


if __name__=='__main__':raise SystemExit(main())
