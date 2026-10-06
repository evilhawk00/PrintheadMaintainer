@echo off
rem Runs build.ps1 with the same arguments, bypassing the PowerShell execution policy for this run.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
exit /b %ERRORLEVEL%
