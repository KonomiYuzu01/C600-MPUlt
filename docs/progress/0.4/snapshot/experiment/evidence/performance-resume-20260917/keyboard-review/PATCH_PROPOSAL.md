> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Copied-source candidate only

Root owns implementation. No product patch was applied by this reviewer. Keep the scope to Shell/Keyboard; leave ExperimentInput unchanged.

## Render batching

Private presentation fields/helpers in Keyboard (names illustrative, no new public API):

```csharp
int keyboardBatchDepth;
bool keyboardRefreshPending;
void BeginKeyboardBatch(){keyboardBatchDepth++;}
void EndKeyboardBatch(){
 if(--keyboardBatchDepth!=0)return;
 bool refresh=keyboardRefreshPending;keyboardRefreshPending=false;
 if(refresh&&!closing&&keyboardWindow!=null&&!keyboardWindow.IsDisposed)
  RenderPhysicalKeyboard();
}
```

At RenderPhysicalKeyboard entry, keep its current null/disposed checks; immediately afterward insert:

```csharp
if(keyboardBatchDepth>0){keyboardRefreshPending=true;return;}
```

The actual body still builds every required key from fresh InputState and performs the existing feedback/fit. No cached key map. A flush clears dirty/depth **before** calling that body, so an exception cannot leave deferred state latched. No BeginInvoke or delayed flush.

Replace only the Shift branch in DrawInputPressFeedback:

```csharp
if(displayedInverseShift!=input.InverseShiftHeld){
 displayedInverseShift=input.InverseShiftHeld;
 if(keyboardBatchDepth>0)keyboardRefreshPending=true;
 else {RenderPhysicalKeyboard();return;}
}
// Keep the existing physical/rejected-bit loop here, including immediate Update().
```

This prevents the current recursive-render branch from returning before clearing pressed bits when the full render was deferred. At the start of a real full render, synchronizing `displayedInverseShift` to the value whose captions are about to be rendered also avoids a redundant end-of-render Shift recursion. Do not defer VisualChanged itself, Reset, onGrip or inputFeedback.Text.

## Exact adoption boundary and failure ordering

Start the batch inside the current SendRoute OnUi callback before response adoption. Do not start it before Task.Factory.StartNew/HTTP. Preserve the existing adoption try/catch (ReadPair/Apply/authoritative work/RefreshContext/Draw and warnings), but move its final task completion outside the batch. The final sequence is:

1. Set busy=false; retain the existing provisional session-new/default-view decision.
2. RefreshCommandAvailability while full keyboard refresh remains deferred. Catch UI failure using the existing connected=false/native-error behavior.
3. EndKeyboardBatch/flush in its own protected finalization path. If flush throws, mark disconnected, pause the bridge and disable work controls; report the display error. Do not recursively call the full keyboard renderer to report its own failure.
4. Publish lastCommandResult and the existing durable import receipt. Form import recovery warnings **after** the flush outcome is known. Import receipt presence remains acceptance even if the presentation failed; no invitation to repeat import.
5. Compute accepted from final connected/fault/phase status, with the existing importReceipt exception. Schedule the existing completion summary once if appropriate.
6. Call completion.TrySetResult(accepted) in the outermost finally. Initialize accepted from importReceipt presence before potentially fallible warning formatting, so a durable receipt cannot become a retryable failure because presentation finalization threw.

The existing skipped OnUi path still TrySetCanceled; it never entered the batch. End must execute even if RefreshCommandAvailability throws. Root should preserve its existing response-error semantics rather than adding an independent rollback/error authority. A Task must not complete before the flush, and no UI exception may leave it unresolved.

## Stop visual admission

For command key Enabled, preserve the **existing explicit allowed-tool list** and reason calculation, replacing busy alone with OperationInputBlocked:

```csharp
connected && reason==null && (!OperationInputBlocked || allowedTool)
```

For Grip/Twist replace `!busy` with `!OperationInputBlocked`, retaining the existing connected, Shift, active-Grip and valid-cap requirements. Do not apply InputState.Enabled as a blanket prefix to allowed tools: stopFailure makes that false and would also disable the previously allowed tool exception. Do not change actual router or Send/RunCommand admission.

## Smallest deterministic tests

- In the existing StopAcknowledgementNativeChecks delayed responder, after received and busy=false but before releasing the acknowledgment, call RefreshCommandAvailability. In the established KeyboardFeedback 33-A fixture, first require a real assigned Grip key is enabled and Shift is released; then require it disabled while the stop barrier remains, an allowed bank/index tool still enabled, direct keydown unable to acquire Grip/dispatch, and release+ack restoring the same key. The existing Context equality checks remain. An initially disabled/unassigned key is not a valid witness.
- In KeyboardFeedbackNativeChecks, reuse actual pressed pixels, hold/latch release, bank-release barrier and Shift inverse cases. Add a held-batch visual clear check and final caption assertion; two deferred full-render requests must produce one actual body update. Count the body after the deferral guard, not the number of requests.
- Run the same actual 5-bank/5-Local diagnostic with full state assertions and trace validation. Bank should normally go from three full bodies to one, Local two to one; held-input/error paths can have additional immediate feedback and must not be forcibly capped. Exact final key properties can be compared with an explicit full reference render of the same already-adopted state outside timing.
- Retain the existing reply-recovery and delayed-stop paths; assert batch flags cleared and returned task settled if adoption or flush throws. Nonempty pending and durable-import receipt preservation need their existing integration fixtures, not inference from the diagnostic's pending=null baseline.

No GUI or candidate execution has been performed for this proposal.
