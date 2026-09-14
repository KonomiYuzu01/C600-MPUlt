[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$SourceRoot,
    [Parameter(Mandatory=$true)][string]$Destination,
    [Parameter(Mandatory=$true)][string]$BaseRef
)
$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path -LiteralPath $SourceRoot).Path
$targetPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destination)
if (Test-Path -LiteralPath $targetPath) { throw 'Destination already exists; choose a new worktree directory.' }
$status = & git -C $sourcePath status --porcelain --untracked-files=all
if ($LASTEXITCODE -ne 0) { throw 'Source is not an accessible Git repository.' }
if ($status) { throw 'Source has uncommitted changes. Prepare an agreed baseline without discarding current work.' }
$commit = & git -C $sourcePath rev-parse --verify --end-of-options ($BaseRef + '^{commit}')
if ($LASTEXITCODE -ne 0) { throw 'BaseRef must resolve to an existing commit.' }
Write-Warning 'Ignored experimental files are not included. Confirm this commit is the task baseline before assigning agents.'
& git -C $sourcePath worktree add --detach -- $targetPath $commit
if ($LASTEXITCODE -ne 0) { throw 'Git worktree creation failed.' }
Write-Output ('Created isolated worktree at ' + $targetPath + ' from ' + $commit)
