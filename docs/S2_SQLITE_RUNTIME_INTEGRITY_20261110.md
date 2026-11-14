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

## 2026-11-10: local runtime integrity acceptance

Exact commit `2069eb9a40ba0e594973cc67cd31af60e751d233` was executed on the existing Windows .NET 10 operator machine. The operator log reports:

```text
STAGE_PASS=runtime-test-restore
STAGE_PASS=runtime-test-build
STAGE_PASS=runtime-test-run
SQLITE_RUNTIME_INTEGRITY=PASS
COMMIT_SHA=2069eb9a40ba0e594973cc67cd31af60e751d233
```

Evidence: ignored local directory
`logs/yangspace/sqlite-runtime-integrity/2069eb9a40ba-20261110-075520-ad751c09`.

**Verdict:** local S2-02 runtime integrity acceptance PASS. This does not approve the active database, Linux deployment, TLS, operational backup policy, or production cutover.

## 2026-11-10: operator recovery rehearsal result

The operator ran the exact commit `1ce3fa35421534e48c28959febf15933290c12b6` on Windows .NET 10:

```text
STAGE=runtime-test-restore
STAGE_PASS=runtime-test-restore
STAGE=runtime-test-build
STAGE_PASS=runtime-test-build
STAGE=runtime-test-run
STAGE_PASS=runtime-test-run

SQLITE_RUNTIME_INTEGRITY=PASS
COMMIT_SHA=1ce3fa35421534e48c28959febf15933290c12b6
```

The PowerShell runner only prints the final PASS after validating each required stdout marker, including online backup creation, recovered SQLite integrity, recovered FK check, recovered schema gate, migration history, pre-backup row presence and point-in-time exclusion of the post-backup write. It then writes its local evidence manifest.

Evidence directory (operator machine, ignored and not committed):
`logs/yangspace/sqlite-runtime-integrity/1ce3fa354215-20261110-080557-2dc9f323`.

**Verdict:** disposable local SQLite runtime + backup/restore recovery rehearsal PASS. This is not a PVE/LXC backup destination test and does not establish off-host durability, encryption, retention, access-control, monitoring, or production RPO/RTO.

## 2026-11-10: disposable online backup/restore rehearsal

The same regression program now also tests `Microsoft.Data.Sqlite.SqliteConnection.BackupDatabase` against temporary databases only:

- Create an online snapshot of the migrated disposable DB.
- Assert that a backup artifact exists, then commit a post-backup record in the source.
- Restore the snapshot into a distinct new disposable DB file using the SQLite online backup API.
- Open the recovered file with `Mode=ReadWrite;ForeignKeys=True`.
- Run `PRAGMA integrity_check` and `PRAGMA foreign_key_check`.
- Run Production schema gate and check the applied migration ID.
- Confirm pre-backup data exists while the post-backup record does not (point-in-time recovery proof).
- Delete all temporary DB, snapshot and recovered files during test cleanup.

The operator must rerun the exact new commit before marking **RECOVERY_REHEARSAL=PASS**. This tests local recoverability only. Real operations still require an approved encrypted backup destination, retention, access controls, monitoring, and a restore rehearsal in the dedicated YangSpace LXC.

## 2026-11-10: persistence-test correction

On exact SHA `05b51aa`, the operator Windows runtime run passed FK enablement, initial migration, orphan rejection, parent delete, SQLite-level cascade and reopened-connection FK pragma, then stopped at `DATABASE_PERSISTENCE_AFTER_REOPEN`. The raw SQL query bound `persistedLocationId.ToString()` as lowercase TEXT, unlike EF Core SQLite's uppercase GUID storage. The fix centralizes SQL GUID formatting, uses typed SQLite TEXT parameters in all direct-SQL GUID calls, and asserts the persisted row before closing and after reopening the connection. No migration or production application code was changed by this correction. **Run the exact new commit before marking runtime acceptance PASS.**
