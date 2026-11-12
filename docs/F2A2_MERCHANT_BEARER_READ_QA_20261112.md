# F2a-2 — bearer-only merchant read QA slice

**State:** source-only, NOT BUILD-TESTED or merged. Stacked on F2a-1 SHA 1ec2b7ee23b78f07d3cf2644f1d9e5e30752d001. See Issue #4 for security contract.

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
