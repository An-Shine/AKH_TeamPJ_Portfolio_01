Add-Type -AssemblyName System.Drawing
$dir=Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/Obstacles'
$baseName='BossRoom_BreachedWall_Sand_4x5_64px'
$basePath=Join-Path $dir ($baseName+'.png')
$baseHash=(Get-FileHash -LiteralPath $basePath).Hash
$baseMetaHash=(Get-FileHash -LiteralPath ($basePath+'.meta')).Hash
$rawRoot='C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3'
$items=@(
    @('BossRoom_BreachedWall_Sand_Center_4x5_64px','exec-db1e2190-0b05-43db-859d-6af954a8cfc6.png',32,820),
    @('BossRoom_BreachedWall_Sand_Left_4x5_64px','exec-95d5ece1-945d-4d45-933d-b6a1cad6b4de.png',14,835)
)
function New-Image($w,$h){return [System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)}
function Draw-Pixels($dst,$src,$dest,$crop){
    $g=[System.Drawing.Graphics]::FromImage($dst)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $g.DrawImage($src,$dest,$crop,[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
}
$preview=New-Image 1536 688
$g=[System.Drawing.Graphics]::FromImage($preview);$g.Clear([System.Drawing.Color]::FromArgb(255,37,42,48));$g.Dispose()
$base=[System.Drawing.Bitmap]::FromFile($basePath)
$bitmaps=@($base)
$report=@()
foreach($item in $items){
    $name=$item[0];$path=Join-Path $dir ($name+'.png')
    if(Test-Path -LiteralPath $path){throw "Asset already exists: $name"}
    $src=[System.Drawing.Bitmap]::FromFile((Join-Path $rawRoot $item[1]))
    $first=[int]$item[2];$split=[int]$item[3]
    $last=-1
    for($y=$src.Height-1;$y -ge $split;$y--){
        for($x=0;$x -lt $src.Width;$x++){if($src.GetPixel($x,$y).A -ge 128){$last=$y;break}}
        if($last -ge 0){break}
    }
    if($last -lt $split){throw 'No sand footprint'}
    $dst=New-Image 256 320
    Draw-Pixels $dst $src ([System.Drawing.Rectangle]::new(0,0,256,192)) ([System.Drawing.Rectangle]::new(0,$first,$src.Width,$split-$first))
    Draw-Pixels $dst $src ([System.Drawing.Rectangle]::new(0,192,256,128)) ([System.Drawing.Rectangle]::new(0,$split,$src.Width,$last-$split+1))
    $src.Dispose()
    for($y=0;$y -lt 320;$y++){for($x=0;$x -lt 256;$x++){
        $p=$dst.GetPixel($x,$y)
        if($p.A -lt 128){$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(0,0,0,0))}
        else{$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(255,$p.R,$p.G,$p.B))}
    }}
    $dst.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
    $meta=Get-Content -LiteralPath ($basePath+'.meta') -Raw
    $meta=$meta.Replace($baseName,$name)
    $meta=[regex]::Replace($meta,'(?m)^guid: [0-9a-f]+',('guid: '+[Guid]::NewGuid().ToString('N')))
    $meta=[regex]::Replace($meta,'spriteID: [0-9a-f]+',{param($m) 'spriteID: '+[Guid]::NewGuid().ToString('N')})
    $meta=$meta.Replace('textureCompression: 1','textureCompression: 0')
    Set-Content -LiteralPath ($path+'.meta') -Value $meta -Encoding utf8
    $slices=([regex]::Matches($meta,'      width: 64')).Count
    if($slices -ne 20){throw 'Incorrect grid count'}
    $report+=[PSCustomObject]@{Name=$name;Width=256;Height=320;WallRegion=192;SandRegion=128;Slices=$slices}
    $bitmaps+=$dst
}
$labels=@('Original (right breach)','Center breach','Left breach')
for($i=0;$i -lt 3;$i++){
    $g=[System.Drawing.Graphics]::FromImage($preview)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.DrawImage($bitmaps[$i],([System.Drawing.Rectangle]::new($i*512,0,512,640)),([System.Drawing.Rectangle]::new(0,0,256,320)),[System.Drawing.GraphicsUnit]::Pixel)
    $font=[System.Drawing.Font]::new('Arial',20)
    $g.DrawString($labels[$i],$font,[System.Drawing.Brushes]::White,[single]($i*512+16),[single]648)
    $font.Dispose();$g.Dispose();$bitmaps[$i].Dispose()
}
$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/WallBreach_Variants_2x_Preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
if((Get-FileHash -LiteralPath $basePath).Hash -ne $baseHash -or (Get-FileHash -LiteralPath ($basePath+'.meta')).Hash -ne $baseMetaHash){throw 'Original asset changed'}
$report | Format-Table -AutoSize
