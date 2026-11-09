# YangSpace manual API integration — isolated snapshot, random loopback port,
# random bootstrap identity and disposable SQLite. NEVER touches live DB/PVE.
# Requirements: Windows PowerShell 5.1+, git, tar, dotnet 10. No Python.
[CmdletBinding()]
param([string]$CommitSha='', [switch]$KeepSandbox)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
foreach($name in @('git.exe','tar.exe','dotnet.exe','powershell.exe')){
    if(-not (Get-Command $name -ErrorAction SilentlyContinue)){throw "REQUIRED_TOOL_MISSING=$name"}
}
$Git=(Get-Command git.exe).Source
$Tar=(Get-Command tar.exe).Source
$Dotnet=(Get-Command dotnet.exe).Source
$WindowsPS=(Get-Command powershell.exe).Source
if(-not $CommitSha){
    $CommitSha=(& $Git -C $Repo rev-parse HEAD).Trim()
    if($LASTEXITCODE -ne 0){throw 'GIT_HEAD_NOT_FOUND'}
}
if($CommitSha -cnotmatch '^[0-9a-f]{40}$'){throw 'FULL_COMMIT_SHA_REQUIRED'}
& $Git -C $Repo cat-file -e ($CommitSha+'^{commit}')
if($LASTEXITCODE -ne 0){throw 'COMMIT_NOT_FOUND'}
$Sandbox=Join-Path ([IO.Path]::GetTempPath()) ('yangspace-ci-'+[guid]::NewGuid().ToString('N'))
$Source=Join-Path $Sandbox 'source'
$Db=Join-Path $Sandbox 'preview.db'
$State=Join-Path $Sandbox 'state.json'
$Evidence=Join-Path $Repo ('logs\yangspace\api-integration\'+$CommitSha.Substring(0,12)+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $Evidence -Force | Out-Null
$Created=$false
$Pass=$false
$ApiProcess=$null
$EnvNames=@('ASPNETCORE_ENVIRONMENT','ASPNETCORE_URLS','Preview__Enabled','ConnectionStrings__Main',
    'Bootstrap__AdminEmail','Bootstrap__AdminPassword','Preview__AllowRegistration','YANGSPACE_TEST_ISOLATED_DB',
    'YANGSPACE_TEST_SANDBOX','YANGSPACE_TEST_DB_PATH','YANGSPACE_TEST_STATE','YANGSPACE_TEST_BASE_URL')
$EnvOriginal=@{}
foreach($name in $EnvNames){$EnvOriginal[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
function Step([string]$Label,[string]$Exe,[string[]]$Arguments,[string]$Wd){
    Invoke-YangSpaceStage -Label $Label -Executable $Exe -CommandArguments $Arguments -WorkingDirectory $Wd -EvidenceDirectory $Evidence -TimeoutSeconds 600
}

function Stop-Api {
    if($null -ne $ApiProcess -and -not $ApiProcess.HasExited){
        Stop-Process -Id $ApiProcess.Id -Force -ErrorAction Stop
        [void]$ApiProcess.WaitForExit(10000)
    }
}
try {
    New-Item -ItemType Directory -Path $Source -Force | Out-Null
    $Created=$true
    [IO.File]::WriteAllText((Join-Path $Sandbox '.yangspace-disposable-test'),('YES'+[char]10))
    $Archive=Join-Path $Sandbox 'source.tar'
    & $Git -C $Repo archive --format=tar --output=$Archive $CommitSha
    if($LASTEXITCODE -ne 0){throw 'GIT_ARCHIVE_FAILED'}
    & $Tar -xf $Archive -C $Source
    if($LASTEXITCODE -ne 0){throw 'SNAPSHOT_EXTRACTION_FAILED'}
    Step 'api-restore' $Dotnet @('restore','apps/api/YangSpace.Api.csproj','-warnaserror') $Source
    Step 'api-build' $Dotnet @('build','apps/api/YangSpace.Api.csproj','-c','Release','--no-restore','-warnaserror') $Source
    $listener=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0)
    $listener.Start()
    $Port=[int]$listener.LocalEndpoint.Port
    $listener.Stop()
    $BaseUrl='http://127.0.0.1:'+$Port
    $bytes=New-Object byte[] 24
    $rng=[Security.Cryptography.RandomNumberGenerator]::Create()
    try{$rng.GetBytes($bytes)}finally{$rng.Dispose()}
    $Password='Aa!1'+[BitConverter]::ToString($bytes).Replace('-','').ToLowerInvariant()
    $values=@{
        'ASPNETCORE_ENVIRONMENT'='Staging';'ASPNETCORE_URLS'=$BaseUrl;
        'Preview__Enabled'='true';'Preview__AllowRegistration'='true';'ConnectionStrings__Main'=('Data Source='+$Db);
        'Bootstrap__AdminEmail'='ci-admin@example.invalid';
        'Bootstrap__AdminPassword'=$Password;'YANGSPACE_TEST_ISOLATED_DB'='YES';
        'YANGSPACE_TEST_SANDBOX'=$Sandbox;'YANGSPACE_TEST_DB_PATH'=$Db;
        'YANGSPACE_TEST_STATE'=$State;'YANGSPACE_TEST_BASE_URL'=$BaseUrl
    }
    foreach($name in $EnvNames){[Environment]::SetEnvironmentVariable($name,$values[$name],'Process')}
    $Dll=Join-Path $Source 'apps\api\bin\Release\net10.0\YangSpace.Api.dll'
    if(-not(Test-Path $Dll)){throw 'API_DLL_MISSING'}
    for($cycle=1;$cycle -le 2;$cycle++){
        $stdout=Join-Path $Evidence ("api-cycle-$cycle.stdout.log")
        $stderr=Join-Path $Evidence ("api-cycle-$cycle.stderr.log")
        $ApiProcess=Start-Process -FilePath $Dotnet -ArgumentList ('"'+$Dll+'"') -WorkingDirectory (Split-Path $Dll -Parent) -PassThru -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        $ready=$false
        for($attempt=0;$attempt -lt 50;$attempt++){
            if($ApiProcess.HasExited){throw 'ISOLATED_API_EXITED_PREMATURELY'}
            try {
                $resp=Invoke-WebRequest -UseBasicParsing -Uri ($BaseUrl+'/health/ready') -TimeoutSec 2 -ErrorAction Stop
                if([int]$resp.StatusCode -eq 200){$ready=$true;break}
            } catch {Start-Sleep -Milliseconds 300}
        }
        if(-not $ready){throw 'ISOLATED_API_READINESS_TIMEOUT'}
        if($cycle -eq 1){
            Step 'api-prepare' $WindowsPS @('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File','scripts/Invoke-Api-Integration.ps1','-Phase','prepare') $Source
        }else{
            Step 'api-restart-verify' $WindowsPS @('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File','scripts/Invoke-Api-Integration.ps1','-Phase','verify') $Source
        }
        Stop-Api
        $ApiProcess=$null
        if($cycle -eq 1){
            # The same test database is reused to verify disabled-by-default
            # registration while existing admin authentication remains functional.
            [Environment]::SetEnvironmentVariable('Preview__AllowRegistration','false','Process')
            $stillListening=$false
            try {
                $resp=Invoke-WebRequest -UseBasicParsing -Uri ($BaseUrl+'/health/ready') -TimeoutSec 2 -ErrorAction Stop
                $stillListening=([int]$resp.StatusCode -eq 200)
            }catch{}
            if($stillListening){throw 'API_PROCESS_RESTART_NOT_VERIFIED'}
        }
    }
    # SEC-02: attempt a privileged bootstrap with an existing, ordinary
    # test user on the SAME isolated SQLite DB. It MUST abort startup.
    $saved=ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($State))
    $ReaderEmail=[string]$saved.readerEmail
    if ($ReaderEmail -cnotmatch '^ci-reader-[0-9a-f]{12}@example\.invalid$') {
        throw 'SEC02_INVALID_DISPOSABLE_NONADMIN_IDENTITY'
    }
    $OriginalBootstrapEmail=[Environment]::GetEnvironmentVariable('Bootstrap__AdminEmail','Process')
    $ProbeProcess=$null
    $ProbeOut=Join-Path $Evidence 'bootstrap-denial.stdout.log'
    $ProbeErr=Join-Path $Evidence 'bootstrap-denial.stderr.log'
    try {
        [Environment]::SetEnvironmentVariable('Bootstrap__AdminEmail',$ReaderEmail,'Process')
        Write-Host 'STAGE=api-bootstrap-denial'
        $ProbeProcess=Start-Process -FilePath $Dotnet -ArgumentList ('"'+$Dll+'"') -WorkingDirectory (Split-Path $Dll -Parent) -NoNewWindow -PassThru -RedirectStandardOutput $ProbeOut -RedirectStandardError $ProbeErr
        $Deadline=[DateTime]::UtcNow.AddSeconds(20)
        while (-not $ProbeProcess.HasExited -and [DateTime]::UtcNow -lt $Deadline) {
            Start-Sleep -Milliseconds 200
        }
        if (-not $ProbeProcess.HasExited) {
            throw 'SEC02_FAIL: EXISTING_NONADMIN_BOOTSTRAP_DID_NOT_ABORT'
        }
        $ProbeError=''
        foreach ($Log in @($ProbeErr,$ProbeOut)) {
            if (Test-Path -LiteralPath $Log) {
                $ProbeError += [IO.File]::ReadAllText($Log) + [char]10
            }
        }
        if ($ProbeError -cnotmatch 'Existing non-admin account cannot be promoted through bootstrap\.') {
            throw 'SEC02_FAIL: EXPECTED_PRIVILEGE_ESCALATION_DENIAL_NOT_OBSERVED'
        }
        Write-Host 'STAGE_PASS=api-bootstrap-denial'
        Write-Host 'BOOTSTRAP_PRIVILEGE_ESCALATION_DENIED=PASS'
    }
    finally {
        if ($null -ne $ProbeProcess -and -not $ProbeProcess.HasExited) {
            Stop-Process -Id $ProbeProcess.Id -Force -ErrorAction Stop
            [void]$ProbeProcess.WaitForExit(10000)
        }
        [Environment]::SetEnvironmentVariable('Bootstrap__AdminEmail',$OriginalBootstrapEmail,'Process')
    }

    # SEC-02: recovery must preserve admin access, registration denial,
    # existing content and the test user's non-admin authorization.
    Write-Host 'STAGE=api-post-bootstrap-recovery'
    $ApiProcess=Start-Process -FilePath $Dotnet -ArgumentList ('"'+$Dll+'"') -WorkingDirectory (Split-Path $Dll -Parent) -NoNewWindow -PassThru -RedirectStandardOutput (Join-Path $Evidence 'api-recovery.stdout.log') -RedirectStandardError (Join-Path $Evidence 'api-recovery.stderr.log')
    $Recovered=$false
    for ($attempt=0;$attempt -lt 50;$attempt++) {
        if ($ApiProcess.HasExited) {throw 'SEC02_FAIL: ISOLATED_API_EXITED_DURING_RECOVERY'}
        try {
            $resp=Invoke-WebRequest -UseBasicParsing -Uri ($BaseUrl+'/health/ready') -TimeoutSec 2 -ErrorAction Stop
            if ([int]$resp.StatusCode -eq 200) { $Recovered=$true; break }
        } catch {
            Start-Sleep -Milliseconds 300
        }
    }
    if (-not $Recovered) { throw 'SEC02_FAIL: ISOLATED_API_RECOVERY_TIMEOUT' }
    Step 'api-post-bootstrap-verify' $WindowsPS @('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File','scripts/Invoke-Api-Integration.ps1','-Phase','verify') $Source
    Stop-Api
    $ApiProcess=$null
    Write-Host 'STAGE_PASS=api-post-bootstrap-recovery'
    Write-Host 'BOOTSTRAP_NONADMIN_ROLE_RETAINED=PASS'
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),@(
        ('COMMIT_SHA='+$CommitSha),'REAL_API_INTEGRATION=PASS',
        'DB_SCOPE=DISPOSABLE_OS_TEMP_SQLITE','AUTH_AND_LIFECYCLE=PASS',
        'PROCESS_RESTART_PERSISTENCE=PASS','BOOTSTRAP_PRIVILEGE_ESCALATION_DENIED=PASS',
        'BOOTSTRAP_NONADMIN_ROLE_RETAINED=PASS','PVE_DEPLOYMENT=NONE'
    ))
    $Pass=$true
    Write-Host 'REAL_API_INTEGRATION=PASS'
    Write-Host ('EVIDENCE_DIR='+$Evidence)
}finally{
    try{Stop-Api}catch{Write-Warning 'ISOLATED_API_CLEANUP_FAILED'}
    foreach($name in $EnvNames){[Environment]::SetEnvironmentVariable($name,$EnvOriginal[$name],'Process')}
    if($Created -and -not $KeepSandbox){
        try{Remove-Item -LiteralPath $Sandbox -Recurse -Force -ErrorAction Stop}
        catch{Write-Warning ('TEMP_CLEANUP_FAILED='+$Sandbox)}
    }elseif($Created){Write-Host ('TEMP_SANDBOX_RETAINED='+$Sandbox)}
    if(-not $Pass){
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),'REAL_API_INTEGRATION=HOLD')
        Write-Warning ('REAL_API_INTEGRATION=HOLD; EVIDENCE_DIR='+$Evidence)
    }
}
