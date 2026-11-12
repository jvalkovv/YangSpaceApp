# Windows PowerShell 5.1; isolated synthetic SQLite transaction preflight.
# NOT claim-review HTTP, real PostgreSQL, production role or approval.
# Never opens CT164/CT152, uses app settings/credentials or deploys.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2a3-claim-tx-qa-20261112'
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source

function GitValue {
    param([Parameter(Mandatory=$true)][string[]]$CommandArgs)
    $Output = @(& $Git -C $Repo @CommandArgs)
    if ($LASTEXITCODE -ne 0) {
        throw ('F2A3_GIT_FAILED=' + ($CommandArgs -join ' '))
    }
    return ($Output -join '').Trim()
}

if ((GitValue -CommandArgs @('rev-parse','HEAD')) -cne $CommitSha) {
    throw 'F2A3_SOURCE_SHA_MISMATCH'
}
if ((GitValue -CommandArgs @(
    'rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'F2A3_REMOTE_TIP_SHA_MISMATCH'
}
$CurrentBranch = GitValue -CommandArgs @('branch','--show-current')
if ($CurrentBranch -and $CurrentBranch -cne $Branch) {
    throw 'F2A3_WORKTREE_BRANCH_MISMATCH'
}
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all |
    Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) {
    throw 'F2A3_QA_WORKTREE_NOT_CLEAN'
}
Write-Host 'F2A3_SOURCE_IDENTITY=PASS'

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' +
    [Guid]::NewGuid().ToString('N').Substring(0,8)
$Evidence = Join-Path $Repo ('logs\yangspace\f2a3-claim-tx\' +
    $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')

function Invoke-Stage {
    param([string]$Label,[string[]]$CommandArguments)
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet `
        -CommandArguments $CommandArguments -WorkingDirectory $Repo `
        -EvidenceDirectory $Evidence -TimeoutSeconds 900
}
$Completed = $false
try {
    Invoke-Stage -Label 'f2a3-restore' -CommandArguments @(
        'restore',$Project,'-warnaserror'
    )
    Invoke-Stage -Label 'f2a3-build' -CommandArguments @(
        'build',$Project,'-c','Release','--no-restore','-warnaserror'
    )
    Invoke-Stage -Label 'f2a3-transaction' -CommandArguments @(
        'run','--project',$Project,'-c','Release','--no-build',
        '--no-restore','--','--claim-transaction-only'
    )

    $Stdout = Join-Path $Evidence 'f2a3-transaction.stdout.log'
    if (-not (Test-Path -LiteralPath $Stdout -PathType Leaf)) {
        throw 'F2A3_TRANSACTION_STDOUT_MISSING'
    }
    $Output = Get-Content -LiteralPath $Stdout -Raw

    foreach ($Marker in @(
        'CLAIM_TX_DISPOSABLE_SQLITE'
        'CLAIM_TX_APPROVAL_PERSISTED_ATOMICALLY'
        'CLAIM_TX_AUDIT_CARDINALITY'
        'CLAIM_TX_DUPLICATE_AUDIT_FORCED_FAILURE'
        'CLAIM_TX_FAILURE_ROLLBACK_ALL_ROWS'
        'CLAIM_TX_NO_HTTP_ROUTE_OR_REAL_REVIEWER'
        'F2A3_CLAIM_TX_ISOLATED_ONLY'
    )) {
        $Pattern = '(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$'
        if ($Output -notmatch $Pattern) {
            throw ('F2A3_CONTRACT_MARKER_MISSING=' + $Marker)
        }
    }

    $After = @(& $Git -C $Repo status --porcelain --untracked-files=all |
        Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $After.Count -ne 0) {
        throw 'F2A3_TEST_CHANGED_SOURCE'
    }

    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]@(
        ('COMMIT_SHA=' + $CommitSha),
        'SOURCE_RESTORE_BUILD=PASS',
        'F2A3_CLAIM_TX_ISOLATED_ONLY=PASS',
        'TEST_PROVIDER=SQLITE_DISPOSABLE_MEMORY',
        'REAL_IDENTITY_REVIEWER_HTTP=NOT_TESTED',
        'REAL_POSTGRESQL_APP_TRANSACTION=NOT_TESTED',
        'PARALLEL_WRITERS=NOT_TESTED',
        'CLAIM_REVIEW_POLICY=NOT_APPROVED',
        'CLAIM_MUTATION_ROUTE=NOT_EXPOSED',
        'CT164_DB=NOT_ACCESSED',
        'CT152=NOT_ACCESSED',
        'PRODUCTION=NOT_EXPOSED',
        'RELEASE_GATE=HOLD'
    ))
    $Completed = $true
}
finally {
    if (-not $Completed) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),
            'F2A3_CLAIM_TX_ISOLATED_ONLY=HOLD')
        Write-Warning ('F2A3_CLAIM_TX_ISOLATED_ONLY=HOLD; EVIDENCE_DIR=' + $Evidence)
    }
}
Write-Host 'F2A3_CLAIM_TX_ISOLATED_ONLY=PASS'
Write-Host ('COMMIT_SHA=' + $CommitSha)
Write-Host ('EVIDENCE_DIR=' + $Evidence)
Write-Host 'POSTGRESQL_REVIEWER_HTTP_PRODUCTION=NOT_TESTED'
