# ADR-019 — PostgreSQL target for YangSpace verified multi-tenant production

**Status:** Accepted production **direction** from project owner instruction to proceed, 2026-11-11. **Runtime migration/deployment authorization: NOT GRANTED.** Supersedes the unresolved production-provider choice in ADR-007/ADR-013; does not retroactively replace any existing SQLite migration.
**Scope:** YangSpace V2 production data architecture for verified merchants, memberships, claims, audit and high-integrity content updates.

## Decision

1. **PostgreSQL is the target persistence engine for real multi-tenant/pilot data.** Use the maintained `Npgsql.EntityFrameworkCore.PostgreSQL` EF Core **10.x** provider for the .NET 10 application; pin exact compatible packages and test migrations/provider behavior against a dedicated disposable PostgreSQL instance before any release.
2. **SQLite remains the existing, separately managed, read-only fictional CT164 preview and disposable QA engine.** Its existing EF migration history and `PreviewBootstrap.EnsureCreatedAsync` demo are NOT convertible in place to PostgreSQL. The publicly served DB is not a migration source.
3. **No dual-write, automatic startup migration, blind data copy or silent production provider switch.** Provider activation requires a dedicated code and operator plan with isolated PG database, migration generation on PG provider, deterministic backfill/source mapping, downtime/freeze design, backup + restore, reconciliation counts/hashes and explicit approval.
4. The next F2a-1 source slice may model `Organization`, `OrganizationMember`, `BusinessClaim`, nullable location ownership and a forward-compatible audit schema as EF entities, but **must not merge without provider-specific schema proof**; no tenant ownership or claim privileges until F2a-2 authorization and F2b audit gates pass.
5. **Use provider-independent application-managed integer revisions** and transactional, per-field allowlisted audit for eventual concurrency (HTTP 409). PostgreSQL native types/constraints can supplement, not replace, application authorization/transaction tests.
6. **Operational conditions:** authenticated isolated database network, least-privileged roles (migration role distinct from app role), TLS where applicable, secrets outside Git, version-aware backups, monitored disk/WAL growth, restore drills, bounded RPO/RTO with approved thresholds, and correlation/latency/error metrics.

## Rationale and alternatives

- **PostgreSQL:** dedicated concurrent writer engine, transactional integrity, migration/read replica/backup ecosystem, indexing and optional PostGIS expansion; no SQL Server license dependency. Adds operational responsibility to provision, patch, secure, monitor and restore PostgreSQL.
- **SQLite:** low footprint and reliable existing preview/disposable QA, but a single-file, single-writer deployment and provider-specific migration limitations make it a less suitable target for a growing multi-merchant write workload. No claim that SQLite cannot be secure at small scale.
- **SQL Server:** viable EF Core platform but introduces avoidable licensing/deployment operational choices for the current PVE-first project. Revisit only if verified project needs change.

## Unresolved decisions (do not infer approval)

- Hosting topology (same node vs isolated PVE guest), PostgreSQL major version, backup store and restore RPO/RTO thresholds, storage capacity, operational owner and cross-host firewall rules.
- Claim evidence requirements, reviewer delegation, applicant authentication and notification policy.
- Audit retention/GDPR and historical identity handling; initial merchant access stays disabled until separately accepted.

## Mandatory staged release gates

A. Review source and target provider package versions; explicit provider-selection configuration refuses invalid/mixed startup settings; existing SQLite demo remains unchanged.

B. Generate **PostgreSQL-specific EF migration artifacts** from selected baseline in an isolated environment; produce migration SQL; compare to an approved schema contract. **Never reuse SQLite SQL/scripts/snapshot as though they were PostgreSQL DDL.** If maintaining two providers, migrations histories must be isolated.

C. Test fresh schema, old→new migration with synthetic existing rows, FK/uniqueness/isolation, parallel edits/revisions, audit atomic rollback and failure cases on PostgreSQL with real API.

D. Backup, restore into separate scratch DB, reconcile and check PG startup/operational behavior, permission separation, monitoring and rollback, with exact SHA evidence.

E. Only then schedule a separately approved production activation. No `Migrate()`/`EnsureCreated()` on request path or live CT164 preview DB.

**References:** [F2a contract](F2A_TENANT_OWNERSHIP_AND_AUDIT_CONTRACT_20261111.md), [SSOT](SSOT.md), [Issue #4](https://github.com/jvalkovv/YangSpaceApp/issues/4), [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11), [ADR-018](ADR-018-NO-GITHUB-ACTIONS.md).

## External technical verification (2026-11-11)

- Npgsql 10 EF Core release notes: https://www.npgsql.org/efcore/release-notes/10.0.html
- NuGet provider dependency compatibility: https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.0
- EF concurrency and SQLite limitations: https://learn.microsoft.com/en-us/ef/core/saving/concurrency and https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations
