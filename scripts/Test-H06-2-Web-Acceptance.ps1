[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha,

    [string]$Work = 'D:\GitHub\YangSpaceApp-F2b-H06.2-20261118',
    [string]$EvidenceRoot = 'D:\GitHub\YangSpaceApp-QA-Evidence\H06.2',

    [switch]$InstallChromium
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ExpectedBranch = 'feat/yangspace-f2b-h06-2-public-product-ux-20261118'
$Work = (Resolve-Path -LiteralPath $Work).Path
$App = Join-Path $Work 'apps\web'
if (-not (Test-Path -LiteralPath (Join-Path $App 'package-lock.json') -PathType Leaf)) {
    throw 'H06_2_WEB_APP_MISSING'
}

function Get-GitValue([string[]]$Arguments) {
    $value = & git -C $Work @Arguments
    if ($LASTEXITCODE -ne 0) { throw ('H06_2_GIT_FAILED=' + ($Arguments -join ' ')) }
    return ($value | Out-String).Trim()
}

if ((Get-GitValue @('branch', '--show-current')) -cne $ExpectedBranch) {
    throw 'H06_2_WRONG_BRANCH'
}
if ((Get-GitValue @('rev-parse', 'HEAD')) -cne $CommitSha) {
    throw 'H06_2_EXACT_SHA_MISMATCH'
}
if (@(& git -C $Work status --porcelain --untracked-files=all).Count -gt 0) {
    & git -C $Work status --short
    throw 'H06_2_SOURCE_DIRTY_BEFORE'
}

$remoteLines = @(& git -C $Work ls-remote --heads origin ('refs/heads/' + $ExpectedBranch))
if ($LASTEXITCODE -ne 0 -or $remoteLines.Count -ne 1) {
    throw 'H06_2_REMOTE_BRANCH_LOOKUP_FAILED'
}
$remoteSha = ($remoteLines[0] -split '\s+')[0]
if ($remoteSha -cne $CommitSha) { throw 'H06_2_REMOTE_SHA_MISMATCH' }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$Evidence = Join-Path $EvidenceRoot ($CommitSha + '-' + $stamp)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null

$gates = [ordered]@{
    NPM_CI = 'NOT_RUN'
    TYPECHECK = 'NOT_RUN'
    PRODUCTION_BUILD = 'NOT_RUN'
    TARGETED_PLAYWRIGHT = 'NOT_RUN'
    FULL_PLAYWRIGHT = 'NOT_RUN'
    SOURCE_CLEAN_POST = 'NOT_RUN'
}
$failure = $null

function Invoke-NativeGate {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Binary,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )
    $log = Join-Path $Evidence ($Name + '.log')
    # Windows PowerShell 5.1 can wrap native stderr warnings as ErrorRecords.
    # A warning is not a failing exit code: preserve stderr and inspect EXIT.
    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $exitCode = -1
    try {
        & $Binary @Arguments *>&1 | Out-File -LiteralPath $log -Encoding utf8
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }
    Get-Content -LiteralPath $log -Tail 25 | ForEach-Object { Write-Host $_ }
    Write-Host ('GATE=' + $Name + ' EXIT=' + $exitCode)
    if ($exitCode -ne 0) { throw ('H06_2_GATE_FAILED=' + $Name + ' EXIT=' + $exitCode) }
}

function Save-BrowserEvidence([string]$Label) {
    $src = Join-Path $App 'test-results'
    if (Test-Path -LiteralPath $src -PathType Container) {
        $dest = Join-Path $Evidence ('test-results-' + $Label)
        Copy-Item -LiteralPath $src -Destination $dest -Recurse -Force
    }
}

