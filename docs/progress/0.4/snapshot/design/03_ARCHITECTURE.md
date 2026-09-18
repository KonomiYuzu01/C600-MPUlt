> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Magic 600 Cell — Architecture and Implementation Contract

Revision: 2026-09-14 / 0.4-R2  
Method: **Orbit First Block Building Solving**  
Status: confirmed product direction; implementation contract; runtime work remains. **Build two operational experiments first: G1 Grip/Twist mechanics and G2 the concrete abstract-hub frontend. Submit them for separate user reviews. Continue production integration only after both are explicitly approved.**

## S00. Authority, scope and reading contract

### Confirmed solving-assistance amendment — 2026-09-16, after the macro-variant stage

The user resumed development and explicitly revised three former manual-only boundaries. These precise permissions supersede the conflicting historical clauses below; the immutable model, exact transactions, protection correctness and prohibition on setup search/automatic solving remain.

- Automatically derive an exact reference variant from the **user-selected Grip/local frame**, then show the complete operation preview. An ambiguous mapping requires the user's explicit choice. A display-coordinate change or arbitrary legal-word conjugation is not sufficient proof of a geometric reference transform. No setup search, automatic choice of a solving macro combination, or automatic execution is authorized.
- After a successfully committed insertion, automatically activate the user's locked Next first; otherwise activate a switchable block-building candidate **within the current orbit**. The user explicitly confirmed that a locked Next in another orbit also takes priority: switch to its orbit while retaining the previous orbit's draft and settings. This changes work context only, never executes the following operation, overwrites a bookmark while browsing, or reuses an old review for the new target. Precise candidate ordering and exhausted/satisfied handling must be visible and justified by actual block structure.
- Automatically protect an orbit only when it transitions from unfinished to **exact-label solved during solving**. New/reset do not blanket-lock the initial solved model. Explicit manual unprotection must not immediately relock the same unchanged state. Preserve the chosen net/prefix protection mode, full hidden scope, journal correctness and explicit execution. Automatic protection must be coordinated with the existing commit/recovery boundary, not published ahead of successful commit.

Macro Base classifies likely **solving use**, not just affected-orbit sets. Affected orbits, verified Star, pure cycles and frame effects are calculation inputs to explainable local inference. Multiple orbit uses and Other are permitted; insufficient evidence remains visibly different from a known general use. Current protection changes applicability without pretending to prove human intent or rewriting structural certificates. The user confirmed **Orbit Based Pure Piece Cycle** means: for the stated orbit, exactly one nontrivial piece cycle, with **every edge carrying no additional orientation change relative to the explicit reference**. Other-orbit effects are reported separately and do not invalidate that scoped label. A positional three-cycle, the absence of fixed-position twists, or eventual frame return after a whole cycle is insufficient.

The user further confirmed that orbit-local Pure Cycle and Star use is evaluated against **the currently enabled orbit/piece protection**, not an implicit lock on every other orbit. Verify the full Prepare / Macro / Cleanup operation in the chosen final/strict-prefix mode, including hidden scope; list effects on unprotected orbits separately. A body with collateral can therefore remain relevant to that orbit's solving use. Intrinsic structure and this composition's protected applicability must not be conflated.

Extend the continuous workflow from finding a piece through manual preparation to a state handled by the chosen macro, insertion and the next operation. Macro Base must support direct editing/composition and saving the explicitly composed result as a new independent macro. For pure-cycle components, expose the actual composite effect and current orbit/piece protection together. The user confirmed that a graphical “cycle block” denotes an **actual piece cycle in the final composite permutation**; expanding it shows each piece's position and orientation correspondence. It is not a claim that a group moves as a rigid mechanical block. Recompute the complete composition: pure components can yield different cycle lengths, cancellation or orientation-only residuals. Preserve original component revisions and explicit order; never search for a composition or inherit its safety/purity from the components. Missing orientation references remain unknown.

The former coarse triangular hosting-cell overview may be substantially simplified or removed to recover workspace area. Migrate its unique identity/position selection, role, Current/Next, phase/body scope, frame inspection and keyboard operations before removing duplicate controls. Retain faithful Local/puzzle geometry and exact sticker correspondence. These changes are new authorized work, not evidence that the current native slice is complete.

Consolidate Solve functions into one compact, optionally expandable independent tool window, reusing existing faithful geometry to show focused piece, buffer positions, frame/orientation effects and protection suggestions. Retain the full graphical workspace and separately launchable observation/Keyboard windows, consistent input focus and single-key routes. Prefer concise in-place geometry and symbols with precise meanings; detailed explanations remain available in Help/inspection. Existing functional coverage, old-UI alignment, native verification, final recording and privacy requirements remain active. These additions are implementation work, not a claim of completion.

### Execution addendum — 2026-09-16

The subsequent confirmed [integration and endgame contract](08_INTEGRATION_AND_ENDGAME.md) defines one versioned ReviewContext, distinct Current/Operation/After graph meanings, explicit orientation/buffer intents, residual and invariant diagnostics, per-orbit continuity, scoring adaptation and 35-orbit evidence obligations. It supersedes ordinary nonbuffer-insertion-only assumptions without authorizing automatic setup, macro composition or execution. Implement in existing services and the existing Solve/graph/keyboard surfaces.

The latest detailed user contract in [07_NAMING_AND_RECOMMENDATION.md](07_NAMING_AND_RECOMMENDATION.md) is authoritative for structural naming, coherent orientation classification and both ranking formulas. In particular it supersedes the earlier single-cycle-only Pure requirement and the prior nonnumeric use inference. Its census/signature/address assumptions must be verified before they become claims in the UI. Mathematical proofs, heuristic scores and complete-operation execution permission remain separate. Existing immutable identities and human choice boundaries are preserved.

Latest keyboard refinement: direct solving operations (Grip, Twist, Macro, Piece Filter, Solve work and opening/switching sets) retain multiple editable single-key sets. General utilities such as opening tool windows, Reset, Save and changing views are grouped in a dedicated Functions set; modifier chords are allowed when unmodified keys are insufficient. Show its chord keycaps below the onscreen keyboard. Provide a dedicated unmodified-key entry from the other sets and an explicit unmodified-key return to the prior set, independently of typed set-ID switching. Preserve customized mappings and held-key release barriers. This supersedes the former requirement that every general utility must have an unmodified-key route; it does not remove complete editable command coverage.

The new composite-cycle view must be primarily graphical: dedicate the recovered coarse-triangle area to actual directed cycles, position sockets, piece occupants and inspectable orientation correspondence. Reduce explanatory prose and empty tool-window area; retain concise identities, state and conflict cues, with exact detail on demand. A collection of textual cards is not an adequate replacement.

The user has explicitly approved G1 and G2 in the active development task, after the a156 native sample and its recorded examples. Continue the remaining P2–P6 work (called G3–G6 in the latest user instruction). Historical Pending statements describe the former checkpoint, not a current blocker. Approval does not certify unfinished functions. No intermediate test build delivery is requested; internal checks remain mandatory. Finish with a continuous native solver recording demonstrating the improved mechanics and subsequent features together.

Retain the fullscreen hub and add cooperative independent Local, Global, onscreen Keyboard and operation tool windows. All share the same authoritative state and explicit input focus. Geometry views primarily provide faithful observation; functional actions belong in aligned, expandable tool windows. Local must show actual 433-sticker cell structure, real adjacency/layers and piece/sticker correspondence, including transparent sticker filtering. Do not replace actual geometry with an abstract proxy to match the hub style.

Use verified mathematical names in the primary UI while retaining canonical IDs and compatibility. Distinguish stable piece identity, Home structure and current location. Grip frame marks, short keycap notation and executed permutations must be proved to agree. Provide editable unmodified-key routes in enough explicitly selected, visibly named sets; no implicit keymap switch. Add a color-indexed Local-center picker and keyboard routes. Existing manual-solving, immutable-model, transaction, recovery and privacy boundaries remain unchanged. Routine isolated implementation is authorized; unresolved architectural direction changes still require user confirmation.

This document replaces the previous architecture in full. It integrates the original quoted product brief and every subsequent correction: a piece-focused workflow, keyboard convenience as the preferred operation method, an abstract graphical hub built around Macro Base, orbit-specific keyboard banks, and an explicitly locked Next Piece. Older design files are historical explanations, not competing implementation instructions. The current user's instructions take precedence if they change this contract.

Implement the smallest coherent system satisfying this complete contract. Reuse the verified 0.3 engine, persistence and native rendering boundaries; reorganize the product around the new hub. Do not interpret “from scratch” as permission to rewrite working puzzle mathematics, discard unfinished changes, or rebuild the renderer before the workflow works.

0.4 must support sustained human solving through block construction, insertion, protection and final buffer/orientation work. A usable middle-game demonstration is insufficient. 1.0 must be publishable, with a modern integrated frontend and defensible comparisons against 0.3. Neither a complete-solve probability nor a human time estimate is established by this design.

The accompanying executable development Prompt is supplied in the current conversation, not in a new Prompt file. Start implementation in the isolated development experiment area when that Prompt is used. The user's latest instruction supersedes the earlier single-gate rule: finish working G1 and G2 experiments, present them independently, and pause at the review checkpoint before continuing production development/integration. Written specifications, elapsed time and partial approval do not approve either experiment. Approval of G1 does not imply approval of G2, or vice versa. See S17.0 for the exact checkpoint.

Read S00–S04 first, then the sections needed by the current phase in S17. Use S18 as the acceptance checklist. Consult [reference evidence](06_REFERENCE_AUDIT.md) for pinned sources, not as a substitute for the latest requirements here.

| Section | Purpose |
| --- | --- |
| S01–S04 | Foundation, method, domain objects, state and execution boundaries |
| S05–S07 | Macro Base, protection and reusable human work sheets |
| S08–S11 | Main graphical hub, keyboard system, Local/Global and filters |
| S12–S14 | Reference alignment, persistence, performance and frontend migration |
| S15–S16 | Exact worked examples and UI states |
| S17–S20 | Implementation sequence, acceptance, delivery and traceability |
| S21 | Sources and factual evidence limits |

## S01. Retained foundation and non-negotiable invariants

### S01.1. Verified starting point

Locate the implementation checkout through the root `PROJECT_MEMORY.md`, inspect its current `AGENTS.md`, Git status and applicable development documents, and preserve pre-existing changes. At the design audit, the source HEAD was `5e1d35de792ccc8db896b8abf74ff2b1b00e0749`; the previously dirty paths were `SOURCE_MANIFEST.json`, `docs/DEVELOPMENT_LOG.md`, `docs/LIMITATIONS_AND_ROADMAP.md` and `docs/NEXT_UPDATE.md`. Recheck these facts at implementation time.

The working basis includes `core.py`, `grips.py`, `session.py`, `preparation.py`, `server.py` and the existing native host, picking, structure, auxiliary view, docking and snapshot modules. `preparation.py` is an isolated, read-only preparation service awaiting integration, not a completed 0.4 workflow.

The audited model identity is `58c83f336e451aa64147fe1b8db2d60d7d43806aa3a35122f8403d5af51253f9`.

### S01.2. Exactness and ownership

