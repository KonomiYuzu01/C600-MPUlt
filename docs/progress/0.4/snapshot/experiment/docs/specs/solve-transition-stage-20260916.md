> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Atomic solving transitions

Date: 2026-09-16. Status: bounded design for independent review; not implemented or verified. Owner of this document: geometry_frames. No Session, adapter or native source is changed by this document.

## Purpose and authority

Complete the already authorized transition from one explicitly executed insertion to the next working object without separating its mechanical result from the resulting protection and workspace preferences. Reuse one Model, PuzzleState, Session, journal, SQLite transaction and shared lock.

Authority: the current architecture S00 amendment of 2026-09-16, S01/S04/S06/S07/S13, current PROJECT_MEMORY.md, and the coordinator's clarified acceptance contract. G1/G2 are already user-approved for their identified earlier native samples; this new behavior still requires internal verification. It is not a new approval of unfinished features.

Confirmed behavior:

- Orbit protection is added only on an actual newly committed solving operation's transition from not exactly solved to exactly solved. New/reset do not blanket-lock the solved model. Manual unprotection does not immediately relock unchanged state.
- Current advances only after an actual successful Insert: its exact target requirement changes from unsatisfied to satisfied. A previously satisfied target or a cancelling identity recipe is not an insertion success.
- Locked Next has priority, including another orbit. Activate it even if it is already satisfied, showing Satisfied rather than silently skipping it. Activation consumes the bookmark as the existing explicit Next activation does.
- Otherwise offer/activate a switchable, structurally justified block-building candidate within the source orbit. No candidate means retain the present Current and explain exhaustion; do not choose another orbit.
- Preserve the source orbit's draft, component receipts, reference, block and settings. Keep the selected keyboard bank unchanged and visible. No following operation is executed, generated, retargeted or implicitly reviewed.
- Preserve existing undo/redo semantics: mechanical state changes, workspace/preferences remain. Do not turn undo/redo into workspace-history replay. This is an existing compatibility constraint, not a new question for the user.

The physical live-turn path temporarily uses a draft for validation. Its persistent goal can still say Insert while the user is preparing. The smallest safe trigger for automatic Current advancement is therefore an explicitly reviewed complete work-sheet commit with declared Insert goal and the exact false-to-true target transition. Ordinary live Grip/native-word turns may auto-protect an orbit they finish, but must not advance Current merely by borrowing that persistent goal. A later explicit live Insert action would need an equally explicit purpose contract; none is introduced here.

## Observed implementation and failure cases

Inspected source: root `session.py`; experiment `adapter.py`, particularly `save`, `review_draft`, `commit_with_result_warning`, `restore_completed_operation`, `switch_orbit`, and command branches for commit/live turns/Next/undo/redo/reset. This is source evidence, not an executed crash or native test.

| Observed boundary | Consequence for this change |
| --- | --- |
| `Session.commit` atomically writes event, head, current preferences and periodic snapshot, then publishes state. It currently cannot receive post-operation preferences. | Calling commit followed by separate protection/workspace saves leaves a crash window with committed labels but missing automatic transitions. |
| `switch_orbit` mutates workspace and separately calls `save_prefs({'orbit': o})`. | Calling it after commit cannot make the source context, destination context and Session orbit atomic with the move. |
| Live `twist`/`native-word` temporarily substitutes `w.draft`, restoring it in `finally`. | Capturing preferences inside that temporary span can persist a one-turn replacement while retaining unrelated original component receipts. Capture the real workspace before substitution. |
| `restore_completed_operation` marks every context whose normalized draft matches the current event recipe. | Identical recipes in two orbits do not prove which context executed. Recipe equality also cannot distinguish an explicitly reused draft after restart. |
| `operation-reuse` clears an in-memory completed marker but does not persist a new operation generation. | Restart at the same head can reclassify an intentionally reused draft as executed. |
| `save_prefs` and commit can report a status/read failure after the SQL write succeeded. | An exception alone cannot justify retry, rollback claims or a second automatic advancement. |
| Source context fields currently include target requirements and component receipts, but phase/input/prefix/filter and bank are global. | Preserve their existing scope; do not silently introduce per-orbit copies of every setting. Enumerate retained versus switched fields. |
| No block-candidate service exists in the adapter. Native button-neighbor navigation is screen navigation. | Do not use UI ordering, execution trees or a target-solving endpoint as mathematical block adjacency. |

