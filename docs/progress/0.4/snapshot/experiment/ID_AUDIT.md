> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Magic 600 Cell experimental identity and mathematical profile audit

Date: 2026-09-15. Scope: retained full model and the isolated G1/G2 experiment. This document records source inspection, bounded read-only model calculations and explicitly identified agent-operated tests. It is not a human solving trial, a proof of new orbit nomenclature or native DirectX validation.

The latest product instruction keeps `I`/`P` identifiers for pieces and positions for now. Large implementation identifiers should recede from the normal work surface. Orbit selection should primarily present mathematical structure and an actual geometric profile. Internal identifiers remain stable and available in details; presentation changes never relabel the puzzle.

## Exact types and numbering

| Object | Range / interpretation | Authoritative source | Presentation / boundary rule |
| --- | --- | --- | --- |
| Piece identity | Integer 0–177119; displayed `I` | `Model.pids`, `slot_piece`; `PuzzleState.at`/`where` | Identity follows a piece. `where[I]` gives its present position. Do not use a frame-node or sticker ID here. |
| Physical position | Integer 0–177119; displayed `P` | Same position universe; `Model.slots(P)` | A fixed socket. `at[P]` gives its changing occupant. Selecting its outline must retain P while occupants move. |
| Canonical cell / cap | C1–C600 | Displayed form of model lab-cell indices | Hosting cell and affecting cap are different relations, even though they use the same cell universe. |
| Lab-cell index | Integer 0–599 | `face_values`, `mask_values`, `normals`, `frames` | Convert only at the typed boundary: canonical C = lab index + 1. Validate the original integer before subtraction; Boolean values are invalid. |
| Physical sticker slot | Integer 0–259799; displayed `S` when needed | `Model.ids`, `Model.slots(P)` | Slot location is fixed. There are 433 slots per cell; canonical cell is `floor(S / 433) + 1`. |
| Sticker identity label | Integer 0–259799 | `state.labels[S]` | The label occupying S is a distinct object. Equal numeric ranges do not make slot and label interchangeable. Exact label transport carries orientation information. |
| Moving orbit | Integer 0–34 internally | `orbit_id`, `census.json`, certified model orbits | The number is a stable lookup key, not a mathematical name or solve order. `-1` denotes fixed pieces and is not an active moving orbit. |
| Vertex | Canonical V1–V120; geometry arrays use 0–119 | `Model.vertex_positions`, `vertex_cells`, `vertices4` | A vertex index is not its piece-position ID. Each vertex is incident to 20 cells; each cell has four vertices. |
| Legal primitive | Signed IDs ±1…±1200; zero invalid | `Model.move`, `primitives.npz` | Positive index is `2 * lab_cell + type + 1`; a negative ID denotes the inverse. Retain exact legal words. |
| Star frame node | Nonnegative index scoped to one orbit tree | `trees[orbit].positions/frames/parent` | Validate against that tree's actual size. `node 11` alone is ambiguous. Its position and ordered slot frame are separate fields. |
| A/B/Target role | Named role bound to explicit positions and frame | Retained buffer tables plus work intent | Role assignment is not a turn or proof that a selected macro implements those roles. Show actual occupants separately. |
| H/T axis label | Scoped to a particular captured cap and basis | `grips.py` exact words | Seven visible axes represent eleven nonidentity rotations. Inverse word/ID is authoritative; inverse display labels need not match forward labels. |
| Native sticker / move token | Native-profile-specific | `lab_to_native.json`, verified native bridge/profile | Never treat a native index as a lab slot without the verified mapping. UI hit validation also needs current state and interaction mask. |
| Macro record / bank / work sheet | Stable application identifiers | Experimental library, bank and work-sheet records | Not geometric IDs. Preserve selected macro recipe/version; changing a target does not retarget it. A bank's orbit association does not authorize a turn. |
| Review / pending token / state hash | Different scopes and lifetimes | Workbench guard and authoritative Session | A hash is not execution permission. A current staged preview is distinct from stale analysis. |

Model identity used for this audit: `58c83f336e451aa64147fe1b8db2d60d7d43806aa3a35122f8403d5af51253f9`.

## Confirmed boundary defects and corrections

The following corrections were verified through real-model API tests for experimental build `G1G2-20260915-a2`, adapter SHA256 `DC0A4B8526B285DD9A1FC5D61A7F4595C233D5C6229F3C1507DA9AAEAD621725`. Later source edits need their own matching evidence.

| Trigger | Previous defect | Corrected contract / verified result |
| --- | --- | --- |
| Canonical cell input `true`, float, string, zero or 601 | Subtracting one before type validation allowed a Boolean to become a valid index | Validate the original value as an integer in 1–600. Rejection preserves workspace, preferences, labels and pending preview. |
| Capture `[7, null, 113]`, empty/internal gaps or more than 20 entries | Sparse input could be compacted in UI and silently shift physical key meanings | Backend accepts only an explicit contiguous array of 1–20 canonical integers; it rejects holes. UI must reject internal gaps before sending, not silently remove them. |
| Twist inverse/destination with wrong types | Truthy values or unspecified destination could imply an action | Require an explicit Boolean inverse and `draft` or `live`; execute the retained exact inverse word. |
| Click a fixed position then turn | Identity-only inspection could follow the prior occupant | `inspect-position` stores `inspected_position`; identity inspection clears it. An E1 test verified fixed P stays at A while its occupant changes, whereas inspected I follows that piece. |
| Successful Preview | Its newly created pending token made the displayed review read Stale despite a valid enabled Commit | Display Staged only when the existing execution guard and pending token match. Actual intent/reference/policy changes still invalidate execution. |
| Cross-orbit focus / checkpoint restore / invalid Next target | Outgoing Current could be overwritten; workspace could disagree with restored checkpoint; impossible cross-orbit Next could replace a valid pin | Outgoing context is saved before assigning new Current; restore rehydrates workspace; Next validates moving identity and destination orbit before replacement. |
| Oversized settings | Session rejected persistence while the in-memory workspace retained the rejected edit | Restore the last persisted workspace/library when preference validation fails. Existing labels and pending work remain unchanged. |
| `grips` response `cell` | Legacy zero-based field could be mistaken for canonical C | Preserve legacy `cell`; add explicit `lab_cell_index` and `canonical_cell` on the response and each axis/inverse. Original `grips.py` remains unchanged. |

