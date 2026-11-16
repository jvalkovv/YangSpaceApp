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

The HMAC key must be supplied through protected runtime configuration, Base64-encoded and at least 32 bytes. It is required only when `PublicAccounts:Enabled=true`. Rotating the key intentionally resets account throttle history; treat rotation as a security operation.

## Persistence and concurrency

- SQLite and PostgreSQL each receive a provider-specific EF migration.
- Counter increments are single-statement atomic UPSERTs with `RETURNING Count`; concurrent app instances share the same database budget.
- Fixed windows are aligned to UTC epoch boundaries.
- Old windows are opportunistically purged after two days, at most once per process every six hours.
- The runtime database role requires SELECT/INSERT/UPDATE/DELETE only on `IdentityAbuseCounters`; no schema ownership or migration rights.

## Boundaries and honest claims

- The existing IP limiter still keys on `Connection.RemoteIpAddress`. Behind a proxy this may be the proxy peer; H03 does not configure forwarded headers or claim public per-client IP isolation.
- Account-keyed controls add distributed protection for a target account regardless of source IP.
- Registration, email delivery and public dispatch remain governed by the existing signed release authorization and default-off switches.
- Real SMTP, CT164 and Production DB are not used by H03 QA.