- Keep the complete 177,120 pieces and 259,800 labelled slots. There are 35 moving orbits and 600 fixed pieces; progress over moving pieces uses 176,520, with the denominator named.
- Preserve immutable model assets, original binaries, stable model IDs, all 1,200 primitive generators, retained seeds, certificates, trees and verified reference maps. Branding changes do not change mathematical identity.
- `at[position]` identifies the occupant; `where[identity]` identifies its present position. A home destination normally requires the identity with the same canonical ID. An arbitrary block destination instead requires an explicit identity/destination pair.
- There is one authoritative Model/PuzzleState/Session, one session journal and the existing authenticated server/worker. All readers/writers use the same reentrant Session lock. Draft copies are disposable predictions, never a second authority.
- Full finite legal witnesses determine effects. Chronological source-to-destination composition and orientation/slot transport must be exact. A macro name, diagram, short hash or local cycle shape is not proof.
- Draft/review is read-only. `Session.preview` and `Session.commit` remain the execution boundary; a review hash is not an execution token. Do not attach automatic target-solving endpoints to the new hub.
- Durably commit the journal transaction before publishing the corresponding native state. Failure, stale input or cancelled analysis must preserve unrelated pending previews, committed labels, preferences and history.
- Respect existing EngineProcess ownership, authentication and parent-pipe lifecycle. Stop only owned processes. Test with isolated sessions; never reuse a personal solve database as a fixture.

### S01.3. Preserved input, topology and rendering contracts

Keep existing 3D/4D drags, Ctrl gestures, multi-click twists, undo/redo, checkpoints, import/export, timer and recovery as command adapters. In particular:

- Shift-left exact hit at position `p`: inspect occupying identity `q = at[p]`, including the centers of its home cells. Shift-right: keep destination `p` fixed and inspect the current location of the identity required there. Preserve Shift-left 4D drag disambiguation; a drag must not also count as a click.
- Use the verified native-to-lab mapping and exact current interaction mask. Reject hidden, stale and malformed hits without mutation. Visible reference annotations are not permission to twist; they must not cause click-through to unrelated geometry.
- Display canonical C1–C600 and V1–V120 while preserving documented legacy zero-based filter syntax. Do not reinterpret existing saved expressions.
- The face-adjacency graph is connected and four-regular. Cell-center BFS layers partition cells; whole-piece membership can overlap layers. Each vertex belongs to 20 cells; each cell has four vertices. Geometry uses actual model incidence and 4D coordinates, not invented metric relationships.
- Retain atomic snapshot/delta/full fallback semantics. State, colors, visibility, interaction, annotations, progress and related status must describe one consistent revision. Never patch a delta onto an unrelated predecessor.

## S02. The human solving method

The normal work loop is:

1. Choose an orbit and a working block; inspect existing achievements and explicit protection.
2. Select a piece identity and the intended destination/reference that will extend that block.
3. Inspect A/B occupants and frames; optionally lock one Next Piece for later.
4. Browse existing Macro Base entries by verified structure and current requirements. Choose the macro personally.
5. Assign roles and reference; enter any necessary Prepare and Cleanup manually, normally through approved Grip/Twist inputs in a draft.
6. Review the complete chronological operation over the entire model. Resolve missing input and conflicts personally.
7. Explicitly preview and execute the reviewed operation; inspect actual results.
8. Explicitly preserve the new achievement, retain useful work settings, and activate the locked Next Piece when ready.

The retained orbit sequence is model data, not numeric ID order. At the audited source it is:

`6, 0, 17, 15, 2, 22, 21, 8, 23, 9, 29, 25, 13, 11, 10, 34, 28, 26, 24, 16, 14, 12, 3, 33, 32, 31, 30, 27, 20, 19, 18, 7, 5, 4, 1`.

Show that certified sequence where relevant, but do not automatically advance stages or claim it makes an arbitrary chosen word safe. Orbit IDs remain stable identities independent of display order and user work order.

### S02.1. A block is an explicit achievement

A block has an ID/name, member piece identities, intended positions or verified relative relations, an explicit reference, progress predicates and a user-selected preservation policy. Start with two bounded representations:

- **Home block:** an explicit set of identity/home-position requirements, optionally connected by actual topology. Completion requires the selected exact-slot/orientation condition, not merely matching face colors.
- **Referenced block:** explicit identity/position/frame relations expressed in one verified common reference. Completion and preservation use those relations. This can describe a useful partial structure away from Home; it is not automatically a certificate that the whole structure can move rigidly.

Use a small typed schema for these requirements, not a user-programmable predicate language. Verify which relative-frame operations the retained model actually supports. Unsupported relationships remain visibly unsupported; never substitute a drawing or adjacency check for a mathematical test. If an unsupported relationship is necessary to the intended method, record it as a 0.4 product blocker and implement the missing exact boundary.

Building a block can intentionally disturb an unprotected part of the active orbit. Show gains, losses and retained members before execution. “Completed”, “protected” and “selected” are separate facts. Do not silently protect an entire orbit because one block is complete, or silently unlock it to make a macro applicable.

### S02.2. Suggestions remain structural

Show unchosen candidates such as “this piece shares the selected cell boundary”, “this existing macro has an A-to-target cycle on this orbit”, or “this candidate extends the current declared relation”. Each suggestion names the predicate and evidence, and distinguishes missing conditions. Sort by explicit facets or user-selected counts, not a secret recommendation score.

Suggestions never select the next target, generate setup words, choose a macro combination or execute a batch. Do not expose target-dependent tree lookup as an allegedly passive classifier. Expanding a star explicitly named by the user is allowed; deciding which star solves their target is not.

### S02.3. Current and locked Next Piece

Maintain separate Current and Next objects. Next stores a stable piece identity, optional intended destination/reference, and associated orbit/block. It is a work bookmark, not a mechanical protection policy.

Provide graphical and editable keyboard commands: `Next: Pin selected piece`, `Replace`, `Clear`, `Locate`, and `Activate`. Pinning a position requires showing its occupant and explicitly choosing identity tracking. Track the chosen identity through every move, undo, redo and resume. Hover, filter, library selection, camera changes and bank switching must not overwrite it.

After a current operation, show Next's updated location, orientation and whether its intended condition is already satisfied. Do not substitute a different piece. Activation is explicit: preserve the previous work draft, make Next the current work intent, clear the Next slot, rebind the visible roles and mark relevant review results stale. Do not execute, select a new macro or promote another Next automatically.

Pinning or changing an unrelated Next bookmark does not invalidate the current mechanical review. If Next is referenced by the current recipe, block predicate or protection, that dependency does invalidate it. This distinction keeps safety precise without forcing unnecessary rechecks.

## S03. Domain objects and ownership

These are logical responsibilities, not an instruction to create one class, module, service or database table per row. Prefer small cohesive additions to existing boundaries.

| Object | Required information | Owner / persistence |
| --- | --- | --- |
| Model facts | IDs, incidence, generators, orbit order, reference certificates | Existing immutable Model |
| Committed state | Full labels, epoch, revision/hash, journal head | Existing Session/journal |
| Work context | Active orbit, block, Current identity/destination, A/B positions, reference, active phase | Versioned workspace preferences, model-bound |
| Next bookmark | Explicit identity and optional intention, derived present location | Workspace preferences; location recomputed |
| Block | Explicit members/relations, frame, completion/preservation predicates | User work records; results derived |
| Macro record | Stable ID, name, immutable recipe revision, provenance, notes, typed role declaration | Macro library, model-bound |
| Effect certificate | Full canonical effect, cycle/orientation facts, cost, proof version | Rebuildable bounded cache |
| Macro relation | Equality/inverse/reference relation, scope, verified transform, differences | Version-bound relation index |
| Applicability | Role/frame/method/goal/protection diagnostics for one exact context | Disposable contextual result |
| Work sheet/template | Version-pinned macro steps, required role/reference fields, explicit phase steps and settings | User library; no execution permission |
| Operation draft | Chronological Prepare/Macro/Cleanup, concrete bindings, source segment provenance | Saved work draft; normalized before review |
| Protection policy | Explicit orbits, pieces/positions, block predicates and net/prefix policy | Session/work preferences, revisioned |
| Review | Frozen input identities, full effect/outcome, guard set, prefix/net findings | Ephemeral; cannot survive as permission |
| Keyboard bank | Stable ID, orbit, purpose, command bindings, approved grip/frame captures and macro references | Versioned user preferences |
| View state | Focus, cameras, layout, filtered/displayed sets, hover | Workspace/UI; never mechanical authority |

Use typed IDs in command payloads and imports. A cell, cap, piece identity, position, orbit, macro revision, frame and bank ID must not be interchangeable integers. UI labels may be compact, but accessible descriptions and inspectors expose their exact type.

### S03.1. Action registry

Route menu items, graphical actions, keyboard bindings and onscreen controls through one command registry. Each action declares input schema, context, availability reason, whether it changes work metadata/draft/policy/committed mechanics, and undo/cancel behavior. A disabled button and a keyboard command must fail for the same reason.

The registry covers every retained program operation. Generate the searchable command index, editor choices and keyboard coverage report from it. Do not build separate shortcut logic in Local, Global, Macro and Solve. A bank is a selection of mappings into this registry, not an executable script.

### S03.2. Analysis path

`Hub / command -> existing authenticated worker -> same Session lock -> frozen capture -> bounded exact analysis -> context-tagged result -> UI adoption if still current`.

A worker may analyze an immutable capture outside the lock only if its inputs are detached and every execution-relevant field is revalidated under the shared lock. No native UI thread performs full-model macro expansion. Reuse the current queue/cancellation mechanism; do not add another service process.

`Use reviewed operation -> revalidate under lock -> explicit pending replacement decision if needed -> Session.preview -> explicit Session.commit -> journal -> atomic native publication -> linked views`.

Audit the current implementation of each arrow; proposed endpoint names are not evidence of existing routes.

Audit the existing preparation inspection payload as well as execution routes: it can expose setup words/frame alternatives from older solver-oriented work. The human hub adapter must not request or display target-solving setup suggestions, automatic next/suggest output or target-dependent tree lookup. Preserve useful explicit frame facts and expansion of a recipe the user actually selected. A retained legacy/internal capability does not authorize connecting it to the new workflow.

## S04. State, invalidation and execution

### S04.1. Review state machine

Draft -> Checking -> Ready / Missing input / Mismatch / Conflict / Unverified. Any relevant change yields Stale. Ready permits requesting a preview, not committing without the existing preview token and its guards. Previewed -> Committing -> Committed / Rejected / Recovery required. Use explicit action labels, not an ambiguous general Enter-to-continue behavior.

Ready means the selected review goal and policy have been checked. A useful user-selected preparation operation need not solve the target immediately: support explicit review intentions such as Prepare buffer, Extend block, Insert target and Endgame. Show whether the stated goal was achieved separately from whether execution obeys protection. A user may explicitly execute a legal, policy-compliant intermediate operation while seeing unmet target predicates; do not incorrectly reject every non-solving move.

Existing `PreparationIntent` only reviews a nonbuffer Home destination. Extend the declared intent contract for relative blocks, preparation and final buffers. Do not pass unsupported endgame through that narrow interface and relabel the result as supported.

### S04.2. Guard dependencies

Bind model identity, service/process epoch, journal head, full state revision/hash, canonical complete recipe, macro content revisions, role bindings, reference/certificate version, relevant block predicates, protection revision and pending-preview identity. Preserve the exact normalized order and segment boundaries. Revalidate all dependencies before creating a preview and before committing.

