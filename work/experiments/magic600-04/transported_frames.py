"""Exact orientation coordinates in one versioned, immutable transported chart.

The chart never depends on a current macro, puzzle state or chosen Grip. It does
not find setup words, prove execution permission or own any Session state.
"""
from collections import defaultdict
from copy import deepcopy

import numpy as np

from core import cp, digest

FRAME_VERSION = 'retained-first-frame-v1'


class FrameUnavailable(ValueError):
    """Known slot action has no reliable independent orientation comparison."""


class TransportedFrames:
    def __init__(self, model):
        self.m = model
        self._zero = np.full(model.n, -1, np.int32)
        self._index = np.full(model.n, -1, np.int16)
        self._groups = {}
        self._certificates = {}

    def ensure(self, orbit):
        """Check an orbit completely before publishing its immutable chart."""
        if type(orbit) is not int or not 0 <= orbit < 35:
            raise ValueError('A frame chart requires a canonical moving orbit')
        self.m.check_cancel()
        if orbit not in self._certificates:
            self._build(orbit)
        return deepcopy(self._certificates[orbit])

    def _build(self, orbit):
        model, tree = self.m, self.m.trees[orbit]
        seed = model.certify_seed(orbit)
        width = model.census['orbits'][orbit]['colors']
        order = model.census['orbits'][orbit]['orientation_order']
        frames = np.asarray(tree['frames'], np.int32)
        positions = np.asarray(tree['positions'], np.int32)
        parents = np.asarray(tree['parent'], np.int32)
        moves = np.asarray(tree['move'], np.int32)
        if (positions.ndim != 1 or not len(positions) or frames.shape != (len(positions), width)
                or parents.shape != positions.shape or moves.shape != positions.shape
                or np.any(positions < 0) or np.any(positions >= model.np)
                or np.any(frames < 0) or np.any(frames >= model.n)):
            raise FrameUnavailable('Retained frame arrays have missing or invalid entries')
        if (np.any(model.oid[positions] != orbit) or np.any(model.sp[frames] != positions[:, None])
                or np.any(np.diff(np.sort(frames, axis=1), axis=1) <= 0)
                or parents[0] != -1 or np.any(parents[1:] < 0)
                or np.any(parents[1:] >= np.arange(1, len(parents)))
                or tree['frames'][0] != model.atlas[orbit]['frames'][2]):
            raise FrameUnavailable('Retained frame incidence or root transport is inconsistent')
        buffers = tree['buffers']
        if (len(buffers) != 2 or len(set(buffers)) != 2
                or any(type(p) is not int or not 0 <= p < model.np or model.oid[p] != orbit for p in buffers)
                or np.any(np.isin(positions, buffers))):
            raise FrameUnavailable('Retained buffer frame positions are inconsistent')
        guard = set(model.caps(buffers[0]) + model.caps(buffers[1]))
        for move in np.unique(moves[1:]):
            model.check_cancel()
            if not 1 <= abs(int(move)) <= 1200 or (abs(int(move)) - 1) // 2 in guard:
                raise FrameUnavailable('Retained frame edge is not a guarded legal transport')
            nodes = np.flatnonzero((moves == move) & (parents >= 0))
            actual = model.mapped(int(move), frames[parents[nodes]].ravel()).reshape(-1, width)
            if not np.array_equal(actual, frames[nodes]):
                raise FrameUnavailable('Retained frame edge conflicts with its exact legal transport')
        selected_positions, first = np.unique(positions, return_index=True)
        selected_positions = np.concatenate((selected_positions, np.asarray(buffers, np.int32)))
        selected_frames = np.concatenate((frames[first], np.asarray(model.atlas[orbit]['frames'][:2], np.int32)))
        sorting = np.argsort(selected_positions)
        selected_positions, selected_frames = selected_positions[sorting], selected_frames[sorting]
        expected = np.flatnonzero(model.oid == orbit)
        if (not np.array_equal(selected_positions, expected)
                or np.any(model.sp[selected_frames] != selected_positions[:, None])
                or np.any(np.diff(np.sort(selected_frames, axis=1), axis=1) <= 0)):
            raise FrameUnavailable('Independent frames do not cover every orbit position exactly')
        # Build privately. Cancellation/failure cannot publish half an orbit.
        local_index = np.full(model.n, -1, np.int16)
        local_index[selected_frames] = np.arange(width)
        by_position = defaultdict(set)
        relative = local_index[frames]
        for node, (position, element) in enumerate(zip(positions, relative)):
            if node % 512 == 0:
                model.check_cancel()
            by_position[int(position)].add(tuple(map(int, element)))
        group = by_position[int(positions[0])]
        if (len(group) != order or tuple(range(width)) not in group
                or any(elements != group for elements in by_position.values())
                or any(cp(a, b) not in group for a in group for b in group)):
            raise FrameUnavailable('Retained frames do not realize one coherent census orientation group')
        certificate = dict(status='Verified', model=model.model_id, frame_version=FRAME_VERSION,
            convention='Atlas A/B; first retained tree frame at every other fixed position',
            position_count=len(selected_positions), parent_edges=len(positions) - 1,
            group_order=order, orientation_group=model.census['orbits'][orbit]['orientation_group'],
            seed_sha256=seed['seed_sha256'],
            frame_sha256=digest(selected_frames.astype('<i4').tobytes()))
        model.check_cancel()
        destinations = model.fo[selected_positions, None] + np.arange(width)
        self._zero[destinations] = selected_frames
        self._index[selected_frames] = np.arange(width)
        self._groups[orbit] = frozenset(group)
        self._certificates[orbit] = certificate

    def frame(self, position):
        """Detached ordered canonical slots of one fixed position."""
        if type(position) is not int or not 0 <= position < self.m.np:
            raise ValueError('Frame position must be a canonical integer')
        orbit = int(self.m.oid[position])
        if orbit < 0:
            return self.m.slots(position).tolist()
        if orbit not in self._certificates:
            self.ensure(orbit)
        return self._zero[self.m.fo[position]:self.m.fo[position+1]].tolist()

    def finite_group(self, orbit):
        """Immutable exact permutations of this orbit's certified frame group."""
        self.ensure(orbit)
        return tuple(sorted(self._groups[orbit]))

    def _slots(self, value):
        if isinstance(value, (list, tuple)) and any(isinstance(item, (bool, np.bool_)) for item in value):
            raise ValueError('A slot mapping cannot use Boolean values as canonical integers')
        try:
            array = np.asarray(value)
        except (TypeError, ValueError) as error:
            raise ValueError('A slot mapping must be a one-dimensional integer array') from error
        if array.ndim != 1 or (array.size and array.dtype.kind not in 'iu'):
            raise ValueError('A slot mapping must be a one-dimensional integer array')
        if np.any(array < 0) or np.any(array >= self.m.n):
            raise ValueError('A slot mapping contains an out-of-range canonical slot')
        return array.astype(np.int32, copy=False)

    def decompose(self, source, destination):
        """Return exact full position map and sparse orientation increments.

        The position map is source-to-destination. When the input action is the
        actual state's Home-to-Current transport, a Current-to-Home residual
        graph must invert it. Absent orientation_elements mean identity only
        outside orientation_unknown. These are internal arrays, not UI indexes.
        """
        model = self.m
        model.check_cancel()
        source, destination = self._slots(source), self._slots(destination)
        if (len(source) != len(destination) or len(np.unique(source)) != len(source)
                or not np.array_equal(np.sort(source), np.sort(destination))):
            raise ValueError('Slot source/destination pairs must define a complete bijective permutation')
        action = model.ids.copy(); action[source] = destination
        pi = model.sp[action[model.first]].copy()
        if not np.array_equal(model.sp[action], pi[model.sp]):
            raise ValueError('Slot action would split a physical piece')
        if not np.array_equal(model.oid[pi], model.oid):
            raise ValueError('Slot action crosses a legal piece orbit')
        if np.any(pi[model.center_piece] != model.center_piece):
            raise ValueError('Slot action moves an immutable fixed center')
        changed_slots = source[action[source] != source]
        affected = np.unique(model.sp[changed_slots])
        affected_orbits = sorted(set(map(int, model.oid[affected])))
        elements, unknown, rows, certificates = {}, [], [], []
        for orbit in affected_orbits:
            model.check_cancel()
            positions = affected[model.oid[affected] == orbit]
            moved = pi[positions] != positions
            reason, known_count, known_overlap = None, 0, 0
            try:
                certificate = self.ensure(orbit)
            except FrameUnavailable as error:
                missing = positions.tolist()
                reason = str(error)
            else:
                certificates.append(certificate)
                width = model.census['orbits'][orbit]['colors']
                frames = self._zero[model.fo[positions, None] + np.arange(width)]
                increments = self._index[action[frames]]
                missing = []
                for index, (position, increment) in enumerate(zip(positions, increments)):
                    if index % 512 == 0:
                        model.check_cancel()
                    element = tuple(map(int, increment)); position = int(position)
                    if element not in self._groups[orbit]:
                        missing.append(position)
                        reason = 'Exact orientation transport is outside the certified orientation group'
                    elif element != tuple(range(width)):
                        elements[position] = list(element)
                        known_count += 1
                        known_overlap += int(pi[position] != position)
            unknown.extend(missing)
            position_count = int(np.count_nonzero(moved))
            classification = ('Unknown' if missing else 'Mixed' if position_count and known_count
                else 'PurePosition' if position_count else 'PureOrientation' if known_count else 'NoEffect')
            rows.append(dict(orbit=orbit, classification=classification,
                position_count=position_count, orientation_count=None if missing else known_count,
                affected_count=len(positions), overlap_count=None if missing else known_overlap,
                known_orientation_count=known_count, known_overlap_count=known_overlap,
                unknown_positions=missing, reason=reason,
                frame_version=FRAME_VERSION, model=model.model_id))
        model.check_cancel()
        return dict(model=model.model_id, frame_version=FRAME_VERSION, position_map=pi,
            position_map_scope='Canonical source position to destination position',
            affected_positions=affected.tolist(), changed_slot_count=len(changed_slots),
            orientation_elements=elements, orientation_unknown=sorted(unknown),
            orientation_complete=not unknown, orbits=rows,
            unchanged_orbits=[o for o in range(35) if o not in affected_orbits],
            frame_certificates=certificates)
