> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Independent native solver review — 2026-09-17

**Latest actual-UI boundary — wave d:** build `8da86038fbe0cf37` completed the documented legal E1 Star0-reverse/Star11-reverse sequence by two explicit commits. Locked Next continuation, undo/redo, fixed-step reuse, whole-orbit rejection/Cancel/recovery, protected X and automatic exact-orbit protection were actually exercised. The completion summary appeared. **S3 remains open:** whole-orbit protection requires raw internal IDs; the known `[33]` was an explicit developer workaround. Sample closed normally, both processes exited,111 bound files unchanged. See wave d at the end; broader endgame/Resume/final-film acceptance is not claimed.

**Repair recheck: S1/S2 are closed for the reproduced fresh-E1 discovery scenario** by independent inspection of root's native regression `postapproval-g2-20260917-052216`: 65 assertions, exit0,133 bound inputs unchanged, inspected GDI state images and actual desktop capture. The final section records scope and remaining limits. This is review of a root-run native regression, not a new agent-operated solver slice; execution/protection-recovery/full-solve coverage is unchanged.

**Current status — wave 2 completed a bounded actual-UI slice on build `9a8a8e39533dc179`.** Synthetic legal E1 loaded through visible commands; Current/locked Next inspected; a visible macro selected, explicitly added to Prepare, checked, and the committed Current view recovered. Two related macro-discovery problems are recorded below. No execute, protection rejection, undo, full solve, or human trial is claimed. The dedicated b sample closed normally, both processes exited, and all 111 bound files stayed unchanged. GUI/source ownership is released to root. See the final section for current evidence; wave 1 below is historical.

**Wave 1 status: BLOCKED/INTERRUPTED before legal solver operations.** The review found automation compatibility blockers and then stopped for the user's active desktop use. It establishes real startup/command-window transitions only. No product correctness defect or solver pass is inferred from untested functions. At that historical handoff the isolated process/session was intentionally left alive; it has since been closed as documented below.

## Question and scope

Can a mathematically competent solver unfamiliar with the program establish a legal state, identify Current and locked Next, explicitly prepare/review/execute an operation, understand protection rejection, and recover using the actual native UI without guessing internal IDs? The latest scope also requires a capability coverage matrix across retained and new functions. This review is agent-operated native UI evidence, not a human trial, a full solve, or a replacement for the user's already-approved G1/G2 gates.

Unknown at start: whether the blank solved startup exposes a discoverable legal practice source; whether a selected macro/reference and active frame remain legible across windows; whether protection rejection provides an actionable recovery route; whether unfinished orientation/buffer stages are accessible using the existing legal fixtures.

Latest user scope refinement: do not redundantly retest unchanged retained observation/Instant Turn behavior. Preserve valid prior evidence as inherited evidence; verify suspected missing UI routes and later retest changed controls. The full coverage matrix spans repair/recheck waves, not an obligation to repeat unchanged renderer or mathematics in this first slice.

## Artifact and environment

- Checkout: `<source>`; experiment `work/experiments/magic600-04`.
- Git HEAD `5e1d35de792ccc8db896b8abf74ff2b1b00e0749`, with existing tracked/untracked modifications retained. Ignored experiment is bound by the build manifest, not HEAD alone.
- Command: `<user-home>/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe native_launch.py --mode g2 --session solver-review-20260917-a`.
- Session path was confirmed absent before launch: `sessions/g2-solver-review-20260917-a`. No personal session or profile was used.
- Build `39c8cb288142dba913f1a90fb8e7ce62a4c579098bb26fd54acd60fcc60231e4`; executable SHA-256 `8fcc6cb25d7a86ce64257145e62d7720372813eb56883731b8dafce5e78d5d60`.
- Manifest: `native-build/39c8cb288142dba9/build.json`; model `58c83f336e451aa64147fe1b8db2d60d7d43806aa3a35122f8403d5af51253f9`; before-start 110 immutable files unchanged.
- Windows 10 build 19045; Intel HD Graphics 620; desktop capture 1920×1080; native window DPI96 from read-only Win32 inventory. No Legion/RTX performance claim.
- Engine PID 12716; host PID 25540; launcher exec session 70508. Run intentionally left open at orchestrator request. Both processes still report Responding=True; no normal-exit claim or launcher exit code is available.

## Evidence method and initial observations

ComputerUse `@oai/sky` is the input/accessibility interface. Actual desktop PNGs are captured with the retained `recording-tools/python/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe -f gdigrab -framerate 1 -i desktop -frames:v 1 -update 1` and visually inspected. Screenshots are in `evidence/native-solver-review-20260917/`.

1. Fresh native launch opened a blank solved O33 workspace. Current and Next both visibly say “not set”; protection count is visibly zero orbits/zero positions. Startup initially showed “Checking the explicit operation”; eventually showed “Completed explicit inspect-phase” and “Exact orbit complete”. No measured latency claim: snapshots were taken around unrelated tool recovery.
2. `sky.click` on the observed Tools accessibility element failed with `coordinate input geometry is unavailable`. A fresh selection/activation was made; the screenshot interface independently failed with `SetIsBorderRequired failed: 不支持此接口 (0x80004002)`.
3. `sky.press_key(F1)` was received by the application, but its status said “Unsupported physical scan code; no experiment action was dispatched.” This is an automation compatibility finding, not proof that a physical F1 key fails. Read-only inspection confirms experiment input intentionally requires native Set-1 scan codes; the tool's actual sent scan bits have not yet been measured.
4. Standard Tab navigation moved focus from Keyboard to Tools, and Space actually opened the Commands dialog. `02-tools.png` proves that transition. Default context is Operation, with a search field and context dropdown. The listed entries include Edit active operation phase, inverse Prepare, full review, Solve, fixed work sheet, and Macro Base.
5. `sky.list_apps/list_windows` exposes only the main Window object; the owned modal exists in accessibility as index238, not as a returned targetable Window. Sending Tab to the main Window reactivates the disabled owner and leaves the modal without keyboard focus. This blocked further tool-based traversal at this point. `02-tools-accessibility.json` and `02-tools.png` retain the actual state.

