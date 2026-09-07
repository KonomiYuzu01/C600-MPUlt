#!/usr/bin/env python3
"""Audit nine pure 600-cell controllers and an exact colour-count inequality.

Reads declarative JSON and numeric NPZ data only. No code from the reference
package is imported or executed. See README.md for scope and interpretation.
"""

import argparse
import collections
import decimal
import hashlib
import json
import math
from pathlib import Path

import numpy as np


SELECTED_ORBITS = (1, 4, 5, 7, 18, 19, 27, 31, 33)
STAGE_ORDER = (
    6, 0, 17, 15, 2, 22, 21, 8, 23, 9, 29, 25, 13, 11, 10, 34, 28,
    26, 24, 16, 14, 12, 3, 33, 32, 31, 30, 27, 20, 19, 18, 7, 5, 4, 1,
)
DATA_FILES = {
    "census": "engine/census.json",
    "seeds": "engine/target_seeds.json",
    "buffers": "engine/buffer_certificates.json",
    "model": "engine/model.npz",
    "expansion_audit": "audits/expansion_audit.json",
}


def sha256(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def compose(a, b):
    """Chronological permutation composition: first a, then b."""
    return tuple(b[i] for i in a)


def inverse(a):
    result = [0] * len(a)
    for i, j in enumerate(a):
        result[j] = i
    return tuple(result)


def order(a):
    identity = tuple(range(len(a)))
    current = identity
    for length in range(1, 121):
        current = compose(current, a)
        if current == identity:
            return length
    raise AssertionError("Unexpected local permutation order")


def parity(a):
    seen = set()
    cycles = 0
    for i in range(len(a)):
        if i in seen:
            continue
        cycles += 1
        while i not in seen:
            seen.add(i)
            i = a[i]
    return (len(a) - cycles) % 2


def audit(reference):
    paths = {key: reference / relative for key, relative in DATA_FILES.items()}

    def read(key):
        return json.loads(paths[key].read_text(encoding="utf-8"))

    rows = read("census")["orbits"]
    assert len(rows) == 35 and [r["id"] for r in rows] == list(range(35))
    assert sum(r["pieces"] for r in rows) == 176520
    assert sum(r["stickers"] for r in rows) == 259200

    group_checks = []
    for row in rows:
        size, stickers = row["orientation_order"], row["colors"]
        identity = tuple(range(stickers))
        permutations = set(map(tuple, row.get(
            "orientation_permutations", [identity] if size == 1 else [])))
        assert len(permutations) == size and identity in permutations
        assert all(sorted(p) == list(range(stickers)) for p in permutations)
        assert all(compose(a, b) in permutations
                   for a in permutations for b in permutations)
        commutators = {
            compose(compose(compose(a, b), inverse(a)), inverse(b))
            for a in permutations for b in permutations
        }
        assert all(compose(a, b) in commutators
                   for a in commutators for b in commutators)
        group_checks.append({
            "orbit": row["id"], "size": size,
            "element_orders": dict(sorted(collections.Counter(
                order(p) for p in permutations).items())),
            "commutator_elements": len(commutators),
            "abelianization_order": size // len(commutators),
            "all_sticker_permutations_even": all(parity(p) == 0 for p in permutations),
        })

    decimal.getcontext().prec = 65
    D = decimal.Decimal

    def log_factorial(n):
        return D(math.factorial(n)).log10()

    positional_log = sum(log_factorial(r["pieces"]) for r in rows)
    orientation_log = sum(D(r["pieces"]) * D(r["orientation_order"]).log10()
                          for r in rows)
    constraint_log = D(35) * D(2).log10() + sum(
        D(g["abelianization_order"]).log10() for g in group_checks)
    counts = {
        "moving_piece_histogram": dict(sorted(collections.Counter(
            r["pieces"] for r in rows).items())),
        "labelled_structural_log10": str(positional_log + orientation_log),
        "necessary_invariant_constrained_log10": str(
            positional_log + orientation_log - constraint_log),
        "constraint_log10": str(constraint_log),
        "status": (
            "Upper counts. The stronger count assumes the source-reported separate "
            "orbit parity and transported-frame abelian invariants; that all-generator "
            "audit is not rerun here. Equality requires full reachability sufficiency."
        ),
    }

    seeds = read("seeds")
    buffers = {b["orbit"]: b for b in read("buffers")}
    assert len(seeds) == 35 and set(buffers) == set(range(35))
    assert {s["orbit"] for s in seeds} == set(range(35))
    pure = [s["orbit"] for s in seeds if set(map(int, s["signature"])) == {s["orbit"]}]
    assert all(o in pure and rows[o]["colors"] == 1
               and rows[o]["orientation_order"] == 1 for o in SELECTED_ORBITS)
    stage_rank = {o: i for i, o in enumerate(STAGE_ORDER)}
    assert all(stage_rank[d] > stage_rank[s["orbit"]]
               for s in seeds for d in map(int, s["signature"]) if d != s["orbit"])
    assert all(b["status"] == "PASS" and b["expected"] == b["reachable_oriented_positions"]
               == (rows[o]["pieces"] - 2) * rows[o]["orientation_order"]
               for o, b in buffers.items())

    # Arrays are copied out before the archive closes. Pickled/object arrays are refused.
    with np.load(paths["model"], allow_pickle=False) as archive:
        arrays = {name: archive[name] for name in (
            "slot_piece", "orbit_id", "face_values", "face_offsets", "mask_values",
            "mask_offsets", "rotperms", "move_src", "move_dst", "normals")}
    sp, oid = arrays["slot_piece"], arrays["orbit_id"]
    fv, fo = arrays["face_values"], arrays["face_offsets"]
    mv, mo = arrays["mask_values"], arrays["mask_offsets"]
    rot, normals = arrays["rotperms"], arrays["normals"]
    move_src, move_dst = arrays["move_src"], arrays["move_dst"]
    assert len(sp) == 259800 and len(oid) == 177120 and len(normals) == 600
    assert np.count_nonzero(oid < 0) == 600
    assert np.array_equal(np.bincount(oid[oid >= 0], minlength=35),
                          np.array([r["pieces"] for r in rows]))
    slot_faces = np.arange(259800) // 433
    piece_order = np.argsort(sp, kind="stable")
    piece_sizes = np.bincount(sp, minlength=177120)
    offsets = np.r_[0, np.cumsum(piece_sizes)]
    assert np.array_equal(fo, offsets)
    assert np.array_equal(fv, slot_faces[piece_order])
    keys = sp.astype(np.int64) * 600 + slot_faces
    key_order = np.argsort(keys)
    sorted_keys = keys[key_order]
    cached = {}

    def move_arrays(move):
        assert 0 <= move < 1200
        if move not in cached:
            a, b = move_src[move], move_dst[move]
            src = piece_order[np.concatenate([
                np.arange(offsets[p], offsets[p + 1]) for p in a])]
            destinations = np.repeat(b, piece_sizes[a])
            faces = rot[move, slot_faces[src]]
            wanted = destinations.astype(np.int64) * 600 + faces
            positions = np.searchsorted(sorted_keys, wanted)
            assert np.array_equal(sorted_keys[positions], wanted)
            dst = key_order[positions]
            assert len(np.unique(src)) == len(src) and len(np.unique(dst)) == len(dst)
            assert np.array_equal(np.sort(src), np.sort(dst))
            changed = src != dst
            cached[move] = src[changed], dst[changed]
        return cached[move]

    antipodal = np.argmin(np.sum((normals[:, None, :] + normals[None, :, :])**2, axis=2), axis=1)
    assert np.max(np.abs(normals + normals[antipodal])) < 1e-8
    identity_labels = np.arange(259800)
    checks, colour_log = [], D(0)
    exact_lower = 1
    for orbit in SELECTED_ORBITS:
        seed = next(s for s in seeds if s["orbit"] == orbit)
        positions = np.flatnonzero(oid == orbit)
        n = len(positions)
        copies = n // 600
        slots = np.flatnonzero(oid[sp] == orbit)
        multiplicity = np.bincount(slot_faces[slots], minlength=600)
        assert np.all(multiplicity == copies) and len(slots) == n and copies >= 2
        labels = identity_labels.copy()
        for primitive in seed["word"]:
            src, dst = move_arrays(abs(primitive) - 1)
            if primitive < 0:
                src, dst = dst, src
            labels[dst] = labels[src]
        changed = np.flatnonzero(labels != identity_labels)
        assert len(changed) == 3 and np.all(oid[sp[changed]] == orbit)
        permutation = {int(labels[s]): int(s) for s in changed}
        start = min(permutation)
        cycle = [start, permutation[start], permutation[permutation[start]]]
        assert permutation[cycle[-1]] == start and len(set(cycle)) == 3

        b = buffers[orbit]
        A, B = map(int, b["buffers"])
        C = int(b["third"])
        guard_A = set(map(int, mv[mo[A]:mo[A + 1]]))
        guard = guard_A | set(map(int, mv[mo[B]:mo[B + 1]]))
        assert guard == set(b["guard_cells"]) and A != B and C not in (A, B)
        assert list(map(int, sp[cycle])) == b["seed_anchors"]
        relocated = np.array(cycle, dtype=np.int64)
        for primitive in b["relocation"]:
            assert 1 <= abs(primitive) <= 1200 and (abs(primitive) - 1) // 2 not in guard_A
            src, dst = move_arrays(abs(primitive) - 1)
            if primitive < 0:
                src, dst = dst, src
            lookup = {int(s): int(d) for s, d in zip(src, dst)}
            relocated = np.array([lookup.get(int(s), int(s)) for s in relocated], dtype=np.int64)
        assert list(map(int, sp[relocated])) == [A, B, C]
        assert set(map(int, antipodal[mv[mo[A]:mo[A + 1]]])) == set(map(int, mv[mo[B]:mo[B + 1]]))

        # Rebuild the positive-move graph instead of trusting stored BFS totals.
        adjacency = {int(p): [] for p in positions}
        for move in range(1200):
            if move // 2 in guard:
                continue
            src, dst = move_src[move], move_dst[move]
            active = oid[src] == orbit
            assert np.all(oid[dst[active]] == orbit)
            assert not np.any(((src == A) | (src == B)) & (src != dst))
            for p, q in zip(src[active], dst[active]):
                adjacency[int(p)].append(int(q))
        reached, queue = {C}, [C]
        for p in queue:
            for q in adjacency[p]:
                if q not in reached:
                    reached.add(q)
                    queue.append(q)
        assert len(reached) == n - 2 and A not in reached and B not in reached

        value = log_factorial(n) - D(600) * log_factorial(copies)
        colour_log += value
        factor, remainder = divmod(math.factorial(n), math.factorial(copies)**600)
        assert remainder == 0
        exact_lower *= factor
        checks.append({
            "orbit": orbit, "pieces": n, "symbols": 600, "copies_per_symbol": copies,
            "raw_word_length": len(seed["word"]),
            "independent_full_word_three_cycle": cycle,
            "all_other_259797_slots_fixed": True,
            "relocation_anchors_verified": True, "relocation_length": len(b["relocation"]),
            "relocated_piece_cycle": [A, B, C],
            "independent_guarded_positive_bfs": len(reached), "expected_bfs": n - 2,
            "antipodal_cap_sets": True, "colored_log10_lower": str(value),
        })
        print(f"Verified O{orbit:02d}: pure word, relocation, {len(reached)} guarded targets", flush=True)

    exact_upper, remainder = divmod(math.factorial(57344), math.factorial(4096)**14)
    assert remainder == 0
    assert exact_lower > 14**57344
    assert exact_lower > 10**151851 and 14**57344 < 10**65724
    assert exact_upper < 10**65696
    assert exact_lower > exact_upper * 10**86155
    upper_log = log_factorial(57344) - D(14) * log_factorial(4096)

    lengths = {r["orbit"]: r["length"] for r in read("expansion_audit")}
    star_bounds = {
        r["id"]: 2 * (r["pieces"] - 2) + (2 * (r["pieces"] - 2) + 3
            + 8 * (r["orientation_order"] in (10, 60))) * (r["orientation_order"] > 1)
        for r in rows
    }
    return {
        "scope": "Independent declarative-data checks of nine pure single-sticker orbits; no supplied solver code executed.",
        "census": {"total_pieces": 177120, "moving": 176520, "fixed": 600,
                   "stickers": 259800, "orbits": 35},
        "groups": group_checks, "counts": counts,
        "source_reported_pure_raw_orbits": pure,
        "stored_dependency_order_valid": True,
        "stored_35_buffer_counts_consistent": True,
        "independent_single_sticker_checks": checks,
        "colored_lower_log10": str(colour_log),
        "four_power_seven_loose_color_log10_upper": str(D(57344) * D(14).log10()),
        "four_power_seven_multinomial_log10_upper": str(upper_log),
        "lower_over_multinomial_upper_log10": str(colour_log - upper_log),
        "exact_integer_comparison": {
            "lower_exceeds_14_power_57344": True,
            "lower_exceeds_10_power_151851": True,
            "loose_upper_below_10_power_65724": True,
            "multinomial_upper_below_10_power_65696": True,
            "lower_over_multinomial_upper_exceeds_10_power_86155": True,
            "note": "Exact integer division and comparisons; no logarithm rounding is used for these assertions.",
        },
        "comparison_assumptions": (
            "4^7: 57344 fixed sticker positions, 14 symbols, 4096 copies each. "
            "Full 600-cell: 600 distinct cell-colour symbols in each selected orbit; "
            "all other stickers fixed at macro boundaries. Fixed-frame counting on "
            "both sides, without a global spatial-rotation quotient. The upper bound ignores legality."
        ),
        "input_sha256": {relative: sha256(paths[key]) for key, relative in DATA_FILES.items()},
        "workload_arithmetic": {
            "star_bound": sum(star_bounds.values()),
            "primitive_bound": sum(star_bounds[o] * lengths[o] for o in star_bounds),
            "seed600_mean_primitives_per_star": str(D(352698206) / D(374178)),
            "seed600_phase_sum": 345833 + 28302 + 27 + 16,
            "evidence": "Arithmetic from census and retained maximum expansion lengths; all35 expansion audits are not rerun here.",
        },
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--reference-dir", type=Path, required=True,
                        help="Extracted Full_600cell_Curated_Reference_and_Replay directory")
    parser.add_argument("--output", type=Path, required=True,
                        help="Destination JSON file; contains no local paths or timing/process data")
    args = parser.parse_args()
    if not __debug__:
        parser.error("Run without -O or PYTHONOPTIMIZE: audit assertions must remain enabled.")
    result = audit(args.reference_dir)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, ensure_ascii=True) + "\n", encoding="utf-8")
    print("PASS: exact colour lower bound exceeds the 4^7 multinomial upper bound by more than 10^86155.")


if __name__ == "__main__":
    main()
