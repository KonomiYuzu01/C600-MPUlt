> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Magic 600 Cell native development samples

Status: P1 development. G1 and G2 are independently runnable, but neither has been approved. This guide does not declare the entire architecture implemented or request production integration. Browser launchers are older internal experiments.

## Run and identify

- `Open-Native-G1.cmd`: physical-input harness and retained MPUlt viewport.
- `Open-Native-G2.cmd`: graphical workbench and on-demand linked native views.
- `Open-Native-G2-Instrument.cmd`: the current shared-block graphical scene in a separate `g2-instrument-review` session. The supplied review session contains the explicit E1 practice witness; relaunch resumes your work.
- `Open-Native-G2-Refinement.cmd`: the older `g2-graphical-refinement` session. Existing G1/G2 review sessions are preserved.

The default entries use `sessions/g1-native-review` and `sessions/g2-native-review`; Instrument and Refinement use the separate directories named above. Closing the native window stops its owned engine. Relaunch resumes that isolated session. Original launchers, immutable model assets and personal solves are untouched.

The launch requires Windows, the bundled 64-bit Python with NumPy, the local x86 .NET Framework compiler, retained MPUlt runtime and matching Managed DirectX assemblies. The launcher downloads and installs nothing. Exact source/executable hashes are in `native-build/<content-hash>/build.json`; the directory printed during the launch identifies the sample. Do not use a report from another build as evidence for the running version.

Current identified sample: `a156c3e260d97336` (validation recorded below). Its title includes that ID. An untouched startup focuses the existing Keyboard button before loading begins; Enter opens it, and work shortcuts do not start under text-input suppression. Loading completion does not reclaim focus if you choose another control or application. The safeguards while actually editing text remain intentional.

## G2: explicit two-step block example

This example is a synthetic legal witness. It demonstrates operations, not human completion probability.

1. Open the Instrument launcher. Its prepared practice session already has the E1 block. In a different fresh solved experimental session, open **Tools** (`F1`), choose **All commands**, search `fixture-e1`, and explicitly activate it. Do not reset an unfinished review merely to follow this example.
2. Inspect Current **I35778**, Next **I26789**, buffers **P2712 / P175618**, and Home requirements **P35778 / P26789**. Selecting a token inspects; selecting a socket inspects a fixed position.
3. Select **Macro** (`Ctrl+2`) and set **Turns to draft**. Open Macro Base (`F3`), search exact record `o33-n0-inverse`, select it, then choose **Add** (`Ctrl+M` from the workspace). Selection inspects the fixed effect; Add inserts it. Search owns text input; close it with Escape or return to the graph before using work shortcuts. The chosen inverse has 56 legal turns. Adding keeps existing steps; editing the phase is a separate action (`F4`).
4. Compare **Actual / After Prepare / After Macro / After Cleanup**. The latter three inspect your finite steps without executing. Hollow tokens represent forecasts and allow identity-safe inspection; they cannot be used as actual-position protection or Grip captures. Solid tokens retain actual pieces. Open **Operation** (`Ctrl+R`), **Check**, then **Preview**. **Cancel** keeps the draft and actual state. The persistent protection strip and **Findings** (`Shift+F12`) separate the declared boundary, final preservation and intermediate motion. A macro-body arrow is not a complete-operation certificate.
5. Review/stage again if needed, then explicitly **Execute**. I35778 must occupy P35778. Open that object's action menu and choose **Protect position**.
6. Explicitly **Activate Next** (`Ctrl+Alt+N`). It becomes Current I26789; S02.3 clears the consumed Next bookmark. It does not select another Next or a solving macro. The completed recipe remains marked Executed; only Actual is available until you explicitly start another operation.
7. Choose **New operation** (`Ctrl+N`) to empty all three phases, then choose `o33-n11-inverse` and **Add** in Macro. Check all effects, preview and execute this 58-turn inverse. Both Home requirements must be met, with the first protected requirement preserved. **Reuse steps** (`Ctrl+Alt+R`) is a separate choice that retains the exact recipe and requires a fresh check; it does not retarget or choose a macro.
8. Inspect the result, then try **Undo** and **Redo** through Commands. Check identities and the protected position again. Save a checkpoint before exploring unrelated operations.

On a token/socket, Enter inspects, arrow keys move focus and Shift+F10 opens its explicit actions. Before/After, selection and role assignment execute no turns. A pending menu waits for exact inspection; it must not act on an old selection.

