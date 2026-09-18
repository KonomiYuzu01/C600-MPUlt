> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Keyboard dock review cycle — 2026-09-15

Scope: isolated G2 native keyboard/settings integration; G1 remains a separate pending review. Root owns Shell/Workspace integration, keyboard reviewer owns Keyboard/Tools then workflow tests, interaction reviewer owns Hub, solver reviewer reads without edits. Shared native UI belongs to root.

## Decisions and concrete defects

| Trigger | Evidence and cost | Correction | Verification state |
|---|---|---|---|
| Keyboard open at minimum window size | Hub originally spent93px on three header rows; a second permanent dock would crowd the work objects | One shared work region, Keyboard or Operation; Hub header46px at the measured fixture sizes |39 Hub layout and64 safety assertions on C7D89D8A; later readability correction requires fresh affected evidence |
| First display of locked Next | Owned hidden-form probe showed zero-width Activate Next because inherited Visible was false | Allocate from acknowledged Next presence rather than parent visibility | Reproduced failure, then39 passing header assertions |
| Lane editor opened during outstanding Append | Source path bypassed RunCommand busy guard and could later overwrite the adopted append | Entry guard plus original-phase/state comparison before Save | First native214026 run passed direct-entry rejection and stale-save preservation |
| Orbit/macro/object selection during inline capture | Local selection could change before SendRoute refused its request; old macro arcs remained | Disable orbit/catalogue/Hub for editor scope; retain input and request guards | First native214026 checks passed control locks and full work preservation |
| Escape on Apply or conflict control | Only capture and ComboBox handled Escape | Editor-subtree Escape handling; reset held input; return graph focus if still foreground | First native214026 passed Escape on Apply and separate-background-window close |
| Save finishes after switching applications | Unconditional FocusWorkspace activates the host | Return graph focus only when host remains foreground | Source-reviewed; close-in-background fixture passed. Exact delayed asynchronous save is not independently reproduced |
| Inline edit readouts became nearly black | Root and independent reviewer inspected08-command-key-editor-gdi.png | Preserve disabled-state Hub readout contrast while keeping interaction disabled | Correction in progress at this point; not yet final evidence |
| Test calls New operation after first insertion | Runtime test selected both the visible Operation action and the hidden Keyboard action | Test lookup must resolve the visible page, retaining both legitimate page entries | First run failed here; no app behavior was inferred from the test selector error |

A suspected input-phase page-switch defect was withdrawn after reading the full method: ShowOperation belonged to Add macro, not ChooseInputPhase. No unnecessary application patch was made.

## Design tools

Figma existing file PfDqOqBB3f3WNDxzVoZkCB now contains the keyboard and inline-editor vector layout at node4:2. Actual browser canvas was inspected; no Figma AI prompt was sent. This is a design study, not a running application or geometry certificate. Lucid existing workflow eacbff8c-5739-46d4-8c8f-8af654ccda86 was reached, but a first-use role/frequency questionnaire prevents editing. Its answer must come from the user. One accidental duplicate editor tab was closed; original Documents tab and one editor retained.

## Evidence limits

First integrated build2a84c5ce515b8896 compiled. Native run workflow-20260915-214026 reached the first insertion and Next activation, then failed on hidden-control ambiguity in the test. It is not an overall pass. The purported compact image measured1262×679; it does not establish1000×650 behavior. New test must record actual dimensions and WindowState. GDI images do not capture DirectX output or establish physical-input latency, high DPI, RTX4070 performance or human usability.

## Final integration and sizing evidence

- `workflow-20260915-220112`: exit0,413 assertions using real WinForms/MPUlt and full-model transaction routes. The E1 two-insertion cycle, first-block preservation, Next activation, cancellation, undo/redo, filters, input ownership and inline capture all passed. These are assertions, including containment checks, not413 human scenarios.
- `workflow-20260915-220613`: exit0,88 assertions. An explicitly chosen E1 inverse was reviewed and staged; keyboard open/close, foreign-orbit bank/return, inline Escape and F11 Apply preserved the exact pending token, public certificate, review guard, executable state, full mechanical hash, Current/Next, roles, reference, block, phases and protection. No commit was performed; explicit final Cancel retained the original Actual state. Injected scan codes are controlled native-message evidence, not physical hardware typing.
- The initial compact regression `214831` correctly failed: retained loading enlarged MinimumSize to1278x718. `215801` isolated the transition from the retained form's legacy font autoscaling: minimum1000x650 before load,1278x718 atShown. An explicit post-load minimum restored actual1000x650 without state change. The fix applies the experiment minimum in Shown, matching the retained NativeHost boundary; autoscaling and production source were not disabled or changed. The220112 report confirms actual1000x650/client984x611, Normal, and minimum1000x650.
- Hub disabled-state text: the focused before fixture reproduced10 failures; final focused fixture passed51. Final220112 image08 was inspected independently and keeps Current/Next, phase and protection readable while editor input owns the workspace.
- Independent final image inspection identified a remaining compact inverse label wrap and clipped Macro1/2 shortcut suffix. Root changed only the G2 action-row width allocation and label to `Click / inverse`; the physical `Shift: inverse` hint remains. A focused compact Native rerun is required for this final presentation patch. No handler, command or mathematical state changed.

