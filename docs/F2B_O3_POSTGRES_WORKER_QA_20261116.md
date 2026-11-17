# F2b O3 — PostgreSQL 17 Worker/Provider QA (manual runbook)

Status: **SOURCE CANDIDATE / HOLD**, not an accepted runtime result. Reference: Draft PR #42, Issue #36, SSOT and MVP roadmap. There is no deployment authorization in this runbook.

## Scope and controls

- Exact clean source SHA, Windows PowerShell 5.1, .NET 10 SDK, local Linux Docker Engine, **existing** `postgres:17` image.
- Runs the already accepted SQLite synthetic worker suite again at the current source SHA, then publishes a self-contained linux-x64 runtime test binary.
- Runs real Npgsql/EF Core migrations and worker execution against a **new disposable PostgreSQL 17** database inside an internal Docker network with tmpfs data and TLS material; no host port or bind mount, no Docker image pull.
- Dedicated non-superuser migrator and runtime roles. Runtime: outbox DML, column-level Identity reads; prohibited operations must return `42501`.
- Covers confirmation and password-reset token dispatch, suppression, retry/backoff, uncertain acknowledgement (at-least-once), concurrent claims, expired/null/crash lease recovery, dead-letter, operator replay, retention and no sensitive outbox fields.
- Synthetic fake transport only. Neither public HTTP, CT164, live database, real SMTP nor GitHub Actions are involved.

## Single execution block

Run in Windows PowerShell, with Docker Desktop Linux Engine available and the PostgreSQL image already pulled. This uses a **new detached worktree**, so existing development worktrees are not modified.

```powershell
$ErrorActionPreference = 'Stop'
$Repo   = 'D:\GitHub\YangSpaceApp'
$Branch = 'feat/yangspace-f2b-o3-outbox-worker-20261115'
$Work   = 'D:\GitHub\YangSpaceApp-F2b-O3-PG-20261116'

if (Test-Path -LiteralPath $Work) { throw 'O3_PG_WORKTREE_ALREADY_EXISTS' }
git -C $Repo fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'O3_PG_FETCH_FAILED' }

git -C $Repo worktree add --detach $Work ('origin/' + $Branch)
if ($LASTEXITCODE -ne 0) { throw 'O3_PG_WORKTREE_CREATE_FAILED' }

$Sha = (git -C $Work rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $Sha -notmatch '^[a-f0-9]{40}$') {
    throw 'O3_PG_SOURCE_IDENTITY_INVALID'
}

Push-Location $Work
try {
    & .\scripts\Test-F2b-Outbox-Worker-PostgreSql-Local.ps1 -CommitSha $Sha
    if (-not $?) { throw 'O3_PG_QA_FAILED' }
}
finally { Pop-Location }
```

The runner automatically calls `Verify-F2b-Outbox-Worker-PostgreSql-Postflight.ps1` **after** generating its evidence and cleaning up. The independent verifier can also be run separately using the exact `EVIDENCE_DIR` and `COMMIT_SHA` printed at the end:

```powershell
& (Join-Path $Work 'scripts\Verify-F2b-Outbox-Worker-PostgreSql-Postflight.ps1') `
    -CommitSha $Sha -EvidenceDirectory '<EVIDENCE_DIR>'
```

## Evidence and acceptance

Every successful run creates `logs/yangspace/f2b-o3-postgres/<sha12>-<runid>/` under the worktree. Required:

- Per-stage `.runner.cmd`, `.stdout.log`, `.stderr.log` receipts.
- `manifest.txt`, `SHA256SUMS.txt`, ephemeral **public** CA certificate.
- Copied Linux apphost and runtime-test DLL, both independently hashed against the manifest.
- Required SQLite and PostgreSQL assertion markers and independent postflight confirmation.
- Cleanup verifies container and network are gone, temporary secret files do not remain, and the source checkout is clean.
- PostgreSQL role, TLS, HBA and storage checks must all pass. No port may be published and no non-tmpfs mount may exist.

If any gate fails, record the **first failing marker**, source SHA and evidence directory; do not report PASS or close PR #42. Do not paste PostgreSQL passwords or temporary secret scripts into GitHub. On cleanup failure, reconcile leaked disposable resources before retrying.

## Decision boundary

`F2B_O3_POSTGRESQL_WORKER_QA=PASS` and `F2B_O3_POSTGRESQL_INDEPENDENT_POSTFLIGHT=PASS` are necessary but not sufficient. After they are observed on the same exact SHA, perform provider-specific source review and decide whether O3 acceptance is justified. **PR #42 remains Draft; O4 and CT164 deployment remain HOLD.**
