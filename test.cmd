@echo off
setlocal EnableExtensions
chcp 65001 >nul

set "ROOT=%~dp0"
set "SOLUTION=%ROOT%MV Media Downloader.sln"
set "CORE_TESTS=%ROOT%tests\MVMediaStudio.Tests\MVMediaStudio.Tests.csproj"
set "UI_TESTS=%ROOT%tests\MVMediaStudio.UiSmoke\MVMediaStudio.UiSmoke.csproj"
set "UPDATER_PROJECT=%ROOT%updater\MVMediaStudio.Updater\MVMediaStudio.Updater.csproj"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo CHYBA: Nebylo nalezeno .NET SDK. Nainstaluj .NET 10 SDK.
  exit /b 1
)

powershell.exe -NoProfile -Command "$extensions=@('.cs','.xaml','.xml','.json','.yml','.yaml','.ps1','.cmd','.md','.py','.txt','.csproj','.sln'); $files=Get-ChildItem -LiteralPath '%ROOT%' -Recurse -File | Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() -and $_.FullName -notmatch '\\(dist|release|artifacts|bin|obj|\.git)\\' }; $leaks=$files | Select-String -Pattern 'AIza[0-9A-Za-z_-]{20,}'; if($leaks){$leaks | ForEach-Object { Write-Host ('CHYBA: Podezrely klic v ' + $_.Path + ':' + $_.LineNumber) }; exit 1}"
if errorlevel 1 exit /b 1

powershell.exe -NoProfile -Command "$plugin=[IO.File]::ReadAllText('%ROOT%yt-dlp-plugins\mv-joj-play\yt_dlp_plugins\extractor\jojplay.py'); if($plugin -notmatch '_REFRESH_TOKEN' -or $plugin -notmatch 'refresh_token' -or $plugin -notmatch '_TOKEN_EXPIRES_AT'){Write-Host 'CHYBA: JOJ konektor nema obnovu relace.'; exit 1}"
if errorlevel 1 exit /b 1

dotnet restore "%SOLUTION%" --nologo --verbosity minimal
if errorlevel 1 exit /b 1

dotnet build "%SOLUTION%" --configuration Release --no-restore --nologo --verbosity minimal
if errorlevel 1 exit /b 1

dotnet run --project "%CORE_TESTS%" --configuration Release --no-build --no-restore
if errorlevel 1 exit /b 1

dotnet run --project "%UPDATER_PROJECT%" --configuration Release --no-build --no-restore -- --self-test
if errorlevel 1 exit /b 1

dotnet run --project "%UI_TESTS%" --configuration Release --no-build --no-restore
if errorlevel 1 exit /b 1

echo HOTOVO: Vsechny automaticke kontroly prosly.
exit /b 0
