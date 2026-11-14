# YangSpace S0 — MANUAL local validation, no GitHub Actions and no live
# source/node_modules/DB changes. Runs a committed snapshot in TEMP.
# Example: .\scripts\Verify-Source-Local.ps1
[CmdletBinding()]
param(
    [string]$CommitSha = '',
    [switch]$SkipBrowser,
    [switch]$KeepSandbox
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Get-Executable([string]$Name) {
    $item = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $item) { throw ('MISSING_EXECUTABLE=' + $Name) }
    return $item.Source
}
$Git = Get-Executable 'git.exe'
$Tar = Get-Executable 'tar.exe'
$Npm = Get-Executable 'npm.cmd'
$Node = Get-Executable 'node.exe'
$Dotnet = Get-Executable 'dotnet.exe'

if ([string]::IsNullOrWhiteSpace($CommitSha)) {
    $CommitSha = (& $Git -C $Repo rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'GIT_HEAD_NOT_FOUND' }
}
if ($CommitSha -cnotmatch '^[0-9a-f]{40}$') { throw 'FULL_40_CHAR_COMMIT_SHA_REQUIRED' }
& $Git -C $Repo cat-file -e ($CommitSha + '^{commit}')
if ($LASTEXITCODE -ne 0) { throw 'COMMIT_NOT_AVAILABLE_LOCALLY' }

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$Evidence = Join-Path $Repo ('logs\yangspace\verify\' + $CommitSha.Substring(0, 12) + '-' + $RunId)
$Sandbox = Join-Path ([System.IO.Path]::GetTempPath()) ('yangspace-s0-' + [guid]::NewGuid().ToString('N'))
$Source = Join-Path $Sandbox 'source'
$Created = $false
$Pass = $false

New-Item -ItemType Directory -Path $Evidence -Force | Out-Null

. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
function Invoke-Stage([string]$Label,[string]$Exe,[string[]]$Arguments,[string]$Directory){
    Invoke-YangSpaceStage -Label $Label -Executable $Exe -CommandArguments $Arguments -WorkingDirectory $Directory -EvidenceDirectory $Evidence -TimeoutSeconds 600
}

try {
    New-Item -ItemType Directory -Path $Source -Force | Out-Null
    $Created = $true
    $Archive = Join-Path $Sandbox 'snapshot.tar'
    & $Git -C $Repo archive --format=tar --output=$Archive $CommitSha
    if ($LASTEXITCODE -ne 0) { throw 'GIT_ARCHIVE_FAILED' }
    & $Tar -xf $Archive -C $Source
    if ($LASTEXITCODE -ne 0) { throw 'SNAPSHOT_EXTRACTION_FAILED' }

    $Web = Join-Path $Source 'apps\web'
    if (-not (Test-Path (Join-Path $Web 'package-lock.json'))) { throw 'TRACKED_NPM_LOCK_MISSING' }
    Invoke-Stage 'web-lock' $Node @('scripts/verify-lock.mjs') $Web
    Invoke-Stage 'web-install' $Npm @('ci', '--no-audit', '--no-fund') $Web
    Invoke-Stage 'web-audit' $Npm @('audit', '--audit-level=high') $Web
    Invoke-Stage 'web-typecheck' $Npm @('run', 'typecheck') $Web
    Invoke-Stage 'web-build' $Npm @('run', 'build') $Web
    if (-not $SkipBrowser) {
        # Playwright starts its OWN local test server on 127.0.0.1:4300.
        # Does not need the live .NET process or live Angular on 4200.
        Invoke-Stage 'web-playwright' $Npm @('run', 'test:e2e') $Web
    }

    Invoke-Stage 'api-restore' $Dotnet @('restore', 'apps/api/YangSpace.Api.csproj', '-warnaserror') $Source
    Invoke-Stage 'api-build' $Dotnet @('build', 'apps/api/YangSpace.Api.csproj', '-c', 'Release', '--no-restore', '-warnaserror') $Source
    $Manifest = @(
        'COMMIT_SHA=' + $CommitSha
        'SOURCE_VALIDATION=PASS'
        'QA_KIND=MANUAL_LOCAL_CLEAN_SNAPSHOT'
        'SRC=GIT_ARCHIVE'
        'WEB_LOCK_NPM_CI_AUDIT_TYPECHECK_BUILD=PASS'
        'WEB_BROWSER=' + $(if ($SkipBrowser) { 'SKIPPED' } else { 'PASS' })
        'DOTNET_RESTORE_BUILD=PASS'
        'REAL_API_INTEGRATION=NOT_RUN_BY_THIS_SCRIPT'
        'PVE_DEPLOYMENT=NONE'
        'PASS_SCOPE=SOURCE_VALIDATION_NOT_PRODUCTION_RELEASE'
    )
    [System.IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'), $Manifest)
    $Pass = $true
    Write-Host 'SOURCE_VALIDATION=PASS'
    Write-Host ('SOURCE_COMMIT=' + $CommitSha)
    Write-Host ('EVIDENCE_DIR=' + $Evidence)
}
finally {
    if ($Created -and -not $KeepSandbox) {
        try { Remove-Item -LiteralPath $Sandbox -Recurse -Force -ErrorAction Stop }
        catch { Write-Warning ('TEMP_CLEANUP_FAILED=' + $Sandbox) }
    } elseif ($Created) {
        Write-Host ('TEMP_SANDBOX_RETAINED=' + $Sandbox)
    }
    if (-not $Pass) {
        [System.IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'), 'SOURCE_VALIDATION=HOLD')
        Write-Warning ('SOURCE_VALIDATION=HOLD; EVIDENCE_DIR=' + $Evidence)
    }
}
