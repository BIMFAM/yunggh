@echo off
rem ---------------------------------------------------------------------------
rem  Double-click to build yunggh and publish it to Rhino 8's Package Manager.
rem  All logic lives in publish-rhino8.ps1 next to this file.
rem ---------------------------------------------------------------------------
title Publish yunggh to Rhino 8 Package Manager

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-rhino8.ps1"
set EXITCODE=%ERRORLEVEL%

echo.
if not "%EXITCODE%"=="0" echo Finished with errors (exit code %EXITCODE%).
pause
exit /b %EXITCODE%
