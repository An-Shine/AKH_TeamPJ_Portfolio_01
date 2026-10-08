Add-Type -AssemblyName System.Drawing
$assetDir=Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/Map/Walls'
[void](New-Item -ItemType Directory -Path $assetDir -Force)
$inputs=@(
@{name='SpaceBase_Wall_Intact_1x3_64px';source='C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-2c4f3e4f-e3a4-4dd1-9fc9-6739029a0252.png'},
@{name='SpaceBase_Wall_PowerPanel_1x3_64px';source='C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-4f1196cd-641e-45b0-a8a4-ce34747dfcb7.png'}
)
$preview=[System.Drawing.Bitmap]::new(512,768,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$index=0
foreach($inputAsset in $inputs){
    $name=$inputAsset.name;$out=Join-Path $assetDir ($name+'.png')
    if(Test-Path -LiteralPath $out){throw 'Asset exists'}
    $src=[System.Drawing.Bitmap]::FromFile($inputAsset.source)
    $dst=[System.Drawing.Bitmap]::new(64,192,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($dst)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $g.DrawImage($src,([System.Drawing.Rectangle]::new(0,0,64,192)),([System.Drawing.Rectangle]::new(10,0,$src.Width-20,$src.Height)),[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose();$src.Dispose()
    $dst.Save($out,[System.Drawing.Imaging.ImageFormat]::Png)
    $pg.DrawImage($dst,([System.Drawing.Rectangle]::new($index*256,0,256,768)),([System.Drawing.Rectangle]::new(0,0,64,192)),[System.Drawing.GraphicsUnit]::Pixel)
    $dst.Dispose()
    $template=Get-Content -LiteralPath 'Assets/04.Sprites/Tilemaps/Obstacles/DamagedResearchConsole_3x3_64px_v2.png.meta' -Raw
    $header=($template -split '  spriteSheet:',2)[0]
    $ids=[System.Text.StringBuilder]::new();[void]$ids.AppendLine('  internalIDToNameTable:')
    $sprites=[System.Text.StringBuilder]::new();[void]$sprites.AppendLine("  spriteSheet:`n    serializedVersion: 2`n    sprites:")
    $names=[System.Text.StringBuilder]::new();[void]$names.AppendLine('    nameFileIdTable:')
    for($row=0;$row -lt 3;$row++){
        $id=21300000+2*$row;$sn=('{0}_r{1:00}' -f $name,$row);$y=(2-$row)*64;$sid=[Guid]::NewGuid().ToString('N')
        [void]$ids.AppendLine("  - first:`n      213: $id`n    second: $sn")
        [void]$sprites.AppendLine(@"
    - serializedVersion: 2
      name: $sn
      rect:
        serializedVersion: 2
        x: 0
        y: $y
        width: 64
        height: 64
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: $sid
      internalID: $id
      vertices: []
      indices:
      edges: []
      weights: []
"@)
        [void]$names.AppendLine("      ${sn}: $id")
    }
    $header=[regex]::Replace($header,'(?s)  internalIDToNameTable:.*?(?=  externalObjects:)',$ids.ToString())
    $header=[regex]::Replace($header,'(?m)^guid: [0-9a-f]+',('guid: '+[Guid]::NewGuid().ToString('N')))
    $header=$header.Replace('spriteMeshType: 1','spriteMeshType: 0')
    $tail="    outline: []`n    physicsShape: []`n    bones: []`n    spriteID: $([Guid]::NewGuid().ToString('N'))`n    internalID: 0`n    secondaryTextures: []`n"
    Set-Content -LiteralPath ($out+'.meta') -Value ($header+$sprites.ToString()+$tail+$names.ToString()+"  userData:`n  assetBundleName:`n  assetBundleVariant:`n") -Encoding utf8
    [PSCustomObject]@{File=$name;Width=64;Height=192;Slices=3}
    $index++
}
$pg.Dispose();$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/WallModules_4x_preview.png'),[System.Drawing.Imaging.ImageFormat]::Png);$preview.Dispose()
