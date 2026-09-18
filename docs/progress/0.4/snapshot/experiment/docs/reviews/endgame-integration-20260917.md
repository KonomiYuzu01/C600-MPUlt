> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Manual endgame integration — 2026-09-17

This cycle extends the existing Session, Solve/Macro Base, graph and command sets.
There is no separate endgame workbench or parallel puzzle/history store. Finite
construction parameters and reference frames remain explicit user choices.

## Changes and evidence

| Scope | Evidence | What it establishes |
|---|---|---|
| Known endgame families | `evidence/endgame-library-validation.json`, `endgame-library-matrix.md`, `endgame-library-coverage.json` | 7 tests; 35 orbit catalogues, 299 finite legal witnesses with complete net collateral; necessary invariant exclusions remain distinct from available solutions. |
| Reference variants | `evidence/reference-variants-validation.json` | 10 tests; explicit proper source/destination frames, real legal generator words and full-model conjugate-action comparison. New words require their own prefix protection. |
| Session atomic continuity | `evidence/workflow-continuity-20260916/`, `evidence/core-contracts-/20260916-234601/` | Existing journal commit atomically stores the work-context transition and compact residual delta. Core, reference-map and crash checks passed after Session changes. |
| Integrated regression | `evidence/endgame-integration-/20260916-235948/` | Commands 1–7 passed: 6 endgame, 21 protection, 6 ownership, 6 recommendation, 10 residual, 10 intent and 5 worksheet checks. The complete batch failed at commands 0 and 8; those failures are retained. |
| Candidate and reference API | `evidence/endgame-integration-/20260917-001749/` | 10 candidate and 2 reference integration checks passed with unchanged inputs. Candidate comparison neither changes the draft nor grants execution permission. |
| Default phase order | `evidence/endgame-order-collateral-audit.json` | All 299 catalogued witnesses have 1,202 collateral edges strictly later in existing `core.ORDER`. This does not certify Strict prefix safety or a user's different macro. |
| Endgame/reference integration | `evidence/endgame-integration-/20260917-003749/`, `evidence/endgame-reference-matrix.json` | 59 explicit constructions across35 orbits under C1→C7 proper frames; all259800 labels, A/B/X/Y and complete collateral checked. Single bound run69.875s,58 inputs unchanged; largest98380→98444 primitives. Not all frames or emitted-prefix protection. |

The two earlier test failures had incorrect premises: a position-only cleanup was
expected to satisfy exact insertion despite intentional remaining orientation, and
a candidate fixture invented executed ownership without its journal receipt. The
revised tests use explicit PlacePiece/Home requirements and a real committed event;
the product's goal and ownership checks were not relaxed.

## Continuous path scope

`tests/test_manual_residual_paths.py` constructs legal start states by reversing a
fixed, explicitly chosen sequence of known operations. Its initial successful run
`evidence/endgame-integration-/20260917-001453/` covers all 35 orbits and 59 stages:
35 placement, 11 nonbuffer orientation transfers, 11 Buffer A operations and two
nonidentity final-B examples. All 259,800 labels return Home at each path endpoint.
Current is derived from committed state; After remains a conditional simulation.

Unfinished orientation recovery is checked for C2/O22, C5/O17, D5/O6 and A5/O34:
checkpoint, close/reopen, undo, redo and restore compare actual labels, at/where,
residual facts, journal head and operation ownership. The recorded later-orbit
protection releases are explicit test actions, never automatic policy changes.
Independent review requested adding actual earlier-orbit protection throughout
these paths. The stronger bound run `evidence/endgame-integration-/20260917-002047/`
passed in 80.822 seconds with unchanged inputs: all earlier ORDER orbits remain
explicitly protected and their labels unchanged across commits, recovery and the
recorded later-only releases. `evidence/manual-residual-paths.json` holds this latest
result. These are agent-operated fixtures,
not arbitrary-state reachability proofs or human completion evidence.

## Native review and corrections

The existing Solve Macros page expands the known-family selector. Existing
Current/Operation/After graphics and Prepare/Macro/Cleanup remain authoritative.
Optional residual/effect/journal dialogs only inspect details or locate an object.

The first 73-assertion run `postapproval-g2-20260917-000202` observed working
selection, compact controls, reference inspection, position-only/exact locks and
cross-orbit Next/return. Help changed during that run, so it is developmental
evidence rather than final unchanged-build acceptance.

Independent actual-screen review found family/X/Y scrolled out of context, an
ambiguous leading reference-verification caption, and an incorrect tool-editor
focus caption. The existing result label now retains the chosen family and objects;
reference results lead with net-effect-only and protection-not-reviewed wording.

Native `postapproval-g2-20260917-001138` reproduced a more important defect: same-orbit
checkpoint restore returned the backend macro binding but left the previous macro
selected in the UI. The native snapshot now adopts the full authoritative binding
before effects and forecasts, clears mismatched results, and rolls back rejected
optimistic selection. Camera/redraw does not rebind selection. Independent source
review found no further blocker in that correction; fresh native replay is required.

Native `postapproval-g2-20260917-001749` stopped at the new owned-editor focus assertion.
Do not claim that focus correction passed. A follow-up captures actual foreground
ownership and the readout before/after capture to distinguish event timing from
an incorrect focus source. No safety or goal checks were loosened.

## Remaining acceptance

Native `postapproval-g2-20260917-002202` passed 98 checks with all 123 bound source,
model, tool and runtime inputs unchanged. Executable SHA256:
`ee2af289e1873796f614847654ff0a80304de6d25f5523fedfd8786d8de67d66`.
The real restore → selection → Add sequence now uses the saved macro, withdraws
the old macro effect/forecast and preserves the authoritative work context.
Candidate ordering is visible in the existing catalogue; empty filtering retains
selection and work, while a changed goal withdraws the prior comparison. Four
explicit candidates took 1,739 ms end-to-end and returned 30,435 result bytes in this
single fixture; this is not a general latency benchmark. Residual/journal details
and cross-orbit context return also passed.

The unchanged product in this replay showed the correct owned-dialog focus caption
both before and after verified foreground desktop capture. This is a successful
observation, not a proven explanation or elimination of the earlier single failed
focus assertion. Repeat focus transitions during final continuous acceptance.

The final follow-up `postapproval-g2-20260917-002905` passed 104 checks with all
123 immutable inputs unchanged (executable
`ecfce866afe0025a0f2e4899cc25ddb4522472b71f00248278d4fea92a3c94c5`).
It shortens the catalogue tooltip after actual-screen review found it spanning
the graph and candidate scores. The new three-line hint stays near its list.
Three additional editor entry/cancel cycles kept actual focus, its readout and
all work identities consistent. The earlier failed assertion remains recorded;
these repeats do not retroactively establish its cause. This is the current
focused native receipt; the earlier 98-check build is superseded by these small
source/test changes. No further product files were changed after this run.

Captured environment: existing Windows/MPUlt/WinForms/DirectX runtime, 1920×1080
actual desktop, 1280×720 logical main window and 780×570 compact Solve. Bitmap control
captures and actual desktop captures are separately named; no 2560×1600 hardware,
GPU-latency or human-usability result is claimed.

Final continuous native acceptance, measured latency, all required window/scaling
scenes, packaging/compatibility and the concise continuous solver recording remain
separate work. No intermediate distribution or public release was produced.
