param([Parameter(Mandatory=$true)][string]$XrealSdkPath)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$sdk=(Resolve-Path -LiteralPath $XrealSdkPath).Path
if(!(Test-Path -LiteralPath (Join-Path $sdk 'package.json'))){throw 'SDK package.json fehlt'}
Expand-Archive -LiteralPath (Join-Path $root 'assets.zip') -DestinationPath (Join-Path $root 'SpatialWindowUnity/Assets') -Force
$path=Join-Path $root 'SpatialWindowUnity/Packages/manifest.json'
$manifest=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
$manifest.dependencies | Add-Member -NotePropertyName 'com.xreal.xr' -NotePropertyValue ('file:'+($sdk -replace '\\','/')) -Force
$manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8
Write-Output 'Assets vorbereitet und lokales XREAL SDK eingetragen. Unity-Projekt jetzt öffnen.'
