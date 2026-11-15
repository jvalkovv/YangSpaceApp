# F2b O2 — Dual-provider migration, integrity and concurrency validation

**Status: SOURCE PREPARED — O2 NOT RUN — HOLD.**  
**Branch:** `fix/yangspace-f2b-o2-race-safe-20261115`  
**Latest TLS/tmpfs lifecycle correction:** `628db8cb728566af88773dd6963abe6eafe49f05` (runner) + `9bd65e7a6e75e7c995e4a4c9b520486d531ae6f7` (independent postflight).  
**Migration artifacts commit:** `b720f930d9cb64f1a6b26a854af37e8293ada3f5`  
**Execution rule:** run only against the exact current branch HEAD recorded in the handoff and in the resulting evidence manifest. Any source change invalidates the evidence.

## Scope

| Provider | Pinned baseline | Target migration |
|---|---|---|
| SQLite | `20261114183515_F2aOrganizationClaimFoundation` | `20261115101409_F2bIdentityEmailOutbox` |
| PostgreSQL 17 | `20261111203833_InitialPostgreSqlSchema` | `20261115101421_F2bIdentityEmailOutbox` |

The O2 test runs real EF migrations only against newly created disposable databases. It exercises:

1. Baseline apply → Outbox apply → rollback to the pinned baseline → reapply.
2. Exact schema columns, FK cascade, ordinary indexes, unique partial Pending index and filter.
3. Duplicate Pending rejection, leased-history coexistence, different-purpose coexistence and orphan-FK rejection.
4. Concurrent enqueue, Pending coalescing and exactly one expired-Pending refresh/revision increment.
5. SQLite integrity/FK checks and PostgreSQL catalog/constraint/orphan checks.
6. PostgreSQL 17 TLS VerifyFull, positive TLS session verification, negative hostname and untrusted-CA checks.
7. A non-superuser migrator with schema CREATE only, plus a separate runtime role restricted to SELECT/INSERT/UPDATE/DELETE on the Outbox table. Runtime cannot read AspNetUsers, create schema objects, create DB objects, use TEMP or TRUNCATE.
8. SHA-256 source/evidence manifest and independent postflight.

## Storage and isolation contract

- Reuses the existing Windows worktree. It does not clone another repository or create another worktree.
- Requires an already-present local `postgres:17` image. **No automatic image pull.** If absent, the runner stops before creating a container.
- Creates one uniquely named local Docker container (create → copy a temporary entrypoint wrapper and one initdb hook → start once) and one `--internal` network. The container is not restarted: stopping it would discard both tmpfs mounts, including PGDATA.
- Publishes exactly one PostgreSQL port on `127.0.0.1`; no external bind.
- PostgreSQL 17's image declares `/var/lib/postgresql/data` as a Dockerfile `VOLUME`. The runner currently requests explicit `--mount type=tmpfs` for the data and TLS paths. This request alone is not treated as proof of effective isolation: while the exact container is alive, the runner writes `o2-pg-storage-diagnostics.json` with Docker Server version/OS, image and container `Config.Volumes`, `HostConfig.Tmpfs`, `HostConfig.Mounts`, `HostConfig.Binds`, and effective `.Mounts`. Acceptance still requires exactly two writable tmpfs mounts at the expected destinations and no volume/bind mount. A failed gate must be diagnosed from that file; do not infer the effective mount layout from image metadata alone.
- A single `docker-entrypoint-initdb.d` hook generates the ephemeral CA/server/wrong-CA certificates, writes the strict `pg_hba.conf`, and applies TLS settings with `ALTER SYSTEM` while the official image's temporary initdb server is running. The official entrypoint then stops that temporary server and launches the first final server with TLS already enabled. The runner never calls `docker restart` or copies PostgreSQL config into a running tmpfs data directory.
- The server certificate is valid for `localhost`; the client uses `SSL Mode=VerifyFull` and the ephemeral CA. Wrong hostname and wrong CA must both fail.
- The migrator and runtime passwords are random per run and are not written to command logs. Temporary SQL/password files and the wrong CA are removed. Private keys remain inside tmpfs and disappear with the container. Only the public root CA is retained for evidence.
- Container and network are removed in `finally`. Postflight independently confirms both are absent.
- No CT164 connection, production database, SMTP connection, real registration, public-account enablement or GitHub Actions.

