# F2b O4 / H01 — Production PostgreSQL TestServer startup (source candidate)

Status: **DRAFT / NOT RUN / HOLD**. Depends on accepted O3 commit `5e0546ae4adcc27d6c997f6f4bfa4eb03d2e28cd` and [Issue #33](https://github.com/jvalkovv/YangSpaceApp/issues/33). No public-account activation or CT164 access is authorized.

## Scope and invariant

Run **the real API Program** in `Production` via `WebApplicationFactory` on a fresh, local-only disposable PostgreSQL 17 TLS fixture. The runner inherits and **re-executes** the accepted O3 test suite first, then launches H01 in a separate process against the *same ephemeral* database. H01 grants the web runtime role **read-only EF migration history**, not schema CREATE or credential-column SELECT.

H01 gates:
- Exact clean branch SHA = remote HEAD; Release build; self-contained Linux runtime publish.
- PostgreSQL 17, internal Docker network, tmpfs, **no host port/bind**, TLS VerifyFull with generated ephemeral CA/cert.
- Non-superuser role, verified disposable database sentinel, role is **not** the migrator/schema owner.
- Actual `Production` Program + unmodified `ProductionPostgreSqlPolicy` + `ProductionSchemaGate` startup path.
- `/health/live` and `/health/ready` return HTTP 200; disabled public and legacy registration endpoints return 404.
- Bad TLS mode, TLS hostname mismatch, elevated migrator role, premature public activation and non-Production activation attempts are refused.
- Complete receipts, logs, manifest and source/binary SHA-256; independent Postflight; container/network/temporary secret cleanup.

**Architectural boundary:** the current production `IdentityEmailOutboxDispatchPolicy.DispatcherAvailable` is hardcoded `false`, so **enabled public-account HTTP cannot legitimately start**. H01 must explicitly verify it fails closed; it **must not** bypass this gate via test-only runtime overrides. Consequently successful H01 is **not** H02–H06 or public HTTP acceptance. A separately reviewed production release-authorization design is mandatory before testing enabled Identity flows.

## Operator: one Windows PowerShell block

Requires .NET 10 SDK, Windows PowerShell 5.1, local Docker Desktop Linux Engine and existing `postgres:17` image. No image pull or GitHub Actions. Creates a new detached worktree; it does not modify the accepted O3 SHA.

```powershell
$ErrorActionPreference = 'Stop'

$Repo = 'D:\GitHub\YangSpaceApp'
$Branch = 'feat/yangspace-f2b-o4-h01-pg-http-startup-20261116'
$Work = 'D:\GitHub\YangSpaceApp-F2b-O4-H01-20261116'

if (Test-Path -LiteralPath $Work) { throw 'H01_WORKTREE_ALREADY_EXISTS' }

git -C $Repo fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'H01_FETCH_FAILED' }

git -C $Repo worktree add --detach $Work ("origin/$Branch")
if ($LASTEXITCODE -ne 0) { throw 'H01_WORKTREE_FAILED' }

$Sha = (git -C $Work rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $Sha -notmatch '^[a-f0-9]{40}$') {
    throw 'H01_SHA_FAILED'
}

Push-Location $Work
try {
    & .\scripts\Test-F2b-O4-H01-PostgreSql-Local.ps1 -CommitSha $Sha
    if (-not $?) { throw 'H01_QA_FAILED' }
}
finally { Pop-Location }
```

The runner automatically calls `scripts/Verify-F2b-O4-H01-PostgreSql-Postflight.ps1` on the same exact SHA after Docker cleanup and evidence manifest creation.

Required final indicators:
```text
F2B_O4_H01_PRODUCTION_STARTUP_QA=PASS
F2B_O4_H01_INDEPENDENT_POSTFLIGHT=PASS
F2B_O4_H01_ENABLED_PUBLIC_HTTP=BLOCKED_BY_RELEASE_POLICY
RELEASE_GATE=HOLD_O4_H01_REVIEW_AND_H02_H06
COMMIT_SHA=<exact_sha>
EVIDENCE_DIR=<local_disposable_evidence_directory>
MANIFEST_SHA256=<sha256>
```

On any failure: record **first failing marker**, exact SHA and sanitized evidence path; do not close H01/PR or enable SMTP/registration. Do not paste secrets. If cleanup fails, resolve leftover disposable resources before retrying. Only exact-SHA tests and reviewed evidence count toward acceptance.

## Next work after H01

- H02: public HTTP lifecycle with fake transport and strict separately approved scoped activation (not a generic config escape).
- H03: partitioned rate limits, proxy/trust boundaries, abuse and 429 matrix.
- H04: production-equivalent account register/confirm/resend/login/reset/session and role negatives.
- H05: full independent HTTP/postflight evidence with TLS/non-elevated role and failure injection.
- H06: privacy/browser/mobile/accessibility.
- O5: DataProtection off-release key backups, alerting and operational recovery; O6: reviewed integration/merge.
- CT164/public production: **HOLD** until all gates pass.
