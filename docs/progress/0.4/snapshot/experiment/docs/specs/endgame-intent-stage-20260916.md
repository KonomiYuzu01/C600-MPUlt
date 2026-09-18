> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Finite work intents and residuals — bounded design

Date: 2026-09-16. Status: design only, not implemented. Authority: desktop `outputs/magic600-v0.4-design/08_INTEGRATION_AND_ENDGAME.md`, with 07's versioned orientation convention. Extend the existing Workbench/Solve; no new Session, execution permission, state store, target search or recipe chooser. ReviewContext wiring belongs to the coordinator's separate slice.

## Existing behavior that must survive

Source anchors in the experiment:

- `adapter.py:610–674` evaluates one complete draft on exact labels. Whole-orbit and captured-position protection determine permission; an unmet work goal does not block a safe intermediate operation.
- `goal='insert'` means **exact destination identity and labels**, using a matching `target_requirement`, otherwise canonical Home labels only when `target == current`. It is not position-only placement. `target-capture` only captures Current actually at the selected destination.
- `goal='block'` evaluates all explicit member label arrangements; `endgame` evaluates every position's full correctness in the orbit; `prepare` currently returns true without claiming a target outcome.
- `command` focus/roles handlers already permit a canonical buffer destination. The nonbuffer rejection belongs to retained `core.Planner.next`, which this work must not call. Removing that rejection would not implement buffer goals.
- `position_locks()` unions all orbit contexts. These captured fixed-position label requirements persist independently of the active block/selection. `switch_orbit()` copies goal, target requirement and block; protection remains global.
- `work_sheets.py:_validate/build_sheet/inspect_sheet` currently accepts four goal strings and captures exact input comparisons. Native `ExperimentWorkspace.cs:55`, `ExperimentShell.cs:338,415`, `ExperimentSolveWindow.cs:25,43–51` already provide the one goal selector, commands and Prepare controls.
- `tests/test_postapproval_contracts.py:128–181` proves whole-orbit finish differs from target insertion and an explicit captured non-Home orientation must not be overwritten by Home.

## Minimal intent representation and compatibility

Keep `w.goal`, `current`, `target`, `roles`, `target_requirement` and existing block records authoritative. Compute a detached normalized intent for ReviewContext; do not add a second persisted `w.intent`. Existing saved strings stay unchanged. Add new accepted goal strings and native labels:

| Stored goal / accepted alias | Normalized intent | Actual predicate |
|---|---|---|
| `prepare` / `Prepare` | Prepare | No outcome claimed; any explicit block/target facts remain inspectable. |
| existing `insert` | PlacePiece, Exact requirement (legacy Insert) | Preserve the existing identity **and** exact label predicate. |
| new `place` / `PlacePiece` | PlacePiece, Position requirement | Specified identity occupies the specified fixed destination. No orientation success implied. |
| new `orient` / `OrientPiece` | OrientPiece | Specified identity at the specified destination with its explicitly declared orientation/reference requirement. Home location alone is insufficient. |
| new `finish-buffer` / `FinishBuffer` | FinishBuffer | Target is one explicitly bound A/B position and meets the full declared identity/label requirement. |
| existing `endgame`, alias `FinishOrbit` | FinishOrbit | All labels of the active orbit are canonical Home labels. |
| existing `block` | BuildBlock compatibility intent | Nonempty block; every member meets its explicit requirement. Do not discard this unique existing capability. |

New clients may send readable aliases; normalize only that submitted command. Do not rewrite old workspace/worksheet data on load. Update the existing validators and display switch statements together. Keep workspace version 1 and worksheet schema 2: this is an additive supported-value/optional-field extension, no bulk migration. Old four-goal records retain their precise prior predicates. Old executable rollback must not be promised to interpret newly introduced goal values.

`goal` still changes only the goal. It must not silently choose Current, target, reference or macro. Native “Finish A/B” is an explicit binding action: present the selected role position and required identity, then submit those exact canonical values through a validated command. Its default Home requirement is visible and explicitly selected, not inferred from the old Current. Existing `focus` plus `goal` routes may be reused; validate a combined request completely before any mutation if implementing it as one action. Changing A/B later does not retarget the stored target; a FinishBuffer target no longer among A/B becomes MissingInput.