The two tool errors above are environment/tool capability failures, not permission denials. They have been reported to the orchestrator for a bounded real-input fallback. No backend action, reflection call, label editing, setup search, or automatic solution was substituted for UI operation.

6. A bounded test-only Win32 SendInput fallback was prepared independently at `tests/native_review_input.py` and documented in the adjacent Markdown. It pins PID/path/executable SHA, checks actual owned foreground HWND, refuses held keys and foreign coordinates, and sends one balanced action at a time. Final helper SHA-256 `4712692032da44c580421c35b57328ee5a862f7250c024b587e7f9a66e65c330`. This is agent automation, not physical hardware input.
7. One activation attempt returned true from SetForegroundWindow but its immediate foreground observation was false. No activation retry was performed. The next actual screenshot (`03-tools-active.png`) and fresh read-only inventory independently confirmed the Commands dialog was foreground with the search Edit focused. This was an asynchronous observation, not proof of a permission rejection.
8. Actual SendInput click at desktop `(1434,425)` opened the observed Command context dropdown. Accessibility showed four options: Selected object, Operation, Macro, All commands. One scan-code End was then inserted (two events, foreground-still-target true). Before its final UI state could be assessed, the next desktop observation showed the user using another application. All input stopped. That unrelated screenshot was removed from this review's exact artifact path; no user file was changed. The final All commands content, E1 entry, and display-control reachability were therefore **not observed**.

Fresh immutable verification at pause: `build-at-pause.json`, `checks.solver_pause`: **110 files unchanged, zero changed/missing**, command exit0. This binds the observed UI to the recorded build. It does not certify the interrupted solver requirements.

## Coverage matrix — wave 1

| Capability group | Actual UI evidence | Current result |
|---|---|---|
| Native startup, solved initial state, Current/Next/protection visibility | Fresh launch; 01-start/02-tools screenshots | Observed; legal scrambled/E1 flow not yet used |
| Command index and context | Tools opened through Tab/Space; context dropdown opened by actual SendInput click | Actual UI used; four contexts observed; All commands final result not assessed after desktop interruption |
| Grip/Twist; physical sets; Functions; OSK; bank edits/files | F1 injection received with unsupported scan status | Not covered; injected F1 is not a physical-key verdict |
| Retained Instant Turn | No legal live turn/inverse yet; parent reports retained source flag only | Not covered; final-state appearance and full input latency must be recorded separately |
| Legal E1 or scramble; New/Resume/timer/completion/history/checkpoint/logs | No state mutation yet | Not covered |
| Current→Operation→After; names/identity/Home/Current/frame | Current solved view visible only | Not covered |
| Macro classification/reasons/compound cycles/reference variants | Command entry visible only | Not covered |
| Goal Prepare/Place/Orient/FinishBuffer/FinishOrbit | No operation selected | Not covered |
| Net/Strict; position/label protection; rejection/recovery/auto protection | Zero protection visibly established | Not covered |
| Cross-orbit locked Next and per-orbit context; worksheets | No target yet | Not covered |
| Local433, transparency, centre, Global, puzzle | No renderer tool opened yet | Not covered |
| Retained hide-framework, observation/display controls, low-detail behavior | No UI route exercised; parent reports missing command-route suspicion from source | Not covered; existing adaptive-during-motion behavior must not be conflated with a permanent low-detail mode |
| Undo/redo/cancel/save/close/reopen | No legal mutation yet | Not covered |

## Highest-impact findings and next correction

| Finding | Minimal reproducer / expected vs actual | Impact and smallest correction | Result |
|---|---|---|---|
| T1 — Sky capture/input geometry unavailable | Launch this native build, request screenshot or click observed Tools element. Expected capture/input; actual `SetIsBorderRequired 0x80004002` / `coordinate input geometry is unavailable`. | Blocks point-and-click review. Keep FFmpeg desktop capture and the guarded test-only real-input fallback; do not change product semantics for a tool defect. | Tooling failed; fallback pointer input subsequently worked |
| T2 — Sky key target/scan mismatch | Send F1 to main Window; open modal through Tab/Space, then send Tab to main again. Expected intended function/modal traversal; actual unsupported-scan status and disabled-owner activation. | Blocks keyboard evidence through this tool. Guarded Set-1 SendInput targets actual owned foreground modal. Do not call this a physical keyboard/product failure. | Sky route failed; fallback dropdown navigation input inserted, final selection interrupted |
| E1 — Active desktop interruption | During command-context exploration, user switches to other application. | Stop input, preserve session, coordinate next review window. | Blocked/interrupted; no product failure |
| P? — Retained display-control entry gap | Root's independent source audit reports no current command route for retained frame/display/adaptive controls. This reviewer reached the context selector but not the All commands list/Help. | Root may perform its bounded source-grounded repair; independently test actual changed entries on the next build. This report does not elevate source absence to an actual UI observation. | Runtime reachability not covered |

There are no substantiated incorrect-action, lost-work, protection-bypass, preview-staleness, or recovery defects from this interrupted slice. Claiming 3–5 product issues would require inventing evidence. No legal scrambled/E1 state was established, no macro selected, and no puzzle-state mutation was performed. No synthetic residuals or direct label edits were used. Existing `evidence/final-continuous-cleanup.json` was only read to understand a lawful future orientation/buffer source; it is not evidence of UI use here.

## Handoff

First next slice remains: establish visible legal E1 → inspect/select macro → full review/preview/explicit execution (or concrete blocker) → one recovery. Follow-up waves retain the capability matrix above, with inherited valid evidence distinguished from actual new interaction. Root owns targeted repair/rebuild and process disposition. Independent verifier should recheck the changed final build before any completion claim. Project memory is updated by the root to avoid concurrent shared-memory edits.

## Wave 2 — offline preparation, awaiting explicit GUI handoff

Root reports the missing display command routes have been corrected and the user has resumed development. This section is a proposed actual-UI sequence only; it does not change any wave-1 coverage result. No GUI tools were used during this preparation. Root currently owns the desktop.

