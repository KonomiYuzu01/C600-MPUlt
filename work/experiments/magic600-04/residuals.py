"""Read-only residuals from the authoritative state's exact labels.

Run derive_residual in a cancellable analysis job under the caller's Session lock,
never from paint or a window's passive snapshot. The caller may reuse a completed
result only with the same ReviewContext and state; this module stores no result.
"""
from copy import deepcopy

import numpy as np

from core import canonical, digest
from mathematical_names import NAMING_VERSION
from transported_frames import FRAME_VERSION, FrameUnavailable, TransportedFrames

RESIDUAL_VERSION = 'exact-residual-v1'
COMPLETION_VERSION = 'orbit-completion-v1'


def _integer(value, limit, label):
    if type(value) is not int or not 0 <= value < limit:
        raise ValueError(label + ' must be a canonical integer in range')
    return value


def _bindings(model, orbit, bindings):
    _integer(orbit, 35, 'Orbit')
    if not isinstance(bindings, dict):
        raise ValueError('Residual bindings must be an object')
    roles = bindings.get('roles')
    if not isinstance(roles, list) or len(roles) != 2:
        raise ValueError('Residual bindings require explicit ordered A/B positions')
    for position in roles:
        _integer(position, model.np, 'Buffer position')
        if model.oid[position] != orbit:
            raise ValueError('Buffer positions must belong to the inspected orbit')
    if roles[0] == roles[1]:
        raise ValueError('A and B must be distinct fixed positions')
    if not isinstance(bindings.get('goal'), str):
        raise ValueError('The existing explicit goal must be retained')
    if type(bindings.get('prefix')) is not bool:
        raise ValueError('The explicit current prefix-protection policy is required')
    policy = bindings.get('protected_requirements')
    if not isinstance(policy, dict) or set(policy) != {'orbits', 'positions'}:
        raise ValueError('Protection requires orbit and captured-position lists')
    if not isinstance(policy['orbits'], list) or not isinstance(policy['positions'], list):
        raise ValueError('Protection scopes must be lists')
    for protected in policy['orbits']:
        _integer(protected, 35, 'Protected orbit')
    for item in policy['positions']:
        if not isinstance(item, dict):
            raise ValueError('A protected position requires exact captured labels')
        position = _integer(item.get('position'), model.np, 'Protected position')
        labels = item.get('labels')
        if (not isinstance(labels, list) or len(labels) != int(model.k[position])
                or any(type(label) is not int or not 0 <= label < model.n for label in labels)
                or len(set(labels)) != len(labels)):
            raise ValueError('Captured labels must identify every slot of one whole piece')
        identities = model.sp[labels]
        if np.any(identities != identities[0]) or model.oid[identities[0]] != model.oid[position]:
            raise ValueError('Captured labels must belong to one identity in the destination orbit')
        if item.get('mode', 'exact') not in ('position', 'exact'):
            raise ValueError('Protection mode must be position or exact')
    return deepcopy(roles), deepcopy(policy)


def _context(model, state, orbit, bindings, review_context, source):
    if state.m is not model:
        raise ValueError('State and model must share the authoritative model instance')
    if not isinstance(review_context, dict):
        raise ValueError('A versioned ReviewContext is required')
    context = deepcopy(review_context)
    identifier = context.pop('id', None)
    if (not isinstance(identifier, str) or identifier != digest(canonical(context).encode())
            or context.get('version') != 'review-context-v1'
            or context.get('model') != model.model_id
            or context.get('frame_version') != FRAME_VERSION
            or context.get('naming_version') != NAMING_VERSION
            or context.get('orbit') != orbit or context.get('roles') != bindings['roles']
            or context.get('goal') != bindings['goal']):
        raise ValueError('Residual inputs do not match the versioned work context')
    context['id'] = identifier
    policy = bindings['protected_requirements']
    protection_revision = digest(canonical(dict(protected=policy['orbits'],
        prefix=bindings['prefix'], position_locks=policy['positions'])).encode())
    if context.get('protection_revision') != protection_revision:
        raise ValueError('Residual protection requirements do not match the work context')
    if source == 'Current':
        if context.get('state_hash') != state.hash:
            raise ValueError('Current residual state does not match the work context')
        origin = dict(kind='Current', state_hash=state.hash)
    elif (isinstance(source, dict) and source.get('kind') == 'After'
          and source.get('base_state_hash') == context.get('state_hash')
          and source.get('recipe_hash') == context.get('recipe_hash')):
        origin = dict(kind='After', state_hash=state.hash,
            base_state_hash=source['base_state_hash'], recipe_hash=source['recipe_hash'])
    else:
        raise ValueError('After must bind the actual base state and complete operation recipe')
    return context, origin


