# YangSpace 2.0 — Homepage acceptance (Preview)

**Scope:** `apps/web/src/app/home.component.ts`, `apps/web/src/styles.css`, public .NET endpoints. This is the visual+functional acceptance checklist. A committed source file is not a substitute for successful runtime evidence.

## Implemented in source

- Deep-green, cream and sage YangSpace design identity with custom CSS city-map illustration (no third-party images/fonts needed).
- Hero, primary CTA, navigable anchor links, responsive header/footer and preview-only disclosure.
- Clickable category chips connected to the same filters as search form.
- Neighborhood/category search with 280 ms typing debounce, immediate select/submit, bounded timeout and cancellation of superseded requests.
- 12-per-page directory with dynamic count, pagination, reset, empty, loading and error/retry states.
- Data-driven offers, brochures and jobs (only published and currently valid).
- Three-step discovery guide and business CTA clearly labeled as demo CMS.
- Accessibility baseline: visible keyboard focus, semantic sections, status/error announcements, mobile nav control, reduced-motion preference.
- Backend: initial `GET /api/v1/homepage` combining facets, catalog and highlights in one HTTP round trip; public listing/details unchanged.

## QA gates (NOT yet executed)

- [ ] Backend `dotnet restore`, `dotnet build -c Release -warnaserror` and `scripts/api-smoke.sh` pass.
- [ ] Angular `npm run typecheck` and `npm run build` pass on supported Node.
- [ ] First load: full cards replace skeletons; no infinite spinner. Verify no duplicate initial `/meta` + `/locations` calls.
- [ ] Filtering: category chip, category dropdown, neighborhood dropdown, text search and reset return consistent data.
- [ ] Rapid filter changes do not show stale results; pagination works.
- [ ] Unavailable API shows actionable error within 10s, retry recovers without a page reload.
- [ ] On first-run seeded preview, six fictional locations, offer/brochure/job highlights load.
- [ ] All navigation and listing links resolve, including `/obekt/{slug}`.
- [ ] Browser viewport screenshots at 375, 390, 768, 1024 and 1440 pixels. Check overflow, tap-targets, text contrast and no CLS on load.
- [ ] Keyboard only: logical focus order, mobile menu, search form, category chips, errors.
- [ ] Browser DevTools: no JavaScript errors, no 404 assets, no CORS failures; measure cold/warm network requests.
- [ ] Lighthouse/Core Web Vitals sampled on target hardware; record results, not estimates.
- [ ] Public/live security gate separate from preview QA. Do not deploy to public PVE URL before TLS/access controls/backups.

## Measurement guidance

The API local smoke benchmark is read-only:

```powershell
powershell -NoProfile -File scripts/Test-Homepage.ps1 -Iterations 7
```

It reports HTTP times, backend payload counts and a median over warm calls. These are **not** browser LCP or production performance measurements. Use Chrome Network/Performance and Lighthouse for real UI timings.

## Known limits

- This is a client-side Angular preview; SSR/indexable SEO is postponed.
- Offer/job data in preview are fictional. There are no real merchant uploads, business verification or pricing plans.
- Actual local performance cannot be determined from the attached screenshot alone.

## 2026-11-08 — Reported runtime regression: Angular 22 change detection

User measured **cold 28 ms / warm median 4.4 ms** on `GET /api/v1/homepage`, contract PASS (6 locations/6 categories/3 offers/1 brochure/1 job). Slow/stuck rendering is **not** supported as a DB latency issue by these metrics.

User browser evidence: initial cards require an unrelated click/blur to appear; detail `/obekt/bike-demo` remains on Loading; selected `Красота` can display stale `Заведения` card during refresh.

Root cause verified against Angular 22 defaults: zoneless + OnPush requires a change-notification after asynchronous subscriptions update fields. Fix: `markForCheck()` in Home/Detail/Admin; detail uses `switchMap` to cancel outdated route requests; filter changes clear stale items before pending query; ZoneJS polyfill removed as unnecessary.

Acceptance **requires** browser evidence after the fix:
- [ ] Six cards load without clicking any field.
- [ ] Detail loads on fresh URL without click/blur.
- [ ] Category selection does not display stale-category cards while the request is pending.
- [ ] Rapid category switching and text searches show the latest response, never an older one.
- [ ] Browser errors and preview admin HTTP results update without unrelated interaction.
- [ ] Automated Playwright regression checks pass in local Chromium.

## 2026-11-08 — Desktop visual/filter follow-up

User-supplied screenshots exposed:
1. Selected `Магазини` in chip and select while a `Красота` card was still visible and the interface showed "Обновяваме резултатите…". This requires both exact control-value forwarding and a rendering invariant: *never render result cards during a pending filter request*.
2. The `Y.` brand badge rendered its period below the dark badge because the `Y` text and nested `span` were separate children of CSS Grid. Render the badge as a single text item.
3. Hero line wraps were too tall for the desktop column. Reduce maximum heading size, increase copy width, and tighten tracking.
4. Single-result directory should center the card instead of leaving an oversized empty right column.

