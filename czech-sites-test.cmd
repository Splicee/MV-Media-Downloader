@echo off
setlocal
chcp 65001 >nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Test-CzechSources.ps1" %*
exit /b %ERRORLEVEL%
