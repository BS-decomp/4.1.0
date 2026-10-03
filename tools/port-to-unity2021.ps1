[CmdletBinding(SupportsShouldProcess=$true)]
param(
  [Parameter(Mandatory=$true)][string]$ProjectPath,
  [string]$BackupPath = ""
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $ProjectPath).Path
if (!(Test-Path (Join-Path $root 'ProjectSettings/ProjectVersion.txt'))) { throw "Not a Unity project: $root" }
if ([string]::IsNullOrWhiteSpace($BackupPath)) { $BackupPath = "$root-unity4-baseline" }
if (Test-Path $BackupPath) { throw "Backup already exists: $BackupPath" }
Write-Host "This script only creates a rollback copy and reports the required manual migration."
Write-Host "It deliberately does not rewrite ProjectVersion.txt or assets automatically."
if ($PSCmdlet.ShouldProcess($BackupPath, 'Create baseline copy')) {
  New-Item -ItemType Directory -Force -Path $BackupPath | Out-Null
  Copy-Item -LiteralPath (Join-Path $root 'Assets') -Destination $BackupPath -Recurse
  Copy-Item -LiteralPath (Join-Path $root 'Packages') -Destination $BackupPath -Recurse
  Copy-Item -LiteralPath (Join-Path $root 'ProjectSettings') -Destination $BackupPath -Recurse
}
Write-Host "Baseline saved to $BackupPath"
Write-Host "Next: duplicate the project, then open the duplicate in Unity 2021.3.45f2 and capture Editor.log."