Required regression checks, not automatically accepted:
- [ ] `Магазини` chip/select sends `category=Магазини` on the first interaction; no `Красота` card in a pending or finished state.
- [ ] Selecting a category and changing it rapidly never leaks results from another category.
- [ ] Search text invalidates currently displayed cards immediately, including during 280ms debounce.
- [ ] Header brand period stays inside its 37px dark square at 375/390/768/1440px.
- [ ] Desktop hero at 1440px wraps cleanly; no horizontal overflow, clipping or floating-card collisions.
- [ ] Single filtered card is centered on desktop and full width on mobile.

## 2026-11-08 — API connectivity failure captured

A browser screenshot of `127.0.0.1:4200/#catalog` shows a service request failure and a misleading "0 обекта" count. Earlier user-run API benchmark passed (6 locations, 28 ms cold and 4.4 ms warm), but that benchmark **does not establish that the API was still running when this screenshot was captured**. The Angular proxy still targets `127.0.0.1:5090`, matching the .NET HTTP launch profile.

Remediation:
- Status-aware error text (HTTP 0 network, 502/503/504 proxy/availability, timeout, 404 version drift, 500 backend).
- No success-style count when the request failed.
- Retry on an unfiltered directory reloads `/api/v1/homepage` to restore facets and highlights (instead of refreshing only the listings).
- New read-only `scripts/Test-Local-Stack.ps1` diagnoses direct API readiness, homepage, Angular server and proxy separately.
- Playwright regression scenarios for initial 503 → recovery and filtered 502 → recovery.

Acceptance:
- [ ] With .NET running: direct `http://127.0.0.1:5090/health/ready` and via-proxy `http://127.0.0.1:4200/api/v1/homepage` return HTTP 200.
- [ ] With .NET deliberately stopped: Angular shows a recoverable service error, **not** "0 обекта".
- [ ] After restarting .NET and clicking Retry: six demo locations and category facets appear without page reload.
- [ ] `scripts/Test-Local-Stack.ps1` reports `LOCAL_STACK=PASS`.

## 2026-11-09 — Integration/build confirmation and Playwright activation

- [x] Windows local backend / Angular dev server / proxy health: all 5 checks PASS, including HTTP 200 homepage payload through the proxy.
- [x] `npm run typecheck`: PASS (user console output).
- [x] `npm run build`: PASS; 407.56 kB initial raw bundles / 100.61 kB estimated transfer. This is not a runtime page-load benchmark.
- [x] `npm run test:e2e`: **14/14 PASS** on Windows after `npm install` and `npx playwright install chromium`, 2026-11-09 user console evidence. Includes 5 responsive width tests, change detection, filtering, error recovery and preview admin. Browser screenshot artifacts are local and have **not** been manually inspected here.
- [ ] Visual QA at actual browser CSS viewport widths 375/390/768/1024/1440 using `apps/web/e2e/responsive-preview.spec.ts`; inspect screenshots before acceptance.
- [ ] Document acceptance of navigation, mobile controls, text wrapping and visual spacing from real browser captures.

Screenshot dimensions differ from CSS viewport width: a 727×2048 image shows desktop-style content, but it could have been resized/cropped. Use Playwright `page.setViewportSize` to validate real breakpoints rather than assuming a responsive CSS defect from that file alone.

## 2026-11-09 — Regression acceptance evidence (user console)

- **Executed:** `npm run test:e2e` from `D:\GitHub\YangSpaceApp\apps\web` on Windows.
- **Result:** 14 passed, 0 failed, 16.0 seconds; Playwright 1.65.0, Chromium Headless Shell build 1254 installed locally.
- **Responsive:** 375, 390, 768, 1024 and 1440 CSS px — all PASS (no document/body overflow according to automated assertions).
- **Functional:** initial cards without focus, exact category filtering (including first chip click vs dropdown), detail route, admin async display, HTTP failure states and retry — PASS under mocked API fixtures.
- **Separate actual local stack evidence:** API live/ready/homepage and Angular server/proxy all returned HTTP 200.
- **Remaining checks:** screenshots themselves need human review for visual quality; mocked Playwright flows do not replace admin end-to-end validation against the real API; CI branch status not yet green; lockfile still absent.

**Decision:** automated homepage browser regression gate **PASS**; complete visual and production acceptance **PENDING**.

## 2026-11-09 — Manual visual review of five uploaded viewport screenshots

