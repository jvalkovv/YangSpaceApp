# F2b — Public ASP.NET Core Identity foundation (P0)

**Date:** 2026-11-13  
**Tracking:** Issue #28 — https://github.com/jvalkovv/YangSpaceApp/issues/28  
**Status:** SOURCE COMMITTED / NOT WINDOWS-BUILT / NOT DEPLOYED / DRAFT-HOLD.

## Architecture

- Public directory browsing remains anonymous. Account endpoints use a dedicated /api/public-account namespace, not the protected admin /api/auth gateway.
- Uses existing ASP.NET Core Identity, EF user persistence and Identity bearer scheme; no Entra/OAuth, no new account schema or migration.
- New accounts start EmailConfirmed=false, with no Identity role and no tenant membership. Public login rejects accounts holding *any* Identity role. Ownership never follows from signup.
- Registration password policy is checked before account lookup to limit username enumeration. Duplicate usernames result in a generic Accepted response.
- Public sign-in requires confirmed email and lockout. No cookies; Angular keeps access tokens only in memory (not localStorage, URL or refresh persistence).
- Registration email uses a canonical https://yangspace.gndstore.eu confirmation URL, never an untrusted request Host. SMTP settings must be supplied at runtime; STARTTLS on 587 is mandatory.
- PublicAccounts:Enabled is false in committed defaults. Enabling requires Production, PostgreSQL, explicit AllowedHosts, exact canonical origin and complete SMTP settings. No production configuration is changed by this PR.
- Angular routes: /login, /register, /recover, /reset, /resend-confirmation, /account. All are draft UI awaiting runtime acceptance.

## Acceptance still required

1. Run exact-SHA script/Test-F2b-Public-Identity-Source-Local.ps1. It checks source policy, .NET restore/Release build, npm ci without install scripts, Angular typecheck and production build; no DB, SMTP, Docker or CT access.
2. Disposable PostgreSQL TestServer matrix: default disabled; duplicate account non-enumeration; confirmation token expiry/reuse; login denied until email verification; bearer-only account session; staff and future-role exclusion; lockout/rate limiting; reset flow; wrong tokens; concurrency.
3. Mock mail sink + real outbound SMTP integration, delivery/failure/timeout/retry and queue/outbox decisions. Request-triggered SMTP is not a durable queue; an outbox/worker and operational alerting are required for release.
4. Privacy notice and lawful-basis review for actual account data. Real merchant evidence or Owner claims continue to depend on Issue #24 legal/privacy clearance.
5. Protected frontend entry, keyboard/mobile/screen reader QA at 320/375/390/768/1440 and production origin ingress security review.
6. CT164 manual deploy/rollback, least-privilege DB account, off-host restore and monitoring acceptance. No GitHub Actions.

**No production or public-account release is authorized by source compile.**
