# F2b O3 exact-SHA worker, retry, lease, recovery and fake-transport QA.
# Uses shared in-memory SQLite only. No PostgreSQL, public HTTP, real SMTP, Docker or CT164.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[a-f0-9]{40}$')]
    [string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2b-o3-outbox-worker-20261115'
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source

function GitValue {
    param([string[]]$ArgsList)
    $lines = @(& $Git -C $Repo @ArgsList)
    if ($LASTEXITCODE -ne 0) { throw 'F2B_O3_GIT_FAILED' }
    return ($lines -join '').Trim()
}
function Require {
    param([bool]$Condition,[string]$Name)
    if (-not $Condition) { throw ('F2B_O3_PREFLIGHT_FAILED=' + $Name) }
    Write-Host ($Name + '=PASS')
}

& $Git -C $Repo fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'F2B_O3_FETCH_FAILED' }
if ((GitValue @('rev-parse','HEAD')) -cne $CommitSha) { throw 'F2B_O3_SOURCE_MISMATCH' }
if ((GitValue @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'F2B_O3_REMOTE_MISMATCH'
}
$Active = GitValue @('branch','--show-current')
if ($Active -and $Active -cne $Branch) { throw 'F2B_O3_WRONG_BRANCH' }
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) { throw 'F2B_O3_DIRTY_SOURCE' }
Require $true 'F2B_O3_EXACT_SOURCE'

$Config = Get-Content -LiteralPath (Join-Path $Repo 'apps\api\appsettings.json') -Raw | ConvertFrom-Json
Require ($Config.PublicAccounts.Enabled -eq $false) 'PUBLIC_ACCOUNTS_DEFAULT_OFF'
Require ($Config.PublicAccounts.Outbox.DispatchEnabled -eq $false) 'OUTBOX_DISPATCH_DEFAULT_OFF'
Require ($Config.ClaimReviewPostgreSqlQa.Enabled -eq $false) 'POSTGRES_REVIEW_QA_DEFAULT_OFF'

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + ([guid]::NewGuid().ToString('N').Substring(0,8))
$Evidence = Join-Path $Repo ('logs\yangspace\f2b-o3-worker\' + $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')

function Stage {
    param([string]$Label,[string[]]$CommandArguments)
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $CommandArguments -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 1200
}

$Passed = $false
try {
    Stage 'o3-restore' @('restore',$Project,'-warnaserror')
    Stage 'o3-build' @('build',$Project,'-c','Release','--no-restore','-warnaserror')
    Stage 'o3-worker-synthetic' @('run','--project',$Project,'-c','Release','--no-restore','--no-build','--','--f2b-outbox-worker-synthetic-only')

    foreach ($Label in @('o3-restore','o3-build','o3-worker-synthetic')) {
        foreach ($Suffix in @('.runner.cmd','.stdout.log','.stderr.log')) {
            Require (Test-Path -LiteralPath (Join-Path $Evidence ($Label+$Suffix)) -PathType Leaf) ('STAGE_RECEIPT_' + $Label + $Suffix)
        }
    }

    $Log = [IO.File]::ReadAllText((Join-Path $Evidence 'o3-worker-synthetic.stdout.log'))
    foreach ($Marker in @(
        'F2B_O3_WORKER_CONFIRMATION_DISPATCH',
        'F2B_O3_WORKER_RESET_DISPATCH',
        'F2B_O3_WORKER_RECHECKS_CURRENT_EMAIL',
        'F2B_O3_WORKER_RECHECKS_CONFIRMATION_AND_STAFF_STATE',
        'F2B_O3_WORKER_EXPIRED_INTENT_SUPPRESSION',
        'F2B_O3_WORKER_RETRY_BACKOFF',
        'F2B_O3_WORKER_RETRY_RECOVERY',
        'F2B_O3_WORKER_UNCERTAIN_ACK_AT_LEAST_ONCE',
        'F2B_O3_WORKER_CONCURRENT_CLAIM',
        'F2B_O3_WORKER_NULL_LEASE_RECOVERY',
        'F2B_O3_WORKER_CRASH_LEAVES_EXPIRING_LEASE',
        'F2B_O3_WORKER_LEASE_EXPIRY_RECOVERY',
        'F2B_O3_WORKER_DEADLETTER',
        'F2B_O3_WORKER_OPERATOR_REPLAY',
        'F2B_O3_WORKER_RETENTION_PURGE',
        'F2B_O3_NO_TOKEN_OR_RECIPIENT_AT_REST',
        'F2B_O3_WORKER_FAKE_TRANSPORT_ONLY',
        'F2B_O3_WORKER_SQLITE_SYNTHETIC_ONLY'
    )) {
        Require ($Log -match ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) $Marker
    }

    $After = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
    Require ($LASTEXITCODE -eq 0 -and $After.Count -eq 0) 'CLEAN_WORKTREE_AFTER_TEST'

    $Manifest = @(
        "COMMIT_SHA=$CommitSha"
        'F2B_O3_EXACT_SOURCE=PASS'
        'F2B_O3_RELEASE_BUILD=PASS'
        'F2B_O3_WORKER_SQLITE_SYNTHETIC=PASS'
        'F2B_O3_POSTGRES_WORKER=NOT_RUN'
        'F2B_O3_REAL_SMTP=NOT_CONNECTED'
        'F2B_O3_IDENTITY_HTTP=NOT_TESTED'
        'PUBLIC_REGISTRATION_DEFAULT_OFF=PASS'
        'OUTBOX_DISPATCH_DEFAULT_OFF=PASS'
        'CT164_DB=NOT_ACCESSED'
        'GITHUB_ACTIONS=NOT_USED'
        'RELEASE_GATE=HOLD'
    )
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'), [string[]]$Manifest)
    $Passed = $true
}
finally {
    if (-not $Passed) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'), "F2B_O3_WORKER=HOLD; COMMIT_SHA=$CommitSha")
    }
}

Write-Host 'F2B_O3_WORKER_SOURCE_RUNTIME=PASS'
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "EVIDENCE_DIR=$Evidence"
Write-Host ('MANIFEST_SHA256=' + (Get-FileHash (Join-Path $Evidence 'manifest.txt') -Algorithm SHA256).Hash.ToLowerInvariant())