## Chosen transaction boundary

Reject the simple sequence `commit -> protect -> switch_orbit -> save`: it cannot recover atomically. Also reject a second journal, durable job queue or domain callback running arbitrary adapter code inside Session. The required extension is a data-only, optional argument to the existing Session transaction.

Proposed shape, finalized by the implementation owner before editing:

`Session.commit(token, *, preference_changes=None, event_context=None)`

Existing callers with only a token retain their behavior. Neither optional argument is accepted directly from an HTTP client. The Workbench constructs a detached plan under the existing Session lock after validating its current execution guard.

1. Capture the authoritative pre-state, original workspace, active source orbit, operation generation, explicit goal, target requirement, locked Next and enabled protection. For a live turn this precedes temporary draft substitution.
2. Obtain the exact post-state already associated with the pending preview. Do not approximate through affected piece counts, sticker colors, truncated cycle rows or a second permutation implementation.
3. Build a detached candidate workspace/preferences and small transition context. All automatic decisions use this exact before/after pair. Build the source context before activating a cross-orbit Next.
4. Recheck the existing preview token/head/revision/full hash and execution-context guard. The original enabled protection remains the policy used to admit the operation. A newly solved orbit's new lock must not reject the operation which just solved it.
5. Validate the candidate preferences completely before any write. Extract only the existing preference validation into a reusable no-write helper; preserve allowed fields, canonical identity checks, filter validation and the existing 500,000-character bound. Validate state-dependent expressions against the correct predicted state. No `status()` call or nested write belongs in validation.
6. In the existing SQLite transaction insert the event, write head, write the candidate preferences, and write the optional small event context. Every periodic snapshot created by this transaction uses those same candidate preferences. A failure before SQL commit leaves all of them unchanged.
7. Only after durable commit publish the new head, labels, preferences and workspace together, clear pending execution authority, increment the existing state revision and invalidate prior reviews. Presentation adopts this single committed result.

Use a narrowly named entry in the existing `meta` table, keyed by the inserted event ID, for optional event context. This needs no second table or alteration of old event rows. Its bounded schema records source orbit, source operation generation and the resulting automatic transition decisions; the existing event already supplies recipe, pre/post hashes and assistance. Do not duplicate full label arrays, recipes, reviews or safety certificates in the receipt. Old readers may ignore this additive metadata; new readers treat absent metadata as legacy rather than infer a new transition.

The event-context receipt is evidence of what committed, never preview/execute permission. A snapshot restore loads its saved preferences as today; it does not execute the receipt. Recovery loads final durable preferences rather than running candidate selection or auto-protection again.

The existing `switch_orbit` needs a bounded detached-workspace helper which performs its field copying without persistence. Use this helper for the post-commit plan; ordinary manual switching may then reuse it and save once. Preserve field scopes:

- Per-orbit: Current, target, exact target requirement, A/B roles, draft, component receipts, reference, block, goal and the new operation generation.
- Global and retained: bank/previous bank, input destination/phase, prefix mode, filter settings, keybinds/captures, tool-window layout, macro library and worksheets.
- Next is one explicit global bookmark, consumed only by successful activation. Inspected identity may follow the new Current; hover is not a candidate source.

Copy preferences from the latest lock-protected authoritative value so an earlier completed background layout save is not overwritten. There is no queued operation and no retry of a mechanical command when a layout save or response fails.

## Exact transition rules

### Orbit completion

For each moving orbit `o`, exact solved means every slot whose model orbit is `o` contains its original canonical label. Using `PuzzleState.correct` over every position in that orbit is equivalent only because that property is derived from all its labels; retain a full-label oracle test.

