param(
    [string]$StraightSource = "C:\Users\akh10\.codex\generated_images\01a0c7d3-dd34-7320-89ef-a7777882bcd3\exec-9b8e026a-c23d-41bb-8428-ca9bcde6b616.png",
    [string]$CornerSource = "C:\Users\akh10\.codex\generated_images\01a0c7d3-dd34-7320-89ef-a7777882bcd3\exec-97e01c5b-8ba9-46d5-87af-9228b1aa8729.png",
    [string]$OutputDirectory = "Assets\04.Sprites\Tilemaps\DesertSpaceBase\TallWalls\Modular2x"
)

Add-Type -AssemblyName System.Drawing

function Get-AlphaBounds([System.Drawing.Bitmap]$Bitmap) {
    $minX = $Bitmap.Width
    $minY = $Bitmap.Height
    $maxX = -1
    $maxY = -1

    for ($y = 0; $y -lt $Bitmap.Height; $y++) {
        for ($x = 0; $x -lt $Bitmap.Width; $x++) {
            if ($Bitmap.GetPixel($x, $y).A -gt 8) {
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }

    if ($maxX -lt 0) { throw "Source image has no visible pixels." }
    return [System.Drawing.Rectangle]::new($minX, $minY, $maxX - $minX + 1, $maxY - $minY + 1)
}

function Export-Nearest(
    [string]$Source,
    [string]$Destination,
    [int]$Width,
    [int]$Height,
    [bool]$ForceSeam = $false
) {
    $sourceBitmap = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $Source))
    try {
        $bounds = Get-AlphaBounds $sourceBitmap
        $result = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($result)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighSpeed
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
                $graphics.DrawImage($sourceBitmap, [System.Drawing.Rectangle]::new(0, 0, $Width, $Height), $bounds, [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally { $graphics.Dispose() }

            if ($ForceSeam) {
                for ($y = 0; $y -lt $Height; $y++) {
                    $result.SetPixel($Width - 1, $y, $result.GetPixel(0, $y))
                }
            }

            $result.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $result.Dispose() }
    }
    finally { $sourceBitmap.Dispose() }
}

function Export-Rotation([string]$Source, [string]$Destination, [System.Drawing.RotateFlipType]$Rotation) {
    $bitmap = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $Source))
    try {
        $bitmap.RotateFlip($Rotation)
        $bitmap.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bitmap.Dispose() }
}

$resolvedOutput = Join-Path (Get-Location) $OutputDirectory
New-Item -ItemType Directory -Force -Path $resolvedOutput | Out-Null

Copy-Item -LiteralPath $StraightSource -Destination (Join-Path $resolvedOutput "DSB_Wall_Straight_North_Source.png") -Force
Copy-Item -LiteralPath $CornerSource -Destination (Join-Path $resolvedOutput "DSB_Wall_Corner_NW_Source.png") -Force

$north = Join-Path $resolvedOutput "DSB_Wall_Straight_North_32x64.png"
$south = Join-Path $resolvedOutput "DSB_Wall_Straight_South_32x64.png"
$east = Join-Path $resolvedOutput "DSB_Wall_Straight_East_64x32.png"
$west = Join-Path $resolvedOutput "DSB_Wall_Straight_West_64x32.png"

Export-Nearest $StraightSource $north 32 64 $true
Export-Rotation $north $east ([System.Drawing.RotateFlipType]::Rotate90FlipNone)
Export-Rotation $north $south ([System.Drawing.RotateFlipType]::Rotate180FlipNone)
Export-Rotation $north $west ([System.Drawing.RotateFlipType]::Rotate270FlipNone)

$cornerNW = Join-Path $resolvedOutput "DSB_Wall_Corner_NW_64x64.png"
$cornerNE = Join-Path $resolvedOutput "DSB_Wall_Corner_NE_64x64.png"
$cornerSE = Join-Path $resolvedOutput "DSB_Wall_Corner_SE_64x64.png"
$cornerSW = Join-Path $resolvedOutput "DSB_Wall_Corner_SW_64x64.png"

Export-Nearest $CornerSource $cornerNW 64 64 $false
Export-Rotation $cornerNW $cornerNE ([System.Drawing.RotateFlipType]::Rotate90FlipNone)
Export-Rotation $cornerNW $cornerSE ([System.Drawing.RotateFlipType]::Rotate180FlipNone)
Export-Rotation $cornerNW $cornerSW ([System.Drawing.RotateFlipType]::Rotate270FlipNone)

Get-ChildItem -LiteralPath $resolvedOutput -Filter "*.png" |
    Sort-Object Name |
    ForEach-Object {
        $image = [System.Drawing.Image]::FromFile($_.FullName)
        try {
            [PSCustomObject]@{ Name = $_.Name; Width = $image.Width; Height = $image.Height }
        }
        finally { $image.Dispose() }
    }
