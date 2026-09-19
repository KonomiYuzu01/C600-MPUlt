"""Detached work-goal comparisons; never execution permission or target choice."""
from copy import deepcopy

import numpy as np

from core import canonical, digest
from mathematical_names import NAMING_VERSION
from transported_frames import FRAME_VERSION

GOALS = ('prepare', 'insert', 'place', 'orient', 'finish-buffer', 'endgame', 'block')
ALIASES = dict(Prepare='prepare', PlacePiece='place', OrientPiece='orient',
               FinishBuffer='finish-buffer', FinishOrbit='endgame', BuildBlock='block')
KINDS = dict(prepare='Prepare', insert='PlacePiece', place='PlacePiece',
             orient='OrientPiece', **{'finish-buffer': 'FinishBuffer'},
             endgame='FinishOrbit', block='BuildBlock')


def normalize_goal(value):
    if not isinstance(value, str):
        raise ValueError('Work goal must be a supported explicit name')
    value = ALIASES.get(value, value)
    if value not in GOALS:
        raise ValueError('Unknown work goal: ' + value)
    return value


def _integer(value, limit, label):
    if type(value) is not int or not 0 <= value < limit:
        raise ValueError(label + ' must be a canonical integer in range')
    return value


def _objects(model, identity, position, orbit=None):
    _integer(identity, model.np, 'Piece identity')
    _integer(position, model.np, 'Destination position')
    if model.oid[identity] < 0 or model.oid[identity] != model.oid[position]:
        raise ValueError('Identity and destination must share a moving orbit')
    if orbit is not None and model.oid[identity] != orbit:
        raise ValueError('Goal objects must belong to the active work orbit')


def _labels(model, requirement, identity, position):
    values = requirement.get('labels')
    if (not isinstance(values, list) or len(values) != int(model.k[position])
            or any(type(value) is not int or not 0 <= value < model.n for value in values)
            or len(set(values)) != len(values)
            or any(model.sp[value] != identity for value in values)):
        raise ValueError('The requirement must contain every exact label of the specified identity')
    return values


def _position(before, after, identity, position):
    return dict(kind='position', identity=identity, position=position, known=True,
        before=bool(before.at[position] == identity), after=bool(after.at[position] == identity))


def _exact(model, before, after, identity, position, labels):
    slots = model.slots(position)
    return dict(kind='labels', identity=identity, position=position, known=True,
        required_labels=list(labels),
        before=bool(before.at[position] == identity and np.array_equal(before.labels[slots], labels)),
        after=bool(after.at[position] == identity and np.array_equal(after.labels[slots], labels)))


def _frame_pair(model, before, after, work, residual_before, residual_after):
    """Accept only the already-derived, coherently paired residual evidence."""
    contexts = []
    known = []
    orbit = work['orbit']; count = int(np.count_nonzero(model.oid == orbit))
    for state, residual, kind in ((before, residual_before, 'Current'), (after, residual_after, 'After')):
        if residual is None:
            known.append(False)
            continue
        if not isinstance(residual, dict):
            raise ValueError('Residual evidence must be a bound result')
        context = deepcopy(residual.get('review_context'))
        if not isinstance(context, dict):
            raise ValueError('Residual work context is missing')
        identifier = context.pop('id', None)
        if (identifier != digest(canonical(context).encode())
                or context.get('version') != 'review-context-v1'
                or context.get('model') != model.model_id
                or context.get('frame_version') != FRAME_VERSION
                or context.get('naming_version') != NAMING_VERSION
                or context.get('state_hash') != before.hash
                or context.get('orbit') != orbit
                or normalize_goal(context.get('goal')) != normalize_goal(work['goal'])
                or context.get('roles') != work.get('roles')):
            raise ValueError('Residual evidence does not match the current work context')
        for key in ('current', 'target', 'reference', 'block', 'target_requirement'):
            if key in context and context[key] != work.get(key):
                raise ValueError('Residual work binding changed: ' + key)
        source = residual.get('source', {})
        if (residual.get('model') != model.model_id or residual.get('orbit') != orbit
                or residual.get('state_hash') != state.hash
                or residual.get('frame_version') != FRAME_VERSION
                or residual.get('naming_version') != NAMING_VERSION
                or source.get('kind') != kind or source.get('state_hash') != state.hash):
            raise ValueError('Residual evidence describes a different state or scope')
        if kind == 'After' and (source.get('base_state_hash') != before.hash
                or source.get('recipe_hash') != context.get('recipe_hash')):
            raise ValueError('After residual does not describe this complete-operation prediction')
        contexts.append(identifier)
        frame = residual.get('frame_status', {}); certificate = frame.get('certificate')
        known.append(bool(frame.get('status') == 'Verified' and frame.get('orbit') == orbit
            and frame.get('position_count') == count and isinstance(certificate, dict)
            and certificate.get('status') == 'Verified' and certificate.get('model') == model.model_id
            and certificate.get('frame_version') == FRAME_VERSION
            and certificate.get('position_count') == count
            and isinstance(certificate.get('frame_sha256'), str) and certificate['frame_sha256']))
    if len(contexts) == 2 and contexts[0] != contexts[1]:
        raise ValueError('Before and After residuals belong to different work contexts')
    return tuple(known)


