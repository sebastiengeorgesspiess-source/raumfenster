$ErrorActionPreference = 'Stop'
$spatialRoot = Split-Path -Parent $PSScriptRoot
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe'
$projectPath = Join-Path $spatialRoot 'SpatialWindowUnity'
$logPath = Join-Path $spatialRoot 'spatial-build.log'
$buildProcess = Start-Process -FilePath $unityExe -ArgumentList "-batchmode -nographics -quit -projectPath `"$projectPath`" -buildTarget Android -executeMethod SpatialBuild.Build -logFile `"$logPath`"" -WindowStyle Hidden -PassThru -Wait
if ($buildProcess.ExitCode -ne 0) { throw "Unity-Bau fehlgeschlagen. Details: $logPath" }
Write-Output (Join-Path $spatialRoot 'builds\Raumfenster.apk')
