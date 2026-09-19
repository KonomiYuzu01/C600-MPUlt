"""Read-only, paged operation effects and authoritative Home-ownership relations."""
from copy import deepcopy

import numpy as np

from core import PuzzleState, canonical, digest
from filter_projection import PieceFilterProjection
from transported_frames import FRAME_VERSION

PAGE_SIZE = 32
PHASES = ('prepare', 'macro', 'cleanup')
SCOPES = ('selected-macro', 'macro-steps', 'complete')
MODES = ('Current', 'Operation', 'After')


def _integer(value, maximum, name):
    if type(value) is not int or not 0 <= value < maximum:
        raise ValueError(name + ' must be a canonical integer in 0..' + str(maximum - 1))
    return value


def _recipe(model, value):
    if not isinstance(value, list):
        raise ValueError('Inspection recipe must be an explicit list')
    return model.normalize(value)[0] if value else []


def validate_cycle_display(model, value):
    """Validate display metadata before any primary command can mutate state."""
    if value is None:
        return None
    allowed = {'mode', 'scope', 'orbit', 'position', 'cycle_after', 'residual_after', 'edge_offset'}
    if (not isinstance(value, dict) or set(value) - allowed or value.get('scope') not in SCOPES
            or value.get('mode', 'Operation') not in MODES):
        raise ValueError('Cycle display needs a supported scope and only display-selection fields')
    if value.get('orbit') is not None:
        _integer(value['orbit'], 35, 'Orbit')
    for key in ('position', 'cycle_after', 'residual_after'):
        if value.get(key) is not None:
            _integer(value[key], model.np, key)
    if 'edge_offset' in value:
        _integer(value['edge_offset'], model.np, 'Edge offset')
    return deepcopy(value)


def inspect_cycle_display(wb, metadata, macro_id=None):
    """Pair a display request with the state after an already-authorized action."""
    with wb.lock:
        body = deepcopy(metadata)
        body['orbit'] = wb.w['orbit'] if body.get('orbit') is None else body['orbit']
        if body.get('mode', 'Operation') in ('Current', 'After'):
            body.update(model=wb.m.model_id, guard=wb.guard())
            return inspect_cycles(wb, body, select_first=True), None
        scope = body['scope']
        if scope == 'selected-macro':
            if macro_id is None:
                return None, 'Select a macro to inspect its cycles'
            record = wb.bound_macro(macro_id)
            body['macro'] = {key: deepcopy(record[key]) for key in ('id', 'version', 'recipe')}
            chosen = record['recipe']
        else:
            chosen = (wb.w['draft']['macro'] if scope == 'macro-steps' else
                      [step for phase in PHASES for step in wb.w['draft'][phase]])
        body.update(model=wb.m.model_id, guard=wb.guard(), recipe=_recipe(wb.m, chosen))
        return inspect_cycles(wb, body, select_first=True), None


def _page(values, after, name):
    start = 0
    if after is not None:
        if type(after) is not int or after not in values:
            raise ValueError(name + ' does not identify a member of this exact effect index')
        start = values.index(after) + 1
    page = values[start:start + PAGE_SIZE]
    return page, page[-1] if start + len(page) < len(values) else None


def _reference(model, chart, exact, source, destination, transport):
    result = dict(status='Unknown', reason='missing_independent_reference',
                  basis='Canonical fixed-position slots; independent of entry occupants')
    if exact is None or source in exact['orientation_unknown']:
        return result
    first, second = chart.frame(source), chart.frame(destination)
    mapped = list(map(int, transport[first]))
    permutation = [second.index(slot) for slot in mapped]
    same = mapped == second
    result.update(status='Matched' if same else 'Mismatch',
        reason='edge_matches_reference' if same else 'edge_orientation_mismatch',
        source_frame=list(first), destination_frame=list(second), permutation=permutation,
        certificate=dict(kind='coherent-transported-frames', version=FRAME_VERSION, model=model.model_id))
    return result


def _selection(model, body, orbit):
    position = body.get('position')
    if position is not None:
        _integer(position, model.np, 'Position')
        if int(model.oid[position]) != orbit:
            raise ValueError('Position belongs to another orbit; choose that orbit explicitly')
    offset = body.get('edge_offset', 0)
    if type(offset) is not int or offset < 0 or (offset and position is None):
        raise ValueError('Edge offset needs a selected cycle and a nonnegative integer')
    return position, offset


def _refcontext(context, state):
    return dict(review_context_id=context['id'], frame_version=context['frame_version'],
        naming_version=context['naming_version'], state_hash=state.hash if state is not None else None,
        recipe_hash=context['recipe_hash'])


