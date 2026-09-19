"""Read-only comparison of explicitly supplied, existing macro revisions.

The shared Session stays authoritative. Only the alternate Macro phase lives in
a detached work context; every row uses the existing complete review pipeline.
The optional cache is one prior local result, never a client-supplied proof.
"""
from collections import OrderedDict
from copy import copy, deepcopy

from core import canonical, digest
from current_recommendation import CURRENT_SCORE_VERSION, sort_recommendations
from macro_use import PROOF_VERSION, USE_SCORE_VERSION

VERSION = 'fixed-phase-candidates-v1'
MAX_CANDIDATES = 12


def analyse_candidates(wb, candidates, cached=None):
    """Return analysis only. Any stale input or cancellation discards the batch.

    Callers may retain only the last returned batch as ``cached``. They must not
    pass request JSON as cached evidence or treat any row as execution authority.
    """
    with wb.lock:
        wb.m.check_cancel()
        if not isinstance(candidates, list) or not 1 <= len(candidates) <= MAX_CANDIDATES:
            raise ValueError('Choose between 1 and 12 existing macro revisions')
        bound = []
        for supplied in candidates:
            record = wb.bound_macro(supplied, binding_required=True)
            if any(item['id'] == record['id'] for item in bound):
                raise ValueError('Choose each macro identity only once in a candidate batch')
            bound.append({key: deepcopy(record[key]) for key in ('id', 'version', 'recipe')})
        base = wb.review_context()
        key = digest(canonical(dict(version=VERSION, context=base, candidates=bound,
            score_version=CURRENT_SCORE_VERSION, use_version=USE_SCORE_VERSION,
            effect_version=PROOF_VERSION)).encode())

        def current():
            wb.m.check_cancel()
            if wb.review_context()['id'] != base['id']:
                raise ValueError('Candidate analysis work changed; inspect the current work again')
            for binding in bound:
                wb.bound_macro(binding, binding_required=True)

        current()
        if (isinstance(cached, dict) and cached.get('version') == VERSION
                and cached.get('cache_key') == key):
            return deepcopy(cached)

        scratch = copy(wb)
        scratch.w = deepcopy(wb.w)
        scratch.review = scratch.execution = scratch.residual_cache = None
        scratch.completed_operations = dict(wb.completed_operations)
        scratch.completed_operations.pop(scratch.w['orbit'], None)
        # Review may evict effects and update their scalar byte counter. Keep
        # both containers local, while reusing immutable existing exact facts.
        scratch.effects = OrderedDict(wb.effects)
        scratch.effect_facts = OrderedDict(wb.effect_facts)
        rows = []
        for binding in bound:
            current()
            scratch.review = scratch.execution = scratch.residual_cache = None
            recipe = deepcopy(binding['recipe'])
            sources = [dict(deepcopy(binding), start=0, count=len(recipe))]
            scratch.replace_phase('macro', recipe, sources)
            scratch.review_draft(response_snapshot=False)
            current()
            review = scratch.review['public']
            rows.append(dict(candidate=deepcopy(binding), review_context=review['review_context'],
                recommendation=review['recommendation'], effect=review['effect'],
                policy={field: review[field] for field in ('status', 'conflicts', 'block_conflicts', 'prefix')},
                goal_result=review['goal_result'], post_hash=review['post_hash']))
        # Use canonical library identity only for the final exact-action tie.
        # Preserve the recommendation's own evidence key in each returned row.
        ranked = sort_recommendations([dict(row['recommendation'], candidate_id=row['candidate']['id'],
            stable_key=row['recommendation']['stable_key'] + '|' + row['candidate']['id'])
                                       for row in rows])
        result = dict(version=VERSION, model=wb.m.model_id, base_review_context_id=base['id'],
            base_review_context=base, cache_key=key,
            fixed_phases={phase: deepcopy(wb.w['draft'][phase]) for phase in ('prepare', 'cleanup')},
            basis='Current Prepare and Cleanup with each explicitly supplied alternate Macro body; not the current draft or execution permission',
            candidates=rows, order=[row['candidate_id'] for row in ranked])
        current()
        return deepcopy(result)
