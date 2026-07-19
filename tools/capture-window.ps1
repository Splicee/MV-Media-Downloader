param(
    [Parameter(Mandatory = $true)][string]$ExePath,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [string]$Arguments = "",
    [switch]$ScreenCapture
)

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class WindowCaptureNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint flags);
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
}
"@

Add-Type -AssemblyName System.Drawing
$process = if ([string]::IsNullOrWhiteSpace($Arguments)) {
    Start-Process -FilePath $ExePath -PassThru
} else {
    Start-Process -FilePath $ExePath -ArgumentList $Arguments -PassThru
}
try {
    $handle = [IntPtr]::Zero
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        Start-Sleep -Milliseconds 200
        $process.Refresh()
        $handle = $process.MainWindowHandle
        if ($handle -ne [IntPtr]::Zero) { break }
    }
    if ($handle -eq [IntPtr]::Zero) { throw "Okno aplikace nebylo nalezeno." }

    [WindowCaptureNative]::SetForegroundWindow($handle) | Out-Null
    if ($ScreenCapture) {
        [WindowCaptureNative]::SetWindowPos($handle, [IntPtr](-1), 0, 0, 0, 0, 0x0043) | Out-Null
    }
    Start-Sleep -Milliseconds 1800
    $rect = New-Object WindowCaptureNative+RECT
    [WindowCaptureNative]::GetWindowRect($handle, [ref]$rect) | Out-Null
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        if ($ScreenCapture) {
            $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
        } else {
            $hdc = $graphics.GetHdc()
            try {
                [WindowCaptureNative]::PrintWindow($handle, $hdc, 2) | Out-Null
            }
            finally {
                $graphics.ReleaseHdc($hdc)
            }
        }
        $bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}
finally {
    if ($null -ne $process -and !$process.HasExited) { Stop-Process -Id $process.Id -Force }
}
