# Developer guide

Work only on the full `600-cell-Full` profile. Keep all 259,800 labelled sticker slots and all 1,200 legal generators. Rendering filters, framework visibility and motion sampling must never change mechanical state or relabel pieces.

- Treat `assets/manifest.json` as an immutable model boundary. Geometry, cuts, IDs, seeds and frame changes require a new model identity and migration.
- Preserve finite legal witnesses and full collateral effects for every macro. Execute source-to-destination permutations in chronological order.
- Recheck preview revisions, full-state hashes and protected-orbit constraints before commit.
- Keep reset/import transactional and recoverable. Validate complete input before database writes; retain current preferences and the recovery checkpoint.
- Use `EngineProcess` for owned local engine startup, authenticated health checks, graceful shutdown and parent-pipe recovery.
- Run `tests/test_core.py`, `tests/test_reference_maps.py` and `tests/test_crash.py` after mechanics or persistence changes; run lifecycle tests after process-ownership changes.
- Compile and run `tests/native/NativeHostRegression.cs` before actual native regressions. Never run regression fixtures during normal user startup.
- Use fresh isolated test data. Do not run destructive tests against a personal session.
- Distinguish source/fixture, synthetic geometry, actual Windows/DirectX, and performance evidence. Publish only verified results for the matching source/build.
- Preserve Andrey Astrelin's primary MPUlt credit and all upstream license notices. Do not redistribute Microsoft Managed DirectX DLLs in the public package.
- Keep public UI and documentation in English. Never publish user databases, personal logs, credentials, private paths, screenshots or raw machine diagnostics.

See `docs/DEVELOPMENT.md`, `docs/RUNTIME_PROVENANCE.md` and `DIRECTX.md` for build, provenance and dependency details.

## Project engineering team

Read local PROJECT_MEMORY.md first when present; it points to the desktop project shared memory. If unavailable, use current approved architecture and request missing decisions instead of treating historical docs as current approval.

For substantial authorized development, use docs/team/OPERATING_MODEL.md and the seven .agents/skills roles, with independent review and fresh verification. Project agent definitions are in .codex/agents. Delegate bounded independent work; use no more than three children plus the primary, inheriting its model settings. Shared directories require explicit file ownership.

Resolve local source paths through team-project.json; use scripts/verify.ps1 with the appropriate stage. Routine isolated implementation and checks can proceed; unresolved architecture choices require the user. G1 and G2 each need an independently approved native runnable sample before formal integration. API planning sessions have no local execution environment and cannot provide native acceptance. Never commit local memory, credentials, private evidence or machine-specific paths.
