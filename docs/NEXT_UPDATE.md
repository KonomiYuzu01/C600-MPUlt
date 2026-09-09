# Next update: human-directed orbit-first preparation

This is the implementation and handoff contract for the next update. It is an **unreleased, isolated foundation** based on C600 Studio 0.3. The next release number is not assigned. It does not add a preparation UI, HTTP endpoint, or executable feature to the published 0.3 package.

## Product purpose and priorities

The sole product purpose is to increase the practical possibility of a human completing the full 600-cell. Functional depth, reliable computation, and speed serve that purpose. Ease of learning and convenient controls are important enabling work; they must not reduce available capability or impose an artificial ceiling on expert workflows.

The user's theory work establishes a workflow and investigates human effort. The stated main bottleneck is **buffer preparation and the operations/checks needed to preserve completed orbits before a macro**, rather than developing or executing the macro itself. Treat this as the development priority and a hypothesis to measure in real use, not a newly measured numerical result.

The human chooses stages, targets, methods, constraints, and execution. Software may locate identities, track buffers and frames, enumerate supported setups, compare candidates, verify full effects, and execute an explicitly selected finite operation. Manual authorship does not require entering every primitive separately. Do not silently choose and solve subsequent targets or unlock protected work.

The unit of optimization is a **preparation cycle**, from an explicit human goal to an operation whose conditions have been checked and which the human chooses to apply. A cycle can include several stars or reuse part of a previous preparation. Star count, primitive count, transaction count, preparation cycles, and human effort are different quantities.

## Reuse the existing architecture

Keep one authoritative Model, PuzzleState, Session, session lock, owned EngineProcess, and journal. Keep the original MPUlt viewport and current revisioned rendering protocol. No additional solver process, database, event bus, rendering engine, generic plugin framework, or duplicate permutation implementation is needed.

Existing foundations:

- core.py supplies complete legal permutations, retained buffer positions, ordered-frame setup paths, full support, exact state, and the fixed-buffer planner.
- session.py owns preferences, preview, commit, stale-state checks, protected orbits, undo, checkpoints, imports, and recovery.
- server.py already serializes work under one reentrant lock and has a bounded asynchronous job path.
- native/NativeHost.cs owns native commands, keyboard context, backend requests, and presentation. Auxiliary views share committed state.

The new preparation.py composes those capabilities into coherent inspection and complete-operation review. It is a domain service to call from the existing engine worker. It owns no mechanical state or persistence.

Proposed integration:

    Native preparation view / command dispatcher
        -> existing authenticated server + worker + SAME session lock
        -> PreparationService.inspect / review
        -> user explicitly chooses to use the reviewed operation
        -> existing Session.preview
        -> user commits through existing Session.commit
        -> existing journal + atomic native update + all linked views

The integration arrows after the new service are the next work package. They are not installed by this branch.

## Available code now

preparation.py exports PreparationIntent and PreparationService. Construct the service with the **same lock used by every Session reader/writer**, not a new independent lock. It is intentionally synchronous and should not run on the native UI thread.

PreparationIntent contains:

| Field | Meaning |
| --- | --- |
| orbit | Explicit internal moving orbit, 0..34; never inferred from a hovered object. |
| destination | Explicit physical position whose home identity is required there. |
| preserve_finished_nonbuffers | Boolean, default true; preserve currently exact-solved nonbuffer positions in the active orbit at the complete-operation boundary. |

The destination remains fixed even when its required identity moves. Inspection returns the occupant at the destination, the required identity, its current location, current occupants of the retained A/B positions, ordered-frame candidates and setup words, and all 35 stages' current exact completion/protection flags.

Fixed centers, fixed buffer destinations, and a mismatched destination orbit remain safely inspectable with explicit reasons. This first service does not review insertions into those unsupported destinations. Final-buffer residual correction and arbitrary buffer relocation need their own declared contracts; do not pretend the nonbuffer contract covers them.

Each capture includes an opaque context ID bound to model, process/service epoch, journal head, state revision/hash, protected-orbit set, and the identity of an existing pending preview. It does not expose that preview's execution token. Undo returning to identical labels still invalidates old context. Restart/reconnect creates a new epoch. Camera and display filters do not change this mechanical context.

Inspection keeps at most one serialized result and returns detached data. It does not save a goal, advance a stage, select a piece, modify filters, create a preview, or write a journal event. Existing model certification caches and lazy progress caches can be populated.

Review accepts one explicit chronological list with an optional prepare segment, one macro segment, and an optional cleanup segment. Each segment holds existing word/star recipe steps. All aggregate limits from Model.normalize apply to the combined operation.

Review returns:

