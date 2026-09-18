> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Bounded fixture triage — 2026-09-17

Observed evidence: `../endgame-integration-/20260917-060109/5.log` has one
executed-projection failure among 14 checks; `6.log` has five setup errors,
then Windows `engine.lock` cleanup errors. This initial diagnosis is source/log
inspection, not a fresh product regression result. No product/test originals
were changed. Root separately authorized isolated candidate copies afterward.

## Executed projection

`tests/test_cycle_projection.py:218–228` writes only the process-local
`completed_operations[orbit] = head`. It never commits an operation and does
not have a saved generation marker or journal receipt. `adapter.py:382–385`
therefore correctly reports Draft. `cycle_projection.py:289–297` derives
projection availability from that authoritative operation state, not from a
bare dictionary membership. It will project Prepare in this fabricated Draft.

Actual commit builds the saved `completed_operation` marker and atomically
writes its matching workflow receipt (`adapter.py:416–452`). On reopen/undo,
`restore_completed_operation` checks current ancestry, generation, model,
pre/post/parent and exact recipe (`340–380`); recipe equality alone is not
ownership. `completed_operations` itself is not a persisted workspace schema
field. No accepted historical migration path was found that licenses treating
the test's standalone dictionary assignment as an executed operation.

Minimum repair: use explicit legal Prepare/Macro drafts, Prepare goal, then real
Review → Preview → Commit. Use distinct Prepare/Macro words so an extra Prepare
evaluation can be detected. Preserve unavailable forecast/source/slot-label
assertions and available actual objects; assert full actual labels/head/revision
and pending remain unchanged during inspection. Review/edit must remain rejected
until explicit New/Reuse. New must clear phases, change operation generation,
and permit a fresh valid draft/forecast without executing it. Do not manufacture
the new marker merely to make the old test pass. The same obsolete injection
exists in `tests/test_residual_cycles.py:147` and needs the same real fixture.

## HTTP shared context and setup cleanup

The common fixture (`tests/test_window_layout_transport.py:130–139`) omits
`workflow` and defines `ObservedWorkbench.__init__(*args)` without keyword
forwarding. `adapter.install:1793–1794` now requires the server's existing shared
Workflow and supplies `workflow=` and `native_profile=`. Adding only the missing
context entry will expose a second `TypeError` from that observed constructor.

This is a test setup mismatch, not evidence that the actual engine lacks the
dependency: retained `server.py:117` constructs `workflow=Workflow(session)`,
and `engine.py:20` injects `install(Handler, locals())` in that same main scope.
Minimum repair: construct one real Workflow for the existing test Session, pass
that instance in context, and forward `**kwargs`. Keep the explicitly labelled
native-snapshot stub; these are HTTP/Session tests, not renderer validation.

The setup exception occurs after Session/worker creation, before server creation.
unittest does not call tearDown when setUp fails. Cleanup currently lives only
in tearDown (`147–154`), so the still-open Session owns engine.lock while
TemporaryDirectory's finalizer tries to delete it. This explains the observed
WinError32 without implying a product crash or a foreign process.

Register cleanup immediately as resources are acquired, before adapter.install:
server stop/join only after thread start; pool shutdown; dynamic current-Session
close; temp deletion last. The existing restart case closes the first Session
and sets `self.session=None`, so cleanup must not blindly close a captured old
Session twice. Retain all malformed-before-commit, post-commit display-error/
cancellation, pending preservation, no-selection/follow-orbit, auth/layout/
lock/persistence assertions. A narrow injected install failure should prove the
fixture now removes its own temp directory without leaking the lock.

## Inspected source bindings

```
adapter.py                         a798b9d5d0ae704790552331fe4ec729dd1a0ca4c41f3eb3d33bc511df2ea32d
cycle_projection.py                455015fa4d284b22a5132f051767047a04116488c0100a9eb0dcb43e2e711947
tests/test_cycle_projection.py     d05d33e4996801d6fa9e7996d4377cfc3e0d95ed727de0e7f02cf5e7153352c9
tests/test_cycle_transport.py      febcabcee6bcfa78842374ccdf43cb9f0f65d722a36f1c91fd20a136df0baf4f
tests/test_window_layout_transport.py 919eb3cf8d71cd2c9ecb3f1c1dfd5c0a5b52bab4810865aa217354eea490c7c8
tests/test_residual_cycles.py      ecc2b47d3e9c524d25778482b348039e0673da9c69c1640017f470b0875f9531
server.py                          3f3e48f5cf7888e02d01aaf7a4b4153c5ec2e496bf0b455a8ab1efea8286a8c4
engine.py                          9f607dfb5f259979a6807c817ea099aad2f335b80f335cbb48515b8dce0204ec
```

