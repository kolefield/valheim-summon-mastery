$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$dll = Join-Path $root 'bin/Release/netstandard2.1/SummonMastery.dll'
if (!(Test-Path -LiteralPath $dll)) { throw 'Build Release first.' }
[xml]$project = Get-Content (Join-Path $root 'SummonMastery.csproj')
$version = $project.Project.PropertyGroup.Version
$binaryVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3)
if ($version -ne $binaryVersion) { throw 'Project and DLL versions differ; rebuild.' }
$manifestPath = Join-Path $root 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.version_number -ne $version) { throw 'Manifest and DLL versions differ.' }
if ($manifest.name -ne 'Summon_Mastery' -or $manifest.description.Length -gt 250 -or !$manifest.dependencies.Count) {
    throw 'Invalid package manifest.'
}
$iconPath = Join-Path $root 'icon.png'
Add-Type -AssemblyName System.Drawing
$icon = [System.Drawing.Image]::FromFile($iconPath)
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256 -or $icon.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid) {
        throw 'Icon must be a 256x256 PNG.'
    }
} finally { $icon.Dispose() }
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null
$archive = Join-Path $dist "SummonMastery-$version.zip"
$files = @($dll, (Join-Path $root 'README.md'), (Join-Path $root 'CHANGELOG.md'),
    (Join-Path $root 'VERIFICATION.md'), (Join-Path $root 'LICENSE'), $manifestPath, $iconPath)
Compress-Archive -LiteralPath $files -DestinationPath $archive -Force
$zip = [System.IO.Compression.ZipFile]::OpenRead($archive)
try {
    $expected = @('SummonMastery.dll','README.md','CHANGELOG.md','VERIFICATION.md','LICENSE','manifest.json','icon.png')
    if (@(Compare-Object ($expected | Sort-Object) ($zip.Entries.FullName | Sort-Object)).Count -ne 0) {
        throw 'Unexpected archive contents.'
    }
    $entry = $zip.GetEntry('SummonMastery.dll')
    if ($null -eq $entry) { throw 'Archive DLL is missing.' }
    $stream = $entry.Open()
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = [Convert]::ToHexString($sha.ComputeHash($stream)) }
    finally { $stream.Dispose(); $sha.Dispose() }
    if ($hash -ne (Get-FileHash -LiteralPath $dll).Hash) { throw 'Packaged DLL hash differs from built DLL.' }
    $zip.Entries | ForEach-Object { $_.FullName }
} finally { $zip.Dispose() }
Write-Output "Verified package: $archive"
