@echo off
setlocal EnableExtensions

set "ROOT=%~dp0"
set "PACKAGE=%ROOT%release\MV-Media-Downloader-win-x64.zip"
set "CHECKSUM=%PACKAGE%.sha256"

call "%ROOT%build.cmd"
if errorlevel 1 exit /b 1
if not exist "%ROOT%release" mkdir "%ROOT%release"
if exist "%PACKAGE%" del /Q "%PACKAGE%"
if exist "%CHECKSUM%" del /Q "%CHECKSUM%"

powershell.exe -NoProfile -Command "Compress-Archive -LiteralPath '%ROOT%dist\MV Media Downloader.exe','%ROOT%dist\MV Media Downloader Updater.exe','%ROOT%dist\yt-dlp.exe','%ROOT%dist\yt-dlp-plugins','%ROOT%README.md','%ROOT%THIRD_PARTY_NOTICES.md' -DestinationPath '%PACKAGE%' -CompressionLevel Optimal"
if errorlevel 1 exit /b 1

powershell.exe -NoProfile -Command "$hash=(Get-FileHash -LiteralPath '%PACKAGE%' -Algorithm SHA256).Hash.ToLowerInvariant(); [IO.File]::WriteAllText('%CHECKSUM%', $hash + '  MV-Media-Downloader-win-x64.zip' + [Environment]::NewLine, [Text.Encoding]::ASCII)"
if errorlevel 1 exit /b 1

echo HOTOVO: release\MV-Media-Downloader-win-x64.zip
echo HOTOVO: release\MV-Media-Downloader-win-x64.zip.sha256
exit /b 0
