# F2a-S2: apply the generated SQLite organization/claim migration only to disposable files.
# The three generated EF artifacts are copied from the exact, hash-verified S1 operator run.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SourceWorktree
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ExpectedTestBranch = 'fix/yangspace-f2a-sqlite-baseline-20261114'
$ExpectedS1Sha = '35c9f576bbc64c0825c1a117b3ee415d3ca9869d'
$S1EvidenceRelative = 'logs\yangspace\f2a-sqlite-baseline\35c9f576bbc6-20261114-203452-4b745f7a'
$MigrationName = '20261114183515_F2aOrganizationClaimFoundation'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$Project = 'apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'

function GitValue([string[]]$Arguments) {
    $output = @(& $Git -C $Repo @Arguments)
    if ($LASTEXITCODE -ne 0) { throw 'F2A_S2_GIT_FAILED' }
    return ($output -join '').Trim()
}

if ((GitValue @('rev-parse','HEAD')) -cne $CommitSha) {
    throw 'F2A_S2_HEAD_MISMATCH'
}
if ((GitValue @('rev-parse','--verify',('refs/remotes/origin/' + $ExpectedTestBranch))) -cne $CommitSha) {
    throw 'F2A_S2_REMOTE_SHA_MISMATCH'
}
$activeBranch = GitValue @('branch','--show-current')
# Detached exact-SHA worktrees are accepted; the fetched remote ref below
# remains the authoritative branch identity and must equal CommitSha.
if ($activeBranch -and $activeBranch -cne $ExpectedTestBranch) {
    throw 'F2A_S2_BRANCH_MISMATCH'
}
$initialStatus = @(& $Git -C $Repo status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $initialStatus.Count -ne 0) {
    throw 'F2A_S2_TEST_WORKTREE_NOT_CLEAN'
}

$Source = (Resolve-Path -LiteralPath $SourceWorktree).Path
$sourceHead = @(& $Git -C $Source rev-parse HEAD)
if ($LASTEXITCODE -ne 0 -or (($sourceHead -join '').Trim() -cne $ExpectedS1Sha)) {
    throw 'F2A_S2_S1_SOURCE_SHA_MISMATCH'
}

$EvidenceSource = Join-Path $Source $S1EvidenceRelative
$ManifestPath = Join-Path $EvidenceSource 'manifest.txt'
$ChecksumsPath = Join-Path $EvidenceSource 'SHA256SUMS.txt'
if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $ChecksumsPath -PathType Leaf)) {
    throw 'F2A_S2_S1_EVIDENCE_MISSING'
}
# Read as bytes-to-text explicitly, strip a possible BOM/NUL, and normalize
# all whitespace. The S1 manifest is generated with WriteAllLines; this
# accepts CRLF/LF and editor/tool line-ending normalization without weakening
# the exact key=value checks.
$manifestText = [IO.File]::ReadAllText($ManifestPath)
$manifestText = $manifestText.TrimStart([char]0xFEFF).Replace([string][char]0, '')
$manifestNormalized = [regex]::Replace($manifestText, '\s+', ' ')

