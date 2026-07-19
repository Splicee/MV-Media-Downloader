param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\assets\MVMediaStudio.ico")
)

Add-Type -AssemblyName System.Drawing
$directory = Split-Path -Parent $OutputPath
if (!(Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory | Out-Null
}

$bitmap = New-Object System.Drawing.Bitmap 256, 256
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$radius = 48
$path.AddArc(8, 8, $radius, $radius, 180, 90)
$path.AddArc(200, 8, $radius, $radius, 270, 90)
$path.AddArc(200, 200, $radius, $radius, 0, 90)
$path.AddArc(8, 200, $radius, $radius, 90, 90)
$path.CloseFigure()

$background = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point 20, 20),
    (New-Object System.Drawing.Point 236, 236),
    ([System.Drawing.Color]::FromArgb(32, 164, 243)),
    ([System.Drawing.Color]::FromArgb(8, 112, 190)))
$graphics.FillPath($background, $path)

$font = New-Object System.Drawing.Font "Segoe UI", 78, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center
$graphics.DrawString("MV", $font, [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF 0, 0, 256, 248), $format)
$bitmap.Save([System.IO.Path]::ChangeExtension($OutputPath, ".png"), [System.Drawing.Imaging.ImageFormat]::Png)

$png = New-Object System.IO.MemoryStream
$bitmap.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
$bytes = $png.ToArray()
$stream = [System.IO.File]::Create($OutputPath)
$writer = New-Object System.IO.BinaryWriter $stream
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]1)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$bytes.Length)
    $writer.Write([uint32]22)
    $writer.Write($bytes)
}
finally {
    $writer.Dispose()
    $png.Dispose()
    $font.Dispose()
    $format.Dispose()
    $background.Dispose()
    $path.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}
