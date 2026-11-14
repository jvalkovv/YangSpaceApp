# Diagnose local .NET API vs Angular proxy — read-only, Windows PowerShell 5.1+.
[CmdletBinding()]
param(
    [string]$ApiBase = 'http://127.0.0.1:5090',
    [string]$WebBase = 'http://127.0.0.1:4200'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Check-Endpoint {
    param([string]$Name, [string]$Uri)
    try {
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri $Uri -Method Get -UseBasicParsing -TimeoutSec 6
        $watch.Stop()
        $valid = ($response.StatusCode -eq 200)
        [pscustomobject]@{
            Name = $Name; OK = $valid; Status = [string]$response.StatusCode
            ElapsedMs = [math]::Round($watch.Elapsed.TotalMilliseconds, 1)
            Error = ''
        }
    } catch {
        $status = 'NO_HTTP_RESPONSE'
        if (($_.Exception.PSObject.Properties.Name -contains 'Response') -and $null -ne $_.Exception.Response) {
            try { $status = [string][int]$_.Exception.Response.StatusCode } catch {}
        }
        [pscustomobject]@{
            Name = $Name; OK = $false; Status = $status
            ElapsedMs = $null; Error = $_.Exception.Message
        }
    }
}

$api = $ApiBase.TrimEnd('/')
$web = $WebBase.TrimEnd('/')
$checks = @(
    (Check-Endpoint 'API_LIVE' "$api/health/live")
    (Check-Endpoint 'API_READY' "$api/health/ready")
    (Check-Endpoint 'API_HOMEPAGE_DIRECT' "$api/api/v1/homepage")
    (Check-Endpoint 'ANGULAR_DEV_SERVER' "$web/")
    (Check-Endpoint 'ANGULAR_PROXY_HOMEPAGE' "$web/api/v1/homepage")
)

foreach ($check in $checks) {
    $flag = if ($check.OK) { 'PASS' } else { 'FAIL' }
    Write-Host "$($check.Name)=$flag HTTP=$($check.Status) MS=$($check.ElapsedMs)"
    if ($check.Error) { Write-Host "$($check.Name)_DETAIL=$($check.Error)" }
}

if (-not $checks[0].OK -or -not $checks[1].OK) {
    Write-Host 'ROOT_CAUSE=API_NOT_READY_OR_NOT_LISTENING'
    Write-Host 'NEXT=Start dotnet run --project apps/api/YangSpace.Api.csproj --launch-profile http in a separate terminal.'
    exit 2
}
if (-not $checks[2].OK) {
    Write-Host 'ROOT_CAUSE=API_HOMEPAGE_ENDPOINT_FAILED'
    Write-Host 'NEXT=Check the running API commit, ASPNETCORE_ENVIRONMENT and .NET console errors.'
    exit 3
}
if (-not $checks[3].OK) {
    Write-Host 'ROOT_CAUSE=ANGULAR_DEV_SERVER_NOT_READY'
    Write-Host 'NEXT=Start npm in apps/web with npm start and keep that terminal open.'
    exit 4
}
if (-not $checks[4].OK) {
    Write-Host 'ROOT_CAUSE=ANGULAR_PROXY_MISROUTED_OR_NOT_RESTARTED'
    Write-Host 'NEXT=Restart npm start; check apps/web/proxy.conf.json and apps/web/angular.json.'
    exit 5
}
Write-Host 'LOCAL_STACK=PASS'
Write-Host 'NEXT=Open http://127.0.0.1:4200/ and check the Network tab for uncaught frontend errors if UI still fails.'
