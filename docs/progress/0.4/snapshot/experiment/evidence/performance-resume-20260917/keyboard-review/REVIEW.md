> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Keyboard redraw review — bounded, read only

Sources: ExperimentShell `78a82867…67ff1`, ExperimentKeyboard `2a608070…c00dc`, ExperimentInput `43925ae5…fca2b`. No product edit or GUI action. Measurements below are existing 052827 copied-source diagnostics, not a new performance run.

## Evidence and call paths

Every bank command has three full RenderPhysicalKeyboard calls; Local has two. Their per-command inclusive sums have medians 522.42 ms and 228.89 ms respectively. FitKeyboardRows is nested within these spans and must not be added to them. Example actual intervals:

| Command | First full render | DrawKeyboard render | Final focus render |
|---|---:|---:|---:|
| 7, bank | 277.62 ms | 57.11 ms | 195.53 ms |
| 14, Local settings | absent | 156.30 ms | 101.80 ms |

The source explains this observed count without assuming duplicate input:

- Shell.Draw:260 → input.RefreshContext → Input.State:162 detects a bank/frame/map change → Reset:138–143 → Feedback → Shell's callback:56 → DrawGripFeedback → RenderGripFeedback → RenderPhysicalKeyboard.
- Shell.Draw:281 explicitly calls DrawKeyboard → RenderKeyboard → RenderPhysicalKeyboard. At this point `busy` remains true.
- SendRoute's UI completion finally:239 sets `busy=false`, then RefreshCommandAvailability:332 → RefreshFocusFeedback → Feedback → the same full renderer. This last call reflects final availability and actual focus. Local changes do not change the router fingerprint, so normally omit the first call.

On a held Grip, Reset also invokes onGrip and VisualChanged, so there may be more calls than this released-input diagnostic. Those effects must remain immediate and cannot be omitted merely because the measured baseline has no held key.

## Recommendation: synchronous adoption batch first

Prefer (a): defer **only full keyboard content/layout regeneration** inside the one existing OnUi response-adoption callback, then perform one final full update. Scope is Shell SendRoute plus Keyboard render entry/flush, with a depth/dirty flag if needed; no new snapshot cache or authority. Keep Input.cs untouched.

Do not batch the network wait. Do not use Shell's current `updating` flag: Draw clears it before final availability changes. End the batch after `busy`/Stop/connected/focus have their final values and after RefreshCommandAvailability, but **before completion.TrySetResult** publishes completion. A ThreadPool continuation can observe Task completion immediately, even before a UI continuation runs.

Keep RefreshContext, Reset, epoch/release barriers, onGrip (including Local/Hub highlight), Feedback text, VisualChanged and physical pressed/rejected drawing synchronous. A render request during the batch only records that a full update is required; it does not queue a command or turn. All paths, including a rejected response, successful-write recovery, disconnected recovery and disposal, must clear batch state in finally. Do not let a render exception suppress the operation receipt or leave the Task unresolved. Stop acknowledgment callbacks and direct physical/mouse/focus input outside that callback remain immediate.

Shift requires care: DrawInputPressFeedback:131 currently changes displayedInverseShift, recursively invokes the full renderer, then returns without updating the ordinary pressed bits in that invocation. A generic early-return deferral must not consume that Shift transition while losing pressed/rejected clears. During a deferred full update, still apply the visual-bit loop; flush the inverse key meanings before response completion. No indefinite dirty flag should survive a closed/disposed keyboard.

This should reduce redundant full traversals, but it does **not** prove the surviving call will have the previous last-call cost: bank-dependent extra-row construction moves into that call. Measure actual full-body entry, not merely RenderPhysicalKeyboard requests that return after setting dirty.

## Why not start with unchanged-property/layout caching

Option (b) is narrower in authority but has more invalidation edges and unmeasured benefit. RenderPhysicalKeyboard:117–124 writes every key, then FitKeyboardRows:51 wraps every caption again using actual column width. Some WinForms setters already avoid identical-value work; current timing does not isolate setter, text measurement and layout cost. RefreshAdditionalKeys already skips structural rebuilding when its effective additional-code signature is unchanged.

Skipping work based on caption alone is unsafe: Enabled/rejection reason, frame detail, cap palette, selected Grip, Shift inverse, focus/hover correspondence and tooltip can change independently. Custom CapColor and SelectedGrip fields need explicit invalidation if redundant setters no longer cause paint. Fitting still depends on font/DPI, actual width/scrollbar, extra-row visibility and context/detail height. A stale “same rows” cache could reintroduce known clipping. If batching leaves one expensive render, measure its loop, RefreshAdditionalKeys, feedback and Fit separately before selecting a local equal-value/measurement optimization.

## Counterexamples and minimum executable regression

Use the actual isolated native Shell, existing 5-bank/5-Local diagnostic plus KeyboardFeedbackNativeChecks; the existing ExperimentInputBankRegression and ExperimentInputFeedbackRegression provide router cases but cannot alone prove Shell batching.

1. **Final state and completion:** for each actual bank/Local request, count one full keyboard-body update after adoption, before the returned task completes; compare every canonical code's caption, Enabled, cap color, selected Grip, accessibility/tooltip, additional-row membership and focused-key detail with an explicit unbatched reference render of that same final state. Retain full 259800 labels, draft, pending, Current/Next and protection checks. Restore the final intended state after any reference-only render.
2. **Bank release versus ordinary Hold:** hold an actual Grip, adopt A→B→A; release is still required and epoch advances as before. An unchanged-context Local update or busy interval must retain deliberate Hold/epoch. No turn may be queued. Existing bank/input feedback fixtures already express these distinctions.
3. **Immediate input:** outside adoption, accepted/rejected keydown changes the actual product key before dispatch; keyup clears it while a task is pending. Test Shift forward/inverse XOR, Shift release during a context reset, rollover, pointer/latch, and bank change while physically held. During an explicitly held batch in the test seam, VisualChanged must still clear stale press bits; final flush supplies the correct captions.
4. **Text/IME/Stop:** text/IME events never become application key presses; no deferred repaint steals editor focus. A pending or uncertain Stop never grants execution when the original task finishes first; retain the existing deterministic delayed-stop admission fixture. No full repaint is deferred across the asynchronous stop wait.
5. **Recovery/teardown:** simulate the existing reply-recovery path and a throw inside the synchronous adoption block; batch state is cleared, final disabled/recovered state is represented, accepted durable receipt retained, and completion finishes. Closing a keyboard/owner during a callback must not create/reactivate a window or leave dirty state to repaint a later unrelated window.

One relevant pre-existing discrepancy: key Enabled branches in Keyboard:118–120 use `connected`/`busy`; InputState.Busy uses OperationInputBlocked (`busy || stopPending || stopFailure`). Thus stop-pending after busy clears can look enabled while the router correctly refuses. Do not treat those Enabled values as execution authority or make the batching optimization relax the router. Root should explicitly decide whether the separate visual-availability correction belongs in the same small change.

Acceptance is a successful source-bound native regression plus a fresh diagnostic showing fewer **actual full-body** updates and lower measured work. Rejected/recovered samples, lost immediate feedback, altered focus/ID/frame/guard state, incomplete traces or a still-expensive single render must be reported. No p95, GPU-completed-frame or physical hardware claim follows from this slice.
