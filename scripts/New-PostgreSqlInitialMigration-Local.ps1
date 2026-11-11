# Scaffold ONLY the initial PostgreSQL EF migration and review SQL.
# No DB connection, schema apply, data copy, PVE or production operation.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2a1-domain-foundation-20261111'
$Project = 'apps/api.Migrations.PostgreSql/YangSpace.PostgreSql.Migrations.csproj'
$MigrationDir = Join-Path $Repo 'apps\api.Migrations.PostgreSql\Migrations'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source

if ($CommitSha -cnotmatch '^[0-9a-f]{40}$') { throw 'FULL_SHA_REQUIRED' }
# PowerShell 5.1 emits $null for native commands with no stdout;
# detached HEAD is a legitimate checkout only for the exact remote branch tip.
$HeadOutput = @(& $Git -C $Repo rev-parse HEAD)
$HeadExit = $LASTEXITCODE
$Head = ($HeadOutput -join '').Trim()
if ($HeadExit -ne 0 -or $Head -cne $CommitSha) { throw 'SOURCE_SHA_MISMATCH' }

$RemoteOutput = @(& $Git -C $Repo rev-parse --verify --quiet ('refs/remotes/origin/' + $Branch))
$RemoteExit = $LASTEXITCODE
$Remote = ($RemoteOutput -join '').Trim()
if ($RemoteExit -ne 0 -or $Remote -cne $CommitSha) { throw 'REMOTE_SHA_MISMATCH' }

