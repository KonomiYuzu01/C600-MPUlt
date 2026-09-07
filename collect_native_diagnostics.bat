@echo off
setlocal
set "PYTHONUTF8=1"
set "PYTHONIOENCODING=utf-8"
set "PYTHONUNBUFFERED=1"
cd /d "%~dp0"
if not exist ".venv\Scripts\python.exe" (
  echo Run start_native_windows.bat first.
  pause
  exit /b 1
)
.venv\Scripts\python.exe native\collect_diagnostics.py %*
pause
