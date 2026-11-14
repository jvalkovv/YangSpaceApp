# Windows PowerShell 5.1; independent postflight for synthetic F2a-3 evidence QA.
# Validates exact source identity, manifest, stage receipts and output markers.
# Does not access production, PVE, CT164/CT152, live databases or real evidence.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha,
    [Parameter(Mandatory=$true)]
    [string]$EvidenceDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Tokens = $null
$ParseErrors = $null
[void][System.Management.Automation.Language.Parser]::ParseFile(
    $PSCommandPath, [ref]$Tokens, [ref]$ParseErrors)
if ($ParseErrors.Count -ne 0) { throw 'F2A3_EVIDENCE_POSTFLIGHT_RUNNER_SYNTAX_FAILED' }
$VerifierScriptHash = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
$RunnerScriptPath = Join-Path $PSScriptRoot 'Test-F2a3-Evidence-Policy-Local.ps1'
$RunnerScriptHash = (Get-FileHash -LiteralPath $RunnerScriptPath -Algorithm SHA256).Hash.ToLowerInvariant()

$Branch = 'feat/f2a3-evidence-synthetic-qa-20261113'
$Git = (Get-Command git.exe -ErrorAction Stop).Source

function GitValue {
    param([Parameter(Mandatory=$true)][string[]]$CommandArgs)
    $Output = @(& $Git -C $Repo @CommandArgs)
    if ($LASTEXITCODE -ne 0) {
        throw ('F2A3_EVIDENCE_POSTFLIGHT_GIT_FAILED=' + ($CommandArgs -join ' '))
    }
    return ($Output -join '').Trim()
}

if ((GitValue @('rev-parse','HEAD')) -cne $CommitSha) {
    throw 'F2A3_EVIDENCE_POSTFLIGHT_SOURCE_SHA_MISMATCH'
}
if ((GitValue @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'F2A3_EVIDENCE_POSTFLIGHT_REMOTE_SHA_MISMATCH'
}
$CurrentBranch = GitValue @('branch','--show-current')
if ($CurrentBranch -and $CurrentBranch -cne $Branch) {
    throw 'F2A3_EVIDENCE_POSTFLIGHT_BRANCH_MISMATCH'
}

$ExpectedPrefix = Join-Path $Repo ('logs\yangspace\f2a3-evidence-policy\' + $CommitSha.Substring(0,12) + '-')
$Evidence = (Resolve-Path -LiteralPath $EvidenceDirectory).Path
if (-not $Evidence.StartsWith($ExpectedPrefix,[StringComparison]::OrdinalIgnoreCase)) {
    throw 'F2A3_EVIDENCE_POSTFLIGHT_PATH_OUTSIDE_EXPECTED_ROOT'
}
if (Test-Path -LiteralPath (Join-Path $Evidence 'gate.txt')) {
    throw 'F2A3_EVIDENCE_POSTFLIGHT_FAILURE_GATE_PRESENT'
}

$ManifestPath = Join-Path $Evidence 'manifest.txt'
if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
    throw 'F2A3_EVIDENCE_POSTFLIGHT_MANIFEST_MISSING'
}
$Manifest = Get-Content -LiteralPath $ManifestPath
if ($Manifest -cnotcontains ('RUNNER_SCRIPT_SHA256=' + $RunnerScriptHash)) {
    throw 'F2A3_EVIDENCE_POSTFLIGHT_RUNNER_HASH_MISMATCH'
}
foreach ($Required in @(
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
)) {
    if ($Manifest -cnotcontains $Required) {
        throw ('F2A3_EVIDENCE_POSTFLIGHT_MANIFEST_MARKER_MISSING=' + $Required)
    }
}

$ReceiptNames = @(
    'f2a3-evidence-restore.runner.cmd'
    'f2a3-evidence-restore.stdout.log'
    'f2a3-evidence-restore.stderr.log'
    'f2a3-evidence-build.runner.cmd'
    'f2a3-evidence-build.stdout.log'
    'f2a3-evidence-build.stderr.log'
    'f2a3-evidence-policy.runner.cmd'
    'f2a3-evidence-policy.stdout.log'
    'f2a3-evidence-policy.stderr.log'
)
foreach ($Name in $ReceiptNames) {
    if (-not (Test-Path -LiteralPath (Join-Path $Evidence $Name) -PathType Leaf)) {
        throw ('F2A3_EVIDENCE_POSTFLIGHT_RECEIPT_MISSING=' + $Name)
    }
}

$Output = Get-Content -LiteralPath (Join-Path $Evidence 'f2a3-evidence-policy.stdout.log') -Raw
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
    'EVIDENCE_SCANNER_CLEAN_ACCEPTED'
    'EVIDENCE_SCANNER_THREAT_FAILS_CLOSED'
    'EVIDENCE_SCANNER_INDETERMINATE_FAILS_CLOSED'
    'EVIDENCE_SYNTHETIC_STORE_ROUNDTRIP'
    'EVIDENCE_SYNTHETIC_STORE_DELETE'
    'EVIDENCE_SYNTHETIC_STORE_NOT_FOUND_AFTER_DELETE'
    'F2A3_EVIDENCE_POLICY_CONTRACT'
    'F2A3_EVIDENCE_POLICY_SYNTHETIC_ONLY'
)) {
    if ($Output -notmatch ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) {
        throw ('F2A3_EVIDENCE_POSTFLIGHT_OUTPUT_MARKER_MISSING=' + $Marker)
    }
}

$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) {
    throw 'F2A3_EVIDENCE_POSTFLIGHT_WORKTREE_DIRTY'
}

$FilesToHash = @('manifest.txt') + $ReceiptNames
$HashLines = [System.Collections.Generic.List[string]]::new()
foreach ($Name in $FilesToHash) {
    $Hash = (Get-FileHash -LiteralPath (Join-Path $Evidence $Name) -Algorithm SHA256).Hash.ToLowerInvariant()
    $HashLines.Add(($Name + '_SHA256=' + $Hash))
}
$ManifestHash = (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
$Postflight = @(
    ('COMMIT_SHA=' + $CommitSha)
    'F2A3_EVIDENCE_POSTFLIGHT=PASS'
    'VERIFY_SCRIPT_SYNTAX=PASS'
    ('VERIFY_SCRIPT_SHA256=' + $VerifierScriptHash)
    ('MANIFEST_SHA256=' + $ManifestHash)
) + $HashLines.ToArray()
[IO.File]::WriteAllLines((Join-Path $Evidence 'postflight.txt'),[string[]]$Postflight)

Write-Host 'F2A3_EVIDENCE_POSTFLIGHT=PASS'
Write-Host ('COMMIT_SHA=' + $CommitSha)
Write-Host ('MANIFEST_SHA256=' + $ManifestHash)
Write-Host ('EVIDENCE_DIR=' + $Evidence)
