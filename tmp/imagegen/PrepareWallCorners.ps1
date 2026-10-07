Add-Type -AssemblyName System.Drawing
$dir=Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/Map/Walls'
$entries=@(
@{name='SpaceBase_Wall_CornerLeft_1x3_64px';source='C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-91b80e9a-6500-42e5-a21f-e50eb9f0c001.png';x=74;w=578;ys=@(108,278,354,609,1273,2056)},
@{name='SpaceBase_Wall_CornerRight_1x3_64px';source='C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-45213a77-03d7-4361-b25f-87d14aed5d4f.png';x=213;w=507;ys=@(101,253,334,536,1154,1670)}
)
$destY=@(4,22,33,64,153,192)
foreach($entry in $entries){
    $out=Join-Path $dir ($entry.name+'.png')
    if(Test-Path -LiteralPath $out){throw 'Asset exists.'}
    $src=[System.Drawing.Bitmap]::FromFile($entry.source)
    $dst=[System.Drawing.Bitmap]::new(64,192,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($dst)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    for($part=0;$part -lt 5;$part++){
        $g.DrawImage($src,([System.Drawing.Rectangle]::new(0,$destY[$part],64,$destY[$part+1]-$destY[$part])),([System.Drawing.Rectangle]::new($entry.x,$entry.ys[$part],$entry.w,$entry.ys[$part+1]-$entry.ys[$part])),[System.Drawing.GraphicsUnit]::Pixel)
    }
    $g.Dispose();$src.Dispose()
    for($y=0;$y -lt 192;$y++){for($x=0;$x -lt 64;$x++){if($dst.GetPixel($x,$y).A -le 2){$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(0,0,0,0))}}}
    $dst.Save($out,[System.Drawing.Imaging.ImageFormat]::Png);$dst.Dispose()
    $meta=Get-Content -LiteralPath (Join-Path $dir 'SpaceBase_Wall_Intact_1x3_64px.png.meta') -Raw
    $meta=$meta.Replace('SpaceBase_Wall_Intact_1x3_64px',$entry.name)
    $meta=[regex]::Replace($meta,'(?m)^guid: [0-9a-f]+',('guid: '+[Guid]::NewGuid().ToString('N')))
    $meta=[regex]::Replace($meta,'spriteID: [0-9a-f]+',{param($m) 'spriteID: '+[Guid]::NewGuid().ToString('N')})
    Set-Content -LiteralPath ($out+'.meta') -Value $meta -Encoding utf8
    [PSCustomObject]@{File=$entry.name;Width=64;Height=192;Slices=3;WallBaseline=153}
}
$preview=[System.Drawing.Bitmap]::new(768,768,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$names=@($entries[0].name,'SpaceBase_Wall_Intact_1x3_64px',$entries[1].name)
for($i=0;$i -lt 3;$i++){$b=[System.Drawing.Bitmap]::FromFile((Join-Path $dir ($names[$i]+'.png')));$pg.DrawImage($b,([System.Drawing.Rectangle]::new($i*256,0,256,768)),([System.Drawing.Rectangle]::new(0,0,64,192)),[System.Drawing.GraphicsUnit]::Pixel);$b.Dispose()}
$pg.Dispose();$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/WallCorners_Joined_4x_preview.png'),[System.Drawing.Imaging.ImageFormat]::Png);$preview.Dispose()
