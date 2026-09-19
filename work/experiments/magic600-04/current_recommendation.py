"""Bounded heuristic for one already-reviewed explicit complete operation.

Inputs are local Workbench evidence, never imported or client-supplied proof.
No recipe search, state simulation, frame construction, persistence or permission
is performed here. The caller publishes only after this cancellable call returns.
"""
from copy import deepcopy
from fractions import Fraction

import numpy as np

from core import canonical, digest
from macro_use import checked, infer_use, _score, USE_SCORE_VERSION
from mathematical_names import NAMING_VERSION
from transported_frames import FRAME_VERSION
from work_intents import evaluate_goal, _frame_pair

CURRENT_SCORE_VERSION = 'current-score-v1'
WEIGHTS = dict(F=Fraction(35, 100), Gp=Fraction(25, 100), Gtheta=Fraction(15, 100),
               B=Fraction(10, 100), UseScore=Fraction(1, 1000), M=Fraction(5, 100),
               D=Fraction(-20, 100), Cost=Fraction(-5, 100))
PHASES = ('prepare', 'macro', 'cleanup')
EVIDENCE_LIMIT = 12


def _rational(value):
    return None if value is None else dict(numerator=value.numerator, denominator=value.denominator)


def _clip(value):
    return min(Fraction(1), max(Fraction(-1), value))


def _bound(model, before, after, work, review):
    if before.m is not model or after.m is not model or not isinstance(review, dict):
        raise ValueError('Recommendation needs the authoritative states and complete local review')
    context = deepcopy(review.get('review_context', {})); identifier = context.pop('id', None)
    if (identifier != digest(canonical(context).encode())
            or context.get('version') != 'review-context-v1' or context.get('model') != model.model_id
            or context.get('frame_version') != FRAME_VERSION or context.get('naming_version') != NAMING_VERSION
            or context.get('state_hash') != before.hash or review.get('post_hash') != after.hash):
        raise ValueError('Recommendation states or evidence versions do not match the review context')
    context['id'] = identifier
    for key in ('orbit', 'current', 'target', 'roles', 'reference', 'block', 'goal', 'target_requirement'):
        if canonical(context.get(key)) != canonical(work.get(key)):
            raise ValueError('Recommendation work binding changed: ' + key)
    if canonical(context.get('locked_next')) != canonical(work.get('next')):
        raise ValueError('Recommendation Next bookmark changed')
    phases = work['draft']; recipe = [step for phase in PHASES for step in phases[phase]]
    if (context.get('recipe_hash') != digest(canonical(recipe).encode())
            or context.get('phase_hashes') != {phase: digest(canonical(phases[phase]).encode()) for phase in PHASES}):
        raise ValueError('Recommendation does not describe the complete current steps')
    sources = {phase: [dict(id=row['id'], version=row['version'], start=row['start'], count=row['count'],
                           recipe_hash=digest(canonical(row['recipe']).encode()))
                       for row in work['draft_sources'][phase]] for phase in PHASES}
    if canonical(context.get('macro_sources')) != canonical(sources):
        raise ValueError('Recommendation macro revisions changed')
    facts = review.get('effect')
    normalized = model.normalize(recipe)[0] if recipe else []
    if (not checked(facts) or facts['model'] != model.model_id or facts.get('frame_version') != FRAME_VERSION
            or canonical(facts.get('recipe')) != canonical(normalized)
            or type(facts.get('primitives')) is not int or facts['primitives'] < 0):
        raise ValueError('Recommendation needs full locally verified effects of these exact steps')
    first, second = review.get('residual_before'), review.get('residual_after')
    if not isinstance(first, dict) or not isinstance(second, dict):
        raise ValueError('Recommendation needs the paired Current and After residuals')
    for residual in (first, second):
        if canonical(residual.get('review_context')) != canonical(context):
            raise ValueError('Recommendation residuals belong to a different review context')
    policy = first.get('protected_requirements')
    if (not isinstance(policy, dict) or canonical(second.get('protected_requirements')) != canonical(policy)
            or context.get('protection_revision') != digest(canonical(dict(protected=policy.get('orbits'),
                prefix=work['prefix'], position_locks=policy.get('positions'))).encode())):
        raise ValueError('Recommendation protection policy does not match the review context')
    known_frames = _frame_pair(model, before, after, work, first, second)
    if all(known_frames) and canonical(first['frame_status']['certificate']) != canonical(second['frame_status']['certificate']):
        raise ValueError('Before and After orientation evidence must use the same fixed frame certificate')
    goal = evaluate_goal(model, before, after, work, first, second)
    if canonical(goal) != canonical(review.get('goal_result')):
        raise ValueError('Recommendation goal result does not match the same complete operation')
    return context, facts, first, second, policy, known_frames, goal


