@echo off
"%~dp0Check-Runtime.exe"
set "result=%errorlevel%"
echo.
pause
exit /b %result%
