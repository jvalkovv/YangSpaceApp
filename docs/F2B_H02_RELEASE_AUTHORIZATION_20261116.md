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
- HTTP coverage includes registration/outbox transaction rollback, confirmation/replay/expiry, resend retry release, bearer login/session, forgot/reset/expiry, staff-role denial and generic unknown-email responses.
- The exact-SHA runner includes O3 regression, enqueue regression, H01/H02 Production TestServer, evidence hashing, cleanup and Independent Postflight.

## QA status

**Runtime acceptance is NOT complete.** The Windows exact-SHA runner and Independent Postflight have not yet been executed at the current candidate SHA. No runtime PASS is claimed. PR #46 remains Draft and H02 remains HOLD until every stage and the independent verifier pass on one exact commit.

The operator run must use the exact SHA from PR #46 and produce a fresh evidence directory and manifest. Evidence from earlier commits is not reusable.
