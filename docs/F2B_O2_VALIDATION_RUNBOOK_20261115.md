# F2b O2 — Dual-provider migration, integrity and concurrency validation

**Status: O2 RUNNER PASS at `e1944650f48f1ec9acc3161df0527a57c70defe9`; independent postflight pending.**  
**Branch:** `fix/yangspace-f2b-o2-race-safe-20261115`  
**Exact O2 runner source:** `e1944650f48f1ec9acc3161df0527a57c70defe9`. **Postflight verifier correction:** `scripts/Verify-F2b-O2-Postflight-v2.ps1` (separate verifier-only commit; does not alter the tested runner source).  
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
- Creates one uniquely named local PostgreSQL 17 Docker container and one `--internal` network. The container is started once and never restarted: stopping it would discard both tmpfs mounts, including PGDATA.
- **No host port is published.** The runtime contract is built as a self-contained `linux-x64` application on the Windows host and copied into the existing PostgreSQL container's disposable writable layer at `/tmp/f2b-o2-runtime`. A short bootstrap script containing the per-run connection strings and scope token is copied to `/tmp` and moved into the `/run/pgcerts` tmpfs before execution. The contract runs as the unprivileged OS user `postgres` and connects to `localhost:5432` inside the same container/network namespace. This preserves the single-container, internal-only network boundary and avoids Docker's unsupported internal-network host-port publication path. No second container or additional image is required.
- PostgreSQL 17's image declares `/var/lib/postgresql/data` as a Dockerfile `VOLUME`. The runner requests explicit `--mount type=tmpfs` for PGDATA (512 MiB) and `/run/pgcerts` (8 MiB). The self-contained runtime payload is not placed in tmpfs; it exists only in the disposable container layer and is removed with the container. While the exact container is alive, the runner writes `o2-pg-storage-diagnostics.json` with Docker Server version/OS, image and container `Config.Volumes`, `HostConfig.Tmpfs`, `HostConfig.Mounts`, `HostConfig.Binds`, and effective `.Mounts`. Acceptance still requires exactly two writable tmpfs mounts at the expected destinations and no volume/bind mount. A failed gate must be diagnosed from that file; do not infer the effective mount layout from image metadata alone.
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
.\scripts\Verify-F2b-O2-Postflight-v2.ps1 -CommitSha $Commit -EvidenceDirectory $Evidence
```

Do not substitute a different worktree, source SHA, database, image tag or evidence directory. The runner checks the branch tip and clean worktree before doing anything.

The O2 runner evidence SHA remains the exact tested commit. The v2 verifier accepts a later commit only when the run SHA is its ancestor and the complete intervening diff is limited to the v2 verifier and this runbook. It independently pins the verifier HEAD to the remote branch and checks the source worktree is clean. The original verifier file is intentionally retained unchanged because its SHA-256 is part of the runner evidence manifest.


## Evidence layout

`logs/yangspace/f2b-o2/<sha-prefix>-<run-id>/`

- Stage receipts: `*.runner.cmd`, `*.stdout.log`, `*.stderr.log`.
- Reviewed provider SQL: `outbox.sqlite.review.sql`, `outbox.postgresql.review.sql`.
- `manifest.txt`, `SHA256SUMS.txt`, retained public `pg-root-ca.crt`.
- `postflight.txt` after independent postflight.
- Any `gate.txt` means HOLD. Do not proceed to CT164.

## Failure and cleanup

The runner intentionally fails closed if the image is missing, the Docker context is not local/Linux, a host bind/volume or host-published port appears, the server is not PostgreSQL 17, the self-contained Linux runtime cannot be published or executed, TLS verification fails, any migration/constraint/concurrency marker is missing, or cleanup cannot be proven.

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

**Correction (latest runner commit 7157482a6ce7f0b089d1631a23796a79b0be5a21):**

1. Use docker create with the same single PostgreSQL 17 container, internal network, loopback-only port and exactly two tmpfs mounts.
2. Copy a temporary entrypoint wrapper to /tmp/ and one initdb shell hook into /docker-entrypoint-initdb.d/ while the container is stopped.
3. Start the container once through the wrapper. It changes /run/pgcerts ownership to postgres:postgres mode 0700, then delegates to the official PostgreSQL entrypoint.
4. During the official initdb hook, generate the ephemeral CA, server certificate (SAN DNS:localhost), wrong CA, private/public file permissions, and strict HBA policy. Use ALTER SYSTEM to set ssl=on, certificate/key paths and SCRAM password encryption while the official entrypoint's temporary initialization server is running.
5. The official entrypoint stops its temporary initialization server and starts the first final PostgreSQL server. It reads the generated postgresql.auto.conf and HBA file on that first final start. No Docker container restart occurs.
6. Verify init-hook receipts, certificate chain/SAN/permissions, config ownership/modes, pg_settings.sourcefile, pg_hba_file_rules, SHOW ssl=on, and Docker RestartCount=0. The existing TLS VerifyFull positive and negative tests remain unchanged.

The independent postflight was updated in commit 9bd65e7a6e75e7c995e4a4c9b520486d531ae6f7 to require the create/copy/start/initdb lifecycle receipts, assert the TLS/HBA configuration markers and zero container restarts, and reject retention of both temporary shell scripts.

**Acceptance is not yet proven.** The above commits correct the source; the next exact-SHA runner execution and independent postflight must still pass before O2 can be marked PASS. Prior evidence from de927f44 and cfd73dbc remains HOLD and cannot be reused. CT164 remains untouched.


### Windows CMD argument-guard correction — 2026-11-15

The next exact-SHA operator run passed certificate generation, chain/SAN checks, key permissions and PostgreSQL config ownership/modes, then stopped before running the TLS provenance SQL at `o2-pg-tls-configured` with `STAGE_UNSAFE_ARGUMENT`. The SQL contained PostgreSQL string-concatenation operator `||`, which is intentionally rejected by `Invoke-YangSpaceStage.ps1` because its temporary CMD launcher must not interpolate shell metacharacters.

**Scoped fix:** runner commit `5fcd567a8356ea423dfeeecd6f5218c9a7f14709` rewrites only the SQL predicate. Instead of concatenating the data directory and filename, it verifies the `sourcefile` prefix with `left(...)`, the exact path separator with `substring(... for 1)`, and the remaining filename with `substring(...)`. The SQL argument no longer contains any character rejected by the shared Stage guard. The Stage guard is unchanged; no shell-safety policy was weakened. The predicate still proves that the active `ssl` setting came from `<data_directory>/postgresql.auto.conf` and that no restart is pending.

This failure occurred before the TLS provenance query reached PostgreSQL. It is not a PostgreSQL/TLS failure and does not supersede the earlier tmpfs lifecycle root cause. Re-run the full suite against the new exact branch HEAD; the previous attempt is HOLD and cannot be used as acceptance evidence.


### PostgreSQL public CA export correction — 2026-11-15

The operator run reached and passed the in-container CA certificate validation, server certificate chain validation, CA/server certificate permission checks, TLS provenance, PostgreSQL configuration checks and `SHOW ssl=on`. The next `docker cp` stage failed to export `/run/pgcerts/ca.crt` with Docker reporting that the source path could not be found. This conflicts with the immediately preceding in-container OpenSSL and file-permission checks, which had already read and listed that exact file. The failure is at the container-to-host export boundary; it is not evidence of failed CA generation or disabled TLS.

**Scoped runner correction:** commit `f156b6f265846184c2ac394b23328d1bf0a631bd` replaces both container-to-host `docker cp` operations with `docker exec openssl base64 -A -in ...`. PowerShell validates the base64 alphabet, decodes the exact bytes, verifies the PEM certificate envelope and writes the public root CA to the evidence directory. The wrong-CA certificate is exported the same way for the negative VerifyFull test. Private keys remain inside tmpfs; no certificate private material is exported. The Stage argument guard and TLS/VerifyFull gates are unchanged.

The independent postflight stage inventory was updated in commit `d36729563ae29bd1ca13c031f6e1be1bc1aa3d9b` to require both new export receipts. Existing evidence from the failed `docker cp` run remains HOLD. Re-run the complete O2 runner against the new exact branch HEAD, then run independent postflight against that new evidence directory. Do not reuse earlier run receipts or proceed to CT164 before both gates pass.


### PowerShell runner source repair — 2026-11-15

The exact-SHA invocation at `81f3c62b8b0f299ee23c45b93e41db2cea656ee1` failed during PowerShell parsing, before the runner created a PostgreSQL container or entered any O2 stage. The reported errors around the role SQL at line 702 and later script lines were cascading parse errors, not invalid PostgreSQL role SQL.

**Confirmed source-authoring root cause:** the prior runner update inserted a replacement string containing a PowerShell regex terminator `$'`. JavaScript's string-replacement API interprets `$'` as the suffix of the original source. This caused the CA export block to be truncated and displaced the remaining source to the end of the script, producing an invalid 1,662-line file.

