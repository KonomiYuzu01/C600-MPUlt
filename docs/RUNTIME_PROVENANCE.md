# MPUlt runtime provenance

**Magic Puzzle Ultimate is the work of Andrey Astrelin.** His [original project page](https://superliminal.com/andrey/mpu/) provides the program and source with a requirement for clear author credit. The C600 workbench adds its own interface and state-management layer around MPUlt; it does not claim authorship of the original simulator or renderer.

The retained compatible executable is from the **2026-07-11 distribution**:

- File: `native/runtime/MPUlt.exe`
- Size: 196,608 bytes
- SHA256: `228b3145460a399e2accc17cb5b891568ec8ddd33c011f1f2a32a00c59743327`

The public [ivan216 release/tag](https://github.com/ivan216/MPUlt/releases/tag/2026-07-11) provides version context. Its currently empty release-assets list does not permit a fresh online byte comparison. The retained binary matches the archived July 11 distribution, but it has not been shown to be a reproducible build of that tag or of the later source used for reference. It is different from the older `MPUlt_155.exe` currently served by the original project website. Do not silently swap runtimes or claim these binaries are identical.

Public source references include [cutelyaware/MPUlt](https://github.com/cutelyaware/MPUlt) and [ivan216/MPUlt](https://github.com/ivan216/MPUlt). Their MIT notice names Copyright (c) 2018 Melinda Green and is retained unchanged in [MPUlt-MIT.txt](../licenses/MPUlt-MIT.txt). The original author's credit is retained separately and prominently.

`MPUlt_puzzles.txt` and `MPUlt_settings.txt` are the controlled initial distribution definitions/defaults, not a copy of a live user's session. They contain puzzle definitions and numerical display settings only. The fixed full 600-cell numerical assets are unchanged. Connecting the native host verifies all 259,800 sticker slots and all 1,200 positive generators against that model.

Microsoft DirectX assemblies are excluded from the public package and remain externally installed dependencies under Microsoft's terms. Other third-party notices are in [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md).