The central image uses one common projection of the block/Target hosting cells. Shared vertices and faces are actual incidence; cell regions are not exact cut-piece centres or an orientation proof. Click a hosting region to inspect its fixed position; where several positions share it, choose explicitly. A/B remain preparation positions. Curved dashed arrows show the selected exact cycle, separate from plain geometric edges and fixed-position leaders. **Entry slots** opens the exact, read-only slot/label correspondence when a draft is available.

**Macro body / All steps** changes the displayed effect scope without editing the operation. After checking a complete operation, return to Macro body to inspect the same selected macro without finding it again. Unknown or stale All steps does not silently substitute the macro body. **Actual Local / Actual Global** use committed state even when the hub shows a forecast; their readout explicitly identifies that boundary. Projected linked geometry is not implemented in this sample.

Work-orbit switching retains declared position protection, including hidden or inactive contexts. Bank switching changes the keyboard only; it preserves the selected work orbit and draft. Explicitly release a position to remove its protection. Neither changing views nor browsing macros releases it.

## Keyboard and Solve checks

- Graphical object actions, **Find command** (`F1`) and the keyboard use one command registry. `Ctrl+F1` returns to graph actions; the command picker starts with a small contextual set, with **All commands** available explicitly.
- `Ctrl+1 / 2 / 3` selects Prepare / Macro / Cleanup. It does not change live/draft destination or execute anything.
- `Ctrl+Alt+1 / 2` selects the current bank's first/second fixed macro. It does not append or execute it. Check the named record and its effect, then append explicitly.
- `Ctrl+Shift+I` computes Cleanup from the inverse of the explicitly supplied Prepare. It does not search for preparation.
- `Ctrl+R` focuses the operation controls. There, `Ctrl+Enter` previews and `Ctrl+Shift+Enter` executes; holding Enter must not acquire a new action by changing modifiers. These are separate from Enter in text fields or Local/Global.
- **Stop check** / `Shift+Escape` requests cancellation. **Cancel preview** is a different action. Actual long-running native job cancellation remains to verify; the input route has focused tests.
- Open **Keyboard** (`F9`) inside the graphical hub. It shares the bottom work region with **Operation**; both are never stacked. Switching input phase keeps the keyboard visible; explicit **Add macro** or **Check** opens Operation. Switch bank A/B/I and choose live/draft through its input menu. Closing the region preserves drafts and Next.
- The keyboard's two Grip rows show physical key positions, exact cap IDs and original cell colors. Select a cap, then an H/T Twist. **Click inverse** affects onscreen clicks; physical **Shift** controls physical-key inverse. Hold/Latch changes explicitly release the previous grip. Unassigned slots are labelled, not silently mapped.
- Current/Next and protection remain in the graphical header above the keyboard. The keyboard shows the exact bank, selected cap/frame or **Set grips first**, and acknowledged input destination. Both Grip rows, all seven Twists and pointer inverse are available together. **More** retains additional custom Grip bindings, the full command index and advanced configuration. An uncaptured insertion bank is intentional: use **Set grips** explicitly or choose a configured bank; it never silently adopts caps.
- Choose **Change key** in the keyboard to edit inline. Select an action and bank/shared scope; focus the capture surface and press a new chord. **F5** demonstrates an existing command conflict; **F11** is available in the default test configuration. Inspect the old/new binding, then **Apply key** or **Cancel**. Grip/Twist keys require a modifier chord. Escape from any editor control cancels. Graph/orbit/catalogue selection is temporarily disabled while the edit owns input, but remains visible. Closing returns graph focus only while this application remains foreground; asynchronous completion must not reactivate a background window.
- **Add inspected current state as a block requirement** captures exact current labels, including a piece at Home with the wrong orientation. **Add to Home block** requires the Home state. Neither enables mechanical protection; protect explicitly.

Use **Change key** in the command picker, or right-click an action. The capture editor shows the old/new physical key, current-bank or shared scope, and actual conflicts. Capture never executes a turn. Advanced full-record editing remains secondary. The phase editor accepts finite explicit turns; typed turns must be added before Save, so partially entered text cannot silently disappear. It cannot open through a lane double-click while a request is busy, and it rejects a changed draft/state base instead of overwriting later work.

The bank picker shows five real banks for the chosen orbit. Type an exact bank ID such as `33-I` to switch deliberately. Orbit descriptions use verified mathematical structure; these bank IDs remain exact keyboard addresses. Four orbit pairs share the same count/group summary. Their separate canonical records and actual cap memberships remain inspectable in details; a same-frame graphical discriminator is still missing. This sample does not invent chirality labels.

Native graphical reference selection, complete work-sheet input binding and all endgame classes still require implementation and verification.

## Piece Filter in the views

