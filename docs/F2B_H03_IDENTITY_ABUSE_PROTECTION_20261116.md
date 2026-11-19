# F2b H03 — Identity application abuse protection

**Scope:** application code, persistence, Identity endpoints and isolated runtime tests only. No proxy, Nginx, Cloudflare, firewall, production deployment or public activation.

## Existing controls retained

- ASP.NET auth transport-peer rate limiter: 12/minute, zero queue, HTTP 429.
- Identity password lockout: five failed attempts, 15-minute lockout.
- Generic responses for resend-confirmation and forgot-password.
- Outbox pending-intent coalescing and worker retry/recovery.
- Staff identities remain excluded from public account actions.

## H03 account-keyed throttles

| Operation | Limit | Window | Denial response |
|---|---:|---:|---|
| Register | 3 | 24 hours | HTTP 202 Accepted |
| Resend confirmation | 3 | 1 hour | HTTP 202 Accepted |
| Forgot password | 3 | 1 hour | HTTP 202 Accepted |
| Reset password | 5 | 15 minutes | Existing generic HTTP 400 |

The account key is normalized through ASP.NET Identity's `ILookupNormalizer`, then transformed with HMAC-SHA256 using `PublicAccounts:AbuseProtection:HmacKey`. Only the 64-character digest is persisted. Raw email addresses, tokens and request payloads are never stored in the counter table or emitted in throttle logs.

The HMAC key must be supplied through protected runtime configuration, Base64-encoded and at least 32 bytes. Every API instance sharing the same database must use the same key. It is required only when `PublicAccounts:Enabled=true`. Rotating the key intentionally resets account throttle history; treat rotation as a security operation.

## Persistence and concurrency

- SQLite and PostgreSQL each receive a provider-specific EF migration.
- Counter increments are single-statement atomic UPSERTs with `RETURNING Count`; concurrent app instances share the same database budget.
- Fixed windows are aligned to UTC epoch boundaries.
- Old windows are opportunistically purged after two days, at most once per process every six hours.
- When `PublicAccounts:Enabled=true`, the identity runtime database role requires SELECT/INSERT/UPDATE/DELETE only on `IdentityAbuseCounters`; no schema ownership or migration rights. Production startup checks these four privileges and fails closed if any are missing. When public accounts are disabled, generic runtime readiness does not require or probe access to this feature's counter data.

## Boundaries and honest claims

- The existing IP limiter still keys on `Connection.RemoteIpAddress`. Behind a proxy this may be the proxy peer; H03 does not configure forwarded headers or claim public per-client IP isolation.
- Account-keyed controls add distributed protection for a target account regardless of source IP.
- Registration, email delivery and public dispatch remain governed by the existing signed release authorization and default-off switches.
- Real SMTP, CT164 and Production DB are not used by H03 QA.


## 2026-11-17 — H01 readiness feature-dependency correction

The O4-H01 TestServer run reached the real Production `/health/ready` route, then failed with PostgreSQL SQLSTATE `42501` on `public."IdentityAbuseCounters"`. This was not a missing broad production grant: the H01 runtime role is intentionally a non-identity, default-off principal and must not receive account-abuse data access while `PublicAccounts:Enabled=false`.

Root cause: `ProductionSchemaGate.ValidateRuntimeReadinessAsync` queried `IdentityAbuseCounters` unconditionally, although the feature's full SELECT/INSERT/UPDATE/DELETE privilege gate is only required when public accounts are enabled.

Correction in current source:
- Pass the actual `publicAccounts` feature switch into runtime readiness.
- Keep DB connectivity, migrations/model-drift, Identity user columns and outbox readiness checks.
- Probe `IdentityAbuseCounters` only when `PublicAccounts:Enabled=true`; the existing startup gate still requires all four table privileges in that state.
- Add an H01 regression assertion that `ys_o3_runtime` has **no SELECT grant** on the abuse table while public accounts are off, and that Production readiness still returns HTTP 200.
- Runner manifest and Independent Postflight require the new negative-privilege marker.

This preserves least privilege; no generic table grants or production role expansion. Exact-SHA rerun and Independent Postflight remain pending. PR #56 stays Draft; Issues #50/#55 stay OPEN; release HOLD.
