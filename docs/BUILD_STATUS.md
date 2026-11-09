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

## 2026-11-09 — Manual Source PASS and API build-stage stall investigation

Operator executed `Verify-Source-Local.ps1` from reviewed `133cfd65a590883fcfd9b1263dd3d389ce360b46`. All eight stages passed: tracked npm lock, npm ci, npm audit, TypeScript, Angular production build, Playwright browser regressions, .NET restore and Release build. Evidence directory: `logs/yangspace/verify/133cfd65a590-20261109-125436-cdc40482`. **Source validation PASS for that exact SHA.**

Next operator run `Verify-Api-Local.ps1` produced `STAGE_PASS=api-restore` and then `STAGE=api-build` without reported completion or failure. Because output is redirected to `logs/yangspace/api-integration/<run-id>/api-build.stdout.log` and `api-build.stderr.log`, lack of console progress alone cannot prove a compilation error. The script used `Start-Process -Wait`, which on Windows can wait for descendant process trees (e.g., persistent MSBuild servers) even after the main process exits. The actual cause remains pending inspection of the operator's build log.

**Remediation:** both manual QA wrappers now use direct process `.WaitForExit(600000)`, bounded parent PID wait, fail-closed stage timeout, per-command log tail on failure, and dispose process handles. The timeout cleanup targets only the process launched by the test runner. Does not modify app/DB/PVE, does not kill existing Angular or the separately running API. **New source has not been runtime tested; S0-04 HOLD until success.**

## 2026-11-09 — S0-04 false FAIL after successful restore; process runner replacement

On source `a9132a35099010e31f4df273bc6b045867fd081f`, operator ran `Verify-Api-Local.ps1` and observed: `STAGE=api-restore`, successful .NET restore output (`Restored ... YangSpace.Api.csproj`), followed by `STAGE_FAILED=api-restore EXIT=` (empty exit code). This proves that the wrapper classified an absent exit code as failure; **it does not prove a restore failure**. The prior run's `api-build.stdout.log` likewise confirmed `Build succeeded, 0 Warning(s), 0 Error(s)` in 4.26 s. The old `Start-Process -Wait` may have waited for descendants; the second revision used `Start-Process -PassThru` and observed a null exit code after `WaitForExit`.

**Root-cause correction in source:** introduced shared `scripts/Invoke-YangSpaceStage.ps1` using an explicit `System.Diagnostics.Process` instance and a generated local `cmd.exe` launcher that writes distinct stdout/stderr files. It waits on the exact launcher PID with a bounded 600 s timeout and reads the integer Process.ExitCode. Both Windows wrappers now call this shared helper; no GitHub Actions. Added `scripts/Test-Process-Runner.ps1` as a direct self-test of exit 0, exit 7 (must fail) and npm.cmd invocation. Logs and command runner files are generated under the ignored local `logs/yangspace` workspace.

**Evidence:** source-only fix; new helper/self-test have not been executed on operator Windows. `YS-S0-03` source QA remains PASS for older `133cfd6` only; `YS-S0-04` remains HOLD. Before re-running full API integration, run `Test-Process-Runner.ps1`; if it does not print `PROCESS_RUNNER_SELFTEST=PASS`, stop and inspect logs. Do not stop port 5090, delete the real DB or run `npm ci` in the live web tree.

## 2026-11-09 — S0-04 Python Store alias blocker: native Windows API integration prepared

Operator pulled `5b07716`, ran `Test-Process-Runner.ps1`: runner exit 0, deliberate exit 7, and npm.cmd all **PASS**. `Verify-Api-Local.ps1` then reported `STAGE_PASS=api-restore`, `STAGE_PASS=api-build`, but `STAGE=api-prepare` failed with `EXIT=9009` and the Windows Microsoft Store prompt: `Python was not found`. **Root cause: Windows `python.exe` App Execution Alias instead of a real Python runtime.** The API contract has not been executed; this is not an API failure.

**Code change:** `Verify-Api-Local.ps1` now launches the native Windows PowerShell 5.1 test `scripts/Invoke-Api-Integration.ps1 -Phase prepare|verify` via the validated shared stage runner. No Python dependency for Windows. Native test covers guest/nonadmin vs admin access, duplicate/invalid location, create/update/publish, active/expired/future offers, brochure/job, hide, public exclusion and persistence after process restart. Strict fail-closed safeguards require a random OS-TEMP sandbox, marker file, matching Staging SQLite DB and loopback-only 127.0.0.1 port. Separate `Test-Api-Native-Guard.ps1` validates parsing and verifies refusal outside the isolated environment. The earlier Python test remains as a secondary tool for environments with Python, but is **not invoked** by the Windows QA script.

**Evidence:** source is staged, **not yet run** on Windows; no production or PVE mutation, no GitHub Actions. `YS-S0-04=HOLD` until guard and full integration PASS on a pinned SHA. Need a matching Source QA PASS on that same candidate SHA before G0.

## 2026-11-09 — S0-02/03/04 jointly PASS on f3f4e1c

Operator's `Verify-Source-Local.ps1 -CommitSha f3f4e1c9e29a640bab2637fac9ff98098747e41a` completed eight stages (web lock/install/audit/typecheck/build/Playwright, api restore/build) and returned `SOURCE_VALIDATION=PASS`. Evidence: `logs/yangspace/verify/f3f4e1c9e29a-20261109-140701-f8aa61a1`. The earlier `Verify-Api-Local.ps1` returned `REAL_API_INTEGRATION=PASS`, with native PowerShell syntax/guard PASS, and prepare/restart-verify stages PASS; evidence: `logs/yangspace/api-integration/f3f4e1c9e29a-20261109-134901-71bf0807`. Both tests used the same SHA. Source and API QA **DONE for that historical candidate**, not a public release security approval.

**New S0-05 hardening source (runtime QA pending):** preview self-registration opt-in only (`Preview:AllowRegistration=false` default), and bootstrap refuses to promote an existing ordinary Identity user to `PlatformAdmin`. The disposable API test enables registration only to create a non-admin negative-auth account on cycle 1, then turns it off after restart and expects HTTP 404; admin login and DB persistence still tested on cycle 2. Security review is partially complete; remaining risks tracked in `SECURITY_REVIEW_V2_20261109.md`. No actions/deploy/PVE/DB changes.
