# YangSpace — Production CMS security/persistence contract

Status: **SOURCE GATES IMPLEMENTED / RUNTIME UNVERIFIED / NO PRODUCTION GO**
Target: one active YangSpace LXC on `yangspace.gndstore.eu`. No additional permanent QA or staging environments.

## The architecture boundary

Public directory GET endpoints remain available independently of the CMS. `Admin:Enabled` defaults to false, so normal Production requests never map login or write endpoints. **Do not set it true on the live service until the following separate gates have passed.**

Production CMS is distinct from disposable Preview: `Preview:Enabled=false`, `Preview:AllowRegistration=false`, `ASPNETCORE_ENVIRONMENT=Production`, `Admin:Enabled=true`, `AllowedHosts=yangspace.gndstore.eu`. The application requires:

- Identity **bearer** PlatformAdmin (no cookies for CMS authorization); Identity API production surface restricted to `POST /api/auth/login` and `POST /api/auth/refresh`. Public registration and manage endpoints return 404, cookie login query parameters are rejected.
- Actual TCP `RemoteIpAddress` loopback plus constant-time comparison of `X-YangSpace-Internal-Gateway` against a 32-byte random secret (Base64 in protected `Admin__GatewaySecret` process environment). This internal value **must never reach browser JavaScript**, logs or Git; never use client-supplied `CF-Connecting-IP` as proof of origin.
- Nginx/edge must independently authenticate the operator with a configured, auditable access policy that remains effective for direct-origin IP access. It must **strip incoming** gateway headers and inject the secret **only after successful authentication** for `/api/auth/*` and `/api/v1/admin/*`. Public site access alone must not cause injection. Protect `/admin` UI as well. Do not mistake Cloudflare proxied DNS for proof of secure origin-side authorization.

The gateway header is intentionally defense-in-depth, **not a substitute** for Nginx authentication, TLS, rate limits, role controls, password policy or monitoring. Review reverse-proxy behavior with direct-origin negative probes before enabling CMS.

## Persistence and operator enrollment

- Production uses an external **existing** SQLite file with a fully qualified path and `Mode=ReadWrite`, not `ReadWriteCreate` or in-memory. File must be outside the immutable code/release tree and protected by filesystem permissions. Example only: `Data Source=/var/lib/yangspace/yangspace.db;Mode=ReadWrite`.
- **Reviewed EF migrations are mandatory.** At present, the repository has **no migrations**. The app intentionally rejects production CMS startup until there is a migration in the compiled assembly, no unapplied migrations, and a confirmed PlatformAdmin user. It never creates/migrates the production DB at service startup.
- Preview uses `EnsureCreated` and fictional seeds; never change Preview mode to Production on that database. Migrate/export any required real content through a separate reviewed process, with database backup and explicit rollback.
- After reviewed migrations have been generated and applied offline to the fresh approved DB, first admin can be enrolled with an **interactive terminal**, the exact vetted application binary, Production config, `YANGSPACE_OFFLINE_ADMIN_PROVISION=CONFIRMED` and `--yangspace-provision-admin`. Enrollment does not run the HTTP listener, asks for email and hidden password with confirmation, refuses an existing user or pre-existing PlatformAdmin, and uses a single DB transaction. The consent variable **must not** appear in the active service unit.

Suggested operator-only sequence, *not an instruction to execute before PVE release approval*:

```text
1. Approve the exact GitHub SHA, create reviewed EF migration code, and apply it OFFLINE to a new DB.
2. Configure the external absolute SQLite path with Mode=ReadWrite; ensure the DB is backed up.
3. Audit the reverse proxy, TLS/Access policy and protected 32-byte gateway secret.
4. Start the compiled binary in a TTY with --yangspace-provision-admin and explicit consent.
5. Run the negative startup script on its own disposable non-existent SQLite path.
6. Confirm real bearer-only admin login and nonadmin/direct-origin denials on the protected host.
7. Deploy one immutable release manually and validate DB, observability and rollback.
```

The service fails closed with `Admin:Enabled=true` if secret, host, DB or migration/admin setup is missing; it does not silently downgrade to an unprotected Preview server.

## Acceptance and NO-GO

Before any release: reviewed EF migration/DB create/upgrades and restore drill; historical credential rotation decision (#2); private origin access and TLS (#3); release approval/ACL (#5); real CMS lifecycle tests (#6/#7); negative startup tests. Log command exit status and exact SHA under `logs/yangspace/`; do not record passwords, bearer tokens, gateway secrets or database contents. No GitHub Actions, merge or PVE mutation in this source change.

Reference: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0 and https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/

## Final source boundary clarification

The external existing SQLite file and reviewed schema gate apply to **all ASP.NET Production launches**, including the public directory when `Admin:Enabled=false`; it cannot create an empty live database simply because the CMS is disabled. The extra confirmed-PlatformAdmin requirement applies only when the CMS is enabled. Schema validation additionally rejects drift between the compiled EF model and the reviewed migration snapshot (`HasPendingModelChanges`). The offline enrollment rechecks prior admin membership inside its transaction to reduce concurrent-operator races. These are **source contracts**, not proof of an executed migration or valid live production configuration.

## 2026-11-10 — EF Core 10 design-time generator, separate from DB apply

Design-time EF tooling now has a locked local `dotnet-ef 10.0.0` manifest and an API `Microsoft.EntityFrameworkCore.Design 10.0.0` package reference. The explicit factory requires `YANGSPACE_EF_DESIGNTIME=CONFIRMED` and uses an **in-memory SQLite** connection for model scaffolding, bypassing runtime host/Production guards. Do not use `dotnet ef database update` or `--connection` to generate the initial migration. The Windows `New-InitialMigration-Local.ps1 -CommitSha <FULL_HEAD>` script only generates `apps/api/Migrations/*` and a SQL review file under ignored `logs/yangspace/migration-scaffold`; no real database is opened. Generated code and SQL must be independently reviewed, then committed to the Draft PR and retested; application startup does not apply migration. Until the scaffold is actually executed and the migration C# + snapshot reviewed/committed, Production remains deliberately blocked. Source-only work is not a migration delivery acceptance. Single active LXC remains the future deployment model.
