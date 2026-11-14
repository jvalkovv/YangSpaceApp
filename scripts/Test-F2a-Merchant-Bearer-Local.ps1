# Isolated real Identity bearer TestServer plus disposable SQLite model.
# Windows PowerShell 5.1. Does not access PVE, CT164/CT152 or any live DB.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2a2-bearer-read-qa-20261112'
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source

$Head = (@(& $Git -C $Repo rev-parse HEAD) -join '').Trim()
if ($LASTEXITCODE -ne 0 -or $Head -cne $CommitSha) {
    throw 'F2A2_SOURCE_SHA_MISMATCH'
}
$Remote = (@(& $Git -C $Repo rev-parse --verify ('refs/remotes/origin/' + $Branch)) -join '').Trim()
if ($LASTEXITCODE -ne 0 -or $Remote -cne $CommitSha) {
    throw 'F2A2_REMOTE_SHA_MISMATCH'
}
$CurrentBranch = (@(& $Git -C $Repo branch --show-current) -join '').Trim()
if ($LASTEXITCODE -ne 0 -or ($CurrentBranch -and $CurrentBranch -cne $Branch)) {
    throw 'F2A2_BRANCH_MISMATCH'
}
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) {
    throw 'F2A2_WORKTREE_DIRTY'
}

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$Evidence = Join-Path $Repo ('logs\yangspace\f2a2-merchant-http\' + $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Path $Evidence -Force | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')

function Stage {
    param([string]$Label, [string[]]$CommandArgs)
    $Settings = @{
        Label=$Label
        Executable=$Dotnet
        CommandArguments=$CommandArgs
        WorkingDirectory=$Repo
        EvidenceDirectory=$Evidence
        TimeoutSeconds=900
    }
    Invoke-YangSpaceStage @Settings
}

$Completed = $false
try {
    Stage -Label 'f2a2-restore' -CommandArgs @('restore',$Project,'-warnaserror')
    Stage -Label 'f2a2-build' -CommandArgs @('build',$Project,'-c','Release','--no-restore','-warnaserror')
    Stage -Label 'f2a2-tenant-regression' -CommandArgs @(
        'run','--project',$Project,'-c','Release','--no-build','--no-restore','--','--tenant-scope-only'
    )
    Stage -Label 'f2a2-bearer-http' -CommandArgs @(
        'run','--project',$Project,'-c','Release','--no-build','--no-restore','--','--merchant-bearer-http-only'
    )

    $Parent = [IO.File]::ReadAllText((Join-Path $Evidence 'f2a2-tenant-regression.stdout.log'))
    if ($Parent -notmatch '(?m)^F2A_TENANT_SCOPE_ISOLATED_ONLY=PASS\s*$') {
        throw 'F2A2_PARENT_TENANT_REGRESSION_FAILED'
    }
    $Result = [IO.File]::ReadAllText((Join-Path $Evidence 'f2a2-bearer-http.stdout.log'))
    foreach ($Marker in @(
        'MERCHANT_HTTP_DEFAULT_OFF'
        'MERCHANT_HTTP_AUTH_DEFAULT_OFF'
        'MERCHANT_HTTP_ENVIRONMENT_FAIL_CLOSED_PRODUCTION'
        'MERCHANT_HTTP_ENVIRONMENT_FAIL_CLOSED_DEVELOPMENT'
        'MERCHANT_HTTP_SYNTHETIC_DB_SEEDED'
        'MERCHANT_HTTP_ISSUED_REAL_BEARER'
        'MERCHANT_HTTP_ANONYMOUS_401'
        'MERCHANT_HTTP_INVALID_BEARER_401'
        'MERCHANT_HTTP_COOKIE_ONLY_401'
        'MERCHANT_HTTP_COOKIE_LOGIN_REJECTED'
        'MERCHANT_HTTP_OWNER_A_200'
        'MERCHANT_HTTP_EDITOR_B_200'
        'MERCHANT_HTTP_CROSS_TENANT_404'
        'MERCHANT_HTTP_UNKNOWN_UNCLAIMED_SUSPENDED_404'
        'MERCHANT_HTTP_FORGED_ACTOR_DENIED'
        'MERCHANT_HTTP_PLATFORM_ADMIN_NOT_OWNER'
        'MERCHANT_HTTP_REGISTRATION_NOT_EXPOSED'
        'MERCHANT_HTTP_NO_MUTATION_ROUTE'
        'MERCHANT_HTTP_PRIVATE_DTO_ALLOWLIST'
        'MERCHANT_HTTP_SAME_TOKEN_REVOKED_IMMEDIATELY'
        'MERCHANT_HTTP_SAME_TOKEN_ORGANIZATION_SUSPENDED'
        'MERCHANT_HTTP_NO_BUSINESS_SIDE_EFFECTS'
        'F2A_MERCHANT_BEARER_HTTP_CONTRACT'
        'F2A_MERCHANT_BEARER_HTTP_ISOLATED_ONLY'
    )) {
        if ($Result -notmatch ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) {
            throw ('F2A2_MISSING_HTTP_MARKER=' + $Marker)
        }
    }
    $DirtyAfter = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $DirtyAfter.Count -ne 0) {
        throw 'F2A2_TEST_MUTATED_SOURCE'
    }

    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]@(
        ('COMMIT_SHA=' + $CommitSha),
        'SOURCE_RESTORE_BUILD=PASS',
        'F2A_TENANT_SCOPE_ISOLATED_ONLY=PASS',
        'F2A_MERCHANT_BEARER_HTTP_ISOLATED_ONLY=PASS',
        'AUTHENTICATION=REAL_ASPNET_IDENTITY_BEARER_TESTSERVER',
        'MERCHANT_FEATURE=QA_ONLY_DEFAULT_OFF',
        'TEST_DB=DISPOSABLE_SQLITE_ENSURE_CREATED',
        'LIVE_POSTGRESQL_BEARER_HTTP=NOT_TESTED',
        'APP_AUDIT_TRANSACTION=NOT_TESTED',
        'PRODUCTION=NOT_EXPOSED',
        'CT164_DB=NOT_ACCESSED',
        'RELEASE_GATE=HOLD'
    ))
    $Completed = $true
}
finally {
    if (-not $Completed) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),'F2A2_MERCHANT_BEARER_HTTP=HOLD')
        Write-Warning ('F2A2_MERCHANT_BEARER_HTTP=HOLD; EVIDENCE_DIR=' + $Evidence)
    }
}
Write-Host 'F2A2_MERCHANT_BEARER_HTTP=PASS'
Write-Host ('COMMIT_SHA=' + $CommitSha)
Write-Host ('EVIDENCE_DIR=' + $Evidence)
Write-Host 'LIVE_PG_REVIEWER_AUDIT_PRODUCTION=NOT_TESTED'
