# Run the disposable SQLite runtime-integrity regression harness on an exact commit.
# It never reads application connection settings, starts an HTTP listener or accesses PVE.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$AllowedBranches = @('feat/yangspace-v2-foundation-20261108',
    'feat/yangspace-publication-timestamps-f1b-20261111')
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'

$Head = (& $Git -C $Repo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $Head -cne $CommitSha) {
    throw 'RUNTIME_TEST_COMMIT_SHA_MISMATCH'
}

$CurrentBranch = (& $Git -C $Repo branch --show-current).Trim()
if ($LASTEXITCODE -ne 0 -or ($CurrentBranch -and $AllowedBranches -cnotcontains $CurrentBranch)) {
    throw 'RUNTIME_TEST_BRANCH_MISMATCH'
}
# For detached QA checkouts, require this exact source SHA to be the
# fetched remote tip of an allowlisted branch. No arbitrary local history.
$MatchesRemote = $false
foreach ($Branch in $AllowedBranches) {
    $RemoteSha = (& $Git -C $Repo rev-parse --verify
        ('refs/remotes/origin/' + $Branch) 2>$null)
    if ($LASTEXITCODE -eq 0 -and $RemoteSha -and
        $RemoteSha.Trim() -ceq $CommitSha) {
        $MatchesRemote = $true
        break
    }
}
if (-not $MatchesRemote) {
    throw 'RUNTIME_TEST_REMOTE_SHA_NOT_PINNED'
}

$Status = @(& $Git -C $Repo status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $Status.Count -ne 0) {
    throw 'RUNTIME_TEST_WORKTREE_NOT_CLEAN'
}

$Evidence = Join-Path $Repo (
    'logs\yangspace\sqlite-runtime-integrity\' +
    $CommitSha.Substring(0, 12) + '-' +
    (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' +
    [Guid]::NewGuid().ToString('N').Substring(0, 8)
)
New-Item -ItemType Directory -Path $Evidence -Force | Out-Null

. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')

function Invoke-Stage {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string[]]$CommandArguments
    )

    $Stage = @{
        Label = $Label
        Executable = $Dotnet
        CommandArguments = $CommandArguments
        WorkingDirectory = $Repo
        EvidenceDirectory = $Evidence
        TimeoutSeconds = 600
    }
    Invoke-YangSpaceStage @Stage
}

Invoke-Stage 'runtime-test-restore' @('restore', $Project, '-warnaserror')
Invoke-Stage 'runtime-test-build' @('build', $Project, '-c', 'Release', '--no-restore', '-warnaserror')
Invoke-Stage 'runtime-test-run' @('run', '--project', $Project, '-c', 'Release', '--no-build', '--no-restore')

$RunLog = Join-Path $Evidence 'runtime-test-run.stdout.log'
if (-not (Test-Path -LiteralPath $RunLog -PathType Leaf)) {
    throw 'RUNTIME_INTEGRITY_LOG_MISSING'
}

$RunText = Get-Content -LiteralPath $RunLog -Raw
foreach ($Marker in @(
    'SQLITE_RUNTIME_INTEGRITY'
    'RECOVERY_REHEARSAL'
    'RECOVERED_SCHEMA_GATE'
    'RECOVERED_SQLITE_INTEGRITY'
    'RECOVERED_FOREIGN_KEY_CHECK'
    'RECOVERED_MIGRATION_HISTORY'
    'RECOVERED_PRE_BACKUP_ROW'
    'F1B_LEGACY_SCHEMA_CREATED'
    'F1B_LEGACY_ROWS_PRESERVED'
    'F1B_LEGACY_TIMESTAMPS_NULL'
    'F1B_MIGRATION_HISTORY'
    'F1B_UPGRADE_INTEGRITY'
    'F1B_PREUPGRADE_BACKUP_RESTORED'
    'F1B_UPGRADE_REHEARSAL'
    'RECOVERY_POINT_IN_TIME'
)) {
    if ($RunText -notmatch ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) {
        throw "RUNTIME_ACCEPTANCE_MARKER_MISSING=$Marker"
    }
}

$Manifest = @(
    "COMMIT_SHA=$CommitSha"
    'RUNTIME_TEST_RESTORE=PASS'
    'RUNTIME_TEST_BUILD=PASS'
    'MIGRATION_APPLY=DISPOSABLE_SQLITE_ONLY'
    'FOREIGN_KEYS=PASS'
    'ORPHAN_REJECTION=PASS'
    'CASCADE_DELETE=PASS'
    'PERSISTENCE_REOPEN=PASS'
    'DISABLED_FK_GATE=PASS'
    'LEGACY_ORPHAN_DETECTION=PASS'
    'ONLINE_BACKUP=PASS'
    'RECOVERY_REHEARSAL=PASS'
    'F1B_INITIAL_TO_LATEST_DISPOSABLE_UPGRADE=PASS'
    'F1B_PRE_UPGRADE_BACKUP_RESTORABLE=PASS'
    'RECOVERED_INTEGRITY_CHECK=PASS'
    'RECOVERED_SCHEMA_GATE=PASS'
    'RECOVERY_POINT_IN_TIME=PASS'
    'PRODUCTION_DATABASE=NOT_ACCESSED'
    'HTTP_LISTENER=NOT_STARTED'
    'PVE_DEPLOYMENT=NONE'
)
[IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'), [string[]]$Manifest)

$FinalStatus = @(& $Git -C $Repo status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $FinalStatus.Count -ne 0) {
    throw 'RUNTIME_TEST_CREATED_TRACKED_OR_UNIGNORED_FILES'
}

Write-Host ''
Write-Host 'SQLITE_RUNTIME_INTEGRITY=PASS'
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "EVIDENCE_DIR=$Evidence"
