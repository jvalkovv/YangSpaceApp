# F2a-3 — Reviewer authorization and conflict-policy QA

**Status:** DRAFT / SOURCE-ONLY / OPERATOR WINDOWS QA NOT RUN / RELEASE HOLD.  
**Parent:** [F2a-1 PR #17](https://github.com/jvalkovv/YangSpaceApp/pull/17).  
**Policy:** technical security policy explicitly approved by the owner on 2026-11-12. Evidence categories, reviewer evidence checklist and evidence retention/erasure remain a separate mandatory approval gate.

## Scope of this child

- Dedicated `MerchantClaimReviewer` Identity role; `PlatformAdmin` is not implicitly a reviewer.
- Bearer-only policy. The current Identity role is loaded from the persistent Identity store on each request; a previously issued token must stop authorizing after the reviewer role is removed.
- Conflict policy denies self-review and a reviewer who is an active member of the target organization. A revoked membership is not an active-member conflict.
- Separate `ClaimReview:QaEnabled` feature flag. Default OFF; only `YangSpaceQa` + disposable SQLite; incompatible with Preview, Production CMS, registration and MerchantRead QA.
- A minimal `GET /api/v1/staff/claim-review/access` QA probe proves identity authorization only.
- No claim lookup, Approve/Reject route, location ownership change, Owner membership grant, evidence upload/storage, audit mutation, migration or production activation.

## Exact operator acceptance

Use a fresh clean detached worktree at the current PR head. Fetch the branch first so the runner can verify the remote ref:

```powershell
$Repo = 'D:\GitHub\YangSpaceApp'
$Branch = 'feat/yangspace-f2a3-reviewer-auth-qa-20261112'
$Sha = '<exact 40-character PR head SHA>'
$Work = 'D:\GitHub\YangSpaceApp-F2a3-ReviewerAuth-QA'
$Refspec = 'refs/heads/' + $Branch + ':refs/remotes/origin/' + $Branch
git -C $Repo fetch --no-tags origin $Refspec
git -C $Repo worktree add --detach $Work $Sha
```

Run native PowerShell parsing before the runner:

```powershell
$Script = Join-Path $Work 'scripts\Test-F2a3-Reviewer-Authorization-Local.ps1'
$Tokens = $null
$Errors = $null
[System.Management.Automation.Language.Parser]::ParseFile(
    $Script, [ref]$Tokens, [ref]$Errors
) | Out-Null
if ($Errors.Count -gt 0) { $Errors | Format-List; throw 'RUNNER_SYNTAX_FAILED' }

& $Script -CommitSha $Sha
if (!$?) { throw 'REVIEWER_AUTH_QA_FAILED' }
```

The runner pins HEAD and `origin/<branch>` to the same SHA, requires a clean worktree, runs .NET restore and Release build with warnings-as-errors, then runs only `--claim-reviewer-auth-http-only`. It preserves per-stage `runner.cmd`, stdout and stderr, writes a manifest only after all markers and checks pass, and writes `gate.txt` on failure.

## Required PASS markers

- Default-off route and auth login; fail closed in Development and Production; PostgreSQL provider rejected for this SQLite-only harness.
- Real Identity bearer login; anonymous/invalid bearer/cookie-only 401.
- Dedicated reviewer 200; PlatformAdmin-only and non-reviewer 403.
- Same bearer returns 403 immediately after the persisted reviewer role is revoked.
- Registration and claim decision mutation route are not exposed.
- Self-review, active target-organization membership, unknown claim and missing reviewer identity fail closed; revoked membership and unrelated reviewer do not conflict.
- No business rows, claim rows or audit rows are created or changed by the probe/policy checks.
- Source worktree remains clean after tests.

## Proof boundary

This is real ASP.NET Core Identity bearer HTTP on an in-process TestServer backed by disposable SQLite. It is **not** PostgreSQL bearer HTTP, a live reviewer decision endpoint, competing claim approval, evidence verification, audit transaction, uncertain-COMMIT recovery, production DB least-privilege, off-host recovery or release acceptance.

The earlier PR #22 PostgreSQL QA proves only a synthetic disposable Npgsql EF transaction and rollback. Do not repeat it for this authorization-only child. A later decision-mutation child requires the separate evidence categories/checklist/retention approval and its own PostgreSQL HTTP/concurrency/fault-injection acceptance.

## Governance

Report exact SHA, stage PASS/PARTIAL/HOLD, evidence path and next action at each meaningful milestone. Merge/close only when this child’s own bounded DoD is 100% accepted. No GitHub Actions, CT164/CT152 access, live DB or deployment.
