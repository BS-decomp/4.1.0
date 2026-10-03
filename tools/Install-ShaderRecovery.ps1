param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $ProjectPath).Path

if (Test-Path -LiteralPath (Join-Path $root 'Temp/UnityLockfile')) {
    throw 'Close Unity first.'
}
if (!(Test-Path -LiteralPath (Join-Path $root 'Assets/Scripts/Assembly-CSharp/LevelManager.cs'))) {
    throw 'Not the expected Block Strike Unity project.'
}

$source = Join-Path $PSScriptRoot 'unity-editor/ShaderRecovery'
if (!(Test-Path -LiteralPath $source)) { throw 'Missing ShaderRecovery folder; update the repository.' }

$shaders = @(Get-ChildItem -LiteralPath $source -Filter '*.shader' -File -Recurse)
if ($shaders.Count -ne 35) { throw "Expected 35 recovered shaders, found $($shaders.Count)." }

$backup = Join-Path $root ('RecoveryBackups/ShaderInstaller-' + [guid]::NewGuid().ToString('N'))
$replaced = 0
$already = 0

foreach ($file in $shaders) {
    $relative = $file.FullName.Substring($source.Length).TrimStart('\', '/')
    $target = Join-Path $root $relative
    if (!(Test-Path -LiteralPath $target)) {
        throw "Missing placeholder in the project: $relative"
    }
    $current = Get-Content -LiteralPath $target -Raw
    if ($current.Contains('DummyShaderTextExporter')) {
        New-Item -ItemType Directory -Force -Path $backup | Out-Null
        $backupTarget = Join-Path $backup $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupTarget) | Out-Null
        Copy-Item -LiteralPath $target -Destination $backupTarget -Force
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
        $replaced++
    } else {
        $already++
    }
    $result = Get-Content -LiteralPath $target -Raw
    if ($result.Contains('DummyShaderTextExporter')) {
        throw "Recovery did not stick: $relative"
    }
    if ($result.Contains('Shader "') -eq $false) {
        throw "Recovered shader looks wrong: $relative"
    }
}

Write-Host "Shader recovery complete: $replaced placeholders replaced, $already already recovered."
Write-Host 'Material-to-shader links were preserved (all .meta files untouched, GUIDs unchanged).'
Write-Host "Backups of the replaced placeholders: $backup"
Write-Host 'Open Unity and wait for the shader import to finish, then check any map.'
