# F1b — scaffold ONLY the next EF SQLite migration in a clean, pinned Git worktree.
# Does not connect to, update, back up, or replace a live/preview/production database.
# Generated EF files stay uncommitted for independent review.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$CommitSha
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-publication-timestamps-f1b-20261111'
$Project = 'apps/api/YangSpace.Api.csproj'
$Migration = 'PublicationFreshnessTimestamps'
$Previous = '20261110043149_InitialDirectorySchema'
$Dir = Join-Path $Repo 'apps\api\Migrations'

function Executable([string]$Name) {
    $found = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $found) { throw ('MISSING_EXECUTABLE=' + $Name) }
    return $found.Source
}
$Git = Executable 'git.exe'
$Dotnet = Executable 'dotnet.exe'
if ($CommitSha -cnotmatch '^[0-9a-f]{40}$') { throw 'REFUSED: full SHA required' }
$Head = (& $Git -C $Repo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $Head -cne $CommitSha) {
    throw 'REFUSED: local HEAD is not the pinned candidate'
}
$Remote = (& $Git -C $Repo rev-parse ('refs/remotes/origin/' + $Branch)).Trim()
if ($LASTEXITCODE -ne 0 -or $Remote -cne $CommitSha) {
    throw 'REFUSED: remote-tracking source SHA differs'
}
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or @($Dirty | Where-Object { $_ }).Count -ne 0) {
    throw 'REFUSED: worktree not clean'
}
if (-not (Test-Path -LiteralPath (Join-Path $Dir ($Previous + '.cs')) -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $Dir 'DirectoryDbModelSnapshot.cs') -PathType Leaf)) {
    throw 'REFUSED: missing committed initial migration/snapshot'
}
if (@(Get-ChildItem -LiteralPath $Dir -Filter ('*_' + $Migration + '*.cs') -File).Count -ne 0) {
    throw 'REFUSED: migration has already been generated'
}
if (-not (Test-Path -LiteralPath (Join-Path $Repo '.config\dotnet-tools.json'))) {
    throw 'REFUSED: pinned dotnet-ef tool manifest missing'
}

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8)
$Evidence = Join-Path $Repo ('logs\yangspace\migration-scaffold\f1b-' + $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Path $Evidence -Force | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
function Stage([string]$Label,[string[]]$Args) {
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $Args -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 600
}
$PriorDesignGate = [Environment]::GetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME','Process')
$Completed = $false
try {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME','CONFIRMED','Process')
    Stage 'ef-tool-restore' @('tool','restore','--tool-manifest','.config/dotnet-tools.json')
    Stage 'ef-tool-version' @('ef','--version')
    $Version = [IO.File]::ReadAllText((Join-Path $Evidence 'ef-tool-version.stdout.log'))
    if ($Version -notmatch '10\.0\.0') { throw 'REFUSED: expected dotnet-ef 10.0.0' }
    Stage 'api-restore' @('restore',$Project,'-warnaserror')
    Stage 'api-build-before' @('build',$Project,'-c','Release','--no-restore','-warnaserror')

    Stage 'ef-f1b-add' @(
        'ef','migrations','add',$Migration,
        '--project',$Project,'--startup-project',$Project,
        '--context','DirectoryDb','--output-dir','Migrations',
        '--namespace','YangSpace.Api.Migrations',
        '--configuration','Release','--no-build'
    )

    # Mirror the known EF tooling normalization for namespace-derived snapshots.
    $Snapshot = Join-Path $Dir 'DirectoryDbModelSnapshot.cs'
    $OtherDir = Join-Path $Repo 'apps\api\YangSpace\Api\Migrations'
    $OtherSnapshot = Join-Path $OtherDir 'DirectoryDbModelSnapshot.cs'
    if (Test-Path -LiteralPath $OtherDir) {
        $Unexpected = @(Get-ChildItem -LiteralPath $OtherDir -Force)
        if ($Unexpected.Count -ne 1 -or $Unexpected[0].Name -cne 'DirectoryDbModelSnapshot.cs' -or
            $Unexpected[0].PSIsContainer) { throw 'REFUSED: unexpected namespace-derived artifacts' }
        Move-Item -LiteralPath $OtherSnapshot -Destination $Snapshot -Force
    }

    $Generated = @(Get-ChildItem -LiteralPath $Dir -Filter ('*_' + $Migration + '*.cs') -File)
    if ($Generated.Count -ne 2 -or
        @($Generated | Where-Object { $_.Name -match ('^[0-9]{14}_' + $Migration + '\.cs$') }).Count -ne 1 -or
        @($Generated | Where-Object { $_.Name -match ('^[0-9]{14}_' + $Migration + '\.Designer\.cs$') }).Count -ne 1) {
        throw 'REFUSED: exactly two EF-generated migration files expected'
    }

    # Verify only the generated migration and existing snapshot were modified.
    $Changes = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $Changes.Count -ne 3) {
        throw 'REFUSED: unexpected number of Git changes after scaffold'
    }
    foreach ($Line in $Changes) {
        if ($Line -cnotmatch '^\?\? apps/api/Migrations/[0-9]{14}_PublicationFreshnessTimestamps(\.Designer)?\.cs$' -and
            $Line -cne ' M apps/api/Migrations/DirectoryDbModelSnapshot.cs') {
            throw ('REFUSED: unexpected Git change: ' + $Line)
        }
    }

    Stage 'api-build-after' @('build',$Project,'-c','Release','--no-restore','-warnaserror')
    Stage 'ef-model-diff' @(
        'ef','migrations','has-pending-model-changes',
        '--project',$Project,'--startup-project',$Project,
        '--context','DirectoryDb','--configuration','Release','--no-build'
    )

    $Sql = Join-Path $Evidence ($Migration + '.review.sql')
    Stage 'ef-delta-sql' @(
        'ef','migrations','script',$Previous,
        '--project',$Project,'--startup-project',$Project,
        '--context','DirectoryDb','--configuration','Release','--no-build',
        '--output',$Sql
    )
    if (-not (Test-Path -LiteralPath $Sql -PathType Leaf)) {
        throw 'REFUSED: migration SQL not generated'
    }
    $SqlText = [IO.File]::ReadAllText($Sql)
    foreach ($Table in @('Offers','Brochures','Jobs')) {
        foreach ($Field in @('CreatedAtUtc','UpdatedAtUtc','PublishedAtUtc')) {
            $Pattern = '(?im)^\s*ALTER\s+TABLE\s+"' + $Table + '"\s+ADD\s+"' + $Field + '"\s+TEXT\s+NULL\s*;'
            if (@([regex]::Matches($SqlText,$Pattern)).Count -ne 1) {
                throw ('REFUSED: SQL does not add expected nullable column ' + $Table + '.' + $Field)
            }
        }
    }
    if (@([regex]::Matches($SqlText,'(?im)^\s*ALTER\s+TABLE\b')).Count -ne 9 -or
        $SqlText -match '(?im)^\s*(DROP|DELETE|UPDATE|REPLACE)\s+' -or
        $SqlText -match '(?im)^\s*ALTER\s+TABLE\s+"(?!Offers"|Brochures"|Jobs")') {
        throw 'REFUSED: unexpected/destructive migration SQL'
    }

    $HashList = @($Generated + @(Get-Item -LiteralPath $Snapshot) | Sort-Object FullName | ForEach-Object {
        (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash.ToLowerInvariant() +
            '  apps/api/Migrations/' + $_.Name
    })
    $HashList += ((Get-FileHash -Algorithm SHA256 -LiteralPath $Sql).Hash.ToLowerInvariant() +
        '  ' + $Migration + '.review.sql')
    [IO.File]::WriteAllLines((Join-Path $Evidence 'SHA256SUMS.txt'),$HashList)
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),@(
        'SOURCE_SHA=' + $CommitSha
        'EF_VERSION=10.0.0'
        'MIGRATION=' + $Migration
        'MIGRATION_SCAFFOLD=PASS'
        'MODEL_SNAPSHOT_DIFF=NONE'
        'GENERATED_FILES_NOT_COMMITTED=YES'
        'SQL_REVIEW=REQUIRED'
        'DISPOSABLE_DATABASE_UPGRADE=NOT_RUN'
        'LIVE_DATABASE_APPLY=NOT_RUN'
        'CT164_DEPLOYMENT=NONE'
    ))
    $Completed = $true
    Write-Host 'F1B_MIGRATION_SCAFFOLD=PASS'
    Write-Host 'MODEL_SNAPSHOT_DIFF=NONE'
    Write-Host ('GENERATED_DIR=' + $Dir)
    Write-Host ('EVIDENCE_DIR=' + $Evidence)
    Write-Host 'NEXT=Independent review of generated migration and SQL; no DB apply'
}
finally {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME',$PriorDesignGate,'Process')
    if (-not $Completed) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),'F1B_MIGRATION_SCAFFOLD=HOLD')
        Write-Warning ('F1B_MIGRATION_SCAFFOLD=HOLD; EVIDENCE_DIR=' + $Evidence)
    }
}
