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

**Status (2026-11-16):** First exact-SHA Release build PASS at `f63149e8908d19945ca3dd5d2345798a92d68e2f`; synthetic contract FAIL at line 76; initial remediation committed at `1618f2c4fdc7f19c9571523b9cd7721877a1b245`; production compile-time dispatch HOLD restored in current HEAD `de7980008c7109e83f9caf396e480c40ae18714d`; remediated rerun and independent Postflight NOT RUN; RELEASE HOLD.

## Implementation

O3 consumes the intent rows from O1 and the dual-provider schema accepted by O2. It adds no schema migration. The worker is an ASP.NET Core hosted BackgroundService inside the existing API host; it does not require a separate container, LXC or service process.

- Provider-neutral, revision-guarded compare-and-swap lease claim. This intentionally replaces the older PostgreSQL-only SKIP LOCKED proposal: a conditional UPDATE with the candidate revision and eligible state is atomic on both SQLite and PostgreSQL, and only one worker can advance that revision. PostgreSQL 17 runtime concurrency proof remains mandatory. Candidate selection and atomic claim use the same eligibility rule, including recovery of a Leased row whose lease deadline is null.
- Enqueue timestamps and worker eligibility use the same injected TimeProvider; the synthetic contract verifies this deterministically instead of relying on wall-clock timing.
- Fresh email-confirmation/password-reset Identity tokens generated at dispatch, never at enqueue and never persisted.
- Re-read current Identity user, email, email-confirmation state and all assigned roles immediately before dispatch.
- Deleted, malformed-email, staff-role, already-confirmed confirmation, and unconfirmed password-reset intents are suppressed.
- Injectable transport. Tests use a fake; production uses the existing STARTTLS SMTP adapter.
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

Dispatch requires both switches and the protected production admin surface. Dispatch cannot be enabled while PublicAccounts is off. No SMTP credentials or live SMTP transport are exercised by the synthetic runner.

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
