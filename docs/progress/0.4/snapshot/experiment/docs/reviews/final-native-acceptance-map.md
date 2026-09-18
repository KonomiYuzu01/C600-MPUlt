> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Final native acceptance map

2026-09-17. Independent source and saved-image review; no native input, builds or new mathematical tests were run for this review. G1/G2 user approvals remain the recorded approvals, not an outcome of this document.

## Initial high-impact findings

1. The inspected default full harness still expected locked Next to survive a successful insertion (`PostApprovalNativeRegression.cs:227–228`), contrary to current 08 and the passing endgame assertion at `EndgameNativeChecks.cs:80`. Its focused intents/recommendation/endgame branches return early (`:155–159`); these helpers were absent from the default full flow. Root is correcting the superseded expectation and composing those suites with explicit fixture boundaries. This is a test-contract defect, not evidence of a new product Next defect.
2. The default flow contained two insertions and one actual worksheet load, not the required ≥30 continuous mixed work cycles and ≥10 genuine reuses. Command-union coverage at `PostApprovalNativeRegression.cs:152–153` is useful but does not visit all 175 physical banks, render their actual caps, or exercise the key editor. `FunctionsNativeChecks.cs` primarily writes settings and calls the input router directly. Root's separate proposed 32-cycle/16-reuse continuous manifest is appropriate; resets between regression scopes must not count as that run.
3. Session entry/reset/scramble/completion and whole-desktop layout remain separate final gates. The experiment hides the inherited menu (`ExperimentShell.cs:127`), instantiates `ExperimentBridge`, and does not instantiate `NativeWorkbench`. The latter's scramble/timer/report methods are therefore not an exposed experiment workflow. Current experiment registry offers checkpoint, restore and one reset, not the complete S12 entry/reset/summary flow. This is now an assigned integration task, not an accepted delivery limitation.

## Evidence identities and limits

- Current native record: `native-baseline/postapproval-g2-20260917-002905/{build.json,report.json,run.log}`; `scope={mode:g2,focus:endgame}`, 104 assertions, exit 0, 123 immutable inputs unchanged; executable `ecfce866afe0025a0f2e4899cc25ddb4522472b71f00248278d4fea92a3c94c5`.
- Only this native record is current-passed in the reviewed validation index. Earlier full/keyboard/intent/recommendation captures are stale, partial or failed; their historical observations are not current final acceptance.
- 002905 reports Solve bounds `(440,150,840,570)` in a `(0,0,1280,720)` working area. Actual PNGs are 1920×1080. That does not by itself establish the process DPI-awareness mode or a passed 100/125/150/200% matrix. Root separately observed Intel HD 620, driver 31.0.101.2140, 1920×1080 desktop; this is not evidence of a discrete GPU or 2560×1600 hardware.
- Current headless evidence: scoped passing commands 1–7 from `evidence/endgame-integration-/20260916-235948/` (64 tests, aggregate failed and remains failed); candidate/reference API batch `20260917-001749`; manual 35-orbit/59-stage batch `20260917-002047`; reference matrix `20260917-003749`. These do not prove native input or human solving.
- Initial harness source identities: `PostApprovalNativeRegression.cs` 0D4B4D1D…4B976F; `EndgameNativeChecks.cs` E44DA2D4…8EFD2; `run_postapproval.py` 727F1E46…F519CA. Root is changing the harness; this review does not grant a future build a pass.

## U01–U29 coverage map

“Current partial” means the exact current native slice proves the named subset. “Source/historical” means an existing helper or old observation must be run on the final artifact. Headless results remain a distinct evidence level.