Before input, obtain the new build manifest/executable hash, host and engine PIDs, dedicated disposable session path/current state, launcher lifecycle handle, and an explicit GUI/source-freeze handoff. The existing input helper is still pinned to old PID25540/build39c8; its owner must provide the new pin and helper hash. All window handles, coordinates and focus observations must be refreshed. Do not infer that the old session or old helper now targets the new build.

First ten minutes after handoff (timeboxes guide reporting, not acceptance shortcuts):

| Elapsed | Actual planned step | Observation to retain |
|---|---|---|
| 0–1 min | Read fresh owned-window inventory and capture actual desktop; bind build/session, focus and visible current state | Build/source receipt, desktop/window/DPI, actual initial state |
| 1–3 min | Find the visible Tools/Session route and explicitly load the legal E1 practice fixture in the disposable session | Visible route, confirmation/provenance, resulting Current/Next/protection; label E1 as a synthetic legal practice source |
| 3–5 min | Select a depicted Current and locked Next; inspect a visible macro and compare selection with explicit Add | Mathematical displayed identity, focus/Next distinction, macro selected-state and exact operation before/after Add |
| 5–7 min | Inspect Prepare/Macro/Cleanup and Current→Operation→After; read complete protection result | Committed vs predicted state, selected full operation and protection scope; no automatic execution |
| 7–10 min | Where the chosen legal operation permits it, exercise one explicit protection rejection, recover through a visible control, re-review/preview/commit, then undo | Rejection cause, state/draft preservation, invalidation of old preview, refreshed permission, committed result and recovery |

If the legal source cannot be found within the initial exploration, record the exact visible route/failed expectation and report that access blocker. Do not invent internal IDs, use arbitrary residuals, bypass protection, or substitute a backend action for a missing UI entry. If a real computation exceeds the timebox, record its actual pending boundary and elapsed observation rather than claim the later steps completed. At most three high-impact problems are collected before handing back for bounded repair. Work-sheet reuse follows only if this first execute/recover path reaches it naturally; otherwise it stays explicitly not covered in this wave. Existing valid evidence for unchanged retained display/Instant Turn behavior is inherited, not redundantly retested.

Wave-2 handoff received. The old owned Commands modal was closed with Escape and old sample closed with Alt+F4 through the still-pinned helper. `b01-old-modal.png` / `b02-old-main.png` show the observed controls before those actions. The old native host returned0 and both owned PIDs ended. Launcher session70508 returned1 because frozen inputs had legitimately changed after the previous GUI/source freeze was released; `build-old-at-close.json` preserves this distinction (`run_outcome.exit_code=0`, immutable inputs unchanged=false). The earlier `build-at-pause.json` remains the valid wave-1 input binding. The old helper and documentation were preserved as `input-helper-a.py/.md` before any new pin update. New launch is held pending root's final harness freeze, because `native_launch` also binds `tests/run_postapproval.py`.

## Wave 2 — actual solver slice, completed 2026-09-17 04:57 JST

Question retained: can the unfamiliar solver reach a useful macro from the displayed mathematical Current identity, distinguish selection from adding to the operation, and understand the next preparation step? Root supplied exclusive GUI ownership and a frozen build. This slice stayed within the legal E1 source and visible controls. It did not use setup search, automatic solving, internal ID guessing, backend state mutation, arbitrary residuals, or label edits.

### Frozen artifact and lifecycle

- Launcher: bundled Python, `native_launch.py --mode g2 --session solver-review-20260917-b`. The session name was confirmed absent before creation; retained path is `sessions/g2-solver-review-20260917-b`.
- Build identity `9a8a8e39533dc1795e5dfc180e5cf57e13cf1d4b59caceabfbfadf29013b846a`; executable SHA-256 `fc16e300de8bfaa1b5fe3fae4b5c259c99f47bc32ddf706f6c4a0aa0dcf6beab`; model `58c83f336e451aa64147fe1b8db2d60d7d43806aa3a35122f8403d5af51253f9`.
- Host PID16828; engine PID23296; launcher exec20490. `build-b-before-ui.json` and `build-b-at-close.json` preserve the source/model/toolchain/artifact identities. After-run verification: **111 files unchanged; zero changed/missing**. Host and launcher exit0; immutable inputs unchanged=true; final acceptance remains not-assessed.
- Only the test input helper's PID/path/SHA constants and explanatory documentation were updated for b. Helper SHA-256 `a62246891a4a1db0e7226e3a57cb8249cb87b948b9017293a24eaf62ac0de704`. Its guards were retained. Inventory `b03-inventory.json`; x64 INPUT ABI40/MOUSEINPUT32/KEYBDINPUT24/union offset8 checked. These are test-tool checks, not product acceptance.
- Same 1920×1080 desktop, DPI96, Intel HD620 environment. Actual inputs were guarded Win32 SendInput after observed screen/focus checks; this is agent automation, not physical keyboard hardware evidence. FFmpeg PNGs were inspected individually. No continuous source movie was recorded in this slice; these images do not satisfy the separate final-film deliverable.
- Startup screenshot at04:45:07, recovered Current screenshot at04:57:18, owned process-exit receipt at04:57:38 JST. This wall interval includes tool calls, screenshots and reasoning; it is **not an input/computation latency benchmark**.

### Actual steps and observations

All filenames below are under `evidence/native-solver-review-20260917/`.