| Change | Required effect |
| --- | --- |
| Turn, undo/redo, import/reset, reconnect or restart | Invalidate mechanical reviews/tokens; same labels after undo do not revive old context |
| Recipe, relevant macro revision, target, roles, reference, block or protection | Invalidate applicable review and proposed preview |
| Another pending preview appears | Old use-review cannot replace it implicitly |
| Filter, camera, hover, view layout or library sort | No mechanical invalidation unless the draft explicitly captured a changed set as an input |
| Keyboard mapping only, with identical operation context | No mechanical invalidation; always reset held input state |
| Bank restores a different orbit/reference/work intent | Revalidate restored intent and mark dependent review stale |
| Rename or note with unchanged recipe | Update presentation; no effect-certificate rebuild |
| Next bookmark unrelated to current operation | No mechanical invalidation |
| Next activation or editing a Next dependency of the current operation | New work intent; invalidate dependent review |

Cancellation, analysis limits and timeouts produce explicit incomplete results. Never convert missing analysis to Safe. A late response can populate a keyed cache but cannot overwrite current roles, display Ready or replace a newer draft.

### S04.3. Manual turning and transaction boundaries

Make input destination explicit: `Draft: Prepare`, `Draft: Cleanup`, or `Live puzzle`. In draft mode, approved Grip/Twist input appends exact legal primitives to the chosen segment and updates a clearly labelled predicted view. Committed state remains separate. Switching input destination is explicit and clears held keys.

In live mode, each accepted twist is an immediate complete operation under the active policy and existing native-turn transaction. It cannot borrow the promised cleanup of a future draft. If a user wants protected pieces to move temporarily inside a larger safe composition, they construct and review the complete draft first. This prevents a misleading “safe later” exemption while preserving ordinary manual play when its policy permits it.

Do not replay uncertain commits after a lost response. Fetch authoritative state and journal identity, recover the view, and re-enable input only after an actual valid frame. Busy inputs are accepted or rejected visibly with bounded buffering; no unbounded turn queue.

## S05. Macro Base: exact classification and convenient use

### S05.1. Layer 1 — intrinsic action

Normalize each explicit recipe, retain its original representation, and compute its complete exact action independently of the current puzzle occupants. Record:

- All affected orbits, per-orbit support and complete cross-orbit collateral.
- Directed piece permutation cycles, including cycle length and order. Fixed piece positions with nontrivial slot/orientation changes remain affected.
- Exact sticker/slot transport and frame/orientation change, with the reference convention and proof version. Avoid a universal scalar “orientation number” across incompatible groups.
- Primitive length, expanded operation size, structural steps and applicable limits. Cost is not a human-time estimate.
- Verified star, orbit-local three-cycle, other supported effect, exact identity, or Unverified. Categories can overlap by scope; identify that scope.
- Raw recipe/version, source and name/notes. A user's description never changes the certified facts.

A verified star must satisfy the retained model's actual star predicate/certificate: declared orbit, legal witness, A/B/Target mapping, direction, exact orientation/frame behavior and known complete collateral. Do not invent a weaker predicate just to label more macros as stars. A three-cycle seen on one orbit is classified as an orbit-local three-cycle until the additional star requirements are proven.

### S05.2. Layer 2 — proven relationships

| Relationship | Proof and presentation |
| --- | --- |
| Same complete net action | Compare canonical full-model actions exactly; retain both raw recipes, costs and intermediate-motion differences |
| Inverse | Verify exact composition is identity over every labelled slot; show reverse direction and independently evaluated prefix behavior |
| Reference transform | Store explicit user-selected reference mapping, its legal witness/certificate, direction convention and transformed complete effect |
| Same action on one orbit | Compare that orbit exactly and label the relation local; show other-orbit differences |
| Similar cycle type/use | A browsing facet only; never present as equality or substitutability |

Hashes index candidates; exact comparison or an applicable exact certificate proves the relationship. Do not merge identities of different library entries. Use a group with named variants and differences. A shorter equivalent body can have different temporary motion and thus different applicability under a strict prefix policy.

A reference transform must be explicitly selected by the user. Compute that exact transform and validate that it is a supported puzzle automorphism with a legal recipe mapping. Multiple possible frames are displayed as candidates for user choice. Do not pick the frame that happens to solve the target, infer arbitrary setup, or treat a geometrically plausible rotation as a certified macro transform.

### S05.3. Layer 3 — current applicability

For the chosen orbit/block/piece, A/B occupants, target, reference, complete draft and protection, display independent facts:

1. Model/recipe support: valid, unsupported or still checking.
2. Role correspondence: matched, missing role, wrong orbit, wrong occupant, target collision or unsupported role assignment.
3. Frame/orientation correspondence: exact, explicit choice needed, mismatched or unverified.
4. Work goal: predicted satisfied, partly satisfied or unmet, naming the relevant predicates.
5. Complete-operation protection: net findings plus separate prefix findings for all orbits and declared block constraints.
6. Currency: current, checking or stale, with the input that changed.

Examples of useful explanations: “Body cycles A -> Target -> B; the chosen identity is currently at B, so this direction does not insert it.” “Target permutation matches, but two protected orientation labels change.” “No Cleanup supplied; body classification is known, complete-operation suitability is not.” No single Ready badge may obscure these distinctions.

An intrinsically conflicting macro body can still be selected and combined if the complete Prepare/Macro/Cleanup cancels the conflict. Browse it with its warning; judge execution against the complete composition. Conversely, an individually harmless body cannot certify unsafe preparation.

### S05.4. Library interaction

Use a compact visual catalogue, with real effect glyphs, name, orbit scope, direction, frame marker, cost and applicability reasons. Support Boolean facets for orbit, cross-orbit effect, star status, cycle direction/type, orientation, buffer role, explicit use tags, user notes and verification state. Multiple labels per entry are normal.

Allow human naming, notes, pins, collection membership, recording, text input, paste validation, edit-as-new-revision, duplicate, inverse creation, explicit reference transformation, import/export and source inspection. Retain parser diagnostics with line/step location and preserve invalid text for editing without executing it.

Selecting a row previews its known action and suitability. `Use in Macro`, `Use in Prepare` or `Use in Cleanup` adds an explicit instance to the draft. Double-click or drag must never execute committed moves. Macro hotkeys refer to stable ID and content revision; library sorting does not change what a key means. Missing or changed revisions are visible, never silently substituted.

Build certificates lazily from explicit entries and bounded batches. Cache immutable effects by model/recipe/proof version; keep context evaluation separate. Virtualize long lists. Hover uses cached small data and causes no full-model analysis or DirectX reconstruction. Cancellation does not remove the library entry or its input.

## S06. Orbit Protection throughout the workflow

The protection strip remains visible above the main working region, including compact layouts, Macro input, fullscreen puzzle, floating windows and bank picker. It shows the active orbit/block, protected scope, net/prefix policy, review currency and conflict count. Do not rely on color alone; use labels and shapes. A protected orbit can be collapsed to a compact index entry but never disappear because of a filter.

Support explicit whole-orbit, identity/position-set and block-relation protection. Define each scope precisely:

- A position lock preserves its occupants and slots at the operation boundary.
- An identity lock preserves the selected identities' location/orientation at that boundary.
- A completed-block lock preserves its declared predicates.
- A relative-block lock uses verified relative predicates; it must not silently become a Home lock.

For a currently unsolved protected orbit, “preserve” normally means no change to its captured labelled state, not “make it solved”. State the policy in the UI. Changes to policy require an explicit user action, with affected scope visible and all dependent reviews invalidated.

Analyze the complete Prepare / Macro / Cleanup on **all 259,800 labels**, including filtered-out or collapsed regions. Show a per-orbit table with moved pieces, slot/orientation changes, gains/losses, protected conflicts and stage/block implications. Drill-down exposes exact IDs and a noninteractive reference overlay.

Separate:

- **Net preserved:** protected requirements hold at the complete-operation boundary.
- **Temporary motion:** a protected object moves at an intermediate primitive; show the first violating step and phase when computed.
- **Prefix unchecked:** full intermediate analysis has not completed or is unavailable.
- **Strict prefix policy:** no intermediate violation is permitted; unchecked/unknown blocks execution under this policy.

Net policy may permit an operation whose prefixes move protected pieces. Show that fact, never relabel it “untouched”. Full net analysis is required for every executable operation; strict prefix analysis can be computed on demand with bounded incremental checking. Existing preparation code proves net effects only, so do not claim prefix support before implementing it.

Do not equate “no protected conflict” with “target solved” or “method useful”. Keep protection, work-goal outcome and star classification separate. Template reuse, inverse grouping and reference transforms must all obtain fresh applicable protection results.

## S07. Reusable work sheets without an automatic solver

A work sheet is a short, visible form and three chronological phase strips, not a node programming language. It contains:

- Chosen macro IDs/content revisions and explicit finite steps.
- Named user inputs: piece, target, A/B roles, reference/frame, block and policy dependencies.
- Fixed Prepare/Macro/Cleanup steps, or an explicit `Cleanup = inverse of this Prepare segment` relationship.
- Optional view/filter/bank preferences and intended work goal.

Keep three phases; a phase can contain multiple explicit finite recipe steps. Flatten and enforce existing aggregate normalization/resource limits across the entire operation. Preserve phase provenance after flattening. No loops, branches, target iteration, hidden searches or commands which dynamically choose a macro.

On reuse, keep the chosen method, fixed steps, library versions and useful display settings. Clear old review/preview authority. Mark variable fields as requiring confirmation or new binding. Show fixed bindings which no longer fit. A new target does not mutate the fixed macro into a new star or automatically choose a reference transform.

If the user explicitly requests a supported reference transform, calculate that transformation and expose its mapping. If it fails, preserve the draft and display why. Automatically computing the inverse of a specifically chosen Prepare segment is permitted because the operation is already fully specified.

Separate `Save macro` (a concrete legal operation) from `Save work sheet` (a reusable method with human inputs) and `Save workspace` (layout and current work context). These are accessible through the same hub, keyboard index and save menu, without being merged into an opaque universal template.

Recording distinguishes Draft and Live sources, start/end boundaries and raw chronology. It does not accidentally include camera/filter actions as twists. Editing the recorded recipe invalidates its certificates. Every concrete compiled operation can be inspected/exported as a finite witness.

## S08. Main graphical hub — the product's primary workspace

### S08.0. G2 frontend experiment and independent review

Implement the concrete hub frontend in the isolated experiment area before promoting it into the product. It must be runnable and sufficiently complete for the user to judge actual repeated work, not just a layout image or static click-through. Use real model-derived fixtures and exact effects for its demonstrated operations. Include Macro Base classification/browsing, typed graph selection/role/reference actions, the three phase strips, persistent protection, Current/Next, Local/Global/puzzle linkage, filter switching, bank ID feedback and work-sheet reuse. Include compact/high-DPI, missing input, mismatch, hidden conflict, stale and recovery states.

Provide an independent launcher/entry and a short G2 review route with expected results, known limitations, build identity and the specific interaction decisions to approve. An isolated session may be used for execution demonstrations; never connect to personal solve progress or change the production launcher. G2 must be testable with a pointer and existing validated input so evaluating its frontend does not require approving G1 first. It may offer the clearly labelled experimental G1 integration as an additional route.

The user reviews the concrete visual language, layout, operational usefulness and repeated-work behavior. Address requested changes inside the experiment and resubmit the changed scope. Do not continue production integration until the separate G1 and G2 decisions are both approved. This is the user's explicit review requirement, not a new gate inferred from a skill.

### S08.1. Default composition

Use a restrained geometry workbench, with aligned typography, stable regions and high information density. The abstract hub owns the central workspace. The actual puzzle is an integrated, resizable viewport, not a permanently dominant display that reduces the hub to a sidebar.

At a 1600 x 1000 logical-pixel workspace, start with:

