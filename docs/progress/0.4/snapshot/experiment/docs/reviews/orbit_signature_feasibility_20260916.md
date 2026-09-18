> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Signature and address feasibility — 2026-09-16

## Conclusion

The proposed unanchored `Signature` is already closely represented by
`mathematical_names.py:142`. On the complete 433-region reference cell it produces
36 distinct classes: the 35 moving orbit classes and the fixed-center class.
This does **not** make the geometric symmetry group equal to the legal turn group.

The proposed address rule has a concrete obstruction. One representative per full
Signature, followed only by the twelve reference-cell A4 rotations, covers **361
of 433 regions**. Six Signature classes each contain two separate anchored A4
region orbits of size 12. The address interpretation needs confirmation before
implementation; do not silently merge these into one twelve-element orbit.

No application, model, Session, classification or UI source was changed in this
review. The probe and this report are development-only evidence.

## Reproducible evidence and scope

Run from `work/experiments/magic600-04`:

```text
python tests/probe_orbit_signature.py
```

The actual run exited 0 in 23.229 seconds. Raw output is
`tests/probe_orbit_signature_result.json`.

- Model: `58c83f336e451aa64147fe1b8db2d60d7d43806aa3a35122f8403d5af51253f9`.
- Probe SHA256: `5A6118CE41C8B8C78629F4DC18775196B7AA308AFEBBD86423FC3E00C35F8807`.
- Output SHA256: `EEAF636248E93D8728ADBA875AAE97AEA8BA58ABA074EE66FD0116AFCC437846`.
- Inspected naming source: `1BCAB9F28E0C9F7C9FB1878564EA36EEE61B8739C2CDB84285B82C9C8CB53184`.

The canonical minimum was directly evaluated for every reference-cell region,
not independently recomputed for all 177,120 positions. A separate integer check
verified all **259,800** hosting-region instances: applying the stored `F_cell`
to a reference region's complete Host/M pair identifies exactly the stored
physical position for that slot. Complete Host/M pairs identify **177,120 unique
positions**. The existing constructor additionally checks that the orbit template
is identical in all 600 cells. These are full-model incidence checks, not a claim
that every global canonical-minimum computation was separately run.

All signature comparisons and region permutations use integer cell membership.
They do not use centroids, nearest-neighbor geometry, floating tolerances or the
current puzzle state. The golden-coordinate display reconstruction is a separate
numerical fact: a fresh computation measured maximum pole residual
`4.884981308350689e-15`, and frame/normal covariance residual
`1.3322676295501878e-14`. Those measurements are not a formal exact-arithmetic
proof of the source geometry.

## Available authoritative data

- `core.Model.hosting(p)` and `caps(p)` read `face_offsets/face_values` and
  `mask_offsets/mask_values`. These are the complete Host and affecting-cap M
  sets; neither may substitute for the other.
- `model.z['frameperms']` contains 600 permutations of 600 cell indices;
  `np.argsort(..., axis=1)` is the exact inverse permutation used by the current
  signature. `model.z['rotperms']` supplies the retained H/T actions.
- The reference H/T actions generate exactly 12 permutations fixing cell 0.
  `grip_frames.py:48` independently anchors the four-corner order through the
  actual H double transposition and T three-cycle. These are proper tetrahedral
  rotations; their words here describe a geometric address, not a suggested
  physical turn of the current puzzle.
- `slot_piece`, `orbit_id` and `Model.slots(p)` retain physical incidence and the
  legal-orbit census. `census['orientation_group']` supplies H_o. The actual group
  names are trivial, C2, C5, D5 (order 10), and A5 (order 60); a Twist of order 3
  does not imply a C3 piece-orientation group.
- The existing display address uses approximate barycentric centroids and a
  minimum-pole hosting anchor (`mathematical_names.py:168`). It does not yet
  implement reversible structure/word addresses or an explicit `namingVersion`.
  The new display version must freeze its integer ordering and model binding.

## Exact counterexample to one representative per full Signature

All numbers in this evidence table are zero-based canonical diagnostics, not
proposed front-end names.

| Legal orbit | Reference regions | A4 components | Chosen full-Signature representative |
|---|---:|---|---:|
| 9 | 24 | 12 + 12 | 294 |
| 14 | 24 | 12 + 12 | 371 |
| 16 | 24 | 12 + 12 | 373 |
| 20 | 24 | 12 + 12 | 399 |
| 23 | 24 | 12 + 12 | 404 |
| 30 | 24 | 12 + 12 | 426 |

