# F2a-1 source contract: in-memory tenant scoping without an EF migration.
# NEVER touches CT164, external DBs, app connection config or a public endpoint.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2a1-domain-foundation-20261111'
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source

$HeadOutput = @(& $Git -C $Repo rev-parse HEAD)
$HeadExit = $LASTEXITCODE
$Head = ($HeadOutput -join '').Trim()
if ($HeadExit -ne 0 -or $Head -cne $CommitSha) {
    throw 'F2A_SOURCE_SHA_MISMATCH'
}

$RemoteOutput = @(& $Git -C $Repo rev-parse --verify --quiet ('refs/remotes/origin/' + $Branch))
$RemoteExit = $LASTEXITCODE
$Remote = ($RemoteOutput -join '').Trim()
if ($RemoteExit -ne 0 -or $Remote -cne $CommitSha) {
    throw 'F2A_REMOTE_TIP_SHA_MISMATCH'
}

$CurrentOutput = @(& $Git -C $Repo branch --show-current)
$CurrentExit = $LASTEXITCODE
$CurrentBranch = ($CurrentOutput -join '').Trim()
if ($CurrentExit -ne 0 -or ($CurrentBranch -and $CurrentBranch -cne $Branch)) {
    throw 'F2A_WORKTREE_BRANCH_MISMATCH'
}

$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) {
    throw 'F2A_WORKTREE_NOT_CLEAN'
}

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' +
    [Guid]::NewGuid().ToString('N').Substring(0, 8)
$Evidence = Join-Path $Repo (
    'logs\yangspace\f2a-tenant-scope\' + $CommitSha.Substring(0, 12) + '-' + $RunId
)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null

. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')

function Invoke-Stage {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string[]]$CommandArguments
    )
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $CommandArguments -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 600
}

$Success = $false
try {
    Invoke-Stage 'f2a-restore' @('restore', $Project, '-warnaserror')
    Invoke-Stage 'f2a-build' @('build', $Project, '-c', 'Release', '--no-restore', '-warnaserror')
    Invoke-Stage 'f2a-tenant-scope' @('run', '--project', $Project, '-c', 'Release',
        '--no-build', '--no-restore', '--', '--tenant-scope-only')

    $Stdout = Join-Path $Evidence 'f2a-tenant-scope.stdout.log'
    if (-not (Test-Path -LiteralPath $Stdout -PathType Leaf)) {
        throw 'F2A_RUN_STDOUT_MISSING'
    }
    $Result = Get-Content -LiteralPath $Stdout -Raw
    foreach ($Marker in @(
        'F2A_OWNER_OWN_LOCATION_ALLOWED'
        'F2A_EDITOR_OWN_LOCATION_ALLOWED'
        'F2A_CROSS_TENANT_LOCATION_DENIED'
        'F2A_PENDING_CLAIM_NO_GRANT'
        'F2A_REVOKED_MEMBERSHIP_DENIED'
        'F2A_SUSPENDED_ORGANIZATION_DENIED'
        'F2A_UNCLAIMED_LEGACY_DENIED'
        'F2A_PLATFORM_ADMIN_NOT_IMPLICIT_OWNER'
        'F2A_ANONYMOUS_AND_UNAUTHENTICATED_DENIED'
        'F2A_MEMBERSHIP_REVOCATION_IMMEDIATE'
        'F2A_TENANT_SCOPE_CONTRACT'
        'F2A_TENANT_SCOPE_ISOLATED_ONLY'
    )) {
        if ($Result -notmatch ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) {
            throw ('F2A_CONTRACT_MARKER_MISSING=' + $Marker)
        }
    }

    $After = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $After.Count -ne 0) {
        throw 'F2A_TEST_CHANGED_WORKTREE'
    }
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'), [string[]]@(
        'COMMIT_SHA=' + $CommitSha
        'F2A_TENANT_SCOPE_ISOLATED_ONLY=PASS'
        'SOURCE_RESTORE_BUILD=PASS'
        'DATABASE=DISPOSABLE_SQLITE_MEMORY_ONLY'
        'PG_MIGRATION=NOT_GENERATED'
        'API_BEARER_AUTHORIZATION=NOT_TESTED'
        'CT164_DB=NOT_ACCESSED'
        'PVE_DEPLOYMENT=NONE'
        'RELEASE_GATE=HOLD'
    ))
    $Success = $true
    Write-Host 'F2A_TENANT_SCOPE_ISOLATED_ONLY=PASS'
    Write-Host ('COMMIT_SHA=' + $CommitSha)
    Write-Host ('EVIDENCE_DIR=' + $Evidence)
}
finally {
    if (-not $Success) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'), 'F2A_TENANT_SCOPE_ISOLATED_ONLY=HOLD')
        Write-Warning ('F2A_TENANT_SCOPE_ISOLATED_ONLY=HOLD; EVIDENCE_DIR=' + $Evidence)
    }
}
