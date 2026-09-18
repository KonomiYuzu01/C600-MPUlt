> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Fresh bounded backend diagnosis

`profile-current-20260917-054939/report.json` is passed with all source/model/
profile hashes unchanged. The process exited 0. `profile_current.py` constructs
one isolated real Model/Session/Workbench and uses the persisted verified
mapping from `postapproval-g2-20260917-050049/session/native_profile.json`.
It did not read a personal Session, search for targets, open a native window,
run HTTP or modify product code.

The fixture was corrected **before running** to match the current native
latency test, rather than reuse the historical reviewed-E1 profile: explicit
legal `[1,3]` live, no draft/preview/protection, filter `active`, Current graph,
no selected macro. Actual bank requests are Macro/Keyboard and Local requests
C1/C7. Each action has two warmups and five recorded samples, followed by one
cProfile sample. No p95 or quiet-machine comparison is claimed.

## Fresh measurements

Medians, milliseconds. Top-level stage medians need not sum to the median total;
nested counters overlap and must not be added. Timing wrappers add overhead.

| Stage | Bank | Local |
|---|---:|---:|
| Primary command / preference save | 6.33 | 4.17 |
| Phase inspection | 4.96 | 3.48 |
| Current cycle inspection | 22.70 | 18.26 |
| Complete work snapshot | 25.65 | 22.32 |
| Local 433-region payload | 4.32 | 5.63 |
| Native snapshot / arrays / state | 14.33 | 14.81 |
| Full reply canonical JSON | 10.02 | 7.75 |
| **Total paired pipeline** | **88.44** | **77.30** |

Five-sample ranges: bank 72.50–128.63 ms, Local 69.51–89.81 ms. Reply size is
449168 bytes: work 259015, cycle 110775, phase 36013, Local 27925 and native
15255, plus envelope. Predicted work is absent, as expected for the empty draft.
The native arrays are deltas; the complete metadata is still reconstructed.

Useful measured nested work:

| Function / count per reply | Bank | Local |
|---|---:|---:|
| residual_bundle, 1 | 14.99 | 11.41 |
| SessionWorkflow.report, 1 | 12.88 | 10.00 |
| Filters.styles, 7 | 11.69 | 10.62 |
| library_record, 72 | 5.36 | 4.88 |
| banks, 2 / 1 | 5.00 | 2.15 |
| save_prefs, 1 | 3.81 | 3.97 |
| Session.status, 3 | 2.94 | 2.68 |
| review_context, 12 | 2.14 | 1.77 |
| event reads, 8 | 0.48 | 0.40 |

Both residual lookups remain NotAnalysed on every request. Current inspection
constructs a detached residual bundle but does not publish it to the existing
cache. This is a real repeated analysis, not an inferred cold-cache cost.
No effect/PuzzleState construction was called in the measured empty-draft
pipeline, so repeated macro replay or forecast construction is not the cause
in this exact native scene. There is no strict-prefix review on these actions.

cProfile isolates another small repeated cost: this fresh profile uses the
legacy attempt fallback. SessionWorkflow.report invokes `_attempt` three times
(attempt id, detached attempt, timer adjustment), each hashing the same Home
label array. The profiled sample attributes about 12 ms to those three
state_hash calls. Journal reads are only about 0.4–0.5 ms, so the earlier
ancestry-traversal suspicion is not material for this short fixture. A long
journal was not measured. cProfile also includes invariant checking and payload
size accounting **outside** the timed stages; its complete total must not be
substituted for the five ordinary samples.

## Correctness checked

After every warmup, measured sample and profiled sample: all 259800 labels,
head/revision, exact ReviewContext, guard, Current/Next/roles/reference/draft,
protection and pending state remained unchanged. The requested bank or Local
center was adopted. Every Local payload contained the correct 433 actual labels;
work/phase/cycle guards and contexts matched the same actual state. For repeated
identical requested targets, complete work/Local/phase/cycle dictionaries were
deeply equal. No output was weakened to a cheaper subset.

## Decision

**No significant, low-risk backend correction identified that would address the
observed 1.3–1.8 second Send. Do not implement the bank-ID micro-optimization as
the next latency fix.** One catalogue build saves only roughly a few ms here.
The aggregate seven filter evaluations cost only 11 ms; request-local sharing
can save a portion, but cannot explain the missing hundreds of milliseconds.

The largest reusable item is the existing context-bound residual bundle,
roughly 11–15 ms. Publishing it after successful final cancellation/context
checks could remove that repeated work using the existing single cache; it also
intentionally changes snapshot residual status from NotAnalysed to Analysed.
That deserves a small semantic/atomicity review, not a silent timing patch.
Keep complete protection and stale-context rejection unchanged. Reading the
attempt once within SessionWorkflow.report would avoid two identical Home
hashes in the legacy fallback, but again saves a bounded single-digit-ms amount
and is irrelevant once explicit attempt metadata exists. Neither was changed.

The discriminating next step is **the existing native Api.Post span**, which
was about 279/285 ms in 052827. On a root-owned copied diagnostic, separately
time request/response I/O, each JSON Parse, poll count and sleep, with server
worker completion/serialization and reply size. The fresh backend run is not
simultaneous with that trace; the numeric difference is not proven network,
polling or .NET cost. Do not guess its attribution. Meanwhile the measured
Draw/availability spans remain much larger than this backend pipeline.

Reproduce (fresh output only):

```powershell
python -B evidence/performance-resume-20260917/backend-review/profile_current.py --profile native-baseline/postapproval-g2-20260917-050049/session/native_profile.json --output <new isolated output>
```

Any later comparison must preserve this exact request context and payload,
including Current mode/cache state, complete native metadata and all guards.
The safety rejection cases in `REVIEW.md` remain applicable if a targeted
optimization is subsequently authorized.
