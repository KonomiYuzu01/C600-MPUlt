> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Post-approval solving workspace

Authority: user approved G1 and G2 in the active task on 2026-09-16, against a156 and its native recording. This authorizes the remaining development, not a claim that unimplemented features are complete. Latest user requirements override older layout/approval clauses. No intermediate test version is requested; retain private validation and finish with a continuous, paced native solver recording. No public release is authorized.

## Limiting factors and bounded design

1. Local currently draws cell shells; these omit the real 433 cut regions. Use the immutable triangle meshes and canonical slot mapping to show the actual selected cell in its tangent frame, retaining true adjacency and piece/label correspondence. An optional selected hosting-cell context must keep its real incidence. Do not invent a flattened twenty-cell tiling. Global retains real 4D incidence and selected group comparison. Observation geometry does not execute mechanical operations.
2. Long-lived tools currently compete in one dock or block each other as modal forms. Reparent the existing controls into owned modeless Local, Global, Keyboard, Operation and Macro Base windows. Retain fullscreen hub and a compact persistent work-context/protection strip. One authoritative Session, one router, and one view instance per tool. Atomic edits may remain modal; no duplicated work state.
3. Input meaning must be visible. Only an explicit bank command changes the active set. Current/Next/orbit selection does not silently switch keys. Keep all 175 orbit records, populate their actual cap/macro/context data, and provide explicit shared action sets to cover every registry command with an unmodified key. Opening a tool does not change bank. Text fields retain normal typing; key capture never executes. Window deactivation/bank change clears held state and requires release.
4. A Grip selects a cap and one of its twelve proper ordered tetrahedral frames. The four local reference directions use short vertex symbols; twist labels show the actual directed permutation of these symbols, not guessed Cartesian axes. The existing full-label `grip_frames.resolve` verifies the emitted legal word. Camera, center, filtering and piece motion do not silently rebind a Grip. Explicit capture/rebind shows old/new cap and frame. Local frame marks and onscreen keycaps consume the same mapping payload.
5. Main labels use mathematical structure. Stable IDs remain canonical and available for copy/diagnostics. Orbit names must distinguish the four colliding coarse summaries with actual cut-structure facts, never an unproved chirality name. Piece identity names derive from immutable Home structure; Home address and present address are separate fields. An address can expand to exact coordinates/cut signature. Sorting/labels never resolve command targets.

## Ownership and interfaces

- Root: adapter.py, engine/build integration, final regression/recording, documentation and promotion. Preserve existing prefs namespace/imports; additive fields only, no bulk rewriting. Shared view preferences store only view/center/layout, outside mechanical review guards.
- Geometry owner: ExperimentCellView.cs, small exact geometry/frame projection helper, focused model tests. Provide immutable mesh geometry separately from revision-tagged per-cell labels/mask. Native view accepts payload with model/state hash, canonical center, positions/identities/labels, mesh regions and explicit frame correspondence. No direct HTTP/Session ownership.
- Window/input owner: ExperimentShell/Workspace/Keyboard/Tools/Input and one small windows partial. Use root APIs; no backend edits. Coordinate geometry calls through an explicit API contract. Existing keyboard editor and command registry remain canonical.
- Naming/backend reviewer: grounded display-name helper/tests and isolated hub captions, after naming proposal is independently checked. No changes to core IDs or source assets.

## Acceptance and rejection conditions

- Each Local cell has all 433 original regions; coordinates/topology derive from retained arrays; every shown region resolves to exact slot, occupant piece and sticker label. Transparent filter changes presentation only, including Actual/forecast distinction. Reject any substituted proxy mesh.
- Every displayed local vertex permutation equals the operation resolved by the current key and ordered frame. Test all twelve frames and eleven actions, representative 1/2/5/20-cell captures, and rejected wrong/mirrored frame. Inverse is exact, not label replacement.
- All registry commands have an editable single-key route in an explicitly visible set. Correct press in owned window invokes at most once; text, IME, lost-focus, held-key bank switch and busy input do not invoke a turn. Restore focus without taking focus from other apps.
- Local, Global, Keyboard, Operation and catalogue can coexist; closing/reopening preserves draft, Current, Next, center and protection. Selecting Current/Next changes only declared work state. Window layout does not invalidate mechanical review or grant execution.
- Short names are traceable and mathematical; all orbit captions distinguish actual signatures. Identity stays stable through legal turns; Home stays fixed; current address follows state. Different display sorting cannot change payload identity.
- Complete legal preparation, macro, cleanup, hidden conflict, cancellation, reference/relative block, orientation/final-buffer and reuse remain exact across all labels. No automatic target/setup/macro search.
- Fresh native run at actual available sizes; same-state before/after screenshot comparison. Record actual display/DPI/GPU evidence. Continuous final solver recording uses the tested build and discloses scripted versus physical input. Internal checks and user approval are separate facts.

## Order

Verify data/contracts → independently review design/plan → implement geometry + windows/input + names in disjoint files → integrate exact frame/state transport → spec/code review → focused tests and native continuous loop → complete remaining macro/reuse/endgame/consolidation gaps → broad required acceptance → packaging integrity and final recording. A failing correspondence, lost context or false protection blocks promotion.
