> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Reference alignment discovery — 2026-09-16

Independent geometry/reference audit. Baseline: accepted useful work through native G2 `postapproval-g2-20260916-085608`; this audit does not run the native UI or change application code. The latest architecture S00 amendment authorizes an actual reference variant derived from the selected Grip/local frame, with complete preview and explicit resolution of ambiguity. It does not authorize setup search or execution.

## Conclusion

The retained data supports a practical, discrete construction of a cross-cell reference map. A read-only probe constructed one full 259,800-slot bijection and compiled four explicitly chosen generator actions into legal words with exactly matching complete effects. This is enough to justify an isolated implementation; it is **not** certification of all 1,200 generators or a production-ready service.

The missing input is the selected macro's **source cap and ordered four-corner frame**. The destination can use the user's already selected Grip cap/frame. A focused piece or its current position alone does not supply a unique ordered reference. Existing source recipes and star slot frames must not silently be interpreted as that cap frame.

No further general “camera-only or actual action” decision is needed: the user already chose actual action. Missing reference inputs should be requested in the normal operation flow. Do not add an automatic choice based on which variant happens to solve the focused target.

## Authoritative data and current boundaries

| Source | Observed meaning | Limit |
| --- | --- | --- |
| `assets/model.npz`: `frameperms`, `frames` | 600 discrete normal permutations and corresponding 4×4 matrices. Entry c maps base cell C1 to cell c+1. | A whole-puzzle coordinate correspondence is not itself a legal puzzle move. |
| `core.py:37–94` | Canonical piece/slot/orbit incidence; 120 vertices, four vertices per cell, 20 cells at each vertex. | Hosting cells and affecting caps remain different sets. |
| `grip_frames.py:80,114` | Twelve proper ordered frames per cap; resolves seven axis choices to eleven real nonidentity actions. Complete label equality checked for emitted cap words. | Same-cap frame proof does not establish arbitrary cross-cell macro reference. |
| `local_geometry.py:10,35` | Actual retained 433-region mesh; current slot, label, position and occupant; real palette; filter and protection. | Current payload has no dedicated A/B/Target overlay. Adding those observations need not alter mechanics. |
| `mathematical_names.py:38–50,159–165` | Retained base-frame vertices and barycentric location hints from stored centers. | Display centers are not exact orientation correspondences or canonical IDs. |
| `adapter.py:396` | Existing reference variant is explicitly supplied finite-word `R^-1 / Macro / R`. | This is not the proposed geometric reference automorphism. Preserve that distinct operation and its provenance. |
| `core.py:119,145` | Execution-tree paths and transported star certificates. | Do not use target-dependent paths as a reference-alignment implementation. |

The model identity checked by `Model()` is `58c83f336e451aa64147fe1b8db2d60d7d43806aa3a35122f8403d5af51253f9`.

The 600 frame matrices agree with their discrete normal permutations within `1.3322676295501878e-14`. The separately inspected determinants range from `0.9999999999999956` to `1.0000000000000002`, and orthogonality residual is below `3.8e-15`. Those are floating geometry checks; exact combinatorial equalities below compare integer arrays.

## Smallest safe reference contract

1. Bind the chosen macro by existing canonical ID, version and exact recipe.
2. Bind a source `{cell, ordered_vertices[4]}`. For an existing unbound macro, use the existing cell/frame chooser once; keep this as explicit metadata, not a guessed first-generator convention. An old record with no frame remains unbound and compatible.
3. Read the destination from the visible, user-selected Grip `{cell, ordered_vertices[4]}`. Require that same frame in the request/context guard. Camera changes do not change it.
4. Derive the unique proper cell-incidence map which sends the source ordered corners to the destination ordered corners. If the source/destination reference has not specified enough information, present the remaining exact frame candidates. Do not rank them by target-solving success.
5. Map the macro's actual support, A/B/Target positions where declared, and sticker correspondences. Verify the requested focused position/target matches that mapped result. A reference map transforms buffers too; it cannot promise fixed A/B while mapping an arbitrary target. A mismatch stays visible and requires another explicit reference or macro choice, not a setup search.
6. Compile the mapped finite recipe to legal generators, preserving the source. The preview must identify source and mapped references, mapped buffers/occupants, full operation effects, and the actual emitted word. Net equality does not establish prefix equality; strict protection checks that emitted word.
7. Bind the full preview to existing authoritative Session/context checks and require explicit execution. Computing the variant is not applying a turn. No model IDs, labels or stored personal recipes are rewritten.

