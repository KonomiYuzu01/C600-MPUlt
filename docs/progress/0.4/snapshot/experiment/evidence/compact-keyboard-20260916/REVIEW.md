> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Compact Keyboard review — 2026-09-16

The current native Keyboard is 880×490 outer / 864×451 client, compared with 1056×540 / 1040×501 in the same I108, C6, piece(108) Grip/filter fixture. This is a 24.38% outer-area reduction, not a usability score or a human-time claim. Body font was retained. Keys have at least44px targets. The five primary rows are visible without scrolling in the tested default; additional modifier chords are explicitly expandable. Custom unmodified keys remain directly available.

## Changes and evidence

- The header combines set, destination, mode and focus. Current and locked Next retain their Home identities; repeated category lines move to existing detail access. Protection stays visible. Canonical IDs and saved identities are unchanged.
- Compact Grip caps retain the actual cell color as a secondary cue. Focus/hover discovers the full mathematical cap and exact frame; the active Grip persists in the header. Frame `abcd ← cabd` identifies the selected corners relative to the verified retained H/T basis, not a puzzle move.
- Extra-key disclosure is registered/editable with a Backquote default. Old physical Backquote bindings retain their meaning unless the user explicitly assigns a command. Text/IME ownership, capture, modal and release gates remain authoritative.
- Shift changes the displayed effective cycle and explicit onscreen Twist consistently with the existing inverse-XOR route. Independent review reproduced stale Shift feedback after reset/lost key-up; red tests were retained and the bounded display-state correction passed19 synthetic checks.
- The previously clipped last extra-key row was reproduced as a live nested-layout timing defect, rather than treated as padding. The Macro dialog's minimum-width Next clipping was separately reproduced and fixed by a coalesced measured parent layout;31 offscreen checks and the actual G2 dialog passed.
- Grip highlighting intersects actual affecting-cap membership with evaluated Piece Filter results in Local and committed Hub tokens. Unknown/hidden matches cannot appear highlighted. This does not change full-model rotation or protection scope. Actual Puzzle/Global have not acquired that extra overlay.

## Fresh verification

G2 `native-baseline/postapproval-g2-20260916-081709`: exit0,307 internal assertions. Real E1 two explicit insertions, first-block protection, Current/Next, full operation findings, worksheets, undo/redo, Macro Base filtering and metadata, Help, all key reachability and owned-window lifecycle. These are assertions, not307 independent user scenarios.

G1 `native-baseline/postapproval-g1-20260916-082150`: exit0,162 internal assertions. Real control press/release/latch/rejection pixel changes, explicit Numpad/Backquote compatibility, window focus loss, full labels unchanged, Shift cycle display and an actual injected Shift Twist paired with its forward word. All162 assertions use the final current source hashes. G2 differs only in the final Input Shift-release correction and the G1-specific fixture extension; G1 plus19 direct-router checks cover that correction.

Other focused checks:29 offscreen caption/layout checks across12 standard bank patterns at two effective widths;9 real-model frame tests, including132 exact full-label frame/action witnesses. Earlier core/reference/crash wrapper `20260915T223149Z-ba82286e` passed, including1200 reference mappings, after the layout-persistence change; report restoration was byte-exact. No repeated unrelated core run was needed for the subsequent display-only changes.

Inspect `verification.json` for executable identities, source equality and dimensions. GDI comparison:075232/11-grip-filter-keyboard.png versus081709/11-grip-filter-keyboard.png. Actual Windows desktop captures:081709/desktop-keyboard.png and desktop-help.png. G1 includes separate held/released/rejected/latched frames and keyboard-shift-inverse.png.

## Limits and next issue

Evidence uses the existing Windows WinForms/MPUlt environment, a1280×720 logical desktop and1920×1080 physical captures with existing150% virtualization/Intel HD620. GDI control images are not DirectX screenshots. Inputs are agent-injected, not physical typing or human solving. No2560×1600/RTX4070 performance claim.

Independent visual review found no new collision or missing identity in the same-state compact comparison. It did find that simultaneously open Keyboard and Macro Base windows still obscure much of the central graph on this small desktop. The next workspace iteration should test a reproducible cooperative layout without shrinking type or losing advanced keys. Command-to-ready measurements remain around1.3–1.8s in the prior bounded sample and have not been established as acceptable input-to-frame latency.

G1/G2 user approvals remain Approved for the previously accepted sample. This cycle does not grant a release approval or complete0.4 acceptance. Remaining feature integration, sustained mixed-cycle acceptance and final continuous solver recording are outstanding. No intermediate test package or public release was created.
