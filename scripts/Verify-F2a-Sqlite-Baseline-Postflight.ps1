# Independent, read-only F2a-S1 SQLite scaffold postflight.
# Does not connect to databases, commit generated migrations, or remove artifacts.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$CommitSha,
    [Parameter(Mandatory=$true)][string]$EvidenceDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch='fix/yangspace-f2a-sqlite-baseline-20261114'
$ExpectedRoot=Join-Path $Repo 'logs/yangspace/f2a-sqlite-baseline'
$Directory=(Resolve-Path -LiteralPath $EvidenceDirectory -ErrorAction Stop).Path
function Check([bool]$OK,[string]$Marker) {
    if (-not $OK) { throw "F2A_S1_POSTFLIGHT_FAILED=$Marker" }
    Write-Host "$Marker=PASS"
}
$Prefix=[IO.Path]::GetFullPath($ExpectedRoot).TrimEnd('\')+'\'
Check ($Directory.StartsWith($Prefix,[StringComparison]::OrdinalIgnoreCase)) 'EVIDENCE_SCOPE'
Check ((Split-Path -Path $Directory -Leaf).StartsWith($CommitSha.Substring(0,12)+'-')) 'EVIDENCE_SHA_PREFIX'
$Head=@(& git -C $Repo rev-parse HEAD)
Check ($LASTEXITCODE -eq 0 -and ($Head -join '').Trim() -ceq $CommitSha) 'SOURCE_SHA'
$Remote=@(& git -C $Repo rev-parse --verify ('refs/remotes/origin/'+$Branch))
Check ($LASTEXITCODE -eq 0 -and ($Remote -join '').Trim() -ceq $CommitSha) 'REMOTE_SHA'
Check (-not (Test-Path -LiteralPath (Join-Path $Directory 'gate.txt'))) 'NO_FAILURE_GATE'
$ManifestPath=Join-Path $Directory 'manifest.txt'
Check (Test-Path -LiteralPath $ManifestPath -PathType Leaf) 'MANIFEST'
$Manifest=@(Get-Content -LiteralPath $ManifestPath)
foreach ($Marker in @(
    "SOURCE_SHA=$CommitSha",
    'EF_TOOL_VERSION=10.0.0',
    'F2A_SQLITE_BASELINE_SCAFFOLD=PASS',
    'F2A_SQLITE_GENERATED_FILES=UNCOMMITTED',
    'LOCATIONS_REBUILD=REQUIRES_DISPOSABLE_DATA_PRESERVATION_TEST',
    'SQL_REVIEW=REQUIRED',
    'DISPOSABLE_UPGRADE=NOT_RUN',
    'LIVE_DB_APPLY=NOT_RUN',
    'CT164_DB=NOT_ACCESSED',
    'RELEASE_GATE=HOLD'
)) {
    Check ($Manifest -ccontains $Marker) ('MANIFEST_'+$Marker.Split('=')[0])
}
$Stages=@('f2a-sqlite-tool-restore','f2a-sqlite-tool-version','f2a-sqlite-restore',
    'f2a-sqlite-build-before','f2a-sqlite-migration-add',
    'f2a-sqlite-build-after','f2a-sqlite-model-diff','f2a-sqlite-sql')
foreach ($Stage in $Stages) {
    foreach ($Suffix in @('.runner.cmd','.stdout.log','.stderr.log')) {
        Check (Test-Path -LiteralPath (Join-Path $Directory ($Stage+$Suffix)) -PathType Leaf) ('RECEIPT_'+$Stage+$Suffix)
    }
}
$Changed=@(& git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
Check ($LASTEXITCODE -eq 0 -and $Changed.Count -eq 3) 'ONLY_THREE_GENERATED_CHANGES'
$ExpectedFiles=@('apps/api/Migrations/DirectoryDbModelSnapshot.cs')
$SqlPath=Join-Path $Directory 'f2a-sqlite-baseline.review.sql'
Check (Test-Path -LiteralPath $SqlPath -PathType Leaf) 'SQL_REVIEW_FILE'
$Sql=[IO.File]::ReadAllText($SqlPath)
$Names=@([regex]::Matches($Sql,'(?i)\bCREATE\s+TABLE\s+"([^"]+)"') |
    ForEach-Object { $_.Groups[1].Value })
$ExpectedTables=@('Organizations','BusinessClaims','ContentAuditEvents',
    'OrganizationMembers','ef_temp_Locations')
Check ($Names.Count -eq 5 -and
    @(Compare-Object -ReferenceObject ($Names | Sort-Object) -DifferenceObject ($ExpectedTables | Sort-Object)).Count -eq 0) 'F2A_ONLY_CREATE_TABLE_SCOPE'
Check ($Sql -notmatch 'IdentityEmailOutbox') 'NO_OUTBOX_TABLE'
Check ($Sql -match '(?im)^\s*INSERT\s+INTO\s+"ef_temp_Locations"(?=\s*\()' -and
    $Sql -match '(?im)^\s*DROP\s+TABLE\s+"Locations"\s*;' -and
    $Sql -match '(?im)^\s*ALTER\s+TABLE\s+"ef_temp_Locations"\s+RENAME\s+TO\s+"Locations"') 'LOCATIONS_REBUILD_PATTERN_REVIEWABLE'
$SumPath=Join-Path $Directory 'SHA256SUMS.txt'
Check (Test-Path -LiteralPath $SumPath -PathType Leaf) 'SHA256SUMS_PRESENT'
$Sum=@(Get-Content -LiteralPath $SumPath)
Check ($Sum.Count -eq 4) 'SHA256SUMS_COUNT'
$HashedPaths=@()
foreach ($Line in $Sum) {
    if ($Line -cnotmatch '^([0-9a-f]{64})  (.+)$') { throw 'BAD_SHA256SUMS_LINE' }
    $ExpectedHash=$Matches[1]
    $Relative=$Matches[2]
    if ($Relative -ceq 'f2a-sqlite-baseline.review.sql') {
        $Full=$SqlPath
    } elseif ($Relative -cmatch '^apps/api/Migrations/([0-9]{14}_F2aOrganizationClaimFoundation(\.Designer)?\.cs|DirectoryDbModelSnapshot\.cs)$') {
        $Full=Join-Path $Repo $Relative
    } else { throw "UNEXPECTED_HASHED_PATH=$Relative" }
    $ActualHash=(Get-FileHash -LiteralPath $Full -Algorithm SHA256).Hash.ToLowerInvariant()
    Check ($ActualHash -ceq $ExpectedHash) ('SHA256_'+(Split-Path $Full -Leaf))
    $HashedPaths+=$Relative
}
Check (@($HashedPaths | Select-Object -Unique).Count -eq 4) 'HASHED_PATHS_UNIQUE'
$ExpectedChanged=@($Changed | ForEach-Object { $_.Substring(3) })
foreach ($Rel in $ExpectedChanged) {
    Check ($HashedPaths -ccontains $Rel) ('HASHED_GENERATED_'+(Split-Path $Rel -Leaf))
}
Write-Host 'F2A_SQLITE_BASELINE_INDEPENDENT_POSTFLIGHT=PASS' -ForegroundColor Green
Write-Host "SOURCE_SHA=$CommitSha"
Write-Host ('MANIFEST_SHA256='+
    (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant())
Write-Host "EVIDENCE_DIR=$Directory"
Write-Host 'DISPOSABLE_DATA_PRESERVATION=NOT_TESTED'
Write-Host 'RELEASE_GATE=HOLD'