def _orientation(residual, frame_known):
    if not frame_known or residual.get('frame_status', {}).get('status') != 'Verified':
        return None
    identities = set()
    for row in residual['orientation_residuals'] + residual['displaced_orientation']:
        element = row.get('correction_element')
        if row.get('orientation_status') != 'Known' or not isinstance(element, list):
            return None
        if element != list(range(len(element))):
            identities.add(row['identity'])
    return identities


def _match(model, after, work, review, supplied):
    if supplied is not None and (not isinstance(supplied, dict) or supplied.get('scope') != 'complete'
            or supplied.get('review_context_id') != review['review_context']['id']
            or supplied.get('status') not in ('Declared', 'NotApplicable', 'Unknown')):
        raise ValueError('Role/reference evidence must belong to this complete-operation context')
    status = 'NotApplicable' if supplied is None else supplied['status']
    star = supplied.get('scoped_star') if supplied is not None else None
    if status == 'Declared' and (not isinstance(star, dict) or not any(canonical(star) == canonical(row)
            for row in review['effect'].get('scoped_stars', [])) or star.get('status') != 'Verified'
            or star.get('orbit') != work['orbit']):
        raise ValueError('Declared role relation lacks this complete effect\'s verified scoped star')
    if work['reference']:
        return None, dict(status='Unknown', reason='This scorer has no certificate for the selected noncanonical reference map')
    if status == 'NotApplicable':
        return Fraction(0), dict(status=status, reason='No role-specific relation is claimed')
    if status == 'Unknown':
        return None, dict(status=status, reason='The declared complete-operation relation is not verified')
    if any(row.get('kind') == 'orientation-reference' and not row.get('known')
           for row in review['goal_result']['predicates']):
        return None, dict(status='Unknown', reason='The declared target frame evidence is unavailable')
    identity, target = work['current'], work['target']
    if identity is None or target is None:
        return Fraction(0), dict(status='NotApplicable', reason='No Current-to-target relation is assigned')
    requirement = work.get('target_requirement')
    labels = (requirement['labels'] if requirement and requirement['identity'] == identity and requirement['position'] == target
              else model.slots(target).tolist() if identity == target and work['goal'] in ('insert', 'place', 'prepare', 'block', 'endgame') else None)
    if labels is None:
        return None, dict(status='Unknown', reason='The target has no declared exact destination arrangement')
    same = (work['roles'] == [star['a'], star['b']] and target == star['target']
            and int(after.at[target]) == identity and np.array_equal(after.labels[model.slots(target)], labels))
    return Fraction(int(same)), dict(status='Matched' if same else 'Mismatch',
        reason='Ordered roles, canonical reference and exact Current arrival match' if same
        else 'Ordered roles, target or exact Current arrival differ',
        roles=deepcopy(work['roles']), target=target, identity=identity)


