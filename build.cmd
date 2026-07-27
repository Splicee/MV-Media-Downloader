@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "ROOT=%~dp0"
set "SOLUTION=%ROOT%MV Media Downloader.sln"
set "APP_PROJECT=%ROOT%src\MVMediaStudio\MVMediaStudio.csproj"
set "UPDATER_PROJECT=%ROOT%updater\MVMediaStudio.Updater\MVMediaStudio.Updater.csproj"
if not defined BUILD_OUTPUT_DIR set "BUILD_OUTPUT_DIR=%ROOT%dist"
set "UPDATER_STAGE=%ROOT%artifacts\publish\updater"
set "APP_OUTPUT=%BUILD_OUTPUT_DIR%\MV Media Downloader.exe"
set "UPDATER_OUTPUT=%BUILD_OUTPUT_DIR%\MV Media Downloader Updater.exe"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo CHYBA: Nebylo nalezeno .NET SDK. Nainstaluj .NET 10 SDK.
  exit /b 1
)
if not exist "%SOLUTION%" (
  echo CHYBA: Nebylo nalezeno reseni "%SOLUTION%".
  exit /b 1
)

for /f "delims=" %%V in ('dotnet --version 2^>nul') do set "DOTNET_VERSION=%%V"
echo Pouzivam .NET SDK %DOTNET_VERSION%.

if not exist "%BUILD_OUTPUT_DIR%" mkdir "%BUILD_OUTPUT_DIR%"
if errorlevel 1 exit /b 1
if exist "%UPDATER_STAGE%" rmdir /S /Q "%UPDATER_STAGE%"
if not exist "%UPDATER_STAGE%" mkdir "%UPDATER_STAGE%"
if errorlevel 1 exit /b 1

if exist "%APP_OUTPUT%" del /Q "%APP_OUTPUT%"
if exist "%UPDATER_OUTPUT%" del /Q "%UPDATER_OUTPUT%"
if exist "%BUILD_OUTPUT_DIR%\yt-dlp-plugins" rmdir /S /Q "%BUILD_OUTPUT_DIR%\yt-dlp-plugins"

dotnet publish "%APP_PROJECT%" --configuration Release --runtime win-x64 --self-contained true --property:PublishProfile=Windows-x64 --output "%BUILD_OUTPUT_DIR%" --nologo --verbosity minimal
if errorlevel 1 (
  echo CHYBA: Sestaveni aplikace se nepovedlo.
  exit /b 1
)

dotnet publish "%UPDATER_PROJECT%" --configuration Release --runtime win-x64 --self-contained true --property:PublishProfile=Windows-x64 --output "%UPDATER_STAGE%" --nologo --verbosity minimal
if errorlevel 1 (
  echo CHYBA: Sestaveni aktualizatoru se nepovedlo.
  exit /b 1
)

copy /Y "%UPDATER_STAGE%\MV Media Downloader Updater.exe" "%UPDATER_OUTPUT%" >nul
if errorlevel 1 (
  echo CHYBA: Aktualizator se nepodarilo vlozit do vystupu.
  exit /b 1
)

if exist "%ROOT%tools\yt-dlp.exe" copy /Y "%ROOT%tools\yt-dlp.exe" "%BUILD_OUTPUT_DIR%\yt-dlp.exe" >nul
if not exist "%BUILD_OUTPUT_DIR%\yt-dlp-plugins" (
  echo CHYBA: V sestaveni chybi slozka yt-dlp-plugins.
  exit /b 1
)

call :SignIfConfigured "%APP_OUTPUT%"
if errorlevel 1 exit /b 1
call :SignIfConfigured "%UPDATER_OUTPUT%"
if errorlevel 1 exit /b 1

echo HOTOVO: %APP_OUTPUT%
echo HOTOVO: %UPDATER_OUTPUT%
exit /b 0

:SignIfConfigured
set "SIGN_TARGET=%~1"
if /I "%SKIP_SIGN%"=="1" exit /b 0
if not defined SIGN_CERT_PATH if not defined SIGN_CERT_SHA1 (
  echo Podpis preskocen: neni nastaven SIGN_CERT_PATH ani SIGN_CERT_SHA1.
  exit /b 0
)

set "SIGNTOOL="
for %%S in (signtool.exe) do set "SIGNTOOL=%%~$PATH:S"
if not defined SIGNTOOL (
  for /d %%D in ("%ProgramFiles(x86)%\Windows Kits\10\bin\*") do if exist "%%~fD\x64\signtool.exe" set "SIGNTOOL=%%~fD\x64\signtool.exe"
)
if not defined SIGNTOOL (
  echo CHYBA: Pro podpis nebyl nalezen signtool.exe.
  exit /b 1
)
if not defined SIGN_TIMESTAMP_URL set "SIGN_TIMESTAMP_URL=http://timestamp.digicert.com"

if defined SIGN_CERT_PATH (
  if defined SIGN_CERT_PASSWORD (
    "%SIGNTOOL%" sign /fd SHA256 /f "%SIGN_CERT_PATH%" /p "%SIGN_CERT_PASSWORD%" /tr "%SIGN_TIMESTAMP_URL%" /td SHA256 /d "MV Media Downloader" "%SIGN_TARGET%"
  ) else (
    "%SIGNTOOL%" sign /fd SHA256 /f "%SIGN_CERT_PATH%" /tr "%SIGN_TIMESTAMP_URL%" /td SHA256 /d "MV Media Downloader" "%SIGN_TARGET%"
  )
  exit /b %ERRORLEVEL%
)

"%SIGNTOOL%" sign /fd SHA256 /sha1 "%SIGN_CERT_SHA1%" /tr "%SIGN_TIMESTAMP_URL%" /td SHA256 /d "MV Media Downloader" "%SIGN_TARGET%"
exit /b %ERRORLEVEL%
