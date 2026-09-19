"""Detached, conditional commit choices for the existing Session transaction.

Nothing here commits, searches for moves, selects a macro, or publishes a Current
completion certificate. The caller validates its preview/guard and atomically
stores these preferences with that one operation, then derives actual residuals.
"""
from copy import deepcopy
import secrets

import numpy as np

from core import ORDER, canonical, digest
from mathematical_names import NAMING_VERSION
from residuals import certify_completion
from transported_frames import FRAME_VERSION
from work_intents import evaluate_goal, normalize_goal

CONTEXT_FIELDS = ('current', 'target', 'target_requirement', 'roles', 'draft',
                  'draft_sources', 'reference', 'block', 'goal', 'selected_macro',
                  'operation_generation', 'completed_operation')
SOURCES = ('work-sheet', 'live', 'undo', 'redo', 'restore', 'reset', 'new')


def _integer(value, limit, label):
    if type(value) is not int or not 0 <= value < limit:
        raise ValueError(label + ' must be a canonical integer in range')
    return value


def capture_orbit_context(workspace):
    context = {key: deepcopy(workspace[key]) for key in CONTEXT_FIELDS if key in workspace}
    context['local_view'] = {key: deepcopy(workspace.get('view', {})[key])
        for key in ('local_center',) if key in workspace.get('view', {})}
    return context


def switch_context(model, workspace, orbit):
    """Existing orbit context semantics without persistence or global-policy rollback."""
    _integer(orbit, 35, 'Orbit')
    result = deepcopy(workspace)
    if orbit == workspace['orbit']:
        return result
    contexts = result.setdefault('contexts', {})
    contexts[str(workspace['orbit'])] = capture_orbit_context(workspace)
    default = dict(current=None, target=None, target_requirement=None,
        roles=list(map(int, model.trees[orbit]['buffers'])), reference=[],
        draft={p: [] for p in ('prepare', 'macro', 'cleanup')},
        draft_sources={p: [] for p in ('prepare', 'macro', 'cleanup')},
        block=dict(name='Working block', members=[], protected=[]), goal='insert',
        selected_macro=None, operation_generation=None)
    saved = contexts.get(str(orbit), {})
    for key in CONTEXT_FIELDS:
        if key in default:
            result[key] = deepcopy(saved.get(key, default[key]))
        elif key in saved:
            result[key] = deepcopy(saved[key])
        else:
            result.pop(key, None)
    if not isinstance(result.get('operation_generation'), str) or not result['operation_generation']:
        result['operation_generation'] = secrets.token_hex(12)
    result['view'] = deepcopy(result.get('view', {}))
    for key in ('local_center',):
        if key in saved.get('local_view', {}):
            result['view'][key] = deepcopy(saved['local_view'][key])
    result['orbit'] = orbit
    return result


def _member(model, member):
    identity = _integer(member.get('identity'), model.np, 'Block identity')
    position = _integer(member.get('position'), model.np, 'Block position')
    labels = member.get('labels')
    if (int(model.oid[identity]) != int(model.oid[position]) or not isinstance(labels, list)
            or len(labels) != int(model.k[position]) or any(type(x) is not int or not 0 <= x < model.n for x in labels)
            or len(set(labels)) != len(labels) or any(int(model.sp[x]) != identity for x in labels)):
        raise ValueError('Block requirement must retain one exact identity and its complete destination labels')
    mode = member.get('mode', 'exact')
    if mode not in ('position', 'exact'):
        raise ValueError('Block requirement mode must be position or exact')
    return identity, position, labels, mode


def _satisfied(model, state, member):
    identity, position, labels, mode = _member(model, member)
    return bool(state.at[position] == identity and (mode == 'position'
        or np.array_equal(state.labels[model.slots(position)], labels)))


def block_candidates(model, state, workspace):
    """Explicit order, then real shared-hosting incidence; never cap or screen adjacency."""
    if state.m is not model:
        raise ValueError('Candidates require the authoritative model state')
    orbit = _integer(workspace['orbit'], 35, 'Orbit')
    members = workspace['block']['members']; roles = workspace['roles']
    result = []; seen = set()

    def append(member, reason):
        identity, position, labels, mode = _member(model, member)
        key = identity, position, mode, tuple(labels)
        if int(model.oid[position]) != orbit or key in seen or _satisfied(model, state, member):
            return
        seen.add(key)
        role = 'A' if position == roles[0] else 'B' if position == roles[1] else None
        result.append(dict(identity=identity, position=position, requirement=deepcopy(member),
            reason=reason, buffer_role=role, automatic=role is None))

    for index, member in enumerate(members):
        model.check_cancel()
        append(member, dict(kind='ExplicitMember', member_index=index))
    if result or not members:
        return result
    # A non-Home block has no implied destination orientation for a new member.
    if any(member['identity'] != member['position'] or member['labels'] != model.slots(member['position']).tolist()
           for member in members):
        return result
    seeds = {}
    for index, member in enumerate(members):
        if _satisfied(model, state, member):
            for cell in model.hosting(member['position']):
                seeds.setdefault(int(cell), index)
    positions = {int(p) for cell in seeds for p in model.cell_positions[cell]
                 if int(model.oid[p]) == orbit and not state.correct[p]}
    for position in sorted(positions):
        model.check_cancel()
        cell = min(set(model.hosting(position)) & set(seeds))
        append(dict(identity=position, position=position, labels=model.slots(position).tolist()),
               dict(kind='SharedHostingCell', cell=cell, member_index=seeds[cell]))
    return result


