param(
    [string]$TargetPath = (Join-Path $PSScriptRoot "yt-dlp.exe")
)

$ErrorActionPreference = "Stop"
$downloadUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"
$checksumsUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS"
$temporaryPath = "$TargetPath.download"

try {
    $headers = @{ "User-Agent" = "MV-Media-Downloader-Build/3.0" }
    $checksumResponse = Invoke-WebRequest -UseBasicParsing -Uri $checksumsUrl -Headers $headers
    $checksums = if ($checksumResponse.Content -is [byte[]]) {
        [System.Text.Encoding]::UTF8.GetString($checksumResponse.Content)
    } else {
        [string]$checksumResponse.Content
    }
    $hashMatch = [regex]::Match($checksums, "(?im)^([a-fA-F0-9]{64})\s+\*?yt-dlp\.exe\s*$")
    if (!$hashMatch.Success) {
        throw "V oficiálním seznamu nebyl nalezen SHA-256 pro yt-dlp.exe."
    }
    $expected = $hashMatch.Groups[1].Value.ToUpperInvariant()

    Invoke-WebRequest -UseBasicParsing -Uri $downloadUrl -Headers $headers -OutFile $temporaryPath
    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $temporaryPath).Hash.ToUpperInvariant()
    if ($actual -ne $expected) {
        throw "SHA-256 staženého yt-dlp.exe nesouhlasí."
    }

    Move-Item -LiteralPath $temporaryPath -Destination $TargetPath -Force
    & $TargetPath --version
    Write-Output "SHA256 $actual"
}
finally {
    if (Test-Path -LiteralPath $temporaryPath) {
        Remove-Item -LiteralPath $temporaryPath -Force
    }
}
