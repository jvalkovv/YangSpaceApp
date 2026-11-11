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
