$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$folder = Join-Path $root 'dist'
$null = New-Item -ItemType Directory -Path $folder -Force
$tracked = @(& git -C $root ls-files)
if ($LASTEXITCODE -ne 0 -or $tracked.Count -eq 0) { throw 'Package from a committed clean source tree' }
& git -C $root diff --quiet HEAD
if ($LASTEXITCODE -ne 0) { throw 'Commit source changes before packaging matching source' }
$source = Join-Path $folder 'Shixun-1.0.0-source.zip'
& git -C $root archive --format=zip --output=$source HEAD
if ($LASTEXITCODE -ne 0) { throw 'Source archive failed' }
$stage = Join-Path $folder 'portable'
$null = New-Item -ItemType Directory -Path $stage -Force
$files = @('Shixun.exe','Shixun.exe.config','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','LICENSE','LICENSE-WebView2.txt','NOTICE-WebView2.txt','README.md','THIRD-PARTY-NOTICES.md','CHANGELOG.md','ui')
foreach ($file in $files) { Copy-Item -LiteralPath (Join-Path $root "build/$file") -Destination $stage -Recurse -Force }
Copy-Item -LiteralPath $source -Destination $stage -Force
$zip = Join-Path $folder 'Shixun-1.0.0-win-x64.zip'
Compress-Archive -LiteralPath (@($files | ForEach-Object { Join-Path $stage $_ }) + @((Join-Path $stage 'Shixun-1.0.0-source.zip'))) -DestinationPath $zip -Force
Get-FileHash -LiteralPath $zip,$source -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_.Path))" } | Set-Content -LiteralPath (Join-Path $folder 'SHA256SUMS.txt') -Encoding ascii
Write-Output 'Created portable and matching source archives using an explicit file allowlist.'
