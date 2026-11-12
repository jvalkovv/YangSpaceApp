# F2a-1 — source foundation implementation checkpoint

**Status: DRAFT / SOURCE ONLY.** This branch is not approved for merge or runtime deployment.

**Baseline:** ADR-019 PostgreSQL target (accepted direction), current V2 `fb3e032944f0afafed3922c87b0e53dfb99c3220`. [Issue #4](https://github.com/jvalkovv/YangSpaceApp/issues/4), [Issue #11](https://github.com/jvalkovv/YangSpaceApp/issues/11).

## Included source

- EF entity types for Organization, OrganizationMember, BusinessClaim and ContentAuditEvent. No new HTTP routes, signups, privileges or writes.
- Nullable `BusinessLocation.OrganizationId`. Existing unclaimed rows must remain NULL on real migration, never infer an owner from admin or public contact data.
- Relationships with restrictive delete behavior; identity-based membership; unique organization/user membership and per-subject/revision event index.
- `TenantScope.CanManageLocationAsync`: fail-closed lookup of live unrevoked membership in an active organization, **not** a globally-privileged admin fallback. It is internal and not mapped to an endpoint.
- Future revision fields on new records are EF concurrency tokens; **no audit/transactional write service exists yet**, and existing location/content writes are NOT revision-guarded.

## Required blockers before any merge

1. **Provider wiring:** ADR-019 selected PostgreSQL production; current active `Program.cs` and EF design-time factory remain SQLite. Create separately reviewed and pinned Npgsql EF10 integration with provider-safe migrations, explicitly isolated migration histories. Do not claim SQLite migrations are PostgreSQL DDL.
2. **EF-generated migrations:** produce provider-specific snapshot/schema and reviewed SQL using exact-SHA tool; do not hand-author EF migration or snapshot. Must apply fresh and as upgrade to synthetic existing rows in isolated databases, with legacy OrganizationId NULL, FK/unique constraints and restore rehearsal.
3. **Tests:** real EF fresh/upgrade, audit insert/rollback constraints, owner A/B/member/revoke/unknown/deleted claim/suspended org (with real Identity bearer for API gates), and repeatable cross-tenant and unauthorized read/write denial. No merchant authorization enabled by this PR.
4. **Claims policy:** approved reviewer roles, evidence policy, audit retention and GDPR handling still open. Before F2a-2 grant/claim endpoints, explicitly decide and test all.
5. **Operations:** demo CT164/CT152 stay untouched; no automatic startup migration, no live DB copy/convert, no new PostgreSQL service, no GitHub Actions.

**Note:** This candidate will cause `HasPendingModelChanges()` until reviewed migrations are generated. Any test failures from schema/model drift are a real HOLD and may not be suppressed by disabling the production schema gate. No live/demo deployment of this source.

## Acceptance matrix for follow-up test slice

| Test | Target result |
| --- | --- |
| Anonymous principal / missing subject / unclaimed location | Permission deny |
| Member of other organization or revoked member | Permission deny |
| Suspended org; forged organization ID in request | Permission deny |
| Active organization, active Owner/Editor membership | Permission allowed *only for its scoped resource* |
| Old location/offer/brochure/job data migration | Counts/data preserved; nullable ownership unknown |
| FK and deletion attempts | No orphans; restrictive behavior, no cascade of audit |
| Concurrent revisions | Exactly one successful update, one 409, one event — **future F2b** |
| Audit persistence failure | Business state rollback — **future F2b** |
| Public API | No new private actor/member/claim DTO exposure |

## Isolated Windows source contract test (new code, not yet executed)

`scripts/Test-F2a-Tenant-Scope-Local.ps1 -CommitSha <exact-remote-head>`
requires a clean exact-SHA checked-out worktree and matches the allowlisted
remote branch tip. The script runs `dotnet restore`, `dotnet build`, then
`dotnet run ... -- --tenant-scope-only`; checks all positive/negative
`F2A_...` markers and saves the command logs under
`logs/yangspace/f2a-tenant-scope/<sha>-<run-id>/`. The mode invokes
**only** `TenantScopeContract` on a fresh SQLite `:memory:` database and
returns before the old migration harness. **Do not use this as a substitute
for PostgreSQL schema, migration, real bearer API or production security QA.**

## PostgreSQL provider and migration-assembly boundary (source candidate)

- The EF `DirectoryDb` and entity types now live in the provider-neutral `YangSpace.Persistence` project. Their existing `YangSpace.Api` namespace is intentionally retained to avoid a broad endpoint contract rename in this migration-focused refactor.
- Existing SQLite migration history remains in `YangSpace.Api`; all SQLite runtime/design-time/test contexts explicitly select `MigrationsAssembly("YangSpace.Api")`.
- PostgreSQL migration history is isolated in `YangSpace.PostgreSql.Migrations`, a separate EF migrations project with its own opt-in design-time factory. The API references the migration assembly at runtime and selects it only when `Database:Provider=PostgreSQL`.
- `Database:Provider` is explicit and defaults to `SQLite` in checked-in appsettings, preserving the current demo. Preview mode refuses PostgreSQL.
- PostgreSQL production connection policy requires `SslMode=VerifyFull`, explicit host/database/user, and no detailed server error payloads. The read-only schema gate checks provider-specific migration state/model snapshot, non-elevated runtime role without schema CREATE, validated foreign keys and valid indexes.
- No PostgreSQL initial migration is hand-authored. `scripts/New-PostgreSqlInitialMigration-Local.ps1` scaffolds the initial migration with EF CLI into the separate project, emits review SQL and SHA receipts, and explicitly does **not** connect/apply/deploy.
- Microsoft EF Core recommends a separate data project plus provider-specific migrations project for multiple migration sets; official guide: https://learn.microsoft.com/ef/core/managing-schemas/migrations/projects. Npgsql's `VerifyFull` validates both the certificate and host identity: https://www.npgsql.org/doc/api/Npgsql.SslMode.html.

**Not yet proven:** source build after this provider refactor, EF scaffold output, SQL review, disposable PostgreSQL fresh/upgrade/restore tests, and real API bearer authorization. PR remains Draft/HOLD.

## F2a-1 schema corrections after uploaded PostgreSQL SQL audit

**Old generated ZIP:** `YangSpace-F2a1-PostgreSQL-Migration-Review.zip`, SHA-256 `96544589a2a27560a64df8e9da09235d9f81c6716ae151dc92bbc4d915e252c5`. Its three EF artifacts and SQL are internally consistent, but **not approved for commit**: no uniqueness for active claims and missing `BusinessLocation.Revision`. Findings are recorded in PR #17 and Issues #4/#11.

**New source-only changes (new exact-SHA test required):**
- `apps/persistence/Models.cs` now maps a filtered unique `Approved` claim per Location and a filtered unique `Pending` claim per (Location, Organization). Different organizations may compete; resolved applications preserve history and permit resubmission.
- `BusinessLocation.Revision` is now a model-level EF concurrency token with an application-managed revision counter. No API endpoint or HTTP 409 mapping has been introduced.
- `TenantScopeContract` tests positive competing pending applications and new pending after withdrawn, rejects duplicate pending/approved inserts at DB level, and verifies two writers cannot silently overwrite the same Location revision. The isolated Windows runner requires these markers.
- The new code has **NOT** been compiled/runtime-tested yet. Existing accepted `ccd2acb...` restore/tenant QA applied **before** these model changes. Previous PostgreSQL migration scaffold/SQL/ZIP likewise applies only to the old model.

**Next source gate:** create an entirely new clean detached QA worktree at current remote PR #17 SHA; run `Test-F2a-Tenant-Scope-Local.ps1`. The old `D:\GitHub\YangSpaceApp-F2a1-PG-QA` worktree contains uncommitted EF-generated artifacts; do **not** force checkout/reset or overwrite them.

**Next migration gate after source PASS:** run `New-PostgreSqlInitialMigration-Local.ps1` in the new clean worktree, then independently inspect new three-file EF migration/snapshot + SQL + SHA receipts. Generated files remain uncommitted until reviewed. Test PostgreSQL uniqueness, FK/integrity, initial data, backup and restore on a **disposable** PostgreSQL database. CT164 active SQLite remains untouched.


## 2026-11-12 — isolated PostgreSQL execution gate (source candidate)

**Operator environment capability PASS (Windows, 2026-11-12 06:03 Sofia):** `PG_DOCKER_CLI=FOUND`, `PG_DOCKER_ENGINE=AVAILABLE`, Linux Docker Server `29.9.1`, local named-pipe context PASS. This is **only an engine capability check**; no actual database schema apply has run.

Source-only QA assets in PR #17:
- `scripts/Test-F2a-PostgreSql-Disposable-Local.ps1` — exact remote tip SHA and clean worktree, local Docker Linux Engine, reviewed SQL SHA-256 match, ephemeral PostgreSQL 17 container with `--network none`, `--rm`, tmpfs data, no published ports/host binds; command logs, forced cleanup
- `scripts/postgresql/F2a-Disposable-Constraints.sql` — synthetic Identity/org/location/claim inserts; FK/partial UNIQUE negative tests, reapplication, sequential revision CAS, DB-level audit rollback
- `scripts/postgresql/F2a-Disposable-Restore-Verify.sql` — custom-format `pg_dump` and restore to second disposable DB; row counts, EF migration history, FK and partial index checks

The image `postgres:17` must already be explicitly acquired (`docker pull postgres:17`) before invoking the runner. Its image ID is recorded, but tag immutability is **not** asserted. The runner cannot reach CT164 or production databases; it uses synthetic records only.

**Gate status: SOURCE IMPLEMENTED, NOT QA EXECUTED.** Require exact-source PowerShell parse, source restore/build/tenant regression and actual disposable PostgreSQL run with `F2A_PG_DISPOSABLE_RUNTIME=PASS`, evidence/cleanup review. This is NOT concurrent EF clients, real bearer authentication, production app-role permissions, GDPR/audit-service enforcement, off-host disaster recovery, or deployment. PR #17 remains Draft/HOLD; parent PR #1 Draft/HOLD. Merged PR #18 already requires meaningful progress reports and 100%-scoped PR closure.