1. Fresh main window began solved, with Current/Next not set and zero protection. Tools opened Commands. Selected All commands and searched `E1`. The actual row said **Load explicit synthetic E1 witness into a fresh solved session**. Used that selected action. `b04-start.png`, `b05-tools.png`, `b10-e1-result.png` show entry/provenance.
2. E1 completed visibly. Main showed Current and locked Next mathematical Home/At identities, two residual 2-cycles, a Shared-face Home block, and zero protected orbits/positions. This is the program's fixed legal synthetic practice source, not a personal scramble or a claimed human solve. `b11-e1-loading.png` is the completed E1 screen despite its early filename.
3. Actual Set-1 F3 opened Solve on Macros. Prepare, Macro and Cleanup were Empty. Macro rows were displayed by mathematical structure, and visible rows said **Use not checked**. The Solve window occupied `(660,225)-(1920,1080)` and covered much of the main residual's right side; Current/Next summary lines remained visible. This overlap is an observation, not a separate defect claim. `b12-macrobase.png`.
4. Searched `19 cap`, taken from the visible current-orbit name **1 sticker / 19 cap domains**. Result: **No matches · adjust search or filters**. Cleared the search, selected **Current orbit** from All uses, and again saw the same empty message. Filter opened a separate dialog showing unrestricted Exact action/Affected orbit and Likely use=Current orbit; no unchecked-count or action explaining classification was visible there. `b13-macro-filter.png`, `b17-current-orbit-results.png`, `b18-filter-action.png`.
5. Clicked Clear facets and Apply filter. All uses and the unchecked macro rows returned. This is actual recovery from the empty-filter state, not protection-rejection recovery. `b19-clear-facets.png`, `b20-all-macros-restored.png`.
6. Selected the first visible **2 stickers / 2 cap domains · C2 orientation · Star 0 · for…** row solely to inspect selection/Add semantics. It was not chosen as an E1 solution. The main switched to Operation / Selected macro and displayed several cycle sizes, including7,6,4. Prepare/Macro/Cleanup remained Empty. The row changed to Likely use · Other; the detail area reported applicability issues. Check library became visible lower in the scrolled details area, after selection. `b21-selected-macro.png`.
7. Clicked **Add to prepare V**. The selected macro appeared in Prepare; Macro and Cleanup remained Empty. Current/locked Next summary addresses stayed the same. `b22-added-draft.png` establishes the distinction between selection and adding.
8. Clicked Check B. Main Operation changed to All steps; the Solve result visibly said **Goal not yet met**, while its footer said **Ready**. No Execute or Preview action was taken. This is a checked draft with an unmet goal, not a successful insertion or proof of an unsafe commit. `b23-check-result.png`.
9. Clicked Cancel; main status confirmed **Completed explicit cancel-preview**. There had been no active Preview action, so this does not prove preview invalidation/recovery; Prepare remained present. Closed Solve normally, clicked Current in main, and confirmed the E1 two-piece Current/Next structure and mathematical addresses remained displayed. `b24-cancelled.png`, `b25-main-final.png`, `b26-current-recovered.png`.
10. Closed main through Alt+F4, preserving the disposable session. Launcher exited0, and read-only process checks found neither owned host nor engine. `b27-process-exit.json` and `build-b-at-close.json`. No reset or directory deletion occurred.

### Concrete findings requiring a bounded recheck

**P2 S1 — Current orbit filtering hides unchecked preparation without explaining the prerequisite.** Minimal reproducer: fresh b/E1 → F3 Macros → Current orbit. Expected: either useful classified results or a clear explanation that unchecked entries are excluded, with the existing explicit classification action nearby. Actual: empty list and only “adjust search or filters”; Filter dialog does not explain the missing prerequisite. Clear facets recovers the list, where rows say Use not checked. Check library was later discovered lower in the scrolled macro-details area after selecting an unrelated entry. Evidence: b12,b17,b18,b20,b21. Solver cost: a natural Current-based discovery path looks like no available macro, causing unrelated selection or menu exploration. **Actual-UI preparation/discoverability failure; no mathematical incorrectness claim.** Small correction: show unchecked/pending-reference counts and the existing Check library action beside the filter; keep classification explicit.

**P2 S2 — Searching the displayed current-orbit mathematical name gives no results.** Minimal reproducer: same fresh E1, Macros, search `19 cap` copied from the visible orbit structure. Expected: name-based discovery should include that full mathematical orbit name. Actual: No matches. Evidence: b11,b13. Independently supplied root source analysis explains that the current-orbit prefix is omitted from the displayed short MacroName and search only inspects that short name plus UseSummary; this explanation is **source corroboration from root**, not an extra UI operation by this reviewer. The reviewer did not enumerate the full library to prove every matching entry. Small correction: include the full mathematical orbit name in search without requiring internal identifiers or effect classification. **Observed search failure with source-corroborated cause; recheck still needed.**

No third high-impact product finding is supported by this slice. Window overlap and changing scroll position made context harder to inspect, but did not establish lost work, incorrect execution, or hidden protection. The “Goal not yet met / Ready” pair is recorded for semantic clarity; Execute was not attempted and this review does not infer a bypass.

### Updated capability coverage

| Workflow/capability | Actual wave-2 use | Result / remaining limit |
|---|---|---|
| Fresh dedicated native startup and normal close | New b launcher, solved UI, Alt+F4, both PIDs exit;111-file after-run binding | Pass for this lifecycle; Resume/reopen unchanged session not run |
| Legal state source | Visible E1 command in All commands, explicit load | Pass for fixed synthetic E1 only; scramble, New/Resume/timer/summary not run |
| Current/locked Next, mathematical Home/At, residual cycles | E1 main and Solve summaries, return to Current | Actual UI used; activation, cross-orbit continuity, manual identity selection/frame changes not run |
| Current→Operation→After | Current and selected/full Operation transitions observed | Partial; After/Preview not exercised |
| Macro search/filter/classification reasons | Search, Current orbit facet, Filter/Clear facets, selected macro and Other classification | S1/S2 fail discoverability; Check library itself, chosen references/variants, detailed comparisons not run |
| Macro selection versus Add; Prepare/Macro/Cleanup | Selection kept all Empty; Add populated Prepare only; Check returned unmet goal | Pass for selected/Add distinction; phase editing/reuse and completed insertion not run |
| Goal Prepare/Place/Orient/FinishBuffer/FinishOrbit | Insert remained selected | Not run; no invented endgame or orientation fixture |
| Protection Net/Strict, position/label, automatic exact-orbit protection | Zero protection count visible; draft Check showed End preserved/During unchecked | Settings, nonempty protection, rejected execute and recovery not run |
| Explicit preview/commit, stale preview, undo/redo | Cancel-preview clicked without active preview; Current view recovered | These acceptance cases not run; no commit/undo claim |
| Grip/Twist, sets, Functions, OSK, editable banks, keymap imports | Correct scan-code F3 and ordinary dialog input through fallback only | Functional keyboard/turn/bank coverage not run; not physical-hardware proof |
| Worksheets, cross-Next/context | Work sheet and Next controls visible only | Not run |
| Session history/checkpoint/restore/log, save/reopen | Dedicated session retained after normal close | Not run; file presence is not functional acceptance |
| Local433, transparency/centre/Global/puzzle | Not exercised | Not run in this slice |
| Retained hide framework, low-detail/adaptive observation, Instant Turn | No redundant retest; latest user instructed reuse of unchanged evidence | Inherited evidence remains with root; no new renderer/instant/latency pass from this review |
| Actual source footage/final subtitled demonstration | Actual transition PNGs only | Movie deliverable not run in this slice; root owns final recording |

