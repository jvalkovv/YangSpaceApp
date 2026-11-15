# F2b O2 — Postflight recovery handoff (2026-11-15)

## Accepted runner evidence

The complete local O2 runner passed at exact source commit:

`e1944650f48f1ec9acc3161df0527a57c70defe9`

Evidence directory:

`logs/yangspace/f2b-o2/e1944650f48f-20261115-220828-c3fc043e`

Manifest SHA-256:

`cdb36392e06cba0ee8b2f42e399d3a75562bc57ead88412e50260b41c06af03f`

Observed runner markers:

- `F2B_O2_SQLITE=PASS`
- `F2B_O2_POSTGRESQL_17=PASS`
- `F2B_O2_INTEGRITY=PASS`
- `F2B_O2_CONCURRENCY=PASS`
- `F2B_O2_POSTFLIGHT=READY`

CT164 and production databases were not accessed. SMTP remains disconnected, PublicAccounts remains OFF, and the dispatcher remains fail-closed.

## Why the first postflight stopped

The original read-only verifier completed the evidence hashes, including PostgreSQL SQL, public root CA, Linux runtime DLL and LF/UTF-8-no-BOM bootstrap. It then invoked `docker inspect` for the exact ephemeral PostgreSQL container.

Docker correctly returned `no such object`: the runner had already removed the container. Windows PowerShell 5.1 combined with `$ErrorActionPreference = 'Stop'` promoted this expected native stderr into a terminating error. This is a verifier error-handling defect, not an O2 runner or migration failure.

## Corrected independent verifier

Use `scripts/Verify-F2b-O2-Postflight-v2.ps1` with the original runner SHA and evidence directory.

The v2 verifier:
- Pins its own HEAD to the fetched remote branch HEAD.
- Requires the original runner SHA to be an ancestor of the verifier SHA.
- Allows only this recovery document and the v2 verifier as intervening changes.
- Keeps the original `Verify-F2b-O2-Postflight.ps1` and this run's original runbook unchanged; both are included in the original runner SHA-256 manifest.
- Uses successful local Docker container/network listings and exact name matching instead of treating a failing `docker inspect` as proof of absence.
- Treats Docker CLI/list failures as hard failures, so an unavailable daemon cannot be mistaken for successful cleanup.

## Operator command

Run in the existing worktree only; do not create another QA clone or rerun the O2 migration suite:

```powershell
$ErrorActionPreference = 'Stop'

$Repo = 'D:\GitHub\YangSpaceApp-F2b-O2-RaceSafe-20261115'
$Branch = 'fix/yangspace-f2b-o2-race-safe-20261115'
$RunCommit = 'e1944650f48f1ec9acc3161df0527a57c70defe9'

Set-Location $Repo
git fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'FETCH_FAILED' }

$VerifierCommit = (git rev-parse "origin/$Branch").Trim()
if ($VerifierCommit -notmatch '^[0-9a-f]{40}$') { throw 'VERIFIER_COMMIT_INVALID' }
if ((git rev-parse "origin/$Branch").Trim() -cne $VerifierCommit) {
    throw 'REMOTE_VERIFIER_HEAD_MISMATCH'
}

git switch --detach $VerifierCommit
if ($LASTEXITCODE -ne 0) { throw 'CHECKOUT_FAILED' }
if ((git rev-parse HEAD).Trim() -cne $VerifierCommit) { throw 'LOCAL_VERIFIER_HEAD_MISMATCH' }

if (@(git status --porcelain --untracked-files=all).Count -ne 0) {
    throw 'WORKTREE_NOT_CLEAN'
}

$Evidence = Join-Path $Repo 'logs\yangspace\f2b-o2\e1944650f48f-20261115-220828-c3fc043e'

.\scripts\Verify-F2b-O2-Postflight-v2.ps1 `
    -CommitSha $RunCommit `
    -EvidenceDirectory $Evidence
```

Expected final marker: `F2B_O2_POSTFLIGHT=PASS`.

The migration suite is not rerun because no tested application, migration, runtime runner or evidence artifact changed. The original source commit remains independently pinned and hash-verified.

## Release boundary

Even after O2 postflight PASS, release remains `HOLD_CT164_PENDING`. O2 PASS authorizes only the next separately gated CT164 preview/runtime validation; it does not authorize public registration, SMTP dispatch or production migration.
) { throw 'VERIFIER_COMMIT_INVALID' }

Set-Location $Repo
git fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'FETCH_FAILED' }

if ((git rev-parse "origin/$Branch").Trim() -cne $VerifierCommit) {
    throw 'REMOTE_VERIFIER_HEAD_MISMATCH'
}

git switch --detach $VerifierCommit
if ($LASTEXITCODE -ne 0) { throw 'CHECKOUT_FAILED' }

if (@(git status --porcelain --untracked-files=all).Count -ne 0) {
    throw 'WORKTREE_NOT_CLEAN'
}

$Evidence = Join-Path $Repo 'logs\yangspace\f2b-o2\e1944650f48f-20261115-220828-c3fc043e'

.\scripts\Verify-F2b-O2-Postflight-v2.ps1 `
    -CommitSha $RunCommit `
    -EvidenceDirectory $Evidence
```

Expected final marker: `F2B_O2_POSTFLIGHT=PASS`.

The migration suite is not rerun because no tested application, migration, runtime runner or evidence artifact changed. The original source commit remains independently pinned and hash-verified.

## Release boundary

Even after O2 postflight PASS, release remains `HOLD_CT164_PENDING`. O2 PASS authorizes only the next separately gated CT164 preview/runtime validation; it does not authorize public registration, SMTP dispatch or production migration.
