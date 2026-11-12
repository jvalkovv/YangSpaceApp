# Windows PowerShell 5.1; isolated reviewer Identity bearer authorization QA.
# SQLite TestServer only: NOT PostgreSQL HTTP, claim mutation or production approval.
# Never accesses PVE, CT164/CT152, live databases or deployment credentials.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2a3-reviewer-auth-qa-20261112'
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source

function GitValue {
    param([Parameter(Mandatory=$true)][string[]]$CommandArgs)
    $Output = @(& $Git -C $Repo @CommandArgs)
    if ($LASTEXITCODE -ne 0) { throw ('F2A3_REVIEWER_GIT_FAILED=' + ($CommandArgs -join ' ')) }
    return ($Output -join '').Trim()
}
if ((GitValue @('rev-parse','HEAD')) -cne $CommitSha) { throw 'F2A3_REVIEWER_SOURCE_SHA_MISMATCH' }
if ((GitValue @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'F2A3_REVIEWER_REMOTE_SHA_MISMATCH'
}
$CurrentBranch = GitValue @('branch','--show-current')
if ($CurrentBranch -and $CurrentBranch -cne $Branch) { throw 'F2A3_REVIEWER_BRANCH_MISMATCH' }
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) { throw 'F2A3_REVIEWER_WORKTREE_DIRTY' }
Write-Host 'F2A3_REVIEWER_SOURCE_IDENTITY=PASS'

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$Evidence = Join-Path $Repo ('logs\yangspace\f2a3-reviewer-auth\' + $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
function Invoke-Stage {
    param([string]$Label,[string[]]$CommandArguments)
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $CommandArguments -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 900
}

$Completed = $false
try {
    Invoke-Stage 'f2a3-reviewer-restore' @('restore',$Project,'-warnaserror')
    Invoke-Stage 'f2a3-reviewer-build' @('build',$Project,'-c','Release','--no-restore','-warnaserror')
    Invoke-Stage 'f2a3-reviewer-http' @('run','--project',$Project,'-c','Release','--no-build','--no-restore','--','--claim-reviewer-auth-http-only')

    $OutputPath = Join-Path $Evidence 'f2a3-reviewer-http.stdout.log'
    if (-not (Test-Path -LiteralPath $OutputPath -PathType Leaf)) { throw 'F2A3_REVIEWER_HTTP_STDOUT_MISSING' }
    $Output = Get-Content -LiteralPath $OutputPath -Raw
    foreach ($Marker in @(
        'CLAIM_REVIEW_HTTP_DEFAULT_OFF','CLAIM_REVIEW_AUTH_DEFAULT_OFF',
        'CLAIM_REVIEW_ENVIRONMENT_FAIL_CLOSED_PRODUCTION','CLAIM_REVIEW_ENVIRONMENT_FAIL_CLOSED_DEVELOPMENT',
        'CLAIM_REVIEW_DATABASE_PROVIDER_FAIL_CLOSED','CLAIM_REVIEW_REVIEWER_ROLE_ASSIGNED',
        'CLAIM_REVIEW_ACTIVE_MEMBER_CONFLICT','CLAIM_REVIEW_SELF_REVIEW_CONFLICT',
        'CLAIM_REVIEW_REVOKED_MEMBER_NOT_ACTIVE_CONFLICT','CLAIM_REVIEW_UNRELATED_REVIEWER_NO_CONFLICT',
        'CLAIM_REVIEW_UNKNOWN_CLAIM_FAIL_CLOSED',
        'CLAIM_REVIEW_EMPTY_REVIEWER_FAIL_CLOSED',
        'CLAIM_REVIEW_ANONYMOUS_401','CLAIM_REVIEW_INVALID_BEARER_401','CLAIM_REVIEW_COOKIE_ONLY_401',
        'CLAIM_REVIEW_DEDICATED_ROLE_200','CLAIM_REVIEW_PLATFORM_ADMIN_ONLY_403',
        'CLAIM_REVIEW_NON_REVIEWER_403','CLAIM_REVIEW_SAME_TOKEN_ROLE_REVOCATION_403',
        'CLAIM_REVIEW_REGISTRATION_NOT_EXPOSED','CLAIM_REVIEW_COOKIE_LOGIN_REJECTED',
        'CLAIM_REVIEW_DECISION_ROUTE_NOT_EXPOSED','CLAIM_REVIEW_NO_BUSINESS_SIDE_EFFECTS',
        'F2A3_REVIEWER_AUTHORIZATION_HTTP_CONTRACT','F2A3_REVIEWER_AUTH_HTTP_ISOLATED_ONLY'
    )) {
        if ($Output -notmatch ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) {
            throw ('F2A3_REVIEWER_HTTP_MARKER_MISSING=' + $Marker)
        }
    }
    $After = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $After.Count -ne 0) { throw 'F2A3_REVIEWER_TEST_CHANGED_SOURCE' }

    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]@(
        ('COMMIT_SHA=' + $CommitSha),
        'SOURCE_RESTORE_BUILD=PASS',
        'F2A3_REVIEWER_AUTHORIZATION_HTTP_ISOLATED_ONLY=PASS',
        'AUTHENTICATION=REAL_ASPNET_IDENTITY_BEARER_TESTSERVER',
        'ROLE_REVOCATION=SAME_TOKEN_LIVE_IDENTITY_STORE=PASS',
        'CONFLICT_POLICY=SELF_REVIEW_AND_ACTIVE_MEMBER=PASS',
        'TEST_DATABASE=DISPOSABLE_SQLITE',
        'CLAIM_MUTATION_ROUTE=NOT_EXPOSED',
        'EVIDENCE_CATEGORIES=NOT_APPROVED',
        'EVIDENCE_RETENTION=NOT_APPROVED',
        'POSTGRESQL_BEARER_HTTP=NOT_TESTED',
        'PARALLEL_CLAIM_WRITERS=NOT_TESTED',
        'UNCERTAIN_COMMIT=NOT_TESTED',
        'PRODUCTION=NOT_EXPOSED',
        'CT164_DB=NOT_ACCESSED',
        'CT152=NOT_ACCESSED',
        'RELEASE_GATE=HOLD'
    ))
    $Completed = $true
}
finally {
    if (-not $Completed) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),'F2A3_REVIEWER_AUTHORIZATION_HTTP=HOLD')
        Write-Warning ('F2A3_REVIEWER_AUTHORIZATION_HTTP=HOLD; EVIDENCE_DIR=' + $Evidence)
    }
}
Write-Host 'F2A3_REVIEWER_AUTHORIZATION_HTTP=PASS'
Write-Host ('COMMIT_SHA=' + $CommitSha)
Write-Host ('EVIDENCE_DIR=' + $Evidence)
Write-Host 'POSTGRESQL_REVIEWER_HTTP_PRODUCTION=NOT_TESTED'
