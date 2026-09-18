> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Final Session native integration

Scope: retained Session/Workflow entry points in the experimental native shell.
Root alone operated the native fixture. G1/G2 approval remains the existing user
approval; these checks are agent-operated acceptance, not human solving evidence.

## First native pass

`native-baseline/postapproval-g2-20260917-014758/build.json`: exit0, 70 assertions,
all input hashes unchanged during the run. The executable compiled against the
actual MPUlt/WinForms/DirectX bridge. It exercised New and Resume, explicit shared
timer controls, seeded scramble staging without movement, legal Star/inverse
transactions, exact completion receipt and automatic summary, persistent ACK,
undo/redo suppression, explicit reopening and reset/new distinction.

Separate adapter evidence: nine tests in
`evidence/endgame-integration-/20260917-014751/result.json`.
Session helper: `evidence/session-workflow-validation.json`.
Core/reference/crash after the base reset hook:
`evidence/core-contracts-/20260917-013431/result.json`.

## Verified native refinements

The real control renders `session-report.png` and `session-completion.png` showed
all readonly text selected on entry and a misleading locked Next label while
no bookmark existed. Both were corrected without changing identity state.
Source review also found a deferred-completion notice could remain waiting after
a Stop acknowledgement; availability refresh now processes only an explicitly
queued completion, not passive snapshots.

`native-baseline/postapproval-g2-20260917-020650/build.json`: exit0,85 assertions,
input hashes unchanged. Actual readonly selection/focus, default camera reset,
New/Local C1, deferred completion after an owned modal closes, persistent ACK
and no repeated completion on undo/redo/resume/reset passed. Failed focus runs
015522,015836 and020322 remain evidence: initialization in the posted Shown event
was too late for the first visible/active state; setting ActiveControl and text
selection before Show fixed the observed defect.

A Computer Use bounded-window screenshot attempt failed with
`SetIsBorderRequired: 0x80004002 / interface not supported`. No capture succeeded
through that call; the saved initial PNGs are GDI control captures. The supported
FFmpeg desktop capture succeeded for report and summary in020650. Actual desktop is
1920x1080 on Intel HD Graphics620; no high-DPI,2560x1600 or discrete-GPU acceptance
follows from this pass.

## Remaining compatibility connections

Retained log import, MPUlt export profile forwarding and file-based keymap
import/export were absent from the new shell. They are now the bounded correction
scope;85 passing Session checks do not cover them. New retained-helper log checks:
`evidence/session-log-workflow-validation.json` (12new+11retained, unchanged).
Native log/keymap correction results are now recorded in
`final-compatibility-20260917.md`. Continuous and final build acceptance remain pending.

The continuous32-cycle/16-sheet-reuse fixture, final package, full performance
acceptance and new captioned solver film remain pending. A previous full native
run failed clipboard restoration; its independent controlled-contention fix is
in `evidence/clipboard-restore-20260917/DIAGNOSIS.md` and still needs full native
copy/editor replay.
