## 2026-11-16 — Expanded O3 PostgreSQL provider QA gates, source only (HOLD)

O3 PostgreSQL worker contract now adds password reset, already-confirmed suppression, uncertain SMTP-style acknowledgement simulation, crash/lease restart recovery, dead-letter and operator replay, retention purge, and absence of tokens/recipients at rest. Its fixture Password policy is explicitly configured, and reset tokens are Base64Url-decoded before verification. Dedicated non-superuser runtime role and narrow Identity reads remain mandatory.

The disposable PostgreSQL runner now performs its **independent Postflight automatically** (same exact source SHA), persists public-CA and Linux binary evidence, validates source/log hashes, and checks cleanup. It supports clean detached worktrees. Runbook: `docs/F2B_O3_POSTGRES_WORKER_QA_20261116.md`.

**No build or actual PostgreSQL execution of these new changes has occurred.** Source is not runtime PASS; O3 / PR #42 are HOLD/Draft. No CT164, live DB, SMTP, public HTTP or GitHub Actions. Next operator action is the single Windows PowerShell runbook block; capture the first failing marker or accepted evidence before progressing.

---

## 2026-11-16 — O3 PostgreSQL 17 worker QA source candidate (RUNTIME HOLD)

This iteration adds `scripts/Test-F2b-Outbox-Worker-PostgreSql-Local.ps1` (exact-SHA disposable QA) and `scripts/Verify-F2b-Outbox-Worker-PostgreSql-Postflight.ps1` (independent evidence verification).

- The PostgreSQL worker contract runs via the real Npgsql/EF Core provider, with a dedicated non-superuser migrator and limited runtime role.
- The runtime role has outbox DML and column-restricted Identity read; negative privilege probes reject credential reads and administrative writes.
- Tests cover confirmation token dispatch, retry/backoff, concurrent claims, expired lease recovery, and the full previously accepted SQLite synthetic regression.
- Disposable PostgreSQL 17 runs on local Docker only: internal network, no published ports, tmpfs-only storage, ephemeral TLS VerifyFull, no image pulls and no CT164/production access.
- The runner captures command receipts, source and log hashes, manifest and public ephemeral CA; secrets, container and network must be removed.
- The independent verifier checks exact local/remote SHA, evidence scope and hashes, required markers and resource absence.

**No new exact-SHA build, SQLite run, PostgreSQL run or independent Postflight has been executed in this source iteration.** Status is `HOLD`; this is not accepted evidence. Production dispatch and public registration remain default OFF. No real SMTP, no production DB, no deployment, no GitHub Actions. PR #42 remains Draft.

Operator from an exact clean branch checkout (or detached worktree after the runner supports it):

```powershell
$Sha = (git rev-parse HEAD).Trim()
& .\scripts\Test-F2b-Outbox-Worker-PostgreSql-Local.ps1 -CommitSha $Sha
# Take EVIDENCE_DIR emitted by the runner, then run independently:
& .\scripts\Verify-F2b-Outbox-Worker-PostgreSql-Postflight.ps1 -CommitSha $Sha -EvidenceDirectory '<EVIDENCE_DIR>'
```

Provider QA is a prerequisite for O3 approval; O4 and CT164 deployment remain blocked.

---

## 2026-11-16 — O3 SQLite synthetic + independent Postflight PASS; PostgreSQL worker QA next

