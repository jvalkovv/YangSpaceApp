# O3 PostgreSQL 17 worker/provider QA: manual, exact-SHA, disposable only.
# No CT164, production DB, SMTP, published ports, bind mounts, Actions or image pulls.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{40}$')][string]$CommitSha)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$Repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Branch='feat/yangspace-f2b-h02-release-auth-http-20261116'
$Project='apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj'
$Db='yangspace_f2b_o3_worker_disposable'
$Image='postgres:17'
$Git=(Get-Command git.exe -ErrorAction Stop).Source
$Dotnet=(Get-Command dotnet.exe -ErrorAction Stop).Source
$Docker=(Get-Command docker.exe -ErrorAction Stop).Source
$Utf8=[System.Text.UTF8Encoding]::new($false)
function Check([bool]$Ok,[string]$Marker) {
    if (-not $Ok) { throw ('F2B_O3_POSTGRES_FAIL=' + $Marker) }
    Write-Host ($Marker + '=PASS')
}
function GitValue([string[]]$ArgsList) {
    $V=@(& $Git -C $Repo @ArgsList)
    if ($LASTEXITCODE -ne 0) { throw 'F2B_O3_GIT_QUERY_FAILED' }
    return ($V -join '').Trim()
}
function DockerValue([string[]]$ArgsList) {
    $V=@(& $Docker @ArgsList)
    if ($LASTEXITCODE -ne 0) { throw 'F2B_O3_DOCKER_QUERY_FAILED' }
    return ($V -join '').Trim()
}
function RandomHex {
    $B=New-Object byte[] 32
    $R=[Security.Cryptography.RandomNumberGenerator]::Create()
    try { $R.GetBytes($B) } finally { $R.Dispose() }
    return [BitConverter]::ToString($B).Replace('-','').ToLowerInvariant()
}
function Stage([string]$Label,[string]$Executable,[string[]]$ArgsList,[int]$TimeoutSeconds=1200) {
    Invoke-YangSpaceStage -Label $Label -Executable $Executable -CommandArguments $ArgsList -WorkingDirectory $Repo -EvidenceDirectory $Evidence -TimeoutSeconds $TimeoutSeconds
}
function Marker([string]$Label,[string]$Name) {
    $Log=Join-Path $Evidence ($Label+'.stdout.log')
    Check (Test-Path -LiteralPath $Log -PathType Leaf) ('RECEIPT_'+$Label)
    Check (([IO.File]::ReadAllText($Log)) -match ('(?m)^'+[regex]::Escape($Name)+'=PASS\s*$')) $Name
}
function Ready([string]$Name) {
    $Ok=$false
    for ($n=0;$n -lt 60;$n++) {
        $Pref=$ErrorActionPreference
        $ErrorActionPreference='Continue'
        try {
            $null=@(& $Docker exec $Name pg_isready -U postgres -d postgres 2>&1)
            $Exit=$LASTEXITCODE
        } finally { $ErrorActionPreference=$Pref }
        if ($Exit -eq 0) { $Ok=$true; break }
        Start-Sleep -Seconds 2
    }
    Check $Ok 'F2B_O3_PG_READY'
}
Set-Location $Repo
& $Git -C $Repo fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'F2B_O3_FETCH_FAILED' }
Check ((GitValue @('rev-parse','HEAD')) -ceq $CommitSha) 'F2B_O3_EXACT_SHA'
Check ((GitValue @('rev-parse','--verify',('refs/remotes/origin/'+$Branch))) -ceq $CommitSha) 'F2B_O3_REMOTE_SHA'
$ActiveBranch=GitValue @('branch','--show-current')
Check ($ActiveBranch -eq '' -or $ActiveBranch -ceq $Branch) 'F2B_O3_BRANCH_OR_DETACHED_WORKTREE'
$Dirty=@(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
Check ($LASTEXITCODE -eq 0 -and $Dirty.Count -eq 0) 'F2B_O3_SOURCE_CLEAN'
$Settings=Get-Content -LiteralPath (Join-Path $Repo 'apps/api/appsettings.json') -Raw | ConvertFrom-Json
Check ($Settings.PublicAccounts.Enabled -eq $false) 'F2B_O3_REGISTRATION_OFF'
Check ($Settings.PublicAccounts.Outbox.DispatchEnabled -eq $false) 'F2B_O3_DISPATCH_OFF'
$DockerHost=[Environment]::GetEnvironmentVariable('DOCKER_HOST','Process')
if ($DockerHost -and $DockerHost -notlike 'npipe://*') { throw 'F2B_O3_REMOTE_DOCKER_HOST_REFUSED' }
Check ((DockerValue @('context','inspect','--format','{{.Endpoints.docker.Host}}')) -like 'npipe://*') 'F2B_O3_DOCKER_LOCAL'
Check ((DockerValue @('info','--format','{{.OSType}}')) -ceq 'linux') 'F2B_O3_DOCKER_LINUX'
$DockerVersion=DockerValue @('info','--format','{{.ServerVersion}}')
$ImageId=DockerValue @('image','inspect','--format','{{.Id}}',$Image)
Check ($ImageId -match '^sha256:[0-9a-f]{64}$') 'F2B_O3_PG_IMAGE_PREEXISTS'
$Nonce=RandomHex
$Evidence=Join-Path (Join-Path $Repo 'logs/yangspace/f2b-o4-h02-identity') ($CommitSha.Substring(0,12)+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+$Nonce.Substring(0,8))
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
. (Join-Path $PSScriptRoot 'Invoke-YangSpaceStage.ps1')
$Temp=Join-Path ([IO.Path]::GetTempPath()) ('yangspace-h02-'+$Nonce.Substring(0,12))
$Publish=Join-Path $Temp 'publish'
$Network='ys-f2b-o3-'+$Nonce.Substring(0,12)
$Container='ys-f2b-o3-pg17-'+$Nonce.Substring(0,12)
$InitPath=Join-Path $Evidence 'temporary-init.sh'
$EntryPath=Join-Path $Evidence 'temporary-entry.sh'
$RolesPath=Join-Path $Evidence 'temporary-roles.sql'
$GrantsPath=Join-Path $Evidence 'temporary-grants.sql'
$RunnerPath=Join-Path $Evidence 'temporary-run.sh'
$H01RunnerPath=Join-Path $Evidence 'temporary-o4-h01-run.sh'
$H02RunnerPath=Join-Path $Evidence 'temporary-h02-identity-run.sh'
$CaPath=Join-Path $Evidence 'pg-root-ca.crt'
$MigratorPw=RandomHex
$RuntimePw=RandomHex
$IdentityPw=RandomHex
$Scope=RandomHex
$NetworkAttempted=$false
$ContainerAttempted=$false
$Complete=$false
$Cleanup=$true
$PgVersion=''
$AppHash=''
$DllHash=''
$CaHash=''
try {
    Stage 'o3-restore' $Dotnet @('restore',$Project,'-warnaserror')
    Stage 'o3-build' $Dotnet @('build',$Project,'-c','Release','--no-restore','-warnaserror')
    Stage 'o3-sqlite-regression' $Dotnet @('run','--project',$Project,'-c','Release','--no-restore','--no-build','--','--f2b-outbox-worker-synthetic-only')
    foreach ($M in @(
        'F2B_O3_WORKER_CONFIRMATION_DISPATCH','F2B_O3_WORKER_RESET_DISPATCH',
        'F2B_O3_WORKER_RETRY_RECOVERY','F2B_O3_WORKER_UNCERTAIN_ACK_AT_LEAST_ONCE',
        'F2B_O3_WORKER_CONCURRENT_CLAIM','F2B_O3_WORKER_NULL_LEASE_RECOVERY',
        'F2B_O3_WORKER_LEASE_EXPIRY_RECOVERY','F2B_O3_WORKER_DEADLETTER',
        'F2B_O3_WORKER_OPERATOR_REPLAY','F2B_O3_WORKER_RETENTION_PURGE',
        'F2B_O3_WORKER_SQLITE_SYNTHETIC_ONLY'
    )) { Marker 'o3-sqlite-regression' $M }
    Stage 'h02-release-auth-contract' $Dotnet @('run','--project',$Project,'-c','Release','--no-restore','--no-build','--','--f2b-h02-release-authorization-only')
    foreach ($M in @(
        'F2B_H02_AUTH_VALID_SIGNATURE','F2B_H02_AUTH_MISSING_REFUSED',
        'F2B_H02_AUTH_BAD_SIGNATURE_REFUSED','F2B_H02_AUTH_EXPIRED_REFUSED',
        'F2B_H02_AUTH_FUTURE_ISSUED_REFUSED','F2B_H02_AUTH_LONG_LIFETIME_REFUSED',
        'F2B_H02_AUTH_WRONG_AUDIENCE_REFUSED','F2B_H02_AUTH_WRONG_SCOPE_REFUSED',
        'F2B_H02_AUTH_WRONG_ENVIRONMENT_REFUSED','F2B_H02_AUTH_WRONG_COMMIT_REFUSED',
        'F2B_H02_AUTH_WRONG_KEY_ID_REFUSED','F2B_H02_AUTH_BAD_PUBLIC_KEY_REFUSED',
        'F2B_H02_AUTH_DEPLOYMENT_SHA_BINDING','F2B_H02_AUTH_PRODUCTION_ONLY',
        'F2B_H02_AUTH_DEFAULT_SWITCHES_OFF','F2B_H02_AUTH_IMPLEMENTATION_AVAILABLE_AFTER_O3',
        'F2B_H02_SIGNED_RELEASE_AUTHORIZATION_CONTRACT'
    )) { Marker 'h02-release-auth-contract' $M }
    Stage 'o3-publish-linux' $Dotnet @('publish',$Project,'-c','Release','-r','linux-x64','--self-contained','true','-warnaserror','-o',$Publish)
    $AppHost=Join-Path $Publish 'YangSpace.Api.RuntimeTests'
    $Dll=Join-Path $Publish 'YangSpace.Api.RuntimeTests.dll'
    Check ((Test-Path -LiteralPath $AppHost -PathType Leaf) -and (Test-Path -LiteralPath $Dll -PathType Leaf)) 'F2B_O3_PUBLISH_OUTPUT'
    $AppHash=(Get-FileHash -LiteralPath $AppHost -Algorithm SHA256).Hash.ToLowerInvariant()
    $DllHash=(Get-FileHash -LiteralPath $Dll -Algorithm SHA256).Hash.ToLowerInvariant()
    Copy-Item -LiteralPath $AppHost -Destination (Join-Path $Evidence 'YangSpace.Api.RuntimeTests.linux-x64') -Force
    Copy-Item -LiteralPath $Dll -Destination (Join-Path $Evidence 'YangSpace.Api.RuntimeTests.dll') -Force
    $Init=@'
#!/bin/bash
set -Eeuo pipefail
umask 077
test -n "$PGDATA"
openssl req -x509 -newkey rsa:2048 -nodes -keyout /run/pgcerts/ca.key -out /run/pgcerts/ca.crt -days 2 -subj '/CN=YangSpaceO3EphemeralCA' -addext 'basicConstraints=critical,CA:TRUE' -addext 'keyUsage=critical,keyCertSign,cRLSign'
openssl req -newkey rsa:2048 -nodes -keyout /run/pgcerts/server.key -out /run/pgcerts/server.csr -subj '/CN=localhost' -addext 'subjectAltName=DNS:localhost' -addext 'extendedKeyUsage=serverAuth' -addext 'keyUsage=critical,digitalSignature,keyEncipherment'
openssl x509 -req -in /run/pgcerts/server.csr -CA /run/pgcerts/ca.crt -CAkey /run/pgcerts/ca.key -CAcreateserial -out /run/pgcerts/server.crt -days 2 -sha256 -copy_extensions copy
chmod 600 /run/pgcerts/ca.key /run/pgcerts/server.key
chmod 644 /run/pgcerts/ca.crt /run/pgcerts/server.crt
cat > "$PGDATA/pg_hba.conf" <<'HBA'
local all all trust
hostssl all all 0.0.0.0/0 scram-sha-256
hostssl all all ::/0 scram-sha-256
hostnossl all all 0.0.0.0/0 reject
hostnossl all all ::/0 reject
HBA
chmod 600 "$PGDATA/pg_hba.conf"
psql -X -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d postgres -c "ALTER SYSTEM SET ssl='on';"
psql -X -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d postgres -c "ALTER SYSTEM SET ssl_cert_file='/run/pgcerts/server.crt';"
psql -X -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d postgres -c "ALTER SYSTEM SET ssl_key_file='/run/pgcerts/server.key';"
psql -X -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d postgres -c "ALTER SYSTEM SET password_encryption='scram-sha-256';"
'@
    $Entry=@'
#!/bin/bash
set -Eeuo pipefail
chown postgres:postgres /run/pgcerts
chmod 700 /run/pgcerts
exec /usr/local/bin/docker-entrypoint.sh postgres
'@
    $Init=[regex]::Replace($Init,"\r\n?",[string][char]10)
    $Entry=[regex]::Replace($Entry,"\r\n?",[string][char]10)
    [IO.File]::WriteAllText($InitPath,$Init,$Utf8)
    [IO.File]::WriteAllText($EntryPath,$Entry,$Utf8)
    $NetworkAttempted=$true
    Stage 'o3-network-create' $Docker @('network','create','--internal',$Network)
    Check ((DockerValue @('network','inspect','--format','{{.Internal}}',$Network)) -ceq 'true') 'F2B_O3_NETWORK_INTERNAL'
    $ContainerAttempted=$true
    Stage 'o3-container-create' $Docker @(
        'create','--network',$Network,'--network-alias','pg','--name',$Container,
        '--mount','type=tmpfs,dst=/var/lib/postgresql/data,tmpfs-size=536870912,tmpfs-mode=0700',
        '--mount','type=tmpfs,dst=/run/pgcerts,tmpfs-size=16777216,tmpfs-mode=0700',
        '-e','POSTGRES_HOST_AUTH_METHOD=trust','--entrypoint','/bin/bash',$Image,'/tmp/o3-entry.sh'
    )
    Stage 'o3-entry-copy' $Docker @('cp',$EntryPath,($Container+':/tmp/o3-entry.sh'))
    Stage 'o3-init-copy' $Docker @('cp',$InitPath,($Container+':/docker-entrypoint-initdb.d/99-o3-tls.sh'))
    Stage 'o3-container-start' $Docker @('start',$Container)
    Ready $Container
    Check ((DockerValue @('inspect','--format','{{.HostConfig.NetworkMode}}',$Container)) -ceq $Network) 'F2B_O3_CONTAINER_NETWORK'
    Check ((DockerValue @('inspect','--format','{{json .HostConfig.Binds}}',$Container)) -in @('null','[]')) 'F2B_O3_NO_HOST_BINDS'
    Check ((DockerValue @('inspect','--format','{{json .HostConfig.PortBindings}}',$Container)) -in @('null','{}')) 'F2B_O3_NO_PUBLISHED_PORTS'
    $MountJson=DockerValue @('inspect','--format','{{json .Mounts}}',$Container)
    $Mounts=@((ConvertFrom-Json -InputObject ('{"items":'+$MountJson+'}')).items)
    Check ($Mounts.Count -eq 2 -and @($Mounts | Where-Object {
        $_.Type -cne 'tmpfs' -or $_.RW -ne $true -or $_.Destination -notin @('/var/lib/postgresql/data','/run/pgcerts')
    }).Count -eq 0) 'F2B_O3_TMPFS_ONLY'
    Check ((DockerValue @('inspect','--format','{{.RestartCount}}',$Container)) -ceq '0') 'F2B_O3_ZERO_RESTARTS'
    Stage 'o3-pg-version' $Docker @('exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1','-U','postgres','-d','postgres','-c','SHOW server_version_num;')
    $PgVersion=[IO.File]::ReadAllText((Join-Path $Evidence 'o3-pg-version.stdout.log')).Trim()
    Check ($PgVersion -match '^\d+$' -and [int]$PgVersion -ge 170000 -and [int]$PgVersion -lt 180000) 'F2B_O3_POSTGRESQL_17'
    Stage 'o3-pg-ssl' $Docker @('exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1','-U','postgres','-d','postgres','-c','SHOW ssl;')
    Check (([IO.File]::ReadAllText((Join-Path $Evidence 'o3-pg-ssl.stdout.log')).Trim()) -ceq 'on') 'F2B_O3_PG_SSL_ON'
    Stage 'o3-pg-hba' $Docker @('exec',$Container,'psql','-X','-A','-t','-v','ON_ERROR_STOP=1','-U','postgres','-d','postgres','-c',"SELECT CASE WHEN count(*)=5 AND count(*) FILTER (WHERE error IS NOT NULL)=0 AND count(*) FILTER (WHERE type='local' AND auth_method='trust')=1 AND count(*) FILTER (WHERE type='hostssl' AND auth_method='scram-sha-256')=2 AND count(*) FILTER (WHERE type='hostnossl' AND auth_method='reject')=2 THEN 'PASS' ELSE 'FAIL' END FROM pg_hba_file_rules;")
    Check (([IO.File]::ReadAllText((Join-Path $Evidence 'o3-pg-hba.stdout.log')).Trim()) -ceq 'PASS') 'F2B_O3_HBA_POLICY'
    Stage 'o3-ca-export' $Docker @('exec',$Container,'openssl','base64','-A','-in','/run/pgcerts/ca.crt')
    $CaBytes=[Convert]::FromBase64String(([IO.File]::ReadAllText((Join-Path $Evidence 'o3-ca-export.stdout.log')).Trim()))
    Check ([Text.Encoding]::ASCII.GetString($CaBytes).StartsWith('-----BEGIN CERTIFICATE-----')) 'F2B_O3_CA_VALID'
    [IO.File]::WriteAllBytes($CaPath,$CaBytes)
    $CaHash=(Get-FileHash -LiteralPath $CaPath -Algorithm SHA256).Hash.ToLowerInvariant()

    [IO.File]::WriteAllText($RolesPath,(
        "CREATE ROLE ys_o3_migrator LOGIN PASSWORD '$MigratorPw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;" + [Environment]::NewLine +
        "CREATE ROLE ys_o3_runtime LOGIN PASSWORD '$RuntimePw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOINHERIT;" + [Environment]::NewLine +
        "CREATE ROLE ys_h02_identity LOGIN PASSWORD '$IdentityPw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOINHERIT;" + [Environment]::NewLine
    ),$Utf8)
    Stage 'o3-roles-copy' $Docker @('cp',$RolesPath,($Container+':/tmp/o3-roles.sql'))
    Stage 'o3-roles-apply' $Docker @('exec',$Container,'psql','-X','-v','ON_ERROR_STOP=1','-U','postgres','-d','postgres','-f','/tmp/o3-roles.sql')
    Stage 'o3-roles-clean' $Docker @('exec',$Container,'rm','-f','/tmp/o3-roles.sql')
    Remove-Item -LiteralPath $RolesPath -Force
    Stage 'o3-db-create' $Docker @('exec',$Container,'createdb','-U','postgres','--owner=postgres',$Db)
    [IO.File]::WriteAllText($GrantsPath,(
        "REVOKE ALL ON DATABASE $Db FROM PUBLIC;" + [Environment]::NewLine +
        "GRANT CONNECT ON DATABASE $Db TO ys_o3_migrator,ys_o3_runtime,ys_h02_identity;" + [Environment]::NewLine +
        "REVOKE ALL ON SCHEMA public FROM PUBLIC;" + [Environment]::NewLine +
        "GRANT USAGE,CREATE ON SCHEMA public TO ys_o3_migrator;" + [Environment]::NewLine +
        "GRANT USAGE ON SCHEMA public TO ys_o3_runtime,ys_h02_identity;" + [Environment]::NewLine +
        "CREATE TABLE public.yangspace_f2b_o3_qa_guard(scope_token text PRIMARY KEY);" + [Environment]::NewLine +
        "INSERT INTO public.yangspace_f2b_o3_qa_guard(scope_token) VALUES ('$Scope');" + [Environment]::NewLine +
        "GRANT SELECT ON public.yangspace_f2b_o3_qa_guard TO ys_o3_migrator,ys_o3_runtime;" + [Environment]::NewLine
    ),$Utf8)
    Stage 'o3-grants-copy' $Docker @('cp',$GrantsPath,($Container+':/tmp/o3-grants.sql'))
    Stage 'o3-grants-apply' $Docker @('exec',$Container,'psql','-X','-v','ON_ERROR_STOP=1','-U','postgres','-d',$Db,'-f','/tmp/o3-grants.sql')
    Stage 'o3-grants-clean' $Docker @('exec',$Container,'rm','-f','/tmp/o3-grants.sql')
    Remove-Item -LiteralPath $GrantsPath -Force
    $BaseCs="Host=localhost;Port=5432;Database=$Db;SSL Mode=VerifyFull;Root Certificate=/run/pgcerts/ca.crt;Pooling=false;Timeout=10;Command Timeout=30;Include Error Detail=false"
    $MigratorCs="$BaseCs;Username=ys_o3_migrator;Password=$MigratorPw"
    $RuntimeCs="$BaseCs;Username=ys_o3_runtime;Password=$RuntimePw"
    $IdentityCs="$BaseCs;Username=ys_h02_identity;Password=$IdentityPw"
    $MigratorB64=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($MigratorCs))
    $RuntimeB64=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($RuntimeCs))
    $IdentityB64=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($IdentityCs))
    $ScopeB64=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($Scope))
    $Run=@'
