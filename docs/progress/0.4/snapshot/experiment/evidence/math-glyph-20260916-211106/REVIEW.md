> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Inverse typography follow-up

`mathematical_names.py` now renders the inverse address-word token as `T⁻¹`.
Canonical word arrays, shortlex order and lookup keys retain `T^-1`. The central
typed-address parser accepts both exact spellings. Newly copied addresses use
the displayed Unicode spelling; existing ASCII copies continue to resolve.
Model IDs, canonical piece/position/slot identities and naming semantics are
unchanged, so `incidence-address-v1` is retained.

New regression first failed on the old ASCII display, then four focused naming
checks passed in 2.453 s: all 433 reference-region roundtrips, both spellings for
Slot/Position/Piece, unchanged exact shortlex words, and malformed/type/model/
version rejection. Six existing adapter-input checks passed in 6.190 s,
including canonical persistence, legal moves and rejected-input preservation.

Fresh offscreen run: 60 specimen measurements, zero measured rectangle overflow.
The inspected compact PNG renders `T⁻¹` neatly in address words and full Home
addresses. Historical native address fixtures received only the explicit new
typographic spelling; the source of each sample is recorded in inputs.json.
All source/font bindings remained unchanged during this run.

Exit 1 remains the previously identified **primary-font-gap**, not a missing
TextRenderer-glyph claim: ten source symbols rely on Windows fallback. `T`, `⁻`
and `¹` are present in the actual Segoe UI font. No production font change,
native-window interaction or Windows-scaling verification occurred here.

Earlier coverage methodology and limitations are in
`../math-glyph-20260916-210146/REVIEW.md`. This folder's manifest and PNGs bind the
new typography; the earlier screenshots remain historical ASCII evidence.
