> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Confirmed integration and endgame contract — 2026-09-16

This is the user's latest 0.4 implementation requirement. Extend the existing Session, macro analysis, mathematical names, protection and Solve window. Do not create parallel state authority or another endgame workbench. No automatic setup search, macro composition or execution. Earlier G1/G2 approvals remain valid for their approved samples; unfinished features are not certified by them.

## One operation, three linked capabilities

Retain faithful puzzle/433-sticker Local structure, real adjacency and transparent filtering; a cycle/orientation graph; and mathematical names. Link all by canonical identities, never display strings or screen coordinates. The approved u0/u1 local representative correction changes address disambiguation only, not orbit or chirality. Selection highlights the same identity across windows. Observation center, Current assignment, Next bookmark, Grip and Twist remain distinct operations.

An immutable read-only ReviewContext binds:

- Session epoch and state revision.
- Model, naming and frame versions.
- Active orbit, Current and locked Next.
- Buffer roles, work intent and explicit goals.
- Macro revision and complete recipe hash.
- Protection revision.

Names, graph, scoring reasons and execution status must refer to the same result. Changes to state, recipe, roles, reference or protection invalidate old results and execution permission immediately. Camera-only changes do not invalidate mathematical analysis. Static certificates may be reused only under their proper keys; a score is never a commit token.

The shared graph has three distinct modes:

- **Current:** current-position → that physical piece's Home. This is a residual ownership relation, not a legal one-step move.
- **Operation:** source position → destination under the explicitly chosen complete operation.
- **After:** recompute residuals from its exact simulated resulting state.

Show fixed-position orientation changes even without position cycles. Draw buffer positions as fixed sockets with changing actual occupants. Clicking a scoring reason highlights its actual evidence; expand real collateral for other orbits. Unknown, unchecked and protection conflict remain explicit states, not merely low scores. Selecting a macro inspects it without adding or executing it. After commit refresh actual residuals and candidates under the confirmed Next rules.

## Explicit finite work intents

Introduce/extend existing intent handling to distinguish:

- Prepare: manual preparation or establishment of a usable relation.
- PlacePiece: specified identity at a specified destination.
- OrientPiece: specified identity's orientation relative to an explicit reference.
- FinishBuffer: the specified buffer position's full identity/orientation requirement.
- FinishOrbit: every label in the orbit exactly solved.

An intent carries its explicit piece, destination, orientation reference, buffer bindings and requirements to preserve. A Home position alone cannot satisfy an OrientPiece intent. Buffer destinations need genuine goal/validation support; deleting the ordinary nonbuffer insertion rejection is insufficient.

Execution permission and goal outcome are separate. A legal, protection-preserving manual operation may execute without completing the chosen goal; report that goal as unmet. Do not block all intermediate preparation on goal improvement.

## Exact residuals and reversible stage interpretation

Per orbit compute:

- P_nonbuffer: nonbuffer pieces not at Home.
- P_buffer: identities mismatched at buffer positions.
- O_nonbuffer: nonbuffer pieces at Home whose sticker arrangement is wrong.
- O_buffer: Home identities at buffer positions whose sticker arrangement is wrong.
- FrameUnknown: objects lacking reliable orientation comparison.
- ExactSolved: every orbit label is exactly correct.

Displaced-piece reference orientation remains a separate diagnostic. Do not count it as a Home orientation residual or omit its operation effect.

Suggested stages, never a forced sequence:

1. **Position finish:** inspect remaining cycles, A/B occupants and Home; use chosen macros with manual Prepare/Cleanup; show gained/lost block requirements. Allow early orientation work.
2. **Nonbuffer orientation:** once nonbuffer positions are correct, emphasize stationary orientation differences, exact group elements and references. Verify preservation of the required positions. If a correction transfers orientation to a buffer, show both ends. Position-only preservation differs from exact label preservation.
3. **Buffer A:** A is a valid goal. Inspect its full occupant/orientation and collateral on B/auxiliaries. Use verified complete constructions; a local orientation fragment does not prove full correctness.
4. **Buffer B:** recheck every other position/orientation; inspect exact residual and certified invariants. Evaluate the user's existing macro against it and all preserved objects. Missing applicable macro/certificate is an explicit gap, not an automatic unlock or generated solution.

Stages explain current state and filter tools. Returning is allowed; newly introduced position residuals immediately change the displayed stage. Do not preserve a fictional completed stage.

## Certified invariant diagnostics

Require the matching model, legal-state provenance and relevant certificate. Audit existing project theory, finite-group verification and final-buffer recipes, including full collateral and chronological multiplication. Orbit-local equality does not authorize replacing complete recipes.

If all nonbuffer positions are correct and only A/B are exchanged, the odd permutation conflicts with the project's certified per-orbit even-permutation invariant. Diagnose provenance, mapping or certificate; another orbit cannot compensate for it.

When every position is correct and only the final buffer can have orientation residual:

| Orientation group | Necessary diagnostic |
| --- | --- |
| Trivial | Independent nonidentity orientation should not exist. |
| C2 | A lone nonidentity residual conflicts with the flip invariant. |
| C5 | A lone nonidentity residual conflicts with the modulo-five invariant. |
| D5 | Only the rotation-subgroup residual is allowed by the invariant; this does not mean solved. |
| A5 | Abelianization does not exclude nonidentity residual; inspect the complete element. |

D5/A5 use finite-group certificates and full elements, not a single remaining angle. Passing necessary invariants does not establish an executable solution under current protection. A5 perfectness alone does not prove arbitrary correction coverage by one given commutator; require the project's finite coverage evidence. Show actual residual, inverse and whether a chosen macro matches; never choose construction parameters or target setup automatically. Provide certified reference macros for explicit human selection.

## Protection and helpers

