> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Evidence closeout — 2026-09-17

User requested documentation-only closure. No product/candidate/test promotion,
new test, new diagnosis, timer, recording or publication is authorized by this
closeout. Existing completed runs are retained, including failures. Resume only
after the user's next instruction. G1/G2 remain approved; final0.4 is not accepted.

## Completed evidence

| Scope | Actual result and boundary | Evidence |
|---|---|---|
| Keyboard response adoption and Stop availability | **104 native checks;134 inputs unchanged. Already in experiment source.** Final key legends, immediate Shift/key-up feedback, Stop pending disabled caps, real banks/Local, rejected input and full state/context. Injected input, not physical-key latency. | `native-baseline/postapproval-g2-20260917-060123/`; `docs/reviews/keyboard-batch-independent-review-20260917.md` |
| Core / reference / crash recovery | Three commands passed, inputs unchanged; prior report bytes restored. Headless scope. | `evidence/core-contracts-/20260917-054300/result.json` |
| Headless baseline | **55 groups;497 reported cases;46 groups exit0,9 red; overall failed.** One optional smoke skip. Input bindings unchanged.53-minute concurrent run is not performance evidence. | `evidence/final-headless-resume-20260917/BASELINE_RESULT.md`, `baseline-verification.json`, `bound-run/` |
| Fresh selected manual paths |35 moving orbits/59 legal stages passed in the baseline component. Finite inverse-built paths, not every reachable/protected state or human full solve. | `evidence/final-headless-resume-20260917/fresh-fixed-reports/manual-residual-paths.json`; `bound-run/24.log` |
| Independent Solver wave-d | Actual two-star E1 execution, Next promotion, undo/redo, whole-orbit rejection/cancel, single-position protection, second execution and exact completion UI.111 inputs unchanged; normal close. Raw `[33]` was a disclosed workaround, not discoverability success. | `evidence/native-solver-review-20260917/wave-d/index.json` and immutable `review.md` |
| Candidate backend and fixtures | **42/44 passed;overall exit1;87 inputs unchanged during run.** Cycles5/5, missing frame1/1, log reply4/4, lifecycle20/20; two test-copy assertions remain. Current later-edited files are not all the tested versions. | `evidence/final-headless-resume-20260917/fixture-candidates/STATUS.md` and `product-run-20260917-065159/result.json` |

Retained Display72, keymap-file16, compatibility, naming and mathematical evidence
remain in `evidence/validation-index.json` with their original scope/bindings.
Unchanged Instant turn, framework hiding and low-detail capabilities do not need
an extra test solely because this closeout was requested.

## Three candidate fixes — none promoted

| Candidate | What is corrected | Evidence now | Remaining action when resumed |
|---|---|---|---|
| C1 post-primary cancellation reply | Optional display cancellation cannot turn a completed commit into a false unchanged-state failure; authoritative read-only retry, explicit cancellation warning, no operation replay. | Tested adapter SHA `8c545199737361e080997cd32f9b6b1b4ca614ce7c6cfa24b3f18648d016e748`; cycle5/5 checks exact labels/head, one commit and Stop ownership; native cancellation case passed; log4/4 preserved. | Integrate reviewed diff into original `adapter.py`, restore its original HERE boundary, then bind affected checks to integrated files. |
| C2 missing transported frames | Missing verified frames produce unavailable/unknown diagnosis before cached invariant use, instead of AttributeError. | Same adapter copy; dedicated missing-frame1/1 passed. Missing-frame case is dependency fault injection, not normal startup failure. | Integrate with C1; do not certify missing frames as safe. |
| C3 named orbit protection picker | Mathematical names replace raw JSON entry; canonical IDs preserved; controls appear above elastic requirements list. | Copied Shell/Solve sources; **20 native checks,141 inputs unchanged**, `native-baseline/postapproval-g2-20260917-064813/`. All35 distinct orbits, Space/Apply/Cancel/empty set, exact full labels. Stop/stale tests inject client state. Screens inspected. | Integrate two reviewed C# copies; retain native test coverage and independently revisit S3. No final native acceptance implied. |