Add `o` only if `solved_before(o) == false` and `solved_after(o) == true` on a new explicitly committed solving operation. This can include collateral completion outside the active orbit. Preserve all existing enabled locks and the chosen net/strict-prefix mode. Hidden/filter-excluded pieces participate normally.

Do not run this transition from a snapshot getter, filter change, review, cancelled preview, library analysis, undo, redo, reset, New, import or checkpoint restore. Consequently a manual unlock of an already solved orbit stays unlocked through display changes and a no-op commit. If a later allowed operation actually unsolves and subsequently solves it, that is a new completion transition.

Automatic protection does not convert per-position captured protection into an orbit lock, nor delete either type. It adds only the completed orbit entry. The original complete Prepare/Macro/Cleanup net and strict-prefix checks still govern admission against pre-existing locks.

### Insertion completion and Next

Re-use the current review's exact target definition as a pure predicate evaluated independently on both states:

- Home requires the correct physical identity at its own Home position and the canonical ordered labels at every slot.
- A non-Home destination requires the matching explicit captured identity/position/ordered-label requirement. Missing or mismatched capture is not success.

Only an explicitly committed complete Insert with `false -> true` advances. `goal_met` alone is insufficient. Preparation, block and endgame goals do not masquerade as insertion.

When Next exists, validate its canonical identity and destination as today, preserve the source context, restore/create its orbit context, then set Current/target to that exact bookmark. Keep the bank and existing valid settings. A target requirement from a different object must not be reused: retain only a matching requirement already associated with the chosen identity/destination, or show missing frame. Do not generate a new reference or destination orientation. Evaluate Satisfied against the exact available requirement; missing orientation is Unknown, even if the occupant is correct.

Clear any preview/review authority for the previous target. On adoption, reset held input and require physical releases before a key can act on the new work context, even though the bank ID has not changed. Show the activated object and reason next to Current. Do not switch keyboard banks to make the new orbit appear convenient.

### No-Next candidates: bounded dependency

This requires its own small read-only selector before the transition stage can be called complete. It consumes the post-state and explicit block requirements and returns canonical identity/destination/requirement records with concrete structural reasons; it cannot call the execution tree, choose a macro or compute a setup.

The minimal deterministic proposal is:

1. Unsatisfied explicit members of the active block, in their user-defined order, whose identity and destination lie in the active orbit. Their exact saved label requirements remain authoritative.
2. For a Home block with no such pending member, unsatisfied Home positions in the active orbit which share an actual hosting cell with a satisfied block member. Use retained `Model.hosting` / `cell_positions` incidence, not affecting caps. Describe them as sharing a cell, not as face-neighbors or rigid blocks. Keep the qualifying block member/cell in the explanation. A stable canonical tie-break is permissible for display order; it does not redefine identity or imply solving quality.
3. For a non-Home/transported block, do not invent new destination frames from proximity. Only existing explicit requirements qualify until a separately verified reference transport can provide more. If no justified candidate exists, retain Current and show the reason.

The candidate list must remain switchable without executing anything, be based on the exact committed state, and exclude an already satisfied target from automatic no-Next selection. Empty block has no structural seed and therefore no automatic replacement. Retained buffer positions are not silently excluded from the mathematical list; mark their actual role so endgame work is not hidden. Do not claim the shared-cell ordering is an optimal block-building strategy or satisfies future full endgame support.

This candidate ordering is an implementation proposal for independent review, not an observed existing feature. If stronger block connectivity is required, establish its exact model relation before substituting it. Do not block independent transaction work on rendering this list.

## Operation ownership, recovery and compatibility

Persist one opaque operation generation in each active/per-orbit context. Explicit New, Reuse and loading a worksheet start a new generation; draft changes continue to invalidate the existing review through its actual content/guard. Generation is work identity, never model piece identity. Ensure all paths which intentionally start a new operation use the same narrow helper.

Bind new completed-operation receipts to the source orbit and generation as well as the existing event. After a cross-orbit activation, mark only that preserved source context executed. Do not mark the restored destination draft executed just because its recipe is identical. A fresh reused generation remains a draft after restart even at the same head and with the same recipe.

