# YangSpace 2.0 — Verification Status

> **CURRENT RULE (ADR-018, 2026-11-09): NO GitHub Actions.** Earlier hosted-CI notes below are historical and superseded. Source/API checks are operator-run from clean pinned commits with local evidence.

## Implemented (source, not yet acceptance-tested)

- `apps/api`: .NET 10 minimal API; EF Core SQLite **preview**; public catalog/meta/detail; demo data; Identity-based isolated PlatformAdmin content creation and hide.
- `apps/web`: Angular 22 mobile-responsive listing, filters, detail and demo admin workflow.
- `infra/pve`: sample Nginx/systemd + controlled pinned SHA GitHub build script; **no PVE provisioning was done**.
- `.github/workflows/main_yangspace.yml`: CI build + API smoke + Angular typecheck/build. **No deployment job**.

## Must pass before live preview

- [ ] GitHub CI API and Angular jobs green on exact commit.
- [ ] Local `dotnet run` and Angular `npm start` working together.
- [ ] Manual visual QA: 375/390/768/1024/1440 px, keyboard navigation, link targets.
- [ ] Admin login, create location/offer/brochure/job and hide verified, guest actions rejected.
- [ ] Verify dependencies, generate and commit `package-lock.json`, switch to `npm ci`.
- [ ] QA input validation and accessibility tests.
- [ ] Security review of ASP.NET Identity preview only and content media URL handling.
- [ ] PVE CT/DNS/TLS/backup/restore planning approved before deployment.

## Not implemented / explicitly deferred

Business-owner verification & memberships, production DB migrations and storage, direct uploads, true moderation/audit workflow, real local business data, SSR/SEO go-live, image asset pipeline, pricing/payments.

## Major caveat

SQLite `EnsureCreated` is intentionally enabled only in non-production `Preview:Enabled`; not a production migration strategy. Admin preview uses bearer token in memory (no localStorage). These are developer-preview controls, not authorization for public production launch.

## Local feedback — 2026-11-08

- **API:** Windows `dotnet run` failed with NU1903 because EF Core SQLite transitive dependency resolved `SQLitePCLRaw.lib.e_sqlite3 2.1.11` (GHSA-2m69-gcr7-jv3q). Remediation added: direct native-only reference **3.53.3** in `apps/api/YangSpace.Api.csproj`, without disabling security audits or mixing SQLitePCLRaw managed major versions. **Build/restore still needs re-test.**
- **Web:** `npm install` completed (reported zero vulnerabilities), but Angular CLI refused Node **24.14.1**; its actual supported ranges are Node 22.22.3+, 24.15.0+ or 26.0.0+. `package.json engines` and PVE validation are now aligned. **Node upgrade on local Windows host and Angular typecheck/build are still needed.**
- **CI:** pending passing run evidence; no production/PVE deployment.

## Homepage v2 design/performance source update (2026-11-08)

- Single `/api/v1/homepage` API for initial metadata, first page and offer/brochure/job highlights. `/api/v1/locations` remains for filtered pagination.
- EF Core `AsSplitQuery()` added for details with multiple related collections, reducing accidental cartesian multiplication.
- Angular homepage: cancel stale searches, debounce typing, 10s timeout + retry, nonempty loading skeleton instead of misleading “0 обекта”, responsive directory, categories, highlights, how-it-works and business CTA.
- New navigation anchors, focus states, reduced motion, mobile menu, no remote font import, localhost-only default dev server.
- Added API smoke assertions and read-only PowerShell timing script. **No local browser screenshots, build logs or latency numbers have been collected through GitHub connector.**
- Until the exact commit passes CI + manual viewport/browser acceptance, homepage status is **SOURCE COMPLETE / RUNTIME QA PENDING**, not 100% accepted.

## 2026-11-08 — Angular 22 zoneless repair

User confirmed read-only API performance: first request **28 ms**, warm median **4.4 ms**; `/api/v1/homepage` contract PASS. Screenshots revealed rendering after focus/blur and a detail page stuck on loading. Corrective source changes now explicitly notify Angular change detection after async HTTP results/errors; detail cancels prior route fetches; filters hide stale cards during pending requests. Removed unnecessary ZoneJS runtime polyfill. Real browser verification on this commit remains pending.

