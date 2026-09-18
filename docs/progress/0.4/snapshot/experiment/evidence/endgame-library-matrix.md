> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Manual endgame coverage — 2026-09-16

This is a headless finite-family certificate, not native workflow acceptance or a human solve. Every listed word retains full-model collateral. Net and strict protection still require a fresh complete-operation review.

Parameters were chosen from fixed immutable references for offline coverage only. Runtime composition requires explicit orbit, family, auxiliary positions and exact q/r permutations; no Session or residual is read.

| Orbit | Actual orientation group | Placement | Nonbuffer q | A q | Final B outcomes | Collateral orbit union |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| 0 | C2 | 1 | 1 | 1 | 1 | 1, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34 |
| 1 | trivial | 1 | 0 | 0 | 1 | None |
| 2 | trivial | 1 | 0 | 0 | 1 | 4, 5, 8, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33 |
| 3 | C2 | 1 | 1 | 1 | 1 | 4, 5 |
| 4 | trivial | 1 | 0 | 0 | 1 | None |
| 5 | trivial | 1 | 0 | 0 | 1 | None |
| 6 | D5 (order 10) | 1 | 9 | 9 | 5 | 7, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34 |
| 7 | trivial | 1 | 0 | 0 | 1 | None |
| 8 | C2 | 1 | 1 | 1 | 1 | 9, 10, 11, 12, 13, 14, 16, 18, 25, 26, 27, 30, 31, 32, 33 |
| 9 | trivial | 1 | 0 | 0 | 1 | 10, 11, 12, 14, 31, 32, 33 |
| 10 | trivial | 1 | 0 | 0 | 1 | 12, 14, 31, 32, 33 |
| 11 | C2 | 1 | 1 | 1 | 1 | 12, 31, 32, 33 |
| 12 | trivial | 1 | 0 | 0 | 1 | 33 |
| 13 | trivial | 1 | 0 | 0 | 1 | 16, 20 |
| 14 | trivial | 1 | 0 | 0 | 1 | 32, 33 |
| 15 | trivial | 1 | 0 | 0 | 1 | 10, 11, 12, 13, 14, 16, 18, 19, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34 |
| 16 | trivial | 1 | 0 | 0 | 1 | 19 |
| 17 | C5 | 1 | 4 | 4 | 1 | 14, 16, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34 |
| 18 | trivial | 1 | 0 | 0 | 1 | None |
| 19 | trivial | 1 | 0 | 0 | 1 | None |
| 20 | trivial | 1 | 0 | 0 | 1 | None |
| 21 | trivial | 1 | 0 | 0 | 1 | 23, 25, 26, 27, 30 |
| 22 | C2 | 1 | 1 | 1 | 1 | 23, 24, 25, 26, 27, 28, 30, 31, 32 |
| 23 | trivial | 1 | 0 | 0 | 1 | 25, 26, 27, 30 |
| 24 | trivial | 1 | 0 | 0 | 1 | 27 |
| 25 | C2 | 1 | 1 | 1 | 1 | 26, 27, 30 |
| 26 | trivial | 1 | 0 | 0 | 1 | 30 |
| 27 | trivial | 1 | 0 | 0 | 1 | None |
| 28 | C5 | 1 | 4 | 4 | 1 | 30, 31, 32 |
| 29 | trivial | 1 | 0 | 0 | 1 | 24, 26, 27, 31, 32, 33 |
| 30 | trivial | 1 | 0 | 0 | 1 | None |
| 31 | trivial | 1 | 0 | 0 | 1 | None |
| 32 | C2 | 1 | 1 | 1 | 1 | None |
| 33 | trivial | 1 | 0 | 0 | 1 | None |
| 34 | A5 (order 60) | 1 | 59 | 59 | 60 | 33 |

Counts: placement=35, transfer=83, buffer_a=83, final_b=98.

Orientation transfer/A: every nonidentity element of each nontrivial group was composed and checked against its exact target slot mapping. Trivial groups have no independent orientation residual.

Final B: D5 has all five rotation-subgroup outcomes; A5 has all sixty outcomes. Trivial/C2/C5 have only the identity outcome: a nonidentity lone-B orientation conflicts with the audited necessary quotient invariant. Identity-table entries are explicit cancelling witnesses, not a suggested correction.

The exact full recipes, original primitive costs, fixed-frame certificates, target slot pairs, complete support and per-entry collateral are in endgame-library-coverage.json. Noncommutative q/r order was checked against actual fixed-frame decomposition. Four retained C2/C5/D5/A5 recipes were also expanded primitive by primitive and compared across all labels.

The previously completed 1200 × 35 primitive parity/abelianization proof was loaded only after pinned artifact verification and exact current model, verifier source, manifest, chart and finite-quotient checks. That unchanged proof was not rerun or replaced by a summary.

Remaining integration: root must expose explicit parameter selection through existing Solve, preserve naming/focus, run current full protection and conditional goal review, add the source/model-bound audit artifact to packaging, and verify native buffer/endgame workflows. Missing artifact or stale dependency is Unavailable; never infer solved or executable. Arbitrary buffer relocation and automatically selected q/r are not implemented by these helpers.

Evidence: endgame-library-final.log (7 checks), endgame-library-validation.json (before/after source and asset hashes), endgame-library-coverage.json (35-orbit finite witnesses).
