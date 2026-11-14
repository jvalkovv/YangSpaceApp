# F2a-2 — bearer-only merchant read QA slice

**State:** exact-SHA operator acceptance PASS; not yet merged at the time of this document update. The tested code commit is `f579c70db9cc0986d2f1df8047158587f0e87eef`, stacked on F2a-1 SHA `1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001`. See Issue #4 for the broader security contract.

## Scope

- A new `MerchantRead:Enabled` flag defaults to false. Enabling it is allowed ONLY in the explicit `YangSpaceQa` environment with Preview/Admin/Registration disabled and SQLite persistence. No Production or ordinary Development activation.
- The isolated HTTP mode exposes only Identity `/api/auth/login` and `/api/auth/refresh` with cookie query parameters denied; registration, recovery and account endpoints return 404.
- `GET /api/v1/merchant/locations/{id}` requires the ASP.NET Core Identity bearer scheme. One server-side joined EF query checks live active organization and unrevoked Owner/Editor membership. The API returns 401 without a valid bearer, generic 404 for foreign/nonexistent/legacy/revoked/suspended, 200 only for own location.
- Only `id, slug, name, isPublished, revision` are returned. No raw EF entities, contacts, actor IDs, claim evidence, audit data, ownership changes or write routes. PlatformAdmin is not a merchant member automatically.
- The existing staff admin, preview and public directory behavior is not intentionally modified.

## Test gate

Run `scripts/Test-F2a-Merchant-Bearer-Local.ps1 -CommitSha <exact child-branch SHA>` from a clean detached Windows worktree with .NET 10. It uses `Microsoft.AspNetCore.Mvc.Testing` (test-only) to issue **real HTTP requests** to the actual API entry point and retrieve real Identity-issued bearer tokens. SQLite model is created in a fresh temp file using `EnsureCreated` by the TEST harness only, never by production startup; all users, organizations and membership are synthetic.

Required negatives: default-off login/merchant endpoint, environments outside QA fail closed, invalid/anonymous/cookie bearer 401, cross-tenant 404, legacy and unknown 404, forged actor header 404, PlatformAdmin without membership 404, registration refused, cookie login refused, write route not mapped, no sensitive DTO properties, same still-valid bearer denied immediately on membership revocation or organization suspension, no business writes or audit rows from reads. All expected markers are checked by the exact-SHA Windows runner and retained per-stage in logs.

**Out of scope / HOLD:** bearer HTTP on real PostgreSQL, production role and TLS ingress, reviewer/approval workflow, grant/revoke transaction and true application audit, concurrent EF writers, HTTP 409 edits, CT164 or CT152 activation, real registration, off-host recovery. Passing this test is not F2a/F2b or pilot completion.

ASP.NET Core integration testing reference: https://learn.microsoft.com/en-us/aspnet/core/testing/integration-testing


## 2026-11-12 08:09 Europe/Sofia — exact-SHA operator acceptance and evidence postflight

**Verdict: F2a-2 isolated bearer read gate PASS.** This is not a production, PostgreSQL, or pilot acceptance.

### Exact source and execution

- Tested source SHA: \`f579c70db9cc0986d2f1df8047158587f0e87eef\`.
- PR #19 base / F2a-1 source SHA: \`1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001\`.
- Operator runner: \`scripts/Test-F2a-Merchant-Bearer-Local.ps1 -CommitSha f579c70db9cc0986d2f1df8047158587f0e87eef\`.
- Operator output: syntax PASS; restore PASS; Release build with warnings-as-errors PASS; tenant regression PASS; real Identity bearer HTTP TestServer PASS; final \`F2A2_MERCHANT_BEARER_HTTP=PASS\`.
- The runner checks the exact SHA, remote branch identity and clean worktree, then validates every expected marker before writing its manifest.

### Read-only evidence postflight

**Result: \`EVIDENCE_POSTFLIGHT=PASS\`** on the operator workstation.

Evidence directory:
\`D:\GitHub\YangSpaceApp-F2a2-Bearer-QA\logs\yangspace\f2a2-merchant-http\f579c70db9cc-20261112-075949-ea648860\`

- \`manifest.txt\` SHA-256: \`336D8A9DCB4C69483981A2A5124A5123E8AD9044C14A1619199439E2E121F366\`.
- HTTP stdout SHA-256: \`D08C82D76C8C076907B80F1D8972975A718809E1E04720E30276AED217F73A13\`.
- Tenant regression stdout SHA-256: \`730F84F19155530A13C30E757B1C5216F29AE5A66EA0DB1A86FEA30B2FCB1169\`.
- Build stdout SHA-256: \`947A9587242975BB7872C59C17707CA56E133D056CF3F49F8ACE817A79560F4C\`.
- Restore stdout SHA-256: \`2AD9967F2280D0195AD457A92FE6A353A8492625846632386DF6DFC446EFE4BC\`.
- All four stage groups contain \`runner.cmd\`, \`stdout.log\` and \`stderr.log\`; all expected markers are present; no \`gate.txt\` exists.
- The postflight also recomputed SHA-256 for every evidence file. Empty stderr files are expected for successful stages.

### Accepted test scope

Disposable SQLite + ASP.NET Core TestServer only, using actual Identity login-issued bearer tokens. The runner and HTTP contract cover default-off behavior, QA-only environment guards, anonymous/invalid/cookie-only 401, Owner/Editor access, cross-tenant and unknown/unclaimed/suspended/revoked 404, same-token revocation, forged actor denial, PlatformAdmin without membership, registration/cookie login denial, no mutation route, strict DTO allowlist and no business/audit side effects.

### Remaining HOLD — explicitly not tested by this slice

- Real PostgreSQL bearer HTTP and parallel EF writers.
- Reviewer-only claim approval and membership grant/revoke.
- Atomic application business mutation + audit transaction and DB fault injection.
- Least-privilege production DB role, production ingress/TLS and deployment.
- Off-host backup/restore, recovery rehearsal and pilot sign-off.

No CT164/CT152 access, live database, migration, feature activation or deployment occurred. No GitHub Actions were introduced. This PASS closes only the isolated F2a-2 read-authorization acceptance gate.
