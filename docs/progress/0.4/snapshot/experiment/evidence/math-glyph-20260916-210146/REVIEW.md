> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Mathematical text — offscreen evidence

Scope: the current `Experiment*.cs` text and `mathematical_names.py` formats.
No product font, UI, identity syntax, model, session or native window changed.

Run `python tests/run_math_glyph_checks.py` from the experiment. The runner builds
an isolated x86 .NET Framework executable and draws only memory bitmaps.
`manifest.json` binds the source, actual font files, compiler, executable, input,
recorded native address examples and hash-verified immutable census. Source/font
inputs and census were unchanged across this run.

## Results

- 53 actual non-ASCII source characters plus 3 requested coverage extensions.
  Actual UI fonts are Segoe UI 9, 10 and 10.5 pt. Regular and bold were checked.
- 336 primary-font glyph lookups: 60 report an absent primary glyph, corresponding
  to ten characters at all six size/style combinations: `↵ ↻ ⋮ ▶ ▸ ▾ ◇ ✓ ⟨ ⟩`.
  The unassigned U+0378 negative control correctly reports no glyph.
- Actual TextRenderer samples visibly render all ten using Windows fallback.
  There is no observed tofu/missing-character defect in these specimens.
  The primary-font-gap status and process exit **1** are retained; they are not
  relabelled as a passing full-UI test.
- Greek structure symbols, phi, superscripts, ordinary arrows, Unicode minus
  and the middle dot are present in Segoe UI itself.
- Segoe UI Symbol contains all ten primary-font gaps. Cambria Math lacks one
  and has substantially different metrics in the comparison. This comparison
  does not identify the actual Windows fallback font or recommend substitution.
- 60 width/height measurements across normal, compact, narrow 9/10 pt and
  simulated 150/200% specimens fit their deliberately measured text rectangles.
  Native recorded Current/Next addresses need two lines at 300 px / 10 pt;
  simply allocating one line there would lose information. Explicit line breaks
  before the structure/address word preserve the pole as a readable group.

## Images inspected

`glyph-atlas.png` shows each exact codepoint. `fallback-comparison.png` separates
the Segoe UI TextRenderer result from two comparison fonts. `normal.png`,
`compact.png`, `narrow-primary-10pt.png`, `narrow-secondary-9pt.png`,
`scaled-150.png` and `scaled-200.png` contain actual recorded Home addresses,
actual retained D5/A5 census names, reference notation and address-word symbols.
The fixture-only line break is not a new parser or identity conversion.

## Remaining checks

These are GDI bitmap samples, not a visible application window, hardware/GPU
test, human trial or actual Windows scaling test. Sample wrapping cannot prove
that production labels allocate the same height, that ellipsis remains
distinguishable, or that all inherited control-font states use these metrics.
The main native pass must inspect real small-window Current/Next labels, inverse
keycaps, frame correspondence, macro lists, selection/focus states and actual
supported DPI settings. No font change is justified solely by the cmap gap.

API semantics: [GetGlyphIndicesW](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-getglyphindicesw)
marks unsupported primary glyphs with 0xffff; [TextRenderer.MeasureText](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.textrenderer.measuretext)
measures the supplied text with its specified font and flags.
