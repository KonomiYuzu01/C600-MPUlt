"""Explicit proper cap-local Grip frames for the isolated G1 proposal.

This helper owns no Session, capture storage or input state. Frame selection is
not a puzzle move, piece orientation, star frame or automatic role transport.
"""
import numpy as np
from weakref import WeakKeyDictionary

from core import invword
from grips import grips

_ROTATIONS = WeakKeyDictionary()


def rotation_table(model):
    """Only immutable model geometry is shared, never Session-derived state."""
    if model not in _ROTATIONS:
        rotations = model.z['rotperms']
        rotations.setflags(write=False)
        _ROTATIONS[model] = rotations
    return _ROTATIONS[model]


def _cell(cell):
    if type(cell) is not int or not 1 <= cell <= 600:
        raise ValueError('Grip cap must be a canonical integer C1..C600')
    return cell - 1


def _normal_word(rotations, word):
    permutation = np.arange(600, dtype=np.int32)
    for move in word:
        rotation = rotations[abs(move) - 1]
        permutation = (np.argsort(rotation) if move < 0 else rotation)[permutation]
    return permutation


def _vertex_map(model, cap, permutation):
    vertices = model.cell_vertices[cap]
    incidence = {frozenset(model.vertex_cells[v]): v for v in vertices}
    try:
        return {v: incidence[frozenset(int(permutation[c]) for c in model.vertex_cells[v])]
                for v in vertices}
    except KeyError as error:
        raise ValueError('Rotation does not preserve this cap vertex incidence') from error


def _table(model, cell):
    cap = _cell(cell)
    rotations = rotation_table(model)
    h, t = 2 * cap + 1, 2 * cap + 2
    hp = _vertex_map(model, cap, _normal_word(rotations, [h]))
    tp = _vertex_map(model, cap, _normal_word(rotations, [t]))
    fixed = [v for v in tp if tp[v] == v]
    if len(fixed) != 1:
        raise ValueError('Retained T generator must fix exactly one cap vertex')
    # The retained H/T actions anchor the order, never sorted vertex IDs.
    v4 = fixed[0]
    v1 = hp[v4]
    v3 = tp[v1]
    v2 = tp[v3]
    base = [v1, v2, v3, v4]
    if len(set(base)) != 4 or [hp[v] for v in base] != [v4, v3, v2, v1] or tp[v2] != v1:
        raise ValueError('Retained H/T actions do not define the expected proper tetrahedral frame')
    retained = grips(model, cap, rotations=rotations)
    words = [[]]
    for axis in retained['axes']:
        words.append(axis['word'])
        if axis['order'] == 3:
            words.append(axis['inverse']['word'])
    records = []
    for word in words:
        mapped = _vertex_map(model, cap, _normal_word(rotations, word))
        records.append(dict(vertices=[mapped[v] + 1 for v in base], frame_word=list(word)))
    if len(records) != 12 or len({tuple(r['vertices']) for r in records}) != 12:
        raise ValueError('Retained cap rotations do not provide twelve distinct proper frames')
    return base, retained, records, rotations


def frames(model, cell):
    """Return twelve explicit one-based vertex orders and legal frame witnesses."""
    base, retained, records, _ = _table(model, cell)
    return dict(model=model.model_id, cell=cell, vertex_id_base=1,
                base_vertices=[v + 1 for v in base], frames=records,
                basis=retained['basis'], scope='Twelve proper cap-local ordered frames')


def notation(model):
    """Corner cycles derived from retained vertex actions, not axis-name guesses."""
    base, retained, _, rotations = _table(model, 1)
    def describe(word):
        mapped = _vertex_map(model, 0, _normal_word(rotations, word))
        permutation = [base.index(mapped[v]) for v in base]
        seen, cycles = set(), []
        for start in range(4):
            if start in seen or permutation[start] == start:
                continue
            cycle, q = [], start
            while q not in seen:
                seen.add(q); cycle.append('abcd'[q]); q = permutation[q]
            cycles.append('(' + ''.join(cycle) + ')')
        return permutation, ''.join(cycles)
    result = []
    for axis in retained['axes']:
        p, text = describe(axis['word'])
        inverse, reverse = describe(axis['inverse']['word'])
        result.append(dict(axis=axis['label'], forward=text, inverse=reverse,
                           permutation=p, inverse_permutation=inverse,
                           fixed=['abcd'[i] for i in range(4) if p[i] == i],
                           order=axis['order'], symbols=['a', 'b', 'c', 'd']))
    return result


def resolve(model, cell, vertices, axis, inverse):
    """Verify the requested frame action and emit its equal retained legal word.

    Full-action equality is checked on every resolution. Prefix protection must
    inspect the emitted word; equality does not equate intermediate motion.
    """
    _cell(cell)
    if not isinstance(vertices, (list, tuple)) or len(vertices) != 4 or any(type(v) is not int for v in vertices):
        raise ValueError('Grip frame needs four explicit canonical integer vertex IDs')
    if type(inverse) is not bool:
        raise ValueError('Twist inverse must be an explicit Boolean')
    _, retained, records, rotations = _table(model, cell)
    frame = next((r for r in records if r['vertices'] == list(vertices)), None)
    if frame is None:
        raise ValueError('Frame is not a proper ordered frame of the selected cap')
    selected = next((a for a in retained['axes'] if a['label'] == axis), None)
    if selected is None:
        raise ValueError('Twist must name H1/H2/H3 or T1/T2/T3/T4')
    original_word = (selected['inverse'] if inverse else selected)['word']
    conjugating_word = invword(frame['frame_word']) + list(original_word) + frame['frame_word']
    desired = _normal_word(rotations, conjugating_word)
    matches = []
    for candidate in retained['axes']:
        for backward in ([False, True] if candidate['order'] == 3 else [False]):
            word = (candidate['inverse'] if backward else candidate)['word']
            if np.array_equal(desired, _normal_word(rotations, word)):
                matches.append((candidate, backward, list(word)))
    if len(matches) != 1:
        raise ValueError('Framed action does not have one exact retained cap correspondence')
    matched, backward, emitted = matches[0]
    source, destination = model.word_net(conjugating_word)
    actual_source, actual_destination = model.word_net(emitted)
    if not np.array_equal(source, actual_source) or not np.array_equal(destination, actual_destination):
        raise ValueError('Retained word differs from the complete framed label action')
    return dict(model=model.model_id, cell=cell, vertices=list(vertices),
                frame_word=list(frame['frame_word']), axis=axis, inverse=inverse,
                word=emitted, conjugating_word=conjugating_word,
                retained_axis=matched['label'], retained_inverse=backward, order=matched['order'],
                proof=dict(scope='Complete labelled-slot action', labels=model.n, full_action_equal=True),
                prefix_semantics='Check prefixes of word, the actual emitted witness. '
                                 'The conjugating witness has equal net action, not certified equal intermediate motion.')
