# Independent H04 application Identity postflight; evidence-only, no database connections.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$CommitSha,
    [Parameter(Mandatory=$true)][ValidateNotNullOrEmpty()][string]$EvidenceDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch='feat/yangspace-f2b-h04-identity-edge-matrix-20261117'
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
$ActiveBranch=GitValue @('branch','--show-current')
Check ($ActiveBranch -eq '' -or $ActiveBranch -ceq $Branch) 'POSTFLIGHT_BRANCH_OR_DETACHED_WORKTREE'
$Dirty=@(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
Check ($LASTEXITCODE -eq 0 -and $Dirty.Count -eq 0) 'POSTFLIGHT_CLEAN_SOURCE'
$Evidence=(Resolve-Path -LiteralPath $EvidenceDirectory).Path
$Root=[IO.Path]::GetFullPath((Join-Path $Repo 'logs/yangspace/f2b-o4-h04-identity-edge'))
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
    'POSTGRES_WORKER_PASSWORD_RESET=PASS','POSTGRES_WORKER_UNCERTAIN_ACK=PASS',
    'POSTGRES_WORKER_CRASH_RECOVERY=PASS','POSTGRES_WORKER_DEADLETTER_REPLAY=PASS',
    'POSTGRES_WORKER_RETENTION=PASS','POSTGRES_WORKER_NO_SENSITIVE_OUTBOX_FIELDS=PASS',
    'SQLITE_REGRESSION=PASS','H02_ENQUEUE_SYNTHETIC=PASS','FAKE_TRANSPORT=PASS',
    'H01_REAL_PRODUCTION_TESTSERVER_DEFAULT_OFF=PASS',
    'H01_PRODUCTION_SCHEMA_GATE=PASS',
    'H01_TLS_HOSTNAME_NEGATIVE=PASS',
    'H01_LEAST_PRIVILEGE_NEGATIVE=PASS',
    'H01_PUBLIC_STARTUP_FAIL_CLOSED=PASS',
    'H01_ENABLED_PUBLIC_HTTP=BLOCKED_BY_SIGNED_AUTHORIZATION',
    'H02_RELEASE_AUTHORIZATION=EPHEMERAL_TEST_ONLY',
    'H02_AUTHORIZATION_MAX_LIFETIME=1_HOUR',
    'H02_AUTHORIZATION_STARTUP_NEGATIVE_MATRIX=PASS',
    'H02_IDENTITY_RUNTIME_ROLE=ys_h02_identity',
    'H02_IDENTITY_RUNTIME_LEAST_PRIVILEGE=PASS',
    'H03_APPLICATION_ABUSE_PROTECTION=PASS_TESTSERVER_AND_DISPOSABLE_SQLITE_POSTGRES',
    'H03_ACCOUNT_KEY=HMAC_SHA256_ONLY',
    'H03_RAW_EMAIL_COUNTER_STORAGE=PROHIBITED',
    'H04_IDENTITY_EDGE_MATRIX=PASS_PRODUCTION_TESTSERVER_PG17',
    'H02_PRODUCTION_HTTP=PASS_TESTSERVER_ONLY',
    'H02_FAKE_TRANSPORT=PASS',
    'H02_NO_REAL_SMTP=PASS',
    'H02_NO_CT164=PASS',
    'H02_NO_PRODUCTION_DB=PASS',
    'H02_SOURCE_DEFAULTS_OFF=PASS',
    'NO_HOST_PORTS=PASS','NO_HOST_BINDS=PASS','TMPFS_ONLY=PASS',
    'CONTAINER_REMOVED=PASS','NETWORK_REMOVED=PASS',
    'TEMPORARY_SECRETS_REMOVED=PASS','CLEAN_SOURCE=PASS',
    'PUBLIC_REGISTRATION=OFF','OUTBOX_DISPATCH=OFF',
    'SMTP=NOT_CONNECTED','CT164_DB=NOT_ACCESSED','PRODUCTION_DB=NOT_ACCESSED',
    'GITHUB_ACTIONS=NOT_USED','RELEASE_GATE=HOLD_H04_REVIEW_AND_H05_H06'
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
    'o3-restore','o3-build','o3-sqlite-regression','h02-enqueue-synthetic','o3-publish-linux',
    'o3-network-create','o3-container-create','o3-container-start',
    'o3-pg-version','o3-pg-ssl','o3-pg-hba','o3-ca-export',
    'o3-roles-apply','o3-db-create','o3-grants-apply',
    'o3-runtime-copy','o3-runtime-chmod','o3-pg-worker',
    'o4-h01-root-mkdir','o4-h01-root-copy','o4-h01-root-verify',
    'o4-h01-runner-copy','o4-h01-runner-mode','o4-h01-testserver',
    'h02-release-auth-contract','h03-abuse-sqlite','h03-auth-rate-http',
    'h02-identity-runner-copy','h02-identity-runner-mode','h02-production-http','o3-runtime-clean'
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
    'F2B_O3_WORKER_SIGNED_RELEASE_AUTHORIZATION',
    'F2B_O3_WORKER_SQLITE_SYNTHETIC_ONLY'
)) { Marker 'o3-sqlite-regression' $M }
foreach ($M in @(
    'F2B_OUTBOX_EMAIL_INTENT_PERSISTED','F2B_OUTBOX_MINIMAL_PENDING_INTENT',
    'F2B_OUTBOX_PENDING_COALESCED','F2B_OUTBOX_PENDING_RETRY_EXPEDITED',
    'F2B_OUTBOX_EXPIRED_PENDING_REUSED','F2B_OUTBOX_RESET_INTENT_PERSISTED',
    'F2B_OUTBOX_NO_EMAIL_PAYLOAD_STORED','F2B_OUTBOX_DISPATCH_IMPLEMENTATION_AVAILABLE',
    'F2B_OUTBOX_ATOMIC_ROLLBACK','F2B_OUTBOX_CONCURRENT_ENQUEUE_COALESCED',
    'F2B_OUTBOX_PENDING_UNIQUE_INDEX','F2B_OUTBOX_LEASED_HISTORY_DOES_NOT_BLOCK_PENDING',
    'F2B_OUTBOX_CONCURRENT_EXPIRED_REFRESH','F2B_OUTBOX_ENQUEUE_SYNTHETIC_ONLY',
    'F2B_OUTBOX_SQLITE_CONCURRENCY'
)) { Marker 'h02-enqueue-synthetic' $M }
foreach ($M in @(
    'F2B_H03_ABUSE_HMAC_KEY_VALIDATED',
    'F2B_H03_ABUSE_MISSING_HMAC_KEY_FAILS_CLOSED',
    'F2B_H03_ABUSE_SHORT_HMAC_KEY_FAILS_CLOSED',
    'F2B_H03_ABUSE_SQLITE_MIGRATIONS_APPLIED',
    'F2B_H03_ABUSE_SQLITE_SCHEMA_GATE',
    'F2B_H03_ABUSE_RESEND_FIRST_ALLOWED',
    'F2B_H03_ABUSE_NORMALIZED_EMAIL_SHARED_BUCKET',
    'F2B_H03_ABUSE_THIRD_ALLOWED',
    'F2B_H03_ABUSE_RESEND_LIMIT_ENFORCED',
    'F2B_H03_ABUSE_OPERATION_BUCKET_ISOLATION',
    'F2B_H03_ABUSE_ACCOUNT_BUCKET_ISOLATION',
    'F2B_H03_ABUSE_COUNTER_PERSISTED',
    'F2B_H03_ABUSE_NO_RAW_EMAIL_AT_REST',
    'F2B_H03_ABUSE_WINDOW_EXPIRY_RECOVERY',
    'F2B_H03_ABUSE_ATOMIC_CONCURRENT_BUDGET',
    'F2B_H03_ABUSE_RETENTION_NEW_WINDOW_ALLOWED',
    'F2B_H03_ABUSE_EXPIRED_COUNTERS_PURGED',
    'F2B_H03_ABUSE_SQLITE_RUNTIME',
    'F2B_H03_ABUSE_CROSS_INSTANCE_ATOMIC_UPSERT',
    'F2B_H03_ABUSE_RETENTION'
)) { Marker 'h03-abuse-sqlite' $M }
foreach ($M in @(
    'F2B_H03_AUTH_RATE_HTTP_429_AFTER_TWELVE',
    'F2B_H03_AUTH_RATE_CLIENT_B_INDEPENDENT_BUDGET',
    'F2B_H03_AUTH_RATE_MAPPED_IPV4_SAME_BUCKET',
    'F2B_H03_AUTH_RATE_UNKNOWN_PEER_LIMITED',
    'F2B_H03_AUTH_RATE_REAL_MIDDLEWARE',
    'F2B_H03_AUTH_RATE_FORWARDED_HEADER_NOT_TRUSTED',
    'F2B_H03_AUTH_RATE_TESTSERVER_ONLY'
)) { Marker 'h03-auth-rate-http' $M }
foreach ($M in @(
    'F2B_O3_PG_MIGRATIONS_APPLIED','F2B_O3_PG_MIGRATOR_TLS_SCOPE',
    'F2B_O3_PG_RUNTIME_LEAST_PRIVILEGE','F2B_O3_PG_RUNTIME_DENIED_OPERATIONS',
    'F2B_O3_PG_RUNTIME_TLS_SCOPE','F2B_O3_PG_DISPATCH_IMPLEMENTATION_AVAILABLE',
    'F2B_O3_PG_DI_ACTIVATION','F2B_O3_PG_FAKE_TRANSPORT_ONLY',
    'F2B_O3_PG_SIGNED_RELEASE_AUTHORIZATION',
    'F2B_O3_PG_CONFIRMATION_DISPATCH','F2B_O3_PG_RETRY_BACKOFF',
    'F2B_O3_PG_RETRY_RECOVERY','F2B_O3_PG_CONCURRENT_CLAIM',
    'F2B_O3_PG_LEASE_EXPIRY_RECOVERY','F2B_O3_PG_PASSWORD_RESET',
    'F2B_O3_PG_CONFIRMED_USER_SUPPRESSION','F2B_O3_PG_UNCERTAIN_ACK_PENDING_RETRY',
    'F2B_O3_PG_UNCERTAIN_ACK_AT_LEAST_ONCE','F2B_O3_PG_CRASH_LEASE_PRESERVED',
    'F2B_O3_PG_CRASH_RESTART_RECOVERY','F2B_O3_PG_DEADLETTER',
    'F2B_O3_PG_OPERATOR_REPLAY','F2B_O3_PG_RETENTION_PURGE',
    'F2B_O3_PG_NO_TOKEN_OR_RECIPIENT_AT_REST',
    'F2B_O3_WORKER_NPGSQL_PROVIDER','F2B_O3_WORKER_POSTGRESQL_17',
    'F2B_O3_WORKER_TLS_VERIFYFULL','F2B_O3_WORKER_RUNTIME_LEAST_PRIVILEGE',
    'F2B_O3_WORKER_FAKE_TRANSPORT_ONLY','F2B_O3_WORKER_DISPOSABLE_ONLY'
)) { Marker 'o3-pg-worker' $M }
foreach ($M in @(
    'F2B_H02_AUTH_VALID_SIGNATURE','F2B_H02_AUTH_MISSING_REFUSED',
    'F2B_H02_AUTH_BAD_SIGNATURE_REFUSED','F2B_H02_AUTH_EXPIRED_REFUSED',
    'F2B_H02_AUTH_FUTURE_ISSUED_REFUSED','F2B_H02_AUTH_ONE_HOUR_BOUNDARY_ACCEPTED',
    'F2B_H02_AUTH_OVER_ONE_HOUR_REFUSED',
    'F2B_H02_AUTH_WRONG_AUDIENCE_REFUSED','F2B_H02_AUTH_WRONG_SCOPE_REFUSED',
    'F2B_H02_AUTH_WRONG_ENVIRONMENT_REFUSED','F2B_H02_AUTH_WRONG_COMMIT_REFUSED',
    'F2B_H02_AUTH_NULL_SOURCE_COMMIT_REFUSED','F2B_H02_AUTH_NULL_APPROVAL_ID_REFUSED',
    'F2B_H02_AUTH_WRONG_KEY_ID_REFUSED','F2B_H02_AUTH_BAD_PUBLIC_KEY_REFUSED',
    'F2B_H02_AUTH_DEPLOYMENT_SHA_BINDING','F2B_H02_AUTH_PRODUCTION_ONLY',
    'F2B_H02_AUTH_DEFAULT_SWITCHES_OFF','F2B_H02_AUTH_IMPLEMENTATION_AVAILABLE_AFTER_O3',
    'F2B_H02_SIGNED_RELEASE_AUTHORIZATION_CONTRACT'
)) { Marker 'h02-release-auth-contract' $M }
foreach ($M in @(
        'F2B_O4_H01_RUNTIME_TLS_SCOPE_NON_ELEVATED',
        'F2B_O4_H01_RUNTIME_METADATA_READ_ONLY',
        'F2B_O4_H01_PRODUCTION_PG_POLICY',
        'F2B_O4_H01_REAL_PRODUCTION_ENVIRONMENT',
        'F2B_O4_H01_PRODUCTION_HTTP_LIVE',
        'F2B_O4_H01_PRODUCTION_HTTP_READY',
        'F2B_O4_H01_DISABLED_PUBLIC_REGISTER_404',
        'F2B_O4_H01_DISABLED_LEGACY_REGISTER_404',
        'F2B_O4_H01_REJECTS_DISABLED_TLS',
        'F2B_O4_H01_REJECTS_TLS_HOSTNAME_MISMATCH',
        'F2B_O4_H01_REJECTS_MIGRATOR_AS_RUNTIME',
        'F2B_O4_H01_REJECTS_MISSING_RELEASE_AUTHORIZATION',
        'F2B_O4_H01_REJECTS_BAD_RELEASE_SIGNATURE',
        'F2B_O4_H01_REJECTS_EXPIRED_RELEASE_AUTHORIZATION',
        'F2B_O4_H01_REJECTS_WRONG_RELEASE_COMMIT',
        'F2B_O4_H01_REJECTS_PUBLIC_WITH_DISPATCH_OFF',
        'F2B_O4_H01_REJECTS_NONPRODUCTION_PUBLIC_ENABLE',
        'F2B_O4_H01_REAL_TESTSERVER_DEFAULT_OFF',
        'F2B_O4_H01_DISPOSABLE_PG_TLS_VERIFYFULL',
        'F2B_O4_H01_PRODUCTION_GATES_FAIL_CLOSED'
)) { Marker 'o4-h01-testserver' $M }
$H01Log=[IO.File]::ReadAllText((Join-Path $Evidence 'o4-h01-testserver.stdout.log'))
Check ($H01Log -match '(?m)^F2B_O4_H01_ENABLED_PUBLIC_HTTP=BLOCKED_BY_SIGNED_AUTHORIZATION\s*$') 'POSTFLIGHT_H01_PUBLIC_RELEASE_HELD'
Check ($H01Log -match '(?m)^F2B_O4_H01_SMTP=NOT_CONNECTED\s*$') 'POSTFLIGHT_H01_SMTP_DISCONNECTED'
foreach ($M in @(
    'F2B_H03_ABUSE_STORAGE_PRIVILEGE_FAIL_CLOSED',
    'F2B_H03_ABUSE_STORAGE_PRIVILEGES_RESTORED',
    'F2B_H03_REGISTER_ACCOUNT_BUDGET',
    'F2B_H03_RESEND_ACCOUNT_BUDGET',
    'F2B_H03_POSTGRES_ATOMIC_CROSS_PEER_COUNTER',
    'F2B_H03_FORGOT_ACCOUNT_BUDGET',
    'F2B_H03_RESET_ACCOUNT_BUDGET',
    'F2B_H03_IDENTITY_FIVE_FAILURE_LOCKOUT',
    'F2B_H03_IDENTITY_LOCKOUT_NOT_BYPASSED_BY_PEER',
    'F2B_H03_IDENTITY_LOCKOUT_EXPIRY_RECOVERY'
)) { Marker 'h02-production-http' $M }
foreach ($M in @(
    'F2B_H02_QA_SCOPE_BOUND',
    'F2B_H02_IDENTITY_RUNTIME_LEAST_PRIVILEGE',
    'F2B_H02_TEST_PEER_PARTITIONING',
    'F2B_H02_EARLY_STARTUP_CONFIGURATION',
    'F2B_H02_POSTGRES_ENUM_TEXT_MAPPING',
    'F2B_H02_REAL_PRODUCTION_ENVIRONMENT',
    'F2B_H02_FAKE_TRANSPORT_ONLY',
    'F2B_H02_RELEASE_AUTHORIZATION_VALID',
    'F2B_H02_PRODUCTION_LIVE_READY',
    'F2B_H02_REGISTER_ACCEPTED_WITH_TRANSPORT_FAILURE',
    'F2B_H02_REGISTER_IDENTITY_AND_OUTBOX_ATOMIC',
    'F2B_H02_RESEND_UNKNOWN_GENERIC','F2B_H02_RESEND_CONFIRMATION_ACCEPTED',
    'F2B_H02_RESEND_CONFIRMATION_RELEASES_RETRY',
    'F2B_H02_CONFIRMATION_LINK_CANONICAL_HTTPS',
    'F2B_H02_CONFIRMATION_TOKEN_DELIVERED',
    'F2B_H02_EMAIL_CONFIRMATION_HTTP',
    'F2B_H02_CONFIRMATION_REPLAY_REJECTED',
    'F2B_H04_CONFIRMATION_RESPONSE_SECURITY_HEADERS',
    'F2B_H04_CONFIRM_MALFORMED_TOKEN_FAIL_CLOSED',
    'F2B_H04_CONFIRM_UNKNOWN_USER_FAIL_CLOSED',
    'F2B_H02_CONCURRENT_CONFIRMATION_SINGLE_WINNER',
    'F2B_H02_EXPIRED_CONFIRMATION_TOKEN_REJECTED',
    'F2B_H02_EXPIRED_RESET_TOKEN_REJECTED',
    'F2B_H02_EXPIRED_RESET_PRESERVES_PASSWORD',
    'F2B_H02_DUPLICATE_REGISTRATION_GENERIC',
    'F2B_H04_REGISTER_INVALID_EMAIL_NO_ACCOUNT',
    'F2B_H04_REGISTER_WEAK_PASSWORD_NO_ACCOUNT',
    'F2B_H04_DUPLICATE_REGISTRATION_BODY_GENERIC',
    'F2B_H04_STAFF_MULTIPLE_ROLES_CREATED',
    'F2B_H04_STAFF_ANY_ROLE_EXCLUDED_FROM_ALL_PUBLIC_ACTIONS',
    'F2B_H04_RESET_TOKEN_REPLAY_REJECTED',
    'F2B_H04_RESET_REPLAY_DOES_NOT_CHANGE_PASSWORD',
    'F2B_H04_RESET_MALFORMED_TOKEN_REJECTED',
    'F2B_H04_RESET_MALFORMED_TOKEN_PRESERVES_PASSWORD',
    'F2B_H02_LOGIN_HTTP','F2B_H02_LOGIN_NO_SET_COOKIE','F2B_H02_LOGIN_BEARER_ONLY',
    'F2B_H02_SESSION_REQUIRES_BEARER','F2B_H02_SESSION_BEARER_OK',
    'F2B_H02_STAFF_ROLE_FIXTURES_CREATED','F2B_H02_STAFF_CONFIRMATION_DENIED',
    'F2B_H02_STAFF_RESEND_NO_OUTBOX','F2B_H02_STAFF_LOGIN_DENIED',
    'F2B_H02_STAFF_FORGOT_NO_OUTBOX','F2B_H02_STAFF_RESET_DENIED',
    'F2B_H02_FORGOT_PASSWORD_ACCEPTED','F2B_H02_FORGOT_PASSWORD_UNKNOWN_GENERIC',
    'F2B_H02_PASSWORD_RESET_HTTP',
    'F2B_H02_REGISTER_ENQUEUE_FAILURE_HTTP_500','F2B_H02_REGISTER_ENQUEUE_FAILURE_ROLLBACK',
    'F2B_H02_OLD_PASSWORD_REVOKED','F2B_H02_FAKE_CONFIRMATION_AND_RESET_ONLY',
    'F2B_H02_NO_COOKIE_CLIENT_FALLBACK','F2B_H02_NO_REAL_SMTP',
    'F2B_H02_NO_CT164_OR_PRODUCTION_DB','F2B_H02_IDENTITY_HTTP_FAKE_TRANSPORT'
)) { Marker 'h02-production-http' $M }
foreach ($Temp in @('temporary-init.sh','temporary-entry.sh','temporary-roles.sql','temporary-grants.sql','temporary-run.sh','temporary-o4-h01-run.sh','temporary-h02-identity-run.sh')) {
    Check (-not (Test-Path -LiteralPath (Join-Path $Evidence $Temp))) ('POSTFLIGHT_NO_'+($Temp -replace '[^a-zA-Z0-9]','_'))
}
Check (@(Get-ChildItem -LiteralPath $Evidence -File | Where-Object { $_.Extension -in @('.key','.p12','.pfx') }).Count -eq 0) 'POSTFLIGHT_NO_PRIVATE_KEYS'