- The exact combined normalized recipe, segment boundaries, plan/context/review IDs, and full predicted post-state hash.
- Complete net support and protected-orbit conflicts, including hidden geometry.
- Predicted target and buffer states, exact gains/losses, and loss of previously finished nonbuffer positions.
- Primitive and star counts, plus structural preparation/cleanup step counts.
- Explicit null values for human preparation cycles and estimated human seconds: these are not inferred from algebraic counts.

The returned meets_target_and_protection flag means only that the predicted target is exactly solved and the implemented boundary constraints pass. analysis_only is always true. A review/context hash is not an authorization token, signature, or replacement for Session.preview/commit.

## Protection semantics

The established orbit-first invariant applies at **complete macro boundaries**. A legal setup preserves each moving orbit as a set. If the certified seed fixes a completed orbit, its conjugate fixes that orbit at completion. This relies on the actual retained seeds, complete collateral, legal setup, and applicable stage order.

Preparing, executing, and undoing a setup may move previously completed pieces temporarily. Protecting an orbit's net effect is not a ban on every intermediate motion. Review the complete prepare/macro/cleanup operation; a safe macro body alone is not proof that an external preparation is safe.

This foundation checks exact full net support, not just seed collateral metadata. Seed support and the prescribed order are reference information, not permission to execute a different word. The currently solved count and an explicitly protected orbit are separate facts; the service never silently promotes one to the other.

Preserving arbitrary sets/colors/layers, strict no-intermediate-motion constraints, and a persisted stage-completion ledger remain future capabilities. The current nonbuffer guard preserves exact correctness within the chosen orbit and excludes the retained buffers. Opting out is explicit and does not hide the resulting losses.

## Execution adapter: required next boundary

Add adapters to the existing authenticated server; do not expose a second local service. The proposed operations are preparation-inspect, preparation-review, and preparation-use-review. These names are a contract proposal, not live endpoints.

For review, use the existing worker/cancellation mechanism under its lock. Respond with request identity so the native UI can discard a late response for an earlier target/frame. Handle unavailable analysis separately from a failed renderer. No hover or ordinary camera navigation should call this service.

For use-review:

1. Under the SAME lock, revalidate current context and the explicit target, constraint, and complete segmented recipe.
2. Recompute or validate the exact reviewed result. Require its plan/review identity and applicable full-effect constraints to match.
3. Preserve an unrelated pending preview unless the user explicitly chose to replace it. A failure must leave it intact.
4. Call the existing Session.preview with the normalized complete recipe and a truthful assistance label.
5. Return the existing preview and, where requested, the established atomic native update. Do not report a committed state yet.
6. Commit only via the existing token/head/revision/hash/protection checks. Keep truthful finite witnesses, cancellation, journal recovery, and undo.

Do not add an unchecked "apply post-state" endpoint. Never promote client-supplied flags such as safe, ready, protected, or analysis_only into backend authorization. A buffer/frame or policy change requires another review. A view-only action should not discard useful analysis.

A multi-step interactive execution mode will need an explicit planned-operation cursor and verified return path. It must label incomplete preparation, retain recovery, and check every continuation. This service does not install that mode or temporarily disable protection to imitate it.

## Frontend and keybind architecture

Build one compact preparation work area around Target, Buffers, Conditions, Plan, and Changes. Reuse existing inspection, Structure, filters and auxiliary views. Keep current/destination identities distinct; permit lookup without requiring a precise hit on dense geometry.

Present full power through searchable actions and progressive disclosure. Keep advanced frame, witness, collateral and protection information available. Convenience should reduce navigation and memory cost, not hide essential controls or force every expert action through an introductory wizard.

Buttons, menus, keybindings, and a searchable command panel should call one native command dispatcher with explicit arguments and context guards. Evolve the existing KeyAction boundary; do not create a separate keyboard execution engine.

| Proposed command | Required scope and behavior |
| --- | --- |
| preparation.inspect-target | Explicit destination; read-only coherent inspection. |
| preparation.select-frame | Local candidate choice with stable frame ID; no mechanical action. |
| preparation.review-plan | Exact explicit segmented recipe; cancellable worker review. |
| preparation.use-reviewed-plan | Current context and reviewed recipe; guarded existing preview adapter. |
| preview.commit / preview.cancel | Existing preview semantics, with visible keyboard ownership. |
| preparation.show-buffers / show-conflicts | Presentation only; no silent filter or protection changes. |
| target.previous / target.next | User-defined task list order; no automatically executed solver continuation. |
| stage.confirm-complete | Future exact check and explicit durable protection decision. |

