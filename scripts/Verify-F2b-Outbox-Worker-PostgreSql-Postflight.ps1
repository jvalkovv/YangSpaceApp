# Independent O3 PostgreSQL worker postflight; evidence-only, no database connections.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$CommitSha,
    [Parameter(Mandatory=$true)][ValidateNotNullOrEmpty()][string]$EvidenceDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch='feat/yangspace-f2b-o3-outbox-worker-20261115'
$Git=(Get-Command git.exe -ErrorAction Stop).Source
$Docker=(Get-Command docker.exe -ErrorAction Stop).Source
function Check([bool]$Ok,[string]$Marker) {
    if (-not $Ok) { throw ('F2B_O3_POSTFLIGHT_FAIL='+$Marker) }
    Write-Host ($Marker+'=PASS')
}
function GitValue([string[]]$ArgsList) {
    $V=@(& $Git -C $Repo @ArgsList)
    if ($LASTEXITCODE -ne 0) { throw 'F2B_O3_POSTFLIGHT_GIT_QUERY_FAILED' }
    return ($V -join '').Trim()
}
function ManifestLine([string[]]$Lines,[string]$Expected) {
    Check ($Lines -ccontains $Expected) ('MANIFEST_'+($Expected -replace '[^a-zA-Z0-9]','_'))
}
function Marker([string]$Label,[string]$Name) {
    $Path=Join-Path $Evidence ($Label+'.stdout.log')
    Check (Test-Path -LiteralPath $Path -PathType Leaf) ('RECEIPT_LOG_'+$Label)
    Check (([IO.File]::ReadAllText($Path)) -match ('(?m)^'+[regex]::Escape($Name)+'=PASS\s*$')) $Name
}
Set-Location $Repo
& $Git -C $Repo fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'F2B_O3_POSTFLIGHT_FETCH_FAILED' }
Check ((GitValue @('rev-parse','HEAD')) -ceq $CommitSha) 'POSTFLIGHT_EXACT_SHA'
Check ((GitValue @('rev-parse','--verify',('refs/remotes/origin/'+$Branch))) -ceq $CommitSha) 'POSTFLIGHT_REMOTE_SHA'
Check ((GitValue @('branch','--show-current')) -ceq $Branch) 'POSTFLIGHT_BRANCH'
$Dirty=@(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
Check ($LASTEXITCODE -eq 0 -and $Dirty.Count -eq 0) 'POSTFLIGHT_CLEAN_SOURCE'
$Evidence=(Resolve-Path -LiteralPath $EvidenceDirectory).Path
$Root=[IO.Path]::GetFullPath((Join-Path $Repo 'logs/yangspace/f2b-o3-postgres'))
Check ($Evidence.StartsWith($Root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) 'POSTFLIGHT_EVIDENCE_SCOPE'
Check ((Split-Path $Evidence -Leaf) -match ('^'+$CommitSha.Substring(0,12)+'-[0-9]{8}-[0-9]{6}-[a-f0-9]{8}$')) 'POSTFLIGHT_EVIDENCE_IDENTITY'
$ManifestPath=Join-Path $Evidence 'manifest.txt'
$ChecksumsPath=Join-Path $Evidence 'SHA256SUMS.txt'
Check ((Test-Path -LiteralPath $ManifestPath -PathType Leaf) -and (Test-Path -LiteralPath $ChecksumsPath -PathType Leaf)) 'POSTFLIGHT_EVIDENCE_PRESENT'
Check (-not (Test-Path -LiteralPath (Join-Path $Evidence 'gate.txt'))) 'POSTFLIGHT_NO_HOLD_MARKER'
$Lines=[IO.File]::ReadAllLines($ManifestPath)
ManifestLine $Lines "SOURCE_SHA=$CommitSha"
ManifestLine $Lines "BRANCH=$Branch"
foreach ($Value in @(
    'POSTGRES_DISPOSABLE=PASS','POSTGRES_17=PASS','POSTGRES_TLS_VERIFYFULL=PASS',
    'POSTGRES_HBA=HOSTSSL_SCRAM_HOSTNOSSL_REJECT',
    'POSTGRES_RUNTIME_LEAST_PRIVILEGE=PASS',
    'POSTGRES_WORKER_CONFIRMATION=PASS','POSTGRES_WORKER_RETRY=PASS',
    'POSTGRES_WORKER_CONCURRENT_CLAIM=PASS','POSTGRES_WORKER_LEASE_RECOVERY=PASS',
    'SQLITE_REGRESSION=PASS','FAKE_TRANSPORT=PASS',
    'NO_HOST_PORTS=PASS','NO_HOST_BINDS=PASS','TMPFS_ONLY=PASS',
    'CONTAINER_REMOVED=PASS','NETWORK_REMOVED=PASS',
    'TEMPORARY_SECRETS_REMOVED=PASS','CLEAN_SOURCE=PASS',
    'PUBLIC_REGISTRATION=OFF','OUTBOX_DISPATCH=OFF',
    'SMTP=NOT_CONNECTED','CT164_DB=NOT_ACCESSED','PRODUCTION_DB=NOT_ACCESSED',
    'GITHUB_ACTIONS=NOT_USED','RELEASE_GATE=HOLD_PENDING_PROVIDER_REVIEW'
)) { ManifestLine $Lines $Value }
$PgVersionEntry=@($Lines | Where-Object { $_ -like 'POSTGRES_VERSION=*' })
Check ($PgVersionEntry.Count -eq 1) 'POSTFLIGHT_PG_VERSION_RECORDED'
$PgVersionString=$PgVersionEntry[0].Substring('POSTGRES_VERSION='.Length)
Check ($PgVersionString -match '^\d+$' -and [int]$PgVersionString -ge 170000 -and [int]$PgVersionString -lt 180000) 'POSTFLIGHT_POSTGRES_17'
$ContainerEntry=@($Lines | Where-Object { $_ -like 'POSTGRES_CONTAINER=*' })
$NetworkEntry=@($Lines | Where-Object { $_ -like 'POSTGRES_NETWORK=*' })
Check ($ContainerEntry.Count -eq 1 -and $NetworkEntry.Count -eq 1) 'POSTFLIGHT_RESOURCE_IDENTIFIERS'
$Container=$ContainerEntry[0].Substring('POSTGRES_CONTAINER='.Length)
$Network=$NetworkEntry[0].Substring('POSTGRES_NETWORK='.Length)
Check ($Container -match '^ys-f2b-o3-pg17-[a-f0-9]{12}$') 'POSTFLIGHT_CONTAINER_NAME'
Check ($Network -match '^ys-f2b-o3-[a-f0-9]{12}$') 'POSTFLIGHT_NETWORK_NAME'
$Was=$ErrorActionPreference
$ErrorActionPreference='Continue'
try {
    $null=@(& $Docker inspect $Container 2>&1)
    $ContainerExit=$LASTEXITCODE
    $null=@(& $Docker network inspect $Network 2>&1)
    $NetworkExit=$LASTEXITCODE
} finally { $ErrorActionPreference=$Was }
Check ($ContainerExit -ne 0) 'POSTFLIGHT_CONTAINER_GONE'
Check ($NetworkExit -ne 0) 'POSTFLIGHT_NETWORK_GONE'

foreach ($Label in @(
    'o3-restore','o3-build','o3-sqlite-regression','o3-publish-linux',
    'o3-network-create','o3-container-create','o3-container-start',
    'o3-pg-version','o3-pg-ssl','o3-pg-hba','o3-ca-export',
    'o3-roles-apply','o3-db-create','o3-grants-apply',
    'o3-runtime-copy','o3-runtime-chmod','o3-pg-worker','o3-runtime-clean'
)) {
    foreach ($Suffix in @('.runner.cmd','.stdout.log','.stderr.log')) {
        Check (Test-Path -LiteralPath (Join-Path $Evidence ($Label+$Suffix)) -PathType Leaf) ('STAGE_'+($Label+$Suffix -replace '[^a-zA-Z0-9]','_'))
    }
}
foreach ($M in @(
    'F2B_O3_WORKER_CONFIRMATION_DISPATCH','F2B_O3_WORKER_RESET_DISPATCH',
    'F2B_O3_WORKER_RETRY_RECOVERY','F2B_O3_WORKER_UNCERTAIN_ACK_AT_LEAST_ONCE',
    'F2B_O3_WORKER_CONCURRENT_CLAIM','F2B_O3_WORKER_NULL_LEASE_RECOVERY',
    'F2B_O3_WORKER_LEASE_EXPIRY_RECOVERY','F2B_O3_WORKER_DEADLETTER',
    'F2B_O3_WORKER_OPERATOR_REPLAY','F2B_O3_WORKER_RETENTION_PURGE',
    'F2B_O3_WORKER_SQLITE_SYNTHETIC_ONLY'
)) { Marker 'o3-sqlite-regression' $M }
foreach ($M in @(
    'F2B_O3_PG_MIGRATIONS_APPLIED','F2B_O3_PG_MIGRATOR_TLS_SCOPE',
    'F2B_O3_PG_RUNTIME_LEAST_PRIVILEGE','F2B_O3_PG_RUNTIME_DENIED_OPERATIONS',
    'F2B_O3_PG_RUNTIME_TLS_SCOPE','F2B_O3_PG_PRODUCTION_DISPATCHER_FALSE',
    'F2B_O3_PG_DI_ACTIVATION','F2B_O3_PG_FAKE_TRANSPORT_ONLY',
    'F2B_O3_PG_CONFIRMATION_DISPATCH','F2B_O3_PG_RETRY_BACKOFF',
    'F2B_O3_PG_RETRY_RECOVERY','F2B_O3_PG_CONCURRENT_CLAIM',
    'F2B_O3_PG_LEASE_EXPIRY_RECOVERY',
    'F2B_O3_WORKER_NPGSQL_PROVIDER','F2B_O3_WORKER_POSTGRESQL_17',
    'F2B_O3_WORKER_TLS_VERIFYFULL','F2B_O3_WORKER_RUNTIME_LEAST_PRIVILEGE',
    'F2B_O3_WORKER_FAKE_TRANSPORT_ONLY','F2B_O3_WORKER_DISPOSABLE_ONLY'
)) { Marker 'o3-pg-worker' $M }
foreach ($Temp in @('temporary-init.sh','temporary-entry.sh','temporary-roles.sql','temporary-grants.sql','temporary-run.sh')) {
    Check (-not (Test-Path -LiteralPath (Join-Path $Evidence $Temp))) ('POSTFLIGHT_NO_'+($Temp -replace '[^a-zA-Z0-9]','_'))
}
Check (@(Get-ChildItem -LiteralPath $Evidence -File | Where-Object { $_.Extension -in @('.key','.p12','.pfx') }).Count -eq 0) 'POSTFLIGHT_NO_PRIVATE_KEYS'

$HashLines=[IO.File]::ReadAllLines($ChecksumsPath)
Check ($HashLines.Count -ge 10) 'POSTFLIGHT_CHECKSUMS_COUNT'
foreach ($HashLine in $HashLines) {
    if ($HashLine -cnotmatch '^([0-9a-f]{64})  ([a-zA-Z0-9._/-]+)$') { throw 'F2B_O3_BAD_CHECKSUM_LINE' }
    $ExpectedHash=$Matches[1]
    $Relative=$Matches[2]
    if ($Relative.Contains('..')) { throw 'F2B_O3_BAD_CHECKSUM_PATH' }
    if ($Relative.StartsWith('apps/') -or $Relative.StartsWith('scripts/')) {
        $Full=Join-Path $Repo $Relative
    } elseif ($Relative -in @('pg-root-ca.crt','o3-sqlite-regression.stdout.log','o3-pg-worker.stdout.log')) {
        $Full=Join-Path $Evidence $Relative
    } else { throw ('F2B_O3_UNKNOWN_CHECKSUM_TARGET='+$Relative) }
    Check (Test-Path -LiteralPath $Full -PathType Leaf) ('HASH_FILE_'+($Relative -replace '[^a-zA-Z0-9]','_'))
    Check ((Get-FileHash -LiteralPath $Full -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $ExpectedHash) ('HASH_VALUE_'+($Relative -replace '[^a-zA-Z0-9]','_'))
}
$Settings=Get-Content -LiteralPath (Join-Path $Repo 'apps/api/appsettings.json') -Raw | ConvertFrom-Json
Check ($Settings.PublicAccounts.Enabled -eq $false) 'POSTFLIGHT_REGISTRATION_OFF'
Check ($Settings.PublicAccounts.Outbox.DispatchEnabled -eq $false) 'POSTFLIGHT_DISPATCH_OFF'
$Dirty=@(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
Check ($LASTEXITCODE -eq 0 -and $Dirty.Count -eq 0) 'POSTFLIGHT_FINAL_CLEAN_SOURCE'
Write-Host 'F2B_O3_POSTGRESQL_INDEPENDENT_POSTFLIGHT=PASS'
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "EVIDENCE_DIR=$Evidence"
Write-Host ('MANIFEST_SHA256='+(Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant())
Write-Host 'RELEASE_GATE=HOLD_PENDING_PROVIDER_REVIEW'
