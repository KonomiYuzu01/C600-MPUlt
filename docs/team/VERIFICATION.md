# Magic 600 Cell verification

The team infrastructure is installed in both the desktop project and the actual application source repository. Resolve `source_root` in local `team-project.json`, or explicitly supply `-SourceRoot` / `--source-root`. The source repository uses `.`; the desktop mirror points to the external checkout. Do not assume every project folder contains application code.

## Entry points

```powershell
# Read-only source inspection and infrastructure syntax checks; creates private evidence only.
.\scripts\verify.ps1 -Stage infrastructure
# Inspect exact commands without launching an engine or native window.
.\scripts\verify.ps1 -Stage core -DryRun
# Explicit isolated checkout and its existing Python environment.
.\scripts\verify.ps1 -Stage core -SourceRoot 'C:\isolated\magic600' -Python 'C:\isolated\magic600\.venv\Scripts\python.exe' -AllowSourceTests
```

```bash
bash scripts/verify.sh --stage infrastructure --source-root /path/to/source
bash scripts/verify.sh --stage core --source-root /path/to/isolated/source --python /path/to/venv/bin/python --allow-source-tests
```

Use 64-bit Python 3.11 or newer with the application's `requirements.txt` (`numpy>=1.26,<3`). The runner installs nothing. `-Python` selects the interpreter for both wrapper and application commands; the Bash wrapper uses `PYTHON` for the runner and `--python` for application tests. The default infrastructure stage validates source location, Git identity, script presence, Python syntax, seven skill/agent definitions and project concurrency settings. It does not certify live role execution or API access. Lint and typecheck are **not configured**, not passing checks.

## Stages and evidence boundaries

| Stage | Existing source commands | Effects and evidence |
|---|---|---|
| `core` | `tests/test_core.py`, `tests/test_reference_maps.py`, `tests/test_crash.py` | Full-model mechanics, retained independent-map comparison, isolated SQLite hard-process-exit recovery. Not a power-loss or native renderer test. |
| `lifecycle` | `tests/test_engine_lifecycle.py`, `tests/test_frame_preferences.py`, `tests/test_packaged_engine_command.py` | Starts owned temporary engine processes. Reported historical lifecycle failure remains unresolved until this exact source passes. |
| `native-layout` | `native/bootstrap.py --self-test-only --data <fresh>` | Compiles and opens WinForms layout fixture; no DirectX certification. |
| `native-auxiliary` | `tests/test_native_auxiliary_controls.py --output <fresh>` | Compiles GDI/WinForms auxiliary fixtures; no MPUlt or HTTP engine. |
| `native-renderer` | `native/bootstrap.py --renderer-test --data <fresh>` | Compiles host/layout fixture and runs real MPUlt/DirectX regression using fresh session data. Not a performance measurement. |

Application stages require `-AllowSourceTests`: existing tests write reports into their source checkout and may create temporary processes. Select an isolated checkout, never the active development directory without coordinating ownership. Native stages also require `-AllowNative` and Windows; this flag records the caller's decision to take exclusive GUI ownership. Do not run them while another task owns the shared native window. Native data/output directories are fresh under the evidence run; personal sessions are not inputs.

Every invocation writes a new `work/team-evidence/<UTC>-<unique>/report.json`, exact command arguments, exit codes, and per-command logs. Git HEAD, status, tracked-diff fingerprint, model-manifest hash and test-entrypoint hashes identify the checked inputs. These are not a full snapshot: ignored experiments and untracked file contents are not covered by the Git fingerprint. Preserve a separate source manifest/build hash for native 0.4 experiment claims. Logs are private local artifacts and must not be published automatically. A dry run is labelled `dry-run`, never a test pass. Commands stop at the first failure and return nonzero.

## Native dependencies and build boundary

The existing source `docs/DEVELOPMENT.md`, `docs/RUNTIME_PROVENANCE.md` and `DIRECTX.md` remain authoritative. Native checks require Windows, 64-bit Python, the x86 .NET Framework 4.x compiler (`Microsoft.NET/Framework/v4.0.30319/csc.exe`), an interactive desktop, the retained pinned MPUlt runtime, and the matching Managed DirectX assemblies. The recorded DirectX / Direct3D / Direct3DX identity is version `1.0.2902.0`, token `31bf3856ad364e35`. Do not substitute newer identities or redistribute Microsoft's DLLs. Obtain missing prerequisites through their official installers and user authorization where needed.

Linux CI can verify Python model behavior. It cannot validate WinForms focus, keyboard input, native picking, Managed DirectX compatibility, actual GPU selection, mixed scaling, or rendering performance. A hosted Windows VM is likewise not the Legion's real GPU. Record the actual hardware, runtime, source/build and display settings for native/performance evidence. G1/G2 user approvals remain separate and pending until explicitly given.

Packaging is deliberately excluded from the wrapper. The source supports `python packaging/build_windows.py --output <fresh> --work <fresh>` and an Inno Setup installer step; these build release artifacts and may launch fixtures. Never trigger these as a side effect of infrastructure checks or silently treat a historical 0.3 package as a 0.4 build.

## Worktree caveat

At inspection, source HEAD was `5e1d35de792ccc8db896b8abf74ff2b1b00e0749` with four existing modifications: `SOURCE_MANIFEST.json`, `docs/DEVELOPMENT_LOG.md`, `docs/LIMITATIONS_AND_ROADMAP.md`, and `docs/NEXT_UPDATE.md`. The source ignores `work/`, which contains the active `work/experiments/magic600-04/` implementation. A fresh Git worktree or CI checkout does not contain those ignored experiments or existing uncommitted changes. Do not claim these stages validate 0.4 experiments; use their documented explicit tests after synchronizing the intended files and recording their identity. Do not copy personal sessions into a worktree.
