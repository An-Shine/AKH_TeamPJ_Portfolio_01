param([string[]]$Sources, [switch]$ReplaceExisting, [switch]$Monochrome)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskNames=@('MapIcon_Combat_64px','MapIcon_ItemRoom_64px','MapIcon_Boss_64px')
$taskLabels=@('Combat','Item Room','Boss')
if($Sources.Count -ne 3){throw 'Three sources required'}
$taskPreview=[Drawing.Bitmap]::new(600,232,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$taskPreviewG=[Drawing.Graphics]::FromImage($taskPreview)
$taskPreviewG.Clear([Drawing.Color]::FromArgb(255,35,40,49))
$taskFont=[Drawing.Font]::new('Arial',13)
try{
 for($taskI=0;$taskI -lt 3;$taskI++){
  $taskPath="Assets/04.Sprites/UI/Minimap/$($taskNames[$taskI]).png"
  if((Test-Path -LiteralPath $taskPath) -and !$ReplaceExisting){throw "Output exists: $taskPath"}
  $taskSrc=[Drawing.Bitmap]::FromFile($Sources[$taskI])
  $taskSmall=[Drawing.Bitmap]::new(32,32,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $taskOut=[Drawing.Bitmap]::new(64,64,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
  try{
   $taskMinX=$taskSrc.Width;$taskMinY=$taskSrc.Height;$taskMaxX=-1;$taskMaxY=-1
   for($taskY=0;$taskY -lt $taskSrc.Height;$taskY++){
    for($taskX=0;$taskX -lt $taskSrc.Width;$taskX++){
     if($taskSrc.GetPixel($taskX,$taskY).A -ge 128){
      $taskMinX=[Math]::Min($taskMinX,$taskX);$taskMinY=[Math]::Min($taskMinY,$taskY)
      $taskMaxX=[Math]::Max($taskMaxX,$taskX);$taskMaxY=[Math]::Max($taskMaxY,$taskY)
     }
    }
   }
   if($taskMaxX -lt 0){throw 'Empty generated icon'}
   $taskW=$taskMaxX-$taskMinX+1;$taskH=$taskMaxY-$taskMinY+1
   $taskScale=28/[Math]::Max($taskW,$taskH)
   $taskDW=[Math]::Max(1,[int][Math]::Round($taskW*$taskScale));$taskDH=[Math]::Max(1,[int][Math]::Round($taskH*$taskScale))
   $taskDX=[int][Math]::Floor((32-$taskDW)/2);$taskDY=[int][Math]::Floor((32-$taskDH)/2)
   for($taskY=0;$taskY -lt $taskDH;$taskY++){
    for($taskX=0;$taskX -lt $taskDW;$taskX++){
     $taskC=$taskSrc.GetPixel($taskMinX+[int][Math]::Floor(($taskX+0.5)*$taskW/$taskDW),$taskMinY+[int][Math]::Floor(($taskY+0.5)*$taskH/$taskDH))
     if($taskC.A -ge 128){
      $taskColor=if($Monochrome){[Drawing.Color]::White}else{[Drawing.Color]::FromArgb(255,$taskC.R,$taskC.G,$taskC.B)}
      $taskSmall.SetPixel($taskDX+$taskX,$taskDY+$taskY,$taskColor)
     }
    }
   }
   for($taskY=0;$taskY -lt 64;$taskY++){
    for($taskX=0;$taskX -lt 64;$taskX++){
     $taskOut.SetPixel($taskX,$taskY,$taskSmall.GetPixel([int][Math]::Floor($taskX/2),[int][Math]::Floor($taskY/2)))
    }
   }
   $taskOut.Save((Join-Path (Get-Location) $taskPath),[Drawing.Imaging.ImageFormat]::Png)
   $taskPreviewG.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
   $taskPreviewG.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
   $taskPreviewG.DrawImage($taskOut,[Drawing.Rectangle]::new($taskI*200+4,4,192,192),0,0,64,64,[Drawing.GraphicsUnit]::Pixel)
   $taskPreviewG.DrawString($taskLabels[$taskI],$taskFont,[Drawing.Brushes]::White,$taskI*200+45,204)
  }finally{$taskSrc.Dispose();$taskSmall.Dispose();$taskOut.Dispose()}
  if(!(Test-Path -LiteralPath "$taskPath.meta")){
   $taskMeta=Get-Content -LiteralPath 'Assets/04.Sprites/UI/Minimap/Stage01_Minimap_Frame.png.meta' -Raw
   $taskMeta=[regex]::Replace($taskMeta,'(?m)^guid:.*$',('guid: '+[guid]::NewGuid().ToString('N')))
   $taskMeta=[regex]::Replace($taskMeta,'(?m)^    spriteID:.*$',('    spriteID: '+[guid]::NewGuid().ToString('N')))
   $taskMeta=$taskMeta.Replace('spritePixelsToUnits: 100','spritePixelsToUnits: 64')
   [IO.File]::WriteAllText((Join-Path (Get-Location) "$taskPath.meta"),$taskMeta,[Text.UTF8Encoding]::new($false))
  }
  Write-Output "$taskPath : 64x64, transparent, Single / Point / None"
 }
 $taskPreview.Save((Join-Path (Get-Location) 'tmp/imagegen/MapIcons_Preview.png'),[Drawing.Imaging.ImageFormat]::Png)
}finally{$taskPreviewG.Dispose();$taskPreview.Dispose();$taskFont.Dispose()}
