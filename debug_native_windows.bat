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
.venv\Scripts\python.exe tests\run_debug.py --native-bridge
if errorlevel 1 goto failed
echo.
echo Debug checks completed. Read tests\v022\DEBUG_RESULTS.json.
echo Native DirectX operation is tested when you run start_native_windows.bat.
echo No existing puzzle session was used by this debug run.
pause
exit /b 0
:failed
echo.
echo A debug check failed. Read tests\v022\DEBUG_RESULTS.json and its named log.
echo Your existing puzzle database was not used for these tests.
pause
exit /b 1
