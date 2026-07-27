@echo off
setlocal EnableExtensions
chcp 65001 >nul

if "%~1"=="" (
  echo Pouziti: webshare-test.cmd "https://webshare.cz/#/file/..."
  exit /b 2
)

set "ROOT=%~dp0"
set "PROJECT=%ROOT%tests\MVMediaStudio.WebshareIntegrationTests\MVMediaStudio.WebshareIntegrationTests.csproj"
set "MV_MEDIA_DOWNLOADER_DATA_DIR=%ROOT%artifacts\test-data\webshare"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo CHYBA: Nebylo nalezeno .NET 10 SDK.
  exit /b 1
)

dotnet build "%PROJECT%" --configuration Release --nologo --verbosity minimal
if errorlevel 1 exit /b 1

pushd "%ROOT%"
dotnet run --project "%PROJECT%" --configuration Release --no-build --no-restore -- "%~1"
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
