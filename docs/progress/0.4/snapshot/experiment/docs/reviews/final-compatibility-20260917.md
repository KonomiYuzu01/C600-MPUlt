> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Final compatibility correction cycle

This bounded cycle connects retained log and keybinding file operations. It does
not introduce another Session, rewrite old files or certify a human solve.

## Implemented and independently reviewed

- C600 JSON/gzip and MPUlt log inspection, explicit version-bound import and file
  export use retained codecs, native mapping and atomic Session journal/recovery.
  Current preferences/drafts remain; any old execution review is invalidated.
- Importing Home labels does not create a solve-completion receipt or popup.
- A completed import remains reported as accepted if later display generation
  fails. Unresolved stop/display failures remain visible; disabled input is not
  disguised as an import failure requiring another write.
- Keymap files retain exact canonical binding overrides, schema/model and scope.
  Inspection precedes explicit whole-override replacement. Unknown fields,
  commands, collisions, changed inputs and stale comparisons are rejected.
  Captures, frames, macros, Current/Next and puzzle state are outside file scope.

## Evidence

- `evidence/session-log-workflow-validation.json`:12 new log +11 retained helper
  checks, real model and verified retained MPUlt profile, unchanged bindings.
- `evidence/endgame-integration-/20260917-023221/result.json`:31 checks across
  adapter, keymap file rules, complete default route catalogue and Session routes;
  exit0 and unchanged inputs.
- `evidence/endgame-integration-/20260917-023247/result.json`:4 installed handler
  tests with real atomic imports and injected display producer failures. Passed,
  unchanged. Display producers are test doubles, not native rendering evidence.
- `native-baseline/postapproval-g2-20260917-023401/`:142 assertions before a
  keymap-confirmation focus failure; whole run exit1, not a pass. The separately
  completed `session-log-native.json` is passed and covers C600/MPUlt full-label
  roundtrips, pending Check/Cancel, stale file/state,
  malformed input, valid2.2MB upload, undo/redo/recovery and solved-import suppression.
  Inputs remained unchanged. Actual desktop log-import capture inspected by root;
  remaining renders are identified control captures.

The keymap failure followed a successful readonly check while application focus
was absent. The guard correctly withheld the confirmation. A targeted test now
observes both active Form and actual Windows foreground before beginning; it does
not reactivate during analysis or remove the product guard.

`native-baseline/postapproval-g2-20260917-024123/build.json`: focused keymap rerun
exit0,16 assertions, all inputs unchanged. The real E1 Current/Next/draft/puzzle
context survived file inspection, cancellation, wrong-model rejection, stale
replacement rejection, successful replacement, re-export and original-file restore.
Root inspected the actual1920x1080 desktop capture of the confirmation. No broad
UI/performance/human-usage claim follows. The earlier combined exit1 remains a
failed aggregate; only its completed, bound log sub-result is reused here.

This correction/verification cycle is closed and paused at the user's request.
No independent solver walkthrough, latency run, final package or film was started.

## Pause boundary

User requests pause after this cycle. Do not begin the new independent native
solver review, continuous32-cycle/16-sheet run, latency measurement, packaging,
recording or publication until the user resumes. Preserve final subtitles,
action/feedback/result pacing, labelled cuts/speedups, mathematical/reference
checks and all other outstanding acceptance requirements in the existing plan.
