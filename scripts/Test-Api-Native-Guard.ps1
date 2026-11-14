# Fast, non-destructive Windows check: native integration syntax and fail-closed guard.
# Does NOT launch the API or create any database.
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
$target=Join-Path $PSScriptRoot 'Invoke-Api-Integration.ps1'
$tokens=$null
$parseErrors=$null
[void][System.Management.Automation.Language.Parser]::ParseFile($target,[ref]$tokens,[ref]$parseErrors)
if ($null -ne $parseErrors -and @($parseErrors).Count -gt 0) {
    $parseErrors | ForEach-Object {Write-Error $_.Message}
    throw 'NATIVE_API_INTEGRATION_PARSE_FAIL'
}
Write-Host 'NATIVE_API_INTEGRATION_PARSE=PASS'
$powershell=(Get-Command powershell.exe -ErrorAction Stop).Source
$temp=Join-Path ([IO.Path]::GetTempPath()) ('yangspace-api-guard-test-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp -Force | Out-Null
$old=[Environment]::GetEnvironmentVariable('YANGSPACE_TEST_ISOLATED_DB','Process')
try {
    # Explicitly disarm the write guard regardless of the parent environment.
    [Environment]::SetEnvironmentVariable('YANGSPACE_TEST_ISOLATED_DB','NO','Process')
    $rejected=$false
    try {
        $args=@('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File',$target,'-Phase','prepare')
        Invoke-YangSpaceStage -Label 'api-guard-rejection' -Executable $powershell -CommandArguments $args -WorkingDirectory $temp -EvidenceDirectory $temp -TimeoutSeconds 30
    } catch {
        if($_.Exception.Message -match '^STAGE_FAILED=api-guard-rejection EXIT=[1-9]\d*$'){
            $stderr=Get-Content (Join-Path $temp 'api-guard-rejection.stderr.log') -Raw
            if($stderr -match 'REFUSED:'){
                $rejected=$true
                Write-Host 'NATIVE_API_INTEGRATION_GUARD=PASS (unsafe environment rejected)'
            }else{throw 'GUARD_TEST_FAILED_WRONG_ERROR'}
        }else{throw}
    }
    if(-not $rejected){throw 'GUARD_TEST_FAILED_OPEN'}
    Write-Host 'NATIVE_API_SELFTEST=PASS'
}finally{
    [Environment]::SetEnvironmentVariable('YANGSPACE_TEST_ISOLATED_DB',$old,'Process')
    try{Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction Stop}
    catch{Write-Warning ('GUARD_TEST_CLEANUP_FAILED='+$temp)}
}