## 2026-11-08 — Visual QA follow-up

Additional browser screenshots show a selected category with an incompatible, previously cached card still visible while loading. Control event handlers now receive explicit `ngModelChange` values; filter/type/search actions synchronously invalidate displayed results, and cards render only when the current search has finished. Results are also validated against the requested category/neighborhood. Brand badge and desktop hero sizing corrected; sparse grid centered. Runtime acceptance remains pending.

## 2026-11-08 — Connectivity UX and local diagnostics
Screenshot shows Angular running on `127.0.0.1:4200`, but homepage data request failed. Root cause remains unconfirmed without same-time .NET status/logs; direct and proxied API addresses are consistent in source. Corrected failure status, misleading zero-count display, bootstrap Retry behavior; added local direct/proxy probe and 503/502 regression scenarios. No API/PVE modifications.

## 2026-11-09 — Windows integration evidence + browser QA bootstrap

User-provided logs confirm the local PVE-independent stack: `LOCAL_STACK=PASS`, direct homepage and Angular proxy HTTP 200, Angular `npm run typecheck` **PASS**, Angular production `npm run build` **PASS** (407.56 kB raw total; estimated 100.61 kB transfer; 2.621s compile on the local machine). These are **build and local HTTP metrics**, not browser LCP/FCP measurements.

`npm run test:e2e` did not execute: Windows reported `'playwright' is not recognized`. Root cause is missing locally installed test-runner executable after a `package.json` change; run `npm install` from `apps/web`, then `npx playwright install chromium` before retrying. Source now adds GitHub CI Chromium test execution and responsive viewport screenshots; green CI/browser evidence is still pending. Screenshots show a working directory and a correctly centered single matching card. The 727px-wide tall screenshot contains desktop-style layout and is not sufficient by itself to conclude mobile breakpoint failure without its CSS viewport width.

## 2026-11-09 — Playwright installed and all 14 browser regressions PASS

User installed project dependencies and Chromium on Windows. `npx playwright --version` reported **1.65.0**. `npm run test:e2e` executed all **14 tests**, **14 PASSED in 16.0s**. Five of these tests cover viewports 375, 390, 768, 1024 and 1440 CSS px and generate local full-page screenshots. Functional browser tests confirm first-load rendering, no stale filter cards, detail page, demo-admin UI, HTTP errors and retry (HTTP calls mocked by Playwright). In contrast with the previous entry, the browser test runner is now locally installed and functional. Verified release commit being tested was branch HEAD `dfe8e60` after user's fast-forward pull.

**Release gate:** Local Angular typecheck PASS, production build PASS, API/proxy health PASS, Playwright 14/14 PASS. **Still open:** manually inspect screenshots, test real unmocked workflows/admin/URL navigation, capture Core Web Vitals, pin npm lockfile, confirm GitHub CI run, security/release review. No PVE deployment or `main` merge.

## 2026-11-09 — Uploaded viewport screenshots reviewed and tablet clipping remediated in source

Received and inspected all five PNG artifacts from the user (375/390/768/1024/1440 CSS pixel widths). Desktop and mobile layouts appear consistent. The hero illustration was visibly cropped at 768px due to tablet sizing and horizontal translation, despite the previous overflow tests passing (hero itself hides overflow). Source fix bounds the map and its inner content within the tablet grid, removes horizontal translation, and reduces excessive empty height of the tablet-wide Jobs card. Added direct 768px hero bounds assertions and 3 new populated highlight variants at 390/768/1440, because the prior screenshot fixtures had no offers/brochures/jobs. **No claim of new runtime PASS until the updated tests run.** No PVE deployment or merge.

## 2026-11-09 — New tablet fix & populated-content Playwright PASS

