# YangSpace S0-02 — validate a real npm lockfile in a disposable workspace.
# Never run npm ci inside the live Angular source tree: on Windows the dev
# server can keep @lmdb/*.node open (EPERM on unlink).
#
# Usage: .\scripts\Prepare-Web-Lock.ps1
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$KeepValidationWorkspace
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Web = Join-Path $Root 'apps\web'
$Lock = Join-Path $Web 'package-lock.json'
$Package = Join-Path $Web 'package.json'
$Scratch = Join-Path ([System.IO.Path]::GetTempPath()) (
    'yangspace-npm-qa-' + [guid]::NewGuid().ToString('N')
)

if (-not (Test-Path -LiteralPath $Package -PathType Leaf)) {
    throw 'PACKAGE_JSON_NOT_FOUND'
}
foreach ($tool in @('node', 'npm', 'robocopy')) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw ('REQUIRED_TOOL_MISSING=' + $tool)
    }
}

$Created = $false
$Succeeded = $false

Push-Location $Web
try {
    & node -e "const p=require('./package.json');if(!p.devDependencies['@playwright/test'])process.exit(1)"
    if ($LASTEXITCODE -ne 0) { throw 'PACKAGE_JSON_MISSING_PLAYWRIGHT' }

    # --package-lock-only does not replace or delete active node_modules.
    & npm install --package-lock-only --ignore-scripts --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'NPM_LOCK_GENERATION_FAILED' }
    if (-not (Test-Path -LiteralPath $Lock -PathType Leaf)) {
        throw 'NPM_LOCKFILE_NOT_CREATED'
    }
    $SourceLockSha = (Get-FileHash -LiteralPath $Lock -Algorithm SHA256).Hash

    # A separate directory is essential on Windows: npm ci deletes its target
    # node_modules, which ng serve can keep locked by native LMDB binaries.
    New-Item -ItemType Directory -Path $Scratch -Force | Out-Null
    $Created = $true
    Write-Host 'VALIDATION_MODE=ISOLATED_WORKSPACE'
    Write-Host 'LIVE_NODE_MODULES_MUTATED=NO'
    Write-Host 'LIVE_ANGULAR_SERVER_STOP_REQUIRED=NO'

    $CopyArgs = @(
        $Web, $Scratch, '/E', '/XJ', '/R:1', '/W:1',
        '/XD', 'node_modules', 'dist', '.angular', 'out-tsc', 'coverage',
        'test-results', 'playwright-report', 'blob-report',
        '/NFL', '/NDL', '/NJH', '/NJS', '/NP'
    )
    & robocopy @CopyArgs
    $RobocopyCode = $LASTEXITCODE
    # Robocopy 0..7 are successful bitmasks; >=8 signals failure.
    if ($RobocopyCode -ge 8) {
        throw ('ISOLATED_COPY_FAILED_ROBOCOPY_EXIT=' + $RobocopyCode)
    }
    $StagedLock = Join-Path $Scratch 'package-lock.json'
    if (-not (Test-Path -LiteralPath $StagedLock -PathType Leaf)) {
        throw 'ISOLATED_LOCKFILE_MISSING'
    }
    $StagedLockSha = (Get-FileHash -LiteralPath $StagedLock -Algorithm SHA256).Hash
    if ($StagedLockSha -cne $SourceLockSha) {
        throw 'ISOLATED_LOCKFILE_HASH_MISMATCH'
    }

    Push-Location $Scratch
    try {
        & npm ci --no-audit --no-fund
        if ($LASTEXITCODE -ne 0) { throw 'ISOLATED_NPM_CI_FAILED' }
        Write-Host 'ISOLATED_NPM_CI=PASS'

        if (-not $SkipBuild) {
            & npm run typecheck
            if ($LASTEXITCODE -ne 0) { throw 'ISOLATED_ANGULAR_TYPECHECK_FAILED' }
            & npm run build
            if ($LASTEXITCODE -ne 0) { throw 'ISOLATED_ANGULAR_BUILD_FAILED' }
            Write-Host 'ISOLATED_ANGULAR_TYPECHECK=PASS'
            Write-Host 'ISOLATED_ANGULAR_BUILD=PASS'
        }
    } finally {
        Pop-Location
    }

    if ((Get-FileHash -LiteralPath $Lock -Algorithm SHA256).Hash -cne $SourceLockSha) {
        throw 'SOURCE_LOCKFILE_CHANGED_DURING_VALIDATION'
    }
    $Succeeded = $true
    Write-Host 'LOCKFILE_GENERATION=PASS'
    Write-Host 'LOCKFILE_SHA256_VERIFIED=YES'
    Write-Host 'LOCKFILE_PATH=apps/web/package-lock.json'
    Write-Host 'NEXT=Review and commit apps/web/package-lock.json; then rerun CI.'
}
finally {
    Pop-Location
    if ($Created -and -not $KeepValidationWorkspace) {
        try {
            Remove-Item -LiteralPath $Scratch -Recurse -Force -ErrorAction Stop
        } catch {
            Write-Warning ('TEMP_CLEANUP_FAILED: ' + $Scratch + ' — ' + $_.Exception.Message)
        }
    } elseif ($Created) {
        Write-Host ('VALIDATION_WORKSPACE_RETAINED=' + $Scratch)
    }
    if (-not $Succeeded) {
        Write-Warning 'LOCKFILE_VALIDATION_INCOMPLETE: no GitHub or production changes were applied.'
    }
}