#!/bin/bash
set -Eeuo pipefail
umask 077
export YANGSPACE_F2B_O3_PG_MIGRATOR_CONNECTION="$(printf '%s' '__MIGRATOR__' | base64 -d)"
export YANGSPACE_F2B_O3_PG_RUNTIME_CONNECTION="$(printf '%s' '__RUNTIME__' | base64 -d)"
export YANGSPACE_F2B_O3_PG_SCOPE_TOKEN="$(printf '%s' '__SCOPE__' | base64 -d)"
export YANGSPACE_F2B_H02_PG_IDENTITY_CONNECTION="$(printf '%s' '__IDENTITY__' | base64 -d)"
export YANGSPACE_RELEASE_COMMIT_SHA='__COMMIT_SHA__'
export YANGSPACE_F2B_O3_PG_KEY_DIR=/run/pgcerts/o3-keys
cd /tmp/o3-runtime
exec ./YangSpace.Api.RuntimeTests --f2b-o3-postgresql-worker-only
'@
    $Run=$Run.Replace('__MIGRATOR__',$MigratorB64).Replace('__RUNTIME__',$RuntimeB64).Replace('__SCOPE__',$ScopeB64).Replace('__IDENTITY__',$IdentityB64).Replace('__COMMIT_SHA__',$CommitSha)
    [IO.File]::WriteAllText($RunnerPath,([regex]::Replace($Run,"\r\n?",[string][char]10)),$Utf8)
    Stage 'o3-runtime-mkdir' $Docker @('exec',$Container,'mkdir','-p','/tmp/o3-runtime')
    Stage 'o3-runtime-copy' $Docker @('cp',($Publish+'\.'),($Container+':/tmp/o3-runtime/'))
    Stage 'o3-runtime-chmod' $Docker @('exec',$Container,'chmod','0755','/tmp/o3-runtime/YangSpace.Api.RuntimeTests')
    Stage 'o3-runner-copy' $Docker @('cp',$RunnerPath,($Container+':/tmp/o3-run.sh'))
    Remove-Item -LiteralPath $RunnerPath -Force
    Stage 'o3-pg-worker' $Docker @('exec',$Container,'bash','/tmp/o3-run.sh') 2400
    foreach ($M in @(
        'F2B_O3_PG_MIGRATIONS_APPLIED','F2B_O3_PG_MIGRATOR_TLS_SCOPE',
        'F2B_O3_PG_RUNTIME_LEAST_PRIVILEGE','F2B_O3_PG_RUNTIME_DENIED_OPERATIONS',
        'F2B_O3_PG_RUNTIME_TLS_SCOPE','F2B_O3_PG_DISPATCH_IMPLEMENTATION_AVAILABLE',
        'F2B_O3_PG_DI_ACTIVATION','F2B_O3_PG_FAKE_TRANSPORT_ONLY',
        'F2B_O3_PG_CONFIRMATION_DISPATCH','F2B_O3_PG_RETRY_BACKOFF',
        'F2B_O3_PG_RETRY_RECOVERY','F2B_O3_PG_CONCURRENT_CLAIM',
        'F2B_O3_PG_LEASE_EXPIRY_RECOVERY','F2B_O3_PG_PASSWORD_RESET',
        'F2B_O3_PG_CONFIRMED_USER_SUPPRESSION','F2B_O3_PG_UNCERTAIN_ACK_PENDING_RETRY',
        'F2B_O3_PG_UNCERTAIN_ACK_AT_LEAST_ONCE','F2B_O3_PG_CRASH_LEASE_PRESERVED',
        'F2B_O3_PG_CRASH_RESTART_RECOVERY','F2B_O3_PG_DEADLETTER',
        'F2B_O3_PG_OPERATOR_REPLAY','F2B_O3_PG_RETENTION_PURGE',
        'F2B_O3_PG_NO_TOKEN_OR_RECIPIENT_AT_REST',
        'F2B_O3_WORKER_NPGSQL_PROVIDER','F2B_O3_WORKER_POSTGRESQL_17',
        'F2B_O3_WORKER_TLS_VERIFYFULL','F2B_O3_WORKER_RUNTIME_LEAST_PRIVILEGE',
        'F2B_O3_WORKER_FAKE_TRANSPORT_ONLY','F2B_O3_WORKER_DISPOSABLE_ONLY'
    )) { Marker 'o3-pg-worker' $M }
    # Preserve Invoke-YangSpaceStage's shell-operator rejection.
    # Keep every Docker command as argv, never bash -c/redirection.
    Stage 'o4-h01-root-mkdir' $Docker @('exec',$Container,'mkdir','-p','/qa')
    Stage 'o4-h01-root-copy' $Docker @('exec',$Container,'cp','-a','/tmp/o3-runtime/.','/qa/')
    Stage 'o4-h01-root-verify' $Docker @('exec',$Container,'test','-f','/qa/appsettings.json')
    if (-not $Run.Contains('cd /tmp/o3-runtime') -or
        -not $Run.Contains('--f2b-o3-postgresql-worker-only')) {
        throw 'F2B_O4_H01_BASE_LAUNCHER_CONTRACT_CHANGED'
    }
    # Build the new launcher in PowerShell, then copy via Docker to ephemeral tmpfs.
    # No credentials or unsafe shell syntax in stage arguments/command receipts.
    $H01Run=$Run.Replace('cd /tmp/o3-runtime','cd /qa').Replace(
        '--f2b-o3-postgresql-worker-only','--f2b-o4-h01-postgresql-http-only')
    [IO.File]::WriteAllText($H01RunnerPath,([regex]::Replace($H01Run,"\r\n?",[string][char]10)),$Utf8)
    Stage 'o4-h01-runner-copy' $Docker @('cp',$H01RunnerPath,($Container+':/tmp/o4-h01-run.sh'))
    Stage 'o4-h01-runner-mode' $Docker @('exec',$Container,'chmod','0600','/tmp/o4-h01-run.sh')
    Remove-Item -LiteralPath $H01RunnerPath -Force
    Stage 'o4-h01-testserver' $Docker @('exec',$Container,'bash','/tmp/o4-h01-run.sh') 2400
    foreach ($M in @(
        'F2B_O4_H01_RUNTIME_TLS_SCOPE_NON_ELEVATED',
        'F2B_O4_H01_RUNTIME_METADATA_READ_ONLY',
        'F2B_O4_H01_PRODUCTION_PG_POLICY',
        'F2B_O4_H01_REAL_PRODUCTION_ENVIRONMENT',
        'F2B_O4_H01_PRODUCTION_HTTP_LIVE',
        'F2B_O4_H01_PRODUCTION_HTTP_READY',
        'F2B_O4_H01_DISABLED_PUBLIC_REGISTER_404',
        'F2B_O4_H01_DISABLED_LEGACY_REGISTER_404',
        'F2B_O4_H01_REJECTS_DISABLED_TLS',
        'F2B_O4_H01_REJECTS_TLS_HOSTNAME_MISMATCH',
        'F2B_O4_H01_REJECTS_MIGRATOR_AS_RUNTIME',
        'F2B_O4_H01_REJECTS_MISSING_RELEASE_AUTHORIZATION',
        'F2B_O4_H01_REJECTS_PUBLIC_WITH_DISPATCH_OFF',
        'F2B_O4_H01_REJECTS_NONPRODUCTION_PUBLIC_ENABLE',
        'F2B_O4_H01_REAL_TESTSERVER_DEFAULT_OFF',
        'F2B_O4_H01_DISPOSABLE_PG_TLS_VERIFYFULL',
        'F2B_O4_H01_PRODUCTION_GATES_FAIL_CLOSED'
    )) { Marker 'o4-h01-testserver' $M }
    $H01Log=[IO.File]::ReadAllText((Join-Path $Evidence 'o4-h01-testserver.stdout.log'))
    Check ($H01Log -match '(?m)^F2B_O4_H01_ENABLED_PUBLIC_HTTP=BLOCKED_BY_RELEASE_POLICY\s*$') 'F2B_O4_H01_PUBLIC_RELEASE_POLICY_HELD'
    Check ($H01Log -match '(?m)^F2B_O4_H01_SMTP=NOT_CONNECTED\s*$') 'F2B_O4_H01_SMTP_DISCONNECTED'
    Stage 'o3-runtime-clean' $Docker @('exec',$Container,'rm','-rf','/tmp/o3-runtime','/qa','/tmp/o3-run.sh','/tmp/o4-h01-run.sh','/run/pgcerts/o3-keys')
    $Complete=$true
}
finally {
    $Saved=$ErrorActionPreference
    $ErrorActionPreference='Continue'
    try {
        if ($ContainerAttempted) {
            $null=@(& $Docker inspect $Container 2>&1)
            if ($LASTEXITCODE -eq 0) {
                $null=@(& $Docker rm -f $Container 2>&1)
                if ($LASTEXITCODE -ne 0) { $Cleanup=$false }
            }
            $null=@(& $Docker inspect $Container 2>&1)
            if ($LASTEXITCODE -eq 0) { $Cleanup=$false }
        }
        if ($NetworkAttempted) {
            $null=@(& $Docker network inspect $Network 2>&1)
            if ($LASTEXITCODE -eq 0) {
                $null=@(& $Docker network rm $Network 2>&1)
                if ($LASTEXITCODE -ne 0) { $Cleanup=$false }
            }
            $null=@(& $Docker network inspect $Network 2>&1)
            if ($LASTEXITCODE -eq 0) { $Cleanup=$false }
        }
    } finally {
        $ErrorActionPreference=$Saved
        foreach ($P in @($InitPath,$EntryPath,$RolesPath,$GrantsPath,$RunnerPath,$H01RunnerPath)) {
            if (Test-Path -LiteralPath $P) { Remove-Item -LiteralPath $P -Force -ErrorAction SilentlyContinue }
        }
        if (Test-Path -LiteralPath $Temp) { Remove-Item -LiteralPath $Temp -Recurse -Force -ErrorAction SilentlyContinue }
        if (-not $Complete -or -not $Cleanup) {
            [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),'F2B_O3_PG=HOLD',$Utf8)
        }
    }
}
if (-not $Complete -or -not $Cleanup) {
    [IO.File]::WriteAllText((Join-Path $Evidence 'gate.txt'),'F2B_O3_PG=HOLD',$Utf8)
    throw 'F2B_O3_POSTGRES_RUN_OR_CLEANUP_FAILED'
}
$SecretsRemain=@(@($InitPath,$EntryPath,$RolesPath,$GrantsPath,$RunnerPath,$H01RunnerPath) | Where-Object { Test-Path -LiteralPath $_ })
Check ($SecretsRemain.Count -eq 0 -and -not (Test-Path -LiteralPath $Temp)) 'F2B_O3_TEMPORARY_SECRET_CLEANUP'
$HashPaths=@(
    'apps/api/IdentityEmailOutboxWorker.cs',
    'apps/api.RuntimeTests/IdentityEmailOutboxWorkerContract.cs',
    'apps/api.RuntimeTests/IdentityEmailOutboxPostgreSqlWorkerContract.cs',
    'apps/api.RuntimeTests/Program.cs',
    'apps/api.RuntimeTests/F2bO4H01PostgreSqlStartupContract.cs','apps/api/appsettings.json',
    'scripts/Invoke-YangSpaceStage.ps1',
    'scripts/Test-F2b-Outbox-Worker-PostgreSql-Local.ps1',
    'scripts/Verify-F2b-Outbox-Worker-PostgreSql-Postflight.ps1',
    'scripts/Test-F2b-O4-H01-PostgreSql-Local.ps1',
    'scripts/Verify-F2b-O4-H01-PostgreSql-Postflight.ps1'
)
$Checksums=@()
foreach ($P in $HashPaths) {
    $File=Join-Path $Repo $P
    Check (Test-Path -LiteralPath $File -PathType Leaf) ('SOURCE_'+($P -replace '[^a-zA-Z0-9]','_'))
    $Checksums+=((Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$P)
}
foreach ($P in @(
    'pg-root-ca.crt','o3-sqlite-regression.stdout.log','o3-pg-worker.stdout.log','o4-h01-testserver.stdout.log',
    'YangSpace.Api.RuntimeTests.linux-x64','YangSpace.Api.RuntimeTests.dll'
)) {
    $Checksums+=((Get-FileHash -LiteralPath (Join-Path $Evidence $P) -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$P)
}
[IO.File]::WriteAllLines((Join-Path $Evidence 'SHA256SUMS.txt'),[string[]]$Checksums,$Utf8)
$Dirty=@(& $Git -C $Repo status --porcelain --untracked-files=all | Where-Object { $_ })
Check ($LASTEXITCODE -eq 0 -and $Dirty.Count -eq 0) 'F2B_O3_POST_RUN_CLEAN'
$Manifest=@(
    "SOURCE_SHA=$CommitSha","BRANCH=$Branch","DOCKER_VERSION=$DockerVersion",
    "POSTGRES_IMAGE_ID=$ImageId","POSTGRES_VERSION=$PgVersion",
    "POSTGRES_CONTAINER=$Container","POSTGRES_NETWORK=$Network",
    "ROOT_CA_SHA256=$CaHash","APPHOST_SHA256=$AppHash","DLL_SHA256=$DllHash",
    'POSTGRES_DISPOSABLE=PASS','POSTGRES_17=PASS','POSTGRES_TLS_VERIFYFULL=PASS',
    'POSTGRES_HBA=HOSTSSL_SCRAM_HOSTNOSSL_REJECT',
    'POSTGRES_RUNTIME_LEAST_PRIVILEGE=PASS',
    'POSTGRES_WORKER_CONFIRMATION=PASS','POSTGRES_WORKER_RETRY=PASS',
    'POSTGRES_WORKER_CONCURRENT_CLAIM=PASS','POSTGRES_WORKER_LEASE_RECOVERY=PASS',
    'POSTGRES_WORKER_PASSWORD_RESET=PASS','POSTGRES_WORKER_UNCERTAIN_ACK=PASS',
    'POSTGRES_WORKER_CRASH_RECOVERY=PASS','POSTGRES_WORKER_DEADLETTER_REPLAY=PASS',
    'POSTGRES_WORKER_RETENTION=PASS','POSTGRES_WORKER_NO_SENSITIVE_OUTBOX_FIELDS=PASS',
    'SQLITE_REGRESSION=PASS','FAKE_TRANSPORT=PASS',
    'H01_REAL_PRODUCTION_TESTSERVER_DEFAULT_OFF=PASS',
    'H01_PRODUCTION_SCHEMA_GATE=PASS',
    'H01_TLS_HOSTNAME_NEGATIVE=PASS',
    'H01_LEAST_PRIVILEGE_NEGATIVE=PASS',
    'H01_PUBLIC_STARTUP_FAIL_CLOSED=PASS',
    'H01_ENABLED_PUBLIC_HTTP=BLOCKED_BY_COMPILE_TIME_RELEASE_POLICY',
    'NO_HOST_PORTS=PASS','NO_HOST_BINDS=PASS','TMPFS_ONLY=PASS',
    'CONTAINER_REMOVED=PASS','NETWORK_REMOVED=PASS',
    'TEMPORARY_SECRETS_REMOVED=PASS','CLEAN_SOURCE=PASS',
    'PUBLIC_REGISTRATION=OFF','OUTBOX_DISPATCH=OFF',
    'SMTP=NOT_CONNECTED','CT164_DB=NOT_ACCESSED','PRODUCTION_DB=NOT_ACCESSED',
    'GITHUB_ACTIONS=NOT_USED','RELEASE_GATE=HOLD_O4_H01_REVIEW_AND_H02_H06'
)
[IO.File]::WriteAllLines((Join-Path $Evidence 'manifest.txt'),[string[]]$Manifest,$Utf8)
& (Join-Path $PSScriptRoot 'Verify-F2b-O4-H01-PostgreSql-Postflight.ps1') -CommitSha $CommitSha -EvidenceDirectory $Evidence
if (-not $?) { throw 'F2B_O4_H01_INDEPENDENT_POSTFLIGHT_FAILED' }
Write-Host 'F2B_O4_H01_PRODUCTION_STARTUP_QA=PASS'
Write-Host "COMMIT_SHA=$CommitSha"
Write-Host "EVIDENCE_DIR=$Evidence"
Write-Host ('MANIFEST_SHA256='+(Get-FileHash -LiteralPath (Join-Path $Evidence 'manifest.txt') -Algorithm SHA256).Hash.ToLowerInvariant())
Write-Host 'RELEASE_GATE=HOLD_O4_H01_REVIEW_AND_H02_H06'
