# Independent colour-state bound audit

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
