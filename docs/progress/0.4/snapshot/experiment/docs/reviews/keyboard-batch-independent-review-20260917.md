> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Keyboard response batching — independent review

2026-09-17. Reviewer: native_solver_review. Read-only product/test inspection; no native window or model execution in this review. This does not extend the paused wave-c solver coverage.

**Conclusion:** no new product correctness blocker found in the three frozen candidate files. One concrete test lifecycle defect was returned to root and corrected before the integrated compile. Runtime keyboard, exception, lifecycle and timing acceptance remain separate.

## Inspected identities and evidence

| File | SHA-256 |
|---|---|
| candidate/ExperimentShell.cs | c202132c7efc33b6413459c79d96b134bb026f869a4391204197091b57f06215 |
| candidate/ExperimentKeyboard.cs | 2b26fdcc7ed54d31aa0b125443facf66fc6ef4f257471a75bf8a66d48c95dbeb |
| candidate/StopAcknowledgementNativeChecks.cs | e2c4e9329096ece31f3a858807c86e1ac23d6b904910643f5dc5b4f227568478 |
| corrected tests/KeyboardBatchNativeChecks.cs | 2d954856918de284539b5cb3c2797793072829506ddd69668d6a7264de351d25 |
| tests/PostApprovalNativeRegression.cs | 8b2783aaa2f5b0cfb768724340a70e001d4e84ea03440a7fe4f1aac995a6ee2e |
| tests/run_postapproval.py | 1c8e60f63065ed4e3c356e01042ed4d4f9114ced7ffebe1b88fb76cac4478a53 |

Candidate directory: `evidence/performance-resume-20260917/keyboard-review/candidate/`. Read the actual narrow diff, complete changed functions, original input/focus/reset flow, Stop flow, completion notification and window disposal flow. The promoted product and Stop test hashes match the reviewed candidate hashes.

Read `candidate/compile-20260917-055451/receipt.json` and its compiler log: exit 0, inputs unchanged, three CS0649 warnings in unchanged ExperimentInput fields. That compile predates the new KeyboardBatch test. Read integrated `native-baseline/postapproval-g2-20260917-055855/build.json`: explicit compile-only, 125 inputs unchanged after build, run outcome **not-run**. No runtime pass is inferred from either compilation.

## Product analysis

- `ExperimentShell.cs:229–257`: the batch begins only inside the admitted UI callback. The existing adoption path remains synchronous. Its finalizer sets `busy=false`, computes the existing command/Stop availability and then calls `EndKeyboardBatch` in a finally. The returned Task settles only afterward. No new await, queued full render or cached input map was added inside this batch.
- `ExperimentKeyboard.cs:24–31`: the outermost end decrements depth to zero and clears the pending flag before attempting the full render. A render exception therefore cannot itself leave this batch pending. Closing/disposed-keyboard checks skip rendering after those flags are cleared. The shell's nested finally still settles its Task if the final render or its error feedback fails.
- `ExperimentShell.cs:241–254`: an accepted `importReceipt` initializes `accepted=true` before presentation finalization. Later adoption/flush failure disables operation input and retains that durable receipt, with a do-not-repeat warning. The Task cannot become `false` solely because this final full render failed. This is source-path analysis, not an injected failure run.
- `ExperimentKeyboard.cs:128–148`: key availability now observes `OperationInputBlocked`, so an outstanding/uncertain Stop also disables Grip/Twist. The existing allowed tool list remains unchanged. A deferred Shift legend request still falls through to update physical/rejected press bits; it does not return before key-up feedback. The eventual full render reads current Shift state. ExperimentInput routing, context reset, focus ownership and IME handling are unchanged.

## Test finding and correction

**K1 — P2, test lifecycle, corrected in source.** The first KeyboardBatch test called the keyboard toggle to open it and left it visible. In full G1, the next existing `Invoke("keyboard")` then hid it and the immediate visible assertion would fail. It also hid an already-open keyboard when entered in that state.

Root changed the fixture to capture initial visible/active/restoration state, explicitly call `OpenFloatingKeyboard(false)`, and restore visibility and active form after its operations (including an ordinary body failure). The focused keyboard branch reopens the keyboard before retained feedback/compatibility checks. The corrected test was reread and is in the integrated compile-only receipt. This finding does not require a product change. Runtime confirmation of the complete G1 sequence remains not run here.

The new test correctly compares full key properties immediately after `await Send`, before a later `ready()` wait, against a forced full render of that same state. It checks batch flags, real bank/Local changes, an invalid-bank recovery, immediate Shift/key-up bits, labels and solving context. The Stop fixture now explicitly uses a real assigned 33-A Grip, delays acknowledgement through its documented loopback fixture, checks disabled Grip/allowed bank, rejects direct Grip admission and restores authoritative context and labels. These are meaningful test implementations, not results.

## Limits and next validation

- Native `--mode g2 --focus keyboard` and the affected full G1 sequence: **not run by reviewer**. Root owns execution and desktop access.
- Injected final-render failure, accepted-log-import plus UI failure, and host closing between dispatch and adoption: **not run**. Source nesting supports settlement once the callback starts, and existing `OnUi` skip paths cancel before it starts. The unchanged queued-`BeginInvoke` versus host-destruction race is not proven by this patch or these tests; no blanket all-closing-races guarantee is made.
- No new keyboard performance result, physical-key latency, IME run, nonempty-preview run or user usability conclusion. Same-bank/Local diagnostics and affected native tests must bind the promoted files before reporting runtime improvement.
- Preserves GUI boundary: wave-c's old process/session and immutable evidence were not touched. G1/G2 approval remains the user's already-recorded decision, separate from this source review.
