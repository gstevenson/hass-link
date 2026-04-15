# Generates assets/icon.ico from code — no design tool needed.
# Run from the repo root or the assets/ folder.
# Requires Windows (uses System.Drawing).

Add-Type -AssemblyName System.Drawing

function Add-RoundedRect {
    param(
        [System.Drawing.Drawing2D.GraphicsPath]$Path,
        [System.Drawing.Rectangle]$Rect,
        [int]$Radius
    )
    $d = $Radius * 2
    $Path.AddLine($Rect.Left + $Radius, $Rect.Top, $Rect.Right - $Radius, $Rect.Top)
    $Path.AddArc($Rect.Right - $d, $Rect.Top, $d, $d, 270, 90)
    $Path.AddLine($Rect.Right, $Rect.Top + $Radius, $Rect.Right, $Rect.Bottom - $Radius)
    $Path.AddArc($Rect.Right - $d, $Rect.Bottom - $d, $d, $d, 0, 90)
    $Path.AddLine($Rect.Right - $Radius, $Rect.Bottom, $Rect.Left + $Radius, $Rect.Bottom)
    $Path.AddArc($Rect.Left, $Rect.Bottom - $d, $d, $d, 90, 90)
    $Path.AddLine($Rect.Left, $Rect.Bottom - $Radius, $Rect.Left, $Rect.Top + $Radius)
    $Path.AddArc($Rect.Left, $Rect.Top, $d, $d, 180, 90)
    $Path.CloseFigure()
}

function New-IconBitmap {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded square — #6366f1
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 99, 102, 241))
    $radius  = [Math]::Max(2, [int]($Size * 0.2))
    $rect    = [System.Drawing.Rectangle]::new(0, 0, $Size - 1, $Size - 1)
    $path    = New-Object System.Drawing.Drawing2D.GraphicsPath
    Add-RoundedRect -Path $path -Rect $rect -Radius $radius
    $g.FillPath($bgBrush, $path)

    # "HL" in white — skip at 16px (too small to be legible)
    if ($Size -ge 24) {
        $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
        $fontSize  = [float]($Size * 0.415)
        $font      = New-Object System.Drawing.Font("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $sf        = New-Object System.Drawing.StringFormat
        $sf.Alignment     = [System.Drawing.StringAlignment]::Center
        $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
        $g.DrawString("HL", $font, $textBrush, [System.Drawing.RectangleF]::new(0, 0, $Size, $Size), $sf)
        $font.Dispose(); $textBrush.Dispose(); $sf.Dispose()
    }

    $bgBrush.Dispose(); $path.Dispose(); $g.Dispose()
    return $bmp
}

$sizes = @(16, 32, 48, 256)

# Render each size to PNG bytes
$pngData = New-Object 'System.Collections.Generic.List[byte[]]'
foreach ($s in $sizes) {
    $bmp = New-IconBitmap -Size $s
    $ms  = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngData.Add($ms.ToArray())
    $ms.Dispose(); $bmp.Dispose()
}

# Write ICO file (ICONDIR + ICONDIRENTRYs + PNG data)
$outPath = Join-Path $PSScriptRoot "icon.ico"
$fs      = New-Object System.IO.FileStream($outPath, [System.IO.FileMode]::Create)
$writer  = New-Object System.IO.BinaryWriter($fs)

$count      = $sizes.Count
$dataOffset = 6 + (16 * $count)

# Calculate image offsets
$offsets = New-Object 'System.Collections.Generic.List[int]'
$off = $dataOffset
for ($i = 0; $i -lt $count; $i++) {
    $offsets.Add($off)
    $off += $pngData[$i].Length
}

# ICONDIR
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$count)

# ICONDIRENTRYs
for ($i = 0; $i -lt $count; $i++) {
    $s = $sizes[$i]
    $wh = if ($s -eq 256) { [byte]0 } else { [byte]$s }
    $writer.Write($wh)  # width
    $writer.Write($wh)  # height
    $writer.Write([byte]0)      # colorCount
    $writer.Write([byte]0)      # reserved
    $writer.Write([uint16]1)    # planes
    $writer.Write([uint16]32)   # bitCount
    $writer.Write([uint32]$pngData[$i].Length)
    $writer.Write([uint32]$offsets[$i])
}

# Image data
for ($i = 0; $i -lt $count; $i++) {
    $writer.Write([byte[]]$pngData[$i])
}

$writer.Close(); $fs.Close()
Write-Host "icon.ico written to $outPath" -ForegroundColor Green
