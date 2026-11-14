# F2b O2: OFFLINE EF Core 10 dual-provider migration scaffold; no DB apply.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidatePattern('^[a-f0-9]{40}$')][string]$CommitSha)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch='feat/yangspace-f2b-email-outbox-enqueue-foundation-20261114'
$Migration='F2bIdentityEmailOutbox'
$SQLite='apps/api/YangSpace.Api.csproj'
$Pg='apps/api.Migrations.PostgreSql/YangSpace.PostgreSql.Migrations.csproj'
$Dirs=@('apps/api/Migrations','apps/api.Migrations.PostgreSql/Migrations')
$Git=(Get-Command git.exe -ErrorAction Stop).Source
$Dotnet=(Get-Command dotnet.exe -ErrorAction Stop).Source
function GitValue([string[]]$ArgsList) {
    $output=@(& $Git -C $Repo @ArgsList)
    if ($LASTEXITCODE -ne 0) { throw 'O2_GIT_FAILED' }
    return ($output -join '').Trim()
}
if ((GitValue @('rev-parse','HEAD')) -cne $CommitSha) { throw 'O2_HEAD_MISMATCH' }
if ((GitValue @('rev-parse','--verify',('refs/remotes/origin/'+$Branch))) -cne $CommitSha) { throw 'O2_REMOTE_MISMATCH' }
$Active=GitValue @('branch','--show-current')
if ($Active -and $Active -cne $Branch) { throw 'O2_BRANCH_MISMATCH' }
if ((GitValue @('status','--porcelain','--untracked-files=all')).Length -ne 0) { throw 'O2_DIRTY_WORKTREE' }
foreach($Path in @('.config/dotnet-tools.json','apps/api/Migrations/20261111125709_PublicationFreshnessTimestamps.cs','apps/api/Migrations/DirectoryDbModelSnapshot.cs','apps/api.Migrations.PostgreSql/Migrations/20261111203833_InitialPostgreSqlSchema.cs','apps/api.Migrations.PostgreSql/Migrations/DirectoryDbModelSnapshot.cs')) {
    if (-not (Test-Path -LiteralPath (Join-Path $Repo $Path) -PathType Leaf)) { throw "O2_BASELINE_MISSING=$Path" }
}
foreach($Dir in $Dirs) {
    if (@(Get-ChildItem -LiteralPath (Join-Path $Repo $Dir) -File -Filter "*_$Migration*.cs").Count -ne 0) { throw "O2_ALREADY_SCAFFOLDED=$Dir" }
}
Write-Host 'F2B_O2_EXACT_SOURCE=PASS'
$Id=(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+([guid]::NewGuid().ToString('N').Substring(0,8))
$Evidence=Join-Path $Repo ('logs/yangspace/f2b-outbox-migrations/'+$CommitSha.Substring(0,12)+'-'+$Id)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
function Stage([string]$Label,[string[]]$Arguments) {
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $Arguments -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 900
}
$OldSQLite=[Environment]::GetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME','Process')
$OldPg=[Environment]::GetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME','Process')
$Completed=$false
try {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME','CONFIRMED','Process')
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME','CONFIRMED','Process')
    Stage 'tool-restore' @('tool','restore','--tool-manifest','.config/dotnet-tools.json')
    Stage 'ef-version' @('ef','--version')
    $Version=[IO.File]::ReadAllText((Join-Path $Evidence 'ef-version.stdout.log'))
    if ($Version -notmatch '\b10\.0\.0\b') { throw 'O2_EF_VERSION_WRONG' }
    Stage 'sqlite-restore' @('restore',$SQLite,'-warnaserror')
    Stage 'pg-restore' @('restore',$Pg,'-warnaserror')
    Stage 'sqlite-build-pre' @('build',$SQLite,'-c','Release','--no-restore','-warnaserror')
    Stage 'pg-build-pre' @('build',$Pg,'-c','Release','--no-restore','-warnaserror')
    Stage 'sqlite-add' @('ef','migrations','add',$Migration,'--project',$SQLite,'--startup-project',$SQLite,'--context','DirectoryDb','--output-dir','Migrations','--namespace','YangSpace.Api.Migrations','--configuration','Release','--no-build')
    Stage 'pg-add' @('ef','migrations','add',$Migration,'--project',$Pg,'--startup-project',$Pg,'--context','DirectoryDb','--output-dir','Migrations','--namespace','YangSpace.PostgreSql.Migrations','--configuration','Release','--no-build')
    $Changes=@(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
    if ($LASTEXITCODE -ne 0 -or $Changes.Count -ne 6) { throw "O2_CHANGE_COUNT=$($Changes.Count)" }
    foreach($Dir in $Dirs) {
        $Esc=[regex]::Escape($Dir)
        foreach($Suffix in @('.cs','.Designer.cs')) {
            $Pattern='^\?\? '+$Esc+'/[0-9]{14}_'+$Migration+[regex]::Escape($Suffix)+'$'
            if (@($Changes | Where-Object { $_ -cmatch $Pattern }).Count -ne 1) { throw "O2_GENERATED_FILE_NOT_FOUND=$Dir/$Suffix" }
        }
        if (@($Changes | Where-Object { $_ -ceq (' M '+$Dir+'/DirectoryDbModelSnapshot.cs') }).Count -ne 1) { throw "O2_SNAPSHOT_CHANGED_UNEXPECTEDLY=$Dir" }
    }
    Stage 'sqlite-build-post' @('build',$SQLite,'-c','Release','--no-restore','-warnaserror')
    Stage 'pg-build-post' @('build',$Pg,'-c','Release','--no-restore','-warnaserror')
    Stage 'sqlite-model-diff' @('ef','migrations','has-pending-model-changes','--project',$SQLite,'--startup-project',$SQLite,'--context','DirectoryDb','--configuration','Release','--no-build')
    Stage 'pg-model-diff' @('ef','migrations','has-pending-model-changes','--project',$Pg,'--startup-project',$Pg,'--context','DirectoryDb','--configuration','Release','--no-build')
    $SQLiteFile=Join-Path $Evidence 'outbox.sqlite.review.sql'
    $PgFile=Join-Path $Evidence 'outbox.pg.review.sql'
    Stage 'sqlite-sql' @('ef','migrations','script','20261111125709_PublicationFreshnessTimestamps','--project',$SQLite,'--startup-project',$SQLite,'--context','DirectoryDb','--configuration','Release','--no-build','--output',$SQLiteFile)
    Stage 'pg-sql' @('ef','migrations','script','20261111203833_InitialPostgreSqlSchema','--project',$Pg,'--startup-project',$Pg,'--context','DirectoryDb','--configuration','Release','--no-build','--output',$PgFile)
    foreach($Path in @($SQLiteFile,$PgFile)) {
        if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "O2_SQL_MISSING=$Path" }
        $Sql=[IO.File]::ReadAllText($Path)
        $Tables=@([regex]::Matches($Sql,'(?i)\bCREATE\s+TABLE\s+"([^"]+)"'))
        if ($Tables.Count -ne 1 -or $Tables[0].Groups[1].Value -cne 'IdentityEmailOutbox') { throw "O2_UNEXPECTED_CREATE_TABLE=$Path" }
        if ($Sql -match '(?im)^\s*(DROP|ALTER|TRUNCATE|DELETE|UPDATE)\b') { throw "O2_UNEXPECTED_SQL_MUTATION=$Path" }
    }
    $ChangedPaths=@($Changes | ForEach-Object { $_.Substring(3) } | Sort-Object)
    $Checksums=@($ChangedPaths | ForEach-Object { (Get-FileHash -LiteralPath (Join-Path $Repo $_) -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_ })
    foreach($Path in @($SQLiteFile,$PgFile)) { $Checksums+=((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+(Split-Path $Path -Leaf)) }
    [IO.File]::WriteAllLines((Join-Path $Evidence 'SHA256SUMS.txt'),[string[]]$Checksums)
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]@(
        "SOURCE_SHA=$CommitSha",'EF_TOOL_VERSION=10.0.0',
        'O2_SQLITE_SCAFFOLD=PASS','O2_POSTGRESQL_SCAFFOLD=PASS','O2_SNAPSHOT_DIFF=NONE',
        'O2_MIGRATIONS=UNCOMMITTED','SQL_REVIEW=PENDING','DB_APPLY=NOT_RUN',
        'OUTBOX_WORKER=NOT_IMPLEMENTED','PUBLIC_REGISTRATION=OFF',
        'CT164_DB=NOT_ACCESSED','RELEASE_GATE=HOLD'
    ))
    $Completed=$true
    Write-Host 'F2B_OUTBOX_MIGRATION_SCAFFOLD=PASS'
    Write-Host "SOURCE_SHA=$CommitSha"
    Write-Host "EVIDENCE_DIR=$Evidence"
    Write-Host ('MANIFEST_SHA256='+(Get-FileHash -LiteralPath (Join-Path $Evidence 'manifest.txt') -Algorithm SHA256).Hash.ToLowerInvariant())
    Write-Host 'GENERATED_FILES=UNCOMMITTED; SQL_REVIEW=PENDING; DB_APPLY=NOT_RUN'
}
finally {
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_DESIGNTIME',$OldSQLite,'Process')
    [Environment]::SetEnvironmentVariable('YANGSPACE_EF_POSTGRESQL_DESIGNTIME',$OldPg,'Process')
    if (-not $Completed) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),'F2B_OUTBOX_MIGRATION_SCAFFOLD=HOLD')
        Write-Warning ("F2B_OUTBOX_MIGRATION_SCAFFOLD=HOLD; EVIDENCE_DIR=$Evidence")
    }
}
