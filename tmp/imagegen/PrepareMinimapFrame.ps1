$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskSource = 'C:/Users/akh10/.codex/generated_images/01a0c7d3-dd34-7320-89ef-a7777882bcd3/exec-6529cbee-6bf2-42e8-9ce3-93972862f209.png'
$taskOutput = 'Assets/04.Sprites/UI/Minimap/Stage01_Minimap_Frame.png'
if(Test-Path -LiteralPath $taskOutput){throw 'Output exists'}
$taskSrc = [Drawing.Bitmap]::FromFile($taskSource)
$taskDst = [Drawing.Bitmap]::new(572,362,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    for($taskY=0;$taskY -lt 362;$taskY++){
        for($taskX=0;$taskX -lt 572;$taskX++){
            $taskC=$taskSrc.GetPixel([Math]::Min($taskSrc.Width-1,[int][Math]::Floor(($taskX+0.5)*$taskSrc.Width/572)),[Math]::Min($taskSrc.Height-1,[int][Math]::Floor(($taskY+0.5)*$taskSrc.Height/362)))
            if($taskC.A -lt 128){continue}
            $taskValue=($taskC.R+$taskC.G+$taskC.B)/3
            if($taskValue -ge 165){$taskColor=[Drawing.Color]::FromArgb(255,180,200,234)}
            elseif($taskValue -ge 130){$taskColor=[Drawing.Color]::FromArgb(255,109,127,174)}
            elseif($taskValue -ge 72){$taskColor=[Drawing.Color]::FromArgb(255,72,82,125)}
            else{$taskColor=[Drawing.Color]::FromArgb(255,48,57,87)}
            $taskDst.SetPixel($taskX,$taskY,$taskColor)
        }
    }
    # Retain the connected map silhouette; discard isolated alpha artifacts.
    $taskSeen=[bool[]]::new(572*362)
    $taskLargest=[Collections.Generic.List[int]]::new()
    for($taskIndex=0;$taskIndex -lt $taskSeen.Length;$taskIndex++){
        if($taskSeen[$taskIndex]){continue}
        $taskSeen[$taskIndex]=$true
        if($taskDst.GetPixel($taskIndex%572,[int][Math]::Floor($taskIndex/572)).A -eq 0){continue}
        $taskComponent=[Collections.Generic.List[int]]::new()
        $taskQueue=[Collections.Generic.Queue[int]]::new()
        $taskQueue.Enqueue($taskIndex)
        while($taskQueue.Count -gt 0){
            $taskCurrent=$taskQueue.Dequeue();$taskComponent.Add($taskCurrent)
            $taskCX=$taskCurrent%572;$taskCY=[int][Math]::Floor($taskCurrent/572)
            foreach($taskNeighbor in @(($taskCurrent-1),($taskCurrent+1),($taskCurrent-572),($taskCurrent+572))){
                if($taskNeighbor -lt 0 -or $taskNeighbor -ge $taskSeen.Length){continue}
                if([Math]::Abs(($taskNeighbor%572)-$taskCX)+[Math]::Abs([int][Math]::Floor($taskNeighbor/572)-$taskCY) -ne 1){continue}
                if($taskSeen[$taskNeighbor]){continue};$taskSeen[$taskNeighbor]=$true
                if($taskDst.GetPixel($taskNeighbor%572,[int][Math]::Floor($taskNeighbor/572)).A -gt 0){$taskQueue.Enqueue($taskNeighbor)}
            }
        }
        if($taskComponent.Count -gt $taskLargest.Count){$taskLargest=$taskComponent}
    }
    $taskKeep=[bool[]]::new(572*362)
    foreach($taskIndex in $taskLargest){$taskKeep[$taskIndex]=$true}
    for($taskIndex=0;$taskIndex -lt $taskKeep.Length;$taskIndex++){
        if(!$taskKeep[$taskIndex]){$taskDst.SetPixel($taskIndex%572,[int][Math]::Floor($taskIndex/572),[Drawing.Color]::FromArgb(0,0,0,0))}
    }
    $taskDst.Save((Join-Path (Get-Location) $taskOutput),[Drawing.Imaging.ImageFormat]::Png)
} finally {$taskSrc.Dispose();$taskDst.Dispose()}
$taskMeta=Get-Content 'Assets/04.Sprites/UI/Minimap/Stage01_Minimap_Draft.png.meta' -Raw
$taskMeta=[regex]::Replace($taskMeta,'(?m)^guid:.*$',('guid: '+[guid]::NewGuid().ToString('N')))
$taskMeta=[regex]::Replace($taskMeta,'(?s)  internalIDToNameTable:.*?(?=  externalObjects:)','  internalIDToNameTable: []' + "`n")
$taskMeta=$taskMeta.Replace('filterMode: 1','filterMode: 0').Replace('spriteMode: 2','spriteMode: 1').Replace('spriteMeshType: 1','spriteMeshType: 0').Replace('textureCompression: 1','textureCompression: 0')
$taskSheet="  spriteSheet:`n    serializedVersion: 2`n    sprites: []`n    outline: []`n    physicsShape: []`n    bones: []`n    spriteID: $([guid]::NewGuid().ToString('N'))`n    internalID: 0`n    vertices: []`n    indices: `n    edges: []`n    weights: []`n    secondaryTextures: []`n    nameFileIdTable: {}`n"
$taskMeta=[regex]::Replace($taskMeta,'(?s)  spriteSheet:.*?(?=  mipmapLimitGroupName:)',$taskSheet)
[IO.File]::WriteAllText((Join-Path (Get-Location) "$taskOutput.meta"),$taskMeta,[Text.UTF8Encoding]::new($false))
Write-Output "Saved $taskOutput (572x362, transparent, Single Sprite, Point, no compression)."