The input chooser may offer **current location** or **intended destination** as inspection context, but must not interchange them. The concrete destination reference remains the visible selected Grip. A 20-cell piece's 20 sticker correspondences must come from the verified slot map; four cap-corner names are not a replacement for its orientation data.

## Exact construction available in the retained assets

Use the existing 12-element proper cap stabilizer to derive 12 permutations of the base cell's 433 regions. A region key is its exact `(hosting cells, affecting caps)` pair, with each set transformed by the candidate normal permutation. In this model all 433 base-region keys are distinct, and all twelve mapped tables are bijections.

For a chosen normal map g and source cell c, express g locally using the retained frames:

`local = inverse(frameperms[g[c]]) ∘ g ∘ frameperms[c]`

This is one of the verified twelve base-cell permutations. Thus every source slot `433*c + region` maps to `433*g[c] + mapped_region`. Check bijection, coherent whole-piece transport, orbit preservation, and source/destination ordered corners.

For each source generator, conjugate its **normal action** by g, then identify the corresponding member of the destination cap's twelve-element legal H/T closure. Replay the resulting legal word against the relabelled full sparse action, not merely the normal action. Finally compare the full transformed macro effect on all labels.

The map g itself permutes fixed cell-center labels. Every legal puzzle move leaves those centers fixed. Consequently g must not be passed off as a legal setup word R; the legal witness is the **compiled transformed macro**, not execution of g.

## Reproduced witness

Run from the source checkout:

```text
python -B work/experiments/magic600-04/tests/probe_reference_incidence.py
```

Actual run: bundled Windows Python, exit 0, no Session created, no GUI, no application or asset writes.

- Source: C1, ordered vertices `[1,2,3,4]`.
- Destination: C7, ordered vertices `[1,10,8,3]`.
- Source finite word: `[1,2,-7,14]`.
- Emitted finite word: `[13,14,24,23,-24,11,12,11]`.
- The four primitive mappings and complete macro support agree exactly; 10,577 labels are affected.
- Full reference map: 259,800 slots, a bijection, coherent physical-piece mapping, all orbit identities preserved.
- **Not run:** all 1,200 generator certifications, star expansion through this transform, arbitrary selected frames, native insertion, prefix protection and cancellation performance.

Before application use, cover all positive generators for a certified candidate reference (negative actions follow by exact inversion), transformed star finite witnesses, inverse/composition consistency, wrong/mirrored frame rejection, multi-cell pieces, stale frame requests, and complete/prefix protection. Preserve an independently replayed full-label oracle; do not certify a map from its name or hash.

## Geometry evidence: corrected interpretation

The stored `slot_centers` are not covariant representatives under these symmetries. C1→C7 through the exact incidence map has a maximum center discrepancy of `0.5509154135945482`. Even the stored fixed-center location differs from the retained cell pole by `0.036118840453795764`. Therefore center proximity must not choose object correspondence or prove orientation.

An initial mesh diagnostic measured `0.24096758390315862` between the two triangle-vertex sets for C1 region 37 under H. That **does not prove different solid geometry**: two triangulations can use different vertices for the same convex region. A subsequent independent supporting-plane check gives only approximately `6.7e-7` maximum residual in both directions, consistent with the same region at f32 precision. The earlier stronger interpretation was withdrawn before implementation.

Only this one convex-region case was inspected. All-mesh rigid equivalence remains unproved. The probe records both quantities to preserve the distinction. It uses original arrays and a bounded supporting-plane calculation; no SciPy dependency is added. No immutable geometry change is justified by these observations.

For the first native slice, highlight corresponding exact slots on the original source and destination meshes and draw directions from the actual ordered tetrahedral vertices. Preserve the original mesh, labels and cell palette. Do not require shape morphing, center matching, or pretend that a projected direction proves an orientation relation.

## Independent UI work and remaining proof

The existing actual geometry can already show Current's occupant and position, selected buffers' positions and occupants, intended target, protected regions, and exact before/predicted-after sticker correspondences in a compact linked display. Derive these from one revision-bound snapshot. Unverified orientation should remain explicitly unverified; filter transparency cannot suppress conflict scope.

Pure Piece Cycle was confirmed as an edge-by-edge no-extra-orientation condition relative to an explicit reference, not merely return after a full cycle. The reference service can supply exact sticker correspondences for that proof, but the cycle classifier is independently owned and must not equate a cap frame with a 20-sticker piece frame.

No remaining general architecture choice was found for this bounded incidence-based approach. The operational source frame and any incomplete destination correspondence are real user inputs still required. Replacing immutable mesh assets or changing the meaning of a spatial reference would be a separate direction requiring confirmation.
