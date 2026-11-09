# Builds an npm lockfile for reviewed commit, then exercises a clean dependency install.
# Run from any directory on Windows after updating apps/web/package.json.
[CmdletBinding()]
param(
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Web = Join-Path $Root 'apps\web'
$Lock = Join-Path $Web 'package-lock.json'
if (-not (Test-Path (Join-Path $Web 'package.json'))) { throw 'PACKAGE_JSON_NOT_FOUND' }
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) { throw 'NPM_NOT_FOUND' }

Push-Location $Web
try {
    & node -e "const p=require('./package.json');if(!p.devDependencies['@playwright/test'])process.exit(1)"
    if ($LASTEXITCODE -ne 0) { throw 'PACKAGE_JSON_MISSING_PLAYWRIGHT' }
    & npm install --package-lock-only --ignore-scripts --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'NPM_LOCK_GENERATION_FAILED' }
    if (-not (Test-Path $Lock)) { throw 'NPM_LOCKFILE_NOT_CREATED' }
    & npm ci --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'NPM_CI_FAILED' }
    if (-not $SkipBuild) {
        & npm run typecheck
        if ($LASTEXITCODE -ne 0) { throw 'ANGULAR_TYPECHECK_FAILED' }
        & npm run build
        if ($LASTEXITCODE -ne 0) { throw 'ANGULAR_BUILD_FAILED' }
    }
    Write-Host 'LOCKFILE_GENERATION=PASS'
    Write-Host 'NPM_CI=PASS'
    Write-Host 'LOCKFILE_PATH=apps/web/package-lock.json'
    Write-Host 'NEXT=Review and commit apps/web/package-lock.json; then rerun CI.'
} finally {
    Pop-Location
}
