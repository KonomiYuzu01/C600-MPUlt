# Puzzle theory

**[Puzzle Theory of the Full 600-Cell (PDF)](Full_600cell_Puzzle_Theory.pdf)** is a 30-page English LaTeX supplement on the geometry, topology, group actions and optimization of the retained full-cut puzzle. Its [standalone LaTeX source](Full_600cell_Puzzle_Theory.tex) is included.

The mathematical development contains **15 lemmas and a combined theorem**, with the full 35-orbit census. It covers exact golden-ratio coordinates, incidence and duality; the radial homeomorphism to the three-sphere; binary icosahedral quaternions and the 14,400-element H4 symmetry group; cap signatures and local frames; parity and orientation invariants; conditional configuration bounds; and the difference between geometric symmetry and legal twists. The full legal-group order is not asserted.

The optimization discussion explains complete permutation caching, incremental progress updates, ordered-frame and weighted setup search, conditional symmetry reduction, GPU projection and instancing as future options, and consistent filtering, framework visibility and picking. Proposed changes are distinguished from implemented behavior and measured performance.

## Review and reproducibility

Rethlas, using separate Astra generation and verification invocations with a documented Windows I/O adaptation, returned **correct**, with **zero critical errors and zero gaps**, for the complete mathematical blueprint. This is an informal AI proof review, not a proof-assistant certificate or peer review. Retained puzzle conclusions are explicitly conditional on the model and controller hypotheses.

| Material | Introduction |
| --- | --- |
| [Reviewed mathematical blueprint](theory/RETHLAS_BLUEPRINT.md) | Exact returned proof, including its original review statement. Its opening candidate-status sentence records the generation stage; the subsequent verdict below is authoritative for the review outcome. |
| [Exact verifier verdict](theory/rethlas-verification.json) | The independent verifier's complete report and strict result, preserved without editing. |
| [Review provenance and hashes](theory/review-provenance.json) | Records upstream revision, models, scope, I/O adaptation, editorial changes and artifact identities. |
| [Review problem](theory/review-problem.md) | Complete technical statement supplied to Rethlas, with separate geometric and conditional claims. |
| [Exact geometry checker](audit/verify_regular_geometry.py) and [results](audit/regular-geometry-results.json) | Standard-library arithmetic over integer pairs in Z[phi]. Checks the 120 vertices, 600 supporting tetrahedra, incidences, mod-2 boundary ranks, graph distances, quaternion closure and explicit symmetries. Does not reconstruct the cut subdivision. |
| [Original algorithms paper](reference/Full_600cell_35_Orbit_Algorithms.pdf) | Unchanged 51-page reference containing the full 35-orbit algorithm cards and constructive procedures. |
| [Technical report and replay materials](README.md) | Broader development history, software contracts, exact counting audits, and curated synthetic replay evidence. |

Run the geometry audit from the repository root:

```text
python research/audit/verify_regular_geometry.py
```

Run with assertions enabled, without Python `-O`. The script writes its JSON beside itself and never opens an application or session. Integral homology and simple connectedness follow from the separately supplied radial argument, not from mod-2 ranks alone.

Compile the PDF from `research/`:

```text
pdflatex -interaction=nonstopmode -halt-on-error Full_600cell_Puzzle_Theory.tex
pdflatex -interaction=nonstopmode -halt-on-error Full_600cell_Puzzle_Theory.tex
pdflatex -interaction=nonstopmode -halt-on-error Full_600cell_Puzzle_Theory.tex
```

The published `.tex` is self-contained: no Pandoc, private transcript, external image, application binary or shell escape is needed to rebuild it. Checksums are in [SHA256SUMS.txt](SHA256SUMS.txt).

## HSC wiki and primary references

The Hypercubing wiki provides background definitions, not this full-cut puzzle's census:

- [Formal Introduction to Grip Theory](https://hypercubing.xyz/theory/grip-theory/formal/) introduces active grips, attitudes and frame stabilizers.
- [Piece Invariants](https://hypercubing.xyz/theory/invariants/) explains parity and orientation constraints; its hypercube monoflip example is distinguished from this puzzle's A5 frame group.
- [God's Number](https://hypercubing.xyz/theory/gods-number/) motivates metric-specific counting bounds.
- [Cut Depths](https://hypercubing.xyz/theory/cut-depths/) explains normalized cutting conventions.

The geometric references include [Baez, From the Icosahedron to E8](https://arxiv.org/abs/1712.06436), [Choi and Lee, Binary Icosahedral Group and 600-Cell](https://doi.org/10.3390/sym10080326), and [Vogan, Regular polyhedra and Coxeter groups](https://math.mit.edu/~dav/regpolyTUFTSHO2.pdf). The workflow is based on [Rethlas](https://github.com/frenzymath/Rethlas).

Primary credit for the original MPUlt simulator and renderer belongs to **Andrey Astrelin**. C600 Studio remains a mod; Nan Ma's methodologies and ivan216's projects are acknowledged as background. No private transcript, personal history or machine log is included.

The full-detail target remains **30 FPS at 1920 x 1080 with all 259,800 stickers visible**. No minimum GPU model has yet qualified; see [Full Detail Rotation requirements](FULL_DETAIL_ROTATION_REQUIREMENTS.md). The theory PDF, LaTeX source, exact geometry checker, and retained proof-review records remain unchanged for **C600 Studio 0.3**. The updated [technical report](Full_600cell_Technical_Report.pdf) describes the implementation: canonical cell IDs, 4-regular dual-graph BFS layers, 120-vertex/20-cell incidence, exact inspection semantics, shared auxiliary projections, and committed native revision updates. These are interface and software contracts, not additional mathematical theorems. Current measurements are separated from historical 0.2.4 validation and do not establish full-detail 1080p/30 FPS.
