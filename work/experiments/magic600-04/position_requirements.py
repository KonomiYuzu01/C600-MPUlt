"""Captured fixed-position predicates; missing mode preserves legacy exactness."""
import numpy as np


def mode(value='exact'):
    if value not in ('exact', 'position'):
        raise ValueError('Requirement mode must be exact or position')
    return value


def prepare(model, member):
    p, labels = member.get('position'), member.get('labels')
    kind = mode(member.get('mode', 'exact'))
    if type(p) is not int or not 0 <= p < model.np:
        raise ValueError('A captured requirement needs a canonical position')
    slots = model.slots(p)
    if (not isinstance(labels, list) or len(labels) != len(slots) or
            any(type(label) is not int or not 0 <= label < model.n for label in labels) or
            len(set(labels)) != len(labels)):
        raise ValueError('A captured requirement needs all unique labels of one piece')
    identity = int(model.sp[labels[0]])
    if (any(model.sp[label] != identity for label in labels) or model.oid[identity] != model.oid[p] or
            'identity' in member and (type(member['identity']) is not int or member['identity'] != identity)):
        raise ValueError('Captured labels, piece identity and position orbit do not agree')
    return kind, slots, np.asarray(labels, dtype=np.int32), identity


def holds(model, labels, prepared):
    kind, slots, required, identity = prepared
    actual = labels[slots]
    return bool(np.all(model.sp[actual] == identity) if kind == 'position'
                else np.array_equal(actual, required))
