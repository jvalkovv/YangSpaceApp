# Windows PowerShell 5.1; synthetic-only F2a-3 evidence policy contract.
# No upload/download endpoint, DB migration, production storage or real evidence.
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
$Branch = 'feat/f2a3-evidence-synthetic-qa-20261113'
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source

function GitValue {
    param([Parameter(Mandatory=$true)][string[]]$CommandArgs)
    $Output = @(& $Git -C $Repo @CommandArgs)
    if ($LASTEXITCODE -ne 0) {
        throw ('F2A3_EVIDENCE_GIT_FAILED=' + ($CommandArgs -join ' '))
    }
    return ($Output -join '').Trim()
}

$Tokens = $null
$ParseErrors = $null
[void][System.Management.Automation.Language.Parser]::ParseFile(
    $PSCommandPath, [ref]$Tokens, [ref]$ParseErrors)
if ($ParseErrors.Count -ne 0) { throw 'F2A3_EVIDENCE_RUNNER_SYNTAX_FAILED' }
Write-Host 'F2A3_EVIDENCE_RUNNER_SYNTAX=PASS'

if ((GitValue @('rev-parse','HEAD')) -cne $CommitSha) {
    throw 'F2A3_EVIDENCE_SOURCE_SHA_MISMATCH'
}
if ((GitValue @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'F2A3_EVIDENCE_REMOTE_SHA_MISMATCH'
}
$CurrentBranch = GitValue @('branch','--show-current')
if ($CurrentBranch -and $CurrentBranch -cne $Branch) {
    throw 'F2A3_EVIDENCE_BRANCH_MISMATCH'
}
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) {
    throw 'F2A3_EVIDENCE_WORKTREE_DIRTY'
}
Write-Host 'F2A3_EVIDENCE_SOURCE_IDENTITY=PASS'

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' +
    [Guid]::NewGuid().ToString('N').Substring(0,8)
