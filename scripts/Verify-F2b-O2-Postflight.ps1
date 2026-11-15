# Independent, read-only O2 evidence postflight. Does not connect to either database.
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
$Branch = 'fix/yangspace-f2b-o2-race-safe-20261115'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Docker = (Get-Command docker.exe -ErrorAction Stop).Source
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Check([bool]$Condition, [string]$Marker) {
    if (-not $Condition) { throw ('F2B_O2_POSTFLIGHT_FAILED=' + $Marker) }
    Write-Host ($Marker + '=PASS')
}
function GitValue {
    param([string[]]$CommandArgs)
    $Output = @(& $Git -C $Repo @CommandArgs)
    if ($LASTEXITCODE -ne 0) { throw ('F2B_O2_GIT_FAILED=' + ($CommandArgs -join ' ')) }
    return ($Output -join '').Trim()
}

if (-not [IO.Path]::IsPathRooted($EvidenceDirectory)) {
    $EvidenceDirectory = Join-Path $Repo $EvidenceDirectory
}
$Evidence = (Resolve-Path -LiteralPath $EvidenceDirectory).Path
$ExpectedRoot = [IO.Path]::GetFullPath((Join-Path $Repo 'logs/yangspace/f2b-o2')) + [IO.Path]::DirectorySeparatorChar
if (-not $Evidence.StartsWith($ExpectedRoot,[StringComparison]::OrdinalIgnoreCase)) {
    throw 'F2B_O2_EVIDENCE_PATH_OUTSIDE_APPROVED_ROOT'
}
Check ((GitValue @('rev-parse','HEAD')) -ceq $CommitSha) 'POSTFLIGHT_EXACT_HEAD'
Check ((GitValue @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -ceq $CommitSha) 'POSTFLIGHT_REMOTE_HEAD'
$CurrentBranch = GitValue @('branch','--show-current')
Check (-not $CurrentBranch -or $CurrentBranch -ceq $Branch) 'POSTFLIGHT_BRANCH_IDENTITY'
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) { throw 'F2B_O2_POSTFLIGHT_WORKTREE_NOT_CLEAN' }
Check $true 'POSTFLIGHT_SOURCE_WORKTREE_CLEAN'

