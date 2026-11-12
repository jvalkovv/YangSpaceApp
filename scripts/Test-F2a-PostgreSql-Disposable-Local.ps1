# Windows PowerShell 5.1; disposable PostgreSQL schema/constraints/restore QA only.
# Refuses stale/dirty Git, nonlocal Docker contexts, missing reviewed SQL hash.
# Never accesses CT164/CT152, publishes ports, binds host files or enables merchant APIs.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2a1-domain-foundation-20261111'
$Project = 'apps/api.Migrations.PostgreSql/YangSpace.PostgreSql.Migrations.csproj'
$ReviewedSqlSha256 = '2c6175976c82463eea5d40a3082528b8d72e1505f8803188a3c4473129bb7ee7'
$Image = 'postgres:17'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$Docker = (Get-Command docker.exe -ErrorAction Stop).Source

function GitValue {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]$CommandArgs
    )
    # Explicit named array binding avoids Windows PowerShell 5.1
    # positional expansion that dropped HEAD from rev-parse.
    $Output = @(& $Git -C $Repo @CommandArgs)
    if ($LASTEXITCODE -ne 0) {
        throw ('PG_GIT_CHECK_FAILED=' + ($CommandArgs -join ' '))
    }
    return ($Output -join '').Trim()
}
if ((GitValue -CommandArgs @('rev-parse','HEAD')) -cne $CommitSha) {
    throw 'PG_EXACT_SOURCE_SHA_MISMATCH'
}
if ((GitValue -CommandArgs @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'PG_REMOTE_TIP_SHA_MISMATCH'
}
$CurrentBranch = GitValue -CommandArgs @('branch','--show-current')
if ($CurrentBranch -and $CurrentBranch -cne $Branch) {
    throw 'PG_WORKTREE_BRANCH_MISMATCH'
}
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) { throw 'PG_WORKTREE_NOT_CLEAN' }
Write-Host 'PG_GIT_SOURCE_IDENTITY=PASS'

$HostOverride = [Environment]::GetEnvironmentVariable('DOCKER_HOST','Process')
if ($HostOverride -and $HostOverride -notlike 'npipe://*') {
    throw 'PG_REMOTE_DOCKER_HOST_REFUSED'
}
$Endpoint = (@(& $Docker context inspect --format '{{.Endpoints.docker.Host}}') -join '').Trim()
if ($LASTEXITCODE -ne 0 -or $Endpoint -notlike 'npipe://*') {
    throw 'PG_DOCKER_CONTEXT_NOT_LOCAL'
}
$DockerOs = (@(& $Docker info --format '{{.OSType}}') -join '').Trim()
if ($LASTEXITCODE -ne 0 -or $DockerOs -cne 'linux') {
    throw 'PG_DOCKER_LINUX_ENGINE_UNAVAILABLE'
}
$DockerVersion = (@(& $Docker info --format '{{.ServerVersion}}') -join '').Trim()
if ($LASTEXITCODE -ne 0 -or -not $DockerVersion) {
    throw 'PG_DOCKER_VERSION_UNKNOWN'
}
$ImageId = (@(& $Docker image inspect --format '{{.Id}}' $Image) -join '').Trim()
if ($LASTEXITCODE -ne 0 -or $ImageId -notmatch '^sha256:[0-9a-f]{64}$') {
    throw 'PG_IMAGE_NOT_PREPARED_PULL_POSTGRES_17'
}

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' +
    [Guid]::NewGuid().ToString('N').Substring(0,8)
