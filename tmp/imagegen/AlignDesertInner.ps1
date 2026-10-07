Add-Type -AssemblyName System.Drawing
$root=Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/DesertSpaceBase/Cliffs'
$innerPath=Join-Path $root 'DesertCliff_Inner_64px_v1.png'
$metaHash=(Get-FileHash -LiteralPath ($innerPath+'.meta')).Hash
if(-not(Test-Path -LiteralPath tmp/imagegen/DesertCliff_Inner_BeforeAlignment.png)){Copy-Item -LiteralPath $innerPath -Destination tmp/imagegen/DesertCliff_Inner_BeforeAlignment.png}
$outer=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'DesertCliff_Outer_64px_v1.png'))
function New-Image($w,$h){return [System.Drawing.Bitmap]::new($w,$h,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)}
function Tile-Pixel($col,$row,$x,$y){return $outer.GetPixel($col*64+$x,$row*64+$y)}
$inner=New-Image 128 128
# Maintain existing names/order: VoidSE, VoidSW, VoidNE, VoidNW.
for($i=0;$i -lt 4;$i++){
    $voidRight=($i -eq 0 -or $i -eq 2);$voidBottom=($i -lt 2)
    $vc=if($voidRight){2}else{0};$hr=if($voidBottom){2}else{0}
    $ox=($i%2)*64;$oy=[int][Math]::Floor($i/2)*64
    for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
        $v=Tile-Pixel $vc 1 $x $y;$h=Tile-Pixel 1 $hr $x $y
        $vGround=if($voidRight){$x -lt 46}else{$x -ge 18}
        $hGround=if($voidBottom){$y -lt 26}else{$y -ge 20}
        if($hGround -and $vGround){$p=Tile-Pixel 1 1 $x $y}
        elseif($vGround){$p=$v}
        elseif($hGround){$p=$h}
        elseif($h.A -le 2){$p=$v}
        elseif($v.A -le 2){$p=$h}
        else{
            # Existing front face supplies the depth at the concave turn.
            $p=$h
        }
        # Copy actual neighboring edge strips, rather than rescaling the corner.
        # Right connects to horizontal tile's left; left to its right.
        if($voidRight -and $x -ge 60){$p=Tile-Pixel 1 $hr ($x-60) $y}
        elseif(-not $voidRight -and $x -lt 4){$p=Tile-Pixel 1 $hr ($x+60) $y}
        # Bottom connects to vertical tile's top; top to its bottom.
        if($voidBottom -and $y -ge 60){$p=Tile-Pixel $vc 1 $x ($y-60)}
        elseif(-not $voidBottom -and $y -lt 4){$p=Tile-Pixel $vc 1 $x ($y+60)}
        if($p.A -le 2){$p=[System.Drawing.Color]::FromArgb(0,0,0,0)}
        $inner.SetPixel($ox+$x,$oy+$y,$p)
    }}
}
$inner.Save($innerPath,[System.Drawing.Imaging.ImageFormat]::Png)
if((Get-FileHash -LiteralPath ($innerPath+'.meta')).Hash -ne $metaHash){throw 'Metadata changed'}
# Four L-shaped land areas; the missing quadrant exercises each concave turn.
$preview=New-Image 1344 1344
$g=[System.Drawing.Graphics]::FromImage($preview);$g.Clear([System.Drawing.Color]::FromArgb(255,49,53,58));$g.Dispose()
for($variant=0;$variant -lt 4;$variant++){
    $map=New-Image 320 320
    $missingRight=($variant -eq 0 -or $variant -eq 2);$missingBottom=($variant -lt 2)
    function Land($x,$y){
        if($x -lt 0 -or $x -ge 5 -or $y -lt 0 -or $y -ge 5){return $false}
        $mx=if($missingRight){$x -ge 3}else{$x -lt 2}
        $my=if($missingBottom){$y -ge 3}else{$y -lt 2}
        return -not($mx -and $my)
    }
    for($row=0;$row -lt 5;$row++){for($col=0;$col -lt 5;$col++){
        if(-not(Land $col $row)){continue}
        $n=Land $col ($row-1);$s=Land $col ($row+1);$w=Land ($col-1) $row;$e=Land ($col+1) $row
        $src=$outer;$sc=1;$sr=1
        if(-not $n -and -not $w){$sc=0;$sr=0}
        elseif(-not $n -and -not $e){$sc=2;$sr=0}
        elseif(-not $s -and -not $w){$sc=0;$sr=2}
        elseif(-not $s -and -not $e){$sc=2;$sr=2}
        elseif(-not $n){$sr=0}
        elseif(-not $s){$sr=2}
        elseif(-not $w){$sc=0}
        elseif(-not $e){$sc=2}
        elseif(-not(Land ($col+1) ($row+1))){$src=$inner;$sc=0;$sr=0}
        elseif(-not(Land ($col-1) ($row+1))){$src=$inner;$sc=1;$sr=0}
        elseif(-not(Land ($col+1) ($row-1))){$src=$inner;$sc=0;$sr=1}
        elseif(-not(Land ($col-1) ($row-1))){$src=$inner;$sc=1;$sr=1}
        $mg=[System.Drawing.Graphics]::FromImage($map)
        $mg.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $mg.DrawImage($src,([System.Drawing.Rectangle]::new($col*64,$row*64,64,64)),([System.Drawing.Rectangle]::new($sc*64,$sr*64,64,64)),[System.Drawing.GraphicsUnit]::Pixel)
        $mg.Dispose()
    }}
    $px=($variant%2)*672+16;$py=[int][Math]::Floor($variant/2)*672+16
    $g=[System.Drawing.Graphics]::FromImage($preview)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.DrawImage($map,([System.Drawing.Rectangle]::new($px,$py,640,640)),([System.Drawing.Rectangle]::new(0,0,320,320)),[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose();$map.Dispose()
}
$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/DesertCliff_Inner_Aligned_2x_Preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose();$inner.Dispose();$outer.Dispose()
[PSCustomObject]@{Width=128;Height=128;Tiles=4;MetadataUnchanged=$true;Preview='tmp/imagegen/DesertCliff_Inner_Aligned_2x_Preview.png'}
