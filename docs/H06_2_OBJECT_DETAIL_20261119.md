# H06.2-B — Public Object Detail Reliability and UX

## Implementation

The public `/obekt/:slug` journey now uses explicit loading, success, not-found and unavailable states. A 404 is distinguished from a transient API/network failure; transient failures expose a retry action and preserve a clear path back to the directory. The retry is scoped to the current slug.

Route parameter changes and retry requests share one `switchMap` pipeline. A newer slug cancels the previous request subscription, and the component clears the previous object before loading, preventing stale profile content from being shown as the current object.

The detail view now:
- Uses the public image when present, with a category-based visual fallback.
- Distinguishes verified from unverified profiles without inventing an open-now or rating claim.
- Presents offers, brochures and jobs with count, clear empty state, validity dates and explicit external links.
- Presents address, neighborhood, opening hours and available phone/website contact actions.
- Announces loading and failure states through semantic live regions.
- Uses Bulgaria's `Europe/Sofia` timezone for public timestamps.
- Provides responsive layouts down to 320px and disables loading animation under reduced-motion preference.

No API/backend contract, dependency, migration, infrastructure or production behavior changed.

## Playwright coverage

The tests are split into focused suites:
- `apps/web/e2e/detail.spec.ts`: populated profile/contact/publications, genuine empty states and stale response on in-app slug change.
- `apps/web/e2e/detail-navigation.spec.ts`: 503/retry, 404 distinction, loading announcement and busy state, network failure/retry, navigation away during a pending request, mobile Escape/focus.
- `apps/web/e2e/product-responsive.spec.ts`: detail and PlatformAdmin preview/editor at 320/360/375/390/430/768/820/1024/1280/1440 px, clipping/overflow checks and screenshots.
- `apps/web/e2e/public-journey.spec.ts`: anonymous category filter → detail → catalogue and public search.
- `apps/web/e2e/responsive-preview.spec.ts`: homepage and populated highlights at the same ten widths.

These are committed test contracts, not yet runtime PASS at the current feature head.

## Verification status

- Source changes committed to H06.2 Draft PR #61.
- Playwright execution: **PENDING operator exact-SHA run**.
- Angular typecheck and production build: **PENDING operator exact-SHA run**.
- Real-browser visual review: **PENDING owner screenshots/manual review**.

The branch remains Draft/HOLD until exact-source tests and browser acceptance pass.