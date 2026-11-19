# F2b H06.2 — Public Product UX, Responsive and Accessibility Acceptance

Parent: [Issue #60](https://github.com/jvalkovv/YangSpaceApp/issues/60) · [Draft PR #61](https://github.com/jvalkovv/YangSpaceApp/pull/61)

## Scope and source lineage

- Accepted F2b baseline: `31cd8ffca9ff3f0fcc750f5b1bde6c0e83724cd1` (H06.1 merge).
- H06.2-A Homepage Highlights Reliability: `e928b6c998ceec7e349c9fde48fb2588bca097cf`. Operator-reported local QA: targeted 6/6, full Playwright 52/52, typecheck and production build PASS.
- H06.2-B Object Detail: loading, successful profile, missing/404, transient unavailable with retry; different copy/CTAs for each state; themed profile hero and publication cards; stale request cancellation; Bulgarian civil-time formatting retained.
- H06.2-C Mobile Navigation: Escape closes open navigation and returns focus to the menu toggle; dynamic accessible button name and expanded state.
- H06.2-D Responsive: homepage/highlights, object detail and preview admin acceptance at 320/360/375/390/430/768/820/1024/1280/1440 px.
- H06.2 regression source includes all H06.1 Account E2E tests. Account registration, email dispatch, Owner grant and claim decisions stay OFF.

## Behavior and accessibility contract

### Homepage

- A failed bootstrap cannot show three genuine empty states.
- Offers, brochures and jobs have separate loading/success-empty/unavailable presentations and a retry.
- Recovery with active directory filters/pagination preserves the current directory result list.

### Object detail

- `/obekt/:slug` reads one route-controlled request stream; a retry or slug change supersedes an older request.
- Pending loading has status/live feedback and an `aria-busy` region.
- 404/missing shows a not-found state and catalogue CTA, **not** a transient retry.
- 5xx/network/timeout shows visitor-facing feedback, retry and catalogue exit; no API endpoint/localhost/proxy/internal detail leaked to the visitor.
- The existing accepted publication copy, section selectors and Bulgarian date/time semantics are preserved.
- Photo fallback, contact actions and publication visual hierarchy follow the current forest/sage/cream design system.
- Avoid nested `main` landmarks; top-level app already owns `<main id="main-content">`.

### Navigation

- Mobile toggle has `aria-expanded`, `aria-controls` and an action-specific accessible name.
- Escape works from a focused navigation link or the toggle, closes the menu and restores focus to the toggle.
- Navigation links still close the menu after selection.

### Responsive and manual review

Automated screenshot matrix:
- Homepage with data and offers/brochures/jobs at the ten required widths.
- Object detail with offers/brochures/jobs at all ten widths.
- Preview Admin authorized workspace plus editor at all ten widths, using mock endpoints only.

Manual acceptance on actual devices / browsers:
- 320/390/768/1440 primary visual hierarchy; touch targets, keyboard focus ring, accessible names, scroll/overflow, sticky and fixed elements.
- Anonymous home → filtered directory → detail → back and preview admin authorization boundary.
- Keyboard tab sequence, Escape behavior, reduced-motion setting, screen-reader status announcements.
- Console/page errors: separate expected mocked 503 noise from unexpected exceptions; no real PII/tokens in screenshots/logs.
- Record any contrast or WCAG 2.2 AA exception rather than silently marking PASS.

## One-command interactive browser QA

Run from PowerShell 5.1 or PowerShell 7, using the existing clean H06.2 worktree:

\`\`\`powershell
$Work = 'D:\GitHub\YangSpaceApp-F2b-H06.2-20261118'
$Branch = 'feat/yangspace-f2b-h06-2-public-product-ux-20261118'

git -C $Work fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'H06_2_FETCH_FAILED' }

git -C $Work merge --ff-only ('origin/' + $Branch)
if ($LASTEXITCODE -ne 0) { throw 'H06_2_SYNC_FAILED' }

& (Join-Path $Work 'scripts\Start-H06-2-Browser-QA.ps1') -Work $Work
\`\`\`

The launcher is the normal operator entry point. It validates the clean branch, fetches and fast-forwards to the exact remote SHA, installs locked dependencies and Chromium, runs TypeScript and production build gates, starts the disposable in-memory mock API plus Angular preview, and opens the browser. Review the homepage, directory, detail page and Preview Admin; then return to the PowerShell window and press **Enter**. The package runs targeted and full headed Playwright suites and performs the independent checksum/source postflight.

The mock API uses disposable demo fixtures only. No production API, database, real account, SMTP, tenant data or external service is used. The browser preview is \`http://127.0.0.1:4300/\`; mock API is \`http://127.0.0.1:5090/\`.

The launcher prints \`EVIDENCE_DIR\` and \`MANIFEST_SHA256\`. On a failed gate it prints the run summary, evidence location and first failing log; do not rerun only the postflight or mark the milestone accepted. Send the evidence directory and first failing log for diagnosis.

The lower-level runner remains available for controlled diagnosis:

\`\`\`powershell
& (Join-Path $Work 'scripts\Test-H06-2-Web-Acceptance.ps1') -CommitSha $Sha -Work $Work -EvidenceRoot 'D:\GitHub\YangSpaceApp-QA-Evidence\H06.2'
\`\`\`

Do not use the lower-level command as the normal operator flow. The launcher owns sync, browser preview, test execution, evidence discovery and independent postflight.

## Status

**SOURCE IMPLEMENTATION IN PROGRESS / QA PENDING.** Do not mark this milestone accepted, merge PR #61 or deploy until the complete exact-SHA acceptance matrix and independent postflight are PASS.

No backend H01–H05 contract, package manifest, production host, CT164, real SMTP, Cloudflare/Nginx/ingress, public registration, claim decision, evidence intake or GitHub Actions change belongs to this scope.


## 2026-11-20 — integrated build failure correction and interactive QA package

Operator's exact-SHA run at `51a9059d4a520b21d40f0279b536f0ef11b07983`:
- `npm ci`: PASS (exit 0). npm printed an install-scripts policy warning for four packages; this was not the failing gate.
- TypeScript typecheck: PASS.
- Angular production build: FAIL because Angular's template event binding exposes `$event` as `Event`, but `onMenuEscape` unnecessarily required `KeyboardEvent`.
- Targeted/full Playwright did not run after the build gate failed.

Correction: `onMenuEscape(event: Event)` is the accurate contract for the operations used (`preventDefault`, `stopPropagation`); focus restoration and Escape behavior are retained.

New interactive QA package:
- `scripts/Start-H06-2-Browser-QA.ps1`: single entry point; clean-worktree guard, GitHub fetch and safe fast-forward, automatic exact SHA, Chromium install, build and evidence orchestration, automatic independent postflight.
- `scripts/Test-H06-2-Web-Acceptance.ps1`: added interactive/headed mode, Node mock API syntax gate, local server lifecycle, Enter prompt, and headed focused/full Playwright. Preview cleanup is wired to the returned API/web process set.
- `apps/web/qa/mock-api.mjs`: deterministic fictional data and in-memory preview-only endpoints on loopback `127.0.0.1:5090`; no database, SMTP or persistence. The runner smoke-checks homepage, object detail, fake PlatformAdmin identity, admin directory and publications endpoints before the Playwright suites.
- `apps/web/playwright.config.ts`: permits reuse only when the launcher explicitly sets `H06_2_REUSE_SERVER=1`; default Playwright behavior still starts its own server and refuses reuse.
- `scripts/Verify-H06-2-Web-Postflight.ps1`: now requires local mock API syntax and API smoke gates in the evidence manifest.
- Full operator guide: `docs/H06_2_BROWSER_QA_LAUNCHER_20261120.md`.

Expected interaction: the launcher starts the local mock API and Angular dev server, opens the browser on `http://127.0.0.1:4300/`, and waits for Enter in the PowerShell console. After Enter, it opens headed Playwright Chromium, runs the focused and full suites, stops only the processes it created, writes screenshots/logs/trace evidence and performs independent postflight. The manual browser and Playwright-controlled browser are separate windows.

Preview Admin demo identity is `qa@example.invalid` / `DisposableTestPassword123!`; the mock token is local-only, data is in-memory and resets on shutdown. Public account flows remain disabled.

**Status: source correction and launcher committed; exact-SHA build, headed Playwright, visual review and postflight are PENDING.** Do not mark H06.2 accepted or merge PR #61 until the new launcher completes successfully.


### QA package hardening follow-up

- Detail loading test now checks that the loading card disappears while the three publication empty-state live regions remain; it no longer incorrectly expects all `role=status` elements to disappear.
- Native gate output is captured as separate `.stdout.log` and `.stderr.log` files plus a combined log. Native process exit code remains the only pass/fail authority; npm lifecycle policy warnings are not promoted to gate failures.
- Evidence folder timestamps include milliseconds to avoid same-second collisions.
- The one-command launcher snapshots existing evidence folders, identifies exactly one newly created run directory, prints its manifest hash, and preserves the evidence path and first failing gate log when acceptance fails.
- Latest source checkpoint before this documentation update: `3fd2a648c7037602f58d55083217b7ab52f09ca3`. Runtime typecheck/build, headed Playwright and postflight remain **PENDING**.
