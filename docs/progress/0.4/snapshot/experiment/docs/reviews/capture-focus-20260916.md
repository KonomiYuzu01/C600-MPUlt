> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Native capture boundary review — 2026-09-16

Scope: read-only review of the capture guard in `tests/PostApprovalNativeRegression.cs` (SHA-256 `63C46B6ADACBDDEA91133B5A1392980BE6C6AD1839590126A30612157D4C2D31`). No product edits or native runs were performed for this review. Unrelated foreground screenshots are not reproduced or accepted as application evidence.

## Known evidence

| Receipt under `native-baseline/postapproval-g2-20260916-` | Observed result and limit |
|---|---|
| `213332` | Exit 1 after 389 checks: the Solve desktop-capture focus precondition failed. The recorded foreground was ChatGPT. Explicit activation in the later test establishes a harness precondition, not a production focus fix. |
| `214045` | Exit 1 after 17 checks: native `WM_SYSKEYDOWN` S/H messages interleaved with the goal test; H started operation reuse, so the next submission was rejected as busy. Their provenance is unknown. The recorded lParam ALT-context bits were zero; this is not evidence of an ignored ALT-down flag. |
| `214529` | Exit 0, 35 checks. The GDI Solve image shows all mathematical role addresses. The actual desktop image also shows those addresses, but lower Solve controls are white rectangles and the host has stray control surfaces. Actual desktop rendering is therefore **unresolved**, despite the assertions passing. |
| `214921` | Exit 1, 30 checks. The temporary three-capture experiment was inconclusive: its first `Form.ActiveForm` precheck passed, but the coordinator inspected an unrelated foreground Codex image; the next precheck failed. This cannot discriminate paint timing from `DrawToBitmap` effects. |

The four retained `report.json` SHA-256 values, in table order, are:

```text
FF4F4FA273C999D078B1DD623B5001CDB2F4C2C969343C9F87C83F3402EDDF9C
5D84C706F193B9E52411C2C72DF65A8624611F15B113C5450CAA38FFC7F74979
8071A03E92C93DA4E45D92D04691C3BBDED9C4D226C73D16E85814AD5D75EB21
3E43865ACB3131D362FDD20AE9652161D23B3ED2F8B587DC733CD48164B75383
```

## Guard review

`CaptureDesktop` now requires `GetForegroundWindow() == window.Handle` immediately before starting FFmpeg and after successful process completion. A failed endpoint check throws; a saved PNG or an earlier capture-completed log line is not sufficient to accept that capture. This correctly checks the Windows foreground HWND rather than only the application's active Form.

Two endpoint checks **cannot establish uninterrupted foreground ownership during capture**. Another window could become foreground and the intended window return between them. The post-check wording “retained … ownership” must be interpreted as endpoint evidence only. An actual image still requires inspection and attribution.

The temporary probe is removed. `Image` still calls `DrawToBitmap` first, then synchronously waits for FFmpeg on the UI thread. This guard patch changes test evidence admission only: it does not modify production painting, fonts, layout, focus routing or mathematical state, and does not resolve the white surfaces. The existing UI-thread wait can prevent queued paints from processing during the capture interval; whether that caused the recorded defect is unproven. This is separate from the verified `T⁻¹` typography and corrected role-button height.

## Next minimal discriminating experiment — not run

Keep the same scene, bounds and state/context hash. Compare capture before any `DrawToBitmap` with capture after it; in both cases await the capture process without blocking the UI message loop. Record actual foreground HWND at both boundaries and reject misattributed frames. Do not force repaint or change product rendering in this first comparison. If both images are correct, reproduce the old synchronous capture against the same scene to test paint starvation; if only the post-`DrawToBitmap` image is wrong, investigate that interaction; if both remain wrong, inspect the live paint path. None of these candidate causes is yet established.