## Exact operator procedure

Use the existing worktree; do not create another QA clone:

```powershell
$ErrorActionPreference = 'Stop'

$Repo = 'D:\GitHub\YangSpaceApp-F2b-O2-RaceSafe-20261115'
$Branch = 'fix/yangspace-f2b-o2-race-safe-20261115'
$Commit = '<EXACT_O2_COMMIT_SHA_FROM_THE_HANDOFF>'

Set-Location $Repo
git fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'FETCH_FAILED' }

if ((git rev-parse HEAD).Trim() -cne $Commit) {
    throw 'LOCAL_HEAD_MISMATCH'
}
if ((git rev-parse "origin/$Branch").Trim() -cne $Commit) {
    throw 'REMOTE_HEAD_MISMATCH'
}
if (@(git status --porcelain --untracked-files=all).Count -ne 0) {
    throw 'WORKTREE_NOT_CLEAN'
}

.\scripts\Test-F2b-O2-Local.ps1 -CommitSha $Commit
```

The runner prints `EVIDENCE_DIR=...` only after SQLite and PostgreSQL tests pass and cleanup succeeds. Copy that exact path:

```powershell
$Evidence = '<EXACT_EVIDENCE_DIR_FROM_RUNNER>'
.\scripts\Verify-F2b-O2-Postflight.ps1 -CommitSha $Commit -EvidenceDirectory $Evidence
```

Do not substitute a different worktree, source SHA, database, image tag or evidence directory. The runner checks the branch tip and clean worktree before doing anything.

## Evidence layout

`logs/yangspace/f2b-o2/<sha-prefix>-<run-id>/`

- Stage receipts: `*.runner.cmd`, `*.stdout.log`, `*.stderr.log`.
- Reviewed provider SQL: `outbox.sqlite.review.sql`, `outbox.postgresql.review.sql`.
- `manifest.txt`, `SHA256SUMS.txt`, retained public `pg-root-ca.crt`.
- `postflight.txt` after independent postflight.
- Any `gate.txt` means HOLD. Do not proceed to CT164.

## Failure and cleanup

The runner intentionally fails closed if the image is missing, the Docker context is not local/Linux, a host bind/volume appears, a port is not loopback-only, the server is not PostgreSQL 17, TLS verification fails, any migration/constraint/concurrency marker is missing, or cleanup cannot be proven.

If PowerShell is interrupted, do **not** run `docker system prune`. Use the exact `POSTGRES_CONTAINER` and `POSTGRES_NETWORK` values from the run manifest, if present:

```powershell
docker rm -f <POSTGRES_CONTAINER>
docker network rm <POSTGRES_NETWORK>
```

If no manifest was written, inspect only the run-specific names with `docker ps -a --filter name=ys-f2b-o2-pg17-` and `docker network ls --filter name=ys-f2b-o2-`; remove only the container/network created by this run.

## Acceptance and next gate

O2 is PASS only when:

- The exact-SHA runner exits 0.
- Both provider migrations pass apply → rollback → reapply.
- Both model-diff checks are clean and both generated SQL deltas pass the single-table/no-destructive-SQL review.
- Integrity, FK, uniqueness, concurrency, TLS VerifyFull and least-privilege markers all pass.
- The SQLite file, temporary secrets, PostgreSQL container and network are removed.
- `Verify-F2b-O2-Postflight.ps1` returns `F2B_O2_POSTFLIGHT=PASS`.

Until then, the state remains HOLD and CT164 is not touched. After O2 PASS, the next stage is the separately gated CT164 preview/runtime validation. PublicAccounts remains OFF, dispatcher remains fail-closed, and SMTP/production remain out of scope.


### PostgreSQL 17 storage-gate diagnosis and correction

