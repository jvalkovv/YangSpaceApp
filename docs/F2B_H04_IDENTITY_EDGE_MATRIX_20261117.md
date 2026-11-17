# F2b H04 — Identity HTTP edge-case matrix

Status: **SOURCE IMPLEMENTED / EXACT-SHA RUNTIME QA PENDING** for the current follow-up HEAD. Previous H04 runtime evidence applies only to its recorded SHA and is not reusable after these test/runner changes.

## Scope

Application/backend only. This slice extends the real Production `Program` / `WebApplicationFactory` TestServer contract. It does not change production runtime behavior, account switches, Cloudflare, Nginx, proxy, firewall, DNS, ingress or deployment.

## Required application-level matrix

### Registration

- Malformed, empty and oversized (>254 characters) email return the same stable HTTP 400 validation response.
- Oversized fixture is independently verified to pass `EmailAddressAttribute` before the application length guard rejects it.
- Invalid email and weak-password cases create no Identity user and no outbox insertion.
- Duplicate registration remains indistinguishable from a new accepted request.
- A successfully registered public account receives no Identity role assignment and no `OrganizationMember` row.
- Public registration never grants owner, staff or organization membership.

### Email confirmation

- Missing/unknown user, malformed Base64Url, syntactically valid but invalid Identity token and expired token fail closed.
- Negative confirmation does not change `EmailConfirmed`.
- Success is single-use; concurrent confirmation has exactly one winner.
- Confirmation response retains `Cache-Control: no-store` and `Referrer-Policy: no-referrer`.

### Password recovery

- Malformed, syntactically valid but invalid, expired and replayed reset tokens fail closed.
- Successful reset invalidates the old password and the reset token.
- Failed reset preserves the current password.
- Unknown and known email responses remain indistinguishable.

### Staff exclusion

- Any persisted Identity role excludes an account from public login, confirmation, resend, forgot and reset.
- Verify multiple distinct staff roles, not only one synthetic role.
- Public registration creates no Identity role or organization membership.

### Bearer/session and abuse

- Login never emits `Set-Cookie`.
- Session requires a real bearer token; no cookie-only fallback.
- Preserve H03 account budgets and transport 429; do not weaken or duplicate accepted H03 logic.

## Runtime and evidence

- Real Production `Program` through WebApplicationFactory/TestServer.
- Disposable PostgreSQL 17 with TLS VerifyFull and the existing least-privilege Identity role.
- Fake transport only; no SMTP socket or real credentials.
- Exact-SHA Release build with warnings as errors.
- Every H04 case has a stable PASS marker and negative assertion against persisted state.
- Independent Postflight verifies exact source/remote SHA, stage receipts, manifest/log hashes, clean worktree and disposable resource cleanup.
- Registration outbox assertion compares total persisted outbox rows before/after invalid registration requests; worker processing may update/purge existing rows but must not add rows for rejected requests.
- Identity role and `OrganizationMembers` checks use the disposable migrator connection for observation; the application runtime role is not granted extra tenant-table privileges.

## Required H04 markers

```text
F2B_H04_REGISTER_INVALID_EMAIL_NO_ACCOUNT
F2B_H04_REGISTER_EMPTY_EMAIL_NO_ACCOUNT
F2B_H04_OVERSIZED_EMAIL_FIXTURE_VALIDATED
F2B_H04_REGISTER_OVERSIZED_EMAIL_NO_ACCOUNT
F2B_H04_REGISTER_WEAK_PASSWORD_NO_ACCOUNT
F2B_H04_INVALID_REGISTRATIONS_NO_OUTBOX_INSERT
F2B_H04_PUBLIC_REGISTER_NO_IDENTITY_ROLE
F2B_H04_PUBLIC_REGISTER_NO_ORGANIZATION_MEMBERSHIP
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
$Repo = 'D:\\GitHub\\YangSpaceApp'
$Branch = 'feat/yangspace-f2b-h04-identity-edge-matrix-20261117'
$Sha = '<exact 40-character remote HEAD>'

git -C $Repo fetch --no-tags origin $Branch
if ($LASTEXITCODE -ne 0) { throw 'FETCH_FAILED' }

git -C $Repo rev-parse "origin/$Branch"
git -C $Repo worktree list --porcelain
```

Create a new, non-existing detached QA worktree at the exact SHA, verify clean source and execute:

```powershell
& "$Repo\\scripts\\Test-F2b-O4-H01-PostgreSql-Local.ps1" -CommitSha $Sha
if ($LASTEXITCODE -ne 0) { throw 'H04_ACCEPTANCE_FAILED' }
```

The runner includes Independent Postflight and verifies the evidence manifest, source/binary hashes, exact remote SHA, no source mutation and disposable PostgreSQL/network/secret cleanup.

## Release boundary

Public registration and outbox dispatch remain OFF in source-controlled defaults. Fake transport only. No real SMTP, CT164, Production DB, production migration/deployment or GitHub Actions. H04 acceptance does not complete H05/H06 or authorize public activation.
