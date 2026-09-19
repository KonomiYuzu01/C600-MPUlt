"""Explicit retained endgame families in the fixed transported frame.

No Session is accepted and no target, setup or construction parameter is chosen
from puzzle state. Callers review the complete recipe under their real policy.
"""
from copy import deepcopy

import numpy as np

from core import canonical, commp, digest, invrecipe, ip
from orbit_invariants import finite_quotient
from transported_frames import FRAME_VERSION
from endgame_invariants import load_audited_invariants

VERSION = 'manual-retained-endgames-v1'
SCOPE = 'Complete full-model net effect; no protection or execution permission'
FAMILIES = ('placement-star', 'transfer', 'buffer-a', 'final-b')


class EndgameLibrary:
    def __init__(self, model, frames):
        if frames.m is not model:
            raise ValueError('Endgame references must share the authoritative model')
        self.m, self.frames = model, frames

    def _choices(self, orbit, position):
        self.m.check_cancel()
        if type(orbit) is not int or not 0 <= orbit < 35:
            raise ValueError('Choose a canonical moving orbit')
        if (type(position) is not int or not 0 <= position < self.m.np
                or self.m.oid[position] != orbit or position in self.m.trees[orbit]['buffers']):
            raise ValueError('Choose an explicit nonbuffer position in this orbit')
        certificate = self.frames.ensure(orbit)
        frame = self.frames.frame(position); inverse = {slot:index for index,slot in enumerate(frame)}
        choices = {}
        for node in self.m.bypos[orbit][position]:
            self.m.check_cancel()
            actual = self.m.trees[orbit]['frames'][node]
            element = tuple(inverse[slot] for slot in actual)
            if element in choices:
                raise ValueError('Multiple retained words have this frame; an explicit word choice is required')
            choices[element] = node
        if set(choices) != set(self.frames.finite_group(orbit)):
            raise ValueError('This position does not cover the certified orientation group')
        return certificate, frame, choices

    @staticmethod
    def _element(value, choices):
        if (not isinstance(value, (list,tuple)) or any(type(v) is not int for v in value)
                or tuple(value) not in choices):
            raise ValueError('Choose an exact element of this fixed-frame orientation group')
        return tuple(value)

    def options(self, orbit, position):
        certificate, frame, choices = self._choices(orbit, position)
        return dict(version=VERSION, model=self.m.model_id, frame_version=FRAME_VERSION,
            orbit=orbit, position=position, frame=frame, certificate=certificate,
            group=self.m.census['orbits'][orbit]['orientation_group'],
            choices=[dict(element=list(element), node=choices[element]) for element in sorted(choices)],
            scope='Explicit mathematical parameters only; no suggested correction or execution permission')

    def compose(self, orbit, family, x, q, y=None, r=None):
        """Construct and verify only the user's supplied finite parameters."""
        if family not in FAMILIES:
            raise ValueError('Choose a supported explicit endgame family')
        certificate, fx, gx = self._choices(orbit, x)
        q = self._element(q, gx); identity = tuple(range(len(fx)))
        a,b = self.m.trees[orbit]['buffers']; fa,fb = self.frames.frame(a),self.frames.frame(b)
        if family != 'final-b' and (y is not None or r is not None):
            raise ValueError('This family does not use a second auxiliary or element')
        def star(node, sign=1): return dict(kind='star',orbit=orbit,node=node,sign=sign)
        def transfer(choices, element): return [star(choices[element]),star(choices[identity],-1)]
        expected = {}
        def mapping(source, destination):
            expected.update((int(s),int(d)) for s,d in zip(source,destination) if s != d)
        parameters = dict(x=x,q=list(q))
        if family == 'placement-star':
            recipe=[star(gx[q])]
            xq=[fx[i] for i in q]
            mapping(fa,fb); mapping(fb,xq); mapping(xq,fa)
        elif family == 'transfer':
            recipe=transfer(gx,q)
            mapping(fx,[fx[i] for i in ip(q)]); mapping(fb,[fb[i] for i in q])
        elif family == 'buffer-a':
            recipe=[star(gx[identity]),star(gx[q]),star(gx[identity])]
            mapping(fa,[fa[i] for i in q]); mapping(fb,[fb[i] for i in ip(q)])
        else:
            if x == y: raise ValueError('The final-buffer commutator requires two distinct nonbuffer positions')
            _,fy,gy=self._choices(orbit,y); r=self._element(r,gy)
            first,second=transfer(gx,q),transfer(gy,r)
            recipe=first+second+invrecipe(first)+invrecipe(second)
            result=commp(q,r); mapping(fb,[fb[i] for i in result])
            parameters.update(y=y,r=list(r),result=list(result))
        source,destination,cost,normalized=self.m.net(recipe)
        target=self.m.so[source] == orbit
        actual=dict(zip(map(int,source[target]),map(int,destination[target])))
        if actual != expected:
            raise ValueError('The complete retained recipe disagrees with the stated fixed-frame family')
        support=self.m.support(source)
        self.m.check_cancel()
        return deepcopy(dict(version=VERSION,model=self.m.model_id,frame_version=FRAME_VERSION,
            orbit=orbit,family=family,parameters=parameters,roles=[a,b],certificate=certificate,
            recipe=normalized,cost=cost,action_hash=digest(source.astype('<i4').tobytes()+destination.astype('<i4').tobytes()),
            support=support,collateral=[row for row in support if row['orbit'] != orbit],
            target_slot_pairs=[[s,expected[s]] for s in sorted(expected)],
            target_positions_preserved=bool(np.all(self.m.sp[source[target]] == self.m.sp[destination[target]])),
            target_identity=not expected,scope=SCOPE))

    def commutator_table(self, orbit, x, y):
        """Offline/manual lookup table, never matched to a current residual."""
        _,_,gx=self._choices(orbit,x); _,_,gy=self._choices(orbit,y)
        if x == y: raise ValueError('Choose distinct explicit auxiliaries')
        group=tuple(sorted(gx)); table=finite_quotient(group)
        witnesses={}
        for q in group:
            self.m.check_cancel()
            for r in sorted(gy):
                witnesses.setdefault(commp(q,r),(q,r))
        if set(witnesses) != table['derived']:
            raise ValueError('This finite group does not have complete single-commutator coverage')
        return [self.compose(orbit,'final-b',x,witnesses[result][0],y,witnesses[result][1])
                for result in sorted(witnesses)]

    def certify_catalogue(self):
        """Explicit offline coverage run; no result is published on cancellation."""
        audit=load_audited_invariants(self.m,self.frames)
        rows=[]
        for orbit in range(35):
            self.m.check_cancel()
            tree=self.m.trees[orbit]; x=tree['third']
            y=next(int(p) for p in self.m.pids[self.m.oid == orbit] if p not in (*tree['buffers'],x))
            options=self.options(orbit,x); group=[tuple(row['element']) for row in options['choices']]
            identity=tuple(range(len(options['frame']))); nonidentity=[q for q in group if q != identity]
            final=self.commutator_table(orbit,x,y)
            na={}
            if not nonidentity:
                na.update(transfer='Trivial group: no independent orientation residual exists.',
                          buffer_a='Trivial group: no independent orientation residual exists.')
            if len(final)==1:
                na['final_b']='Abelian group: no nonidentity final-buffer-only orientation satisfies the certified quotient invariant.'
            rows.append(dict(orbit=orbit,group=options['group'],reference=options['certificate'],
                invariant_certificate=audit.certificate(orbit),
                parameters=dict(x=x,y=y),placement=[self.compose(orbit,'placement-star',x,identity)],
                transfer=[self.compose(orbit,'transfer',x,q) for q in nonidentity],
                buffer_a=[self.compose(orbit,'buffer-a',x,q) for q in nonidentity],final_b=final,
                not_applicable=na))
        self.m.check_cancel()
        return dict(version=VERSION,status='Verified',model=self.m.model_id,frame_version=FRAME_VERSION,
            scope='Finite known manual families at explicit reference auxiliaries; not a full solve, protection or arbitrary reachability proof',
            orbits=rows)
