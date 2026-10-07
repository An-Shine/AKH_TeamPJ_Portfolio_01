Add-Type -AssemblyName System.Drawing
$assetName = 'DamagedResearchTable_L_3x3_64px'
$assetDir = Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/Obstacles'
$assetPath = Join-Path $assetDir ($assetName + '.png')
if (Test-Path -LiteralPath $assetPath) { throw 'Asset already exists; choose a new version.' }
$src = [System.Drawing.Bitmap]::FromFile('C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-cb367069-0c2b-4e41-8cd6-09b1e7cc5152.png')
$minX=$src.Width; $minY=$src.Height; $maxX=-1; $maxY=-1
for ($y=0; $y -lt $src.Height; $y++) {
    for ($x=0; $x -lt $src.Width; $x++) {
        if ($src.GetPixel($x,$y).A -gt 0) {
            $minX=[Math]::Min($minX,$x); $minY=[Math]::Min($minY,$y)
            $maxX=[Math]::Max($maxX,$x); $maxY=[Math]::Max($maxY,$y)
        }
    }
}
$cropWidth=$maxX-$minX+1; $cropHeight=$maxY-$minY+1
$targetWidth=176
$targetHeight=[Math]::Min(192,[int][Math]::Round(176*$cropHeight/$cropWidth))
$dst=[System.Drawing.Bitmap]::new(192,192,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($dst)
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($src,([System.Drawing.Rectangle]::new(8,192-$targetHeight,176,$targetHeight)),([System.Drawing.Rectangle]::new($minX,$minY,$cropWidth,$cropHeight)),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose(); $src.Dispose()
# Align the rasterized alpha bounds after nearest-neighbor sampling.
for($pass=0;$pass -lt 4;$pass++) {
    $bx=192; $by=192; $ex=-1; $ey=-1
    for($y=0;$y -lt 192;$y++){for($x=0;$x -lt 192;$x++){if($dst.GetPixel($x,$y).A -gt 0){$bx=[Math]::Min($bx,$x);$by=[Math]::Min($by,$y);$ex=[Math]::Max($ex,$x);$ey=[Math]::Max($ey,$y)}}}
    if($bx -eq 8 -and $ex -eq 183 -and $ey -eq 191){break}
    $cw=$ex-$bx+1; $ch=$ey-$by+1
    $th=[Math]::Min(192,[int][Math]::Round(176*$ch/$cw))
    $aligned=[System.Drawing.Bitmap]::new(192,192,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $ag=[System.Drawing.Graphics]::FromImage($aligned)
    $ag.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $ag.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $ag.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $ag.DrawImage($dst,([System.Drawing.Rectangle]::new(8,192-$th,176,$th)),([System.Drawing.Rectangle]::new($bx,$by,$cw,$ch)),[System.Drawing.GraphicsUnit]::Pixel)
    $ag.Dispose();$dst.Dispose();$dst=$aligned
}
$dst.Save($assetPath,[System.Drawing.Imaging.ImageFormat]::Png)
$preview=[System.Drawing.Bitmap]::new(768,768,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$pg.DrawImage($dst,([System.Drawing.Rectangle]::new(0,0,768,768)),([System.Drawing.Rectangle]::new(0,0,192,192)),[System.Drawing.GraphicsUnit]::Pixel)
$pg.Dispose(); $preview.Save((Join-Path (Get-Location) 'tmp/imagegen/ResearchTable_4x_preview.png'),[System.Drawing.Imaging.ImageFormat]::Png); $preview.Dispose()
$bounds=@{minX=192;maxX=-1;maxY=-1}
for ($y=0;$y -lt 192;$y++) { for ($x=0;$x -lt 192;$x++) { if($dst.GetPixel($x,$y).A -gt 0) { $bounds.minX=[Math]::Min($bounds.minX,$x);$bounds.maxX=[Math]::Max($bounds.maxX,$x);$bounds.maxY=[Math]::Max($bounds.maxY,$y) } } }
$dst.Dispose()
$template=Get-Content -LiteralPath (Join-Path $assetDir 'DamagedResearchConsole_3x3_64px_v2.png.meta') -Raw
$template=$template.Replace('DamagedResearchConsole_3x3_64px_v2',$assetName)
$template=[regex]::Replace($template,'(?m)^guid: [0-9a-f]+',('guid: '+[Guid]::NewGuid().ToString('N')))
$template=[regex]::Replace($template,'spriteID: [0-9a-f]+',{ param($m) 'spriteID: '+[Guid]::NewGuid().ToString('N') })
$template=$template.Replace('spriteMeshType: 1','spriteMeshType: 0')
Set-Content -LiteralPath ($assetPath+'.meta') -Value $template -Encoding utf8
[PSCustomObject]@{File=$assetPath;Width=192;Height=192;Slices=([regex]::Matches($template,'width: 64')).Count;LeftMargin=$bounds.minX;RightMargin=191-$bounds.maxX;BottomOccupiedRow=$bounds.maxY}
