Add-Type -AssemblyName System.Drawing
$taskPath = Join-Path (Get-Location) 'Assets/04.Sprites/UI/Minimap/Stage01_Minimap_Frame.png'
$taskSource = [Drawing.Bitmap]::FromFile($taskPath)
$taskResult = [Drawing.Bitmap]::new($taskSource)
$taskWidth = $taskSource.Width
$taskHeight = $taskSource.Height
$taskMask = [bool[]]::new($taskWidth * $taskHeight)
for ($y = 0; $y -lt $taskHeight; $y++) {
    for ($x = 0; $x -lt $taskWidth; $x++) {
        $taskMask[$y * $taskWidth + $x] = $taskSource.GetPixel($x, $y).A -gt 0
    }
}
# Join each passage to its room; keep room footprints outside these junctions.
foreach ($taskStrip in @(@(95,111,190,213), @(241,257,194,221), @(405,420,185,229))) {
    for ($y = $taskStrip[2]; $y -le $taskStrip[3]; $y++) {
        for ($x = $taskStrip[0]; $x -le $taskStrip[1]; $x++) {
            $taskMask[$y * $taskWidth + $x] = $true
        }
    }
}
# Remove the old right-hand kink, aligning both sides with the upper passage.
for ($y = 203; $y -le 227; $y++) { $taskMask[$y * $taskWidth + 421] = $false }
$taskFill = [Drawing.Color]::FromArgb(255,72,82,125)
$taskEdge = [Drawing.Color]::FromArgb(255,180,200,234)
$taskClear = [Drawing.Color]::FromArgb(0,0,0,0)
$taskChanged = 0
foreach ($taskPatch in @(@(88,113,189,215), @(239,260,193,223), @(403,423,184,230))) {
    for ($y = $taskPatch[2]; $y -le $taskPatch[3]; $y++) {
        for ($x = $taskPatch[0]; $x -le $taskPatch[1]; $x++) {
            $taskColor = $taskClear
            if ($taskMask[$y * $taskWidth + $x]) {
                $taskColor = $taskFill
                foreach ($taskDelta in @(@(-1,0),@(1,0),@(0,-1),@(0,1),@(-2,0),@(2,0),@(0,-2),@(0,2))) {
                    $nx = $x + $taskDelta[0]
                    $ny = $y + $taskDelta[1]
                    if (!$taskMask[$ny * $taskWidth + $nx]) { $taskColor = $taskEdge; break }
                }
            }
            if ($taskSource.GetPixel($x,$y).ToArgb() -ne $taskColor.ToArgb()) { $taskChanged++ }
            $taskResult.SetPixel($x,$y,$taskColor)
        }
    }
}
$taskOutput = Join-Path (Get-Location) 'tmp/minimap-corridor-fix/Stage01_Minimap_Frame.fixed.png'
$taskResult.Save($taskOutput,[Drawing.Imaging.ImageFormat]::Png)
$taskSource.Dispose()
$taskResult.Dispose()
Copy-Item -LiteralPath $taskOutput -Destination $taskPath -Force
Write-Output "Updated $taskWidth x $taskHeight map; changed $taskChanged pixels inside three junction patches."
