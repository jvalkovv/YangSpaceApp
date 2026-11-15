# F2b O2: exact-SHA disposable dual-provider migration, integrity and race QA.
# One local PostgreSQL 17 container on an internal Docker network; PGDATA and keys
# live on tmpfs. Only a loopback host port is published. No bind mounts/volumes,
# no image pulls, no CT164/production access, no SMTP and no GitHub Actions.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'fix/yangspace-f2b-o2-race-safe-20261115'
$RuntimeProject = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$SqliteProject = 'apps/api/YangSpace.Api.csproj'
$PostgreSqlProject = 'apps/api.Migrations.PostgreSql/YangSpace.PostgreSql.Migrations.csproj'
$SqliteBaseline = '20261114183515_F2aOrganizationClaimFoundation'
$SqliteTarget = '20261115101409_F2bIdentityEmailOutbox'
$PostgreSqlBaseline = '20261111203833_InitialPostgreSqlSchema'
$PostgreSqlTarget = '20261115101421_F2bIdentityEmailOutbox'
$Database = 'yangspace_f2b_o2_disposable'
$MigratorRole = 'ys_o2_migrator'
$RuntimeRole = 'ys_o2_runtime'
$PgImage = 'postgres:17'

$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$Docker = (Get-Command docker.exe -ErrorAction Stop).Source
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Check([bool]$Condition, [string]$Marker) {
    if (-not $Condition) { throw ('F2B_O2_PREFLIGHT_FAILED=' + $Marker) }
    Write-Host ($Marker + '=PASS')
}

function GitValue {
    param([Parameter(Mandatory=$true)][string[]]$CommandArgs)
    $Output = @(& $Git -C $Repo @CommandArgs)
    if ($LASTEXITCODE -ne 0) {
        throw ('F2B_O2_GIT_FAILED=' + ($CommandArgs -join ' '))
    }
    return ($Output -join '').Trim()
}

function NewHexSecret {
    $Bytes = New-Object byte[] 32
    $Generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $Generator.GetBytes($Bytes) }
    finally { $Generator.Dispose() }
    return ([BitConverter]::ToString($Bytes).Replace('-', '').ToLowerInvariant())
}

function Stage {
    param(
        [Parameter(Mandatory=$true)][string]$Label,
        [Parameter(Mandatory=$true)][string]$Executable,
        [Parameter(Mandatory=$true)][string[]]$Arguments,
        [ValidateRange(1,3600)][int]$TimeoutSeconds = 900
    )
    Invoke-YangSpaceStage -Label $Label -Executable $Executable -CommandArguments $Arguments -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds $TimeoutSeconds
}

function RequireMarker([string]$Label, [string]$Marker) {
    $Log = Join-Path $Evidence ($Label + '.stdout.log')
    if (-not (Test-Path -LiteralPath $Log -PathType Leaf)) {
        throw ('F2B_O2_MARKER_LOG_MISSING=' + $Label)
    }
    if ([IO.File]::ReadAllText($Log) -notmatch
        ('(?m)^' + [regex]::Escape($Marker) + '\s*$')) {
        throw ('F2B_O2_MARKER_MISSING=' + $Marker)
    }
}

function WaitForPostgreSql([string]$ContainerName) {
    $Ready = $false
    for ($Try = 0; $Try -lt 60; $Try++) {
        $Previous = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $Probe = @(& $Docker exec $ContainerName pg_isready -U postgres -d postgres 2>&1)
            $ProbeExit = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $Previous }
        if ($ProbeExit -eq 0) { $Ready = $true; break }
        Start-Sleep -Seconds 2
    }
    if (-not $Ready) { throw 'F2B_O2_PG_SERVER_NOT_READY' }
    Write-Host 'F2B_O2_PG_SERVER_READY=PASS'
}

function GetDockerValue {
    param([Parameter(Mandatory=$true)][string[]]$Arguments)
    $Output = @(& $Docker @Arguments)
    if ($LASTEXITCODE -ne 0) {
        throw ('F2B_O2_DOCKER_QUERY_FAILED=' + ($Arguments -join ' '))
    }
    return ($Output -join '').Trim()
}

# PowerShell can treat a root JSON array as one pipeline object. Wrapping
# the array in an object and reading its property preserves individual entries
# consistently across Windows PowerShell and PowerShell 7.
$JsonArrayProbe = ConvertFrom-Json -InputObject '{"items":[{"id":1},{"id":2}]}'
$JsonArrayProbeItems = @($JsonArrayProbe.items)
Check (
    $JsonArrayProbeItems.Count -eq 2 -and
    [int]$JsonArrayProbeItems[0].id -eq 1 -and
    [int]$JsonArrayProbeItems[1].id -eq 2
) 'F2B_O2_POWERSHELL_JSON_ARRAY_PRESERVATION'

if ((GitValue -CommandArgs @('rev-parse','HEAD')) -cne $CommitSha) {
    throw 'F2B_O2_EXACT_HEAD_MISMATCH'
}
if ((GitValue -CommandArgs @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'F2B_O2_REMOTE_BRANCH_MISMATCH'
}
$CurrentBranch = GitValue -CommandArgs @('branch','--show-current')
if ($CurrentBranch -and $CurrentBranch -cne $Branch) {
    throw 'F2B_O2_WORKTREE_BRANCH_MISMATCH'
}
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) {
    throw 'F2B_O2_WORKTREE_NOT_CLEAN'
}

foreach ($Path in @(
    $RuntimeProject, $SqliteProject, $PostgreSqlProject,
    'apps/api.RuntimeTests/Program.cs',
    'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj',
    'apps/api.RuntimeTests/F2bOutboxO2MigrationContract.cs',
    'apps/api/Migrations/20261115101409_F2bIdentityEmailOutbox.cs',
    'apps/api.Migrations.PostgreSql/Migrations/20261115101421_F2bIdentityEmailOutbox.cs',
    'scripts/Invoke-YangSpaceStage.ps1'
)) {
    Check (Test-Path -LiteralPath (Join-Path $Repo $Path) -PathType Leaf) ('SOURCE_FILE_' + (Split-Path $Path -Leaf))
}

$AppConfig = Get-Content -LiteralPath (Join-Path $Repo 'apps/api/appsettings.json') -Raw |
    ConvertFrom-Json
Check ($AppConfig.PublicAccounts.Enabled -eq $false) 'PUBLIC_ACCOUNTS_DEFAULT_OFF'
Check ($AppConfig.Preview.Enabled -eq $false) 'PREVIEW_DEFAULT_OFF'
Check ($AppConfig.Admin.Enabled -eq $false) 'ADMIN_DEFAULT_OFF'

$DockerHostOverride = [Environment]::GetEnvironmentVariable('DOCKER_HOST','Process')
if ($DockerHostOverride -and $DockerHostOverride -notlike 'npipe://*') {
    throw 'F2B_O2_REMOTE_DOCKER_HOST_REFUSED'
}
$DockerEndpoint = GetDockerValue -Arguments @('context','inspect','--format','{{.Endpoints.docker.Host}}')
if ($DockerEndpoint -notlike 'npipe://*') { throw 'F2B_O2_DOCKER_CONTEXT_NOT_LOCAL' }
$DockerOs = GetDockerValue -Arguments @('info','--format','{{.OSType}}')
if ($DockerOs -cne 'linux') { throw 'F2B_O2_LOCAL_LINUX_DOCKER_REQUIRED' }
$DockerVersion = GetDockerValue -Arguments @('info','--format','{{.ServerVersion}}')
$PgImageId = GetDockerValue -Arguments @('image','inspect','--format','{{.Id}}',$PgImage)
if ($PgImageId -notmatch '^sha256:[0-9a-f]{64}$') {
    throw 'F2B_O2_POSTGRES_17_IMAGE_NOT_PRESENT_NO_AUTO_PULL'
}
Write-Host 'F2B_O2_LOCAL_DOCKER_AND_EXISTING_PG17_IMAGE=PASS'

$RunToken = NewHexSecret
$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + $RunToken.Substring(0,8)
$EvidenceRoot = Join-Path $Repo 'logs/yangspace/f2b-o2'
$Evidence = Join-Path $EvidenceRoot ($CommitSha.Substring(0,12) + '-' + $RunId)
$RuntimeWork = Join-Path ([IO.Path]::GetTempPath()) ('yangspace-f2b-o2-runtime-' + $RunToken.Substring(0,12))
$RuntimePublishDirectory = Join-Path $RuntimeWork 'publish'
$RuntimeScriptPath = Join-Path $RuntimeWork 'run-contract.sh'
$RuntimeAppHost = Join-Path $RuntimePublishDirectory 'YangSpace.Api.RuntimeTests'
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null

. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')

$SqliteSql = Join-Path $Evidence 'outbox.sqlite.review.sql'
$PostgreSqlSql = Join-Path $Evidence 'outbox.postgresql.review.sql'
$RuntimeDll = Join-Path $Repo 'apps/api.RuntimeTests/bin/Release/net10.0/YangSpace.Api.RuntimeTests.dll'
$RuntimeDllEvidence = Join-Path $Evidence 'YangSpace.Api.RuntimeTests.dll'
$SqliteDb = Join-Path $Evidence 'yangspace-f2b-o2-sqlite.db'
$RootCaPath = Join-Path $Evidence 'pg-root-ca.crt'
$WrongCaPath = Join-Path $Evidence 'temporary-wrong-ca.crt'
$RoleSql = Join-Path $Evidence 'temporary-roles.sql'
$PermissionSql = Join-Path $Evidence 'temporary-db-permissions.sql'
$TlsConfig = Join-Path $Evidence 'temporary-postgresql.auto.conf'
$HbaConfig = Join-Path $Evidence 'temporary-pg_hba.conf'
$TlsInitScript = Join-Path $Evidence 'temporary-f2b-o2-postgres-init.sh'
$TlsEntryScript = Join-Path $Evidence 'temporary-f2b-o2-postgres-entrypoint.sh'

$Network = 'ys-f2b-o2-' + $RunToken.Substring(0,12)
$Container = 'ys-f2b-o2-pg17-' + $RunToken.Substring(0,12)
$NetworkCreated = $false
$ContainerAttempted = $false
$Completed = $false
$CleanupOk = $true
$PgVersionNum = ''
$MigratorPassword = NewHexSecret
$RuntimePassword = NewHexSecret
$ScopeToken = NewHexSecret
$OldDesignTime = [Environment]::GetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME','Process')
$OldPostgreSqlDesignTime = [Environment]::GetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME','Process')
$EnvironmentNames = @(
    'YANGSPACE_F2B_O2_SQLITE_ONLY',
    'YANGSPACE_F2B_O2_SQLITE_PATH',
    'YANGSPACE_F2B_O2_PG_ONLY',
    'YANGSPACE_F2B_O2_PG_MIGRATOR_CONNECTION',
    'YANGSPACE_F2B_O2_PG_RUNTIME_CONNECTION',
    'YANGSPACE_F2B_O2_PG_SCOPE_TOKEN',
    'YANGSPACE_F2B_O2_PG_WRONG_CA_PATH'
)
$OldEnvironment = @{}
foreach ($Name in $EnvironmentNames) {
    $OldEnvironment[$Name] = [Environment]::GetEnvironmentVariable($Name,'Process')
}

try {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME','CONFIRMED','Process')
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME','CONFIRMED','Process')

    Stage -Label 'o2-tool-restore' -Executable $Dotnet -Arguments @('tool','restore','--tool-manifest','.config/dotnet-tools.json')
    Stage -Label 'o2-ef-version' -Executable $Dotnet -Arguments @('ef','--version')
    $EfVersionText = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-ef-version.stdout.log'))
    if ($EfVersionText -notmatch '\b10\.0\.0\b') { throw 'F2B_O2_EF_TOOL_VERSION_MISMATCH' }

    Stage -Label 'o2-runtime-restore' -Executable $Dotnet -Arguments @('restore',$RuntimeProject,'-warnaserror')
    Stage -Label 'o2-runtime-build' -Executable $Dotnet -Arguments @('build',$RuntimeProject,'-c','Release','--no-restore','-warnaserror')
    if (-not (Test-Path -LiteralPath $RuntimeDll -PathType Leaf)) {
        throw 'F2B_O2_RUNTIME_TEST_DLL_MISSING'
    }
    if (Test-Path -LiteralPath $RuntimeWork) {
        throw 'F2B_O2_RUNTIME_TEMP_DIRECTORY_ALREADY_EXISTS'
    }
    New-Item -ItemType Directory -Path $RuntimeWork | Out-Null
    Stage -Label 'o2-runtime-linux-publish' -Executable $Dotnet -Arguments @(
        'publish',$RuntimeProject,'-c','Release','-r','linux-x64',
        '--self-contained','true','-warnaserror','--output',$RuntimePublishDirectory
    )
    if (-not (Test-Path -LiteralPath $RuntimeAppHost -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $RuntimePublishDirectory 'YangSpace.Api.RuntimeTests.dll') -PathType Leaf)) {
        throw 'F2B_O2_LINUX_RUNTIME_PUBLISH_INCOMPLETE'
    }
    Copy-Item -LiteralPath (Join-Path $RuntimePublishDirectory 'YangSpace.Api.RuntimeTests.dll') -Destination $RuntimeDllEvidence -Force

    Stage -Label 'o2-sqlite-model-diff' -Executable $Dotnet -Arguments @(
        'ef','migrations','has-pending-model-changes',
        '--project',$SqliteProject,'--startup-project',$SqliteProject,
        '--context','DirectoryDb','--configuration','Release','--no-build'
    )
    Stage -Label 'o2-postgresql-model-diff' -Executable $Dotnet -Arguments @(
        'ef','migrations','has-pending-model-changes',
        '--project',$PostgreSqlProject,'--startup-project',$PostgreSqlProject,
        '--context','DirectoryDb','--configuration','Release','--no-build'
    )

    Stage -Label 'o2-sqlite-review-sql' -Executable $Dotnet -Arguments @(
        'ef','migrations','script',$SqliteBaseline,$SqliteTarget,
        '--project',$SqliteProject,'--startup-project',$SqliteProject,
        '--context','DirectoryDb','--configuration','Release','--no-build',
        '--output',$SqliteSql
    )
    Stage -Label 'o2-postgresql-review-sql' -Executable $Dotnet -Arguments @(
        'ef','migrations','script',$PostgreSqlBaseline,$PostgreSqlTarget,
        '--project',$PostgreSqlProject,'--startup-project',$PostgreSqlProject,
        '--context','DirectoryDb','--configuration','Release','--no-build',
        '--output',$PostgreSqlSql
    )

    foreach ($SqlPath in @($SqliteSql,$PostgreSqlSql)) {
        if (-not (Test-Path -LiteralPath $SqlPath -PathType Leaf)) {
            throw ('F2B_O2_REVIEW_SQL_MISSING=' + (Split-Path $SqlPath -Leaf))
        }
        $SqlText = [IO.File]::ReadAllText($SqlPath)
        $CreateTable = [regex]::Matches(
            $SqlText,'(?i)\bCREATE\s+TABLE\s+"IdentityEmailOutbox"')
        if ($CreateTable.Count -ne 1) {
            throw ('F2B_O2_UNEXPECTED_CREATE_TABLE_SCOPE=' + (Split-Path $SqlPath -Leaf))
        }
        if ($SqlText -match '(?im)^\s*(DROP|TRUNCATE|DELETE|UPDATE|ALTER)\b') {
            throw ('F2B_O2_UNEXPECTED_DESTRUCTIVE_SQL=' + (Split-Path $SqlPath -Leaf))
        }
        $UniqueIndexPattern = '(?is)CREATE\s+UNIQUE\s+INDEX\s+"UX_IdentityEmailOutbox_PendingUserPurpose"\s+ON\s+"IdentityEmailOutbox"\s*\("UserId",\s*"Purpose"\)\s+WHERE\s+"Status"\s*=\s*''Pending'''
        if ($SqlText -notmatch $UniqueIndexPattern -or $SqlText -notmatch 'AspNetUsers') {
            throw ('F2B_O2_EXPECTED_INDEX_OR_FK_MISSING=' + (Split-Path $SqlPath -Leaf))
        }
    }
    Write-Host 'F2B_O2_BOTH_PROVIDER_SQL_REVIEW=PASS'

    [Environment]::SetEnvironmentVariable('YANGSPACE_F2B_O2_SQLITE_ONLY','CONFIRMED_DISPOSABLE_ONLY','Process')
    [Environment]::SetEnvironmentVariable('YANGSPACE_F2B_O2_SQLITE_PATH',$SqliteDb,'Process')
    Stage -Label 'o2-sqlite-migration-contract' -Executable $Dotnet -Arguments @(
        'run','--project',$RuntimeProject,'-c','Release','--no-build','--',
        '--f2b-o2-sqlite-only'
    )

    foreach ($Marker in @(
        'F2B_O2_SQLITE_PATH_EVIDENCE_SCOPED=PASS',
        'F2B_O2_SQLITE_BASELINE_APPLIED=PASS',
        'F2B_O2_SQLITE_OUTBOX_ABSENT_AT_BASELINE=PASS',
        'F2B_O2_SQLITE_MIGRATION_APPLY=PASS',
        'F2B_O2_SQLITE_EXACT_OUTBOX_COLUMNS=PASS',
        'F2B_O2_SQLITE_PENDING_INDEX_UNIQUE_PARTIAL=PASS',
        'F2B_O2_SQLITE_PENDING_INDEX_COLUMNS=PASS',
        'F2B_O2_SQLITE_PENDING_INDEX_FILTER=PASS',
        'F2B_O2_SQLITE_ORDINARY_INDEXES=PASS',
        'F2B_O2_SQLITE_USER_FK_CASCADE=PASS',
        'F2B_O2_SQLITE_ROLLBACK_TO_PINNED_BASELINE=PASS',
        'F2B_O2_SQLITE_REAPPLY=PASS',
        'F2B_O2_SQLITE_INTEGRITY_CHECK=PASS',
        'F2B_O2_SQLITE_FOREIGN_KEY_CHECK=PASS',
        'F2B_O2_SQLITE_DUPLICATE_PENDING_REJECTED=PASS',
        'F2B_O2_SQLITE_ORPHAN_FK_REJECTED=PASS',
        'F2B_O2_SQLITE_CONCURRENT_ENQUEUE_SINGLE_PENDING=PASS',
        'F2B_O2_SQLITE_CONCURRENT_EXPIRED_PENDING_REFRESH_ONCE=PASS',
        'F2B_O2_SQLITE_APPLY_ROLLBACK_REAPPLY=PASS',
        'F2B_O2_SQLITE_INTEGRITY=PASS',
        'F2B_O2_SQLITE_CONCURRENCY=PASS',
        'F2B_O2_SQLITE_DISPOSABLE_ONLY=PASS',
        'F2B_O2_DISPATCHER_FAIL_CLOSED=PASS'
    )) { RequireMarker 'o2-sqlite-migration-contract' $Marker }

    foreach ($Suffix in @('','-wal','-shm','-journal')) {
        if (Test-Path -LiteralPath ($SqliteDb + $Suffix)) {
            throw 'F2B_O2_SQLITE_DISPOSABLE_DATABASE_NOT_REMOVED'
        }
    }
    Write-Host 'F2B_O2_SQLITE_DATABASE_CLEANUP=PASS'

    # PostgreSQL PGDATA and TLS material are tmpfs. Never restart the
    # Docker container after initialization: stopping it destroys both tmpfs
    # mounts. Generate certificates and write ALTER SYSTEM settings from the
    # official initdb hook, before the first final PostgreSQL server start.
    $TlsInitScriptContent = @'