The official `postgres:17` image declares `/var/lib/postgresql/data` in image `Config.Volumes`. The captured Docker Engine 29.9.1 / Linux diagnostics from the run at 2026-11-15 18:00 local time proved the requested and effective mounts were correct:

- `HostConfig.Mounts`: exactly two `tmpfs` entries, targets `/var/lib/postgresql/data` and `/run/pgcerts`.
- Effective `.Mounts`: exactly two writable `tmpfs` entries, destinations `/run/pgcerts` and `/var/lib/postgresql/data`.
- `HostConfig.Binds=null`; no bind mount was present.
- The disposable container was absent when inspected after the runner stopped.

**Confirmed root cause:** the runner piped a root JSON array directly into `ConvertFrom-Json` and wrapped its output with `@(...)`. In the Windows PowerShell execution path, the JSON array was surfaced as one array-valued pipeline item. Consequently `$Mounts.Count` was 1 and property access aggregated both destinations, creating a false storage failure. Docker storage isolation itself passed the observed configuration.

**Source correction:** parse the root array through a JSON object envelope (`{"mounts": <array>}`) and then read `$MountEnvelope.mounts`. A two-item synthetic parser contract is checked before O2 starts. The runner continues to require exactly two writable effective tmpfs mounts at the expected destinations and rejects every bind/volume mount. The diagnostic JSON remains in place for future failures.

Hard acceptance is unchanged: exactly two writable tmpfs mounts at `/var/lib/postgresql/data` and `/run/pgcerts`, no bind/volume mount, and independent postflight proof of cleanup. No weakening of the storage isolation contract. PostgreSQL migration/TLS/integrity/concurrency stages have not yet run; rerun the full O2 suite against the new exact branch HEAD.


### Confirmed PostgreSQL tmpfs lifecycle root cause and correction

**Root cause (confirmed from the 2026-11-15 O2 evidence):** the earlier runner copied postgresql.auto.conf and pg_hba.conf into a running container and then invoked docker restart. Both /var/lib/postgresql/data and /run/pgcerts are tmpfs mounts. Stopping/restarting that container discarded the tmpfs contents. The startup log showed a second initdb, and the subsequent postgresql.auto.conf contained only its two default comments; pg_settings therefore reported ssl=off, source=default, and sourcefile=null. This was a runner lifecycle defect, not a certificate-generation or PostgreSQL TLS parsing defect.

**Correction (latest runner commit 628db8cb728566af88773dd6963abe6eafe49f05):**

1. Use docker create with the same single PostgreSQL 17 container, internal network, loopback-only port and exactly two tmpfs mounts.
2. Copy a temporary entrypoint wrapper to /tmp/ and one initdb shell hook into /docker-entrypoint-initdb.d/ while the container is stopped.
3. Start the container once through the wrapper. It changes /run/pgcerts ownership to postgres:postgres mode 0700, then delegates to the official PostgreSQL entrypoint.
4. During the official initdb hook, generate the ephemeral CA, server certificate (SAN DNS:localhost), wrong CA, private/public file permissions, and strict HBA policy. Use ALTER SYSTEM to set ssl=on, certificate/key paths and SCRAM password encryption while the official entrypoint's temporary initialization server is running.
5. The official entrypoint stops its temporary initialization server and starts the first final PostgreSQL server. It reads the generated postgresql.auto.conf and HBA file on that first final start. No Docker container restart occurs.
6. Verify init-hook receipts, certificate chain/SAN/permissions, config ownership/modes, pg_settings.sourcefile, pg_hba_file_rules, SHOW ssl=on, and Docker RestartCount=0. The existing TLS VerifyFull positive and negative tests remain unchanged.

The independent postflight was updated in commit 9bd65e7a6e75e7c995e4a4c9b520486d531ae6f7 to require the create/copy/start/initdb lifecycle receipts, assert the TLS/HBA configuration markers and zero container restarts, and reject retention of both temporary shell scripts.

**Acceptance is not yet proven.** The above commits correct the source; the next exact-SHA runner execution and independent postflight must still pass before O2 can be marked PASS. Prior evidence from de927f44 and cfd73dbc remains HOLD and cannot be reused. CT164 remains untouched.
