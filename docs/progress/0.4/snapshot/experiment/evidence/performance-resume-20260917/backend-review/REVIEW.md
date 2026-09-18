> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Bank / Local paired-reply backend review

Read-only source and existing evidence review, 2026-09-17. No Model/Session was
constructed, no current benchmark or GUI was run, and no product file was
changed. `observations.json` binds the inspected sources and input artifacts.
The older Python profile is historical: several backend sources have since
changed. Its costs are useful priorities, not current measurements.

## What the existing measurements establish

Eight accepted bank and eight settings commands per trace, including warmup and
restoration; medians in milliseconds. Inclusive spans overlap and must not be
added. These are not physical input/GPU timings or p95 evidence.

| Trace/action | Send | Api.Post | Draw | Hub inside Draw | Availability refresh |
|---|---:|---:|---:|---:|---:|
| 051917 bank | 1928.59 | 276.62 | 1035.71 | 604.08 | 405.56 |
| 051917 settings | 1266.64 | 231.50 | 606.39 | 465.83 | 287.49 |
| 052827 bank | 1816.60 | 278.60 | 859.50 | 434.73 | 429.98 |
| 052827 settings | 1320.57 | 284.97 | 536.40 | 356.26 | 298.32 |

Both structural timing validations say complete, with no dropped events. The
native client `native/NativeHost.cs:44-48` includes the initial POST, repeated
job GETs, 2/4/8/16/32/40 ms backoff, response reads and JavaScriptSerializer
parsing inside Api.Post. Therefore that span is not pure Python time.

The earlier isolated `packaging/evidence/paired-reply-20260917-0131/report.json`
recorded bank median 107.39 ms: command 8.93, phase 5.81, cycles 8.11, snapshot
45.29, Local 5.32, native snapshot 20.49, JSON 16.90. It emitted 569399 bytes:
work 360808, phase 116058, cycles 50176, Local 26818, native 15354. It did not
include HTTP or native adoption. It is a different, explicitly reviewed E1
fixture under historical sources. No subtraction from the latest Api.Post
should be labelled measured network/parse overhead.

## Actual call chain

1. Every G2 Send invokes `ExperimentPhase.cs:32-39` PreparePhaseRequest. Bank and
   Local-center settings still attach `inspect_boundary`, selected fixed macro,
   cycle mode/scope/selection/page and `native_since`. Existing stale withdrawal
   happens immediately and must remain.
2. `adapter.py:1895-2008` authenticates and validates metadata, rejects another
   busy operation, submits to the existing single worker and holds the shared
   Session lock. `command(..., response_snapshot=False)` does not itself return
   a duplicate snapshot on this native route.
3. Bank (`1290`) currently materializes `banks()` just to validate/find its ID.
   There are **175 standard records (35 × A/B/I/M/E), plus shared banks**, not
   140. An already selected valid bank returns early, preserving previous_bank.
   A different bank changes only bank/previous_bank. Local-center settings
   (`1658`) validate the canonical cell and update view. Both ordinary edits
   then call `save()` (`141`, `1709`), retaining persistence and rollback.
4. Save calls Session.save_prefs (`session.py:109`): validates preferences and
   full filter rules, writes SQLite, and constructs status even though save()
   ignores that returned status. `command` also canonicalizes intent/protection
   before/after. Bank and local_center are absent from intent/ReviewContext;
   they do not change mathematical authority or epoch by themselves.
5. After that mutation: `inspect_phase` (`draft_inspection.py:13`), optional
   `inspect_cycle_display` (`cycle_projection.py:46`), then `native_reply`
   (`adapter.py:1799`). Optional inspection failures are isolated from a
   successful primary mutation; this boundary must remain.
6. `native_reply` unconditionally constructs `snapshot(prediction=True)`,
   selected-frame `cell_state`, and `NativeSnapshotCache.read(..., since, 2)`.
   A job GET serializes the full result via `server.py:146,169-174`. The arrays
   may be an empty delta while the entire work/phase/cycle metadata is rebuilt
   and serialized.

## Confirmed repetition and realistic priorities

* `snapshot:782` rebuilds every library record (72 defaults plus personal
  entries), complete bank catalogue, names and current contextual facts.
  `library_record:528` repeatedly invokes `facets` for each kind and each scoped
  support orbit, even when the facts are unchanged. The old cProfile observed
  72 library_record calls / 28 ms, 730 facets / 15 ms, two banks calls / 13 ms.
  These are inclusive values from one instrumented sample, not additive or
  current. Missing facts remain unchecked; no suggestion to run Check library
  passively. Overrides, captures, selected frames and applicability stay dynamic.
* `PieceFilterProjection` instances are independent in phase, cycles, work and
  Local. Each evaluates the actual full model again. Additional actual styles
  are evaluated in preference validation, native render and interactive masks;
  prediction may add another state. Old profile: eight Filters.styles calls /
  17 ms. The class already caches per-state results within its own lifetime;
  it simply is not shared across this single locked reply.
* `snapshot` forecast recreates a PuzzleState from `m.net(concrete())` whenever
  review is Ready/Staged. Phase/Operation/After can also construct identical
  prepared/predicted states. No evidence that full protection is being replayed
  for ordinary bank/Local changes: `review_draft` and strict-prefix replay are
  not on these action branches. The forecast is display work, not new permit.
