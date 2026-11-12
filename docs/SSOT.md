# YangSpace 2.0 — Single Source of Truth (SSOT)

**Status:** ACTIVE planning/status authority on the YangSpace V2 feature branch; introduced by merged PR #9. This document does **not** authorize deployment, database mutation, exposure, or feature-to-`main` merge.  
**Baseline date:** 2026-11-11 (Europe/Sofia).  
**Latest accepted app-code engineering QA source:** `feat/yangspace-publication-timestamps-f1b-20261111` @ `2634014f64fcff237b29564d0572643f2cba034a` (three exact-SHA Windows gates PASS); squash-merged to V2 as `403b281abad2fb0c8048c2763a606c3435f3e5dd`, not separately re-run after squash. Historical CT164/CT152 deployment remains `a4802805421f10e834ad35bbf2f259db4e4eab84`. [Parent PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) stays Draft. `main` remains the legacy application.  
**Scope of this update:** repository/source review, accepted operator-reported evidence, project documentation and user-supplied iPhone screenshots; **no new build, real-browser, DB, PVE, or public-origin execution** was performed for this document.

> **Authoritative summary:** strong design and functional *preview foundation*; real multi-business/owner workflows, public-pilot content governance, SEO, ingress protection, observability and off-host recovery are not accepted. **G0, G1, G2, G3, G4, G-PREVIEW and G-PILOT remain HOLD at this reviewed source baseline.** Do not infer percentages or production readiness from code, screenshots, HTTP 200 or historical PASS markers.

## 2026-11-12 08:02 Europe/Sofia — F2a-2 bearer HTTP acceptance

