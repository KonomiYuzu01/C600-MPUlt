@echo off
setlocal
set "magic600_node=node"
if exist "%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe" set "magic600_node=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe"
"%magic600_node%" "%~dp0src\configure-key.mjs"
set "magic600_result=%errorlevel%"
if not "%magic600_result%"=="0" echo Setup did not complete. Do not paste a key into this window.
echo Press any key to close this window.
pause >nul
exit /b %magic600_result%