$ManifestPath = Join-Path $Evidence 'manifest.txt'
$ChecksumsPath = Join-Path $Evidence 'SHA256SUMS.txt'
if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $ChecksumsPath -PathType Leaf) -or
    (Test-Path -LiteralPath (Join-Path $Evidence 'gate.txt'))) {
    throw 'F2B_O2_POSTFLIGHT_GATE_OR_MANIFEST_MISSING'
}
$Manifest = @{}
foreach ($Line in Get-Content -LiteralPath $ManifestPath) {
    if ($Line -notmatch '^([A-Z0-9_]+)=(.*)$') { throw 'F2B_O2_MANIFEST_LINE_INVALID' }
    $Manifest[$Matches[1]] = $Matches[2]
}
$Required = @{
    SOURCE_SHA=$CommitSha
    BRANCH=$Branch
    SQLITE_BASELINE='20261114183515_F2aOrganizationClaimFoundation'
    SQLITE_TARGET='20261115101409_F2bIdentityEmailOutbox'
    POSTGRESQL_BASELINE='20261111203833_InitialPostgreSqlSchema'
    POSTGRESQL_TARGET='20261115101421_F2bIdentityEmailOutbox'
    EF_TOOL_VERSION='10.0.0'
    POSTGRES_NETWORK_INTERNAL='PASS'
    POSTGRES_HOST_PORT_PUBLISHED='none'
    POSTGRES_RUNTIME_EXECUTION='IN_DISPOSABLE_CONTAINER'
    POSTGRES_RUNTIME_CLIENT='SELF_CONTAINED_LINUX_X64'
    POSTGRES_RUNTIME_CONNECTION='LOCALHOST_5432'
    POSTGRES_HOST_BINDS='none'
    POSTGRES_ANONYMOUS_VOLUMES='none'
    POSTGRES_IMAGE_DECLARED_VOLUME='/var/lib/postgresql/data'
    POSTGRES_IMAGE_VOLUME_OVERRIDE='EXPLICIT_TMPFS'
    POSTGRES_TLS_INITDB_CONFIG='PASS'
    POSTGRES_HBA_POLICY_ACTIVE='PASS'
    POSTGRES_CONTAINER_RESTARTS='0'
    POSTGRES_DATA_STORAGE='tmpfs'
    POSTGRES_TMPFS_MOUNTS='PASS'
    POSTGRES_HBA_POLICY='HOSTSSL_SCRAM_HOSTNOSSL_REJECT'
    POSTGRES_TLS='VERIFY_FULL'
    POSTGRES_MIGRATOR='ys_o2_migrator'
    POSTGRES_MIGRATOR_SUPERUSER='false'
    POSTGRES_RUNTIME='ys_o2_runtime'
    POSTGRES_RUNTIME_SUPERUSER='false'
    POSTGRES_RUNTIME_PRIVILEGES='OUTBOX_DML_ONLY'
    SQLITE_PATH_SCOPED='PASS'
    SQLITE_APPLY_ROLLBACK_REAPPLY='PASS'
    POSTGRES_DB_SCOPE_SENTINEL='PASS'
    POSTGRES_APPLY_ROLLBACK_REAPPLY='PASS'
    INTEGRITY='PASS'
    CONCURRENCY='PASS'
    PENDING_COALESCING='PASS'
    UNIQUE_PARTIAL_INDEX='PASS'
    FK_AND_ORPHAN_REJECTION='PASS'
    MIGRATOR_LEAST_PRIVILEGE='PASS'
    RUNTIME_DML_ONLY='PASS'
    TLS_HOSTNAME_AND_CA_NEGATIVES='PASS'
    SQLITE_DATABASE_CLEANUP='PASS'
    POSTGRES_CONTAINER_CLEANUP='PASS'
    POSTGRES_NETWORK_CLEANUP='PASS'
    TEMPORARY_SECRETS_CLEANUP='PASS'
    PUBLIC_ACCOUNTS='OFF'
    DISPATCHER='FAIL_CLOSED'
    SMTP='NOT_CONNECTED'
    CT164_DB='NOT_ACCESSED'
    PRODUCTION_DB='NOT_ACCESSED'
    GITHUB_ACTIONS='NOT_USED'
    RELEASE_GATE='HOLD_CT164_PENDING'
}
foreach ($Key in $Required.Keys) {
    Check ($Manifest.ContainsKey($Key) -and $Manifest[$Key] -ceq $Required[$Key]) ('MANIFEST_' + $Key)
}
$PgVersion = [int]$Manifest['POSTGRES_SERVER_VERSION_NUM']
Check ($PgVersion -ge 170000 -and $PgVersion -lt 180000) 'MANIFEST_POSTGRESQL_17'
Check ($Manifest['POSTGRES_CONTAINER'] -match '^ys-f2b-o2-pg17-[0-9a-f]{12}$') 'MANIFEST_EPHEMERAL_CONTAINER_NAME'
Check ($Manifest['POSTGRES_NETWORK'] -match '^ys-f2b-o2-[0-9a-f]{12}$') 'MANIFEST_EPHEMERAL_NETWORK_NAME'

