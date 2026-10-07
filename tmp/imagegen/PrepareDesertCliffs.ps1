Add-Type -AssemblyName System.Drawing
$dir=Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/DesertSpaceBase/Cliffs'
[void](New-Item -ItemType Directory -Path $dir -Force)
function New-Raster($w,$h){return [System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)}
function Draw-Nearest($target,$source,$destRect,$sourceRect){
    $g=[System.Drawing.Graphics]::FromImage($target)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $g.DrawImage($source,$destRect,$sourceRect,[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
}
function Save-Sheet($bitmap,$name,$cols,$rows,$tileNames){
    $path=Join-Path $dir ($name+'.png')
    $keepMeta=Test-Path -LiteralPath ($path+'.meta')
    for($y=0;$y -lt $bitmap.Height;$y++){for($x=0;$x -lt $bitmap.Width;$x++){if($bitmap.GetPixel($x,$y).A -le 2){$bitmap.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(0,0,0,0))}}}
    $bitmap.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
    if($keepMeta){[PSCustomObject]@{File=$name;Width=$bitmap.Width;Height=$bitmap.Height;Tiles=$cols*$rows;Metadata='Preserved'};return}
    $template=Get-Content -LiteralPath 'Assets/04.Sprites/Tilemaps/Obstacles/DamagedResearchConsole_3x3_64px_v2.png.meta' -Raw
    $header=($template -split '  spriteSheet:',2)[0]
    $ids=[System.Text.StringBuilder]::new();[void]$ids.AppendLine('  internalIDToNameTable:')
    $sprites=[System.Text.StringBuilder]::new();[void]$sprites.AppendLine("  spriteSheet:`n    serializedVersion: 2`n    sprites:")
    $names=[System.Text.StringBuilder]::new();[void]$names.AppendLine('    nameFileIdTable:')
    for($row=0;$row -lt $rows;$row++){for($col=0;$col -lt $cols;$col++){
        $index=$row*$cols+$col;$id=21300000+2*$index;$sn=$tileNames[$index];$x=$col*64;$y=($rows-1-$row)*64
        [void]$ids.AppendLine("  - first:`n      213: $id`n    second: $sn")
        [void]$sprites.AppendLine(@"
    - serializedVersion: 2
      name: $sn
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
      spriteID: $([Guid]::NewGuid().ToString('N'))
      internalID: $id
      vertices: []
      indices:
      edges: []
      weights: []
"@)
        [void]$names.AppendLine("      ${sn}: $id")
    }}
    $header=[regex]::Replace($header,'(?s)  internalIDToNameTable:.*?(?=  externalObjects:)',$ids.ToString())
    $header=[regex]::Replace($header,'(?m)^guid: [0-9a-f]+',('guid: '+[Guid]::NewGuid().ToString('N')))
    $header=$header.Replace('spriteMeshType: 1','spriteMeshType: 0')
    $tail="    outline: []`n    physicsShape: []`n    bones: []`n    spriteID: $([Guid]::NewGuid().ToString('N'))`n    internalID: 0`n    secondaryTextures: []`n"
    Set-Content -LiteralPath ($path+'.meta') -Value ($header+$sprites.ToString()+$tail+$names.ToString()+"  userData:`n  assetBundleName:`n  assetBundleVariant:`n") -Encoding utf8
    [PSCustomObject]@{File=$name;Width=$bitmap.Width;Height=$bitmap.Height;Tiles=$cols*$rows}
}
$src=[System.Drawing.Bitmap]::FromFile('C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-c3e47dd4-2bc2-442b-898f-6e7fcf32e436.png')
$outer=New-Raster 192 192
Draw-Nearest $outer $src ([System.Drawing.Rectangle]::new(0,0,192,192)) ([System.Drawing.Rectangle]::new(0,0,$src.Width,$src.Height))
$src.Dispose()
Save-Sheet $outer 'DesertCliff_Outer_64px_v1' 3 3 @('Cliff_Outer_NW','Cliff_Edge_N','Cliff_Outer_NE','Cliff_Edge_W','Cliff_Sand_Center','Cliff_Edge_E','Cliff_Outer_SW','Cliff_Edge_S','Cliff_Outer_SE')
$src=[System.Drawing.Bitmap]::FromFile('C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-9a09f9fa-6c5b-4871-81ed-42ff80df4d09.png')
$inner=New-Raster 128 128
$crops=@(@(160,200),@(695,200),@(160,650),@(695,650))
for($i=0;$i -lt 4;$i++){Draw-Nearest $inner $src ([System.Drawing.Rectangle]::new(($i%2)*64,[Math]::Floor($i/2)*64,64,64)) ([System.Drawing.Rectangle]::new($crops[$i][0],$crops[$i][1],400,400))}
$src.Dispose()
Save-Sheet $inner 'DesertCliff_Inner_64px_v1' 2 2 @('Cliff_Inner_VoidSE','Cliff_Inner_VoidSW','Cliff_Inner_VoidNE','Cliff_Inner_VoidNW')
$expanded=New-Raster 320 320
for($row=0;$row -lt 5;$row++){for($col=0;$col -lt 5;$col++){
    $sr=if($row -eq 0){0}elseif($row -eq 4){2}else{1}
    $sc=if($col -eq 0){0}elseif($col -eq 4){2}else{1}
    Draw-Nearest $expanded $outer ([System.Drawing.Rectangle]::new($col*64,$row*64,64,64)) ([System.Drawing.Rectangle]::new($sc*64,$sr*64,64,64))
}}
$preview=New-Raster 960 960
Draw-Nearest $preview $expanded ([System.Drawing.Rectangle]::new(0,0,960,960)) ([System.Drawing.Rectangle]::new(0,0,320,320))
$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/DesertCliff_RepeatPreview_3x.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose();$expanded.Dispose();$outer.Dispose();$inner.Dispose()
# Reapply the matched concave corners after any full-sheet regeneration.
& (Join-Path $PSScriptRoot 'PrepareDesertInnerFromSketch.ps1')