## Exact references and requirements

For each piece I and fixed destination P, first validate same moving orbit and complete slot incidence. Current tracks I; target tracks P. Do not resolve via display names, UI ordering or current occupant guesses.

- Position requirement: `state.at[P] == I`.
- Exact requirement: Position requirement plus `state.labels[m.slots(P)] == required_labels`.
- Canonical Home full requirement: I=P and required labels are `m.slots(P)`; selecting Home for an OrientPiece goal must be an **explicit reference choice**, not merely a consequence of I=P.
- Captured arrangement: retain existing `target_requirement={identity,position,labels}` as exact authority. It is an explicit slot-level reference even if a group-element encoding is unavailable. Never replace it with Home because its position equals I.
- Chart reference: use the already verified `TransportedFrames`, never derive a frame from the macro being tested. For explicit desired element a, canonical ordered frames F_I and F_P specify `required_labels[F_P[a[j]]] = F_I[j]`. Validate a in the orbit's concrete group before publishing. Store the resulting canonical labels in `target_requirement`, with optional provenance `{kind:'coherent-frame',model,frame_version,frame_sha256,element}`; keep the labels as the single comparison authority. Missing/version-mismatched chart provenance is not silently reinterpreted. No angle/mod-20 approximation for D5/A5.

The original bare captured requirement remains valid exact evidence. A coordinate encoding may be Unknown while its full explicit label comparison is known. Conversely, a requested chart-relative orientation without a valid reference is Unknown/MissingInput, not an unmet known orientation. Changing macro or camera never changes the desired requirement.

### Position preservation versus exact preservation

Extend existing block member/protected capture records with optional `mode:'position'|'exact'`; absence means exact. Keep existing `{position,labels}` capture shape; for position mode derive its expected identity from the validated captured labels, all of which must belong to the same piece. This adds one policy discriminator, not a general constraint framework.

`block-add` and `block-protect` accept the explicit mode; omitted mode preserves all old behavior. Exact mode compares labels. Position mode compares the fixed position's occupant identity and allows its orientation to change. Whole-orbit protection remains full-label protection. Include mode in deduplication, worksheet comparison, guard and ReviewContext; two owners' different requirements are conjunctive, so exact protection cannot be weakened by another position-only owner. Removing a member still does not unlock it.

`block-reference` transports member requirements using its existing exact finite mapping, preserving their mode; it never moves existing protected positions. Net checks the complete operation end, Strict checks every primitive using the same requirement predicate. Captured-position policy and completion goal are separate: choosing OrientPiece does not automatically unlock orientation or reduce protection.

## Before/after API contract

Keep old request routes and old `review.goal`, `target_met`, `goal_met`, `block_met`, `remaining_pieces/positions` for compatibility. `target_met` remains the old exact predicate even for a new position-only goal; do not weaken downstream exact-insertion transitions.

Add one detached `goal_result` to the existing complete review:

```text
goal_result = {
  kind, requirement_mode,
  status: Met | Unmet | MissingInput | Unknown | NotApplicable,
  before, after,                         # bool or null where comparison is unknown
  predicates: [{kind, identity?, position?, known, before, after, reason?}],
  reason
}
```

Prepare is NotApplicable (old compatibility boolean may remain true; new UI must not call that a completed target). New goals set compatibility `goal_met` true only for Met. Missing/unknown is not a computed false residual. `Ready/Conflict`, preview token and commit remain governed by exact legality/protection, never by this goal status or score. A known legal protected operation can remain Ready with Unmet/Unknown goal; unknown protection cannot become Ready.

Add `residual_before` / `residual_after` to that same review and the actual residual snapshot, **all derived from the same bound states**:

```text
{orbit, buffers:[A,B], P_nonbuffer, P_buffer, O_nonbuffer, O_buffer,
 FrameUnknown, ExactSolved, displaced_orientation, counts, suggested_stage}
```

Each P/O list contains canonical fixed positions, with occupant identity accessible through the same snapshot. For the active orbit's fixed positions Q and selected buffer set B:

