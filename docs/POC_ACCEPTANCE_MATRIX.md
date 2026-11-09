# YangSpace 2.0 — PoC / Pilot Acceptance Matrix

**Purpose:** objective pass/fail checks. Each row has a test owner, target environment and evidence artifact. Existing Playwright screenshots use mocked HTTP responses. A browser screenshot is NOT a substitute for real API/database or security validation.

## Gate definitions

| Gate | Scope | Required result |
|---|---|---|
| G0 Engineering | Lockfile, SDK/npm, CI, backend tests, security inventory, pinned SHA policy | PASS before trusting deployable candidate |
| G1 Real local CMS PoC | DB-backed admin operations, auth negative tests, persisted lifecycle | PASS for private preview |
| G2 Production domain | Provider ADR, migrations, memberships, cross-tenant isolation, audit | PASS before real owner data |
| G3 Content + moderation | Full offer/brochure/job lifecycle, safe media, review queue | PASS before real business self-service |
| G4 Public discovery | Geo search, profile richness, SEO/indexing, a11y and performance | PASS before indexable pilot |
| G-PREVIEW Private PVE | Isolated CT, approved SHA, private TLS/ACL, backup/restore/rollback, observability | PASS before private stakeholder access |
| G-PILOT Public pilot | G2/G3/G4 + legal/safety/content/ops reviews and real data | PASS before public marketing/indexing |

## Exact scenario checklist

| Test ID | Environment / actor | Scenario | Expected assertion | Evidence/status |
|---|---|---|---|---|
| BUILD-01 | Clean checkout | `npm ci`, Angular typecheck/build | Reproducible with committed lockfile; no arbitrary mutation of dependency tree | TODO |
| BUILD-02 | Clean checkout | `dotnet restore`/build/test with audit as errors | No high/critical package issue; release compiles | PARTIAL: local API runs, CI not verified |
| CI-01 | GitHub PR | API smoke, tests, Angular build, browser regressions | All green on exact SHA, logs/artifacts retained | TODO |
| WEB-01 | Headless Chromium | Full homepage at 375/390/768/1024/1440 | No overflow, correct mobile/nav/hero/card layout | PASS: 17/17 suite evidence on 20aa30c |
| WEB-02 | Headless Chromium | Populated offers/brochures/jobs 390/768/1440 | Panels visible, no clipping, valid links | PASS on mocked content |
| WEB-03 | Headless Chromium | First load, filters, text query, rapid interactions | Correct cards and counts; never stale/mismatched | PASS on mocked content |
| WEB-04 | Headless Chromium | Direct business URL and API downtime/retry | Detail loads without click; unavailable != zero results; retry works | PASS on mocked content |
| API-01 | Real local API | `GET /api/v1/homepage` | 200 + schema/required fields; valid-only content | PASS smoke direct/proxy, not all business states |
| API-02 | Real local API/DB | Create location, restart API, read by slug | Persistent, correct public response | TODO |
| API-03 | Real API/DB | Admin edit/hide/restore business | Status and public visibility change correctly; reversible and audited | TODO |
| API-04 | Real API/DB | Create/update/remove offers/brochures/jobs | API returns correct status/validation; public sees only approved active entries | TODO |
| API-05 | Real API/DB | Expired future/past dates and timezone edge | No expired or scheduled-not-yet-valid public content | TODO |
| API-06 | Real API | Anonymous/nonadmin invoke admin/owner endpoints | 401/403/404; no DB mutation | TODO |
| AUTH-01 | Real identity | User register/login/password reset/lockout in intended environment | No open admin bootstrap, protected routes, rate limit, safe token handling | TODO |
| AUTH-02 | Owner A vs B | Guess another organization's ID and mutate/read private records | Forbidden consistently across location/content/media | TODO |
| AUTH-03 | Claim approval/revocation | Moderation grants and revokes membership | Reviewer-only, scoped role, audited | TODO |
| AUTH-04 | Non-browser upload/API | Filename/path-traversal/MIME/oversized payload/invalid PDF | Reject safely; no executable public upload; no unscanned downloads | TODO |
| DB-01 | Fresh DB | Apply all migrations from empty schema | Reproducible idempotent setup | TODO |
| DB-02 | Existing test DB | Upgrade schema with existing records | Data preserved; compatibility/rollback gate documented | TODO |
| DB-03 | Restore target | Restore DB + media backup into scratch | Records and links reconcile; RPO/RTO recorded | TODO |
| GEO-01 | Public listing | City+neighborhood/category/search combinations | No incorrect neighborhood hits or duplicates; stable pagination | PARTIAL: demo categories work |
| MEDIA-01 | Owner/moderator | Authorized brochure upload, preview, expire and remove | Type/size/rights checks, audit, no direct arbitrary URL assumption | TODO |
| MOD-01 | Owner/moderator | Draft → review → publish/reject → hidden | No unauthorized publication; state and audit consistency | TODO |
| MOD-02 | Visitor/moderator | Report inaccurate listing / unauthorized material | Acknowledgment, moderation queue, resolution history | TODO |
| SEO-01 | Public crawler | View indexable business details without relying on post-load JS | Correct canonical/title/schema/sitemap/robots | TODO |
| SEO-02 | Preview/hidden | Ensure previews, demo, hidden/private data excluded | No search-engine indexing or metadata leakage | PARTIAL: robots noindex in frontend |
| A11Y-01 | Keyboard & screen reader spot check | Nav/menu, filters, forms/errors and modals | Focus order, labels, announcements, usable keyboard actions | TODO |
| PERF-01 | Local/integration | Profile search & homepage p95 against realistic sample data | Baseline recorded, no N+1 regression, query plans reviewed | TODO |
| PERF-02 | Browser on target | LCP/CLS/INP plus asset request waterfall | Real measurements recorded on mobile/desktop, acceptance targets approved | TODO |
| OPS-01 | Protected PVE | Exact SHA release; restart; health/logs | Single trusted artifact deployed; no auto-deploy; URL private | TODO |
| OPS-02 | Protected PVE | Crash/reboot/volume pressure | Service recovers and alarms fire; data not in release root | TODO |
| OPS-03 | Protected PVE | Code rollback and DB restore rehearsal | Known-good release restored; migration safety documented | TODO |
| LEGAL-01 | Pilot review | Rights, price disclosure, privacy, moderation and terms | Review performed by appropriate expert; evidence retained | TODO |
| BIZ-01 | Pilot outreach | Source-verified businesses and owner contacts | Traceable evidence and freshness/review cadence | TODO |
| BIZ-02 | Pilot monitoring | Click-to-call/directions/offer/job action analytics | Privacy-aware useful-action metrics, no synthetic KPIs | TODO |