def evaluate_goal(model, before, after, workspace, residual_before=None, residual_after=None):
    """Compare the user's existing bindings against two explicit states.

    After is conditional even when the goal is met. It never certifies Current
    completion, changes a goal, or weakens legality and protection checks.
    """
    model.check_cancel()
    if before.m is not model or after.m is not model or not isinstance(workspace, dict):
        raise ValueError('Goal states must share the authoritative model and workspace')
    goal = normalize_goal(workspace.get('goal'))
    orbit = _integer(workspace.get('orbit'), 35, 'Work orbit')
    mode = 'position' if goal == 'place' else 'exact'
    predicates = []

    def result(status=None, reason=None):
        def outcome(boundary):
            return (all(item[boundary] for item in predicates)
                if predicates and all(item[boundary] is not None for item in predicates) else None)
        was, now = outcome('before'), outcome('after')
        status = status or ('Unknown' if now is None else 'Met' if now else 'Unmet')
        if status in ('NotApplicable', 'MissingInput'):
            was = now = None
        model.check_cancel()
        return deepcopy(dict(kind=KINDS[goal], requirement_mode=mode, status=status,
            before=was, after=now, predicates=predicates, conditional_after=True,
            reason=reason or ('The chosen operation meets this goal conditionally; this is not a Current completion certificate.'
                if now else 'The chosen operation does not meet this goal; execution permission is checked separately.'
                if now is False else 'Required reference evidence is unavailable; known label facts remain inspectable.')))

    if goal == 'prepare':
        mode = None
        return result('NotApplicable', 'Prepare claims no completed target; legality and protection remain separate.')
    if goal == 'endgame':
        frame_before, frame_after = _frame_pair(model, before, after, workspace, residual_before, residual_after)
        slots = model.ids[model.so == orbit]
        predicates.append(dict(kind='orbit-labels', orbit=orbit, known=True,
            before=bool(np.array_equal(before.labels[slots], slots)),
            after=bool(np.array_equal(after.labels[slots], slots))))
        predicates.append(dict(kind='orbit-frame', orbit=orbit, known=frame_before and frame_after,
            before=True if frame_before else None, after=True if frame_after else None))
        return result()
    if goal == 'block':
        block = workspace.get('block')
        members = block.get('members') if isinstance(block, dict) else None
        if not isinstance(members, list) or not members:
            return result('MissingInput', 'Choose at least one explicit block requirement.')
        modes = set()
        for member in members:
            model.check_cancel()
            if not isinstance(member, dict):
                raise ValueError('Block requirements must be records')
            identity, position = member.get('identity'), member.get('position')
            _integer(identity, model.np, 'Block identity')
            _integer(position, model.np, 'Block position')
            # Existing exact blocks may include an immutable Home center.
            if identity != position or model.oid[identity] >= 0:
                _objects(model, identity, position)
            labels = _labels(model, member, identity, position)
            requirement_mode = member.get('mode', 'exact')
            if requirement_mode not in ('position', 'exact'):
                raise ValueError('Block requirement mode must be position or exact')
            modes.add(requirement_mode)
            predicates.append(_position(before, after, identity, position) if requirement_mode == 'position'
                else _exact(model, before, after, identity, position, labels))
        mode = next(iter(modes)) if len(modes) == 1 else 'mixed'
        return result()
    identity, position = workspace.get('current'), workspace.get('target')
    if identity is None or position is None:
        return result('MissingInput', 'Choose the piece identity and fixed destination explicitly.')
    _objects(model, identity, position, orbit)
    predicates.append(_position(before, after, identity, position))
    if goal == 'place':
        return result()
    if goal == 'finish-buffer':
        roles = workspace.get('roles')
        if not isinstance(roles, list) or len(roles) != 2 or position not in roles:
            return result('MissingInput', 'The chosen destination must be an explicitly bound A or B position.')
        for role in roles:
            _integer(role, model.np, 'Buffer position')
            if model.oid[role] != orbit:
                raise ValueError('Buffer positions must belong to the work orbit')
        if roles[0] == roles[1]:
            raise ValueError('A and B must be distinct fixed positions')
    requirement = workspace.get('target_requirement')
    if requirement is not None and not isinstance(requirement, dict):
        raise ValueError('Target requirement must be a captured or explicitly declared label arrangement')
    if requirement is not None:
        _objects(model, requirement.get('identity'), requirement.get('position'))
        if 'provenance' in requirement and not isinstance(requirement['provenance'], dict):
            raise ValueError('Target reference provenance must be a record')
    matching = requirement is not None and requirement.get('identity') == identity and requirement.get('position') == position
    if matching:
        labels = _labels(model, requirement, identity, position)
    elif goal == 'insert' and identity == position:
        labels = model.slots(position).tolist()
    else:
        return result('MissingInput', 'Declare the exact destination arrangement, including an explicit Home reference when desired.')
    predicates.append(_exact(model, before, after, identity, position, labels))
    provenance = requirement.get('provenance') if matching else None
    if isinstance(provenance, dict) and provenance.get('kind') == 'coherent-frame':
        reference_current = (provenance.get('model') == model.model_id
            and provenance.get('frame_version') == FRAME_VERSION)
        reference_known = (False, False)
        if reference_current:
            frames = _frame_pair(model, before, after, workspace, residual_before, residual_after)
            reference_known = tuple(known and
                residual['frame_status']['certificate']['frame_sha256'] == provenance.get('frame_sha256')
                for known, residual in zip(frames, (residual_before, residual_after)))
        predicates.append(dict(kind='orientation-reference', identity=identity, position=position,
            known=all(reference_known), before=True if reference_known[0] else None,
            after=True if reference_known[1] else None,
            reason=None if all(reference_known) else 'Declared frame evidence is unavailable or out of date at one or both boundaries.'))
    return result()
