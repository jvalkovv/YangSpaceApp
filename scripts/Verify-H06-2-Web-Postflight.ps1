[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha,

    [Parameter(Mandatory = $true)]
    [string]$EvidenceDirectory,

    [string]$Work = 'D:\GitHub\YangSpaceApp-F2b-H06.2-20261118'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ExpectedBranch = 'feat/yangspace-f2b-h06-2-public-product-ux-20261118'
$Evidence = (Resolve-Path -LiteralPath $EvidenceDirectory).Path.TrimEnd('\')
$Work = (Resolve-Path -LiteralPath $Work).Path

$manifest = Join-Path $Evidence 'manifest.txt'
$hashFile = Join-Path $Evidence 'manifest.sha256'
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf) -or
    -not (Test-Path -LiteralPath $hashFile -PathType Leaf)) {
    throw 'H06_2_POSTFLIGHT_MANIFEST_MISSING'
}
$expectedHash = (Get-Content -LiteralPath $hashFile -Raw).Trim().ToLowerInvariant()
$actualHash = (Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash.ToLowerInvariant()
if ($expectedHash -notmatch '^[0-9a-f]{64}$' -or $actualHash -cne $expectedHash) {
    throw 'H06_2_POSTFLIGHT_MANIFEST_SHA256_MISMATCH'
}
Write-Host 'POSTFLIGHT_MANIFEST_SHA256=PASS'

$rows = @(Get-Content -LiteralPath $manifest)
$required = @(
    'COMMIT_SHA=' + $CommitSha,
    'BRANCH=' + $ExpectedBranch,
    'LOCAL_MOCK_API_SYNTAX=PASS',
    'NPM_CI=PASS',
    'TYPECHECK=PASS',
    'PRODUCTION_BUILD=PASS',
    'TARGETED_PLAYWRIGHT=PASS',
    'FULL_PLAYWRIGHT=PASS',
    'SOURCE_CLEAN_POST=PASS',
    'FAILURE='
)
foreach ($row in $required) {
    if ($rows -cnotcontains $row) {
        throw ('H06_2_POSTFLIGHT_MISSING_MARKER=' + $row)
    }
}
Write-Host 'POSTFLIGHT_REQUIRED_MARKERS=PASS'

$entries = @($rows | Where-Object { $_.StartsWith('FILE_SHA256=') })
if ($entries.Count -lt 5) { throw 'H06_2_POSTFLIGHT_INSUFFICIENT_ARTIFACTS' }
$recorded = @{}
foreach ($entry in $entries) {
    $fields = $entry.Substring('FILE_SHA256='.Length) -split '\|', 2
    if ($fields.Count -ne 2 -or $fields[1] -notmatch '^[0-9a-f]{64}$') {
        throw 'H06_2_POSTFLIGHT_INVALID_FILE_HASH_RECORD'
    }
    $rel = $fields[0]
    if ($rel -match '(^|[\\/])\.\.([\\/]|$)' -or
        [System.IO.Path]::IsPathRooted($rel) -or [string]::IsNullOrWhiteSpace($rel)) {
        throw 'H06_2_POSTFLIGHT_UNSAFE_ARTIFACT_PATH'
    }
    if ($recorded.ContainsKey($rel)) { throw 'H06_2_POSTFLIGHT_DUPLICATE_ARTIFACT' }
    $path = Join-Path $Evidence $rel
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw ('H06_2_POSTFLIGHT_FILE_MISSING=' + $rel)
    }
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -cne $fields[1]) { throw ('H06_2_POSTFLIGHT_FILE_SHA256_MISMATCH=' + $rel) }
    $recorded[$rel] = $true
}

$actualFiles = @(Get-ChildItem -LiteralPath $Evidence -Recurse -File |
    Where-Object { $_.Name -notin @('manifest.txt', 'manifest.sha256') })
foreach ($file in $actualFiles) {
    $rel = $file.FullName.Substring($Evidence.Length).TrimStart('\')
    if (-not $recorded.ContainsKey($rel)) {
        throw ('H06_2_POSTFLIGHT_UNRECORDED_ARTIFACT=' + $rel)
    }
}
if ($actualFiles.Count -ne $recorded.Count) { throw 'H06_2_POSTFLIGHT_ARTIFACT_COUNT_MISMATCH' }
Write-Host 'POSTFLIGHT_ALL_FILE_HASHES=PASS'

$head = (& git -C $Work rev-parse HEAD | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $head -cne $CommitSha) {
    throw 'H06_2_POSTFLIGHT_EXACT_SOURCE_MISMATCH'
}
$branch = (& git -C $Work branch --show-current | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $branch -cne $ExpectedBranch) {
    throw 'H06_2_POSTFLIGHT_BRANCH_MISMATCH'
}
$dirty = @(& git -C $Work status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $dirty.Count -ne 0) {
    throw 'H06_2_POSTFLIGHT_DIRTY_SOURCE'
}
Write-Host 'POSTFLIGHT_SOURCE_CLEAN=PASS'

$remoteLines = @(& git -C $Work ls-remote --heads origin ('refs/heads/' + $ExpectedBranch))
if ($LASTEXITCODE -ne 0 -or $remoteLines.Count -ne 1) {
    throw 'H06_2_POSTFLIGHT_REMOTE_LOOKUP_FAILED'
}
$remoteSha = ($remoteLines[0] -split '\s+')[0]
if ($remoteSha -cne $CommitSha) {
    throw 'H06_2_POSTFLIGHT_REMOTE_SHA_MISMATCH'
}
Write-Host 'POSTFLIGHT_REMOTE_SHA=PASS'
Write-Host ('COMMIT_SHA=' + $CommitSha)
Write-Host ('MANIFEST_SHA256=' + $actualHash)
Write-Host 'H06_2_INDEPENDENT_POSTFLIGHT=PASS'
