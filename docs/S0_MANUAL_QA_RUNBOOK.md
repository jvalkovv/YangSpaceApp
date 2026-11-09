# YangSpace — S0 Manual QA and exact-SHA release gate

**GitHub Actions: NOT USED.** This is the canonical replacement for the retired S0 CI runbook.

## Scope and safety

Validation uses the **committed source** from GitHub with a detached clean copy. The current Windows working tree may contain unsaved changes to Angular configuration, test ZIPs and a running dev server; these are neither staged nor modified by the scripts.

- No direct npm ci against the active apps/web/node_modules while Angular is running.
- No tests against a running merchant DB, local preview database or PVE production instance.
- No secret passwords passed as arguments or committed to Git.
- No GitHub hosted jobs, webhook deployment, or implicit release on push.
- Logs stored under logs/yangspace/verify/ and logs/yangspace/api-integration/ (ignored by Git, retained by the operator).

## Step 1 — Exact-commit clean-source validation (Windows)

From repository root, after fetching the approved feature branch:

~~~powershell
cd D:\GitHub\YangSpaceApp
git pull --ff-only origin feat/yangspace-v2-foundation-20261108
git rev-parse HEAD
.\scripts\Verify-Source-Local.ps1
~~~

The script:
- archives the local HEAD commit into disposable OS TEMP (does not copy local uncommitted files);
- checks source-tracked npm lock manifest/integrity and runs npm ci, high-severity audit, typecheck, production build, and Playwright;
- runs .NET restore and Release build with warnings-as-errors;
- records exact SHA and per-command stdout/stderr evidence;
- cleans the isolated source tree (unless explicitly retained for debugging).

**Required result:** SOURCE_VALIDATION=PASS, manifest contains matching full COMMIT_SHA and SOURCE_VALIDATION=PASS. If browser testing is deliberately skipped, mark the gate HOLD until browser evidence exists. A dependency advisory failure is a real HOLD, not an excuse to remove npm audit.

A successful run proves source compilation/browser QA, not admin DB authorization.

## Step 2 — Real API integration in disposable SQLite (Windows)

~~~powershell
.\scripts\Verify-Api-Local.ps1
~~~

The script builds from a separate committed archive, creates a new OS-TEMP sandbox prefixed yangspace-ci-, picks an ephemeral 127.0.0.1-only port, creates a random non-logged bootstrap secret, and launches Staging Preview from the isolated build. It runs native `scripts/Invoke-Api-Integration.ps1 -Phase prepare`, terminates only the test API, restarts the same isolated DB, and runs `-Phase verify`. **No Python installation is needed on Windows.**

PowerShell's strict guard rejects unexpected DB paths, missing sandbox marker, non-loopback URLs, mismatched config or absent secrets. Its write actions must never run against real business data.

**Required result:** REAL_API_INTEGRATION=PASS; matching COMMIT_SHA in manifest; expected guest/nonadmin denial, create/edit/publish/expiry/hide and persistence after restart.

## Step 3 — Evidence acceptance

Check that both scripts tested the *same* full SHA, both returned PASS and the audit did not fail. Review unexpected HTTP statuses/advisories and preserve logs in the corresponding app directory. The operator records:

~~~text
TASK=YS-S0-03/YS-S0-04
CANDIDATE_SHA=40 hex
SOURCE_VALIDATION_MANIFEST=logs/yangspace/verify/.../manifest.txt
API_INTEGRATION_MANIFEST=logs/yangspace/api-integration/.../manifest.txt
DEPENDENCY_AUDIT=PASS/HOLD
PLAYWRIGHT=17+ PASS/HOLD
REAL_API_INTEGRATION=PASS/HOLD
SECURITY_REVIEW=PASS/HOLD
DECISION=PASS/HOLD
~~~

Do not declare G0 PASS until the security review and release candidate policy are approved.

## Step 4 — PVE private preview build (later; no deployment now)

The build-only script supports **allowlisted branch refs** or main. The current branch is feat/yangspace-v2-foundation-20261108. Required operator-provided environment references:

~~~bash
export YANGSPACE_SOURCE_QA_MANIFEST=/secure/operator-evidence/source-manifest.txt
export YANGSPACE_API_QA_MANIFEST=/secure/operator-evidence/api-manifest.txt
bash scripts/build-preview-from-github.sh FULL_40_HEX_SHA feat/yangspace-v2-foundation-20261108
~~~