The snapshot additionally provides `model_counts` (`np`, `n`, cells, vertices, moving_orbits), typed `id_schema` limits and each block member's actual orbit. These fields support consistent frontend validation and graphical marking; they do not replace model validation.

Actual baseline Edge screenshots also exposed low-contrast diagram labels, tiny Local correspondence and weak conflict localization. These are frontend findings, not identity mathematics. Their corrections require matching screen/interaction checks, not a source-only claim.

## Mathematical orbit profiles: facts

The census provides hosting count, affecting-cap count, orientation group, representative and stable orbit membership. `rank` in the current core is derived from cap count minus one; it is not an independent discriminator or a newly established geometric dimension.

Four pairs have the same simple descriptors. In all eight audited representatives the sole hosting cell is C1 and the orientation group is trivial:

| Internal pair, details only | Truthful shared caption | Representative physical positions / slots | Difference in the same retained C1 reference |
| --- | --- | --- | --- |
| O04 / O05 | 1-cell · 5 affecting caps · trivial orientation | P12 / S12 and P13 / S13 | One cap is C39 versus C36; the other four cap memberships coincide. |
| O12 / O13 | 1-cell · 9 affecting caps · trivial orientation | P32 / S32 and P41 / S41 | C7 versus C6; the other eight coincide. |
| O18 / O19 | 1-cell · 11 affecting caps · trivial orientation | P65 / S65 and P66 / S66 | C68 versus C57; the other ten coincide. |
| O26 / O27 | 1-cell · 15 affecting caps · trivial orientation | P82 / S82 and P90 / S90 | C31 versus C27; the other fourteen coincide. |

For each pair, rounded nine-decimal cap-pole dot-product histograms and host-marked per-pole distance profiles also coincide. This floating-geometry observation is not a symbolic congruence proof. It does establish that these particular unordered scalar summaries do not distinguish the profiles in the retained data.

A separate bounded check enumerated exactly twelve permutations from the retained C1 H/T rotation generators. None maps either audited representative's complete cap set to its partner's cap set. This is an exact integer-permutation comparison, scoped to those twelve C1 rotations. It does not by itself prove reflection equivalence, chirality, a left/right naming convention or a new classification theorem. No assets, labels, seeds, witnesses or sessions were changed by these checks.

Trivial piece orientation means no distinct internal labelled-slot orientation state for these one-sticker pieces. It does not make the chosen reference frame or Grip/Twist basis irrelevant.

## Inference and bounded recommendation

**Inference:** count/group labels are useful summaries but insufficient selectors for the four collisions. A distinct actual geometric profile must remain visible before selection. Text equality must not merge entries or choose a default match.

**Recommendation:** retain the mathematical count/group caption, and place each representative's actual hosting-cell/affecting-cap arrangement in a common, ordered reference and common camera. For the four known collisions that reference can be the same retained C1 basis. Distinguish the host region from affecting caps; expanding the row should show the differing membership and permit linked inspection. Keep the orbit number in details only.

The current real-geometry hosting/cap thumbnail can satisfy this without an architectural change. The small required constraint is consistent orientation, camera and scale for the compared profiles. Do not independently auto-fit or rotate the two views into misleading similarity. If the small projection obscures the difference, use the expanded comparison or a synchronized view change. A reusable thumbnail must not silently claim that a generic dot or count-only icon identifies the orbit.

An optional more concrete local portrait can use the existing representative sticker mesh inside its hosting-cell tetrahedron, with the same ordered cell frame. Use the retained mesh/slot data rather than inventing a shape. This is an interaction proposal, not an additional verified rendering result or a request to replace the existing renderer.

**Unverified hypothesis:** some paired patterns might admit a reflection relationship. Do not name them left/right, handed, clockwise/counterclockwise, inner/outer or similar without a specific verified definition and relation. No such naming is authorized by this audit.

**Smallest falsification for a UI disagreement:** display one known pair with identical reference/camera/scale, select each row explicitly, and compare the highlighted actual cap memberships against the table. If both portraits are visually indistinguishable at the intended size, the thumbnail alone is insufficient: expand or improve that comparison. Do not infer a new mathematical name. If claiming a reflection relationship later, first supply and verify the actual full mapping and its stated scope; unordered distance equality is insufficient.

## Verification limits and next check

Source/read-only model evidence establishes the numbering, exact representative memberships and twelve-rotation result above. The a2 agent-operated contract run passed 20 Workbench tests plus owned-engine startup/shutdown. Mandatory retained core, independent reference-map and crash checks passed. The full lifecycle suite remains incomplete: after twenty passing checks it encountered an unchanged baseline source assertion expecting `native/snapshot` while the retained host uses `native/snapshot?protocol=2`. That failure must not be labelled a pass.

The next useful check is the actual candidate UI at the same 1440×1000 settings: complete E1 through visible controls, verify Staged versus truly stale feedback, inspect fixed P versus tracked I after a move, compare one ambiguous mathematical profile pair, then redo/restore the work context. Record loaded module hashes and actual screenshots. Agent review does not replace either independent G1/G2 user approval.
