> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Conditional workflow planning and atomic Session data

Scope: real-model isolated Python checks. No native interaction, power-loss test,
full solve, automatic solver or user acceptance claim.

`red.log` records the missing helper/optional Session API failure. `green1.log`
records the initial 12 passing checks; `green2.log` records 15 passing checks in
6.041 seconds, exit 0. Eight explicitly listed source inputs were hashed before
and after each green run. These receipts do not replace the full core/reference/
crash regression or the root's adapter/native integration verification.

Executed command: bundled Python `-m unittest discover -s tests -p
'test_workflow*.py' -v`, from the experiment. The command's complete output and
exit value are retained separately; no personal Session was used.

## Contracts supplied to the adapter owner

- `capture_orbit_context(workspace)` and `switch_context(model, workspace, orbit)`
  return detached work. Retained fields: Current, destination/requirement, A/B,
  draft and source receipts, reference, block, goal, optional `selected_macro`
  bound record and `operation_generation`, and `local_view.local_center`.
  Bank, previous bank, input/phase, prefix, global window bounds and protection
  are not restored from an old orbit context.
- `block_candidates(model, state, workspace)` uses unmet explicit requirements
  in user order; otherwise only an all-Home block can expand through actual
  shared hosting cells. It does not use affecting caps, execution-tree paths,
  screen order or macro selection. Buffer roles are identified; automatic ordinary
  selection does not silently substitute a buffer task.
- `plan_commit_continuity(model, before, after, workspace, *, source='work-sheet',
  completion_evidence=())` returns workspace, transition, candidates, conditional
  completion plans, planned protection additions, later-orbit suggestions, and
  a bounded event context. Only explicit Insert/Place false-to-true transitions
  advance Current; locked Next wins, including another orbit and an already
  satisfied bookmark. Other intents and history/reset sources do not auto-target.
- Completion inputs are the existing model/frame/name/context-bound After
  residuals. Raw labels must independently match the supplied post-state;
  unknown frames, outside pending action, stale pair/recipe and unchanged solved
  state cannot newly lock an orbit. Plans remain `Conditional`; the root must
  derive the actual Current certificate after a successful atomic commit.
- `Session.commit(token, *, preference_changes=None, event_context=None)` retains
  legacy token-only calls. `_validated_prefs(changes, *, state=None)` performs
  the existing checks without publishing or writing. With optional workflow
  data, the existing transaction stores event/head/new preferences and
  `event:{id}:workflow`; the periodic snapshot uses the same new preferences.
  Receipt version is `workflow-transition-v1`; model/pre/post/source orbit must
  match the transaction, and serialized receipt size is limited to 32,000 chars.

## Observed boundary evidence

Real legal states exercise exact requirements, a stationary C2 orientation,
cross-orbit Next, retained macro/draft/reference/Local center, and deterministic
shared-hosting candidates. Failure/cancellation leaves inputs unchanged. A child
process exits with code 79 after SQL commit and before its response; reopening
recovers exactly one event, its final preferences and matching receipt. This is
process-exit recovery, not hardware power-loss evidence.

Undo/redo still change mechanics while preserving current preferences. Explicit
checkpoint restore loads its saved preferences, without rerunning transitions.
Automatic protection and Next are not yet wired by this helper-only artifact:
the root owns adapter/GUI integration, guard invalidation, held-input release,
selected-macro persistence, operation-generation receipts and actual certificate
adoption. No separate history database or persistent puzzle state was added.