Both manifests must match the SHA and PASS values. The script fetches the exact allowlisted remote branch tip, verifies it matches the SHA, builds an immutable release directory and prints ACTIVATED=NO. No PVE CT ID, DNS, TLS or backup values are invented by this document. Activation requires separate backup/rollback and private-access approval.

## Failure classification

| Symptom | Root-cause area |
|---|---|
| npm ci EPERM on LMDB | Wrong working directory (live node_modules); use clean snapshot |
| npm audit HIGH/CRITICAL | Review pinned dependency vulnerabilities; upgrade in reviewed commit |
| Playwright failure | Browser cache, reserved test port 4300, async UI contracts |
| API restore/build failure | NuGet/SDK/SQLite native package constraints |
| Real API integration failure | Auth/endpoint/DB behavior; inspect per-stage local logs |
| SHA mismatch | Git HEAD, detached snapshot, manifests or remote ref drift |
| PVE build refuses | Unapproved branch, missing matching QA or not-exact remote tip |

The workflow **does not require GitHub Actions** at any point.

## S0 build-stage apparent hang (Windows)

The API runner redirects build output to `logs/yangspace/api-integration/<run-id>/api-build.stdout.log` and `api-build.stderr.log`. In another PowerShell console, inspect the newest run before stopping anything:

~~~powershell
$D = Get-ChildItem .\logs\yangspace\api-integration -Directory | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Get-Content (Join-Path $D.FullName 'api-build.stdout.log') -Tail 30
Get-Content (Join-Path $D.FullName 'api-build.stderr.log') -Tail 20
~~~

If stdout already says `Build succeeded.` with `0 Error(s)` but the test session is still blocked at `STAGE=api-build`, interrupt **only that test shell** with Ctrl+C, update GitHub branch and rerun the patched script. The updated wrapper does not use `Start-Process -Wait`: it waits for the direct command PID with bounded timeout and logs the failure, avoiding indefinite process-tree waits. If stdout shows errors/ongoing build, diagnose the log rather than assuming a wait bug. Never taskkill all dotnet processes or stop API port 5090.

## 2026-11-09 — Process runner self-test (required before retesting API)

A successful .NET restore can be misreported as failed if `Start-Process -PassThru` leaves `ExitCode` empty in Windows PowerShell 5.1. The shared `Invoke-YangSpaceStage.ps1` uses an explicit .NET process handle and a short temporary CMD launcher with separate log streams, a bounded timeout and a real exit code. Test positive and negative exits **before** the more expensive integration flow:

~~~powershell
cd D:\GitHub\YangSpaceApp
git pull --ff-only origin feat/yangspace-v2-foundation-20261108
.\scripts\Test-Process-Runner.ps1
~~~

Expected: `STAGE_PASS=runner-success`, a deliberate `STAGE_FAILED=runner-failure EXIT=7` caught by the self-test, `STAGE_PASS=runner-npm-cmd`, and `PROCESS_RUNNER_SELFTEST=PASS`. This is non-destructive, operates solely within a temporary scratch directory and never touches the live API or DB. Then run `Verify-Api-Local.ps1`; only after its PASS re-run `Verify-Source-Local.ps1` for the exact same latest SHA. The original source PASS for `133cfd6` remains valid for that historical commit.

## 2026-11-09 — Native Windows API QA (Python not required)

Windows `python.exe` can be a Microsoft Store App Execution Alias; it returns exit 9009. The Windows API wrapper now runs a native PowerShell HTTP contract, without Python. After pulling the latest approved source:

~~~powershell
cd D:\GitHub\YangSpaceApp
.\scripts\Test-Api-Native-Guard.ps1
.\scripts\Verify-Api-Local.ps1
~~~

The guard test first parses the native test file and then expects a **failure** when disposable-test conditions are disabled. Required output is `NATIVE_API_INTEGRATION_PARSE=PASS`, `NATIVE_API_INTEGRATION_GUARD=PASS`, `NATIVE_API_SELFTEST=PASS`. Only then execute real API integration; the wrapper starts its own random loopback instance and an isolated temp SQLite database, never your local API on port 5090. Required result: `REAL_API_INTEGRATION=PASS`. All run logs are retained under `logs/yangspace/api-integration/`.

