# YangSpace S0-03 — Enable, Run and Diagnose GitHub Actions

**Current status (2026-11-09):** GitHub REST `/actions/runs` returned `total_count=0` even after the user's authenticated git push of the reviewed lockfile commit `94f710e`. Both `main` and the active v2 branch contain a workflow file, and the repository administrator permissions are confirmed. **Actions disabled, restricted, billing/usage restrictions, skipped workflow or tool visibility issues are hypotheses, not verified causes.**

**Scope:** CI validation only. Do not grant deployment secrets, enable automatic deployments, or move PR #1 out of Draft for this check.

## Repository settings check (administrator UI)

1. Open [YangSpace Actions](https://github.com/jvalkovv/YangSpaceApp/actions). If the page offers **Enable Actions** or **Enable workflow**, use the relevant control only after reviewing the workflow source.
2. Open [Actions → General settings](https://github.com/jvalkovv/YangSpaceApp/settings/actions). Check whether Actions are enabled and whether allowed actions include `actions/checkout@v4`, `actions/setup-dotnet@v4`, `actions/setup-node@v4`, `actions/upload-artifact@v4`. Organization policy may override repository settings.
3. From Actions, select **YangSpace v2 - API and Angular validation**, choose **Run workflow**, set branch `feat/yangspace-v2-foundation-20261108`, and run. If the workflow is absent, inspect whether the default-branch workflow view or policy is hiding it. Do not assume a disabled flag without UI or API evidence.
4. Record the run URL, attempt number, workflow commit SHA and the first failing step if it does not pass. Never share bootstrap credentials or full environment dumps.

## Expected jobs and gates

| Job | Checks | Evidence |
|---|---|---|
| `api` | .NET 10 restore/audit/build, SQLite dependency pin, isolated API smoke + real admin/guest/expiry/persistence tests | `REAL_API_INTEGRATION=PASS` and exit 0 |
| `web` | Tracked npm lock verification, `npm ci`, `npm audit --audit-level=high`, Angular typecheck/build, Playwright | 17+ browser tests PASS, no high/critical advisories |
| `safety` | No active Jenkins/IIS/Azure deploy hooks or unintended release targets | Exit 0 |

**Green CI acceptance:** all three jobs SUCCESS on the *same exact candidate SHA*, and no skipped tests or edited safeguards to mask failures. An open Draft PR and green CI are compatible. **No merge or PVE deployment** is implied.

## Failure triage

| Symptom | Likely area to inspect |
|---|---|
| No workflow or no runs | Settings → Actions, enablement and policies, event filters, execution allowance; confirm active branch and default-branch visibility |
| `LOCKFILE_VERIFY=FAIL` | Manifest drift, unreviewed dependency source or lock metadata |
| `npm ci` fails | Node engine, lockfile conflict, registry issue, optional native dependency |
| `npm audit` fails | Review package advisory/affected versions and upgrade via reviewed PR, not `npm audit fix` blindly |
| `API readiness timeout` | .NET startup exceptions, configured SQLite `/tmp` path, bootstrap Identity or port conflict |
| `INTEGRATION=FAIL` | Real API contract discrepancy; inspect failed endpoint/status, preserve isolated logs |
| Playwright fails | Browser dependencies, dev server, mocked HTTP contracts or CSS regression |
| Runner restrictions | GitHub settings/policy, runner allocation, usage/permissions |

## Operator evidence (copy the non-sensitive parts)

```text
REPO=jvalkovv/YangSpaceApp
PR=1 (Draft)
WORKFLOW=YangSpace v2 - API and Angular validation
BRANCH=feat/yangspace-v2-foundation-20261108
COMMIT_FULL_SHA:
WORKFLOW_RUN_URL:
RUN_ATTEMPT:
API_JOB=PASS | FAIL
WEB_JOB=PASS | FAIL
SAFETY_JOB=PASS | FAIL
FIRST_FAILURE_STEP:
LOCKFILE_CHECK:
NPM_AUDIT:
REAL_API_INTEGRATION:
PLAYWRIGHT:
DECISION=PASS | HOLD
```

Update [POC_TRACKER.md](POC_TRACKER.md) and [BUILD_STATUS.md](BUILD_STATUS.md) only after exact run evidence is available.