- Top 40: session/source status, command Index, orbit/block selector, current bank ID and short purpose.
- Next 40: persistent protection strip, operation state, Current and locked Next chips.
- Left 280: Macro Base with compact facets and a virtualized list of effect thumbnails.
- Center flexible, approximately 880: a switchable **Block / Operation / Reference** canvas with fixed object semantics.
- Right 360: actual puzzle viewport above an operational Local/Global comparison deck; users can resize or detach the two auxiliary views.
- Bottom 180: Prepare / Macro / Cleanup strips, input destination, review findings and explicit Preview/Commit controls.

These are initial layout constraints, not screenshot-only pixel promises. At 1280 x 800, collapse Macro Base and side views into labelled drawers without hiding protection, Current/Next or bank state. Support 100/125/150/200% DPI, keyboard navigation, readable IDs and screen changes. Restore floating windows into visible bounds. A puzzle-focused layout remains available, with the same persistent context and immediate return to the hub.

The Index is a small persistent launcher: all commands, open views, saved work sheets, filters, banks and session actions. It is searchable and usable entirely by keyboard. Secondary windows are optional views into the same state; the main hub must remain usable when they are closed.

### S08.2. Graphic language

Use these primitives consistently across the three canvas modes:

| Graphic | Meaning | Interaction |
| --- | --- | --- |
| Orbit lane with numeric ID and boundary | A selected subset of one real orbit; not a spatial shell | Select orbit explicitly; expand relevant members only |
| Thin outlined position socket | A fixed position, labelled with typed ID/home cell context | Inspect destination or explicitly assign Target/A/B |
| Solid occupant token inside a socket | Actual piece identity currently there | Inspect/track identity, set Current or pin Next |
| Small labelled A / B / T flags | Operation role assignments on fixed sockets | Assign via role picker or typed drag; does not move pieces |
| Frame notch and labelled sticker correspondence | Exact orientation/reference information | Inspect or explicitly choose a supported reference |
| Block bracket around selected members | Explicit block membership and declared relation status | Add/remove selected member through an explicit command |
| Directed macro arc | Certified source-to-destination effect at the displayed boundary | Select to inspect exact cycles, phase and affected slots |
| Thin adjacency edge | Real model incidence/face adjacency | Navigate topology; never interpreted as a legal setup |
| Dashed correspondence line | A selected reference mapping between two structures | Inspect mapping/certificate or assign an explicitly chosen correspondence |
| Shield with text | Explicit preservation policy, not current solved status | Open policy and conflict scope |
| Separate Next chip/outline | User-locked work identity | Locate, replace, clear or activate explicitly |

Geometry, macro-effect arcs and reference correspondence never share an unlabeled line style. Every ID-bearing object can locate its related Local/Global/puzzle object, open inspection, or insert a typed selection into a suitable field. Context menus and command index expose the same operations.

Show the active orbit, selected block, A/B/Target and relevant collateral by default. Other orbits become compact indexed rows with counts and conflicts. Expand a selected row only on demand. Do not draw the full puzzle as a dense all-to-all graph, lay 177,120 nodes on a canvas, or make users program by wiring nodes.

### S08.3. Block canvas

Show the active block as a small arrangement derived from selected topology: complete members, current open requirements and a few explicitly requested structural candidates. The arrangement is schematic and labelled as such. A side-by-side current/required relation makes mismatches visible without relying on color.

Selecting an occupant inspects it. `Make Current` adopts its identity and chosen destination as work intent. `Pin Next` records the future object. Selecting an open position can set a destination only through an explicit role action. A persistent distinction between inspect selection and work selection prevents browsing from destroying the solve context.

Adding a member edits the block definition; it never moves a piece or asserts completion. Each member exposes which predicate is met or missing. Progress shows exact unique-member counts and relevant active-orbit totals. A cell-based overview may show overlapping incidence counts, but never sums them as global progress.

### S08.4. Operation canvas and star operation

For a selected star, display three stable position sockets A, B and Target with actual occupants inside, exact current/required frame details adjacent, and one directed cycle around them. Select Before / Predicted After to see tokens change sockets while position labels remain fixed. Label the displayed phase: Body or Complete Operation.

Display the verified scope and collateral beneath the triangle. A warning side rail contains other-orbit effects, orientation-only changes and protected findings. “Verified star” requires its certificate; an orbit-local three-cycle receives its own label and can use the same three-position graphic with the narrower claim made explicit.

Dragging an occupant token into a role drop target proposes a **role assignment**. The drop zone says, for example, `Assign selected piece to Current` or `Use this position as Target`. Validate type/orbit/collisions before adoption. A mismatched drop leaves the old draft intact and explains the mismatch. Assigning roles does not retarget the selected raw macro unless the user separately selects and verifies an explicit reference transform.

Generic macros use compact cycle lanes with fixed points carrying orientation markers and an expandable full effect table. Do not force a five-cycle or orientation-only macro into a star triangle or remove it from the library.

### S08.5. Reference canvas

Compare two named cell groups: Current vs Target, A/B vs Target, or two user-selected structures. Display actual coarse 4D geometry in a stable projection, together with a compact incidence/correspondence table. A topology-only schematic must be explicitly named.

Select a cell or ordered frame on either side; use `Set reference` to apply the chosen supported mapping. Show what will change: role coordinates, compiled macro reference and relevant review state. It does not rotate the actual puzzle or move the main camera. `Locate in puzzle` is a separate explicit command.

The Local/Global comparison, reference assignment and macro result stay in one context. No sequence of modal ID-copy dialogs is acceptable for the common workflow.

### S08.6. Phase strip and work actions

The bottom strip makes chronology visible. Dragging a macro into a phase inserts a concrete versioned step; reordering changes the draft and invalidates review. A step shows raw source, expanded cost and selected frame. A compact input field accepts native supported macro syntax with step diagnostics. All insertion/reordering/editing has keyboard equivalents.

Provide separate actions for Inspect effect, Review complete operation, Use reviewed operation, Commit preview, Cancel preview and Undo last committed operation. Display the current input destination beside the active bank. Draft undo and committed undo are distinct commands and labels.

A short post-commit result updates the block, occupants, progress and Next location in place. Avoid a celebratory popup after every insertion. Only an actual whole-solve event uses the solve summary defined in S12.

### S08.7. UI quality and migration checklist

Draw compact, legible geometry and truthful relationships. Use one restrained accent plus semantic status encodings, modest borders and stable spacing. Do not use a generic AI dashboard, oversized metric cards, decorative gradients, glowing dense edges, invented confidence scores or chat as the primary interaction.

During implementation, construct actual UI states in S16 and test the transition between them. Review at the smallest supported workspace and high DPI before adding decorative detail. The user's current request is text-only; this contract requires future implementation-time UI inspection, not an image deliverable in this design task.

| Existing function | 0.4 destination |
| --- | --- |
| ID-based piece inspection | Typed command/selection adapter into Current/inspect context; retire only redundant standalone chrome |
| Highlight commands | Explicit annotations and reference overlays; preserve useful behavior and compatibility |
| Solve buffer/insertion/protection tabs | Integrated work context, role diagram, phase strip and persistent protection |
| Macro list/editor | Visual Macro Base plus contextual input and source pane |
| Local/Global windows | Operational linked views, optionally docked or owned floating windows |
| Filter panel | Composable work-set drawer with exact preview and saved views |
| Progress/stages | Orbit rail, block predicates and session progress; no disconnected success counters |
| Keyboard settings | Bank picker, full command editor and operational onscreen keyboard |
| Actual puzzle | Linked live/draft viewport with retained input/rendering contracts |

Remove redundant UI only after its retained capabilities have a working replacement and migration test. Do not delete useful compatibility behavior merely because its old label says Inspect or Highlight.

## S09. Keyboard banks and the limited Grip/Twist review gate

### S09.1. Keyboard convenience is a program-wide priority

All retained program operations need a keyboard route and freely editable bindings. This includes graphical hub selection, role assignment, Next, Macro Base, Solve macro text input, views, filters, protection, progress, work sheets, session management and window/index controls. The two-action restriction applies only to the **physical turn editor**: Grip and Twist. It does not restrict the global command editor to two commands.

Use one editor with a clearly separated Grip/Twist section and a full program-command section. UI commands, macros and work-sheet commands can coexist in an orbit bank without becoming a third physical turn primitive. Every command can be searched and invoked through the Index even if it has no direct default key.

### S09.2. Complete bank groups for every orbit

Provide five standard banks for **each of the 35 moving orbits**, for 175 independently addressable standard bank records. Share implementation and common command positions, but populate actual orbit-specific context and bindings rather than 175 empty copies.

| Suffix | Purpose | Orbit-specific contents |
| --- | --- | --- |
| `<orbit>-A` | Buffer A preparation | A-related explicit grip/frame captures, Twist controls, user-selected useful macro slots, role/reference and filter commands |
| `<orbit>-B` | Buffer B preparation | B-related captures and macro slots, A/B comparison, draft preparation and tracking |
| `<orbit>-I` | Insertion / block extension | Current/Target local captures, chosen insertion macro slots, block predicates, full review and result inspection |
| `<orbit>-M` | Macro composition and input | Orbit library/input/reference operations, chosen macro slots, Grip/Twist for recording explicit draft steps |
| `<orbit>-E` | Endgame / residual work | Retained buffer structure, orientation inspection, user-chosen residual macro slots and complete-operation review |

Example: `33-I` is orbit 33's insertion bank; `34-E` is orbit 34's endgame bank. IDs are stable when renamed/reordered. Human names and summaries are independently editable. Add a small number of shared Workspace, Views, Filter and Session banks through the same picker; they supplement the orbit banks.

For every orbit, derive and verify default A/B identities, hosting cells, affecting caps, legal grip frames, supported twists and available macro references from model/library data. An unsupported macro slot is explicitly empty with a useful command to assign an existing macro; never fabricate a useful macro, label an empty bank complete, or attach a target-solving lookup. Endgame banks require verified workflow coverage before acceptance, not just a name ending in E.

Default macro slots may point to explicitly identified retained library entries with visible roles and certificates; pressing the slot selects/inserts that fixed macro. It must not use the current target to secretly choose a different entry. A later user-created bank can copy, adapt and pin an entire useful workflow.

### S09.3. ID picker and switch behavior

Provide a small keyboard bank window/popover. Each row shows stable ID, orbit, short purpose, major grips/macros, required inputs and applicability/missing-condition summary. Opening the picker focuses its ID/search field. Typing `33-I` locates the bank; selection previews its description and mapping. Enter activates the selected bank. Clicking or an assigned direct shortcut also activates and displays a brief description; a pinned option keeps the explanation visible.

Support exact ID, human-name search, previous/next bank, previous bank, orbit-group navigation and favourite banks. Current ID, purpose and input destination remain on screen after the popover closes. The onscreen keyboard follows the active bank and can be pinned beside the Local view.

Switching banks is atomic for input: release all held logical keys/grips, increment the input epoch, cancel key capture/repeat, and require physical keys held across the switch to be released before they can acquire new meaning. No ghost turns, stuck latch or key-up interpreted in the next bank.

Switching to another orbit's bank explicitly restores that bank's declared orbit work context, as explained in the picker. Save the previous draft/context first. Do not mutate labels, overwrite a pending preview, select a new target/macro or discard Current/Next bookmarks. A global Next can remain associated with its original orbit; show that association. Restored reviews are stale. A bank change within the same unchanged work context does not invalidate exact mechanical analysis solely because the key arrangement changed.

### S09.4. Editable non-turn bindings

