> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Final solver acceptance — bounded backend review

This review reuses the mathematical evidence indexed in
`endgame-integration-20260917.md`. It does not repeat the 1,200-generator proof,
35-orbit catalogue, or 35 selected paths / 59 stages. It is an agent review and
chosen legal fixture exercise, not a human full solve or arbitrary-state proof.

## Highest-impact remaining seam

The previous tests separately covered changed goals, fixed worksheet reuse,
orientation-only operations, and recovery. They did not combine a staged
OrientPiece preview with reuse of the same fixed recipe after its explicit target
orientation changed while the mechanical state stayed identical.

`tests/test_final_solver_acceptance.py` adds that seam. A real C2 A-transfer first
leaves the piece at Home with wrong orientation. The user explicitly chooses Home,
reviews and stages the inverse, then captures the present wrong orientation and
loads the saved worksheet. Loading preserves the current target requirement. The
old commit and preview are rejected without changing labels, head, pending data,
preferences or work. Fresh review correctly reports **Ready + Unmet**: protection
permission and the selected goal are independent. Explicitly choosing Home again
produces a new context, meets the goal and executes successfully.

A second check restores a checkpoint with identical labels. This still withdraws
the old worksheet confirmation and staged authority; fresh inspection/review is
required. No product defect was found in these two combined boundaries.

Initial run `evidence/endgame-integration-/20260917-011254/` had a test fingerprint
error: Session.pending contains NumPy arrays. The test now hashes those arrays;
no product behavior or assertion was weakened. The final three-check run is
`evidence/endgame-integration-/20260917-011954/result.json` (33.194 seconds, exit 0,
source/model inputs unchanged). The scene test also checks unchanged manifest
bytes. Test SHA256: `20a9cb68d51d08cd117fe3e65b8c0627f55228bdb214c959905991981bdf5b3f`.
Manifest SHA256: `734a23975957d33b7fb3b22cd11280fc33cf519697cb2afa30b3b78f6e195d3c`.

The separately authorized final core/reference/crash run is
`evidence/core-contracts-/20260917-011803/result.json`: all three commands exit 0
(71.510 / 20.020 / 7.678 seconds), all bound inputs unchanged. Existing generated
core reports were restored byte-for-byte after their fresh logs were recorded.
This establishes those backend checks; no native equivalence or hardware power
loss is inferred.

## Native recurring-method manifest

`tests/final_workflow_cases.json` supplies 16 explicit nonidentity methods and 32
interleaved work cycles, eight each for 1, 2, 5 and 20 stickers. The first occurrences
save four methods per size; the second occurrences genuinely reuse those 16 fixed
sheets after other orbit contexts have been used. This is recurring-method stress,
not 32 independently invented algorithms or repeated identity actions.

- One-sticker methods: retained placements in O33, O24, O26 and O21.
- Two-sticker methods: O22 placement, orientation transfer, Buffer A; O0 Buffer A.
- Five-sticker methods: C5/O17 transfer and Buffer A; D5/O6 transfer and final B.
- Twenty-sticker methods: A5/O34 placement, transfer, Buffer A and final B.

All canonical parameters, fixed A/B roles, q/r permutations, inverse preparation,
forward recipes, expanded costs, complete affected-orbit lists, targets, intentions,
bank names and sheet names are in the file. No parameters are selected from the
live residual. The manifest is bound to the existing verified catalogue and model.

Each cycle begins at the previous full-Home endpoint, explicitly performs its legal
inverse as Prepare, then reviews and executes the intended forward operation.
There are no resets, direct label assignments or new Sessions between cycles.
Before preparation, the test-user explicitly releases only locks on that recipe's
affected orbits; all other policies remain. The complete operation still requires
fresh protection review. Automatic completion must never silently release a lock.

**Ordering matters:** enter/restore the chosen orbit before New operation. Otherwise
New clears the old orbit while switching can restore an executed draft in the new
orbit. The normal product refusal to edit that draft is correct. The manifest's
protocol records the corrected sequence.

Native assertions should preserve the actual pre-state across each command; inspect
Current, Operation and conditional After; compare the committed hash with review;
verify complete labels at the endpoint; and check that worksheet loads and bank
changes preserve explicit Current/target/Next/protection until a real commit.
Use existing explicit cross-orbit Next / return and checkpoint / undo / redo /
restore routes between cycle groups. The root owns native execution and recording.

## Continuous residual cleanup for the recording

The manifest also supplies one continuous D5/O6 scene, prepared by the inverse of
five explicitly chosen forward stages. Actual backend replay established:

| Stage | Position wrong (nonbuffer / buffer) | Orientation wrong at Home (nonbuffer / buffer) |
|---|---|---|
| Before | 2 / 2 | 0 / 0 |
| Place Y=10, retained node99+ | 1 / 2 | 0 / 0 |
| Place X=29398, retained node0+ | 0 / 0 | 1 / 2 |
| Transfer q=(0,3,4,1,2) | 0 / 0 | 0 / 2 |
| Buffer A, same q | 0 / 0 | 0 / 1 |
| Final B, q above and r=(2,4,3,1,0) | 0 / 0 | 0 / 0 |

The integers inside q/r are exact ordered-slot permutation arrays, not angles.
The final stage restores all 259,800 labels; FinishOrbit is then derived from
actual Current. Every preceding After remains conditional. The recorded counts
are in `evidence/final-continuous-cleanup.json`. The scene contains two position
steps; its initial graphic's cycle shape must be read from actual Current data,
not described as two 3-cycles without checking.

## Remaining acceptance scope

These checks do not establish native readability, focus ownership, repeat-use
latency, window scaling, film quality or human usability. In the final native pass,
watch the full chain's weakest seam: restore/reuse must show the correct macro,
Current and target requirement before Add, and a Ready/Unmet operation must not be
presented as a successful orientation goal. Missing/Unknown evidence must remain
distinct from zero residual. The 32-cycle replay and continuous cleanup still need
their own native evidence; passing this backend review does not imply either ran.

## Session controls acceptance addendum

The root subsequently identified U25 session controls as an integration gap.
`session_workflow.py` reuses the **same** retained `enhanced.Workflow` instance and
Session: explicit timer, reports, random-word generation and proof-log saving are
delegated to those existing services. Random generation returns a detached recipe
for ordinary full-operation review; it restores the previous pending preview and
never applies the scramble itself.

A whole-model completion plan requires an actual non-Home → full-Home transition,
the exact pending operation and verified fixed frames for all35 orbits. Its metadata
is published only inside the existing `event:{head}:workflow` transaction. Source
labels describe actual journal assistance and never certify a human solve. A durable
acknowledgement suppresses repeat notifications; a separate `recorded_completion`
supports deliberate reopening. Automatic notification must be gated by the root's
new-commit response, not merely by a solved state or a historical receipt exposed
by redo/restore.

New attempt metadata joins the existing atomic `Session.reset` transaction through
the root-owned specialized optional hook. The recovery checkpoint retains the old
workspace preferences before the new workspace is cleared. Tests cover invalid
metadata, a failed metadata write after snapshot creation, exact recovery, legacy
camera/reset behavior, shared timer/resume, pending preservation/cancellation,
missing frames, failed completion-receipt writes, durable acknowledgement/reopen,
and existing log export.

The missing helper and then missing reset keywords were captured in
`evidence/session-workflow-red.log` and `session-workflow-hook-red.log`. Final
`session-workflow-validation.json` records11 passed checks,16.863seconds, exact
unchanged source/model bindings. Helper SHA256:
`c218e1a7954cfa3f6bc67566c89d21df42b39bc8fee117ef6eb11bbfc9ef0969`;
test SHA256: `25288ba04a9c9266c42f5a7e210f869a702c1b3ae5426edc9dfedd738cf94f82`.
The post-hook core/reference/crash receipt is
`evidence/core-contracts-/20260917-013431/result.json`: three exit0 results,
36.566 /16.120 /7.754seconds, bound inputs unchanged. Its Session hash is
`a616edb03c24832616b231502f99eadce841eb31ab7f4cb0adc3491b7231b0cd`.
The earlier011803 receipt applies to the pre-hook Session.

The integrated adapter was then checked with nine real Workbench tests:
`evidence/endgame-integration-/20260917-014751/result.json`, 38.170 seconds in
unittest / 39.919 seconds for the command, all passed with unchanged bound inputs.
The tests cover staged scramble provenance, explicit replacement, source removal
after edits, whole-Home journal receipt and acknowledgement, New recovery and
failed-write rollback, personal-data preservation, shared timer/resume, and log
export. New explicitly starts its timer; Resume does not. The earlier test premise
that New should pause was corrected, not reported as a product failure.

Two concrete integration seams were corrected by the root before this final run:
New/reset and scramble must retain canonical `prepare` so a work sheet can be saved;
recorded-scramble commits must recover Executed ownership from their exact
generation/recipe receipt after restart. Matching recipes alone, or matching
generations with different recipes, do not establish ownership. Default Local
center C1 is now explicit in New and workspace reset and is asserted here.
Adapter SHA256 `7fab22b69e2705ff8fca379d4787013d9db38168a44350cb05753d2379f06b2f`;
test SHA256 `b742395e1317c0d9108d7f403529078d6c812b1df8e4536aaf7c5d84ad34c800`.
Native session controls and uninterrupted replay remain separate acceptance work.