def _partition(model, state, orbit, roles):
    positions = np.flatnonzero(model.oid == orbit)
    buffers = set(roles)
    displaced = positions[~state.position_correct[positions]].tolist()
    oriented = positions[state.position_correct[positions] & ~state.correct[positions]].tolist()
    return dict(P_nonbuffer=[p for p in displaced if p not in buffers],
        P_buffer=[p for p in roles if p in displaced],
        O_nonbuffer=[p for p in oriented if p not in buffers],
        O_buffer=[p for p in roles if p in oriented])


def _suggest(positions, roles, frame_known):
    if positions['P_nonbuffer']:
        return dict(kind='PlacePiece')
    if positions['O_nonbuffer']:
        return dict(kind='OrientPiece')
    for role, position in zip(('A', 'B'), roles):
        if position in positions['P_buffer'] or position in positions['O_buffer']:
            return dict(kind='FinishBuffer', role=role)
    return dict(kind='FinishOrbit' if frame_known else 'VerifyReference')


def derive_residual(model, state, orbit, bindings, orientation_evidence, review_context, source='Current',
                    invariant_evidence=None):
    """Return detached exact facts; do not pick a target, intent, macro or action.

    orientation_evidence is the existing TransportedFrames service or None. Its
    immutable chart is the only orientation authority accepted here. An absent
    operation scope in bindings prevents Current completion certification.
    """
    model.check_cancel()
    roles, policy = _bindings(model, orbit, bindings)
    context, origin = _context(model, state, orbit, bindings, review_context, source)
    positions = _partition(model, state, orbit, roles)
    all_positions = np.flatnonzero(model.oid == orbit)
    orbit_slots = model.ids[model.so == orbit]
    changed_slots = orbit_slots[state.labels[orbit_slots] != orbit_slots]
    changed_positions = np.unique(model.sp[changed_slots]).tolist()
    certificate, decomposition, reason = None, None, 'Independent frame certificate is unavailable'
    if orientation_evidence is not None:
        if not isinstance(orientation_evidence, TransportedFrames) or orientation_evidence.m is not model:
            raise ValueError('Orientation evidence must use the authoritative transported-frame service')
        try:
            # Identity has no affected-orbit row, but completion still needs this certificate.
            certificate = orientation_evidence.ensure(orbit)
            decomposition = orientation_evidence.decompose(state.labels[changed_slots], changed_slots)
        except FrameUnavailable as error:
            certificate = None
            reason = str(error)
    unknown = (set(map(int, decomposition['orientation_unknown'])) if decomposition is not None
               else set(map(int, state.at[changed_positions])))
    frame_known = certificate is not None and not unknown
    if decomposition is not None and not np.array_equal(decomposition['position_map'][all_positions], state.where[all_positions]):
        raise ValueError('Orientation decomposition does not describe the actual Home-to-Current state')
    increments = decomposition['orientation_elements'] if decomposition is not None else {}

    def record(position):
        identity = int(state.at[position])
        missing = certificate is None or identity in unknown
        observed = increments.get(identity, list(range(int(model.k[identity]))))
        return dict(position=position, identity=identity, home=identity, destination=identity,
            orientation_status='Unknown' if missing else 'Known',
            orientation_element=None if missing else observed,
            correction_element=None if missing else np.argsort(observed).tolist())

    cycles, seen = [], set()
    for position in sorted(positions['P_nonbuffer'] + positions['P_buffer']):
        model.check_cancel()
        if position in seen:
            continue
        current, edges = position, []
        while current not in seen:
            seen.add(current); edges.append(record(current)); current = int(state.at[current])
        if current != position:
            raise ValueError('Current-to-Home positions do not form complete cycles')
        cycles.append(dict(positions=[edge['position'] for edge in edges], edges=edges))
    orientation = [record(p) for p in sorted(positions['O_nonbuffer'] + positions['O_buffer'])]
    displaced_orientation = []
    for position in sorted(positions['P_nonbuffer'] + positions['P_buffer']):
        row = record(position)
        if row['orientation_status'] == 'Unknown' or row['identity'] in increments:
            displaced_orientation.append(row)
    unknown_positions = sorted(int(state.where[identity]) for identity in unknown)
    dependencies = [dict(kind='InvariantCertificate', orbit=orbit,
        reason='No matching model/frame-bound primitive invariant certificate has been supplied')]
    if not frame_known:
        dependencies.append(dict(kind='FrameCertificate', orbit=orbit, reason=reason if certificate is None
            else 'Some exact transports are outside the certified frame group'))
    result = dict(version=RESIDUAL_VERSION, model=model.model_id, frame_version=FRAME_VERSION,
        naming_version=NAMING_VERSION, state_hash=state.hash, orbit=orbit,
        review_context=context, source=origin, positions=positions,
        position_cycles=cycles, orientation_residuals=orientation,
        displaced_orientation=displaced_orientation,
        buffer_occupants=[dict(role=role, position=position, identity=int(state.at[position]),
            home=int(state.at[position]), position_correct=bool(state.position_correct[position]),
            exact_labels_correct=bool(state.correct[position])) for role, position in zip(('A', 'B'), roles)],
        protected_requirements=policy,
        frame_status=dict(orbit=orbit, position_count=len(all_positions),
            status='Verified' if frame_known else 'Unknown' if certificate else 'Unavailable',
            certificate=deepcopy(certificate), scope='Canonical fixed-position frames',
            reason=None if frame_known else dependencies[-1]['reason']),
        invariant_status=dict(status='Unavailable', dependency=deepcopy(dependencies[0])),
        unresolved_dependencies=dependencies, FrameUnknown=len(unknown_positions),
        unknown_orientation_positions=unknown_positions,
        raw_exact_labels_solved=bool(np.all(state.labels[orbit_slots] == orbit_slots)),
        explicit_goal=bindings['goal'], suggested_intent=_suggest(positions, roles, frame_known))
    result.update({key: len(value) for key, value in positions.items()})
    result['completion'] = certify_completion(result, bindings.get('operation'))
    result['exact_solved'] = result['ExactSolved'] = result['completion']['complete']
    if invariant_evidence is not None and decomposition is not None:
        from orbit_invariants import OrbitInvariants
        if not isinstance(invariant_evidence, OrbitInvariants) or invariant_evidence.m is not model:
            raise ValueError('Invariant evidence must use the authoritative model service')
        result['invariant_status'] = invariant_evidence.evaluate(decomposition, orbit)
        if result['invariant_status']['status'] in ('PassNecessary', 'Conflict'):
            result['unresolved_dependencies'] = [d for d in dependencies if d['kind'] != 'InvariantCertificate']
            invariant = result['invariant_status']
            invariant['orientation_group'] = model.census['orbits'][orbit]['orientation_group']
            invariant['reason'] = ('Position parity contradicts this orbit\'s verified even-permutation invariant; inspect state provenance and identity mapping.'
                if invariant['position_parity'] else 'The orientation product contradicts the verified orbit quotient invariant.'
                if invariant['orientation_quotient'] else 'Verified necessary parity and orientation conditions pass; this is not a construction or protection certificate.')
    model.check_cancel()
    return result


