> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Unified Solve window: bounded presentation change

Source baseline: postapproval-g2-20260916-085608 (339 recorded assertions, before this change). This patch has not yet passed the new native workflow.

## Changed presentation

- One canonical modeless `solve` window owns the existing Macro Base catalogue, selected-effect controls and `workDock`. The main Hub, actual Local/Global geometry and onscreen keyboard remain separate.
- Explicit Macros / Prepare / Protection pages share one goal, input destination, three phase lanes, execution strip and Current / locked Next / protection readout.
- `macro-search` opens Macros and focuses its search. `operation-focus` opens the same window and focuses the original guarded execution strip. The old `macro` / `operation` open and hide requests are aliases, not second windows.
- Existing macro selection and explicit Add/Replace semantics remain. Page selection, layout and search do not execute, select a target, change a key set or add a macro.
- New registry hooks are `solve-macros`, `solve-prepare`, and `solve-protection`; the root owner supplies their editable single-key routes and build registration.
- The original `operationStrip` still directly owns Check / Preview / Execute / Cancel. Stop check remains directly available. All state-changing controls use the existing command/Send admission boundary.
- Long shortcut chords remain in tooltips and the command index. Compact buttons show their available unmodified keys rather than letting long modifiers push adjacent actions outside the window.

## Compatibility and placement

The new window restores `view.windows.solve` if present. Otherwise it reads the former operation bounds, then macro bounds. It clamps to the readable Solve minimum and current screen without rewriting old records. With no canonical solve entry, either previously visible old window opens one Solve; an explicitly hidden canonical solve entry takes precedence over old visibility.

Only `solve` is registered in the live window dictionary and emitted in new layout saves. The backend owner adds this permitted layout key. Existing windows are never moved by the new placement rule. The minimum is 780×570 outer; preferred is 840×620 outer. On a small work area the existing policy may reduce preferred height to the minimum. Actual content fit remains a native acceptance check.

## Evidence so far

Command run from the experiment directory:

```powershell
& C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe /nologo /r:System.Drawing.dll /out:evidence\solve-window-20260916\SolvePlacementChecks.exe native\ExperimentWindowPlacement.cs tests\SolvePlacementChecks.cs
& .\evidence\solve-window-20260916\SolvePlacementChecks.exe
```

Compilation and execution succeeded. Four pure rectangle checks passed: logical 1280×680 and 2560×1560 work areas, negative monitor origin, and adapting former operation dimensions. This does not establish native rendering, DPI fit, focus or actual monitor behavior.

`tests/SolveWindowNativeChecks.cs` supplies a callable native helper for the root's serialized GUI run. It checks one window instance, macro/execution focus routes, original execution-parent guards, all three minimum-size pages, retained shared context, empty search without identity loss, and unchanged hash/draft/roles/reference/bank/Next/pending state after presentation actions.

Independent source review found that stale reviews retain their old prefix result in the adapter. The Solve readout now explicitly marks that prefix result stale. No old `Clear` result is presented as current after the whole review expires.

A no-window WinForms text/control measurement reproduced the layout concern: four selected-macro action buttons require more width than the approximately 359px minimum right column; a representative role caption measures 38px of text, exceeding the old role allocation after padding and borders. The action row now wraps with content-measured height, and role height is measured from the actual caption and available width. The native helper independently checks complete role-text height and the Check library hit target. This measurement is not a screenshot or native application usability test.

The R readout shows the first twelve exact terms and total count. The original full word stays unchanged and available in its editor; the compact repaint no longer joins an arbitrarily long word on every adoption.

## Remaining verification and limits

- Run the actual current-build native helper and complete E1 check/preview/execute/next workflow, including the independent delayed Stop acknowledgement regression.
- Recheck minimum and preferred sizes, real dense names, owned-dialog focus return and close/reopen. There is no claim that a 1280×720 desktop can show all large tools without overlap.
- The Prepare page exposes existing exact role and frame controls; it does not create automatic frame variants, Next policies or a new macro-use classifier. Those are separately owned backend/root work.
- The protection page displays existing exact records. A canonical position fallback is retained where the current payload has no mathematical name; it must not guess an identity from a label.
- Legacy generic role/reference/protection editors remain available from the consolidated context. Replacing their input widgets is outside this first presentation slice.

## First native replay and bounded correction

The 20260916-183032 native run exited 1 at `Minimum Solve exposes solve-macros in macros`. The earlier window identity and F3/F4 focus checks passed, but this is not an accepted stage result. Its helper stopped before taking the page screenshot; that diagnostic ordering has been corrected.

The navigation row reserved 34px regardless of a selected button's actual preferred height and its margins. A hidden WinForms layout experiment with the same Segoe UI 10pt, flat-button border, padding and margins allocated only 31px to a button whose preferred height was 34px. The content-derived row height is 40px; the same hidden layout then contained the full 34px button. The first too-simple one-row fixture stretched its sole row and did not reproduce the native topology; a second-row percentage remainder was required to test a fixed-height navigation row. The experiment proves insufficient row allocation, not the entire native clipping path.

The correction measures every FlowLayoutPanel row in this Solve surface from its controls, margins and padding, including selected-button borders. Prepare's fixed content height is derived from complete role and frame text plus measured action rows. Overflow scrolls only within the active middle page; the phase and execution strips remain outside those scroll containers. A coalesced, guarded second layout pass accounts for width changes when scrollbars appear. Label margins are explicitly empty where padding already supplies the inset.

The native helper now captures each page before assertions. Failures include control and ancestor bounds, client and screen rectangles, preferred sizes, margins, scroll range and display rectangles. It may scroll an explicitly overflowing content page to reach an action, but still requires the full hit rectangle and independently checks that the execution strip stays fixed and exposed. This correction awaits the root's fresh native replay.

The 20260916-184404 replay passed navigation and then failed on `macro-label`. Its actual screenshot (`solve-macros-minimum.png`) and geometry show an unwrapped catalogue tool row needing 406px in a 373px container: Edit occupied X=352, W=52 and was clipped. The retained catalogue toolbar still had `WrapContents=false`; it now wraps and uses the same content-height calculation as the other Solve tool rows. No control width, type size or filter semantics changed. A hidden FlowLayoutPanel fixture using those exact recorded dimensions reproduced Edit at `{352,2,52,31}` before the change and `{2,37,52,31}` inside the measured 70px row after wrapping. It exited 0. The full native empty-list, dense-data and operation workflow remain pending a fresh replay.