**Repair:** commit `3d21e68003ff1d5acc0224fc64b9e61151e6c98c` restores the intact runner from the last clean source baseline (`ba84435`) and reapplies the CA export change with a replacement callback. Base64 input is now validated by `[Convert]::FromBase64String` and the PEM envelope is checked explicitly; no regex-based Base64 gate is needed. Both public CA exports use `docker exec openssl base64 -A`; private keys remain inside tmpfs. The independent postflight stage requirements remain `o2-pg-root-ca-export` and `o2-pg-wrong-ca-export`.

The repaired source is 1,044 lines (the clean baseline was 1,002 lines); the duplicate tail and old `docker cp` CA-export stages are absent. The runner itself has not yet been executed or parser-validated in Windows PowerShell 5.1. Previous evidence remains HOLD.

**Next exact branch HEAD:** `3d21e68003ff1d5acc0224fc64b9e61151e6c98c`. First run a parser-only check with Windows PowerShell 5.1, then run the full exact-SHA O2 suite only if parsing succeeds. Follow with independent postflight. CT164 and production remain untouched.

### PostgreSQL host-port publication failure — 2026-11-15

The exact-SHA run passed PowerShell parsing, SQLite migration/integrity/concurrency checks, PostgreSQL 17 startup, TLS certificate generation and validation, TLS/HBA configuration, public CA exports, role creation and database permissions. It then failed at the first Npgsql TLS connection with `SocketException (10061)` against `127.0.0.1:53306`.

