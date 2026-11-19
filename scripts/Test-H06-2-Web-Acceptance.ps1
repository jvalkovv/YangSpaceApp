[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-f]{40}$')]
    [string]$CommitSha,

    [string]$Work = 'D:\GitHub\YangSpaceApp-F2b-H06.2-20261118',
    [string]$EvidenceRoot = 'D:\GitHub\YangSpaceApp-QA-Evidence\H06.2',

    [switch]$InstallChromium,
    [switch]$InteractiveBrowser,
    [switch]$Headed
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ExpectedBranch = 'feat/yangspace-f2b-h06-2-public-product-ux-20261118'
$Work = (Resolve-Path -LiteralPath $Work).Path
$App = Join-Path $Work 'apps\web'
$MockApi = Join-Path $App 'qa\mock-api.mjs'
if (-not (Test-Path -LiteralPath (Join-Path $App 'package-lock.json') -PathType Leaf)) {
    throw 'H06_2_WEB_APP_MISSING'
}
if (-not (Test-Path -LiteralPath $MockApi -PathType Leaf)) {
    throw 'H06_2_LOCAL_MOCK_API_MISSING'
}

function Get-GitValue([string[]]$Arguments) {
    $value = & git -C $Work @Arguments
    if ($LASTEXITCODE -ne 0) { throw ('H06_2_GIT_FAILED=' + ($Arguments -join ' ')) }
    return ($value | Out-String).Trim()
}

if ((Get-GitValue -Arguments @('branch', '--show-current')) -cne $ExpectedBranch) {
    throw 'H06_2_WRONG_BRANCH'
}
if ((Get-GitValue -Arguments @('rev-parse', 'HEAD')) -cne $CommitSha) {
    throw 'H06_2_EXACT_SHA_MISMATCH'
}
$preflightStatus = @(& git -C $Work status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $preflightStatus.Count -gt 0) {
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
    LOCAL_MOCK_API_SYNTAX = 'NOT_RUN'
    NPM_CI = 'NOT_RUN'
    TYPECHECK = 'NOT_RUN'
    PRODUCTION_BUILD = 'NOT_RUN'
    TARGETED_PLAYWRIGHT = 'NOT_RUN'
    FULL_PLAYWRIGHT = 'NOT_RUN'
    SOURCE_CLEAN_POST = 'NOT_RUN'
}
$failure = $null
$previewProcess = $null
$previousReuseServer = [Environment]::GetEnvironmentVariable('H06_2_REUSE_SERVER', 'Process')

function Invoke-NativeGate {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Binary,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )
    $log = Join-Path $Evidence ($Name + '.log')
    # Windows PowerShell 5.1 may wrap native stderr warnings as ErrorRecords.
    # Preserve stderr and decide from the native process exit code.
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

function Start-InteractivePreview {
    $url = 'http://127.0.0.1:4300/'
    if (Test-LocalTcpPort 5090) { throw 'H06_2_MOCK_API_PORT_5090_ALREADY_IN_USE' }
    if (Test-LocalTcpPort 4300) { throw 'H06_2_PREVIEW_PORT_4300_ALREADY_IN_USE' }

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
                throw 'H06_2_MOCK_API_EXITED_EARLY'
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
        if (-not $apiReady) { throw 'H06_2_MOCK_API_START_TIMEOUT' }

        $webProcess = Start-Process -FilePath $commandShell -ArgumentList @('/d', '/s', '/c', 'npm.cmd start -- --port 4300') -WorkingDirectory $App -WindowStyle Hidden -RedirectStandardOutput $webStdout -RedirectStandardError $webStderr -PassThru

        $webDeadline = (Get-Date).AddSeconds(90)
        $webReady = $false
        while ((Get-Date) -lt $webDeadline) {
            if ($webProcess.HasExited) {
                Write-Host '=== ANGULAR PREVIEW STDOUT ==='
                if (Test-Path $webStdout) { Get-Content $webStdout -Tail 40 }
                Write-Host '=== ANGULAR PREVIEW STDERR ==='
                if (Test-Path $webStderr) { Get-Content $webStderr -Tail 40 }
                throw 'H06_2_PREVIEW_SERVER_EXITED_EARLY'
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
        if (-not $webReady) { throw 'H06_2_PREVIEW_SERVER_START_TIMEOUT' }

        [Environment]::SetEnvironmentVariable('H06_2_REUSE_SERVER', '1', 'Process')
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
    $versions = @(
        'COMMIT_SHA=' + $CommitSha,
        'BRANCH=' + $ExpectedBranch,
        'NODE=' + (& node --version),
        'NPM=' + (& npm.cmd --version),
        'INTERACTIVE_BROWSER=' + [string]$InteractiveBrowser,
        'HEADED_PLAYWRIGHT=' + [string]$Headed
    )
    $versions | Set-Content -LiteralPath (Join-Path $Evidence 'runtime.txt') -Encoding UTF8

    Invoke-NativeGate 'local-mock-api-syntax' 'node.exe' @('--check', $MockApi)
    $gates.LOCAL_MOCK_API_SYNTAX = 'PASS'

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
    }

    $targetedArgs = @(
        'playwright', 'test',
        'e2e/homepage-highlights.spec.ts',
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
    Write-Host ('H06_2_QA_FAILED=' + $failure)
}
finally {
    Stop-InteractivePreview -Process $previewProcess
    [Environment]::SetEnvironmentVariable('H06_2_REUSE_SERVER', $previousReuseServer, 'Process')
    Pop-Location

    if ((Get-GitValue -Arguments @('rev-parse', 'HEAD')) -cne $CommitSha) {
        $gates.SOURCE_CLEAN_POST = 'FAIL_SHA_CHANGED'
    }
    else {
        $postflightStatus = @(& git -C $Work status --porcelain --untracked-files=all)
        if ($LASTEXITCODE -eq 0 -and $postflightStatus.Count -eq 0) {
            $gates.SOURCE_CLEAN_POST = 'PASS'
        }
        else {
            $gates.SOURCE_CLEAN_POST = 'FAIL_DIRTY'
            $postflightStatus | Set-Content -LiteralPath (Join-Path $Evidence 'git-status.txt') -Encoding UTF8
        }
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