- P_nonbuffer = p in Q\B with `!state.position_correct[p]`;
- P_buffer = p in B with `!state.position_correct[p]`;
- O_nonbuffer = p in Q\B with `state.position_correct[p] && !state.correct[p]`;
- O_buffer = p in B with the same Home-identity/full-label mismatch;
- ExactSolved = no incorrect slot anywhere in Q (equivalently all `state.correct[Q]`).

These four sets are disjoint and their union is the exact incorrect-position set. Do not count a displaced piece's reference orientation as an O residual. FrameUnknown is independent diagnostic evidence; it never hides a known Home label mismatch, adds to the P/O total or turns an exactly solved label state into unsolved. Chart decomposition of current labels uses Home→Current transport; the graph's Current→Home correction relation needs inverse position map **and inverse orientation at the corresponding current position**.

Suggested stage is a recomputed presentation fact, not persisted progress: position if P remains; otherwise nonbuffer orientation; otherwise A/B orientation according to actual required residual; otherwise complete. The user may still prepare/orient early and return. Stage changes never choose a macro, change intent, unlock a helper or automatically switch orbit. A reassigned buffer changes this partition and must invalidate the bound comparison, not the actual state.

## Bounded dependencies and acceptance

1. One pure goal/requirement/residual helper receiving explicit Model/PuzzleState/work bindings plus chart evidence. Adapter retains state/lock/epoch and constructs before/after once. ReviewContext owner supplies the same versioned states to graph, names and scoring. No duplicate projection or separate residual cache keyed only by orbit.
2. Extend existing goal validation, target reference capture, block requirement mode, complete review and worksheet validators; retain canonical identity and exact old defaults. Include optional metadata in `intent()`/guard via the existing records. Context restoration re-resolves actual positions and invalidates authority; never restores a stale residual result.
3. Native adds these choices to the existing Solve goal selector and registry, familiar short labels, one compact residual summary linked to graph positions. Show selected requirement and expected change beside the existing operation; full reference detail remains on demand. Register editable single-key Solve routes and OSK labels in existing banks. No second endgame window or parallel command list.
4. Keep automatic Current/protection commit transitions in their separately reviewed atomic slice. This intent addition does **not** silently expand exact Insert advancement to position-only placement, OrientPiece, arbitrary Prepare or a previously satisfied goal.

Use the already audited saved recipes in `docs/reviews/endgame-evidence-audit-20260916.md`:

- O22 C2: nodes `0+,1136+,0+`; O17 C5: `0+,315+,0+` (A/B orientation transfers).
- O6 D5 last B: `54+,0−,112+,99−,0+,54−,99+,112−`.
- O34 A5 last B: `16+,0−,67+,32−,0+,16−,32+,67−`; 98,372 primitives, plus O33 collateral.

Proposed new tests, not yet run: create a legal fixture by applying a chosen saved recipe's full inverse to solved, then review that **explicit same recipe**. Check position-only Met with exact goal Unmet; Home orientation residual despite correct piece location; explicit captured orientation not replaced by Home; Finish A/B versus FinishOrbit; complete O34/B while O33 collateral remains visible. Protect a target-orbit auxiliary that the full commutator preserves: Net may pass, Strict must detect its real intermediate star motion. Do not synthesize illegal lone C2/C5 states as positive solve examples.

Regression boundaries: malformed/ambiguous reference or mixed identity/position rejects before mutation; MissingInput differs from Unknown/Unmet; filter cannot change residual; actual/after swap does not change Session; old worksheets/default captures remain exact; mode and reference survive orbit return/restart; restore/undo/redo recompute actual residual with existing preference semantics; cancellation publishes no partial goal/permission. Genuine full-orbit invariants remain gated on their separate model/frame/provenance certificate, not on this summary or finite group order.

No new product-level semantic decision is needed for this bounded slice. Preserving legacy exact Insert and leaving automatic advancement unchanged are compatibility choices. If a later request wants position-only PlacePiece to trigger Current advancement, treat that as an explicit transition-policy decision rather than inferring it here. Automatic setup/parameter lookup and migration of old personal data remain outside scope.
