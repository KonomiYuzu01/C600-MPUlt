# Magic 600 Cell - integrated architecture source

Revision: 1.0-A3, 18 September 2026.

## Package contents

- `Magic_600_Cell_1_0_Architecture_Integrated.pdf`: compiled reading edition.
- `Magic_600_Cell_1_0_Architecture_Integrated.tex`: complete, editable LaTeX source; no external images or custom style files are required.
- `10_V1_ARCHITECTURE.md`: the updated project architecture, including the release priority and integrated review amendments.
- This build guide.

## Build

Use XeLaTeX with a current LaTeX installation. The source uses the Windows fonts Cambria, Calibri, Consolas and Microsoft YaHei (for a retained quoted source phrase). Install these fonts or replace the corresponding `fontspec` declarations when building on another system. No application runtime, GPU, private credentials or original MPUlt binary is needed to build the document.

Run twice from the extracted directory so the table of contents and internal links settle:

```text
xelatex -interaction=nonstopmode -halt-on-error Magic_600_Cell_1_0_Architecture_Integrated.tex
xelatex -interaction=nonstopmode -halt-on-error Magic_600_Cell_1_0_Architecture_Integrated.tex
```

The document uses standard LaTeX packages: geometry, fontspec, amsmath, amssymb, array, longtable, booktabs, calc, graphicx, xcolor, fancyhdr and hyperref. No shell escape is required.

## Editing and authority

The Markdown file is the project architecture source. The LaTeX file is a standalone editable publication source, not a live synchronized view: changes to one do not automatically modify the other. Carry substantive changes back to the Markdown contract before distributing a new revision.

R00 records the user-confirmed B4-12 deferral, unchanged thresholds and bounded current-release closeout. V11.1 carries the optimization plan and next-device closure evidence into 1.0. Technical proposals remain marked `[P]`; outstanding decisions remain `[D]`. A2 integrated the previous separate review; A3 adds the deferred B4-12 workstream. Appendix C preserves both amendment records. Original A1 observations are historical, not a new audit of the running program.

PDF compilation and page review do not constitute application acceptance, hardware attribution, a successful release or approval of the proposed 1.0 migration.
