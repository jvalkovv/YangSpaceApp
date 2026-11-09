# YangSpace 2.0

> **Accepted owner direction — NO GitHub Actions.** GitHub is for source/PR/commits only. Quality checks are **operator-run on a clean exact-SHA source snapshot**, with separate logs and real API integration in a disposable database. This supersedes old README sections mentioning CI. [ADR-018](docs/ADR-018-NO-GITHUB-ACTIONS.md) · [S0 Manual QA Runbook](docs/S0_MANUAL_QA_RUNBOOK.md).

## Execution plan — canonical documents (2026-11-09)

- **[PoC & Technical Master Plan](docs/POC_MASTER_PLAN.md)** — scope, real baseline, user flows, domain, phases S0–S7, security/release gates and business validation.
- **[Execution Tracker](docs/POC_TRACKER.md)** — task IDs, priority, prerequisites, DONE/PARTIAL/TODO/BLOCKED, first-action queue and evidence format.
- **[Acceptance Matrix](docs/POC_ACCEPTANCE_MATRIX.md)** — objective API, browser, authorization, data, SEO, PVE and pilot test scenarios.
- **[PVE Release Runbook](docs/POC_PVE_RELEASE_RUNBOOK.md)** — private preview topology, reviewed SHA, inventory, backup, verify and rollback.

**Current gate:** Homepage preview visual QA PASS, operator Playwright 17/17 PASS; API/Angular/proxy local health PASS. **Overall product PoC is not finished.** Active work starts at tracker `YS-S0-02`: dependency lockfile / deterministic manual source validation, followed by real .NET integration and CMS. **No CT deployed, no production data or payments.**


**Градът около теб.** Първо квартал във Варна, после цяла Варна и България.

> **STATUS: FUNCTIONAL PREVIEW FOUNDATION — NOT PRODUCTION READY.** Новият .NET + Angular код е в `apps/`. Съдържа публичен каталог, страници на обекти и изолирана preview администрация. Реалните обекти, verified ownership, production persistence, SEO/SSR, jobs lifecycle и billing не са завършени. Инсталация на публичен адрес е забранена до security / QA gates.

## Технологии и hosting

- **Backend:** ASP.NET Core **.NET 10 LTS**, EF Core, SQLite само за локален/изолиран demo preview.
- **Frontend:** **Angular 22**, TypeScript 6, mobile-first, без UI framework.
- **Host:** съществуващ **Proxmox VE (PVE)**, един отделен Linux **LXC** за preview; Nginx + systemd.
- **GitHub:** source и PR; GitHub Actions не се използват. LXC е Git clone/fetch client; **няма автоматичен deploy при push**.
- **Изключени:** Azure, IIS, Jenkins, Kubernetes, Docker като deployment изискване.
- **Production database:** остава архитектурно решение (PostgreSQL или SQL Server). Preview SQLite **не** е миграционна основа за live данни.

## Проектна структура

```text
apps/
  api/           .NET 10 REST API, EF Core, preview identity
  web/           Angular 22 public directory + preview CMS
infra/pve/        Nginx/systemd templates и LXC deployment runbook
scripts/          Local API smoke и pinned GitHub build
docs/             Product, security, architecture, domain, roadmap
YangSpace.slnx    .NET solution
```

Наследената версия 2024 (стар booking app, .NET 8/Angular 18, IIS/Azure) е запазена чрез Git history и `archive/yangspace-v1-2024` branch; не участва в новата build/deployment конфигурация.

## Стартиране локално за Live Preview