| Gate | Current evidence and remaining native check |
|---|---|
| U01 locked Current/Next while inspecting | Current partial: explicit X inspection survives inspecting another position; work hash/bindings preserved. Repeat graph/local selection, hidden objects and keyboard inspection without assigning Current. |
| U02 move/undo/redo/resume Next | Current partial: commit consumes cross-orbit Next, follows its identity and restores context. Update old “no promotion” clause to approved 08. Add undo/redo and checkpoint reopen with a moved locked identity. |
| U03 hidden required/Next/protected | Source/historical: Grip/filter and reason-evidence helpers. Rerun hidden conflict locate without changing filter or click-through; keep Next/protection visible when tools open. |
| U04 changing intent during analysis | Current partial: changed goal withdraws candidate comparison. Still test an actually in-flight result against changed target/frame/policy; rejected busy changes must not falsely appear selected. |
| U05 ABA/reconnect stale permission | Current headless ownership/context recovery; no current native engine reconnect/identical-label ABA sequence. Assert old review and token unusable after each. |
| U06 unrelated pending preview | Source/historical worksheet and macro-close helpers retain pending preview. Rerun failed check and cancel with exact old token/draft retained. |
| U07 held input across ownership changes | Current repeated modal-entry/footer checks do not hold a key. G1 feedback/compatibility and isolated IME cases exist. Run bank switch, text/IME, actual deactivation, close/reopen while held, then require release before a fresh action. |
| U08 text and Global Enter | Source/historical text guards exist. Type through the actual focused editor, then press Enter in Global; verify no puzzle commit or hidden command. |
| U09 banks and two unfinished contexts | Current partial: orbit 33→22 and return preserves exact draft/source/Local center and key set. Add two unfinished edits, return through explicit bank picker, then inspect both. |
| U10 bank/macro edits/import | Current partial: explicit endgame save and canonical macro restoration before Add. Key-editor conflict/replacement/null scopes and local-file import/export UI remain untested on this build. Headless reference import is narrower. |
| U11 capture versus camera/filter | Source/historical captured C55, nondefault frame and Numpad cases. Exercise camera and filter changes while observing actual cap/frame and exact cycle symbols; no recapture/rebind. |
| U12 2-cell/18-cap and 20-cell | Current slice covers only a small orbit sample. All mathematical structures have headless evidence; explicitly expose every required cap at 2-cell/18-cap and inspect actual 20-cell 433-sticker Local. |
| U13 nonstar/orientation-only | Current saved placement macro is not orientation-only acceptance. Rerun actual orientation-only and mixed body views with true classification, visible fixed-position change, no fabricated cycle. |
| U14 inverse/local/full comparison | Current partial: explicit C1→C7 frame compilation and complete-label equality; headless API/matrix current. Rerun inverse/local/full compare and saved variant selection/Cancel without auto-insert. |
| U15 body versus complete safety | Current headless protection/recommendation integration. Rerun body-safe/complete-unsafe and reverse in native Findings and graph; only complete policy permits execution. |
| U16 Net versus Strict | Current headless exact/position protection cases. Rerun temporary-motion/zero-net operation and explicit unchanged-position reason; Strict unchecked/unknown never appears safe. |
| U17 worksheet reuse | Current headless sheet checks; historical native one-load path. Continuous manifest must include ≥10 genuine later uses with visible saved/current inputs, distinct fresh context/review, no implicit rebind. |
| U18 satisfied Next | No current native case. Make the locked identity already satisfy its requirement without explicit Next action; show that state truthfully, preserving bookmark until approved successful-insertion promotion. |
| U19 unrelated Next metadata | 08 explicitly binds locked Next identity/destination into ReviewContext. Do not retain the old test expectation for a real binding change. Display-only metadata/camera may retain evidence; no separate display-name metadata field should be invented solely for this test. |
| U20 relative block away from Home | Headless block/position requirements; no current native transported block demonstration. Show explicit captured target labels and mode, review against those labels, never substitute Home or claim rigid motion. |
| U21 each orbit's residuals | Current 35-orbit/59-stage headless manual witnesses plus reference matrix. Native bounded family/parameter workflow passed; representative orientation/buffer classes and coherent final continuous chain still required. Not arbitrary-state completeness. |
| U22 malformed filters/imports | Source/headless rejection coverage exists. Native invalid/cyclic/oversized input must retain correction text and state; local file cancel and bounded size errors remain UI checks. |
| U23 cancel/disconnect/lost commit reply | Source StopAcknowledgement and current headless atomic/receipt recovery. Rerun actual Stop button, late stop barrier, lost response and authoritative refresh; no automatic retry/duplicate execution. |
| U24 detach/minimize/screens/DPI | Current endgame minimum-size checks and earlier reusable-window helpers. No complete measured DPI/monitor/renderer-resource matrix. Restoration must retain manual bounds unless offscreen, and no hidden window should retain needless rendering. |
| U25 New/Resume/reset/summary | Concrete missing experiment entrypoints identified above; assigned for integration. Needs ordinary launch and resume, three reset scopes, reproducible scramble, exact real completion, exclusion of reset/import/replay, and no duplicate completion after undo/redo. |
| U26 keyboard-only normal work | Command union is current, but key helpers use synthetic router calls and many fields are assigned directly. Add actual command discovery/editing, Tab/Enter/Escape dialogs and a keyboard-only repeat chain; label synthetic versus native-message input accurately. |
| U27 duplicate old entrypoints | SolveWindow helper verifies macro/operation aliases resolve one reusable Solve. Current source still contains original G1 separate presentation; inspect exposed commands rather than delete useful alternate modes by text search. |
| U28 rapid input while busy | Isolated input tests and stop barrier exist; no current integrated burst acceptance. Send bounded down/repeat/up while a known pending job runs; assert explicit rejection/no queued turns and correct release state. |
| U29 independent approvals | User approved both a156 G1/G2 samples on 2026-09-16. Preserve that fact and artifact scope; current agents may report verification only, not renewed human approval. |

## Minimal harness extensions

Keep one harness owner (root) and the existing runner/source manifest. No new UI or alternate workbench is proposed.