def certify_completion(residual, operation=None):
    """Complete Current evidence only; this never authorizes execution or protection.

    operation is trusted Workbench evidence bound to the same context and recipe:
    Known with complete affected_orbits, or Executed for retained committed steps.
    Absent, stale or unknown scope cannot stand for an empty draft.
    """
    context = residual['review_context']; reasons = []
    if not residual['raw_exact_labels_solved']:
        reasons.append('exact_labels_not_solved')
    frame = residual['frame_status']; certificate = frame.get('certificate')
    frame_bound = (frame['status'] == 'Verified' and frame.get('orbit') == residual['orbit']
        and isinstance(certificate, dict) and certificate.get('status') == 'Verified'
        and certificate.get('model') == residual['model']
        and certificate.get('frame_version') == residual['frame_version'] == FRAME_VERSION
        and certificate.get('position_count') == frame.get('position_count')
        and isinstance(certificate.get('frame_sha256'), str) and bool(certificate['frame_sha256']))
    if not frame_bound:
        reasons.append('frame_certificate_unavailable')
    conditional = residual['source']['kind'] != 'Current'
    if conditional:
        reasons.append('conditional_after_state')
    bound = (isinstance(operation, dict) and operation.get('context_id') == context['id']
        and operation.get('recipe_hash') == context['recipe_hash'])
    status = operation.get('status') if bound else None
    scope = operation.get('affected_orbits') if bound else None
    if status == 'Executed':
        pass
    elif (status != 'Known' or not isinstance(scope, list)
          or any(type(orbit) is not int or not 0 <= orbit < 35 for orbit in scope)):
        reasons.append('uncommitted_operation_scope_unknown')
    elif residual['orbit'] in scope:
        reasons.append('uncommitted_operation_affects_orbit')
    return dict(version=COMPLETION_VERSION, status='Conditional' if conditional else 'Complete' if not reasons else 'Incomplete',
        complete=not reasons, reasons=reasons, model=residual['model'], orbit=residual['orbit'],
        state_hash=residual['state_hash'], review_context_id=context['id'],
        frame_version=residual['frame_version'], scope='One moving orbit; not whole-puzzle completion')


