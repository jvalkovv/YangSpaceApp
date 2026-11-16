# F2b H02 — signed public Identity release authorization

Status: H02 runtime acceptance previously passed at `b48129df712800a5f9d3bdb46ac2f918d88b8212`. The scoped review required a stricter one-hour authorization ceiling and real Production startup-negative tests for invalid signature, expiry and wrong deployment SHA. This follow-up is committed on the same Draft PR #46 branch. The current branch HEAD is NOT runtime-accepted until its exact-SHA runner and independent Postflight pass.

## Trust model

O3 acceptance means the worker implementation exists and passed its bounded QA. It does not authorize public Identity. Public activation requires both existing switches, Production environment, PostgreSQL, canonical HTTPS origin/AllowedHosts, valid SMTP configuration and a signed release authorization.

The authorization uses ECDSA P-256 / SHA-256 and is bound to:
- scope: public-identity-email-v1
- environment: Production
- audience: https://yangspace.gndstore.eu
- exact deployed source commit SHA
- approval reference and trusted key ID
- issued-at and expiry, with a maximum lifetime of **one hour**

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

The verifier allows at most five minutes of future clock skew, requires issue time within the previous 24 hours, requires UTC timestamps and rejects any authorization whose expiry is more than **one hour after IssuedAtUtc**. Exactly one hour is valid; one hour plus one tick is refused. Expired and future-dated authorizations are refused. Unknown JSON properties are rejected. The configured trusted key ID must match the signed payload.

## Fail-closed behavior

The same verifier is used at API startup and hosted outbox-worker activation. Missing/malformed payloads, invalid key IDs/public keys/signatures, wrong scope/audience/environment/commit and invalid time windows are refused. Only stable non-secret failure markers are emitted; payloads, signatures, keys and tokens are never logged.

Both public feature switches remain false in source-controlled defaults. This implementation does not generate any production authorization and is not a production approval. No live SMTP, CT164, Production DB or public release is authorized.

## Source implementation

The implementation is tracked by Draft PR #46 on branch `feat/yangspace-f2b-h02-release-auth-http-20261116`.

- API startup and hosted worker activation use the same signed authorization verifier.
- The previous `allowSyntheticDispatch` worker-constructor bypass has been removed. O3 SQLite and PostgreSQL worker contracts use ephemeral signed P-256 authorizations through the same Production verifier; private keys remain in memory.
- H02 Production TestServer uses the real application `Program`, Production environment, disposable PostgreSQL 17/TLS VerifyFull, a non-elevated Identity role and an in-memory fake transport registered through normal DI.
- The real `auth` rate limiter remains enabled at 12 requests/minute per transport peer. TestServer otherwise assigns one loopback peer to every request, so the H02 lifecycle test uses a test-only `IStartupFilter` to inject RFC 5737 documentation IPs into `Connection.RemoteIpAddress` for separate lifecycle phases. It does not trust forwarding headers, change the production limit or disable rate limiting. The test asserts that this peer injection was applied.
- Before `WebApplicationFactory.CreateClient`, the harness seeds the disposable connection, PostgreSQL provider, Production switches, SMTP sentinel values and ephemeral signed authorization into process environment. This is required because minimal-host `Program` evaluates fail-closed configuration guards before `ConfigureWebHost` applies its in-memory overrides; the scope restores every prior process value on disposal.
- PostgreSQL persists `IdentityEmailPurpose` and `IdentityEmailOutboxStatus` as `character varying(32)` through EF string converters. Raw SQL QA helpers bind enum names as VARCHAR and compare returned status names as strings, never as integer enum ordinals.
- Public confirmation rejects already-confirmed accounts before calling `UserManager.ConfirmEmailAsync`; the HTTP contract verifies sequential replay rejection and exactly one winner for two concurrent first-confirmation requests.
- The disposable QA scope guard is verified through the migrator principal only. The Identity runtime principal is deliberately denied access to this QA-only table; its least-privilege contract verifies that denial rather than widening runtime grants.
- HTTP coverage includes registration/outbox transaction rollback, confirmation/replay/expiry, resend retry release, bearer login/session, forgot/reset/expiry, staff-role denial and generic unknown-email responses.
- H01 now executes the real Production startup with ephemeral test keys and verifies that a bad signature, an expired signed authorization and a valid signature bound to a different commit all fail closed with the stable authorization-refused marker.
- The authorization contract verifies an exactly-one-hour lifetime is accepted and one hour plus one tick is refused.

## QA status and operator procedure

The previous H02 runtime run at `b48129df712800a5f9d3bdb46ac2f918d88b8212` passed H01/H02 and both independent Postflights. Its evidence is not reusable for this follow-up because the authorization policy and startup test matrix have changed.

After fetching the updated PR #46 branch and checking out its new exact HEAD, run:

```powershell
$Sha = '<new exact PR #46 HEAD>'
& .\scripts\Test-F2b-O4-H01-PostgreSql-Local.ps1 -CommitSha $Sha
```

The runner must produce the new one-hour boundary markers and H01 Production startup-negative markers, then invoke the independent Postflight. Expected evidence includes:
- `F2B_H02_AUTH_ONE_HOUR_BOUNDARY_ACCEPTED=PASS`
- `F2B_H02_AUTH_OVER_ONE_HOUR_REFUSED=PASS`
- `F2B_O4_H01_REJECTS_BAD_RELEASE_SIGNATURE=PASS`
- `F2B_O4_H01_REJECTS_EXPIRED_RELEASE_AUTHORIZATION=PASS`
- `F2B_O4_H01_REJECTS_WRONG_RELEASE_COMMIT=PASS`
- `F2B_O4_H01_INDEPENDENT_POSTFLIGHT=PASS`
- `F2B_H02_INDEPENDENT_POSTFLIGHT=PASS`

**Release boundary:** Public registration and dispatch remain OFF. No real SMTP, CT164, Production DB, production deployment, public activation or GitHub Actions. H03 trusted-proxy ingress and real multi-peer 429, H04 exhaustive Identity edge cases, H05 remaining operator/recovery acceptance and H06 browser/mobile/privacy/accessibility remain separate gates.