1. Compose isolated smoke helpers with explicit fresh fixture boundaries, correct automatic Next expectations, and record which helpers actually ran. This is regression coverage, separate from the continuous run.
2. Add one 175-bank enumeration helper. Assert exactly 35×5 stable IDs and explicit purpose; adopt each bank, read effective map/actual key controls, preserve all work, verify missing capture stays visibly unassigned, and ensure cap/frame labels use that bank. Visit the 18-cap case with every bound key reachable. Do not execute 175 arbitrary turns or re-prove every generator. Shared Functions/core/legacy groups have separate checks and are not included in the 175 count.
3. Add one actual key-editor round trip: choose an action in Commands, capture a free key, cancel (no mutation), apply bank-local route, see it on OSK and invoke it, reject Grip/Twist conflict, restore; repeat shared route overridden by local null. Use UI controls rather than only posting the resulting JSON.
4. Run the proposed 32-cycle/16-reuse manifest through existing Solve/draft/worksheet/review/preview/execute. Log cycle index, structure size, goal, fixed method identity, input context, review/pre/post/head, bank, protection and Next transition. One cycle is an actual chosen operation with observed outcome, not a toolbar click, identity turn or repeated unchanged load. Preparation/reset of fixtures is not counted. No reset between continuous cases; the manifest's inverse-built initial setup is disclosed.
5. Parameterize final layout/input run by actual environment, not a guessed scale. Record physical desktop and working areas, per-window DPI awareness/DPI and client bounds. For 100/125/150/200%: same scene, actual default/min windows, dense names, 18 caps/custom keys, expanded chords, long operation and macro list, owned modal. Resize does not simulate DPI. Mark unavailable monitor transitions unrun instead of substituting rectangle arithmetic.
6. Keep ≥44px usable key hit areas, preferred text bounds, explicit scrolling for supplementary content, fixed execution strip and visible Current/Next/protection. Add top-level occlusion checks/screens; ancestor clipping alone cannot prove the target graph is visible behind another Form. Capture actual desktop with active message loop and verified foreground, not just DrawToBitmap.
7. Add U25 to the same native runner after its genuine endpoints/UI exist. Observe entry/resume and reset outcomes, not only backend solved flags. Use immutable completion identity plus source/kind to distinguish true completion from reopening or replay.

The existing 480-second subprocess timeout may be insufficient for combined checks plus continuous work. Give separate bounded runner scopes and explicit measured timeout budgets; do not silently truncate a required scope or interpret timeout as a pass.

## Saved desktop observations

All paths below are under `native-baseline/postapproval-g2-20260917-002905/`.

- `desktop-endgame-solve-desktop.png` and `desktop-endgame-candidate-order-desktop.png`: upper Current/Next mathematical identities and Solve footer protection/focus remain legible; shortened tooltip is three lines and no longer spans the graph. However, Solve occupies x≈672 onward in the physical image and conceals the selected graph's right-hand token/edge. This is real obstruction, not proof of invalid graph data. A useful final-layout check must keep the inspected operation endpoint/target visible beside Solve, or provide an explicit temporary tool-hide/return sequence in the demonstrated workflow.
- Candidate screenshot: four ranked entries show category and score; selected macro hidden by the filter is explicitly disclosed. The unfiltered list in the save screenshot truncates long source-group/name suffixes, making adjacent Star entries hard to distinguish. Full selected details exist, so this is a discoverability limitation to judge at final small-window review, not evidence that canonical selection is wrong.
- `desktop-endgame-reference-desktop.png`: both cells/ordered frames, exact verification scope, Current/Next and protection are readable; modal fully covers the graph it refers to. Disabled buttons have very low contrast. Do not count this screen as visual simultaneous comparison of source/destination geometry.
- `desktop-endgame-cross-orbit-desktop.png`: A/B/Target mathematical addresses are visible, no former header-only text loss; Next is truthfully unset, review stale rather than falsely safe, and Stop check remains reachable after wrapping. Right-side graph context remains obscured by Solve.
- These images do not show the keyboard, actual 20-cell Local or Global. They cannot certify those tools, physical key responsiveness, all glyphs or continuous solving. The earlier unexplained focus-caption failure remains recorded despite later repeated passes.

## Recording guidance

Use short chapters, one operation → one critical feedback → one actual result each: (1) choose/inspect identity and roles, (2) visible key set plus Grip/Twist/frame feedback, (3) macro body versus complete effect and manual preparation, (4) worksheet reuse and protection review, (5) explicit execution/Next and return to previous orbit, (6) buffer/orientation finish and true completion/save/review. Keep unknown/conflict/cancel recovery as short genuine supplements.

Place one or two short English caption lines in an added outer video band or a verified empty margin; never cover Current/Next at the top, the lower protection/execution strip, keyboard rows or graph endpoints. Use explicit labels such as “Choose a reference”, “Check the complete operation”, “Next becomes Current”. Do not repeat the entire UI or call scripted operation a human solve. Retain source footage and an edit map; mark waiting/loading cuts or speed changes. Keep the user's meaningful input, corresponding state feedback and final result visibly connected.

## Next ownership

Root owns native runs, harness integration and product corrections; identity owner supplies the finite continuous manifest. This reviewer now owns only the separately assigned Session frontend partial/keymap/help additions after root releases product changes. Packaging/lifecycle acceptance remains the designated owner's scope. This review is a coverage map, not a release approval.
