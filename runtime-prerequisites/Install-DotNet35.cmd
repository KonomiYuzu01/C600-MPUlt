@echo off
setlocal
title Magic 600 Cell - .NET Framework 3.5
set "build="
set "kind="
for /f "tokens=3" %%B in ('reg query "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion" /v CurrentBuildNumber 2^>nul') do set "build=%%B"
for /f "tokens=3" %%B in ('reg query "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion" /v InstallationType 2^>nul') do set "kind=%%B"
if /i not "%kind%"=="Client" goto unsupported
if not defined build goto unsupported
if %build% LSS 10240 goto unsupported
if %build% GEQ 28000 goto standalone
fltmc >nul 2>&1
if errorlevel 1 (
  echo Right-click this file and select Run as administrator.
  pause
  exit /b 1
)
echo Enabling Windows .NET Framework 3.5, including 2.0 and 3.0.
echo Windows Update access may be required. No automatic restart will occur.
DISM.exe /Online /Enable-Feature /FeatureName:NetFx3 /All /NoRestart
set "result=%errorlevel%"
if "%result%"=="3010" (
  echo Installation requires a Windows restart. Restart before installing DirectX.
  pause
  exit /b 3010
)
if not "%result%"=="0" (
  echo Installation failed with code %result%. See README.md for offline and repair guidance.
  pause
  exit /b %result%
)
echo Windows reported success. Next run Install-DirectX.cmd.
pause
exit /b 0
:standalone
echo Windows 11 build 28000 or later requires its version-specific standalone installer.
echo The Microsoft instructions will open. Download and run the installer offered there.
start "" "https://learn.microsoft.com/en-us/dotnet/framework/install/dotnet-35-windows-11"
pause
exit /b 2
:unsupported
echo This helper targets Windows 10 and Windows 11 client systems only.
echo No installation was attempted. Consult the Microsoft instructions in README.md.
pause
exit /b 2
