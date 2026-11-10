# YangSpace — SQLite runtime integrity hardening

**Base migration:** `07dbe4dffbb2f9796a31849364cb979c0bdc70a1`  
**Branch:** `feat/yangspace-v2-foundation-20261108`  
**Scope:** application SQLite connection policy and read-only Production schema gate. No migration source changes.

## Runtime contract

- `SqliteRuntimePolicy.EnableForeignKeys` parses the configured connection string and forces `ForeignKeys=True` before EF Core registers the `DirectoryDb` context. This applies to Preview, Staging and Production runtime contexts.
- `ProductionSqlitePolicy.RequirePreparedDatabase` independently requires `Mode=ReadWrite`, `ForeignKeys=True`, an absolute existing database path and a database outside the immutable release directory.
- `ProductionSchemaGate.ValidateSchemaAsync` checks the actual EF connection's `PRAGMA foreign_keys` value is `1`.
- The same gate runs read-only `PRAGMA foreign_key_check` and refuses startup if existing rows violate declared foreign keys.
- Startup remains non-mutating: no `EnsureCreated`, `Migrate`, migration apply or seed was added to Production startup.

## Disposable regression harness

Project: `apps/api.RuntimeTests/YangSpace.Api.RuntimeTests.csproj`

The harness creates randomly named SQLite files below the OS temporary directory, applies the committed initial migration only to those files, and verifies:

1. Runtime connection normalization overrides a disabled FK setting with `ForeignKeys=True`.
2. Production policy rejects a connection string with FK enforcement disabled.
3. The live SQLite connection reports `PRAGMA foreign_keys = 1`.
4. An orphan child insert is rejected with SQLite foreign-key constraint error 787.
5. Deleting a parent directly in SQL cascades to its child rows. The direct SQL test binds the parent GUID using EF Core SQLite's canonical uppercase TEXT representation and asserts that exactly one parent row was deleted before checking that zero child rows remain.
6. A committed row is checked before disposal and remains after reopening the database connection. All raw-SQL GUID parameters use the same uppercase TEXT representation as EF Core SQLite, preventing false-negative reads.
7. Production schema validation rejects a live connection where FK enforcement is disabled.
8. Production schema validation detects historical orphan rows inserted into a separate disposable database while FK enforcement was disabled.

Run on the existing Windows .NET 10 operator machine after pulling the exact hardening commit:

```powershell
.\scripts\Test-Sqlite-Runtime-Integrity.ps1 -CommitSha <FULL_HARDENING_COMMIT_SHA>
```

The runner requires the exact commit and expected feature branch, a clean worktree, and writes logs/manifests under ignored `logs/yangspace/sqlite-runtime-integrity`.

## Safety boundaries

- No application environment variables or production connection strings are read by the harness.
- No HTTP listener is started.
- No preview database, production database, PVE host, LXC or deployment target is accessed.
- Test databases are deleted after the run.
- This local runtime regression is not a substitute for the later approved single-LXC backup/restore rehearsal and production cutover gate.

## 2026-11-10: persistence-test correction

On exact SHA `05b51aa`, the operator Windows runtime run passed FK enablement, initial migration, orphan rejection, parent delete, SQLite-level cascade and reopened-connection FK pragma, then stopped at `DATABASE_PERSISTENCE_AFTER_REOPEN`. The raw SQL query bound `persistedLocationId.ToString()` as lowercase TEXT, unlike EF Core SQLite's uppercase GUID storage. The fix centralizes SQL GUID formatting, uses typed SQLite TEXT parameters in all direct-SQL GUID calls, and asserts the persisted row before closing and after reopening the connection. No migration or production application code was changed by this correction. **Run the exact new commit before marking runtime acceptance PASS.**
