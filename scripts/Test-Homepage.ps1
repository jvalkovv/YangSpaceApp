# Read-only homepage API contract + latency probe. No secrets or writes.
[CmdletBinding()]
param(
    [string]$ApiBase = "http://127.0.0.1:5090",
    [ValidateRange(2, 50)][int]$Iterations = 7
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Base = $ApiBase.TrimEnd('/')
$Uri = "$Base/api/v1/homepage"
$Samples = [System.Collections.Generic.List[double]]::new()
$payload = $null

Write-Host "CHECK=$Uri"
try {
    Invoke-RestMethod -Uri "$Base/health/ready" -TimeoutSec 10 | Out-Null

    for ($i = 0; $i -lt $Iterations; $i++) {
        $watch = [System.Diagnostics.Stopwatch]::StartNew()
        $result = Invoke-RestMethod -Uri $Uri -Method Get -TimeoutSec 15
        $watch.Stop()
        $Samples.Add($watch.Elapsed.TotalMilliseconds)
        $payload = $result
    }

    if ($null -eq $payload -or
        $payload.directory.total -lt 1 -or
        $payload.directory.items.Count -lt 1 -or
        $payload.meta.neighborhoods.Count -lt 1) {
        throw 'HOMEPAGE_CONTRACT_FAILED: Missing directory records or filter facets'
    }

    # Distinguish first request (may include JIT warmup) from subsequent samples.
    $warm = @($Samples | Select-Object -Skip 1 | Sort-Object)
    $median = [Math]::Round($warm[[int][Math]::Floor(($warm.Count - 1) / 2)], 1)
    $cold = [Math]::Round($Samples[0], 1)

    Write-Host "HOMEPAGE_CONTRACT=PASS"
    Write-Host "LOCATIONS=$($payload.directory.total)"
    Write-Host "CATEGORIES=$($payload.meta.categories.Count)"
    Write-Host "OFFERS=$($payload.offers.Count)"
    Write-Host "BROCHURES=$($payload.brochures.Count)"
    Write-Host "JOBS=$($payload.jobs.Count)"
    Write-Host "COLD_REQUEST_MS=$cold"
    Write-Host "WARM_MEDIAN_MS=$median"
    Write-Host 'NOTE=These are API timings only, not browser page-load timings.'
}
catch {
    Write-Error "HOMEPAGE_CHECK=FAIL $($_.Exception.Message)"
    exit 1
}