$requiredEntries = [string[]]@(
    ("SOURCE_SHA={0}" -f $ExpectedS1Sha),
    'F2A_SQLITE_BASELINE_SCAFFOLD=PASS',
    'F2A_SQLITE_GENERATED_FILES=UNCOMMITTED',
    'DISPOSABLE_UPGRADE=NOT_RUN',
    'RELEASE_GATE=HOLD'
)
foreach ($entry in $requiredEntries) {
    if ($manifestNormalized.IndexOf($entry, [StringComparison]::Ordinal) -lt 0) {
        $manifestHash = (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
        throw ("F2A_S2_S1_MANIFEST_GATE_MISSING={0}; MANIFEST_SHA256={1}; LENGTH={2}" -f
            $entry, $manifestHash, $manifestText.Length)
    }
}
Write-Host 'F2A_S2_S1_MANIFEST=PASS'

# Keep each path as one scalar string. Avoid combining PowerShell's
# comma/array operator with string concatenation inside an array expression.
[string[]]$expectedFiles = @(
    ("apps/api/Migrations/{0}.cs" -f $MigrationName)
    ("apps/api/Migrations/{0}.Designer.cs" -f $MigrationName)
    'apps/api/Migrations/DirectoryDbModelSnapshot.cs'
)
if ($expectedFiles.Count -ne 3 -or
    @($expectedFiles | Where-Object { $_ -isnot [string] -or [string]::IsNullOrWhiteSpace($_) }).Count -ne 0) {
    throw 'F2A_S2_EXPECTED_FILE_LIST_INVALID'
}
$checksumLines = Get-Content -LiteralPath $ChecksumsPath
foreach ($relative in $expectedFiles) {
    $sourceFile = Join-Path $Source $relative
    if (-not (Test-Path -LiteralPath $sourceFile -PathType Leaf)) {
        throw ('F2A_S2_GENERATED_FILE_MISSING=' + $relative)
    }

    $line = @($checksumLines | Where-Object {
        $_ -match ('^([0-9a-f]{64})\s+' + [regex]::Escape($relative) + '$')
    })
    if ($line.Count -ne 1) {
        throw ('F2A_S2_SOURCE_CHECKSUM_ENTRY_MISSING=' + $relative)
    }

    $expectedHash = ([regex]::Match($line[0], '^([0-9a-f]{64})')).Groups[1].Value
    $actualHash = (Get-FileHash -LiteralPath $sourceFile -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -cne $expectedHash) {
        throw ('F2A_S2_SOURCE_CHECKSUM_MISMATCH=' + $relative)
    }
}

foreach ($relative in $expectedFiles) {
    $destination = Join-Path $Repo $relative
    $parent = Split-Path -Parent $destination
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        throw ('F2A_S2_DESTINATION_DIRECTORY_MISSING=' + $parent)
    }
    Copy-Item -LiteralPath (Join-Path $Source $relative) -Destination $destination -Force
}

