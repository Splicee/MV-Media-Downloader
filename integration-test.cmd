@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0"
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set "TEST_EXE=%ROOT%dist\integration-tests.exe"

set "SOURCES="
for /R "%ROOT%src\Core" %%F in (*.cs) do set "SOURCES=!SOURCES! "%%F""
for /R "%ROOT%src\Services" %%F in (*.cs) do if /I not "%%~nxF"=="UpdateService.cs" set "SOURCES=!SOURCES! "%%F""

"%CSC%" /nologo /target:exe /platform:anycpu /optimize+ /codepage:65001 /out:"%TEST_EXE%" /reference:System.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll !SOURCES! "%ROOT%tests\IntegrationTests.cs"
if errorlevel 1 exit /b 1
if /I "%COMPILE_ONLY%"=="1" (
  del /Q "%TEST_EXE%" >nul 2>nul
  exit /b 0
)

"%TEST_EXE%"
set "RESULT=%ERRORLEVEL%"
del /Q "%TEST_EXE%" >nul 2>nul
exit /b %RESULT%
