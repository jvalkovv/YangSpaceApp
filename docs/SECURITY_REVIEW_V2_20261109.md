# YangSpace v2 — S0-05 Source Security Review

**Date:** 2026-11-09  
**Evidence baseline:** `f3f4e1c9e29a640bab2637fac9ff98098747e41a` — Source QA and real API integration both **PASS** (operator logs).  
**Status:** **PARTIAL / G0 HOLD**. Registration hardening passed runtime QA on `6e8ba7b`. The new bootstrap abuse regression requires its own runtime QA. This is a source review, not a penetration test or public exposure assessment.

## Positive controls observed

- Preview API endpoints are registered only when `Preview:Enabled` is true; startup explicitly rejects preview in Production.
- All `/api/v1/admin/*` routes use the `PlatformAdmin` policy and negative authorization tests passed on baseline `f3f4e1c`.
- Public directory reads require a published parent and filter offers, brochures and jobs by publication/validity.
- Current Angular admin bearer token is held only in component memory (not saved in localStorage).
- Production release is manual and SHA-gated; no GitHub Actions or automatic PVE activation.

## Open findings and remediation

| ID | Severity / gate | Source evidence | Action / current disposition |
|---|---|---|---|
| SEC-01 | P0 / any preview reachable beyond private QA | `Program.cs` maps `MapIdentityApi` under preview, including `/api/auth/register`; Staging name does not establish privacy | **Source change prepared:** `Preview:AllowRegistration=false` default; route-group filter rejects registration unless explicitly opted in. Integration test checks enabled-only disposable phase and denial on restart. Runtime PASS pending |
| SEC-02 | P0 / bootstrap role escalation | `PreviewBootstrap.Initialize` now refuses promotion of any pre-existing ordinary user | **Runtime test source added:** after normal integration, attempt boot with `Bootstrap__AdminEmail` of a registered nonadmin, require startup refusal, restore original admin, assert member still gets HTTP 403 after restart. Await operator evidence |
| SEC-03 | P0 / deployment | Templates use loopback API behind Nginx, but private gateway/VPN/TLS and allowlists are not deployed or verified | No public/Staging exposure; PVE read-only inventory, private ACL/gateway and TLS verification required |
| SEC-04 | P0 / historical secrets | `SECURITY_BASELINE.md` records legacy Git history with DB/JWT/demo credentials | **Rotation NOT VERIFIED**; inventory owners, assess if active, rotate any potentially usable secrets outside Git; never copy secrets into reports |
| SEC-05 | P0 / merchant self-service | Only global PlatformAdmin policy exists; no resource-scoped organization/member/claim workflow | Public merchant access and cross-tenant writes remain BLOCKED; S2 requires membership/ownership and negative cross-tenant tests |
| SEC-06 | P1 / DoS resilience | `Program.cs` has one fixed-window `auth` limiter at 12/minute, non-partitioned | Before Internet exposure, choose partition keys with *trusted* proxy config, add lockout/abuse metrics and negative throttling tests |
| SEC-07 | P0 / persistence / public launch | Preview uses SQLite `EnsureCreated` and fictional seeding | Must stay preview-only; choose supported production provider, EF migrations, backup/restore and retention before public pilot |
| SEC-08 | P1 / auditability | Admin writes lack full audited actor/time/reason event trail | Add structured audit and security event logs without storing secrets/token payloads |

## Approval boundaries

**Accepted at source/API level for baseline `f3f4e1c`:** lockfile integrity, dependency audit, Angular browser QA, .NET build, admin/nonadmin negative auth, create/publish/expire/hide/restart persistence.

**NOT accepted:** new default-deny registration/role hardening runtime, historical credential rotation, exposed network boundary, per-merchant authorization, production persistence, private PVE preview, and public pilot.

**Next gate:** fresh clean-SHA `Verify-Source-Local.ps1` + `Verify-Api-Local.ps1` after hardening commit. Follow with precise SEC-02 negative runtime test and historical secrets decision. Keep PR Draft; no merge or deployment based solely on source review.

## SEC-02 negative regression — newly prepared

The isolated API QA runner persists the nonadmin user's **fictional email** in its disposable state file (not a password). It starts a temporary .NET process on the same random loopback port, switches only the *process-local* admin bootstrap email, and demands that startup fail with the expected promotion refusal. It restores the original bootstrap identity, restarts again and checks login, registration disabled, hidden content and nonadmin HTTP 403. A timeout kills **only the test-owned probe process**. `REAL_API_INTEGRATION=PASS` is impossible until these assertions pass. No trusted credentials or external DB are used. The change is source-only pending fresh Windows PASS.

## SEC-02 Windows file-lock finding (2026-11-09)

The operator's `9fd0a84` test reached the expected bootstrap denial probe after guard/restore/build/prepare/restart verification PASSED. Before inspecting the process's exception text, `ReadAllText` on its redirected `bootstrap-denial.stderr.log` raised a file-in-use exception. The overall integration verdict is **HOLD**, even if the application had actually rejected the escalation; no negative privilege proof can be claimed. The refreshed harness reads both process pipes asynchronously and requires a nonzero exit plus the explicit existing-user refusal; it then restarts the API and tests nonadmin HTTP 403. Latest harness runtime pending. This does not close SEC-03/04 network/legacy secret gates or approve a public pilot.

## 2026-11-09 — S1-07 production CMS source guard

New production entrypoints are explicitly gated by `Admin:Enabled` (default false), ASP.NET Production-only, Preview=false, account enrollment disabled, canonical host and environment-only 32-byte Base64 gateway secret. Bearer-only PlatformAdmin is mandatory for CMS API routes, and an additional internal loopback/gateway-header check is required for CMS and login/refresh. Production only allows bearer login/refresh under Identity API; other Identity paths return 404. This is **defense in depth**, not proof of real authenticated Cloudflare Access/Nginx policy. Origin-side operator identity and protected TLS/ACL still require independent verification.

Production data checks require an existing absolute path `Mode=ReadWrite` SQLite DB outside the deployment tree, at least one reviewed migration with no pending migrations, and a confirmed offline-provisioned PlatformAdmin. Startup does not call `EnsureCreated`, `Migrate` or demo seed in Production. The first-admin offline interactive command is opt-in and cannot upgrade an existing user. **Current repo has no EF migrations**: this deliberately blocks Production CMS startup until generated/reviewed/applied. Preview `EnsureCreated` data cannot be adopted as migrations without a distinct migration strategy. SEC-03/04/07/08 remain OPEN for access, historical credential reuse/rotation, production persistence and audited writes. Source/negative-test scripts have not been executed against the new SHA. G0 HOLD. No public exposure approved.