#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

test -n "$PGDATA"
test -d /run/pgcerts
test -w /run/pgcerts
test -d "$PGDATA"
test -w "$PGDATA"

openssl req -x509 -newkey rsa:2048 -nodes \
    -keyout /run/pgcerts/ca.key \
    -out /run/pgcerts/ca.crt \
    -days 2 -subj '/CN=YangSpaceF2bO2EphemeralCA' \
    -addext 'basicConstraints=critical,CA:TRUE' \
    -addext 'keyUsage=critical,keyCertSign,cRLSign'

openssl req -newkey rsa:2048 -nodes \
    -keyout /run/pgcerts/server.key \
    -out /run/pgcerts/server.csr \
    -subj '/CN=localhost' \
    -addext 'subjectAltName=DNS:localhost' \
    -addext 'extendedKeyUsage=serverAuth' \
    -addext 'keyUsage=critical,digitalSignature,keyEncipherment'

openssl x509 -req \
    -in /run/pgcerts/server.csr \
    -CA /run/pgcerts/ca.crt \
    -CAkey /run/pgcerts/ca.key \
    -CAcreateserial \
    -out /run/pgcerts/server.crt \
    -days 2 -sha256 -copy_extensions copy

openssl req -x509 -newkey rsa:2048 -nodes \
    -keyout /run/pgcerts/wrong-ca.key \
    -out /run/pgcerts/wrong-ca.crt \
    -days 2 -subj '/CN=YangSpaceF2bO2WrongCA' \
    -addext 'basicConstraints=critical,CA:TRUE'

chmod 600 \
    /run/pgcerts/server.key \
    /run/pgcerts/ca.key \
    /run/pgcerts/wrong-ca.key

chmod 644 \
    /run/pgcerts/ca.crt \
    /run/pgcerts/server.crt \
    /run/pgcerts/wrong-ca.crt

cat > "$PGDATA/pg_hba.conf" <<'HBA'
local all all trust
hostssl all all 0.0.0.0/0 scram-sha-256
hostssl all all ::/0 scram-sha-256
hostnossl all all 0.0.0.0/0 reject
hostnossl all all ::/0 reject
HBA
chmod 600 "$PGDATA/pg_hba.conf"

psql -X -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
    -c "ALTER SYSTEM SET ssl = 'on';"
psql -X -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
    -c "ALTER SYSTEM SET ssl_cert_file = '/run/pgcerts/server.crt';"
psql -X -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
    -c "ALTER SYSTEM SET ssl_key_file = '/run/pgcerts/server.key';"
psql -X -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
    -c "ALTER SYSTEM SET password_encryption = 'scram-sha-256';"

printf '%s\n' \
    'F2B_O2_PG_CERTIFICATES_CREATED=PASS' \
    'F2B_O2_PG_HBA_INITDB_CONFIGURED=PASS' \
    'F2B_O2_PG_TLS_INITDB_CONFIGURED=PASS'
'@
    $TlsInitScriptContent = $TlsInitScriptContent.Replace("`r`n","`n").Replace("`r",'')
    [IO.File]::WriteAllText($TlsInitScript,$TlsInitScriptContent,$Utf8NoBom)

    $TlsEntryScriptContent = @'
