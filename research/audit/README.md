# Mathematical audits

Two separate checkers accompany the expanded report. The original controller audit establishes the finite-model evidence used for the colour-state lower bound. The extended checker develops additional arithmetic and small-group consequences, retaining an explicit distinction between newly checked facts and assumptions inherited from the model.

## Independent colour-state bound audit

This supplement verifies the nine pure single-sticker orbit controllers used in *Certified Control of the Full 600-Cell*. It reads only declarative JSON and numeric NumPy arrays from the curated reference package. It never imports or executes that package's solver code.

Use Python 3.11 or later and NumPy. Extract the separately supplied `Full_600cell_Curated_Reference_and_Replay` archive, then run:

```text
python -m pip install -r requirements.txt
python verify_color_bound.py --reference-dir Full_600cell_Curated_Reference_and_Replay --output regenerated-results.json
```

The reference directory must contain `engine/census.json`, `engine/target_seeds.json`, `engine/buffer_certificates.json`, `engine/model.npz`, and `audits/expansion_audit.json`. The supplied `results.json` records their hashes. Its `engine/census.json` bytes match the original toolkit's verified census. The output contains relative data names and hashes, without local paths, user/session data, or performance measurements. Do not use Python optimization (`-O` or `PYTHONOPTIMIZE`), which disables assertions; the checker refuses it.

The checker independently reconstructs primitive sticker permutations and replays the complete raw words for O01, O04, O05, O07, O18, O19, O27, O31, and O33 on all 259,800 labels. For each it verifies exactly one pure three-cycle, its relocation to the recorded buffers, and independently rebuilt positive-move reachability of all nonbuffer positions while both buffers are fixed. It also checks uniform multiplicities of the 600 cell-colour symbols and the stored local permutation groups.

These checks support a short group argument. Conjugating a pure `(A B C)` by each buffer-fixing setup supplies every `(A B X)`, generating the alternating group on that orbit. Disjoint pure controllers act independently. Every colour has at least two copies, so a same-colour transposition is an odd stabilizer: the alternating group therefore realizes every balanced colour arrangement on the orbit.

The constructive lower bound is

`L = [3600!/(6!)^600] [2400!/(4!)^600] [7200!/(12!)^600]^7`.

Its base-10 logarithm is approximately `151851.164321964826`. For 57,344 fixed sticker positions with fourteen symbols and 4,096 copies of each, the unconstrained balanced-assignment upper bound is

`U = 57344!/(4096!)^14`, with `log10(U) ≈ 65695.470510075313`.

Exact integer division and comparisons establish `L > 10^151851`, `U < 10^65696`, and `L > U * 10^86155`. These strict inequalities do not depend on rounded decimal logarithms. Both sides count fixed-frame colour arrangements, with a colour understood as a distinct cell/face symbol.

The result concerns the retained finite model. It is not an exact order of the complete puzzle group, a universal difficulty ranking, a minimum move count, or a human solving record. The 4^7 sticker/color census is an explicit comparison assumption whose primary source is cited in the report. The checker does not independently regenerate clipping geometry, rerun the all-generator transported-frame invariant audit, verify every 35-orbit oriented setup tree, or animate the represented solutions in MPUlt. Its additional all-orbit counts and workload arithmetic are clearly labelled as conditional or derived from retained reports.

`results.json` contains the completed audit result. A failed assertion exits unsuccessfully before writing a new result; callers should check the process exit code rather than treating an old result file as a new pass.

## Extended derivations and complete controller table

[verify_extended_derivations.py](verify_extended_derivations.py) uses Python 3.11 or later and the standard library only. From the repository root, run:

```text
python research/audit/verify_extended_derivations.py
```

It reads only `assets/census.json`, `assets/seed_atlas.json`, and `assets/execution_trees.json.gz`. It checks all 35 rows for census agreement, retained seed identity, parent/depth structure, expected frame-state count, and the direction of every recorded collateral edge. It also checks the displayed cycle identities on small permutation domains and enumerates the commutator images of D5 and A5. No application, solver, graphics runtime, private session, or network service is executed.

The completed [extended-results.json](extended-results.json) includes the input hashes and full per-orbit controller-cost table. Its numerical results are:

| Quantity | Checked result and scope |
| --- | --- |
| Moving census | 176,520 pieces and 259,200 stickers; add 600 fixed centers for the full totals. |
| Retained raw seeds | 12 pure and 23 with collateral. |
| Structural labelled upper count | Base-10 logarithm approximately 598075.039945. |
| Invariant-constrained labelled upper count | Divide the structural count by `2^43 * 5^2`; logarithm approximately 598060.697715. This uses the retained generator-invariant audit and does not establish an exact legal-group order. |
| Worst-case distance from solved | At least 46,648 turns in the report's 1,800-symbol lab alphabet, proved by an exact word-ball comparison with the independently supported colour lower bound. |
| 1,000-step scramble support | Total variation distance from uniform reachable face-colour states is strictly greater than `1 - 10^-148595`. This does not measure visual disorder or typical human solving effort. |
| Construction workload | At most 405,945 stars and 385,797,152 represented primitives under the stated controller and stage-correctness hypotheses. |
| Finite commutator images | 5 elements for D5 and all 60 elements for A5. |

The word-count bound is a worst-case existence statement. A state produced by a known 1,000-turn word always has a solution of at most 1,000 turns by inversion. Likewise, a constrained ambient upper count is not an exact reachable-state count, and the constructive workload is not an optimal solution bound for an individual scramble.

The extended checker does not repeat the original nine-controller full-word audit, regenerate clipping geometry, or validate every oriented transition and generator invariant across all 35 orbits. Matching retained tree dimensions and parent depths alone would not establish those stronger properties. Both checkers refuse Python `-O`; check the exit status before accepting an existing results file as a new pass.