Replace raw-JSON-first key editing with action search, press-to-bind capture, conflict/scope feedback, restore-one/all, and import/export. Keep advanced editing available. Show current bindings beside actual controls. Visual grip explanations and optional practice help unfamiliar users without reducing the expert command set. Define busy, text-editor, main-view and auxiliary-view routing; never let Enter in another control unexpectedly commit a plan.

## Human-cost records and plan comparison

Do not install always-on telemetry or publish personal solve traces. Add optional local measurement only after its semantics and privacy controls are reviewed.

One preparation-cycle record should identify the human-selected target, stage, applicable plan/context, outcome, and assistance mode. Record actual events such as target lookup, buffer/frame adjustment, protection-policy change, review, explicit execution, cancellation and recovery. Distinguish user interaction, engine wait, execution wait, and idle/thinking time; foreground duration does not prove active solving time.

Initially show separate counts: buffer reconfigurations, frame changes, protection interventions, context/view changes, full primitive cost, and witnessed operations. Do not invent conversion weights or a human-seconds predictor. Measure where preparation can be reused before ranking by a speculative total score.

The initial hypothesis is that removing repeated buffer/protection preparation yields more human benefit than shortening already-compressed macro playback. Validate this on a small representative orbit-first task, including errors and recovery, before claiming a percentage reduction.

## Work packages and completion gates

1. **Preparation integration:** wire the available read/review service into the existing worker and a native work area. Complete explicit target -> coherent buffer/guard data -> selected plan -> full review -> existing preview -> manual commit.
2. **Stage protection and continuity:** persist user-confirmed stages and explicit protection policy, surface exceptions, restore the preparation context across sessions, and prevent stale continuation. Reuse Session transactions.
3. **Reduce repeated work:** expose supported setup/frame alternatives and reusable preparation context. Compare human-intervention counts separately from legal-word cost. Alternative buffer frameworks require new certification.
4. **Command access and convenience:** cover high-frequency preparation operations with unified actions, configurable keys, visual grip guidance, searchable controls, and focused usability checks.
5. **Later extensions:** user-managed target/cycle queues, richer orientation accounting, arbitrary set protection, candidate comparison, planned step execution, branch comparison, and personal macro composition.

For integration, run only focused contracts plus mandatory AGENTS.md checks for changed boundaries. Mechanics or persistence changes require core/reference-map/crash checks; native interaction changes require host compilation/fixture followed by a short real Windows-native pass. Include stale requests, unrelated pending previews, hidden collateral, buffer identity/frame changes, net versus intermediate protection, cancel/reopen, keyboard focus, and complete label agreement.

Architectural alternatives stay isolated. Promote only measured, smaller, maintainable changes with complete state/recovery and appropriate Windows-native evidence. Do not add benchmark hooks or failed candidates to production. No new release or modification of frozen 0.3 evidence is authorized by this foundation alone.

## Run and continue

From this source checkout, run the focused contract test with a fresh output directory:

    py -3 -B tests/test_preparation.py --work-dir work/preparation-contracts-01

Minimal integration usage, inside the existing engine ownership scope:

    from preparation import PreparationIntent, PreparationService
    service = PreparationService(session, lock)  # Reuse the existing lock.
    intent = PreparationIntent(orbit=33, destination=chosen_position)
    inspection = service.inspect(intent)
    review = service.review(
        intent,
        [{"phase": "macro", "recipe": chosen_legal_recipe}],
        inspection["context"]["context_id"],
    )
    # No state changed. Next implement the guarded use-review adapter above.

Read AGENTS.md, this document, preparation.py, tests/test_preparation.py, and the existing Session preview/commit before continuing. The next useful code change is the authenticated adapter and native preparation workflow, not another state engine.

## Foundation validation

The focused contract runner passed eight groups on real Windows with the retained 259,800-slot model and fresh SQLite data: exact identities/frames; complete-operation review versus primitive replay; untouched pending work; net protection and hidden collateral; finished nonbuffer preservation; invalid/stale/cancel rollback; shared-lock serialization; and complete commit/reopen through existing Session methods.

A private CPU-only equivalent-output comparison used the 60 frame candidates of O34, four warmups, then 30 alternating samples per path. Rebuilding inspection averaged 2.383 ms (p95 3.697 ms); the one-entry cache averaged 0.789 ms (p95 1.127 ms). The cached serialized result was 22,471 bytes. Labels, head, revision and database write count remained unchanged. These are small local cache observations, not human-time, HTTP, native-rendering or end-to-end performance evidence.

There are no mechanics/persistence/process changes in this foundation. No new HTTP/native UI path or GPU behavior has been tested because none is connected. Private sample scripts, reports and sessions stay outside the source tree. The released application's source and binaries remain unchanged.