### Handoff and next independent check

GUI and product freeze released to root after normal closure. Reviewer will not operate the desktop until another explicit handoff. Next bounded check should reproduce S1/S2 on the repaired fresh build, use the now-discoverable explicit library classification, then continue a lawful chosen operation into protection/preview/commit/recovery. The remaining coverage matrix stays open; neither source inventories, prior scripts, nor this partial slice are represented as full actual-UI use. Root owns shared-memory updates to avoid concurrent writes.

## S1/S2 repair — independent review of native run 052216

This verifier inspected `native-baseline/postapproval-g2-20260917-052216/build.json`, `report.json`, the complete `run.log`, the changed blocks in all four product files, and three output images. Root ran the focused native regression under exclusive GUI ownership. No GUI operation, product change or test change was performed during this independent evidence review.

The report has 65 checks and the log has 65 PASS lines; both exit0. After-build verification bound 124 inputs, and before-engine/before-start/after-run each bound 133 inputs unchanged, with no changed/missing files. The actual retained executable hash matches `cc3971af488730c6923b788ed14ccb9d0b1f4c8613b8a45b26f0410c0413b41e`. Model identity remains `58c83f336e451aa64147fe1b8db2d60d7d43806aa3a35122f8403d5af51253f9`. This harness manifest identifies the artifact by source/executable hashes; it does not contain an ordinary-launch build_identity. The four inspected current product hashes and the test hash all match that manifest. Review receipt and reviewed-output hashes: `evidence/native-solver-review-20260917/macro-discovery-052216-review.json`.

| Changed block | Independent assessment |
|---|---|
| `ExperimentShell.cs` DrawMacros | Search adds the complete mathematical OrbitName alongside existing name/use/id/note fields. Checked-use and exact-effect predicates still apply independently. The name search does not confer applicability or select/insert a macro. |
| `ExperimentMacroLibrary.cs` MacroEmptyMessage | Checked-only facets report No verified matches; All/unchecked search misses use No search/filter matches. Global unknown counts are explicitly Library-scoped, with unchecked effects and AwaitingVerification separated. No effect checking or frame mapping is triggered by formatting. |
| `ExperimentWorkspace.cs` libraryTools / DrawWorkspace | The existing Check library command button is now beside the use filter and Filter/Edit. No-selected-macro empty results use the new explanation. The button preserves the original command registry, editable context menu and Macro.Digit1 binding. |
| `ExperimentSolveWindow.cs` details | The old Check library button instance is removed. Existing dynamic measurement of wrapped catalogue action rows remains; the fixed initial row size alone does not indicate clipping. |

Visual checks and actual run observations:

- `macro-discovery-name-before-check.png` is a **GDI control render**, not a desktop capture. It visibly shows search `19 cap`, Star0/Star11 forward/reverse rows still marked Use not checked, no selected macro, and all phases Empty. The real native assertions also passed complete current-orbit-name matching and preservation of Current/Next/draft/protection/use facts.
- `macro-discovery-current-empty-minimum.png` is a **GDI control render** at the declared minimum 780×570. It visibly says No verified matches and Library: 72 effects not checked · Check library. The Check library and Edit buttons occupy the second wrapped row and remain fully visible. Prepare/Macro/Cleanup, Check/Preview/Execute/Cancel and Current/Next/protection footer remain intact.
- Native geometry logs record toolbar height 70 at both 840×570 initial and 780×570 minimum sizes; Check library is 108×31 at local (2,37), Edit is 52×31 at (114,37), all exposed without scrolling. The two-line status needs 43px with 54px available. These are WinForms control coordinates; the actual 1920×1080 desktop capture is displayed separately and is not described as an 840-pixel-wide physical desktop window.
- `desktop-solve.png` is the **actual FFmpeg Windows desktop capture**. Foreground ownership is asserted before and after capture. It confirms the moved button in the visible left catalogue, no selected macro, phases Empty, mathematical Current/locked Next and zero protection. Rows now show classifications after the explicit library action. It is not an actual desktop frame of the earlier empty-filter moment.
- The native button's `PerformClick` completed the existing library check, preserved the bound full state and selection, and made Current-orbit results available from checked facts. All uses plus a nonexistent name retained a search-miss explanation. Macro.Digit1 and the existing visible keycap descriptions passed. This is native harness control/input routing, not physical-key hardware evidence.

**Conclusion:** S1's hidden prerequisite/action and S2's omitted mathematical search prefix are fixed for the original fresh-E1, no-selected-macro reproducer. There is no additional blocking finding in those changed paths from this review. The agent-operated wave-b observations remain the original failure evidence; the repaired behavior is established here by independent review of a source-bound native regression and its rendered/desktop evidence, not by claiming another manual solver trial.

Limits retained:

