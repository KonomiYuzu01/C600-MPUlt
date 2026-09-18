> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Hub update diagnosis — 2026-09-17

Scope: independent read-only review; no product changes, native launch or new performance claim.

## Measured boundary

`native-baseline/postapproval-g2-20260917-050237` has 52 assertions, exit 0, unchanged immutable inputs, and a complete 20-command trace with no drops/overlap. For **all eight bank and eight settings commands, including warmups/restoration**, the raw medians are:

| Inclusive span | Bank | Settings |
|---|---:|---:|
| Hub.UpdateContext | 586.34 ms | 599.55 ms |
| Draw | 985.42 ms | 790.57 ms |
| DrawMacros | 4.27 ms | 4.57 ms |
| DrawWorkspace, two calls/command | 3.25 ms/call | 3.46 ms/call |

There is exactly one Hub.UpdateContext and one Draw per command. This does not support a duplicate outer Draw explanation. Concurrent frozen-checker work means these are diagnostic observations, not a quiet-machine latency result. Nested timings must not be added together.

Reviewed Hub sources match that capture exactly: `ExperimentHub.cs` 46567e49…88bc; `ExperimentHubCycles.cs` d738f48f…84c8. Full bindings and raw-artifact hashes are in `observations.json`.

## Concrete source facts; finer costs remain unmeasured

1. **Fresh native controls on every update.** Hub.UpdateContext (245–251) always calls Rebuild. Rebuild (308–309) disposes all actual/forecast/requirement controls, then AddObject (410–437) recreates them. PopulateDetailedCycles (Cycles116–132) repopulates both selectors and BuildCycleRings (171–180) destroys/recreates every page ring. Only Hub and canvas are layout-suspended; tracking, cycleRings and the selectors are not. This is real lifecycle work on bank/Local navigation even when the depicted identities and edge relation are unchanged. Actual HWND/layout cost is not yet isolated.
2. **Repeated layout/readout work inside one rebuild.** Rebuild ends with LayoutScene then RefreshScene. Those both refresh correspondence, traces and protection. LayoutScene calls LayoutHeader directly; each RefreshTraces calls it again: at least three LayoutHeader calls, two trace/protection/correspondence passes. With an out-of-work tracked socket, LayoutHeader (511–514) first sets tracking.Height to the base height and then increases it, so an otherwise unchanged header has two intermediate size transitions. GeometryWidth repeatedly measures the same captions; every LayoutScene disposes/recreates each sticker strip image. AddObject also creates an identity image that LayoutScene replaces for ordinary sockets. Their individual costs are still hypotheses.
3. **Presentation hints rebuild unrelated input data.** Hub.Hint (637) delegates Shell.KeyHint (324), which calls InputState (288–303). Each call serializes Bank.frames and reconstructs Grip, Twist and command maps, despite needing only the effective command route. Hub requests hints from its repeated header/protection passes and all context-menu actions. Measuring only Draw cannot attribute that work to keyboard rendering.

The selector/checkbox changes are guarded by syncingCycle, syncingCycleOrbit, syncingEffectScope and syncingComparison. No source-backed claim of duplicate command events or accumulating subscriptions was found: old buttons are disposed, and new handlers attach once. The reported 600 ms is not yet proof of any one of these causes.

## Smallest next experiment

`decorate_hub.py` adds 24 inclusive method/block span names to **new copies**, reusing the existing bounded NativeDrawTiming buffer. It includes Shell.KeyHint/InputState/FunctionsShortcutNotice; Hub.Hint; Rebuild; DisposeObjects; AddObject; ResumeLayout; all relevant layout/readout methods; cycle-ring clear/build; image generation and caption width. Event counts supply invocation counts. No extra state request, render, forced paint or mathematical calculation is introduced.

From the experiment directory, after the next build receipt is frozen:

```text
python -B tests/profile_native_draw.py --manifest <fresh-build.json> --output evidence/performance-resume-20260917/<new-base>
python -B evidence/performance-resume-20260917/hub-review/decorate_hub.py evidence/performance-resume-20260917/<new-base> evidence/performance-resume-20260917/<new-hub>
python -B tests/run_native_draw_profile.py --copies evidence/performance-resume-20260917/<new-hub> --compile-only
```

Root alone then runs the same command without `--compile-only` in its exclusive GUI slot. The regular runner still verifies all originals/copies before and after the run. The old 050237 overall capture is stale after unrelated Shell edits; the decorator demonstrably rejects its changed Shell input. Do not launch either `offline-*` transformation fixture or the earlier generated-only demonstration directories.

Interpretation: partition events by actual command and use intervals contained within Hub.UpdateContext. Sum **nonoverlapping calls of the same method**, not parent plus child totals. Shell.InputState is nested in Shell.KeyHint, itself nested in Hub.Hint when called by Hub. Subtract child intervals only when explicitly deriving exclusive time. For ring rebuilds, AddObject and image measurements, report both count and total. Do not infer accepted-sample mapping solely from the command order; use the actual diagnostic receipt.

Reject interpretation if source/copy binding changes, any input is rejected/recovered, the existing trace validator is incomplete, events drop, or nested samples escape their parent command. Reject a proposed fix if full 259800 labels, Current/Next identity, canonical object selection, active ring focus, scroll, correct fresh filter/frame/preview binding, or stale-result withdrawal differs. Keep native state/adoption assertions and do not force a render to manufacture a frame receipt.

## Candidate changes, conditional on the fine measurement

- If repeated InputState construction accounts for the large cost, factor the **effective command map** into one shared helper used by InputState and presentation hints. Preserve convenience-binding collision checks, global/bank override precedence, Functions and legacy behavior. A per-call helper needs no cache invalidation or new authority. This is narrower than changing the redraw lifecycle.
- If native control churn dominates, first test batched selector/ring/tracking updates while retaining exactly the same rebuild and stale-data rules. Reusing controls by canonical socket/identity/boundary may follow only if creation cost remains dominant; it requires explicit focus and duplicate-role tests.
- If repeated layouts dominate, compute final tracking height once and coalesce the presentational trace/protection/layout passes. Do not skip UpdateContext's source_hash/source_guard check or retain a withdrawn forecast.

Offline validation: `transform-check.json` proves that removing only the inserted timing scopes reconstructs all three copied source texts exactly (24 scopes); the input source hashes were unchanged. This is transformation evidence only. Fine timing and native compilation remain root-owned and unrun here.
