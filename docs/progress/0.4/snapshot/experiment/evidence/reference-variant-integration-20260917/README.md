> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Reference variant API integration

Command: `python -m unittest discover -s tests -p test_reference_variant_integration.py -v`.
Python executable: the existing Codex primary-runtime Python recorded in the task.
Working directory: the existing `work/experiments/magic600-04` experiment.

The final two real-model/API tests passed in 13.096 s, exit 0 (`green.log`,
`green-exit.txt`). All six recorded primary source/test inputs are unchanged in
`green-inputs-before.json` and `green-inputs-after.json`. Final test SHA:
`5722C4B920FA4BCBA70D8859C45180DBB9CF9DA824E638F24646518BC418DC63`.

Coverage:

- Explicit C1 to C7 proper frames, source binding for `[1,2,-7,14]`, complete
  review, explicit independent save, exact expected eight-turn comparison,
  selected export and import into a separate temporary Session. Canonical ID,
  normalized recipe and descriptive provenance survive; source record and
  Current/Next/draft/reference/bank remain unchanged. Import creates no checked
  effect cache, geometric mapping cache or execution authority.
- Mirrored/missing frames, stale/missing source ID/version/recipe and floating
  or Boolean vertex IDs are rejected. Both serialized provenance frames are
  checked at import-check and import. Rejection leaves labels, head/revision,
  pending, work, library and persisted preferences unchanged.

The initial run (`run.log`, exit 1, 18.003 s) passed the rejection test but failed
an incorrect test expectation that the review source receipt contained name/tags.
The actual documented compiler contract returns only `{id,version,recipe}`
(`reference_variants.py`, `compile`). Only that assertion was corrected; no
product change was made. Initial and final input receipts remain separate.

These are bounded headless checks, not a repeat of all-generator proofs, a
native usability check, or complete 0.4 acceptance. The receipt records the six
primary files, not a fresh exhaustive dependency/model-manifest certificate.

## Independent native source finding

Inspected `native/ExperimentShell.cs` SHA
`0C65CD4084E6C355A6ACE237EE1B961FC21859B97AB7EEAC57AEDBEA16F1CFD6`.
At line 243 the native selection reads `Workspace.selected_macro` only when
`macroWorkOrbit` changes. The backend restore correctly replaces saved work and
library (`adapter.py`, restore), and Session restore restores checkpoint prefs.
Thus same-orbit checkpoint A, later selected B, then restore A can leave native
selection B. `DrawMacros` retains that local ID and `UseSelectedMacro` sends it
to insertion. This is source inference, not an observed native run.

Minimal correction: reconcile the full authoritative macro binding on snapshot
adoption before `AdoptPhaseInspection`, clearing old body effect/applicability
when it changes. Rebinding only afterward in Draw risks adopting B's in-flight
inspection under A's new caption. Existing phase guards should reject the old
macro receipt; they must not relabel it.

Minimal native check: select existing A, checkpoint; select existing B and inspect
its body; restore the same-orbit checkpoint. Confirm native selection equals the
saved A binding, B's effect/forecast is withdrawn, and explicit Add inserts A.
Also restore a checkpoint with no selection and require selection to clear.
Current and locked Next must match the restored checkpoint throughout. Root owns
this correction and native verification; no native windows were used here.