- AwaitingVerification count was 0. Genuine simultaneous unchecked-effects plus unverified-frame three-line content and its wrapping were **not run**; no fake frame record was introduced. The two-line 54px result must not be generalized to that case.
- `DrawWorkspace` currently invokes MacroEmptyMessage only when no macro is selected. An already selected macro hidden by later filters retains its selected-name/hidden-by-filters readout instead. This source-visible branch was not exercised by the fresh preflight, and its unknown-count discoverability is not certified here.
- Search/classification did not automatically select or add. An actual explicit Add/execute/protection-rejection/recovery cycle on the repaired artifact remains part of the next solver slice. No full solve, final recording, latency acceptance, wider DPI sweep or user G1/G2 approval is inferred.
- Subsequent HubCycles performance changes are a **future affected dependency** for the final artifact. They do not retroactively invalidate this run's unchanged-input receipt. The final integrated artifact must be rebound and relevant rendering/workflow checks rerun after those edits.

Before appending this result, the earlier report was preserved byte-for-byte as `evidence/native-solver-review-20260917/wave-b-review-snapshot.md`; it matches the report hash already recorded by `wave-b-index.json`. The original wave-b evidence/index remain historical. Root continues to own the GUI and shared-memory update.

## Wave c — intended E1 operation prepared; foreground interruption before Check

This actual-UI slice used the exact documented legal witness, after reading architecture S15.1/S15.2 and the corresponding retained E1 fixture/library definitions. E1 is generated chronologically by Star 11 forward then Star 0 forward. The intended two manual steps are Star 0 reverse (56 primitives, A→X→B→A), followed by Star 11 reverse (58 primitives, A→Y→B→A). Initially Current is X at A and locked Next is Y at B. The first commit should put X Home and move Y to A while retaining Y's identity; the second should restore the fixture. These are expected results from the supplied witness, not a claim that this slice executed them.

### Artifact and evidence boundary

- Ordinary launch: bundled Python `native_launch.py --mode g2 --session solver-review-20260917-c`; the dedicated path was confirmed absent beforehand. No personal session was opened.
- Build identity `7f0799dd893827129d0e4e1bd4a6111f7069572868f5107834e62e8b2dd34991`; executable SHA-256 `34c17fc3d7a9ecd3669605373c53bf528785a527e17318890e6dbae29f4eb280`; same verified model `58c83f336e451aa64147fe1b8db2d60d7d43806aa3a35122f8403d5af51253f9`.
- Host PID 22788, engine PID 21788, launcher exec 73759. Test-only SendInput helper was pinned only to this host/path/hash; helper SHA `eddab73a98a9e851e4ec56db5b92ec5ce75490c662f664fd597d9e0ac5deaa07`. Guards remain unchanged.
- Evidence directory: `evidence/native-solver-review-20260917/wave-c/`. `index.json` hashes the retained artifacts; `build-at-gui-stop.json` phase `solver_wave_c_stop` records **111 unchanged inputs, zero changed/missing**, exit0, immediately before source ownership was returned. This is a point-in-time binding, not a completed-run exit receipt. It must not be refreshed against product sources edited after the release.
- Same 1920×1080/Intel HD620 environment and DPI96. The ten retained PNGs are actual FFmpeg desktop captures. They are not a continuous recording or latency benchmark. Owned unneeded/unrelated captures `01-start.png` and `03-ready.png` were removed after exact artifact-path verification; no user file was touched.

### Actual transitions

| Trigger | Observed result | Evidence / verdict |
|---|---|---|
| Launch and owned-window activation | An activation returned true with immediate foreground unconfirmed; a later inventory and actual screenshot independently confirmed the main window. A subsequent capture showed Codex instead, so input paused. Root coordinated one new explicit activation under the continuing GUI authorization; another fresh observation confirmed the native window before input. No foreign application received input. | `01-inventory.json`, `02-active.png`, `04-active.png`; tool/focus boundary, not a security-denial workaround |
| Tools → All commands → search E1 → Use selected action | Visible command explicitly identifies the synthetic witness. E1 loads with Current X, locked Next Y and two residual 2-cycles; protection remains zero. | `05-tools.png`–`07-e1-loaded.png`; Pass for source loading/identity visibility |
| F3 → search `19 cap` | The four intended Star 0/11 forward/reverse entries are found before analysis. Check library is beside the filter and visible. | `08-macros.png`, `09-intended-macros.png`; actual-agent confirmation of repaired discovery route |
| Click Star 0 reverse | Selected macro reports 56 turns, 3 pieces/3 labels, one cycle and no fixed-position frame changes. Operation displays the intended three-piece cycle; all three phases remain Empty. | `10-selected-star0-reverse.png`; explicit intended selection, no automatic Add |
| Click Macro phase | The active phase becomes Macro and the action reads Add to macro. | `11-macro-phase.png`; Pass |
| Click Add to macro | Macro contains the chosen operation; Prepare/Cleanup remain Empty. The visible **Forecast · After Macro** places Current at its Home and locked Next at A. Committed E1 state is still unchanged by this draft operation. | `12-added-star0.png`; Pass for preparation and predicted identity continuity, not execution |
| Attempt Check | The helper refuses before injection: Target is not the actual foreground window. Fresh inventory finds both Solve and the main owned window not foreground, with no focused owned control. No second activation or click is attempted. | `13-focus-after-add.json`; Blocked before full Check |

No full Check, Preview, Execute, protection rejection, undo/redo, explicit Save, close or Resume was exercised. The selected-macro body inspection is not relabelled as complete-operation checking. Forecast Next-at-A is not reported as an actual committed relocation. No new product-correctness failure is established by the lost-foreground event.

### Handoff and recovery entry

Root explicitly instructed that the program may remain alive rather than bypass the foreground guard to exit. Both owned processes were read-only observed Responding=True. GUI and source freeze were released after the 111-file stop receipt; no further GUI input is authorized for this verifier until a new handoff. Root may change source files while retaining this already built executable/engine/model/session; that later work does not retroactively invalidate the stop receipt.

