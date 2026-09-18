> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Retained display controls — independent review, 2026-09-17

Scope: the restored display entry points, retained-slider bridge, display-only preference route, Functions bindings, and reset/async boundaries. This review did not operate the GUI, execute native tests, or re-prove the renderer. G1/G2 approvals are unchanged.

## Findings and disposition

1. **Rejected slider input could falsely show a new value.** In the initial implementation, a copied slider changed before `SetDisplayValue` checked readiness; its catch only reported an error. A slider could therefore differ from the retained control while a checkbox save occupied the busy boundary. The current `ExperimentDisplay.cs` restores `original.Value` under an event-suppression flag and refreshes the caption. Both toggles and the slider panel remain disabled throughout the real save. Source correction inspected; native rejection/reenabling checks are prepared, not run.
2. **Reset could leave new/revealed geometry using old dimensions.** `NativeHost.cs` explicitly documents that retained `SetStickerSize` does not update `FShr/SShr`. Initial `ExperimentBridge.ResetView` reset coordinates without updating those fields. The correction calls `SyncDisplayDimensions` after restoring original sliders and uses the synchronized actual fields for `SetStickerSize`, avoiding a second discrepancy from slider quantization. Source correction inspected; a real hide/reveal coordinate regression is prepared, not run.

No further blocking source defect was found in this bounded review. This is not visual or native acceptance.

## Verified source boundaries

- `adapter.py:1186` validates a nonempty, bool-only display patch before saving it; it merges the two known fields into existing `prefs.view`. This early path does not invalidate the work/review, create a move, change filters, or rewrite keymaps. The existing Session preference transaction remains authoritative.
- The six control names, bounds, increments and value conversion match `NativeHost.OriginalViewControls`. Only cell/sticker size needs the retained dimension synchronization; field of view and light controls continue through their original handlers.
- `ExperimentBridge.Apply` adopts both persisted booleans from the same native snapshot. Frame policy uses the existing subset; adaptive detail uses the existing lifecycle property. No new projection, geometry, picking policy, or mathematical model is introduced.
- The display modal uses the existing owned-window input and focus lifecycle. Closing is prevented during preference save. A rejected/no-op request retains authoritative bridge values; disposed dialogs are not re-enabled. Functions commands remain editable and do not become default Grip/Twist routes.
- The new help distinguishes instant committed results from an input-latency guarantee. Durable preference tests cover the two booleans; this change does not establish cross-process persistence of the six original native sliders.

## Inspected evidence and remaining acceptance

`evidence/display-controls-20260917-042026/result.json` records four headless suites, all exit 0, unchanged captured inputs. Its display suite took 11.453 seconds. Current adapter, catalog and display-test hashes match that receipt. These tests cover all 259800 labels, staged preview/guard/work preservation, saved preferences/reopen, invalid input, save failure and keymap compatibility. They do not test the native slider or modal.

`tests/DisplayNativeChecks.cs` was added for root's disposable native fixture: real staged legal word `[1]` in Prepare and an explicit single solid `active` filter. It tests all six retained controls, accurate captions, a controlled readiness rejection, real async-save disabling, actual lifecycle/preferences, no-op preservation, Done focus restoration, Reset fields, five actual visible sticker coordinate samples across `nothing → all → active`, full old filter-rule restoration, unchanged input bindings and all 259800 labels. The busy check manipulates only the harness admission flag; it does not replace the renderer or backend.

`evidence/display-native-compile-20260917-042810/result.json`: isolated x86 library compilation passed, 2.281 seconds, captured sources unchanged; only three existing CS0649 warnings. **No native execution occurred.** Root must run the focused helper and inspect the actual display dialog/puzzle. Pixel appearance, actual camera-motion detail reduction, full-detail picking and physical-key behavior remain outside this check's current evidence.

Inspected hashes:

| File | SHA-256 |
|---|---|
| `native/ExperimentDisplay.cs` | `B0E07A402016FB7203E8632B1DD1CB4555373590C981FC32BE2CD47C61B183A1` |
| `native/ExperimentBridge.cs` | `009CCC4A066AAA492892247EB2AF76A26AE348478C2342C17C9FAFE716BE721D` |
| `adapter.py` | `A798B9D5D0AE704790552331FE4EC729DD1A0CA4C41F3EB3D33BC511DF2EA32D` |
| `keymap_catalog.py` | `5D14236D2D03E89244A386E237FE50F2A988E031EA83089B20DAA091BDDD4734` |
| `tests/DisplayNativeChecks.cs` | `155B78EEA3072250A0287A24A714BF4B8F35526B22D5F8A0E8B61B4293015B6D` |