#!/usr/bin/env bash
set -Eeuo pipefail
chown postgres:postgres /run/pgcerts
chmod 700 /run/pgcerts
exec /usr/local/bin/docker-entrypoint.sh postgres
'@
    $TlsEntryScriptContent = $TlsEntryScriptContent.Replace("`r`n","`n").Replace("`r",'')
    [IO.File]::WriteAllText($TlsEntryScript,$TlsEntryScriptContent,$Utf8NoBom)

    $NetworkCreated = $true
    Stage -Label 'o2-pg-network-create' -Executable $Docker -Arguments @('network','create','--internal',$Network)
    $NetworkInternal = GetDockerValue -Arguments @('network','inspect','--format','{{.Internal}}',$Network)
    Check ($NetworkInternal -ceq 'true') 'F2B_O2_PG_NETWORK_INTERNAL'

    $ContainerAttempted = $true
    Stage -Label 'o2-pg-container-create' -Executable $Docker -Arguments @(
        'create','--network',$Network,'--network-alias','pg',
        '--mount','type=tmpfs,dst=/var/lib/postgresql/data,tmpfs-size=536870912,tmpfs-mode=0700',
        '--mount','type=tmpfs,dst=/run/pgcerts,tmpfs-size=8388608,tmpfs-mode=0700',
        '--name',$Container,
        '-e','POSTGRES_HOST_AUTH_METHOD=trust',
        '--entrypoint','/bin/bash',
        $PgImage,
        '/tmp/f2b-o2-postgres-entrypoint.sh'
    )
    Stage -Label 'o2-pg-entry-script-copy' -Executable $Docker -Arguments @(
        'cp',$TlsEntryScript,($Container + ':/tmp/f2b-o2-postgres-entrypoint.sh')
    )
    Stage -Label 'o2-pg-init-script-copy' -Executable $Docker -Arguments @(
        'cp',$TlsInitScript,($Container + ':/docker-entrypoint-initdb.d/99-f2b-o2-tls.sh')
    )
    Stage -Label 'o2-pg-container-start' -Executable $Docker -Arguments @('start',$Container)
    WaitForPostgreSql $Container

    $ContainerNetwork = GetDockerValue -Arguments @('inspect','--format','{{.HostConfig.NetworkMode}}',$Container)
    if ($ContainerNetwork -cne $Network) { throw 'F2B_O2_PG_CONTAINER_NETWORK_MISMATCH' }
    $Binds = GetDockerValue -Arguments @('inspect','--format','{{json .HostConfig.Binds}}',$Container)
    if ($Binds -notin @('null','[]')) { throw 'F2B_O2_PG_HOST_BIND_MOUNTS_REFUSED' }
    # postgres:17 declares /var/lib/postgresql/data as an image VOLUME.
    # Explicit --mount type=tmpfs entries are used for both data and TLS paths.
    # Acceptance is based on Docker's effective .Mounts, not request arguments.
    # Persist non-secret Docker storage diagnostics before evaluating the hard gate.
    # The finally block removes the disposable container, so these values must
    # be captured while the exact test container is still alive.
    $ImageVolumesJson = GetDockerValue -Arguments @('image','inspect','--format','{{json .Config.Volumes}}',$PgImage)
    $ContainerVolumesJson = GetDockerValue -Arguments @('inspect','--format','{{json .Config.Volumes}}',$Container)
    $MountJson = GetDockerValue -Arguments @('inspect','--format','{{json .Mounts}}',$Container)
    $TmpfsConfigJson = GetDockerValue -Arguments @('inspect','--format','{{json .HostConfig.Tmpfs}}',$Container)
    $HostMountsJson = GetDockerValue -Arguments @('inspect','--format','{{json .HostConfig.Mounts}}',$Container)
    $Binds = GetDockerValue -Arguments @('inspect','--format','{{json .HostConfig.Binds}}',$Container)
    $ImageVolumes = $ImageVolumesJson | ConvertFrom-Json
    if ($MountJson -notmatch '^\s*\[') {
        throw 'F2B_O2_PG_EFFECTIVE_MOUNTS_JSON_NOT_ARRAY'
    }
    # Do not pipe a root JSON array directly into ConvertFrom-Json: on this
    # Windows PowerShell path it is surfaced as one array-valued pipeline item,
    # causing @(...).Count to report 1. The envelope exposes each mount as a
    # normal property-array entry and keeps .Count / foreach semantics correct.
    $MountEnvelope = ConvertFrom-Json -InputObject ('{"mounts":' + $MountJson + '}')
    $Mounts = @($MountEnvelope.mounts)
    $ExpectedTmpfsDestinations = @('/var/lib/postgresql/data','/run/pgcerts')

    $StorageDiagnostics = [ordered]@{
        commit_sha = $CommitSha
        docker_server_version = (GetDockerValue -Arguments @('version','--format','{{.Server.Version}}'))
        docker_server_os = (GetDockerValue -Arguments @('info','--format','{{.OSType}}'))
        image = $PgImage
        image_config_volumes_json = $ImageVolumesJson
        container_config_volumes_json = $ContainerVolumesJson
        host_config_tmpfs_json = $TmpfsConfigJson
        host_config_mounts_json = $HostMountsJson
        host_config_binds_json = $Binds
        effective_mounts_json = $MountJson
    }
    [IO.File]::WriteAllText(
        (Join-Path $Evidence 'o2-pg-storage-diagnostics.json'),
        ($StorageDiagnostics | ConvertTo-Json -Depth 8),
        $Utf8NoBom
    )
    Write-Host 'F2B_O2_PG_STORAGE_DIAGNOSTICS=RECORDED'

    if (-not ($ImageVolumes.PSObject.Properties.Name -contains '/var/lib/postgresql/data')) {
        throw 'F2B_O2_PG_EXPECTED_IMAGE_DATA_VOLUME_NOT_DECLARED'
    }

    $MountErrors = @()
    if ($Mounts.Count -ne 2) {
        $MountErrors += ('MOUNT_COUNT=' + $Mounts.Count)
    }
    foreach ($Mount in $Mounts) {
        if ($Mount.Type -cne 'tmpfs') {
            $MountErrors += ('NON_TMPFS_TYPE=' + [string]$Mount.Type + '@' + [string]$Mount.Destination)
        }
        if ($Mount.RW -ne $true) {
            $MountErrors += ('MOUNT_NOT_WRITABLE=' + [string]$Mount.Destination)
        }
        if ($Mount.Destination -notin $ExpectedTmpfsDestinations) {
            $MountErrors += ('UNEXPECTED_DESTINATION=' + [string]$Mount.Destination)
        }
    }
    foreach ($Destination in $ExpectedTmpfsDestinations) {
        $DestinationCount = @($Mounts | Where-Object { $_.Destination -ceq $Destination }).Count
        if ($DestinationCount -ne 1) {
            $MountErrors += ('DESTINATION_COUNT=' + $Destination + ':' + $DestinationCount)
        }
    }
    if ($Binds -notin @('null','[]')) {
        $MountErrors += 'HOST_BIND_MOUNTS_PRESENT'
    }
    if ($MountErrors.Count -gt 0) {
        throw ('F2B_O2_PG_STORAGE_GATE_FAILED=' + ($MountErrors -join ';'))
    }
    Check ($true) 'F2B_O2_PG_IMAGE_DECLARED_VOLUME_OVERRIDDEN_BY_TMPFS'
    # The database remains on an internal-only bridge. Do not request a host
    # port: Docker Engine does not publish ports for containers attached only
    # to internal networks. The .NET client runs inside this same container.
    $PortJson = GetDockerValue -Arguments @('inspect','--format','{{json .HostConfig.PortBindings}}',$Container)
    $EffectivePortsJson = GetDockerValue -Arguments @('inspect','--format','{{json .NetworkSettings.Ports}}',$Container)
    $PortObject = $PortJson | ConvertFrom-Json
    $EffectivePortsObject = $EffectivePortsJson | ConvertFrom-Json
    $RequestedBindings = @()
    $EffectiveBindings = @()
    if ($null -ne $PortObject) {
        foreach ($Property in $PortObject.PSObject.Properties) {
            if ($null -ne $Property.Value -and @($Property.Value).Count -gt 0) {
                $RequestedBindings += $Property.Name
            }
        }
    }
    if ($null -ne $EffectivePortsObject) {
        foreach ($Property in $EffectivePortsObject.PSObject.Properties) {
            if ($null -ne $Property.Value -and @($Property.Value).Count -gt 0) {
                $EffectiveBindings += $Property.Name
            }
        }
    }
    if ($RequestedBindings.Count -ne 0 -or $EffectiveBindings.Count -ne 0) {
        throw 'F2B_O2_PG_HOST_PORT_PUBLISHING_NOT_DISABLED'
    }
    Write-Host 'F2B_O2_PG_NO_HOST_PORT_PUBLISHING=PASS'
    Write-Host 'F2B_O2_PG_CONTAINER_ISOLATION=PASS'

    Stage -Label 'o2-pg-init-script-receipts' -Executable $Docker -Arguments @(
        'logs','--tail','300',$Container
    )
    $InitLog = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-init-script-receipts.stdout.log'))
    foreach ($Marker in @(
        'F2B_O2_PG_CERTIFICATES_CREATED=PASS',
        'F2B_O2_PG_HBA_INITDB_CONFIGURED=PASS',
        'F2B_O2_PG_TLS_INITDB_CONFIGURED=PASS'
    )) {
        if (-not $InitLog.Contains($Marker)) {
            throw ('F2B_O2_POSTGRES_INITDB_RECEIPT_MISSING=' + $Marker)
        }
    }
    Write-Host 'F2B_O2_PG_TLS_AND_HBA_CONFIGURED_BEFORE_FIRST_SERVER_START=PASS'

    Stage -Label 'o2-pg-cert-dir-owner' -Executable $Docker -Arguments @(
        'exec',$Container,'ls','-ld','/run/pgcerts'
    )
    $CertDir = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-cert-dir-owner.stdout.log')).Trim()
    if ($CertDir -notmatch '^drwx------\s+\d+\s+postgres\s+postgres\s+') {
        throw 'F2B_O2_PG_CERT_DIR_OWNER_OR_MODE_INVALID'
    }

    Stage -Label 'o2-pg-cert-ca' -Executable $Docker -Arguments @(
        'exec',$Container,'openssl','x509','-in','/run/pgcerts/ca.crt','-noout','-ext','basicConstraints'
    )
    $CaText = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-cert-ca.stdout.log'))
    if ($CaText -notmatch 'CA:TRUE') { throw 'F2B_O2_PG_CA_CERT_INVALID' }

    Stage -Label 'o2-pg-cert-server-csr' -Executable $Docker -Arguments @(
        'exec',$Container,'openssl','req','-in','/run/pgcerts/server.csr','-noout','-verify'
    )
    Stage -Label 'o2-pg-cert-server-sign' -Executable $Docker -Arguments @(
        'exec',$Container,'openssl','verify','-CAfile','/run/pgcerts/ca.crt','/run/pgcerts/server.crt'
    )
    $ServerCertVerify = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-cert-server-sign.stdout.log'))
    if ($ServerCertVerify -notmatch 'server\.crt:\s+OK') { throw 'F2B_O2_PG_SERVER_CERT_CHAIN_INVALID' }

    Stage -Label 'o2-pg-cert-wrong-ca' -Executable $Docker -Arguments @(
        'exec',$Container,'openssl','x509','-in','/run/pgcerts/wrong-ca.crt','-noout','-ext','basicConstraints'
    )
    $WrongCaText = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-cert-wrong-ca.stdout.log'))
    if ($WrongCaText -notmatch 'CA:TRUE') { throw 'F2B_O2_PG_WRONG_CA_CERT_INVALID' }

    Stage -Label 'o2-pg-cert-permissions' -Executable $Docker -Arguments @(
        'exec',$Container,'ls','-l','/run/pgcerts/server.key',
        '/run/pgcerts/ca.key','/run/pgcerts/wrong-ca.key'
    )
    $PrivateModes = @([IO.File]::ReadAllLines((Join-Path $Evidence 'o2-pg-cert-permissions.stdout.log')) |
        Where-Object { $_ -match '\S' })
    if ($PrivateModes.Count -ne 3 -or @($PrivateModes | Where-Object { $_ -notmatch '^-rw-------\s+\d+\s+postgres\s+postgres\s+' }).Count -ne 0) {
        throw 'F2B_O2_PG_PRIVATE_KEY_MODE_INVALID'
    }

    Stage -Label 'o2-pg-cert-public-permissions' -Executable $Docker -Arguments @(
        'exec',$Container,'ls','-l','/run/pgcerts/ca.crt',
        '/run/pgcerts/server.crt','/run/pgcerts/wrong-ca.crt'
    )
    $PublicModes = @([IO.File]::ReadAllLines((Join-Path $Evidence 'o2-pg-cert-public-permissions.stdout.log')) |
        Where-Object { $_ -match '\S' })
    if ($PublicModes.Count -ne 3 -or @($PublicModes | Where-Object { $_ -notmatch '^-rw-r--r--\s+\d+\s+postgres\s+postgres\s+' }).Count -ne 0) {
        throw 'F2B_O2_PG_PUBLIC_CERT_MODE_INVALID'
    }

    Stage -Label 'o2-pg-cert-san-check' -Executable $Docker -Arguments @(
        'exec',$Container,'openssl','x509','-in','/run/pgcerts/server.crt',
        '-noout','-ext','subjectAltName'
    )
    $SanText = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-cert-san-check.stdout.log'))
    if ($SanText -notmatch 'DNS:localhost') { throw 'F2B_O2_PG_SERVER_CERT_SAN_MISMATCH' }

    Stage -Label 'o2-pg-config-owner' -Executable $Docker -Arguments @(
        'exec',$Container,'ls','-l',
        '/var/lib/postgresql/data/postgresql.auto.conf',
        '/var/lib/postgresql/data/pg_hba.conf'
    )
    $ConfigOwners = @([IO.File]::ReadAllLines((Join-Path $Evidence 'o2-pg-config-owner.stdout.log')) |
        Where-Object { $_ -match '\S' })
    if ($ConfigOwners.Count -ne 2 -or @($ConfigOwners | Where-Object { $_ -notmatch '^-rw-------\s+\d+\s+postgres\s+postgres\s+' }).Count -ne 0) {
        throw 'F2B_O2_PG_CONFIG_OWNER_INVALID'
    }

    Stage -Label 'o2-pg-config-permissions' -Executable $Docker -Arguments @(
        'exec',$Container,'ls','-l',
        '/var/lib/postgresql/data/postgresql.auto.conf',
        '/var/lib/postgresql/data/pg_hba.conf'
    )
    $ConfigModes = @([IO.File]::ReadAllLines((Join-Path $Evidence 'o2-pg-config-permissions.stdout.log')) |
        Where-Object { $_ -match '\S' })
    if ($ConfigModes.Count -ne 2 -or @($ConfigModes | Where-Object { $_ -notmatch '^-rw-------\s+\d+\s+postgres\s+postgres\s+' }).Count -ne 0) {
        throw 'F2B_O2_PG_CONFIG_MODE_INVALID'
    }

    Stage -Label 'o2-pg-tls-configured' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-c',
        "SELECT CASE WHEN setting='on' AND left(sourcefile,char_length(current_setting('data_directory')))=current_setting('data_directory') AND substring(sourcefile from char_length(current_setting('data_directory')) + 1 for 1)='/' AND substring(sourcefile from char_length(current_setting('data_directory')) + 2)='postgresql.auto.conf' AND NOT pending_restart THEN 'PASS' ELSE 'FAIL' END FROM pg_settings WHERE name='ssl';"
    )
    $TlsConfigured = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-tls-configured.stdout.log')).Trim()
    if ($TlsConfigured -cne 'PASS') { throw 'F2B_O2_PG_TLS_INITDB_SETTING_NOT_ACTIVE' }

    Stage -Label 'o2-pg-hba-policy-active' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-c',
        "SELECT CASE WHEN count(*)=5 AND count(*) FILTER (WHERE error IS NOT NULL)=0 AND count(*) FILTER (WHERE type='local' AND auth_method='trust')=1 AND count(*) FILTER (WHERE type='hostssl' AND auth_method='scram-sha-256')=2 AND count(*) FILTER (WHERE type='hostnossl' AND auth_method='reject')=2 THEN 'PASS' ELSE 'FAIL' END FROM pg_hba_file_rules;"
    )
    $HbaPolicy = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-hba-policy-active.stdout.log')).Trim()
    if ($HbaPolicy -cne 'PASS') { throw 'F2B_O2_PG_HBA_POLICY_NOT_ACTIVE' }


    Stage -Label 'o2-pg-container-state' -Executable $Docker -Arguments @(
        'inspect','--format','{{.RestartCount}}',$Container
    )
    $ContainerRestartCount = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-container-state.stdout.log')).Trim()
    if ($ContainerRestartCount -cne '0') { throw 'F2B_O2_PG_CONTAINER_RESTARTED_TMPFS_STATE_INVALID' }

    Stage -Label 'o2-pg-server-version' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-c','SHOW server_version_num;'
    )
    $PgVersionNum = [IO.File]::ReadAllText(
        (Join-Path $Evidence 'o2-pg-server-version.stdout.log')).Trim()
    if ($PgVersionNum -notmatch '^\d+$' -or
        [int]$PgVersionNum -lt 170000 -or [int]$PgVersionNum -ge 180000) {
        throw ('F2B_O2_POSTGRESQL_17_REQUIRED=' + $PgVersionNum)
    }

    # Capture the active PostgreSQL configuration paths and effective TLS
    # setting provenance before enforcing the SSL gate. docker cp success alone
    # does not prove that PostgreSQL loaded the intended postgresql.auto.conf.
    Stage -Label 'o2-pg-config-paths' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-c',
        "SELECT row_to_json(x) FROM (SELECT current_setting('config_file') AS config_file, current_setting('data_directory') AS data_directory, current_setting('hba_file') AS hba_file) x;"
    )
    Stage -Label 'o2-pg-config-file-stat' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-c',
        "SELECT row_to_json(pg_stat_file('postgresql.auto.conf'));"
    )
    Stage -Label 'o2-pg-auto-conf-content' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-c',
        "SELECT pg_read_file('postgresql.auto.conf');"
    )
    Stage -Label 'o2-pg-hba-content' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-c',
        "SELECT pg_read_file(current_setting('hba_file'));"
    )
    Stage -Label 'o2-pg-setting-sources' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-c',
        "SELECT row_to_json(x) FROM (SELECT name, setting, source, sourcefile, sourceline, pending_restart FROM pg_settings WHERE name IN ('ssl','ssl_cert_file','ssl_key_file') ORDER BY name) x;"
    )
    Stage -Label 'o2-pg-file-settings' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-c',
        "SELECT row_to_json(x) FROM (SELECT sourcefile, sourceline, name, setting, applied, error FROM pg_file_settings WHERE name IN ('ssl','ssl_cert_file','ssl_key_file') OR error IS NOT NULL ORDER BY seqno) x;"
    )
    Stage -Label 'o2-pg-startup-log' -Executable $Docker -Arguments @(
        'logs','--tail','200',$Container
    )

    Stage -Label 'o2-pg-ssl-state' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-c','SHOW ssl;'
    )
    $SslState = [IO.File]::ReadAllText((Join-Path $Evidence 'o2-pg-ssl-state.stdout.log')).Trim()
    if ($SslState -cne 'on') { throw 'F2B_O2_PG_SSL_NOT_ENABLED' }

    # Export public certificates through docker exec stdout. Decode exact bytes
    # on the host; private keys remain inside the disposable container tmpfs.
    Stage -Label 'o2-pg-root-ca-export' -Executable $Docker -Arguments @(
        'exec',$Container,'openssl','base64','-A','-in','/run/pgcerts/ca.crt'
    )
    $RootCaBase64 = [IO.File]::ReadAllText(
        (Join-Path $Evidence 'o2-pg-root-ca-export.stdout.log')
    ).Trim()
    if ([string]::IsNullOrWhiteSpace($RootCaBase64)) {
        throw 'F2B_O2_PG_ROOT_CA_BASE64_EMPTY'
    }
    try {
        $RootCaBytes = [Convert]::FromBase64String($RootCaBase64)
    } catch {
        throw 'F2B_O2_PG_ROOT_CA_BASE64_DECODE_FAILED'
    }
    $RootCaPem = [Text.Encoding]::ASCII.GetString($RootCaBytes)
    if (-not $RootCaPem.StartsWith('-----BEGIN CERTIFICATE-----') -or
        -not $RootCaPem.Contains('-----END CERTIFICATE-----')) {
        throw 'F2B_O2_PG_ROOT_CA_PEM_INVALID'
    }
    [IO.File]::WriteAllBytes($RootCaPath,$RootCaBytes)

    Stage -Label 'o2-pg-wrong-ca-export' -Executable $Docker -Arguments @(
        'exec',$Container,'openssl','base64','-A','-in','/run/pgcerts/wrong-ca.crt'
    )
    $WrongCaBase64 = [IO.File]::ReadAllText(
        (Join-Path $Evidence 'o2-pg-wrong-ca-export.stdout.log')
    ).Trim()
    if ([string]::IsNullOrWhiteSpace($WrongCaBase64)) {
        throw 'F2B_O2_PG_WRONG_CA_BASE64_EMPTY'
    }
    try {
        $WrongCaBytes = [Convert]::FromBase64String($WrongCaBase64)
    } catch {
        throw 'F2B_O2_PG_WRONG_CA_BASE64_DECODE_FAILED'
    }
    $WrongCaPem = [Text.Encoding]::ASCII.GetString($WrongCaBytes)
    if (-not $WrongCaPem.StartsWith('-----BEGIN CERTIFICATE-----') -or
        -not $WrongCaPem.Contains('-----END CERTIFICATE-----')) {
        throw 'F2B_O2_PG_WRONG_CA_PEM_INVALID'
    }
    [IO.File]::WriteAllBytes($WrongCaPath,$WrongCaBytes)

    if (-not (Test-Path -LiteralPath $RootCaPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $WrongCaPath -PathType Leaf)) {
        throw 'F2B_O2_PG_CA_CERT_EXPORT_FAILED'
    }

    [IO.File]::WriteAllText($RoleSql,(
        "CREATE ROLE $MigratorRole LOGIN PASSWORD '$MigratorPassword' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;" + [Environment]::NewLine +
        "CREATE ROLE $RuntimeRole LOGIN PASSWORD '$RuntimePassword' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOINHERIT;" + [Environment]::NewLine
    ),$Utf8NoBom)
    Stage -Label 'o2-pg-roles-sql-copy' -Executable $Docker -Arguments @(
        'cp',$RoleSql,($Container + ':/tmp/f2b-o2-roles.sql')
    )
    Stage -Label 'o2-pg-roles-create' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-f','/tmp/f2b-o2-roles.sql'
    )
    Stage -Label 'o2-pg-roles-secret-cleanup' -Executable $Docker -Arguments @(
        'exec',$Container,'rm','-f','/tmp/f2b-o2-roles.sql'
    )
    Remove-Item -LiteralPath $RoleSql -Force

    Stage -Label 'o2-pg-database-create' -Executable $Docker -Arguments @(
        'exec',$Container,'createdb','-U','postgres','--owner=postgres',$Database
    )

    [IO.File]::WriteAllText($PermissionSql,(
        "REVOKE ALL ON DATABASE $Database FROM PUBLIC;" + [Environment]::NewLine +
        "GRANT CONNECT ON DATABASE $Database TO $MigratorRole;" + [Environment]::NewLine +
        "GRANT CONNECT ON DATABASE $Database TO $RuntimeRole;" + [Environment]::NewLine +
        "REVOKE ALL ON SCHEMA public FROM PUBLIC;" + [Environment]::NewLine +
        "GRANT USAGE, CREATE ON SCHEMA public TO $MigratorRole;" + [Environment]::NewLine +
        "GRANT USAGE ON SCHEMA public TO $RuntimeRole;" + [Environment]::NewLine +
        "CREATE TABLE public.yangspace_f2b_o2_qa_guard(scope_token text PRIMARY KEY);" + [Environment]::NewLine +
        "INSERT INTO public.yangspace_f2b_o2_qa_guard(scope_token) VALUES ('$ScopeToken');" + [Environment]::NewLine +
        "GRANT SELECT ON TABLE public.yangspace_f2b_o2_qa_guard TO $MigratorRole;" + [Environment]::NewLine
    ),$Utf8NoBom)
    Stage -Label 'o2-pg-permissions-sql-copy' -Executable $Docker -Arguments @(
        'cp',$PermissionSql,($Container + ':/tmp/f2b-o2-permissions.sql')
    )
    Stage -Label 'o2-pg-permissions-apply' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d',$Database,'-f','/tmp/f2b-o2-permissions.sql'
    )
    Stage -Label 'o2-pg-permissions-secret-cleanup' -Executable $Docker -Arguments @(
        'exec',$Container,'rm','-f','/tmp/f2b-o2-permissions.sql'
    )
    Remove-Item -LiteralPath $PermissionSql -Force

    $MigratorConnection = (
        "Host=localhost;Port=5432;Database=$Database;" +
        "Username=$MigratorRole;Password=$MigratorPassword;" +
        "SSL Mode=VerifyFull;Root Certificate=/run/pgcerts/ca.crt;" +
        "Pooling=false;Timeout=10;" +
        "Command Timeout=30;Include Error Detail=false"
    )
    $RuntimeConnection = (
        "Host=localhost;Port=5432;Database=$Database;" +
        "Username=$RuntimeRole;Password=$RuntimePassword;" +
        "SSL Mode=VerifyFull;Root Certificate=/run/pgcerts/ca.crt;" +
        "Pooling=false;Timeout=10;" +
        "Command Timeout=30;Include Error Detail=false"
    )

    $OnlyBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('CONFIRMED_DISPOSABLE_ONLY'))
    $MigratorBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($MigratorConnection))
    $RuntimeBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($RuntimeConnection))
    $ScopeBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($ScopeToken))
    $WrongCaBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('/run/pgcerts/wrong-ca.crt'))
    $RuntimeScriptContent = @'
