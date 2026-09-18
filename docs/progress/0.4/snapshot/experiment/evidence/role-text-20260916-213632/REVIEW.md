> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Solve role text: reproduced clipping and minimal correction

The three strings and 259/261 × 54 px bounds came verbatim from the current
native run `postapproval-g2-20260916-213037/run.log`. They contain complete
mathematical locations. No model or application window was opened by this test.
The fixture created hidden native handles and used `DrawToBitmap` with the
actual Segoe UI 10 pt, Flat button, top-left text, padding 5 and rounded-paint
settings. No `Form.Show` was called.

Thirty role cases were rendered. Exit 0 means the diagnostic completed, not
that the original UI passed. The original clipping was reproduced:

| Setting | Actual mathematical suffix |
|---|---|
| GDI, 54 px; LF or CRLF; rounded on or off | Absent, zero lower-line pixel difference from header-only |
| GDI+, 54 px | Visible in the inspected samples |
| GDI, 78 px | Complete in the inspected samples |
| GDI, native constrained preferred height = 60 px | Complete; exact suffix pixels match the 78 px reference |

The preferred-height image has 701, 859 and 847 additional lower-line pixels
for A, B and Target. Each suffix-pixel SHA256 equals its taller GDI reference,
including positions and pixel colors, not only pixel count. The images were
inspected: the complete golden-coordinate address, structure and inverse word
are visible. Neither replacing LF with CRLF nor disabling rounded paint fixes
the 54 px case.

## Cause and correction

`SolveRoleTextHeight` currently measures text with `TextRenderer.WordBreak`
and adds padding plus six pixels. That produces 54 px for these strings.
The actual Flat Button's constrained `GetPreferredSize` requires 60 px. A text
measurement fitting the nominal interior is therefore not sufficient evidence
that the Button adapter will paint its second line.

Use `button.GetPreferredSize(new Size(actualWidth, Int32.MaxValue)).Height`
for the role row, retaining the caller's existing margins. This uses the real
control metric, includes its padding/chrome, and avoids a guessed extra offset
or renderer change. No production file was edited by this task.

## Reproduction and limits

Run `python tests/run_role_text_rendering_checks.py`. Source, original diagnostic,
compiler and executable hashes are recorded in `manifest.json`; none changed
during the recorded run. `report.json` records each exact text, dimensions,
native preferred dimensions and suffix pixels.

The main native runner must verify the production correction at its minimum
window and changed-name cases. This fixture does not certify high-DPI windows,
desktop screenshots, all future labels or full solving behavior.
