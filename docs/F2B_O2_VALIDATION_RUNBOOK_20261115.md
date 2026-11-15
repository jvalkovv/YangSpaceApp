# F2b O2 — Dual-provider migration, integrity and concurrency validation

**Status: SOURCE PREPARED — O2 NOT RUN — HOLD.**  
**Branch:** `fix/yangspace-f2b-o2-race-safe-20261115`  
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
- Creates one uniquely named local Docker container and one `--internal` network.
- Publishes exactly one PostgreSQL port on `127.0.0.1`; no external bind.
- PostgreSQL 17's image declares `/var/lib/postgresql/data` as a Dockerfile `VOLUME`. The runner overrides that exact destination with an explicit `--mount type=tmpfs` (not the separate `--tmpfs` flag), and uses a second explicit tmpfs mount for `/run/pgcerts`. It verifies the effective `.Mounts` array contains exactly those two writable tmpfs mounts and no volume/bind mount. No bind mounts, named volumes or anonymous volumes are accepted.
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


### PostgreSQL 17 image-volume handling

The official `postgres:17` image declares `/var/lib/postgresql/data` in image `Config.Volumes`. The runner must not use `--tmpfs` for this path because Docker stores that flag separately in `HostConfig.Tmpfs`, which can leave the image-declared anonymous volume in the container mount set. Use explicit `--mount type=tmpfs,dst=...` for both data and TLS paths. Acceptance requires the effective `docker inspect .Mounts` array to contain exactly two writable entries of type `tmpfs`, at `/var/lib/postgresql/data` and `/run/pgcerts`; any `volume` or bind entry is a hard HOLD. The image-declared data volume must be explicitly overridden and the postflight manifest must record that fact.
