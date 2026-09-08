# Build and verification

The source checkout contains the complete generated runtime assets. It builds a 64-bit Python engine and launcher, and a 32-bit .NET Framework host. The native viewport uses the separately credited, pinned MPUlt runtime. Microsoft Managed DirectX is an external prerequisite; see [DIRECTX.md](../DIRECTX.md).

On a Windows development machine with 64-bit Python and .NET Framework 4.x compiler support:

```powershell
py -3 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt -r packaging\requirements-build.txt
.\.venv\Scripts\python.exe packaging\build_windows.py --output dist\build-03 --work work\build-03
```

Choose fresh output and work directories. The builder compiles the native host, runs its WinForms layout fixture, freezes the engine with PyInstaller and writes a portable folder/ZIP. Normal user startup does not require Python, a compiler or fixture windows. See [the packaging guide](../packaging/README.md) for the packaged layout.

To wrap a verified portable folder in an English per-user installer, install official Inno Setup 6.7.3 and run:

```powershell
.\.venv\Scripts\python.exe packaging\build_installer.py --bundle dist\build-03\C600Studio-0.3-Windows-x64 --iscc "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" --output dist\installer-03
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
.\.venv\Scripts\python.exe tests\test_portable_package.py dist\build-03\C600Studio-0.3-Windows-x64 work\portable-check-03
```

Actual native regression uses an explicit fresh data directory:

```powershell
.\.venv\Scripts\python.exe native\bootstrap.py --renderer-test --data work\native-check-03
```

It modifies only its test session. Actual DirectX validation must run on Windows with the supported runtime. Keep performance runs separate from other CPU/GPU work. Raw local test reports are developer artifacts, not files to commit automatically.

For the focused gesture, structure, auxiliary-view, filter, recovery, and workbench feature pass, enable its explicit opt-in flag before the native command:

```powershell
$env:C600_RENDER_FEATURE_TEST = '1'
.\.venv\Scripts\python.exe native\bootstrap.py --renderer-test --data work\native-features-01
Remove-Item Env:C600_RENDER_FEATURE_TEST
```

The native bootstrap runs the host layout fixture before the requested renderer regression. Neither fixture runs during normal application startup. Use a fresh directory for each run and retain reports privately.

To run only the structure, auxiliary geometry, and window-lifecycle fixtures:

```powershell
.\.venv\Scripts\python.exe tests\test_native_auxiliary_controls.py --output work\auxiliary-controls-01
```

Choose a fresh output directory. This command validates the retained model, creates and closes a fresh solved SQLite session for production cell-status data, compiles the structure and auxiliary controls and fixtures with the .NET Framework compiler, then opens isolated WinForms test windows. It checks local structure navigation, preview guards, the real tetrahedral geometry with a generated test palette, and window lifecycle, and records a report, build log, and exact source hashes. It loads neither MPUlt nor an HTTP engine, and does not access a personal session. The native subprocess has a bounded timeout and is terminated if it hangs. These GDI/WinForms checks do not certify the actual native-host input path, mixed-monitor behavior, or a performance target. Run them separately from timed samples and retain their outputs privately.

`tests/reference_solve.json.gz` is a generated seed-600 full-state regression fixture, not a personal solve log. The optional asset rebuilder requires SciPy plus the retained reference/toolkit inputs; running the application does not require rebuilding those assets.