For legacy preferences/events, supply optional defaults without bulk rewriting archives or personal data. Legacy recipe-based display inference must never trigger automatic effects. Prefer an unknown/unattributed completion display over asserting execution ownership for several matching contexts. Do not rewrite old model IDs, macros, labels, snapshots or keybindings.

On a post-commit status/HTTP failure, reconcile the exact newly inserted event and its stored preferences/context under the existing lock. Report committed only when parent/pre/post/recipe/assistance and expected transition data actually agree. Never send commit a second time automatically. If the process died after SQL commit, reopening recovers the already final labels/preferences and does not consume another Next. If evidence is unavailable, stop execution with a precise uncertainty; an exception is not proof of rollback.

Undo/redo continue to change mechanical state only. The current focus and protection preferences remain; if undo makes a protected orbit unsolved, report actual progress separately from the still-enabled protection policy. Do not pretend its lock proves completion, silently unlock it or run a completion transition again on redo. A new explicit user commit after undo is a new event, subject to the normal rules.

## Implementation order and rejection tests

1. Independent review of this design and candidate rule. Assign one owner to Session transaction changes and one to the experiment adapter, serialized where they share a patch. Leave immutable assets and native rendering untouched.
2. Add the minimal no-write preference validator and optional atomic commit data. First prove old token-only calls unchanged and transaction rollback/recovery behavior on isolated databases.
3. Add detached workspace switching, operation-generation ownership and exact transition planning. Cover live temporary drafts before wiring live protection.
4. Wire native adoption/held-key clearing and concise transition/candidate feedback through the existing registry. Do not add automatic queuing or implicit bank selection.
5. Run focused isolated full-model and crash tests, then mandatory core/reference/crash checks. Root alone executes fresh identified native workflows. Measure added commit/adoption latency on that artifact rather than predict it from source.

Required falsifiable checks:

| Check | Rejection condition |
| --- | --- |
| Before-write validation/cancel | Any labels, head, preferences, original draft or unrelated preview change. |
| SQL failure after event insert, before prefs/context completion | A partial event/head/protection/focus survives rollback. |
| Hard exit after SQL commit, before response | Reopen lacks a lock/focus change, advances twice or loses the preserved source context. |
| Status/read failure after durable commit | A success is retried or reported rolled back; committed workspace differs from stored preferences. |
| Periodic snapshot at the 50-event boundary | Snapshot contains old prefs with new labels. |
| Live turn with nonempty saved draft/component receipts | Temporary one-turn draft or mismatched provenance is persisted. |
| Identity recipe with target already satisfied | Current changes despite no exact unsatisfied-to-satisfied insertion. |
| Wrong orientation at correct position | Insertion/whole-orbit completion is reported without exact labels. |
| Cross-orbit locked Next, including satisfied Next | Wrong identity activates, bookmark is skipped, source work is lost or bank changes. |
| No Next, empty/transported block, several candidates | A candidate lacks its stated structural/exact requirement evidence, or another orbit is chosen. |
| Two contexts contain the same normalized recipe | Both contexts acquire the one event's completion ownership. |
| Explicit Reuse, same recipe/head, restart | Reused draft is displayed executed because of the older event. |
| New/reset/import/restore/undo/redo | Transition rules run or existing mechanical-only history semantics change. |
| Manual unlock, unchanged solved state | Orbit immediately relocks on display/review/no-op commit. |
| Hidden collateral and strict-prefix witness | Filtered scope is skipped, temporary motion is called final damage, or the pre-commit policy is weakened. |
| Background layout persistence before/after commit | Latest saved bounds vanish or independent preferences are overwritten. |
| Legacy saved session/macros/keybinds | Canonical objects or recipes resolve differently, or missing new fields prevent opening. |
| Native held key while Current moves; Stop or response delay | The held key executes against the new target without release, or an operation queues silently. |

No tests in this table have been run as part of this design-only task. Existing green builds predate the proposed transaction extension and do not verify it. A broader journal/undo semantic change, second authority, immutable-model migration or unproved block/reference relation is outside this plan and must return to the coordinator/user as appropriate.
