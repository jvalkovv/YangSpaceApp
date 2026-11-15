# Independent read-only postflight for F2b outbox O1 synthetic QA.
# No DB connect, SMTP, HTTP, container operations, migrations or CT164.
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
$Branch = 'feat/yangspace-f2b-email-outbox-enqueue-foundation-20261114'
$ExpectedRoot = Join-Path $Repo 'logs\yangspace\f2b-outbox-enqueue'

function Require([bool]$Condition, [string]$Name) {
    if (!$Condition) { throw "F2B_OUTBOX_POSTFLIGHT_FAILED=$Name" }
    Write-Host "$Name=PASS"
}
$Evidence = (Resolve-Path -LiteralPath $EvidenceDirectory -ErrorAction Stop).Path
$Prefix = [IO.Path]::GetFullPath($ExpectedRoot).TrimEnd('\') + '\'
Require ($Evidence.StartsWith($Prefix, [StringComparison]::OrdinalIgnoreCase)) 'EVIDENCE_SCOPE'
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
    'F2B_OUTBOX_EXACT_SOURCE=PASS',
    'F2B_OUTBOX_RELEASE_BUILD=PASS',
    'F2B_OUTBOX_ENQUEUE_SYNTHETIC_ONLY=PASS',
    'F2B_OUTBOX_PG_MIGRATIONS=NOT_GENERATED',
    'F2B_OUTBOX_WORKER=NOT_IMPLEMENTED',
    'F2B_IDENTITY_HTTP=NOT_TESTED',
    'PUBLIC_REGISTRATION_DEFAULT_OFF=PASS',
    'CT164_DB=NOT_ACCESSED',
    'RELEASE_GATE=HOLD'
)) {
    Require ($Manifest -ccontains $Marker) ("MANIFEST_" + $Marker.Split('=')[0])
}
foreach ($Stage in @('outbox-restore','outbox-build','outbox-enqueue')) {
    foreach ($Suffix in @('.runner.cmd','.stdout.log','.stderr.log')) {
        Require (Test-Path -LiteralPath (Join-Path $Evidence ($Stage+$Suffix)) -PathType Leaf) ("STAGE_" + $Stage + $Suffix)
    }
}
$Stdout = Get-Content -LiteralPath (Join-Path $Evidence 'outbox-enqueue.stdout.log') -Raw
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
    Require ($Stdout -match ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) $Marker
}
$Config = Get-Content -LiteralPath (Join-Path $Repo 'apps\api\appsettings.json') -Raw | ConvertFrom-Json
Require ($Config.PublicAccounts.Enabled -eq $false -and
         $Config.ClaimReviewPostgreSqlQa.Enabled -eq $false) 'DEFAULT_OFF_BOTH'
$Hash = (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host 'F2B_OUTBOX_INDEPENDENT_POSTFLIGHT=PASS' -ForegroundColor Green
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "MANIFEST_SHA256=$Hash"
Write-Host "EVIDENCE_DIR=$Evidence"
