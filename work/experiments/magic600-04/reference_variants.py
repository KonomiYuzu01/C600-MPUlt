"""Explicit proper-cell reference transport into verified legal finite words.

The geometric reference is a relabelling, not an executable setup. No Session,
target search, macro selection or execution permission belongs to this helper.
"""
from collections import OrderedDict
from copy import deepcopy

import numpy as np

from core import canonical, digest, invword
from grip_frames import frames, rotation_table

REFERENCE_VERSION = 'exact-incidence-reference-v1'
MAX_PRIMITIVES = 3000000
WORD_LIMIT = 100000


class ReferenceVariants:
    def __init__(self, model):
        self.model = model
        self._maps = OrderedDict()
        self._base = None
        self._caps = {}
        self._rotations = rotation_table(model)
        self._frames = model.z['frameperms'].astype(np.int32)
        self._inverse_frames = np.argsort(self._frames, axis=1).astype(np.int32)
        self._vertices = {frozenset(cells): v + 1 for v, cells in enumerate(model.vertex_cells)}
        self._manifest_hash = digest(canonical(model.manifest).encode())

    def _frame(self, value):
        if (not isinstance(value, dict) or set(value) != {'cell', 'ordered_vertices'}
                or type(value['cell']) is not int or not 1 <= value['cell'] <= 600):
            raise ValueError('Choose an explicit cell C1..C600 and ordered four-corner frame')
        vertices = value['ordered_vertices']
        if (not isinstance(vertices, list) or len(vertices) != 4
                or any(type(v) is not int for v in vertices)
                or not any(vertices == row['vertices'] for row in frames(self.model, value['cell'])['frames'])):
            raise ValueError('Choose one of this cell\'s twelve proper ordered frames; mirrored or foreign corners are not allowed')
        return deepcopy(value)

    def _vertex_image(self, permutation, vertices):
        try:
            return [self._vertices[frozenset(map(int, permutation[self.model.vertex_cells[v - 1]]))]
                    for v in vertices]
        except KeyError as error:
            raise ValueError('Reference does not preserve the complete vertex incidence') from error

    def _cap_words(self, cap):
        if cap in self._caps:
            return self._caps[cap]
        model = self.model; identity = np.arange(600, dtype=np.int32)
        h, t = 2 * cap + 1, 2 * cap + 2
        letters = ((h, self._rotations[h - 1]), (t, self._rotations[t - 1]),
                   (-t, np.argsort(self._rotations[t - 1])))
        queue, words = [identity], {identity.tobytes(): []}
        for permutation in queue:
            model.check_cancel()
            for letter, action in letters:
                candidate = action[permutation].astype(np.int32)
                key = candidate.tobytes()
                if key not in words:
                    words[key] = words[permutation.tobytes()] + [letter]
                    queue.append(candidate)
            if len(words) > 12:
                raise ValueError('Retained cap actions exceed the proper tetrahedral rotation group')
        if len(words) != 12:
            raise ValueError('Retained cap actions do not provide all twelve proper rotations')
        model.check_cancel()
        self._caps[cap] = (queue, words)
        return queue, words

    def _base_regions(self):
        if self._base is not None:
            return self._base
        model = self.model
        def key(position, permutation=None):
            host, caps = model.hosting(int(position)), model.caps(int(position))
            if permutation is not None:
                host, caps = permutation[host].tolist(), permutation[caps].tolist()
            return tuple(sorted(host)), tuple(sorted(caps))
        regions = {key(p): r for r, p in enumerate(model.sp[:433])}
        if len(regions) != 433:
            raise ValueError('Reference regions do not have unique exact Host/M incidences')
        rotations, _ = self._cap_words(0)
        result = {}
        for rotation in rotations:
            model.check_cancel()
            try:
                values = np.array([regions[key(p, rotation)] for p in model.sp[:433]], dtype=np.int32)
            except KeyError as error:
                raise ValueError('A proper reference rotation does not preserve actual region incidence') from error
            if len(np.unique(values)) != 433:
                raise ValueError('A proper reference rotation is not a 433-region bijection')
            values.setflags(write=False)
            result[rotation.tobytes()] = values
        model.check_cancel()
        self._base = result
        return result

    def _mapping(self, source, destination):
        model = self.model; model.check_cancel()
        source, destination = self._frame(source), self._frame(destination)
        key = canonical([source, destination])
        if key in self._maps:
            return self._maps[key]
        local_regions = self._base_regions()
        start = source['cell'] - 1; finish = destination['cell'] - 1
        candidates = []
        for raw in local_regions:
            local = np.frombuffer(raw, dtype=np.int32)
            permutation = self._frames[finish][local[self._inverse_frames[start]]]
            if self._vertex_image(permutation, source['ordered_vertices']) == destination['ordered_vertices']:
                candidates.append(permutation)
        if len(candidates) != 1:
            raise ValueError('The selected ordered references do not determine one proper correspondence')
        permutation = candidates[0].astype(np.int32)
        if permutation[start] != finish or len(np.unique(permutation)) != 600:
            raise ValueError('Reference cell correspondence is not bijective')
        slots = np.empty(model.n, dtype=np.int32)
        for cell in range(600):
            if cell % 32 == 0:
                model.check_cancel()
            local = self._inverse_frames[permutation[cell]][permutation[self._frames[cell]]].astype(np.int32)
            try:
                region_map = local_regions[local.tobytes()]
            except KeyError as error:
                raise ValueError('Reference requires a non-proper local region map') from error
            slots[cell * 433:(cell + 1) * 433] = int(permutation[cell]) * 433 + region_map
        if len(np.unique(slots)) != model.n:
            raise ValueError('Reference is not a complete labelled-slot bijection')
        pieces = model.sp[slots[model.psorted]]
        low = np.minimum.reduceat(pieces, model.fo[:-1])
        high = np.maximum.reduceat(pieces, model.fo[:-1])
        if (not np.array_equal(low, high) or len(np.unique(low)) != model.np
                or not np.array_equal(model.oid, model.oid[low])):
            raise ValueError('Reference splits a physical piece or changes its legal orbit')
        for array in (permutation, slots, low):
            array.setflags(write=False)
        model.check_cancel()
        return dict(key=key, source_frame=source, destination_frame=destination,
                    cells=permutation, slots=slots, positions=low, primitive_words={})

    def _publish(self, mapping):
        self.model.check_cancel()
        self._maps[mapping['key']] = mapping
        self._maps.move_to_end(mapping['key'])
        while len(self._maps) > 4:
            self._maps.popitem(last=False)

    def _summary(self, mapping):
        return dict(model=self.model.model_id, reference_version=REFERENCE_VERSION,
            manifest_sha256=self._manifest_hash, source_frame=deepcopy(mapping['source_frame']),
            destination_frame=deepcopy(mapping['destination_frame']),
            slot_map_sha256=digest(mapping['slots'].astype('<i4').tobytes()),
            labels=self.model.n, positions=self.model.np, cell_bijection=True,
            slot_bijection=True, piece_consistency=True, orbit_preservation=True,
            scope='Exact fixed-position incidence correspondence; not a legal puzzle move')

    def inspect(self, source_frame, destination_frame):
        mapping = self._mapping(source_frame, destination_frame)
        result = self._summary(mapping)
        self._publish(mapping)
        return result

    def _objects(self, mapping, positions):
        if (not isinstance(positions, list) or len(positions) > 256
                or any(type(p) is not int or not 0 <= p < self.model.np for p in positions)
                or len(set(positions)) != len(positions)):
            raise ValueError('Reference inspection needs at most 256 distinct canonical fixed positions')
        return [dict(source_position=p, destination_position=int(mapping['positions'][p]),
            orbit=int(self.model.oid[p]), slots=[dict(source=int(s), destination=int(mapping['slots'][s]))
                                                for s in self.model.slots(p)]) for p in positions]

    def map_positions(self, source_frame, destination_frame, positions):
        mapping = self._mapping(source_frame, destination_frame)
        result = dict(reference=self._summary(mapping), objects=self._objects(mapping, positions))
        self._publish(mapping)
        return result

    def _primitive(self, mapping, move, cache):
        model = self.model; model.check_cancel()
        if move in cache:
            return cache[move]
        positive = abs(move)
        if move < 0:
            word = invword(self._primitive(mapping, positive, cache))
        else:
            g = mapping['cells']; inverse = np.argsort(g)
            desired = g[self._rotations[positive - 1][inverse]].astype(np.int32)
            cap = int(g[(positive - 1) // 2])
            _, choices = self._cap_words(cap)
            if desired.tobytes() not in choices:
                raise ValueError('The transformed primitive has no exact legal cap action')
            word = choices[desired.tobytes()]
        source, destination = model.move(move)
        actual_source, actual_destination = model.word_net(word)
        order = np.argsort(mapping['slots'][source])
        if (not np.array_equal(actual_source, mapping['slots'][source][order])
                or not np.array_equal(actual_destination, mapping['slots'][destination][order])):
            raise ValueError('Legal reference witness differs on the complete labelled-slot action')
        model.check_cancel()
        cache[move] = list(word)
        return cache[move]

    def compile(self, source_record, source_frame, destination_frame, positions=None):
        model = self.model; model.check_cancel()
        if (not isinstance(source_record, dict) or not isinstance(source_record.get('id'), str)
                or not source_record['id'] or type(source_record.get('version')) is not int
                or source_record['version'] < 1 or source_record.get('model', model.model_id) != model.model_id):
            raise ValueError('Reference variant needs the exact macro identity, content version and model')
        normalized, source_cost = model.normalize(source_record.get('recipe'))
        original = deepcopy({k: source_record[k] for k in ('id', 'version', 'recipe')})
        mapping = self._mapping(source_frame, destination_frame)
        objects = self._objects(mapping, [] if positions is None else positions)
        cache = dict(mapping['primitive_words']); emitted, used = [], set()
        for index, move in enumerate(model.expand(normalized)):
            if index % 128 == 0:
                model.check_cancel()
            word = self._primitive(mapping, move, cache)
            if len(emitted) + len(word) > MAX_PRIMITIVES:
                raise ValueError('Compiled reference exceeds the existing 3,000,000-primitive limit; split the source operation')
            emitted.extend(word); used.add(move)
        recipe = [dict(kind='word', moves=emitted[i:i + WORD_LIMIT]) for i in range(0, len(emitted), WORD_LIMIT)]
        model.normalize(recipe)
        source, destination, _, _ = model.net(normalized)
        actual_source, actual_destination, emitted_cost, _ = model.net(recipe)
        order = np.argsort(mapping['slots'][source])
        if (not np.array_equal(actual_source, mapping['slots'][source][order])
                or not np.array_equal(actual_destination, mapping['slots'][destination][order])):
            raise ValueError('Complete emitted macro differs from the explicitly chosen reference transport')
        result = dict(source=original, reference=self._summary(mapping), recipe=recipe,
            source_primitives=source_cost, emitted_primitives=emitted_cost, objects=objects,
            primitive_correspondence=[dict(source=move, emitted=list(cache[move]), full_action_equal=True)
                                      for move in sorted(used)],
            proof=dict(complete_action_equal=True, labels=model.n, support_labels=len(actual_source),
                affected_orbits=sorted(set(map(int, model.so[actual_source]))),
                source_recipe_sha256=digest(canonical(normalized).encode()),
                emitted_recipe_sha256=digest(canonical(recipe).encode()),
                scope='All labelled slots, including every collateral orbit'),
            prefix_semantics='Only net effects correspond. Review the actual emitted word for strict prefix protection; original approval and cost are not inherited.')
        model.check_cancel()
        completed = dict(mapping, primitive_words=cache)
        self._publish(completed)
        return deepcopy(result)
