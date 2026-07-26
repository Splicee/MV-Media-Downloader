param(
    [string]$YtDlpPath = ""
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($YtDlpPath)) {
    $YtDlpPath = Join-Path $root "dist\yt-dlp.exe"
    if (-not (Test-Path -LiteralPath $YtDlpPath)) {
        $YtDlpPath = Join-Path $root "tools\yt-dlp.exe"
    }
}
if (-not (Test-Path -LiteralPath $YtDlpPath)) {
    throw "yt-dlp nebyl nalezen. Spusť nejprve package.cmd nebo tools\update-ytdlp.ps1."
}
try {
    $versionOutput = & $YtDlpPath --version 2>&1
    $versionExitCode = $LASTEXITCODE
    $ytDlpVersion = $versionOutput | Select-Object -First 1
    if ($versionExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($ytDlpVersion)) {
        throw "neplatný výstup"
    }
}
catch {
    throw "yt-dlp nelze spustit. Obnov ho přes tools\update-ytdlp.ps1."
}
$ErrorActionPreference = "Continue"

$tests = @(
    [pscustomobject]@{ Name = "TV Nova"; Required = $true; Extra = @(); Url = "https://media.cms.nova.cz/embed/KybpWYvcgOa" },
    [pscustomobject]@{ Name = "Český rozhlas"; Required = $true; Extra = @(); Url = "https://prehravac.rozhlas.cz/audio/3421320" },
    [pscustomobject]@{ Name = "MůjRozhlas"; Required = $true; Extra = @("--impersonate", "chrome"); Url = "https://www.mujrozhlas.cz/na-nedelni-vlne-hradce-kralove/francesco-kinsky-dal-borgo-pribeh-navratu-k-rodovemu-dedictvi" },
    [pscustomobject]@{ Name = "Rozhlas Vltava"; Required = $true; Extra = @(); Url = "https://wave.rozhlas.cz/papej-masicko-porcujeme-a-bilancujeme-filmy-a-serialy-ktere-letos-zabily-8891337" },
    [pscustomobject]@{ Name = "Stream.cz"; Required = $true; Extra = @(); Url = "https://www.stream.cz/kdo-to-mluvi/kdo-to-mluvi-velke-odhaleni-prinasi-novy-porad-uz-od-25-srpna-64087937" },
    [pscustomobject]@{ Name = "Televize Seznam"; Required = $true; Extra = @(); Url = "https://www.televizeseznam.cz/video/lajna/buh-57953890" },
    [pscustomobject]@{ Name = "TV Noe"; Required = $true; Extra = @(); Url = "https://www.tvnoe.cz/porad/43216-outdoor-films-s-mudr-tomasem-kempnym-pomahat-potrebnym-nejen-u-nas" },
    [pscustomobject]@{ Name = "DVTV / Aktuálně"; Required = $true; Extra = @(); Url = "https://video.aktualne.cz/dvtv/zeman-si-jen-leci-mindraky-sobotku-nenavidi-a-babis-se-mu-te/r~960cdb3a365a11e7a83b0025900fea04/" },
    [pscustomobject]@{ Name = "Česká televize"; Required = $false; Extra = @(); Url = "https://www.ceskatelevize.cz/porady/17506124194-pad/226388771320001/" },
    [pscustomobject]@{ Name = "Prima+"; Required = $false; Extra = @(); Url = "https://prima.iprima.cz/particka/92-epizoda" },
    [pscustomobject]@{ Name = "CNN Prima"; Required = $false; Extra = @(); Url = "https://cnn.iprima.cz/porady/zpravy/zpravy-26-7-v-10-00-1" },
    [pscustomobject]@{ Name = "Seznam Zprávy"; Required = $false; Extra = @(); Url = "https://www.seznamzpravy.cz/clanek/zahranicni-video-hitleruv-rodny-dum-se-zmenil-na-policejni-stanici-311574" },
    [pscustomobject]@{ Name = "iDNES / Playtvak"; Required = $false; Extra = @(); Url = "http://zpravy.idnes.cz/pes-zavreny-v-aute-rozbijeni-okynek-v-aute-fj5-/domaci.aspx?c=A150809_104116_domaci_pku" }
)

$failed = 0
$passed = 0
$limited = 0
foreach ($test in $tests) {
    $arguments = @(
        "--ignore-config",
        "--skip-download",
        "--playlist-end", "1",
        "--no-warnings",
        "--print", "%(extractor_key)s | %(id)s | %(title)s | %(format_id)s"
    ) + $test.Extra + @("--", $test.Url)
    $output = & $YtDlpPath @arguments 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | ForEach-Object { $_.ToString() }) -join "`n"
    $hasMetadata = $exitCode -eq 0 -and -not [string]::IsNullOrWhiteSpace($text)

    if ($hasMetadata) {
        Write-Host ("OK       {0}" -f $test.Name) -ForegroundColor Green
        $passed++
        continue
    }
    if (-not $test.Required) {
        $lastLine = $output | Select-Object -Last 1
        $reason = if ($null -eq $lastLine) { "" } else { $lastLine.ToString() }
        if ([string]::IsNullOrWhiteSpace($reason)) {
            $reason = "zdroj nevrátil metadata"
        }
        Write-Host ("OMEZENÍ  {0}: {1}" -f $test.Name, $reason) -ForegroundColor Yellow
        $limited++
        continue
    }

    $lastLine = $output | Select-Object -Last 1
    $reason = if ($null -eq $lastLine) { "zdroj nevrátil metadata" } else { $lastLine.ToString() }
    Write-Host ("CHYBA     {0}: {1}" -f $test.Name, $reason) -ForegroundColor Red
    $failed++
}

Write-Host ""
Write-Host ("Výsledek: {0} funkčních, {1} známých omezení, {2} neočekávaných chyb." -f $passed, $limited, $failed)
exit $(if ($failed -eq 0) { 0 } else { 1 })
