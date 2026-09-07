# Build and verification

The source checkout contains the complete generated runtime assets. It builds a 64-bit Python engine and launcher, and a 32-bit .NET Framework host. The native viewport uses the separately credited, pinned MPUlt runtime. Microsoft Managed DirectX is an external prerequisite; see [DIRECTX.md](../DIRECTX.md).

On a Windows development machine with 64-bit Python and .NET Framework 4.x compiler support:

```powershell
py -3 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt -r packaging\requirements-build.txt
.\.venv\Scripts\python.exe packaging\build_windows.py --output dist\build-024 --work work\build-024
```

Choose fresh output and work directories. The builder compiles the native host, runs its WinForms layout fixture, freezes the engine with PyInstaller and writes a portable folder/ZIP. Normal user startup does not require Python, a compiler or fixture windows. See [the packaging guide](../packaging/README.md) for the packaged layout.

To wrap a verified portable folder in an English per-user installer, install official Inno Setup 6.7.3 and run:

```powershell
.\.venv\Scripts\python.exe packaging\build_installer.py --bundle dist\build-024\C600Studio-0.2.4-Windows-x64 --iscc "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" --output dist\installer-024
```

Adjust only the compiler location if Inno Setup is installed elsewhere. The installer does not bundle or silently install Microsoft Managed DirectX.

Useful isolated regressions:

```powershell
.\.venv\Scripts\python.exe tests\test_core.py
.\.venv\Scripts\python.exe tests\test_reference_maps.py
.\.venv\Scripts\python.exe tests\test_crash.py
.\.venv\Scripts\python.exe tests\test_engine_lifecycle.py
.\.venv\Scripts\python.exe tests\test_frame_preferences.py
.\.venv\Scripts\python.exe tests\test_packaged_engine_command.py
.\.venv\Scripts\python.exe tests\test_portable_package.py dist\build-024\C600Studio-0.2.4-Windows-x64 work\portable-check-024
```

Actual native regression uses an explicit fresh data directory:

```powershell
.\.venv\Scripts\python.exe native\bootstrap.py --renderer-test --data work\native-check-024
```

It modifies only its test session. Actual DirectX validation must run on Windows with the supported runtime. Keep performance runs separate from other CPU/GPU work. Raw local test reports are developer artifacts, not files to commit automatically.

`tests/reference_solve.json.gz` is a generated seed-600 full-state regression fixture, not a personal solve log. The optional asset rebuilder requires SciPy plus the retained reference/toolkit inputs; running the application does not require rebuilding those assets.