$Evidence = Join-Path $Repo ('logs\yangspace\pg-disposable\' +
    $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
function Stage([string]$Label,[string]$Executable,[string[]]$Arguments) {
    $Options = @{
        Label=$Label
        Executable=$Executable
        CommandArguments=$Arguments
        WorkingDirectory=$Repo
        EvidenceDirectory=$Evidence
        TimeoutSeconds=900
    }
    Invoke-YangSpaceStage @Options
}
function RequireMarker([string]$LogName,[string]$Marker) {
    $Log = Join-Path $Evidence $LogName
    if (-not (Test-Path -LiteralPath $Log -PathType Leaf)) {
        throw ('PG_MARKER_LOG_MISSING=' + $LogName)
    }
    if ([IO.File]::ReadAllText($Log) -notmatch
        ('(?m)^' + [regex]::Escape($Marker) + '\s*$')) {
        throw ('PG_MARKER_MISSING=' + $Marker)
    }
}

$Container = 'yangspace-f2a-pgqa-' + [Guid]::NewGuid().ToString('N').Substring(0,12)
$RunAttempted = $false
$Completed = $false
$CleanupOk = $true
$OldDesignTime = [Environment]::GetEnvironmentVariable(
    'YANGSPACE_EF_POSTGRESQL_DESIGNTIME','Process'
)
try {
    [Environment]::SetEnvironmentVariable(
        'YANGSPACE_EF_POSTGRESQL_DESIGNTIME','CONFIRMED','Process'
    )
    Stage -Label 'pg-ef-tool-restore' -Executable $Dotnet -Arguments @(
        'tool','restore','--tool-manifest','.config/dotnet-tools.json'
    )
    Stage -Label 'pg-ef-restore' -Executable $Dotnet -Arguments @('restore',$Project,'-warnaserror')
    Stage -Label 'pg-ef-build' -Executable $Dotnet -Arguments @(
        'build',$Project,'-c','Release','--no-restore','-warnaserror'
    )
    Stage -Label 'pg-ef-model-diff' -Executable $Dotnet -Arguments @(
        'ef','migrations','has-pending-model-changes','--project',$Project,
        '--startup-project',$Project,'--context','DirectoryDb',
        '--configuration','Release','--no-build'
    )
    $Sql = Join-Path $Evidence 'initial-reviewed.sql'
    Stage -Label 'pg-ef-script' -Executable $Dotnet -Arguments @(
        'ef','migrations','script','0','--project',$Project,
        '--startup-project',$Project,'--context','DirectoryDb',
        '--configuration','Release','--no-build','--output',$Sql
    )
    $SqlHash = (Get-FileHash -LiteralPath $Sql -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($SqlHash -cne $ReviewedSqlSha256) {
        throw ('PG_REVIEWED_SQL_HASH_MISMATCH=' + $SqlHash)
    }
    Write-Host 'PG_REVIEWED_SQL_SHA256=PASS'

    # No network or host binds; temporary PostgreSQL data lives on tmpfs.
    $RunAttempted = $true
    Stage -Label 'pg-container-start' -Executable $Docker -Arguments @(
        'run','-d','--rm','--network','none',
        '--tmpfs','/var/lib/postgresql/data:rw,size=512m',
        '--name',$Container,
        '-e','POSTGRES_HOST_AUTH_METHOD=trust',$Image
    )
    $Network = (@(& $Docker inspect --format '{{.HostConfig.NetworkMode}}' $Container) -join '').Trim()
    if ($LASTEXITCODE -ne 0 -or $Network -cne 'none') {
        throw 'PG_CONTAINER_NETWORK_ISOLATION_FAILED'
    }
    $Ports = (@(& $Docker inspect --format '{{json .HostConfig.PortBindings}}' $Container) -join '').Trim()
    if ($LASTEXITCODE -ne 0 -or $Ports -notin @('null','{}')) {
        throw 'PG_CONTAINER_PUBLISHED_PORTS_REFUSED'
    }
    $Bindings = (@(& $Docker inspect --format '{{json .HostConfig.Binds}}' $Container) -join '').Trim()
    if ($LASTEXITCODE -ne 0 -or $Bindings -notin @('null','[]')) {
        throw 'PG_CONTAINER_BIND_MOUNTS_REFUSED'
    }

    $Ready = $false
    for ($Try = 0; $Try -lt 60; $Try++) {
        $OldErrorMode = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $ProbeOutput = @(& $Docker exec $Container pg_isready -U postgres -d postgres 2>&1)
            $ProbeExit = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $OldErrorMode
        }
        if ($ProbeExit -eq 0) { $Ready = $true; break }
        Start-Sleep -Seconds 2
    }
    if (-not $Ready) { throw 'PG_CONTAINER_STARTUP_FAILED' }
    Write-Host 'PG_CONTAINER_READY=PASS'

    Stage -Label 'pg-copy-initial' -Executable $Docker -Arguments @('cp',$Sql,($Container + ':/tmp/initial.sql'))
    Stage -Label 'pg-apply-reviewed-initial' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-f','/tmp/initial.sql'
    )
    $CheckSql = Join-Path $Repo 'scripts\postgresql\F2a-Disposable-Constraints.sql'
    Stage -Label 'pg-copy-constraints' -Executable $Docker -Arguments @('cp',$CheckSql,($Container + ':/tmp/constraints.sql'))
    Stage -Label 'pg-test-constraints' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','postgres','-f','/tmp/constraints.sql'
    )
    RequireMarker 'pg-test-constraints.stdout.log' 'F2A_PG_CONSTRAINTS=PASS'
    RequireMarker 'pg-test-constraints.stdout.log' 'F2A_PG_SEQUENTIAL_CAS=PASS'
    RequireMarker 'pg-test-constraints.stdout.log' 'F2A_PG_DB_AUDIT_ROLLBACK=PASS'

    Stage -Label 'pg-dump-synthetic' -Executable $Docker -Arguments @(
        'exec',$Container,'pg_dump','-U','postgres','-Fc',
        '-f','/tmp/synthetic.backup','postgres'
    )
    $Backup = Join-Path $Evidence 'synthetic-postgresql.backup'
    Stage -Label 'pg-copy-synthetic-backup' -Executable $Docker -Arguments @(
        'cp',($Container + ':/tmp/synthetic.backup'),$Backup
    )
    if (-not (Test-Path -LiteralPath $Backup -PathType Leaf) -or
        (Get-Item -LiteralPath $Backup).Length -lt 1000) {
        throw 'PG_SYNTHETIC_BACKUP_NOT_CREATED'
    }
    Stage -Label 'pg-create-restore-db' -Executable $Docker -Arguments @(
        'exec',$Container,'createdb','-U','postgres','f2a_restored'
    )
    Stage -Label 'pg-restore-synthetic' -Executable $Docker -Arguments @(
        'exec',$Container,'pg_restore','--exit-on-error','--no-owner',
        '--no-privileges','-U','postgres','-d','f2a_restored','/tmp/synthetic.backup'
    )
    $RestoreSql = Join-Path $Repo 'scripts\postgresql\F2a-Disposable-Restore-Verify.sql'
    Stage -Label 'pg-copy-restore-check' -Executable $Docker -Arguments @(
        'cp',$RestoreSql,($Container + ':/tmp/restore.sql')
    )
    Stage -Label 'pg-verify-restored' -Executable $Docker -Arguments @(
        'exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1',
        '-U','postgres','-d','f2a_restored','-f','/tmp/restore.sql'
    )
    RequireMarker 'pg-verify-restored.stdout.log' 'F2A_PG_RESTORE=PASS'
    $BackupHash = (Get-FileHash -LiteralPath $Backup -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]@(
        ('COMMIT_SHA=' + $CommitSha),
        ('PG_DOCKER_VERSION=' + $DockerVersion),
        ('PG_IMAGE_ID=' + $ImageId),
        ('PG_SQL_SHA256=' + $SqlHash),
        ('PG_BACKUP_SHA256=' + $BackupHash),
        'NETWORK=none',
        'PUBLISHED_PORTS=none',
        'PG_MIGRATION_APPLY=PASS_DISPOSABLE_ONLY',
        'F2A_PG_CONSTRAINTS=PASS',
        'F2A_PG_SEQUENTIAL_CAS=PASS',
        'F2A_PG_DB_AUDIT_ROLLBACK=PASS',
        'F2A_PG_RESTORE=PASS_DISPOSABLE_ONLY',
        'REAL_BEARER=NOT_TESTED',
        'PARALLEL_WRITERS=NOT_TESTED',
        'APP_ROLE_LEAST_PRIVILEGE=NOT_TESTED',
        'CT164_DB=NOT_ACCESSED',
        'RELEASE_GATE=HOLD'
    ))
    $Completed = $true
}
finally {
    [Environment]::SetEnvironmentVariable(
        'YANGSPACE_EF_POSTGRESQL_DESIGNTIME',$OldDesignTime,'Process'
    )
    if ($RunAttempted) {
        $OldErrorMode = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $Removal = @(& $Docker rm -f $Container 2>&1)
            if ($LASTEXITCODE -ne 0) {
                $CleanupOk = $false
                Write-Warning ('PG_CONTAINER_CLEANUP_FAILED=' + $Container)
            }
        }
        finally {
            $ErrorActionPreference = $OldErrorMode
        }
    }
    if (-not $Completed -or -not $CleanupOk) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),
            'F2A_PG_DISPOSABLE_RUNTIME=HOLD')
    }
}
if (-not $CleanupOk) { throw 'PG_CONTAINER_CLEANUP_FAILED' }
if (-not $Completed) { throw 'PG_DISPOSABLE_RUNTIME_INCOMPLETE' }
Write-Host 'F2A_PG_DISPOSABLE_RUNTIME=PASS'
Write-Host ('COMMIT_SHA=' + $CommitSha)
Write-Host ('EVIDENCE_DIR=' + $Evidence)
Write-Host 'SCOPE=PG_MIGRATION_CONSTRAINTS_SEQUENTIAL_CAS_ROLLBACK_RESTORE'
Write-Host 'REAL_BEARER_PARALLEL_WRITERS_PRODUCTION=NOT_TESTED'
