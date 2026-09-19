"""Necessary orbit invariants in the verified, fixed transported chart.

The explicit generator audit is expensive. Lookup/evaluation never runs it,
loads a persisted certificate, searches for a word, or grants execution safety.
The model/chart are the application's immutable authorities. Certificates are
published together only after every positive primitive has been checked.
"""
from copy import deepcopy
import hashlib
from pathlib import Path
import sys
import time

import numpy as np

from core import canonical, commp, cp, digest, ip
from transported_frames import FRAME_VERSION

PROOF_VERSION = 'orbit-parity-abelianization-v1'
SCOPE = 'Per-orbit necessary invariants only; not reachability, protection, permission or completion.'


class InvariantViolation(ValueError):
    def __init__(self, failure):
        self.failure = deepcopy(failure)
        super().__init__('Primitive invariant verification failed: ' + canonical(failure))


def finite_quotient(group):
    """Derive [H,H] and the normal quotient from actual finite permutations."""
    elements = tuple(sorted(set(group)))
    if not elements:
        raise ValueError('An orientation group cannot be empty')
    width = len(elements[0]); identity = tuple(range(width)); values = set(elements)
    if (not width or identity not in values
            or any(any(type(v) is not int for v in g) or tuple(sorted(g)) != identity for g in elements)
            or any(cp(a, b) not in values for a in elements for b in elements)):
        raise ValueError('Orientation elements must form an exact finite permutation group')
    commutators = {commp(a, b) for a in elements for b in elements}
    derived, pending = {identity}, [identity]
    while pending:
        current = pending.pop()
        for generator in commutators:
            value = cp(current, generator)
            if value not in derived:
                derived.add(value); pending.append(value)
    if (any(ip(d) not in derived for d in derived)
            or any(cp(cp(ip(h), d), h) not in derived for h in elements for d in derived)):
        raise ValueError('Derived subgroup is not normal')
    # Identity coset is always zero; remaining IDs are deterministic internal indexes.
    cosets = [tuple(sorted(derived))]; remaining = values - derived
    while remaining:
        coset = {cp(d, min(remaining)) for d in derived}
        if not coset <= remaining:
            raise ValueError('Normal quotient cosets overlap')
        cosets.append(tuple(sorted(coset))); remaining.difference_update(coset)
    quotient = {g: index for index, coset in enumerate(cosets) for g in coset}
    multiplication = tuple(tuple(quotient[cp(a[0], b[0])] for b in cosets) for a in cosets)
    if (any(quotient[cp(a, b)] != multiplication[quotient[a]][quotient[b]]
            for a in elements for b in elements)
            or any(multiplication[a][b] != multiplication[b][a]
                   for a in range(len(cosets)) for b in range(len(cosets)))):
        raise ValueError('Orientation quotient is not a well-defined abelian group')
    description = dict(elements=elements, derived=tuple(sorted(derived)),
                       cosets=tuple(cosets), multiplication=multiplication)
    return dict(description, derived=frozenset(derived), quotient=quotient,
                sha256=digest(canonical(description).encode()))


def position_parity(position_map, positions):
    """Parity of whole physical-position cycles, never sticker permutation sign."""
    remaining = set(map(int, positions[position_map[positions] != positions]))
    parity = 0
    while remaining:
        first = remaining.pop(); current = int(position_map[first]); length = 1
        while current != first:
            if current not in remaining:
                raise ValueError('Position map does not contain complete disjoint cycles')
            remaining.remove(current); current = int(position_map[current]); length += 1
        parity ^= (length - 1) & 1
    return parity


