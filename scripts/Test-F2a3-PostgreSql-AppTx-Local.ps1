# Windows PowerShell 5.1, exact-SHA local-only disposable PostgreSQL EF transaction QA.
# Two temporary containers: PG --network none and .NET --network container:PG.
# No published ports, host binds, PVE/CT, production DB or merchant HTTP routes.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2a3-pg-transaction-qa-20261112'
$MigrationsProject = 'apps/api.Migrations.PostgreSql/YangSpace.PostgreSql.Migrations.csproj'
$RuntimeProject = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$ReviewedSqlSha = '2c6175976c82463eea5d40a3082528b8d72e1505f8803188a3c4473129bb7ee7'
$PgImage = 'postgres:17'
$RuntimeImage = 'mcr.microsoft.com/dotnet/aspnet:10.0'
$Database = 'yangspace_f2a3_disposable'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$Docker = (Get-Command docker.exe -ErrorAction Stop).Source
$Tar = (Get-Command tar.exe -ErrorAction Stop).Source

function GitValue {
    param([Parameter(Mandatory=$true)][string[]]$CommandArgs)
    $Output = @(& $Git -C $Repo @CommandArgs)
    if ($LASTEXITCODE -ne 0) { throw ('F2A3_PG_GIT_FAILED=' + ($CommandArgs -join ' ')) }
    return ($Output -join '').Trim()
}
if ((GitValue -CommandArgs @('rev-parse','HEAD')) -cne $CommitSha) {
    throw 'F2A3_PG_HEAD_MISMATCH'
}
if ((GitValue -CommandArgs @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'F2A3_PG_REMOTE_BRANCH_MISMATCH'
}
$CurrentBranch = GitValue -CommandArgs @('branch','--show-current')
if ($CurrentBranch -and $CurrentBranch -cne $Branch) {
    throw 'F2A3_PG_WORKTREE_BRANCH_MISMATCH'
}
$PreStatus = @(& $Git -C $Repo status --porcelain --untracked-files=all |
    Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $PreStatus.Count -ne 0) {
    throw 'F2A3_PG_WORKTREE_DIRTY'
}
Write-Host 'F2A3_PG_EXACT_SOURCE=PASS'

$Override = [Environment]::GetEnvironmentVariable('DOCKER_HOST','Process')
if ($Override -and $Override -notlike 'npipe://*') { throw 'F2A3_PG_REMOTE_DOCKER_HOST_REFUSED' }
$Endpoint = (@(& $Docker context inspect --format '{{.Endpoints.docker.Host}}') -join '').Trim()
if ($LASTEXITCODE -ne 0 -or $Endpoint -notlike 'npipe://*') {
    throw 'F2A3_PG_DOCKER_CONTEXT_NOT_LOCAL'
}
$DockerOs = (@(& $Docker info --format '{{.OSType}}') -join '').Trim()
if ($LASTEXITCODE -ne 0 -or $DockerOs -cne 'linux') {
    throw 'F2A3_PG_LOCAL_LINUX_ENGINE_REQUIRED'
}
$PgImageId = (@(& $Docker image inspect --format '{{.Id}}' $PgImage) -join '').Trim()
if ($LASTEXITCODE -ne 0 -or $PgImageId -notmatch '^sha256:[a-f0-9]{64}$') {
    throw 'F2A3_PG_IMAGE_MISSING_PULL_POSTGRES_17_FIRST'
}
$RuntimeImageId = (@(& $Docker image inspect --format '{{.Id}}' $RuntimeImage) -join '').Trim()
if ($LASTEXITCODE -ne 0 -or $RuntimeImageId -notmatch '^sha256:[a-f0-9]{64}$') {
    throw 'F2A3_RUNTIME_IMAGE_MISSING_PULL_ASPNET_10_FIRST'
}
Write-Host 'F2A3_PG_LOCAL_IMAGES=PASS'

$Token = [Guid]::NewGuid().ToString('N')
$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + $Token.Substring(0,8)
$Evidence = Join-Path $Repo ('logs\yangspace\f2a3-pg-apptx\' +
    $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
$Publish = Join-Path $Evidence 'publish'
$Sql = Join-Path $Evidence 'reviewed-initial.sql'
$Archive = Join-Path $Evidence 'runtime-publish.tar'
$Pg = 'yangspace-f2a3-pgqa-' + $Token.Substring(0,12)
$App = 'yangspace-f2a3-appqa-' + $Token.Substring(0,12)
$Conn = 'Host=127.0.0.1;Port=5432;Database=' + $Database +
        ';Username=postgres;SSL Mode=Disable;Pooling=false;Timeout=5;Command Timeout=10'
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceTarStreamStage.ps1')

function Stage {
    param([string]$Label,[string]$Executable,[string[]]$Arguments)
    Invoke-YangSpaceStage -Label $Label -Executable $Executable -CommandArguments $Arguments -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 900
}
function RequireMarker {
    param([string]$Label,[string]$Marker)
    $Log = Join-Path $Evidence ($Label + '.stdout.log')
    if (-not (Test-Path -LiteralPath $Log -PathType Leaf)) {
        throw ('F2A3_PG_LOG_MISSING=' + $Label)
    }
    if ([IO.File]::ReadAllText($Log) -notmatch
        ('(?m)^' + [regex]::Escape($Marker) + '\s*$')) {
        throw ('F2A3_PG_MARKER_MISSING=' + $Marker)
    }
}
function AssertContainerIsolation {
    param([string]$Name,[string]$ExpectedNetwork)
    $Network = (@(& $Docker inspect --format '{{.HostConfig.NetworkMode}}' $Name) -join '').Trim()
    $NetworkExit = $LASTEXITCODE
    $AllowedNetworks = @($ExpectedNetwork)
    if ($ExpectedNetwork -like 'container:*') {
        $TargetName = $ExpectedNetwork.Substring('container:'.Length)
        $TargetId = (@(& $Docker inspect --format '{{.Id}}' $TargetName) -join '').Trim()
        if ($LASTEXITCODE -ne 0 -or $TargetId -notmatch '^[0-9a-f]{64}$') {
            throw 'F2A3_PG_NETWORK_TARGET_NOT_VERIFIED'
        }
        $AllowedNetworks += ('container:' + $TargetId)
    }
    if ($NetworkExit -ne 0 -or $Network -cnotin $AllowedNetworks) {
        throw ('F2A3_PG_CONTAINER_NETWORK_REFUSED=' + $Name)
    }
    $Ports = (@(& $Docker inspect --format '{{json .HostConfig.PortBindings}}' $Name) -join '').Trim()
    if ($LASTEXITCODE -ne 0 -or $Ports -notin @('null','{}')) {
        throw ('F2A3_PG_CONTAINER_PORT_BINDINGS_REFUSED=' + $Name)
    }
    $Mounts = (@(& $Docker inspect --format '{{json .HostConfig.Binds}}' $Name) -join '').Trim()
    if ($LASTEXITCODE -ne 0 -or $Mounts -notin @('null','[]')) {
        throw ('F2A3_PG_CONTAINER_HOST_BINDS_REFUSED=' + $Name)
    }
}

$OldDesignTime = [Environment]::GetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME','Process')
$PgAttempted = $false
$AppAttempted = $false
$Completed = $false
$CleanupOk = $true
try {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME','CONFIRMED','Process')
    Stage -Label 'pgapp-ef-tool-restore' -Executable $Dotnet -Arguments @(
        'tool','restore','--tool-manifest','.config/dotnet-tools.json'
    )
    Stage -Label 'pgapp-ef-restore' -Executable $Dotnet -Arguments @('restore',$MigrationsProject,'-warnaserror')
    Stage -Label 'pgapp-ef-build' -Executable $Dotnet -Arguments @(
        'build',$MigrationsProject,'-c','Release','--no-restore','-warnaserror'
    )
    Stage -Label 'pgapp-ef-model-diff' -Executable $Dotnet -Arguments @(
        'ef','migrations','has-pending-model-changes','--project',$MigrationsProject,
        '--startup-project',$MigrationsProject,'--context','DirectoryDb',
        '--configuration','Release','--no-build'
    )
    Stage -Label 'pgapp-ef-script' -Executable $Dotnet -Arguments @(
        'ef','migrations','script','0','--project',$MigrationsProject,
        '--startup-project',$MigrationsProject,'--context','DirectoryDb',
        '--configuration','Release','--no-build','--output',$Sql
    )
    $SqlHash = (Get-FileHash -LiteralPath $Sql -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($SqlHash -cne $ReviewedSqlSha) {
        throw ('F2A3_PG_REVIEWED_SQL_HASH_REFUSED=' + $SqlHash)
    }
    Write-Host 'F2A3_PG_REVIEWED_SQL_HASH=PASS'

    Stage -Label 'pgapp-runtime-restore' -Executable $Dotnet -Arguments @(
        'restore',$RuntimeProject,'-warnaserror'
    )
    Stage -Label 'pgapp-runtime-publish' -Executable $Dotnet -Arguments @(
        'publish',$RuntimeProject,'-c','Release','--no-restore',
        '-warnaserror','-o',$Publish
    )
    $Dll = Join-Path $Publish 'YangSpace.Api.RuntimeTests.dll'
    if (!(Test-Path -LiteralPath $Dll -PathType Leaf)) {
        throw 'F2A3_PG_PUBLISH_OUTPUT_MISSING'
    }
    $DllHash = (Get-FileHash -LiteralPath $Dll -Algorithm SHA256).Hash.ToLowerInvariant()
    Stage -Label 'pgapp-archive-runtime' -Executable $Tar -Arguments @(
        '-cf',$Archive,'-C',$Publish,'.'
    )
    if (!(Test-Path -LiteralPath $Archive -PathType Leaf) -or
        (Get-Item -LiteralPath $Archive).Length -lt 1024) {
        throw 'F2A3_PG_RUNTIME_ARCHIVE_FAILED'
    }
    $ArchiveHash = (Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash.ToLowerInvariant()

    # The PG server has no network adapters outside loopback, no ports and no
    # host binds. All data is disposable and --rm cleans container storage.
    $PgAttempted = $true
    Stage -Label 'pgapp-container-start' -Executable $Docker -Arguments @(
        'run','-d','--rm','--network','none',
        '--tmpfs','/var/lib/postgresql/data:rw,size=512m',
        '--name',$Pg,'-e','POSTGRES_HOST_AUTH_METHOD=trust',$PgImage
    )
    AssertContainerIsolation -Name $Pg -ExpectedNetwork 'none'

    $Ready = $false
    for ($Try = 0; $Try -lt 60; $Try++) {
        $OldError = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $Probe = @(& $Docker exec $Pg pg_isready -U postgres -d postgres 2>&1)
            $ProbeExit = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $OldError }
        if ($ProbeExit -eq 0) { $Ready = $true; break }
        Start-Sleep -Seconds 2
    }
    if (!$Ready) { throw 'F2A3_PG_SERVER_NOT_READY' }
    Write-Host 'F2A3_PG_SERVER_READY=PASS'

    Stage -Label 'pgapp-create-database' -Executable $Docker -Arguments @(
        'exec',$Pg,'createdb','-U','postgres',$Database
    )
    Stage -Label 'pgapp-copy-reviewed-sql' -Executable $Docker -Arguments @(
        'cp',$Sql,($Pg + ':/tmp/initial.sql')
    )
    Stage -Label 'pgapp-apply-reviewed-sql' -Executable $Docker -Arguments @(
        'exec',$Pg,'psql','-X','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d',$Database,'-f','/tmp/initial.sql'
    )

    # Unique disposable sentinel: a seemingly-valid 127.0.0.1 DB elsewhere
    # cannot be treated as our test server without this exact per-run token.
    $GuardSql = "CREATE TABLE yangspace_f2a3_qa_guard (scope_token TEXT PRIMARY KEY); INSERT INTO yangspace_f2a3_qa_guard(scope_token) VALUES ('$Token');"
    Stage -Label 'pgapp-create-sentinel' -Executable $Docker -Arguments @(
        'exec',$Pg,'psql','-X','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d',$Database,'-c',$GuardSql
    )
    $AppAttempted = $true
    Stage -Label 'pgapp-sidecar-start' -Executable $Docker -Arguments @(
        'run','-d','--rm','--network',('container:' + $Pg),
        '--read-only','--tmpfs','/tmp:rw,size=256m,mode=1777',
        '--name',$App,'--entrypoint','sleep',$RuntimeImage,'1200'
    )
    AssertContainerIsolation -Name $App -ExpectedNetwork ('container:' + $Pg)
    $ReadOnly = (@(& $Docker inspect --format '{{.HostConfig.ReadonlyRootfs}}' $App) -join '').Trim()
    $Tmpfs = (@(& $Docker inspect --format '{{json .HostConfig.Tmpfs}}' $App) -join '').Trim()
    if ($LASTEXITCODE -ne 0 -or $ReadOnly -cne 'true' -or
        $Tmpfs -notmatch '"/tmp"') {
        throw 'F2A3_PG_READONLY_TMPFS_ISOLATION_FAILED'
    }
    Write-Host 'F2A3_PG_TWO_CONTAINERS_ISOLATED=PASS'

    # Docker cp is rejected for containers with --read-only rootfs even when
    # /tmp is tmpfs. Stream the tar bytes through docker exec -i instead.
    Invoke-YangSpaceTarStreamStage -Docker $Docker -Container $App `
        -Archive $Archive -EvidenceDirectory $Evidence
    Stage -Label 'pgapp-verify-runtime-dll' -Executable $Docker -Arguments @(
        'exec',$App,'sha256sum','/tmp/qa/YangSpace.Api.RuntimeTests.dll'
    )
    RequireMarker -Label 'pgapp-verify-runtime-dll' -Marker `
        ($DllHash + '  /tmp/qa/YangSpace.Api.RuntimeTests.dll')
    Write-Host 'F2A3_PG_RUNTIME_TRANSFER_SHA256=PASS'
    Stage -Label 'pgapp-real-ef-transaction' -Executable $Docker -Arguments @(
        'exec',
        '-e','YANGSPACE_F2A3_PG_DISPOSABLE_ONLY=CONFIRMED_CONTAINER_NETNS_ONLY',
        '-e',('YANGSPACE_F2A3_PG_TOKEN=' + $Token),
        '-e',('YANGSPACE_F2A3_PG_CONNECTION=' + $Conn),
        '-e','DOTNET_CLI_HOME=/tmp',
        '-e','DOTNET_EnableDiagnostics=0',
        $App,'dotnet','/tmp/qa/YangSpace.Api.RuntimeTests.dll',
        '--postgres-claim-transaction-only'
    )
    foreach ($Marker in @(
        'F2A3_PG_SENTINEL_MATCHED=PASS',
        'F2A3_PG_LOOPBACK_DISPOSABLE_SCOPE=PASS',
        'F2A3_PG_REVIEWED_MIGRATION_PRESENT=PASS',
        'F2A3_PG_FRESH_SYNTHETIC_STATE=PASS',
        'F2A3_PG_APP_TX_SUCCESS_PERSISTED=PASS',
        'F2A3_PG_APP_TX_AUDIT_CARDINALITY=PASS',
        'F2A3_PG_APP_TX_SECOND_SAVE_AUDIT_REJECTED=PASS',
        'F2A3_PG_APP_TX_OUTER_ROLLBACK_ALL_ROWS=PASS',
        'F2A3_PG_APP_TRANSACTION_ISOLATED_ONLY=PASS'
    )) {
        RequireMarker -Label 'pgapp-real-ef-transaction' -Marker $Marker
    }
    $PostStatus = @(& $Git -C $Repo status --porcelain --untracked-files=all |
        Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $PostStatus.Count -ne 0) {
        throw 'F2A3_PG_SOURCE_CHANGED'
    }
    $Completed = $true
}
finally {
    [Environment]::SetEnvironmentVariable(
        'YANGSPACE_EF_POSTGRESQL_DESIGNTIME',$OldDesignTime,'Process'
    )
    # Remove the .NET sidecar first; it shares PG's isolated network namespace.
    foreach ($Name in @($App,$Pg)) {
        $Attempted = if ($Name -ceq $App) { $AppAttempted } else { $PgAttempted }
        if ($Attempted) {
            $OldError = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            try {
                $Result = @(& $Docker rm -f $Name 2>&1)
                if ($LASTEXITCODE -ne 0) {
                    $CleanupOk = $false
                    Write-Warning ('F2A3_PG_CONTAINER_CLEANUP_FAILED=' + $Name)
                }
            }
            finally { $ErrorActionPreference = $OldError }
        }
    }
    if (!$Completed -or !$CleanupOk) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),
            'F2A3_PG_APP_TRANSACTION_ISOLATED_ONLY=HOLD')
    }
}
if (!$CleanupOk) { throw 'F2A3_PG_CLEANUP_FAILED' }
if (!$Completed) { throw 'F2A3_PG_RUN_INCOMPLETE' }
$Manifest = @(
    ('COMMIT_SHA=' + $CommitSha),
    ('PG_IMAGE_ID=' + $PgImageId),
    ('DOTNET_RUNTIME_IMAGE_ID=' + $RuntimeImageId),
    ('PG_SQL_SHA256=' + $SqlHash),
    ('RUNTIME_ARCHIVE_SHA256=' + $ArchiveHash),
    ('RUNTIME_DLL_SHA256=' + $DllHash),
    'PG_NETWORK=none',
    'DOTNET_NETWORK=container:postgresql',
    'PUBLISHED_PORTS=none',
    'HOST_BINDS=none',
    'RUNTIME_ROOTFS=read-only',
    'RUNTIME_TMPFS=/tmp',
    'RUNTIME_TRANSFER=BINARY_STREAMED_TAR',
    'RUNTIME_TRANSFER_SHA256=PASS',
    'TEST_DATABASE=yangspace_f2a3_disposable',
    'SOURCE_RESTORE_PUBLISH=PASS',
    'REVIEWED_PG_MIGRATION=PASS_DISPOSABLE_ONLY',
    'REAL_NPGSQL_APP_TRANSACTION=PASS_DISPOSABLE_ONLY',
    'SECOND_SAVE_ROLLBACK=PASS_DISPOSABLE_ONLY',
    'CONTAINER_CLEANUP=PASS',
    'REVIEWER_IDENTITY_HTTP=NOT_TESTED',
    'PARALLEL_WRITERS=NOT_TESTED',
    'UNCERTAIN_COMMIT=NOT_TESTED',
    'CLAIM_POLICY=NOT_APPROVED',
    'PRODUCTION=NOT_EXPOSED',
    'CT164_DB=NOT_ACCESSED',
    'CT152=NOT_ACCESSED',
    'RELEASE_GATE=HOLD'
)
[IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]$Manifest)
Write-Host 'F2A3_PG_APP_TRANSACTION_ISOLATED_ONLY=PASS'
Write-Host ('COMMIT_SHA=' + $CommitSha)
Write-Host ('EVIDENCE_DIR=' + $Evidence)
Write-Host 'REVIEWER_HTTP_PARALLEL_PRODUCTION=NOT_TESTED'
