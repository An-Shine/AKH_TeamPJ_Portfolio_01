Add-Type -AssemblyName System.Drawing
# Reuse the grid metadata writer used by the cliff import pipeline.
$helperText=Get-Content -LiteralPath 'tmp/imagegen/PrepareDesertCliffs.ps1' -Raw
$helperText=($helperText -split '\$src=\[System.Drawing.Bitmap\]::FromFile',2)[0]
. ([scriptblock]::Create($helperText))
$dir=Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/Obstacles'
$rawRoot='C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3'
$items=@(
    @('DesertCrashedShip_4x3_64px',4,3,'exec-85cbc2d9-e729-4dd2-848d-7db4c30698cd.png'),
    @('DesertSatelliteWreck_3x3_64px',3,3,'exec-8c827ad3-eb72-4309-b048-ed82c36a373c.png'),
    @('DesertImpactCrater_4x3_64px',4,3,'exec-dc1d038e-c31f-44c6-9c31-387d4fad8ace.png'),
    @('DesertSandDune_4x2_64px',4,2,'exec-7f871095-7b2a-448a-b7d3-5b706948e1af.png'),
    @('DesertRockPillars_3x3_64px',3,3,'exec-f879bed8-2318-4433-a5c8-a91b8fef0aab.png'),
    @('DesertCommsWreck_3x3_64px',3,3,'exec-ee3da7ab-638a-4047-af96-ad0c85b78615.png')
)
function Get-AlphaBounds($bitmap,$threshold){
    $left=$bitmap.Width;$top=$bitmap.Height;$right=-1;$bottom=-1
    for($y=0;$y -lt $bitmap.Height;$y++){for($x=0;$x -lt $bitmap.Width;$x++){
        if($bitmap.GetPixel($x,$y).A -ge $threshold){
            $left=[Math]::Min($left,$x);$right=[Math]::Max($right,$x)
            $top=[Math]::Min($top,$y);$bottom=[Math]::Max($bottom,$y)
        }
    }}
    if($right -lt 0){throw 'Empty sprite'}
    return [System.Drawing.Rectangle]::new($left,$top,$right-$left+1,$bottom-$top+1)
}
$report=@()
$preview=New-Raster 1536 1152
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(255,37,42,48));$pg.Dispose()
for($i=0;$i -lt $items.Count;$i++){
    $name=$items[$i][0];$cols=[int]$items[$i][1];$rows=[int]$items[$i][2]
    $w=$cols*64;$h=$rows*64
    $assetPath=Join-Path $dir ($name+'.png')
    if(Test-Path -LiteralPath $assetPath){throw "Refusing to overwrite existing asset: $assetPath"}
    $source=[System.Drawing.Bitmap]::FromFile((Join-Path $rawRoot $items[$i][3]))
    $crop=Get-AlphaBounds $source 128
    $drawW=$w-16;$drawH=[int][Math]::Round($drawW*$crop.Height/$crop.Width)
    if($drawH -gt $h){throw "Subject does not fit without distortion: $name"}
    $dst=New-Raster $w $h
    Draw-Nearest $dst $source ([System.Drawing.Rectangle]::new(8,$h-$drawH,$drawW,$drawH)) $crop
    $source.Dispose()
    # Normalize the pixel-art alpha after sampling; discard near-transparent RGB artifacts.
    for($y=0;$y -lt $h;$y++){for($x=0;$x -lt $w;$x++){
        $c=$dst.GetPixel($x,$y)
        if($c.A -lt 128){$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(0,0,0,0))}
        else{$dst.SetPixel($x,$y,[System.Drawing.Color]::FromArgb(255,$c.R,$c.G,$c.B))}
    }}
    # Align occupied bounds to 8px side margins and the bottom of the canvas.
    for($pass=0;$pass -lt 4;$pass++){
        $b=Get-AlphaBounds $dst 128
        if($b.Left -eq 8 -and $b.Right -eq $w-8 -and $b.Bottom -eq $h){break}
        $aligned=New-Raster $w $h
        $alignedH=[Math]::Min($h,[int][Math]::Round(($w-16)*$b.Height/$b.Width))
        Draw-Nearest $aligned $dst ([System.Drawing.Rectangle]::new(8,$h-$alignedH,$w-16,$alignedH)) $b
        $dst.Dispose();$dst=$aligned
    }
    $bounds=Get-AlphaBounds $dst 128
    if($bounds.Left -ne 8 -or $bounds.Right -ne $w-8 -or $bounds.Bottom -ne $h){throw "Alpha alignment failed: $name"}
    $tileNames=@()
    for($row=0;$row -lt $rows;$row++){for($col=0;$col -lt $cols;$col++){$tileNames+=('${0}_r{1}_c{2}' -f $name,$row,$col)}}
    Save-Sheet $dst $name $cols $rows $tileNames
    $meta=Get-Content -LiteralPath ($assetPath+'.meta') -Raw
    $sliceCount=([regex]::Matches($meta,'      width: 64')).Count
    if($sliceCount -ne $cols*$rows){throw "Incorrect slice count: $name"}
    $report+=[PSCustomObject]@{Name=$name;Width=$w;Height=$h;Slices=$sliceCount;LeftMargin=$bounds.Left;RightMargin=$w-$bounds.Right;BottomRow=$bounds.Bottom-1}
    $px=($i%3)*512;$py=[int][Math]::Floor($i/3)*576
    $g=[System.Drawing.Graphics]::FromImage($preview)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.DrawImage($dst,([System.Drawing.Rectangle]::new($px+(512-$w*2)/2,$py+48+(384-$h*2)/2,$w*2,$h*2)),([System.Drawing.Rectangle]::new(0,0,$w,$h)),[System.Drawing.GraphicsUnit]::Pixel)
    $font=[System.Drawing.Font]::new('Arial',18)
    $g.DrawString(($name+"`n${cols}x${rows} / ${w}x${h}px"),$font,[System.Drawing.Brushes]::White,[single]($px+16),[single]($py+460))
    $font.Dispose();$g.Dispose();$dst.Dispose()
}
$preview.Save((Join-Path (Get-Location) 'tmp/imagegen/DesertProps_Final_2x_Preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$report | ConvertTo-Json | Set-Content -LiteralPath tmp/imagegen/DesertProps_Validation.json -Encoding utf8
$report | Format-Table -AutoSize