#!/usr/bin/env bash
set -Eeuo pipefail
export YANGSPACE_F2B_O2_PG_ONLY="$(printf '%s' '__ONLY__' | base64 -d)"
export YANGSPACE_F2B_O2_PG_MIGRATOR_CONNECTION="$(printf '%s' '__MIGRATOR__' | base64 -d)"
export YANGSPACE_F2B_O2_PG_RUNTIME_CONNECTION="$(printf '%s' '__RUNTIME__' | base64 -d)"
export YANGSPACE_F2B_O2_PG_SCOPE_TOKEN="$(printf '%s' '__SCOPE__' | base64 -d)"
export YANGSPACE_F2B_O2_PG_WRONG_CA_PATH="$(printf '%s' '__WRONG_CA__' | base64 -d)"
echo 'F2B_O2_PG_CLIENT_INTERNAL_NETWORK=PASS'
cd /tmp/f2b-o2-runtime
exec ./YangSpace.Api.RuntimeTests --f2b-o2-postgresql-only
'@
    $RuntimeScriptContent = $RuntimeScriptContent.Replace('__ONLY__',$OnlyBase64)
    $RuntimeScriptContent = $RuntimeScriptContent.Replace('__MIGRATOR__',$MigratorBase64)
    $RuntimeScriptContent = $RuntimeScriptContent.Replace('__RUNTIME__',$RuntimeBase64)
    $RuntimeScriptContent = $RuntimeScriptContent.Replace('__SCOPE__',$ScopeBase64)
    $RuntimeScriptContent = $RuntimeScriptContent.Replace('__WRONG_CA__',$WrongCaBase64)
    [IO.File]::WriteAllText($RuntimeScriptPath,$RuntimeScriptContent,$Utf8NoBom)

    Stage -Label 'o2-pg-runtime-copy' -Executable $Docker -Arguments @(
        'cp',$RuntimePublishDirectory,($Container + ':/tmp/f2b-o2-runtime')
    )
    Stage -Label 'o2-pg-runtime-script-copy' -Executable $Docker -Arguments @(
        'cp',$RuntimeScriptPath,($Container + ':/tmp/f2b-o2-run-contract.sh')
    )
    Stage -Label 'o2-pg-runtime-script-tmpfs-stage' -Executable $Docker -Arguments @(
        'exec',$Container,'mv','/tmp/f2b-o2-run-contract.sh','/run/pgcerts/run-contract.sh'
    )
    Stage -Label 'o2-pg-runtime-permissions' -Executable $Docker -Arguments @(
        'exec',$Container,'chown','-R','postgres:postgres',
        '/tmp/f2b-o2-runtime','/run/pgcerts/run-contract.sh'
    )
    Stage -Label 'o2-pg-runtime-executable' -Executable $Docker -Arguments @(
        'exec',$Container,'chmod','700',
        '/tmp/f2b-o2-runtime/YangSpace.Api.RuntimeTests',
        '/run/pgcerts/run-contract.sh'
    )
    Stage -Label 'o2-pg-runtime-dependencies' -Executable $Docker -Arguments @(
        'exec',$Container,'ldd','/tmp/f2b-o2-runtime/YangSpace.Api.RuntimeTests'
    )
    $RuntimeDependencies = [IO.File]::ReadAllText(
        (Join-Path $Evidence 'o2-pg-runtime-dependencies.stdout.log')
    )
    if ($RuntimeDependencies -match 'not found') {
        throw 'F2B_O2_LINUX_RUNTIME_NATIVE_DEPENDENCY_MISSING'
    }

    Stage -Label 'o2-pg-runtime-contract' -Executable $Docker -Arguments @(
        'exec','--user','postgres',$Container,'/bin/bash','/run/pgcerts/run-contract.sh'
    )

    foreach ($Marker in @(
        'F2B_O2_PG_CLIENT_INTERNAL_NETWORK=PASS',
        'F2B_O2_PG_MIGRATOR_VERIFYFULL_CONNECTION_POLICY=PASS',
        'F2B_O2_PG_RUNTIME_VERIFYFULL_CONNECTION_POLICY=PASS',
        'F2B_O2_PG_MIGRATOR_TLS_ACTIVE=PASS',
        'F2B_O2_PG_RUNTIME_TLS_ACTIVE=PASS',
        'F2B_O2_PG_VERIFYFULL_HOSTNAME_MISMATCH_REJECTED=PASS',
        'F2B_O2_PG_VERIFYFULL_UNTRUSTED_CA_REJECTED=PASS',
        'F2B_O2_PG_DISPOSABLE_SENTINEL_MATCHED=PASS',
        'F2B_O2_PG_SERVER_VERSION_17_AND_DB_SCOPE=PASS',
        'F2B_O2_PG_BASELINE_APPLIED=PASS',
        'F2B_O2_PG_OUTBOX_ABSENT_AT_BASELINE=PASS',
        'F2B_O2_PG_MIGRATION_APPLY=PASS',
        'F2B_O2_PG_EXACT_OUTBOX_COLUMNS=PASS',
        'F2B_O2_PG_PENDING_INDEX_UNIQUE_PARTIAL=PASS',
        'F2B_O2_PG_ORDINARY_INDEXES=PASS',
        'F2B_O2_PG_USER_FK_CASCADE_VALIDATED=PASS',
        'F2B_O2_PG_NO_UNVALIDATED_CONSTRAINTS=PASS',
        'F2B_O2_PG_NO_ORPHAN_OUTBOX_ROWS=PASS',
        'F2B_O2_PG_ROLLBACK_TO_PINNED_BASELINE=PASS',
        'F2B_O2_PG_REAPPLY=PASS',
        'F2B_O2_PG_MIGRATOR_NON_SUPERUSER=PASS',
        'F2B_O2_PG_RUNTIME_NON_SUPERUSER=PASS',
        'F2B_O2_PG_MIGRATOR_SCHEMA_CREATE_ONLY=PASS',
        'F2B_O2_PG_RUNTIME_DML_ONLY_OUTBOX=PASS',
        'F2B_O2_PG_RUNTIME_CREATE_DENIED=PASS',
        'F2B_O2_PG_RUNTIME_IDENTITY_READ_DENIED=PASS',
        'F2B_O2_PG_RUNTIME_TRUNCATE_DENIED=PASS',
        'F2B_O2_PG_DUPLICATE_PENDING_REJECTED=PASS',
        'F2B_O2_PG_ORPHAN_FK_REJECTED=PASS',
        'F2B_O2_PG_CONCURRENT_ENQUEUE_SINGLE_PENDING=PASS',
        'F2B_O2_PG_CONCURRENT_EXPIRED_PENDING_REFRESH_ONCE=PASS',
        'F2B_O2_PG_RUNTIME_DELETE_ALLOWED=PASS',
        'F2B_O2_PG_FINAL_CONSTRAINT_INTEGRITY=PASS',
        'F2B_O2_PG_APPLY_ROLLBACK_REAPPLY=PASS',
        'F2B_O2_PG_INTEGRITY=PASS',
        'F2B_O2_PG_CONCURRENCY=PASS',
        'F2B_O2_PG_TLS_VERIFYFULL=PASS',
        'F2B_O2_PG_LEAST_PRIVILEGE=PASS',
        'F2B_O2_PG_DISPOSABLE_ONLY=PASS',
        'F2B_O2_DISPATCHER_FAIL_CLOSED=PASS'
    )) { RequireMarker 'o2-pg-runtime-contract' $Marker }

    Stage -Label 'o2-pg-runtime-cleanup' -Executable $Docker -Arguments @(
        'exec',$Container,'rm','-rf',
        '/run/pgcerts/run-contract.sh',
        '/tmp/f2b-o2-runtime','/tmp/f2b-o2-run-contract.sh'
    )

    $Completed = $true
}
finally {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME',$OldDesignTime,'Process')
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME',$OldPostgreSqlDesignTime,'Process')
    foreach ($Name in $EnvironmentNames) {
        [Environment]::SetEnvironmentVariable($Name,$OldEnvironment[$Name],'Process')
    }

    $Previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        if ($ContainerAttempted) {
            $Exists = @(& $Docker inspect $Container 2>&1)
            if ($LASTEXITCODE -eq 0) {
                $Removal = @(& $Docker rm -f -v $Container 2>&1)
                if ($LASTEXITCODE -ne 0) {
                    $CleanupOk = $false
                    Write-Warning ('F2B_O2_PG_CONTAINER_CLEANUP_FAILED=' + $Container)
                }
            }
            $StillExists = @(& $Docker inspect $Container 2>&1)
            if ($LASTEXITCODE -eq 0) {
                $CleanupOk = $false
                Write-Warning ('F2B_O2_PG_CONTAINER_REMAINS=' + $Container)
            }
        }

        if ($NetworkCreated) {
            $NetworkExists = @(& $Docker network inspect $Network 2>&1)
            if ($LASTEXITCODE -eq 0) {
                $NetworkRemoval = @(& $Docker network rm $Network 2>&1)
                if ($LASTEXITCODE -ne 0) {
                    $CleanupOk = $false
                    Write-Warning ('F2B_O2_PG_NETWORK_CLEANUP_FAILED=' + $Network)
                }
            }
            $NetworkStillExists = @(& $Docker network inspect $Network 2>&1)
            if ($LASTEXITCODE -eq 0) {
                $CleanupOk = $false
                Write-Warning ('F2B_O2_PG_NETWORK_REMAINS=' + $Network)
            }
        }

        foreach ($Path in @($RoleSql,$PermissionSql,$WrongCaPath,$TlsConfig,$HbaConfig,$TlsInitScript,$TlsEntryScript)) {
            if (Test-Path -LiteralPath $Path) {
                Remove-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
            }
            if (Test-Path -LiteralPath $Path) {
                $CleanupOk = $false
                Write-Warning ('F2B_O2_TEMPORARY_FILE_REMAINS=' + (Split-Path $Path -Leaf))
            }
        }

        if (Test-Path -LiteralPath $RuntimeWork) {
            Remove-Item -LiteralPath $RuntimeWork -Recurse -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path -LiteralPath $RuntimeWork) {
            $CleanupOk = $false
            Write-Warning 'F2B_O2_RUNTIME_TEMP_DIRECTORY_REMAINS'
        }

        foreach ($Suffix in @('','-wal','-shm')) {
            if (Test-Path -LiteralPath ($SqliteDb + $Suffix)) {
                $CleanupOk = $false
                Write-Warning 'F2B_O2_SQLITE_DATABASE_REMAINS'
            }
        }
    }
    finally {
        $ErrorActionPreference = $Previous
    }

    if (-not $Completed -or -not $CleanupOk) {
        [IO.File]::WriteAllText(
            (Join-Path $Evidence 'gate.txt'),
            'F2B_O2_LOCAL_VALIDATION=HOLD',
            $Utf8NoBom
        )
    }
}

