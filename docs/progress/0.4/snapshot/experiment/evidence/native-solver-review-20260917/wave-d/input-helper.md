> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Native review input assist

Test-only, now pinned to PID **12992**, `native-build/8da86038fbe0cf37/Magic600Experiment.exe`, SHA-256 `bb64c1b1e5750eb6700e31798e4c42d9ae40f2cf2fbfe12a8fb3881ba9bfa847`, disposable session `g2-solver-review-20260917-d`. Pin-only update by the independent verifier after root's explicit GUI/build handoff; all guards remain unchanged. Current script SHA-256 `34d92f031885ed2a7a3601e162d372d8aecb129e9c416d31ac063fbf1078c5a0`. Reusing a PID or replacing the executable is refused. A later sample requires a separately reviewed pin change. This is automated Win32 input, not physical hardware testing. Historical HWND examples below are syntax only and must not be used for this new process.

The independent native verifier owns every actual action. First obtain a fresh screenshot and this read-only inventory. Use the actual current top-level/dialog HWND; do not reuse the examples after a window has closed.

```powershell
$py = '<user-home>/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $py -B -X utf8 tests/native_review_input.py layout
& $py -B -X utf8 tests/native_review_input.py inventory
# Dry-run only: verifies the target/foreground guards and creates no input.
& $py -B -X utf8 tests/native_review_input.py --hwnd 0x1420a6c key ESC
# Explicit own-window activation: verifier only, if the freshly inventoried dialog is not foreground.
& $py -B -X utf8 tests/native_review_input.py --send --hwnd 0x1420a6c activate
# Take a fresh screenshot after activation, then send one action only to that foreground target.
& $py -B -X utf8 tests/native_review_input.py --send --hwnd 0x1420a6c key ESC
& $py -B -X utf8 tests/native_review_input.py --send --hwnd 0xREPLACE key A --modifier CTRL
& $py -B -X utf8 tests/native_review_input.py --send --hwnd 0xREPLACE click 900 500
& $py -B -X utf8 tests/native_review_input.py --send --hwnd 0xREPLACE text 'Explicit text'
```

Coordinates are measured physical virtual-desktop pixels from the fresh screenshot. The click example is syntax only, not a verified button coordinate. The helper sets DPI awareness on its own short-lived thread; it does not change display settings. Inventory gives exact owned-window title/rectangle/DPI, enabled/visible/foreground state and focused control class, without reading control text.

Guards before input: live pinned process image and content hash; chosen owned visible/enabled/unminimized top-level HWND is actual Windows foreground; no held modifier/Windows/mouse buttons. Click additionally requires WindowFromPoint to hit that exact owned foreground root. Unicode additionally requires a focused Edit/RichEdit belonging to that root. Every action is one balanced SendInput batch; there is no implicit activation, retry, loop, direct message delivery, reflection, backend request, clipboard access or security workaround. The separate explicit `activate` command calls standard SetForegroundWindow once after the same process/window checks, without requiring it already foreground. It reports both API return and actual foreground equality. A refusal or immediate unconfirmed result stops input. If the API returns true but foreground has not yet changed, take a fresh read-only inventory/screenshot; that later observation, not another activation retry, determines whether input can proceed. No AttachThreadInput, AllowSetForegroundWindow, elevation or foreground-policy changes are used.

Keys: A–Z, digits, F1–F12, ESC/TAB/ENTER/BACKSPACE/SPACE, arrows/HOME/END/PAGEUP/PAGEDOWN/INSERT/DELETE, explicit punctuation names in the script, optional CTRL/SHIFT/ALT. No Windows key; OS task/security chords are refused. Text accepts at most 256 printable Unicode characters; controls/newlines require separate explicit keys. Scan-code typing follows physical positions, so use Unicode only when literal text is intended.

SendInput cannot atomically lock foreground focus across OS event dispatch. Guards run immediately before the single batch; output records whether foreground still matches afterward. Any partial insertion or subsequent uncertain focus must be inspected before another action, not blindly retried. UIPI or another explicit security rejection stops this route; the script never elevates, disables protection, or changes target to work around it. Application behavior can close a window legitimately; accepted insertion alone does not prove the intended function completed. Verify the next actual screenshot and state.

## Validation performed before handoff

No injection. x64 ctypes layout: INPUT40 / MOUSEINPUT32 / KEYBDINPUT24 / union offset8. Six inline structural checks passed: layout, balanced Ctrl+Shift+A, extended-arrow flags, unsafe route rejection, Unicode surrogate balancing, and control-character rejection. Read-only inventory found modal `Commands for current work` HWND `0x1420a6c`, rectangle `[438,233,1482,944]`, DPI96; main `0x2ce08cc` was disabled by that modal. Neither was foreground at that observation. This is a point-in-time observation, not a permission to target either without checking again.

Activation addition was syntax-checked only; no activation was performed by the helper author. Script SHA-256: `4712692032da44c580421c35b57328ee5a862f7250c024b587e7f9a66e65c330`. No physical-key, GUI behavior, current-window geometry after handoff, or GPU evidence is claimed.
