Add-Type -AssemblyName System.Drawing
$dir=Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/Map/Walls'
$entries=@(
@{name='SpaceBase_Wall_Intact_1x3_64px';source='C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-dd361083-ecf1-4ce6-86e6-ed040699deab.png'},
@{name='SpaceBase_Wall_PowerPanel_1x3_64px';source='C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-d0e18e64-02ba-4abf-98d1-4f607b1be563.png'}
)
$preview=[System.Drawing.Bitmap]::new(512,768,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$i=0
foreach($entry in $entries){
    $out=Join-Path $dir ($entry.name+'.png')
    $before=(Get-FileHash -LiteralPath ($out+'.meta')).Hash
    Copy-Item -LiteralPath $out -Destination (Join-Path (Get-Location) ('tmp/imagegen/'+$entry.name+'_before_original_restoration.png'))
    $src=[System.Drawing.Bitmap]::FromFile($entry.source)
    $dst=[System.Drawing.Bitmap]::new(64,192,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($dst)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $g.DrawImage($src,([System.Drawing.Rectangle]::new(0,0,64,192)),([System.Drawing.Rectangle]::new(0,0,678,$src.Height)),[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose();$src.Dispose()
    for($y=0;$y -lt 192;$y++){for($x=0;$x -lt 64;$x++){if($dst.GetPixel($x,$y).A -le 2){$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(0,0,0,0))}}}
    $dst.Save($out,[System.Drawing.Imaging.ImageFormat]::Png)
    $pg.DrawImage($dst,([System.Drawing.Rectangle]::new($i*256,0,256,768)),([System.Drawing.Rectangle]::new(0,0,64,192)),[System.Drawing.GraphicsUnit]::Pixel)
    [PSCustomObject]@{File=$entry.name;Width=64;Height=192;FloorAlpha=$dst.GetPixel(32,165).A;MetaUnchanged=($before -eq (Get-FileHash -LiteralPath ($out+'.meta')).Hash)}
    $dst.Dispose();$i++
}
$pg.Dispose();$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/WallModules_OriginalRestored_4x_preview.png'),[System.Drawing.Imaging.ImageFormat]::Png);$preview.Dispose()