Candidate directories: `product-candidate/`, `protection-picker-candidate/` under
`evidence/final-headless-resume-20260917/`. Initial provenance is historical;
`closeout/candidate-status.json` records the current and actually tested hashes.
The first picker run064425 failed because its harness loaded E1 twice. Its failure
is retained;064813 corrected only that harness setup and added foreground guarding.
The interrupted root contract run is **cancelled-partial**, never a pass.

## Obsolete test contracts — do not change product to satisfy them

| Original group(s) | Confirmed obsolete assumption / test setup | Disposition |
|---|---|---|
| cycle_projection / residual_cycles | Direct process-local completed-operation stamps replace actual commit/generation ownership. | Copy uses real commit. Missing-frame error is separately C2, not dismissed as fixture drift. |
| cycle_transport / window_layout_transport / native_responses | Handler setup omits shared Workflow; observed methods omit new keyword forwarding/cleanup. | Candidate fixtures corrected. Post-commit cancellation is separately C1. |
| experiment_contracts | Next remains locked after successful promotion; block has no exact/position mode; per-orbit context omits selected macro, generation and Local center. | Candidate assertions follow accepted semantics. Optional selected_macro default assertion remains unfixed/unrun. |
| native_evidence_metadata | Timeout fixture lacks the now-required workflow manifest and fails before timeout. | Candidate6/6 passed; original test untouched. |
| native_responses | Macro transformation returns None; saving a macro changes no workspace field. | Actual Save returns created record/provenance and changes personal_macros. Later precise store assertion edit is **untested**. |
| operation_lifecycle | Mocks lack transactional kwargs; templates need no confirmation; old Next/key sets; reuse writes no new operation generation. | Candidate20/20 passed, retaining both pre-write rejection and post-write durable receipt boundaries. |
| native_transport | unittest discovery is a valid entry point. | Ran **zero cases**; supported `--work-dir` CLI remains unrun. Invocation issue, not product failure. |

No old test is deleted/disabled and no product behavior is reverted. Proposal
patches are not final integration patches. `fixture-corrections-seven-v2.patch`
still includes the pre-correction native assertion and excludes root's adapter and
contract changes. The44-case aggregate remains failed; never relabel it44/44.

## Fixed starting point for tomorrow

1. **B1 — close candidate/test bookkeeping first.** Read fixture `STATUS.md`.
   Fix only the optional-field test-copy assertion; review the already-edited
   personal_macros assertion. Two single-case runs are prepared but **unrun**.
   No need to rerun the passed44-case collection just for these assertions.
2. **B2 — integrate C1/C2/C3 with explicit original-hash checks.** Preserve all
   unrelated dirty work and copy-only path relocation. Update source bindings and
   rerun affected checks. None was integrated during this closeout.
3. **B3 — fill recorded verification gaps.** Fresh transported-frame binding and
   native transport supported CLI are unrun. Reuse unchanged component proofs;
   failed baseline and later edited fixtures cannot be called final green.
4. **B4 — final native and independent solver acceptance.**32 mixed cycles/16
   worksheet reuses, current G1/G2, focus/window/scaling and end-to-end latency;
   independent orientation/buffer endgame, cross-orbit return, unfinished
   save/restore and Net/Strict comparison. Wave-d did not perform these tasks.
   Fix only defects actually reproduced; no design expansion.
5. **B5 — delivery after the above.** Rebuild isolated package, check its actual
   native startup, then compact continuous footage with captions and labelled
   cuts/speedups. Change-only release notes; research report after formal release.
   Retain existing API workflow, rollback/compatibility and publication boundary.

No new percentage, token budget or completion-time promise is inferred from this
inventory. These five bounded items replace repeated broad rediscovery. No tests
or GUI workers remain active; there is no scheduled continuation.