User Windows terminal confirms `git pull --ff-only` advanced `dfe8e60..20aa30c`, then `npm run test:e2e` executed **17 browser tests, all PASS (18.2 seconds)**. Five viewport tests including the new 768px hero-element containment assertion passed. Three new populated offers/brochures/jobs cases passed (390/768/1440px). Nine prior functionality regressions passed. This supersedes the prior 14/14 result for regression acceptance. QA is PASS for this exact source commit only; fresh visual screenshots still require review. GitHub connector currently reports no CI workflow runs for this commit. No deployment or merge.

## 2026-11-09 — Final homepage preview visual review: PASS

Operator provided four post-fix full-page Playwright screenshots in `YangSpace-Final-Visual-QA.zip`: `home-768` (768×5031), `populated-390` (390×7451), `populated-768` (768×5082), and `populated-1440` (1440×4302). Manually reviewed: 768px hero map/callouts are no longer cropped, and mobile/tablet/desktop layouts are visually coherent with populated offers, brochure and jobs cards. No blocking clipping or overlaps found. **Homepage PREVIEW visual acceptance: PASS** on source `20aa30c`; no new CSS changes required.

Operator reported Playwright **17/17 PASS (18.2 s)**, Angular typecheck/build PASS and local API/proxy checks PASS. This does **not** constitute full product or production acceptance: CI runs absent, real unmocked admin/data flows, lockfile, production persistence/security and PVE deployment remain open. PR #1 must stay Draft pending those gates.

## 2026-11-09 — PoC execution plan published (documentation-only)

Added canonical end-to-end product/technical PoC master, stateful execution tracker with task IDs, detailed acceptance matrix and private PVE release/recovery runbook. Baseline source remains `20aa30c` (homepage); user-confirmed Angular build, local API/proxy checks and 17/17 browser tests remain the only accepted runtime evidence. This is **planning, not functionality delivered**. Critical open items include npm lockfile, GitHub CI evidence, real backend-admin integration, Identity/security, production database/ownership, PVE candidate SHA policy, protected preview deployment and all public-pilot gates. PR #1 stays Draft; no runtime, credentials or PVE changes were made.

## 2026-11-09 — S0 execution begins: lockfile and real API testing (source ready)

Task `YS-S0-02`: added PowerShell lockfile generation/reproducibility script. GitHub CI temporarily creates an **artifact-only** `apps/web/package-lock.json` when one is missing, uploads it for review, and then runs `npm ci`. This is a transitional bootstrap **not** a release PASS until the lockfile has been reviewed and committed. PVE release build now fails closed if the reviewed SHA has no tracked lockfile; it uses `npm ci` rather than `npm install`.

Task `YS-S0-04`: introduced guarded Python stdlib integration tests that only run in a temporary `/tmp/yangspace-ci-*` SQLite database in Staging preview mode with a random CI bootstrap admin password. Tests cover guest denial, login, create/edit/publish, duplicate slug and invalid input, active vs expired/future offers, brochure/job creation, hide and persistence after a **real process restart**. CI API job now runs the workflow alongside the public smoke tests.

Task `YS-S0-03`: CI trigger now includes the active branch in addition to PRs targeting main; **a GitHub Actions green run still has to be observed**. No secrets/credentials were committed. All application binaries and homepage UI remain untouched. No PVE mutations.

**Status:** source implementation prepared; npm lockfile absent, full CI runtime evidence pending, G0 HOLD. Do not mark these tasks DONE until checks pass.

## 2026-11-09 — S0-02 Windows LMDB lock failure and isolated validation

Operator ran the previous `Prepare-Web-Lock.ps1`; real `npm install --package-lock-only` returned success, but direct `npm ci` failed with `EPERM unlink apps/web/node_modules/@lmdb/lmdb-win32-x64/node.napi.node`. Windows denied replacement of a native file in the live dev-server dependency tree. The specific lock owner was not independently identified, but an active Angular dev server is a likely cause.

**Remediation committed:** the PowerShell script now creates the registry-resolved lockfile using `--package-lock-only` in the real web folder, copies source into a unique TEMP validation workspace excluding active `node_modules` and build caches, compares lockfile SHA-256, then runs `npm ci`, typecheck and build only in that isolated workspace. Robocopy uses standard exit code checks; clean-up is best-effort. No elevated privileges or process termination required.

