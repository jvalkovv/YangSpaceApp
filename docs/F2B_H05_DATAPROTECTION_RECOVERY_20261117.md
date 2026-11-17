# F2b H05 — Application recovery: bounded DataProtection foundation

## 2026-11-18 — Full H05 acceptance PASS

- Exact tested source SHA: `a73422d9037df691857c3ff8b6b029532345d60c`.
- Runtime gates: O4-H01, H02, H03, H04, H05, H05b, H05c, H05d and H05e — all PASS.
- Independent Postflight: aggregate H05 PASS; required H05e manifest, runtime markers, artifact checksums, cleanup and clean-source assertions completed.
- Evidence: `D:\GitHub\YangSpaceApp-F2b-H05e-QA-20261118-r3\logs\yangspace\f2b-o4-h05e-recovery-observability\a73422d9037d-20261118-002610-a3f3bb75`
- Manifest SHA-256: `6f367b711c8383529fda39ed28faad096db21f4f70c709aedeeaa64a84431f0d`
- [PR #56](https://github.com/jvalkovv/YangSpaceApp/pull/56) merged into the F2b feature branch at `5df7f5c0519d532a2501bcf3194f64e3e585e770`; no `main` merge or production deployment.
- H05 #50 and H05e #55 CLOSED. Next: [H06 #57](https://github.com/jvalkovv/YangSpaceApp/issues/57).

No real SMTP, production DB/key ring, CT164, deployment, public registration/dispatch activation or GitHub Actions. Keep release HOLD until H06 and remaining MVP acceptance.


Status: **FULL H05 ACCEPTED** — H05a–H05e and the complete O4-H01/H02/H03/H04 regression passed exact-SHA runtime QA and Independent Postflight at `a73422d9037df691857c3ff8b6b029532345d60c`. [Issue #50](https://github.com/jvalkovv/YangSpaceApp/issues/50) and H05e [Issue #55](https://github.com/jvalkovv/YangSpaceApp/issues/55) are CLOSED. H06 is next under [Issue #57](https://github.com/jvalkovv/YangSpaceApp/issues/57); overall product release remains HOLD.

## 2026-11-17 — H05e runtime QA blocker: telemetry DI constructor (source correction prepared)

- Exact failed QA source: `3eb99abcea489575cdc3769efc92b659ee984798`.
- Windows Release restore/build passed; the SQLite worker regression stopped at DI container validation before PostgreSQL 17/TLS VerifyFull provisioning.
- Root cause: `IdentityEmailOutboxWorkerTelemetry` is an internal implementation type whose parameterless constructor was also internal. The default Microsoft DI `AddSingleton<T>()` implementation-type registration only selects public constructors, so the hosted worker could not be activated.
- Correction: make the constructor public (the telemetry type itself remains internal), and add an explicit Production worker-contract assertion for singleton DI activation. The SQLite runner and Independent Postflight now require `F2B_H05E_TELEMETRY_DI_ACTIVATION`; the manifest records `H05E_TELEMETRY_DI=PASS_PUBLIC_CONSTRUCTOR_SINGLETON_ACTIVATION`.
- This is a source/test-gate correction only. The failing run did not reach PostgreSQL; no runtime H05e acceptance is claimed. Re-run the complete exact-SHA runner and Independent Postflight after the correction commit. PR #56 remains Draft; Issues #50/#55 remain OPEN; release HOLD.

## 2026-11-17 — H05e SMTP fault-injection evidence correction (runtime QA PENDING)

- Correction baseline: PR #56 head `d845bea20730e9da0611ee06748509c13fdb874b`.
- The previous SMTP-readiness probe only set `FakeTransport.FailAlways` and requested `/health/ready`; no transport send was attempted. The corrected Production TestServer contract uses public registration to enqueue an actual message, observes a failed fake transport send, verifies the persisted Pending retry and attempt count, and confirms readiness/liveness remain 200 while transport remains unavailable.
- The fake transport is restored in `finally`, then the existing resend flow proves delivery recovery. Fake transport only; no real SMTP.
- Required markers: `F2B_H05E_SMTP_TRANSPORT_FAILURE_OBSERVED`, `F2B_H05E_SMTP_RETRY_PERSISTED`, `F2B_H05E_SMTP_UNAVAILABLE_READINESS_200`; manifest: `H05E_SMTP_FAILURE_READINESS=PASS_OBSERVED_FAKE_TRANSPORT_RETRY_AND_HTTP_200`.
- The runner and Independent Postflight require all three markers. Exact-SHA runtime acceptance remains PENDING; PR #56 remains Draft, Issues #50/#55 remain OPEN and release remains HOLD.

## 2026-11-17 — H05d accepted; H05e implementation source checkpoint

H05d PR #54 is merged into the consolidated F2b feature branch (not main). Exact tested source SHA: 5dc3939a62b0b2766988d21758bc22819f1cb99b. Release build, full H01–H05c regression, PostgreSQL 17/TLS VerifyFull, H05d Production HTTP markers and Independent Postflight passed. Evidence manifest SHA-256: d70584c89a532775a9e8acee4f6709138a13b74c62efc12bf18fc12524e0f35b. Merge commit: 6b411be1295fcff92d1ce5af38aca4c407234bd8.

### H05e source implementation

Branch: feat/yangspace-f2b-h05e-recovery-observability-20261117.
Integrated PR #56 baseline before SMTP fault-injection correction: d845bea20730e9da0611ee06748509c13fdb874b.
Current exact-SHA acceptance target: the live PR #56 head.

- Worker owner transitions use row ID + expected revision + lease ID + unexpired lease for Sent, retry and suppression. Zero-row transitions are recorded as lease loss.
- Transport failure handling is isolated from database finalization. A database exception during Sent finalization is a worker/database cycle failure, not a transport failure; the row remains leased until expiry/reclaim.
- Delivery remains at-least-once when transport accepted but Sent persistence failed. Duplicate delivery is possible and is not misrepresented as exactly-once.
- Production readiness checks DB connectivity, migration/model state and required Identity/outbox/abuse tables. DB/schema exceptions produce 503 while liveness remains independent. SMTP is not a readiness dependency.
- Added low-cardinality System.Diagnostics.Metrics counters/gauges, redacted structured worker events, and a protected PlatformAdmin/gateway worker-status endpoint with aggregate backlog only.
- PostgreSQL worker tests cover stale owner finalization/retry fencing and a trigger-induced Sent finalization failure followed by lease-expiry recovery.
- Production TestServer tests cover actual fake-transport failure through public registration, persisted Pending retry, SMTP-independent readiness/liveness, runtime DB connect loss/restore and required outbox schema loss/restore.

**H05e source is not yet accepted.** The exact-SHA Windows Release build, complete H01–H05d regression, disposable PostgreSQL 17/TLS VerifyFull run, all H05e markers, cleanup, checksum/manifest parity and Independent Postflight have not yet been executed on this HEAD. Keep Issues #50/#55 OPEN and release HOLD. No real SMTP, production DB/key ring, CT164, public activation or GitHub Actions.

## Accepted starting point

H04 exact source `8c68de48a64215eff57f2eaa2bd8594541bba2b3` passed the full Production TestServer/disposable PostgreSQL 17 TLS VerifyFull runner and Independent Postflight. H04 PR #49 is Ready for Review, not merged; H05 is stacked on its exact accepted source. Parent release is HOLD.

## This bounded H05 foundation candidate

- Public-account startup requires a pre-existing, **absolute, non-symlink** DataProtection key-ring directory.
- On Linux, deny group/other directory access; the service identity needs its own 0700-owned path.
- Refuse an empty or missing key ring. No online key generation or implicit fallback during startup.
- Require a pre-provisioned `keyring.sentinel` protected with a stable application discriminator and purpose. Startup decrypts and validates the fixed continuity proof; losing/restoring the ring cannot be treated as a new account identity.
- Identity uses the same persisted DataProtection directory and stable application name for all requests and workers.
- Readiness returns 503 if the continuity proof becomes unavailable or invalid; liveness remains 200. Readiness has **no SMTP dependency**.
- Default-OFF public accounts do not require DataProtection; preview and QA surfaces do not inherit this gate.
- Production TestServer H02 harness creates and disposes a synthetic key ring before startup and verifies independently constructed provider continuity and readiness loss/restore.
- Never persist key material, tokens or synthetic secrets in Git. The test directory lives inside the disposable container and must be deleted on teardown.

## Explicitly NOT accepted by this candidate

- Full proof across independently restarted production TestServer instances for confirmation/reset tokens.
- Actual lost/corrupt key restoration from an off-host backup.
- Admin authorization, replay, concurrent dead-letter recovery and worker crash/fault-injection HTTP matrices.
- Operator observability, incident recovery playbooks and production RPO/RTO.
- Public registration or outbox dispatch activation, real SMTP, CT164/Production DB deployment.

## Offline provisioning contract

Before enabling any public-account production instance, an authorized operator must provision a 0700-owned directory **offline** with a DataProtection-generated `key-*.xml` ring and `keyring.sentinel` encrypted using:
- ApplicationName: `YangSpace.PublicAccounts.v1`
- Purpose: `YangSpace.PublicAccounts.KeyRingContinuity.v1`
- Plaintext continuity check: `YangSpace.PublicAccounts.KeyRingReady.v1`

Set `PublicAccounts__DataProtection__KeyRingDirectory` through protected service configuration to the absolute directory. Preserve all active and older key files required to validate outstanding tokens. Backup/restore the **entire** ring and sentinel as one immutable unit; store the off-host copy encrypted and never commit the contents.

**No production provisioning script or production key has been created or deployed by this PR.**

## QA/operator scope

The H05a provisional runner is cloned from the accepted H04 TestServer/PG17 runner; H05b clones that matrix and pins its own exact branch. Require these new markers:

```
F2B_H05_DATAPROTECTION_UNCONFIGURED_FAILS_CLOSED
F2B_H05_DATAPROTECTION_OFFLINE_PROVISIONED_KEYRING
F2B_H05_DATAPROTECTION_CROSS_INSTANCE_UNPROTECT
F2B_H05_DATAPROTECTION_ISOLATED_RING_REJECTS_TOKEN
F2B_H05_DATAPROTECTION_MISSING_SENTINEL_READINESS_503_LIVE_200
F2B_H05_DATAPROTECTION_SENTINEL_RESTORED_READY_200
F2B_H05_DATAPROTECTION_CORRUPT_SENTINEL_READINESS_503
F2B_H05_DATAPROTECTION_CORRUPT_SENTINEL_RESTORED_READY_200
F2B_H05_DP_FOUNDATION_QA
F2B_H05_INDEPENDENT_POSTFLIGHT
```

Run only in an exact-SHA clean detached worktree with existing disposable local PostgreSQL 17/TLS VerifyFull image; fake transport only. Require Release build with warnings as errors, full H01–H04 regression, all H05 markers, independent manifest/hash/Postflight checks and container/network/temp cleanup.

This runner PASS (when obtained) would accept **only the DataProtection foundation slice**, not the full H05 Issue #50.

## Exact-SHA operator invocation

From Windows PowerShell 5.1 in a **new, clean, detached** worktree checked out at the exact current H05 PR head:

```powershell
$ErrorActionPreference='Stop'
$Repo='D:\GitHub\YangSpaceApp'
$Branch='feat/yangspace-f2b-h05-app-recovery-20261117'
$Sha='<current exact H05 remote HEAD>'

git -C $Repo fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'H05_FETCH_FAILED' }
if ((git -C $Repo rev-parse "origin/$Branch").Trim() -ne $Sha) {
    throw 'H05_REMOTE_SHA_MISMATCH'
}
$Work='D:\GitHub\YangSpaceApp-F2b-H05-DP-QA-20261117'
if (Test-Path $Work) { throw 'H05_WORKTREE_EXISTS' }
git -C $Repo worktree add --detach $Work $Sha
if ($LASTEXITCODE -ne 0) { throw 'H05_WORKTREE_FAILED' }
& (Join-Path $Work 'scripts/Test-F2b-O4-H01-PostgreSql-Local.ps1') -CommitSha $Sha
if (-not $?) { throw 'H05_DP_FOUNDATION_QA_FAILED' }
```

**Do not run this against CT164 or any production database.** Evidence is stored under `logs/yangspace/f2b-o4-h05-dataprotection/<exact-sha-id>/`, within the local QA worktree. The H05 runner reuses the accepted H04 disposable PG17/TLS VerifyFull harness and validates its own SHA, markers, hashes and cleanup.

A passing result is **not** authorization to close full H05 #50: complete worker/operator recovery, restore verification and observability are separate unaccepted slices.


## H05b — Real Identity token recovery after TestServer replacement

**Status: SOURCE CANDIDATE, EXACT-SHA RUNTIME QA PENDING.** H05a's exact tested SHA remains unchanged.

- Create one unconfirmed and one confirmed public Identity user in the first Production TestServer, using the disposable PG17 identity runtime role and synthetic key ring.
- Generate a real `UserManager.GenerateEmailConfirmationTokenAsync` and `GeneratePasswordResetTokenAsync` from that first service provider; never serialize the token to any log/evidence.
- Stop/dispose the **entire first Production TestServer and HttpClient** before replacement.
- Construct a new Production TestServer over the same disposable database but a separately provisioned **foreign key ring**. The original confirmation/reset tokens must fail, preserving the user confirmation/password state.
- Copy only the protected key ring files and sentinel into a new restricted temporary directory as an **offline backup/restore simulation**. Verify the restored ring's sentinel decrypts correctly.
- Construct a third independent Production TestServer over the restored copied ring and same database. The original confirmation and reset tokens must now succeed; old password must fail and new password must authenticate.
- Delete the synthetic restored copy; Postflight must prove no stray keys or credentials were emitted into the evidence directory.
- The restored-copy test proves local application recovery semantics; **it does not prove off-host backup/restore, storage encryption, production RPO/RTO or real mail delivery**.

### New H05b exact-SHA markers

```text
F2B_H05B_FIRST_PRODUCTION_TESTSERVER_DISPOSED
F2B_H05B_FOREIGN_RING_CONFIRMATION_TOKEN_REFUSED
F2B_H05B_FOREIGN_RING_RESET_TOKEN_REFUSED
F2B_H05B_OFFLINE_KEYRING_COPY_CONTINUITY_VALID
F2B_H05B_RESTORED_RING_CONFIRMATION_HTTP_SUCCESS
F2B_H05B_RESTORED_RING_RESET_HTTP_SUCCESS
F2B_H05B_RESTORED_RESET_OLD_PASSWORD_REVOKED
F2B_H05B_RESTORED_RESET_NEW_PASSWORD_VALID
F2B_H05B_RESTORED_KEYRING_CLEANED
F2B_H05B_IDENTITY_RESTART_QA
F2B_H05B_INDEPENDENT_POSTFLIGHT
```

### H05b operator acceptance

- Branch: `feat/yangspace-f2b-h05b-restart-token-restore-20261117`; its head must be exact and clean in a fresh detached worktree.
- Run `scripts/Test-F2b-O4-H01-PostgreSql-Local.ps1 -CommitSha <current H05b 40-character SHA>` from that worktree.
- Runner requires H01–H04 + H05a regression, all H05b markers, Release build/warnings-as-errors, disposable PostgreSQL 17/TLS VerifyFull, fake-only email transport, and Independent Postflight with SHA256 receipts and cleanup.
- Completion is **bounded H05b PASS**, not full H05 #50 PASS. Worker operator replay HTTP authorization, restart/fault cases and production observability remain to be accepted. Public account switches remain OFF.


## H05c — operational outbox summary (source candidate)

**Source only; exact-SHA runtime acceptance is not yet available.** Stacked on bounded H05b accepted SHA `bdac3b0bc0159bb39a31b5f4f3337160f289d2ae`.

### Application API

- Existing restricted admin route: `GET /api/v1/admin/email-outbox/summary`, mapped only when **Production Admin** and **PublicAccounts** are both enabled.
- Inherits existing `PlatformAdmin` bearer policy and `ProductionAdminAccess.RequireGateway`; no new bypass or public route.
- Response: `observedAtUtc`, `pending`, `due`, `retryPending`, `leased`, `sent`, `suppressed`, `deadLetter`, `oldestPendingUtc`, `latestSentUtc`. Exact JSON casing is application serializer dependent; fields are strongly typed UTC counts/timestamps.
- No message/user/account IDs, recipient/email addresses, email body, confirmation/reset tokens, sensitive exception text or arbitrary labels.
- Counts are database-derived at request time. `due` counts unexpired Pending eligible for dispatch. `retryPending` is Pending with `AttemptCount > 0`. `oldestPendingUtc` permits backlog age monitoring, not raw data inspection.
- Empty data must produce zero counts and null oldest/latest dates.

### QA and non-goals

- Disposable PostgreSQL 17 worker fixture verifies pending/sent/dead-letter counts against persisted state and ensures serialized summary cannot expose email/user/token/payload/secret values.
- Main exact-SHA Release PG17/TLS VerifyFull runner and Independent Postflight require `F2B_H05C_OUTBOX_SUMMARY_PERSISTED_COUNTS`, `F2B_H05C_OUTBOX_SUMMARY_NO_PII` and protected file hashes.
- **HTTP auth/gateway negative matrix remains outstanding** (missing/invalid gateway, anonymous, non-admin, cookie-only, bad bearer, valid bearer admin); helper-level tests do not prove HTTP access control. Worker fault/ACK and replay idempotence tests remain outstanding.
- This is a backend operator capability, not a new gateway/nginx or other infrastructure project.
- H05 Issue #50 is not accepted until the remaining operator HTTP/recovery matrix and observability are verified. No production deployment/SMTP/CT164.

### H05c operator run

Execute the existing `scripts/Test-F2b-O4-H01-PostgreSql-Local.ps1` at the **current exact 40-char H05c head** in a clean detached worktree. Evidence root: `logs/yangspace/f2b-o4-h05c-outbox-operations`. TestServer/PG17 and independent Postflight use disposable resources only.


## H05d — Production Admin HTTP bearer/gateway and operator replay

**Source candidate; exact-SHA runtime acceptance PENDING.** Stacked on H05c accepted baseline `8e2509e93551ac95b7dfaa52909c0b9a86b5037f`.

Application TestServer tests are implemented as an extension of the existing real Production `Program` fixture on disposable PostgreSQL 17/TLS VerifyFull, using temporary in-process gateway secret and a synthetic loopback TCP peer injected **only** by the test `IStartupFilter`.

Required new behavioral checks:

- Actual Production startup with `Admin:Enabled=true` and PublicAccounts explicit test-only authorization, confirmed synthetic PlatformAdmin fixture, and real `PlatformAdmin` bearer authorization.
- Exact 32-byte Base64 internal admin gateway secret; missing/incorrect secret refused; valid secret from non-loopback peer refused.
- Anonymous/cookie-only/non-admin bearer denied; valid admin bearer admitted to summary and dead-letter listing without any recipient/token payload.
- Dead-letter replay cannot be performed by non-admin bearer or without gateway; concurrent replay via real HTTP must have exactly one 202 Accepted and one 409 Conflict.
- After replay, persisted PostgreSQL row must be Pending with attempt counter reset, lease cleared, dead-letter/failure fields cleared and exactly one revision increment; repeated replay must conflict, unknown ID 404.
- Fake transport only. Background dispatcher is removed **from the admin TestServer's test DI** to ensure stable row-state assertions; production `Program`, route mapping and auth policies are unchanged.

Sixteen new `F2B_H05D_` markers are asserted by the exact-SHA runner and Independent Postflight, along with H01–H05c regressions, build/receipt/hash parity and disposable cleanup. Do not reuse earlier H05c manifest after this changed SHA.

**Still not H05 full acceptance:** deterministic separate worker fault/re-drive observability and runtime DB-loss readiness/restore may remain; no real SMTP/CT164/production DB/public feature activation/ingress/GitHub Actions. No production secrets in source or evidence.


## 2026-11-17 — H05e PostgreSQL worker log assertion false positive; structured PII gate corrected

- Failed exact-SHA runtime: `323a121dbacdc39d3dac3b0aaa13570b3ef83f30`. SQLite regression and PostgreSQL 17 provisioning passed; the PG worker contract reached H05e lease fencing, DB-finalization recovery, telemetry snapshot and metric-cardinality markers before failing `F2B_H05E_WORKER_LOGS_NO_PII`.
- Root cause is in the test oracle, not evidence of a worker data leak: the old assertion rejected any rendered log message containing the substring `Email`, while the required stable event IDs themselves contain `IDENTITY_EMAIL_OUTBOX_...`. The assertion therefore rejected the expected safe event names.
- Correction: capture each worker log's rendered message, structured property names/values and exception text. The gate now rejects PII-bearing property names, labelled sensitive fields, actual synthetic user IDs/emails, generated delivery payloads/tokens, scoped DB connection strings and release scope token. It still requires both transport-failure and worker-cycle-failure event IDs.
- This is a test-observability correction; production worker log templates and behavior are unchanged. No weakening or deletion of the no-PII acceptance gate.
- Corrected exact-SHA runtime and Independent Postflight are pending. Do not claim H05e PASS until the complete runner, cleanup and Independent Postflight pass at one exact SHA. PR #56 remains Draft; Issues #50/#55 remain OPEN; release HOLD.


## 2026-11-17 — H05e DB-outage fault injection corrected (runtime QA PENDING)

The latest exact-SHA operator run passed the earlier H01 stage and advanced into the H02 Production Identity HTTP contract. It failed at `F2B_H05E_DB_UNAVAILABLE_READINESS_503_LIVE_200` after the H05 DataProtection missing/corrupt-sentinel recovery checks had passed.

**Root cause:** the H02 fixture used `REVOKE CONNECT ON DATABASE ... FROM ys_h02_identity` as its DB outage injection. PostgreSQL applies this to new sessions only; existing authenticated sessions remain connected. Npgsql pooling could therefore reuse an already-established session, allowing readiness queries to continue and invalidating the test's expected 503.

**Correction (test harness only; no production health behavior changed):**
- The TestServer factory now supplies an explicitly owned `NpgsqlDataSource` to the EF Core `DirectoryDb` options, using the same exact TLS VerifyFull connection string and runtime identity as Production.
- Revoke CONNECT, then call `NpgsqlDataSource.Clear()` on the **exact data source used by EF**. This avoids relying on `NpgsqlConnection.ClearAllPools()`, which is not a reliable way to clear an explicitly managed EF data-source pool.
- Wait, with a bounded timeout, until `pg_stat_activity` reports zero remaining sessions for `ys_h02_identity`.
- Prove a fresh non-pooled identity connection is refused with PostgreSQL SQLSTATE `42501`.
- Only then assert `/health/ready=503` and `/health/live=200`.
- Restore CONNECT and clear the same data source again before requiring readiness recovery to HTTP 200.
- Runner and Independent Postflight require the EF data-source-cleared, session-drained and new-connection-denied markers plus manifest `H05E_DB_UNAVAILABLE_PROBE=PASS_CONNECT_REVOKED_EF_DATASOURCE_CLEARED_SESSIONS_DRAINED`.

Superseding source/test correction commit: `e15ad690d2519879f769124b6b28cdc67466ef9e`. Runner marker/manifest commit: `77cacf580e03630b62a808926b7f5ecbd6ae2c7d`; Independent Postflight commit: `0ec6637acb94d4b30613b48c7f93e66fc0f2f6cd`.

**Acceptance remains PENDING:** this correction has not yet been run on Windows at the latest exact PR #56 HEAD, and the full runner plus Independent Postflight must pass at the same SHA. PR #56 remains Draft; Issues #50/#55 remain OPEN; release HOLD. No CT164, production DB, real SMTP, deployment or GitHub Actions.


## 2026-11-18 — H02 DB outage probe: CONNECT grantor mismatch corrected

The 2026-11-17 rerun at `54ae2a10e73801424e353f80be2756d73fddb0cd` passed the complete O3 PostgreSQL worker slice and O4-H01 Production TestServer slice. H02 passed the DataProtection missing/corrupt-sentinel recovery checks and emitted `F2B_H05E_DB_UNAVAILABLE_EF_DATASOURCE_CLEARED=PASS` plus `F2B_H05E_DB_UNAVAILABLE_SESSIONS_DRAINED=PASS`, but failed at `F2B_H05E_DB_UNAVAILABLE_NEW_CONNECTION_DENIED`.

**Root cause:** the disposable runner initially granted database CONNECT to `ys_h02_identity` while executing its grants file as PostgreSQL superuser `postgres`. The H02 fault injector later attempted `REVOKE CONNECT` using `ys_o3_migrator`. The migrator was neither the grantor nor the database owner and did not hold CONNECT with grant option, so PostgreSQL did not remove the original grant. Clearing the EF-owned data source and draining sessions was correct; the fresh non-pooled connection was still legitimately allowed by the remaining ACL. This is a test-fixture ACL ownership defect, not an application readiness defect.

**Correction (disposable test harness only):**
- The setup grants `CONNECT WITH GRANT OPTION` to `ys_o3_migrator` and ordinary CONNECT to `ys_o3_runtime`; it no longer grants identity CONNECT directly as `postgres`.
- H02 `GrantIdentityRoleAsync` now grants CONNECT to `ys_h02_identity` using the migrator connection, making the migrator the actual grantor. Identity connection validation runs after this grant.
- After revocation, H02 queries `has_database_privilege(identityRole, database, 'CONNECT')` and requires `false` before clearing the exact EF `NpgsqlDataSource`, draining sessions, and proving a fresh non-pooled connection fails with SQLSTATE `42501`.
- Runner and Independent Postflight require `F2B_H05E_DB_UNAVAILABLE_CONNECT_PRIVILEGE_REVOKED` plus manifest `H05E_DB_UNAVAILABLE_PROBE=PASS_CONNECT_REVOKED_ACL_VERIFIED_EF_DATASOURCE_CLEARED_SESSIONS_DRAINED`.

Source/test contract correction: `ed45175a108ff41bee9201d40a4467ac329d1c4b`. Runner correction: `c6be324e3d458e4b159607f7ca1ba7ba57b9d0b7`. Independent Postflight correction: `f1e8e0739ffed25bad90886406a24e24f4d448ff`.

PostgreSQL 17 documents that a role can revoke only privileges granted by that role unless it is the object owner or has the relevant grant option; see [REVOKE](https://www.postgresql.org/docs/17/sql-revoke.html) and [Privileges](https://www.postgresql.org/docs/17/ddl-priv.html).

**Acceptance remains PENDING:** correction has not yet been run at the new exact PR #56 HEAD. Full runner, disposable cleanup, and Independent Postflight must all pass at one SHA. PR #56 remains Draft; Issues #50/#55 remain OPEN; release HOLD. No production health code, production DB, CT164, real SMTP, deployment or GitHub Actions.