class OrbitInvariants:
    def __init__(self, model, frames):
        if frames.m is not model:
            raise ValueError('Invariant verifier and frame authority must share one model')
        self.m, self.frames = model, frames
        self._positions = tuple(np.flatnonzero(model.oid == o) for o in range(35))
        self._certificates, self._tables = {}, {}

    @staticmethod
    def _orbit(orbit):
        if type(orbit) is not int or not 0 <= orbit < 35:
            raise ValueError('Invariant orbit must be a canonical moving orbit')

    def _binding(self, frame_certificates):
        model = self.m; combined = hashlib.sha256()
        for name in ('src', 'dst', 'offset', 'sp', 'oid', 'fo', 'first'):
            array = np.asarray(getattr(model, name))
            combined.update(canonical([name, array.dtype.str, array.shape]).encode())
            combined.update(array.tobytes())
        here = Path(__file__).resolve().parent
        paths = (Path(__file__), here / 'transported_frames.py', here.parents[2] / 'core.py')
        return dict(model=model.model_id, frame_version=FRAME_VERSION,
            proof_version=PROOF_VERSION, model_arrays_sha256=combined.hexdigest(),
            manifest_sha256=digest(canonical(model.manifest).encode()),
            sources={str(p.relative_to(here)) if p.is_relative_to(here) else '../../../core.py':
                     digest(p.read_bytes()) for p in paths},
            frames=frame_certificates)

    def _validate(self, decomposition):
        model = self.m
        if not isinstance(decomposition, dict):
            raise ValueError('Invariant input must be an exact frame decomposition')
        pi = np.asarray(decomposition.get('position_map'))
        if (pi.shape != (model.np,) or pi.dtype.kind not in 'iu'
                or np.any(pi < 0) or np.any(pi >= model.np)
                or not np.array_equal(np.sort(pi), model.pids)
                or not np.array_equal(model.oid[pi], model.oid)
                or np.any(pi[model.center_piece] != model.center_piece)):
            raise ValueError('Invariant position map must preserve complete pieces and their orbits')
        unknown = decomposition.get('orientation_unknown')
        elements = decomposition.get('orientation_elements')
        if (not isinstance(unknown, (list, tuple))
                or any(type(p) is not int or not 0 <= p < model.np or model.oid[p] < 0 for p in unknown)
                or len(set(unknown)) != len(unknown) or not isinstance(elements, dict)):
            raise ValueError('Malformed unknown orientation positions or exact orientation elements')
        for position, element in elements.items():
            if (type(position) is not int or not 0 <= position < model.np or model.oid[position] < 0
                    or position in unknown or not isinstance(element, (list, tuple))
                    or any(type(i) is not int for i in element)
                    or sorted(element) != list(range(int(model.k[position])))):
                raise ValueError('Malformed exact orientation permutation')
        if decomposition.get('orientation_complete') is not (not unknown):
            raise ValueError('Orientation completeness contradicts the unknown-position list')
        certificates = decomposition.get('frame_certificates')
        if not isinstance(certificates, list) or any(not isinstance(c, dict) for c in certificates):
            raise ValueError('Frame decomposition needs its certificate bindings')
        return pi, elements, unknown

    def verify_generators(self):
        """Explicit all-1,200 audit; no certificate survives an incomplete attempt."""
        model = self.m; started = time.perf_counter()
        self._certificates, self._tables = {}, {}
        frame_certificates, tables, shared = [], {}, {}
        for orbit in range(35):
            model.check_cancel()
            frame_certificates.append(self.frames.ensure(orbit))
            group = self.frames.finite_group(orbit)
            if group not in shared:
                shared[group] = finite_quotient(group)
            tables[orbit] = shared[group]
        binding = self._binding(frame_certificates)
        rows = []
        for primitive in range(1, 1201):
            model.check_cancel()
            source, destination = model.move(primitive)
            decomposition = self.frames.decompose(source, destination)
            pi, elements, unknown = self._validate(decomposition)
            if unknown:
                raise InvariantViolation(dict(primitive=primitive, reason='Unknown orientation', positions=unknown))
            totals = [0] * 35
            for position, element in elements.items():
                orbit = int(model.oid[position]); table = tables[orbit]
                value = table['quotient'].get(tuple(element))
                if value is None:
                    raise InvariantViolation(dict(primitive=primitive, orbit=orbit,
                                                  position=position, reason='Orientation outside verified group'))
                totals[orbit] = table['multiplication'][totals[orbit]][value]
            parity = [position_parity(pi, p) for p in self._positions]
            for orbit in range(35):
                if parity[orbit] or totals[orbit]:
                    raise InvariantViolation(dict(primitive=primitive, orbit=orbit,
                        position_parity=parity[orbit], orientation_quotient=totals[orbit],
                        moved_positions=self._positions[orbit][pi[self._positions[orbit]] != self._positions[orbit]].tolist(),
                        orientation_elements={p: g for p, g in elements.items() if model.oid[p] == orbit}))
            rows.append(dict(primitive=primitive, position_parity=parity, orientation_quotient=totals))
        model.check_cancel()
        if binding != self._binding([self.frames.ensure(o) for o in range(35)]):
            raise ValueError('Model, implementation or frame content changed during invariant verification')
        binding_hash = digest(canonical(binding).encode())
        candidates = {}
        for orbit in range(35):
            table = tables[orbit]
            candidates[orbit] = dict(status='Verified', model=model.model_id,
                proof_version=PROOF_VERSION, frame_version=FRAME_VERSION, orbit=orbit,
                frame_sha256=frame_certificates[orbit]['frame_sha256'],
                group_order=len(table['elements']), derived_order=len(table['derived']),
                quotient_order=len(table['cosets']), quotient_sha256=table['sha256'],
                position_count=len(self._positions[orbit]), generator_count=1200,
                generators_sha256=digest(canonical(list(range(1, 1201))).encode()),
                checks_sha256=digest(canonical([[r['primitive'], r['position_parity'][orbit],
                                                r['orientation_quotient'][orbit]] for r in rows]).encode()),
                binding_sha256=binding_hash, scope=SCOPE)
        model.check_cancel()
        self._tables, self._certificates = tables, candidates
        return dict(status='passed', passed=True, scope=SCOPE, binding=binding,
            orbits=[deepcopy(candidates[o]) for o in range(35)], generators=rows,
            seconds=time.perf_counter() - started, python=sys.version, numpy=np.__version__,
            reconstruction_scope='Uses the unchanged TransportedFrames decomposition; existing separate full-label reconstruction evidence is not rerun here.')

    def certificate(self, orbit):
        """Cheap in-memory lookup; never loads evidence or starts enumeration."""
        self._orbit(orbit)
        certificate = self._certificates.get(orbit)
        if certificate is None:
            return dict(status='Unavailable', model=self.m.model_id, frame_version=FRAME_VERSION,
                        orbit=orbit, reason='Matching all-generator invariant audit has not completed')
        current = self.frames.ensure(orbit)
        if (certificate['model'] != self.m.model_id or current['model'] != certificate['model']
                or current['frame_version'] != certificate['frame_version']
                or current['frame_sha256'] != certificate['frame_sha256']):
            return dict(status='Unavailable', model=self.m.model_id, frame_version=FRAME_VERSION,
                        orbit=orbit, reason='Invariant certificate no longer matches the frame or model')
        return deepcopy(certificate)

    def evaluate(self, decomposition, orbit):
        """Read exact action coordinates; this does not establish legal provenance."""
        self._orbit(orbit)
        pi, elements, unknown = self._validate(decomposition)
        result = dict(status='Unavailable', model=self.m.model_id, frame_version=FRAME_VERSION,
                      proof_version=PROOF_VERSION, orbit=orbit, scope=SCOPE)
        certificate = self.certificate(orbit)
        if (decomposition.get('model') != self.m.model_id
                or decomposition.get('frame_version') != FRAME_VERSION
                or certificate['status'] != 'Verified'):
            return dict(result, reason='Missing or stale model/frame-bound invariant evidence')
        if any(self.m.oid[p] == orbit for p in unknown):
            return dict(result, reason='Orbit orientation contains unknown coordinates')
        matching = [c for c in decomposition['frame_certificates']
                    if c.get('frame_sha256') == certificate['frame_sha256']]
        active = bool(np.any(pi[self._positions[orbit]] != self._positions[orbit])
                      or any(self.m.oid[p] == orbit for p in elements))
        if (matching and any(c.get('model') != self.m.model_id or c.get('frame_version') != FRAME_VERSION
                             or c.get('status') != 'Verified' for c in matching)) or (active and not matching):
            return dict(result, reason='Action is not bound to the certified fixed frame')
        table = self._tables[orbit]; total = 0
        for position, element in elements.items():
            if self.m.oid[position] == orbit:
                value = table['quotient'].get(tuple(element))
                if value is None:
                    return dict(result, reason='Orientation element is outside the certified group')
                total = table['multiplication'][total][value]
        parity = position_parity(pi, self._positions[orbit])
        return dict(result, status='Conflict' if parity or total else 'PassNecessary',
                    position_parity=parity, orientation_quotient=total,
                    quotient_order=certificate['quotient_order'],
                    certificate_binding=certificate['binding_sha256'])