The full and preview suites use identical22 application sources and4 backend sources. Final application Keyboard SHA2769F1FC3D7B545EE7D22EEEB1EEE1430D7FF19E2E7279EF5CB15CCE802AD890 differs only in those two presentation lines; final affected evidence is recorded next. Source review found no new event/disposal/focus defect; this is not substituted for rendered evidence.

No unresolved reviewer disagreement remains within this keyboard integration scope. Remaining important product limitations are command adoption latency (measured examples about0.66–1.51s including polling), unfinished native ordered-frame G1 capture, graphical reference/work-sheet fields and endgame coverage. The host is1280x720/DPI96; requested2560x1600 remains clamped1300x740. No high-DPI, RTX4070, input-to-GPU-frame or human-trial claim.


## Compact presentation correction verified

`workflow-20260915-221850` passed311 focused assertions, exit0, with final Keyboard2769F1FC. The actual1000x650/client984x611 and1280x720/client1264x681 controls were rendered. Complete inverse wording and distinct Ctrl+Alt+1/2/Ctrl+M lines passed actual-font measurements and independent image inspection. Every20 Grip/seven Twist control retains at least46px hit targets. The explicitly chosen C555/T1 forward and inverse were appended as two legal Prepare words; complete-effect review found zero untruncated label-pair changes. Full mechanical hash, Current/Next and work intent remained unchanged; nothing was staged or committed. This is agent-invoked native-control evidence, not physical pointer/keyboard testing.

Final inspection also found the existing Show after overlay header checkbox clips its text vertically at the23px header height. Its control and event route remain present; this is a rendering defect, not evidence of unauthorized or failed execution. A narrowly scoped paint correction is in progress. Do not claim221850 compiled that later Hub correction.


## Header comparison correction

The remaining23px Show/Hide after overlay CheckBox now paints its text using the same compact convention as adjacent phase buttons, while retaining native Checked/CheckedChanged, focus and keyboard behavior. Header layout, eligibility and preview semantics are unchanged. Final Hub SHA B090B0045032101348E528AB18363426F96A91C228DD5C27E652084B4A2BA430.

`native-baseline/hub/comparison-text-final` passed61 focused checks, including two widths, whole labels, and two actual CheckBox OnClick invocations reaching Predicted without changing the supplied snapshot or sending commands. The initial raster-equality assertion was invalid: saved control and reference images have identical glyph extents13,7..124,19 but different edge colors. The test now checks occupied glyph-row coverage plus measured fit; the before-clipping failure and rejected exact-RGB probes remain preserved in comparison-text-before/comparison-device-probe. Both root and the independent reviewer inspected the complete final phrase. This evidence is owned hidden WinForms/GDI, not a desktop/hardware or human trial.


## Delivered iteration

Final sample `a156c3e260d97336`, executable SHA256 `704eb7ddf5717eb16f47ed28a7db0fe9001ac59b497c1ccadf400258f1e8c057`. Final `workflow-20260915-223319` passed311 focused assertions, with all22 application and4 backend source hashes matching the sample. Its compact/wide images were inspected; full inverse/chord/header labels are visible. Earlier full413 and staged88 results predate only the two documented presentation changes; they are not falsely labelled as executions of the final file hashes.

The sample was actually started and resumes the existing `g2-instrument-review` session. UIA confirms the identified build, full259800/1200 native mapping, CurrentI35778 atP2712, locked NextI26789 atP175618, and20 Grip/seven Twist controls inside the same window. The current33-I bank still explicitly requires cap capture. Immediate post-input accessible snapshots were temporarily empty/stale; reselecting the supported window object restored the complete tree. Process Responding was true, stderr empty; no alternate UI mechanism or input-guard weakening was used. Those observations do not establish physical-key latency.

There is no unresolved reviewer disagreement in this bounded integration. Highest remaining product bottlenecks are the ordered-frame G1 workflow, graphical reference/work-sheet/endgame completeness and command-adoption latency. G1 and G2 remain independently Pending; no production integration occurred.
