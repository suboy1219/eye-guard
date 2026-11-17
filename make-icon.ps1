$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$ico = Join-Path $root "app.ico"

Add-Type -AssemblyName System.Drawing

function New-RoundRect([System.Drawing.RectangleF]$rect, [float]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    if ($d -gt $rect.Width) { $d = $rect.Width }
    if ($d -gt $rect.Height) { $d = $rect.Height }
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $pad = [Math]::Max(0.0, $s * 0.015)
    $rect = New-Object System.Drawing.RectangleF($pad, $pad, ($s - 2 * $pad), ($s - 2 * $pad))
    $radius = [Math]::Max(2.0, $s * 0.235)
    $bgPath = New-RoundRect $rect $radius

    $top = [System.Drawing.Color]::FromArgb(255, 96, 235, 201)
    $bottom = [System.Drawing.Color]::FromArgb(255, 24, 158, 132)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $top, $bottom, 62)
    $g.FillPath($brush, $bgPath)

    $cx = $s / 2.0
    $cy = $s * 0.505
    $ew = $s * 0.60
    $eh = $s * 0.30
    $ink = [System.Drawing.Color]::FromArgb(255, 10, 30, 34)

    $eyePen = New-Object System.Drawing.Pen($ink, [Math]::Max(1.15, $s * 0.072))
    $eyePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $eyePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $eyePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $eye = New-Object System.Drawing.Drawing2D.GraphicsPath
    $eye.AddBezier(
        [float]($cx - $ew / 2), [float]$cy,
        [float]($cx - $ew * 0.17), [float]($cy - $eh),
        [float]($cx + $ew * 0.17), [float]($cy - $eh),
        [float]($cx + $ew / 2), [float]$cy)
    $eye.AddBezier(
        [float]($cx + $ew / 2), [float]$cy,
        [float]($cx + $ew * 0.17), [float]($cy + $eh),
        [float]($cx - $ew * 0.17), [float]($cy + $eh),
        [float]($cx - $ew / 2), [float]$cy)
    $g.DrawPath($eyePen, $eye)

    $pr = [Math]::Max(1.4, $s * 0.098)
    $pupil = New-Object System.Drawing.SolidBrush($ink)
    $g.FillEllipse($pupil, [float]($cx - $pr), [float]($cy - $pr), [float]($pr * 2), [float]($pr * 2))

    if ($s -ge 24) {
        $hr = [Math]::Max(0.8, $s * 0.030)
        $hl = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
        $g.FillEllipse($hl, [float]($cx - $pr * 0.40 - $hr), [float]($cy - $pr * 0.40 - $hr), [float]($hr * 2), [float]($hr * 2))
        $hl.Dispose()
    }

    $eyePen.Dispose()
    $eye.Dispose()
    $pupil.Dispose()
    $brush.Dispose()
    $bgPath.Dispose()
    $g.Dispose()
    return $bmp
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$entries = New-Object System.Collections.ArrayList

foreach ($size in $sizes) {
    $bmp = New-IconBitmap $size
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $ms.ToArray()
    [void]$entries.Add([pscustomobject]@{ Size = $size; Data = $bytes })

    if ($size -eq 256) {
        $preview = Join-Path $root "docs\icon.png"
        $bmp.Save($preview, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    $ms.Dispose()
    $bmp.Dispose()
}

$fs = [System.IO.File]::Create($ico)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$entries.Count)

$offset = 6 + 16 * $entries.Count
foreach ($entry in $entries) {
    if ($entry.Size -ge 256) { $dim = [Byte]0 } else { $dim = [Byte]$entry.Size }
    $bw.Write($dim)
    $bw.Write($dim)
    $bw.Write([Byte]0)
    $bw.Write([Byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$entry.Data.Length)
    $bw.Write([UInt32]$offset)
    $offset = $offset + $entry.Data.Length
}

foreach ($entry in $entries) {
    $bw.Write($entry.Data)
}

$bw.Flush()
$bw.Close()
$fs.Close()

Write-Host ("Icon generated: {0} ({1} sizes)" -f $ico, $entries.Count)
