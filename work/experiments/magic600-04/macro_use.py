"""Explainable solving-use inference, separate from exact structure and policy.

Only the adapter's locally computed facts enter this module. Saved groups, names,
tags and imported proof claims never establish use or orientation certification.
"""
from copy import deepcopy
from fractions import Fraction
import numpy as np
from transported_frames import FRAME_VERSION

PROOF_VERSION = 3
USE_SCORE_VERSION = 'static-use-v1'
USE_WEIGHTS = dict(T=Fraction(40, 100), J=Fraction(25, 100),
                   C=Fraction(20, 100), E=Fraction(15, 100))
USE_MINIMUM, USE_BAND, USE_LIMIT = 70, 10, 3


def classify_effect(chart, source, destination):
    """JSON dimensions plus separate exact arrays; never derive zero from a macro."""
    exact = chart.decompose(source, destination)
    model, pi = chart.m, exact['position_map']
    cycles, seen = {}, set()
    for position in exact['affected_positions']:
        if position in seen or int(pi[position]) == position:
            continue
        model.check_cancel()
        current, length = position, 0
        while current not in seen:
            seen.add(current); length += 1; current = int(pi[current])
        if current != position:
            raise ValueError('Complete position permutation contains an inconsistent cycle')
        cycles.setdefault(int(model.oid[position]), []).append(length)
    rows = []
    for actual in exact['orbits']:
        row = {key: deepcopy(value) for key, value in actual.items()
               if key not in ('classification', 'unknown_positions')}
        kind = actual['classification']; lengths = cycles.get(row['orbit'], [])
        row.update(effect_class=kind, unknown_count=len(actual['unknown_positions']),
                   cycle_count=len(lengths), single_cycle_length=lengths[0] if len(lengths) == 1 else None)
        if kind == 'Unknown':
            status, reason = 'Unverified', 'coherent_frame_unavailable'
        elif kind == 'PurePosition':
            status, reason = (('Verified', 'every_edge_matches_fixed_chart') if len(lengths) == 1
                              else ('NotSingleCycle', 'multiple_pure_position_cycles'))
        elif kind == 'PureOrientation':
            status, reason = 'NotPositionCycle', 'pure_orientation_effect'
        else:
            status, reason = 'NotPure', 'mixed_position_orientation'
        row['pure_cycle'] = dict(status=status, reason=reason,
            cycle_length=row['single_cycle_length'] or 0,
            reference=dict(kind='coherent-transported-chart', model=model.model_id, version=FRAME_VERSION))
        rows.append(row)
    model.check_cancel()
    summary = dict(model=model.model_id, frame_version=FRAME_VERSION,
        orientation_complete=exact['orientation_complete'],
        unchanged_orbits=list(exact['unchanged_orbits']), orbit_effects=rows)
    return summary, exact


def retained_references(model, recipe):
    """Independent retained frames of explicitly named stars; no target search."""
    result = {}
    for step in recipe:
        if step['kind'] != 'star':
            continue
        orbit, node = step['orbit'], step['node']
        certificate = model.certify_seed(orbit)
        tree = model.trees[orbit]
        positions = list(tree['buffers']) + [tree['positions'][node]]
        frames = list(model.atlas[orbit]['frames'][:2]) + [tree['frames'][node]]
        item = result.setdefault(orbit, dict(frames={}, conflicts=set(), sources=[]))
        source = dict(orbit=orbit, node=node, seed_sha256=certificate['seed_sha256'])
        if source not in item['sources']:
            item['sources'].append(source)
        for position, slots in zip(positions, frames):
            position, slots = int(position), list(map(int, slots))
            if sorted(slots) != sorted(map(int, model.slots(position))):
                raise ValueError('Retained frame does not contain the exact position slots')
            if position in item['frames'] and item['frames'][position] != slots:
                item['conflicts'].add(position)
            item['frames'][position] = slots
    return result


def scoped_stars(model, source, destination, recipe):
    """Recognize only explicit stars whose complete orbit-local action survives."""
    result, seen = [], set()
    for index, step in enumerate(recipe):
        if step['kind'] != 'star':
            continue
        orbit, node, sign = step['orbit'], step['node'], step['sign']
        key = (orbit, node, sign)
        if key in seen:
            continue
        seen.add(key)
        model.check_cancel()
        expected_source, expected_destination = model.star_net(orbit, node, sign)
        expected_mask = model.so[expected_source] == orbit
        actual_mask = model.so[source] == orbit
        expected_source, expected_destination = expected_source[expected_mask], expected_destination[expected_mask]
        actual_source, actual_destination = source[actual_mask], destination[actual_mask]
        expected_order, actual_order = np.argsort(expected_source), np.argsort(actual_source)
        if (not np.array_equal(actual_source[actual_order], expected_source[expected_order])
                or not np.array_equal(actual_destination[actual_order], expected_destination[expected_order])):
            continue
        certificate = model.certify_seed(orbit)
        tree = model.trees[orbit]
        result.append(dict(orbit=orbit, node=node, sign=sign, source_step=index,
            a=int(tree['buffers'][0]), b=int(tree['buffers'][1]),
            target=int(tree['positions'][node]), frame=list(map(int, tree['frames'][node])),
            status='Verified', scope='Exact orbit-local equality to the explicitly named retained star',
            reference=dict(kind='retained-star', version=1, model=model.model_id,
                           seed_sha256=certificate['seed_sha256'])))
    return result


