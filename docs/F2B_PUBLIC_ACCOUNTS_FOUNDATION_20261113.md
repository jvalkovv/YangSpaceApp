## 2026-11-14 13:55 Europe/Sofia — F2b integrated exact-source/build + operator postflight PASS (SOURCE GATE ACCEPTED)

**Tested integrated source SHA:** `113c05ff0a171cc3b6d456af7d9ca6b3224e3bca` (reconciled F2b public Identity with F2a reviewer/PostgreSQL QA). Operator used clean detached worktree, matched origin branch, and supplied Windows output:
- `F2B_EXACT_SOURCE=PASS`, `F2B_STATIC_SECURITY_INVARIANTS=PASS`.
- All five source build stages PASS: .NET API Release restore/build, Angular npm ci, typecheck, production build.
- `F2B_PUBLIC_IDENTITY_SOURCE_ONLY=PASS`.
- Subsequent separate independent operator postflight `F2B_INTEGRATED_SOURCE_POSTFLIGHT=PASS`: manifest exact SHA and mandatory PASS/NOT_TESTED/HOLD lines, 5×3 command/stdout/stderr receipts, no failure gate, both `PublicAccounts:Enabled=false` and `ClaimReviewPostgreSqlQa:Enabled=false`, clean worktree.
- **Evidence:** `D:\GitHub\YangSpaceApp-F2b-Integrated-QA-20261114\logs\yangspace\f2b-public-identity\113c05ff0a17-20261114-135340-556e84a0`.
- **Manifest SHA-256:** `5d156889d1e9f54e0923d880427f2d777fe2da244b3c417eb7f8bced87061700`. Logs and manifest bytes remain operator-local; this records verified operator transcript, not direct independent download of local evidence.

**Accepted scope = integrated F2b SOURCE GATE ONLY.** It is 100% for the bounded static security/build/typecheck gate at the tested SHA. The doc-only follow-up to record this milestone must **not** be represented as a new application-code QA run.

**Explicit open blockers:** `PUBLIC_IDENTITY_HTTP=NOT_TESTED`, `PUBLIC_SMTP_DELIVERY=NOT_TESTED`, `OWNER_GRANT=NOT_IMPLEMENTED_NOT_AUTHORIZED`, `CT164_DB=NOT_ACCESSED`, `RELEASE_GATE=HOLD`. Additional P0: PostgreSQL TestServer with non-superuser connection/TLS VerifyFull and reviewed migrations, realistic confirmation/lockout/recovery/role/anti-enumeration matrix, injectable fake email/outbox delivery failure + retry policy, browser privacy/accessibility and production ingress/backups. Issue #24 legal/privacy blocks real business evidence. PR #29 and #17 remain OPEN/DRAFT/HOLD; #1 stays Draft. No runtime deploy, email sends, real registrations or production DB changes.

**Next actionable work:** add scoped disposable F2b PostgreSQL Identity HTTP/email contract and QA runner on PR #29 in a separate code commit, then run new exact-SHA Windows acceptance and operator postflight. Keep default-OFF throughout.

---

## 2026-11-13 — F2b exact-SHA Windows operator source QA PASS

**Exact source SHA:** `3ac9df42023ac36bf0fe970d9cf353bc501dba3d`. **Result: SOURCE BUILD/TYPECHECK PASS, NOT HTTP/SMTP/PRODUCTION ACCEPTANCE.**

Operator transcript (17:52 local):
- `F2B_EXACT_SOURCE=PASS` and `F2B_STATIC_SECURITY_INVARIANTS=PASS`.
- `STAGE_PASS=f2b-api-restore`, `STAGE_PASS=f2b-api-build` (Release).
- `STAGE_PASS=f2b-web-npm-ci`, `STAGE_PASS=f2b-web-typecheck`, `STAGE_PASS=f2b-web-build` (Angular production).
- `F2B_PUBLIC_IDENTITY_SOURCE_ONLY=PASS` and independent `F2B_SOURCE_POSTFLIGHT=PASS` (manifest markers and no failure gate).

**Operator-local evidence:** `logs/yangspace/f2b-public-identity/3ac9df42023a-20261113-175209-a7bdefdb`.  
**Manifest SHA-256:** `829E2B8A9BF07FB22FCA9368F89F30D86AD214ED614FFD601F3D5F35062D5122`.

**Independent evidence limit:** Manifest SHA and stage PASS were provided in the operator console transcript; the file bytes/logs remain operator-local and were not independently fetched by the GitHub connector. This PASS is valid for the tested source SHA; a documentation-only follow-up commit must not relabel it as a new runtime test.

**Next required release gates:** disposable PostgreSQL TestServer with fake email transport and a full registration/confirmation/login/reset/role/lockout/anti-enumeration matrix; backend mail delivery failure/outbox durability; approved privacy notice and lawful basis, browser/mobile/ingress, off-host restore and CT164 deployment review.

**PR #29: OPEN/DRAFT/HOLD**, source compile gate achieved; do not merge until all scoped DoD accepted. `PublicAccounts:Enabled=false` remains default, no production deployment. Issue #24 separately blocks real merchant evidence.


# F2b — Public ASP.NET Core Identity foundation (P0)

**Date:** 2026-11-13  
**Tracking:** Issue #28 — https://github.com/jvalkovv/YangSpaceApp/issues/28  
**Status:** EXACT-SHA WINDOWS SOURCE BUILD/TYPECHECK PASS on `3ac9df42023ac36bf0fe970d9cf353bc501dba3d` / HTTP+SMTP NOT TESTED / NOT DEPLOYED / DRAFT-HOLD.

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

1. **ACCEPTED (source only):** exact-SHA `scripts/Test-F2b-Public-Identity-Source-Local.ps1` at `3ac9df42023ac36bf0fe970d9cf353bc501dba3d`. .NET restore/Release build, npm ci, Angular typecheck and production build all PASS; independent postflight PASS; SHA-256 `829E2B8A9BF07FB22FCA9368F89F30D86AD214ED614FFD601F3D5F35062D5122`. No PostgreSQL/SMTP/CT access.
2. Disposable PostgreSQL TestServer matrix: default disabled; duplicate account non-enumeration; confirmation token expiry/reuse; login denied until email verification; bearer-only account session; staff and future-role exclusion; lockout/rate limiting; reset flow; wrong tokens; concurrency.
3. Mock mail sink + real outbound SMTP integration, delivery/failure/timeout/retry and queue/outbox decisions. Request-triggered SMTP is not a durable queue; an outbox/worker and operational alerting are required for release.
4. Privacy notice and lawful-basis review for actual account data. Real merchant evidence or Owner claims continue to depend on Issue #24 legal/privacy clearance.
5. Protected frontend entry, keyboard/mobile/screen reader QA at 320/375/390/768/1440 and production origin ingress security review.
6. CT164 manual deploy/rollback, least-privilege DB account, off-host restore and monitoring acceptance. No GitHub Actions.

**No production or public-account release is authorized by source compile.**
