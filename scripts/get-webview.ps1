$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$folder = Join-Path $root '.deps'
$null = New-Item -ItemType Directory -Path $folder -Force
$zip = Join-Path $folder 'webview2.zip'
Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.4314.35/microsoft.web.webview2.1.0.4314.35.nupkg' -OutFile $zip
if ((Get-FileHash $zip -Algorithm SHA256).Hash -ne '90778DEB1B77E36292828B4B5833C9255C27533F40E0DDD55891AE718944EDC4') { throw 'WebView2 package SHA256 mismatch' }
Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $folder 'WebView2') -Force
Write-Output 'Verified WebView2 1.0.4314.35 SDK'
