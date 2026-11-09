# YangSpace v2 — S0-05 Source Security Review

**Date:** 2026-11-09  
**Evidence baseline:** `f3f4e1c9e29a640bab2637fac9ff98098747e41a` — Source QA and real API integration both **PASS** (operator logs).  
**Status:** **PARTIAL / G0 HOLD**. Current security hardening in the next commit has **not** yet passed runtime QA. This is a source review, not a penetration test or public exposure assessment.

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
| SEC-02 | P0 / bootstrap role escalation | `PreviewBootstrap.Initialize` previously assigned PlatformAdmin to matching existing account | **Source change prepared:** refuse role assignment when account existed without admin role. Need targeted negative runtime test before closing |
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
