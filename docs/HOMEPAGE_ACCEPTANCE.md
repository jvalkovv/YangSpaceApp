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
- [ ] `npm run test:e2e`: NOT RUN because Playwright binary was not installed in that local `node_modules`; install project devDependencies and Chromium.
- [ ] Visual QA at actual browser CSS viewport widths 375/390/768/1024/1440 using `apps/web/e2e/responsive-preview.spec.ts`; inspect screenshots before acceptance.
- [ ] Document acceptance of navigation, mobile controls, text wrapping and visual spacing from real browser captures.

Screenshot dimensions differ from CSS viewport width: a 727×2048 image shows desktop-style content, but it could have been resized/cropped. Use Playwright `page.setViewportSize` to validate real breakpoints rather than assuming a responsive CSS defect from that file alone.
