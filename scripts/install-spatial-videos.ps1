$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$adb='C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
$devices=@(& $adb devices | Where-Object {$_ -match '^\S+\s+device$'} | ForEach-Object {($_ -split '\s+')[0]})
if($devices.Count -ne 1){throw 'Genau einen autorisierten Beam Pro anschliessen.'}
$serial=$devices[0]
if((& $adb -s $serial shell getprop ro.product.model).Trim() -ne 'X4000'){throw 'Erwartet Beam Pro X4000.'}
$remote='/sdcard/Android/data/ch.sebastienspiess.raumfenster/files/videos'
& $adb -s $serial shell mkdir -p $remote
if($LASTEXITCODE -ne 0){throw 'Videoordner konnte nicht erstellt werden.'}
foreach($name in @('bees-h264.mp4')){
 $source=Join-Path $root "spatial/video/$name"
 if(!(Test-Path -LiteralPath $source)){throw "Fehlendes Video: $name"}
 & $adb -s $serial push $source "$remote/$name"
 if($LASTEXITCODE -ne 0){throw "Videoübertragung fehlgeschlagen: $name"}
 $expected=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
 $actual=((& $adb -s $serial shell sha256sum "$remote/$name") -split '\s+')[0]
 if($actual -ne $expected){throw "Prüfsumme stimmt nicht: $name"}
 Write-Output "$name lokal installiert und SHA256 geprüft."
}
