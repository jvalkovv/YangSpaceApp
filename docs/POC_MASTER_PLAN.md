# YangSpace 2.0 — Product PoC & Technical Master Plan

> **Accepted project rule — ADR-018:** GitHub is source/PR only. **GitHub Actions are NOT USED.** QA is operator-run against a pinned SHA with retained logs. See [ADR-018](ADR-018-NO-GITHUB-ACTIONS.md) and [S0 Manual QA](S0_MANUAL_QA_RUNBOOK.md). Any prior hosted CI requirement below is superseded.

**Version:** 1.0 · **Baseline date:** 2026-11-09 · **Plan status:** ADOPTED FOR EXECUTION · **Release status:** PRIVATE PREVIEW NOT YET DEPLOYED

> This document is the canonical execution plan. [POC_TRACKER.md](POC_TRACKER.md) tracks the work, [POC_ACCEPTANCE_MATRIX.md](POC_ACCEPTANCE_MATRIX.md) defines test evidence and [POC_PVE_RELEASE_RUNBOOK.md](POC_PVE_RELEASE_RUNBOOK.md) defines infrastructure/release gates. Existing PRODUCT, DOMAIN_MODEL, ARCHITECTURE, PVE_OPERATIONS and MVP_ROADMAP documents provide supporting design rationale. A plan or source commit is not runtime acceptance.

## 1. Product proposition and business hypothesis

**YangSpace** is a hyperlocal directory connecting residents and businesses, starting in **one neighborhood in Varna**, then expanding to Varna and only afterwards to other cities. Discover local shops/services, current offers, weekly/monthly brochures and jobs. Residents browse without account creation. Verified businesses update their own records with controlled access.

**Differentiator:** verified freshness and useful neighborhood-specific content—not simply another generic Google Maps clone. The user should know who is nearby, what is relevant *now*, and how to contact or visit a business.

**Monetization:** business subscriptions, prices and in-platform payments are **TBD/out of scope for the technical PoC and initial pilot**. Do not hard-code €1–5 in entities, policies or UI. Pilot KPI must validate actual willingness-to-pay and operator support cost before pricing.

**Success is both technical and commercial.** Zero crashes with six fake listings does not establish marketplace product/market fit; 100 unverified scraped shops do not establish high-quality supply.

## 2. Locked direction and deliberate exclusions

| Topic | Decision |
|---|---|
| API | C#, ASP.NET Core **.NET 10 LTS**, EF Core; supported patch releases |
| Frontend | **Angular 22**, responsive, accessible; SEO delivery decided before public launch |
| Architecture | **Modular monolith** with directory, organizations/ownership, publications, moderation, media, search and operations as logical boundaries |
| Hosting | Existing **Proxmox VE**; controlled **unprivileged Linux LXC** preferred where feasible |
| Gateway | Nginx, systemd, TLS / authenticated access gateway, no public dev server |
| Source and verification | GitHub source/PR, clean exact-SHA local validation with logs, **no GitHub Actions and no push-to-production** |
| Production DB | **OPEN ADR**: compare PostgreSQL vs SQL Server with migration/spatial/backup/ops/total cost criteria; SQLite is **temporary isolated-preview only** |
| Storage | Protected local media for small pilot or S3-compatible later; approval via threat/backup model |
| Legacy | Old .NET 8 / Angular 18 archived; no blind migration of booking/service tables |
| Not in PoC | Native mobile apps, booking, payment checkout, CV/ATS, AI recommendations, bulk national imports, separate microservices |
| Design | Green/cream YangSpace homepage preview accepted; improvements must preserve coherent visual system |
| Geography | No dedicated deployment/DB per neighborhood; city/neighborhood IDs and normalized geo records |