**Evidence status:** source change only, Windows rerun required. A local untracked `apps/web/package-lock.json` may already exist from the first successful command; it is not committed. `YS-S0-02=PARTIAL`; G0 HOLD.

## 2026-11-09 — S0-02 isolated Windows verification: PASS; Git lock gate still HOLD

Operator ran `scripts/Prepare-Web-Lock.ps1` on branch `6449990` after fast-forward pull. Results: `VALIDATION_MODE=ISOLATED_WORKSPACE`, `LIVE_NODE_MODULES_MUTATED=NO`, `ISOLATED_NPM_CI=PASS` (243 packages), `ISOLATED_ANGULAR_TYPECHECK=PASS`, `ISOLATED_ANGULAR_BUILD=PASS`, `LOCKFILE_GENERATION=PASS`, `LOCKFILE_SHA256_VERIFIED=YES`. Production web bundles: 407.88 kB raw and 100.68 kB estimated transfer, compiled in 2.994 s. Build was run in a temporary copy sourced from the operator's working tree and is not independently equivalent to a clean checkout of the commit.

`git status --short` reports `?? apps/web/package-lock.json`, `M apps/web/angular.json`, and two untracked visual QA ZIP archives. The Angular manifest differs locally; do not discard or stage it without reviewing its diff. The lockfile exists **only on the operator's machine**, not in GitHub, and must be reviewed+committed to close `YS-S0-02`. GitHub Actions REST still reports `total_count=0`; do not assert CI PASS. No PVE or DB change.

## 2026-11-09 — S0-02 lockfile committed at 94f710e, supply-chain guard added

Operator committed and pushed `apps/web/package-lock.json` at `94f710e505a2df81b1cc542e7d4a1ae479a37536`. Independent GitHub source inspection: lockfile v3, root `dependencies`/`devDependencies`/`engines` match package.json, 366 dependency entries, all resolved URL-based packages hosted on npm registry with integrity hashes; Playwright resolved at 1.65.0. Operator's isolated Windows `npm ci`, typecheck and production build previously PASS.

The transitional GitHub CI step that generated a new lockfile in the runner was **removed**. CI and PVE release now require source-tracked lockfile, validate manifest/registry/integrity through `apps/web/scripts/verify-lock.mjs`, and use frozen `npm ci`. CI includes `npm audit --audit-level=high` to uncover vulnerable dependencies rather than silently passing audit omissions. **No successful GitHub Actions workflow run exists yet (REST API reported 0)**; YS-S0-02 remains PARTIAL pending exact-commit clean CI. No PVE changes.

## 2026-11-09 — Binding direction changed: NO GitHub Actions (ADR-018)

Owner explicitly disallowed GitHub Actions and requested this to be treated as persistent project policy. Deleted the v2 workflow from the feature branch and retired S0 CI runbook. GitHub is **source/PR/commits only**. The old workflow file may remain on `main` until this Draft PR is merged; no workflow execution is requested.

New local operator gates: `scripts/Verify-Source-Local.ps1` builds a **clean Git archive** and runs npm lock validation, clean `npm ci`, `npm audit --audit-level=high`, Angular typecheck/build/Playwright, .NET restore/build; `scripts/Verify-Api-Local.ps1` launches isolated local-only Staging API/SQLite with random bootstrap identity and confirms auth/content lifecycle and persistence across restart. Logs and SHA manifests are kept in separate `logs/yangspace/verify` and `logs/yangspace/api-integration` folders. Real API Python guard now supports OS TEMP on Windows/Linux but requires disposable sandbox marker and matching loopback endpoint.

PVE build-only script now accepts only `main` or explicit approved feature ref whose **remote tip equals exact SHA**, and requires matching source/API PASS manifest paths before building. It still does not activate any release. No production DB/PVE changes.

**Actual state:** New scripts and revised runbook are **source prepared, runtime NOT TESTED**. Last accepted browser test remains 17/17 on earlier homepage code. G0 HOLD until both new local checks PASS and security/release reviews close.
