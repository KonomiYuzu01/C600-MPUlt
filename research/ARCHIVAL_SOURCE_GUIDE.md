# Archival source guide

The technical report draws on the privately retained 52-page *600 Cell Solving Strategy* design transcript and the supplied 51-page *Full 600-cell: 35-orbit algorithm reference*. This guide identifies technical page ranges without reproducing private exchanges, account links, screenshots or original solve histories. Page numbers refer to the exported transcript, not the new report.

The public algorithm PDF is unchanged. `Full_600cell_Curated_Reference_and_Replay.zip` contains complete words for all 35 orbit seeds, model/frame tables, a standalone replay verifier, three synthetic seed-600/601/602 histories, and clearly separated historical and fresh validation results. The original paper discusses five histories; the other two, the source solve log, derived personal-history data and giant native expansions are outside this curated scope.

## Technical source map

| Transcript pages | Technical content | Retained public evidence and boundary |
| --- | --- | --- |
| 1-18 | Full versus simplified census; stable cap/sticker addresses; legal generators; collateral-aware seed words; antipodal buffers, guarded oriented setups, placement and final-buffer orientation; reduction, blockbuilding and orbit-first alternatives; initial toolchain design. | Algorithm PDF, all 35 exact-word files, `algorithms` tables and `engine` model/controller data. This construction predates the later supplied historical solve log. Its original native integration proposal was not a Windows execution result. |
| 19-24 | Analysis of a subsequently supplied simplified solve; native token/count interpretation; short recorded algorithms and a 60-state corner-orientation construction; the residual produced by lifting simplified moves to the full model. | The report and unchanged algorithm PDF retain the relevant methodological discussion. The original log, mined catalog, lifted state and cleanup history are excluded. Attribution and historical announcements do not substitute for a redistribution grant for a complete third-party history. |
| 25-31 | Definition and primitive cost of a star; 35 orbit cards; parity/orientation cases; compact versus expanded histories; stage workloads and human-throughput scenarios. | Exact words, three synthetic compact records, `replay_logs.py`, historical `audits`, and fresh `validation` reports. Fresh replays check complete collateral effects and all 259,800 labels. Neither stream-format checking nor these compact replays constitutes native animation of every represented primitive. |
| 32-41 | Browser workbench: full-state/view separation, exact filters, certificates, buffer analysis, keybindings, assistance counters, checkpoints, backup, proof export and early performance limitations. | Current repository state, session, filter and verification sources preserve the architectural lineage. Earlier browser/container measurements remain historical; they are not native Windows performance measurements. |
| 42-46 | Compact interface and original-MPUlt native-host route; source-project references; shared filters, macros and workflow tools; remaining differences between frontends and limits of the initial tests. | Current native sources, runtime-provenance documentation and `tests/VALIDATION.md`. Later 0.2.4 results are cited separately from these prototype integration claims. |
| 47-49 | Splitter-construction defect; coherent state/color/style snapshots; startup, shutdown and error handling; the distinction between cached full-state replay and actual native rendering. | Native layout/snapshot/lifecycle sources and regression fixtures, together with the later actual-Windows validation summary. The historical fix and source-level tests alone did not establish successful DirectX rendering. |
| 50-52 | Windows virtual-environment launcher PID versus interpreter PID; authenticated readiness; owned-child shutdown, parent-pipe recovery and explicit UTF-8 diagnostics. | `engine_process.py`, lifecycle and packaged-command regressions, and final frozen-application validation. Private process logs, screenshots and launch credentials are not public attachments. |

## Separate later evidence

The independent `verify_color_bound.py` and its `results.json` accompany the new report separately from the frozen reference ZIP. They verify the selected pure controllers, guarded reachability and conservative face-color lower bound; that new computation is not attributed to the original transcript.

The expanded report adds explicit group-action and orientation formulas, pure-star and triangular-preservation proofs, word-metric and scramble-support bounds, a complete 35-orbit controller-cost table, and a detailed analysis of the published software. `verify_extended_derivations.py` and `extended-results.json` separately document the new arithmetic and finite-group checks. Structural tree checks do not replace the full geometric and oriented-transition certificates. The software analysis identifies inspected application revision `7f1e250571e39c149434686641d1fbc6497c7905` and distinguishes source contracts from recorded native tests.

In particular, the 46,648-turn worst-case lower bound, the short-scramble total-variation bound, and the constrained ambient upper count are new report-specific developments. They are not claims taken from the original conversation, Nan Ma's historical solve, or ivan216's projects. The algorithm paper and curated ZIP remain unchanged.

Likewise, Studio 0.2.4's native checks and measured adaptive/full-detail behavior are subsequent engineering evidence. No retained result certifies a minimum GPU for continuous full-detail 1080p/30 FPS, a human solving record, or a primitive-by-primitive native replay of the enormous archival histories.

## Source identities

SHA-256 fingerprints identify retained bytes; they are not signatures of authorship.

| Material | SHA-256 |
| --- | --- |
| Private 52-page transcript | `ef2d3146cbdeacae4bf17fe41afa064ae07f93f1b1ed67298930c76335613177` |
| Unchanged 51-page algorithm PDF | `5b8761a38527451217b1eb6351b5cb8b4e81491ae8c248b392f1cc5f38d6d530` |
| Curated reference/replay ZIP | `85d2d9a6b337442aae10b7c26056f518ce0d0a738e2f098d47d8a52678d0ec5a` |
| Retained model (`model.npz`) | `680de6710e8a05b12dd4ba2ea22c866e645af032a31faf985d869798521f8b13` |
| Independent bound checker | `c9c7e695445ec0f3d2194b826d059b2bfa130f966dacc1fe898bca9cb0d51c6e` |
| Independent bound results | `ff440bc7268303fc467ed3765338bbc5e61cd55897d807092f5d00013a8ba45b` |

Andrey Astrelin retains primary credit for MPUlt. The source context also acknowledges ivan216 and Nan Ma. C600-generated arguments and code are distinguished from upstream authorship and from the private conversations in which the work developed.
