@echo off
setlocal EnableExtensions

if "%~1"=="" (
  echo Pouziti: webshare-test.cmd "https://webshare.cz/#/file/..."
  exit /b 2
)

set "ROOT=%~dp0"
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set "TEST_EXE=%TEMP%\mv-media-webshare-test.exe"

"%CSC%" /nologo /target:exe /platform:anycpu /optimize+ /nowarn:0649 /codepage:65001 /out:"%TEST_EXE%" /reference:System.dll /reference:System.Core.dll /reference:System.Security.dll "%ROOT%src\Core\AppPaths.cs" "%ROOT%src\Core\DiagnosticRedactor.cs" "%ROOT%src\Core\Models.cs" "%ROOT%src\Core\DownloadProviders.cs" "%ROOT%src\Services\WebshareService.cs" "%ROOT%tests\WebshareIntegrationTests.cs"
if errorlevel 1 exit /b 1

"%TEST_EXE%" "%~1"
set "RESULT=%ERRORLEVEL%"
del /Q "%TEST_EXE%" >nul 2>nul
exit /b %RESULT%