if (-not $CleanupOk) { throw 'F2B_O2_RESOURCE_CLEANUP_FAILED' }
if (-not $Completed) { throw 'F2B_O2_VALIDATION_INCOMPLETE' }

$SqliteHash = (Get-FileHash -LiteralPath $SqliteSql -Algorithm SHA256).Hash.ToLowerInvariant()
$PostgreSqlHash = (Get-FileHash -LiteralPath $PostgreSqlSql -Algorithm SHA256).Hash.ToLowerInvariant()
$RootCaHash = (Get-FileHash -LiteralPath $RootCaPath -Algorithm SHA256).Hash.ToLowerInvariant()
$RuntimeDllHash = (Get-FileHash -LiteralPath $RuntimeDllEvidence -Algorithm SHA256).Hash.ToLowerInvariant()

$HashPaths = @(
    'apps/api.RuntimeTests/Program.cs',
    'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj',
    'apps/api.RuntimeTests/F2bOutboxO2MigrationContract.cs',
    'apps/api/Migrations/20261115101409_F2bIdentityEmailOutbox.cs',
    'apps/api/Migrations/20261115101409_F2bIdentityEmailOutbox.Designer.cs',
    'apps/api/Migrations/DirectoryDbModelSnapshot.cs',
    'apps/api.Migrations.PostgreSql/Migrations/20261115101421_F2bIdentityEmailOutbox.cs',
    'apps/api.Migrations.PostgreSql/Migrations/20261115101421_F2bIdentityEmailOutbox.Designer.cs',
    'apps/api.Migrations.PostgreSql/Migrations/DirectoryDbModelSnapshot.cs',
    'apps/api/OutboxIdentityEmailSender.cs',
    'apps/api/ProductionPostgreSqlPolicy.cs',
    'apps/api/appsettings.json',
    'apps/persistence/IdentityEmailOutboxModels.cs',
    'scripts/Invoke-YangSpaceStage.ps1',
    'scripts/Test-F2b-O2-Local.ps1',
    'scripts/Verify-F2b-O2-Postflight.ps1',
    'docs/F2B_O2_VALIDATION_RUNBOOK_20261115.md'
)
$Checksums = @()
foreach ($Relative in $HashPaths) {
    $Full = Join-Path $Repo $Relative
    if (-not (Test-Path -LiteralPath $Full -PathType Leaf)) {
        throw ('F2B_O2_HASH_SOURCE_MISSING=' + $Relative)
    }
    $Checksums += ((Get-FileHash -LiteralPath $Full -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $Relative)
}
foreach ($Relative in @('outbox.sqlite.review.sql','outbox.postgresql.review.sql','YangSpace.Api.RuntimeTests.dll','pg-root-ca.crt')) {
    $Full = Join-Path $Evidence $Relative
    if (-not (Test-Path -LiteralPath $Full -PathType Leaf)) {
        throw ('F2B_O2_HASH_EVIDENCE_MISSING=' + $Relative)
    }
    $Checksums += ((Get-FileHash -LiteralPath $Full -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $Relative)
}
[IO.File]::WriteAllLines((Join-Path $Evidence 'SHA256SUMS.txt'),[string[]]$Checksums,$Utf8NoBom)

$Manifest = @(
    "SOURCE_SHA=$CommitSha",
    "BRANCH=$Branch",
    "SQLITE_BASELINE=$SqliteBaseline",
    "SQLITE_TARGET=$SqliteTarget",
    "POSTGRESQL_BASELINE=$PostgreSqlBaseline",
    "POSTGRESQL_TARGET=$PostgreSqlTarget",
    'EF_TOOL_VERSION=10.0.0',
    "DOCKER_SERVER_VERSION=$DockerVersion",
    "POSTGRES_IMAGE_ID=$PgImageId",
    "POSTGRES_SERVER_VERSION_NUM=$PgVersionNum",
    "POSTGRES_CONTAINER=$Container",
    "POSTGRES_NETWORK=$Network",
    'POSTGRES_NETWORK_INTERNAL=PASS',
    'POSTGRES_HOST_PORT_PUBLISHED=none',
    'POSTGRES_RUNTIME_EXECUTION=IN_DISPOSABLE_CONTAINER',
    'POSTGRES_RUNTIME_CLIENT=SELF_CONTAINED_LINUX_X64',
    'POSTGRES_RUNTIME_CONNECTION=LOCALHOST_5432',
    'POSTGRES_HOST_BINDS=none',
    'POSTGRES_ANONYMOUS_VOLUMES=none',
    'POSTGRES_IMAGE_DECLARED_VOLUME=/var/lib/postgresql/data',
    'POSTGRES_IMAGE_VOLUME_OVERRIDE=EXPLICIT_TMPFS',
    'POSTGRES_TLS_INITDB_CONFIG=PASS',
    'POSTGRES_HBA_POLICY_ACTIVE=PASS',
    'POSTGRES_CONTAINER_RESTARTS=0',
    'POSTGRES_DATA_STORAGE=tmpfs',
    'POSTGRES_TMPFS_MOUNTS=PASS',
    'POSTGRES_HBA_POLICY=HOSTSSL_SCRAM_HOSTNOSSL_REJECT',
    'POSTGRES_TLS=VERIFY_FULL',
    'POSTGRES_MIGRATOR=ys_o2_migrator',
    'POSTGRES_MIGRATOR_SUPERUSER=false',
    'POSTGRES_RUNTIME=ys_o2_runtime',
    'POSTGRES_RUNTIME_SUPERUSER=false',
    'POSTGRES_RUNTIME_PRIVILEGES=OUTBOX_DML_ONLY',
    'SQLITE_APPLY_ROLLBACK_REAPPLY=PASS',
    'POSTGRES_APPLY_ROLLBACK_REAPPLY=PASS',
    'INTEGRITY=PASS',
    'CONCURRENCY=PASS',
    'PENDING_COALESCING=PASS',
    'UNIQUE_PARTIAL_INDEX=PASS',
    'FK_AND_ORPHAN_REJECTION=PASS',
    'MIGRATOR_LEAST_PRIVILEGE=PASS',
    'RUNTIME_DML_ONLY=PASS',
    'TLS_HOSTNAME_AND_CA_NEGATIVES=PASS',
    'SQLITE_PATH_SCOPED=PASS',
    'SQLITE_DATABASE_CLEANUP=PASS',
    'POSTGRES_DB_SCOPE_SENTINEL=PASS',
    'POSTGRES_CONTAINER_CLEANUP=PASS',
    'POSTGRES_NETWORK_CLEANUP=PASS',
    'TEMPORARY_SECRETS_CLEANUP=PASS',
    "SQLITE_SQL_SHA256=$SqliteHash",
    "POSTGRES_SQL_SHA256=$PostgreSqlHash",
    "POSTGRES_ROOT_CA_SHA256=$RootCaHash",
    "RUNTIME_DLL_SHA256=$RuntimeDllHash",
    'PUBLIC_ACCOUNTS=OFF',
    'DISPATCHER=FAIL_CLOSED',
    'SMTP=NOT_CONNECTED',
    'CT164_DB=NOT_ACCESSED',
    'PRODUCTION_DB=NOT_ACCESSED',
    'GITHUB_ACTIONS=NOT_USED',
    'RELEASE_GATE=HOLD_CT164_PENDING'
)
[IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]$Manifest,$Utf8NoBom)

$PostStatus = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $PostStatus.Count -ne 0) {
    [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),'F2B_O2_SOURCE_WORKTREE_NOT_CLEAN',$Utf8NoBom)
    throw 'F2B_O2_SOURCE_WORKTREE_NOT_CLEAN'
}

Write-Host 'F2B_O2_SQLITE=PASS'
Write-Host 'F2B_O2_POSTGRESQL_17=PASS'
Write-Host 'F2B_O2_INTEGRITY=PASS'
Write-Host 'F2B_O2_CONCURRENCY=PASS'
Write-Host 'F2B_O2_POSTFLIGHT=READY'
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "EVIDENCE_DIR=$Evidence"
Write-Host ('MANIFEST_SHA256=' + (Get-FileHash -LiteralPath (Join-Path $Evidence 'manifest.txt') -Algorithm SHA256).Hash.ToLowerInvariant())
Write-Host 'CT164_DB=NOT_ACCESSED'
Write-Host 'RELEASE_GATE=HOLD_CT164_PENDING'
