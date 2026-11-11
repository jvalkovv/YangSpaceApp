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
function Assert-NewPublicationFreshness([object]$Item,[string]$Kind) {
    foreach ($field in @('createdAtUtc','updatedAtUtc','publishedAtUtc')) {
        $value = [string]$Item.$field
        if ([string]::IsNullOrWhiteSpace($value)) {
            throw ('PUBLICATION_TIMESTAMP_MISSING ' + $Kind + ':' + $field)
        }
        $parsed = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParse($value,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind, [ref]$parsed)) {
            throw ('PUBLICATION_TIMESTAMP_INVALID ' + $Kind + ':' + $field)
        }
    }
    if ([DateTimeOffset]$Item.updatedAtUtc -lt [DateTimeOffset]$Item.createdAtUtc) {
        throw ('PUBLICATION_TIMESTAMP_ORDER_INVALID ' + $Kind)
    }
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
    # S1-03 content lifecycle: all records remain visible to the administrator,
    # including expired/hidden records, but only valid published records go public.
    $base='/api/v1/admin/locations/'+$id
    $content=Assert-Status 'GET' ($base+'/content') @(200) $null $token
    [void](Assert-Status 'GET' ($base+'/content') @(403) $null ([string]$member.accessToken))
    [void](Assert-Status 'GET' ('/api/v1/admin/locations/'+[Guid]::NewGuid().ToString()+'/content') @(404) $null $token)
    $active=@($content.offers | Where-Object {$_.title -ceq 'Active CI offer'})
    $expired=@($content.offers | Where-Object {$_.title -ceq 'Expired CI offer'})
    $brochure=@($content.brochures | Where-Object {$_.title -ceq 'CI brochure'})
    $job=@($content.jobs | Where-Object {$_.title -ceq 'CI temporary job'})
    if ($active.Count -ne 1 -or $expired.Count -ne 1 -or $brochure.Count -ne 1 -or $job.Count -ne 1) {
        throw 'CMS_CONTENT_INVENTORY_MISMATCH'
    }
    Assert-NewPublicationFreshness $active[0] 'offer'
    Assert-NewPublicationFreshness $brochure[0] 'brochure'
    Assert-NewPublicationFreshness $job[0] 'job'
    $offerCreated = [string]$active[0].createdAtUtc
    $offerPublished = [string]$active[0].publishedAtUtc
    $offerOriginalUpdated = [string]$active[0].updatedAtUtc
    $brochureCreated = [string]$brochure[0].createdAtUtc
    $jobCreated = [string]$job[0].createdAtUtc

    $offerUrl=$base+'/offers/'+[string]$active[0].id
    $offerInput=@{title='Updated CI offer';description='Updated from CMS API';startsAtUtc=(Utc-Date -1);endsAtUtc=(Utc-Date 12);isPublished=$true}
    [void](Assert-Status 'PUT' $offerUrl @(403) $offerInput ([string]$member.accessToken))
    [void](Assert-Status 'PUT' ('/api/v1/admin/locations/'+[Guid]::NewGuid().ToString()+'/offers/'+[string]$active[0].id) @(404) $offerInput $token)
    [void](Assert-Status 'PUT' $offerUrl @(204) $offerInput $token)
    $changedOfferContent=Assert-Status 'GET' ($base+'/content') @(200) $null $token
    $changedOffer=@($changedOfferContent.offers | Where-Object {$_.title -ceq 'Updated CI offer'})
    if ($changedOffer.Count -ne 1 -or [string]$changedOffer[0].createdAtUtc -cne $offerCreated -or
        [string]$changedOffer[0].publishedAtUtc -cne $offerPublished -or
        [string]$changedOffer[0].updatedAtUtc -ceq $offerOriginalUpdated) {
        throw 'OFFER_EDIT_TIMESTAMP_CONTRACT_FAILED'
    }
    $offerEdited = [string]$changedOffer[0].updatedAtUtc
    [void](Assert-Status 'PUT' $offerUrl @(204) $offerInput $token)
    $unchangedOfferContent=Assert-Status 'GET' ($base+'/content') @(200) $null $token
    $unchangedOffer=@($unchangedOfferContent.offers | Where-Object {$_.title -ceq 'Updated CI offer'})
    if ($unchangedOffer.Count -ne 1 -or [string]$unchangedOffer[0].updatedAtUtc -cne $offerEdited) {
        throw 'OFFER_NOOP_PUT_CHANGED_TIMESTAMP'
    }
    $detail=Assert-Status 'GET' ('/api/v1/locations/'+$slug) @(200)
    if (@($detail.offers | Where-Object {$_.title -ceq 'Updated CI offer'}).Count -ne 1) { throw 'OFFER_UPDATE_NOT_PUBLIC' }
    [void](Assert-Status 'POST' ($offerUrl+'/hide') @(204) @{} $token)
    $offerHiddenContent=Assert-Status 'GET' ($base+'/content') @(200) $null $token
    $offerHidden=@($offerHiddenContent.offers | Where-Object {$_.title -ceq 'Updated CI offer'})
    if ($offerHidden.Count -ne 1 -or [string]$offerHidden[0].updatedAtUtc -ceq $offerEdited) {
        throw 'OFFER_HIDE_TIMESTAMP_NOT_ADVANCED'
    }
    $offerHiddenUpdated=[string]$offerHidden[0].updatedAtUtc
    [void](Assert-Status 'POST' ($offerUrl+'/hide') @(204) @{} $token)
    $offerHiddenAgainContent=Assert-Status 'GET' ($base+'/content') @(200) $null $token
    $offerHiddenAgain=@($offerHiddenAgainContent.offers | Where-Object {$_.title -ceq 'Updated CI offer'})
    if ($offerHiddenAgain.Count -ne 1 -or [string]$offerHiddenAgain[0].updatedAtUtc -cne $offerHiddenUpdated) {
        throw 'OFFER_IDEMPOTENT_HIDE_CHANGED_TIMESTAMP'
    }
    $detail=Assert-Status 'GET' ('/api/v1/locations/'+$slug) @(200)
    if (@($detail.offers | Where-Object {$_.title -ceq 'Updated CI offer'}).Count -ne 0) { throw 'HIDDEN_OFFER_LEAK' }
    [void](Assert-Status 'POST' ($offerUrl+'/restore') @(403) @{} ([string]$member.accessToken))
    [void](Assert-Status 'POST' ($offerUrl+'/restore') @(204) @{} $token)
    $offerRestoredContent=Assert-Status 'GET' ($base+'/content') @(200) $null $token
    $offerRestored=@($offerRestoredContent.offers | Where-Object {$_.title -ceq 'Updated CI offer'})
    if ($offerRestored.Count -ne 1 -or [string]$offerRestored[0].createdAtUtc -cne $offerCreated -or
        [string]$offerRestored[0].publishedAtUtc -ceq $offerPublished -or
        [string]$offerRestored[0].publishedAtUtc -cne [string]$offerRestored[0].updatedAtUtc) {
        throw 'OFFER_REPUBLISH_TIMESTAMP_CONTRACT_FAILED'
    }
    $offerRestoredUpdated=[string]$offerRestored[0].updatedAtUtc
    [void](Assert-Status 'POST' ($offerUrl+'/restore') @(204) @{} $token)
    $offerRestoredAgainContent=Assert-Status 'GET' ($base+'/content') @(200) $null $token
    $offerRestoredAgain=@($offerRestoredAgainContent.offers | Where-Object {$_.title -ceq 'Updated CI offer'})
    if ($offerRestoredAgain.Count -ne 1 -or [string]$offerRestoredAgain[0].updatedAtUtc -cne $offerRestoredUpdated) {
        throw 'OFFER_IDEMPOTENT_RESTORE_CHANGED_TIMESTAMP'
    }
    $detail=Assert-Status 'GET' ('/api/v1/locations/'+$slug) @(200)
    if (@($detail.offers | Where-Object {$_.title -ceq 'Updated CI offer'}).Count -ne 1) { throw 'RESTORED_OFFER_MISSING' }

    $expiredUrl=$base+'/offers/'+[string]$expired[0].id
    [void](Assert-Status 'POST' ($expiredUrl+'/hide') @(204) @{} $token)
    [void](Assert-Status 'POST' ($expiredUrl+'/restore') @(409) @{} $token)
    [void](Assert-Status 'PUT' $expiredUrl @(400) @{
        title='Expired CI offer';description='No';startsAtUtc=(Utc-Date 2);endsAtUtc=(Utc-Date 1);isPublished=$true
    } $token)

    $brochureUrl=$base+'/brochures/'+[string]$brochure[0].id
    $brochureInput=@{title='Updated CI brochure';url='https://example.org/updated.pdf';validFromUtc=(Utc-Date -1);validToUtc=(Utc-Date 10);isPublished=$true}
    [void](Assert-Status 'PUT' $brochureUrl @(400) @{
        title='Updated CI brochure';url='http://example.org/unsafe.pdf';validFromUtc=(Utc-Date -1);validToUtc=(Utc-Date 10);isPublished=$true
    } $token)
    [void](Assert-Status 'PUT' $brochureUrl @(204) $brochureInput $token)
    [void](Assert-Status 'POST' ($brochureUrl+'/hide') @(204) @{} $token)
    $detail=Assert-Status 'GET' ('/api/v1/locations/'+$slug) @(200)
    if (@($detail.brochures).Count -ne 0) { throw 'HIDDEN_BROCHURE_LEAK' }
    [void](Assert-Status 'POST' ($brochureUrl+'/restore') @(204) @{} $token)
    $brochureRestoredContent=Assert-Status 'GET' ($base+'/content') @(200) $null $token
    $brochureRestored=@($brochureRestoredContent.brochures | Where-Object {$_.title -ceq 'Updated CI brochure'})
    if ($brochureRestored.Count -ne 1 -or [string]$brochureRestored[0].createdAtUtc -cne $brochureCreated -or
        [string]$brochureRestored[0].publishedAtUtc -cne [string]$brochureRestored[0].updatedAtUtc) {
        throw 'BROCHURE_REPUBLISH_TIMESTAMP_CONTRACT_FAILED'
    }
    $detail=Assert-Status 'GET' ('/api/v1/locations/'+$slug) @(200)
    if (@($detail.brochures | Where-Object {$_.title -ceq 'Updated CI brochure'}).Count -ne 1) { throw 'BROCHURE_RESTORE_MISSING' }

    $jobUrl=$base+'/jobs/'+[string]$job[0].id
    $jobInput=@{title='Updated CI job';description='Updated';applyUrl='https://example.org/updated-job';expiresAtUtc=(Utc-Date 14);isPublished=$true}
    [void](Assert-Status 'PUT' $jobUrl @(204) $jobInput $token)
    [void](Assert-Status 'POST' ($jobUrl+'/hide') @(204) @{} $token)
    $detail=Assert-Status 'GET' ('/api/v1/locations/'+$slug) @(200)
    if (@($detail.jobs).Count -ne 0) { throw 'HIDDEN_JOB_LEAK' }
    [void](Assert-Status 'POST' ($jobUrl+'/restore') @(204) @{} $token)
    $jobRestoredContent=Assert-Status 'GET' ($base+'/content') @(200) $null $token
    $jobRestored=@($jobRestoredContent.jobs | Where-Object {$_.title -ceq 'Updated CI job'})
    if ($jobRestored.Count -ne 1 -or [string]$jobRestored[0].createdAtUtc -cne $jobCreated -or
        [string]$jobRestored[0].publishedAtUtc -cne [string]$jobRestored[0].updatedAtUtc) {
        throw 'JOB_REPUBLISH_TIMESTAMP_CONTRACT_FAILED'
    }
    Write-Host 'PUBLICATION_FRESHNESS_LIFECYCLE=PASS'
    $detail=Assert-Status 'GET' ('/api/v1/locations/'+$slug) @(200)
    if (@($detail.jobs | Where-Object {$_.title -ceq 'Updated CI job'}).Count -ne 1) { throw 'JOB_RESTORE_MISSING' }

    Write-Host 'CONTENT_LIFECYCLE_INTEGRATION=PASS'
    [void](Assert-Status 'POST' ('/api/v1/admin/locations/'+$id+'/hide') @(204) @{} $token)
    Assert-Hidden $slug
    # S1-02: restoring a hidden location must restore public visibility,
    # reject non-admins and unknown IDs, and remain safe on repeated calls.
    $restore='/api/v1/admin/locations/'+$id+'/restore'
    [void](Assert-Status 'POST' $restore @(403) @{} ([string]$member.accessToken))
    [void](Assert-Status 'POST' ('/api/v1/admin/locations/'+[Guid]::NewGuid().ToString()+'/restore') @(404) @{} $token)
    [void](Assert-Status 'POST' $restore @(204) @{} $token)
    [void](Assert-Status 'POST' $restore @(204) @{} $token)
    $restored=Assert-Status 'GET' ('/api/v1/locations/'+$slug) @(200)
    if ($restored.slug -cne $slug) { throw 'RESTORED_LOCATION_PUBLIC_READ_MISMATCH' }
    Write-Host 'LOCATION_RESTORE_API=PASS'
    [void](Assert-Status 'POST' ('/api/v1/admin/locations/'+$id+'/hide') @(204) @{} $token)
    Assert-Hidden $slug
    $listing=Assert-Status 'GET' '/api/v1/locations?category=Services' @(200)
    if (@($listing.items | Where-Object {$_.slug -ceq $slug}).Count -gt 0) { throw 'Hidden business leaked in listing' }
    [IO.File]::WriteAllText($state,(ConvertTo-Json @{id=$id;slug=$slug;readerEmail=$readerEmail} -Compress))
    Write-Host 'INTEGRATION_PREPARE=PASS'
} else {
    if (-not (Test-Path -LiteralPath $state -PathType Leaf)) { throw 'Prepared-state fixture missing' }
    $record=ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($state))
    if ($env:Preview__AllowRegistration -cne 'false') {
        throw 'SECOND_CYCLE_REGISTRATION_GATE_NOT_DISABLED'
    }
    $blockedEmail='blocked-'+[Guid]::NewGuid().ToString('N').Substring(0,12)+'@example.invalid'
    [void](Assert-Status 'POST' '/api/auth/register' @(404) @{
        email=$blockedEmail; password=$env:Bootstrap__AdminPassword
    })
    Write-Host 'PREVIEW_SELF_REGISTRATION_DISABLED=PASS'
    $token=Admin-Token
    $locations=Assert-Status 'GET' '/api/v1/admin/locations' @(200) $null $token
    $entry=@($locations | Where-Object {$_.id -eq $record.id -and $_.isPublished -eq $false})
    if ($entry.Count -ne 1) { throw 'Persisted hidden location not found after restart' }
    Assert-Hidden ([string]$record.slug)
    $persistedContent=Assert-Status 'GET' ('/api/v1/admin/locations/'+[string]$record.id+'/content') @(200) $null $token
    if (@($persistedContent.offers | Where-Object {$_.title -ceq 'Updated CI offer' -and $_.isPublished}).Count -ne 1 -or
        @($persistedContent.brochures | Where-Object {$_.title -ceq 'Updated CI brochure' -and $_.isPublished}).Count -ne 1 -or
        @($persistedContent.jobs | Where-Object {$_.title -ceq 'Updated CI job' -and $_.isPublished}).Count -ne 1) {
        throw 'CONTENT_RESTART_PERSISTENCE_MISMATCH'
    }
    foreach ($item in @(
        @($persistedContent.offers | Where-Object {$_.title -ceq 'Updated CI offer'})[0],
        @($persistedContent.brochures | Where-Object {$_.title -ceq 'Updated CI brochure'})[0],
        @($persistedContent.jobs | Where-Object {$_.title -ceq 'Updated CI job'})[0]
    )) {
        Assert-NewPublicationFreshness $item 'persisted'
    }
    Write-Host 'PUBLICATION_FRESHNESS_PERSISTENCE=PASS'
    Write-Host 'CONTENT_LIFECYCLE_PERSISTENCE=PASS'
    [void](Assert-Status 'GET' '/api/v1/admin/locations' @(401,403))
    if ([string]$record.readerEmail -cnotmatch '^ci-reader-[0-9a-f]{12}@example\.invalid$') {
        throw 'INVALID_PERSISTED_NONADMIN_TEST_IDENTITY'
    }
    $reader=Assert-Status 'POST' '/api/auth/login' @(200) @{
        email=[string]$record.readerEmail;password=$env:Bootstrap__AdminPassword
    }
    if (-not $reader -or [string]::IsNullOrWhiteSpace($reader.accessToken)) {
        throw 'PERSISTED_NONADMIN_TOKEN_MISSING'
    }
    [void](Assert-Status 'GET' '/api/v1/admin/locations' @(403) $null ([string]$reader.accessToken))
    Write-Host 'NONADMIN_PRIVILEGES_PERSISTENTLY_DENIED=PASS'
    Write-Host 'INTEGRATION_RESTART_PERSISTENCE=PASS'
}