$Evidence = Join-Path $Repo ('logs\yangspace\f2a3-evidence-policy\' +
    $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')

function Invoke-Stage {
    param([string]$Label,[string[]]$CommandArguments)
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $CommandArguments -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 900
}

$Completed = $false
try {
    Invoke-Stage 'f2a3-evidence-restore' @('restore',$Project,'-warnaserror')
    Invoke-Stage 'f2a3-evidence-build' @(
        'build',$Project,'-c','Release','--no-restore','-warnaserror'
    )
    Invoke-Stage 'f2a3-evidence-policy' @(
        'run','--project',$Project,'-c','Release','--no-build',
        '--no-restore','--','--evidence-policy-only'
    )

    $Stdout = Join-Path $Evidence 'f2a3-evidence-policy.stdout.log'
    if (-not (Test-Path -LiteralPath $Stdout -PathType Leaf)) {
        throw 'F2A3_EVIDENCE_POLICY_STDOUT_MISSING'
    }
    $Output = Get-Content -LiteralPath $Stdout -Raw

    foreach ($Marker in @(
        'EVIDENCE_PDF_ACCEPTED'
        'EVIDENCE_JPEG_ACCEPTED'
        'EVIDENCE_PNG_ACCEPTED'
        'EVIDENCE_EXTENSION_SIGNATURE_MISMATCH_REJECTED'
        'EVIDENCE_MIME_MISMATCH_REJECTED'
        'EVIDENCE_EXECUTABLE_EXTENSION_REJECTED'
        'EVIDENCE_PATH_TRAVERSAL_NAME_REJECTED'
        'EVIDENCE_EMPTY_FILE_REJECTED'
        'EVIDENCE_ENCRYPTED_PDF_MARKER_REJECTED'
        'EVIDENCE_PER_FILE_LIMIT_ENFORCED'
        'EVIDENCE_CLAIM_FILE_COUNT_LIMIT_ENFORCED'
        'EVIDENCE_CLAIM_TOTAL_SIZE_LIMIT_ENFORCED'
        'EVIDENCE_INVALID_EXISTING_STATE_REJECTED'
        'EVIDENCE_EXACT_FILE_LIMIT_ACCEPTED'
        'EVIDENCE_PENDING_90_PLUS_30_DAYS'
        'EVIDENCE_REJECTED_180_PLUS_30_DAYS'
        'EVIDENCE_APPROVED_REVERIFY_12_MONTHS_PLUS_30_DAYS'
        'EVIDENCE_SUPERSEDED_30_DAYS'
        'EVIDENCE_ACCESS_LOG_12_MONTHS'
        'EVIDENCE_ACTIVE_CLAIM_METADATA_RETAINED'
        'EVIDENCE_DECISION_METADATA_24_MONTHS'
        'EVIDENCE_BACKUP_MAX_35_DAYS'
        'EVIDENCE_MISSING_RETENTION_ANCHOR_FAILS'
        'EVIDENCE_NON_UTC_RETENTION_ANCHOR_REJECTED'
        'EVIDENCE_SCANNER_MISSING_FAILS_CLOSED'
        'EVIDENCE_SYNTHETIC_STORE_ROUNDTRIP'
        'EVIDENCE_SYNTHETIC_STORE_DELETE'
        'EVIDENCE_SYNTHETIC_STORE_NOT_FOUND_AFTER_DELETE'
        'F2A3_EVIDENCE_POLICY_CONTRACT'
        'F2A3_EVIDENCE_POLICY_SYNTHETIC_ONLY'
    )) {
        $Pattern = '(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$'
        if ($Output -notmatch $Pattern) {
            throw ('F2A3_EVIDENCE_POLICY_MARKER_MISSING=' + $Marker)
        }
    }

    $After = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $After.Count -ne 0) {
        throw 'F2A3_EVIDENCE_TEST_CHANGED_SOURCE'
    }

    $RunnerScriptHash = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]@(
        ('COMMIT_SHA=' + $CommitSha)
        'RUNNER_SYNTAX=PASS'
        ('RUNNER_SCRIPT_SHA256=' + $RunnerScriptHash)
        'SOURCE_RESTORE_BUILD=PASS'
        'F2A3_EVIDENCE_POLICY_SYNTHETIC_ONLY=PASS'
        'TEST_PROVIDER=PURE_POLICY_PLUS_IN_MEMORY_SYNTHETIC_STORE'
        'FILE_FIXTURES=GENERATED_SYNTHETIC_BYTES_ONLY'
        'HTTP_UPLOAD_DOWNLOAD=NOT_EXPOSED'
        'DATABASE_SCHEMA_MIGRATION=NOT_CHANGED'
        'PRODUCTION_STORAGE=NOT_REGISTERED'
        'MALWARE_SCANNER=NOT_CONFIGURED_FAIL_CLOSED'
        'REAL_EVIDENCE=NOT_USED'
        'LEGAL_PRIVACY_CLEARANCE=NOT_COMPLETE'
        'CT164_DB=NOT_ACCESSED'
        'CT152=NOT_ACCESSED'
        'PRODUCTION=NOT_ACCESSED'
        'RELEASE_GATE=HOLD'
    ))
    $Completed = $true
}
finally {
    if (-not $Completed) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),'F2A3_EVIDENCE_POLICY_SYNTHETIC_ONLY=HOLD')
        Write-Warning ('F2A3_EVIDENCE_POLICY_SYNTHETIC_ONLY=HOLD; EVIDENCE_DIR=' + $Evidence)
    }
}

Write-Host 'F2A3_EVIDENCE_POLICY_SYNTHETIC_ONLY=PASS'
Write-Host ('COMMIT_SHA=' + $CommitSha)
Write-Host ('EVIDENCE_DIR=' + $Evidence)
Write-Host 'REAL_EVIDENCE_PRODUCTION_INTAKE=NOT_AUTHORIZED'
