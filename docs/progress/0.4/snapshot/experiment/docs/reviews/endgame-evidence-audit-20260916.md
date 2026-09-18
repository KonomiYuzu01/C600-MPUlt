> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Existing endgame evidence audit — 2026-09-16

Scope: independent read-only audit for `08_INTEGRATION_AND_ENDGAME.md`. Application sources and personal sessions were not changed. No `Planner.next`, automatic setup endpoint, target-dependent parameter search or new solve was run. The native application remained exclusively operated by the coordinator.

## Conclusion

The repository already contains useful legal, parameterized endgame families and exact saved examples for C2/C5 transfer, D5 final-buffer rotation and A5 final-buffer orientation. Ten saved examples were freshly checked against the current complete model, including every primitive in their stored expansions. They are usable evidence for a manual endgame workbench, not permission to automatically choose a correction or claim protection.

The important missing integration proof is current model/frame-version-bound invariant certification and a manual workflow covering all required residual classes. Historical all-generator summaries and a finite orientation-group table must not be presented as that new certificate. The retained raw families have cross-orbit collateral; target-orbit-only algebra cannot remove it.

## Authoritative sources and evidence levels

All paths below are relative to the actual source checkout.

- `core.py:27–32`: `cp(a,b)[i] = b[a[i]]`, chronological composition, exact inverse and `commp(a,b) = a b a^-1 b^-1`.
- `core.py:119–156`: `Model.certify_seed` replays the full legal seed, validates target three-cycle, atlas frame transport and all collateral. `star_net` follows an already stored finite node path, checks guarded caps, conjugates the full action and verifies A/B/target frames. These are exact action verifiers, not abstract group-name proofs.
- `core.py:160–208`: normalization, full-model net action, exact primitive expansion and full support. `Model.expand` uses `path^-1 / relocation^-1 / seed / relocation / path` and reverses/inverts the entire word for an inverse.
- `core.py:286–328`: retained `Planner.next` contains position, two-star orientation transfer, three-star A transfer and eight-star B commutator formulas. It also chooses targets and parameters from the state automatically. Only its existing mathematical constructions are audited here; the selection behavior must not be exposed as the new human-directed UI.
- `assets/{model.npz,census.json,seed_atlas.json,execution_trees.json.gz,buffer_certificates.json}`: actual canonical labels, orbit groups, legal words, ordered frames, fixed A/B/third positions and finite paths.
- `research/Full_600cell_Curated_Reference_and_Replay.zip` (abbreviated **Archive** below): retained reference algorithms, independent replay engine, exact synthetic histories, historical audits and replay reports. Archive prefix is `Full_600cell_Curated_Reference_and_Replay/`.
- Archive `Full_600cell_35_Orbit_Algorithms.tex:118–155`: two-star transfer; three-star A form; eight-star last-B commutator; separate C2/C5/D5/A5 invariants; explicit warning that `K^3 = 1` is only guaranteed on the target orbit. `:159` describes recovery of phase boundaries against the saved command trace.
- Archive `engine/full_solver.py:78–118`: exact finite frame-to-node mapping and the same existing constructions. Archive `replay_logs.py:38–152` consumes saved commands, preserves collateral, checks every completed stage and the final 259,800 labels; it does not generate another solve.
- Archive `audits/all_primitive_invariants.json`: retained assertion of even piece permutation for all 1,200 generators and per-orbit zero abelianization totals. It has no per-generator results, current frame-version key or complete verifier source in this curated archive. Treat as historical evidence, not a freshly re-established certificate.
- Archive `audits/orientation_group_audit.json`: actual attainable-group closure, element orders and commutator-image sizes. Archive `validation/summary.json` records three synthetic saved-history replays (seeds 600/601/602), 35 stages and all labels solved; explicitly not primitive-by-primitive native replay. These histories were not fully rerun in this audit.
- `research/audit/verify_extended_derivations.py:126–135`: finite checks of `[R^k,F]=R^(2k)`, the D5 commutator image and all 60 A5 elements as single commutators. This script's own scope excludes all-orbit geometry/native replay. `research/theory/RETHLAS_BLUEPRINT.md:455,563–628` treats the all-generator invariant statement as a separate hypothesis, and distinguishes it from controller evidence.
- `tests/test_core.py:33–50` retains all-orbit full-collateral synthetic completion checks, but invokes automatic `Planner.next`; not run for this audit and not a human-solving or new UI acceptance test. `tests/test_reference_maps.py` checks all packed generator maps against the independent retained constructor; that proves maps, not the transported-frame invariant by itself.

