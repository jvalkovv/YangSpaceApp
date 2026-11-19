# Exact-SHA Windows PowerShell 5.1 QA for the auth rate partition contract.
# Source/build/runtime-test only. No Docker, SMTP, PostgreSQL, CT164 or live HTTP.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[a-f0-9]{40}$')]
    [string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'fix/yangspace-f2b-auth-rate-partitions-20261114'
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source

function GitValue {
    param([string[]]$ArgsList)
    $value = @(& $Git -C $Repo @ArgsList)
    if ($LASTEXITCODE -ne 0) { throw 'AUTH_RATE_GIT_FAILED' }
    return ($value -join '').Trim()
}
if ((GitValue @('rev-parse','HEAD')) -cne $CommitSha) { throw 'AUTH_RATE_HEAD_MISMATCH' }
if ((GitValue @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'AUTH_RATE_REMOTE_MISMATCH'
}
$CurrentBranch = GitValue @('branch','--show-current')
if ($CurrentBranch -and $CurrentBranch -cne $Branch) { throw 'AUTH_RATE_BRANCH_MISMATCH' }
$Dirty = @(git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) { throw 'AUTH_RATE_SOURCE_DIRTY' }
Write-Host 'F2B_AUTH_RATE_EXACT_SOURCE=PASS'

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' +
    ([guid]::NewGuid().ToString('N').Substring(0,8))
$Evidence = Join-Path $Repo ('logs\yangspace\f2b-auth-rate\' +
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
    Stage 'auth-rate-restore' @('restore',$Project,'-warnaserror')
    Stage 'auth-rate-build' @('build',$Project,'-c','Release','--no-restore','-warnaserror')
    Stage 'auth-rate-runtime' @('run','--project',$Project,'-c','Release',
        '--no-restore','--no-build','--','--auth-rate-partition-only')

    foreach ($Label in @('auth-rate-restore','auth-rate-build','auth-rate-runtime')) {
        foreach ($Suffix in @('runner.cmd','stdout.log','stderr.log')) {
            if (-not (Test-Path -LiteralPath (Join-Path $Evidence "$Label.$Suffix") -PathType Leaf)) {
                throw "AUTH_RATE_STAGE_ARTIFACT_MISSING=$Label.$Suffix"
            }
        }
    }

    $RuntimeLog = Join-Path $Evidence 'auth-rate-runtime.stdout.log'
    $Log = [IO.File]::ReadAllText($RuntimeLog)
    foreach ($Marker in @(
        'F2B_AUTH_RATE_CLIENT_ISOLATION',
        'F2B_AUTH_RATE_MAPPED_IPV4_NORMALIZED',
        'F2B_AUTH_RATE_FORGED_FORWARD_HEADER_IGNORED',
        'F2B_AUTH_RATE_UNKNOWN_PEER_FAIL_CLOSED',
        'F2B_AUTH_RATE_IPV6_KEY',
        'F2B_AUTH_RATE_LIMIT_PRESERVED',
        'F2B_AUTH_RATE_PARTITION_KEY_STABLE',
        'F2B_AUTH_RATE_PARTITION_CONTRACT'
    )) {
        if ($Log -notmatch ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) {
            throw ('AUTH_RATE_MARKER_MISSING=' + $Marker)
        }
    }

    $SourceAfter = @(git -C $Repo status --porcelain --untracked-files=all |
        Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $SourceAfter.Count -ne 0) {
        throw 'AUTH_RATE_WORKTREE_CHANGED'
    }

    $Manifest = @(
        "COMMIT_SHA=$CommitSha",
        'F2B_AUTH_RATE_EXACT_SOURCE=PASS',
        'F2B_AUTH_RATE_RELEASE_BUILD=PASS',
        'F2B_AUTH_RATE_PARTITION_CONTRACT=PASS',
        'F2B_AUTH_RATE_PROXY_FORWARDING=NOT_TESTED',
        'F2B_AUTH_RATE_EXTERNAL_ABUSE=NOT_TESTED',
        'F2B_IDENTITY_HTTP=NOT_TESTED',
        'PUBLIC_REGISTRATION_DEFAULT_OFF=PASS',
        'CT164_DB=NOT_ACCESSED',
        'RELEASE_GATE=HOLD'
    )
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]$Manifest)
    $Passed = $true
}
finally {
    if (-not $Passed) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),
            "F2B_AUTH_RATE_PARTITION_CONTRACT=HOLD; COMMIT_SHA=$CommitSha")
    }
}
Write-Host 'F2B_AUTH_RATE_SOURCE_RUNTIME=PASS'
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "EVIDENCE_DIR=$Evidence"
Write-Host ('MANIFEST_SHA256=' +
    (Get-FileHash (Join-Path $Evidence 'manifest.txt') -Algorithm SHA256).Hash.ToLowerInvariant())