Reviewed the actual Playwright full-page PNGs supplied in `YangSpace-Viewport-QA.zip`:
- `yangspace-home-375.png` (375×7344): mobile navigation, hero, category wraps, vertical search form, single-column listings/offer cards and footer all fit the viewport; no visible horizontal clipping.
- `yangspace-home-390.png` (390×7351): same expected mobile behavior, with readable CTA and content hierarchy.
- `yangspace-home-768.png` (768×5126): **VISUAL DEFECT** — hero map and right-side decorative content clipped by `.hero { overflow:hidden }`; the old `translateX(40px)` and 300px map overflowed the allocated 260px visual column. The third full-width Jobs card also had disproportionate empty vertical height.
- `yangspace-home-1024.png` (1024×4326): hero, navigation, directory and content sections are visible without obvious clipping.
- `yangspace-home-1440.png` (1440×4250): desktop composition, card grid, section spacing and footer appear consistent.

**Changes in follow-up:** remove the tablet horizontal translation; constrain map to a 225px grid cell, shrink its interior callouts, reduce Jobs minimum height at tablet widths. Add 768px bounding-box regression (detects children cropped inside the overflow-hidden hero) and 390/768/1440 populated-content screenshot tests. Existing five screenshot tests use **empty** mocked offers, brochures and jobs, so populated-section visual acceptance was not supported by that ZIP.

**Scope of evidence:** source screenshot inspection, not an automated performance audit. New CSS and tests still require a new Playwright run and screenshots for final visual acceptance.

## 2026-11-09 — Post-fix Playwright execution (Windows operator evidence)

**Commit under test:** `20aa30c7da581316ba31900b6953025a3d1f3b00` (`feat/yangspace-v2-foundation-20261108`, confirmed by successful fast-forward pull).

- **Command:** `npm run test:e2e`, Windows PowerShell, `apps/web`.
- **Result:** **17 passed / 0 failed in 18.2 seconds**.
- **Five viewport layout tests PASS:** 375, 390, 768, 1024, 1440 CSS px.
- **768px-specific clipping assertions PASS**: tablet hero map and feature callout stay within their bounding boxes.
- **Three populated-content cases PASS:** offers, brochures, jobs at 390, 768 and 1440 px.
- **Nine functional browser regressions PASS:** first-load rendering, filter race handling, detail route, admin UI rendering, failed HTTP cases and retry.
- Tests use mocked API responses: this is **browser regression evidence**, not real content/DB acceptance or production Lighthouse data.

**Open gates:** inspect the newly generated PNG screenshots from this run (particularly `yangspace-home-768.png` and `yangspace-populated-{390,768,1440}.png`), verify real unmocked owner/admin lifecycle, record GitHub CI evidence, commit reviewed npm lockfile, and finish PVE release safeguards. Do not claim 100% visual/production acceptance yet.

## 2026-11-09 — Final visual acceptance from post-fix Playwright screenshots

**Verdict: HOMEPAGE PREVIEW VISUAL QA — PASS** for the four reviewed full-page PNGs attached by the operator in `YangSpace-Final-Visual-QA.zip`. This acceptance applies to **source commit `20aa30c7da581316ba31900b6953025a3d1f3b00`**, tested by Playwright (17/17 PASS), plus later documentation-only changes. It is **not** a claim of production readiness or acceptance of other application modules.

| Artifact | Actual PNG dimensions | Manual findings | Verdict |
|---|---:|---|---|
| `yangspace-home-768.png` | 768×5031 | Previously clipped map and callout now fully within hero. Header/nav, category wraps and catalog structure remain legible. | PASS |
| `yangspace-populated-390.png` | 390×7451 | Mobile header and menu affordance, single-column search/listings, three populated highlight panels, steps, business CTA and footer fit without visible horizontal clipping. | PASS |
| `yangspace-populated-768.png` | 768×5082 | Tablet hero map no longer clipped; categories, two-column directory and populated offers/brochures/jobs panels lay out consistently. | PASS |
| `yangspace-populated-1440.png` | 1440×4302 | Desktop header/hero, 3-column catalog, populated content panels, step section, business CTA and footer maintain alignment and whitespace. | PASS |

**Observations:** all four screenshots use Playwright mocked demo data and show intended preview labeling. No new blocking overflow, clipping or content collision was found in those captures. No further stylesheet changes were needed after the previous tablet correction.

**Supporting automated evidence:** operator's Windows Playwright run on `20aa30c` — 17 passed in 18.2 s, including 5 viewport checks, 3 populated-content checks and 9 functional regressions. Earlier local .NET/Angular/proxy integration checks, Angular typecheck and production frontend build also PASS.

**Separately open release gates (NOT visual defects):**
- Real unmocked admin/business creation/edit/publish/hide and end-to-end persistence/security QA.
- Full production data/ownership, SSR/SEO, migrations, license, dependency lockfile and immutable builds.
- GitHub Actions CI run for exact release commit (connector returned no runs).
- PVE provisioning, TLS/access protection, backups/restore and staging deployment.

**Scope decision:** approve homepage responsive/functional **preview** baseline. Keep PR #1 Draft until repository-wide integration/security/CI release gates pass.
