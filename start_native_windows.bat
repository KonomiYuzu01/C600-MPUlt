@echo off
setlocal
set "PYTHONUTF8=1"
set "PYTHONIOENCODING=utf-8"
set "PYTHONUNBUFFERED=1"
cd /d "%~dp0"
if not exist ".venv\Scripts\python.exe" (
  py -3 -m venv .venv
  if errorlevel 1 goto failed
)
.venv\Scripts\python.exe -m pip install -r requirements.txt
if errorlevel 1 goto failed
.venv\Scripts\python.exe native\bootstrap.py %*
if errorlevel 1 goto failed
exit /b 0
:failed
echo.
echo Native launch stopped. No prior MPUlt installation was modified.
echo Keep using start_windows.bat for the browser client.
pause
exit /b 1