def derive_delta(model, before, after, orbit, bindings):
    """Compare two supplied journal/simulation endpoints without storing history."""
    model.check_cancel()
    if before.m is not model or after.m is not model:
        raise ValueError('Residual Delta endpoints must use the authoritative model')
    roles, policy = _bindings(model, orbit, bindings)

    def counts(state):
        partition = _partition(model, state, orbit, roles)
        return dict(position=len(partition['P_nonbuffer']) + len(partition['P_buffer']),
            orientation=len(partition['O_nonbuffer']) + len(partition['O_buffer']),
            **{key: len(value) for key, value in partition.items()})

    changed_slots = np.flatnonzero(before.labels != after.labels)
    changed_positions = np.unique(model.sp[changed_slots]).tolist()
    protected_orbits = []
    for protected in sorted(set(policy['orbits'])):
        slots = changed_slots[model.so[changed_slots] == protected]
        if len(slots):
            protected_orbits.append(dict(orbit=protected, changed_slots=len(slots),
                positions=np.unique(model.sp[slots]).tolist()))
    requirements = []
    for item in policy['positions']:
        model.check_cancel()
        position, labels = item['position'], item['labels']; mode = item.get('mode', 'exact')
        identity = int(model.sp[labels[0]])

        def matches(state):
            return (bool(state.at[position] == identity) if mode == 'position'
                    else bool(np.array_equal(state.labels[model.slots(position)], labels)))

        before_met, after_met = matches(before), matches(after)
        requirements.append(dict(position=position, identity=identity, mode=mode,
            before_met=before_met, after_met=after_met, newly_violated=before_met and not after_met))
    buffer_changes = []
    for role, position in zip(('A', 'B'), roles):
        slots = model.slots(position)
        if not np.array_equal(before.labels[slots], after.labels[slots]):
            buffer_changes.append(dict(role=role, position=position, before_identity=int(before.at[position]),
                after_identity=int(after.at[position]), before_labels=before.labels[slots].tolist(),
                after_labels=after.labels[slots].tolist()))
    result = dict(version=RESIDUAL_VERSION, model=model.model_id, orbit=orbit,
        before_state_hash=before.hash, after_state_hash=after.hash,
        before=counts(before), after=counts(after), buffer_changes=buffer_changes,
        changed_positions=[dict(position=position, orbit=int(model.oid[position]),
            before_identity=int(before.at[position]), after_identity=int(after.at[position]))
            for position in changed_positions],
        protected_requirements=requirements, protected_orbit_changes=protected_orbits,
        protection_scope='Net endpoints only; no prefix claim')
    model.check_cancel()
    return result