Push-Location $App
try {
    $versions = @(
        'COMMIT_SHA=' + $CommitSha,
        'BRANCH=' + $ExpectedBranch,
        'NODE=' + (& node --version),
        'NPM=' + (& npm.cmd --version)
    )
    $versions | Set-Content -LiteralPath (Join-Path $Evidence 'runtime.txt') -Encoding UTF8

    Invoke-NativeGate 'npm-ci' 'npm.cmd' @('ci', '--no-audit', '--no-fund')
    $gates.NPM_CI = 'PASS'

    if ($InstallChromium) {
        Invoke-NativeGate 'chromium-install' 'npx.cmd' @('playwright', 'install', 'chromium')
    }

    Invoke-NativeGate 'typecheck' 'npm.cmd' @('run', 'typecheck')
    $gates.TYPECHECK = 'PASS'

    Invoke-NativeGate 'production-build' 'npm.cmd' @('run', 'build')
    $gates.PRODUCTION_BUILD = 'PASS'

    try {
        Invoke-NativeGate 'targeted-playwright' 'npx.cmd' @(
            'playwright', 'test',
            'e2e/homepage-highlights.spec.ts',
            'e2e/detail-navigation.spec.ts',
            'e2e/product-responsive.spec.ts',
            'e2e/responsive-preview.spec.ts',
            '--reporter=line'
        )
        $gates.TARGETED_PLAYWRIGHT = 'PASS'
    }
    finally { Save-BrowserEvidence 'targeted' }

    try {
        Invoke-NativeGate 'full-playwright' 'npm.cmd' @('run', 'test:e2e', '--', '--reporter=line')
        $gates.FULL_PLAYWRIGHT = 'PASS'
    }
    finally { Save-BrowserEvidence 'full' }
}
catch {
    $failure = $_.Exception.Message
    Write-Host ('H06_2_QA_FAILED=' + $failure)
}
finally {
    Pop-Location
    if ((Get-GitValue @('rev-parse', 'HEAD')) -cne $CommitSha) {
        $gates.SOURCE_CLEAN_POST = 'FAIL_SHA_CHANGED'
    }
    elseif (@(& git -C $Work status --porcelain --untracked-files=all).Count -eq 0) {
        $gates.SOURCE_CLEAN_POST = 'PASS'
    }
    else {
        $gates.SOURCE_CLEAN_POST = 'FAIL_DIRTY'
        & git -C $Work status --porcelain --untracked-files=all |
            Set-Content -LiteralPath (Join-Path $Evidence 'git-status.txt') -Encoding UTF8
    }

    $summary = @('COMMIT_SHA=' + $CommitSha, 'BRANCH=' + $ExpectedBranch)
    foreach ($key in $gates.Keys) { $summary += ($key + '=' + $gates[$key]) }
    $summary += ('FAILURE=' + $failure)
    $summary | Set-Content -LiteralPath (Join-Path $Evidence 'run-summary.txt') -Encoding UTF8

    $files = @(Get-ChildItem -LiteralPath $Evidence -Recurse -File |
        Where-Object { $_.Name -notin @('manifest.txt', 'manifest.sha256') } |
        Sort-Object FullName)
    $manifestLines = @($summary)
    foreach ($file in $files) {
        $rel = $file.FullName.Substring($Evidence.Length).TrimStart('\')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $manifestLines += ('FILE_SHA256=' + $rel + '|' + $hash)
    }
    $manifest = Join-Path $Evidence 'manifest.txt'
    $manifestLines | Set-Content -LiteralPath $manifest -Encoding UTF8
    $manifestHash = (Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash.ToLowerInvariant()
    $manifestHash | Set-Content -LiteralPath (Join-Path $Evidence 'manifest.sha256') -Encoding Ascii
    Write-Host ('EVIDENCE_DIR=' + $Evidence)
    Write-Host ('MANIFEST_SHA256=' + $manifestHash)
    foreach ($key in $gates.Keys) { Write-Host ($key + '=' + $gates[$key]) }
}

if ($failure) { throw ('H06_2_ACCEPTANCE_HOLD=' + $failure) }
if ($gates.SOURCE_CLEAN_POST -ne 'PASS') { throw 'H06_2_SOURCE_CLEAN_POST_FAILED' }
if (@($gates.Values | Where-Object { $_ -ne 'PASS' }).Count -ne 0) {
    throw 'H06_2_INCOMPLETE_GATES'
}
Write-Host 'H06_2_QA=PASS'