**Confirmed architecture mismatch:** the runner attached the PostgreSQL container exclusively to a Docker `--internal` bridge network while also requesting `--publish 127.0.0.1:<host-port>:5432`. Docker accepted the requested `HostConfig.PortBindings`, and the container started, but the published port was not effectively installed. The runner checked only `HostConfig.PortBindings`, not `NetworkSettings.Ports`, so it incorrectly marked the requested mapping as valid. Docker's maintainers documented this exact behavior for containers attached only to internal bridge networks: no default-gateway network means no host port publication; `HostConfig.PortBindings` can retain the request while `NetworkSettings.Ports` and `docker port` show no effective mapping. See [Moby discussion #53256](https://github.com/moby/moby/discussions/53256).

**Correction in runner commit `b56c65fa078657e144bceecb489c29463761e3ac`:**

1. Keep the PostgreSQL container on the single `--internal` network and remove host port publication entirely.
2. Build the existing runtime test project as a self-contained `linux-x64` application on the Windows host; no image pull or second container is introduced.
3. Copy the self-contained runtime payload to the disposable container layer at `/tmp/f2b-o2-runtime`. Copy the short environment bootstrap script to `/tmp`, then move only that script into `/run/pgcerts` tmpfs. Connection strings and scope token are base64-encoded in the temporary script so secrets do not appear in Docker command arguments or stage logs.
4. Execute the runtime contract as OS user `postgres` inside the PostgreSQL container. It connects to `localhost:5432`; the positive VerifyFull, wrong-host and wrong-CA tests remain unchanged.
5. Remove the runtime payload from the disposable container layer and the bootstrap script from tmpfs after the contract. The published Linux runtime assembly is retained in evidence and its SHA-256 is independently checked by postflight. The container is then removed in the existing `finally` cleanup.
6. Validate both requested and effective Docker port bindings are empty. The manifest records `POSTGRES_HOST_PORT_PUBLISHED=none` and `POSTGRES_RUNTIME_EXECUTION=IN_DISPOSABLE_CONTAINER`.