For orbit 23, regions 404 and 73 have the **same full Signature**:

```text
Host = [0, 1]
M    = [0, 1, 2, 3, 5, 7, 8, 10, 13, 14, 15, 18, 23]
```

Their concrete reference-cell incidence differs:

```text
region 404: Host [0,1]
            M [0,1,2,3,5,7,8,10,13,14,15,18,23]
region 73:  Host [0,10]
            M [0,5,6,8,10,12,13,17,35,38,39,45,67]
```

The entire A4 orbit of representative 404 is:
`[70,80,93,199,210,220,319,337,347,383,404,409]`.
Region 73 is absent. The other component is
`[73,88,98,196,204,215,316,329,342,386,393,414]`.
No longer H/T/T^-1 word can bridge the components: the complete generated group
of 12 has already been enumerated. Overall there are **42 anchored region
components**, versus 36 full Signature classes. Stabilizer duplicates were
deduplicated and do not account for the missing 72 regions.

Recommended clarification: keep the same unanchored StructureClass and legal
orbit, but allow a canonical representative for each anchored A4 component.
An address can be `Cell / StructureClass / u0:word` or `u1:word` where necessary.
The representative mark is local-address disambiguation, not another orbit,
chirality, layer or additional alpha/beta structure label. Within each component,
the declared generator order H, T, T^-1 supports shortest first-discovered words
and exact inverse parsing. This changes the prescribed single-u0 interpretation
and requires confirmation.

Alternative: restrict canonical hosting anchors to those whose local region is
reachable from the one chosen u0. This may preserve three address components,
but some of the reference cell's regions would be addressed through another
hosting cell. It changes what “coverage of the 433 regions” means and is not
implemented or certified by this probe. It should not silently replace the first
interpretation.

## Four equal-count/group pairs remain distinguishable

Full Signature ordering can provide alpha/beta only within these equal coarse
groups. Under the current exact integer ordering, the earlier Signature is:

| Counts / H_o | Alpha orbit | Beta orbit | Actual cap-set difference in canonical representatives |
|---|---:|---:|---|
| 1 sticker / 5 caps / trivial | 5 | 4 | 10 versus 12 |
| 1 sticker / 9 caps / trivial | 12 | 13 | 14 versus 18 |
| 1 sticker / 11 caps / trivial | 19 | 18 | membership of 2 versus 4 in differing complete sets |
| 1 sticker / 15 caps / trivial | 26 | 27 | 32 versus 41 |

The raw output contains each complete pair of sets. These are actual membership
differences, not a claim of handedness. A thumbnail should draw the actual
canonical cap membership in the same reference and scale. Displaying a different
letter alone does not demonstrate the geometric distinction.

## Transported orientation reference feasibility

The union of each orbit's stored `tree.positions/tree.frames` and the atlas's two
buffer frames covers **every position in all 35 moving orbits**. Every inspected
frame contains exactly the slots of its stated position. There are no uncovered
positions in this inventory; no target-dependent setup search is needed to read
these immutable data.

Each non-buffer position has precisely `|H_o|` distinct stored ordered frames;
the two protected buffer positions each have their one fixed atlas frame. For
example orbit 25 has 3,598 positions with two frames and two buffer positions with
one; orbit 34 has 118 positions with sixty frames and two buffers with one.

The frames are not one already-selected global reference. At orbit 25 position
35851, node 0 gives `[49018,72226]` while node 284 gives `[72226,49018]`.
`core.py:124–164` defines the existing legal seed and transported-Star frame
checks. The earlier independent `tests/probe_pure_cycle_reference.py` replayed
these two explicit nodes and established different edge orientation relative to
the same chosen reference. Neither identical position cycles nor third-power
identity makes them orientation-equivalent.

A future versioned chart can choose one independent ordered frame per position
from these complete immutable records, preserving the atlas buffer frames and a
documented deterministic selection rule. This is a choice of reference, not a
proof that all legal paths have the same frame. Before using it for the new Pure
classification and score, verify the selected transport witnesses, local H_o
membership, and chronological noncommutative composition against complete label
replay. This probe inventories frame coverage and exact slot membership; it did
not replay every tree path or validate the new orientation/ranking implementation.

Until that chart is fixed and verified, retain Unknown where reference evidence
is insufficient. Do not retrofit zero orientation or infer a chart from the
macro being classified.
