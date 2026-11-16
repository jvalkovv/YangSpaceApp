# F2b H02 — signed public Identity release authorization

Status: source implementation in progress. This document is not a production approval and contains no signing key or live authorization.

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

Both public feature switches remain false in source-controlled defaults. This implementation does not generate any authorization and is not a production approval. No live SMTP, CT164, Production DB or public release is authorized.

## QA

The pure runtime contract generates an ephemeral P-256 keypair and tests valid signature, signature tampering, expiry, future issuance, excessive lifetime, wrong scope, audience, environment, deployment SHA, key ID and invalid public key. This is verifier QA only, not production authorization.

The next H02 runtime stage must use the same verifier in a real Production WebApplicationFactory against disposable PostgreSQL 17/TLS VerifyFull and a fake transport registered through normal DI. It must use a separate non-elevated Identity runtime role; the O3 worker-only role must not be broadened to grant credential reads.
