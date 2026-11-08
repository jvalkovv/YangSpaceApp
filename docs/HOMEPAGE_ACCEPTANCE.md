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