The `/run/pgcerts` tmpfs remains at its original 8 MiB limit and contains only certificates, private keys and the short bootstrap script. The self-contained runtime payload is held in the disposable container layer and disappears when the container is removed; no persistent volume or host bind is added. The independent postflight requires the new publish/copy/script-stage/execute/cleanup receipts and the internal-client marker.

**Acceptance is still HOLD.** The new runner has not yet been parser-validated or executed on the operator's Windows/Docker Desktop host. First run parser-only validation on the exact current branch HEAD, then the complete O2 suite and independent postflight. Do not reuse the failed run's evidence. CT164 and production remain untouched.
### Linux bootstrap CRLF failure — 2026-11-15

The exact-SHA execution at `1cce588544d7408f5972b759e995abbc21e553dd` passed all three Windows PowerShell 5.1 parser checks, SQLite model/migration/integrity/concurrency checks, PostgreSQL 17 startup, tmpfs isolation, no-host-port gate, TLS/HBA configuration, certificate validation/export, role creation, database permissions, Linux self-contained publish, runtime copy, bootstrap copy, tmpfs staging, permissions, executable and native dependency checks. It stopped at `o2-pg-runtime-contract` before Npgsql tests or migration contract execution.

**Confirmed root cause:** the PowerShell here-string used to generate `/run/pgcerts/run-contract.sh` inherited Windows CRLF line endings. `docker cp` preserved those bytes. Bash parsed the second line as `set -Eeuo pipefail\r` and returned `set: pipefail\r: invalid option name`. This is a Windows-to-Linux text serialization defect; it is not a PostgreSQL, TLS, Npgsql, permission or runtime dependency failure.

**Correction in runner commit `030f6070bb5600cb88b04857b58fb980dee5a649`:** normalize the bootstrap script to LF before writing it; validate the raw bytes contain no CR, begin with the expected shebang, contain the exact `set -Eeuo pipefail` first command, and are UTF-8 without BOM; retain a sanitized `runtime-bootstrap-validation.txt` evidence receipt; record `RUNTIME_BOOTSTRAP_LINE_ENDINGS=LF` and `RUNTIME_BOOTSTRAP_ENCODING=UTF8_NO_BOM` in the manifest; include the validation receipt in `SHA256SUMS.txt`.

**Independent postflight correction in `c238b3fd909417a6c564e2a9005dc7071584697c`:** require the new manifest keys, include the sanitized validation receipt in the evidence-path checksum allowlist, and verify its exact contents.

**Next gate:** parser-only validation on Windows PowerShell 5.1 at the new exact branch HEAD. If all three scripts parse, rerun the complete O2 runner from the beginning; do not resume the failed container or reuse the `1cce5885` evidence. After a complete runner PASS and successful cleanup, execute independent postflight. CT164 and production remain untouched.

### 2026-11-15 — O2 runner PASS and postflight verifier correction

The complete exact-SHA O2 runner completed at `e1944650f48f1ec9acc3161df0527a57c70defe9` with SQLite, PostgreSQL 17, TLS VerifyFull, integrity, concurrency, Linux runtime contract and cleanup all passing. Evidence directory: `logs/yangspace/f2b-o2/e1944650f48f-20261115-220828-c3fc043e`. Manifest SHA-256: `cdb36392e06cba0ee8b2f42e399d3a75562bc57ead88412e50260b41c06af03f`.

The first independent postflight reached the final Docker cleanup check, then PowerShell 5.1 promoted the expected stderr from `docker inspect` of the already-removed ephemeral container into a terminating error. The O2 evidence itself is not invalidated. The v2 verifier uses successful local Docker container/network listings and exact name matching; Docker CLI/list failures remain hard failures and cannot be mistaken for cleanup success.

To preserve exact-SHA evidence without rerunning the migration suite, the original runner commit remains the `CommitSha`. The follow-up commit is restricted to the new v2 verifier and this runbook. The v2 verifier checks that the runner SHA is an ancestor of its own exact remote-pinned HEAD and rejects any other intervening source changes. The original verifier file remains unchanged because it is covered by the original runner's SHA-256 manifest.

**Current acceptance:** runner PASS; independent postflight pending. CT164 and production DB remain untouched; SMTP disconnected; PublicAccounts OFF; dispatcher fail-closed.
