> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Independent macro-variant review — 2026-09-16

Scope: compliance with [the bounded evening specification](../specs/macro-variants-stage-20260916.md), followed by source-level correctness/lifecycle review and read-only inspection of root's final native evidence. No application edits, build or GUI input were performed by this reviewer. Root alone operated the native runs. Final G2 evidence is identified below; G1 `postapproval-g1-20260916-085932` was still root-owned work at this update and is not claimed passed here.

## Inspected identities

- `adapter.py`: `6C1BA412743E3347D66EC7144CD29340E3792D6D0EB337770DCE65B9848141C4`.
- `native/ExperimentMacroRelations.cs`: final reviewed `60A003A3D15FFD25A064A367E5B708EA10A2DFBBC074836EF72C44FA1708FB9A` (earlier reviewed `4133CBE4…` contained the cancellation race below).
- `native/ExperimentShell.cs`: final reviewed `6438050A2A9DFFE4052115DA75B51DBD392DB78E231E4E632B061781ABD62C19`.
- `tests/MacroVariantsNativeChecks.cs`: initial reviewed `EDC534DF781705FE8F22664B474B4174290AE45364894CB0837317FA018F93D2`; later coordinator test-entry synchronization changes were also inspected. Use the final run manifest for its compiled identity.
- `tests/StopAcknowledgementNativeChecks.cs`: `35EA38C4F59943320748EF56A64C501322D79EFB0B13AAA4966BD217F4CAA0B6`.
- `native/ExperimentHelp.cs`: `671CA3B4F492C932DBF84E196473E7C282C208DAB5F3AA9B24D166FBBA463C9D`.
- Also traced the narrow Tools/Shell/Keyboard integration, shared Macro key-set append, native compile manifest/runner, existing `SendRoute`, `ShowOwned`, stop-job endpoint and exact `Model.net`/normalization contracts.

## Compliance findings

The bounded product direction is appropriate: explicit comparison and separately saved inverse/legal-word conjugate, using the existing catalogue and operation boundary. It does not claim spatial-reference certification, endgame completion or a new user approval.

The inspected backend binds source ID/version/exact recipe, checks the exact current R, validates the complete resulting recipe before mutation, recomputes full net actions and separates equal-active from both-inactive orbits. Complete net inverse/equality does not become a prefix-protection certificate. Creation uses a new canonical identity and preserves the source saved group; the current correction assigns fresh personal metadata instead of copying a forward-use tag, note or pin onto its inverse. Imported provenance is not trusted as proof. Save failure restores the prior workspace/library references. Existing recipe, pending preview, state and review authority are outside the creation change.

The native code requires an explicit second entry, binds returned records/model, rejects mismatched or late UI results, and does not update disposed dialogs. Saving does not select or insert; `Select saved macro` is a separate action followed by the existing read-only effect route. Details exits its modal ownership before opening the save dialog. New shared Macro keys append after existing entries. Help explains legal R and net-only scope accurately.

These are source findings. Backend owner reports 12 real-model tests passing on the identified backend; this reviewer inspected the tests but did not independently rerun them. Actual controls, sizing and routed workflow evidence comes from the separately identified root-operated run below.

## Concrete issues and disposition

1. **Resolved test mismatch.** The initial native helper expected second-choice index `-1`, while the interface deliberately uses index `0` with empty canonical ID and a disabled Check button. That implementation does not choose a macro. Root corrected the assertion to inspect the empty ID and disabled action. No product change was needed.

2. **Resolved, with scoped native evidence.** The first modal implementation had no reachable Stop while comparison/save ran. The current version uses the existing action controls for Stop, retains the original request's authority, prevents a second save and reports an already-completed save rather than claiming rollback. Final G2 actually compares under a staged preview, requests Stop and checks retained execution context. The test correctly does not claim that cancellation necessarily beat a fast completed request.

3. **Cancellation-ownership race resolved; deterministic admission check passed.** The earlier `ExperimentShell.StopAnalysis` started an independent fire-and-forget HTTP task, while Relations re-enabled actions at original `Send` completion. The retained `/api/stop-job` handler sets one global cancel event, which each new experiment job clears/reuses. The source-derived counterexample was A finishing → delayed A Stop → B starting → old Stop cancelling B; it was not an observed native failure.

   The final source now gates `SendRoute`, `IsReady`, command dispatch, input state and the native bridge on `busy || stopPending || stopFailure`. Closing Compare does not clear that global barrier. Stop requests are deduplicated and only sent while work is active; release requires the actual `cancel_requested: true` response. A failed or uncertain response latches a clearly reported disabled-input state requiring isolated relaunch. Relations captures the original comparison/save receipt before awaiting acknowledgment, so a completed save remains reported rather than falsely rolled back. No second command or turn is queued.

   The coordinator's test uses a test-only loopback responder to delay a real LocalApi stop response, simulates original work completion with the actual Shell busy field, checks that a subsequent real Shell submission is refused, then releases acknowledgment and explicitly submits a fresh read-only command. All six admission assertions passed in final G2. This proves the tested admission ordering; it is not a production-engine long-job race or a physical-input trial. The uncertain-response latch was source-reviewed, not separately fault-injected by those six checks.

## Final native evidence inspected

Read G2 report (`../../native-baseline/postapproval-g2-20260916-085608/report.json`; local-only reference), build manifest (`../../native-baseline/postapproval-g2-20260916-085608/build.json`; local-only reference) and its `run.log`, without rerunning them. The report records **exit 0 and 339 assertions**. Executable SHA256 is `a5ade6a5da43e12e5a66c2102c33163570bd36da1f9dfdd0ce9f0a8b26755d71`; report SHA256 is `1D8BC58A707E0925B938C96FB4623F286334FA7DB0E58CD4184582B1B44F7EBB`. The manifest's backend, Shell, Relations and delayed-stop fixture hashes match the final source identities reviewed above.

The report covers explicit source/second-macro comparison; separate inverse creation with source preservation; explicit selection and Add with fixed provenance; exact retained-witness equality; chosen R derivation without selection; full operation review, preview and explicit commit; work-sheet comparison/reuse; second insertion with protection and locked Next; undo/redo; compact comparison/save controls; and the six deterministic stop-admission assertions. An actual comparison and Stop request under a staged operation also preserved its pending context and executable state. Assertions are not 339 separate solver scenarios. These are root/agent-operated native controls over legal synthetic witnesses, not a human solve or physical-key trial.

The earlier `postapproval-g2-20260916-084855` report remains a genuine exit-1 record: `Macro details` did not open. Root diagnosed the test's posted dialog action racing a legitimate trailing macro-effect request. The revised fixture waits for current readiness inside that posted action; the application did not gain automatic command queuing. The successful final run is the result used here, rather than relabeling the earlier failure as a pass.

## Final review boundary

No remaining blocking mathematical, identity, persistence or lifecycle defect was found within this limited source-review scope; matching G2 evidence now establishes the tested native connection. This does not grant user approval or establish complete 0.4 acceptance. All-orbit residual coverage, cooperative-window improvements, sustained-cycle acceptance and final recording remain separate obligations in [tomorrow's coverage review](TOMORROW_COVERAGE_20260916.md). That document retains its original cutoff; subsequent work belongs in root's final HANDOFF.
