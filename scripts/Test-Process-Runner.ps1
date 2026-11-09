# Verify that the Windows QA process runner returns real exit codes,
# captures output and fails closed on non-zero status. No API/DB involved.
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
$node=Get-Command node.exe -ErrorAction Stop
$testDir=Join-Path ([IO.Path]::GetTempPath()) ('yangspace-runner-test-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
try {
    $good=Join-Path $testDir 'good.js'
    $bad=Join-Path $testDir 'bad.js'
    [IO.File]::WriteAllText($good, "process.stdout.write('QA_OK'); process.exit(0);")
    [IO.File]::WriteAllText($bad, "process.stderr.write('EXPECTED_FAILURE'); process.exit(7);")

    Invoke-YangSpaceStage -Label 'runner-success' -Executable $node.Source -CommandArguments @($good) -WorkingDirectory $testDir -EvidenceDirectory $testDir -TimeoutSeconds 30
    $stdout=Get-Content (Join-Path $testDir 'runner-success.stdout.log') -Raw
    if($stdout -cne 'QA_OK'){throw 'RUNNER_STDOUT_ASSERTION_FAILED'}

    $failedAsExpected=$false
    try {
        Invoke-YangSpaceStage -Label 'runner-failure' -Executable $node.Source -CommandArguments @($bad) -WorkingDirectory $testDir -EvidenceDirectory $testDir -TimeoutSeconds 30
    } catch {
        if($_.Exception.Message -like '*STAGE_FAILED=runner-failure EXIT=7*'){
            $failedAsExpected=$true
            Write-Host 'RUNNER_NONZERO_EXIT=PASS (exit 7 rejected)'
        } else {throw}
    }
    if(-not $failedAsExpected){throw 'RUNNER_DID_NOT_PROPAGATE_NONZERO_EXIT'}

    # npm.cmd needs cmd.exe as its interpreter; prove this path works too.
    $npm=Get-Command npm.cmd -ErrorAction Stop
    Invoke-YangSpaceStage -Label 'runner-npm-cmd' -Executable $npm.Source -CommandArguments @('--version') -WorkingDirectory $testDir -EvidenceDirectory $testDir -TimeoutSeconds 60
    $npmVersion=(Get-Content (Join-Path $testDir 'runner-npm-cmd.stdout.log') -Raw).Trim()
    if($npmVersion -notmatch '^\d+\.\d+\.\d+'){throw 'RUNNER_NPM_CMD_ASSERTION_FAILED'}

    Write-Host 'PROCESS_RUNNER_SELFTEST=PASS'
} finally {
    try{Remove-Item -LiteralPath $testDir -Recurse -Force -ErrorAction Stop}
    catch{Write-Warning ('RUNNER_TEST_CLEANUP_FAILED='+$testDir)}
}
