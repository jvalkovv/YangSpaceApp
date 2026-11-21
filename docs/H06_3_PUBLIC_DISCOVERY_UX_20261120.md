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


## 2026-11-20 — H06.3 first Playwright execution and test corrections

QA attempt at exact SHA `68eadb43c729ec01a6d27aa3f6b2a35d214c76b0`:
- Evidence: `D:\\GitHub\\YangSpaceApp-QA-Evidence\\H06.3\\68eadb43c729ec01a6d27aa3f6b2a35d214c76b0-20261120-073003-092`
- Manifest SHA-256: `7eb7d8d44e42b89ded75078dbda30ab7261d8cc9c40161cc8bbf458a9d7ab03f`
- Passed: mock API syntax/smoke, Playwright reuse contract, npm CI, TypeScript typecheck, production build.
- Targeted Playwright executed 76 tests: 62 passed, 14 failed. Full suite and independent postflight did not run because the targeted gate stopped on failures.

Failure analysis from the failing specs and current template:
1. `discovery-ux.spec.ts`: the DOM-order assertion read `classList[0]`; `.quick-discovery` has multiple classes and its first class is `shell`. The test was checking class-token order rather than the intended semantic section selector.
2. `homepage-highlights.spec.ts`: the “genuine empty state” fixture returned empty offers/jobs while directory cards still advertised active offers/jobs. The application correctly treated these contradictory counters as inconsistent/unavailable. The empty-state fixture now sets both counters to zero.
3. `public-journey.spec.ts` and `responsive-preview.spec.ts`: broad accessible-link matching for the business name matched both the directory profile card and a separate offer highlight linking to the same profile. The assertions now scope to `.location-card`, preserving both valid public links.

The 14 reported failures map to these test expectation/fixture defects (1 discovery-order assertion, 1 empty-state fixture, 2 public journey link assertions, 10 responsive viewport assertions). This run did not establish an application runtime defect; it also does not constitute acceptance.

Corrections committed to the same H06.3 branch:
- `892e70465eb588362163974333d62f83050fda9d` — use semantic class membership for DOM-order assertion.
- `2a59af6143f45e9936c5409182b7bd17461279a5` — make empty highlights fixture internally consistent.
- `02fbcc8555699a1e8972d833cabd1f9726734ee6` — scope public journey link checks to directory cards.
- `ef5e1e78cd7b5e47aa56e974e73ad6a3ca540e54` — scope responsive link checks to directory cards.

The test-correction commit is `ef5e1e78cd7b5e47aa56e974e73ad6a3ca540e54`. Subsequent acceptance-document updates advanced the branch tip; run the existing H06.3 launcher from the existing worktree so it fetches and fast-forwards to the current remote branch HEAD. Do not create a new worktree or pin the earlier test commit. Runtime acceptance remains pending until targeted and full Playwright plus independent postflight pass.


## 2026-11-21 — H06.3 rerun blocked by occupied preview port

Rerun at exact commit `1be585705b044f3f1afc6b430fd4b0371ff4ba6f` synchronized successfully from GitHub. Gates:
- LOCAL_MOCK_API_SYNTAX=PASS
- PLAYWRIGHT_REUSE_CONFIG=PASS
- NPM_CI=PASS (npm emitted install-scripts approval warnings to stderr, but native exit code was 0; do not approve install scripts as part of this incident)
- Chromium install=PASS
- TYPECHECK=PASS
- PRODUCTION_BUILD=PASS
- SOURCE_CLEAN_POST=PASS
- LOCAL_MOCK_API_SMOKE=NOT_RUN
- TARGETED_PLAYWRIGHT=NOT_RUN
- FULL_PLAYWRIGHT=NOT_RUN

The runner stopped in `Start-InteractivePreview()` with `H06_3_PREVIEW_PORT_4300_ALREADY_IN_USE`. Its preflight checks port 4300 and refuses to start a second Angular preview. This is an environment/process ownership collision, not a Playwright test failure and not a code/build failure.

Evidence directory: `D:\GitHub\YangSpaceApp-QA-Evidence\H06.3\1be585705b044f3f1afc6b430fd4b0371ff4ba6f-20261121-125252-247`
Manifest SHA-256: `1bbae75d1c849a553a19a86375a87ec03032b042dac3f7b77ea982f7f0068095`

Recovery is deliberately read-only first: inspect listeners on TCP 4300 and 5090, capture PID, executable, command line and parent process, then decide whether the process is the stale H06.3 preview, another project, or an unrelated service. Do not terminate an unidentified listener. After identifying it, either reuse the correct existing H06.3 preview safely or stop only the confirmed stale QA process and rerun the launcher.

Acceptance remains HOLD. No merge, deployment, process termination, or npm install-script approval is authorized by this run.
