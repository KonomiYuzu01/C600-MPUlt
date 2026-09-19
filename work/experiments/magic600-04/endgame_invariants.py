"""Load one reviewed all-generator proof into the existing invariant service.

This is an application proof artifact, never imported user metadata. The pinned
bytes identify the audited report; its full checks and current mathematical
dependencies are validated too. No generator enumeration or state is stored here.
"""
from copy import deepcopy
from pathlib import Path
import json

from core import canonical, digest
from orbit_invariants import OrbitInvariants, PROOF_VERSION, SCOPE, finite_quotient
from transported_frames import FRAME_VERSION

AUDIT_PATH = Path(__file__).resolve().parent / 'evidence' / 'orbit-invariants-20260916-generators.json'
AUDIT_SHA256 = 'a9bb5b73ff967ec9adb6bfa74b3a7c8ed4f8440a80bb7b0a22efc34b2187226c'


def load_audited_invariants(model, frames, path=None):
    """Explicit cancellable load; return a fully validated existing service.

Missing, changed or stale evidence raises ValueError. A caller must display
Unavailable, not infer zero or rerun the expensive proof during painting.
"""
    model.check_cancel()
    path = AUDIT_PATH if path is None else Path(path)
    try:
        raw = path.read_bytes()
    except OSError as error:
        raise ValueError('The matching all-generator invariant report is unavailable') from error
    if digest(raw) != AUDIT_SHA256:
        raise ValueError('Invariant report bytes do not match the reviewed proof artifact')
    report = json.loads(raw)
    if report.get('status') != 'passed' or report.get('passed') is not True:
        raise ValueError('Invariant proof is incomplete')
    service = OrbitInvariants(model, frames)
    certificates = [frames.ensure(orbit) for orbit in range(35)]
    binding = service._binding(certificates)
    if report.get('binding') != binding:
        raise ValueError('Invariant proof has stale model, source, manifest or fixed-frame dependencies')
    rows = report.get('generators')
    if not isinstance(rows, list) or len(rows) != 1200:
        raise ValueError('Invariant proof must cover every positive primitive')
    for number, row in enumerate(rows, 1):
        model.check_cancel()
        if type(row.get('primitive')) is not int or row['primitive'] != number:
            raise ValueError('Invariant primitive coverage is incomplete or reordered')
        for field in ('position_parity', 'orientation_quotient'):
            values = row.get(field)
            if not isinstance(values, list) or len(values) != 35 or any(type(v) is not int or v != 0 for v in values):
                raise ValueError('Invariant proof lacks complete evenness or zero quotient checks')
    saved = report.get('orbits')
    if not isinstance(saved, list) or len(saved) != 35:
        raise ValueError('Invariant proof lacks every moving orbit')
    tables, shared, accepted = {}, {}, {}
    for orbit in range(35):
        model.check_cancel()
        group = frames.finite_group(orbit)
        if group not in shared:
            shared[group] = finite_quotient(group)
        table = tables[orbit] = shared[group]
        expected = dict(status='Verified', model=model.model_id, proof_version=PROOF_VERSION,
            frame_version=FRAME_VERSION, orbit=orbit, frame_sha256=certificates[orbit]['frame_sha256'],
            group_order=len(table['elements']), derived_order=len(table['derived']),
            quotient_order=len(table['cosets']), quotient_sha256=table['sha256'],
            position_count=len(service._positions[orbit]), generator_count=1200,
            generators_sha256=digest(canonical(list(range(1,1201))).encode()),
            checks_sha256=digest(canonical([[r['primitive'], r['position_parity'][orbit],
                                            r['orientation_quotient'][orbit]] for r in rows]).encode()),
            binding_sha256=digest(canonical(binding).encode()), scope=SCOPE)
        if saved[orbit] != expected:
            raise ValueError('Invariant orbit certificate differs from its exact finite group or checks')
        accepted[orbit] = deepcopy(expected)
    model.check_cancel()
    if binding != service._binding([frames.ensure(orbit) for orbit in range(35)]):
        raise ValueError('Invariant dependencies changed during loading')
    service._tables, service._certificates = tables, accepted
    return service