$Stages = @(
    'o2-tool-restore','o2-ef-version','o2-runtime-restore','o2-runtime-build',
    'o2-runtime-linux-publish','o2-sqlite-model-diff','o2-postgresql-model-diff','o2-sqlite-review-sql',
    'o2-postgresql-review-sql','o2-sqlite-migration-contract',
    'o2-pg-network-create','o2-pg-container-create','o2-pg-entry-script-copy','o2-pg-init-script-copy',
    'o2-pg-container-start','o2-pg-init-script-receipts','o2-pg-cert-dir-owner',
    'o2-pg-cert-ca','o2-pg-cert-server-csr','o2-pg-cert-server-sign',
    'o2-pg-cert-wrong-ca','o2-pg-cert-permissions','o2-pg-cert-public-permissions',
    'o2-pg-cert-san-check','o2-pg-config-owner','o2-pg-config-permissions',
    'o2-pg-tls-configured','o2-pg-hba-policy-active','o2-pg-container-state',
    'o2-pg-server-version','o2-pg-config-paths','o2-pg-config-file-stat',
    'o2-pg-auto-conf-content','o2-pg-hba-content','o2-pg-setting-sources',
    'o2-pg-file-settings','o2-pg-startup-log','o2-pg-ssl-state',
    'o2-pg-root-ca-export','o2-pg-wrong-ca-export',
    'o2-pg-roles-sql-copy','o2-pg-roles-create',
    'o2-pg-roles-secret-cleanup','o2-pg-database-create',
    'o2-pg-permissions-sql-copy','o2-pg-permissions-apply',
    'o2-pg-permissions-secret-cleanup','o2-pg-runtime-copy',
    'o2-pg-runtime-script-copy','o2-pg-runtime-script-tmpfs-stage',
    'o2-pg-runtime-permissions',
    'o2-pg-runtime-executable','o2-pg-runtime-dependencies',
    'o2-pg-runtime-contract','o2-pg-runtime-cleanup'
)
foreach ($Stage in $Stages) {
    foreach ($Suffix in @('.runner.cmd','.stdout.log','.stderr.log')) {
        if (-not (Test-Path -LiteralPath (Join-Path $Evidence ($Stage + $Suffix)) -PathType Leaf)) {
            throw ('F2B_O2_STAGE_RECEIPT_MISSING=' + $Stage + $Suffix)
        }
    }
}
Check $true 'POSTFLIGHT_ALL_STAGE_RECEIPTS_PRESENT'

