Add-Type -AssemblyName System.Drawing
$assetRoot=Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/DesertSpaceBase/Cliffs'
$path=Join-Path $assetRoot 'DesertCliff_Inner_64px_v1.png'
$hash=(Get-FileHash -LiteralPath ($path+'.meta')).Hash
$source=[System.Drawing.Bitmap]::FromFile('C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-417982b9-339e-47fc-8872-9ef95f470f1a.png')
$outer=[System.Drawing.Bitmap]::FromFile((Join-Path $assetRoot 'DesertCliff_Outer_64px_v1.png'))
function Raster($w,$h){return [System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)}
function Blit($dst,$src,$dx,$dy,$dw,$dh,$sx,$sy,$sw,$sh){
    $g=[System.Drawing.Graphics]::FromImage($dst)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $g.DrawImage($src,([System.Drawing.Rectangle]::new($dx,$dy,$dw,$dh)),([System.Drawing.Rectangle]::new($sx,$sy,$sw,$sh)),[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
}
$inner=Raster 128 128
# Preserve the user's existing placement: VoidNW top-left, VoidNE top-right,
# VoidSW bottom-left, VoidSE bottom-right. Only pixels change, never sprite IDs.
$corners=@(
    @(3,164,63,370,362),
    @(2,860,63,365,362),
    @(1,164,675,370,390),
    @(0,860,675,365,390)
)
foreach($a in $corners){
    $i=$a[0];$ox=($i%2)*64;$oy=[int][Math]::Floor($i/2)*64
    Blit $inner $source $ox $oy 64 64 $a[1] $a[2] $a[3] $a[4]
}
for($y=0;$y -lt 128;$y++){for($x=0;$x -lt 128;$x++){
    $c=$inner.GetPixel($x,$y)
    if($c.A -lt 128){$inner.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(0,0,0,0))}
    else{$inner.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(255,$c.R,$c.G,$c.B))}
}}
# Keep the incoming straight rim cross-sections from the real Outer tiles.
for($q=0;$q -lt 4;$q++){
    $i=3-$q;$ox=($i%2)*64;$oy=[int][Math]::Floor($i/2)*64
    $left=($q%2 -eq 0);$top=($q -lt 2)
    $edgeRow=if($top){0}else{2};$edgeCol=if($left){0}else{2}
    for($strip=0;$strip -lt 3;$strip++){
        for($t=0;$t -lt 23;$t++){
            $tx=if($left){61+$strip}else{$strip}
            $hx=if($left){$strip}else{61+$strip}
            $ty=if($top){$t}else{63-$t}
            $p=$outer.GetPixel(64+$hx,$edgeRow*64+$ty)
            if($p.A -le 2){$p=[System.Drawing.Color]::FromArgb(0,0,0,0)}
            $inner.SetPixel($ox+$tx,$oy+$ty,$p)
            $vx=if($left){$t}else{63-$t}
            $vy=if($top){$strip}else{61+$strip}
            $dy=if($top){61+$strip}else{$strip}
            $p=$outer.GetPixel($edgeCol*64+$vx,64+$vy)
            if($p.A -le 2){$p=[System.Drawing.Color]::FromArgb(0,0,0,0)}
            $inner.SetPixel($ox+$vx,$oy+$dy,$p)
        }
    }
}
# Match physical end lengths against the actual neighboring Outer alpha profiles.
function Edge-Bounds($sx,$sy,$vertical){
    $first=64;$last=-1
    for($t=0;$t -lt 64;$t++){
        $p=if($vertical){$outer.GetPixel($sx,$sy+$t)}else{$outer.GetPixel($sx+$t,$sy)}
        if($p.A -gt 2){$first=[Math]::Min($first,$t);$last=[Math]::Max($last,$t)}
    }
    return @($first,$last)
}
$nLeft=Edge-Bounds 64 0 $true;$nRight=Edge-Bounds 127 0 $true
$wTop=Edge-Bounds 0 64 $false;$wBottom=Edge-Bounds 0 127 $false
$eTop=Edge-Bounds 128 64 $false;$eBottom=Edge-Bounds 128 127 $false
$sLeft=Edge-Bounds 64 128 $true;$sRight=Edge-Bounds 127 128 $true
$transparent=[System.Drawing.Color]::FromArgb(0,0,0,0)
for($q=0;$q -lt 4;$q++){
    $i=3-$q;$ox=($i%2)*64;$oy=[int][Math]::Floor($i/2)*64
    $left=($q%2 -eq 0);$top=($q -lt 2)
    $xmin=if($left){if($top){$wTop[0]}else{$wBottom[0]}}else{0}
    $xmax=if($left){63}else{if($top){$eTop[1]}else{$eBottom[1]}}
    $ymin=if($top){if($left){$nLeft[0]}else{$nRight[0]}}else{0}
    $ymax=if($top){63}else{if($left){$sLeft[1]}else{$sRight[1]}}
    if(-not $top){
        # Continue the lower horizontal cap into a deep falling face.
        # South Outer face starts at y26 and has a ~33px depth; keep its native pixels.
        for($x=$xmin;$x -le $xmax;$x++){
            $horizontalPart=if($left){$x -le 51}else{$x -ge 12}
            if(-not $horizontalPart){continue}
            $rim=-1
            for($y=0;$y -le 12;$y++){if($inner.GetPixel($ox+$x,$oy+$y).A -ge 128){$rim=$y;break}}
            if($rim -lt 0){continue}
            for($depth=6;$depth -le 32;$depth++){
                $y=$rim+$depth
                if($y -gt $ymax){break}
                $p=$outer.GetPixel(64+$x,128+26+$depth)
                if($p.A -le 2){$p=$transparent}
                $inner.SetPixel($ox+$x,$oy+$y,$p)
            }
        }
    }
    for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
        if($x -lt $xmin -or $x -gt $xmax -or $y -lt $ymin -or $y -gt $ymax){$inner.SetPixel($ox+$x,$oy+$y,$transparent)}
    }}
    # Check that no protrusion survives past any Outer-defined end plane.
    for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
        if($inner.GetPixel($ox+$x,$oy+$y).A -gt 2 -and ($x -lt $xmin -or $x -gt $xmax -or $y -lt $ymin -or $y -gt $ymax)){throw 'Inner endpoint exceeds Outer'}
    }}
}
$inner.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
if((Get-FileHash -LiteralPath ($path+'.meta')).Hash -ne $hash){throw 'Metadata changed'}
$map=Raster 192 192
for($r=0;$r -lt 3;$r++){for($c=0;$c -lt 3;$c++){
    if($r -ne 1 -and $c -ne 1){
        $idx=if($r -eq 0){if($c -eq 0){3}else{2}}else{if($c -eq 0){1}else{0}}
        Blit $map $inner ($c*64) ($r*64) 64 64 (($idx%2)*64) ([int][Math]::Floor($idx/2)*64) 64 64
    }else{Blit $map $outer ($c*64) ($r*64) 64 64 ($c*64) ($r*64) 64 64}
}}
$preview=Raster 768 768
for($y=0;$y -lt 768;$y++){for($x=0;$x -lt 768;$x++){
    $preview.SetPixel($x,$y,$map.GetPixel([int][Math]::Floor($x/4),[int][Math]::Floor($y/4)))
}}
$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/DesertCliff_Inner_UserLayout_4x.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$source.Dispose();$outer.Dispose();$inner.Dispose();$map.Dispose();$preview.Dispose()
[PSCustomObject]@{Width=128;Height=128;Slices=4;MetadataUnchanged=$true}
