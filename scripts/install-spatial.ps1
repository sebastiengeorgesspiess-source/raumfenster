$ErrorActionPreference = 'Stop'
$spatialRoot = Split-Path -Parent $PSScriptRoot
$adb = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
$apk = Join-Path $spatialRoot 'builds\Raumfenster.apk'
if (!(Test-Path -LiteralPath $apk)) { throw 'Raumfenster.apk fehlt. Zuerst build-spatial.ps1 ausführen.' }
$devices = @(& $adb devices | Where-Object { $_ -match '^\S+\s+device$' } | ForEach-Object { ($_ -split '\s+')[0] })
if ($devices.Count -ne 1) { throw 'Genau einen entsperrten Beam Pro per USB-Debugging verbinden.' }
$model = (& $adb -s $devices[0] shell getprop ro.product.model).Trim()
if ($model -ne 'X4000') { throw "Erwartet Beam Pro X4000, gefunden: $model. Installation abgebrochen." }
& $adb -s $devices[0] install -r $apk
if ($LASTEXITCODE -ne 0) { throw 'APK-Installation fehlgeschlagen.' }
& $adb -s $devices[0] shell am force-stop ch.sebastienspiess.raumfenster
Write-Output 'Raumfenster installiert. Brille mit Eye anschliessen und Raumfenster in MyGlasses öffnen.'
