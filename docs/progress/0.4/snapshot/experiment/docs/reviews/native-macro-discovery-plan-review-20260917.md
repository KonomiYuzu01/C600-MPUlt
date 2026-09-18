> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Independent review: bounded macro-discovery correction

Status: plan accepted with the wording/layout boundaries below; implementation and native recheck are not assessed. No GUI input or product edit was performed for this review. Evidence inspected: the actual wave-b screenshots `b17-current-orbit-results.png` and `b21-selected-macro.png`, alongside the recorded search result `b13-macro-filter.png`. Root retains GUI ownership.

The proposed scope directly addresses the observed preparation problem: search additionally includes the macro's complete mathematical orbit name; the existing empty-result/status area explains unchecked effects and unverified frames; the existing Check library action moves beside filtering. No automatic classification, frame mapping, macro selection, insertion, or new command is needed.

## Counterexamples and required boundaries

1. **Empty results are not always caused by verification.** `All uses` plus a nonsense search term also produces an empty list while the library can contain unchecked entries. Use “No checked matches” only where the selected use filter actually requires checked classification. Otherwise retain a search/filter no-match explanation. Label any global counts explicitly as library-wide; do not imply those unchecked records are known matches for the current query. Zero unknown records must not produce a stale “check library” prerequisite explanation.
2. **Effect checking and frame verification are different prerequisites.** Keep their counts/statuses separate. Check library must not imply that it supplies a verified reference frame or automatically maps one. After checking, frame-unknown entries must remain unknown and excluded from classifications requiring verification. Refresh counts from the same current library/context; do not treat an unsuccessful or cancelled check as complete.
3. **The moved action must work before a macro is selected.** In b17 the list is empty and the old Check library action is below the visible details area. Putting it in the filter toolbar resolves that exact obstacle only if it remains enabled, visible and clickable with no selected row. The extra wrapped row must not obscure Prepare/Macro/Cleanup, Check/Preview/Execute, or Current/Next/protection summaries. In b21 selection changed scroll position; test both the empty and selected states. No sticky-toolbar redesign is requested.

For search, add the complete mathematical orbit name to the existing matching fields; retain custom names and other currently searchable text. This does not authorize guessing affected orbits from unchecked effects or returning an unknown entry as an applicable operation. Retain the same command, editable route and Macro.Digit1 binding; remove the former button instance rather than leave duplicate entries.

## Same-scenario native recheck

1. Bind the repaired fresh build and a new disposable session; load synthetic E1 through the visible command. Record Current/locked Next, protection and empty phases.
2. Before classifying anything, search `19 cap` from the displayed current-orbit name. Confirm corresponding mathematical-name entries are discoverable without internal IDs, while their use remains unchecked. Searching alone must not select/add/execute anything.
3. Clear the search and select Current orbit. On an initially unchecked library, inspect the empty explanation, unknown counts and adjacent Check library action. Confirm the button is accessible without selecting an unrelated macro or scrolling the right details pane.
4. Test the counterexample `All uses` plus an intentionally nonmatching term. It should report a search/filter miss, with any global unknown count clearly scoped; it must not imply guaranteed matching operations after checking. Clear that term.
5. Explicitly invoke the existing Check library action once. Observe the actual completion or failure/cancellation boundary rather than assume a fixed delay. Confirm refreshed counts and classifications, no automatic selection/insertion, unchanged Current/Next/protection/draft. Frame-unknown rows remain unknown where applicable.
6. Reapply Current orbit and inspect the actual results/reasons. If no verified matches remain, the message must accurately explain that state; an empty result is not itself a failure. Select an intended visible entry and ensure the relocated toolbar still leaves selection/Add and phase controls usable. Continue the next legal solver slice only from actual available results.
7. Verify the retained Macro.Digit1 route in the explicitly selected Macro set and its existing editable route metadata. Text-field focus must retain its established behavior. Mouse and keyboard invocation must reach the same command. This check does not certify physical keyboard hardware.

Screenshots should retain the before-check empty state, post-check result, full-name search result, and selected-state layout. The existing wave-b evidence remains historical; only the new artifact can close S1/S2. This plan review does not grant G1/G2 approval or claim a complete solver workflow.

## Regression checkpoint — 2026-09-17 05:19 JST

Ownership: only `tests/MacroUseNativeChecks.cs` was edited, plus this review note. Product changes remain root's work. Test SHA-256: `ca7fc6ee31f5475dac790202d87fff4eb79c44a0da752d70dd6859d4d86ff000`. The test file is frozen while root records native diagnostics.

Before the existing library-analysis step, the focused `macro-use` run now checks the real fresh E1 library through its native TextBox, ComboBox event and existing button. It searches the complete mathematical current-orbit name and `19 cap` before analysis, proves unknown rows remain unknown/unselected and the full work context is unchanged, checks the Current-use explanation and library-wide counts, and distinguishes All uses plus a nonexistent search term. The Check library button is found as the single existing command control inside Solve and invoked through `PerformClick`, replacing the previous direct command invocation for this step. Its editable context menu and retained Macro.Digit1 route are checked.

The fresh preflight is explicitly limited to `--focus macro-use`: the full regression has already selected and inspected macros before calling this class. It must not silently reset those existing facts to manufacture a fresh catalogue. Both routes retain the existing real-model use/classification checks after the button action. `Stable` now also includes target, block, per-turn scope and explicit position locks, extending the preexisting label hash/Current/Next/draft/reference/bank/pending/orbit-protection comparison.

Layout checks run at the initially opened and declared minimum Solve sizes, without scrolling. They print actual toolbar and child bounds/preferred sizes/exposure before asserting complete access to search, use picker, Filter, Check library and Edit; compare the empty-result label's preferred height with its available height; and check all three phase controls plus Check/Preview/Execute/Cancel. A fixed initial row height in source is not itself treated as a clipping failure because FitSolveWindow measures wrapped action rows. The actual run must supply the measured result.

Compile command (executed, exit0):

```powershell
& '<user-home>/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 tests/run_postapproval.py --mode g2 --focus macro-use --compile-only
```

Receipt: `native-baseline/postapproval-g2-20260917-051851/build.json`, after-build **124 inputs unchanged**, executable `e67b62bedf75bc485dba27d8a9425a9b1ee4117b59d18fcd3c3d128abb7d7d15`. Runtime explicitly **not run**. The earlier compile attempt `051823/compile.log` is retained: it rejected the test's reference to private nested StepLane; the test alone was corrected to inspect the existing dictionary through IDictionary before this successful compile.

Root's exclusive-GUI run command:

```powershell
& '<user-home>/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 tests/run_postapproval.py --mode g2 --focus macro-use
```

Limits still open: no native run was launched by this reviewer in this task; actual button visibility/clipping and classification completion are not yet passing. New intermediate `macro-discovery-*.png` images are the existing harness's DrawToBitmap output, not desktop capture. The existing final `solve-macro-use` capture still includes its real FFmpeg desktop frame. Preflight proves no automatic selection/Add; the prior agent-operated selection-versus-Add evidence remains separate. If the fresh fixture contains zero AwaitingVerification records, the test verifies that no nonexistent frame count is advertised; it does not exercise a genuine three-line mixed-unknown state. No fake frame record was introduced. A later actual-UI recheck is still required to close S1/S2.
