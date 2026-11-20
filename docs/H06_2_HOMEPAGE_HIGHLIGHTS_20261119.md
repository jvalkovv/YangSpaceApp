# H06.2-A — Homepage Highlights Reliability

## Implementation

Offers, brochures and jobs now share an explicit homepage-bootstrap state: `loading`, `success` or `unavailable`. Highlight cards show loading feedback while the homepage request is pending, and show a genuine “Няма активни…” message only after a successful response with an empty list. Failed requests clear all three lists and show a section-specific unavailable message with an accessible retry action, so neither an error nor stale response data is presented as an empty or current result.

The retry reuses `reload()` when the directory is at its default state. With an active search, filter or later page, it refreshes homepage highlights while preserving the directory state and current results. The existing 404 compatibility path, directory search, filters, pagination and directory error recovery remain in place.

## Verification

- Targeted Playwright: `e2e/homepage-highlights.spec.ts` — 6 passed.
- Full Playwright suite — 52 passed.
- `npm run typecheck` — passed.
- `npm run build` — passed (production configuration).
- Retry browser checks recorded no unexpected console errors or uncaught page errors. The mocked HTTP 503 emits the expected browser network error; Playwright's Node process also reports the existing `NO_COLOR`/`FORCE_COLOR` environment warning.
- Screenshots are generated under the ignored `apps/web/test-results/` directory: `homepage-highlights-unavailable.png` and `homepage-highlights-recovered.png`.

No API/backend contract, dependency manifest, deployment or infrastructure changes are included.
