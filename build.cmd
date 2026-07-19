@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0"
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not defined BUILD_OUTPUT_DIR set "BUILD_OUTPUT_DIR=%ROOT%dist"
set "APP_OUTPUT=%BUILD_OUTPUT_DIR%\MV Media Downloader.exe"
set "UPDATER_OUTPUT=%BUILD_OUTPUT_DIR%\MV Media Downloader Updater.exe"

if not exist "%CSC%" (
  echo CHYBA: Nebyl nalezen systemovy C# kompilator.
  exit /b 1
)
if not exist "%BUILD_OUTPUT_DIR%" mkdir "%BUILD_OUTPUT_DIR%"

set "WINDOWSBASE="
set "PRESENTATIONCORE="
set "PRESENTATIONFRAMEWORK="
set "SYSTEMXAML="
for /R "%WINDIR%\Microsoft.NET\assembly\GAC_MSIL\WindowsBase" %%F in (WindowsBase.dll) do if exist "%%F" set "WINDOWSBASE=%%F"
for /R "%WINDIR%\Microsoft.NET\assembly\GAC_64\PresentationCore" %%F in (PresentationCore.dll) do if exist "%%F" set "PRESENTATIONCORE=%%F"
if not defined PRESENTATIONCORE for /R "%WINDIR%\Microsoft.NET\assembly\GAC_32\PresentationCore" %%F in (PresentationCore.dll) do if exist "%%F" set "PRESENTATIONCORE=%%F"
for /R "%WINDIR%\Microsoft.NET\assembly\GAC_MSIL\PresentationFramework" %%F in (PresentationFramework.dll) do if exist "%%F" set "PRESENTATIONFRAMEWORK=%%F"
for /R "%WINDIR%\Microsoft.NET\assembly\GAC_MSIL\System.Xaml" %%F in (System.Xaml.dll) do if exist "%%F" set "SYSTEMXAML=%%F"

if not defined WINDOWSBASE goto :MissingWpf
if not defined PRESENTATIONCORE goto :MissingWpf
if not defined PRESENTATIONFRAMEWORK goto :MissingWpf
if not defined SYSTEMXAML goto :MissingWpf

set "SOURCES="
for /R "%ROOT%src" %%F in (*.cs) do set "SOURCES=!SOURCES! "%%F""
set "ICON_ARG="
if exist "%ROOT%assets\MVMediaStudio.ico" set "ICON_ARG=/win32icon:"%ROOT%assets\MVMediaStudio.ico""

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /out:"%APP_OUTPUT%" /win32manifest:"%ROOT%src\app.manifest" %ICON_ARG% /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll /reference:"%SYSTEMXAML%" /reference:"%WINDOWSBASE%" /reference:"%PRESENTATIONCORE%" /reference:"%PRESENTATIONFRAMEWORK%" /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll !SOURCES!
if errorlevel 1 (
  echo CHYBA: Sestaveni aplikace se nepovedlo.
  exit /b 1
)

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /out:"%UPDATER_OUTPUT%" /reference:System.dll /reference:System.Core.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "%ROOT%updater\Updater.cs"
if errorlevel 1 (
  echo CHYBA: Sestaveni aktualizatoru se nepovedlo.
  exit /b 1
)

if exist "%ROOT%tools\yt-dlp.exe" copy /Y "%ROOT%tools\yt-dlp.exe" "%BUILD_OUTPUT_DIR%\yt-dlp.exe" >nul
if exist "%BUILD_OUTPUT_DIR%\yt-dlp-plugins" rmdir /S /Q "%BUILD_OUTPUT_DIR%\yt-dlp-plugins"
if exist "%ROOT%yt-dlp-plugins" xcopy "%ROOT%yt-dlp-plugins" "%BUILD_OUTPUT_DIR%\yt-dlp-plugins\" /E /I /Y >nul

call :SignIfConfigured "%APP_OUTPUT%"
if errorlevel 1 exit /b 1
call :SignIfConfigured "%UPDATER_OUTPUT%"
if errorlevel 1 exit /b 1

echo HOTOVO: %APP_OUTPUT%
echo HOTOVO: %UPDATER_OUTPUT%
exit /b 0

:MissingWpf
echo CHYBA: V systemu chybi knihovny Windows Presentation Foundation.
exit /b 1

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