Create, edit, duplicate, rename, reorder, delete, import/export and reset banks with schema/model checks and recoverable preference changes. Built-in defaults can be restored without erasing personal banks. Detect conflicts by scope, modifiers, text context and chord timing. Save/cancel is atomic; a key-capture dialog never invokes captured actions.

A suggested common command vocabulary, freely editable and not awaiting Grip/Twist approval, is:

| Scope | Required command families |
| --- | --- |
| Hub | Navigate/select/inspect objects, switch Block/Operation/Reference, assign Current/Target/A/B/reference, locate typed ID |
| Continuity | Pin/replace/clear/locate/activate Next; save/restore orbit work context |
| Block/Solve | Create/edit block, inspect requirement, add/remove member, set work goal, review full operation, inspect actual result |
| Macro | Search facets, select, insert into each phase, record/stop, focus macro input, validate, inverse, explicit transform, save/load, edit version |
| Draft | Select phase/input destination, reorder/remove step, undo/redo draft, inspect effect, save/reuse work sheet |
| Protection | Inspect policy/conflicts, explicitly edit scopes and net/prefix policy; no accidental one-key silent unlock |
| Filters | Focus expression, preview/apply, Replace/Intersect/Union/Subtract, next/previous saved filter, capture live/frozen sets |
| Views | Local/Global/puzzle, dock/float/close, projection/camera actions, explicit locate, view reset, onscreen keyboard |
| Session | Preview/commit/cancel, committed undo/redo, checkpoint, New/Resume, scramble, reset, import/export, timer/summary |
| Keyboard | Open bank ID picker, activate by ID, previous/next, previous bank, open editor, capture/rebind grips explicitly |

Use a consistent modifier layer for these frequent non-turn commands, with direct defaults for bank picker, command Index, Next actions, macro input, filter and view switch. Resolve actual Windows/HSC/IME conflicts during reference audit. The action registry must make every remaining command assignable; it need not assign an obscure permanent chord to every rarely used command.

Focus precedence: key capture/modal > text/IME > focused control/editor > active workbank > global nonconflicting commands. Enter in Global selects a Global object; it cannot commit a main pending preview. Escape cancels the focused transient operation before any wider action. Text input and IME composition never emit twists. Alt-Tab, focus loss, busy transition and closing an owned window reset held input.

Default non-turn positions shared across the orbit banks are specified below. They remain freely editable; resolve a demonstrated platform or retained-command conflict through the editor/registry and document the final map, without a new approval gate. Function keys are used here to keep frequent commands outside the proposed letter/number Grip/Twist layer.

| Default input | Action and scope |
| --- | --- |
| F1 | Open searchable command Index |
| F2 | Open bank ID picker; type an ID and Enter to activate |
| Alt+1 / Alt+2 / Alt+3 / Alt+4 / Alt+5 | Switch the current orbit to A / B / I / M / E bank, with purpose feedback |
| F3 | Focus Macro Base search and facets |
| F4 | Focus the selected phase's macro text input |
| F5 | Open/focus Piece Filter and its current expression |
| F6 / Shift+F6 | Next/previous Block, Operation, Reference canvas |
| F7 / F8 / F9 | Toggle Local / Global / onscreen keyboard |
| F10 | Open saved work sheets and workspace actions |
| F11 | Toggle puzzle-focused layout while keeping context/protection accessible |
| F12 | Review the complete current draft; never commit directly |
| Ctrl+Shift+N | Pin selected identity as Next; if occupied, show explicit Replace action |
| Ctrl+Alt+N | Explicitly activate locked Next |
| Ctrl+Shift+L | Locate locked Next without activating it |
| Ctrl+Shift+Backspace | Clear locked Next in hub context; never intercept text deletion |
| Ctrl+Enter | Use the current review only when the review controls have focus |
| Ctrl+Shift+Enter | Commit the pending preview only when its execution controls have focus |

Within a focused Macro Base list, Up/Down selects a row and Enter inspects it; phase insertion uses a named command with a visible key hint. Within a canvas, Tab/Shift+Tab and arrows navigate typed objects, Enter inspects, and the action menu offers role assignment, Current, Next, filter and reference actions. Search results and role pickers must work without pointer input. Orbit-specific user macro slots occupy an editable modifier layer with visible stable-entry labels; a slot selects/inserts a named existing entry and never resolves a target-dependent solution.

### S09.5. Proposed Grip/Twist mechanics — USER REVIEW REQUIRED

This subsection is a concrete proposal, **not approved merely by being written into the architecture**. Implement it as the isolated **G1 Grip/Twist experiment**, then provide a runnable test entry, actual model-derived mapping tables, clear operation examples, build identity and known limitations for independent user review. Include Hold/Latch behavior, capture/rebind behavior, twist/frame resolution, onscreen keyboard, bank switching, focus/IME and 1/2/5/20-cell cases. A small real-model harness must let the user evaluate mechanics without having to approve G2's frontend. Do not activate this mapping in production or continue beyond the combined checkpoint until G1 and G2 both receive explicit approval.

Proposed physical mapping:

- Grip chooses one explicit cap/cell plus an ordered frame captured from the current role context. It does not move the puzzle.
- Twist chooses one legal A4 cap rotation in that grip; the command compiles to the exact primitive ID/sign or verified finite witness.
- Candidate Grip slots: `1 2 3 4 5 6 7 8 9 0 Q W E R T Y U I O P`, up to 20 simultaneously visible slots.
- Candidate Twist keys: `A/S/D` for H1/H2/H3, `F/G/H/J` for T1/T2/T3/T4; Shift requests the inverse. H turns are order two and self-inverse; T turns have order three and two directions.
- Hold Grip is the candidate default; Latch is an optional same-action behavior for hardware/ergonomic constraints. Lost key-up, rollover and accidental latch must be tested before choosing the approved default.

These are **seven axis choices, eleven nonidentity cap rotations**, not eleven unrelated axes. Resolve actual generator IDs/frame maps: an inverse label in a nested source table may not match the outer normalized axis name. Do not implement inverse by string substitution or diagram intuition.

Hosting cells are an ergonomic starting point, not the complete set of affecting caps. A two-cell piece can have 18 affecting caps. Allow explicit role captures, alternate named cap pages and direct C-ID grip assignment so all 1,200 generators remain reachable. Twenty slots are a display/mapping choice, never a model restriction.

Captures list exact old/new cap IDs, frame and keys before `Apply capture`. Camera/filter/piece motion does not silently remap them. If a bank requires a new Current/Target reference, mark the capture missing or outdated until the user explicitly applies a new mapping. Explain the distinction between stable mapping and applicability: an unchanged grip can remain mathematically valid while no longer affecting the focused piece.

Onscreen keyboard shows the real current bank, held grip, C ID, frame, twist axis/direction, draft/live destination and unavailable reasons. Clicking it uses the same input dispatcher and protection rules. It must allow complete operation for 20-cell pieces and for keyboards with limited rollover. Test simultaneous-key conflicts before finalizing the proposal.

## S10. Operational Local and Global views

### S10.1. Local: actual piece structure and keyboard meaning

Provide Current / Required structure side by side, using actual hosting cells, stickers/slots and ordered frame information. Highlight the selected correspondence with label and shape as well as color. A thin surrounding cap list distinguishes hosting cells from all affecting caps.

For 1/2/5-cell pieces, show complete structure directly. For 20-cell pieces, use a compact incidence arrangement with selectable cell faces and a synchronized full cell/slot list. All 20 cells must remain reachable without a maze of nested pages; do not fake a flat orientation angle for the A5 orientation group.

Operational actions: select a cell/frame; assign an explicit reference; capture/rebind the approved grip mapping; inspect a twist's exact meaning; append an explicitly chosen twist to the active draft; locate Current or Target in Global/puzzle; add the selected structure to a filter; pin Next. Keyboard and graphical paths use the same registry.

Show filter effects directly: admitted work objects, hidden objects and reference-only outlines. Explain why a visible object is not twistable. The keyboard overlay highlights the actual cap affected by the held grip, not whichever face is most visually prominent after a camera rotation.

### S10.2. Global: two real cell groups and their relationship

Show actual coarse 4D geometry for the two selected groups, plus a small relation pane for intersections, shared vertices/cells, face-adjacency and selected frame correspondences. Coordinate axes and projection mode are labelled. A topology distance has a named graph and origin; it is not a minimum number of puzzle moves.

Provide group presets Current/Target, A/Target, B/Target and A/B, plus explicit custom selection. Commands can select groups, inspect cells, navigate neighbours/layers, assign reference, create/modify filter and locate in the main puzzle. Hover only inspects. Explicit `Locate` moves the requested camera; changing a group does not unexpectedly recenter the main puzzle.

Keep separate Local/Global/main cameras, shared model/state and persistent group selections. Minimized/closed views stop drawing and release their owned resources appropriately. Reopening preserves meaningful selections but recomputes dynamic state; stale snapshots never remain as if current.

## S11. Piece Filter workbench

Maintain three independent scopes: full mechanical analysis universe, the user's work/interactivity filter, and noninteractive reference/annotation display. No display filter narrows protection or effect analysis. Annotation helpers must not overwrite a saved work filter.

Support Boolean composition of retained filters and explicit new facets: orbit, identity/position sets, piece structure, cells/vertices/layers, current block, Current/Next, macro support, progress and declared protection. Do not silently change semantics of existing expressions. Use typed builder controls with an equivalent readable expression, not a second incompatible query engine.

Saved set semantics are explicit:

- Live query: reevaluates its declared context; show whether it depends on current orbit/block/reference.
- Frozen identities: members follow those identities through moves.
- Frozen positions: members remain fixed physical positions and their occupants can change.

Preview gives exact unique-piece and slot counts, added/removed membership and context identity. Apply supports Replace, Intersect, Union and Subtract in visible order. Revalidate under lock; invalid expressions, cyclic saved-set references, stale context or exceeded limits leave previous preferences and mechanics intact. Capture supported saved-set sizes from existing code; do not silently truncate an oversized filter.

The hub exposes saved filter switching directly in banks and the Index. Display a compact active-expression summary, with a deliberate one-action return to the prior filter. Locating a hidden conflict can create a reference overlay; making it interactive is a separate explicit Union/Replace action.

## S12. HSC, HSC2 and MPUlt behavior alignment

Keep a separate audit matrix with actual executable identity/version/hash where available, pinned repository source, behavior tested, Magic 600 Cell equivalent, intended differences and evidence level. Recheck releases at implementation time; the pinned 2026-09-13 observations in S21 are not a claim about a later latest version or the user's installed executable.

| Area | Required Magic 600 Cell behavior | Reference/audit work |
| --- | --- | --- |
| Keyboard philosophy | Frequent operations reachable directly, editable configuration, immediate mapping and focus feedback | Compare HSC stable and HSC2 separately; test physical/semantic key behavior |
| New/default state | New session is solved, with deliberate documented default camera, projection and display state | Inspect actual entry/reset views, not only a solved boolean in source |
| Resume/reset | Resume preserves long solves; Reset puzzle, Reset view and Reset workspace have separate effects | Check menus, shortcuts, defaults and recovery expectations |
| Scramble | Familiar accessible short/custom/full controls, clear type/length and reproducibility record | Verify each reference's meaning of Full; do not claim uniform random-state sampling |
| Macro convenience | Record/input, name, reference selection, inverse, save/load and easy phase insertion | Audit MPUlt's actual workflow and source; validate our own exact transforms |
| Filters | Compose, preview, restore/switch and understand displayed pieces quickly | HSC/HSC2 grammar and actual feedback, with retained Magic 600 compatibility |
| Completion | Useful local timer/solve summary and explicit save/review actions | Compare actual HSC/MPUlt behavior; no unsolicited online services |
| Mouse/native interaction | Preserve verified useful 0.3 gestures and precise visibility/picking behavior | Compare reference affordances and document necessary large-model differences |

