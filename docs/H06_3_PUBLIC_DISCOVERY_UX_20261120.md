# H06.3 — Public Discovery UX

## Source and dependency

- Parent: H06 application UX/UI and browser acceptance #57.
- H06.2 predecessor: #60 / PR #61.
- H06.2 exact tested base: `080602b4a3eaa209f4045d4232495348512be06d`.
- H06.3 branch: `feat/yangspace-f2b-h06-3-public-discovery-ux-20261120`.
- H06.3 is stacked on H06.2. Its PR targets the H06.2 branch and must not be merged independently into the F2b base before H06.2 owner sign-off/merge.

## Implemented frontend changes

1. **Mobile-first search placement**
   - Primary directory search now appears immediately after the pilot/demo notice and before category chips and results.
   - The Homepage hero CTA points to the search surface.
   - Catalog heading no longer repeats search instructions.
   - Demo notice copy is Bulgarian and shorter.
2. **Compact mobile hero**
   - Reduced mobile hero vertical padding and headline/lead sizing.
   - Desktop/tablet decorative neighborhood map and existing brand palette remain unchanged.
3. **Real business image rendering**
   - Directory cards render the existing API `LocationCard.imageUrl` when present.
   - Images use lazy loading, async decoding, business-specific alt text and `object-fit: cover`.
   - Missing or failed images fall back to the existing category illustration.
   - No stock assets, generated media or third-party dependencies were added.
4. **Verified profile cue**
   - `isVerified=true` displays a restrained “Проверен” indicator with an accessible label.
   - False values do not display a trust badge.
5. **Homepage publication consistency guard**
   - If a featured directory card advertises active offers/jobs while the corresponding homepage highlight list is empty, the UI shows an explicit inconsistent-data state with retry.
   - It does not claim “Няма активни…” for contradictory payloads.
   - API failure and genuine successful empty states remain distinct.
6. **Fixture integrity**
   - Existing browser fixtures were corrected so their directory publication counters and homepage highlight collections agree.
   - Public journey fixture now carries the same offer through Homepage and Object Detail.

## Files changed

- `apps/web/src/app/home.component.ts`
- `apps/web/src/styles.css`
- `apps/web/e2e/discovery-ux.spec.ts` (new)
- `apps/web/e2e/public-journey.spec.ts`
- `apps/web/e2e/responsive-preview.spec.ts`
- `apps/web/e2e/zoneless-render.spec.ts`
- `scripts/Test-H06-3-Web-Acceptance.ps1` (new)
- `scripts/Start-H06-3-Browser-QA.ps1` (new)
- `scripts/Verify-H06-3-Web-Postflight.ps1` (new)

No backend/API contract, database, account activation, SMTP, deployment, infrastructure, package manifest or dependency changes.

## Acceptance gates

- [ ] Exact branch sync and clean source.
- [ ] Local mock API syntax and smoke.
- [ ] npm CI, Angular typecheck and production build.
- [ ] Targeted Playwright including `e2e/discovery-ux.spec.ts`.
- [ ] Full Playwright suite.
- [ ] Search order and visibility verified at 320–1440 px.
- [ ] Image success, image failure fallback, alt text and verified badge tests pass.
- [ ] Inconsistent publication counters show unavailable/inconsistent state and recover on retry.
- [ ] Existing directory filters, pagination, cancellation, detail and responsive tests remain passing.
- [ ] Independent postflight verifies exact source SHA, remote SHA, clean source, manifest SHA-256 and every artifact hash.
- [ ] Owner visual review of the new mobile search/hero and business card imagery.

## QA operator entry point

See [H06.3 Browser QA Launcher](H06_3_BROWSER_QA_LAUNCHER_20261120.md). It creates a dedicated worktree, opens the browser, waits for Enter in PowerShell, runs targeted and full headed Playwright, writes evidence and performs independent postflight.

## Current status

**Implementation committed; runtime QA pending.** No local Windows build/browser run has been executed by the operator at this exact H06.3 HEAD. Do not mark H06.3 accepted or merge/deploy until the exact-SHA package passes and screenshots receive owner review.

H06.2 remains Draft/owner-sign-off pending. H06.3 PR is stacked on H06.2.


## 2026-11-20 — implementation handoff

- Current exact branch HEAD: `b997fbb88e4305d6dd23f094a355bff5d611b4d5`.
- New exact-SHA launcher: `scripts/Start-H06-3-Browser-QA.ps1`.
- Targeted runner includes `e2e/discovery-ux.spec.ts` plus the H06.2 public, accessibility, detail and responsive suites.
- Independent postflight retains exact SHA, remote branch, clean-source, manifest SHA-256 and all-file hash checks.
- Dedicated worktree default: `D:\\GitHub\\YangSpaceApp-F2b-H06.3-20261120`.
- Evidence root: `D:\\GitHub\\YangSpaceApp-QA-Evidence\\H06.3`.

**Runtime QA has not yet been executed at this HEAD.** Implementation is committed; all acceptance checkboxes remain pending until the operator runs the package.


## 2026-11-20 — first QA attempt and operator defect correction

First operator run at the previous HEAD:
- Evidence directory: `D:\\GitHub\\YangSpaceApp-QA-Evidence\\H06.3\\df705458a77861b5fe88226d774a7a77bfa92d79-20261120-072246-658`
- Manifest SHA-256: `7394897063e5b7f350e906fbbf6a20a05883b88ad025cc0cf8f5f4b4fbe44549`
- Mock API syntax/smoke, npm CI, Chromium install, TypeScript typecheck and production build passed.
- Targeted Playwright did not execute tests; it stopped before test discovery because the already-running preview occupied port 4300.
- Root cause was an environment variable mismatch: the H06.3 runner set `H06_3_REUSE_SERVER=1`, while Playwright config still read `H06_2_REUSE_SERVER`. This caused Playwright to launch a second server instead of reusing the QA preview.

Correction committed:
- `adb77e7ef0c83931ffbb8bea0523d45c4ba953fb` — Playwright config now reads `H06_3_REUSE_SERVER`.
- `00a13be163f899cfa849a2882d2fdb149e271427` and `1ac7b34d561dd1083dc2343c106c268a631a3dc4` — runner checks the exact config/runner reuse contract before tests.
- `6d0dc67a3c7016cf2eca23c280d1f565bb056be0` — independent postflight requires the new `PLAYWRIGHT_REUSE_CONFIG=PASS` evidence marker.

The first attempt is **not** a Playwright test failure and is not an application acceptance pass. Re-run the package from the existing H06.3 worktree at the latest remote HEAD. The launcher fast-forwards that clean worktree automatically. Do not create another worktree and do not kill unrelated processes.

The npm install-scripts warnings were non-fatal (npm CI returned exit code 0; TypeScript and production build passed). Do not approve package install scripts merely to suppress warnings.
