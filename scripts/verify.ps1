param(
    [ValidateSet('infrastructure','core','lifecycle','native-layout','native-auxiliary','native-renderer')]
    [string]$Stage = 'infrastructure',
    [string]$SourceRoot,
    [string]$Python = 'python',
    [switch]$DryRun,
    [switch]$AllowSourceTests,
    [switch]$AllowNative
)
$ErrorActionPreference = 'Stop'
$runnerArgs = @((Join-Path $PSScriptRoot 'verify_team.py'), '--stage', $Stage, '--python', $Python)
if ($SourceRoot) { $runnerArgs += @('--source-root', $SourceRoot) }
if ($DryRun) { $runnerArgs += '--dry-run' }
if ($AllowSourceTests) { $runnerArgs += '--allow-source-tests' }
if ($AllowNative) { $runnerArgs += '--allow-native' }
& $Python @runnerArgs
exit $LASTEXITCODE
