[CmdletBinding()]
param(
    [string]$Work = 'D:\GitHub\YangSpaceApp-F2b-H06.2-20261118',
    [string]$EvidenceRoot = 'D:\GitHub\YangSpaceApp-QA-Evidence\H06.2'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ExpectedBranch = 'feat/yangspace-f2b-h06-2-public-product-ux-20261118'
$Work = (Resolve-Path -LiteralPath $Work).Path
$acceptance = Join-Path $Work 'scripts\Test-H06-2-Web-Acceptance.ps1'
$postflight = Join-Path $Work 'scripts\Verify-H06-2-Web-Postflight.ps1'

foreach ($requiredPath in @($acceptance, $postflight, (Join-Path $Work '.git'))) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw ('H06_2_QA_PACKAGE_FILE_MISSING=' + $requiredPath)
    }
}

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $output = & git -C $Work @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw ('H06_2_QA_PACKAGE_GIT_FAILED=' + ($Arguments -join ' '))
    }
    return $output
}

$branch = (Invoke-Git -Arguments @('branch', '--show-current') | Out-String).Trim()
if ($branch -cne $ExpectedBranch) {
    throw ('H06_2_QA_PACKAGE_WRONG_BRANCH=' + $branch)
}

$dirty = @(Invoke-Git -Arguments @('status', '--porcelain', '--untracked-files=all'))
if ($dirty.Count -gt 0) {
    & git -C $Work status --short
    throw 'H06_2_QA_PACKAGE_SOURCE_DIRTY'
}

Write-Host '=== 1. SYNC EXACT GITHUB BRANCH ==='
[void](Invoke-Git -Arguments @('fetch', '--no-tags', 'origin', $ExpectedBranch))

$counts = ((Invoke-Git -Arguments @('rev-list', '--left-right', '--count', ('HEAD...origin/' + $ExpectedBranch))) | Out-String).Trim() -split '\s+'
if ($counts.Count -ne 2) { throw 'H06_2_QA_PACKAGE_GIT_DIVERGENCE_CHECK_FAILED' }
$localAhead = [int]$counts[0]
$remoteAhead = [int]$counts[1]

if ($localAhead -gt 0 -and $remoteAhead -gt 0) {
    throw 'H06_2_QA_PACKAGE_BRANCH_DIVERGED'
}
if ($localAhead -gt 0) {
    throw 'H06_2_QA_PACKAGE_LOCAL_COMMITS_NOT_PUSHED'
}
if ($remoteAhead -gt 0) {
    [void](Invoke-Git -Arguments @('merge', '--ff-only', ('origin/' + $ExpectedBranch)))
}

$sha = (Invoke-Git -Arguments @('rev-parse', 'HEAD') | Out-String).Trim()
$remoteLines = @(Invoke-Git -Arguments @('ls-remote', '--heads', 'origin', ('refs/heads/' + $ExpectedBranch)))
if ($remoteLines.Count -ne 1) { throw 'H06_2_QA_PACKAGE_REMOTE_BRANCH_MISSING' }
$remoteSha = ($remoteLines[0] -split '\s+')[0]
if ($remoteSha -cne $sha) { throw 'H06_2_QA_PACKAGE_EXACT_REMOTE_SHA_MISMATCH' }

Write-Host ('BRANCH=' + $ExpectedBranch)
Write-Host ('COMMIT_SHA=' + $sha)
Write-Host 'GITHUB_SYNC=PASS'
Write-Host ''

Write-Host '=== 2. INSTALL / BUILD / OPEN BROWSER / PLAYWRIGHT ==='
$runStarted = Get-Date
& $acceptance -CommitSha $sha -Work $Work -EvidenceRoot $EvidenceRoot -InstallChromium -InteractiveBrowser -Headed

if ($LASTEXITCODE -ne 0) {
    throw ('H06_2_QA_PACKAGE_ACCEPTANCE_EXIT=' + $LASTEXITCODE)
}

$evidenceCandidates = @(Get-ChildItem -LiteralPath $EvidenceRoot -Directory |
    Where-Object {
        $_.Name.StartsWith(($sha + '-'), [System.StringComparison]::OrdinalIgnoreCase) -and
        $_.CreationTime -ge $runStarted.AddMinutes(-2)
    } |
    Sort-Object CreationTime -Descending)

if ($evidenceCandidates.Count -eq 0) {
    throw 'H06_2_QA_PACKAGE_EVIDENCE_DIRECTORY_NOT_FOUND'
}
$evidenceDirectory = $evidenceCandidates[0].FullName

Write-Host ''
Write-Host '=== 3. INDEPENDENT EVIDENCE POSTFLIGHT ==='
Write-Host ('EVIDENCE_DIR=' + $evidenceDirectory)
& $postflight -CommitSha $sha -EvidenceDirectory $evidenceDirectory -Work $Work

if ($LASTEXITCODE -ne 0) {
    throw ('H06_2_QA_PACKAGE_POSTFLIGHT_EXIT=' + $LASTEXITCODE)
}

Write-Host ''
Write-Host 'H06_2_AUTOMATED_QA_PACKAGE=PASS'
Write-Host ('COMMIT_SHA=' + $sha)
Write-Host ('EVIDENCE_DIR=' + $evidenceDirectory)
Write-Host 'Следваща стъпка: изпрати screenshots и MANIFEST_SHA256 за визуална приемка.'
