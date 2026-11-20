[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha,

    [string]$Work = 'D:\GitHub\YangSpaceApp-F2b-H06.3-20261120',
    [string]$EvidenceRoot = 'D:\GitHub\YangSpaceApp-QA-Evidence\H06.3',

    [switch]$InstallChromium,
    [switch]$InteractiveBrowser,
    [switch]$Headed
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$GitExe = (Get-Command git.exe -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
$ExpectedBranch = 'feat/yangspace-f2b-h06-3-public-discovery-ux-20261120'
$Work = (Resolve-Path -LiteralPath $Work).Path
$App = Join-Path $Work 'apps\web'
$MockApi = Join-Path $App 'qa\mock-api.mjs'
if (-not (Test-Path -LiteralPath (Join-Path $App 'package-lock.json') -PathType Leaf)) {
    throw 'H06_3_WEB_APP_MISSING'
}
if (-not (Test-Path -LiteralPath $MockApi -PathType Leaf)) {
    throw 'H06_3_LOCAL_MOCK_API_MISSING'
}

function Get-GitValue([string[]]$Arguments) {
    $value = & $GitExe -C $Work @Arguments
    if ($LASTEXITCODE -ne 0) { throw ('H06_3_GIT_FAILED=' + ($Arguments -join ' ')) }
    return ($value | Out-String).Trim()
}

if ((Get-GitValue -Arguments @('branch', '--show-current')) -cne $ExpectedBranch) {
    throw 'H06_3_WRONG_BRANCH'
}
if ((Get-GitValue -Arguments @('rev-parse', 'HEAD')) -cne $CommitSha) {
    throw 'H06_3_EXACT_SHA_MISMATCH'
}
$preflightStatus = @(& $GitExe -C $Work status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $preflightStatus.Count -gt 0) {
    & $GitExe -C $Work status --short
    throw 'H06_3_SOURCE_DIRTY_BEFORE'
}

$remoteLines = @(& $GitExe -C $Work ls-remote --heads origin ('refs/heads/' + $ExpectedBranch))
if ($LASTEXITCODE -ne 0 -or $remoteLines.Count -ne 1) {
    throw 'H06_3_REMOTE_BRANCH_LOOKUP_FAILED'
}
$remoteSha = ($remoteLines[0] -split '\s+')[0]
if ($remoteSha -cne $CommitSha) { throw 'H06_3_REMOTE_SHA_MISMATCH' }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$Evidence = Join-Path $EvidenceRoot ($CommitSha + '-' + $stamp)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null

$gates = [ordered]@{
    LOCAL_MOCK_API_SYNTAX = 'NOT_RUN'
    LOCAL_MOCK_API_SMOKE = 'NOT_RUN'
    PLAYWRIGHT_REUSE_CONFIG = 'NOT_RUN'
    NPM_CI = 'NOT_RUN'
    TYPECHECK = 'NOT_RUN'
    PRODUCTION_BUILD = 'NOT_RUN'
    TARGETED_PLAYWRIGHT = 'NOT_RUN'
    FULL_PLAYWRIGHT = 'NOT_RUN'
    SOURCE_CLEAN_POST = 'NOT_RUN'
}
$failure = $null
$previewProcess = $null
$previousReuseServer = [Environment]::GetEnvironmentVariable('H06_3_REUSE_SERVER', 'Process')
[Environment]::SetEnvironmentVariable('H06_3_REUSE_SERVER', $null, 'Process')

function Invoke-NativeGate {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Binary,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )
    $log = Join-Path $Evidence ($Name + '.log')
    $stdoutLog = Join-Path $Evidence ($Name + '.stdout.log')
    $stderrLog = Join-Path $Evidence ($Name + '.stderr.log')

    # PowerShell 7.3+ can promote native stderr text to ErrorRecords when this
    # preference is enabled. Keep native exit codes authoritative and capture
    # stdout/stderr as files so npm policy warnings never masquerade as failures.
    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $PSNativeCommandUseErrorActionPreference = $false
    $exitCode = -1
    try {
        & $Binary @Arguments 1> $stdoutLog 2> $stderrLog
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }

    $combined = @('=== STDOUT ===')
    if (Test-Path -LiteralPath $stdoutLog) {
        $combined += @(Get-Content -LiteralPath $stdoutLog)
    }
    $combined += '=== STDERR ==='
    if (Test-Path -LiteralPath $stderrLog) {
        $combined += @(Get-Content -LiteralPath $stderrLog)
    }
    $combined | Set-Content -LiteralPath $log -Encoding utf8

    Get-Content -LiteralPath $log -Tail 25 | ForEach-Object { Write-Host $_ }
    Write-Host ('GATE=' + $Name + ' EXIT=' + $exitCode)
    if ($exitCode -ne 0) { throw ('H06_3_GATE_FAILED=' + $Name + ' EXIT=' + $exitCode) }
}

function Save-BrowserEvidence([string]$Label) {
    $src = Join-Path $App 'test-results'
    if (Test-Path -LiteralPath $src -PathType Container) {
        $dest = Join-Path $Evidence ('test-results-' + $Label)
        Copy-Item -LiteralPath $src -Destination $dest -Recurse -Force
    }
}

function Test-LocalTcpPort([int]$Port) {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $client.Connect('127.0.0.1', $Port)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Stop-PreviewProcess([System.Diagnostics.Process]$Process) {
    if ($null -ne $Process -and -not $Process.HasExited) {
        $taskkill = Join-Path $env:SystemRoot 'System32\taskkill.exe'
        & $taskkill /PID $Process.Id /T /F 2>&1 | Out-Null
        try { $Process.WaitForExit(10000) | Out-Null } catch { }
    }
}

function Invoke-LocalMockApiSmoke {
    $log = Join-Path $Evidence 'mock-api-smoke.log'
    $lines = @()
    try {
        $homepage = Invoke-RestMethod -Uri 'http://127.0.0.1:5090/api/v1/homepage' -UseBasicParsing -TimeoutSec 5
        if ($homepage.directory.items.Count -lt 6) { throw 'MOCK_API_PUBLIC_DIRECTORY_FIXTURE_INCOMPLETE' }
        $lines += 'MOCK_API_PUBLIC_DIRECTORY=PASS'

        $detail = Invoke-RestMethod -Uri 'http://127.0.0.1:5090/api/v1/locations/bike-demo' -UseBasicParsing -TimeoutSec 5
        if ($detail.slug -cne 'bike-demo' -or $detail.offers.Count -lt 1) {
            throw 'MOCK_API_PUBLIC_DETAIL_FIXTURE_INCOMPLETE'
        }
        $lines += 'MOCK_API_PUBLIC_DETAIL=PASS'

        $loginBody = @{
            email = 'qa@example.invalid'
            password = 'DisposableTestPassword123!'
        } | ConvertTo-Json -Compress
        $login = Invoke-RestMethod -Method Post -Uri 'http://127.0.0.1:5090/api/auth/login' -ContentType 'application/json' -Body $loginBody -UseBasicParsing -TimeoutSec 5
        if ([string]::IsNullOrWhiteSpace($login.accessToken)) { throw 'MOCK_API_ADMIN_LOGIN_FAILED' }
        $headers = @{ Authorization = 'Bearer ' + $login.accessToken }

        $identity = Invoke-RestMethod -Uri 'http://127.0.0.1:5090/api/v1/admin/me' -Headers $headers -UseBasicParsing -TimeoutSec 5
        if ($identity.role -cne 'PlatformAdmin') { throw 'MOCK_API_ADMIN_ROLE_FAILED' }
        $lines += 'MOCK_API_ADMIN_IDENTITY=PASS'

        $adminLocations = Invoke-RestMethod -Uri 'http://127.0.0.1:5090/api/v1/admin/locations' -Headers $headers -UseBasicParsing -TimeoutSec 5
        if ($adminLocations.Count -lt 6) { throw 'MOCK_API_ADMIN_DIRECTORY_FIXTURE_INCOMPLETE' }
        $lines += 'MOCK_API_ADMIN_DIRECTORY=PASS'

        $content = Invoke-RestMethod -Uri ('http://127.0.0.1:5090/api/v1/admin/locations/' + $adminLocations[0].id + '/content') -Headers $headers -UseBasicParsing -TimeoutSec 5
        if ($null -eq $content.offers -or $null -eq $content.brochures -or $null -eq $content.jobs) {
            throw 'MOCK_API_ADMIN_PUBLICATIONS_FIXTURE_INCOMPLETE'
        }
        $lines += 'MOCK_API_ADMIN_PUBLICATIONS=PASS'
        $lines | Set-Content -LiteralPath $log -Encoding utf8
        Write-Host 'LOCAL_MOCK_API_SMOKE=PASS'
    }
    catch {
        $lines += ('MOCK_API_SMOKE_FAILURE=' + $_.Exception.Message)
        $lines | Set-Content -LiteralPath $log -Encoding utf8
        throw
    }
}

function Start-InteractivePreview {
    $url = 'http://127.0.0.1:4300/'
    if (Test-LocalTcpPort 5090) { throw 'H06_3_MOCK_API_PORT_5090_ALREADY_IN_USE' }
    if (Test-LocalTcpPort 4300) { throw 'H06_3_PREVIEW_PORT_4300_ALREADY_IN_USE' }

    $apiStdout = Join-Path $Evidence 'mock-api.stdout.log'
    $apiStderr = Join-Path $Evidence 'mock-api.stderr.log'
    $webStdout = Join-Path $Evidence 'interactive-preview.stdout.log'
    $webStderr = Join-Path $Evidence 'interactive-preview.stderr.log'
    $apiProcess = $null
    $webProcess = $null
    $commandShell = if ($env:ComSpec) { $env:ComSpec } else { 'cmd.exe' }

    try {
        $apiProcess = Start-Process -FilePath 'node.exe' -ArgumentList ('"' + $MockApi + '"') -WorkingDirectory $App -WindowStyle Hidden -RedirectStandardOutput $apiStdout -RedirectStandardError $apiStderr -PassThru

        $apiDeadline = (Get-Date).AddSeconds(30)
        $apiReady = $false
        while ((Get-Date) -lt $apiDeadline) {
            if ($apiProcess.HasExited) {
                Write-Host '=== LOCAL MOCK API STDOUT ==='
                if (Test-Path $apiStdout) { Get-Content $apiStdout -Tail 40 }
                Write-Host '=== LOCAL MOCK API STDERR ==='
                if (Test-Path $apiStderr) { Get-Content $apiStderr -Tail 40 }
                throw 'H06_3_MOCK_API_EXITED_EARLY'
            }
            try {
                $apiResponse = Invoke-WebRequest -Uri 'http://127.0.0.1:5090/health' -UseBasicParsing -TimeoutSec 2
                if ($apiResponse.StatusCode -eq 200) {
                    $apiReady = $true
                    break
                }
            }
            catch {
                # The local in-memory fixture API is still starting.
            }
            Start-Sleep -Seconds 1
        }
        if (-not $apiReady) { throw 'H06_3_MOCK_API_START_TIMEOUT' }

        $webProcess = Start-Process -FilePath $commandShell -ArgumentList @('/d', '/s', '/c', 'npm.cmd start -- --port 4300') -WorkingDirectory $App -WindowStyle Hidden -RedirectStandardOutput $webStdout -RedirectStandardError $webStderr -PassThru

        $webDeadline = (Get-Date).AddSeconds(90)
        $webReady = $false
        while ((Get-Date) -lt $webDeadline) {
            if ($webProcess.HasExited) {
                Write-Host '=== ANGULAR PREVIEW STDOUT ==='
                if (Test-Path $webStdout) { Get-Content $webStdout -Tail 40 }
                Write-Host '=== ANGULAR PREVIEW STDERR ==='
                if (Test-Path $webStderr) { Get-Content $webStderr -Tail 40 }
                throw 'H06_3_PREVIEW_SERVER_EXITED_EARLY'
            }
            try {
                $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3
                if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                    $webReady = $true
                    break
                }
            }
            catch {
                # Angular compilation is still starting; retry until deadline.
            }
            Start-Sleep -Seconds 1
        }
        if (-not $webReady) { throw 'H06_3_PREVIEW_SERVER_START_TIMEOUT' }

        [Environment]::SetEnvironmentVariable('H06_3_REUSE_SERVER', '1', 'Process')
        Write-Host ''
        Write-Host ('PREVIEW_READY=' + $url)
        Write-Host 'LOCAL_API=127.0.0.1:5090 (in-memory demo data; no database, SMTP or external services)'
        Write-Host ('ADMIN_DEMO_LOGIN=' + 'qa@example.invalid')
        Write-Host ('ADMIN_DEMO_PASSWORD=' + 'DisposableTestPassword123!')
        Write-Host 'Отвори се браузърът. Можеш да разгледаш началната страница, търсенето, обектите и Preview Admin.'
        Write-Host 'Когато си готов, върни се в този PowerShell прозорец и натисни Enter.'
        Start-Process $url
        [void](Read-Host 'ENTER = стартирай headed Playwright acceptance')

        return [pscustomobject]@{ Api = $apiProcess; Web = $webProcess }
    }
    catch {
        Stop-PreviewProcess -Process $webProcess
        Stop-PreviewProcess -Process $apiProcess
        throw
    }
}

function Stop-InteractivePreview {
    param([object]$ProcessSet)
    if ($null -ne $ProcessSet) {
        Stop-PreviewProcess -Process $ProcessSet.Web
        Stop-PreviewProcess -Process $ProcessSet.Api
        Write-Host 'INTERACTIVE_PREVIEW_SERVERS=STOPPED'
    }
}

Push-Location $App
try {
    # Write explicit records; avoid comma/concatenation array coercion on Windows PowerShell.
    $versions = New-Object 'System.Collections.Generic.List[string]'
    [void]$versions.Add('COMMIT_SHA=' + $CommitSha)
    [void]$versions.Add('BRANCH=' + $ExpectedBranch)
    [void]$versions.Add('NODE=' + (& node --version))
    [void]$versions.Add('NPM=' + (& npm.cmd --version))
    [void]$versions.Add('INTERACTIVE_BROWSER=' + [string]$InteractiveBrowser)
    [void]$versions.Add('HEADED_PLAYWRIGHT=' + [string]$Headed)
    $versions.ToArray() | Set-Content -LiteralPath (Join-Path $Evidence 'runtime.txt') -Encoding UTF8

    Invoke-NativeGate 'local-mock-api-syntax' 'node.exe' @('--check', $MockApi)
    $gates.LOCAL_MOCK_API_SYNTAX = 'PASS'

    $playwrightConfig = Get-Content -LiteralPath (Join-Path $App 'playwright.config.ts') -Raw
    $runnerSource = Get-Content -LiteralPath $PSCommandPath -Raw
    if ($playwrightConfig -notmatch "process\.env\['H06_3_REUSE_SERVER'\]\s*===\s*'1'" -or
        $playwrightConfig -match 'H06_2_REUSE_SERVER' -or
        $runnerSource -notmatch "SetEnvironmentVariable\('H06_3_REUSE_SERVER',\s*'1',\s*'Process'\)") {
        throw 'H06_3_PLAYWRIGHT_REUSE_SERVER_CONTRACT_MISMATCH'
    }
    $gates.PLAYWRIGHT_REUSE_CONFIG = 'PASS'
    Write-Host 'PLAYWRIGHT_REUSE_CONFIG=PASS'

    Invoke-NativeGate 'npm-ci' 'npm.cmd' @('ci', '--no-audit', '--no-fund')
    $gates.NPM_CI = 'PASS'

    if ($InstallChromium) {
        Invoke-NativeGate 'chromium-install' 'npx.cmd' @('playwright', 'install', 'chromium')
    }

    Invoke-NativeGate 'typecheck' 'npm.cmd' @('run', 'typecheck')
    $gates.TYPECHECK = 'PASS'

    Invoke-NativeGate 'production-build' 'npm.cmd' @('run', 'build')
    $gates.PRODUCTION_BUILD = 'PASS'

    if ($InteractiveBrowser) {
        $previewProcess = Start-InteractivePreview
        Invoke-LocalMockApiSmoke
        $gates.LOCAL_MOCK_API_SMOKE = 'PASS'
    }

    $targetedArgs = @(
        'playwright', 'test',
        'e2e/homepage-highlights.spec.ts',
        'e2e/discovery-ux.spec.ts',
        'e2e/accessibility.spec.ts',
        'e2e/admin-cms.spec.ts',
        'e2e/directory.spec.ts',
        'e2e/public-journey.spec.ts',
        'e2e/detail.spec.ts',
        'e2e/detail-navigation.spec.ts',
        'e2e/product-responsive.spec.ts',
        'e2e/responsive-preview.spec.ts',
        '--reporter=line'
    )
    if ($Headed) { $targetedArgs += '--headed' }

    try {
        Invoke-NativeGate 'targeted-playwright' 'npx.cmd' $targetedArgs
        $gates.TARGETED_PLAYWRIGHT = 'PASS'
    }
    finally { Save-BrowserEvidence 'targeted' }

    $fullArgs = @('playwright', 'test', '--reporter=line')
    if ($Headed) { $fullArgs += '--headed' }

    try {
        Invoke-NativeGate 'full-playwright' 'npx.cmd' $fullArgs
        $gates.FULL_PLAYWRIGHT = 'PASS'
    }
    finally { Save-BrowserEvidence 'full' }
}
catch {
    $failure = $_.Exception.Message
    Write-Host ('H06_3_QA_FAILED=' + $failure)
}
finally {
    Stop-InteractivePreview -ProcessSet $previewProcess
    [Environment]::SetEnvironmentVariable('H06_3_REUSE_SERVER', $previousReuseServer, 'Process')
    Pop-Location

    if ((Get-GitValue -Arguments @('rev-parse', 'HEAD')) -cne $CommitSha) {
        $gates.SOURCE_CLEAN_POST = 'FAIL_SHA_CHANGED'
    }
    else {
        $postflightStatus = @(& $GitExe -C $Work status --porcelain --untracked-files=all)
        if ($LASTEXITCODE -eq 0 -and $postflightStatus.Count -eq 0) {
            $gates.SOURCE_CLEAN_POST = 'PASS'
        }
        else {
            $gates.SOURCE_CLEAN_POST = 'FAIL_DIRTY'
            $postflightStatus | Set-Content -LiteralPath (Join-Path $Evidence 'git-status.txt') -Encoding UTF8
        }
    }

    # Use explicit string records: the previous comma/concatenation expression
    # joined COMMIT_SHA and BRANCH into one line under Windows PowerShell.
    $summary = New-Object 'System.Collections.Generic.List[string]'
    [void]$summary.Add('COMMIT_SHA=' + $CommitSha)
    [void]$summary.Add('BRANCH=' + $ExpectedBranch)
    foreach ($key in $gates.Keys) {
        [void]$summary.Add($key + '=' + $gates[$key])
    }
    [void]$summary.Add('FAILURE=' + $failure)
    $summaryLines = $summary.ToArray()
    $summaryLines | Set-Content -LiteralPath (Join-Path $Evidence 'run-summary.txt') -Encoding UTF8

    $files = @(Get-ChildItem -LiteralPath $Evidence -Recurse -File |
        Where-Object { $_.Name -notin @('manifest.txt', 'manifest.sha256') } |
        Sort-Object FullName)
    $manifestLines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($row in $summaryLines) {
        [void]$manifestLines.Add($row)
    }
    foreach ($file in $files) {
        $rel = $file.FullName.Substring($Evidence.Length).TrimStart('\')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        [void]$manifestLines.Add('FILE_SHA256=' + $rel + '|' + $hash)
    }
    $manifest = Join-Path $Evidence 'manifest.txt'
    $manifestLines.ToArray() | Set-Content -LiteralPath $manifest -Encoding UTF8
    $manifestHash = (Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash.ToLowerInvariant()
    $manifestHash | Set-Content -LiteralPath (Join-Path $Evidence 'manifest.sha256') -Encoding Ascii
    Write-Host ('EVIDENCE_DIR=' + $Evidence)
    Write-Host ('MANIFEST_SHA256=' + $manifestHash)
    foreach ($key in $gates.Keys) { Write-Host ($key + '=' + $gates[$key]) }
}

if ($failure) { throw ('H06_3_ACCEPTANCE_HOLD=' + $failure) }
if ($gates.SOURCE_CLEAN_POST -ne 'PASS') { throw 'H06_3_SOURCE_CLEAN_POST_FAILED' }
if (@($gates.Values | Where-Object { $_ -ne 'PASS' }).Count -ne 0) {
    throw 'H06_3_INCOMPLETE_GATES'
}
Write-Host 'H06_3_QA=PASS'
