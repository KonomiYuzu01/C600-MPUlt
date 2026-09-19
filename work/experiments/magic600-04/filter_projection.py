"""Detached Piece Filter observations; no change to rules or input authority."""
from copy import deepcopy

import numpy as np

from core import Filters, canonical, digest


class PieceFilterProjection:
    """Evaluate retained rules once per distinct state within one locked reply."""
    def __init__(self, workbench):
        self.m = workbench.m
        session = workbench.s
        prefs = session.prefs
        preview = [] if session.pending is None else np.unique(self.m.sp[session.pending['src']]).tolist()
        self.context = deepcopy(dict(model=self.m.model_id, orbit=prefs['orbit'],
            selected=prefs.get('selected'), protected=prefs['protected'], preview=preview,
            sets=prefs.get('named_sets', {}), rules=prefs['rules']))
        self.context_hash = digest(canonical(self.context).encode())
        self.source_hash = session.st.hash
        self.states = {}

    def evaluate(self, state):
        if state.hash not in self.states:
            context = self.context
            rules = context['rules']
            expression = (rules[0].get('expr') if isinstance(rules, list) and len(rules) == 1
                          and isinstance(rules[0], dict) and rules[0].get('style') == 'solid' else None)
            result = dict(status='evaluated', expression=expression, rules=deepcopy(rules),
                context_hash=self.context_hash, source_hash=self.source_hash,
                state_hash=state.hash, reason=None, matched_pieces=None, matched_stickers=None)
            try:
                styles = Filters(state, context['orbit'], context['selected'],
                    context['protected'], context['preview'], context['sets']).styles(rules, False)
                matched = styles[self.m.first] != 0
                result.update(matched_pieces=int(matched.sum()),
                              matched_stickers=int(self.m.k[matched].sum()))
            except (ValueError, TypeError, KeyError, IndexError) as error:
                matched = None
                result.update(status='unavailable', reason=str(error))
            self.states[state.hash] = matched, result
        return self.states[state.hash]

    def metadata(self, state):
        return deepcopy(self.evaluate(state)[1])

    def record(self, record, state):
        if record is None:
            return None
        matched, _ = self.evaluate(state)
        return dict(record, filter_match=None if matched is None else bool(matched[record['position']]))