[string[]]$expectedStatus = @(
    ' M apps/api/Migrations/DirectoryDbModelSnapshot.cs'
    ("?? apps/api/Migrations/{0}.Designer.cs" -f $MigrationName)
    ("?? apps/api/Migrations/{0}.cs" -f $MigrationName)
) | Sort-Object
$actualStatus = @(& $Git -C $Repo status --porcelain=v1 --untracked-files=all | ForEach-Object { $_ } | Sort-Object)
if ($LASTEXITCODE -ne 0 -or
    (Compare-Object -ReferenceObject $expectedStatus -DifferenceObject $actualStatus).Count -ne 0) {
    throw 'F2A_S2_GENERATED_CHANGESET_NOT_EXACT'
}

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8)
$Evidence = Join-Path $Repo ('logs\yangspace\f2a-sqlite-org-upgrade\' + $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null

. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
function Stage([string]$Label, [string[]]$Arguments) {
    Invoke-YangSpaceStage -Label $Label -Executable $Dotnet -CommandArguments $Arguments -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 900
}

Write-Host 'F2A_S2_EXACT_TEST_SOURCE=PASS'
Write-Host 'F2A_S2_GENERATED_ARTIFACT_HASHES=PASS'
Stage 'f2a-s2-runtime-restore' @('restore',$Project,'-warnaserror')
Stage 'f2a-s2-runtime-build' @('build',$Project,'-c','Release','--no-restore','-warnaserror')
Stage 'f2a-s2-disposable-upgrade-recovery' @('run','--project',$Project,'-c','Release','--no-build','--no-restore','--','--f2a-organization-claim-upgrade-only')

$RunLog = Join-Path $Evidence 'f2a-s2-disposable-upgrade-recovery.stdout.log'
if (-not (Test-Path -LiteralPath $RunLog -PathType Leaf)) {
    throw 'F2A_S2_RUNTIME_LOG_MISSING'
}
$RunText = Get-Content -LiteralPath $RunLog -Raw
foreach ($marker in @(
    'F2A_S2_LEGACY_BASELINE',
    'F2A_S2_SUCCESSFUL_UPGRADE',
    'F2A_S2_LOCATION_ALL_FIELDS_PRESERVED',
    'F2A_S2_OFFERS_PRESERVED',
    'F2A_S2_BROCHURES_PRESERVED',
    'F2A_S2_JOBS_PRESERVED',
    'F2A_S2_IDENTITY_PRESERVATION',
    'F2A_S2_NEW_SCHEMA_RELATIONSHIPS',
    'F2A_S2_NO_OUTBOX_TABLE',
    'F2A_S2_RECOVERY_ALL_BOUNDARIES',
    'F2A_S2_DISPOSABLE_UPGRADE_RECOVERY',
    'FAULT-AFTER-F2A-DDL_FAILURE_INJECTED',
    'FAULT-AFTER-F2A-DDL_RESTORE_RECOVERY',
    'FAULT-AFTER-LOCATIONS-REBUILD_FAILURE_INJECTED',
    'FAULT-AFTER-LOCATIONS-REBUILD_RESTORE_RECOVERY',
    'FAULT-BEFORE-MIGRATION-HISTORY_FAILURE_INJECTED',
    'FAULT-BEFORE-MIGRATION-HISTORY_RESTORE_RECOVERY'
)) {
    if ($RunText -notmatch ('(?m)^' + [regex]::Escape($marker) + '=PASS\s*$')) {
        throw ('F2A_S2_ACCEPTANCE_MARKER_MISSING=' + $marker)
    }
}

$finalStatus = @(& $Git -C $Repo status --porcelain=v1 --untracked-files=all | ForEach-Object { $_ } | Sort-Object)
if ($LASTEXITCODE -ne 0 -or
    (Compare-Object -ReferenceObject $expectedStatus -DifferenceObject $finalStatus).Count -ne 0) {
    throw 'F2A_S2_UNEXPECTED_WORKTREE_CHANGES'
}

$Manifest = @(
    "TEST_COMMIT_SHA=$CommitSha"
    "S1_SOURCE_SHA=$ExpectedS1Sha"
    'GENERATED_ARTIFACT_HASHES=PASS'
    'RUNTIME_RESTORE=PASS'
    'RUNTIME_BUILD=PASS'
    'HISTORIC_SQLITE_UPGRADE=DISPOSABLE_ONLY'
    'LOCATIONS_DATA_PRESERVATION=PASS'
    'OFFERS_BROCHURES_JOBS_PRESERVATION=PASS'
    'IDENTITY_USERS_ROLES_AND_DEPENDENCIES=PASS'
    'NEW_F2A_RELATIONSHIPS=PASS'
    'SQLITE_INTEGRITY=PASS'
    'FOREIGN_KEY_INTEGRITY=PASS'
    'FAULT_INJECTION_AFTER_INITIAL_F2A_DDL=PASS'
    'FAULT_INJECTION_AFTER_LOCATIONS_REBUILD=PASS'
    'FAULT_INJECTION_BEFORE_MIGRATION_HISTORY=PASS'
    'RECOVERY_METHOD=RESTORE_VERIFIED_PREUPGRADE_BACKUP'
    'BLIND_RETRY=NOT_USED'
    'PRODUCTION_DATABASE=NOT_ACCESSED'
    'CT164=NOT_ACCESSED'
    'HTTP_LISTENER=NOT_STARTED'
    'PVE_DEPLOYMENT=NONE'
    'MIGRATION_ARTIFACTS=UNCOMMITTED'
    'RELEASE_GATE=HOLD_PENDING_OPERATOR_EVIDENCE_REVIEW'
)
[IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]$Manifest)

Write-Host ''
Write-Host 'F2A_S2_OPERATOR_QA=PASS'
Write-Host "TEST_COMMIT_SHA=$CommitSha"
Write-Host "EVIDENCE_DIR=$Evidence"
