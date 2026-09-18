> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Functions frontend: isolated implementation evidence

## Scope

The native frontend registers `functions-toggle` and `bank-Functions`. The former enters the explicit Functions bank, or returns to the exact saved previous bank. Missing/self return destinations are rejected. No new persisted return field or keybind migration is introduced. Backend same-bank handling and the catalogue are owned and tested separately.

The effective-key merge removes moved utility defaults in current core sets before applying saved shared and per-bank overrides. Old Views/Session sets remain selectable in a Legacy group and by exact-ID search. Default next/previous cycling skips inactive legacy sets. Existing saved physical Backslash assignments keep their original meaning unless the user explicitly assigned a command; null command entries remain unbound.

Functions shows modifier combinations below the main keyboard without changing the normal Extra keys preference. Backslash is placed in the existing Q row, using compact punctuation columns and preserving letter-key width and 44px minimum hits. Input routing and physical scan mappings are unchanged. The Functions return destination is visible. A missing unmodified toggle route directs the user to the existing editor rather than silently choosing a new key. The editor retains its previous bank-local scope default; choosing shared scope is explicit.

## Actual checks

- `tests/run_functions_binding_checks.py`: first failed because the Functions contract was absent; final run passed 12 exact-source pure binding/return checks. No product window or input event was generated.
- Caption measurement first found 36 overflows after a naive equal-width 14-column row. A measured punctuation-column allocation resolved letter-key loss of space. Later passes caught Tools and newly added cycle labels; final 4,002 measured lines across actual catalogue maps at 864px and 847px passed. Font remains Segoe UI 10pt. This is GDI text measurement, not native layout or usability acceptance.
- Existing `ExperimentInputFeedbackRegression` passed 19 direct synthetic-router checks. No HWND, physical keyboard or GPU evidence. The first compile invocation failed because the legacy compiler interpreted forward-slash source paths; using native backslash paths compiled and ran successfully.
- `tests/FunctionsNativeChecks.cs` is prepared for the coordinator's real application run: default enter/return, repeated same-bank selection, text and IME isolation, chord hit targets, custom Grip/Twist/command/null compatibility, and retained hash/draft/Current/Next. It was not executed by this worker.

## Remaining verification

The coordinator must compile the integrated native sources and replay the new helper. Inspect actual minimum-size keyboard main rows, Functions shortcut scrolling, focus/release behavior, legacy selection, and restored state. Native scan43 is the existing physical Backslash position; ISO scan86 is still unsupported and has not been silently aliased. Actual hardware/layout verification is not claimed.

The shared binding editor's pre-existing conflict inspection is based on the currently active set. Its scope has not been broadened in this change; a user explicitly choosing shared scope must inspect other saved bank overrides. No personal profile was inspected or rewritten.
