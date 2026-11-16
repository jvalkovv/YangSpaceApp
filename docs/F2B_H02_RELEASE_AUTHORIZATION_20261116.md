# F2b H02 — signed public Identity release authorization

Status: H02 source candidate committed on the implementation branch; the exact SHA is recorded in Draft PR #46. Runtime acceptance is pending. This document is not a production approval and contains no signing key or live authorization.

## Trust model

O3 acceptance means the worker implementation exists and passed its bounded QA. It does not authorize public Identity. Public activation requires both existing switches, Production environment, PostgreSQL, canonical HTTPS origin/AllowedHosts, valid SMTP configuration and a signed release authorization.

The authorization uses ECDSA P-256 / SHA-256 and is bound to:
- scope: public-identity-email-v1
- environment: Production
- audience: https://yangspace.gndstore.eu
- exact deployed source commit SHA
- approval reference and trusted key ID
- issued-at and expiry

Protected deployment configuration:
- PublicAccounts:ReleaseAuthorization:Payload — Base64Url UTF-8 JSON.
- PublicAccounts:ReleaseAuthorization:Signature — Base64Url DER ECDSA signature over decoded payload bytes.
- PublicAccounts:ReleaseAuthorization:TrustedKeyId — active key identifier.
- PublicAccounts:ReleaseAuthorization:TrustedPublicKeyPem — public key only.

The protected deployment metadata variable YANGSPACE_RELEASE_COMMIT_SHA must contain the exact deployed 40-character lowercase commit SHA. The private signing key stays outside the repository, application configuration, application container, test evidence and logs.

## Signed payload

The strict JSON schema contains exactly these fields:

```json
{
  "version": 1,
  "keyId": "yangspace-release-2026",
  "scope": "public-identity-email-v1",
  "environment": "Production",
  "audience": "https://yangspace.gndstore.eu",
  "sourceCommit": "<40-character-lowercase-git-sha>",
  "approvalId": "<reviewed-issue-or-release-approval-reference>",
  "issuedAtUtc": "2026-11-16T14:00:00+00:00",
  "expiresAtUtc": "2026-11-16T15:00:00+00:00"
}
```

The verifier allows at most five minutes of future clock skew, requires issue time within the previous 24 hours, requires UTC timestamps and limits authorization lifetime to 24 hours. Unknown JSON properties are rejected. The configured trusted key ID must match the signed payload.

## Fail-closed behavior

The same verifier is used at API startup and hosted outbox-worker activation. Missing/malformed payloads, invalid key IDs/public keys/signatures, wrong scope/audience/environment/commit and invalid time windows are refused. Only stable non-secret failure markers are emitted; payloads, signatures, keys and tokens are never logged.

Both public feature switches remain false in source-controlled defaults. This implementation does not generate any production authorization and is not a production approval. No live SMTP, CT164, Production DB or public release is authorized.

## Source implementation candidate

The current candidate is the HEAD of implementation branch `feat/yangspace-f2b-h02-release-auth-http-20261116`, tracked by Draft PR #46 and stacked on the accepted H01 source. PR #46 is the source of truth for the exact commit SHA.

- API startup and hosted worker activation use the same signed authorization verifier.
- The previous `allowSyntheticDispatch` worker-constructor bypass has been removed. O3 SQLite and PostgreSQL worker contracts now use ephemeral signed P-256 authorizations through the same Production verifier; private keys remain in memory.
- H02 Production TestServer uses the real application `Program`, Production environment, disposable PostgreSQL 17/TLS VerifyFull, a non-elevated Identity role and an in-memory fake transport registered through normal DI.
- Before `WebApplicationFactory.CreateClient`, the harness seeds the disposable connection, PostgreSQL provider, Production switches, SMTP sentinel values and ephemeral signed authorization into process environment. This is required because minimal-host `Program` evaluates fail-closed configuration guards before `ConfigureWebHost` applies its in-memory overrides; the scope restores every prior process value on disposal.
- PostgreSQL persists `IdentityEmailPurpose` and `IdentityEmailOutboxStatus` as `character varying(32)` through EF string converters. The H02 raw SQL QA helpers must therefore bind enum names as VARCHAR and compare returned status names as strings, never as integer enum ordinals.
- Public confirmation now rejects already-confirmed accounts before calling `UserManager.ConfirmEmailAsync`. Identity token validation alone does not enforce single-use confirmation; the API must enforce the account-state transition and retain Identity's concurrency-stamp protection for simultaneous first confirmations.
- The disposable QA scope guard is verified through the migrator principal only. The Identity runtime principal is deliberately denied access to this QA-only table; its least-privilege contract verifies that denial rather than widening runtime grants.
- HTTP coverage includes registration/outbox transaction rollback, confirmation/replay/expiry, resend retry release, bearer login/session, forgot/reset/expiry, staff-role denial and generic unknown-email responses.
- The exact-SHA runner includes O3 regression, enqueue regression, H01/H02 Production TestServer, evidence hashing, cleanup and Independent Postflight.

## QA status

**Runtime acceptance is NOT complete.** At exact SHA `6108367b1fc9df2188a1cbb16ff9fb3612252658`, the full runner passed O3, PostgreSQL worker, H01 Production TestServer, H02 startup, registration and atomic Identity+Outbox persistence, then failed the sequential confirmation replay assertion. Root cause: the endpoint called `UserManager.ConfirmEmailAsync` for an already-confirmed account; token validation alone does not enforce single-use confirmation. The endpoint now checks `EmailConfirmed == false` before invoking Identity. This correction is committed at the next exact source SHA. The full exact-SHA runner and Independent Postflight must be rerun there; no H02 HTTP acceptance is claimed.

The operator run must use the exact SHA from PR #46 and produce a fresh evidence directory and manifest. Evidence from earlier commits is not reusable.