* Current/After graph uses cached_residuals if its exact context matches. If
  absent, `_inspect_residual_cycles:151` calls residual_bundle without publishing
  that result to the residual cache. This can repeat real analysis on each
  display-only request. Its significance depends on the actual cache/mode;
  the historical warm Current profile was only about 8 ms. Measure hit/miss
  before proposing any publication or reuse change.
* `NativeSnapshotCache.read` (`server.py:23`) reuses immutable native mapping but
  recomputes all colors/render styles/interactive arrays and hashes before
  producing a delta. It also serializes Session.status to detach it. A zero
  wire delta therefore does not imply zero backend work. Cell counts already
  have an exact existing state/orbit/interaction cache (`session.py:283`).
* SessionWorkflow.report traverses current ancestry twice: Workflow.stats and
  `_source`. This can grow with history and is a new source-level suspect; no
  current long-journal measurement exists. It is not a reason to remove source
  provenance or timer persistence.
* ReviewContext is recomputed in guards, residual/candidate lookup, phase and
  cycle bindings. Old profile: 12 calls / 5 ms. Canonical JSON/clone operations
  dominated more than that (340 dumps / 38 ms across the whole profile). Keep
  version, pending, roles, goals and all inactive-context locks in the binding.
* Do **not** reopen the earlier progress/frame-cache fixes: PuzzleState.progress
  is already cached per immutable state (`core.py:270`), snapshot supplies its
  completed list to library_record, and cap frame choices/rotations are cached.
  Their repeated method calls do not prove repeated mathematical enumeration.

## Smallest safe changes to test first

1. **Remove bank catalogue construction used only for ID validation.** Validate
   against the same 175 canonical IDs plus actual shared IDs; keep the existing
   same-bank no-op and build the full dynamic catalogue once for the response.
   No persistence/schema/new cache. Expected saving is one catalogue build,
   not hundreds of milliseconds. Include shared/unknown/previous-bank tests.
2. If filter calls remain measurable, pass one **existing** PieceFilterProjection
   through phase/cycles/work/Local within this one post-command locked reply.
   Optional internal arguments can preserve standalone public behavior. This
   is a request-local lifetime, not a cross-revision cache. It must be created
   after the command; each distinct actual/prepared/predicted state still gets
   its own evaluation. Do not reuse unpinned filter results for native rendered
   annotations or pin_safety; those intentionally have different semantics.
3. Profile library_record before changing it. A pure single-pass extraction of
   the same kind/scoped flags from trusted facts can replace repeated query
   validation; preserve facets as the public query validator. Do not cache
   current protected/completed applicability or infer facts from imported data.

The first change is the smallest; request-local sharing addresses a broader
proven duplication but requires several internal signatures. Neither is a
credible explanation/fix for the entire 1.3–1.8 second Send. Do not replace the
fresh reply with a bare preference ACK, omit full protection, or introduce a
cross-request display cache without separately reviewing all bindings.

## Minimal discriminating profile (not run here)

Reuse the existing `packaging/profile_reply.py` isolated E1, actual verified
profile and explicit reviewed recipe. Limit to bank 33-A ↔ 33-I and
`settings {view:{local_center:55}}` ↔ 17: one warmup pair then three measured
pairs. Record unchanged head/hash/Current/Next/draft/protection/pending token,
plus ReviewContext equality, while permitting only bank/previous_bank or view
and the existing timer bookkeeping to change. Keep Current mode plus the same
macro/boundary/selected anchor as the target native trace. Repeat one pair with
an actual cache miss only if the live trace used one; do not mix it into warm
statistics. No 35-orbit enumeration or broad fixture matrix is needed.

Add narrow spans/counters to the copied diagnostic pipeline: command vs save /
_validated_prefs / status; inspect_phase; inspect_cycle_display with residual
cache status; snapshot with library_record total / banks / prediction; Local;
NativeSnapshotCache.read; final canonical(reply) and bytes. Count actual
Filters.styles evaluations and states, effect cache hit/miss, PuzzleState
constructions, ReviewContext calls, ancestry rows and SQLite writes. cProfile
one warm bank and one warm Local sample only; others use perf_counter without
profiling. This separates math, filtering, dictionary/JSON work and persistence.

If the Python paired pipeline stays near the historical 100–150 ms while
Api.Post remains near 280 ms, instrument LocalApi.Request and Parse plus job
poll count/wait separately on the next root-owned native run. Do not assign the
unmeasured difference to networking, GIL, polling or .NET parsing by guess.

## Rejection cases

Reject any optimization if complete paired JSON semantics change (allow only
explicit elapsed timer/timing fields), 259800 labels/head/revision change, or
Current/Next/roles/reference/draft/protection/preview/epoch are retargeted.
Bank selection must retain exact frame/slots/captures/name overrides and return
context, including shared banks and same-ID no-op. Local must show its requested
canonical cell/frame and all 433 labels/cap memberships without changing work.

Use existing focused tests for staged preview, same-hash changed goal/roles,
filter preview and cancellation, hidden piece filters, inactive-context locks,
selected macro revision, strict-prefix net-identity conflict, undo/restore, and
Current/Operation/After unavailable/executed cases. Display errors/cancellation
must not revoke or misreport a durable action; stale/unavailable must never
reuse prior evidence. Full protection remains exclusively the original fresh
review/preview/commit boundary. Save failure and same-bank behavior must keep
their current rollback/acknowledgement contracts.
