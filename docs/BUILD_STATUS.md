# YangSpace 2.0 — Verification Status

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