- Exact tested child SHA: \`f579c70db9cc0986d2f1df8047158587f0e87eef\`, [Draft PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19), stacked on F2a-1 \`1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001\`.
- Operator Windows output: restore PASS, Release build warnings-as-errors PASS, tenant regression PASS, actual Identity bearer HTTP TestServer PASS; final \`F2A2_MERCHANT_BEARER_HTTP=PASS\`.
- Evidence path: \`D:\\GitHub\\YangSpaceApp-F2a2-Bearer-QA\\logs\\yangspace\\f2a2-merchant-http\\f579c70db9cc-20261112-075949-ea648860\`. Runner source confirms that it checks the required marker list and writes \`manifest.txt\` only after all stages/assertions pass. **Actual local manifest/log bytes have not yet been independently reviewed.**
- Scope: disposable SQLite/TestServer only; real Identity-issued bearer, 401/404, cross-tenant isolation, membership revocation and org suspension with same token, default-off/QA environment fail-closed, no registration/cookies/mutations, safe DTO/no side effects. No PostgreSQL HTTP claim.
- Source review of the bounded #19 diff found no blocking defect in the QA gate, Bearer-only policy, live membership JOIN, DTO allowlist, fixture setup or exact-SHA runner. Independent reviewer and evidence postflight remain outstanding.
- #19 remains Draft/HOLD pending evidence postflight and review; #17 remains Draft/HOLD pending real PostgreSQL bearer/parallel writer/audited mutation and least-privilege gates; #1 remains Draft/HOLD for program release gates. **0 open PRs eligible for closure.**
- Next: read-only manifest/log verification; then owner-approved merge decision for #19. After that, implement reviewer-only claim verification and membership grant/revoke with atomic audit and DB fault-injection as a separate scoped gate. No Actions, CT164/CT152 access, live DB, migration or deployment.

## 2026-11-12 07:49 Europe/Sofia — superseding F2a-2 HTTP QA status

- Windows operator exact HEAD `ceea4723ccc5d2fc7612e63ec3ff4239b37ea0b8` verified **PowerShell syntax, .NET restore, Release build and prior tenant regression PASS**, then HTTP fixture startup **FAIL** at API `Program.cs:46`: no `ConnectionStrings:Main` during early `WebApplication.CreateBuilder`. The fixture's late `ConfigureAppConfiguration` injection missed the startup guard. No real bearer HTTP tests completed.
- New corrective test-only [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) HEAD `f579c70db9cc0986d2f1df8047158587f0e87eef`: process-scoped synthetic SQLite + explicit QA mode **before host creation**, with restoration of process configuration and preserved fail-closed Production/Development checks. **New exact-SHA runtime QA NOT RUN**.
- [Roadmap](MVP_ROADMAP.md#2026-11-12-0749-europesofia--f2a-2-http-testserver-startup-recovery) owns current recovery gate. Open PRs #19, #17, #1: all Draft/HOLD, none 100%-accepted for merge. F2a-1 PostgreSQL synthetic recovery PASS stays scoped to `1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001`. No production database, CT164/CT152 or preview activation changed.

## 2026-11-12 07:39 Europe/Sofia — latest F2a progress and PR inventory

**Canonical milestone source:** [MVP roadmap progress/PR-closure checkpoint](MVP_ROADMAP.md#2026-11-12-0739-europesofia--f2a-2-build-failure--recovery-and-pr-closure-review). Earlier F2a "proposed/not implemented" paragraphs below are historical snapshots, not the current implementation status.

- **F2a-1 PostgreSQL disposable QA:** PASS at `1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001`; schema, constraints, sequential CAS, DB audit rollback, backup/restore and evidence integrity/cleanup verified from operator console. Not a production rollout.
- **F2a-2 real merchant Identity bearer HTTP:** [Draft PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) at `ceea4723ccc5d2fc7612e63ec3ff4239b37ea0b8`. Prior HEAD `62505864c934df1536194eb3bdc07eab29af648e` passed runner syntax/restore but failed Release build with test-only `CS0136` duplicate `owner` and `editor` locals. New commit renames fixture locals only. **Exact-new-HEAD build, tenant regression and actual bearer HTTP QA remain NOT RUN / HOLD.**
- **Open PR inventory:** #19 Draft/HOLD (HTTP QA), #17 Draft/HOLD (reviewed claims/real PG runtime/auth and release security), #1 Draft/HOLD (full program/release gates). **No open PR accepted 100% for merge/closure** at this checkpoint.
- **Standing governance:** meaningful in-conversation progress reports with full SHA and real evidence; review all open PRs at closure checkpoints; only owner-approved 100%-accepted scoped PRs may be merged/closed. Policy in merged PR #18 and [MVP roadmap](MVP_ROADMAP.md#delivery-governance--mandatory-progress-reporting--pr-closure-accepted-2026-11-11).
- **No exposure:** no YangSpaceQa authorization enabled on CT164, no active DB changes, no migrations applied to CT164, no production/preview activation or GitHub Actions. Next is fresh operator Windows exact-SHA QA and evidence review.

## F2a architecture work — 2026-11-11 (proposed, review pending)

**Status: SOURCE REVIEW / DESIGN PROPOSAL ONLY**, implementation gates remain HOLD. The current API has only global `PlatformAdmin`; Organization/Member/Claim rows and actor+revision audit are absent. [Issue #4](https://github.com/jvalkovv/YangSpaceApp/issues/4) owns tenant authorization, [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11) owns the full freshness/audit requirements. Proposed detailed contract: [F2a ownership and audit](F2A_TENANT_OWNERSHIP_AND_AUDIT_CONTRACT_20261111.md). **ADR-019 accepts PostgreSQL as the production target, but provider migration/activation is HOLD**; no new EF migration or owner endpoint enabled. F1a/F1b remain accepted for local code QA only, public demo unchanged.

## 2026-11-11 18:22 Europe/Sofia — current accepted F1 freshness milestone

This is the **current source/PR status**, superseding dated historical QA snapshots below. The V2 source baseline following F1b approval is `403b281abad2fb0c8048c2763a606c3435f3e5dd`. Additional documentation-only commits may advance Git HEAD without changing the tested application source.

| Workstream | Scoped source / QA status | Release boundary |
| --- | --- | --- |
| [F1a — public freshness (#13)](https://github.com/jvalkovv/YangSpaceApp/pull/13) | **MERGED/CLOSED**; candidate `9656518bb3d795a1e8cfaa7c3004678ee27fa6d4`, `SOURCE_VALIDATION=PASS`, `REAL_API_INTEGRATION=PASS`; squash `54b47576ab8206d599f837cd99340c008d49f0d6` | Only public read-only detail rendering and Bulgarian timezone/validity, **not deployed** |
| [F1b — publication timestamps (#14)](https://github.com/jvalkovv/YangSpaceApp/pull/14) | **MERGED/CLOSED**; candidate `2634014f64fcff237b29564d0572643f2cba034a`, `SOURCE_VALIDATION=PASS`, `REAL_API_INTEGRATION=PASS`, `SQLITE_RUNTIME_INTEGRITY=PASS`; squash `403b281abad2fb0c8048c2763a606c3435f3e5dd` | 9 nullable migration columns, lifecycle timestamps and EF initial→F1b disposable DB upgrade/recovery proved locally; **not applied to CT164** |
| [Issue #11 — F2 actor/field audit](https://github.com/jvalkovv/YangSpaceApp/issues/11) | **NOT STARTED** — needs authenticated organization/member scope, atomic audit and optimistic revision | Do not allow owner writes with PlatformAdmin as proxy for real organization ownership |
| Issue #11 — F3 owner confirmation/moderation | **NOT STARTED**, depends on F2 | No verified merchant freshness badge or unmanaged self-service |
| [V2 → legacy main parent PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) | **DRAFT/OPEN**, only open application PR after #14 | Full architecture, PVE security, off-host restore, release and pilot gates remain **HOLD** |

**F1b exact candidate acceptance evidence (operator-local, not copied into GitHub):**
- `D:\GitHub\YangSpaceApp-F1b-Scaffold\logs\yangspace\verify\2634014f64fc-20261111-181828-f2898981`
- `D:\GitHub\YangSpaceApp-F1b-Scaffold\logs\yangspace\api-integration\2634014f64fc-20261111-181923-5dbe2df0`
- `D:\GitHub\YangSpaceApp-F1b-Scaffold\logs\yangspace\sqlite-runtime-integrity\2634014f64fc-20261111-181244-81cbfb42`

**Current operations:** no release, live database apply, CT164/CT152 change or tenant authorization acceptance from PRs #13/#14. Existing public demo remains the earlier source release `a480280...` per previous operator evidence. Source QA is not production gate acceptance. Use [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11) for the sequenced F2/F3 work contract. Historical status notes below remain archival.

## 2026-11-11 runtime status addendum — operator-provided evidence

**Why this addendum exists:** the source snapshot at `a4802805` and the historical CT164 provisioning log were written **before** the owner activated the application and public reverse proxy. The older "application not installed / CT152 unchanged" statements remain historically correct **at their timestamps**, but must not be used as the current live-state assessment.

**Evidence provenance:** console output pasted by the operator into the YangSpace conversation from `root@pve` on **2026-11-10**, plus the independent **read-only** Cloud Browser audit reported on 2026-11-11. This is **not** an independent SSH read or a persisted, signed release attestation. No credentials, API writes, database contents, or infrastructure mutations were used to prepare this documentation.

| Layer | Updated observation | Acceptance boundary |
| --- | --- | --- |
| CT164 application release | Operator's guarded shell verified `/srv/yangspace/current` resolves to `/srv/yangspace/releases/a4802805421f10e834ad35bbf2f259db4e4eab84`; `yangspace-preview.service` active; demo SQLite present. | **Operator-reported runtime PASS**, not cryptographic binary-to-source attestation. |
| CT164 internal web/API | Local Nginx `/ = 200`, `/api/v1/homepage = 200`; local CT-IP request denied `403`; registration negative probe `404`. | Scoped preview/read-only smoke; broader direct-origin ACL and auth negative matrix remain unverified. |
| CT152 proxy and HTTPS | Operator output: `nginx -t` PASS, YangSpace virtual host present and enabled, `/ = 200`, `/api/v1/homepage = 200`; admin/auth/health paths `404`, public API POST `403`. CT152 service journal records successful reload. | Public **read-only demo** route active, not production merchant access. |
| Public Cloudflare endpoint | Operator's external `curl https://yangspace.gndstore.eu/` reported `200` and `/admin` reported `404`; Work Cloud Browser reported 6 detail pages, search, filters, retry and 375–1920 px responsive smoke. | Live browser smoke only. No signed client build fingerprint, backend auth with credentials, cross-browser proof or load/WCAG certification. |
| Operational/release readiness | PVE thin pool reported `84.76%` during activation; effective firewall, off-host encrypted backup + restore, monitoring and rollback evidence still incomplete. | **G-PREVIEW remains HOLD** as a comprehensive operational gate; **G-PILOT remains HOLD**. Browser reachability is not approval for real merchant content or writes. |

