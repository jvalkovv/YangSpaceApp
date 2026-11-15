## 2026-11-15 — O2 PostgreSQL storage isolation correction committed (SOURCE COMMITTED / NOT RUN / HOLD)

**Current exact source SHA:** `4836c330794efc45e5afcc270d0096532d8f4dcd` on `fix/yangspace-f2b-o2-race-safe-20261115`.

The first local O2 run reached and passed RuntimeTests restore/build, both provider model-diff checks, both generated-SQL review gates, and the full SQLite apply → rollback → reapply contract. PostgreSQL 17 started, but the runner correctly stopped at the effective mount inspection gate before creating roles or applying PostgreSQL migrations.

**Root cause:** the official `postgres:17` image declares `/var/lib/postgresql/data` in image `Config.Volumes`. The previous runner used the separate Docker `--tmpfs` option and then detected an effective mount set that was not exactly two tmpfs mounts. The container and internal network were removed by the runner's `finally`; the follow-up read-only inspection confirmed both resources were already absent.

**Correction committed:** replace both `--tmpfs` options with explicit `--mount type=tmpfs` entries. The runner now verifies the effective `docker inspect .Mounts` array contains exactly two writable tmpfs mounts at `/var/lib/postgresql/data` and `/run/pgcerts`, checks that the image-declared data volume is overridden, and rejects any volume/bind mount. The independent postflight also requires the corresponding manifest fields. The runbook records the image-volume behavior and acceptance rule.

**Acceptance remains NOT RUN / HOLD.** The correction is source-committed but has not yet been executed on the local Docker Engine. The earlier SQLite PASS remains valid for SHA `b31f7375fdcd35378526357c362a599aa060e2c6`, not for the new exact SHA. Re-run the complete O2 runner against `4836c330794efc45e5afcc270d0096532d8f4dcd`; do not reuse prior evidence as proof for the new commit.

CT164 and production DB remain NOT ACCESSED; SMTP NOT CONNECTED; PublicAccounts OFF; dispatcher fail-closed; no GitHub Actions. CT164 remains gated on a full new-SHA O2 PASS and independent postflight PASS.

---

## 2026-11-15 — O2 runtime harness committed; execution still HOLD

The offline dual-provider migrations have been generated, reviewed at source level and committed on `fix/yangspace-f2b-o2-race-safe-20261115` (migration artifacts commit `b720f930d9cb64f1a6b26a854af37e8293ada3f5`). **This source slice adds** the real migration contract selectors and Windows runner for SQLite + PostgreSQL 17.

The runner performs apply → rollback → reapply against fresh disposable databases, checks FK/index/unique/data integrity, exercises concurrent enqueue and Pending coalescing, validates TLS VerifyFull with negative hostname/CA tests, and proves the migration role is non-superuser while runtime has only Outbox DML privileges.

It uses the existing worktree and an already-cached local `postgres:17` image only. No image pull, extra clone, bind mount, named/anonymous volume, persistent QA container, CT164 access, production DB or SMTP. PostgreSQL data and keys are tmpfs-backed; container/network and temporary secrets are removed. Evidence is SHA256-manifested and independently postflight-verified.

**Do not interpret this source change as O2 PASS.** The harness has not yet been compiled or executed. Keep PR #41 Draft/HOLD until exact-SHA SQLite + PostgreSQL runtime tests and independent postflight both pass. PublicAccounts OFF; dispatcher fail-closed; CT164 remains gated.

See [F2b O2 Validation Runbook](F2B_O2_VALIDATION_RUNBOOK_20261115.md).

---

## 2026-11-15 — Reconciled parent; exact-SHA QA PENDING

This Outbox branch is being reapplied onto the independently verified current F2b parent after the F2a/F2b reconciliation. The earlier O1 run at `4aeeb1c351140f43168aad1eaa62d3bd31ce00ae` belongs to the pre-reconciliation parent and is **not inherited as acceptance** for this new branch. Re-run the exact-SHA O1 source + synthetic SQLite enqueue/rollback contract and independent postflight on the reconciled branch before O2 migration scaffolding.

The current work remains offline/source-only: no provider migration has been generated or applied, no worker exists, no SMTP delivery or PostgreSQL HTTP lifecycle has been tested, PublicAccounts remains OFF, CT164 is untouched. Keep Draft/HOLD.

---

## 2026-11-14 — O1 accepted; O2 offline dual-provider migration scaffold READY / NOT RUN

