Add-Type -AssemblyName System.Drawing
$dir=Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/Map/Walls'
$src=[System.Drawing.Bitmap]::FromFile('C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-b901e88f-b08c-4e99-930c-003e6fd66c60.png')
$sourceY=@(28,58,64,90,98,142,215,421,992,1235)
$destY=@(4,5,6,12,13,22,33,64,153,192)
$entries=@(@{name='SpaceBase_Wall_CornerLeft_1x3_64px';x=10;w=408},@{name='SpaceBase_Wall_CornerRight_1x3_64px';x=837;w=408})
foreach($entry in $entries){
    $out=Join-Path $dir ($entry.name+'.png')
    $metaHash=(Get-FileHash -LiteralPath ($out+'.meta')).Hash
    Copy-Item -LiteralPath $out -Destination (Join-Path (Get-Location) ('tmp/imagegen/'+$entry.name+'_before_join_alignment.png'))
    $dst=[System.Drawing.Bitmap]::new(64,192,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($dst)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    for($part=0;$part -lt ($sourceY.Count-1);$part++){
        $g.DrawImage($src,([System.Drawing.Rectangle]::new(0,$destY[$part],64,$destY[$part+1]-$destY[$part])),([System.Drawing.Rectangle]::new($entry.x,$sourceY[$part],$entry.w,$sourceY[$part+1]-$sourceY[$part])),[System.Drawing.GraphicsUnit]::Pixel)
    }
    $g.Dispose()
    for($y=0;$y -lt 192;$y++){for($x=0;$x -lt 64;$x++){if($dst.GetPixel($x,$y).A -le 2){$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(0,0,0,0))}}}
    $dst.Save($out,[System.Drawing.Imaging.ImageFormat]::Png);$dst.Dispose()
    [PSCustomObject]@{File=$entry.name;Width=64;Height=192;MetaUnchanged=($metaHash -eq (Get-FileHash -LiteralPath ($out+'.meta')).Hash)}
}
$src.Dispose()
$preview=[System.Drawing.Bitmap]::new(768,768,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$names=@($entries[0].name,'SpaceBase_Wall_Intact_1x3_64px',$entries[1].name)
for($i=0;$i -lt 3;$i++){$b=[System.Drawing.Bitmap]::FromFile((Join-Path $dir ($names[$i]+'.png')));$pg.DrawImage($b,([System.Drawing.Rectangle]::new($i*256,0,256,768)),([System.Drawing.Rectangle]::new(0,0,64,192)),[System.Drawing.GraphicsUnit]::Pixel);$b.Dispose()}
$pg.Dispose();$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/WallCorners_Aligned_4x_preview.png'),[System.Drawing.Imaging.ImageFormat]::Png);$preview.Dispose()
