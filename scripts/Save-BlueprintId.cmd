@echo off
rem Wrapper to bypass PowerShell ExecutionPolicy without changing system settings.
rem Calls Save-BlueprintId.ps1 with -NoProfile -ExecutionPolicy Bypass.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Save-BlueprintId.ps1" %*
exit /b %ERRORLEVEL%
