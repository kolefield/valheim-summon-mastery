# Original geometric skull, portal and rank bars. No game or external artwork.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$bitmap = [Drawing.Bitmap]::new(256, 256)
$g = [Drawing.Graphics]::FromImage($bitmap)
$g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$background = [Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Point]::new(0,0), [Drawing.Point]::new(256,256), [Drawing.ColorTranslator]::FromHtml('#152831'), [Drawing.ColorTranslator]::FromHtml('#080D18'))
$bone = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#EEE2C9'))
$dark = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#12232A'))
$mint = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#68E5C0'), 6)
$gold = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#E5B96B'), 5)
$thin = [Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#365861'), 2)
$font = [Drawing.Font]::new('Segoe UI', 20, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
$format = [Drawing.StringFormat]::new()
$format.Alignment = [Drawing.StringAlignment]::Center
try {
    $g.FillRectangle($background, 0, 0, 256, 256)
    $g.DrawRectangle($thin, 8, 8, 239, 239)
    $g.DrawEllipse($mint, 52, 23, 152, 161)
    $g.DrawEllipse($thin, 42, 13, 172, 181)
    $g.FillEllipse($bone, 86, 57, 84, 76)
    $g.FillRectangle($bone, 100, 105, 56, 44)
    $g.FillEllipse($dark, 99, 88, 21, 24)
    $g.FillEllipse($dark, 136, 88, 21, 24)
    $g.FillPolygon($dark, [Drawing.Point[]]@([Drawing.Point]::new(128,110), [Drawing.Point]::new(120,125), [Drawing.Point]::new(136,125)))
    foreach ($x in @(111,124,137)) { $g.FillRectangle($dark, $x, 135, 5, 14) }
    foreach ($bar in @(@(28,142,28,162), @(37,132,37,162), @(46,122,46,162), @(55,112,55,162))) {
        $g.DrawLine($gold, $bar[0], $bar[1], $bar[2], $bar[3])
    }
    $g.DrawString('SUMMON', $font, $bone, [Drawing.RectangleF]::new(10,190,236,27), $format)
    $g.DrawString('MASTERY', $font, $bone, [Drawing.RectangleF]::new(10,216,236,27), $format)
    $bitmap.Save((Join-Path (Split-Path $PSScriptRoot) 'icon.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally {
    $format.Dispose(); $font.Dispose(); $thin.Dispose(); $gold.Dispose(); $mint.Dispose()
    $dark.Dispose(); $bone.Dispose(); $background.Dispose(); $g.Dispose(); $bitmap.Dispose()
}
