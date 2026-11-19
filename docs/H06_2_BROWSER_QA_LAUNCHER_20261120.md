# H06.2 — One-command interactive browser QA package

## Entry point

Run from Windows PowerShell 5.1 or later, on the H06.2 branch:

```powershell
$Work = 'D:\GitHub\YangSpaceApp-F2b-H06.2-20261118'
$Branch = 'feat/yangspace-f2b-h06-2-public-product-ux-20261118'

git -C $Work pull --ff-only origin $Branch
& (Join-Path $Work 'scripts\Start-H06-2-Browser-QA.ps1')
```

After this first update, the launcher fetches the branch itself and fast-forwards only when the local worktree is clean and the local branch has no unpushed commits. It refuses dirty, diverged, or locally-ahead branches. It resolves the current exact commit SHA automatically; no manual SHA copy is needed.

## What the launcher does

1. Verifies the expected branch and a clean source worktree.
2. Fetches GitHub and fast-forwards only; verifies local HEAD equals the remote branch HEAD.
3. Runs Node syntax validation for the local QA mock API.
4. Runs `npm ci`, installs Playwright Chromium, TypeScript typecheck and Angular production build.
5. Starts a deterministic in-memory API fixture bound only to `127.0.0.1:5090`.
6. Starts the Angular development server on `127.0.0.1:4300`, using the existing local proxy configuration.
7. Opens the default browser at `http://127.0.0.1:4300/` and waits for the operator to press Enter in the same PowerShell window.
8. On Enter, runs the focused H06.2 and full Playwright suites in headed Chromium against the already-running local preview.
9. Stops only the two child processes it started, preserves logs/screenshots/traces and writes a SHA-256 manifest.
10. Runs the independent evidence postflight automatically. It checks exact source/remote SHA, clean worktree, required gate markers, manifest and every evidence-file hash.

The browser opened for manual inspection and the Playwright-controlled Chromium window are separate windows. Keep the PowerShell session open. Inspect the preview first, then return to the console and press Enter.

## Local preview data and safety

The mock API is `apps/web/qa/mock-api.mjs` and uses Node built-ins only. It:
- binds exclusively to loopback `127.0.0.1`;
- stores demo data in process memory only;
- has no database, filesystem persistence, SMTP, external API or production connection;
- resets all preview edits when the mock process stops;
- uses fictional Varna businesses and a local-only fake PlatformAdmin identity;
- rejects public-account registration and recovery routes with HTTP 403.

Preview Admin demo credentials:
- Email: `qa@example.invalid`
- Password: `DisposableTestPassword123!`

These credentials and the bearer token are local QA fixtures, not production credentials. Never reuse them outside this isolated mock.

The launcher checks ports 5090 and 4300 before starting. If either is already occupied, it stops rather than terminating or reusing an unrelated process.

## Evidence

Evidence is written under:

`D:\GitHub\YangSpaceApp-QA-Evidence\H06.2\<commit-sha>-<timestamp>\`

Expected contents include runtime versions, npm/typecheck/build logs, mock API and Angular server logs, targeted/full Playwright logs, screenshots/traces, run summary and SHA-256 manifest.

On complete success the console ends with:
- `H06_2_QA=PASS`
- `H06_2_INDEPENDENT_POSTFLIGHT=PASS`
- `H06_2_AUTOMATED_QA_PACKAGE=PASS`

On failure, no PASS is asserted. The runner prints `EVIDENCE_DIR`, `MANIFEST_SHA256` and the first failed gate. Send the full console output and the evidence directory; do not modify the source worktree during the run.

## Current defect correction

The first integrated run at `51a9059d4a520b21d40f0279b536f0ef11b07983` passed npm CI and TypeScript typecheck but failed Angular production compilation because the template's keyboard event was inferred as `Event`, while the handler unnecessarily required `KeyboardEvent`. The handler now accepts the actual `Event` contract while retaining preventDefault, stopPropagation and focus restoration. The npm `install-scripts` output was a warning with exit code 0, not the build failure.

## Acceptance boundary

This package is local browser QA only. It does not deploy, change the backend API contract, enable public registration/email dispatch, alter production DB/key-ring, grant Owner privileges, ingest merchant evidence, or touch CT164, SMTP, DNS, proxy, ingress or GitHub Actions.