The preserved session is `sessions/g2-solver-review-20260917-c`, with Star 0 reverse in Macro, Prepare/Cleanup empty, Current X and locked Next Y. `progress.json` records that boundary. Resuming this existing process would require fresh owned-window/focus observations and a new explicit handoff; final acceptance on a repaired build requires a freshly bound run. The next substantive step remains full operation checking, then the two explicit commits with protected-X/Next continuation and recovery. No new architectural correction is proposed for a foreground event whose cause was not established.


## Wave d — documented E1 two commits, protection rejection and recovery

Closed at 2026-09-17 06:43 JST. **The bounded actual-UI execution/recovery slice passed; S3 whole-orbit protection discoverability failed.** This is independent agent operation of the ordinary native sample, not a scripted regression, human trial, general solving result, GPU benchmark or final 0.4 acceptance. The program reported whole-model completion of this fixed legal practice fixture after the second explicit commit.

### Frozen artifact, source and lifecycle

- Ordinary command: bundled Python `native_launch.py --mode g2 --session solver-review-20260917-d`; fresh isolated session `sessions/g2-solver-review-20260917-d` remains intact.
- Build `8da86038fbe0cf371497ff2861eef25d09c49d0a3396d271fd1db5f412727430`; EXE SHA-256 `bb64c1b1e5750eb6700e31798e4c42d9ae40f2cf2fbfe12a8fb3881ba9bfa847`. Complete source/model/toolchain/runtime hashes are retained in `wave-d/build-before-ui.json` and `build-at-close.json` under `evidence/native-solver-review-20260917/`.
- Host12992, engine10572, launcher exec18379. Build receipt before engine/start, after run and reviewer close each binds **111 files unchanged, zero changed/missing**; host/launcher exit0. `close-receipt.json` independently records neither owned process remains. Summary, Solve and main were closed by their visible controls. No reset or session deletion.
- Old wave-c was normally closed first; host22788/engine21788 exited. Its historical point-in-time receipt was preserved and was not rebound to subsequently edited sources.
- Same Windows10/Intel HD620/1920×1080/DPI96 environment. ComputerUse Sky supplied real window selection/activation/accessibility. WGC screenshot/coordinate capability failure required FFmpeg desktop PNGs and the approved pinned Win32 SendInput helper. Helper SHA `34d92f031885ed2a7a3601e162d372d8aecb129e9c416d31ac063fbf1078c5a0`; only PID/path/EXE hash pins changed, guards retained. Its exact copy is in this evidence directory.
- Captures from06:06–06:42 JST include tool recovery, screenshots, reading and reasoning, and are not latency measurements. Individual PNGs were inspected; no continuous film or subtitled deliverable was recorded.

### Legal case and expected identities

The architecture S15.1/S15.2 witness uses orbit33, A2712/C7, B175618/C594, X35778/HomeC113 and Y26789/HomeC84. The visible E1 command explicitly creates a synthetic legal practice state by Star11 forward then Star0 forward. Current starts as X at A; locked Next is Y at B. The intended chosen macros are Star0 reverse (56 primitives), then Star11 reverse (58). The first puts X Home and Y at A; the current automatic-Next amendment makes Y Current after that commit. The second restores this fixture while preserving X. These identifiers explain the supplied fixture; macro selection in the UI used visible names rather than internal IDs.

### Actual transitions and observations

All evidence names below are relative to `evidence/native-solver-review-20260917/wave-d/`.

| Trigger | Observed result | Evidence / result |
|---|---|---|
| Tools → All commands → search E1 → Use selected action | Explicit legal synthetic source loads; Current X at A, locked Next Y at B, two residual cycles, no protection | `08-e1-command.png`, `11-e1-loaded.png`; Pass |
| F3 → search `19 cap` → personally select Star0 reverse | Intended named entry found; selected effect shows56 turns. Selection leaves Prepare/Macro/Cleanup empty | `14-macros.png`, `16-star0-ready.png`; Pass; repaired S2 route exercised |
| Select Macro phase → Add to macro | Exactly one Macro step; Prepare/Cleanup empty; After forecast places X Home/Y at A while committed identities stay unchanged | `18-macro-phase-ready.png`, `19-added.png`; Pass |
| Full Check → Preview | Complete56-turn operation says Goal met/Ready, then Staged. No commit from checking or preview | `20-checked.png`, `23-preview-verified.png`; Pass |
| M while focus is main; then F4 and M in Solve | Main input requests Operation focus and does not execute. F4 transfers focus; explicit M commits. Current becomes the formerly locked Y at A, Next clears; old Star0 steps stay retained and stale | `24-executed.png` actually shows the **rejected main-focus M**, despite filename; `25-commit-result.png` is the real first commit. Pass for focus guard/Next continuation |
| Ctrl+Z, Ctrl+Shift+Z in Solve | Undo restores two residual cycles and Y at B; redo restores one3-cycle and Y at A. Current identity remains Y; Next remains unassigned. Operation retained/stale | `26-undo.png`, `27-redo.png`; Pass for this actual journal recovery; not a claim that Undo restores prior UI selection |
| Protection tab, scroll, Orbit boundary | Initial tab has a large blank area; controls only appear after scrolling. Dialog accepts only canonical-orbit JSON, with no mathematical picker | `28-protection.png`–`30-orbit-protect.png`; **S3 fail** |
| Explicit developer workaround `[33]` → Apply; Reuse steps; full Check; Preview | Known fixture orbit becomes protected. Reused Star0 remains the original56-turn operation, not retargeted. Check displays full-operation conflict and worsened selected requirement; Preview refuses with Complete operation violates protection, no current forecast. Current Y remains at A | `31-explicit33.png`–`35-blocked-preview.png`; Pass for actual conflict/rejected-preview behavior **after an internal-ID workaround**, not discoverability |
| Cancel; Orbit boundary `[]` → Apply | Cancel returns committed view and retains draft. Explicitly clearing orbit protection makes the prior assessment stale; no mutation of Y's actual location | `36-cancelled.png`–`39-recovered-protection.png`; Pass for recovery; forbidden Execute itself was not pressed |
| Close Solve → select actual X Home block position → context menu Protect position `/` | Visible context provides the exact-position protection route. After Escape and the displayed slash shortcut, X shows a lock and count becomes0 orbits/1 position | `40-main-block.png`–`47-x-lock.png`; Pass. `45-x-protected.png` is only an unchanged menu after Enter, **not** the protection result |
| F3 → personally select Star11 reverse → Replace phase | Selection first keeps prior Macro; explicit replacement leaves one Macro step, Prepare/Cleanup empty,58 turns, protected X retained | `49-star11-selected.png`, `50-replace-star11.png`; Pass |
| Full Check → Preview → click Execute | Check says Goal met/Ready with1 protected position; Preview says Staged; Execute commits and automatically opens Solve summary | `51-second-check.png`, `52-second-staged.png`, `53-second-commit.png`; Pass |
| Close summary/Solve and inspect main | Current Y's At equals Home; Next not set; Exact orbit complete; X at Home; protection1 orbit +1 position. Summary reports all259800 labels equal Home/all35 fixed orbit frames verified, source Practice journal, timer0 paused, journalstep3, solution228 primitives/4 stars/3 operations | `53-second-commit.png`, `54-main-complete.png`; actual UI completion report, **not a new independent full-label oracle**. Counts include the fixture journal; do not relabel as a114-turn timed human solve |
| Normally close main | Launcher/host exit0; both owned PIDs absent;111 immutable inputs unchanged | `close-receipt.json`, `build-at-close.json`; Pass; Resume not run |