def checked(facts):
    return (isinstance(facts, dict) and facts.get('proof_version') == PROOF_VERSION
            and isinstance(facts.get('model'), str) and bool(facts['model'])
            and type(facts.get('slots')) is int and isinstance(facts.get('support'), list)
            and isinstance(facts.get('orbit_effects'), list))


def _score(kind, count, total, length):
    t = Fraction(1) if kind in ('PurePosition', 'PureOrientation') else Fraction(1, 2) if kind == 'Mixed' else None
    terms = dict(T=t, J=Fraction(count, total), C=Fraction(4, count + 3), E=Fraction(30, length + 30))
    points = {key: None if value is None else 100 * USE_WEIGHTS[key] * value for key, value in terms.items()}
    score = None if t is None else sum(points.values())
    detail = {key: None if value is None else float(value) for key, value in terms.items()}
    detail.update(n_o=count, N=total, L=length,
                  points={key: None if value is None else float(value) for key, value in points.items()})
    return score, detail


def infer_use(facts):
    """Versioned prescribed heuristic; exact thresholds precede display rounding."""
    result = dict(status='Unchecked', memberships=[], other=False, candidates=[],
                  reasons=['exact_effect_not_checked'],
                  score_version=USE_SCORE_VERSION, frame_version=FRAME_VERSION,
                  basis='Likely solving use; not proof of intention or execution safety')
    if (not checked(facts) or facts.get('frame_version') != FRAME_VERSION
            or type(facts.get('primitives')) is not int or facts['primitives'] < 0
            or type(facts.get('orientation_complete')) is not bool):
        return result
    support, dimensions = {}, {}
    for row in facts['support']:
        if (not isinstance(row, dict) or type(row.get('orbit')) is not int or not 0 <= row['orbit'] < 35
                or row['orbit'] in support or type(row.get('pieces')) is not int or row['pieces'] < 1):
            return result
        support[row['orbit']] = row['pieces']
    for row in facts['orbit_effects']:
        if (not isinstance(row, dict) or row.get('orbit') not in support or row['orbit'] in dimensions
                or type(row.get('affected_count')) is not int or row['affected_count'] != support[row['orbit']]
                or row.get('effect_class') not in ('PurePosition', 'PureOrientation', 'Mixed', 'Unknown')):
            return result
        dimensions[row['orbit']] = row
    if set(dimensions) != set(support):
        return result
    result.update(status='Checked', reasons=[])
    if not support:
        result.update(other=True, reasons=['identity_effect'])
        return result
    total = sum(support.values()); ranked = []
    complete = facts['orientation_complete'] and all(row['effect_class'] != 'Unknown' for row in dimensions.values())
    for orbit in sorted(support):
        kind = dimensions[orbit]['effect_class']
        score, terms = _score(kind, support[orbit], total, facts['primitives'])
        reason = {'PurePosition': 'pure_position_effect', 'PureOrientation': 'pure_orientation_effect',
                  'Mixed': 'mixed_position_orientation', 'Unknown': 'coherent_frame_unavailable'}[kind]
        row = dict(orbit=orbit, kind='scored', effect_class=kind,
                   score=None if score is None else float(score), contributions=terms, reasons=[reason])
        ranked.append((score, row))
    ranked.sort(key=lambda item: (item[0] is None, -item[0] if item[0] is not None else 0, item[1]['orbit']))
    best = ranked[0][0]
    if not complete:
        result.update(status='AwaitingVerification', reasons=['coherent_frame_unavailable'])
    for score, row in ranked:
        if not complete:
            reason = 'awaiting_reference_for_macro'
        elif score < USE_MINIMUM:
            reason = 'score_below_threshold'
        elif score < best - USE_BAND:
            reason = 'outside_best_band'
        elif len(result['memberships']) >= USE_LIMIT:
            reason = 'membership_limit'
        else:
            row['reasons'].append('use_score_qualifies')
            result['memberships'].append(row)
            continue
        row['reasons'].append(reason)
        result['candidates'].append(row)
    if complete and not result['memberships']:
        result.update(other=True, reasons=['no_score_qualifies'])
    return result


def use_context(facts, active_orbit, protected, completed):
    """Current body-level conditions; never revise intrinsic use membership."""
    result = dict(status='Unchecked', active_orbit=active_orbit,
        body_protected_orbits=[], body_completed_orbits=[], candidates=[],
        scope='Macro body only; review complete Prepare / Macro / Cleanup')
    if not checked(facts):
        return result
    support = sorted(row['orbit'] for row in facts['support'])
    protected, completed = set(protected), set(completed)
    unfinished = set(support) - completed
    result.update(status='Checked', body_protected_orbits=sorted(set(support) & protected),
                  body_completed_orbits=sorted(set(support) & completed))
    for orbit in support:
        reasons = ['affected_orbit']
        if orbit == active_orbit:
            reasons.append('current_work_orbit')
        reasons.append('orbit_complete' if orbit in completed else 'orbit_unfinished')
        if orbit in protected:
            reasons.append('body_conflicts_with_protection')
        if unfinished == {orbit}:
            reasons.append('only_unfinished_affected_orbit')
        result['candidates'].append(dict(orbit=orbit, reasons=reasons))
    return result
