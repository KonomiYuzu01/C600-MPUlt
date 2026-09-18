> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Measured Hub breakdown

Read-only analysis of `postapproval-g2-20260917-051917`: 52 checks, exit 0; timing validator complete for 20 commands, no errors. Root separately verifies the build binding. The table includes all eight complete bank and eight complete settings commands, including warmups/restoration. Detailed per-command values/counts and input artifact hashes: `fine-051917.json`; compact 16-row table: `fine-051917.csv`.

| Span | Calls per command | Median bank / settings, ms |
|---|---:|---:|
| Hub.UpdateContext | 1 | 604.08 / 465.83 |
| PopulateDetailedCycles | 1 | 419.25 / 316.67 |
| └ PopulateCycleOrbit | 1 | 182.73 / 135.25 |
| └ BuildCycleRings | 1 | 105.38 / 86.46 |
| Population remainder, excluding those two direct children | — | 134.91 / 95.58 |
| AddObject, summed | 14 | 57.47 / 51.16 |
| DisposeObjects | 1 | 33.72 / 28.28 |
| LayoutScene | 1 | 77.55 / 56.93 |
| LayoutHeader **within Hub update** | 3 | 5.75 / 4.22 |
| Shell.InputState **within Hub update** | 16 | 6.10 / 4.03 |
| Shell.InputState across the whole command | 99–102 / 95 | 41.30 / 29.10 |
| Shell.KeyHint across the whole command | 86 / 85 | 35.22 / 26.05 |

All values are inclusive per-command sums, then medians. Parent/child rows overlap and must not be added. Population remainder is calculated per command before taking its median. IdentityImage and StickerImage each occur seven times and each aggregate below 1 ms median. Inside Hub update, GeometryWidth occurs 14 times, costing 18.73 / 15.04 ms; the whole command has 42 calls, including calls outside that update.

**KeyHint is not the principal Hub bottleneck.** Do not introduce the proposed command-map refactor to address this measurement. Repeated headers/readouts and image construction are also not the dominant cost.

The largest measured work is cycle/Orbit selector repopulation. The remaining population time contains cycle-picker reconstruction plus the small data preparation; it has not separately proved which WinForms operation is expensive. Likewise BuildCycleRings includes new controls/layout, not just geometry. The meaningful first change is two narrow ComboBox BeginUpdate/EndUpdate regions, preserving the complete item lists and exact selection. `combo-batch-proposal.patch` is staged only, against HubCycles d738f48f…84c8; it keeps syncing guards true through EndUpdate and resets them in finally. No product file has been edited by this review.

Ring disposal is not hidden in the measured ring build: each command calls ClearCycleRings three times, but the nonempty one occurs during withdrawal **before** Hub.UpdateContext. Its aggregate median is 64.45 / 53.23 ms; the call inside BuildCycleRings is ~0.005 ms. Keep immediate stale withdrawal intact. Do not remove it to make the measured Hub region appear faster.

## Comparison limit

050237 → 051917 medians: Hub 586.34 → 604.08 ms for bank; 599.55 → 465.83 ms for settings. Draw 985.42 → 1035.71 ms and 790.57 → 606.39 ms respectively. This is **not** a controlled improvement comparison: the fine instrumentation is new; Shell, Workspace, MacroLibrary and SolveWindow changed; concurrent workload differs. The two Hub product files themselves match between captures. These observations justify where to measure/change next, not an S14 latency or p95 claim.

## Falsifiable acceptance for the minimal batching change

Run the identical explicit bank IDs and Local center values under the same copied fine instrumentation. Require complete trace, no dropped/recovered/rejected commands and stable bound sources. Compare population method times separately from total Send; retain all full-state/adoption checks. A material reduction in the two population regions is required before attributing benefit to batching; no reduction falsifies that proposed cause.

Reject the correction for any changed full 259800 label state/hash, pending preview token, Current/Next identity, draft, protection or selected canonical ID. In Current, Operation and After, assert item counts, ordered canonical cycle anchors, selected anchor/SelectedIndex (including -1), orientation-only/unchanged/empty cases, and all 36 orbit choices. Check Working orbit versus explicit orbit selection and enabled state, user selecting one item dispatches exactly one request, repopulation dispatches none, keyboard/ring focus and scroll remain correct, and new stale/Unavailable results still withdraw old evidence immediately. Changing selected request or using a hidden cheaper data page invalidates the timing comparison. No frame cache, proof cache or skipped mathematical work is part of this change.