$BranchOutput = @(& $Git -C $Repo branch --show-current)
$BranchExit = $LASTEXITCODE
$CurrentBranch = ($BranchOutput -join '').Trim()
if ($BranchExit -ne 0 -or ($CurrentBranch -and $CurrentBranch -cne $Branch)) {
    throw 'WORKTREE_BRANCH_MISMATCH'
}
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) { throw 'WORKTREE_NOT_CLEAN' }
if (Test-Path -LiteralPath $MigrationDir) { throw 'POSTGRESQL_MIGRATION_DIR_ALREADY_EXISTS' }
if (-not (Test-Path -LiteralPath (Join-Path $Repo '.config\dotnet-tools.json'))) { throw 'PINNED_EF_TOOL_MANIFEST_MISSING' }

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$Evidence = Join-Path $Repo ('logs\yangspace\postgresql-migration-scaffold\' + $CommitSha.Substring(0, 12) + '-' + $RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
function Stage([string]$Label, [string[]]$Arguments) {
    if ($null -eq $Arguments -or $Arguments.Count -eq 0) { throw ('EMPTY_STAGE_ARGUMENTS=' + $Label) }
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $Arguments -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 900
}

$PriorGate = [Environment]::GetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME', 'Process')
$Pass = $false
try {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME', 'CONFIRMED', 'Process')
    Stage 'ef-tool-restore' @('tool', 'restore', '--tool-manifest', '.config/dotnet-tools.json')
    Stage 'ef-tool-version' @('ef', '--version')
    $Version = [IO.File]::ReadAllText((Join-Path $Evidence 'ef-tool-version.stdout.log'))
    if ($Version -notmatch '10\.0\.0') { throw 'EF_TOOL_VERSION_MISMATCH' }

    Stage 'pg-migrations-restore' @('restore', $Project, '-warnaserror')
    Stage 'pg-migrations-build-before' @('build', $Project, '-c', 'Release', '--no-restore', '-warnaserror')
    Stage 'pg-initial-migration-add' @(
        'ef', 'migrations', 'add', 'InitialPostgreSqlSchema',
        '--project', $Project, '--startup-project', $Project,
        '--context', 'DirectoryDb', '--output-dir', 'Migrations',
        '--namespace', 'YangSpace.PostgreSql.Migrations',
        '--configuration', 'Release', '--no-build'
    )

    $Snapshot = Join-Path $MigrationDir 'DirectoryDbModelSnapshot.cs'
    $NamespaceDir = Join-Path $Repo 'apps\api.Migrations.PostgreSql\YangSpace\PostgreSql\Migrations'
    $NamespaceSnapshot = Join-Path $NamespaceDir 'DirectoryDbModelSnapshot.cs'
    if (Test-Path -LiteralPath $NamespaceDir) {
        $Entries = @(Get-ChildItem -LiteralPath $NamespaceDir -Force)
        if ($Entries.Count -ne 1 -or $Entries[0].Name -cne 'DirectoryDbModelSnapshot.cs' -or $Entries[0].PSIsContainer -or (Test-Path -LiteralPath $Snapshot)) {
            throw 'UNEXPECTED_NAMESPACE_SNAPSHOT_OUTPUT'
        }
        New-Item -ItemType Directory -Force -Path $MigrationDir | Out-Null
        Move-Item -LiteralPath $NamespaceSnapshot -Destination $Snapshot
        Remove-Item -LiteralPath $NamespaceDir -Force
    }

    $Generated = @(Get-ChildItem -LiteralPath $MigrationDir -File -Filter '*.cs')
    if ($Generated.Count -ne 3 -or
        @($Generated | Where-Object { $_.Name -match '^([0-9]{14})_InitialPostgreSqlSchema\.cs$' }).Count -ne 1 -or
        @($Generated | Where-Object { $_.Name -match '^[0-9]{14}_InitialPostgreSqlSchema\.Designer\.cs$' }).Count -ne 1 -or
        @($Generated | Where-Object { $_.Name -ceq 'DirectoryDbModelSnapshot.cs' }).Count -ne 1) {
        throw 'EXPECTED_THREE_EF_GENERATED_POSTGRESQL_FILES'
    }

    $Changes = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $Changes.Count -ne 3 -or
        @($Changes | Where-Object { $_ -cnotmatch '^\?\? apps/api\.Migrations\.PostgreSql/Migrations/.*\.cs$' }).Count -ne 0) {
        throw 'UNEXPECTED_WORKTREE_CHANGES_AFTER_SCAFFOLD'
    }

    Stage 'pg-migrations-build-after' @('build', $Project, '-c', 'Release', '--no-restore', '-warnaserror')
    Stage 'pg-model-diff' @(
        'ef', 'migrations', 'has-pending-model-changes',
        '--project', $Project, '--startup-project', $Project,
        '--context', 'DirectoryDb', '--configuration', 'Release', '--no-build'
    )

    $Sql = Join-Path $Evidence 'InitialPostgreSqlSchema.review.sql'
    Stage 'pg-initial-sql-script' @(
        'ef', 'migrations', 'script', '0',
        '--project', $Project, '--startup-project', $Project,
        '--context', 'DirectoryDb', '--configuration', 'Release',
        '--no-build', '--output', $Sql
    )
    if (-not (Test-Path -LiteralPath $Sql -PathType Leaf) -or (Get-Item -LiteralPath $Sql).Length -lt 5000) {
        throw 'POSTGRESQL_REVIEW_SQL_MISSING_OR_TOO_SMALL'
    }
    $SqlText = [IO.File]::ReadAllText($Sql)
    foreach ($Table in @(
        'AspNetUsers', 'AspNetRoles', 'AspNetUserClaims', 'AspNetUserLogins',
        'AspNetUserRoles', 'AspNetUserTokens', 'AspNetRoleClaims',
        'Locations', 'Offers', 'Brochures', 'Jobs',
        'Organizations', 'OrganizationMembers', 'BusinessClaims', 'ContentAuditEvents'
    )) {
        $Quoted = [regex]::Escape('"' + $Table + '"')
        if ($SqlText -notmatch ('(?im)\bCREATE\s+TABLE\s+(?:(?:"?public"?)\.)?' + $Quoted)) {
            throw ('POSTGRESQL_SQL_TABLE_MISSING=' + $Table)
        }
    }
    if ($SqlText -match '(?im)^\s*(DROP\s+(TABLE|INDEX|COLUMN)|DELETE\s+FROM|UPDATE\s+)' ) {
        throw 'UNEXPECTED_DESTRUCTIVE_STATEMENT_IN_INITIAL_POSTGRESQL_SQL'
    }

    $Hashes = @($Generated | Sort-Object Name | ForEach-Object {
        (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash.ToLowerInvariant() +
            '  apps/api.Migrations.PostgreSql/Migrations/' + $_.Name
    })
    $Hashes += ((Get-FileHash -Algorithm SHA256 -LiteralPath $Sql).Hash.ToLowerInvariant() + '  InitialPostgreSqlSchema.review.sql')
    [IO.File]::WriteAllLines((Join-Path $Evidence 'SHA256SUMS.txt'), [string[]]$Hashes)
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'), [string[]]@(
        'SOURCE_SHA=' + $CommitSha
        'EF_TOOL_VERSION=10.0.0'
        'POSTGRESQL_INITIAL_MIGRATION_SCAFFOLD=PASS'
        'MODEL_SNAPSHOT_DIFF=NONE'
        'SQL_REVIEW=REQUIRED'
        'POSTGRESQL_CONNECTION=NOT_OPENED'
        'DATABASE_APPLY=NOT_RUN'
        'SQLITE_DATABASE=NOT_ACCESSED'
        'DATA_COPY=NOT_RUN'
        'CT164_DEPLOYMENT=NONE'
    ))
    $Pass = $true
    Write-Host 'POSTGRESQL_INITIAL_MIGRATION_SCAFFOLD=PASS'
    Write-Host 'MODEL_SNAPSHOT_DIFF=NONE'
    Write-Host ('GENERATED_DIR=' + $MigrationDir)
    Write-Host ('EVIDENCE_DIR=' + $Evidence)
    Write-Host 'NEXT=Independent SQL review and isolated PostgreSQL fresh-schema/restore tests; no apply'
}
finally {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME', $PriorGate, 'Process')
    if (-not $Pass) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'), 'POSTGRESQL_INITIAL_MIGRATION_SCAFFOLD=HOLD')
        Write-Warning ('POSTGRESQL_INITIAL_MIGRATION_SCAFFOLD=HOLD; EVIDENCE_DIR=' + $Evidence)
    }
}
