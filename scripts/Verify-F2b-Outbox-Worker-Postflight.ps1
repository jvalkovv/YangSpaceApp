# Independent read-only O3 worker postflight. No database, Docker, SMTP, HTTP or migrations.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[a-f0-9]{40}$')]
    [string]$CommitSha,
    [Parameter(Mandatory=$true)]
    [string]$EvidenceDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2b-o3-outbox-worker-20261115'
$ExpectedRoot = Join-Path $Repo 'logs\yangspace\f2b-o3-worker'

function Require([bool]$Condition,[string]$Name) {
    if (-not $Condition) { throw ('F2B_O3_POSTFLIGHT_FAILED=' + $Name) }
    Write-Host ($Name + '=PASS')
}
$Evidence = (Resolve-Path -LiteralPath $EvidenceDirectory -ErrorAction Stop).Path
$Prefix = [IO.Path]::GetFullPath($ExpectedRoot).TrimEnd('\') + '\'
Require ($Evidence.StartsWith($Prefix,[StringComparison]::OrdinalIgnoreCase)) 'EVIDENCE_SCOPE'
Require ((Split-Path $Evidence -Leaf).StartsWith($CommitSha.Substring(0,12) + '-')) 'EVIDENCE_SHA_PREFIX'

$Head = @(& git -C $Repo rev-parse HEAD)
Require ($LASTEXITCODE -eq 0 -and ($Head -join '').Trim() -ceq $CommitSha) 'SOURCE_SHA'
$Remote = @(& git -C $Repo rev-parse --verify ('refs/remotes/origin/' + $Branch))
Require ($LASTEXITCODE -eq 0 -and ($Remote -join '').Trim() -ceq $CommitSha) 'REMOTE_SHA'
$Dirty = @(& git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
Require ($LASTEXITCODE -eq 0 -and $Dirty.Count -eq 0) 'CLEAN_WORKTREE'
Require (-not (Test-Path -LiteralPath (Join-Path $Evidence 'gate.txt'))) 'NO_FAILURE_GATE'

$ManifestPath = Join-Path $Evidence 'manifest.txt'
Require (Test-Path -LiteralPath $ManifestPath -PathType Leaf) 'MANIFEST_PRESENT'
$Manifest = @(Get-Content -LiteralPath $ManifestPath)
foreach ($Marker in @(
    "COMMIT_SHA=$CommitSha",
    'F2B_O3_EXACT_SOURCE=PASS',
    'F2B_O3_RELEASE_BUILD=PASS',
    'F2B_O3_WORKER_SQLITE_SYNTHETIC=PASS',
    'F2B_O3_POSTGRES_WORKER=NOT_RUN',
    'F2B_O3_REAL_SMTP=NOT_CONNECTED',
    'F2B_O3_IDENTITY_HTTP=NOT_TESTED',
    'PUBLIC_REGISTRATION_DEFAULT_OFF=PASS',
    'OUTBOX_DISPATCH_DEFAULT_OFF=PASS',
    'CT164_DB=NOT_ACCESSED',
    'GITHUB_ACTIONS=NOT_USED',
    'RELEASE_GATE=HOLD'
)) {
    Require ($Manifest -ccontains $Marker) ('MANIFEST_' + $Marker.Split('=')[0])
}
foreach ($Stage in @('o3-restore','o3-build','o3-worker-synthetic')) {
    foreach ($Suffix in @('.runner.cmd','.stdout.log','.stderr.log')) {
        Require (Test-Path -LiteralPath (Join-Path $Evidence ($Stage+$Suffix)) -PathType Leaf) ('STAGE_' + $Stage + $Suffix)
    }
}
$Stdout = Get-Content -LiteralPath (Join-Path $Evidence 'o3-worker-synthetic.stdout.log') -Raw
foreach ($Marker in @(
    'F2B_O3_WORKER_CONFIRMATION_DISPATCH',
    'F2B_O3_WORKER_RESET_DISPATCH',
    'F2B_O3_WORKER_RECHECKS_CURRENT_EMAIL',
    'F2B_O3_WORKER_RECHECKS_CONFIRMATION_AND_STAFF_STATE',
    'F2B_O3_WORKER_RETRY_BACKOFF',
    'F2B_O3_WORKER_RETRY_RECOVERY',
    'F2B_O3_WORKER_UNCERTAIN_ACK_AT_LEAST_ONCE',
    'F2B_O3_WORKER_CONCURRENT_CLAIM',
    'F2B_O3_WORKER_CRASH_LEAVES_EXPIRING_LEASE',
    'F2B_O3_WORKER_LEASE_EXPIRY_RECOVERY',
    'F2B_O3_WORKER_DEADLETTER',
    'F2B_O3_WORKER_OPERATOR_REPLAY',
    'F2B_O3_WORKER_RETENTION_PURGE',
    'F2B_O3_NO_TOKEN_OR_RECIPIENT_AT_REST',
    'F2B_O3_WORKER_FAKE_TRANSPORT_ONLY',
    'F2B_O3_WORKER_SQLITE_SYNTHETIC_ONLY'
)) {
    Require ($Stdout -match ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) $Marker
}
$Config = Get-Content -LiteralPath (Join-Path $Repo 'apps\api\appsettings.json') -Raw | ConvertFrom-Json
Require ($Config.PublicAccounts.Enabled -eq $false -and
         $Config.PublicAccounts.Outbox.DispatchEnabled -eq $false -and
         $Config.ClaimReviewPostgreSqlQa.Enabled -eq $false) 'DEFAULT_OFF_GUARDS'
$Hash = (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host 'F2B_O3_WORKER_INDEPENDENT_POSTFLIGHT=PASS' -ForegroundColor Green
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "MANIFEST_SHA256=$Hash"
Write-Host "EVIDENCE_DIR=$Evidence"
