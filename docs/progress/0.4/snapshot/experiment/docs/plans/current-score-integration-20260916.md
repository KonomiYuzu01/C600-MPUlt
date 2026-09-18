> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Current-operation recommendation integration

Goal: extend the existing review so an explicitly chosen complete operation has
endgame-aware, explainable CurrentScore evidence. This is not a solver or a new library.

Authority: desktop 07_NAMING_AND_RECOMMENDATION, Two separate ranking layers;
08_INTEGRATION_AND_ENDGAME, WorkIntent and scoring. All formulas and manual-control
boundaries are already approved. Ordinary isolated implementation is authorized.

The limiting factor is the missing connection between exact residual/goal evidence
and the operation's current usefulness. Static UseScore alone cannot describe an
orientation transfer or buffer cleanup in the current state.

## Boundaries and ownership

- identity_solving: one bounded calculation in macro_use.py or a focused helper,
  with tests/test_current_recommendation.py. No adapter or UI ownership.
- root: adapter.review_draft/snapshot, integration tests, existing Solve/Findings
  presentation and canonical evidence inspection, verification receipts and handoff.
- geometry_frames: independent mathematical/interface and implementation review.
- workspace_keyboard: independent UI-route and context-preservation review.
- Only root runs native windows. No model, persistence format, frame, renderer,
  public release, automatic macro choice or hidden setup work changes.

## Sequence and acceptance

1. Review helper interface against exact F/Gp/Gtheta/B/D/M, full support/cost and
   protection precedence. Reuse the already simulated before/after states and
   ReviewContext. Unknown never means zero orientation. Preserve negative scores.
2. Record failing helper/integration checks, implement the bounded helper and attach
   its detached result to the existing review. Complete strict/net protection remains
   the only execution gate. Score is not an authorization token.
3. Withdraw old recommendation eligibility on context change; camera, filter and
   inspection must preserve mathematical context. No result is stored as puzzle state.
4. Present category and up to three computed reasons in existing Solve/Findings;
   keyboard/pointer evidence inspection uses canonical objects and preserves
   Current/locked Next. No new endgame workbench or large dashboard.
5. Independently challenge displaced orientation, hidden collateral damage,
   orientation transfer to buffer, protected high-score operations, wrong reference,
   target changes, cancellation and detached result mutation. Run affected regressions
   after source freeze and retain source/model-bound logs. Native evidence is separate.

## Completion limits

This stage evaluates a user-supplied complete operation. It does not certify all35
endgames, finish library-wide dynamic browsing, or certify a human full solve.
The prior desktop-capture white-surface/focus issue remains independently recorded.
