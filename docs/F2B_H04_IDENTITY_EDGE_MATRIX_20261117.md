# F2b H04 — Identity HTTP edge-case matrix

Status: **SOURCE IMPLEMENTED / EXACT-SHA RUNTIME QA PENDING**.

## Scope

Application/backend only. This slice extends the real Production `Program` / WebApplicationFactory TestServer contract. It does not change Cloudflare, Nginx, proxy, firewall, DNS, ingress, production deployment or the public application switches.

## Added negative and replay cases

- Invalid email registration returns 400 and creates no Identity row.
- Weak-password registration returns 400 and creates no Identity row.
- Duplicate registration response body remains indistinguishable from the initial accepted registration response.
- Successful confirmation response has `Cache-Control: no-store` and `Referrer-Policy: no-referrer`.
- Malformed confirmation code and unknown user fail closed with the generic failure redirect; malformed confirmation leaves the account unconfirmed.
- A staff fixture carries four distinct roles (`H02Staff`, `PlatformAdmin`, `Moderator`, `SupportAgent`); confirmation, resend, login, forgot and reset remain denied/suppressed for staff.
- A successful password-reset token cannot be replayed to set another password.
- A malformed reset token is rejected and the existing password remains valid.

Existing H02/H03 contracts remain in the same run: Production startup and Identity HTTP, signed release authorization, fake transport, PostgreSQL 17/TLS VerifyFull, least-privilege runtime role, account budgets, lockout, SQLite regression, worker recovery and exact-SHA postflight.

## Required H04 markers

```text
F2B_H04_REGISTER_INVALID_EMAIL_NO_ACCOUNT
F2B_H04_REGISTER_WEAK_PASSWORD_NO_ACCOUNT
F2B_H04_CONFIRMATION_RESPONSE_SECURITY_HEADERS
F2B_H04_CONFIRM_MALFORMED_TOKEN_FAIL_CLOSED
F2B_H04_CONFIRM_INVALID_TOKEN_FAIL_CLOSED
F2B_H04_CONFIRM_UNKNOWN_USER_FAIL_CLOSED
F2B_H04_DUPLICATE_REGISTRATION_BODY_GENERIC
F2B_H04_STAFF_MULTIPLE_ROLES_CREATED
F2B_H04_STAFF_ANY_ROLE_EXCLUDED_FROM_ALL_PUBLIC_ACTIONS
F2B_H04_RESET_TOKEN_REPLAY_REJECTED
F2B_H04_RESET_REPLAY_DOES_NOT_CHANGE_PASSWORD
F2B_H04_RESET_MALFORMED_TOKEN_REJECTED
F2B_H04_RESET_MALFORMED_TOKEN_PRESERVES_PASSWORD
F2B_H04_RESET_INVALID_TOKEN_REJECTED
F2B_H04_RESET_INVALID_TOKEN_PRESERVES_PASSWORD
F2B_H04_IDENTITY_EDGE_QA
F2B_H04_INDEPENDENT_POSTFLIGHT
```

## Operator acceptance

From a clean detached worktree at the exact current branch HEAD:

```powershell
$ErrorActionPreference = 'Stop'
$Repo = 'D:\GitHub\YangSpaceApp'
$Branch = 'feat/yangspace-f2b-h04-identity-edge-matrix-20261117'
$Sha = '<exact 40-character remote HEAD>'

git -C $Repo fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'FETCH_FAILED' }

git -C $Repo rev-parse "origin/$Branch"
git -C $Repo worktree list --porcelain
```

Create a new, non-existing detached QA worktree at the exact SHA, verify clean source and execute:

```powershell
& "$Repo\scripts\Test-F2b-O4-H01-PostgreSql-Local.ps1" -CommitSha $Sha
if ($LASTEXITCODE -ne 0) { throw 'H04_ACCEPTANCE_FAILED' }
```

The runner includes Independent Postflight and verifies the evidence manifest, source/binary hashes, exact remote SHA, no source mutation and disposable PostgreSQL/network/secret cleanup.

## Release boundary

Public registration and outbox dispatch remain OFF in source-controlled defaults. Fake transport only. No real SMTP, CT164, Production DB, production migration/deployment or GitHub Actions. H04 acceptance does not complete H05/H06 or authorize public activation.