**Current interpretation:** the apparent *live-versus-provisioning mismatch* is explained by the historical log's age, not a demonstrated alternative origin. The **remaining release identity gap** is to verify the exact running binary/assets against immutable release SHA and publish a safe build fingerprint. Preserve the recorded operator console evidence and close this gap with independent, read-only verification before a later deployment. **Do not repeat activation or mutate CT152/CT164 to update documentation.**

## 2026-11-11 — source QA, communication and PR closure addendum

**Exact-SHA manual acceptance:** operator ran `Verify-Source-Local.ps1` and `Verify-Api-Local.ps1` from Windows on `2baeb797b4d9ca0f5ce9d6c3d311a7b8092be7dc`. Console transcript reported `SOURCE_VALIDATION=PASS`, `REAL_API_INTEGRATION=PASS`, `BOOTSTRAP_PRIVILEGE_ESCALATION_DENIED=PASS`, `BOOTSTRAP_NONADMIN_ROLE_RETAINED=PASS`. Source runner passed lock/install/audit/typecheck/build/Playwright/API restore/build; API runner passed disposable database prepare/restart/anti-escalation/recovery checks. No live DB writes. Local evidence, not independently read by this GitHub-only update:

- `D:\GitHub\YangSpaceApp\logs\yangspace\verify\2baeb797b4d9-20261111-074951-04b1acd1`
- `D:\GitHub\YangSpaceApp\logs\yangspace\api-integration\2baeb797b4d9-20261111-075201-77648186`