def score_current(model, before, after, workspace, review, match_evidence=None):
    """Score one explicit full operation, without changing or executing it."""
    model.check_cancel()
    context, facts, first, second, policy, frames, goal = _bound(model, before, after, workspace, review)
    orbit = workspace['orbit']; length = facts['primitives']
    # Unique labels make endpoint differences exactly the operation's full net
    # support. No visible cycle, filtered piece list or truncated summary is used.
    affected = np.unique(model.sp[np.flatnonzero(before.labels != after.labels)])
    support = {int(o): int(np.count_nonzero(model.oid[affected] == o)) for o in np.unique(model.oid[affected])}
    expected = {row['orbit']: row['pieces'] for row in facts['support']}
    if support != expected or any(o < 0 for o in support):
        raise ValueError('Complete effect support disagrees with the exact before/after labels')
    n, total = support.get(orbit, 0), len(affected)
    use = infer_use(facts)
    if use['status'] == 'Unchecked':
        raise ValueError('Complete static-use facts are not verified for this model/frame version')
    dimensions = {row['orbit']: row for row in facts['orbit_effects']}
    use_score = _score(dimensions[orbit]['effect_class'], n, total, length)[0] if n else Fraction(0)
    use_status = 'NotApplicable' if not n else 'Known' if use_score is not None else 'Unknown'
    positions_before = set(first['positions']['P_nonbuffer'] + first['positions']['P_buffer'])
    positions_after = set(second['positions']['P_nonbuffer'] + second['positions']['P_buffer'])
    theta_before, theta_after = _orientation(first, frames[0]), _orientation(second, frames[1])
    focus = (Fraction(0) if goal['status'] == 'NotApplicable' else
             Fraction(int(goal['after']) - int(goal['before']))
             if type(goal['before']) is bool and type(goal['after']) is bool else None)
    block_work = dict(workspace, goal='block')
    block = evaluate_goal(model, before, after, block_work)
    block_rows = block['predicates']; block_count = len(block_rows)
    block_before = sum(row['before'] is True for row in block_rows)
    block_after = sum(row['after'] is True for row in block_rows)
    if any(row['before'] is None or row['after'] is None for row in block_rows):
        block_gain = None
    else:
        block_gain = _clip(Fraction(block_after - block_before, max(1, block_count)))
    exact_locks = {row['position'] for row in policy['positions'] if row.get('mode', 'exact') == 'exact'}
    protected_orbits = set(policy['orbits'])
    broken = [int(p) for p in affected if before.correct[p] and not after.correct[p]
              and int(model.oid[p]) not in protected_orbits and int(p) not in exact_locks]
    match_value, match = _match(model, after, workspace, review, match_evidence)
    terms = dict(F=focus, Gp=_clip(Fraction(len(positions_before) - len(positions_after), max(1, n))),
        Gtheta=None if theta_before is None or theta_after is None else
            _clip(Fraction(len(theta_before) - len(theta_after), max(1, n))),
        B=block_gain, D=Fraction(len(broken), max(1, total)), M=match_value,
        UseScore=use_score, Cost=Fraction(length, length + 30))
    conflicts, block_conflicts = review.get('conflicts'), review.get('block_conflicts')
    prefix = review.get('prefix', {}).get('status')
    policy_known = (isinstance(conflicts, list) and isinstance(block_conflicts, list)
                    and (not workspace['prefix'] or prefix in ('Preserved', 'Violation')))
    blocked = bool(conflicts or block_conflicts or workspace['prefix'] and prefix == 'Violation')
    complete = (policy_known and review.get('status') in ('Ready', 'Conflict') and all(frames)
                and use['status'] == 'Checked' and all(value is not None for value in terms.values()))
    direct = any(terms[key] is not None and terms[key] > 0 for key in ('F', 'Gp', 'Gtheta', 'B'))
    category = 'Blocked' if blocked else 'NeedsVerification' if not complete else 'Direct' if direct else 'PotentialPreparation'
    status = 'Blocked' if blocked else 'NeedsVerification' if not complete else 'Analysed'
    contributions = {key: None if value is None else 100 * WEIGHTS[key] * value for key, value in terms.items()}
    score = sum(contributions.values()) if complete and not blocked else None
    counts = dict(n_o=n, N=total, L=length, position_before=len(positions_before), position_after=len(positions_after),
        orientation_before=None if theta_before is None else len(theta_before),
        orientation_after=None if theta_after is None else len(theta_after), block_total=block_count,
        block_before=block_before, block_after=block_after, damage=len(broken))
    related = bool(n or any(row['position'] in affected for row in block_rows)
                   or workspace.get('current') is not None and int(before.where[workspace['current']]) in affected)
    evidence, term_objects = {}, {}

    def record(key, items):
        evidence[key] = dict(items=deepcopy(items[:EVIDENCE_LIMIT]), total=len(items), truncated=len(items) > EVIDENCE_LIMIT)
        identities = sorted({row['identity'] for row in items if row.get('identity') is not None})
        term_objects[key] = [dict(identity=i, position=int(before.where[i]), orbit=int(model.oid[i]),
                                  position_kind='before') for i in identities]

    goal_items = []
    for row in goal['predicates']:
        if row['before'] == row['after']:
            continue
        if row.get('identity') is not None:
            goal_items.append(dict(identity=row['identity'], position=row['position'], kind=row['kind']))
        elif row['kind'] == 'orbit-labels':
            goal_items.extend(dict(identity=int(p), position=int(p), kind='orbit-labels')
                for p in np.flatnonzero((model.oid == row['orbit']) & (before.correct != after.correct)))
    record('F', goal_items)
    record('Gp', [dict(position=p, identity=p, change='resolved' if p not in positions_after else 'introduced')
                  for p in sorted(positions_before ^ positions_after)])
    record('Gtheta', [] if theta_before is None or theta_after is None else
        [dict(identity=i, before_position=int(before.where[i]), position=int(after.where[i]),
              change='resolved' if i not in theta_after else 'introduced') for i in sorted(theta_before ^ theta_after)])
    record('B', [dict(identity=row['identity'], position=row['position'], before=row['before'], after=row['after'])
                 for row in block_rows if row['before'] != row['after']])
    record('D', [dict(identity=p, position=p, after_position=int(after.where[p]), orbit=int(model.oid[p])) for p in broken])
    reasons = []

    def reason(code, text, term=None, objects=None, objects_total=None, **extra):
        full = term_objects.get(term, []) if objects is None else objects
        items = full[:EVIDENCE_LIMIT]
        count = len(full) if objects_total is None else objects_total
        reasons.append(dict(code=code, text=text, term=term,
            identities=sorted({r['identity'] for r in items if r.get('identity') is not None}),
            positions=sorted({r['position'] for r in items if r.get('position') is not None}),
            objects=deepcopy(items), objects_total=count, objects_truncated=count > len(items), **extra))

    if blocked:
        conflict_orbits = {row['orbit'] for row in conflicts or []}
        conflict_positions = sorted({int(p) for p in affected if int(model.oid[p]) in conflict_orbits}
                                    | set(block_conflicts or []))
        conflict_count, conflict_scope = len(conflict_positions), 'net'
        if workspace['prefix'] and prefix == 'Violation':
            # The reviewer already inspected the real first violating prefix.
            # A cancelling operation's net support cannot identify these objects.
            conflict_positions = review['prefix'].get('conflicting_positions', [])
            conflict_count = review['prefix'].get('conflicting_positions_total', len(conflict_positions))
            conflict_scope = 'first-prefix'
        reason('protection_conflict', 'The complete operation violates the selected protection policy.',
               objects=[dict(identity=int(before.at[p]), position=p, orbit=int(model.oid[p]),
                             position_kind='protected-fixed') for p in conflict_positions],
               objects_total=conflict_count, objects_scope=conflict_scope,
               orbits=[row['orbit'] for row in conflicts or []], conflict_positions=deepcopy(block_conflicts or []),
               first_prefix_violation=review.get('prefix', {}).get('first_violation'))
    elif not complete:
        missing = [key for key, value in terms.items() if value is None]
        reason('verification_required', 'Required goal, reference or protection evidence is unavailable; no current score is published.',
               missing_terms=missing)
    if focus is not None and focus != 0:
        reason('focus_improved' if focus > 0 else 'focus_lost',
               'The chosen goal becomes satisfied.' if focus > 0 else 'The chosen goal stops being satisfied.', 'F')
    if broken:
        reason('exact_damage', str(len(broken)) + ' previously exact pieces lose their unprotected exact state.', 'D')
    for key, label, delta in (('Gtheta', 'coherent-frame orientation errors', None if theta_before is None or theta_after is None else len(theta_before) - len(theta_after)),
                              ('Gp', 'position errors', len(positions_before) - len(positions_after)),
                              ('B', 'selected block requirements', block_after - block_before)):
        if delta:
            reason(key + '_change', str(abs(delta)) + ' ' + label + (' improve.' if delta > 0 else ' worsen.'), key)
    if not reasons:
        reason('no_direct_gain' if related else 'no_work_relation',
               'This operation affects current work but shows no immediate goal or residual improvement.' if related
               else 'No direct benefit or demonstrated relation to the current work.')
    model.check_cancel()
    return deepcopy(dict(version=CURRENT_SCORE_VERSION, status=status, category=category,
        eligible=complete and not blocked, score=None if score is None else float(score), exact_score=_rational(score),
        review_context_id=context['id'], model=model.model_id, frame_version=FRAME_VERSION,
        use_score_version=USE_SCORE_VERSION, before_state_hash=before.hash, after_state_hash=after.hash,
        recipe_hash=context['recipe_hash'], stable_key=context['recipe_hash'], orbit=orbit,
        terms={key: None if value is None else float(value) for key, value in terms.items()},
        contributions={key: None if value is None else float(value) for key, value in contributions.items()},
        exact_terms={key: _rational(value) for key, value in terms.items()}, counts=counts,
        use_status=use_status, evidence=evidence, reasons=reasons[:3], match=match, related=related,
        scope='Explicit complete operation; heuristic, not execution permission'))


def sort_recommendations(results):
    """Order supplied results only; no candidates are created or evaluated."""
    order = dict(Direct=0, PotentialPreparation=1, Blocked=2, NeedsVerification=3)
    return sorted(deepcopy(results), key=lambda row: (order[row['category']],
        -row['score'] if row['score'] is not None else 0, row['counts']['damage'], row['counts']['L'], row['stable_key']))
