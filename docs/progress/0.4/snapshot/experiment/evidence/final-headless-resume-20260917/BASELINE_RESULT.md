> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Headless baseline — 2026-09-17

55 suites; 497 reported test cases; 46 suites exit 0, 9 suites nonzero. Overall exit 1. Duration 3172.275 s during concurrent work; not a performance benchmark. One optional engine-smoke case was skipped.

Python/test/model/certificate input hashes stayed unchanged. Copied receipt/log hashes were verified. Historical fixed-report bytes were restored; the fresh 35-orbit / 59-stage manual-path report is retained in `fresh-fixed-reports/manual-residual-paths.json`. Supplemental C# text/profile inputs were closed unchanged after all consumers finished and before subsequent C# promotion.

## Red suites

| Suite | Cases | Log |
|---|---:|---|
| test_cycle_projection.py | 14 | 5.log (`bound-run/5.log`; local-only reference) |
| test_cycle_transport.py | 5 | 6.log (`bound-run/6.log`; local-only reference) |
| test_experiment_contracts.py | 36 | 11.log (`bound-run/11.log`; local-only reference) |
| test_native_evidence_metadata.py | 6 | 28.log (`bound-run/28.log`; local-only reference) |
| test_native_responses.py | 9 | 29.log (`bound-run/29.log`; local-only reference) |
| test_native_transport.py | 0 | 30.log (`bound-run/30.log`; local-only reference) |
| test_operation_lifecycle.py | 20 | 31.log (`bound-run/31.log`; local-only reference) |
| test_residual_cycles.py | 12 | 37.log (`bound-run/37.log`; local-only reference) |
| test_window_layout_transport.py | 9 | 47.log (`bound-run/47.log`; local-only reference) |

Original failures remain evidence. Workflow setup, old execution/Next expectations and the missing-frame AttributeError are being addressed separately; this baseline does not infer fixes from later candidate results. `test_native_transport.py` used the wrong discovery entry point and ran zero cases; its supported CLI is still unrun.

## Scope limits

`selection.json` records the exact 55-file selection and exclusions. `reused-evidence.json` and `reuse-index-check.json` distinguish unchanged component proofs from stale aggregate integration bindings. The direct endgame reference matrix can support only its declared mathematical component scope, not the stale broad batch. The initial transported-frame reuse note was superseded: fresh binding is required and remains on hold. No browser/native/renderer run occurred.

Remaining followups are not launched: fresh `test_transported_frames.py`, and `test_native_transport.py --work-dir ...` with its existing cached synthetic fixture. No final headless-green or final release claim.

Evidence: `baseline-verification.json`, `run-summary.json`, `bound-run/result.json`, `supplemental-consumers-closed.json`.