Open **Piece Filter** with `F5`. The four presets edit the expression only; **Apply filter** is explicit and **Cancel** preserves the existing rules. Current identity uses `piece(n)` and follows the physical identity. Current position uses `position(n)` and stays at that fixed position. `active` means the working orbit, independently of the keyboard bank. `cell(C55)` uses an explicit canonical hosting cell. Boolean `&`, `|`, `!` and parentheses remain available.

Union/Intersect/Subtract combine with the actual applied single-expression filter, including on resumed sessions. A styled or multi-rule configuration requires explicit Replace; it is never flattened silently. Incorrect syntax and out-of-range new piece/position arguments keep the typed input and old rules. Full legacy rules remain visible in the tool.

The hub dims excluded identity tokens and their color strip, marks them with ×, and preserves their identity, fixed position, role and keyboard entry. Unknown evaluation uses ? and does not pretend the object is excluded. Actual and forecast records use their own state: in the explicit20-cell C55/T1 example, `piece(17810)` follows I17810 from P17810 to predicted P5060, while `position(17810)` keeps the occupant at P17810 selected. Filter changes do not change the actual puzzle or choose a target.

The actual Puzzle keeps its exact existing filter/interaction mask; safety/context pins retain their existing noninteractive behavior. Its retained ghost appearance is opaque grey, not verified DirectX transparency. Local/Global render cell structure: cells with zero matching stickers become faint; their cell outline remains selectable as a reference. Counts refer to stickers in each cell and must not be summed as distinct pieces. These views do not draw individual cut-piece shapes.

Try an empty filter `none`, cancellation, a compound filter and a bank switch. Current, locked Next, the operation draft, Local selection and complete protection must remain. Hidden or faded work still participates in exact protection checks. Fading does not certify safety.

## G1: current development scope

Use the native G1 entry separately and open **Keyboard** (`F9`). The twenty visible Grip slots refer to explicit canonical caps; H1–H3/T1–T4 buttons execute their exact legal words in the chosen draft/live destination. Test camera/filter stability, hold/latch release, bank switching and inverse feedback only in this isolated session.

**Ordered-frame capture is not yet implemented in this native sample.** Cell-only captures currently use the retained cap basis. The mathematical frame audit is evidence for implementing the missing editor, not user approval of G1. Do not approve this sample as a complete G1 checkpoint on the strength of cap-only input.

## Current limits and evidence

Some secondary work editors still expose explicit JSON instead of the intended graphical editing flow; native per-orbit discovery, reference manipulation, endgame and recovery coverage are incomplete. G2 visual/interaction review is still being iterated. The code does not search setup or choose solving macros.

Native control renders are labelled GDI; they are not desktop screenshots and do not capture DirectX pixel output. The current capture tool reports `SetIsBorderRequired / E_NOINTERFACE`. Original renderer readiness, mathematical replay, control interactions, hardware rendering and human trials are separate evidence. See `DEVELOPMENT_LOG.md` and each run's report for what actually ran and failed.

G1 and G2 require separate explicit user decisions on identified native builds. Neither internal tests nor approval of one authorizes production integration or public release.

## Design-tool study

The Figma keyboard study (private design reference omitted) contains the keyboard and inline-edit layouts. It is an editable vector study, not a native screenshot or model certificate. The existing Lucid workflow (private design reference omitted) is open; its first-use role/frequency questionnaire prevents this iteration from editing it. Complete that profile in the official page. User profile answers were not inferred. The exact planned keyboard branch is ready in `docs/specs/keyboard-dock-lucid.mmd`.


## This keyboard iteration: verified evidence

Sample `a156c3e260d97336` is an isolated development iteration. The final Native compact/wide case `workflow-20260915-223319` passed311 assertions and matches all22 application/four backend hashes. It covers actual1000x650 and1280x720, complete key/chord labels, explicit C555/T1 plus inverse draft clicks, and a full-model zero-net-action check without commit. The full two-insertion workflow passed413 and staged-keyboard preservation passed88 before the final presentation-only refinements; the review log identifies these source boundaries precisely. Final header rendering/toggle fixture passed61 checks.

Representative final GDI renders: `native-baseline/workflow-20260915-223319/compact-keyboard-gdi.png` and `wide-keyboard-gdi.png`. Inline-edit render: `native-baseline/workflow-20260915-220112/08-command-key-editor-gdi.png` (predates the final inverse/header paint changes). Actual startup/UIA evidence and hash comparison: `native-baseline/instrument-launch-a156c3e260d97336`. These do not establish human usability, physical-key rollover, high DPI or input-to-GPU latency. G1 and G2 both remain Pending.
