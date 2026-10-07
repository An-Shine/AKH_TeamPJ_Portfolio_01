# Reuse the validated alpha-bounds and sprite metadata functions without running
# the earlier six-asset batch.
$helper=Get-Content -LiteralPath 'tmp/imagegen/PrepareDesertProps.ps1' -Raw
$helper=($helper -split '\$report=@\(\)',2)[0]
. ([scriptblock]::Create($helper))
$items=@(
    @('DesertImpactCrater_Large_8x8_64px',8,8,'exec-0b71b0cf-5869-4016-8d7e-d6a3eac16038.png'),
    @('DesertErodedCrater_Large_7x7_64px',7,7,'exec-1f16a11c-3518-494f-ae36-bcd489bfdb16.png'),
    @('DesertSandMountain_4x4_64px',4,4,'exec-6dcf267e-889c-4dba-bec6-20f8fe76297f.png'),
    @('DesertFracturedRockMass_4x4_64px',4,4,'exec-9f339aa2-4cd1-47ad-a319-a37a9def32e5.png')
)
$preview=New-Raster 2176 1792
$g=[System.Drawing.Graphics]::FromImage($preview)
$g.Clear([System.Drawing.Color]::FromArgb(255,37,42,48));$g.Dispose()
$report=@()
for($i=0;$i -lt 4;$i++){
    $name=$items[$i][0];$cols=[int]$items[$i][1];$rows=[int]$items[$i][2]
    $w=$cols*64;$h=$rows*64;$path=Join-Path $dir ($name+'.png')
    if(Test-Path -LiteralPath $path){throw "Refusing to replace existing asset: $name"}
    $source=[System.Drawing.Bitmap]::FromFile((Join-Path $rawRoot $items[$i][3]))
    $crop=Get-AlphaBounds $source 128
    $drawH=[int][Math]::Round(($w-16)*$crop.Height/$crop.Width)
    if($drawH -gt $h){throw "Aspect ratio exceeds canvas: $name"}
    $dst=New-Raster $w $h
    Draw-Nearest $dst $source ([System.Drawing.Rectangle]::new(8,$h-$drawH,$w-16,$drawH)) $crop
    $source.Dispose()
    for($y=0;$y -lt $h;$y++){for($x=0;$x -lt $w;$x++){
        $p=$dst.GetPixel($x,$y)
        if($p.A -lt 128){$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(0,0,0,0))}
        else{$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(255,$p.R,$p.G,$p.B))}
    }}
    for($pass=0;$pass -lt 4;$pass++){
        $b=Get-AlphaBounds $dst 128
        if($b.Left -eq 8 -and $b.Right -eq $w-8 -and $b.Bottom -eq $h){break}
        $aligned=New-Raster $w $h
        $ah=[Math]::Min($h,[int][Math]::Round(($w-16)*$b.Height/$b.Width))
        Draw-Nearest $aligned $dst ([System.Drawing.Rectangle]::new(8,$h-$ah,$w-16,$ah)) $b
        $dst.Dispose();$dst=$aligned
    }
    $b=Get-AlphaBounds $dst 128
    if($b.Left -ne 8 -or $b.Right -ne $w-8 -or $b.Bottom -ne $h){throw "Incorrect margins: $name"}
    $tileNames=@()
    for($r=0;$r -lt $rows;$r++){for($c=0;$c -lt $cols;$c++){$tileNames+=('${0}_r{1}_c{2}' -f $name,$r,$c)}}
    Save-Sheet $dst $name $cols $rows $tileNames
    $meta=Get-Content -LiteralPath ($path+'.meta') -Raw
    $meta=$meta.Replace('textureCompression: 1','textureCompression: 0')
    Set-Content -LiteralPath ($path+'.meta') -Value $meta -Encoding utf8
    $slices=([regex]::Matches($meta,'      width: 64')).Count
    if($slices -ne $cols*$rows){throw "Incorrect slices: $name"}
    $report+=[PSCustomObject]@{Name=$name;Width=$w;Height=$h;Slices=$slices;LeftMargin=$b.Left;RightMargin=$w-$b.Right;BottomRow=$b.Bottom-1}
    $px=($i%2)*1088+(1088-$w*2)/2
    $py=if($i -lt 2){40+1024-$h*2}else{1220+512-$h*2}
    $g=[System.Drawing.Graphics]::FromImage($preview)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.DrawImage($dst,([System.Drawing.Rectangle]::new($px,$py,$w*2,$h*2)),([System.Drawing.Rectangle]::new(0,0,$w,$h)),[System.Drawing.GraphicsUnit]::Pixel)
    $font=[System.Drawing.Font]::new('Arial',22)
    $textY=if($i -lt 2){1088}else{1738}
    $g.DrawString(($name+" / ${w}x${h}px"),$font,[System.Drawing.Brushes]::White,[single](($i%2)*1088+32),[single]$textY)
    $font.Dispose();$g.Dispose();$dst.Dispose()
}
$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/ExtraDesertProps_Final_2x_Preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$report | ConvertTo-Json | Set-Content -LiteralPath tmp/imagegen/ExtraDesertProps_Validation.json -Encoding utf8
$report | Format-Table -AutoSize