O1 tested application source `4aeeb1c351140f43168aad1eaa62d3bd31ce00ae`: native Windows .NET 10 Release build, disposable SQLite in-memory enqueue/rollback contract, exact-SHA independent postflight PASS. Manifest SHA-256 `8a6e06cfe5e4386bcf01f499988515fb2ca4205ff3a34120d4b5d56cf1815ef6`. Synthetic test is accepted ONLY for O1; PostgreSQL migrations and worker remain NOT IMPLEMENTED.

O2 operator helper: `scripts/New-F2b-Outbox-Migrations-Offline.ps1` requires clean exact-source detached worktree, pinned `dotnet-ef 10.0.0`, both explicit design-time opt-ins. It scaffolds two EF migrations and snapshots, checks six changed source files, rebuilds projects, checks model diff and creates **offline SQL deltas** for human review. It never opens a database, applies SQL, mails users, or accesses CT164. Generated files remain UNCOMMITTED; the script is NOT YET TESTED on Windows. Any unexpected schema change causes HOLD.

**Do not merge PR #37 after the migration scaffold alone.** Require migration SQL inspection, both provider upgrades on disposable DBs and rollback/restore, leased dispatcher/at-least-once retry, DataProtection, real PostgreSQL TLS/restricted-account HTTP and ingress test gates. PublicAccounts remains OFF.

---

# F2b O1 — durable email intent foundation (Draft, 2026-11-14)

**Tracking:** Issue #36 (https://github.com/jvalkovv/YangSpaceApp/issues/36) / parent #33 / master #32.

## Source changes

- New `IdentityEmailOutboxMessage` model and `DirectoryDb.IdentityEmailOutbox` mapping. Only synthetic user ID, purpose, UTC times, due/lease state, attempt counter and allowlisted failure code; **no stored email address, raw token, SMTP payload or password**.
- `OutboxIdentityEmailSender` implements Identity's sender interface but only **enqueues**; never connects to SMTP.
- `PublicAccountEndpoints.register` wraps Identity creation and outbox enqueue using the same scoped EF transaction. Either both commit, or neither does. Resend and forgot-password enqueue as pending intent.
- `Program.cs` registers queue sender rather than direct SMTP when public accounts eventually enabled.
- Startup deliberately fails closed with `PUBLIC_ACCOUNT_EMAIL_DISPATCH_NOT_READY` if PublicAccounts is enabled: worker, reviewed migrations, retry/recovery **do not yet exist**. Config defaults remain OFF.
- A small SQLite memory-only test exercises intent insertion, no token persistence in row schema, pending coalescing and forced transaction rollback, without any real DB or HTTP.

## Known gaps / required gates

**This branch is NOT deployable and must NOT be merged into F2b yet.** EF provider-specific migrations and snapshots are missing on purpose; source review must not mislabel build PASS as migration readiness.

1. Generate and REVIEW separate EF Core migrations for `YangSpace.Api` SQLite and `YangSpace.PostgreSql.Migrations` PostgreSQL using dotnet EF from a clean source worktree. Include idempotent SQL inspection, no unintended changes to Identity/claim/publication schema, no automatic production migration, and verify no pending model changes.
2. Implement PostgreSQL leased dispatcher with concurrent `SKIP LOCKED`, expiry/retry/backoff/dead-letter, persistent response-free evidence, and fake transport. Fresh confirmation/reset tokens are generated only at send time. Recheck user confirmation/staff role and recipient in the worker.
3. Proof of SMTP-uncertain ACK: **at-least-once, not exactly-once** semantics. Bounded duplicate mail handling, dedupe/idempotency and eventual operator recovery remain required; a pending query alone is not race-safe.
4. Test actual PostgreSQL17 TLS VerifyFull and nonprivileged user, offline migration, TestServer full HTTP account lifecycle, and worker restart/SMTP failure before opening public accounts. Configure protected Identity DataProtection key ring outside releases; backup/restore.
5. Privacy, abuse, real Nginx trusted proxy and full CT164 recovery gates remain HOLD. No GitHub Actions, no second permanent QA system.

## QA

`scripts/Test-F2b-Outbox-Enqueue-Local.ps1 -CommitSha <exact new SHA>` runs .NET10 restore/Release build and synthetic local rollback test. It records 3×3 stage receipts and a manifest containing `F2B_OUTBOX_ENQUEUE_SYNTHETIC_ONLY=PASS` but `F2B_OUTBOX_PG_MIGRATIONS=NOT_GENERATED`, `F2B_OUTBOX_WORKER=NOT_IMPLEMENTED`, `RELEASE_GATE=HOLD`.

**Source commit != completed feature.** No CT164 access, public signup, real email or product release.
