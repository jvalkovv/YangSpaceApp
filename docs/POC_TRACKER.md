# YangSpace 2.0 — Execution Tracker

> **Current gates and ordering:** [SSOT.md](SSOT.md). Keep this file as the detailed task-ID/evidence ledger; dated rows record their own acceptance scope and may be older than the SSOT snapshot. Do not promote a task to DONE from a newer source commit without fresh exact-SHA runtime evidence. CT164 guest preparation is not an activated application release.

**Tracked source baseline:** `20aa30c7da581316ba31900b6953025a3d1f3b00` (homepage code) · **visual QA docs:** `e2edefb70c648199ba8e82f83d7a3396424c88c4`.  
**Branch:** `feat/yangspace-v2-foundation-20261108` · **PR:** [#1](https://github.com/jvalkovv/YangSpaceApp/pull/1) (Draft).  
**Status values:** DONE = proven, PARTIAL = source present without full gate, TODO = not built/verified, BLOCKED = unmet dependency, HOLD = intentionally deferred.  
**Governance:** evidence is required before marking DONE. Do not infer progress percentages from file counts.

## 2026-11-12 15:18 Europe/Sofia — F2a-3 isolated transaction candidate operator PASS

- [Draft PR #21](https://github.com/jvalkovv/YangSpaceApp/pull/21), exact source `91f34f17a52e11dd107645ebb44ea0e578e4fa73`: operator source/worktree and PowerShell syntax PASS; `STAGE_PASS=f2a3-restore`, `STAGE_PASS=f2a3-build`, `STAGE_PASS=f2a3-transaction`; `F2A3_CLAIM_TX_ISOLATED_ONLY=PASS`. The scoped Windows runner verifies six transaction contract markers before emitting final PASS.
- Evidence dir: `D:\GitHub\YangSpaceApp-F2a3-ClaimTx-QA\logs\yangspace\f2a3-claim-tx\91f34f17a52e-20261112-151758-9c434338`. **Read-only manifest and stage file/hash postflight PENDING; no merge decision yet.**
- Accepted runtime scope is synthetic SQLite EF in-memory transaction success + rollback after deliberately failed second SaveChanges on audit UNIQUE index. This is **not** live reviewer authorization, real PostgreSQL app transaction, concurrent client or actual claim approval.
- Current PR inventory: #21 OPEN/DRAFT/HOLD (evidence postflight pending), #17 OPEN/DRAFT/HOLD (larger F2a security/PG gates), #1 OPEN/DRAFT/HOLD (release/pilot); #19 MERGED/CLOSED. No open PR qualifies for closure until its scoped DoD completes. No Actions, CT164/CT152, production DB, claim activation or deployment.

## 2026-11-12 14:48 Europe/Sofia — next F2a-3 QA slice and PR hygiene

- The required **in-conversation progress at every meaningful source/test/review/merge milestone** and **100%-scoped PR closure only after QA/review/owner approval** are reaffirmed in [MVP roadmap](MVP_ROADMAP.md#2026-11-12-1448-europesofia--execution-governance-reaffirmed-visible-progress-and-pr-closure). Monitoring is optional, not a substitute for stage reports.
- [Draft PR #21](https://github.com/jvalkovv/YangSpaceApp/pull/21) is now open, stacked on #17, **SOURCE-ONLY / NOT BUILD TESTED**. Its allowlist is one test-only `ClaimTransactionContract` class, one isolated test CLI mode, one Windows exact-SHA runner and a proof-boundary doc. Synthetic SQLite EF transaction test succeeds or rolls back in one outer transaction across two SaveChanges; no real reviewer/HTTP/PG interaction or production behavior changes. **All runtime PASS is pending operator execution**.
- [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) still requires explicit policy acceptance (dedicated reviewer, no self-approval, verified evidence, no silent transfer, member regrant/revocation, audit/idempotency). No new claim approval endpoint was exposed.
- PR inventory: #21 OPEN/DRAFT/HOLD (source-only); #17 OPEN/DRAFT/HOLD (larger security/PG app/recovery); #1 OPEN/DRAFT/HOLD (V2 pilot/release); #19 MERGED/CLOSED (isolated bearer read QA). **No open PR is 100%-eligible for closure at this checkpoint.**
- Earlier tested combined F2a application SHA `445db4d9a5d28b798054c426afeba9b9be2c771b` retains its two Windows disposable PG/tenant PASS and operator evidence postflight PASS. No CT164/CT152, live DB, provider migration, claim activation, GitHub Actions or deployment.

## 2026-11-12 13:57 — F2a-3 review contract proposed (not approved)

Created [F2a-3 reviewer-controlled claim and Owner membership design](F2A3_CLAIM_REVIEW_SECURITY_CONTRACT_20261112.md), linked from [Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20). It is **PROPOSED / HOLD**: dedicated live-checked reviewer authority, no self-approval, claim decision state machine, first-time only location ownership, atomic claim+organization+location+Owner membership+audit transaction, concurrency/uncertain-COMMIT protection and PostgreSQL HTTP negative/fault-injection matrix. Existing `BusinessClaim`/membership/audit indexes are accounted for.

**Important test-topology dependency:** the present disposable PG runner uses `--network none` and cannot be directly reached by a host `WebApplicationFactory`. A separate approved isolated PostgreSQL bearer HTTP test topology is needed. No implementation or public feature enabled yet; role, evidence handling, re-grant and transfer policy require explicit owner acceptance.

## 2026-11-12 13:57 Europe/Sofia — F2a combined exact-SHA evidence postflight PASS

**Operator read-only evidence verification: `F2A_COMBINED_EVIDENCE_POSTFLIGHT=PASS`** on the tested application SHA `445db4d9a5d28b798054c426afeba9b9be2c771b` ([PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17)). Two earlier complete tenant + disposable PostgreSQL regression runs passed at the identical SHA. The operator recomputed file hashes, verified both manifests and checked local disposable-container cleanup; the underlying local artifact bytes were not uploaded for an additional remote verification.

- `COMBINED_MANIFESTS=PASS`, `PG_SQL_INTEGRITY=PASS`, `PG_BACKUP_INTEGRITY=PASS`, `PG_EVIDENCE_MARKERS=PASS`, `PG_CONTAINER_CLEANUP=PASS`.
- Reviewed migration SQL SHA-256: `2c6175976c82463eea5d40a3082528b8d72e1505f8803188a3c4473129bb7ee7` (pinned review checksum).
- Synthetic PostgreSQL backup SHA-256: `94c7c2cb1a2fcb93633c23efcb642764baea04f866e464e0728a22e06bdd664e` (matched to PG manifest).
- Tenant manifest SHA-256: `F544C05CE7DC829692072291660A60DAD40DCA00A68DD262B0EE125BF656BEE3`; PG manifest SHA-256: `738D715F7EA4D73A02DF09DBFEFA69530F6DBFD17BC2673DABFEB3DC5D1BA54B`.
- Latest tenant evidence directory: `D:\GitHub\YangSpaceApp-F2a1-Combined-QA-20261112-0822\logs\yangspace\f2a-tenant-scope\445db4d9a5d2-20261112-134120-cc109012`.
- Latest PostgreSQL directory: `D:\GitHub\YangSpaceApp-F2a1-Combined-QA-20261112-0822\logs\yangspace\pg-disposable\445db4d9a5d2-20261112-134126-299426c5`.
- **Accepted bounded gate:** combined source/tenant regression and disposable PG initial schema, constraints, sequential CAS, DB audit uniqueness rollback, synthetic backup/restore/integrity and isolated cleanup. No further retry of this same QA needed for Markdown-only changes.
- **Still HOLD:** actual Identity bearer authorization over PostgreSQL (existing F2a-2 real bearer TestServer acceptance is SQLite-only), parallel EF application writers, reviewer-controlled claim decision and ownership/membership grant/revoke, atomic application-level business+audit failure injection, PG least-privilege role, production migration/backfill/off-host recovery and release. No CT164/CT152, live DB, merchant activation or GitHub Actions.
- **PRs:** #19 MERGED/CLOSED; #17 OPEN/DRAFT/HOLD; #1 OPEN/DRAFT/HOLD. Next work is [F2a-3 Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) security design decisions before implementation. Tested code SHA remains `445db4d…`; any newer docs-only PR head must not be labeled as separately runtime-tested.

## 2026-11-12 13:41 — F2a combined HEAD exact-SHA QA accepted

| Item | Result | Evidence / remaining dependency |
| --- | --- | --- |
| #17 combined tenant regression | **DONE — exact-SHA operator PASS twice** at `445db4d9a5d28b798054c426afeba9b9be2c771b` | `f2a-restore`, `f2a-build`, `f2a-tenant-scope` PASS, final tenant marker PASS; latest evidence `logs/yangspace/f2a-tenant-scope/445db4d9a5d2-20261112-134120-cc109012` in the operator-local QA worktree |
| #17 disposable PostgreSQL schema/constraints/recovery | **DONE — exact-SHA operator PASS twice** | Model-diff and SQL hash PASS; migration apply, FK/partial unique, CAS, DB audit rollback, backup/restore PASS; latest evidence `logs/yangspace/pg-disposable/445db4d9a5d2-20261112-134126-299426c5`. Local manifests/SQL/backup and container cleanup require independent SHA-256 postflight. |
| #19 actual Identity bearer tenant HTTP | **DONE — scoped SQLite/TestServer; merged/closed** | Tested code SHA `f579c70db9cc0986d2f1df8047158587f0e87eef`; no claim of real PostgreSQL bearer HTTP. |
| [#20 F2a-3 reviewer claim/membership lifecycle](https://github.com/jvalkovv/YangSpaceApp/issues/20) | **CONTRACT PROPOSED; implementation NOT STARTED** | Confirm reviewer role, self-approval denial, claim state machine, ownership transfers, membership re-grant, last-owner policy, transaction boundaries, audit and fault injection. |
| #17 PR + V2 #1 | **OPEN / DRAFT / HOLD** | PostgreSQL bearer HTTP/parallel writers, reviewer/mutation workflow, atomic application audit, least-privilege PG role and recovery/release gates outstanding. |

**No CT164/CT152 operations, live DB writes, merchant activation, deployment or GitHub Actions.**

## 2026-11-12 08:15 — F2a-2 read-only bearer authorization accepted

| Work package | Result | Evidence / next required gate |
| --- | --- | --- |
| F2a-1 domain + PostgreSQL schema foundation / [PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17) | **PARTIAL / DRAFT / HOLD**; current combined head \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\` | Earlier disposable PostgreSQL migration/constraints/backup-restore PASS is exact-SHA scoped to \`1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001\`; rerun source/build + disposable PostgreSQL regression on combined head. Then real PG bearer/parallel writer, reviewer claim lifecycle, atomic application audit/fault injection, least-privilege role and off-host restore. |
| F2a-2 read-only Identity bearer authorization / [PR #19](https://github.com/jvalkovv/YangSpaceApp/pull/19) | **DONE scoped acceptance; MERGED/CLOSED** via squash \`17d08530db7fed0bbe7ed4444577a7b0e6b73c4d\` into the F2a-1 feature branch | Exact tested code SHA \`f579c70db9cc0986d2f1df8047158587f0e87eef\`: Windows restore/build/tenant regression/real Identity bearer TestServer PASS; operator evidence postflight PASS. Manifest SHA-256 \`336D8A9DCB4C69483981A2A5124A5123E8AD9044C14A1619199439E2E121F366\`. Disposable SQLite/TestServer only; no production or PG bearer acceptance. |
| [F2a-3 reviewer claim decision + membership lifecycle — Issue #20](https://github.com/jvalkovv/YangSpaceApp/issues/20) | **NOT IMPLEMENTED / contract next** | Confirm dedicated reviewer authority, no self-approval, claim state machine, Owner grant/revoke, atomic ownership+membership+audit transaction, concurrency/unique conflicts, fault-injection and recovery before coding. |
| F2b owner CMS mutations + per-field audit / Issue #11 | **NOT IMPLEMENTED / BLOCKED by F2a** | Real PostgreSQL transactional API tests, expected-revision/409, field allowlist, one audit event per accepted change; no owner write endpoints yet. |
| V2 parent / [PR #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) | **DRAFT / OPEN / HOLD** | No V2-to-main merge, merchant activation, production rollout or pilot approval. |

**PR inventory at this checkpoint:** #19 MERGED/CLOSED; #17 OPEN/DRAFT/HOLD; #1 OPEN/DRAFT/HOLD. No other open PR is 100%-accepted for closure. No GitHub Actions, CT164/CT152, live DB changes or deployment.

## F2a start — 2026-11-11: authorization/audit design gate

| Work package | Result | Next required evidence |
| --- | --- | --- |
| YS-S2-01 / ADR-019 production provider | **Target PostgreSQL ACCEPTED; rollout HOLD** | [ADR-019](ADR-019-POSTGRESQL-PRODUCTION-TARGET.md): operator topology, PG-specific EF migrations, backup/restore, and DB hardening not yet done |
| YS-S2-04 / Issue #4 | **F2a contract PROPOSED** | [F2a ownership and audit proposal](F2A_TENANT_OWNERSHIP_AND_AUDIT_CONTRACT_20261111.md); no schema migration/merchant endpoints |
| YS-S2-05 / tenant access | **NOT IMPLEMENTED** | Real bearer owner A↔B 404/no-leak negative tests, grant/revoke, old-token denial |
| Issue #11 F2b / audit + concurrency | **NOT IMPLEMENTED** | Transactional one-event-per-change and per-resource revision 409 tests |
| F3 owner confirmation/moderation | **BLOCKED by F2** | Reviewed membership/actor source and valid confirmation event |

**No active CT164 DB or public endpoint changes.** This is a status snapshot describing a proposed design; no acceptance of the provider ADR or security controls is implied.

## 2026-11-11 18:22 — F1 freshness delivery checkpoint (current)

The **current V2 application source** after merged [PR #14](https://github.com/jvalkovv/YangSpaceApp/pull/14) is `403b281abad2fb0c8048c2763a606c3435f3e5dd` (squash SHA, not separately QA-run). The exactly tested F1b source is `2634014f64fcff237b29564d0572643f2cba034a`. This checkpoint supersedes the 07:xx source snapshot below **for current workstream status only**.

| Task / requirement | Result | Evidence and boundary |
| --- | --- | --- |
| Issue #11 F1a — truthful public freshness | **DONE scoped source/QA**; PR #13 **MERGED/CLOSED** | Candidate `9656518...`: source+API tests PASS; squash `54b47576...`; no live deploy |
| Issue #11 F1b — nullable publication lifecycle timestamps | **DONE scoped source/QA**; PR #14 **MERGED/CLOSED** | Candidate `2634014...`: Source+Playwright, real API/security, EF initial→F1b upgrade and recovery all PASS on disposable DB; 9 nullable historical columns; squash `403b281...` |
| Local F1b verify evidence | **PASS at candidate SHA** | `logs/yangspace/verify/2634014f64fc-20261111-181828-f2898981`; `logs/yangspace/api-integration/2634014f64fc-20261111-181923-5dbe2df0`; `logs/yangspace/sqlite-runtime-integrity/2634014f64fc-20261111-181244-81cbfb42` (under isolated operator worktree) |
| Issue #11 F2/F3 — owner-scoped field audit & confirmation | **NOT STARTED** | Requires Organization/Member claims and negative cross-tenant tests; no real owner CMS entitlement |
| Parent PR #1 and public pilot gates | **DRAFT/OPEN; HOLD** | No CT164 active DB migration, release, off-host recovery/ingress acceptance; current public demo still older release `a480280...` |

The previous checkpoint below is retained as historical evidence; do not use its old `2baeb797...` SHA or open-PR list as the current status.

## 2026-11-11 latest verified source QA & PR hygiene

This checkpoint **supersedes older exact-SHA source-QA TODO entries for engineering validation only**, without changing the historical rows below. It does not mark the full S0/security/release/production gate DONE.

| Item | Evidence | Scoped result |
| --- | --- | --- |
| Combined V2 feature commit `2baeb797b4d9ca0f5ce9d6c3d311a7b8092be7dc` | Owner confirmed fetched SHA and ran `Verify-Source-Local.ps1 -CommitSha` and `Verify-Api-Local.ps1 -CommitSha` on **same** SHA | `SOURCE_VALIDATION=PASS`, `REAL_API_INTEGRATION=PASS`; disposable DB only |
| Frontend/typecheck/browser/restore/build | Source runner stages `web-lock`, `web-install`, `web-audit`, `web-typecheck`, `web-build`, `web-playwright`, `api-restore`, `api-build` all `STAGE_PASS` | **PASS** for exact SHA |
| Real auth/API integration | API runner `api-prepare`, `api-restart-verify`, `api-bootstrap-denial`, `api-post-bootstrap-verify`, `api-post-bootstrap-recovery` pass | **PASS**; escalation denied, non-admin role retained |
| Local operator artifacts | `logs/yangspace/verify/2baeb797b4d9-20261111-074951-04b1acd1`, `logs/yangspace/api-integration/2baeb797b4d9-20261111-075201-77648186` on operator Windows PC | Local logs **reported**, not Git-tracked |
| Completed child PRs | [#9](https://github.com/jvalkovv/YangSpaceApp/pull/9) and [#10](https://github.com/jvalkovv/YangSpaceApp/pull/10) | **MERGED/CLOSED**; [parent #1](https://github.com/jvalkovv/YangSpaceApp/pull/1) stays **Draft/Open** |
| Business freshness requirement | [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11) | **OPEN** for implementation; not part of completed UI fixes |
| Operational gates | Current live demo reported at `a480280...`; no fresh exact-candidate activation, off-host restore, verified effective firewall, live merchant auth/audit | **HOLD** |

**Ongoing owner requirement:** provide concise evidence-linked progress at each meaningful checkpoint (plus existing hourly GitHub change watch); distinguish source QA from runtime deployment, and merge/close only fully scoped accepted/owner-approved PRs. Do not close unmerged PRs for list hygiene. Canonical policy: [MVP roadmap](MVP_ROADMAP.md#delivery-governance--mandatory-progress-reporting--pr-closure-accepted-2026-11-11).

## 0. Evidence baseline (confirmed, no rerun by this document)

| ID | Item | Status | Evidence / qualification |
|---|---|---|---|
| YS-B-01 | Homepage green/cream design and visual acceptance | DONE | 375/390/768/1024/1440 baseline; post-fix screenshot review, docs/HOMEPAGE_ACCEPTANCE.md |
| YS-B-02 | Local Angular typecheck and production frontend build | DONE | Windows console output, 2026-11-09 |
| YS-B-03 | Local API + frontend + proxy operational | DONE | `LOCAL_STACK=PASS`, 5 HTTP checks |
| YS-B-04 | Automated browser regressions | DONE | 17/17 Playwright PASS on `20aa30c`, 18.2s; mocked API |
| YS-B-05 | Public read-only demo API | PARTIAL | 6 fictional locations/3 offers/1 brochure/1 job; no real public-data acceptance |
| YS-B-06 | Preview administrator UI + API | PARTIAL | Source present; Playwright auth test mocks backend; real CRUD lifecycle unverified |
| YS-B-07 | Manual source and API validation | DONE at `b30c1c8` | Exact same SHA source 8/8 PASS, API real integration and bootstrap non-escalation PASS; operator logs `logs/yangspace/verify/b30c1c819aa1-20261109-160220-9e00ef05` and `logs/yangspace/api-integration/b30c1c819aa1-20261109-160130-c608197c` |
| YS-B-08 | PVE LXC deployment | PARTIAL | CT164 created/started by operator on 2026-11-10 (Debian 13.6, DHCP 192.168.1.110, 2 vCPU/4 GB/20 GB); app/release, security, recovery and proxy remain HOLD. See docs/CT164_PROVISIONING_20261110.md |

## S0 — Engineering reproducibility and risk closure (START HERE)

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S0-01 | P0 | Record baseline, source/QA scope, branch protection and current blockers | PR #1 has precise accepted homepage SHA; release gates and documentation agreed | DONE |
| YS-S0-02 | P0 | Freeze dependency graph | Commit lockfile; validate npm ci from a clean SHA archive, dependency audit and .NET restore | DONE for verified `133cfd6`: committed lockfile, clean Git archive, npm ci, audit and .NET restore/build all PASS; source, npm ci, audit and .NET verified again at `6e8ba7b`; revalidate next HEAD |
| YS-S0-03 | P0 | Manual reproducible source QA | `Verify-Source-Local.ps1` on exact SHA: npm ci, npm audit, Angular build, 17+ Playwright, .NET restore/build and logs | DONE at `b30c1c8`: 8/8 stages PASS, `logs/yangspace/verify/b30c1c819aa1-20261109-160220-9e00ef05`; new release-policy HEAD requires revalidation before build |
| YS-S0-04 | P0 | Real .NET API integration | `Verify-Api-Local.ps1`: isolated temp DB, random loopback port, guest/admin auth, publish/expiry/hide, process restart | DONE at `b30c1c8`: guard, restore/build, real API prepare/restart and bootstrap denial + nonadmin recovery PASS; `logs/yangspace/api-integration/b30c1c819aa1-20261109-160130-c608197c`; new release-policy HEAD needs its own SHA evidence |
| YS-S0-05 | P0 | Security inventory of new & legacy surfaces | Default-deny preview registration, fail-closed bootstrap, legacy secret assessment/rotation, protected preview, ownership/rate-limit plan | PARTIAL — SEC-01 registration deny and SEC-02 nonadmin anti-promotion both runtime PASS at `b30c1c8`; legacy secret reuse/rotation, private preview network boundary and future tenant authorization still open |
| YS-S0-06 | P1 | Correct PVE release candidate policy | Remote-tip SHA, reviewed-source/script identity, verified QA scope, protected reviewer approval bound to two SHA-256 manifest digests; inactive build only | PARTIAL — Windows Git Bash synthetic policy all positive/negative cases PASS at `04b1505` (`SIMULATED_MSYS_NTFS`); actual Linux approval ACL, trusted evidence transfer and private CT release acceptance deferred to dedicated YangSpace LXC |
| YS-S0-07 | P1 | Repo hygiene and legal ownership | LICENSE decision, review/branch policy, pinned SDK/node docs, changelog and manual QA instructions | TODO |

**S0 exit:** S0-02/03/04/05/06 verified by manual evidence, not Actions; no open P0 defect; only then mark G0 PASS. Source code and local browser success alone are insufficient.

## S1 — Authenticated real CMS vertical slice

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S1-01 | P0 | Real preview admin bootstrap and authenticated login | Bootstrap secret protected, no default credentials; admin role grant verified; non-admin denied; register endpoint gated | PARTIAL — backend authorization/SEC-02 previously runtime PASS; Angular login now checks authenticated `/admin/me` PlatformAdmin before enabling CMS; new browser tests prepared, pending container runtime |
| YS-S1-02 | P0 | Location full CRUD + hide/restore | Create/edit/publish/hide/restore persist across restart, validate duplicate slug & bad input, positive/negative API tests | PARTIAL — API restore added and idempotent; editor supports save, hide and restore with draft state; updated disposable-DB API regression and mocked Playwright scenarios pending CT runtime |
| YS-S1-03 | P0 | Offers, brochure links, jobs full CRUD | Admin-only list/create/edit/archive/restore, valid/expired public filtering, status synchronization and negative tenant-scoped path tests; hard delete intentionally forbidden | PARTIAL — S1-03 source implemented with separate content manager and REST endpoints; PowerShell disposable-DB + Playwright regressions prepared, unexecuted; real CT acceptance pending |
| YS-S1-04 | P0 | Admin page production-grade UI | Forms, validation, loading errors, status, edit modals/pages, accessibility + screenshots | PARTIAL — real edit/publish UI, role verified login, validation, conflict handling, request timeout and responsive editor source prepared; visual/keyboard/manual CT acceptance pending |
| YS-S1-05 | P0 | End-to-end unmocked workflow | Admin → DB → GET public → hide → public absent → restart → persistent; negative auth evidence | TODO |
| YS-S1-06 | P1 | Demo fixture isolation | Fictional samples are idempotent in preview; no accidental public real-data migration, environment-switch tests | PARTIAL |
| YS-S1-07 | P0 | Production-safe CMS access and first-admin enrollment | Bearer-only PlatformAdmin, gateway-origin policy, no registration, offline one-time admin grant, approved database schema, explicit security preflight | PARTIAL — source gates/operator CLI/negative startup test prepared; NO migrations, live proxy-ACL review, deployment or runtime proof |

**S1 exit:** admin can complete real DB-backed workflows and an unprivileged caller cannot perform them; logs + screenshots recorded.

## S2 — Domain, production persistence and ownership

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S2-01 | P0 | DB ADR: PostgreSQL vs SQL Server | Written benchmark, license/ops/spatial/backup/restore comparison and selected provider | TODO |
| YS-S2-02 | P0 | EF Core migrations/versioning | Clean init + upgrade + rollback compatibility/drill, no `EnsureCreated` in production path | PARTIAL — pinned local EF Core 10 design-time tool, guarded no-DB factory and exact-SHA PowerShell migration scaffold operator committed; generated InitialDirectorySchema + SQL review + runtime DB apply/rollback still PENDING |
| YS-S2-03 | P0 | Organization/Location/Geo taxonomy | Relational constraints, seeded normalized lookups, categories/hours/geo indexes | TODO |
| YS-S2-04 | P0 | Owner/member/claim workflow | Verified reviewer decision creates scoped membership; revoke works; audit, denial and cross-tenant tests | TODO |
| YS-S2-05 | P0 | Policy-based tenant isolation | Tests for owner A vs owner B across all private endpoints (reads/writes); no global `IsOwner` shortcut | TODO |
| YS-S2-06 | P1 | Data lifecycle and GDPR controls | Retention, subject access/correction, source & verified_at, traceable admin changes | TODO |

## S3 — Merchant CMS, publications and moderation

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S3-01 | P1 | Merchant dashboard/location editor | Organization and branch picker; clear edit/draft/review/publish states | TODO |
| YS-S3-02 | P1 | Offers campaigns & automatic expiry | Draft→review→publish→expire, timezone/UTC edge-case tests, worker reconciliation | TODO |
| YS-S3-03 | P0 | Trusted media upload/preview | MIME sniffing, file limits, safe filenames/object keys, quarantine/scan policy, rights attestation, restore | TODO |
| YS-S3-04 | P1 | Brochures and job postings lifecycle | Verified rights, valid dates, outbound job links, no CV storage; positive/negative tests | TODO |
| YS-S3-05 | P1 | Moderation/admin audit/reporting | Review queue, reasoned approve/reject/suspend, abuse reports, immutable audit trail | TODO |
| YS-S3-06 | P1 | Owner/admin UX acceptance | Real multi-role scenario across 375/390/768/1024/1440px, keyboard tests, screenshot evidence | TODO |

## S4 — Search, SEO and quality

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S4-01 | P1 | Browse/search by neighborhood/category/geo | Accurate filtering, query validation, DB indexes, boundary tests, pagination & dedupe | PARTIAL |
| YS-S4-02 | P1 | Public profile enrichment | Coordinates, map/directions, verified freshness, contacts, opening hours & exceptions | TODO |
| YS-S4-03 | P1 | Dedicated offers/brochure/jobs indexes | Public listing/search pages, published/valid-only, no leakage of hidden entries | TODO |
| YS-S4-04 | P1 | SEO SSR/prerender architecture | Canonicals/meta/structured data/sitemap, indexable public pages, demo/noindex isolation | TODO |
| YS-S4-05 | P1 | Accessibility/performance & analytics | A11y, full visual QA, real browser Core Web Vitals + privacy-aware action analytics | TODO |

## S5 — PVE private preview (after S0+S1)

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S5-01 | P0 | Read-only PVE inventory and topology decision | Available CT ID, OS, IP, storage, backup, gateway/DNS/TLS and resource budget documented | PARTIAL — host+CT+storage+bridge inventoried; actual DHCP .110, reservation/DNS, thin-pool risk, independent backup and gateway ACL still open |
| YS-S5-02 | P0 | Provision isolated LXC and services | Unprivileged where feasible, systemd, Nginx, correct service/deploy user, secrets and private network | PARTIAL — CT164 Debian 13.6 unprivileged guest created and booted; service accounts, network ACL, OS hardening and app services pending |
| YS-S5-03 | P0 | GitHub pinned-SHA controlled release | Exact approved SHA + manual QA manifests, `releases/<sha>`, atomic activation, no auto-pull/restart | BLOCKED |
| YS-S5-04 | P0 | Backup/restore/rollback exercise | Consistent DB/media backup, off-host copy, restore into scratch, bad-release revert, command logs | BLOCKED |
| YS-S5-05 | P1 | Observability/maintenance | Separate API/proxy/deploy/verify logs, health, error-rate/latency/disk alert, restart policy | TODO |
| YS-S5-06 | P0 | Protected preview acceptance | Browser on private TLS/ACL endpoint, persistence after reboot, auth denied to outsiders, smoke PASS | BLOCKED |

## S6 — Public pilot, first neighborhood (after G2/G3/G4/G-PREVIEW)

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S6-01 | P1 | Confirm first neighborhood and pilot outreach | One selected neighborhood, merchant contact process, rights/source register | TODO |
| YS-S6-02 | P1 | Recruit verified merchant content | First consented real businesses + owner verification, no copy/paste of third-party protected materials | TODO |
| YS-S6-03 | P0 | Public security/legal review | Permissions, uploads, privacy, T&C, reports/takedown, promotions rights and accessibility checked | TODO |
| YS-S6-04 | P0 | Production readiness drill | TLS/firewall, migrations, no demo data, restore/RPO-RTO, load, alerts, release audit | TODO |
| YS-S6-05 | P1 | Launch KPI and iteration | Fresh profiles and useful visitor actions, merchant/owner activity and support time measured | TODO |

## S7 — Varna / Bulgaria scale (gated)

| ID | Work package | State |
|---|---|---|
| YS-S7-01 | Multi-neighborhood Varna onboarding, duplicate/quality detection | HOLD |
| YS-S7-02 | Subscription pricing/entitlements and payment ADR after pilot evidence | HOLD |
| YS-S7-03 | National expansion capacity/data governance/operational playbooks | HOLD |

## Current active queue (strict order)

1. `YS-S0-02` — lockfile committed; verify clean exact-SHA npm ci, audit and .NET restore.
2. `YS-S0-03` — run [S0 Manual QA](S0_MANUAL_QA_RUNBOOK.md) with `Verify-Source-Local.ps1` and inspect logs.
3. `YS-S0-04` — run `Verify-Api-Local.ps1` and inspect auth/DB/expiry/restart evidence.
4. `YS-S0-05` — auth/secrets/preview exposure review.
5. `YS-S0-06` — solve Draft PR ↔ PVE approved SHA restriction.
6. `YS-S1-01..05` — finish real content CRUD and workflow.
7. `YS-S5-01` — begin PVE **read-only** inventory once PVE preview scope is authorized.

## Per-commit evidence template

```text
TASK_ID:
BRANCH:
COMMIT_SHA:
CHANGED_FILES:
TEST_COMMANDS_AND_RESULT:
BROWSER_SCREENSHOTS:
API_DB_INTEGRATION_EVIDENCE:
SECURITY_NEGATIVE_TESTS:
RUNTIME_TARGET (local / private PVE / public):
BACKUP_REF (if mutation):
ROLLBACK_PATH:
REVIEWER:
GATE_DECISION: PASS / HOLD
```

**Never mark a task complete because only the code exists.** Record failing tests as BLOCKED with root cause, not as completed with an exception.

### 2026-11-09 — Evidence pair and security hardening state

The operator's API and source logs **both PASS** for `f3f4e1c9e29a640bab2637fac9ff98098747e41a` (S0-02/03/04 accepted at that SHA only). Next source update modifies only preview authentication/bootstrap behavior, explicit default config and integration checks; before any G0 release candidacy both operator checks must re-run against the exact new HEAD. `YS-S0-05` remains PARTIAL because historical secrets/rotation, protected preview exposure and ownership model are unresolved. [Security review](SECURITY_REVIEW_V2_20261109.md). No GitHub Actions, no PVE deployment.

### 2026-11-09 — Matched-SHA S0 acceptance and SEC-02 regression extension

Operator-confirmed on `6e8ba7bf2b82773a087e19737bb64e29b732e7d1`: source QA eight stages PASS and real API test/negative registration guard PASS, with matching manifest commit. A follow-on SEC-02 test now exercises bootstrap with an existing regular identity on **the same disposable SQLite DB**, requires startup refusal, then restores original bootstrap settings and verifies both admin login and ordinary-user denial. **New test source only; runtime PASS pending on the next commit.** Historical secrets, private TLS/ACL access and merchant ownership remain open. No PVE or GitHub Actions.

### 2026-11-09 — S0-06 manual release evidence policy

Current verified operator candidate `b30c1c819aa1aac1cd056c4fb8a03c4b7a469dd9` Source and API integration both PASS, including SEC-02. A new **source-only** S0-06 release gate requires a protected operator approval with exact SHA/ref and SHA-256 of both manifests; rejects Playwright SKIPPED and missing SEC-02 assertions; verifies origin and that executing scripts match candidate bytes; never activates. Synthetic negative-case Bash test added. Linux/PVE execution **NOT TESTED**, keep G0 HOLD and the private preview undispatched. See [PVE runbook](POC_PVE_RELEASE_RUNBOOK.md) and [Issue #5](https://github.com/jvalkovv/YangSpaceApp/issues/5).

### 2026-11-09 — S0-06 Git Bash false rejection remediation

Operator fetched `7d60705030f6230a7032af15ead794badd9063d3`. Three scripts passed `bash -n`, but `test-preview-release-evidence.sh` rejected its FIRST valid fixture with `RELEASE_BLOCKED: expected unique COMMIT_SHA value in source.manifest`. This is a release-policy test failure, not a source/API regression. The previous normal Source QA + Real API Integration PASS at `b30c1c8` remain valid for that SHA. Updated Bash manifest comparator checks exactly one required key and exact content, accepting one optional CR before LF without relying on `grep -x` line-ending behavior. New synthetic test cases include LF, source CRLF, all-manifest CRLF, duplicate SHA, appended suffix, and explicit expected negative rejection reason. Local Linux unit test of comparator PASS (LF/CRLF positive; duplicate/suffix/double-CR rejected); full Git Bash policy test on the new SHA is **PENDING**. Restore executable bit on build shell script. No deployment or GitHub Actions.

### 2026-11-09 — Windows Git Bash: NTFS permission-mode test mismatch

Operator pulled `3d0e6e94fc3768b28c759aef7fe72b0182931086`, and all three release scripts passed `bash -n`. Synthetic fixtures reported PASS for normal LF, Windows CRLF source/all manifests, duplicate SHA, modified value, skipped browser, incomplete bootstrap, failed API, incorrect approval scope, duplicate evidence key, and wrong source ref. Next negative case `mutable_approval_file` **incorrectly succeeded** because `chmod 666` via Git Bash on NTFS did not yield reliable POSIX permission bits for `stat -c %a`. The synthetic test overall remained FAIL; this is **not** evidence of approval ACL enforcement on Linux.

**Source remediation (runtime pending):** retain the Linux release validator's real `stat` permission/ownership checks unchanged; explicitly refuse a real build outside Linux. For the Windows-only synthetic regression, inject a *test-only exported Bash stat function* returning mode 666 for the synthetic approval path, assert the injection is active, and verify the validator rejects the mutable-mode condition. Linux synthetic testing performs an actual `chmod 666` and validates the permission bit change (no simulation). The final policy output reports `POLICY_PERMISSION_SOURCE=SIMULATED_MSYS_NTFS` or `REAL_LINUX_MODE`. Linux runtime permission evidence still required for S0-06 completion. No PVE or GitHub Actions.

### 2026-11-09 — Git Bash inside-checkout negative fixture correction

Operator ran S0-06 synthetic policy on commit `0c577434885a54a98c30dcf682914dbd9c4625ee`. Shell syntax and the full positive LF/CRLF set passed, all negatives through `mutable_approval_file` and `missing_approval` passed; the final `evidence_inside_checkout` case failed with `RELEASE_BLOCKED: SOURCE: absolute readable regular file (not symlink) required` instead of the expected **inside checkout** refusal. Root cause: the test computed REPO from `git rev-parse --show-toplevel`, which can return a Windows drive-qualified Git path rather than a POSIX absolute path. The fixture never reached the intended boundary assertion. The release validator remains unchanged. Source-only fix canonicalizes the test REPO with `pwd -P` + `realpath -e` and uses the existing readable tracked test script as the deliberately forbidden fixture, asserting that it really resolves below the repository root. Full Git Bash retest **PENDING**, Linux ACL and real PVE preflight untouched. No auto deployment, no Actions.

### 2026-11-09 — S1 first location CMS vertical slice, target hostname agreed

User accepted future in-container smoke/QA at `yangspace.gndstore.eu` when a dedicated YangSpace LXC is provisioned; **do not introduce Ubuntu/WSL or a separate permanent QA stack now**. No DNS/CT/ports/production state changed at this stage. Use a disposable DB and an isolated, ephemeral local test process even when run on that future CT; do not run destructive CRUD tests against the publicly served database. Check browser UX and private authentication through the real hostname only after protected TLS/gateway/ACL acceptance.

New source work: `apps/api/AdminEndpoints.cs` has idempotent admin-only `POST /locations/{id}/restore` (204/404); existing hide remains. `apps/web/src/app/admin.component.ts` verifies admin role before showing CMS, supports create/edit/publish/hide/restore, validates location inputs, handles conflict/nonadmin/timeout and keeps token in memory. CSS adds responsive editor/list states. `scripts/Invoke-Api-Integration.ps1` adds nonadmin 403, missing-record 404, double restore 204, public 200, re-hide and hidden-read cases; `apps/web/e2e/admin-cms.spec.ts` adds mocked login/edit/status/conflict regression. **Source only** — no Angular/.NET runtime tests or live endpoint acceptance for this new HEAD; mark S1-01/02/04 PARTIAL, S1-03/05 open. Keep PR Draft, G0 HOLD, no Actions/PVE deployment.

### 2026-11-09 — S1-03 publication lifecycle source / single-active-CT policy

One **active YangSpace LXC and one canonical app domain** (`yangspace.gndstore.eu`); no extra permanent dev/staging/QA instances or Ubuntu. Cloudflare A record/proxy configuration was shown by the operator but **application, TLS and public reachability have not been verified**. This does not approve a PVE write or immediate Internet-exposed preview authentication.

S1-03 source adds PlatformAdmin-only per-location `GET /locations/{id}/content`, `PUT /locations/{id}/{offers|brochures|jobs}/{recordId}`, and idempotent `POST /.../{recordId}/{hide|restore}` endpoints. All mutation records are looked up by both location and record identifiers; cross-location access is 404. `IsPublished` represents reversible archive (no physical DELETE); restoring expired content returns 409 until edited and valid. Public APIs continue filtering by parent published state, individual `IsPublished`, and UTC validity. A dedicated Angular publication manager renders all three collections, state, editable fields, archive/restore actions and conflicts; the existing creation controls cause it to refresh. CSS styles are responsive.

The expanded disposable-DB PowerShell integration regression covers unauthorized GET/PUT/restore 403, invalid inputs 400, cross-location 404, repeated hide/restore 204, expired-restore 409, public visibility and restart persistence. Mocked Playwright tests cover CMS changes and expired restore disabled.

**Evidence:** static source/diff review only; no .NET/Angular/Playwright/API runtime for this new HEAD. Windows validation scripts are not automatically compatible with Linux. Before publishing to a live domain, implement/verify Linux-compatible isolated acceptance, backup/rollback, protected admin security and Production-safe auth (the current API's preview-only admin gate is still disabled in Production). G0/G-PREVIEW HOLD, PR Draft. No Actions, PVE mutation or deployment.

### 2026-11-09 — Production CMS fail-closed separation (source only)

One active dedicated YangSpace LXC and canonical `yangspace.gndstore.eu`, no extra permanent QA environments. The **public directory** remains read-only when `Admin:Enabled=false`. The new `Admin:Enabled=true` **is NOT authorized for deployment**: it requires ASP.NET Production, Preview disabled, registration disabled, explicit AllowedHosts, an environment-injected 32-byte Base64 gateway secret, and a loopback peer that supplies the correct internal header after origin-side authentication. All admin writes additionally require the PlatformAdmin **Identity bearer** scheme. Production Identity exposes only `POST /api/auth/login` and `POST /api/auth/refresh`; cookie query options and registration are denied. The reverse proxy must verify Cloudflare Access/other approved authenticated operator policy at origin and strip client-supplied internal headers. The app alone cannot prove this edge/gateway integration.

Production database: an absolute SQLite file path outside releases, `Mode=ReadWrite`, existing file, actual reviewed EF migrations applied offline, and an offline-provisioned PlatformAdmin are mandatory. No PreviewBootstrap/EnsureCreated/fictional seeding in Production. First admin is created by an explicit interactive `--yangspace-provision-admin` operator command with separate consent and no HTTP listener; no existing user promotion or routine startup grants. **No EF migration has been generated**, and historical preview EnsureCreated DB must NOT be converted in place. Source-only negative startup tests added for missing/malformed gateway secret, missing SQLite DB and preview+production misconfiguration. Runtime tests, migrations, actual TLS/ACL, backup/recovery, and secrets rotation remain OPEN. G0/G-PREVIEW HOLD. No changes to PVE, actions, DNS, active service, or database.

Production persistence clarification: even with the CMS disabled, Production startup requires a pre-existing external `Mode=ReadWrite` SQLite database and a reviewed, applied EF migration with no model drift. The first-admin gate applies only when Admin.Enabled=true. This source increment does **not** include a migration; G0 and production activation stay HOLD.

### 2026-11-10 — S2-02 Initial EF migration tooling (not yet generated)

The current YangSpace branch has **zero** EF migration artifacts. A repeatable manual operator for the existing Windows machine is added at `scripts/New-InitialMigration-Local.ps1`: exact full Git SHA and branch check, clean worktree, pinned EF Core 10.0.0 local CLI/tool package, disposable in-memory design-time context (never Production or Preview DB), build-before/after, three generated C# files/snapshot, `has-pending-model-changes` check and initial SQL review script/hash manifest under ignored `logs/yangspace/migration-scaffold`. There is **no migration file yet** because EF tooling has not executed; hand-authored model snapshots are rejected. Generator never runs a DB update, starts listeners, modifies active DB, creates PVE resources or pushes GitHub. Source code static checks only; Windows dotnet runtime needs operator execution. Production remains blocked by its migration presence/drift guard and private proxy/security acceptance. See `docs/S2_INITIAL_MIGRATION_20261110.md`.
