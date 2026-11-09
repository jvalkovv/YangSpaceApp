# YangSpace 2.0 — Execution Tracker

**Tracked source baseline:** `20aa30c7da581316ba31900b6953025a3d1f3b00` (homepage code) · **visual QA docs:** `e2edefb70c648199ba8e82f83d7a3396424c88c4`.  
**Branch:** `feat/yangspace-v2-foundation-20261108` · **PR:** [#1](https://github.com/jvalkovv/YangSpaceApp/pull/1) (Draft).  
**Status values:** DONE = proven, PARTIAL = source present without full gate, TODO = not built/verified, BLOCKED = unmet dependency, HOLD = intentionally deferred.  
**Governance:** evidence is required before marking DONE. Do not infer progress percentages from file counts.

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
| YS-B-08 | PVE LXC deployment | TODO | No CT inventory or deployment executed; runbook/templates only |

## S0 — Engineering reproducibility and risk closure (START HERE)

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S0-01 | P0 | Record baseline, source/QA scope, branch protection and current blockers | PR #1 has precise accepted homepage SHA; release gates and documentation agreed | DONE |
| YS-S0-02 | P0 | Freeze dependency graph | Commit lockfile; validate npm ci from a clean SHA archive, dependency audit and .NET restore | DONE for verified `133cfd6`: committed lockfile, clean Git archive, npm ci, audit and .NET restore/build all PASS; source, npm ci, audit and .NET verified again at `6e8ba7b`; revalidate next HEAD |
| YS-S0-03 | P0 | Manual reproducible source QA | `Verify-Source-Local.ps1` on exact SHA: npm ci, npm audit, Angular build, 17+ Playwright, .NET restore/build and logs | DONE at `b30c1c8`: 8/8 stages PASS, `logs/yangspace/verify/b30c1c819aa1-20261109-160220-9e00ef05`; new release-policy HEAD requires revalidation before build |
| YS-S0-04 | P0 | Real .NET API integration | `Verify-Api-Local.ps1`: isolated temp DB, random loopback port, guest/admin auth, publish/expiry/hide, process restart | DONE at `b30c1c8`: guard, restore/build, real API prepare/restart and bootstrap denial + nonadmin recovery PASS; `logs/yangspace/api-integration/b30c1c819aa1-20261109-160130-c608197c`; new release-policy HEAD needs its own SHA evidence |
| YS-S0-05 | P0 | Security inventory of new & legacy surfaces | Default-deny preview registration, fail-closed bootstrap, legacy secret assessment/rotation, protected preview, ownership/rate-limit plan | PARTIAL — SEC-01 registration deny and SEC-02 nonadmin anti-promotion both runtime PASS at `b30c1c8`; legacy secret reuse/rotation, private preview network boundary and future tenant authorization still open |
| YS-S0-06 | P1 | Correct PVE release candidate policy | Remote-tip SHA, reviewed-source/script identity, verified QA scope, protected reviewer approval bound to two SHA-256 manifest digests; inactive build only | PARTIAL — Git Bash 0c57743: syntax and all policy cases through mutable-approval/missing-approval PASS; final inside-checkout negative failed for wrong reason due Windows drive-qualified repo path. Test-only POSIX canonicalization and regular-file fixture prepared; new runtime PASS/Linux ACL still pending |
| YS-S0-07 | P1 | Repo hygiene and legal ownership | LICENSE decision, review/branch policy, pinned SDK/node docs, changelog and manual QA instructions | TODO |

**S0 exit:** S0-02/03/04/05/06 verified by manual evidence, not Actions; no open P0 defect; only then mark G0 PASS. Source code and local browser success alone are insufficient.

## S1 — Authenticated real CMS vertical slice

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S1-01 | P0 | Real preview admin bootstrap and authenticated login | Bootstrap secret protected, no default credentials; admin role grant verified; non-admin denied; register endpoint gated | PARTIAL — backend authorization/SEC-02 previously runtime PASS; Angular login now checks authenticated `/admin/me` PlatformAdmin before enabling CMS; new browser tests prepared, pending container runtime |
| YS-S1-02 | P0 | Location full CRUD + hide/restore | Create/edit/publish/hide/restore persist across restart, validate duplicate slug & bad input, positive/negative API tests | PARTIAL — API restore added and idempotent; editor supports save, hide and restore with draft state; updated disposable-DB API regression and mocked Playwright scenarios pending CT runtime |
| YS-S1-03 | P0 | Offers, brochure links, jobs full CRUD | Create/edit/delete/archive, valid/expired filtering and public visibility, admin UI/API status synchronization | PARTIAL |
| YS-S1-04 | P0 | Admin page production-grade UI | Forms, validation, loading errors, status, edit modals/pages, accessibility + screenshots | PARTIAL — real edit/publish UI, role verified login, validation, conflict handling, request timeout and responsive editor source prepared; visual/keyboard/manual CT acceptance pending |
| YS-S1-05 | P0 | End-to-end unmocked workflow | Admin → DB → GET public → hide → public absent → restart → persistent; negative auth evidence | TODO |
| YS-S1-06 | P1 | Demo fixture isolation | Fictional samples are idempotent in preview; no accidental public real-data migration, environment-switch tests | PARTIAL |

**S1 exit:** admin can complete real DB-backed workflows and an unprivileged caller cannot perform them; logs + screenshots recorded.

## S2 — Domain, production persistence and ownership

| ID | Priority | Work package | Acceptance / evidence | State |
|---|---|---|---|---|
| YS-S2-01 | P0 | DB ADR: PostgreSQL vs SQL Server | Written benchmark, license/ops/spatial/backup/restore comparison and selected provider | TODO |
| YS-S2-02 | P0 | EF Core migrations/versioning | Clean init + upgrade + rollback compatibility/drill, no `EnsureCreated` in production path | TODO |
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
| YS-S5-01 | P0 | Read-only PVE inventory and topology decision | Available CT ID, OS, IP, storage, backup, gateway/DNS/TLS and resource budget documented | TODO |
| YS-S5-02 | P0 | Provision isolated LXC and services | Unprivileged where feasible, systemd, Nginx, correct service/deploy user, secrets and private network | BLOCKED |
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
