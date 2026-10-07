Add-Type -AssemblyName System.Drawing
$name='BossRoom_BreachedWall_Sand_4x5_64px'
$dir=Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/Obstacles'
$out=Join-Path $dir ($name+'.png')
if(Test-Path -LiteralPath $out){throw 'Asset exists.'}
$src=[System.Drawing.Bitmap]::FromFile('C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-48611001-96ee-4289-bad4-67df160e6752.png')
$dst=[System.Drawing.Bitmap]::new(256,320,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($dst)
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$wallStart=65; $wallEnd=752
$g.DrawImage($src,([System.Drawing.Rectangle]::new(0,0,256,192)),([System.Drawing.Rectangle]::new(0,$wallStart,$src.Width,$wallEnd-$wallStart)),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($src,([System.Drawing.Rectangle]::new(0,192,256,128)),([System.Drawing.Rectangle]::new(0,$wallEnd,$src.Width,$src.Height-$wallEnd)),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose();$src.Dispose()
for($y=0;$y -lt 320;$y++){for($x=0;$x -lt 256;$x++){if($dst.GetPixel($x,$y).A -le 2){$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(0,0,0,0))}}}
$dst.Save($out,[System.Drawing.Imaging.ImageFormat]::Png)
$preview=[System.Drawing.Bitmap]::new(768,960,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$pg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
$pg.DrawImage($dst,([System.Drawing.Rectangle]::new(0,0,768,960)),([System.Drawing.Rectangle]::new(0,0,256,320)),[System.Drawing.GraphicsUnit]::Pixel)
$pg.Dispose();$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/WallBreach_3x_preview.png'),[System.Drawing.Imaging.ImageFormat]::Png);$preview.Dispose();$dst.Dispose()
$template=Get-Content -LiteralPath (Join-Path $dir 'DamagedResearchConsole_3x3_64px_v2.png.meta') -Raw
$header=($template -split '  spriteSheet:',2)[0]
$idTable=[System.Text.StringBuilder]::new()
[void]$idTable.AppendLine('  internalIDToNameTable:')
$sprites=[System.Text.StringBuilder]::new()
[void]$sprites.AppendLine("  spriteSheet:`n    serializedVersion: 2`n    sprites:")
$names=[System.Text.StringBuilder]::new()
[void]$names.AppendLine('    nameFileIdTable:')
for($row=0;$row -lt 5;$row++) {for($col=0;$col -lt 4;$col++) {
    $id=21300000+2*($row*4+$col)
    $sliceName=('{0}_r{1:00}_c{2:00}' -f $name,$row,$col)
    [void]$idTable.AppendLine("  - first:`n      213: $id`n    second: $sliceName")
    $sid=[Guid]::NewGuid().ToString('N');$x=$col*64;$y=(4-$row)*64
    [void]$sprites.AppendLine(@"
    - serializedVersion: 2
      name: $sliceName
      rect:
        serializedVersion: 2
        x: $x
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
    [void]$names.AppendLine("      ${sliceName}: $id")
}}
$header=[regex]::Replace($header,'(?s)  internalIDToNameTable:.*?(?=  externalObjects:)',$idTable.ToString())
$header=[regex]::Replace($header,'(?m)^guid: [0-9a-f]+',('guid: '+[Guid]::NewGuid().ToString('N')))
$header=$header.Replace('spriteMeshType: 1','spriteMeshType: 0')
$tail=@"
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: $([Guid]::NewGuid().ToString('N'))
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
"@
if(-not(Test-Path -LiteralPath ($out+'.meta'))){Set-Content -LiteralPath ($out+'.meta') -Value ($header+$sprites.ToString()+$tail+"`n"+$names.ToString()+"  userData:`n  assetBundleName:`n  assetBundleVariant:`n") -Encoding utf8}
[PSCustomObject]@{File=$out;Width=256;Height=320;WallHeight=192;SandHeight=128;Slices=20}
