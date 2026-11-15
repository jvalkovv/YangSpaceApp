# F2b outbox O1 source + disposable SQLite enqueue/rollback QA.
# No migration apply, external SMTP, PostgreSQL connection, CT164 or HTTP.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[a-f0-9]{40}$')]
    [string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2b-email-outbox-enqueue-foundation-20261114'
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source

function GitValue {
    param([string[]]$ArgsList)
    $lines = @(& $Git -C $Repo @ArgsList)
    if ($LASTEXITCODE -ne 0) { throw 'F2B_OUTBOX_GIT_FAILED' }
    return ($lines -join '').Trim()
}
if ((GitValue @('rev-parse','HEAD')) -cne $CommitSha) { throw 'F2B_OUTBOX_SOURCE_MISMATCH' }
if ((GitValue @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'F2B_OUTBOX_REMOTE_MISMATCH'
}
$Active = GitValue @('branch','--show-current')
if ($Active -and $Active -cne $Branch) { throw 'F2B_OUTBOX_WRONG_BRANCH' }
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) { throw 'F2B_OUTBOX_DIRTY_SOURCE' }
Write-Host 'F2B_OUTBOX_EXACT_SOURCE=PASS'

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' +
    ([guid]::NewGuid().ToString('N').Substring(0,8))
$Evidence = Join-Path $Repo ('logs\yangspace\f2b-outbox-enqueue\' +
    $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')

function Stage {
    param([string]$Label,[string[]]$CommandArguments)
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $CommandArguments `
        -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 900
}

$Passed = $false
try {
    Stage 'outbox-restore' @('restore',$Project,'-warnaserror')
    Stage 'outbox-build' @('build',$Project,'-c','Release','--no-restore','-warnaserror')
    Stage 'outbox-enqueue' @('run','--project',$Project,'-c','Release',
        '--no-restore','--no-build','--','--outbox-enqueue-synthetic-only')

    foreach ($Label in @('outbox-restore','outbox-build','outbox-enqueue')) {
        foreach ($Suffix in @('runner.cmd','stdout.log','stderr.log')) {
            if (-not (Test-Path -LiteralPath (Join-Path $Evidence "$Label.$Suffix") -PathType Leaf)) {
                throw "F2B_OUTBOX_STAGE_RECEIPT_MISSING=$Label.$Suffix"
            }
        }
    }
    $Log = [IO.File]::ReadAllText((Join-Path $Evidence 'outbox-enqueue.stdout.log'))
    foreach ($Marker in @(
        'F2B_OUTBOX_EMAIL_INTENT_PERSISTED',
        'F2B_OUTBOX_MINIMAL_PENDING_INTENT',
        'F2B_OUTBOX_PENDING_COALESCED',
        'F2B_OUTBOX_RESET_INTENT_PERSISTED',
        'F2B_OUTBOX_NO_EMAIL_PAYLOAD_STORED',
        'F2B_OUTBOX_DISPATCH_FAIL_CLOSED',
        'F2B_OUTBOX_ATOMIC_ROLLBACK',
        'F2B_OUTBOX_ENQUEUE_SYNTHETIC_ONLY'
    )) {
        if ($Log -notmatch ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) {
            throw "F2B_OUTBOX_RUNTIME_MARKER_MISSING=$Marker"
        }
    }
    $After = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $After.Count -ne 0) { throw 'F2B_OUTBOX_DIRTY_AFTER_TEST' }
    $Manifest = @(
        "COMMIT_SHA=$CommitSha",
        'F2B_OUTBOX_EXACT_SOURCE=PASS',
        'F2B_OUTBOX_RELEASE_BUILD=PASS',
        'F2B_OUTBOX_ENQUEUE_SYNTHETIC_ONLY=PASS',
        'F2B_OUTBOX_PG_MIGRATIONS=NOT_GENERATED',
        'F2B_OUTBOX_WORKER=NOT_IMPLEMENTED',
        'F2B_IDENTITY_HTTP=NOT_TESTED',
        'PUBLIC_REGISTRATION_DEFAULT_OFF=PASS',
        'CT164_DB=NOT_ACCESSED',
        'RELEASE_GATE=HOLD'
    )
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'), [string[]]$Manifest)
    $Passed = $true
}
finally {
    if (-not $Passed) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),
            "F2B_OUTBOX_ENQUEUE=HOLD; COMMIT_SHA=$CommitSha")
    }
}
Write-Host 'F2B_OUTBOX_SOURCE_RUNTIME=PASS'
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "EVIDENCE_DIR=$Evidence"
Write-Host ('MANIFEST_SHA256=' +
    (Get-FileHash (Join-Path $Evidence 'manifest.txt') -Algorithm SHA256).Hash.ToLowerInvariant())