### S3 — whole-orbit protection requires developer identifiers

**P2, actual usability/requirement failure.** After the first E1 insertion, open Solve → Protection. The working orbit has a readable mathematical name, but the first viewport leaves controls below a blank area. Scroll to Orbit boundary and open it: the only editor asks for “Canonical orbit IDs JSON (0..34)” and provides no mapping or named choice. A mathematically competent unfamiliar solver cannot establish the intended whole-orbit protection from the presented identity without external developer knowledge. Evidence28–30 shows the exact boundary; `[33]` in31 was explicitly authorized as a developer workaround for this known witness and does not pass discoverability.

Expected: reach the existing protection action in the initial usable viewport and choose the working orbit by its mathematical identity, while retaining canonical IDs only as payload. Small correction: remove the unused spacer and replace the raw-ID-only editor with a compact named selection on the same route; preserve explicit Apply/Cancel and global protection state. Root accepted this bounded correction. Recheck on a fresh corrected build: choose the named working orbit, Cancel preserves prior state, explicit Apply protects exactly that orbit, then reproduce old-macro conflict and explicit clearing without typing IDs.

Exact-position protection is reachable by the visible Block context action and was used successfully. S3 does not claim all protection routes are missing. No evidence of label corruption, protection bypass, accidental commit or lost draft was observed. No further high-impact product issue is established from this slice.

### Focus, overlap and interpretation limits

Initial helper `SetForegroundWindow` returned true without establishing immediate foreground. During the precommit pause, the read-only10-second timeline recorded38 samples over9.719seconds, foreground ChatGPT PID9076 throughout and unchanged last-input tick. It establishes no new input during sampling; it does not identify who or what changed focus. The official fresh `sky.list_windows` → `sky.get_window` → `sky.activate_window` succeeded. Fresh native state/inventory/FFmpeg then confirmed the target before guarded input resumed. No input was sent to ChatGPT, no rapid activation loop or permission-denial bypass occurred. The focus pause is a tooling diagnostic, not a product correctness finding. Unrelated desktop captures were removed only from verified owned evidence paths.

Solve overlaps the main diagram's right side; its own Current/Next/protection footer remains visible. Closing it exposed the complete committed layout. Context-menu Enter left the observed Protect position menu unchanged; the advertised slash action then worked. This isolated automation observation was not diagnosed as a physical-key defect. Snapshot delays and transient partial painting occurred amid concurrent offline work and capture overhead; no percentile or device-latency conclusion is drawn.

### Updated capability matrix and handoff

| Capability | This wave's status | Remaining boundary |
|---|---|---|
| Legal E1, mathematical Current/Home/At, same-orbit locked Next | Actual UI used / Pass | No arbitrary scramble or cross-orbit Next tested here |
| Macro search, explicit selection/Add/Replace, fixed reused steps | Actual UI used / Pass | Check-library bulk action, chosen frame variants, comparison and editing inherit separate evidence; not replayed here |
| Current/Operation/After forecast, full Check, Preview, explicit commits | Actual UI used / Pass | Staged preview invalidation by unrelated edits and rejected Execute itself not run |
| Enabled net protection, rejected Preview, Cancel and recovery | Actual UI used / Pass with `[33]` workaround | Named whole-orbit entry failed S3; Strict/each-turn toggle and position-only vs full-label semantics not compared |
| X exact-position protection and automatic exact-orbit protection | Actual UI used / Pass in this witness | Other orbits/protection combinations not generalized |
| Undo/redo, operation reuse, normal close | Actual UI used / Pass | Explicit Save, reopen/Resume, checkpoints/restore/log export not run |
| Completion summary | Actual UI opened automatically and inspected | Timed scramble, saved-log action, reopen-summary behavior not run |
| Goal Prepare/Orient/FinishBuffer/FinishOrbit, unfinished nontrivial orientation | Not run in this wave | Deferred to corrected integrated build and existing verified legal sources |
| Worksheets, editable banks/Functions/OSK/physical Grip/Twist, Local433/Global/puzzle | Not run in this wave | Separate valid evidence stays separate; visible controls are not coverage |
| Unchanged hide-framework/low-detail/Instant Turn | Inherited evidence only as user requested | No redundant renderer or timing claim |
| Human full solve, final subtitled video, final package/native acceptance | Not run | Bounded agent fixture completion does not replace these deliverables |

GUI and source freeze returned to root only after normal owned shutdown and immutable binding. Root owns the S3 repair, final integration and shared-memory update. Next independent UI slice should first recheck the named protection boundary, then resume remaining corrected-build scenarios. Historical wave-c receipts remain valid for their limited point in time; later source changes must not be used to refresh or relabel them.
