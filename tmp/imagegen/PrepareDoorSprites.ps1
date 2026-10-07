Add-Type -AssemblyName System.Drawing
$outDir = Join-Path (Get-Location) 'Assets/04.Sprites/Tilemaps/Obstacles/Doors'
[void](New-Item -ItemType Directory -Path $outDir -Force)
function Export-Part($source, $crop, $width, $height, $target, $dest) {
    $bitmap = [System.Drawing.Bitmap]::new($width,$height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.DrawImage($source,$dest,$crop,[System.Drawing.GraphicsUnit]::Pixel)
    $graphics.Dispose()
    $bitmap.Save((Join-Path $outDir $target),[System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}
$h = [System.Drawing.Bitmap]::FromFile('C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-2c699ad3-7b16-413a-b890-f6708a3259ea.png')
Export-Part $h ([System.Drawing.Rectangle]::new(6,492,1242,360)) 192 64 'SlidingDoor_H_Frame.png' ([System.Drawing.Rectangle]::new(0,0,192,64))
Export-Part $h ([System.Drawing.Rectangle]::new(470,870,314,288)) 64 64 'SlidingDoor_H_Leaf.png' ([System.Drawing.Rectangle]::new(5,11,54,51))
$h.Dispose()
$v = [System.Drawing.Bitmap]::FromFile('C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-c5bdb343-6cb6-40c3-8456-16a0a0bee2c6.png')
$frame = [System.Drawing.Bitmap]::new(64,192,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($frame)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.DrawImage($v,([System.Drawing.Rectangle]::new(24,0,16,64)),([System.Drawing.Rectangle]::new(662,100,212,207)),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($v,([System.Drawing.Rectangle]::new(24,64,16,64)),([System.Drawing.Rectangle]::new(662,307,212,463)),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($v,([System.Drawing.Rectangle]::new(24,128,16,64)),([System.Drawing.Rectangle]::new(662,770,212,124)),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$frame.Save((Join-Path $outDir 'SlidingDoor_V_Frame.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$frame.Dispose()
Export-Part $v ([System.Drawing.Rectangle]::new(1154,307,56,463)) 64 64 'SlidingDoor_V_Leaf.png' ([System.Drawing.Rectangle]::new(30,0,4,64))
$v.Dispose()
foreach ($file in Get-ChildItem -LiteralPath $outDir -Filter '*.png') {
    $metaPath = $file.FullName + '.meta'
    if (-not (Test-Path -LiteralPath $metaPath)) {
        $guid = [Guid]::NewGuid().ToString('N')
        $meta = @"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 64
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: $([Guid]::NewGuid().ToString('N'))
    internalID: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"@
        Set-Content -LiteralPath $metaPath -Value $meta -Encoding utf8
    }
    $bmp = [System.Drawing.Bitmap]::FromFile($file.FullName)
    [PSCustomObject]@{File=$file.Name;Width=$bmp.Width;Height=$bmp.Height;Alpha=$bmp.GetPixel(0,0).A}
    $bmp.Dispose()
}
