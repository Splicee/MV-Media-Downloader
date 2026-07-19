@echo off
setlocal EnableExtensions

set "ROOT=%~dp0"
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
set "TEST_EXE=%TEMP%\mv-media-downloader-tests.exe"
set "UPDATER_TEST_EXE=%TEMP%\mv-media-updater-tests.exe"

powershell.exe -NoProfile -Command "$files=Get-ChildItem -LiteralPath '%ROOT%' -Recurse -File | Where-Object { $_.FullName -notmatch '\\(dist|release|artifacts|\.git)\\' }; $leaks=$files | Select-String -Pattern 'AIza[0-9A-Za-z_-]{20,}'; if($leaks){$leaks | ForEach-Object { Write-Host ('CHYBA: Podezrely klic v ' + $_.Path + ':' + $_.LineNumber) }; exit 1}"
if errorlevel 1 exit /b 1
powershell.exe -NoProfile -Command "$plugin=[IO.File]::ReadAllText('%ROOT%yt-dlp-plugins\mv-joj-play\yt_dlp_plugins\extractor\jojplay.py'); if($plugin -notmatch '_REFRESH_TOKEN' -or $plugin -notmatch 'refresh_token' -or $plugin -notmatch '_TOKEN_EXPIRES_AT'){Write-Host 'CHYBA: JOJ konektor nema obnovu relace.'; exit 1}"
if errorlevel 1 exit /b 1

"%CSC%" /nologo /target:exe /platform:anycpu /optimize+ /nowarn:0649 /codepage:65001 /out:"%TEST_EXE%" /reference:System.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll "%ROOT%src\Core\Models.cs" "%ROOT%src\Core\ArgumentBuilders.cs" "%ROOT%src\Core\DownloadUrlParser.cs" "%ROOT%src\Core\ScrollWheelTuning.cs" "%ROOT%src\Core\UpdateMetadata.cs" "%ROOT%src\Services\JojUrlResolver.cs" "%ROOT%tests\CoreTests.cs"
if errorlevel 1 exit /b 1
"%TEST_EXE%"
if errorlevel 1 goto :Failed

"%CSC%" /nologo /target:exe /platform:anycpu /optimize+ /codepage:65001 /out:"%UPDATER_TEST_EXE%" /reference:System.dll /reference:System.Core.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "%ROOT%updater\Updater.cs"
if errorlevel 1 goto :Failed
"%UPDATER_TEST_EXE%" --self-test
if errorlevel 1 goto :Failed

del /Q "%TEST_EXE%" "%UPDATER_TEST_EXE%" >nul 2>nul
exit /b 0

:Failed
set "RESULT=%ERRORLEVEL%"
del /Q "%TEST_EXE%" "%UPDATER_TEST_EXE%" >nul 2>nul
exit /b %RESULT%