The initial source triage established fixture drift. The subsequently authorized
candidate run below exposed real remaining dependency/response boundaries;
they must not be relabelled as fixed fixtures. Originals remain frozen until
root explicitly promotes reviewed corrections.

## First candidate run: 39 passed, two open failures

`fixture-candidates/run-20260917-061256/result.json` binds 68 input files,
including original/candidate tests, current backend and immutable model assets;
before/after hashes match. Four modules ran against actual product code:
cycle_projection 14/14; cycle_transport 4/5; residual_cycles 11/12;
window_layout_transport 10/10. This is **39/41, exit 1 overall**. Concurrent
headless final-suite work makes elapsed times diagnostic, not a benchmark.

Both corrected executed fixtures now use actual legal Review/Preview/Commit.
The distinct Prepare/Macro case confirms unavailable repeated Prepare projection,
unchanged full actual labels/head/revision, blocked repeated review, and explicit
New re-enablement. All existing layout tests plus the injected-install-failure
cleanup case pass; no engine.lock finalizer errors occurred. The unchanged
cycle_transport companion is byte-identical to the original.

1. **Cancellation after successful commit escapes through a later snapshot.**
   Log `1.log` retains the unchanged cancellation assertion. The display callback
   executes only after `service.command(commit)` returned successfully; it sets
   the job cancel event and raises InterruptedError. The handler catches that
   secondary failure but returns a job error saying the puzzle was unchanged.
   Source chain: `adapter.py:1945` attaches the token; `1950` runs the primary;
   `1962–1967` catch the display failure; `1969` calls native_reply → snapshot
   (`816`) → SessionWorkflow.report (`107`) → `_source` (`62`) checks that same
   still-set token while traversing the committed journal. The token is detached
   only in the outer finally (`1980`), too late to preserve the successful reply.
   The current test aborts at the error response before its later full-label
   assertions; no runtime claim is made that those unreachable assertions passed.

   Existing boundaries to preserve: Session.commit checks cancellation before
   and inside its SQL transaction; commit_with_result_warning accepts only an
   exact matching durable event/recipe/preferences/workflow receipt following a
   status error. The log-import path already retains `import_applied` across
   native reply failure (`1971–1978`) and Native Shell retains importReceipt
   across refresh/adoption failure. Ordinary successful commit currently returns
   None, so that fallback cannot preserve its acknowledgment.

   Minimum cancellation fix: once the primary has definitively succeeded, skip
   canceled optional projections and detach the analysis token only while
   constructing the authoritative final reply under the same Session lock.
   Keep the shared event set until job ownership finishes/next submission clears
   it; never suppress cancellation during validation or commit. If any final
   reply/adoption error is also handled, extend the existing explicit committed
   receipt path with exact head/hash; do not infer success from matching states,
   retry the commit, or invent another transaction authority. Native currently
   preserves only import receipts on adoption failure. Root owns this correction.

2. **Missing frame dependency reaches an unguarded invariant constructor.**
   Log `2.log` keeps the original `transported_frames=None` Unknown injection.
   residual_bundle calls invariant_service; loader constructs OrbitInvariants,
   whose `frames.m` access raises AttributeError. residual_bundle only translates
   ValueError to unavailable invariant evidence. Normal Workbench construction
   supplies frames, so this is a proven injected dependency-failure boundary,
   not evidence of a normal startup failure. A narrow missing-dependency check
   before the loader should yield Unavailable while preserving known position/
   slot facts and preventing ExactSolved certification. Do not broaden the catch
   to hide arbitrary AttributeError or turn Unknown into identity.

The three fixture corrections are proposed, not applied, in
`fixture-candidates/fixture-corrections.patch` (SHA256
`1917579c78c468816e71d99cef52cf554c74ac4bf2e1be37205e3c5cda7dfbbe`).
It excludes copy-only import-root relocation. Read-only
`git apply --check --ignore-space-change` passes; the plain check encounters the
existing newline whitespace difference. This patch does not fix either product
failure above.

## Additional fixture run and current candidate review

