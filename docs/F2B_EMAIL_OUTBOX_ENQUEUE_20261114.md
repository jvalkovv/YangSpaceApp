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