Operator executed exact-SHA O3 worker synthetic at \`750f58ab8ce089161004bec1015cc1a25c760031\`:
- Exact source and all default-OFF guards: PASS.
- Restore and Release build: PASS.
- Complete SQLite synthetic worker contract: PASS.
- DI activation, confirmation/reset dispatch, dispatch-off, current email/confirmation/staff rechecks, expired-intent suppression, retry/backoff, uncertain ACK, concurrent claims, null/expired lease recovery, crash/restart recovery, dead-letter, operator replay, retention purge, no token/recipient at rest: PASS.
- Fake transport only, SQLite shared in-memory only, all stage receipts and clean worktree: PASS.

Evidence directory: \`logs/yangspace/f2b-o3-worker/750f58ab8ce0-20261116-064142-4ce016c0\`
Manifest SHA-256: \`9f59ee1d14a13aaa6f244d831fb94b25a3064b8d01fd98e32e53c8bcc5a90f45\`

The operator then executed \`Verify-F2b-Outbox-Worker-Postflight.ps1\` at the same exact SHA. **Independent Postflight: PASS**, including source/remote identity, clean worktree, evidence scope, manifest, all receipts and worker markers, and default-OFF guards.

**Next gate: disposable PostgreSQL 17 worker/provider QA.** Must exercise the actual Npgsql/EF Core provider path using an isolated PostgreSQL 17 instance, dedicated least-privilege runtime role, fake email transport, real database transactions/atomic claims, concurrent worker claims, lease expiry/recovery and failure/retry behavior. No production DB, CT164, real SMTP, public HTTP, deployment or GitHub Actions. The current O2 disposable PostgreSQL runner is migration-focused and must not be treated as proof of O3 worker/provider behavior.

After PostgreSQL QA: independent PostgreSQL evidence Postflight, provider-specific code review, then O3 acceptance decision. O4 remains gated. PR #42 remains Draft; Issue #36 and MVP remain OPEN.

---

## 2026-11-16 — dispatch-off failure isolated into independent assertions (SOURCE COMMITTED / RE-RUN PENDING)

Operator reran branch HEAD \`933adb6510ff82bcc6ea21b4d64cd9b85b8b24dc\`:
- PostgreSQL review QA default-off: PASS.
- Restore: PASS.
- Release build: PASS.
- DI activation: PASS.
- Synthetic failed at the combined \`F2B_O3_WORKER_DISPATCH_DEFAULT_OFF\` assertion (line 96).

The old compound assertion did not reveal which of its four conditions failed. Source now emits separate markers for runtime switch evaluation, batch blocking, hosted task creation, hosted-loop exit, Pending-state preservation and no fake delivery. The hosted task is awaited with a bounded two-second timeout rather than sampled immediately. This is diagnostic hardening, not a passing result.

Current source-code commit: \`4961d6fe3d53261ec916198b5a3fa2a4c254ebe1\`. Exact-SHA Release build, full synthetic contract and independent Postflight have not yet been rerun at this commit. O3 remains HOLD; PostgreSQL worker QA and O4 remain blocked. Production \`DispatcherAvailable=false\`; both appsettings switches remain false; no SMTP, CT164 DB, deployment or GitHub Actions.

---

## 2026-11-16 — O3 dispatch-off assertion isolated (SOURCE COMMITTED / RE-RUN PENDING)

Operator reran exact branch HEAD \`933adb6510ff82bcc6ea21b4d64cd9b85b8b24dc\`:
- PostgreSQL review QA default-off: PASS.
- Restore: PASS.
- Release build: PASS.
- DI activation: PASS.
- Synthetic failed at the combined \`F2B_O3_WORKER_DISPATCH_DEFAULT_OFF\` assertion (line 96).

The previous assertion combined four independent conditions, so the failing condition was not identifiable. The test now reports each independently:
1. Runtime config switches evaluate disabled.
2. \`ProcessBatchOnceAsync\` claims zero.
3. Hosted task is created and exits successfully within a bounded timeout.
4. Outbox row remains Pending.
5. Fake transport receives no delivery.

This is diagnostic test hardening, not an assertion that the worker already passes. Current source HEAD will be recorded after commit. O3 remains HOLD; do not proceed to PostgreSQL or O4 until exact-SHA synthetic and independent Postflight pass.

---

## 2026-11-16 — O3 production DI activation covered by synthetic contract (SOURCE COMMITTED / RE-RUN PENDING)

The worker contract now mirrors the actual production DI registrations and resolves:
- \`IEmailSender<IdentityUser> -> OutboxIdentityEmailSender\`
- \`SmtpPublicIdentityEmailSender\` without opening an SMTP connection
- \`IHostedService -> IdentityEmailOutboxWorker\`

The synthetic contract uses \`ValidateOnBuild=true\` and resolves the hosted worker and SMTP adapter, so non-public constructor/DI activation failures are caught before PostgreSQL QA. The worker, SMTP adapter and outbox sender now expose DI-discoverable public constructors (their containing types remain internal). New acceptance marker: \`F2B_O3_WORKER_DI_ACTIVATION=PASS\`.

Production dispatch authorization remains compile-time false until O3 acceptance; only the internal synthetic harness supplies the test-only constructor override with fake transport and in-memory switches.

Current branch HEAD: \`7681057020f11d23d9ce16be5aa36e84c12af613\` (latest source-code change: \`28ec89d29c4863dcc77016061eeb15e834d87b07\`; subsequent commit is documentation-only). No rerun has been executed at this HEAD; do not infer acceptance from source changes. PostgreSQL worker/concurrency, Postflight and O4 remain blocked.

---

## 2026-11-16 — production dispatch authorization remains compile-time held (SOURCE COMMITTED / RE-RUN PENDING)

The O3 remediation also restores the explicit production release boundary:
- \`IdentityEmailOutboxDispatchPolicy.DispatcherAvailable=false\` remains in production source until O3 acceptance is formally complete.
- Public Identity startup continues to fail closed if an operator enables PublicAccounts before O3 acceptance.
- The worker's optional synthetic override is an internal constructor argument only. The RuntimeTests harness supplies it with an in-memory SQLite database and fake transport; no configuration key, endpoint or deployment setting can enable it in production.
- Both runtime switches are still required even in the synthetic harness. The dispatch-off regression verifies that the hosted loop exits and the batch method returns without processing.

Current branch HEAD after this release-gate correction: \`de7980008c7109e83f9caf396e480c40ae18714d\`. The earlier exact-SHA build at \`f63149e8908d19945ca3dd5d2345798a92d68e2f\` passed restore/build but failed synthetic; the remediated head has not yet been rerun. PostgreSQL worker QA and O4 remain blocked.

---

## 2026-11-16 — O3 first local run failed; source remediation committed (RE-RUN PENDING / HOLD)

Operator ran exact source \`f63149e8908d19945ca3dd5d2345798a92d68e2f\`:
- Git/source identity: PASS.
- PublicAccounts, Outbox dispatch and PostgreSQL review QA default-off checks: PASS.
- .NET restore: PASS.
- Release build: PASS.
- Worker synthetic contract: FAIL at \`IdentityEmailOutboxWorkerContract.cs:76\`, \`Sequence contains no matching element\` while selecting the expected fake confirmation delivery.
- Independent Postflight: NOT RUN. O3 is NOT accepted.

Root causes and corrections now committed on the same child branch:
1. Enqueue used \`DateTime.UtcNow\` while the worker contract used a frozen \`MutableClock\`; the pending intent could be timestamped a few milliseconds after the worker's frozen time. \`OutboxIdentityEmailSender\` now uses the injected \`TimeProvider\`, and the synthetic contract passes the same clock and asserts timestamp consistency.
2. Candidate selection allowed a leased row with null \`LeasedUntilUtc\`, but the atomic claim predicate did not. Candidate and claim predicates now match, and null-lease recovery remains explicitly tested.
3. Source review found that the hosted worker did not honor \`PublicAccounts:Outbox:DispatchEnabled\`. Both the hosted loop and batch processor now fail closed, with a regression test proving the worker exits and does not dispatch when the switch is false.

- Remediated exact source HEAD: \`1618f2c4fdc7f19c9571523b9cd7721877a1b245\`.
- PR #42 remains OPEN/DRAFT; Issue #36 remains OPEN.
- Next gate: rerun exact-SHA O3 Release build + full SQLite synthetic contract, then independent read-only Postflight.
- PostgreSQL 17 worker/concurrency QA remains NOT RUN.
- PublicAccounts and Outbox dispatch remain false in appsettings. Real SMTP NOT CONNECTED. CT164/Production DB NOT ACCESSED. No deployment and no GitHub Actions.

Do not proceed to PostgreSQL worker acceptance or O4 Identity HTTP until the remediated exact-SHA synthetic contract and independent Postflight both pass.

---

# F2b O3 — durable Identity email outbox worker

**Status (2026-11-16):** First exact-SHA Release build PASS at `f63149e8908d19945ca3dd5d2345798a92d68e2f`; synthetic contract FAIL at line 76; initial remediation committed at `1618f2c4fdc7f19c9571523b9cd7721877a1b245`; production compile-time dispatch HOLD restored in current HEAD `de7980008c7109e83f9caf396e480c40ae18714d`; DI activation smoke test committed at `28ec89d29c4863dcc77016061eeb15e834d87b07`; remediated rerun and independent Postflight NOT RUN; RELEASE HOLD.

## Implementation

O3 consumes the intent rows from O1 and the dual-provider schema accepted by O2. It adds no schema migration. The worker is an ASP.NET Core hosted BackgroundService inside the existing API host; it does not require a separate container, LXC or service process.

- Provider-neutral, revision-guarded compare-and-swap lease claim. This intentionally replaces the older PostgreSQL-only SKIP LOCKED proposal: a conditional UPDATE with the candidate revision and eligible state is atomic on both SQLite and PostgreSQL, and only one worker can advance that revision. PostgreSQL 17 runtime concurrency proof remains mandatory. Candidate selection and atomic claim use the same eligibility rule, including recovery of a Leased row whose lease deadline is null.
- Enqueue timestamps and worker eligibility use the same injected TimeProvider; the synthetic contract verifies this deterministically instead of relying on wall-clock timing.
- Fresh email-confirmation/password-reset Identity tokens generated at dispatch, never at enqueue and never persisted.
- Re-read current Identity user, email, email-confirmation state and all assigned roles immediately before dispatch.
- Deleted, malformed-email, staff-role, already-confirmed confirmation, and unconfirmed password-reset intents are suppressed.
- Injectable transport. Tests use a fake; production uses the existing STARTTLS SMTP adapter. The synthetic contract also resolves the actual DI registrations for the outbox sender, SMTP adapter and hosted worker before running its isolated override.
- Bounded retry: default maximum 5 total attempts with 30-second, 2-minute, 10-minute and 30-minute delays; if an operator raises MaxAttempts above 5, subsequent retries use a 1-hour delay. Exhausted intents move to DeadLetter.
- Expired intents are suppressed before dispatch; lease ownership and intent expiry are revalidated immediately before transport.
- Expired leases, including incomplete/null lease metadata, are reclaimable after process restart. Shutdown cancellation leaves the lease to expire and recover.
- SMTP uncertain acknowledgement is **at-least-once**. If SMTP accepted a message but the ACK was lost, a retry can duplicate it. Exactly-once delivery is not claimed.
- Operator dead-letter listing and replay are inside the existing PlatformAdmin and production gateway-protected route group. Responses do not disclose user IDs, email addresses, tokens or payloads. Successful replay emits a structured operator audit event with outbox ID and actor ID only.
- Sent/Suppressed/DeadLetter rows have 30/30/90-day retention windows.

## Fail-closed configuration

Both switches remain false in appsettings. In addition, the production compile-time dispatch authorization remains held (`DispatcherAvailable=false`) until O3 is formally accepted. The hosted loop and batch processor each independently refuse to process when either runtime switch is false. The synthetic contract uses an internal constructor-only override plus fake transport; there is no configuration key or HTTP path to bypass the production hold. It verifies `F2B_O3_WORKER_DISPATCH_DEFAULT_OFF=PASS`:

- PublicAccounts.Enabled = false
- PublicAccounts.Outbox.DispatchEnabled = false

Worker dispatch requires both switches and remains compile-time held in production until O3 acceptance. Operator replay is separately exposed only inside the protected PlatformAdmin and production gateway admin surface. Dispatch cannot be enabled while PublicAccounts is off. No SMTP credentials or live SMTP transport are exercised by the synthetic runner.

## Exact-SHA local QA

From the existing YangSpace repository worktree, fetch the branch and check out the exact remote HEAD.

~~~powershell
.\scripts\Test-F2b-Outbox-Worker-Local.ps1 -CommitSha <EXACT_40_CHAR_SHA>
~~~

Then run the independent read-only postflight using the exact evidence path printed by the runner.

~~~powershell
.\scripts\Verify-F2b-Outbox-Worker-Postflight.ps1 -CommitSha <EXACT_40_CHAR_SHA> -EvidenceDirectory '<EXACT_EVIDENCE_DIR>'
~~~

Synthetic coverage includes confirmation/reset token generation, current-recipient and role/confirmation rechecks, expired-intent suppression, retry/backoff, uncertain-ACK duplicate semantics, overlapping worker lease exclusion, cancellation/lease recovery, dead-letter, operator replay, retention purge and no token/recipient fields at rest.

## Remaining O3 acceptance

- Local Release build and synthetic SQLite contract: NOT RUN.
- PostgreSQL 17 worker execution under the least-privilege runtime role: NOT RUN.
- Concurrent PostgreSQL claim/recovery and provider-parity evidence: NOT RUN.

## Later release gates

Production-mode Identity HTTP lifecycle and abuse/security matrix remain O4 (#33). DataProtection key-ring persistence/backup/restore, ingress, observability, DR and CT164 acceptance remain M5. Public registration, dispatch and production deployment remain OFF/HOLD.
