# F2a-3 — Reviewer authorization and conflict-policy QA

**Status:** ACCEPTED — OPERATOR WINDOWS QA PASS / INDEPENDENT EVIDENCE POSTFLIGHT PASS / CHILD PR #23 MERGED; BROADER F2a-3 RELEASE HOLD.  
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
- The revocation test reloads the reviewer through the same scoped UserManager/DbContext that performs removal (no cross-scope EF entity reuse), verifies the role exists before removal, then proves the same bearer returns 403.
- Registration and claim decision mutation route are not exposed.
- Self-review, active target-organization membership, unknown claim and missing reviewer identity fail closed; revoked membership and unrelated reviewer do not conflict.
- No business rows, claim rows or audit rows are created or changed by the probe/policy checks.
- Source worktree remains clean after tests.

## Proof boundary

This is real ASP.NET Core Identity bearer HTTP on an in-process TestServer backed by disposable SQLite. It is **not** PostgreSQL bearer HTTP, a live reviewer decision endpoint, competing claim approval, evidence verification, audit transaction, uncertain-COMMIT recovery, production DB least-privilege, off-host recovery or release acceptance.

The earlier PR #22 PostgreSQL QA proves only a synthetic disposable Npgsql EF transaction and rollback. Do not repeat it for this authorization-only child. A later decision-mutation child requires the separate evidence categories/checklist/retention approval and its own PostgreSQL HTTP/concurrency/fault-injection acceptance.

## Governance

Report exact SHA, stage PASS/PARTIAL/HOLD, evidence path and next action at each meaningful milestone. Merge/close only when this child’s own bounded DoD is 100% accepted. No GitHub Actions, CT164/CT152 access, live DB or deployment.


## 2026-11-13 — final exact-SHA operator acceptance

**Status: ACCEPTED for this bounded reviewer-authorization QA child.** The broader F2a-3 claim-decision/release gate remains HOLD.

- Tested source SHA: `e4ba6bbd697eeaeec4bf6fafe81ab78e30d3f5c4`.
- [PR #23](https://github.com/jvalkovv/YangSpaceApp/pull/23) merged into the F2a-1 parent feature branch as `c54c6cc4f4cd58b184c5d6b0dc7ec6a7c3507808`.
- Native PowerShell parser, .NET restore, Release build with warnings-as-errors and isolated ASP.NET Core Identity bearer HTTP: **PASS**.
- Independent postflight: exact local/remote SHA, clean worktree, manifest values, all three stage receipts and command lines: **PASS**.
- HTTP contract: **27/27 required markers PASS**, including same-token live Identity role revocation; no `gate.txt`.
- Manifest SHA-256: `7c3c3894c229f9020c9892f8b618d0220b79dd4561e64b8eea26b8a0385aa9d0`.
- HTTP stdout SHA-256: `a1007fed8f985d94bfcceb7ee221425b2d2ea55f6571ba7e68c2a9a87313f2b7`.

Operator evidence directory: `D:\GitHub\YangSpaceApp-F2a3-ReviewerAuth-QA-v2\logs\yangspace\f2a3-reviewer-auth\e4ba6bbd697e-20261113-061414-30f957ed`.

The earlier failure at `7ed7ab3d0e00da41fb3d11afeb6ed45c69c2fa1b` remains documented as a superseded test-harness defect. The corrected source was rerun at the exact SHA above.

**Proof boundary:** disposable SQLite TestServer bearer HTTP only. No PostgreSQL bearer HTTP, claim decision mutation, Owner grant, evidence storage, audit mutation, production activation, CT164/CT152 access or deployment. The evidence categories, accepted proof checklist, private evidence access/storage, retention/erasure and legal basis remain a separate mandatory approval gate. Parent PR #17 remains Draft/HOLD for its broader F2a-1 DoD.
