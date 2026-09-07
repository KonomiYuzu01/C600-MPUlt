# Using C600 Studio

The workbench uses the full 600-cell model: 35 moving orbits, 177,120 surface pieces, 259,800 labelled sticker slots and 1,200 positive generators. Display filtering never simplifies the mechanical state.

- **Reset** returns to solved and keeps an automatic recovery checkpoint, including the current camera and focused cell. Restore the checkpoint to recover previous progress.
- **Ctrl+S** saves a new C600 log under the session's `logs` folder. **Ctrl+O** imports a log. **File / Session → Export log** selects C600 or MPUlt v1 format and a destination.
- **Ctrl+Z / Ctrl+Shift+Z** undo / redo. **F9** saves a checkpoint. **F8** shows or hides the tools panel.
- Left drag rotates in 3D. Shift+left drag rotates in 4D. Right drag rolls horizontally and moves in 4D vertically; native coordinate index 0 is W.
- Ctrl+click centers a visible cell; Shift+click selects a visible piece. Native multi-click grips remain available. Hidden pieces cannot be picked.

Filters can show exact piece sets, or explicitly include context with pin settings. The optional cell-framework setting changes only the displayed framework. Smooth motion temporarily reduces detail in dense scenes and restores all visible detail after motion stops.

Macros keep complete legal witnesses. Preview and inspect the effect before committing; protected-orbit conflicts are rejected. Buffer analysis, insertion tools and progress/session reports are available in the tools panel.

C600 logs retain transactions, word/star witnesses and full-state hashes. MPUlt compatibility is limited to `MPUltimate v1 600-cell-Full`; the model, checksum, all turns and full colour state must verify. Native redo tails are checked and retained on import. Export represents the current active branch; MPUlt export flattens its legal turns rather than preserving Studio macro categories. File preferences and timers do not overwrite current preferences and timer.

Imports are limited to 16 MiB, expanding to at most 24 MiB. C600 accepts up to 10,000 transactions / 2,000,000 expanded primitive moves; MPUlt accepts up to 10,000 turns. Unsupported formats, foreign models and invalid records are rejected without replacing the session. Streaming and legacy-format imports are not supported.

Sessions and preferences are local under `%LOCALAPPDATA%\C600Studio`. Keep all portable-package files together. See [dependencies](DEPENDENCIES.md) if the native runtime is missing.