## Concrete saved witnesses and fresh replay

Source: Archive `logs/triangular_solution_0.json.gz` (seed 600). The matching `audits/seed600_phase_workload.json` gives sequential phase counts. Offsets below are zero-based, end-exclusive in `commands`. Every stored command is `[orbit,node,sign]`; `n+` means the exact retained star at node n, `n−` its inverse. No new node choice was computed from a target state.

| Group / orbit | Saved operation | Log slice | Exact node/sign sequence | Primitive count | Target-orbit moved positions |
|---|---|---|---|---:|---|
| D5 / O6 | Nonbuffer transfer | 1389:1391 | 138+, 99− | 808 | P10, B=P153202 |
| D5 / O6 | A transfer | 2665:2668 | 0+, 378+, 0+ | 1,200 | A=P4944, B=P153202 |
| D5 / O6 | Last B | 2668:2676 | 54+, 0−, 112+, 99−, 0+, 54−, 99+, 112− | 3,216 | B=P153202 only |
| C5 / O17 | Nonbuffer transfer | 8117:8119 | 2269+, 610− | 3,106 | P25, B=P173788 |
| C5 / O17 | A transfer | 10413:10416 | 0+, 315+, 0+ | 4,644 | A=P269, B=P173788 |
| C2 / O22 | Nonbuffer transfer | 27004:27006 | 477+, 15− | 1,562 | P92, B=P171586 |
| C2 / O22 | A transfer | 30596:30599 | 0+, 1136+, 0+ | 2,334 | A=P5380, B=P171586 |
| A5 / O34 | Nonbuffer transfer | 138284:138286 | 1660+, 32− | 24,598 | P116, B=P130196 |
| A5 / O34 | A transfer | 138520:138523 | 0+, 1219+, 0+ | 36,888 | A=P5060, B=P130196 |
| A5 / O34 | Last B | 138523:138531 | 16+, 0−, 67+, 32−, 0+, 16−, 32+, 67− | 98,372 | B=P130196 only |

“Moved positions” in the final column means positions whose sticker arrangement changes; **all ten operations preserve every piece position on their target orbit**. No target-orbit placement cycle remains. Each fresh check compared `Model.net(recipe)` to `Model.word_net(list(Model.expand(recipe)))` over all 259,800 labels and asserted exact equality. Exit 0. This is headless mathematical evidence, not a native timing/interaction result or replay of the history's complete preceding state.

For the D5 last-B example, auxiliaries are X=P29398 (base node 0) and Y=P10 (base node 99). Relative to those actual ordered frame bases:

- q = `(4,2,1,3,0)` from node 54;
- r = `(2,4,3,1,0)` from node 112;
- chronological `[q,r] = (1,2,4,0,3)` equals the actual five-slot permutation at B in the atlas B frame.

For the A5 last-B example, X=P17810 (base node 0), Y=P116 (base node 32); q is node 16 relative to node 0, r is node 67 relative to node 32. The actual twenty-slot B permutation is `(2,8,12,6,9,1,13,4,18,17,0,3,19,15,7,11,5,14,16,10)`, exactly the chronological commutator of those two stored frame permutations. This is an actual 20-slot witness, not a five-point A5 analogy.