def _residual_reference(wb, state, row):
    result = dict(status='Unknown', reason='missing_independent_reference',
        basis='Current-to-Home ownership in canonical fixed-position slots; not an executable operation')
    if row['orientation_status'] != 'Known':
        return result
    first = wb.transported_frames.frame(row['position'])
    second = wb.transported_frames.frame(row['destination'])
    permutation = row['correction_element']
    if (not isinstance(permutation, list) or sorted(permutation) != list(range(len(first)))
            or any(int(state.labels[slot]) != second[permutation[i]] for i, slot in enumerate(first))):
        raise ValueError('Residual correction does not match the depicted complete slot correspondence')
    same = permutation == list(range(len(first)))
    result.update(status='Matched' if same else 'Mismatch',
        reason='ownership_matches_reference' if same else 'ownership_orientation_correction',
        source_frame=list(first), destination_frame=list(second), permutation=deepcopy(permutation),
        certificate=dict(kind='coherent-transported-frames', version=FRAME_VERSION, model=wb.m.model_id))
    return result


def _inspect_residual_cycles(wb, body, mode, source_guard, *, select_first):
    model, context = wb.m, wb.review_context()
    orbit = body['orbit']
    if orbit != wb.w['orbit']:
        raise ValueError(mode + ' ownership is available only for the active working orbit')
    if 'macro' in body or 'recipe' in body:
        raise ValueError(mode + ' uses the current work context, not a supplied macro or recipe')
    position, offset = _selection(model, body, orbit)
    recipe = [step for phase in PHASES for step in wb.w['draft'][phase]]
    state, facts, residual, reason = wb.s.st, None, None, None
    if mode == 'After':
        if context['operation_state'] == 'executed' or not recipe:
            state = None
            reason = ('Retained steps were already executed' if context['operation_state'] == 'executed'
                      else 'No explicit complete operation is entered')
        else:
            source, destination, facts = wb.effect(recipe)
            labels = wb.s.st.labels.copy(); labels[destination] = labels[source]
            state = PuzzleState(model, labels, trusted=True)
    if state is not None:
        bundle = wb.cached_residuals()
        if bundle['status'] != 'Analysed':
            bundle = wb.residual_bundle(context, facts=facts, predicted=state if facts is not None else None)
        residual = bundle.get(mode.lower())
        if (bundle.get('review_context_id') != context['id'] or not isinstance(residual, dict)
                or residual.get('review_context') != context or residual.get('model') != model.model_id
                or residual.get('orbit') != orbit or residual.get('state_hash') != state.hash
                or residual.get('frame_version') != context['frame_version']
                or residual.get('naming_version') != context['naming_version']
                or residual.get('source', {}).get('kind') != mode
                or residual['source'].get('state_hash') != state.hash
                or (mode == 'After' and (residual['source'].get('base_state_hash') != wb.s.st.hash
                    or residual['source'].get('recipe_hash') != context['recipe_hash']))):
            raise ValueError('Residual projection no longer matches the exact work context and depicted state')
    filters = PieceFilterProjection(wb)
    answer = dict(model=model.model_id, mode=mode, relationKind='home-ownership',
        source_hash=wb.s.st.hash, review_context_id=context['id'], refcontext=_refcontext(context, state),
        source_guard=source_guard, scope=body['scope'], recipe=deepcopy(recipe) if mode == 'After' else [], macro=None,
        operation_state=context['operation_state'], source_boundary='actual' if mode == 'Current' else 'after-complete',
        source_state_hash=state.hash if state is not None else None,
        projection_available=state is not None, projection_reason=reason, orbit=orbit, support=[],
        piece_filter=filters.metadata(state) if state is not None else None,
        actual_piece_filter=filters.metadata(wb.s.st), requested_position=body.get('position'), selected=None, residual=None,
        cycle_index=dict(total=None, items=[], next_after=None),
        orientation_index=dict(total=None, positions=[], next_after=None),
        protection=dict(status='Not evaluated', scope='Ownership is not an operation or execution permission'))
    if residual is not None:
        keys = ('version', 'orbit', 'state_hash', 'source', 'P_nonbuffer', 'P_buffer', 'O_nonbuffer', 'O_buffer',
                'FrameUnknown', 'raw_exact_labels_solved', 'exact_solved', 'ExactSolved', 'completion',
                'frame_status', 'invariant_status', 'buffer_occupants', 'unresolved_dependencies')
        answer['residual'] = {key: deepcopy(residual[key]) for key in keys}
        cycles = {row['positions'][0]: row['edges'] for row in residual['position_cycles']}
        containing = {edge['position']: anchor for anchor, edges in cycles.items() for edge in edges}
        orientation = {row['position']: row for row in residual['orientation_residuals']}
        anchors, next_cycle = _page(list(cycles), body.get('cycle_after'), 'Cycle cursor')
        orientation_page, next_residual = _page(list(orientation), body.get('residual_after'), 'Orientation cursor')
        answer['cycle_index'] = dict(total=len(cycles), items=[dict(anchor=p, length=len(cycles[p])) for p in anchors], next_after=next_cycle)
        answer['orientation_index'] = dict(total=len(orientation), positions=orientation_page, next_after=next_residual)
        if select_first and position is None:
            position = anchors[0] if anchors else orientation_page[0] if orientation_page else None
        if position is not None:
            if position in containing:
                anchor = containing[position]; rows = cycles[anchor]; kind = 'cycle'
            elif position in orientation:
                anchor = position; rows = [orientation[position]]; kind = 'orientation'
            else:
                raise ValueError('Position has no ownership cycle or orientation residual in this state')
            if offset >= len(rows):
                raise ValueError('Edge offset is outside the selected ownership cycle')
            def piece(p, at):
                return filters.record(wb.piece(position=p, state=at), at)
            edges = []
            for row in rows[offset:offset + PAGE_SIZE]:
                model.check_cancel()
                first, second = row['position'], row['destination']
                edges.append(dict(source_position=first, destination_position=second,
                    source=piece(first, state), destination=piece(second, state),
                    actual_source=piece(first, wb.s.st), actual_destination=piece(second, wb.s.st),
                    slots=[dict(source_slot=int(s), destination_slot=int(state.labels[s]), label=int(state.labels[s]))
                           for s in model.slots(first)], reference=_residual_reference(wb, state, row)))
            end = offset + len(edges)
            answer['selected'] = dict(kind=kind, anchor=anchor, length=len(rows), edge_offset=offset,
                next_edge_offset=end if end < len(rows) else None, edges=edges)
    answer['source_signature'] = digest(canonical(dict(mode=mode, refcontext=answer['refcontext'],
        hash=wb.s.st.hash, guard=source_guard, scope=body['scope'], orbit=orbit,
        next=wb.w['next'], inspected=wb.w['inspected'], inspected_position=wb.w.get('inspected_position'),
        filter_context=filters.context_hash)).encode())
    model.check_cancel()
    if wb.guard() != source_guard or wb.review_context()['id'] != context['id']:
        raise ValueError('Residual graph context changed; refresh the workspace')
    return answer


