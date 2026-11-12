param([string]$WebViewSdk = $env:SHIXUN_WEBVIEW_SDK, [switch]$Test)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$outDir = Join-Path $root 'build'
$null = New-Item -ItemType Directory -Path $outDir -Force
if (-not $WebViewSdk) { $WebViewSdk = Join-Path $root '.deps/WebView2' }
if (-not (Test-Path (Join-Path $WebViewSdk 'lib/net462/Microsoft.Web.WebView2.Core.dll'))) { throw 'Set SHIXUN_WEBVIEW_SDK or use -WebViewSdk with extracted Microsoft.Web.WebView2 1.0.4314.35 NuGet directory.' }
foreach ($dll in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll')) { Copy-Item -LiteralPath (Join-Path $WebViewSdk "lib/net462/$dll") -Destination (Join-Path $outDir $dll) -Force }
Copy-Item -LiteralPath (Join-Path $WebViewSdk 'runtimes/win-x64/native/WebView2Loader.dll') -Destination $outDir -Force
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$refs = @('/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll','/reference:System.Xml.Linq.dll','/reference:System.Security.dll','/reference:Microsoft.CSharp.dll',"/reference:$outDir/Microsoft.Web.WebView2.Core.dll","/reference:$outDir/Microsoft.Web.WebView2.WinForms.dll")
$sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName)
& $compiler /nologo /codepage:65001 /target:winexe /platform:x64 "/win32icon:$root/assets/Shixun.ico" @refs "/out:$outDir/Shixun.exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Copy-Item -LiteralPath (Join-Path $root 'App.config') -Destination (Join-Path $outDir 'Shixun.exe.config') -Force
Copy-Item -LiteralPath (Join-Path $root 'ui') -Destination $outDir -Recurse -Force
foreach ($file in @('LICENSE','README.md','THIRD-PARTY-NOTICES.md','CHANGELOG.md')) { Copy-Item -LiteralPath (Join-Path $root $file) -Destination $outDir -Force }
Copy-Item -LiteralPath (Join-Path $WebViewSdk 'LICENSE.txt') -Destination (Join-Path $outDir 'LICENSE-WebView2.txt') -Force
Copy-Item -LiteralPath (Join-Path $WebViewSdk 'NOTICE.txt') -Destination (Join-Path $outDir 'NOTICE-WebView2.txt') -Force
if ($Test) {
 $core = @('Models.cs','Store.cs','Transport.cs','Feeds.cs','Rules.cs','Engine.cs','ImageCache.cs','AiService.cs','BatchEdit.cs') | ForEach-Object { Join-Path $root "src/$_" }
 & $compiler /nologo /codepage:65001 /target:exe /platform:x64 /reference:System.Web.Extensions.dll /reference:System.Security.dll /reference:System.Xml.Linq.dll "/out:$outDir/CoreTests.exe" @core (Join-Path $root 'tests/CoreTests.cs')
 if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
 Copy-Item -LiteralPath (Join-Path $root 'App.config') -Destination (Join-Path $outDir 'CoreTests.exe.config') -Force
 & (Join-Path $outDir 'CoreTests.exe')
 if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
 & $compiler /nologo /codepage:65001 /target:exe /platform:x64 /reference:System.Web.Extensions.dll /reference:System.Security.dll /reference:System.Xml.Linq.dll "/out:$outDir/AiTests.exe" @core (Join-Path $root 'tests/AiTests.cs')
 if ($LASTEXITCODE -ne 0) { throw 'AI test compilation failed' }
 Copy-Item -LiteralPath (Join-Path $root 'App.config') -Destination (Join-Path $outDir 'AiTests.exe.config') -Force
 & (Join-Path $outDir 'AiTests.exe')
 if ($LASTEXITCODE -ne 0) { throw 'AI tests failed' }
 & $compiler /nologo /codepage:65001 /target:exe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "/out:$outDir/CredentialTests.exe" (Join-Path $root 'src/Models.cs') (Join-Path $root 'src/Transport.cs') (Join-Path $root 'src/CredentialDialog.cs') (Join-Path $root 'tests/CredentialTests.cs')
 if ($LASTEXITCODE -ne 0) { throw 'Credential test compilation failed' }
 & (Join-Path $outDir 'CredentialTests.exe')
 if ($LASTEXITCODE -ne 0) { throw 'Credential dialog test failed' }
 & $compiler /nologo /codepage:65001 /target:exe /platform:x64 /reference:System.Windows.Forms.dll "/reference:$outDir/Shixun.exe" "/out:$outDir/SourceActionsTests.exe" (Join-Path $root 'tests/SourceActionsTests.cs')
 if ($LASTEXITCODE -ne 0) { throw 'Source action test compilation failed' }
 Copy-Item -LiteralPath (Join-Path $root 'App.config') -Destination (Join-Path $outDir 'SourceActionsTests.exe.config') -Force
 & (Join-Path $outDir 'SourceActionsTests.exe')
 if ($LASTEXITCODE -ne 0) { throw 'Source action tests failed' }
 & $compiler /nologo /codepage:65001 /target:exe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/reference:$outDir/Shixun.exe" "/out:$outDir/PopupTests.exe" (Join-Path $root 'tests/PopupTests.cs')
 if ($LASTEXITCODE -ne 0) { throw 'Popup test compilation failed' }
 Copy-Item -LiteralPath (Join-Path $root 'App.config') -Destination (Join-Path $outDir 'PopupTests.exe.config') -Force
 & (Join-Path $outDir 'PopupTests.exe')
 if ($LASTEXITCODE -ne 0) { throw 'Popup test failed' }
}
Write-Output 'Built Shixun.exe'
