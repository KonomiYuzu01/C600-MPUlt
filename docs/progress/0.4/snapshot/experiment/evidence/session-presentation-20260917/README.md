> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Session frontend: bounded evidence

The new Session dialog reads the existing authoritative Session/Workflow report. Scramble stages into the existing Prepare/Review/Preview/Execute path. New/reset require explicit scope confirmation. No independent puzzle state, timer, or completion certificate is created by the native UI.

- presentation-result.json: 8 checks against ExperimentSession.cs, compiled with stubs for unrelated Shell dependencies; exact before/after hashes and exit 0. Covers formatting, unavailable measurements, completion wording, explicit notification request, modal deferral and same-label/different-head rejection. No window or engine was run.
- keymap-result.json: 2 Session route checks and 8 existing exact keymap compatibility checks, exit 0 with unchanged inputs. New utilities are explicit Functions chords. The legacy Session map is unchanged.
- before.log: original locale-formatting failure; retained as a historical red log, without retroactively assigning a source hash.
- keymap-before.log: new missing-Session-route failure.
- keymap-regression.log: older “utility holes must stay empty” assertion encountered already-authorized Workspace position/exact protection keys. The correction enumerates only KeyK, KeyL and KeyB; it does not weaken other key compatibility assertions.

tests/SessionNativeChecks.cs is supplied for root's serial native run. Run covers input/cancel paths; RunFresh uses a disposable Session and real legal O33 Star/inverse transactions. CloseCompletion can acknowledge and close actual completion popups in the continuous fixture. These native checks have **not** been run by this owner; desktop rendering, actual view reset, focus, final paired backend wiring and completion trigger behavior require that integrated run.

Completion notifications require RequestSessionCompletion() from a genuinely new successful commit. Passive report/Resume/undo/redo adoption cannot create a notification request. ShowSessionCompletion() may be called when ready or an owned modal closes: it still verifies the captured receipt's id, head and state hash, drops stale requests, and preserves the stop barrier and authoritative accepted outcome.