**PR reconciliation:** [#9](https://github.com/jvalkovv/YangSpaceApp/pull/9) MERGED/CLOSED at `ea959a104275599212156dc4d9f1ce76adcf13f8`; [#10](https://github.com/jvalkovv/YangSpaceApp/pull/10) MERGED/CLOSED at `2baeb797b4d9ca0f5ce9d6c3d311a7b8092be7dc`. At this status checkpoint, only **[#1](https://github.com/jvalkovv/YangSpaceApp/pull/1)** remains Draft/Open; it is **not 100% complete** because merchant ownership, real data, recovery and production gates are not accepted. New doc-only PRs are handled as isolated work and reconciled after merge.

**Mandatory progress reporting:** report meaningful GitHub commits, tests, PR transitions, release gates and blockers promptly in the active conversation; the existing hourly YangSpace GitHub check notifies only on substantive change. Always cite actual source SHA and test/evidence scope. No promise of continuous independent execution. **PR hygiene:** merge/close fully accepted, owner-approved scoped PRs; keep incomplete PRs open/draft; do not close unmerged merely to clear the list. See [MVP roadmap](MVP_ROADMAP.md#delivery-governance--mandatory-progress-reporting--pr-closure-accepted-2026-11-11).

**Gate verdict:** current engineering source QA **PASS for `2ba...`**; comprehensive G0/G-PREVIEW/G-PILOT **remain HOLD** for outstanding security/operational/product requirements. No deployment has occurred from this candidate; CT164 public demo is still reported at `a480...`.

## 1. Purpose, precedence and change control

This file is the **single entrypoint** for *current state, gate status, sequencing and unresolved decisions*. Detailed execution and evidence remain in the linked specialist records below. Do not maintain another conflicting top-level status list.

| Authority / purpose | Source | Rule |
| --- | --- | --- |
| Accepted owner decisions | [Decision log](DECISIONS.md), especially [ADR-018](ADR-018-NO-GITHUB-ACTIONS.md) | An accepted ADR controls policy until explicitly superseded by another approved ADR. |
| **Current cross-project status and execution order** | **This SSOT** | Must be updated after validated milestone/gate changes, linking exact evidence. |
| Detailed work items and implementation state | [POC tracker](POC_TRACKER.md) | Task IDs, dependencies, assigned responsibility, evidence location; align with this SSOT. |
| Objective PASS/FAIL criteria | [Acceptance matrix](POC_ACCEPTANCE_MATRIX.md) | A source change or mocked test cannot substitute for required real-runtime evidence. |
| Product vision and scope | [Product](PRODUCT.md), [Master Plan](POC_MASTER_PLAN.md), [Domain Model](DOMAIN_MODEL.md) | Design targets are not evidence of implementation. Historical “current” summaries are superseded by this file. |
| Operator evidence / procedures | [CT164 log](CT164_PROVISIONING_20261110.md), [release runbook](POC_PVE_RELEASE_RUNBOOK.md), [SQLite integrity report](S2_SQLITE_RUNTIME_INTEGRITY_20261110.md) | Dated observations remain historical facts; older “no CT exists” or TODO inventories must not override later accepted evidence. |
| Code reality | Reviewed Git commit SHA | Actual behavior must be checked in source **and** in the deployed release; a PR description is not runtime proof. |

**Evidence hierarchy:** direct exact-SHA runtime logs + reviewer acceptance > validated operator-provided test records > source inspection > browser screenshots > proposal/narrative. A stale document is not evidence that an implemented feature has disappeared; conversely a code commit is not evidence that it runs correctly. Mark unknown facts **UNVERIFIED**, unmet gates **HOLD**, and confirmed failures **FAIL/BLOCKED**. Separate *source-implemented*, *tested locally*, *verified on CT164*, and *accepted for public release*.

**Policy:** No GitHub Actions/hosted CI, webhook auto-deploy, blind `git pull` activation, unreviewed PVE writes or silent merge. Use clean exact-commit, manual/operator-run QA and retained per-app logs per [ADR-018](ADR-018-NO-GITHUB-ACTIONS.md). Work in isolated feature/docs branches; reconcile to the active feature HEAD before merging. **Never modify the active CT or database merely to synchronize documentation.**

## 2. Accepted product boundaries

- **YangSpace** = hyperlocal directory of **businesses, offers, brochures and local jobs**, starting with one Varna neighborhood, then Varna, then other cities once pilot gates pass.
- Visitors browse without mandatory registration. Verified business owners/editors manage only their own organizations/locations after a controlled claim/approval flow. Moderators manage reviews/reports/audit.
- Target stack: Angular 22 + ASP.NET Core 10/EF Core; modular monolith; controlled PVE single application LXC initially, Nginx/systemd and separated persistent data/release/config.
- **Out of MVP:** booking, integrated payments, paid subscriptions as a predetermined tariff, native iOS/Android apps, CV database/ATS, AI recommendations and bulk uncontrolled imports.
- **No invented data:** demo entries, offers, time schedules and badges must be clearly identified; never claim “verified”, “open now”, actual proximity or authentic promotions without evidence.
- **Content freshness and accountable edits:** [Issue #11 — business freshness, brochure validity, actor/field-level audit and owner confirmation](https://github.com/jvalkovv/YangSpaceApp/issues/11) is the accepted requirement contract. Public expired-brochure filtering and location-level `UpdatedAtUtc` exist, but they do **not** prove owner verification or field-level publication history. Implement per-entity timestamps, server-attributed audit, ownership and approval in reviewed slices **before enabling merchant CMS writes**.
- Initial 10–20 merchant invite/claim pilot is a **proposal**, not an approved target. Agree with product owner before outreach.

## 3. Historical source-review snapshot (pre-activation; superseded by the runtime addendum above)

| Layer | Verified/implemented evidence | Qualification |
| --- | --- | --- |
| Repository | `main` = legacy; Draft PR #1 targets `main` from `feat/yangspace-v2-foundation-20261108`; reviewed HEAD `a4802805`. | This docs branch is isolated. Feature branch may advance independently. |
| Frontend foundation | Home/Detail/Admin Angular components, category/search, pagination, skeleton/error/retry states; green/cream visual language. | Historical 17/17 Playwright PASS on `20aa30c` with **mocked API**. Not proof for reviewed HEAD. |
| Real local engineering | Operator-reported clean source/API integration PASS at `b30c1c8`; committed npm lockfile and .NET build records. | QA does **not** automatically transfer to later HEAD; repeat exact-SHA validation. |
| Persistence | Reviewed EF Core initial migration committed; FK runtime checks and isolated DB backup/restore local regression reported PASS at `2069eb9` / `1ce3fa3`. | **Disposable Windows DB only**; not public database cutover or off-host operational restore. |
| Public directory | Published locations, facets, search, active offers/brochures/jobs, detail routes; six fictional seeded examples. | City/geo coordinates, distance search, owner verification, rich opening hours, real photos and indexable SEO not complete. |
| Admin CMS | PlatformAdmin bearer policy; source APIs for create/edit/hide/restore locations and publications; offline admin provisioning and production startup guards. | Owner portal, cross-tenant permissions, moderation/audit and exact-HEAD real CMS browser/DB acceptance are missing. |
| CT164 guest | Operator reported Debian 13.6 unprivileged LXC; systemd running; .NET 10 runtime; dedicated `yangspace`/`yangdeploy` identities and actual positive/negative directory ACL PASS; DHCP .110, gateway/DNS .102 persisted after reboot. | DHCP **server identifier .107 unexplained**; PVE firewall effective protection, storage headroom, independent backups, application release, CT152 ingress and full recovery are not accepted. |
| Public URL screenshots | User-supplied iPhone screenshots of `yangspace.gndstore.eu` show Hero, menu, search, category select, cards and detail. | **Origin, environment, deployed SHA, DB provenance, effective access control and exact viewport are UNVERIFIED.** A visible URL/HTTP 200 is not a CT164 deployment acceptance. |

**Important drift reconciliation:** Older [PVE runbook](POC_PVE_RELEASE_RUNBOOK.md) opening text (“no CT provisioned”) predates the confirmed CT164 guest work and is **SUPERSEDED for infrastructure inventory status** by the later [CT164 evidence](CT164_PROVISIONING_20261110.md). The runbook's security/release procedure remains applicable. The [production CMS contract](PRODUCTION_CMS_SECURITY_20261109.md) also includes a historical “no migration committed” statement superseded by the actual [migration evidence](S2_INITIAL_MIGRATION_20261110.md); **runtime production schema adoption remains HOLD**.

## 4. Product/architecture findings requiring resolution

### P0 — release/architecture boundaries

1. **Database ADR conflict:** [Master Plan](POC_MASTER_PLAN.md) / [DECISIONS](DECISIONS.md) leave PostgreSQL vs SQL Server open (ADR-007/013); current [Models.cs](../apps/api/Models.cs), [Program.cs](../apps/api/Program.cs), migration and [ProductionSqlitePolicy.cs](../apps/api/ProductionSqlitePolicy.cs) explicitly implement SQLite even for guarded Production startup. **Decision required before G2:** accept SQLite as a bounded single-instance pilot with explicit concurrency/backup/data growth limits, or adopt a reviewed PostgreSQL/SQL Server strategy. No silent provider flip and no real DB conversion as a side effect.
2. **Missing multi-tenant domain:** `DirectoryDb` contains Identity, BusinessLocation, Offer, Brochure, Job, but no Organization/OrganizationMember/BusinessClaim, scoped ownership or cross-tenant API policies. The existing admin is a global PlatformAdmin, **not** a merchant CMS.
3. **Content governance missing:** public boolean publication is a start, not a full draft → review → publish → expire/archive/reject/suspend lifecycle; no persisted actor/reasoned audit, business reports or vetted upload pipeline.
4. **Infrastructure safety:** CT164 guest preparation PASS does not establish protected CT152→CT164 route, direct-origin denial, PVE firewall, trusted DHCP server, storage resilience, off-host backup/restore or safe activation. No service/DB/proxy activation on the strength of this SSOT.
5. **Release identity gap:** the screenshots of a reachable site and the CT164 “no active app release” operator report may refer to different hosting states. Determine actual DNS, edge origin, active release marker, runtime configuration and DB **read-only** before making any release claim.

### P1 — public quality and scale

6. **Geo semantics:** search filters neighborhood/category/string only; no stored coordinates, city filter in public query or distance ranking. Do not label results “nearest”.
7. **Real business presentation:** home cards ignore available `imageUrl` in favor of category decoration; detailed public profile shows a hard-coded demo disclaimer. Introduce contextual demo/verified status, real media and accurate opening-hours presentation.
8. **SEO:** Angular CSR preview and `noindex` are intentional for demo, but not an indexable public pilot. Decide SSR/prerender and build canonical metadata/schema/sitemap before G4.
9. **Error messaging:** [directory-api.service.ts](../apps/web/src/app/directory-api.service.ts) shows development-specific `127.0.0.1:5090`/Angular proxy instructions to end users. Production errors must be actionable without exposing internal operations.

### Mobile UX review (visual observations, **not verified defects**)

- **Keep:** green/cream brand, strong hero type, clearly identifiable CTAs, native form controls and accessible semantic structure.
- **Improve:** reduce first-screen hero and excess vertical spacing; make mobile discovery/filter flow more compact; prioritize useful business information over decorative category covers; ensure menu state, focus and scroll behavior.
- iOS dark select popovers in screenshots are **native browser UI**, not a broken dropdown. Replace only if a custom implementation passes touch, keyboard, VoiceOver and accessibility checks.
- Verify on **real Safari/iPhone and Chromium**; screenshot width alone is not proof of CSS viewport. Test 320/375/390/768/1024/1440 CSS px, dynamic font/zoom, focus, overflow, keyboard, reduced motion and Core Web Vitals.

## 5. Canonical execution sequence and acceptance

| Order | Package / tracker mapping | Must exist before work is accepted | Exit evidence |
| --- | --- | --- | --- |
| **0** | **Reconcile Work → GitHub → CT164/runtime** (cross-cutting) | Exact latest Work HEAD, changed paths, PR, active-release identity; avoid concurrent writes | Read-only reconciliation record, current source SHA, runtime/source drift, revised tracker; HOLD if unknown |
| **1** | **Engineering & infrastructure P0** — YS-S0-02..06, YS-S5-01..06 | Re-run manual exact-SHA QA; CT ingress/auth, effective firewall, storage risk, independent backup/restore, monitored systemd/API and rollback | G0 + G-PREVIEW evidence separately; private only until gate PASS |
| **2** | **Domain & data architecture P0** — YS-S2-01..06 | Database ADR approved; migration compatibility; normalized city/neighborhood/category; organizations, location ownership, claims/memberships, audit | G2 real DB and cross-tenant negative tests |
| **3** | **Accounts, CMS & moderation P0** — YS-S1-01..07, YS-S3-01/02/05 | Scoped business signup/login/verification/claim with reviewer authorization; owner/editor only own resources; publish controls | G1/G3 unmocked end-to-end scenarios; persistence after restart |
| **4** | **Real content/features P1** — YS-S3-03/04, YS-S4-01..03 | Photo/media with type/size/rights/quarantine controls; profiles with address/hours/geo; offers/brochures/jobs expiry and discovery | Real approved content, upload-negative tests, complete public journeys |
| **5** | **Mobile design, SEO & quality P1** — YS-S1-04, YS-S3-06, YS-S4-04/05 | Mobile layout refinement without brand reset; reusable component patterns; SSR/prerender ADR; A11y/performance | Browser visual approvals, keyboard/screen reader, Core Web Vitals, SEO/crawler evidence |
| **6** | **Limited public pilot P0 gate** — YS-S6-01..05 | Independently verified business ownership/content, terms/privacy/reporting, no demo leaks, security/recovery/performance completed | G-PILOT=PASS, operator sign-off, first real KPI baseline |

Dependencies matter more than calendar dates. Work on mobile *design specs* may occur in parallel, but avoid conflicting commits to the same branch, and do not open owner workflows publicly before G2/G3 authorization.

## 6. Release gates — snapshot and blockers

| Gate | Current verdict | Main blocker / proof required |
| --- | --- | --- |
| G0 Engineering | **HOLD** | Re-run source, NuGet/npm, Playwright, real API/security checks against **current candidate SHA**; audit findings and approved manifests. |
| G1 Real local CMS | **HOLD / PARTIAL SOURCE** | Full admin create→edit→publish→public read→hide→restore→restart with real DB; unauthenticated/nonadmin denials. |
| G2 Production domain | **HOLD** | DB ADR; migrations; Organization/claims/membership; per-object tenant authorization and audit. |
| G3 Content/moderation | **HOLD** | Full reviewed publication lifecycle, safe media, abuse reporting and actor/reason audit. |
| G4 Public discovery | **HOLD** | Geo correctness, verified profiles, SEO/indexing, accessibility and measured performance. |
| G-PREVIEW protected PVE | **HOLD** | Verified source↔runtime SHA; private TLS/ACL, reverse-proxy negative probes, recovery and monitoring. CT164 OS-level readiness alone insufficient. |
| G-PILOT public | **HOLD** | All public prerequisites plus real consented merchant data, legal/privacy and operational sign-off. |

**NO-GO conditions:** unverified deployment source, reachable preview identity/registration/bootstrap on public origin, external DB writes outside approved maintenance, cross-tenant write, unauthorized publishing, absent verified off-host restore for real data, missing rollback, or unsolved P0 security finding. The application should remain private/demo-restricted while these are open.

## 7. Immediate ordered queue (next operator/Work actions)

1. Obtain latest Work result, PR URL, exact HEAD, local test evidence and files changed; record whether it supersedes the reviewed `a4802805` baseline. Do **not** merge competing commits blindly.
2. Perform read-only live-origin reconciliation: DNS→CT152→upstream; live artifact commit marker; CT164 service state and configs (redacted); public/preview identity endpoint accessibility; DB schema/identity; no secrets in evidence. Mark unknown results as HOLD.
3. Re-run exact-SHA manual source + real API integration; distinguish mocks from unmocked backend/DB tests. File evidence under separate web/api/deploy/verify/restore logs and reference immutable SHA.
4. Address CT164 risk register **without unsafe speculative changes**: source of DHCP `.107`, effective firewall/ACL, `pve/data` thin allocation/headroom, off-host backup policy and recovery rehearsal.
5. Close/approve DB provider ADR before introducing real organization/claim tables. Implement a focused vertical slice: **Organization → BusinessClaim → reviewed membership → scoped Owner CMS** with strict server-side permission tests.
6. Freeze a short mobile UX specification from actual iPhone/Safari observations; implement in an independent scoped PR only after the Work source merge boundary is known.
7. Update this SSOT + tracker + acceptance matrix + operator runbooks in the **same reviewed change** when any gate moves. Require proof, never percentage-based status.

## 8. Operational risk register

| Risk | Severity | State / owner role | Closure criterion |
| --- | --- | --- | --- |
| Live artifact identity not independently attested | P1 | OPEN — release operator | Operator reported CT164 release symlink, CT152 HTTPS and public edge probes; still require immutable built-asset/API fingerprint and independently retained verification. |
| Thin pool high allocation, no VG free space | P0 | OPEN — PVE operator | Audited capacity/headroom thresholds; backup/restore before storage actions; alerts. |
| No accepted off-host database + media recovery | P0 | OPEN — operator | Encrypted/controlled copy, retention, restore into scratch, integrity check, measured RPO/RTO. |
| PVE firewall / edge admin exposure | P0 | OPEN — network/security operator | Effective ACL and direct-origin denial proven; no client-supplied gateway-header bypass. |
| DHCP responder `.107` unidentified | P1 security | OPEN — network operator | Responder provenance and DHCP trust risk assessed; route/DNS persistence already guest-tested. |
| SQLite production provider vs DB ADR | P0 architecture | OPEN — engineering + owner decision | Accepted documented ADR with limits, migrations, concurrency, migration/restore strategy. |
| Missing multi-tenant auth, audit and uploads | P0 product/security | NOT IMPLEMENTED — backend/product | Owner A/B isolation, moderation, safe media and end-to-end negative tests. |
| Mobile density, useful listing data, SEO | P1 product | NEEDS DESIGN/RUNTIME QA — web/product | Approved real-device usability, a11y, crawler and performance checks. |

Role labels above indicate **required reviewer functions**, not claims that a specific individual has accepted responsibility.

## 9. Update / evidence contract

Every milestone or gate update MUST include:

~~~text
TASK_ID / GATE:
SOURCE_REF / FULL_COMMIT_SHA:
DEPLOYED_COMMIT_SHA: <exact value or UNVERIFIED>
FILES_CHANGED:
TEST_SCOPE: SOURCE | MOCK_BROWSER | REAL_API_DB | PVE_RUNTIME | PUBLIC_ORIGIN
COMMANDS / EXIT_CODES:
HOST / ENVIRONMENT / FIXTURE:
EVIDENCE_PATHS (web, api, deploy, verify, restore):
NEGATIVE_SECURITY_TESTS:
VISUAL_DEVICE / VIEWPORT EVIDENCE:
BACKUP_REF / RESTORE_RESULT / RPO / RTO:
REGRESSIONS / OPEN_RISKS:
REVIEWER / DATE:
VERDICT: PASS | PARTIAL | HOLD | FAIL | UNVERIFIED
NEXT_DEPENDENCY / OWNER_ROLE:
~~~

**Status rules:** `DONE` only after that task's complete acceptance contract; `PARTIAL` means implementation or limited evidence exists; `HOLD` means a prerequisite/approval is missing; `UNVERIFIED` means no trustworthy observation; `FAIL` requires RCA and regression evidence. Historic test PASS always remains attached to its own SHA and environment. Never paste real credentials, private keys, bearer tokens or unredacted DB contents into issues/docs/logs.

**Change protocol:** an accepted architectural direction requires a dated ADR in [DECISIONS](DECISIONS.md), then updates here, in [POC_TRACKER](POC_TRACKER.md) and in [POC_ACCEPTANCE_MATRIX](POC_ACCEPTANCE_MATRIX.md). When details disagree, explicitly record the conflict and the newest evidence; do not silently “fix” the historical record. The latest approved gate report controls release eligibility, not the most recently edited Markdown header.