def inspect_cycles(wb, body, *, select_first=False):
    """Inspect supplied scope; no selection, review, preview or journal mutation."""
    with wb.lock:
        model = wb.m
        model.check_cancel()
        if body.get('model') != model.model_id:
            raise ValueError('Cycle inspection model changed; refresh the workspace')
        source_guard = wb.guard()
        if body.get('guard') != source_guard:
            raise ValueError('Cycle inspection context changed; refresh before inspecting')
        scope = body.get('scope')
        if scope not in SCOPES:
            raise ValueError('Cycle scope must be selected-macro, macro-steps or complete')
        orbit = _integer(body.get('orbit'), 35, 'Orbit')
        mode = body.get('mode', 'Operation')
        if mode not in MODES:
            raise ValueError('Cycle mode must be Current, Operation or After')
        if mode != 'Operation':
            return _inspect_residual_cycles(wb, body, mode, source_guard, select_first=select_first)
        macro = None
        if scope == 'selected-macro':
            record = wb.bound_macro(body.get('macro'), binding_required=True)
            macro = {key: deepcopy(record[key]) for key in ('id', 'version', 'recipe')}
            chosen = record['recipe']
        elif scope == 'macro-steps':
            chosen = wb.w['draft']['macro']
        else:
            chosen = [step for phase in PHASES for step in wb.w['draft'][phase]]
        if scope != 'selected-macro' and 'macro' in body:
            raise ValueError('A library binding is only valid for selected-macro scope')
        recipe = _recipe(model, chosen)
        if canonical(_recipe(model, body.get('recipe'))) != canonical(recipe):
            raise ValueError('Chosen recipe changed; inspect the current exact operation')
        position, offset = _selection(model, body, orbit)

        # The sparse action contains changed slots only. Complete transport is
        # needed even for an unchanged slot of a rotating stationary piece.
        transport = model.ids.copy()
        if recipe:
            source, destination, facts = wb.effect(recipe)
            transport[source] = destination
            support = deepcopy(facts['support'])
        else:
            source = np.empty(0, dtype=np.int32)
            support = []
        positions = np.flatnonzero(model.oid == orbit)
        destinations = model.sp[transport[model.first[positions]]]
        mapping = {int(p): int(q) for p, q in zip(positions, destinations) if p != q}
        cycles, containing, seen = {}, {}, set()
        for p in sorted(mapping):
            if p in seen:
                continue
            model.check_cancel()
            cycle, q = [], p
            while q not in seen:
                seen.add(q); cycle.append(q); q = mapping[q]
            cycles[p] = cycle
            containing.update((member, p) for member in cycle)
        changed = np.unique(model.sp[source[model.so[source] == orbit]])
        residuals = [int(p) for p in changed if int(p) not in mapping]
        anchors, next_cycle = _page(list(cycles), body.get('cycle_after'), 'Cycle cursor')
        orientation_page, next_residual = _page(residuals, body.get('residual_after'), 'Orientation cursor')
        if select_first and position is None:
            position = anchors[0] if anchors else orientation_page[0] if orientation_page else None

        operation_state = wb.operation_state()
        available = operation_state != 'executed'
        boundary = 'actual' if scope == 'complete' else 'prepare'
        state = wb.s.st if available else None
        prepare = deepcopy(wb.w['draft']['prepare']) if boundary == 'prepare' else []
        if state is not None and prepare:
            src, dst, _ = wb.effect(prepare)
            labels = state.labels.copy(); labels[dst] = labels[src]
            state = PuzzleState(model, labels, trusted=True)
        filters = PieceFilterProjection(wb)
        filter_metadata = filters.metadata(state) if state is not None else None
        answer = dict(model=model.model_id, mode='Operation', relationKind='operation-effect',
            refcontext=_refcontext(wb.review_context(), state), source_hash=wb.s.st.hash,
            review_context_id=wb.review_context()['id'],
            source_guard=source_guard, scope=scope, recipe=deepcopy(recipe), macro=macro,
            operation_state=operation_state, source_boundary=boundary,
            source_state_hash=state.hash if state is not None else None,
            projection_available=available,
            projection_reason=None if available else 'Operation already executed; start a new operation before projecting it again',
            orbit=orbit, support=support, piece_filter=filter_metadata,
            actual_piece_filter=filters.metadata(wb.s.st),
            cycle_index=dict(total=len(cycles), items=[dict(anchor=p, length=len(cycles[p])) for p in anchors], next_after=next_cycle),
            orientation_index=dict(total=len(residuals), positions=orientation_page, next_after=next_residual),
            requested_position=body.get('position'), selected=None,
            protection=dict(status='Not evaluated', scope='Use current full-operation review'))
        if position is not None:
            if position in containing:
                anchor = containing[position]; cycle = cycles[anchor]; kind = 'cycle'
            elif position in residuals:
                anchor = position; cycle = [position]; kind = 'orientation'
            elif scope == 'complete':
                anchor = position; cycle = []; kind = 'unchanged'
            else:
                raise ValueError('Position has no changed piece cycle or orientation in this exact scope')
            if (kind == 'unchanged' and offset != 0) or (kind != 'unchanged' and offset >= len(cycle)):
                raise ValueError('Edge offset is outside the selected cycle')
            exact = wb.effect_decomposition(recipe) if recipe and cycle else None
            edges = []
            for index in range(offset, min(offset + PAGE_SIZE, len(cycle))):
                model.check_cancel()
                first, second = cycle[index], cycle[(index + 1) % len(cycle)]
                slots = model.slots(first)
                def piece(p, at):
                    return filters.record(wb.piece(position=p, state=at), at) if at is not None else None
                edges.append(dict(source_position=first, destination_position=second,
                    source=piece(first, state), destination=piece(second, state),
                    actual_source=piece(first, wb.s.st), actual_destination=piece(second, wb.s.st),
                    slots=[dict(source_slot=int(s), destination_slot=int(transport[s]),
                                label=int(state.labels[s]) if state is not None else None) for s in slots],
                    reference=_reference(model, wb.transported_frames, exact, first, second, transport)))
            end = offset + len(edges)
            answer['selected'] = dict(kind=kind, anchor=anchor, length=len(cycle),
                edge_offset=offset, next_edge_offset=end if end < len(cycle) else None, edges=edges)
            if kind == 'unchanged':
                answer['selected'].update(actual_position=filters.record(wb.piece(position=position), wb.s.st),
                    note='No net change at this fixed position. Intermediate steps are checked separately by protection review.')
        # The mechanical guard deliberately excludes some display selections.
        # Bind them here so a late page cannot silently replace another context.
        answer['source_signature'] = digest(canonical(dict(model=model.model_id,
            hash=wb.s.st.hash, guard=source_guard, scope=scope, orbit=orbit, recipe=recipe, macro=macro,
            prepare=prepare, next=wb.w['next'], inspected=wb.w['inspected'],
            inspected_position=wb.w.get('inspected_position'), filter_context=filters.context_hash,
            operation_state=operation_state)).encode())
        model.check_cancel()
        return answer
