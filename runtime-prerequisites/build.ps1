[CmdletBinding()]
param(
    [string]$SourceDirectory = $PSScriptRoot,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\runtime-release-output')
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$source = (Resolve-Path -LiteralPath $SourceDirectory).Path
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'The output directory must not already exist.' }
$files = @('README.md', 'THIRD-PARTY-NOTICES.md', 'Install-DotNet35.cmd', 'Install-DirectX.cmd', 'Check-Runtime.cs', 'Check-Runtime.cmd')
foreach ($file in $files) {
    if (-not (Test-Path -LiteralPath (Join-Path $source $file) -PathType Leaf)) { throw "Missing package source: $file" }
}
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) { throw 'The .NET Framework 4 x86 compiler is required.' }
$bundleName = 'Magic600Cell-Runtime-Prerequisites-2026.09.16'
$bundle = Join-Path $output $bundleName
[void](New-Item -ItemType Directory -Path $bundle)
foreach ($file in $files) { Copy-Item -LiteralPath (Join-Path $source $file) -Destination (Join-Path $bundle $file) }

$downloadUrl = 'https://download.microsoft.com/download/8/4/a/84a35bf1-dafe-4ae8-82af-ad2ae20b6b14/directx_Jun2010_redist.exe'
$expectedHash = '053f76dcbb28802e23341b6a787e3b0791c0fa5c8d4d011b1044172dbf89c73b'
$installerName = 'redist/directx_Jun2010_redist.exe'
$installer = Join-Path $bundle $installerName
[void](New-Item -ItemType Directory -Path (Split-Path -Parent $installer))
Invoke-WebRequest -Uri $downloadUrl -OutFile $installer
if ((Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash) { throw 'The original DirectX installer SHA-256 does not match the reviewed source.' }
$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($signature.Status -ne 'Valid' -or $null -eq $signature.SignerCertificate -or $signature.SignerCertificate.Subject -notmatch '(^|,\s*)O=Microsoft Corporation(,|$)') {
    throw 'The DirectX installer must have a valid Microsoft Corporation Authenticode signature.'
}

$checker = Join-Path $bundle 'Check-Runtime.exe'
& $compiler /nologo /target:exe /platform:x86 "/out:$checker" (Join-Path $bundle 'Check-Runtime.cs')
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $checker -PathType Leaf)) { throw 'Runtime checker compilation failed.' }
$members = @($files) + @($installerName, 'Check-Runtime.exe')
$fileRecords = foreach ($name in $members) {
    $path = Join-Path $bundle $name
    [ordered]@{ name = $name; bytes = (Get-Item -LiteralPath $path).Length; sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$manifest = [ordered]@{
    package = $bundleName
    release_tag = 'runtime-prerequisites-2026.09.16'
    directx_source = [ordered]@{ url = $downloadUrl; sha256 = $expectedHash; authenticode_status = [string]$signature.Status; publisher = $signature.SignerCertificate.Subject; signer_thumbprint = $signature.SignerCertificate.Thumbprint }
    dotnet = 'Windows NetFx3 feature or applicable official installer; no Windows component-store CABs are bundled.'
    checker = 'Read-only .NET Framework 4 x86 executable compiled from the included source; not a target-PC installation test.'
    checksum_scope = 'The included SHA256SUMS.txt and files list cover the eight payload files; they exclude manifest.json and SHA256SUMS.txt to avoid recursive checksums. The separate release SHA256SUMS.txt verifies the complete ZIP.'
    files = @($fileRecords)
}
$payloadSums = @($fileRecords | ForEach-Object { $_.sha256 + '  ' + $_.name })
$payloadSums | Set-Content -LiteralPath (Join-Path $bundle 'SHA256SUMS.txt') -Encoding ascii
$manifestPath = Join-Path $bundle 'manifest.json'
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipPath = Join-Path $output ($bundleName + '.zip')
[System.IO.Compression.ZipFile]::CreateFromDirectory($bundle, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $true)
$expected = @{}
foreach ($name in @($members) + @('manifest.json', 'SHA256SUMS.txt')) {
    $expected[$bundleName + '/' + $name] = (Get-FileHash -LiteralPath (Join-Path $bundle $name) -Algorithm SHA256).Hash.ToLowerInvariant()
}
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    if ($archive.Entries.Count -ne $expected.Count) { throw 'Unexpected ZIP entry count.' }
    $seen = @{}
    foreach ($entry in $archive.Entries) {
        if (-not $expected.ContainsKey($entry.FullName) -or $seen.ContainsKey($entry.FullName)) { throw 'Unexpected or duplicate ZIP entry.' }
        $stream = $entry.Open()
        $hash = [System.Security.Cryptography.SHA256]::Create()
        try { $digest = ([System.BitConverter]::ToString($hash.ComputeHash($stream))).Replace('-', '').ToLowerInvariant() }
        finally { $hash.Dispose(); $stream.Dispose() }
        if ($digest -ne $expected[$entry.FullName]) { throw 'ZIP member hash verification failed.' }
        $seen[$entry.FullName] = $true
    }
} finally { $archive.Dispose() }
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
($zipHash + '  ' + [System.IO.Path]::GetFileName($zipPath)) | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding ascii
Write-Output "Verified runtime bundle: $zipPath"
Write-Output "SHA256: $zipHash"