New does not overwrite a stored solve. Reset puzzle creates a recoverable checkpoint/journal transition. Reset view affects cameras/projection only; Reset workspace affects layout/work presentation without erasing history, personal banks or macros. Explicitly state any work-context resets before applying them.

Record scramble recipe/seed where supported, source, length and mode so replay is truthful. Do not auto-scramble on launch or auto-load a developer fixture. Timer accounting distinguishes active work, paused/resumed time and source metadata.

Show a whole-solve summary only for a real transition satisfying the named completion predicate in a solve session. Distinguish exact-label solved from face-color solved, practice/imported/replayed states from a scramble solve, and user operation from reset/import of solved data. Undo/redo or summary reopening must not create duplicate completion records. Do not infer a human unassisted solve from an agent replay.

Use source and live UI evidence separately. The prior design read reference repositories and source; it did **not** complete native HSC/HSC2/MPUlt interaction testing. A placeholder HSC2 macro/scrambler tab is not evidence that the whole application lacks that capability. Where live inspection remains unavailable, report precisely what is unverified and continue supported implementation work.

## S13. Persistence, compatibility, branding and experiments

Version and model-bind libraries, banks, templates, block definitions and workspace records. Store immutable macro revisions and dependency references. Keep derived certificates rebuildable; stale cache eviction never deletes user recipes/notes. Imports validate schema, model, size and references in isolation before committing changes.

Persist Current/Next identities, explicit roles, pending draft text, chosen macro versions, block/policy intentions, bank ID, filters and owned-window layout. Recompute current locations, frames, progress, applicability and review status on resume. Never restore an executable token or old Safe/Ready conclusion from a saved workspace.

Retain checkpoint/journal crash recovery and migration rollback. Preferences cannot partially save half a bank switch or protection edit. If integrating a small separate versioned library store, use the existing persistence pattern and an explicit transaction boundary; do not create another mechanical journal.

Rename user-facing titles, launchers, About/help and new documentation to **Magic 600 Cell**. Preserve compatibility readers for legacy file/protocol names; do not rename mathematical assets or silently break 0.3 imports. Public UI, diagnostics, source comments, test guide and changelog are English. Personal local handoff notes can remain Chinese.

Preserve Andrey Astrelin's primary attribution. Keep ivan216/Nan Ma/Nan Maack acknowledgments limited to substantiated contributions and existing appropriate credit; make no unsupported priority claim.

Experimental architectures, probes, screenshots, benchmarks, fixture generation and failed renderer candidates stay isolated under development work paths. Production includes only promoted dependencies and required runtime assets. Do not edit original/reference binaries or frozen 0.3 evidence. Packaging excludes personal databases/logs, usernames/private paths, chat records, instrumentation and nonredistributable Microsoft Managed DirectX DLLs.

## S14. Performance and the 1.0 frontend path

0.4 introduces the operational hub within the tested native shell unless a measured blocker requires a narrowly justified change. Do not delay it behind an unproven renderer replacement. Maintain the engine/command/snapshot boundary so a modern frontend can replace the shell for 1.0 without creating a second Session.

For 1.0, compare at most two justified frontend/rendering candidates initially using the full model, exact picking, command parity, owned-window lifecycle and the same fixtures. Select on measured usability, compatibility and performance. Do not predeclare a GPU vendor/API sufficient without evidence. A temporary legacy launch fallback opens an exclusive session; two frontends must not write one live solve concurrently.

| Target | Measurement boundary |
| --- | --- |
| Instant turn p95 <= 100 ms | Accepted input to correct submitted frame, including backend transaction/publication |
| Structure/auxiliary navigation p95 < 50 ms | User action to correct linked view, using cached immutable topology where appropriate |
| Adaptive 1080p >= 30 FPS | Disclosed hardware and detail policy; restore all intended visible content at idle |
| Full-detail 1080p >= 30 FPS | Separate measurement and hardware requirement, not inferred from adaptive performance |
| Closed/minimized windows | No continued unnecessary drawing or resource leak |
| Macro analysis | Bounded/cancellable work, responsive UI and exact results; no invented universal latency promise |

Measure warm/cold, resolution, DPI, visible slot count, mode, hardware and backend/render phases. For short critical interactions, use at least 100 samples where automation is reliable; use a few representative bounded long recipes rather than repeatedly expanding every certificate.

Run a stable 0.3 baseline and one candidate comparison, then retest only after relevant changes or newly discovered failures. Historical 0.3 measurements and synthetic tests are not a current native pass. Cache static topology/effects separately from contextual applicability, virtualize library lists, batch atomic view updates and cancel superseded analysis. Optimize observed bottlenecks; avoid speculative asynchronous complexity.

Track user-effort outcomes independently of word length: avoidable selection/search actions, repeated setting changes, copied IDs, view changes, manual checks, errors and recovery steps. A development target is at least 30% fewer avoidable interaction events on a matched representative repeat workflow compared with 0.3. This is a target requiring evidence, not an established reduction or a statement about full-solve probability.

## S15. Concrete inputs and complete workflow examples

### S15.1. E1 — verified two-step Home block, star selection and Next lock

**Evidence:** the following IDs and net effects were checked read-only against the retained full model. This is a legally generated synthetic demonstration, not a recorded human solve, implemented UI test or proof of the complete solving method. The recipes are existing core recipe syntax; work-sheet fields in other examples are proposed UI/domain inputs.

Orbit O33, fixed buffers A = 2712 / C7 and B = 175618 / C594. Let X = 35778 / Home C113 and Y = 26789 / Home C84. C113 and C84 are face-adjacent in the model. Define a Home block with exact requirements X-at-X and Y-at-Y; adjacency is useful structure, not a rigid-motion certificate.

Generate an isolated state from solved using this explicit chronological recipe:

```json
[
  {"kind":"star","orbit":33,"node":11,"sign":1},
  {"kind":"star","orbit":33,"node":0,"sign":1}
]
```

Initial occupants:

| Fixed position | Actual identity |
| --- | ---: |
| A = 2712 | X = 35778 |
| B = 175618 | Y = 26789 |
| X = 35778 | A = 2712 |
| Y = 26789 | B = 175618 |

Before starting the demo, explicitly import or create two Macro Base entries from these exact recipes. They are human-supplied known operations, not results generated from a target query:

```json
[{"kind":"star","orbit":33,"node":0,"sign":-1}]
```

Name this entry `O33 / node 0 inverse`, illustrative library ID `demo-o33-n0-inverse`. Its expansion has 56 primitives and net support of three pieces/three slots, only O33. Its position cycle is A -> X -> B -> A.

```json
[{"kind":"star","orbit":33,"node":11,"sign":-1}]
```

Name this entry `O33 / node 11 inverse`, illustrative library ID `demo-o33-n11-inverse`. Its expansion has 58 primitives and net support of three pieces/three slots, only O33. Its position cycle is A -> Y -> B -> A. Illustrative library IDs are created during fixture import; they are not pre-existing product records.

Complete interaction:

1. Select orbit 33 and create the two-member Home block. Explicitly protect other currently completed orbits under net preservation. Leave the work area of O33 available; do not infer protection from completion counts.
2. Select X as Current, target X, fixed A/B as above. Pin identity Y as Next. Next initially reads `at B / 175618`; it is not a lock on position B.
3. Open the bank picker and enter `33-I`. Read its insertion purpose and real mapping; activate it. The hub retains both Current and Next. Local compares C7 with C113; Global compares their actual cell groups.
4. In Macro Base, filter for orbit 33 and verified stars. Personally select the existing `demo-o33-n0-inverse` entry and insert it into Macro. Prepare and Cleanup are empty. Use the explicitly selected canonical reference; no setup or transform is generated.
5. The Operation canvas shows A containing X, B containing Y, and target X containing A. The macro arc reads A -> Target -> B -> A. Exact orientation on this orbit is trivial; show that fact.
6. Review the complete operation. Verify X's target predicate, full net support and all declared protection. Prefix status is separately calculated or explicitly unchecked; the known net result does not prove no temporary motion.
7. Request the preview and explicitly commit it. The actual new occupants are A contains Y, B contains A, X contains X, Y contains B. Next still identifies Y and now says `at A / 2712`. The block reports one of two exact requirements met.
8. Explicitly protect the achieved X requirement. Save a simple work sheet retaining role fields, phases, canonical reference and the selected macro version. The template's X-specific macro remains X-specific.
9. Explicitly activate Next. Current becomes Y with target Y; Next clears. The old saved macro is **not** silently retargeted to node 11. If reused unchanged, show its role/target mismatch and its conflict with protected X.
10. Personally select the already-existing `demo-o33-n11-inverse` entry and replace the Macro phase. Keep compatible phase/view/filter settings. Review again against X's protection; do not reuse step 6's review.
11. Preview and commit explicitly. Y reaches Y, X remains exact at the complete boundary, and all 259,800 labels are restored for this fixture. The two-member block is complete. Any whole-solve summary identifies the synthetic practice source truthfully.

This example demonstrates repeatable manual choice, exact occupant tracking, Next lock, block growth, macro mismatch and renewed protection. Empty Prepare/Cleanup in this example do not prove their interaction; E5 covers nonempty composition.

### S15.2. E2 — wrong direction and hidden protection conflict

From E1's initial state, select the positive node-0 star instead of its inverse. Its direction is A -> B -> X -> A, so X at A moves to B rather than its target. Display the actual predicted occupants and `Target predicate unmet`; do not replace it with the inverse.

Separately, set whole O33 protection to preserve its current labelled state, then hide O33 through the work filter. Review the inverse from E1. It changes three protected positions, so show a net conflict and block protected execution despite the hidden geometry. `Locate conflict` produces an identified reference overlay; it does not silently alter the work filter or policy.

### S15.3. E3 — exact orientation and hosting/affecting distinction

Inspect position 14056 in O32. Hosting cells are C43 and C59; its 18 affecting caps are C17, C21, C26, C43, C54, C56, C59, C64, C72, C84, C99, C104, C122, C129, C140, C145, C176 and C186. Its orientation group is C2. Compare position 6290 in O00: hosting and affecting cells are C19/C24, also C2.

Local shows the actual current ordered correspondence, not a guessed wrong orientation. The existing `orientation_wrong` progress measure counts position-correct but not exact-correct pieces; keep that definition. A displaced piece's comparison-frame mismatch is a separate diagnostic.

During implementation, generate reachable orientation fixtures by legal words and save their exact witnesses/hashes. Require a macro with an orientation-only effect somewhere and another whose piece permutation matches a candidate but exact slot action differs. Prove these facts before assigning expected results. Test classification, grouping, protection and block predicates against the full-label oracle. Do not manually swap labels into an impossible state or claim a specific corrective word has already been verified by this design.

### S15.4. E4 — 20-cell piece and frame choice

Select O34 position 17810. Its hosting cells are:

`C55, C62, C66, C93, C98, C109, C125, C131, C143, C151, C180, C193, C196, C201, C216, C234, C236, C247, C255, C298`.

Its orientation group is A5 of order 60. Switch to `34-A`, `34-I`, `34-M` and `34-E`, inspecting each bank's purpose, real cap/frame captures and explicit macro slots. Verify all twenty hosting cells can be selected and mapped, the full correspondence is readable and the exact selected reference is stable through camera/filter changes.