**Изисквания:** .NET SDK 10, Node.js **22.22.3+ (22.x) / 24.15.0+ (24.x) / 26.0.0+**, npm. Node 24.14.1 НЕ се поддържа от Angular 22. При Windows: [официален Node 24.15.0 installer](https://nodejs.org/en/download/archive/v24.15.0), нов PowerShell терминал, `node -v` и `where.exe node`.

### Terminal A — .NET API

**SQLite security fix:** в `apps/api/YangSpace.Api.csproj` е фиксирана актуална native SQLite библиотека `SQLitePCLRaw.lib.e_sqlite3 3.53.3`, защото EF Core 10.0.0 дърпа уязвимата 2.1.11. Не изключвайте NU1903 или warnings-as-errors. След `dotnet restore` проверете транзитивната версия с `dotnet list apps/api/YangSpace.Api.csproj package --include-transitive`.


```bash
dotnet restore apps/api/YangSpace.Api.csproj
dotnet run --project apps/api/YangSpace.Api.csproj --launch-profile http
```

- API: `http://127.0.0.1:5090`
- Readiness: `http://127.0.0.1:5090/health/ready`
- Homepage API (single request): `http://127.0.0.1:5090/api/v1/homepage`
- Public list: `http://127.0.0.1:5090/api/v1/locations`
- Development-only preview data се създават в `yangspace-preview.db` в working directory.

### Terminal B — Angular

```bash
cd apps/web
npm ci --no-audit --no-fund
npm run typecheck
npm start
```

`npm start` listens on **127.0.0.1 only**. For an intentionally isolated LAN test, use `npm run start:lan` after ensuring Windows Firewall/VPN access restrictions. The default localhost server must not be published to the Internet.

Ако `node -v` все още показва `v24.14.1`, затвори всички PowerShell терминали и провери коя инсталация е първа в PATH (`where.exe node`).

Отвори **http://localhost:4200**. Angular proxy пренасочва `/api` и `/health` към .NET API. Страницата има 6 **измислени demo обекта**, маркирани като примерни. Няма фиктивни телефонни номера или реални адреси.

### Preview CMS

- За вход в `/admin` се ползва **само локален bootstrap admin**, създаден чрез `Bootstrap__AdminEmail` и `Bootstrap__AdminPassword` в environment / user secrets. Няма default парола.
- Задай стойностите **преди първия старт на API**; не ги записвай в Git. Паролата трябва да отговаря на Identity policy.
- Preview API включва `/api/auth/login` и admin-only location/offer/brochure/job creation и hide. Access token е само в паметта на Angular и се губи при refresh.
- **Няма публичен owner signup, upload или billing.** Identity registration routes са достъпни само в изолирана preview среда; новорегистрирани потребители нямат admin role.
- Публикуването от preview CMS е само за демо данни; не внасяйте реална лична информация.

> От commit `94f710e` имаме source-tracked `package-lock.json`; за чиста инсталация използвай `npm ci`. S0 source QA остава HOLD до локални clean-SHA PASS logs за същия commit.

## API

| Route | Access |
|---|---|
| `GET /health/live`, `GET /health/ready` | Public |
| `GET /api/v1/homepage` | Public — initial directory, facets, active offers/brochures/jobs in one response |
| `GET /api/v1/meta` | Public |
| `GET /api/v1/locations` | Public |
| `GET /api/v1/locations/{slug}` | Public |
| `POST /api/auth/login` | Preview only |
| `GET/POST/PUT /api/v1/admin/locations` | Preview + PlatformAdmin |
| `POST /api/v1/admin/locations/{id}/hide` | Preview + PlatformAdmin |
| `POST /api/v1/admin/locations/{id}/offers|brochures|jobs` | Preview + PlatformAdmin |

## Homepage QA

The homepage uses a single `/api/v1/homepage` request on initial load, bounded API request timeouts, cancellation of superseded searches, stable skeleton cards, desktop/mobile layouts, local category filters, and real demo-data-driven offer/brochure/job sections. It does **not** require third-party fonts or remote hero images.

On Windows, start the API and run:

```powershell
powershell -NoProfile -File scripts/Test-Homepage.ps1
```

Then inspect the browser at **375 / 390 / 768 / 1024 / 1440 px** per [Homepage Acceptance](docs/HOMEPAGE_ACCEPTANCE.md). Do not mark production visual acceptance without screenshot/runtime evidence.

## PVE / LXC plan

1. Подготвяме отделен CT и инвентаризираме DNS, TLS, storage, backups.
2. Правим Git clone в контейнера. `origin` сочи GitHub, но няма webhook auto-deploy.
3. От конкретен проверен commit SHA пускаме `scripts/build-preview-from-github.sh`; той **само build-ва** версия под `/srv/yangspace/releases`.
4. След backup и QA операторът активира версията чрез симлинк и systemd reload (runbook).
5. Изолирана preview среда, защитена от публично индексиране; production release само след P0/MVP acceptance.

[**PVE инструкции**](infra/pve/README.md) · [**Security gates**](docs/SECURITY_BASELINE.md)

## Product scope

Публичен каталог по квартали и категории, профили, активни оферти, брошури, работни позиции; търговците ще управляват собствените си обекти след claim/ownership workflow в следващ milestone. Точните тарифи са **TBD**, billing не е част от preview.

Документи: [Product](docs/PRODUCT.md) · [Architecture](docs/ARCHITECTURE.md) · [Domain](docs/DOMAIN_MODEL.md) · [Roadmap](docs/MVP_ROADMAP.md) · [Validation](docs/VALIDATION.md) · [Decisions](docs/DECISIONS.md)

## Release policy

Работим чрез GitHub PR, локално изпълнени проверки и документирани commit IDs. Без смесване на preview и live data. GitHub сам по себе си не публикува върху PVE. Не твърдим production readiness без build/test/runtime evidence.

**Лиценз:** преди публично разпространение трябва да бъде добавен и потвърден коректен LICENSE файл.

## Angular 22: zoneless rendering

Angular 22 is zoneless/OnPush by default. All async HTTP subscriptions updating template-visible component fields must notify change detection. Home, business detail and preview admin explicitly call `ChangeDetectorRef.markForCheck()` following network results or errors. The unused ZoneJS polyfill and dependency have been removed. The default local frontend still runs on `127.0.0.1:4200`.

After pulling this fix on a fresh checkout, run `npm ci` (with the dev server stopped), then `npm run typecheck` and `npm start`. Chrome should display the 6 seeded entries **without any click or focus/blur**, and `/obekt/bike-demo` must resolve without an additional UI interaction.

Optional local browser regression tests: `npm run test:e2e` after installing the browser with `npx playwright install chromium`. These tests mock backend HTTP responses and specifically cover zoneless rendering and filter/result consistency.

## Local troubleshooting: API unavailable / "0 обекта"

If Angular renders an error while a previously working demo catalog is empty, **do not reset the database or reinstall dependencies**. The browser app (port 4200) and the .NET API (port 5090) are independent processes; running Angular does not automatically start the API.

Run from repository root:

```powershell
powershell -NoProfile -File scripts/Test-Local-Stack.ps1
```

The diagnostic compares direct API availability and the frontend proxy. `ROOT_CAUSE=API_NOT_READY_OR_NOT_LISTENING` means start the .NET process in its own terminal:

```powershell
dotnet run --project apps/api/YangSpace.Api.csproj --launch-profile http
```

If direct API checks pass but `ANGULAR_PROXY_HOMEPAGE` fails, restart Angular (`npm start` in `apps/web`), check its proxy configuration and verify `http://127.0.0.1:4200/api/v1/homepage`. A loading error is **not** a legitimate zero-location search result.

The homepage shows the backend/proxy error class and the Retry action reloads homepage facets, listings and highlights. No automatic database modifications or API restarts occur.

## Browser QA — Playwright on Windows

If `npm run test:e2e` prints `'playwright' is not recognized`, the project `devDependencies` were not reinstalled after `@playwright/test` was added to `package.json`. This is a **missing local test runner**, not a failed browser test. From `apps/web`:

```powershell
npm ci --no-audit --no-fund
npx playwright --version
npx playwright install chromium
npm run test:e2e
```

The Playwright suite launches its **own temporary Angular dev server at `127.0.0.1:4300`** and uses mocked API data. The .NET API on `5090` is not needed for these isolated browser regression cases. Keep `4200` for manual integration testing.

Automated viewport checks run at **375, 390, 768, 1024 and 1440 CSS pixels**, verify no horizontal document overflow, and produce screenshots under `apps/web/test-results/`. These screenshots provide reproducible visual evidence, not automatic approval of aesthetics or Core Web Vitals. Browser tests run manually from the reviewed commit by `scripts/Verify-Source-Local.ps1` using a disposable clean archive, with retained local evidence.

## S0 execution: reproducible npm and real API integration (2026-11-09)

The production release build **requires** a source-tracked `apps/web/package-lock.json` and uses `npm ci`. Lockfile is now committed (first introduced at `94f710e`). Private PVE release remains blocked by the remaining manual exact-SHA QA/security and operator release gates, not by missing npm dependency metadata.

After pulling the S0 branch on a Windows machine with supported Node, run from the repository root:

```powershell
.\scripts\Prepare-Web-Lock.ps1
git status --short
```

The script runs `npm install --package-lock-only --ignore-scripts`, validates a clean `npm ci`, typecheck and build. The lockfile was reviewed and committed at `94f710e`. Operator QA never generates replacement dependency locks: it verifies the tracked file and executes `npm ci`. Do not regenerate or silently update the lockfile during a normal build; changes require a separate reviewed dependency-update commit.

Manual `scripts/Verify-Api-Local.ps1` runs `scripts/api-integration.py` against a **disposable temporary database and loopback-only test API**, including writes and process restart. It refuses to operate on normal/local/merchant data. See [PoC Tracker](docs/POC_TRACKER.md). No real admin credentials are stored in Git.

### Windows LMDB `EPERM` on `npm ci`

`ng serve` can keep `@lmdb/lmdb-win32-x64/node.napi.node` open. Direct `npm ci` in `apps/web` removes `node_modules` and can fail with `EPERM unlink` if Angular is still running. This is a Windows file-lock problem, not evidence of a corrupt lockfile.

The updated `scripts/Prepare-Web-Lock.ps1` runs `npm ci`/typecheck/build in an isolated temporary folder. It leaves the live `node_modules` and Angular preview untouched. **Do not forcibly delete locked DLLs, kill unrelated Node processes or elevate PowerShell.**

The generated lockfile has been committed and reviewed. The source/reproducibility sub-gate is satisfied; the final S0 gate remains PARTIAL until exact-SHA local QA, real API integration and dependency audit have PASS evidence.

### Immutable npm dependency manual QA gate

Since commit `94f710e`, `apps/web/package-lock.json` is tracked. Manual QA and the release builder do not invent a lockfile: they run `node scripts/verify-lock.mjs` from `apps/web`, followed by `npm ci`; `Verify-Source-Local.ps1` additionally runs `npm audit --audit-level=high`. The lock validator rejects manifest drift, non-registry tarballs and missing package integrity. **Do not use `npm audit fix` automatically**; review and commit dependency upgrades separately. Missing manual QA evidence remains a release blocker; GitHub Actions are never used.

## S0 — Run the manual QA gate (no hosted pipeline)

Run only after fetching the reviewed branch; both scripts archive the **committed HEAD** into isolated OS TEMP, ignoring local uncommitted changes and active dev-server `node_modules`:

```powershell
cd D:\GitHub\YangSpaceApp
git pull --ff-only origin feat/yangspace-v2-foundation-20261108
.\scripts\Verify-Source-Local.ps1
.\scripts\Verify-Api-Local.ps1
```

Source QA validates committed lockfile, `npm ci`, dependency audit, Angular build/Playwright and .NET build. API QA creates its own random local-only port, temporary SQLite and bootstrap identity, verifies auth, publication, expiration, hide and process restart. Logs are separated in `logs/yangspace/verify/` and `logs/yangspace/api-integration/` and are not tracked. **No GitHub Actions, no live DB writes, no PVE deployment.** Both PASS manifests must reference the same full SHA before release review.

Read [S0 Manual QA](docs/S0_MANUAL_QA_RUNBOOK.md) and [ADR-018](docs/ADR-018-NO-GITHUB-ACTIONS.md).

### Windows QA process runner regression (2026-11-09)

If the manual API wrapper prints `STAGE_FAILED=api-restore EXIT=` after `Restored ...`, this is a missing process exit-code reporting bug, not evidence that restore failed. After pulling the latest reviewed branch, run `scripts/Test-Process-Runner.ps1` to test success, non-zero exit and `npm.cmd` handling. A shared `System.Diagnostics.Process` runner replaces `Start-Process -PassThru` in both clean-source and API QA wrappers. Only proceed when `PROCESS_RUNNER_SELFTEST=PASS`; then run the actual isolated API verification.

### Native API integration — no Python requirement

The Windows manual API integration flow uses `scripts/Invoke-Api-Integration.ps1` through `Verify-Api-Local.ps1`. Windows Microsoft Store alias `python.exe` (exit 9009) is not a valid Python interpreter and no longer blocks the integration flow. First execute `scripts/Test-Api-Native-Guard.ps1` to verify syntax and refusal of unsafe DB targets, then run the isolated API test. Linux/manual Python contract is separate and optional. **No GitHub Actions.**

### Preview authentication security gate (S0-05)

Self-registration is **disabled by default** through `Preview:AllowRegistration=false`. An isolated QA process temporarily enables it to create a non-admin test identity, then verifies registration returns 404 after the process restarts with the flag disabled. Existing user accounts lacking the `PlatformAdmin` role are not elevated through bootstrap. This new change requires fresh source and API validation on its own exact commit before accepting G0. See [Security review](docs/SECURITY_REVIEW_V2_20261109.md).
