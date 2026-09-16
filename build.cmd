@echo off
rem Builds this project with the repo-local toolchain (see tools\build.ps1).
rem Usage:
rem   build.cmd                build Debug (normal)
rem   build.cmd Release        build Release (normal)
rem   build.cmd -Clean         clean bin\ + obj\, then build Debug
rem   build.cmd -Clean Release clean bin\ + obj\, then build Release
setlocal
set "PS=pwsh.exe"
where pwsh.exe >nul 2>nul || set "PS=powershell.exe"
"%PS%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\build.ps1" %*
exit /b %ERRORLEVEL%