Pin this piece as Next while another orbit is active, perform a legal operation that moves it in an isolated fixture, and confirm its identity is tracked. Return via the bank picker; it must not become Current until explicitly activated. The design does not assert an unverified 20-cell insertion or orientation solution.

### S15.5. E5 — nonempty Prepare/Macro/Cleanup and template reuse

Use an explicit legal Prepare word supplied through approved Grip/Twist input, a personally selected concrete macro M, and an explicitly requested Cleanup equal to the inverse of that Prepare. Preserve the exact entered word and all phase boundaries. The UI input is a work sheet with these values:

```text
Goal: user-selected buffer preparation or block requirement
Roles: explicitly selected Current, destination, A and B
Reference: explicit verified frame ID
Prepare: the exact word the user entered
Macro: existing library ID + immutable recipe revision
Cleanup: inverse of this Prepare segment, explicitly selected
Policy: declared orbit/block scopes; Net or Strict prefix
```

This is a design input schema, not a claim that a new endpoint already exists. During development, select a short legal witness for the fixture, compute and store the exact expected full effect, and demonstrate entry through the real UI.

Save the work sheet. Reuse it with a different explicit object/reference: unchanged fixed bindings remain visible; missing matches remain missing. The software can compute the inverse of the new explicit Prepare, but cannot find the new Prepare, replace M or search for a solving reference. Compare body effect, complete net effect and prefix motion. Test a body which conflicts but is cancelled by the complete composition, and a body which is safe while external preparation is not; verify both with legal words.

### S15.6. E6 — explicit reference transform and grouping

Take an existing macro and a user-chosen supported reference mapping R. Show ordered source/reference correspondences before applying R. Compile the exact legal mapped recipe and verify its full effect; show whether the resulting entry is a full-action variant, an inverse or only locally related.

Repeat with an incomplete ordered reference, an ambiguous choice, and a frame from the wrong orbit/model. Preserve the original macro/draft and show the specific missing condition. Do not pick a transform by target success. Two same-net-action words remain separate entries with their cost/prefix differences visible.

### S15.7. E7 — endgame and final buffers

A final-buffer destination is a first-class work intent, not a nonbuffer insertion with its error suppressed. For every orbit's retained orientation/closure class, create legal reachable fixtures exercising buffer permutations, orientation residues and required cross-orbit collateral. Record representative class coverage plus orbit-specific boundary checks for all 35 orbits.

The user selects `orbit-E`, inspects actual A/B occupants and residual relations, selects an existing explicit macro or enters a word, reviews the complete operation under current block/orbit policy, and executes personally. If the selected library has no suitable operation, show what relation or witness is missing. Do not fabricate a macro, silently unprotect a completed orbit, or call the target solver.

Implement genuine endgame contracts and supply verified test macros/witnesses during development. A missing legal workflow for a required reachable residual is a product blocker. The current design has not verified all such cases; an implementation may be delivered as an explicitly incomplete test candidate, but cannot claim 0.4 acceptance until this gap is closed.

### S15.8. E8 — interruption, bank switching and repeated work

Load a saved work sheet and bank, pin Next, begin analysis, change Current, and switch banks while holding Grip. Cancel and reopen the view, then restart the isolated session. Expect saved inputs and bookmarks to survive, derived locations to update, held input to clear and all execution authority to be invalidated.

Repeat with an unrelated pending preview, a filter hiding Next, a macro revision edited after selection, a second orbit's saved draft, IME in the macro input, and Enter in Global. No operation is executed because of a focus transition, stale result or bank restoration. Record which repeated settings were actually avoided without removing the user's macro/reference choice.

## S16. Required implementation-time UI states

Build and inspect these actual states with valid fixtures. Text mockups or screenshots alone do not pass interaction acceptance.

| State | Visible content and transition |
| --- | --- |
| F01 Fresh New / Resume | Solved new state, deliberate defaults, retained saved sessions; no surprise scramble or progress overwrite |
| F02 E1 selected star | Stable A/B/Target sockets, actual occupants, direction, explicit reference, Current and Next |
| F03 Hidden conflict | Persistent protection warning, full scope and exact hidden IDs; separate reference overlay |
| F04 Missing/mismatched roles | Old draft preserved, missing field actionable; no auto-retarget |
| F05 20-cell comparison | Readable full structure/frame and real keyboard mappings at high DPI |
| F06 Keyboard ID picker | Many orbit banks, searchable IDs, purpose preview, direct switch feedback, held-key release |
| F07 Complete phase review | Nonempty phases, body/full distinction, net/prefix separation, clear execution state |
| F08 Post-commit continuity | Updated block/occupants, Next follows identity, explicit protection and activation |
| F09 Template reuse | Fixed macro version retained, only necessary fields change, fresh checks required |
| F10 Endgame | Buffer/orientation intent usable, exact limitations shown, no pretend completion |
| F11 Recovery | Preserved draft/bookmarks, invalid old token, correct reconnect frame and process ownership |
| F12 Compact/owned views | Drawers and floating windows retain core context; hidden views stop drawing |

## S17. Implementation sequence and efficient execution

Use the user's selected Astra Ultra configuration. A Prompt does not change the app's model/effort setting. Work autonomously up to the user's two independent experiment reviews: G1 in S09.5 and G2 in S08.0. Use no additional agents unless the user separately authorizes delegation.

### S17.0. Experimental development, two reviews, then continuation

Use isolated paths such as `work/experiments/magic600-04/grip-twist` and `work/experiments/magic600-04/abstract-hub`, or the checkout's established equivalent. Keep experiment-only dependencies, state and launchers out of production. Share small exact adapters where useful rather than maintaining duplicate puzzle engines. Work required to make either experiment genuinely reviewable is authorized; do not stop at another proposal or ask permission to build the experiments.

At the checkpoint, provide two independently testable entries/packages and two concise review guides. G1 covers physical mechanics and key resolution; G2 covers the concrete frontend and hub interaction. Include exact build/source identity, demonstrated cases, limitations and a decision record with separate `Pending / Changes requested / Approved` statuses. A text mockup, synthetic-only internal test or untested screenshot does not replace a usable experiment.

Once both are ready, submit the separate reviews and pause before P2. If one is ready earlier, it may be submitted while finishing the other experiment. A partial approval allows only its recorded decision; it does not authorize continuing the production pipeline while the other gate remains pending. User feedback authorizes relevant experimental revisions; resubmit those revisions. After both approvals, record their exact scope and continue P2–P6 without asking for general approval again. If later work materially changes an approved mechanism or frontend interaction contract, return only that changed scope for review.

Keep a short implementation handoff containing decisions, changed paths, commands/results, unresolved blockers and the next concrete step. Read the architecture once for orientation, then only relevant sections and code. Avoid rereading all historical design files or printing entire repositories. Capture one relevant baseline; repeat checks when a code change, failure or new concern justifies them. Do not equate token economy with skipping critical tests or delivering a partial workflow as complete.

Use focused milestones rather than one monolithic generation. Do not promise an exact token total or estimate it from account usage percentages. Record actual usage if exposed; otherwise say unavailable. Large speculative rewrites, unbounded alternative exploration and repeated whole-suite reruns are the main avoidable costs here. After two failed attempts at the same boundary, identify the failed assumption and change the experiment; do not repeat the same approach with cosmetic edits.

| Phase | Implement | Required exit evidence |
| --- | --- | --- |
| P0 Baseline and contracts | Inspect source/dirty changes; map retained features; pin reference audit; define independent exact oracle/fixture format and two experiment boundaries | Preserved baseline, requirement/action/removal inventory, executable test entry points |
| P1 G1/G2 experiments and reviews | Build operational isolated Grip/Twist harness and concrete abstract hub with exact supporting adapters; test and package each for separate review | Two runnable entries/guides and separate decisions; pause before P2 until both are Approved |
| P2 Approved foundations and hub integration | Promote measured approved components; integrate work/Next/block, exact macro/classification/protection services, transactions and full hub linkage | Full-label oracle, hidden/stale/pending guards, E1/E2 through actual UI, F02/F03/F04/F07; no experiment-only runtime dependencies |
| P3 Keyboard and continuity | Registry/editor; all 35 x 5 banks; ID popover; Next commands; approved Grip/Twist only after review; onscreen keyboard | Bank manifests and actual input tests, F05/F06/F08/F11, complete keyboard routes |
| P4 Reuse and full workflow | Templates, filters, relative-block support, macro conveniences, endgame, default/scramble/summary alignment; redundant UI consolidation | E3–E8 evidence, F01/F09/F10/F12, migration/removal inventory complete |
| P5 Adversarial review and improvement | Run S18 on the current build, measure effort/performance, implement observed corrections | Relevant fixes and reruns, no severe defects, truthful unverified items |
| P6 Test delivery | Isolated Windows candidate, version/hash, English guide/changelog/issues/rollback, final private-data/dependency check | S19 package available and launch verified; acceptance status backed by evidence |

Experiment dependencies can overlap through useful local work before the checkpoint. Do not cross into P2–P6 while either experiment review is pending; do not claim P3 complete with unapproved mechanics or P4 complete with endgame placeholders. Keep existing production controls and launchers unchanged during experiments. At the checkpoint, explain that the pause follows the user's latest explicit request for two independent reviews. Routine details within the approved scope do not need repeated permission.

### S17.1. Mandatory redundancy removal

The user explicitly requires removing redundant functionality. Create a small consolidation inventory during P0: old capability, new canonical location/command, unique behavior to preserve, obsolete UI/state/code to remove, and migration check. This is implementation work with acceptance criteria, not a suggestion for a later cleanup release.

Consolidate repeated highlight/selection controls into the typed annotation and focus system; independent ID inspection into the same inspect/Current/Next operations; disconnected buffer/insertion/protection panels into the hub; duplicate effect/progress displays into shared data views; duplicate filter/profile editors into the canonical workbench/editor. Remove redundant entry points, parallel state, handlers, dead code, obsolete help and tests tied only to the removed duplicate behavior after the replacement is verified.

Retain exact-ID lookup, useful annotations, accessibility and legacy import/command adapters where they still serve a distinct purpose. An API compatibility adapter is not a second user workflow. Do not leave old and new workbenches permanently side by side under an “advanced” toggle. Do not delete unrelated existing code simply because it is untidy. Each removal must trace to an actual replaced capability and retain a test for the resulting user behavior.

## S18. Acceptance and adversarial correction

### S18.1. Release status

Distinguish `development preview`, `test candidate`, and `0.4 accepted`. A build can be convenient to test while a required target remains unmet. Never label unrun checks Passed, source review as native UI verification, or agent/synthetic execution as a human trial.

The following are zero-tolerance failures: unauthorized/ghost/wrong-frame turns; duplicate commits; lost history; hidden protection omissions; stale executable permission; Current/Next identity substitution; unsafe pending replacement; text/focus input triggering a turn; automatic target solution generation; production continuation before both experiment approvals; or a claimed final-solve workflow with a known reachable endgame gap.

### S18.2. Required technical checks

Apply the current checkout's mandatory checks for each changed boundary. At the audited base these include:

| Boundary | Evidence |
| --- | --- |
| Mechanics/protection/persistence | `tests/test_core.py`, `tests/test_reference_maps.py`, `tests/test_crash.py`, plus independent complete-label comparison for new behavior |
| Preparation integration | `tests/test_preparation.py` and new shared-lock, full-phase, target/frame/policy/epoch/head/pending guard tests |
| Native input/control | x86 native host compilation, applicable `tests/native/NativeHostRegression.cs` fixture, then actual current-build Windows/DirectX input checks |
| Native update/picking | Exact mapping/masks, atomic delta/full fallback, stale/hidden hits, reconnect actual-frame readiness |
| Process lifecycle | Existing ownership/authentication/parent-pipe/start/stop/recovery tests in isolated processes |
| Macro/reference | Exact composition/inverse/equality scope, legal witness bounds, orientation-only effects, explicit/ambiguous/invalid reference mapping |
| Banks/Next | All 175 records and 35 orbit groups validated, stable references, input epochs/focus tests, bookmark identity/persistence/dependency tests |
| Filters/block/endgame | Live/frozen semantics, graph/layer overlap, invalid rollback, exact declared relations and reachable residual coverage |
| Packaging/migration | Unicode paths, clean isolated startup, version/hash/assets, old data read/migration/rollback, no private or forbidden files |

Avoid tests that merely reproduce implementation logic. Use retained mathematics and independently applied full witnesses for expected mechanics. Actual keyboard tests must exercise routing and focus, not only call the final action handler directly. No runtime benchmark is required for this architecture-only document edit; these are future implementation gates.

### S18.3. Adversarial user matrix

| ID | Attempt | Pass condition |
| --- | --- | --- |
| U01 | Browse other pieces while Current/Next are locked | Inspection changes; work identities do not silently change |
| U02 | Move, undo, redo and resume a pinned Next | Identity tracked correctly; intended destination preserved; no auto-promotion |
| U03 | Hide required/Next/protected objects | Analysis complete; meaningful locate/reference action; no click-through |
| U04 | Change target/frame/policy during analysis | Late result cannot become Ready for the new intent |
| U05 | Undo to identical labels, restart or reconnect | Old review/token remains invalid |
| U06 | Keep an unrelated pending preview and submit a failing review | Pending preview and draft remain intact |
| U07 | Hold keys through bank switch, IME, modal, Alt-Tab and window close | Input reset; physical release required; zero ghost twists |
| U08 | Type in Macro input or press Enter in Global | Only focused action occurs; no main puzzle commit |
| U09 | Switch orbit banks repeatedly with two unfinished drafts | Correct context restored, bank purpose shown, drafts/Next preserved |
| U10 | Edit, import, delete or reorder banks/macros | Stable references, conflicts visible, recoverable preference changes |
| U11 | Move camera/filter/piece after grip capture | Mapping remains explicit and stable; suitability warnings truthful |
| U12 | Use 2-cell/18-cap and 20-cell cases | All actual structures/caps reachable, no fake orientation model |
| U13 | Enter nonstar or orientation-only macro | Exact classification and usable generic effect view |
| U14 | Compare inverse/local-equal/full-equal variants | Verified scope/differences retained; no unjustified substitution |
| U15 | Body safe / whole unsafe, or body unsafe / whole safe | Complete-operation policy decides; body alone does not gate selection |
| U16 | Net-safe but intermediate-moving operation | Net/prefix facts separate; strict policy rejects unchecked prefixes |
| U17 | Reuse a template with a new target/reference | Fixed method visible, fresh review, no automatic setup/retarget/macro choice |
| U18 | Next becomes already satisfied | Status shown, bookmark unchanged until explicit action |
| U19 | Change unrelated Next metadata after reviewing Current | No needless mechanical recheck; actual dependencies still invalidate |
| U20 | Extend a relative block away from Home | Exact selected relation checked; no false rigid-motion claim |
| U21 | Try every orbit's final-buffer/residual class | Real manual workflow or explicit blocker; no fake completion/unlock |
| U22 | Apply invalid/stale/cyclic filter or oversized import | Atomic rollback, input retained for correction |
| U23 | Cancel long analysis, disconnect engine, lose commit response | No duplicate execution; authoritative recovery and preserved work |
| U24 | Minimize/detach/reopen/move screens at high DPI | Stable context, visible restored windows, no hidden rendering leak |
| U25 | New/Resume/reset variants and solved import/undo/redo | Correct state ownership and truthful nonduplicated solve summary |
| U26 | Use only keyboard for the normal repeat workflow | No mandatory mouse-only role, reference, macro, filter or Next action |
| U27 | Find an old duplicate inspector/highlighter/workbench entry | Canonical replacement accessible; removed duplicates do not remain as parallel workflows |
| U28 | Hold rapid input while preview/commit is busy | Bounded explicit accept/reject, correct state and no delayed surprise moves |
| U29 | Approve G1 while G2 remains pending, or request changes to one experiment | Decisions remain separate; production continuation stays paused until both are approved |

Run at least 30 continuous meaningful work cycles spanning 1/2/5/20-cell structures, with at least 10 genuine template reuses and real bank switching. This is a total mixed run, not 30 artificially identical cycles for each possible combination. Separately validate all 35 orbit groups and required residual classes; do not replace coverage with one attractive O33 example.

Record build identity, fixture/source/witness, exact input sequence, expected/actual outcome and evidence location. Classify human, agent-driven and synthetic runs. Run a matched baseline/candidate repeated workflow and report measured avoidable effort, errors and recovery, without manufacturing completion probability.

After testing, review as a demanding long-session solver: what still requires copying IDs, remembering hidden state, reopening panels, checking the same fact or resetting the same reference? Record Trigger / Impact / Change / Evidence for each real finding, implement the useful corrections, and rerun affected cases and mandatory regressions. Do not stop after listing suggestions. If no defect is found, report actual scope rather than inventing fixes.

## S19. Test build and publication path

Deliver a clearly named isolated Windows package such as `Magic600Cell-0.4-test-<build>.zip`, with one obvious launcher and an isolated test-session entry. A separate developer fixture kit may accompany it; do not ship instrumentation or private sessions inside the public runtime.

Include English `TESTING_GUIDE.md`, `CHANGELOG.md`, `KNOWN_ISSUES.md`, manifest/SHA256, dependencies, source/build identity, actual validation record, compatibility and rollback instructions. Explain expected defaults, session location without private user paths, how to load legal example fixtures, bank IDs, Current/Next, macro selection, role/reference meaning, complete review, protection, draft/live input, recovery and endgame cases.

The short test route is: fresh isolated launch -> E1 block and Next -> bank switch and keyboard-only actions -> E2 hidden conflict -> E3/E4 structure and orientation -> E5/E6 manual work-sheet/reference reuse -> E7 final buffers -> E8 interrupt/resume -> compare actual results with the guide. The guide uses the exact current build and actual supplied witnesses, not aspirational controls.

Describe improvements over 0.3 concretely: the new hub, repeatable orbit/block work, stable Current/Next, complete per-orbit keyboard groups, exact macro classification and contextual protection, stronger filters/reference work, reduced redundant controls and measured effort/performance. Separate delivered 0.4 behavior from planned 1.0 frontend work and unresolved limitations.

Local test packaging is authorized by the development request. A new public release is a separate publication action; do all preparatory work before requesting any approval actually needed for that action. Do not reuse historical 0.3 publication authorization for an unrequested new public release.

## S20. Requirements traceability

| User requirement | Contract / verification |
| --- | --- |
| Original full framework and previous hard requirements | S01, S12–S14, S18–S19; source audit in S21 |
| Orbit First Block Building Solving | S02, S03, S15 E1/E7, U20/U21 |
| Piece-focused mode and locked Next | S02.3, S04.2, S08, U01/U02/U18/U19 |
| Abstract view as main graphical hub | S08, S10, S16, P2/P5 |
| Smart multi-category Macro Base with real proof | S05, S06, E2/E3/E6, U13–U16 |
| Star A/B/Target, actual occupants and orientation | S05.1, S08.4, E1/E2 |
| Complete protection including hidden/prefix effects | S04, S06, U03–U06/U15/U16 |
| Human macro/ref choice; no hidden setup solver | S02.2, S05.2–S05.4, S07, E5/E6, U17 |
| Many banks for every orbit, ID/purpose popover | S09.2–S09.4, U07/U09/U10/U26 |
| Grip/Twist and concrete abstract frontend built experimentally, then independently reviewed | S00, S08.0, S09.5, S17.0, P1, U29 |
| All operations keyboard-accessible/editable | S03.1, S09.1–S09.4, S10–S11, U26 |
| Local/Global/puzzle/filter/progress linkage | S03–S04, S08, S10–S11, S16 |
| Reusable manual work sheets | S07, E1/E5/E8, U17 |
| HSC/HSC2/MPUlt defaults, macros, scramble, summary | S12, S21, F01, U25 |
| Remove redundant features | S08.7, S17.1, P4, U27 |
| Sustained frontend optimization, no AI template UI | S08.7, S14, S16, S18.3 |
| Experimental isolation and truthful performance | S13–S14, S18–S19 |
| Adversarial review, actual adjustment and retest | P5, S18.3 |
| Convenient test version and guide; publishable 1.0 | S14, S19 |
| Reasonable tokens and reduced trial-and-error | S17; scoped evidence, explicit dependencies and bounded experiments |

## S21. Sources and evidence limits

### S21.1. Pinned upstream observations

The reference audit was performed on 2026-09-13. HSC stable v1.0.10 was listed as published 2025-01-05; HSC2 prerelease v2.0.0-zeta.12 as 2026-03-10. Source snapshots and local executable identity are different evidence.

- [HSC v1 key bindings](https://github.com/HactarCE/Hyperspeedcube/blob/3ad693af295c5a0d4a21635e829ef8300b0bbcd0/src/preferences/keybinds.rs) and [piece filters](https://github.com/HactarCE/Hyperspeedcube/blob/3ad693af295c5a0d4a21635e829ef8300b0bbcd0/src/gui/windows/piece_filters.rs).
- [HSC2 physical/semantic key representation](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/crates/hyperprefs/src/keybinds.rs) and [filter expressions](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/crates/hyperprefs/src/filters/expr.rs).
- [HSC2 macro tab](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/crates/hyperspeedcube/src/gui/tabs/macros.rs), [scrambler tab](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/crates/hyperspeedcube/src/gui/tabs/scrambler.rs), and [solve summary](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/crates/hyperspeedcube/src/gui/modals/solve_summary.rs). The two tab placeholders do not prove global feature absence.
- [MPUlt macro storage](https://github.com/cutelyaware/MPUlt/blob/0e2b86cbb713b8286083fa964ea67d6582337d5a/src/CMacroFile.cs), [reference mapping source](https://github.com/cutelyaware/MPUlt/blob/0e2b86cbb713b8286083fa964ea67d6582337d5a/src/PuzzleStructure.cs) and [main UI logic](https://github.com/cutelyaware/MPUlt/blob/0e2b86cbb713b8286083fa964ea67d6582337d5a/src/rubikHT.cs).

The upstream sources support the audited observations; the new hub, bank schema and block workflow are this project's design. Native reference-app interaction checks, local binary equivalence, 0.4 performance and complete human solving have not been established by these reads.

### S21.2. Model examples and development guidance

Read-only model calculations confirmed the piece/slot counts, hosting histogram, listed O00/O32/O34 structures and E1's two explicit inverse-star net effects. The hosting histogram is 107,400 one-cell, 66,000 two-cell, 3,600 five-cell and 120 twenty-cell pieces. Development-only evidence is recorded in the project's `work/reference-04` area and shared memory. This is not evidence for future UI, prefix protection, all endgames or performance.

The [official Astra model guidance](https://developers.openai.com/api/docs/guides/latest-model) recommends explicit autonomy and appropriately scoped verification. S17 applies those ideas as concrete project workflow rules. It does not promise a token cost, turn a Prompt into a model-setting command, or equate a Codex UI option with an API parameter.