## Performance & reliability target proposal — NOT accepted SLAs

Initial hypotheses for representative pilot environment; must be confirmed on actual PVE hardware and user conditions:
- Error-free smoke: 100% of critical routes respond with correct status.
- API p95 for simple indexed public reads: explore **≤500 ms** at agreed pilot concurrency (excluding network) and record measured baseline.
- Browser Lighthouse/Core Web Vitals: target **LCP ≤2.5s, CLS ≤0.1** under documented device/network profile, with INP separately recorded.
- Recovery: propose **RPO ≤24 h, RTO ≤4 h**, subject to a full restore rehearsal; not a guarantee.
- Critical P0/P1 security vulnerabilities unresolved: **0** at public pilot go-live.
- Business expiry/search consistency: no expired offer/job visible after effective end time, even if worker queue is delayed.

## Evidence protocol

Each acceptance entry records **commit SHA, commands, host/environment, database fixture, HTTP status/result, screenshot/log, reviewer and timestamp**. Failed cases block gate; do not edit tests to hide defects. Promised tests cannot substitute execution records.

## Release decision template

```text
GATE:
CANDIDATE_COMMIT:
CODE_REVIEW:
SECURITY_REVIEW:
API_AND_DB_TESTS:
BROWSER_REGRESSIONS:
VISUAL_ACCEPTANCE:
BACKUP_REF:
RESTORE_DRILL:
DEPLOY_LOG:
OBSERVABILITY:
KNOWN_OPEN_ISSUES:
VERDICT: PASS | HOLD | NO-GO
```

## 2026-11-09 — Added S0 executable tests (source stage)

- `scripts/Prepare-Web-Lock.ps1` creates the true registry-resolved npm lockfile on a supported Windows Node installation, validates `npm ci`, Angular typecheck and build. **Do not synthesize a lockfile or treat CI artifact as committed.**
- `scripts/api-integration.py prepare|verify` accepts only a disposable `/tmp/yangspace-ci-*` DB with explicit `YANGSPACE_TEST_ISOLATED_DB=YES`, Staging and matching connection string. The CI API job creates a random bootstrap credential, runs smoke, executes real data mutations, restarts the API process, then confirms persistence and denied guest access.
- CI web job can temporarily produce a lockfile artifact for review, but G0 remains **HOLD** until it is committed and a fresh clone performs `npm ci` with no manifest drift.
- PVE build script fails closed without tracked lockfile and cannot activate a release automatically.
- **Evidence gap:** CI is not yet verified green on this code; don't mark BUILD-01, API-02..06 or G0 PASS.

### S0-02: Windows locked native dependency regression

Operator observed `EPERM unlink @lmdb/lmdb-win32-x64/node.napi.node` from a direct `npm ci` while the Angular development server may have been running. Run the corrected `scripts/Prepare-Web-Lock.ps1` **without stopping** live Angular. Required outputs: `VALIDATION_MODE=ISOLATED_WORKSPACE`, `LIVE_NODE_MODULES_MUTATED=NO`, `ISOLATED_NPM_CI=PASS`, `ISOLATED_ANGULAR_TYPECHECK=PASS`, `ISOLATED_ANGULAR_BUILD=PASS` and `LOCKFILE_SHA256_VERIFIED=YES`. A reviewed committed lockfile and clean CI `npm ci` are still required for G0.

### 2026-11-09 — S0-02 operator verification

Local Windows clean-temporary-directory `npm ci`, TypeScript check and Angular production build **all PASS** with lockfile SHA-256 consistency. Operator's `apps/web/angular.json` is locally modified, so a clean checkout + tracked lockfile + CI build remains required before release acceptance. `package-lock.json` is still untracked; G0 remains HOLD. Evidence comes from the supplied PowerShell command output, not a GitHub workflow run.

### 2026-11-09 — GitHub lockfile supply-chain acceptance

Lockfile v3 was committed at `94f710e` and the root manifest/registry/integrity records were inspected. CI has been tightened to require **tracked** lockfile, run `node scripts/verify-lock.mjs`, install with `npm ci`, then run `npm audit --audit-level=high`, typecheck, production build and Playwright. PVE release checks the same lock manifest before installation. **Acceptance remains conditional on a real green CI execution**; GitHub Actions returned no workflow runs in the observed period.
