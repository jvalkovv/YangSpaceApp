# F2b O3 — durable Identity email outbox worker

**Status:** SOURCE IMPLEMENTED / LOCAL WINDOWS BUILD AND SYNTHETIC QA NOT RUN / INDEPENDENT POSTFLIGHT NOT RUN / RELEASE HOLD.

## Implementation

O3 consumes the intent rows from O1 and the dual-provider schema accepted by O2. It adds no schema migration. The worker is an ASP.NET Core hosted BackgroundService inside the existing API host; it does not require a separate container, LXC or service process.

- Provider-neutral, revision-guarded compare-and-swap lease claim. This intentionally replaces the older PostgreSQL-only SKIP LOCKED proposal: a conditional UPDATE with the candidate revision and eligible state is atomic on both SQLite and PostgreSQL, and only one worker can advance that revision. PostgreSQL 17 runtime concurrency proof remains mandatory.
- Fresh email-confirmation/password-reset Identity tokens generated at dispatch, never at enqueue and never persisted.
- Re-read current Identity user, email, email-confirmation state and all assigned roles immediately before dispatch.
- Deleted, malformed-email, staff-role, already-confirmed confirmation, and unconfirmed password-reset intents are suppressed.
- Injectable transport. Tests use a fake; production uses the existing STARTTLS SMTP adapter.
- Bounded retry: 30 seconds, 2 minutes, 10 minutes, 30 minutes, then 1 hour; terminal failures go to DeadLetter.
- Expired intents are suppressed before dispatch; lease ownership and intent expiry are revalidated immediately before transport.
- Expired leases, including incomplete/null lease metadata, are reclaimable after process restart. Shutdown cancellation leaves the lease to expire and recover.
- SMTP uncertain acknowledgement is **at-least-once**. If SMTP accepted a message but the ACK was lost, a retry can duplicate it. Exactly-once delivery is not claimed.
- Operator dead-letter listing and replay are inside the existing PlatformAdmin and production gateway-protected route group. Responses do not disclose user IDs, email addresses, tokens or payloads. Successful replay emits a structured operator audit event with outbox ID and actor ID only.
- Sent/Suppressed/DeadLetter rows have 30/30/90-day retention windows.

## Fail-closed configuration

Both switches remain false in appsettings:

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
