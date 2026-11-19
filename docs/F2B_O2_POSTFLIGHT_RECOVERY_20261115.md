# F2b O2 — Postflight recovery handoff (2026-11-15)

## Accepted O2 runner evidence

The complete O2 runner passed at exact source commit:

`e1944650f48f1ec9acc3161df0527a57c70defe9`

Evidence directory:

`logs/yangspace/f2b-o2/e1944650f48f-20261115-220828-c3fc043e`

Manifest SHA-256:

`cdb36392e06cba0ee8b2f42e399d3a75562bc57ead88412e50260b41c06af03f`

The operator then ran the first independent postflight successfully at verifier SHA `3e9697961aa2158fd06f40c0579b239879a42069`, producing `F2B_O2_POSTFLIGHT=PASS`.

Accepted markers include:

- `F2B_O2_SQLITE=PASS`
- `F2B_O2_POSTGRESQL_17=PASS`
- `F2B_O2_INTEGRITY=PASS`
- `F2B_O2_CONCURRENCY=PASS`
- `F2B_O2_POSTFLIGHT=READY` from the runner
- Independent postflight `F2B_O2_POSTFLIGHT=PASS`

CT164 and production databases were not accessed. SMTP remains disconnected, PublicAccounts remains OFF, and the dispatcher remains fail-closed.

## Why the original postflight needed a correction

The original read-only verifier completed the evidence hashes, including PostgreSQL SQL, public root CA, Linux runtime DLL and LF/UTF-8-no-BOM bootstrap. It then invoked `docker inspect` for the exact ephemeral PostgreSQL container.

Docker correctly returned `no such object`: the runner had already removed the container. Windows PowerShell 5.1 combined with `$ErrorActionPreference = 'Stop'` promoted this expected native stderr into a terminating error. This was a verifier error-handling defect, not an O2 runner or migration failure.

The v2 verifier replaced that check with successful local Docker container/network listings and exact-name matching. Docker CLI/list failures remain hard failures and cannot be mistaken for cleanup success.

## Why one final read-only postflight is requested

After the first successful postflight, the current F2b status records were synchronized to supersede stale `NOT RUN / HOLD` text in:

- `docs/SSOT.md`
- `docs/POC_TRACKER.md`
- `docs/MVP_ROADMAP.md`
- `docs/F2B_EMAIL_OUTBOX_ENQUEUE_20261114.md`

The v2 verifier's explicit approved-follow-up allowlist now permits only those four documentation paths, this recovery handoff and the v2 verifier itself between the original runner SHA and the current verifier HEAD. The original `scripts/Verify-F2b-O2-Postflight.ps1` and `docs/F2B_O2_VALIDATION_RUNBOOK_20261115.md` remain byte-for-byte unchanged because they are included in the original runner's SHA-256 manifest.

This final invocation verifies the current documentation/verifier-only diff and rechecks the already accepted evidence. It does **not** rerun O2, recreate Docker resources, connect to a database or change production.

## Exact operator command

Use the existing worktree only; do not create another QA clone:

```powershell
$ErrorActionPreference = 'Stop'

$Repo = 'D:\GitHub\YangSpaceApp-F2b-O2-RaceSafe-20261115'
$Branch = 'fix/yangspace-f2b-o2-race-safe-20261115'
$RunCommit = 'e1944650f48f1ec9acc3161df0527a57c70defe9'

Set-Location $Repo

git fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'FETCH_FAILED' }

$VerifierCommit = (git rev-parse "origin/$Branch").Trim()
if ($VerifierCommit -notmatch '^[0-9a-f]{40}$') {
    throw 'VERIFIER_COMMIT_INVALID'
}
if ((git rev-parse "origin/$Branch").Trim() -cne $VerifierCommit) {
    throw 'REMOTE_VERIFIER_HEAD_MISMATCH'
}

git switch --detach $VerifierCommit
if ($LASTEXITCODE -ne 0) { throw 'CHECKOUT_FAILED' }
if ((git rev-parse HEAD).Trim() -cne $VerifierCommit) {
    throw 'LOCAL_VERIFIER_HEAD_MISMATCH'
}

if (@(git status --porcelain --untracked-files=all).Count -ne 0) {
    throw 'WORKTREE_NOT_CLEAN'
}

$Evidence = Join-Path $Repo 'logs\yangspace\f2b-o2\e1944650f48f-20261115-220828-c3fc043e'

.\scripts\Verify-F2b-O2-Postflight-v2.ps1 `
    -CommitSha $RunCommit `
    -EvidenceDirectory $Evidence
```

Expected final markers:

- `POSTFLIGHT_APPROVED_FOLLOWUP_CHANGESET=PASS`
- `F2B_O2_POSTFLIGHT=PASS`

Do not rerun `Test-F2b-O2-Local.ps1`; the exact runner and both provider migrations have already passed.

## Release boundary and next work

O2 acceptance does not authorize CT164, production migration, public registration or SMTP dispatch.

Next:
1. Complete Outbox O3 worker/lease/retry/dead-letter/fake transport in [Issue #36](https://github.com/jvalkovv/YangSpaceApp/issues/36).
2. Then execute Production-equivalent PostgreSQL Identity HTTP and abuse QA in [Issue #33](https://github.com/jvalkovv/YangSpaceApp/issues/33).
3. Keep [master register #32](https://github.com/jvalkovv/YangSpaceApp/issues/32) M1c open until the full HTTP/email acceptance is independently proven.

PR #41 remains OPEN/DRAFT pending scoped code review and merge decision. PublicAccounts stays OFF; SMTP stays disconnected; CT164 and production DB remain untouched.
