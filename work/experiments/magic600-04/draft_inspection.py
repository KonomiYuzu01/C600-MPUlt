"""Detached inspection of explicit draft boundaries; never execution authority."""
from copy import deepcopy

import numpy as np

from core import PuzzleState, canonical, digest
from filter_projection import PieceFilterProjection

BOUNDARIES = ('actual', 'prepare', 'macro', 'cleanup')
POSITION_LIMIT = 256


def inspect_phase(workbench, boundary, macro_id=None):
    """Inspect a finite user-supplied prefix under the authoritative Session lock.

    Effects may populate existing immutable model/effect caches. Session, work
    preferences, review, pending transaction and epoch are never changed.
    Returned dictionaries/lists are detached from those objects and caches.
    """
    if not isinstance(boundary, str) or boundary not in BOUNDARIES:
        raise ValueError('Boundary must be actual, prepare, macro or cleanup')
    wb = workbench
    with wb.lock:
        if macro_id is not None and (not isinstance(macro_id, str) or macro_id not in wb.library):
            raise ValueError('Choose an existing fixed macro entry')
        operation_state = wb.operation_state()
        executed = operation_state == 'executed'
        if executed and boundary != 'actual':
            raise ValueError('These steps were executed. Choose New operation or Reuse steps before inspecting a draft phase.')
        w = deepcopy({key: wb.w[key] for key in (
            'current', 'target', 'next', 'roles', 'reference', 'block', 'draft',
            'inspected', 'inspected_position')})
        macro = deepcopy(wb.library[macro_id]) if macro_id is not None else None
        source_hash, source_guard = wb.s.st.hash, wb.guard()
        source_next = deepcopy(w['next'])
        source_inspected = dict(identity=w['inspected'], position=w['inspected_position'])
        # The held Session lock permits read-only reuse. Forecast writes use a
        # copy, and returned piece dictionaries/lists are detached.
        actual = wb.s.st
        piece_filter = PieceFilterProjection(wb)
        labels = actual.labels
        phase_recipe = [deepcopy(step) for phase in BOUNDARIES[1:BOUNDARIES.index(boundary) + 1]
                        for step in w['draft'][phase]]

        def project(recipe):
            if not recipe:
                return actual
            projected = labels.copy()
            src, dst, _ = wb.effect(recipe)
            projected[dst] = projected[src]
            return PuzzleState(wb.m, projected, trusted=True)

        state = actual if boundary == 'actual' else project(phase_recipe)

        def piece(identity=None, position=None, at=state):
            return piece_filter.record(wb.piece(identity=identity, position=position, state=at), at)

        def meets(member, at):
            from position_requirements import prepare, holds
            return holds(wb.m, at.labels, prepare(wb.m, member))

        blocks = [dict(piece(position=member['position']), member=deepcopy(member),
                       before_met=meets(member, actual), requirement_met=meets(member, state))
                  for member in w['block']['members']]
        answer = dict(boundary=boundary, model=wb.m.model_id, source_hash=source_hash,
            review_context_id=wb.review_context()['id'],
            source_guard=source_guard, source_next=source_next, source_inspected=source_inspected,
            operation_state=operation_state, draft_projection_available=not executed,
            piece_filter=piece_filter.metadata(state), actual_piece_filter=piece_filter.metadata(actual),
            macro_id=macro_id, phase_recipe=phase_recipe, state_hash=state.hash,
            current=piece(identity=w['current']),
            next=piece(identity=w['next']['identity']) if w['next'] else None,
            target=piece(position=w['target']), buffers=[piece(position=p) for p in w['roles']],
            block=blocks,
            inspected=(piece(position=w['inspected_position']) if w['inspected_position'] is not None
                       else piece(identity=w['inspected'])),
            reference=dict(supplied_word=deepcopy(w['reference']),
                status='canonical' if not w['reference'] else 'generic legal word',
                geometric_mapping='unsupported',
                note='Inspection applies only explicit phase steps. A legal conjugating word is not a certified geometric reference.'),
            protection=dict(status='not evaluated',
                note='Draft inspection is not a protection review or permission to execute.'),
            macro_entry=None,
            macro_entry_unavailable_reason=('These steps were executed. Choose New operation or Reuse steps before inspecting macro entry.'
                                            if executed else None))
        support_positions = []
        fixed_roles = []
        if macro is not None and not executed:
            src, _, facts = wb.effect(macro['recipe'])
            facts = deepcopy(facts)
            prepared = state if boundary == 'prepare' else project(w['draft']['prepare'])
            support_positions = list(map(int, np.unique(wb.m.sp[src])))
            entry = dict(id=macro['id'], version=macro['version'], name=macro['name'],
                         recipe=deepcopy(macro['recipe']), boundary='prepare',
                         state_hash=prepared.hash, facts=facts, roles=None,
                         piece_filter=piece_filter.metadata(prepared),
                         frame_status='No retained star frame is declared for this recipe.')
            star = facts['star']
            if star is not None:
                # These are the same ordered frames checked by Model.star_net;
                # no frame or required occupant is inferred from the work target.
                frames = dict(a=wb.m.atlas[star['orbit']]['frames'][0],
                              b=wb.m.atlas[star['orbit']]['frames'][1], target=star['frame'])
                roles = {}
                for name in ('a', 'b', 'target'):
                    fixed_roles.append(star[name])
                    order = list(map(int, frames[name]))
                    values = list(map(int, prepared.labels[order]))
                    roles[name] = dict(piece(position=star[name], at=prepared),
                        frame_order=order, actual_labels=values,
                        correspondence=[dict(slot=s, label=l) for s, l in zip(order, values)])
                entry.update(roles=roles, frame_status='Verified retained-star ordered slot frames; actual occupants are observations, not required inputs.')
            answer['macro_entry'] = entry
        # Work objects are included first; larger macro support remains explicitly
        # truncated here while its complete support totals remain in facts.
        ordered, seen = [], set()
        actual_bookmarks = [piece(identity=w['current'], at=actual),
                            piece(identity=w['next']['identity'], at=actual) if w['next'] else None,
                            piece(position=w['inspected_position'], at=actual) if w['inspected_position'] is not None
                            else piece(identity=w['inspected'], at=actual)]
        for record in actual_bookmarks + [answer['current'], answer['next'], answer['target'], answer['inspected']] + answer['buffers'] + blocks:
            if record is not None and record['position'] not in seen:
                ordered.append(record['position']); seen.add(record['position'])
        for p in fixed_roles + support_positions:
            if p not in seen:
                ordered.append(p); seen.add(p)
        answer.update(positions=[piece(position=p) for p in ordered[:POSITION_LIMIT]],
            actual_positions=[piece(position=p, at=actual) for p in ordered[:POSITION_LIMIT]],
            positions_total=len(ordered), positions_returned=min(len(ordered), POSITION_LIMIT),
            positions_truncated=len(ordered) > POSITION_LIMIT, positions_limit=POSITION_LIMIT,
            positions_scope=('Actual work positions only; retained executed steps are not projected.' if executed else
                             'Fixed work positions and complete selected-macro support, truncated only in this object list.'),
            macro_support_positions_total=len(support_positions), macro_support_evaluated=macro is not None and not executed)
        answer['source_signature'] = digest(canonical(dict(model=wb.m.model_id,
            hash=source_hash, guard=source_guard, next=source_next, inspected=source_inspected,
            boundary=boundary, macro=macro, operation_state=operation_state,
            filter_context=piece_filter.context_hash)).encode())
        return answer
