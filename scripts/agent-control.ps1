[CmdletBinding()]
param(
    [ValidateSet('doctor','preview','create-agent','run','status','items','subagents','continue')]
    [string]$Command = 'doctor',
    [string]$TaskFile
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$mapping = Join-Path $projectRoot 'team-project.json'
if (Test-Path -LiteralPath $mapping) {
    $sourceRoot = (Get-Content -Raw -LiteralPath $mapping | ConvertFrom-Json).source_root
    if (-not [IO.Path]::IsPathRooted($sourceRoot)) { $sourceRoot = Join-Path $projectRoot $sourceRoot }
} else { $sourceRoot = $projectRoot }
$controller = Join-Path $sourceRoot 'agent-control'
$nodeCommand = Get-Command node -ErrorAction SilentlyContinue
$nodePath = if ($nodeCommand) { $nodeCommand.Source } else {
    Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
}
if (-not (Test-Path -LiteralPath $nodePath)) { throw 'Install Node.js 22 or newer, then run this command again.' }
$arguments = @((Join-Path $controller 'src\cli.mjs'), $Command)
if ($TaskFile) {
    $arguments += $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($TaskFile)
} elseif ($Command -in @('preview','run')) {
    $arguments += Join-Path $controller 'tasks\first-review.md'
}
& $nodePath @arguments
exit $LASTEXITCODE
