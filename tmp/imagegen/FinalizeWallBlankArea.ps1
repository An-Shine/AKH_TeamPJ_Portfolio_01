Add-Type -AssemblyName System.Drawing
$src=[System.Drawing.Bitmap]::FromFile('C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-ce3517d8-c8a4-440c-9e0b-481bd52cf863.png')
$names=@('SpaceBase_Wall_Intact_1x3_64px','SpaceBase_Wall_PowerPanel_1x3_64px')
$preview=[System.Drawing.Bitmap]::new(512,768,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
for($i=0;$i -lt 2;$i++){
    $out=Join-Path (Get-Location) ('Assets/04.Sprites/Tilemaps/Map/Walls/'+$names[$i]+'.png')
    $metaBefore=(Get-FileHash -LiteralPath ($out+'.meta')).Hash
    $dst=[System.Drawing.Bitmap]::new(64,192,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($dst)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $g.DrawImage($src,([System.Drawing.Rectangle]::new(0,0,64,192)),([System.Drawing.Rectangle]::new($i*($src.Width/2),0,$src.Width/2,$src.Height)),[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    for($y=0;$y -lt 192;$y++){for($x=0;$x -lt 64;$x++){if($dst.GetPixel($x,$y).A -le 2){$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(0,0,0,0))}}}
    $dst.Save($out,[System.Drawing.Imaging.ImageFormat]::Png)
    $pg.DrawImage($dst,([System.Drawing.Rectangle]::new($i*256,0,256,768)),([System.Drawing.Rectangle]::new(0,0,64,192)),[System.Drawing.GraphicsUnit]::Pixel)
    $visible=0
    for($y=148;$y -lt 192;$y++){for($x=0;$x -lt 64;$x++){if($dst.GetPixel($x,$y).A -gt 0){$visible++}}}
    [PSCustomObject]@{File=$names[$i];Width=64;Height=192;BottomAreaVisiblePixels=$visible;MetaUnchanged=($metaBefore -eq (Get-FileHash -LiteralPath ($out+'.meta')).Hash)}
    $dst.Dispose()
}
$pg.Dispose();$src.Dispose()
$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/WallModules_NoFloorFrame_4x_preview.png'),[System.Drawing.Imaging.ImageFormat]::Png);$preview.Dispose()
