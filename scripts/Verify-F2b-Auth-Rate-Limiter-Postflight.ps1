# Independent read-only postflight of F2b auth partition QA run.
# PowerShell 5.1, no DB/SMTP/Docker/HTTP or production access.
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
$Branch = 'fix/yangspace-f2b-auth-rate-partitions-20261114'
$ExpectedRoot = Join-Path $Repo 'logs\yangspace\f2b-auth-rate'

function Require([bool]$Ok, [string]$Marker) {
    if (!$Ok) { throw "AUTH_RATE_POSTFLIGHT_FAIL=$Marker" }
    Write-Host "$Marker=PASS"
}

$ActualRoot = [IO.Path]::GetFullPath($ExpectedRoot)
$ActualPath = (Resolve-Path -LiteralPath $EvidenceDirectory -ErrorAction Stop).Path
$Prefix = [IO.Path]::GetFullPath($ActualRoot).TrimEnd('\') + '\'
Require ($ActualPath.StartsWith($Prefix, [StringComparison]::OrdinalIgnoreCase)) 'EVIDENCE_SCOPE'
Require ((Split-Path -Path $ActualPath -Leaf).StartsWith($CommitSha.Substring(0,12) + '-')) 'EVIDENCE_SHA_PREFIX'

$Head = @(git -C $Repo rev-parse HEAD)
Require ($LASTEXITCODE -eq 0 -and ($Head -join '').Trim() -ceq $CommitSha) 'SOURCE_SHA'
$Origin = @(git -C $Repo rev-parse --verify ('refs/remotes/origin/' + $Branch))
Require ($LASTEXITCODE -eq 0 -and ($Origin -join '').Trim() -ceq $CommitSha) 'REMOTE_SHA'
$Dirty = @(git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
Require ($LASTEXITCODE -eq 0 -and $Dirty.Count -eq 0) 'CLEAN_WORKTREE'

$Gate = Join-Path $ActualPath 'gate.txt'
Require (-not (Test-Path -LiteralPath $Gate)) 'NO_FAILURE_GATE'
$ManifestPath = Join-Path $ActualPath 'manifest.txt'
Require (Test-Path -LiteralPath $ManifestPath -PathType Leaf) 'MANIFEST'
$Manifest = @(Get-Content -LiteralPath $ManifestPath)
$Expected = @(
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
foreach ($Marker in $Expected) {
    Require ($Manifest -ccontains $Marker) ("MANIFEST_" + $Marker.Split('=')[0])
}

$Stages = @('auth-rate-restore','auth-rate-build','auth-rate-runtime')
foreach ($Stage in $Stages) {
    foreach ($Suffix in @('.runner.cmd','.stdout.log','.stderr.log')) {
        $Path = Join-Path $ActualPath ($Stage + $Suffix)
        Require (Test-Path -LiteralPath $Path -PathType Leaf) ("STAGE_" + $Stage + $Suffix)
    }
}
$Stdout = Get-Content -LiteralPath (Join-Path $ActualPath 'auth-rate-runtime.stdout.log') -Raw
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
    Require ($Stdout -match ('(?m)^' + [regex]::Escape($Marker) + '=PASS\s*$')) $Marker
}

$Config = Get-Content -LiteralPath (Join-Path $Repo 'apps\api\appsettings.json') -Raw | ConvertFrom-Json
Require (($Config.PublicAccounts.Enabled -eq $false) -and
    ($Config.ClaimReviewPostgreSqlQa.Enabled -eq $false)) 'DEFAULT_OFF_BOTH'

$Hash = (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host 'F2B_AUTH_RATE_INDEPENDENT_POSTFLIGHT=PASS' -ForegroundColor Green
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "MANIFEST_SHA256=$Hash"
Write-Host "EVIDENCE_DIR=$ActualPath"
