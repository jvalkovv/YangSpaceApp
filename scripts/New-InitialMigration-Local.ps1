# S2-02 — generate EF Core SQLite migration on existing Windows .NET 10.
# NO live database, HTTP listener, PVE, GitHub Actions or schema APPLY.
# Generated C# remains uncommitted until explicit review.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-v2-foundation-20261108'
$MigrationName = 'InitialDirectorySchema'
$MigrationDir = Join-Path $Repo 'apps\api\Migrations'
$Project = 'apps/api/YangSpace.Api.csproj'

function Require-Exe([string]$Name) {
    $item = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $item) { throw ('MISSING_EXECUTABLE=' + $Name) }
    return $item.Source
}
$Git = Require-Exe 'git.exe'
$Dotnet = Require-Exe 'dotnet.exe'
if ($CommitSha -cnotmatch '^[0-9a-f]{40}$') { throw 'FULL_SHA_REQUIRED' }

$ActualHead = (& $Git -C $Repo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $ActualHead -cne $CommitSha) {
    throw 'REFUSED: current worktree does not match pinned SHA'
}
$ActualBranch = (& $Git -C $Repo branch --show-current).Trim()
if ($LASTEXITCODE -ne 0 -or $ActualBranch -cne $Branch) {
    throw 'REFUSED: unexpected Git branch'
}
$RemoteSha = (& $Git -C $Repo rev-parse ('refs/remotes/origin/' + $Branch)).Trim()
if ($LASTEXITCODE -ne 0 -or $RemoteSha -cne $CommitSha) {
    throw 'REFUSED: remote-tracking branch SHA differs'
}
$WorkingState = @(& $Git -C $Repo status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or @($WorkingState | Where-Object { $_ }).Count -ne 0) {
    throw 'REFUSED: unclean worktree; no changes made'
}
if (Test-Path -LiteralPath $MigrationDir) {
    throw 'REFUSED: migration directory already exists'
}
if (-not (Test-Path -LiteralPath (Join-Path $Repo '.config\dotnet-tools.json'))) {
    throw 'REFUSED: pinned EF tool manifest not found'
}

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$EvidenceDir = Join-Path $Repo ('logs\yangspace\migration-scaffold\' + $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Path $EvidenceDir -Force | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
function Stage([string]$Label, [string[]]$Arguments) {
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $Arguments -WorkingDirectory $Repo -EvidenceDirectory $EvidenceDir -TimeoutSeconds 600
}

$PriorGate = [Environment]::GetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME','Process')
$Pass = $false
try {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME','CONFIRMED','Process')
    Stage 'ef-tool-restore' @('tool','restore','--tool-manifest','.config/dotnet-tools.json')
    Stage 'ef-tool-version' @('ef','--version')
    $VersionOutput = [System.IO.File]::ReadAllText((Join-Path $EvidenceDir 'ef-tool-version.stdout.log'))
    if ($VersionOutput -notmatch '10\.0\.0') { throw 'REFUSED: EF CLI version is not 10.0.0' }
    Stage 'api-restore' @('restore',$Project,'-warnaserror')
    Stage 'api-build-before' @('build',$Project,'-c','Release','--no-restore','-warnaserror')

    # Uses design-time :memory: provider. No production connection string.
    Stage 'ef-initial-add' @(
        'ef','migrations','add',$MigrationName,
        '--project',$Project,'--startup-project',$Project,
        '--context','DirectoryDb','--output-dir','Migrations',
        '--namespace','YangSpace.Api.Migrations',
        '--configuration','Release','--no-build'
    )

    # EF CLI may put the snapshot below the namespace-derived directory,
    # despite --output-dir Migrations. Normalize only the exact known file;
    # fail closed on duplicates or unexpected contents.
    $Snapshot = Join-Path $MigrationDir 'DirectoryDbModelSnapshot.cs'
    $NamespaceSnapshotDir = Join-Path $Repo 'apps\api\YangSpace\Api\Migrations'
    $NamespaceSnapshot = Join-Path $NamespaceSnapshotDir 'DirectoryDbModelSnapshot.cs'
    if (Test-Path -LiteralPath $NamespaceSnapshotDir) {
        $NamespaceEntries = @(Get-ChildItem -LiteralPath $NamespaceSnapshotDir -Force)
        if ($NamespaceEntries.Count -ne 1 -or
            $NamespaceEntries[0].Name -cne 'DirectoryDbModelSnapshot.cs' -or
            $NamespaceEntries[0].PSIsContainer -or
            (Test-Path -LiteralPath $Snapshot)) {
            throw 'REFUSED: unexpected namespace-derived migration artifacts'
        }
        Move-Item -LiteralPath $NamespaceSnapshot -Destination $Snapshot -ErrorAction Stop
    }
    $Files = @(Get-ChildItem -LiteralPath $MigrationDir -File -Filter '*.cs')
    if ($Files.Count -ne 3) {
        throw ('REFUSED: expected three EF-generated C# files, found ' + $Files.Count)
    }
    if (@($Files | Where-Object { $_.Name -match '^[0-9]{14}_InitialDirectorySchema\.cs$' }).Count -ne 1 -or
        @($Files | Where-Object { $_.Name -match '^[0-9]{14}_InitialDirectorySchema\.Designer\.cs$' }).Count -ne 1 -or
        @($Files | Where-Object { $_.Name -ceq 'DirectoryDbModelSnapshot.cs' }).Count -ne 1) {
        throw 'REFUSED: unexpected migration filenames'
    }
    $MigrationDiff = @(& $Git -C $Repo status --porcelain --untracked-files=all)
    if ($LASTEXITCODE -ne 0 -or
        @($MigrationDiff | Where-Object { $_ -and $_ -cnotmatch '^\?\? apps/api/Migrations/' }).Count -ne 0) {
        throw 'REFUSED: unexpected repository change during scaffold'
    }

    Stage 'api-build-after' @('build',$Project,'-c','Release','--no-restore','-warnaserror')
    Stage 'ef-model-diff' @(
        'ef','migrations','has-pending-model-changes',
        '--project',$Project,'--startup-project',$Project,
        '--context','DirectoryDb','--configuration','Release','--no-build'
    )

    $Sql = Join-Path $EvidenceDir 'InitialDirectorySchema.review.sql'
    Stage 'ef-sql-script' @(
        'ef','migrations','script','0',
        '--project',$Project,'--startup-project',$Project,
        '--context','DirectoryDb','--configuration','Release',
        '--no-build','--output',$Sql
    )
    if (-not (Test-Path -LiteralPath $Sql -PathType Leaf) -or
        (Get-Item -LiteralPath $Sql).Length -lt 1000) {
        throw 'REFUSED: migration review SQL not generated'
    }

    # Initial migration must create Identity and directory tables; destructive
    # statements are unexpected and require manual stop before any acceptance.
    $SqlText = [System.IO.File]::ReadAllText($Sql)
    foreach ($RequiredTable in @(
        'AspNetUsers','AspNetRoles','AspNetUserClaims','AspNetUserLogins',
        'AspNetUserRoles','AspNetUserTokens','AspNetRoleClaims',
        'Locations','Offers','Brochures','Jobs'
    )) {
        $QuotedTable = [regex]::Escape('"' + $RequiredTable + '"')
        if ($SqlText -notmatch ('(?im)\bCREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?' + $QuotedTable)) {
            throw ('REFUSED: missing expected table in SQL: ' + $RequiredTable)
        }
    }
    if ($SqlText -match '(?im)^\s*DROP\s+(TABLE|INDEX|COLUMN)\b') {
        throw 'REFUSED: destructive SQL in initial migration'
    }

    $Hashes = @($Files | Sort-Object Name | ForEach-Object {
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash.ToLowerInvariant()
        $hash + '  apps/api/Migrations/' + $_.Name
    })
    $Hashes += ((Get-FileHash -Algorithm SHA256 -LiteralPath $Sql).Hash.ToLowerInvariant() +
        '  InitialDirectorySchema.review.sql')
    [System.IO.File]::WriteAllLines((Join-Path $EvidenceDir 'SHA256SUMS.txt'),$Hashes)
    [System.IO.File]::WriteAllLines((Join-Path $EvidenceDir 'manifest.txt'),@(
        'COMMIT_SHA=' + $CommitSha
        'EF_TOOL_VERSION=10.0.0'
        'MIGRATION_SCAFFOLD=PASS'
        'MODEL_SNAPSHOT_DIFF=NONE'
        'SQL_REVIEW=REQUIRED'
        'DB_APPLY=NOT_RUN'
        'PUBLIC_DB_WRITES=NONE'
        'PVE_DEPLOYMENT=NONE'
    ))
    $Pass = $true
    Write-Host 'MIGRATION_SCAFFOLD=PASS'
    Write-Host 'MODEL_SNAPSHOT_DIFF=NONE'
    Write-Host ('GENERATED_MIGRATIONS=' + $MigrationDir)
    Write-Host ('EVIDENCE_DIR=' + $EvidenceDir)
}
finally {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME',$PriorGate,'Process')
    if (-not $Pass) {
        [System.IO.File]::WriteAllText((Join-Path $EvidenceDir 'gate.txt'),
            'MIGRATION_SCAFFOLD=HOLD')
        Write-Warning ('MIGRATION_SCAFFOLD=HOLD; EVIDENCE_DIR=' + $EvidenceDir)
    }
}
