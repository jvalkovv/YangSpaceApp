# F2b H05 — Application recovery: bounded DataProtection foundation

Status: **SOURCE CANDIDATE / NOT RUNTIME ACCEPTED**. Full H05 recovery remains OPEN in [Issue #50](https://github.com/jvalkovv/YangSpaceApp/issues/50).

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

The H05 provisional runner is cloned from the accepted H04 TestServer/PG17 runner and pins its own H05 branch. Require these new markers:

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
