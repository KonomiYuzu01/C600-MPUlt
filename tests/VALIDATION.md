# 0.2.4 release validation

Validated on Windows 10 with Intel HD Graphics 620, the original compatible
MPUlt runtime, .NET Framework 4.x, and the frozen Windows application.
The dedicated full puzzle retains 35 orbits, 177,120 pieces, 259,800 labelled
sticker slots, and all 1,200 generators. Its model assets are unchanged.

## Native functionality

All **68 comprehensive native checks** and **4 fresh-process reopen checks**
passed against the final native sources and frozen engine, using the official
installed Managed DirectX 1.1 assemblies. The checks use actual WinForms
controls, original MPUlt methods, the viewport's own window messages, and
DirectX render-target pixels. They cover:

- Startup, resize, maximize/restore, tools layout, rendering and coloring.
- Native multi-click twisting, 3D/4D camera rotation, modifiers and key remapping.
- A 1,000-turn scramble, protection rejection, exact undo/redo, reset and recovery.
- Exact filters, layered styles, exclusions, invalid expressions, preset selection,
  and rejection of hidden-piece clicks, including when the frame is visible.
- The synchronized frame checkbox and View menu, busy-state recovery, animated
  redraws, camera/display changes, checkpoint restore and reopening.
- Checkpoints, camera/preferences recovery, macros, finite legal witnesses,
  buffer analysis, insertion, utilities, progress and session reports.
- Log save/import/export, corrupt-file rejection, existing-file preservation,
  and interoperability with original MPUlt v1 Save/Load and sequence replay.
- Controlled device-loss recovery, animation/reentrancy guards and clean shutdown.

All full-state comparisons retain every labelled slot. Three fixed full-detail
poses and a filtered selection/outline view matched the original rendering
path's pixels exactly.

## Performance

All existing performance gates passed without relaxation at a 931 x 604
viewport. Timing ran separately from builds, audits and other test processes.

| Measured workload | Result |
| --- | --- |
| Full 259,800-mesh original draw, mean | 626.57 ms |
| Full 259,800-mesh optimized draw, mean | 453.45 ms (27.63% less time) |
| Optimized full-detail draw, 95th percentile | 533.82 ms |
| Exact active / work-cell camera views | 56.63 / 58.38 FPS |
| Dense scene camera motion, temporary 1,500-mesh detail | 57.84 FPS |
| Dense motion draw, 95th percentile | 7.95 ms |
| Restore every mesh after motion | 429.01 ms; 630.28 ms including quiet delay |
| 1,000-turn scramble / undo / redo | 526.37 / 333.57 / 305.53 ms |
| Native idle CPU, share of total machine capacity | 0.078% |

Continuous full-detail redraw remains approximately **2.2 FPS** on this GPU.
Smooth motion temporarily reduces displayed detail and restores the complete
scene when movement stops; mechanics and saved state always remain complete.
Results describe this hardware and workload, not a guarantee for every scene,
window size or background workload. Reopening preserved all 1,039,200 label
bytes exactly.

## Engine, packaging and installer

The final persistence sources passed `test_core.py`, `test_reference_maps.py`,
`test_crash.py`, and `test_frame_preferences.py`. These include all 1,200
independent generator comparisons, all 35 orbit seed/local-completion checks,
durable-commit recovery and invalid frame-preference rejection. The lifecycle
suite passed 24 checks, with 3 additional packaged-command checks.

Eight build-time WinForms fixture checks passed. The fixture and compiler are
excluded from normal executable startup. Seven real frozen-package checks
passed, including all 165 payload hashes, architecture/dependency checks,
startup with Python/compiler paths absent, Unicode data paths, exact reopen,
parent-pipe EOF cleanup, and rejection of damaged resources. No Microsoft
Managed DirectX DLLs, user sessions or personal diagnostics are distributed.

The setup EXE passed four real Windows installation checks: installation into
an isolated directory, installed-resource verification, frozen-engine
save/reopen/shutdown, and uninstall while preserving the test session.
Normal packaged startup also passed against the existing local session after
a fresh private backup. The live native bridge passed, every label was
byte-identical, and preferences, history, checkpoints and keybindings were
preserved. No startup fixture or external Python/compiler was used.

## Scope and build identity

The unattended checks call the production restore and file-processing paths;
they do not claim manual file selection in Windows common dialogs or manual
confirmation-dialog coverage. Explicit control scaling is not a multi-monitor
DPI test. Controlled process termination and device-loss injection do not
simulate hardware power loss or every possible graphics-driver failure.
Private sessions, screenshots, machine paths and raw diagnostics are excluded.
Build instructions and test entry points are in [Development](../docs/DEVELOPMENT.md).

- Native production source SHA-256:
  `a4eadfa999dae4487bac5d69fa22211fddbe2f87673920e2a87abe009598f0e9`
- Packaged native host SHA-256:
  `984a34f1ae03d5778c600d84c78c3a34d7fb5e1cece5ee3a9217a77282532cf4`
- Frozen engine SHA-256:
  `e68f7bda47f77dfd86ba8986df9267dfa4f9f0e232592456276eadf746308384`

The release assets have separate SHA-256 checksums in `SHA256SUMS.txt`.
