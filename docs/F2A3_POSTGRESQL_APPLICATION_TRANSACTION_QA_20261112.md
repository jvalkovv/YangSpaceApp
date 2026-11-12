# F2a-3 — isolated real PostgreSQL application-transaction QA

**Status:** SOURCE-ONLY / NOT BUILD-TESTED / NOT MERGED / HOLD.  
**Security policy:** Issue [#20](https://github.com/jvalkovv/YangSpaceApp/issues/20) remains **PROPOSED/UNAPPROVED**; no actual reviewer route or role provisioning is included.  
**Target:** stacked on F2a-1 [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17), not V2 or `main`. No GitHub Actions.

## Scope and deliberate separation

The previous merged [PR #21](https://github.com/jvalkovv/YangSpaceApp/pull/21) proved synthetic EF transaction success and full outer rollback on SQLite. This follow-on runs equivalent synthetic EF claims/org/location/membership/audit mutations using **actual Npgsql on disposable PostgreSQL 17**. It does not approve/reject a real claim or grant any live user authority. None of the production API runtime, app configuration, EF entity/migration model, operational CT164/CT152, or public site are changed.

### Isolation topology

| Component | Mode | Protection |
| --- | --- | --- |
| PostgreSQL 17 | Ephemeral `docker run --rm --network none` | `tmpfs` PG data; no published port, host bind or external interface; synthetic database only |
| .NET 10 ASP.NET runtime | Ephemeral `docker run --rm --network container:<pg>` | Shares only PostgreSQL container network namespace; read-only rootfs; tmpfs `/tmp`; no published port or host mount |
| Test application | `dotnet publish` on Windows host, then `docker cp` into disposable sidecar | No host-mounted repository/files, no credentials passed from project settings, no application startup or public HTTP |
| Npgsql EF target | `127.0.0.1:5432`, database `yangspace_f2a3_disposable` | Fail-closed scope opt-in, exact host/database/user, no pooling/password, per-run random server sentinel and matching token |
| Reviewed schema | Pinned EF PostgreSQL migration script SHA-256 | `2c6175976c82463eea5d40a3082528b8d72e1505f8803188a3c4473129bb7ee7` must match before SQL apply |

Local Docker context is required to be a Windows named pipe with a Linux Engine; remote Docker contexts are refused. Both image IDs are recorded. Tags may drift, so image IDs are forensic provenance, not a claim of immutable digest pinning. Both images must be pre-pulled explicitly; runner never performs an image pull. `POSTGRES_HOST_AUTH_METHOD=trust` exists **only inside this network-isolated disposable server**: it must never be reused on a host-published port or real database. The sidecar connects over internal loopback, not through host networking.

### Test-only application checks

1. Verify `YANGSPACE_F2A3_PG_DISPOSABLE_ONLY=CONFIRMED_CONTAINER_NETNS_ONLY`; Npgsql parsed connection must target exactly loopback/5432, database `yangspace_f2a3_disposable`, user `postgres`, SSL disabled **for loopback only**, empty password and pooling=false. Connection is refused otherwise.
2. Connect with Npgsql, query a dedicated `yangspace_f2a3_qa_guard` table, and require the per-run random scope token. Never call `EnsureCreated`, `Migrate`, the main API, or a configured live connection.
3. Verify reviewed EF initial PostgreSQL migration exists and there are zero pre-existing claims/members/audits, then insert **synthetic** Identity applicant/reviewer fixtures, Pending organization and unclaimed locations.
4. In an explicit PostgreSQL EF transaction, update Pending claim + organization + location + new Owner membership + four allowlisted audit events. Commit once and verify all rows using a different `DirectoryDb` with `Pooling=false`.
5. In a second explicit transaction, allow first `SaveChangesAsync` to write all domain/audit rows, then insert a duplicate `(EntityType, EntityId, RevisionAfter)` audit event on second `SaveChangesAsync`; require PostgreSQL error `23505` on the reviewed audit-unique index. Roll back the entire transaction and verify no changed claim/org/location/member/audit rows with another independent connection.
6. Validate all required stdout markers, source cleanliness, two-container isolation, cleanup and evidence manifest before accepting `F2A3_PG_APP_TRANSACTION_ISOLATED_ONLY=PASS`.

### Run / evidence contract

Windows PowerShell 5.1 with local Linux Docker Engine, .NET 10 SDK, Git, and pre-pulled images `postgres:17` and `mcr.microsoft.com/dotnet/aspnet:10.0`:

```powershell
# From a NEW, clean exact-remote-SHA detached worktree on the PR branch:
& .\scripts\Test-F2a3-PostgreSql-AppTx-Local.ps1 -CommitSha '<exact-40-char-GitHub-PR-head>'
```

Required stage receipts include `pgapp-ef-tool-restore`, `pgapp-ef-restore`, `pgapp-ef-build`, `pgapp-ef-model-diff`, `pgapp-ef-script`, `pgapp-runtime-restore`, `pgapp-runtime-publish`, disposable PostgreSQL start/migration/sentinel, isolated sidecar, binary copy and `pgapp-real-ef-transaction`. Each stage emits `.runner.cmd`, `.stdout.log`, `.stderr.log`. Evidence path is under `logs/yangspace/f2a3-pg-apptx/<sha12>-<run-id>/`; a `gate.txt` means HOLD. The runner only writes success manifest **after both containers are removed**.

```text
F2A3_PG_EXACT_SOURCE=PASS
F2A3_PG_LOCAL_IMAGES=PASS
F2A3_PG_REVIEWED_SQL_HASH=PASS
F2A3_PG_SERVER_READY=PASS
F2A3_PG_TWO_CONTAINERS_ISOLATED=PASS
F2A3_PG_SENTINEL_MATCHED=PASS
F2A3_PG_LOOPBACK_DISPOSABLE_SCOPE=PASS
F2A3_PG_REVIEWED_MIGRATION_PRESENT=PASS
F2A3_PG_FRESH_SYNTHETIC_STATE=PASS
F2A3_PG_APP_TX_SUCCESS_PERSISTED=PASS
F2A3_PG_APP_TX_AUDIT_CARDINALITY=PASS
F2A3_PG_APP_TX_SECOND_SAVE_AUDIT_REJECTED=PASS
F2A3_PG_APP_TX_OUTER_ROLLBACK_ALL_ROWS=PASS
F2A3_PG_APP_TRANSACTION_ISOLATED_ONLY=PASS
```

Operator must separately postflight-check manifest, expected stage files and marker lines, both container cleanups, exact source SHA and SHA-256 of evidence. No runtime PASS is claimed until then.

## Explicit exclusions and next dependencies

- No actual Identity reviewer role evaluation, same-token revocation or HTTP access-control checks. No public or admin claim mutation routes are introduced.
- No parallel PostgreSQL writers or competing claim approvals, unknown COMMIT acknowledgement, idempotent retry, DB role least-privilege, real evidence/PII handling, ownership transfer or recovery rehearsal.
- No migration of existing/production data. The fresh synthetic EF migration is applied **only** to the disposable DB. No real `PostgreSQL` host or CT164/CT152 is contacted.
- After passing this QA slice, review the proposed claim security contract under [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20). The first production-code reviewer service requires separately approved role/evidence/transfer/re-grant policy and real PostgreSQL bearer HTTP tests with concurrent clients and fault injection. Parent #17 and V2 #1 remain DRAFT/HOLD.

## 2026-11-12 17:43 Europe/Sofia — R2 PostgreSQL runtime PARTIAL and binary transfer RCA

**Exact operator candidate:** `3fd89d55b337590452974544967f13a017375ed5`. Windows script parser and Git identity **PASS**; local `mcr.microsoft.com/dotnet/aspnet:10.0` pulled and verified (image ID `sha256:36be1e5aacfed3082e501442b9ec2dadf8fc346245b82159cc9ccdf883628415`, registry digest `sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e`).

Operator `STAGE_PASS` receipts: EF tool restore, EF restore, EF build, EF model diff, pinned reviewed SQL script; runtime restore/publish; PG start, ready, create DB, apply reviewed SQL, sentinel; .NET sidecar start. `F2A3_PG_REVIEWED_SQL_HASH=PASS` and `F2A3_PG_TWO_CONTAINERS_ISOLATED=PASS`.

**Failing stage:** `STAGE=pgapp-copy-runtime` returned `STAGE_FAILED=pgapp-copy-runtime EXIT=1`; Docker daemon said `container rootfs is marked read-only`. The host-side `docker cp` API refuses writes to a Docker `--read-only` container, even though `/tmp` is an independently writable tmpfs. The simultaneous `io: read/write on closed pipe` is the consequence of the failed copy, not an independently verified filesystem corruption. **No real Npgsql EF transaction was executed.** Failed evidence dir: `D:\GitHub\YangSpaceApp-F2a3-PG-AppTx-QA-R2\logs\yangspace\f2a3-pg-apptx\3fd89d55b337-20261112-174329-33a19746`. Container cleanup must be verified on the local operator host before next attempt; no success manifest from this run is accepted.

### Corrective design — same PR #22, no removal of `--read-only`

**Current source fix at new candidate `c79bea7c9b7e963f73702980a9147e1de75456df`:**

- Local Windows `tar.exe` archives the published runtime into an evidence-local TAR file with SHA-256.
- New helper `scripts/Invoke-YangSpaceTarStreamStage.ps1` uses `System.Diagnostics.Process` to call `docker exec -i <sidecar> sh -c "mkdir -p /tmp/qa && tar -C /tmp/qa -xf -"`, streaming the TAR's **original binary bytes** into process standard input. Do not use PowerShell's object pipeline for binary data.
- Runtime sidecar remains `--read-only --tmpfs /tmp:rw,size=256m` with `--network container:<pg>`. The runner checks `HostConfig.ReadonlyRootfs=true`, `/tmp` tmpfs, no host binds or published ports.
- After extraction, a sidecar `sha256sum /tmp/qa/YangSpace.Api.RuntimeTests.dll` must match host `Get-FileHash` for the published test DLL. Runner writes TAR and DLL SHA-256 into the evidence manifest after successful Npgsql test and full cleanup.
- Both new `.ps1` files passed a **basic static delimiter scan**, not the native Windows PowerShell parser. Exact-SHA Windows ParseFile must PASS for **both** the main runner and new helper before any .NET/Docker stage. Failure must be recorded as HOLD; no untested runtime PASS is claimed.

**Recovery/cleanup preflight:** inspect Docker (`docker ps -a --filter name=yangspace-f2a3-`) before the R3 run. Do not automatically delete any existing QA container by prefix; inspect unexpected remnants. Keep R2 evidence untouched. Use a **new clean detached worktree** pinned to the new remote branch head. Continue source-only until all R3 stages, cleanup and independent evidence SHA-256 postflight PASS.

**Still out of scope:** real reviewer Identity bearer HTTP, parallel writers/approvals, commit uncertainty, production role privileges, real claim decisions and deployment. Parent PR #17 and V2 #1 remain Draft/HOLD.
