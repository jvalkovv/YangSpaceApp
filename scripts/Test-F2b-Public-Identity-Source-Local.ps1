# Exact-SHA Windows PowerShell 5.1 source-only F2b gate.
# No production HTTP, PostgreSQL, SMTP, Docker, CT or migrations.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidatePattern('^[a-f0-9]{40}$')][string]$CommitSha)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch = 'feat/yangspace-f2b-public-identity-foundation-20261113'
$Git = (Get-Command git.exe -ErrorAction Stop).Source
$Dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$Npm = (Get-Command npm.cmd -ErrorAction Stop).Source
$Web = Join-Path $Repo 'apps\web'

function GitValue([string[]]$ArgsList) {
    $result = @(& $Git -C $Repo @ArgsList)
    if ($LASTEXITCODE -ne 0) { throw 'F2B_GIT_FAILED' }
    return ($result -join '').Trim()
}
if ((GitValue @('rev-parse','HEAD')) -cne $CommitSha) { throw 'F2B_HEAD_MISMATCH' }
if ((GitValue @('rev-parse','--verify',('refs/remotes/origin/' + $Branch))) -cne $CommitSha) {
    throw 'F2B_REMOTE_SHA_MISMATCH'
}
$CheckedBranch = GitValue @('branch','--show-current')
if ($CheckedBranch -and $CheckedBranch -cne $Branch) { throw 'F2B_UNEXPECTED_BRANCH' }
$Dirty = @(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
if ($LASTEXITCODE -ne 0 -or $Dirty.Count -ne 0) { throw 'F2B_DIRTY_SOURCE' }
Write-Host 'F2B_EXACT_SOURCE=PASS'

$Config = Get-Content -Raw (Join-Path $Repo 'apps\api\appsettings.json') | ConvertFrom-Json
$Policy = [IO.File]::ReadAllText((Join-Path $Repo 'apps\api\PublicAccountEndpoints.cs'))
$Program = [IO.File]::ReadAllText((Join-Path $Repo 'apps\api\Program.cs'))
$Routes = [IO.File]::ReadAllText((Join-Path $Web 'src\app\app.routes.ts'))
if ($Config.PublicAccounts.Enabled -ne $false) { throw 'F2B_PUBLIC_DEFAULT_ON' }
if ($Config.PublicAccounts.Email.Password -ne '' -or $Config.PublicAccounts.Email.SmtpHost -ne '') {
    throw 'F2B_PUBLIC_SMTP_DEFAULTS_UNSAFE'
}
foreach ($Required in @('PUBLIC_ACCOUNT_SCOPE_REFUSED','PUBLIC_ACCOUNT_POSTGRESQL_REQUIRED',
    'PUBLIC_ACCOUNT_CANONICAL_ORIGIN_REFUSED','PUBLIC_ACCOUNT_HOST_POLICY_REFUSED',
    'PublicAccountEmailSettings.RequireConfigured','RequireRateLimiting("auth")',
    'IdentityConstants.BearerScheme','manager.GetRolesAsync(user)',
    'IPasswordValidator<IdentityUser>','EmailConfirmed = false')) {
    if (!$Policy.Contains($Required)) { throw ('F2B_STATIC_POLICY_MISSING=' + $Required) }
}
foreach ($Required in @('options.SignIn.RequireConfirmedEmail = publicAccounts',
    'if (publicAccounts)','app.MapPublicAccounts()')) {
    if (!$Program.Contains($Required)) { throw ('F2B_STATIC_PROGRAM_MISSING=' + $Required) }
}
foreach ($Route in @('login','register','recover','reset','resend-confirmation','account')) {
    if (!$Routes.Contains(("path: '" + $Route + "'"))) { throw ('F2B_STATIC_ROUTE_MISSING=' + $Route) }
}
Write-Host 'F2B_STATIC_SECURITY_INVARIANTS=PASS'

$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + ([guid]::NewGuid().ToString('N').Substring(0,8))
$Evidence = Join-Path $Repo ('logs\yangspace\f2b-public-identity\' + $CommitSha.Substring(0,12) + '-' + $RunId)
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
$Passed = $false
try {
    Invoke-YangSpaceStage -Label 'f2b-api-restore' -Executable $Dotnet -CommandArguments @('restore','apps/api/YangSpace.Api.csproj','-warnaserror') -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 1200
    Invoke-YangSpaceStage -Label 'f2b-api-build' -Executable $Dotnet -CommandArguments @('build','apps/api/YangSpace.Api.csproj','-c','Release','--no-restore','-warnaserror') -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds 1200
    Invoke-YangSpaceStage -Label 'f2b-web-npm-ci' -Executable $Npm -CommandArguments @('ci','--ignore-scripts','--no-audit','--no-fund') -WorkingDirectory $Web -EvidenceDirectory $Evidence -TimeoutSeconds 1200
    Invoke-YangSpaceStage -Label 'f2b-web-typecheck' -Executable $Npm -CommandArguments @('run','typecheck') -WorkingDirectory $Web -EvidenceDirectory $Evidence -TimeoutSeconds 1200
    Invoke-YangSpaceStage -Label 'f2b-web-build' -Executable $Npm -CommandArguments @('run','build') -WorkingDirectory $Web -EvidenceDirectory $Evidence -TimeoutSeconds 1200
    foreach ($Label in @('f2b-api-restore','f2b-api-build','f2b-web-npm-ci','f2b-web-typecheck','f2b-web-build')) {
        foreach ($Suffix in @('stdout.log','stderr.log','runner.cmd')) {
            if (!(Test-Path (Join-Path $Evidence ($Label + '.' + $Suffix)))) {
                throw ('F2B_STAGE_LOG_MISSING=' + $Label + '.' + $Suffix)
            }
        }
    }
    $Manifest = @(
        "COMMIT_SHA=$CommitSha",
        'F2B_API_RELEASE=PASS',
        'F2B_ANGULAR_TYPECHECK=PASS',
        'F2B_ANGULAR_BUILD=PASS',
        'F2B_STATIC_SECURITY_INVARIANTS=PASS',
        'PUBLIC_REGISTRATION_DEFAULT_OFF=PASS',
        'PUBLIC_IDENTITY_HTTP=NOT_TESTED',
        'PUBLIC_SMTP_DELIVERY=NOT_TESTED',
        'OWNER_GRANT=NOT_IMPLEMENTED_NOT_AUTHORIZED',
        'CT164_DB=NOT_ACCESSED',
        'RELEASE_GATE=HOLD'
    )
    [IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'), [string[]]$Manifest)
    $Passed = $true
    Write-Host 'F2B_PUBLIC_IDENTITY_SOURCE_ONLY=PASS'
    Write-Host "COMMIT_SHA=$CommitSha"
    Write-Host "EVIDENCE_DIR=$Evidence"
    Write-Host ('MANIFEST_SHA256=' + (Get-FileHash (Join-Path $Evidence 'manifest.txt') -Algorithm SHA256).Hash)
}
finally {
    if (!$Passed) {
        [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'), "F2B_SOURCE_GATE=HOLD; COMMIT_SHA=$CommitSha")
    }
}