Net protects requirements at the complete operation end. Strict protects them after every actual primitive. Verify both as requested; do not silently change modes. On the graph distinguish final change, temporary motion restored at end, protected conflict and explicitly allowed auxiliaries. Selecting an auxiliary does not remove its protection. Any protection change is explicit and identifies the conflict.

Completing one position does not lock its entire orbit. Automatic Orbit Protection only follows the confirmed unfinished→full-label-solved transition during solving; New/reset do not blanket-lock and manual unlock does not immediately relock unchanged state.

## Cross-orbit work and continuity

Use the model's certified phase order as a default reference, not orbit IDs, project rank or score. A directed o→j relation means a specified verified macro family for o has collateral on j; for that family, completing j later can preserve more work. Custom macros require their own full analysis. If these dependencies cycle, identify the corresponding macros/conflicts rather than invent a safe order.

Separate current work from other-orbit impact. Keep Focus, buffers, draft, reference and residual stage in the current context; expand other orbits for actual collateral/conflicts and related unfinished work. "No candidate in this library satisfies protection" does not mean mathematical impossibility.

Per-orbit context retains Current, buffer bindings, draft, selected macro revision/reference, block requirements and Local view settings. On return re-resolve the identities' current positions, keep draft/choices and invalidate prior execution permission. Global Session protection must never roll back with an old orbit context.

After a successful insertion, locked Next has priority, including another orbit with previous context saved. Without Next, use the confirmed current-orbit block-building candidate rule without executing it. If ordinary candidates are exhausted but buffer/orientation residuals remain, keep this orbit and expose endgame. If the orbit is finished and there is no Next, show later-orbit suggestions and wait for the user's selection.

## Endgame scoring

Keep both formulas in 07 unchanged. Adapt F to the explicit position, orientation, buffer or whole-orbit goal. G_theta requires actual differences in the versioned coherent reference; unknown is not zero. Preserve Direct benefit / Potential preparation / Blocked or unchecked categories. An operation that achieves the chosen nonbuffer orientation goal while transferring residual to a buffer must remain available even without increasing the total solved count. Scores order candidates; full protection decides permission.

## Required continuous evidence

1. Multiple position cycles → ordinary insertion → orientation finish.
2. Stationary orientation-only residual with synchronized graph/names/score.
3. Finish A and inspect B, covering C2/C5 diagnostics and real D5/A5 residuals.
4. A real legal recipe allowed by Net but rejected by Strict due to intermediate effects.
5. Cross-orbit locked Next, return with saved draft and updated physical positions.
6. Endgame save, close, restore, undo and redo with consistent identity/state/history.
7. Exact orbit completion auto-protection, and whole-puzzle completion checked across all labels.

Normal demonstrations start from legal sequences. Manually corrupted parity/orientation states are separate error-detection tests, never legal endgame examples. Maintain a coverage table for all35 moving orbits with actual legal witness, orientation group, buffer boundary and collateral. Finite coverage is not exhaustive proof for all reachable states. Missing necessary endgame workflows block 0.4 completion.

Final continuous solver recording: identify residual → choose existing macro → manual preparation/editing → complete effect/protection → explicit execution → actual result → buffer finish → orbit completion → next work. Reuse existing Solve, graph and keyboard surfaces.

## Authoritative residual consolidation — latest user amendment

`ResidualState` is the single read-only residual calculation derived from the current
Session, not a second stored puzzle. Its fields are orbit, position_cycles,
orientation_residuals, buffer_occupants, protected_requirements, frame_status,
invariant_status, unresolved_dependencies and exact_solved. Retain P_nonbuffer,
P_buffer, O_nonbuffer, O_buffer, FrameUnknown and ExactSolved. Displaced-piece
orientation diagnostics remain separate from correctly placed orientation residuals.
Recalculate on commit, undo, redo, restore and relevant role/reference changes;
windows consume this result rather than independently infer residuals.

Derive suggested stages/intents dynamically: nonbuffer positions → nonbuffer
orientation → A → B → exact orbit completion. Returning position errors must be
shown immediately. Suggestions do not overwrite a user's explicit goal or forbid
interleaving. Prepare/PlacePiece/OrientPiece/FinishBuffer/FinishOrbit share the
existing Solve and keyboard paths. Current displays this actual ResidualState;
After recomputes it from a genuine complete-operation copy simulation. Neither
uses screen positions, display names or subtraction of graphical arrows.

Add lightweight Residual Delta derived from the existing Session journal: before/
after position and orientation counts, actual protected damage and buffer change.
Its changed-piece evidence links to the same canonical graph/Local objects. No
parallel history database, analytics dashboard or expanded history product.

`CompletionCertificate` checks exact identities, positions, orientations, all sticker
correspondence, required known frames and absence of an uncommitted operation
affecting the result. Only a complete current certificate can establish ExactSolved
and make the existing solved-transition protection rule eligible. Preserve the raw
exact-label fact separately from certificate eligibility: an After prediction is
conditional and never certifies the committed Session. Final whole-puzzle checking
includes every moving orbit and required fixed structures. Empty graphs, absent
candidates or visually correct colors are not completion evidence.

Stabilize Authoritative Session, ResidualState, WorkIntent, ReviewContext,
MacroEffect, ProtectionResult and CompletionCertificate through existing services.
Macro Base remains the current operation library with exact indexed metadata;
do not build a new database/browser architecture. No1.0 renderer/runtime/UI-framework
replacement, automatic setup search, synthesis, macro choice or execution.

Final acceptance must demonstrate the complete legal manual workflow above without
editing files, interpreting raw IDs, reconciling inconsistent windows or using
developer-only diagnostics. Missing usable paths for any required moving-orbit
endgame remain explicit0.4 blockers. Final recording pacing is tracked in09.
