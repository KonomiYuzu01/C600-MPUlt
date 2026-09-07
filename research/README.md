# Full 600-cell research materials

**[Certified Control of the Full 600-Cell](Full_600cell_Technical_Report.pdf)** is an expanded, 27-page technical report on the full puzzle's mathematics and C600 Studio's software architecture. It centers on the original design transcript and algorithm reference, with 31 numbered equations, two propositions with proofs, the complete moving-orbit census, and component-level software contracts. Historical claims, new derivations, repeated computational checks, and recorded Windows measurements are distinguished throughout. The report is typeset entirely in LaTeX; its [complete source](Full_600cell_Technical_Report.tex) is included.

| Material | Introduction |
| --- | --- |
| [Technical report (PDF)](Full_600cell_Technical_Report.pdf) | Develops piece/frame group actions, orientation invariants, alternating-group reachability, state-count bounds, word metrics, scramble distributions, guarded setup graphs, and nonabelian corrections. It also explains transactional history, bridge equivalence, filtering and picking, log validation, process recovery, and rendering costs. |
| [Original 35-orbit algorithms paper (PDF)](reference/Full_600cell_35_Orbit_Algorithms.pdf) | The unchanged 51-page reference: one card per moving orbit, explicit seed constructions, setup/frame procedures, orientation handling, and workload analysis. Statements about untested native integration describe that historical edition. |
| [Curated reference and replay supplement (ZIP)](Full_600cell_Curated_Reference_and_Replay.zip) | Includes the original algorithm paper and LaTeX source, complete forward/inverse seed words, model/frame data, selected generated reports, and three deterministic synthetic solution histories. Its README explains replay commands and omitted historical records. |
| [Independent configuration-bound audit](audit/README.md) | A separate checker replays nine pure controllers over every labelled slot and independently checks buffer setup coverage and exact integer inequalities. Source, data hashes, and completed results are supplied. |
| [Extended derivation checker](audit/verify_extended_derivations.py) and [results](audit/extended-results.json) | Recomputes all 35 census/controller rows, structural tree and collateral-order checks, small-domain algebra identities, state-count arithmetic, a worst-case move lower bound, the short-scramble support bound, and constructive workload bounds. It does not run the application or access a session. |
| [Documentary source guide](ARCHIVAL_SOURCE_GUIDE.md) | Maps the retained technical evidence to page ranges in the privately held original conversation, without publishing personal exchanges or account-bearing links. |
| [Full Detail Rotation requirements](FULL_DETAIL_ROTATION_REQUIREMENTS.md) | Defines the 1920 x 1080 / 30 FPS qualification target and explains why no minimum GPU model is yet certified. |

The full puzzle contains **177,120 pieces and 259,800 sticker slots**. Three fresh compact replays passed with every labelled slot solved and all 35 stages checked. The companion records these new checks separately from historical timing reports. Compact macro replay is not primitive-by-primitive native animation.

The expanded analysis proves a **worst-case distance of at least 46,648 turns** in the specified 1,800-symbol lab alphabet and shows why a 1,000-step scramble from solved is far from a uniform random reachable state. These are counting results, not a minimum solution length for each scramble or a measure of human difficulty. Software sections explain both verified behavior and current limitations, including the absence of a qualified GPU minimum for continuous full-detail 1080p/30 FPS. This expansion adds arithmetic checks and source analysis; it does not claim new native performance measurements.

Primary credit for **Magic Puzzle Ultimate and its original renderer belongs to [Andrey Astrelin](https://superliminal.com/andrey/mpu/)**. C600 Studio is a mod. Nan Ma's solving methods and ivan216's software projects are acknowledged as background. The research includes AI-assisted analysis and is a technical report, not a peer-reviewed publication.

## Build the report

Use a current TeX Live, TinyTeX, or MiKTeX installation with the packages named in the source preamble. Compile from this directory:

```text
pdflatex -interaction=nonstopmode -halt-on-error Full_600cell_Technical_Report.tex
pdflatex -interaction=nonstopmode -halt-on-error Full_600cell_Technical_Report.tex
```

Repeat compilation if LaTeX requests another pass for long-table widths or cross-references. No external image, system font, application binary, private transcript, or shell escape is required. The original algorithm paper's separate LaTeX source is retained inside the companion archive.

## Scope and integrity

[SHA256SUMS.txt](SHA256SUMS.txt) identifies the three downloadable PDF/ZIP artifacts. The repository's [source manifest](../SOURCE_MANIFEST.json) covers the complete public source inventory; the ZIP contains its own member-level provenance and checksums.

The original conversation export, personal sessions, screenshots, raw diagnostics, third-party solve logs and derived personal-history traces are excluded. The reference paper discusses five histories; only its three synthetic seed 600/601/602 histories are included. Multi-gigabyte expanded native logs are excluded. This research update adds no application release and does not change the **0.2.4** executables.
