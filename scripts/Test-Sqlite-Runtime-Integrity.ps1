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
$Branch = 'feat/yangspace-v2-foundation-20261108'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'

$Head = (& $Git -C $Repo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $Head -cne $CommitSha) {
    throw 'RUNTIME_TEST_COMMIT_SHA_MISMATCH'
}

$CurrentBranch = (& $Git -C $Repo branch --show-current).Trim()
if ($LASTEXITCODE -ne 0 -or $CurrentBranch -cne $Branch) {
    throw 'RUNTIME_TEST_BRANCH_MISMATCH'
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
if (-not (Test-Path -LiteralPath $RunLog -PathType Leaf) -or
    (Get-Content -LiteralPath $RunLog -Raw) -notmatch '(?m)^SQLITE_RUNTIME_INTEGRITY=PASS\s*$') {
    throw 'RUNTIME_INTEGRITY_MARKER_MISSING'
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
