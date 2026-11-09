# Native Windows PowerShell integration contract for a disposable YangSpace API.
# This script is launched by Verify-Api-Local.ps1 and MUST NEVER target a live DB.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidateSet('prepare','verify')][string]$Phase)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

function Require-DisposableTestEnvironment {
    $keys = @(
        'YANGSPACE_TEST_SANDBOX','YANGSPACE_TEST_DB_PATH','YANGSPACE_TEST_STATE',
        'YANGSPACE_TEST_BASE_URL','ConnectionStrings__Main',
        'Bootstrap__AdminEmail','Bootstrap__AdminPassword'
    )
    foreach ($key in $keys) {
        if ([string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($key,'Process'))) {
            throw ('REFUSED: missing isolated environment value ' + $key)
        }
    }
    if ($env:YANGSPACE_TEST_ISOLATED_DB -cne 'YES' -or
        $env:ASPNETCORE_ENVIRONMENT -cne 'Staging' -or
        $env:Preview__Enabled -cne 'true') {
        throw 'REFUSED: expected Staging disposable preview'
    }
    $sandbox = [IO.Path]::GetFullPath($env:YANGSPACE_TEST_SANDBOX)
    $temp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([char[]]@('\','/'))
    $folder = [IO.Path]::GetFileName($sandbox.TrimEnd([char[]]@('\','/')))
    if ($folder -cnotmatch '^yangspace-ci-[a-f0-9]{32}$' -or
        -not [string]::Equals([IO.Path]::GetDirectoryName($sandbox),$temp,[StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $sandbox -PathType Container)) {
        throw 'REFUSED: sandbox outside private OS TEMP folder'
    }
    $sandboxItem = Get-Item -LiteralPath $sandbox
    if (($sandboxItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'REFUSED: sandbox is a reparse point'
    }
    $marker = Join-Path $sandbox '.yangspace-disposable-test'
    if (-not (Test-Path -LiteralPath $marker -PathType Leaf) -or
        [IO.File]::ReadAllText($marker) -cne ('YES' + [char]10)) {
        throw 'REFUSED: disposable DB safety marker missing'
    }
    $db = Join-Path $sandbox 'preview.db'
    $state = Join-Path $sandbox 'state.json'
    if (-not [string]::Equals($env:YANGSPACE_TEST_DB_PATH,$db,[StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals($env:YANGSPACE_TEST_STATE,$state,[StringComparison]::OrdinalIgnoreCase) -or
        $env:ConnectionStrings__Main -cne ('Data Source=' + $db)) {
        throw 'REFUSED: database/state connection mismatch'
    }
    $uri = $null
    if (-not [Uri]::TryCreate($env:YANGSPACE_TEST_BASE_URL,[UriKind]::Absolute,[ref]$uri) -or
        $uri.Scheme -cne 'http' -or
        $uri.Host -cne '127.0.0.1' -or
        $uri.Port -le 1024 -or
        $uri.Port -gt 65535 -or
        $uri.AbsolutePath -cne '/' -or
        $uri.Query -or $uri.Fragment -or $uri.UserInfo -or
        $env:ASPNETCORE_URLS -cne $env:YANGSPACE_TEST_BASE_URL) {
        throw 'REFUSED: endpoint must be matching unprivileged 127.0.0.1 only'
    }
    return $state
}

function Invoke-TestRequest {
    param(
        [Parameter(Mandatory=$true)][ValidateSet('GET','POST','PUT')][string]$Method,
        [Parameter(Mandatory=$true)][string]$Path,
        [object]$Payload=$null,
        [string]$Token=''
    )
    if ($Path -notmatch '^/api/' -or $Path -match '[\r\n]') { throw 'REFUSED: unexpected HTTP path' }
    $uri = $env:YANGSPACE_TEST_BASE_URL.TrimEnd('/') + $Path
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.UseProxy = $false
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(12)
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method),[Uri]$uri)
    try {
        $request.Headers.Accept.ParseAdd('application/json')
        if (-not [string]::IsNullOrEmpty($Token)) {
            $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$Token)
        }
        if ($null -ne $Payload) {
            $json = ConvertTo-Json -InputObject $Payload -Depth 12 -Compress
            $request.Content = [System.Net.Http.StringContent]::new($json,[Text.Encoding]::UTF8,'application/json')
        }
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            $body = ''
            $data = $null
            if ($null -ne $response.Content) {
                $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            }
            if (-not [string]::IsNullOrWhiteSpace($body) -and $null -ne $response.Content -and
                $response.Content.Headers.ContentType -and $response.Content.Headers.ContentType.MediaType -match 'json') {
                $data = ConvertFrom-Json -InputObject $body
            }
            return [pscustomobject]@{Status=[int]$response.StatusCode; Data=$data}
        } finally { $response.Dispose() }
    } finally {
        $request.Dispose()
        $client.Dispose()
        $handler.Dispose()
    }
}
function Assert-Status {
    param([string]$Method,[string]$Path,[int[]]$Expected,[object]$Payload=$null,[string]$Token='')
    $response = Invoke-TestRequest -Method $Method -Path $Path -Payload $Payload -Token $Token
    if ($Expected -notcontains $response.Status) {
        throw ('HTTP_CONTRACT_FAIL ' + $Method + ' ' + $Path + ' expected=' +
            ($Expected -join ',') + ' actual=' + $response.Status)
    }
    return $response.Data
}
function Utc-Date([int]$Days) {
    return [DateTime]::UtcNow.AddDays($Days).ToString("yyyy-MM-ddTHH:mm:ss.fffZ",[Globalization.CultureInfo]::InvariantCulture)
}
function Admin-Token {
    $r=Assert-Status 'POST' '/api/auth/login' @(200) @{
        email=$env:Bootstrap__AdminEmail;password=$env:Bootstrap__AdminPassword
    }
    if (-not $r -or [string]::IsNullOrWhiteSpace($r.accessToken) -or $r.accessToken.Length -lt 30) {
        throw 'Admin bearer token missing'
    }
    return [string]$r.accessToken
}
function Assert-Hidden([string]$Slug) {
    [void](Assert-Status 'GET' ('/api/v1/locations/' + $Slug) @(404))
}
$state = Require-DisposableTestEnvironment
if ($Phase -eq 'prepare') {
    $slug='ci-integr-'+[Guid]::NewGuid().ToString('N').Substring(0,16)
    $location=@{
        slug=$slug;name='CI test location';neighborhood='CI-Kvartal';category='Services'
        address='Fictional CI address';description='Integration test, no real data'
        phone=$null;website=$null;imageUrl=$null;openingHours='09:00-18:00';isPublished=$false
    }
    [void](Assert-Status 'GET' '/api/v1/admin/locations' @(401,403))
    [void](Assert-Status 'POST' '/api/v1/admin/locations' @(401,403) $location)
    Assert-Hidden $slug
    $token=Admin-Token
    [void](Assert-Status 'GET' '/api/v1/admin/me' @(200) $null $token)
    $readerEmail='ci-reader-'+[Guid]::NewGuid().ToString('N').Substring(0,12)+'@example.invalid'
    [void](Assert-Status 'POST' '/api/auth/register' @(200) @{email=$readerEmail;password=$env:Bootstrap__AdminPassword})
    $member=Assert-Status 'POST' '/api/auth/login' @(200) @{email=$readerEmail;password=$env:Bootstrap__AdminPassword}
    if (-not $member -or [string]::IsNullOrWhiteSpace($member.accessToken)) { throw 'Nonadmin token missing' }
    [void](Assert-Status 'GET' '/api/v1/admin/locations' @(403) $null ([string]$member.accessToken))
    [void](Assert-Status 'POST' '/api/v1/admin/locations' @(403) $location ([string]$member.accessToken))
    $created=Assert-Status 'POST' '/api/v1/admin/locations' @(201) $location $token
    $id=[string]$created.id
    $parsedGuid=[Guid]::Empty
    if (-not [Guid]::TryParse($id,[ref]$parsedGuid)) { throw 'Created location ID missing' }
    Assert-Hidden $slug
    [void](Assert-Status 'POST' '/api/v1/admin/locations' @(409) $location $token)
    $invalid=$location.Clone();$invalid.slug=$slug+'-invalid';$invalid.name=''
    [void](Assert-Status 'POST' '/api/v1/admin/locations' @(400) $invalid $token)
    $location.name='CI test location published';$location.isPublished=$true
    [void](Assert-Status 'PUT' ('/api/v1/admin/locations/'+$id) @(200) $location $token)
    $visible=Assert-Status 'GET' ('/api/v1/locations/'+$slug) @(200)
    if ($visible.name -cne $location.name) { throw 'Published location mismatch' }
    $offers='/api/v1/admin/locations/'+$id+'/offers'
    [void](Assert-Status 'POST' $offers @(201) @{title='Active CI offer';description='Test only';startsAtUtc=(Utc-Date -1);endsAtUtc=(Utc-Date 7);isPublished=$true} $token)
    [void](Assert-Status 'POST' $offers @(201) @{title='Expired CI offer';description='Test only';startsAtUtc=(Utc-Date -10);endsAtUtc=(Utc-Date -1);isPublished=$true} $token)
    [void](Assert-Status 'POST' $offers @(201) @{title='Future CI offer';description='Test only';startsAtUtc=(Utc-Date 1);endsAtUtc=(Utc-Date 9);isPublished=$true} $token)
    [void](Assert-Status 'POST' ('/api/v1/admin/locations/'+$id+'/brochures') @(201) @{
        title='CI brochure';url='https://example.org/ci-catalogue.pdf'
        validFromUtc=(Utc-Date -1);validToUtc=(Utc-Date 4);isPublished=$true
    } $token)
    [void](Assert-Status 'POST' ('/api/v1/admin/locations/'+$id+'/jobs') @(201) @{
        title='CI temporary job';description='Test only';applyUrl='https://example.org/ci-job'
        expiresAtUtc=(Utc-Date 7);isPublished=$true
    } $token)
    $detail=Assert-Status 'GET' ('/api/v1/locations/'+$slug) @(200)
    $titles=@($detail.offers | ForEach-Object {$_.title})
    if ($titles.Count -ne 1 -or $titles[0] -cne 'Active CI offer' -or
        @($detail.brochures).Count -ne 1 -or @($detail.jobs).Count -ne 1) {
        throw ('Validity filtering failed; active offer count=' + $titles.Count)
    }
    [void](Assert-Status 'POST' ('/api/v1/admin/locations/'+$id+'/hide') @(204) @{} $token)
    Assert-Hidden $slug
    $listing=Assert-Status 'GET' '/api/v1/locations?category=Services' @(200)
    if (@($listing.items | Where-Object {$_.slug -ceq $slug}).Count -gt 0) { throw 'Hidden business leaked in listing' }
    [IO.File]::WriteAllText($state,(ConvertTo-Json @{id=$id;slug=$slug} -Compress))
    Write-Host 'INTEGRATION_PREPARE=PASS'
} else {
    if (-not (Test-Path -LiteralPath $state -PathType Leaf)) { throw 'Prepared-state fixture missing' }
    $record=ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($state))
    $token=Admin-Token
    $locations=Assert-Status 'GET' '/api/v1/admin/locations' @(200) $null $token
    $entry=@($locations | Where-Object {$_.id -eq $record.id -and $_.isPublished -eq $false})
    if ($entry.Count -ne 1) { throw 'Persisted hidden location not found after restart' }
    Assert-Hidden ([string]$record.slug)
    [void](Assert-Status 'GET' '/api/v1/admin/locations' @(401,403))
    Write-Host 'INTEGRATION_RESTART_PERSISTENCE=PASS'
}