`fixture-candidates/run-20260917-062510/result.json` binds 77 files with
unchanged before/after hashes. Metadata tests pass 6/6. Native response tests
pass 5/9, with three failed obsolete expectations and one real cancellation
error. This is **11/15, overall failed**, not current product acceptance.
The run finished before the coordinator's memory-pressure hold; no further
model-heavy tests started during that hold.

The metadata fixture lacked the newly required real
`tests/final_workflow_cases.json`; the copy now supplies and hashes it, retaining
the timeout and shutdown assertions. Native response setup now supplies the
actual shared Workflow and registers cleanup before possible setup failure.
Its remaining expectation corrections follow actual authority:

- `workflow_continuity.transition` activates explicitly locked Next after a
  successful Place/Insert. Assertions now retain exact placed-X success and
  require Current=Y at its actual current position, locked destination retained,
  and Next consumed. A later actual phase inspection must reflect that Current.
- `transform-macro` returns the created independent record and fresh comparison.
  Assertions check exact R-inverse/M/R recipe, provenance, comparison/model,
  saved ID, source metadata unchanged, and unchanged workspace/pending/labels.
- Cancellation after Preview remains a real error in the original product.
  The test keeps its pending identity, Staged/executable, full-label and no
  forecast assertions. Only the new authoritative retry path may perform the
  second `snapshot(prediction=False)`; normal commands still require one call.

Read-only review of root's initial adapter candidate SHA256
`8c2afc8c82c9ffb9ee4e8f58a9ffb22d028a8c8f8396a38049fabb6653d2faf8`
found primary command outside retry, same-lock reply construction, shared Stop
event retained, model token detached only after success, and durable import
receipt fallback retained across both reply attempts. The None-frame guard is
before cached invariant reuse. No command replay or pre-commit cancellation
suppression was introduced by that candidate.

One additional source finding was reported and root corrected in its candidate:
the first retry omitted the cancelled-forecast diagnostic when Session report
failed before `prediction_error` was produced. It also reused the core's
"puzzle state unchanged" exception text after an already completed primary
action. The revised candidate emits stage-specific cancellation text and an
explicit unavailable forecast, discarding phase inspection on that retry.
Regular exception details remain unchanged. These conclusions are source
review, not executed candidate proof yet.

The cycle cancellation copy now additionally asserts one actual Session.commit,
head increments exactly once, all labels equal the actual pending.after, and
shared Stop remains set while the model token is detached. Only cancellation
permits unavailable phase inspection; a regular display failure must still
return the already computed actual phase. Original product/tests remain frozen.

The revised root adapter candidate is now
`8c545199737361e080997cd32f9b6b1b4ca614ce7c6cfa24b3f18648d016e748`.
Source review confirms the cancellation warning correction described above.
No candidate execution is claimed until its coordinated serial run finishes.

## Operation lifecycle fixture triage

Original `060109/31.log` ran 20 cases with three failures and four errors.
Source inspection identifies obsolete fixtures, not evidence of a new product
failure at these seven points: two commit wrappers rejected the now-required
atomic preference/event kwargs; Next had already been consumed by successful
insertion; template reuse omitted its required inspection confirmation; and
two assertions excluded the now-required durable generation/receipt changes.
The seventh test assumed Reuse leaves preferences byte-identical. That premise
is no longer true: a new `operation_generation` and removal of
`completed_operation` are the exact durable evidence distinguishing Reuse from
the completed work, including after restart.

The candidate keeps all 20 tests. It forwards kwargs to the real Session commit,
requires template inspection and rejects missing confirmation, verifies exact
generation/journal receipt after reopen, and checks generation replacement
separately from the retained work problem. The ambiguous reuse test retains
the negative case as a genuine pre-write error (no stored change, still
Executed), then verifies a post-write acknowledgment failure yields a saved
warning only with changed durable generation, no old completion marker, no
revived review, unchanged head/full labels, and Draft ownership after reopen.
Setup cleanup also closes Session before deleting its temporary directory.

Copy SHA256 `9740a83066530337068db7fb8b5f6f79c87a0aa47cde05596e3ed862111b03e5`;
original SHA256 `74eeb36dfcf828aeda8d2226324671665a4d7a9c0452069d2cf4c6d1446392ca`.
Prepared and compiled only, not yet executed. The updated seven-file proposal
`fixture-candidates/fixture-corrections-seven-v2.patch` has SHA256
`417940f4559887205d3773d75adfc623705427f61feebe735e9a58e2c47a7731`.
Read-only `git apply --check --ignore-space-change` passes. Historical three-
and five-file patches/logs remain intact; no originals were promoted.