$InitReceipts = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-init-script-receipts.stdout.log'))
foreach ($Marker in @(
    'F2B_O2_PG_CERTIFICATES_CREATED=PASS',
    'F2B_O2_PG_HBA_INITDB_CONFIGURED=PASS',
    'F2B_O2_PG_TLS_INITDB_CONFIGURED=PASS'
)) {
    Check ($InitReceipts.Contains($Marker)) ('POSTFLIGHT_' + ($Marker.Split('=')[0]))
}
Check (([IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-tls-configured.stdout.log'))).Trim() -ceq 'PASS') 'POSTFLIGHT_TLS_CONFIGURED_AT_INITDB'
Check (([IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-hba-policy-active.stdout.log'))).Trim() -ceq 'PASS') 'POSTFLIGHT_HBA_POLICY_ACTIVE'
Check (([IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-container-state.stdout.log'))).Trim() -ceq '0') 'POSTFLIGHT_CONTAINER_NOT_RESTARTED'

$SqliteLog = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-sqlite-migration-contract.stdout.log'))
$PgLog = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-runtime-contract.stdout.log'))
foreach ($Marker in @(
    'F2B_O2_SQLITE_PATH_EVIDENCE_SCOPED=PASS',
    'F2B_O2_SQLITE_OUTBOX_ABSENT_AT_BASELINE=PASS',
    'F2B_O2_SQLITE_APPLY_ROLLBACK_REAPPLY=PASS',
    'F2B_O2_SQLITE_PENDING_INDEX_UNIQUE_PARTIAL=PASS',
    'F2B_O2_SQLITE_ORDINARY_INDEXES=PASS',
    'F2B_O2_SQLITE_INTEGRITY=PASS',
    'F2B_O2_SQLITE_CONCURRENCY=PASS',
    'F2B_O2_SQLITE_DISPOSABLE_ONLY=PASS'
)) {
    Check ($SqliteLog.Contains($Marker)) ('POSTFLIGHT_' + ($Marker.Split('=')[0]))
}
foreach ($Marker in @(
    'F2B_O2_PG_CLIENT_INTERNAL_NETWORK=PASS',
    'F2B_O2_PG_SERVER_VERSION_17_AND_DB_SCOPE=PASS',
    'F2B_O2_PG_DISPOSABLE_SENTINEL_MATCHED=PASS',
    'F2B_O2_PG_OUTBOX_ABSENT_AT_BASELINE=PASS',
    'F2B_O2_PG_APPLY_ROLLBACK_REAPPLY=PASS',
    'F2B_O2_PG_PENDING_INDEX_UNIQUE_PARTIAL=PASS',
    'F2B_O2_PG_ORDINARY_INDEXES=PASS',
    'F2B_O2_PG_MIGRATOR_NON_SUPERUSER=PASS',
    'F2B_O2_PG_RUNTIME_DML_ONLY_OUTBOX=PASS',
    'F2B_O2_PG_TLS_VERIFYFULL=PASS',
    'F2B_O2_PG_LEAST_PRIVILEGE=PASS',
    'F2B_O2_PG_INTEGRITY=PASS',
    'F2B_O2_PG_CONCURRENCY=PASS',
    'F2B_O2_PG_DISPOSABLE_ONLY=PASS'
)) {
    Check ($PgLog.Contains($Marker)) ('POSTFLIGHT_' + ($Marker.Split('=')[0]))
}

$Forbidden = @(
    'temporary-roles.sql','temporary-db-permissions.sql','temporary-wrong-ca.crt',
    'temporary-postgresql.auto.conf','temporary-pg_hba.conf',
    'temporary-f2b-o2-postgres-init.sh','temporary-f2b-o2-postgres-entrypoint.sh',
    'yangspace-f2b-o2-sqlite.db','yangspace-f2b-o2-sqlite.db-wal',
    'yangspace-f2b-o2-sqlite.db-shm','yangspace-f2b-o2-sqlite.db-journal'
)
foreach ($Name in $Forbidden) {
    Check (-not (Test-Path -LiteralPath (Join-Path $Evidence $Name))) ('POSTFLIGHT_NO_' + ($Name -replace '[^A-Za-z0-9]','_'))
}
$PrivateFiles = @(Get-ChildItem -LiteralPath $Evidence -Recurse -File |
    Where-Object { $_.Extension -in @('.key','.csr') })
Check ($PrivateFiles.Count -eq 0) 'POSTFLIGHT_NO_PRIVATE_KEYS_OR_CSR'
$RootCa = Join-Path $Evidence 'pg-root-ca.crt'
Check (Test-Path -LiteralPath $RootCa -PathType Leaf) 'POSTFLIGHT_PUBLIC_ROOT_CA_RETAINED'

$ChecksumLines = Get-Content -LiteralPath $ChecksumsPath
Check ($ChecksumLines.Count -ge 15) 'POSTFLIGHT_CHECKSUM_FILE_NONEMPTY'
foreach ($Line in $ChecksumLines) {
    if ($Line -notmatch '^([a-f0-9]{64})  (.+)$') { throw 'F2B_O2_CHECKSUM_LINE_INVALID' }
    $ExpectedHash = $Matches[1]
    $RelativePath = $Matches[2]
    if ([IO.Path]::IsPathRooted($RelativePath) -or $RelativePath -match '(^|[\\/])\.\.([\\/]|$)') {
        throw 'F2B_O2_CHECKSUM_PATH_UNSAFE'
    }
    if ($RelativePath -in @(
        'outbox.sqlite.review.sql','outbox.postgresql.review.sql',
        'YangSpace.Api.RuntimeTests.dll','pg-root-ca.crt'
    )) {
        $FullPath = Join-Path $Evidence $RelativePath
    }
    else {
        $FullPath = Join-Path $Repo $RelativePath
    }
    if (-not (Test-Path -LiteralPath $FullPath -PathType Leaf)) {
        throw ('F2B_O2_CHECKSUM_FILE_MISSING=' + $RelativePath)
    }
    $ActualHash = (Get-FileHash -LiteralPath $FullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($ActualHash -cne $ExpectedHash) { throw ('F2B_O2_CHECKSUM_MISMATCH=' + $RelativePath) }
}
Check $true 'POSTFLIGHT_ALL_SHA256_CHECKS_PASS'
Check ((Get-FileHash -LiteralPath (Join-Path $Evidence 'outbox.sqlite.review.sql') -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $Manifest['SQLITE_SQL_SHA256']) 'POSTFLIGHT_SQLITE_SQL_HASH'
Check ((Get-FileHash -LiteralPath (Join-Path $Evidence 'outbox.postgresql.review.sql') -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $Manifest['POSTGRES_SQL_SHA256']) 'POSTFLIGHT_POSTGRES_SQL_HASH'
Check ((Get-FileHash -LiteralPath $RootCa -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $Manifest['POSTGRES_ROOT_CA_SHA256']) 'POSTFLIGHT_ROOT_CA_HASH'

$DockerHostOverride = [Environment]::GetEnvironmentVariable('DOCKER_HOST','Process')
if ($DockerHostOverride -and $DockerHostOverride -notlike 'npipe://*') { throw 'F2B_O2_POSTFLIGHT_REMOTE_DOCKER_REFUSED' }
$DockerEndpoint = @(& $Docker context inspect --format '{{.Endpoints.docker.Host}}' 2>&1)
if ($LASTEXITCODE -ne 0 -or ($DockerEndpoint -join '').Trim() -notlike 'npipe://*') {
    throw 'F2B_O2_POSTFLIGHT_DOCKER_NOT_LOCAL'
}
$ContainerCheck = @(& $Docker inspect $Manifest['POSTGRES_CONTAINER'] 2>&1)
if ($LASTEXITCODE -eq 0) { throw 'F2B_O2_POSTFLIGHT_CONTAINER_REMAINS' }
$NetworkCheck = @(& $Docker network inspect $Manifest['POSTGRES_NETWORK'] 2>&1)
if ($LASTEXITCODE -eq 0) { throw 'F2B_O2_POSTFLIGHT_NETWORK_REMAINS' }
Check $true 'POSTFLIGHT_DOCKER_CONTAINER_AND_NETWORK_REMOVED'

$Postflight = @(
    "SOURCE_SHA=$CommitSha",'EXACT_SOURCE=PASS','SQLITE=PASS','POSTGRESQL_17=PASS',
    'TLS_VERIFYFULL=PASS','MIGRATOR_NON_SUPERUSER=PASS','RUNTIME_OUTBOX_DML_ONLY=PASS',
    'INTEGRITY=PASS','CONCURRENCY=PASS','SHA256=PASS','TEMPORARY_SECRETS=REMOVED',
    'DOCKER_CONTAINER=REMOVED','DOCKER_NETWORK=REMOVED','CT164_DB=NOT_ACCESSED',
    'PRODUCTION_DB=NOT_ACCESSED','SMTP=NOT_CONNECTED','RELEASE_GATE=HOLD_CT164_PENDING'
)
[IO.File]::WriteAllLines((Join-Path $Evidence 'postflight.txt'),[string[]]$Postflight,$Utf8NoBom)
$DirtyAfter = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $DirtyAfter.Count -ne 0) { throw 'F2B_O2_POSTFLIGHT_CREATED_SOURCE_CHANGES' }
Write-Host 'F2B_O2_POSTFLIGHT=PASS'
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "EVIDENCE_DIR=$Evidence"
Write-Host ('MANIFEST_SHA256=' + (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant())
