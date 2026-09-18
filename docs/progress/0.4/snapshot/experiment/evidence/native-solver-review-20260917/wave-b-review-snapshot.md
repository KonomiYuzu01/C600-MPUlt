> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Independent native solver review — 2026-09-17

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
