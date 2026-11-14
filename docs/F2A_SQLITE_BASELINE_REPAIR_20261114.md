# F2a-S SQLite migration baseline repair (Issue #38)

**Source:** F2a parent `1f1307250eed0d2e5df24570eb560e5ea16e20ee`.
**Problem:** SQLite provider's latest snapshot is older than F2a organization/claim domain model. F2b O2 outbox scaffold accidentally produces a combined migration including four missing entities, `Locations` field additions and EF's table rebuild. PostgreSQL outbox migration delta is clean. This is migration-history drift, not merely a SQL validator bug.

## Safety and procedure

- The only intended change for **F2a-S1** is a separately reviewed SQLite migration `F2aOrganizationClaimFoundation`, before any outbox migration. Use the current F2a model *without* an outbox DbSet, by running this new branch based on the F2a parent.
- `scripts/New-F2a-Sqlite-Baseline-Offline.ps1` is an **offline scaffold**: clean detached exact SHA, design-time SQLite in-memory connection factory, pinned dotnet-ef 10, .NET Release, source-only generated C# and SQL. It should produce exactly two new migrations C# files and one modified SQLite snapshot (no PostgreSQL migration changes).
- SQL allowlist expects exactly four new domain tables plus `ef_temp_Locations` used by the SQLite table-rebuild mechanism. No outbox. The script does **not** approve `DROP TABLE "Locations"` as safe: it requires the copy/rename pattern to be available for review, but safety is only established by a **disposable historic SQLite upgrade and backup/restore**.
- Before accepting F2a-S2 verify rows, keys, all previous Location fields/foreign keys, offers/brochures/jobs, Identity users/roles, EF history, pending claims, audit constraints and per-tenant isolation. Also test interruption/recovery and no data loss using disposable synthetic data; never apply to CT164.
- Only after standalone F2a migration + disposable upgrade/restore **PASS**, merge this scoped PR into F2a parent #17. New parent SHA requires updated/regression QA. Reconcile F2b #29 and outbox #37 *after* accepted F2a migration and rerun clean O2 scaffolding for exactly `IdentityEmailOutbox`.
- Generator source is NOT tested on operator Windows. All PRs stay Draft/HOLD. No GitHub Actions or automatic deployment.

**Evidence from original failed F2b O2:** SQLite SQL SHA256 `51c5e002aab53e8f4713d247304f727e012c8ee9271e0dd3a065eab3375a027d`; PostgreSQL outbox SQL SHA256 `3feb831210a87a64f1bacff9ffbb53dc17c20b464fc8ec8b380ac963759b2c50`. Original source `f84da94bb7c5a6334a79c607fb4543227e2be69d`. Preserve unsuccessful O2 worktree; do not copy its generated migrations into the new baseline PR.
