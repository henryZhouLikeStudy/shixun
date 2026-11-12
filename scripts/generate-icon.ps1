# SPDX-License-Identifier: GPL-3.0-only
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$null = New-Item -ItemType Directory -Path (Join-Path $root 'assets') -Force
$images = @()
foreach ($size in @(16,20,24,32,48,64,128,256)) {
    $bitmap = [Drawing.Bitmap]::new(512,512)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $path = [Drawing.Drawing2D.GraphicsPath]::new()
    foreach ($corner in @(@(8,8,180),@(344,8,270),@(344,344,0),@(8,344,90))) {
        $path.AddArc($corner[0],$corner[1],160,160,$corner[2],90)
    }
    $path.CloseFigure()
    $brush = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#df653f'))
    $g.FillPath($brush,$path)
    $pen = [Drawing.Pen]::new([Drawing.Color]::White,36)
    $pen.StartCap = $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($pen,8,264,240,240,270,90)
    $g.DrawArc($pen,-112,144,480,480,270,90)
    $g.FillEllipse([Drawing.Brushes]::White,104,360,48,48)
    $small = [Drawing.Bitmap]::new($size,$size)
    $sg = [Drawing.Graphics]::FromImage($small)
    $sg.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $sg.DrawImage($bitmap,0,0,$size,$size)
    $stream = [IO.MemoryStream]::new()
    $small.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
    $images += ,@{size=$size;bytes=$stream.ToArray()}
    $stream.Dispose();$sg.Dispose();$small.Dispose();$pen.Dispose();$brush.Dispose();$path.Dispose();$g.Dispose();$bitmap.Dispose()
}
$file = [IO.File]::Create((Join-Path $root 'assets/Shixun.ico'))
$writer = [IO.BinaryWriter]::new($file)
$writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$images.Count)
$offset = 6 + 16*$images.Count
foreach ($item in $images) {
    $dim = if($item.size -eq 256){0}else{$item.size}
    $writer.Write([byte]$dim);$writer.Write([byte]$dim);$writer.Write([uint16]0)
    $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$item.bytes.Length);$writer.Write([uint32]$offset)
    $offset += $item.bytes.Length
}
foreach($item in $images){$writer.Write([byte[]]$item.bytes)}
$writer.Dispose()
Write-Output 'Generated eight-resolution application icon.'
