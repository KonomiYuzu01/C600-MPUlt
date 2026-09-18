> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Executed operation ownership

The original five real Session checks failed because recovery attributed retained
drafts by recipe coincidence and Reuse did not persist a new operation generation.
`red.log` and `red-inputs.json` retain that result. A separate first-use context
check then demonstrated a missing generation (`new-context-red.log`).

After the bounded recovery/generation correction, six checks passed in 11.947 s
(`green2.log`, exit 0). The four recorded source/test inputs are identical in
`green2-inputs-before.json` and `green2-inputs-after.json`.

The checks cover exact source attribution despite equal recipes, fresh Reuse and
work-sheet generations, new-context first use, journal ancestry and undo, legacy
events without ownership receipts, and changed generation/recipe rejection.
They deliberately construct a Session receipt and completed-operation marker to
isolate recovery. Root still owns integrating that marker into the actual commit
path; this result does not certify that integration or any native workflow.

The optional `completed_operation` context field binds generation, parent, pre
and post. Recovery additionally checks the real journal event's exact normalized
recipe and matching source/model/generation receipt. No parallel history or new
database table is introduced. Missing legacy ownership evidence remains draft.

Command: `python -m unittest discover -s tests -p test_workflow_ownership_adversarial.py -v`.

Subsequent independent review strengthened the ancestor test to check the live
Workbench immediately after Undo, before reopening. This exposed a separate
remaining defect: undoing an unrelated later live event clears the earlier
work-sheet ownership in memory, although reopening restores it. The focused
check fails in `undo-ancestor-red.log` (1 test, 3.432 s). Root owns that adapter
correction. The green2 receipt binds the earlier test version and must not be
reported as a passing result for the strengthened current file.
