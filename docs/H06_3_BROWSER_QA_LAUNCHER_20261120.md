# H06.3 — One-command Browser QA Launcher

## Prerequisites

- Windows PowerShell 5.1 or PowerShell 7.
- Git for Windows, Node.js version allowed by `apps/web/package.json`.
- Existing clone at `D:\GitHub\YangSpaceApp`.
- No local edits in the source repository are overwritten; a dedicated worktree is created.

## Create the H06.3 worktree and launch

Run this entire block from PowerShell. It resolves the real `git.exe`, fetches the exact remote branch, creates a separate worktree, then starts the launcher in a clean PowerShell process without loading the user's profile.

```powershell
$ErrorActionPreference = 'Stop'

$GitExe = (Get-Command git.exe -CommandType Application -ErrorAction Stop |
    Select-Object -First 1).Source

$Repo = 'D:\GitHub\YangSpaceApp'
$Work = 'D:\GitHub\YangSpaceApp-F2b-H06.3-20261120'
$Branch = 'feat/yangspace-f2b-h06-3-public-discovery-ux-20261120'

if (!(Test-Path -LiteralPath $Repo -PathType Container)) {
    throw 'YANGSPACE_REPOSITORY_NOT_FOUND'
}
if (Test-Path -LiteralPath $Work) {
    throw 'H06_3_WORKTREE_ALREADY_EXISTS'
}

& $GitExe -C $Repo fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'H06_3_FETCH_FAILED' }

& $GitExe -C $Repo worktree add --track -b $Branch $Work "origin/$Branch"
if ($LASTEXITCODE -ne 0) { throw 'H06_3_WORKTREE_CREATE_FAILED' }

$Launcher = Join-Path $Work 'scripts\Start-H06-3-Browser-QA.ps1'
if (!(Test-Path -LiteralPath $Launcher -PathType Leaf)) {
    throw 'H06_3_LAUNCHER_NOT_FOUND'
}

$PowerShellExe = (Get-Process -Id $PID).Path
& $PowerShellExe -NoProfile -ExecutionPolicy Bypass -File $Launcher -Work $Work

if ($LASTEXITCODE -ne 0) { throw 'H06_3_QA_LAUNCHER_FAILED' }
```

## Operator flow

1. The launcher verifies the branch, clean worktree and exact remote SHA.
2. It installs locked npm dependencies and Chromium when needed.
3. It runs mock API syntax/smoke, TypeScript and production build gates.
4. It starts the disposable localhost mock API and Angular preview, then opens the browser.
5. Review Homepage, mobile discovery search, category filters and object cards.
6. Return to PowerShell and press **Enter** when the browser review is complete.
7. The package runs targeted headed Playwright (including `discovery-ux.spec.ts`), then the full headed suite.
8. It writes screenshots, logs and SHA-256 evidence, then runs independent postflight.

The preview uses disposable local fixtures only. It does not call production APIs, modify a database, send email or activate public registration.

## Evidence

Default evidence root:

`D:\GitHub\YangSpaceApp-QA-Evidence\H06.3`

The package prints:
- `COMMIT_SHA`
- `EVIDENCE_DIR`
- `MANIFEST_SHA256`
- `H06_3_AUTOMATED_QA_PACKAGE=PASS` only after all gates and postflight pass.

If any gate fails, the package stops, preserves evidence and reports the first failing log. Send the full terminal output, evidence directory and manifest hash for diagnosis. Do not mark the issue accepted based on partial Playwright results.

## Recovery

The QA preview and mock API are disposable local processes. If the browser closes unexpectedly, return to the PowerShell window and allow the runner to capture the failure and evidence. Do not delete the evidence directory before review.

No production deployment, backend activation, account registration or email dispatch is part of H06.3.


## Re-run after a failed attempt (existing worktree)

If `D:\\GitHub\\YangSpaceApp-F2b-H06.3-20261120` already exists, **do not run the create-worktree block again**. The H06.3 launcher will fetch the remote branch and fast-forward the clean worktree to the latest commit before starting QA.

Run:

```powershell
$ErrorActionPreference = 'Stop'
$Work = 'D:\\GitHub\\YangSpaceApp-F2b-H06.3-20261120'
$Launcher = Join-Path $Work 'scripts\\Start-H06-3-Browser-QA.ps1'
$PowerShellExe = (Get-Process -Id $PID).Path

if (!(Test-Path -LiteralPath $Launcher -PathType Leaf)) {
    throw 'H06_3_LAUNCHER_NOT_FOUND'
}

& $PowerShellExe -NoProfile -ExecutionPolicy Bypass -File $Launcher -Work $Work
if ($LASTEXITCODE -ne 0) { throw 'H06_3_QA_LAUNCHER_FAILED' }
```

The corrected Playwright configuration and QA runner both use `H06_3_REUSE_SERVER`. The runner now checks this contract before starting browser tests and writes `PLAYWRIGHT_REUSE_CONFIG=PASS` into the evidence manifest. Independent postflight requires that marker.

The previous attempt stopped before Playwright executed any test because the config looked for the old H06.2 environment variable and tried to start a second preview server. That run is retained as diagnostic evidence only; it is not an acceptance result.
