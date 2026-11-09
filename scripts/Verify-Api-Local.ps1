# YangSpace manual API integration — isolated snapshot, random loopback port,
# random bootstrap identity and disposable SQLite. NEVER touches live DB/PVE.
# Requirements: Windows PowerShell 5.1+, git, tar, dotnet 10, python 3.11+.
[CmdletBinding()]
param([string]$CommitSha='', [switch]$KeepSandbox)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
foreach($name in @('git.exe','tar.exe','dotnet.exe','python.exe')){
    if(-not (Get-Command $name -ErrorAction SilentlyContinue)){throw "REQUIRED_TOOL_MISSING=$name"}
}
$Git=(Get-Command git.exe).Source
$Tar=(Get-Command tar.exe).Source
$Dotnet=(Get-Command dotnet.exe).Source
$Python=(Get-Command python.exe).Source
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
    'Bootstrap__AdminEmail','Bootstrap__AdminPassword','YANGSPACE_TEST_ISOLATED_DB',
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
        'Preview__Enabled'='true';'ConnectionStrings__Main'=('Data Source='+$Db);
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
            Step 'api-prepare' $Python @('scripts/api-integration.py','prepare') $Source
        }else{
            Step 'api-restart-verify' $Python @('scripts/api-integration.py','verify') $Source
        }
        Stop-Api
        $ApiProcess=$null
        if($cycle -eq 1){
            $stillListening=$false
            try {
                $resp=Invoke-WebRequest -UseBasicParsing -Uri ($BaseUrl+'/health/ready') -TimeoutSec 2 -ErrorAction Stop
                $stillListening=([int]$resp.StatusCode -eq 200)
            }catch{}
            if($stillListening){throw 'API_PROCESS_RESTART_NOT_VERIFIED'}
        }
    }
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),@(
        ('COMMIT_SHA='+$CommitSha),'REAL_API_INTEGRATION=PASS',
        'DB_SCOPE=DISPOSABLE_OS_TEMP_SQLITE','AUTH_AND_LIFECYCLE=PASS',
        'PROCESS_RESTART_PERSISTENCE=PASS','PVE_DEPLOYMENT=NONE'
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
