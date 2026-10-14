Add-Type -AssemblyName System.Drawing
$taskRoot = (Get-Location).Path
$taskAsset = Join-Path $taskRoot 'Assets/04.Sprites/UI/Minimap/Stage01_Minimap_Frame.png'
$taskOriginal = [Drawing.Bitmap]::FromFile((Join-Path $taskRoot 'tmp/minimap-corridor-fix/Stage01_Minimap_Frame.before.png'))
$taskBitmap = [Drawing.Bitmap]::FromFile($taskAsset)
$taskResult = [Drawing.Bitmap]::new($taskBitmap)
$taskFill = [Drawing.Color]::FromArgb(255,72,82,125)
$taskLine = [Drawing.Color]::FromArgb(255,180,200,234)
$taskClear = [Drawing.Color]::FromArgb(0,0,0,0)
function PaintRect([int]$left,[int]$top,[int]$right,[int]$bottom,[Drawing.Color]$color) {
    for ($y=$top; $y -le $bottom; $y++) {
        for ($x=$left; $x -le $right; $x++) { $taskResult.SetPixel($x,$y,$color) }
    }
}
# Restore the adjoining original linework before aligning the two junctions.
foreach ($region in @(@(88,114,189,216),@(403,423,184,230))) {
    for ($y=$region[2];$y -le $region[3];$y++) {
        for ($x=$region[0];$x -le $region[1];$x++) { $taskResult.SetPixel($x,$y,$taskOriginal.GetPixel($x,$y)) }
    }
}
# Southwest: continue the existing x=109 thin edge down to the room corner.
PaintRect 95 189 110 202 $taskFill
PaintRect 97 189 97 202 $taskLine
PaintRect 91 203 110 208 $taskFill
PaintRect 89 203 96 203 $taskLine
PaintRect 89 204 89 208 $taskLine
PaintRect 87 209 110 211 $taskFill
PaintRect 85 207 89 208 $taskLine
PaintRect 85 209 85 211 $taskLine
PaintRect 109 189 109 212 $taskLine
PaintRect 110 203 110 211 $taskClear
PaintRect 88 212 108 212 $taskFill
PaintRect 109 212 113 212 $taskLine
PaintRect 88 213 112 213 $taskFill
PaintRect 113 213 113 216 $taskLine
# Southeast: match the existing straight upper edges at x=407 and x=418.
PaintRect 405 185 420 227 $taskClear
PaintRect 407 185 418 229 $taskFill
PaintRect 407 185 407 228 $taskLine
PaintRect 418 184 418 228 $taskLine
PaintRect 403 228 407 228 $taskLine
PaintRect 408 228 417 229 $taskFill
PaintRect 419 228 423 229 $taskLine
$taskOutput=Join-Path $taskRoot 'tmp/minimap-corridor-fix/Stage01_Minimap_Frame.thin.png'
$taskResult.Save($taskOutput,[Drawing.Imaging.ImageFormat]::Png)
$taskOriginal.Dispose()
$taskBitmap.Dispose()
$taskResult.Dispose()
Copy-Item -LiteralPath $taskOutput -Destination $taskAsset -Force