The earlier `scripts/api-integration.py` is not used by Windows validation and remains optional for Linux/manual environments. Runtime PASS is pending.

## S0-05 registration/privilege boundary regression

Current hardening makes `/api/auth/register` unavailable by default even in preview. Only the disposable QA wrapper explicitly sets `Preview__AllowRegistration=true` to create a normal user for negative permission tests; before the second API restart, the wrapper switches the same flag to `false`. `Invoke-Api-Integration.ps1 -Phase verify` must then receive HTTP 404 from registration and still authenticate the previously bootstrapped administrator and confirm DB persistence. A pre-existing ordinary account can no longer be auto-promoted to `PlatformAdmin` by setting `Bootstrap__AdminEmail`. This latter case requires additional negative runtime evidence before S0-05 is DONE. For actual private preview, registration remains disabled and access must be protected at the network/gateway layer. **Do not enable self-registration in PVE or expose Staging publicly.**

## SEC-02 — existing nonadmin cannot become PlatformAdmin

After `api-restart-verify`, the updated `Verify-Api-Local.ps1` performs:

1. `STAGE=api-bootstrap-denial`: boot isolated API using the previously created ordinary user as the configured bootstrap admin; startup **must exit** with the expected denial and must not begin serving.
2. `BOOTSTRAP_PRIVILEGE_ESCALATION_DENIED=PASS` only if the expected exception appears in its own logs.
3. `STAGE=api-post-bootstrap-recovery` and `api-post-bootstrap-verify`: restore original admin, restart on the same isolated SQLite DB, require admin success and persisted ordinary-user HTTP 403, confirm preview registration still returns 404.
4. Required final markers: `BOOTSTRAP_NONADMIN_ROLE_RETAINED=PASS`, `REAL_API_INTEGRATION=PASS`; both must appear with matching new SHA. A failure in any step is HOLD.

The negative bootstrap startup exception is **expected**, not an application regression. The nonadmin identity and database are fictional and disposable, not live. Run the guard and API scripts against the next reviewed commit, followed by the full source QA on that exact SHA. No GitHub Actions.

## SEC-02 Windows stdout/stderr file-lock regression

The first `9fd0a84` SEC-02 run passed normal API integration through `api-restart-verify` but was interrupted at `api-bootstrap-denial`: `ReadAllText` could not open the still-locked redirected stderr file. This is a **harness failure**, not an assertion of successful or failed escalation. The updated probe starts its own `System.Diagnostics.Process`, reads both pipes asynchronously, waits with a timeout, then writes closed-stream logs. The ordinary user's role and admin recovery still require a full runtime PASS. The QA wrapper now disposes the normal test API process handle before deleting its own temp sandbox. No action should be taken against arbitrary `dotnet` processes or other databases.

Rerun `Test-Api-Native-Guard.ps1`, `Verify-Api-Local.ps1` and `Verify-Source-Local.ps1` on the updated full SHA and compare manifest evidence. Do not delete old test directories while a process is still using them; after verifying only the isolated test process is gone, manual cleanup may remove that specific `yangspace-ci-*` folder.

## S0-06 — Linux operator manual BUILD-ONLY gate

The last accepted Windows test pair is commit `b30c1c819aa1aac1cd056c4fb8a03c4b7a469dd9`. On a future approved Linux environment, run `bash scripts/test-preview-release-evidence.sh` first. It creates synthetic private TMP fixtures and checks expected fail-closed cases; it **does not use real QA evidence, access GitHub, build, write to PVE, approve or deploy**.

For a real build, use manually reviewed manifest copies **outside the Git checkout** and separate operator-controlled approval file. Must contain matching full `COMMIT_SHA`, allowlisted `SOURCE_REF`, exact SHA-256 of the manifests, `APPROVAL=BUILD_ONLY`, `REVIEWER_ID` and restricted ACLs. Manifest contents must include `WEB_BROWSER=PASS` and both bootstrap denial/persistence markers. See [PVE release runbook](POC_PVE_RELEASE_RUNBOOK.md). No GitHub Actions. Do not use the approval file to authorize an activation/restart: that needs a separate human change window and full G-PREVIEW checks. Private PVE execution remains HOLD pending topology/ACL, backup/restore and approval provenance.
