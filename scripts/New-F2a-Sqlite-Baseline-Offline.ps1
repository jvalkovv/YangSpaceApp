# F2a-S1: scaffold an isolated SQLite baseline migration ONLY.
# Does not touch production, call database update, or apply SQL to any database.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidatePattern('^[a-f0-9]{40}$')][string]$CommitSha)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch='fix/yangspace-f2a-sqlite-baseline-20261114'
$Migration='F2aOrganizationClaimFoundation'
$Project='apps/api/YangSpace.Api.csproj'
$Dir='apps/api/Migrations'
$Git=(Get-Command git.exe -ErrorAction Stop).Source
$Dotnet=(Get-Command dotnet.exe -ErrorAction Stop).Source
function GitValue([string[]]$ArgsList) {
    $lines=@(& $Git -C $Repo @ArgsList)
    if ($LASTEXITCODE -ne 0) { throw 'F2A_S1_GIT_FAILED' }
    return ($lines -join '').Trim()
}
if ((GitValue @('rev-parse','HEAD')) -cne $CommitSha) { throw 'F2A_S1_HEAD_MISMATCH' }
if ((GitValue @('rev-parse','--verify',('refs/remotes/origin/'+$Branch))) -cne $CommitSha) { throw 'F2A_S1_REMOTE_SHA_MISMATCH' }
$Active=GitValue @('branch','--show-current')
if ($Active -and $Active -cne $Branch) { throw 'F2A_S1_BRANCH_MISMATCH' }
if ((GitValue @('status','--porcelain','--untracked-files=all')).Length -ne 0) { throw 'F2A_S1_DIRTY_WORKTREE' }
foreach($Path in @(
    '.config/dotnet-tools.json',
    'apps/api/Migrations/20261111125709_PublicationFreshnessTimestamps.cs',
    'apps/api/Migrations/DirectoryDbModelSnapshot.cs'
)) {
    if (-not (Test-Path -LiteralPath (Join-Path $Repo $Path) -PathType Leaf)) {
        throw "F2A_S1_BASELINE_FILE_MISSING=$Path"
    }
}
if (@(Get-ChildItem -LiteralPath (Join-Path $Repo $Dir) -Filter "*_$Migration*.cs" -File).Count -ne 0) {
    throw 'F2A_S1_MIGRATION_EXISTS'
}
Write-Host 'F2A_S1_EXACT_SOURCE=PASS'
$RunId=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+([guid]::NewGuid().ToString('N').Substring(0,8))
$Evidence=Join-Path $Repo ('logs/yangspace/f2a-sqlite-baseline/'+$CommitSha.Substring(0,12)+'-'+$RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
function Stage([string]$Label,[string[]]$Arguments) {
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $Arguments -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 900
}
$OldGate=[Environment]::GetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME','Process')
$Ok=$false
try {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME','CONFIRMED','Process')
    Stage 'f2a-sqlite-tool-restore' @('tool','restore','--tool-manifest','.config/dotnet-tools.json')
    Stage 'f2a-sqlite-tool-version' @('ef','--version')
    $Version=[IO.File]::ReadAllText((Join-Path $Evidence 'f2a-sqlite-tool-version.stdout.log'))
    if ($Version -notmatch '\b10\.0\.0\b') { throw 'F2A_S1_EF_VERSION' }
    Stage 'f2a-sqlite-restore' @('restore',$Project,'-warnaserror')
    Stage 'f2a-sqlite-build-before' @('build',$Project,'-c','Release','--no-restore','-warnaserror')
    Stage 'f2a-sqlite-migration-add' @(
        'ef','migrations','add',$Migration,'--project',$Project,
        '--startup-project',$Project,'--context','DirectoryDb',
        '--output-dir','Migrations','--namespace','YangSpace.Api.Migrations',
        '--configuration','Release','--no-build')
    $Changed=@(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $Changed.Count -ne 3) { throw "F2A_S1_CHANGED_FILE_COUNT=$($Changed.Count)" }
    foreach($Suffix in @('.cs','.Designer.cs')) {
        $Pattern='^\?\? apps/api/Migrations/[0-9]{14}_'+$Migration+[regex]::Escape($Suffix)+'$'
        if (@($Changed | Where-Object { $_ -cmatch $Pattern }).Count -ne 1) {
            throw "F2A_S1_UNEXPECTED_GENERATED_FILE=$Suffix"
        }
    }
    if (@($Changed | Where-Object { $_ -ceq ' M apps/api/Migrations/DirectoryDbModelSnapshot.cs' }).Count -ne 1) {
        throw 'F2A_S1_SNAPSHOT_NOT_CHANGED'
    }
    Stage 'f2a-sqlite-build-after' @('build',$Project,'-c','Release','--no-restore','-warnaserror')
    Stage 'f2a-sqlite-model-diff' @('ef','migrations','has-pending-model-changes',
        '--project',$Project,'--startup-project',$Project,'--context','DirectoryDb',
        '--configuration','Release','--no-build')
    $SqlPath=Join-Path $Evidence 'f2a-sqlite-baseline.review.sql'
    Stage 'f2a-sqlite-sql' @('ef','migrations','script','20261111125709_PublicationFreshnessTimestamps',
        '--project',$Project,'--startup-project',$Project,'--context','DirectoryDb',
        '--configuration','Release','--no-build','--output',$SqlPath)
    if (-not (Test-Path -LiteralPath $SqlPath -PathType Leaf)) { throw 'F2A_S1_SQL_MISSING' }
    $Sql=[IO.File]::ReadAllText($SqlPath)
    $Created=@([regex]::Matches($Sql,'(?i)\bCREATE\s+TABLE\s+"([^"]+)"') |
        ForEach-Object { $_.Groups[1].Value })
    $Expected=@('Organizations','BusinessClaims','ContentAuditEvents','OrganizationMembers','ef_temp_Locations')
    if ($Created.Count -ne $Expected.Count -or
        @(Compare-Object -ReferenceObject ($Expected | Sort-Object) -DifferenceObject ($Created | Sort-Object)).Count -ne 0) {
        throw ('F2A_S1_TABLE_ALLOWLIST_FAILED=' + ($Created -join ','))
    }
    if ($Sql -match '(?i)IdentityEmailOutbox') { throw 'F2A_S1_OUTBOX_LEAKED_INTO_BASELINE' }
    if (@([regex]::Matches($Sql,'(?im)^\s*DROP\s+TABLE\s+"Locations"\s*;')).Count -ne 1 -or
        $Sql -notmatch '(?im)^\s*INSERT\s+INTO\s+"ef_temp_Locations"(?=\s*\()' -or
        $Sql -notmatch '(?im)^\s*ALTER\s+TABLE\s+"ef_temp_Locations"\s+RENAME\s+TO\s+"Locations"') {
        throw 'F2A_S1_LOCATIONS_REBUILD_NOT_REVIEWABLE'
    }
    if ($Sql -match '(?im)^\s*(DELETE\s+FROM|UPDATE\s+|TRUNCATE\s+)') {
        throw 'F2A_S1_UNEXPECTED_DATA_MUTATION'
    }
    $DropTables=@([regex]::Matches($Sql,'(?im)^\s*DROP\s+TABLE\s+"([^"]+)"\s*;') |
        ForEach-Object { $_.Groups[1].Value })
    if ($DropTables.Count -ne 1 -or $DropTables[0] -cne 'Locations') { throw 'F2A_S1_UNEXPECTED_DROP_TARGET' }
    $Paths=@($Changed | ForEach-Object { $_.Substring(3) } | Sort-Object)
    $Hashes=@($Paths | ForEach-Object {
        (Get-FileHash -LiteralPath (Join-Path $Repo $_) -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_
    })
    $Hashes+=((Get-FileHash -LiteralPath $SqlPath -Algorithm SHA256).Hash.ToLowerInvariant()+'  f2a-sqlite-baseline.review.sql')
    [IO.File]::WriteAllLines((Join-Path $Evidence 'SHA256SUMS.txt'),[string[]]$Hashes)
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]@(
        "SOURCE_SHA=$CommitSha",'EF_TOOL_VERSION=10.0.0',
        'F2A_SQLITE_BASELINE_SCAFFOLD=PASS',
        'F2A_SQLITE_GENERATED_FILES=UNCOMMITTED',
        'LOCATIONS_REBUILD=REQUIRES_DISPOSABLE_DATA_PRESERVATION_TEST',
        'SQL_REVIEW=REQUIRED','DISPOSABLE_UPGRADE=NOT_RUN',
        'LIVE_DB_APPLY=NOT_RUN','CT164_DB=NOT_ACCESSED','RELEASE_GATE=HOLD'
    ))
    $Ok=$true
    Write-Host 'F2A_SQLITE_BASELINE_SCAFFOLD=PASS'
    Write-Host "SOURCE_SHA=$CommitSha"
    Write-Host "EVIDENCE_DIR=$Evidence"
    Write-Host ('MANIFEST_SHA256='+
        (Get-FileHash -LiteralPath (Join-Path $Evidence 'manifest.txt') -Algorithm SHA256).Hash.ToLowerInvariant())
    Write-Host 'NEXT=review 3 EF files and SQLite SQL; disposable data-preservation tests required'
}
finally {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME',$OldGate,'Process')
    if (-not $Ok) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),'F2A_SQLITE_BASELINE_SCAFFOLD=HOLD')
        Write-Warning ("F2A_SQLITE_BASELINE_SCAFFOLD=HOLD; EVIDENCE_DIR=$Evidence")
    }
}
