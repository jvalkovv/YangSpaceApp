# YangSpace 2.0 — Single Source of Truth (SSOT)

**Status:** ACTIVE planning/status authority **within this documentation PR only**; adoption into the development branch is pending review. This document does **not** authorize deployment, database mutation, exposure, or PR merge.  
**Baseline date:** 2026-11-11 (Europe/Sofia).  
**Reviewed application source:** \`feat/yangspace-v2-foundation-20261108\` @ \`a4802805421f10e834ad35bbf2f259db4e4eab84\` (Draft [PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1)). The \`main\` branch is the legacy application, not the active YangSpace 2.0 baseline.  
**Scope of this update:** repository/source review, accepted operator-reported evidence, project documentation and user-supplied iPhone screenshots; **no new build, real-browser, DB, PVE, or public-origin execution** was performed for this document.

> **Authoritative summary:** strong design and functional *preview foundation*; real multi-business/owner workflows, public-pilot content governance, SEO, ingress protection, observability and off-host recovery are not accepted. **G0, G1, G2, G3, G4, G-PREVIEW and G-PILOT remain HOLD at this reviewed source baseline.** Do not infer percentages or production readiness from code, screenshots, HTTP 200 or historical PASS markers.

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

**Policy:** No GitHub Actions/hosted CI, webhook auto-deploy, blind \`git pull\` activation, unreviewed PVE writes or silent merge. Use clean exact-commit, manual/operator-run QA and retained per-app logs per [ADR-018](ADR-018-NO-GITHUB-ACTIONS.md). Work in isolated feature/docs branches; reconcile to the active feature HEAD before merging. **Never modify the active CT or database merely to synchronize documentation.**

## 2. Accepted product boundaries

- **YangSpace** = hyperlocal directory of **businesses, offers, brochures and local jobs**, starting with one Varna neighborhood, then Varna, then other cities once pilot gates pass.
- Visitors browse without mandatory registration. Verified business owners/editors manage only their own organizations/locations after a controlled claim/approval flow. Moderators manage reviews/reports/audit.
- Target stack: Angular 22 + ASP.NET Core 10/EF Core; modular monolith; controlled PVE single application LXC initially, Nginx/systemd and separated persistent data/release/config.
- **Out of MVP:** booking, integrated payments, paid subscriptions as a predetermined tariff, native iOS/Android apps, CV database/ATS, AI recommendations and bulk uncontrolled imports.
- **No invented data:** demo entries, offers, time schedules and badges must be clearly identified; never claim “verified”, “open now”, actual proximity or authentic promotions without evidence.
- Initial 10–20 merchant invite/claim pilot is a **proposal**, not an approved target. Agree with product owner before outreach.

## 3. Current evidence snapshot (at reviewed source SHA)

| Layer | Verified/implemented evidence | Qualification |
| --- | --- | --- |
| Repository | \`main\` = legacy; Draft PR #1 targets \`main\` from \`feat/yangspace-v2-foundation-20261108\`; reviewed HEAD \`a4802805\`. | This docs branch is isolated. Feature branch may advance independently. |
| Frontend foundation | Home/Detail/Admin Angular components, category/search, pagination, skeleton/error/retry states; green/cream visual language. | Historical 17/17 Playwright PASS on \`20aa30c\` with **mocked API**. Not proof for reviewed HEAD. |
| Real local engineering | Operator-reported clean source/API integration PASS at \`b30c1c8\`; committed npm lockfile and .NET build records. | QA does **not** automatically transfer to later HEAD; repeat exact-SHA validation. |
| Persistence | Reviewed EF Core initial migration committed; FK runtime checks and isolated DB backup/restore local regression reported PASS at \`2069eb9\` / \`1ce3fa3\`. | **Disposable Windows DB only**; not public database cutover or off-host operational restore. |
| Public directory | Published locations, facets, search, active offers/brochures/jobs, detail routes; six fictional seeded examples. | City/geo coordinates, distance search, owner verification, rich opening hours, real photos and indexable SEO not complete. |
| Admin CMS | PlatformAdmin bearer policy; source APIs for create/edit/hide/restore locations and publications; offline admin provisioning and production startup guards. | Owner portal, cross-tenant permissions, moderation/audit and exact-HEAD real CMS browser/DB acceptance are missing. |
| CT164 guest | Operator reported Debian 13.6 unprivileged LXC; systemd running; .NET 10 runtime; dedicated \`yangspace\`/\`yangdeploy\` identities and actual positive/negative directory ACL PASS; DHCP .110, gateway/DNS .102 persisted after reboot. | DHCP **server identifier .107 unexplained**; PVE firewall effective protection, storage headroom, independent backups, application release, CT152 ingress and full recovery are not accepted. |
| Public URL screenshots | User-supplied iPhone screenshots of \`yangspace.gndstore.eu\` show Hero, menu, search, category select, cards and detail. | **Origin, environment, deployed SHA, DB provenance, effective access control and exact viewport are UNVERIFIED.** A visible URL/HTTP 200 is not a CT164 deployment acceptance. |

**Important drift reconciliation:** Older [PVE runbook](POC_PVE_RELEASE_RUNBOOK.md) opening text (“no CT provisioned”) predates the confirmed CT164 guest work and is **SUPERSEDED for infrastructure inventory status** by the later [CT164 evidence](CT164_PROVISIONING_20261110.md). The runbook's security/release procedure remains applicable. The [production CMS contract](PRODUCTION_CMS_SECURITY_20261109.md) also includes a historical “no migration committed” statement superseded by the actual [migration evidence](S2_INITIAL_MIGRATION_20261110.md); **runtime production schema adoption remains HOLD**.

## 4. Product/architecture findings requiring resolution

### P0 — release/architecture boundaries

1. **Database ADR conflict:** [Master Plan](POC_MASTER_PLAN.md) / [DECISIONS](DECISIONS.md) leave PostgreSQL vs SQL Server open (ADR-007/013); current [Models.cs](../apps/api/Models.cs), [Program.cs](../apps/api/Program.cs), migration and [ProductionSqlitePolicy.cs](../apps/api/ProductionSqlitePolicy.cs) explicitly implement SQLite even for guarded Production startup. **Decision required before G2:** accept SQLite as a bounded single-instance pilot with explicit concurrency/backup/data growth limits, or adopt a reviewed PostgreSQL/SQL Server strategy. No silent provider flip and no real DB conversion as a side effect.
2. **Missing multi-tenant domain:** \`DirectoryDb\` contains Identity, BusinessLocation, Offer, Brochure, Job, but no Organization/OrganizationMember/BusinessClaim, scoped ownership or cross-tenant API policies. The existing admin is a global PlatformAdmin, **not** a merchant CMS.
3. **Content governance missing:** public boolean publication is a start, not a full draft → review → publish → expire/archive/reject/suspend lifecycle; no persisted actor/reasoned audit, business reports or vetted upload pipeline.
4. **Infrastructure safety:** CT164 guest preparation PASS does not establish protected CT152→CT164 route, direct-origin denial, PVE firewall, trusted DHCP server, storage resilience, off-host backup/restore or safe activation. No service/DB/proxy activation on the strength of this SSOT.
5. **Release identity gap:** the screenshots of a reachable site and the CT164 “no active app release” operator report may refer to different hosting states. Determine actual DNS, edge origin, active release marker, runtime configuration and DB **read-only** before making any release claim.

### P1 — public quality and scale

6. **Geo semantics:** search filters neighborhood/category/string only; no stored coordinates, city filter in public query or distance ranking. Do not label results “nearest”.
7. **Real business presentation:** home cards ignore available \`imageUrl\` in favor of category decoration; detailed public profile shows a hard-coded demo disclaimer. Introduce contextual demo/verified status, real media and accurate opening-hours presentation.
8. **SEO:** Angular CSR preview and \`noindex\` are intentional for demo, but not an indexable public pilot. Decide SSR/prerender and build canonical metadata/schema/sitemap before G4.
9. **Error messaging:** [directory-api.service.ts](../apps/web/src/app/directory-api.service.ts) shows development-specific \`127.0.0.1:5090\`/Angular proxy instructions to end users. Production errors must be actionable without exposing internal operations.

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

1. Obtain latest Work result, PR URL, exact HEAD, local test evidence and files changed; record whether it supersedes the reviewed \`a4802805\` baseline. Do **not** merge competing commits blindly.
2. Perform read-only live-origin reconciliation: DNS→CT152→upstream; live artifact commit marker; CT164 service state and configs (redacted); public/preview identity endpoint accessibility; DB schema/identity; no secrets in evidence. Mark unknown results as HOLD.
3. Re-run exact-SHA manual source + real API integration; distinguish mocks from unmocked backend/DB tests. File evidence under separate web/api/deploy/verify/restore logs and reference immutable SHA.
4. Address CT164 risk register **without unsafe speculative changes**: source of DHCP \`.107\`, effective firewall/ACL, \`pve/data\` thin allocation/headroom, off-host backup policy and recovery rehearsal.
5. Close/approve DB provider ADR before introducing real organization/claim tables. Implement a focused vertical slice: **Organization → BusinessClaim → reviewed membership → scoped Owner CMS** with strict server-side permission tests.
6. Freeze a short mobile UX specification from actual iPhone/Safari observations; implement in an independent scoped PR only after the Work source merge boundary is known.
7. Update this SSOT + tracker + acceptance matrix + operator runbooks in the **same reviewed change** when any gate moves. Require proof, never percentage-based status.

## 8. Operational risk register

| Risk | Severity | State / owner role | Closure criterion |
| --- | --- | --- | --- |
| Unverified public URL / CT164 source drift | P0 | OPEN — release operator | Exact deployed SHA, origin, runtime config, data provenance and negative security probes recorded. |
| Thin pool high allocation, no VG free space | P0 | OPEN — PVE operator | Audited capacity/headroom thresholds; backup/restore before storage actions; alerts. |
| No accepted off-host database + media recovery | P0 | OPEN — operator | Encrypted/controlled copy, retention, restore into scratch, integrity check, measured RPO/RTO. |
| PVE firewall / edge admin exposure | P0 | OPEN — network/security operator | Effective ACL and direct-origin denial proven; no client-supplied gateway-header bypass. |
| DHCP responder \`.107\` unidentified | P1 security | OPEN — network operator | Responder provenance and DHCP trust risk assessed; route/DNS persistence already guest-tested. |
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

**Status rules:** \`DONE\` only after that task's complete acceptance contract; \`PARTIAL\` means implementation or limited evidence exists; \`HOLD\` means a prerequisite/approval is missing; \`UNVERIFIED\` means no trustworthy observation; \`FAIL\` requires RCA and regression evidence. Historic test PASS always remains attached to its own SHA and environment. Never paste real credentials, private keys, bearer tokens or unredacted DB contents into issues/docs/logs.

**Change protocol:** an accepted architectural direction requires a dated ADR in [DECISIONS](DECISIONS.md), then updates here, in [POC_TRACKER](POC_TRACKER.md) and in [POC_ACCEPTANCE_MATRIX](POC_ACCEPTANCE_MATRIX.md). When details disagree, explicitly record the conflict and the newest evidence; do not silently “fix” the historical record. The latest approved gate report controls release eligibility, not the most recently edited Markdown header.