The official supported .NET and Angular lifecycle must be rechecked on upgrade/release: [Microsoft .NET policy](https://dotnet.microsoft.com/en-us/platform/support/policy), [Angular compatibility](https://angular.dev/reference/versions).

## 3. Three distinct acceptance levels

### A. Proven local preview — CURRENT BASELINE
- Local .NET, Angular and Angular→API proxy health: PASS in operator log.
- Angular typecheck and production build: PASS in operator log.
- Playwright **17/17 PASS** against mocked APIs; viewport visual review at 375/390/768/1024/1440; last tablet visual defect corrected.
- Six fictional seed businesses and fake offers/brochures/jobs; homepage visual/functional preview accepted at source commit `20aa30c7da581316ba31900b6953025a3d1f3b00`.
- **Not proven:** real admin create/edit/publish/hide against actual DB, manual clean-source and real API evidence on exact SHA, deployment readiness, real data/owner access.

### B. CLOSED PVE PoC — GATE G-PREVIEW
An authenticated / VPN-only preview running on its own PVE LXC, with actual DB-backed create→publish→read→unpublish flows, reproducible locally verified build, restore evidence, basic logging and rollback. **No public indexing or real production data.**

### C. PUBLIC NEIGHBORHOOD PILOT — GATE G-PILOT
Real verified businesses and owners, production migrations/storage, server-side tenant authorization, moderated lifecycle, SEO/indexability, ops monitoring, legal/privacy/content rights and documented release/rollback. Not equivalent to closed preview.

Do not declare either B or C completed based only on homepage screenshots.

## 4. User journeys and acceptance contracts

| Actor | Journey | End-to-end proof |
|---|---|---|
| Anonymous visitor | Select city/neighborhood/category → search → open profile | Results correspond to filters; no login; URL survives refresh; empty/error states accurate |
| Resident | Discover current offer/brochure/job → business → contact/maps/application link | Only valid/published content; no fictional “open now” without opening-hours logic |
| Owner applicant | Register/login → claim business → provide evidence → wait for review | Unauthorized user cannot self-approve/claim another business |
| Moderator | Inspect claim → approve/reject with reason → assign scoped membership | Rejection cannot publish; decisions generate audit events |
| Authorized owner/editor | Edit *their* business information and promotions | Ownership check performed on API for **every** read/write; another owner gets 403/404 |
| Owner | Draft offer/brochure/job → send for review → publish → expire/archive | Date/terms/media validation, moderation and expiry reproduced in tests |
| Admin | Search records, review reports, hide/restore and inspect history | Every sensitive mutation authorized, validated and audited |
| Operator | Build exact commit → deploy privately → verify → recover | Build evidence, logs, backups, restore drill, rollback and immutable release identity |

**PoC minimum vertical slice** is visitor browse + authenticated admin CRUD + actual persistence + permission negative tests + expiration + private PVE delivery. **Pilot adds** verified owner claims, moderation, real media/SEO and real verified businesses.

## 5. Information architecture / routes

### Public
- `/`: hero, categories, featured local directory, offers/brochures/jobs, business introduction.
- `/obekt/:slug`: full location details with contact/hours/location and currently valid publications.
- `/kategorii/:slug`, `/varna/:kvartal`: planned indexable landing pages, only after SEO architecture gate.
- `/oferti`, `/broshuri`, `/rabota`: planned browsable listing/filter pages, not only homepage teaser.
- `/za-biznesa`, `/privacy`, `/terms`, `/report`: public onboarding/policy/reporting surfaces.

### Authenticated business
- `/portal`: own organizations/locations, verification status, publication drafts, metrics (basic).
- `/portal/obekti/:id`: profile/hours/contacts/gallery management with save/publish distinction.
- `/portal/oferti`, `/portal/broshuri`, `/portal/rabota`: lifecycle/validation/preview.
- `/portal/claims`: claim and review status.

### Moderation/platform admin
- `/admin`: preview-only currently; must evolve into accessible production role-based admin.
- Claims queue, publication queue, abuse reports, audits, verification freshness and scheduled expiry failures.

Existing `/api/v1/homepage`, `/locations`, `/locations/{slug}` remain, but must be versioned/mapped consistently as domain evolves. Existing admin endpoints are **PlatformAdmin-only preview**, not business owner APIs.

## 6. Target domain / data boundaries

**Organizations** own one or multiple **BusinessLocations**. **OrganizationMembers** bind identity to scoped role (owner/editor). **Claims** are reviewed before membership grant. **City, Neighborhood and Category** are normalized lookups; locations hold coordinates, address, timezone, opening hours/exceptions, public status and verification metadata.

**Content**: Offer, Brochure/MediaAsset, JobPost, each with lifecycle `draft → pending_review → published → expired / archived`; moderator can reject/suspend; published content must meet date and rights rules. **ModerationDecision, Report and AuditEvent** record what changed, actor, reason and timestamp. **Scheduled worker** expires and reconciles content; public query also enforces validity by timestamp independently of worker.

Persistence:
- Unique constraints, FK and filtered/search indexes reviewed in migrations.
- UTC timestamps; display and opening-hours interpretation in `Europe/Sofia`.
- Transactional publication + audit events; no direct anonymous write to business records.
- Separate secrets, DB storage, media and code release directories.
- No real personal data in seed/automated tests.

**Open design decision:** PostgreSQL is a practical candidate for PVE spatial indexing and cost control; SQL Server remains viable. Select one in S2 after a migration/backup/restore spike; do **not** silently switch providers or introduce migrations in a live database.

## 7. Architecture and delivery view

```text
Visitor / Merchant / Moderator
        |
   TLS + access policy / Nginx
        |
  Angular public / portal / admin
        | HTTP /api/v1
  ASP.NET Core 10 modular monolith
   | Directory  | Identity & Ownership
   | Publications & Expiry | Moderation
   | Search & SEO | Media | Audit & Health
        |
  EF Core / reviewed migrations
        |
  SQL Server or PostgreSQL (ADR pending)
        |
  Backup + restore / metrics + logs + alarms

GitHub PR → clean-SHA local QA logs → operator approval → controlled PVE LXC release
(no Jenkins, IIS, Azure, Kubernetes, webhook auto-deployment)
```

**Private preview topology:** one LXC may host Nginx + API + static Angular + disposable SQLite preview; protected network/VPN, no real merchant data, no public exposure. **Public pilot topology:** app LXC with production DB isolated logically and, if beneficial for backup/fault isolation, in a separate DB CT; final topology decided from inventory and measured load, never assumed.

## 8. Phases and execution order

### S0 — Baseline freeze and engineering gates (NEXT)
**Priority P0; dependency:** none.
1. Record last accepted homepage source `20aa30c` and QA docs SHA; no redesign.
2. Protect `main`; keep PR #1 Draft until exact merge/release gates.
3. Create/commit reviewed `apps/web/package-lock.json`; use `npm ci` in local QA/PVE build; pin SDK/toolchain where possible.
4. Audit transitive NuGet dependencies, SQLite native security pin, secrets history, license and repository permissions; ensure warnings-as-errors remain.
5. Make operator-run exact-commit validation reproducible; archive backend, Angular and 17+ Playwright logs locally per app. Do not use hosted CI.
6. Add real API integration test project + isolated database fixture (do not rely only on HTTP-mocked Playwright).
7. Fix deployment policy conflict: existing `scripts/build-preview-from-github.sh` only accepts commits already in `origin/main`, while code is in Draft PR. For an approved preview candidate use the allowlisted branch ref, exact SHA and manually reviewed source/API evidence or merge after gate; **never remove identity checks**.

**Exit G0:** deterministic dependency restore; clean local source and real API verification PASS for the same exact SHA; no open P0 secret/auth build findings; release script SHA policy resolves draft-branch preview without weakening verification. **Blocked today.**

### S1 — Real local admin vertical slice (CLOSED POC functionality)
**Priority P0; dependency G0 or parallel safe local development.**
1. Confirm admin bootstrap and role gate in isolated non-production env.
2. Build clean admin UX for list/create/edit/hide/restore with validated forms, disabled/submitting/success/error states.
3. Implement full offer/brochure/job **CRUD** + edit/archive/unpublish, not just POST.
4. Real .NET integration tests: guest denied; admin creates location; GET public; updates; creates offer/brochure/job; validates active/expired; hides location; public result disappears; restore; audit proof.
5. Ensure no anonymous `/api/auth/register` exposure at public preview; remove/gate before any Internet-reachable release.
6. Separate demo content from real data; idempotent seed only in local preview, with clear reset strategy.

**Exit G1:** repeatable **database-backed** end-to-end admin workflow, negative auth tests, no hanging UI, all persisted writes verifiable after restart; screenshots of admin and business detail. **Currently unverified/incomplete.**

### S2 — Production domain and database foundation
**Priority P0; dependency G0 and domain design review.**
1. Approve ADR DB (SQL Server vs PostgreSQL): licensing/ops cost, spatial/search, migrations, restore, support.
2. Introduce Organization, Member, Location, City, Neighborhood, Category, hours, status and verification in domain packages within the monolith.
3. Replace `EnsureCreated` production assumptions with EF Core migrations; versioned scripts and clean deploy/rollback rehearsal.
4. Implement scoped owner/editor/moderator/admin authorization at service + query boundaries.
5. Claim request and manual moderator approval, reject, revoke membership; audit log.
6. Add constraint, concurrency and cross-tenant integration tests (owner A must never edit/view private data of owner B).
7. Define privacy/data retention/access export and correction flows before storing merchant user data.

**Exit G2:** schema/migrations clean on fresh DB, restore test, impersonation/IDOR negative tests PASS, claim and membership lifecycle works; no unapproved open privileged registration.

### S3 — Business CMS and content lifecycle completion
**Priority P1; dependency G2; can prototype UI in parallel.**
1. Merchant dashboard with clear Business > Location > Content hierarchy; multiple locations per org.
2. Profile CRUD: business identity, address/point, multiple categories, opening hours/exceptions, contacts, gallery, visibility.
3. Offers: title, details/terms, valid period, scheduling, status, disclosure requirements.
4. Brochures: upload PDF/image with MIME sniffing, size/page/scan policy, rights attestation, safe serving and review; NOT arbitrary untrusted file paths.
5. Jobs: description, employment mode, expiration, outbound application (no stored CV), moderation and reports.
6. Workflow: draft, save, preview, submit, approve/reject, publish, unpublish, expire, archive, restore where permitted.
7. Administrative moderation, abuse reports, audit trail and scheduled reconciliation worker.
8. Integration tests for content publication, expiry, role limits, upload threat cases and stale/duplicate submissions.

**Exit G3:** verified owner completes each flow without admin intervention except required review; visitors only see eligible published content; expired items disappear automatically; screenshots + test logs.

### S4 — Public discovery, content quality and SEO
**Priority P1; dependency G2/G3.**
1. Search/category/city/neighborhood taxonomy and URL filters; DB indexes and anti-duplication.
2. Profile UI: contact, verified-data timestamp, hours with exception handling, directions, images, offers/brochures/jobs.
3. Dedicated pages for offers/brochures/jobs with active-only filtering and pagination.
4. Choose Angular SSR/prerender strategy, canonical URLs, dynamic page metadata, sitemap, robots, structured data; avoid indexation of demo/hidden/duplicate pages.
5. Accessibility/mobile keyboard tests and real Core Web Vitals measurement.
6. Privacy-aware click-to-call, directions and outbound-click aggregation with documented consent rules.

**Exit G4:** approved indexable SEO pages, discoverability tests, usable mobile screens and valid contact/redirects, measurable useful actions.

### S5 — Private PVE LXC preview (START AFTER G0/G1)
**Priority P0 for deployment; does NOT require public pilot readiness.**
1. Read-only PVE inventory: unused CT ID, host resources, storage, backups, DNS, gateway, ACL, network.
2. Provision isolated unprivileged LXC; OS + .NET runtime/build toolchain + Node + Nginx + systemd and separate service/deploy users.
3. Private network and TLS/auth gateway; no direct public exposure of admin, database or Angular dev server.
4. Git clone/fetch with read-only credentials; validate approved SHA + matching local source/API QA manifests; build into immutable `releases/<sha>`, never build over `current`.
5. Persist DB/media/config outside release tree; secrets via protected environment, no config in Git.
6. Offline/host-consistent DB backup + off-host copy; restore dry run; check logs and monitoring.
7. Operator activation, smoke / rollback; document CT, host, SHA, config hash, backup ref, verification and recovery.

**Exit G-PREVIEW:** private URL loads after reboot; real API/CMS writes survive restart; guest auth check; backup/restore PASS; downtime/rollback demonstrated; no public crawl. No PVE mutation merely from completing this plan.

### S6 — Closed pilot and public launch gate
**Priority P1; dependencies G2–G5.**
1. Choose **one** concrete neighborhood and collect a small, consented set of real pilot merchants before scaling.
2. Validate real contact data, source rights, owner permission and freshness. Clearly identify unverified/claimed states.
3. Finalize GDPR/privacy, terms, report/takedown, brochure copyright/usage rights, promotion disclosure, accessibility and data retention with appropriate review.
4. Production migrations, hardening, penetration/abuse testing, TLS/firewall, alerting, backup/restore and rollback complete.
5. Go-live content and SEO indexability enabled **only after** sample-data isolation, quality and safety checks.
6. Onboard progressively; monitor user actions, update freshness, owner activation and help/support workload.

**Exit G-PILOT:** verified real data, no open P0/P1 vulnerabilities, real owner/visitor/admin flows PASS, legal/ops gates reviewed, measurable useful activity. **No public pilot while preview-only Identity/EnsureCreated remains the persistence/security model.**

### S7 — Commercial validation and scale
After pilot evidence: assess visits → useful actions, business retention and support cost; test subscription willingness-to-pay with concrete offers. Only then approve plan/entitlements/payment ADR. Standardize Varna-wide onboarding and city taxonomy before national expansion. No premature national bulk-scraping or unsupported payments infrastructure.

## 9. Test/evidence policy

Test layers:
- **Unit:** validation, state transitions, date/expiry boundaries, mapping, business rules.
- **Integration:** real EF DB/migrations and API host, auth/tenant isolation, persistence, idempotency and negative inputs.
- **Browser:** mocked frontend regressions for deterministic UI + **separate unmocked E2E** for API/CMS/auth.
- **Visual/a11y:** screenshots at 375/390/768/1024/1440; keyboard focus, semantics, contrast, no overflow; populated and empty states.
- **Security:** privilege escalation, CSRF/token policies, upload MIME/path/traversal/size, rate limits, secrets scanning.
- **Performance:** measure p50/p95 API, browser LCP/CLS/INP, DB query counts, manual QA baseline; define thresholds with representative pilot hardware, don't assert estimates as measurements.
- **Operations:** reboot, backup, restore, disk-full simulation, restart loops, bad release rollback, credential rotation.

Each completed item records: **task ID → commit SHA/PR → test command + result → screenshot/log artifact → runtime target → reviewer → gate decision**. No "100%" claim based on source alone.

See [POC_ACCEPTANCE_MATRIX.md](POC_ACCEPTANCE_MATRIX.md) for executable scenario definitions.

## 10. Risk register and mitigations

| Risk | Severity | Treatment |
|---|---|---|
| Preview admin endpoints exposed publicly | P0 | Private ACL by default; audit Identity route map; remove/gate unverified registration; auth negative tests |
| Historical credentials from legacy repo | P0 if real/reused | Inventory, rotate actual reused secrets, no demo credential reuse; historical secrets remain in Git history |
| Cross-business unauthorized access | P0 | Organization membership policies, query scoping, IDOR regression |
| Nonrepeatable npm dependencies | P1 | Commit lockfile, `npm ci`, dependency audit and local QA |
| Preview SQLite/EnsureCreated used for public data | P0 | Production DB ADR, EF migrations, restore drills, no automatic production seed |
| Brochure and image upload risks | P0 | Quarantine, content sniffing, limits, malware scanning where appropriate, private media storage |
| Manual QA evidence missing | P1 | Run clean source + real API tests and retain exact-SHA logs, no hosted pipeline |
| Draft PR commit cannot pass current PVE build script | P1 | Resolve approved-branch SHA policy before private CT deploy |
| Paid plans before proof of value | P2 | Defer billing; measure conversion/retention/support before price |
| Unverified stale offers/contacts | P1 | Source/verified_at, expiry worker, reporting, update reminders |
| Public scaling on single PVE CT | P1 | Capacity tests, off-host backups, isolation, documented migration path |

## 11. Delivery management / change control

- Work only in reviewed branches/PRs; protect `main`. Current YangSpace 2.0 lives in Draft PR #1.
- **No silent design rewrites**: homepage accepted, changes to shared visual system require targeted QA.
- Implement in vertical slices, not simultaneous module-wide rewrites.
- Each phase is blocked until its gate is evidenced; parallel work is allowed when dependencies are clearly isolated.
- PVE is a deployment target, not a substitute for local manual QA. No direct DB mutation without migration/backup review.
- Update [POC_TRACKER.md](POC_TRACKER.md) at each accepted commit; add decision records for DB/storage/SEO/hosting.
- Costs, subscription plans, and public launch date stay open until business and technical evidence supports commitments.

## 12. Immediate execution packet (NEXT TURN)

**Start with `YS-S0-01` through `YS-S0-05` in [POC_TRACKER.md](POC_TRACKER.md):** freeze source QA, pin npm dependencies, run exact-commit source and API integration QA locally, test the real .NET API/admin lifecycle, audit secrets/Identity and adjust pinned-SHA branch release policy. Then transition to `YS-S1` for complete live-data CMS, before any LXC provisioning.

Do not merge PR #1 into `main`, create a PVE CT, invent a CT ID or claim production readiness as part of adopting this plan.
