@echo off
setlocal
title Magic 600 Cell - Managed DirectX prerequisite
if not exist "%~dp0redist\directx_Jun2010_redist.exe" (
  echo Installer missing. Extract the complete runtime ZIP before running this file.
  pause
  exit /b 1
)
echo First enable .NET Framework 3.5 and complete any requested restart.
echo The Microsoft package below EXTRACTS installation files.
echo Choose an empty folder. After extraction, open that folder and run DXSETUP.exe.
echo Read and accept Microsoft's terms only if you agree.
echo Afterwards, return here and run Check-Runtime.cmd.
pause
start /wait "" "%~dp0redist\directx_Jun2010_redist.exe"
set "result=%errorlevel%"
echo Extractor exit code: %result%. This does NOT confirm DXSETUP installation.
pause
exit /b %result%
