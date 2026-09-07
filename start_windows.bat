@echo off
cd /d "%~dp0"
if not exist ".venv\Scripts\python.exe" (
  py -3 -m venv .venv
  if errorlevel 1 goto failed
)
.venv\Scripts\python.exe -c "import numpy" >nul 2>nul
if errorlevel 1 (
  .venv\Scripts\python.exe -m pip install -r requirements.txt
  if errorlevel 1 goto failed
)
.venv\Scripts\python.exe server.py
if errorlevel 1 goto failed
exit /b
:failed
echo.
echo Launch failed. Install 64-bit Python 3.11 or newer, then retry.
echo Full error output is above. Do not disable your antivirus.
pause