$HashLines=[IO.File]::ReadAllLines($ChecksumsPath)
Check ($HashLines.Count -ge 20) 'POSTFLIGHT_CHECKSUMS_COUNT'
foreach ($Binary in @('YangSpace.Api.RuntimeTests.linux-x64','YangSpace.Api.RuntimeTests.dll')) {
    if ($Binary -eq 'YangSpace.Api.RuntimeTests.linux-x64') {
        $Prefix='APPHOST_SHA256='
    } else {
        $Prefix='DLL_SHA256='
    }
    $Artifact=Join-Path $Evidence $Binary
    Check (Test-Path -LiteralPath $Artifact -PathType Leaf) ('POSTFLIGHT_BINARY_'+$Binary)
    $ManifestHash=@($Lines | Where-Object { $_.StartsWith($Prefix,[StringComparison]::Ordinal) })
    Check ($ManifestHash.Count -eq 1) ('POSTFLIGHT_BINARY_MANIFEST_'+$Binary)
    Check ((Get-FileHash -LiteralPath $Artifact -Algorithm SHA256).Hash.ToLowerInvariant() -ceq
        $ManifestHash[0].Substring($Prefix.Length)) ('POSTFLIGHT_BINARY_HASH_'+$Binary)
}
foreach ($HashLine in $HashLines) {
    if ($HashLine -cnotmatch '^([0-9a-f]{64})  ([a-zA-Z0-9._/-]+)$') { throw 'F2B_O3_BAD_CHECKSUM_LINE' }
    $ExpectedHash=$Matches[1]
    $Relative=$Matches[2]
    if ($Relative.Contains('..')) { throw 'F2B_O3_BAD_CHECKSUM_PATH' }
    if ($Relative.StartsWith('apps/') -or $Relative.StartsWith('scripts/') -or $Relative.StartsWith('docs/')) {
        $Full=Join-Path $Repo $Relative
    } elseif ($Relative -in @(
        'pg-root-ca.crt','o3-sqlite-regression.stdout.log','h02-enqueue-synthetic.stdout.log','o3-pg-worker.stdout.log',
        'h02-release-auth-contract.stdout.log','h03-abuse-sqlite.stdout.log','h03-auth-rate-http.stdout.log',
        'o4-h01-testserver.stdout.log','h02-production-http.stdout.log',
        'YangSpace.Api.RuntimeTests.linux-x64','YangSpace.Api.RuntimeTests.dll'
    )) {
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
Write-Host 'F2B_O4_H01_INDEPENDENT_POSTFLIGHT=PASS'
Write-Host 'F2B_H02_INDEPENDENT_POSTFLIGHT=PASS'
Write-Host 'F2B_H03_INDEPENDENT_POSTFLIGHT=PASS'
Write-Host 'F2B_H04_INDEPENDENT_POSTFLIGHT=PASS'
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "EVIDENCE_DIR=$Evidence"
Write-Host ('MANIFEST_SHA256='+(Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant())
Write-Host 'RELEASE_GATE=HOLD_H04_REVIEW_AND_H05_H06'