C2/C5 have **no nonidentity last-B-only correction** in these retained legal endgames: after every other piece and A are fully solved, the certified invariant would force B solved. Their nonbuffer and A transfer witnesses are the correct positive cases; an apparent lone C2/C5 residual is a diagnostic case, not a reason to fabricate a solving macro.

### Full collateral and operation limits

- D5 last-B moves five target labels plus labels in **22 other orbits**: O7,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,29. It is not globally pure.
- A5 last-B moves twenty target labels plus **14 labels / 14 pieces in O33**. Its A transfer also changes O33 (21 labels); its nonbuffer transfer changes O33 (20 labels).
- C2 O22 A transfer changes O23,24,25,26,27,28,30,31,32 as well as O22. C5 O17 A transfer changes O14,16,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34 as well as O17.
- Net target-orbit preservation does not establish Strict preservation. This audit did not evaluate protected-prefix safety; complete primitive prefix checking remains required under the actual chosen protection policy. Auxiliary X/Y do not become exempt automatically.
- Current `Model.normalize` limits: 1–256 recipe steps, 1–100,000 primitives per explicit word, aggregate at most 3,000,000 primitives. The 98,372-primitive A5 example fits as eight stars and even as one explicit word. **No current numeric-limit blocker was found.** Its observed headless expansion check took about 9.9 seconds in this run; this is not UI latency evidence. Cancellation/responsiveness and strict-prefix cost on that actual operation remain acceptance work.

## Coverage by orbit

All 35 have retained words, A/B positions and oriented-node tables, and are included in the historical all-label stage replay. Below, N/A/B are counts of nonbuffer-orientation, A-transfer and B-commutator stars in **seed 600 only**, not current feature acceptance. Zero A means that example did not need A correction; it does not prove an A workflow.

| Orbit | Orientation group | Historical N / A / B stars | Fresh primitive witness here |
|---|---|---|---|
| O0 | C2 | 682 / 0 / 0 | Not rerun |
| O1 | trivial | 0 / 0 / 0 | Not rerun |
| O2 | trivial | 0 / 0 / 0 | Not rerun |
| O3 | C2 | 3486 / 3 / 0 | Not rerun |
| O4 | trivial | 0 / 0 / 0 | Not rerun |
| O5 | trivial | 0 / 0 / 0 | Not rerun |
| O6 | D5 (order 10) | 1276 / 3 / 8 | N, A, B |
| O7 | trivial | 0 / 0 / 0 | Not rerun |
| O8 | C2 | 3642 / 3 / 0 | Not rerun |
| O9 | trivial | 0 / 0 / 0 | Not rerun |
| O10 | trivial | 0 / 0 / 0 | Not rerun |
| O11 | C2 | 3668 / 3 / 0 | Not rerun |
| O12 | trivial | 0 / 0 / 0 | Not rerun |
| O13 | trivial | 0 / 0 / 0 | Not rerun |
| O14 | trivial | 0 / 0 / 0 | Not rerun |
| O15 | trivial | 0 / 0 / 0 | Not rerun |
| O16 | trivial | 0 / 0 / 0 | Not rerun |
| O17 | C5 | 2296 / 3 / 0 | N, A |
| O18 | trivial | 0 / 0 / 0 | Not rerun |
| O19 | trivial | 0 / 0 / 0 | Not rerun |
| O20 | trivial | 0 / 0 / 0 | Not rerun |
| O21 | trivial | 0 / 0 / 0 | Not rerun |
| O22 | C2 | 3592 / 3 / 0 | N, A |
| O23 | trivial | 0 / 0 / 0 | Not rerun |
| O24 | trivial | 0 / 0 / 0 | Not rerun |
| O25 | C2 | 3558 / 0 / 0 | Not rerun |
| O26 | trivial | 0 / 0 / 0 | Not rerun |
| O27 | trivial | 0 / 0 / 0 | Not rerun |
| O28 | C5 | 2284 / 3 / 0 | Not rerun |
| O29 | trivial | 0 / 0 / 0 | Not rerun |
| O30 | trivial | 0 / 0 / 0 | Not rerun |
| O31 | trivial | 0 / 0 / 0 | Not rerun |
| O32 | C2 | 3582 / 3 / 0 | Not rerun |
| O33 | trivial | 0 / 0 / 0 | Not rerun |
| O34 | A5 (order 60) | 236 / 3 / 8 | N, A, B |

