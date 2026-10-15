# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (c) 2026 EndfieldBridge contributors. Modified 2026-10-15.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$InnoCompiler,
    [string]$OutputDirectory,
    [string]$SourceRevision = 'HEAD'
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
[xml]$project = Get-Content -LiteralPath (Join-Path $root 'src\EndfieldBridge.csproj')
$version = [string]$project.Project.PropertyGroup.Version
$runtime = [string]$project.Project.PropertyGroup.RuntimeFrameworkVersion
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid project version.' }
if (!(Test-Path -LiteralPath $InnoCompiler -PathType Leaf)) { throw 'Specify an installed Inno Setup 6.7+ ISCC.exe.' }
$gitArgs = @('-c', "safe.directory=$root", '-C', $root)
$revision = (& git @gitArgs rev-parse $SourceRevision).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve source revision.' }
if ((& git @gitArgs rev-parse HEAD).Trim() -ne $revision) { throw 'Build the checked-out revision only.' }
if (& git @gitArgs status --porcelain) { throw 'Commit source and packaging changes before building a release.' }
if (!$OutputDirectory) { $OutputDirectory = Join-Path $root "outputs\releases\$version" }
$out = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $out) { throw "Choose a fresh output directory: $out" }
$payloadName = "EndfieldBridge-$version-win-x64"
$payload = Join-Path $out "payload\$payloadName"
$assets = Join-Path $out 'assets'
New-Item -ItemType Directory -Path $payload, $assets | Out-Null
$env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
$env:MSBuildEnableWorkloadResolver = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
& dotnet publish (Join-Path $root 'src\EndfieldBridge.csproj') -c Release -r win-x64 --self-contained true -o $payload -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -p:ContinuousIntegrationBuild=true
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
foreach ($name in @('LICENSE','NOTICE.md','INSTALL.md','CHANGELOG.md','README.md','README.zh-CN.md','RISK.md','RISK.zh-CN.md','CONTRIBUTING.md')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination $payload
}
New-Item -ItemType Directory -Path (Join-Path $payload 'docs\images') -Force | Out-Null
Copy-Item -Path (Join-Path $root 'docs\images\*.png') -Destination (Join-Path $payload 'docs\images')
$cache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget\packages' }
foreach ($pack in @('microsoft.netcore.app.runtime.win-x64','microsoft.windowsdesktop.app.runtime.win-x64')) {
    $packDir = Join-Path $cache "$pack\$runtime"
    $legal = @(Get-ChildItem -LiteralPath $packDir -File | Where-Object { $_.Name -match '(?i)license|thirdpartynotices' })
    if (!$legal.Count) { throw "Runtime notices missing in $packDir" }
    foreach ($file in $legal) { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $payload "$pack-$($file.Name)") }
}
$compilerLicense = Join-Path (Split-Path -Parent $InnoCompiler) 'License.txt'
if (!(Test-Path -LiteralPath $compilerLicense)) { throw 'Inno Setup License.txt is required.' }
Copy-Item -LiteralPath $compilerLicense -Destination (Join-Path $payload 'INNO-SETUP-LICENSE.txt')
$sourceName = "EndfieldBridge-$version-source.zip"
$sourceUrl = "https://github.com/uyujkk/EndfieldBridge/releases/download/v$version/$sourceName"
$buildInfo = [ordered]@{version=$version;commit=$revision;runtime=$runtime;rid='win-x64';selfContained=$true;source=$sourceUrl;license='AGPL-3.0-only'}
$buildInfo | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $payload 'BUILD-INFO.json') -Encoding utf8
@("Corresponding source / 对应源码: $sourceUrl", "Commit: $revision", 'AGPL-3.0-only; see LICENSE and NOTICE.md. Rebuild with scripts/Build-Release.ps1.') | Set-Content -LiteralPath (Join-Path $payload 'SOURCE.txt') -Encoding utf8
"@echo off`r`n`"%~dp0EndfieldBridge.exe`" --launch`r`nexit /b %errorlevel%`r`n" | Set-Content -LiteralPath (Join-Path $payload 'StartFromModManager.cmd') -Encoding ascii
$exe = Join-Path $payload 'EndfieldBridge.exe'
$test = Start-Process -FilePath $exe -ArgumentList '--self-test' -WorkingDirectory $payload -WindowStyle Hidden -PassThru
if (!$test.WaitForExit(60000)) { throw 'Self-test timed out; inspect the isolated test process.' }
$test.Refresh()
Copy-Item -LiteralPath (Join-Path $payload 'self-test.txt') -Destination (Join-Path $out 'self-test.txt')
if ($test.ExitCode -ne 0) { throw 'Self-test failed; see self-test.txt.' }
# A fresh publish output is staged with test fixtures and user data excluded.
$forbidden = @('profile.json','bridge.log','self-test.txt','parent-probe.txt','integration-backup.json','test-fixture')
$cleanPayload = Join-Path $out "distribution\$payloadName"
New-Item -ItemType Directory -Path $cleanPayload | Out-Null
foreach ($item in Get-ChildItem -LiteralPath $payload -Force) {
    if ($item.Name -notin $forbidden) { Copy-Item -LiteralPath $item.FullName -Destination $cleanPayload -Recurse }
}
$bad = @(Get-ChildItem -LiteralPath $cleanPayload -Recurse -File | Where-Object { $_.Name -match '(?i)(^profile\.json$|\.bak$|\.lock$|\.pfx$|\.p12$|\.pdb$|^bridge\.log$|^self-test\.txt$)' })
if ($bad.Count) { throw 'Private or temporary files detected in distribution.' }
$portable = Join-Path $assets "EndfieldBridge-$version-win-x64-portable.zip"
Compress-Archive -LiteralPath $cleanPayload -DestinationPath $portable -CompressionLevel Optimal
& git @gitArgs archive --format=zip "--prefix=EndfieldBridge-$version-source/" "--output=$(Join-Path $assets $sourceName)" $revision
if ($LASTEXITCODE -ne 0) { throw 'Source archive failed.' }
& $InnoCompiler "/DAppVersion=$version" "/DPayloadDir=$cleanPayload" "/DArtifactDir=$assets" (Join-Path $root 'packaging\EndfieldBridge.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
Get-ChildItem -LiteralPath $assets -File | Sort-Object Name | ForEach-Object { '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name } | Set-Content -LiteralPath (Join-Path $assets 'SHA256SUMS.txt') -Encoding ascii
Write-Output "Release assets: $assets"