def _completion_plans(model, before, after, workspace, evidence):
    plans = []
    recipe = [step for phase in ('prepare', 'macro', 'cleanup') for step in workspace['draft'][phase]]
    recipe_hash = digest(canonical(recipe).encode())
    for residual in evidence:
        model.check_cancel()
        orbit = _integer(residual.get('orbit'), 35, 'Completion orbit')
        context = deepcopy(residual.get('review_context', {})); identifier = context.pop('id', None)
        origin = residual.get('source', {})
        if (identifier != digest(canonical(context).encode()) or context.get('version') != 'review-context-v1'
                or context.get('model') != model.model_id or residual.get('model') != model.model_id
                or context.get('frame_version') != FRAME_VERSION or residual.get('frame_version') != FRAME_VERSION
                or context.get('naming_version') != NAMING_VERSION or context.get('orbit') != orbit
                or context.get('state_hash') != before.hash or context.get('recipe_hash') != recipe_hash
                or residual.get('state_hash') != after.hash or origin.get('kind') != 'After'
                or origin.get('base_state_hash') != before.hash or origin.get('state_hash') != after.hash
                or origin.get('recipe_hash') != recipe_hash):
            raise ValueError('Conditional completion evidence does not match this operation and exact state pair')
        slots = model.ids[model.so == orbit]
        raw_before = bool(np.array_equal(before.labels[slots], slots))
        raw_after = bool(np.array_equal(after.labels[slots], slots))
        if residual.get('raw_exact_labels_solved') != raw_after:
            raise ValueError('Completion labels disagree with the authoritative post-state')
        certificate = certify_completion(residual, dict(status='Executed',
            context_id=identifier, recipe_hash=recipe_hash))
        reason = ('AlreadySolved' if raw_before else 'LabelsUnfinished' if not raw_after
            else 'ExternalPending' if context.get('external_pending') is not None
            else 'FrameUnknown' if certificate['reasons'] != ['conditional_after_state'] else None)
        plans.append(dict(orbit=orbit, status='Conditional', eligible=reason is None,
            reason=reason, requires_committed_current_certificate=True,
            before_state=before.hash, after_state=after.hash, review_context_id=identifier))
    return plans


def plan_commit_continuity(model, before, after, workspace, *, source='work-sheet', completion_evidence=()):
    """Build a detached plan. The existing Session commits it once, or discards all of it."""
    model.check_cancel()
    if before.m is not model or after.m is not model or workspace.get('model') != model.model_id:
        raise ValueError('Continuity must use one authoritative model')
    if source not in SOURCES:
        raise ValueError('Unknown continuity event source')
    orbit = _integer(workspace['orbit'], 35, 'Orbit')
    result = dict(workspace=deepcopy(workspace), transition=dict(kind='Stay', reason='No successful insertion'),
                  candidates=[], completion_plans=[], protection_additions=[], suggestions=[])
    if source in ('work-sheet', 'live'):
        result['completion_plans'] = _completion_plans(model, before, after, workspace, completion_evidence)
        result['protection_additions'] = sorted({p['orbit'] for p in result['completion_plans'] if p['eligible']})
    goal = normalize_goal(workspace.get('goal'))
    if source == 'work-sheet' and goal in ('insert', 'place'):
        outcome = evaluate_goal(model, before, after, workspace)
        if outcome['before'] is False and outcome['after'] is True:
            nxt = workspace.get('next')
            if nxt is not None:
                identity = _integer(nxt.get('identity'), model.np, 'Next identity')
                target = _integer(nxt.get('target'), model.np, 'Next target')
                destination = int(model.oid[identity])
                if destination < 0 or int(model.oid[target]) != destination:
                    raise ValueError('Locked Next identity and destination must share a moving orbit')
                work = switch_context(model, workspace, destination)
                work.update(current=identity, target=target, next=None, inspected=identity, inspected_position=None)
                requirement = work.get('target_requirement')
                if requirement and (requirement.get('identity') != identity or requirement.get('position') != target):
                    work['target_requirement'] = None
                check = dict(work, goal='insert')
                target_result = evaluate_goal(model, after, after, check)
                status = 'Satisfied' if target_result['after'] is True else 'Unsatisfied' if target_result['after'] is False else 'Unknown'
                result.update(workspace=work, transition=dict(kind='LockedNext', from_orbit=orbit,
                    orbit=destination, identity=identity, position=target, target_status=status))
            else:
                candidates = block_candidates(model, after, workspace)
                result['candidates'] = candidates
                chosen = next((item for item in candidates if item['automatic']), None)
                if chosen is not None:
                    work = result['workspace']
                    work.update(current=chosen['identity'], target=chosen['position'],
                        target_requirement=deepcopy(chosen['requirement']), inspected=chosen['identity'], inspected_position=None)
                    result['transition'] = dict(kind='BlockCandidate', orbit=orbit, identity=chosen['identity'],
                        position=chosen['position'], reason=deepcopy(chosen['reason']))
                else:
                    result['transition']['reason'] = 'No ordinary block candidate; inspect the remaining residuals'
    if orbit in result['protection_additions'] and workspace.get('next') is None:
        later = ORDER[ORDER.index(orbit) + 1:]
        result['suggestions'] = [dict(orbit=o, status='NeedsInspection', reason='Later in retained phase order')
            for o in later if not bool(np.all(after.correct[model.oid == o]))]
    result['event_context'] = dict(version='workflow-transition-v1', model=model.model_id,
        pre_state=before.hash, post_state=after.hash, source_orbit=orbit,
        source_operation=workspace.get('operation_generation'), transition=deepcopy(result['transition']),
        planned_auto_protection=list(result['protection_additions']))
    model.check_cancel()
    return result