A separate fresh finite enumeration used the actual ordered tree frames at the retained third position on each orbit. All 35 frame sets had the census cardinality and were closed under chronological composition. Their commutator images had size 1 for trivial/C2/C5, 5 for O6/D5, 60 for O34/A5. This certifies those finite tables; it does not certify every primitive's transport through every other position or every invariant.

## Specific gaps and smallest follow-up

1. **Versioned invariant certificate:** reproduce all 1,200 primitive parity and abelianization checks using the new explicit coherent frame authority; record model, frame version, convention and exact coverage. Refuse invariant-based legality diagnostics when that provenance is missing. Do not derive D5's reflection bit from ordinary five-sticker permutation parity.
2. **Manual parameter access:** expose the already retained finite reference families as explicit inputs/choices, with exact node/frame and full effect. Do not call the retained automatic planner to choose q/r, target or setup. User-selected auxiliaries and references remain visible. This audit does not decide a new UI.
3. **Coverage missing from this audit:** fresh primitive examples on C2 O0/3/8/11/25/32, C5 O28 and trivial-orbit endgames; O0/O25 A handling is absent from seed600's phase trace. Other stored histories may contain it, but that was not checked. D5 all five residual rotations and A5 all 60 legal orientations need explicit finite witness-family coverage beyond the one saved correction replayed here.
4. **Protection and completion:** record actual Net/Strict consequences for the complete selected operation; demonstrate allowed Net but rejected Strict with protected auxiliaries. Full target placement, one oriented buffer, full orbit and full puzzle completion must remain separate.
5. **Continuous native evidence:** manual orientation and final-buffer preparation, display/reference clarity, cancellation, session resume/undo/redo, explicit Next across orbits and fresh automatic orbit protection remain outside this audit. Existing synthetic histories do not replace those tests or human solving evidence.

## Identity and reproducibility

Archive SHA256: `85d2d9a6b337442aae10b7c26056f518ce0d0a738e2f098d47d8a52678d0ec5a`.
Current `core.py`: `67839fc28bf4dd020b515330efcde43a2116474e9970a1d743de55c7deb2eea2`.
Model ID: `58c83f336e451aa64147fe1b8db2d60d7d43806aa3a35122f8403d5af51253f9`.
Archive/current assets were compared **byte-for-byte** and match:

- model.npz: `680de6710e8a05b12dd4ba2ea22c866e645af032a31faf985d869798521f8b13`;
- census.json: `779aaae849d363856ef622080649c5c02130be44090944846f86aa2b25c4f3eb`;
- execution_trees.json.gz: `a820837c948eeff41df6f064d817312dcb9206ba4e2a73d0ced5ccf136ea3d20`;
- seed_atlas.json: `de41b58067ac8c0bf5339c11e43b828aa4944b6b40e8b2e7e1287e625e898891`.

Reproduction: read the indicated command slice from the zip without extracting/modifying assets; convert each triple to `dict(kind='star', orbit=o, node=n, sign=sg)`; call `net` and `word_net(expand(recipe))`, compare source and destination arrays exactly; target-position preservation is `m.sp[src[target]] == m.sp[dst[target]]`. For actual frame groups use `m.bypos[o][tree['third']]`, express each stored frame in its first-node frame, enumerate `cp`/`commp` and compare sets. No target-dependent search or live Session is required.

The initial read-only probe used the wrong `Model.net` unpacking arity and failed before producing evidence; it was corrected to the actual four-field contract and rerun successfully. The finite-table probe initially requested a nonexistent census `orientation` key; the corrected run used the inspected `orientation_group` schema. Neither failed probe changed project state. Only this review document was written.
