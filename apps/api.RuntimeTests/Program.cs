using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YangSpace.Api;

static void Check(bool condition, string marker)
{
    if (!condition)
        throw new InvalidOperationException("FAIL: " + marker);
    Console.WriteLine(marker + "=PASS");
}

static void ExpectInvalidOperation(Action action, string expectedMessage, string marker)
{
    try
    {
        action();
    }
    catch (InvalidOperationException exception)
        when (exception.Message.Contains(expectedMessage, StringComparison.Ordinal))
    {
        Console.WriteLine(marker + "=PASS");
        return;
    }

    throw new InvalidOperationException("FAIL: " + marker + " was not rejected as expected.");
}

static async Task ExpectInvalidOperationAsync(
    Func<Task> action,
    string expectedMessage,
    string marker)
{
    try
    {
        await action();
    }
    catch (InvalidOperationException exception)
        when (exception.Message.Contains(expectedMessage, StringComparison.Ordinal))
    {
        Console.WriteLine(marker + "=PASS");
        return;
    }

    throw new InvalidOperationException("FAIL: " + marker + " was not rejected as expected.");
}

static async Task<int> ReadForeignKeysAsync(SqliteConnection connection)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA foreign_keys;";
    return Convert.ToInt32(
        await command.ExecuteScalarAsync(),
        CultureInfo.InvariantCulture);
}

// SQLite stores EF Core Guid keys as uppercase TEXT. Every direct-SQL
// parameter in this harness must use the same representation.
static string SqliteGuidText(Guid value) => value.ToString("D").ToUpperInvariant();

static string BuildConnectionString(string path, bool foreignKeys)
{
    return new SqliteConnectionStringBuilder
    {
        DataSource = path,
        Mode = SqliteOpenMode.ReadWrite,
        ForeignKeys = foreignKeys,
        Pooling = false
    }.ConnectionString;
}

static async Task ApplyMigrationAsync(string connectionString)
{
    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<DirectoryDb>()
        .UseSqlite(connection)
        .Options;

    await using var db = new DirectoryDb(options);
    await db.Database.MigrateAsync();
}

// Exercise a point-in-time, online SQLite backup and restore using only
// temporary DB files belonging to this harness. BackupDatabase uses SQLite's
// online backup API instead of unsafe file copying while a database is live.
static async Task RunRecoveryRehearsalAsync(
    string sandbox,
    string releaseRoot,
    string activeConnectionString,
    Guid persistedLocationId)
{
    var backupPath = Path.Combine(sandbox, "directory.backup.db");
    var recoveredPath = Path.Combine(sandbox, "directory.recovered.db");
    Check(
        !File.Exists(backupPath) && !File.Exists(recoveredPath),
        "RECOVERY_PATHS_NEW");

    var backupDestination = new SqliteConnectionStringBuilder
    {
        DataSource = backupPath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        ForeignKeys = true,
        Pooling = false
    }.ConnectionString;

    // The source is a migrated, existing disposable database and remains
    // readable while the snapshot is made. All writers are blocked briefly.
    await using (var source = new SqliteConnection(activeConnectionString))
    {
        await source.OpenAsync();
        await using var backup = new SqliteConnection(backupDestination);
        await backup.OpenAsync();
        source.BackupDatabase(backup);
    }
    Check(
        File.Exists(backupPath) && new FileInfo(backupPath).Length > 0,
        "ONLINE_BACKUP_CREATED");

    // Make a committed change AFTER the backup to prove the restored state
    // represents the recovery point, not an accidental read of the live file.
    var postBackupSlug = "after-backup-" + Guid.NewGuid().ToString("N");
    await using (var active = new SqliteConnection(activeConnectionString))
    {
        await active.OpenAsync();
        var activeOptions = new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite(active)
            .Options;
        await using var activeDb = new DirectoryDb(activeOptions);
        activeDb.Locations.Add(new BusinessLocation
        {
            Slug = postBackupSlug,
            Name = "After-backup fixture",
            Category = "test",
            Neighborhood = "test",
            Description = "Created after backup for recovery point verification",
            Address = "Disposable recovery fixture"
        });
        await activeDb.SaveChangesAsync();
        Check(
            await activeDb.Locations.AsNoTracking()
                .AnyAsync(x => x.Slug == postBackupSlug),
            "POST_BACKUP_SOURCE_MUTATION");
    }

    var backupReadOnly = new SqliteConnectionStringBuilder
    {
        DataSource = backupPath,
        Mode = SqliteOpenMode.ReadOnly,
        ForeignKeys = true,
        Pooling = false
    }.ConnectionString;
    var recoveredDestination = new SqliteConnectionStringBuilder
    {
        DataSource = recoveredPath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        ForeignKeys = true,
        Pooling = false
    }.ConnectionString;

    // Restore from the snapshot into a DIFFERENT, freshly created temp file.
    // Never overwrite an active DB or use a connection string from config.
    await using (var backup = new SqliteConnection(backupReadOnly))
    {
        await backup.OpenAsync();
        await using var recovered = new SqliteConnection(recoveredDestination);
        await recovered.OpenAsync();
        backup.BackupDatabase(recovered);
    }
    Check(
        File.Exists(recoveredPath) && new FileInfo(recoveredPath).Length > 0,
        "RECOVERED_DATABASE_CREATED");

    var recoveredConnectionString =
        BuildConnectionString(recoveredPath, foreignKeys: true);
    ProductionSqlitePolicy.RequirePreparedDatabase(
        recoveredConnectionString, releaseRoot);

    await using var reopened = new SqliteConnection(recoveredConnectionString);
    await reopened.OpenAsync();
    Check(
        await ReadForeignKeysAsync(reopened) == 1,
        "RECOVERED_FOREIGN_KEYS_ON");

    await using (var integrity = reopened.CreateCommand())
    {
        integrity.CommandText = "PRAGMA integrity_check;";
        var value = await integrity.ExecuteScalarAsync();
        Check(
            string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture),
                "ok", StringComparison.OrdinalIgnoreCase),
            "RECOVERED_SQLITE_INTEGRITY");
    }

    await using (var foreignKeyCheck = reopened.CreateCommand())
    {
        foreignKeyCheck.CommandText = "PRAGMA foreign_key_check;";
        await using var reader = await foreignKeyCheck.ExecuteReaderAsync();
        Check(!await reader.ReadAsync(), "RECOVERED_FOREIGN_KEY_CHECK");
    }

    var restoredOptions = new DbContextOptionsBuilder<DirectoryDb>()
        .UseSqlite(reopened)
        .Options;
    await using var restoredDb = new DirectoryDb(restoredOptions);
    await ProductionSchemaGate.ValidateSchemaAsync(restoredDb);
    Console.WriteLine("RECOVERED_SCHEMA_GATE=PASS");

    var applied = (await restoredDb.Database.GetAppliedMigrationsAsync()).ToArray();
    Check(
        applied.Length == 1 &&
        applied[0] == "20261110043149_InitialDirectorySchema",
        "RECOVERED_MIGRATION_HISTORY");

    Check(
        await restoredDb.Locations.AsNoTracking()
            .AnyAsync(x => x.Id == persistedLocationId),
        "RECOVERED_PRE_BACKUP_ROW");

    Check(
        !await restoredDb.Locations.AsNoTracking()
            .AnyAsync(x => x.Slug == postBackupSlug),
        "RECOVERY_POINT_IN_TIME");

    Console.WriteLine("RECOVERY_REHEARSAL=PASS");
}

var sandbox = Path.Combine(
    Path.GetTempPath(),
    "yangspace-sqlite-runtime-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox);

try
{
    var releaseRoot = Path.Combine(sandbox, "release");
    Directory.CreateDirectory(releaseRoot);

    var databasePath = Path.Combine(sandbox, "directory.db");
    File.WriteAllBytes(databasePath, Array.Empty<byte>());

    var enabledConnectionString = BuildConnectionString(databasePath, foreignKeys: true);
    var disabledConnectionString = BuildConnectionString(databasePath, foreignKeys: false);

    var normalizedConnectionString =
        SqliteRuntimePolicy.EnableForeignKeys(disabledConnectionString);
    Check(
        new SqliteConnectionStringBuilder(normalizedConnectionString).ForeignKeys == true,
        "RUNTIME_CONNECTION_FORCES_FOREIGN_KEYS");

    ProductionSqlitePolicy.RequirePreparedDatabase(enabledConnectionString, releaseRoot);
    ExpectInvalidOperation(
        () => ProductionSqlitePolicy.RequirePreparedDatabase(disabledConnectionString, releaseRoot),
        "ForeignKeys=True",
        "PRODUCTION_POLICY_REJECTS_FOREIGN_KEYS_DISABLED");

    Guid persistedLocationId;

    await using (var connection = new SqliteConnection(enabledConnectionString))
    {
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite(connection)
            .Options;

        await using var db = new DirectoryDb(options);
        await db.Database.MigrateAsync();

        Check(await ReadForeignKeysAsync(connection) == 1, "LIVE_PRAGMA_FOREIGN_KEYS_ON");
        await ProductionSchemaGate.ValidateSchemaAsync(db);
        Console.WriteLine("MIGRATION_AND_PRODUCTION_SCHEMA_GATE=PASS");

        var orphan = new LocalOffer
        {
            LocationId = Guid.NewGuid(),
            Title = "Orphan regression fixture",
            Description = "Must be rejected by SQLite",
            StartsAtUtc = DateTime.UtcNow,
            EndsAtUtc = DateTime.UtcNow.AddDays(1)
        };

        db.Offers.Add(orphan);
        var orphanRejected = false;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException sqlite &&
                  sqlite.SqliteErrorCode == 19 &&
                  sqlite.SqliteExtendedErrorCode == 787)
        {
            orphanRejected = true;
            db.Entry(orphan).State = EntityState.Detached;
        }

        Check(orphanRejected, "ORPHAN_INSERT_REJECTED");

        var cascadeLocation = new BusinessLocation
        {
            Slug = "cascade-" + Guid.NewGuid().ToString("N"),
            Name = "Cascade regression fixture",
            Category = "test",
            Neighborhood = "test",
            Description = "Disposable cascade fixture",
            Address = "Not a real address"
        };
        var cascadeOffer = new LocalOffer
        {
            LocationId = cascadeLocation.Id,
            Title = "Cascade child",
            Description = "Must be removed by SQLite FK cascade",
            StartsAtUtc = DateTime.UtcNow,
            EndsAtUtc = DateTime.UtcNow.AddDays(1)
        };

        db.Locations.Add(cascadeLocation);
        db.Offers.Add(cascadeOffer);
        await db.SaveChangesAsync();

        // EF Core's SQLite Guid mapping stores GUIDs as TEXT using its
        // canonical uppercase representation. A plain Guid.ToString() is
        // lowercase and would not match the stored key in a case-sensitive
        // SQLite TEXT comparison, making the test delete zero parent rows.
        var cascadeLocationKey = SqliteGuidText(cascadeLocation.Id);

        int deletedParents;
        await using (var delete = connection.CreateCommand())
        {
            delete.CommandText = "DELETE FROM \"Locations\" WHERE \"Id\" = $id;";
            delete.Parameters.Add("$id", SqliteType.Text).Value = cascadeLocationKey;
            deletedParents = await delete.ExecuteNonQueryAsync();
        }

        Check(deletedParents == 1, "CASCADE_PARENT_DELETE_MATCHED");

        await using (var verifyCascade = connection.CreateCommand())
        {
            verifyCascade.CommandText =
                "SELECT COUNT(*) FROM \"Offers\" WHERE \"LocationId\" = $id;";
            verifyCascade.Parameters.Add("$id", SqliteType.Text).Value = cascadeLocationKey;
            var remainingChildren = Convert.ToInt32(
                await verifyCascade.ExecuteScalarAsync(),
                CultureInfo.InvariantCulture);
            Check(remainingChildren == 0, "DATABASE_CASCADE_DELETE");
        }

        var persistedLocation = new BusinessLocation
        {
            Slug = "persist-" + Guid.NewGuid().ToString("N"),
            Name = "Persistence regression fixture",
            Category = "test",
            Neighborhood = "test",
            Description = "Disposable persistence fixture",
            Address = "Not a real address"
        };
        db.Locations.Add(persistedLocation);
        await db.SaveChangesAsync();
        persistedLocationId = persistedLocation.Id;
        Check(
            await db.Locations.AsNoTracking().AnyAsync(x => x.Id == persistedLocationId),
            "PERSISTED_ROW_BEFORE_REOPEN");

        await ProductionSchemaGate.ValidateSchemaAsync(db);
    }

    await using (var reopened = new SqliteConnection(enabledConnectionString))
    {
        await reopened.OpenAsync();
        Check(await ReadForeignKeysAsync(reopened) == 1, "REOPENED_CONNECTION_FOREIGN_KEYS_ON");

        await using var command = reopened.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"Locations\" WHERE \"Id\" = $id;";
        command.Parameters.Add("$id", SqliteType.Text).Value = SqliteGuidText(persistedLocationId);
        var count = Convert.ToInt32(
            await command.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture);

        Check(count == 1, "DATABASE_PERSISTENCE_AFTER_REOPEN");
    }

    await RunRecoveryRehearsalAsync(
        sandbox, releaseRoot, enabledConnectionString, persistedLocationId);

    await using (var disabledConnection = new SqliteConnection(disabledConnectionString))
    {
        await disabledConnection.OpenAsync();
        var disabledOptions = new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite(disabledConnection)
            .Options;
        await using var disabledDb = new DirectoryDb(disabledOptions);

        await ExpectInvalidOperationAsync(
            () => ProductionSchemaGate.ValidateSchemaAsync(disabledDb),
            "foreign-key enforcement is disabled",
            "PRODUCTION_SCHEMA_GATE_REJECTS_DISABLED_FOREIGN_KEYS");
    }

    var corruptPath = Path.Combine(sandbox, "legacy-orphan.db");
    File.WriteAllBytes(corruptPath, Array.Empty<byte>());
    var corruptEnabled = BuildConnectionString(corruptPath, foreignKeys: true);
    var corruptDisabled = BuildConnectionString(corruptPath, foreignKeys: false);
    await ApplyMigrationAsync(corruptEnabled);

    await using (var legacyConnection = new SqliteConnection(corruptDisabled))
    {
        await legacyConnection.OpenAsync();
        await using var insert = legacyConnection.CreateCommand();
        insert.CommandText = """
            INSERT INTO "Offers"
                ("Id", "LocationId", "Title", "Description", "StartsAtUtc", "EndsAtUtc", "IsPublished")
            VALUES
                ($id, $locationId, 'Legacy orphan', 'Injected only in disposable test DB',
                 '2026-11-10T00:00:00.0000000Z', '2026-11-11T00:00:00.0000000Z', 0);
            """;
        insert.Parameters.Add("$id", SqliteType.Text).Value = SqliteGuidText(Guid.NewGuid());
        insert.Parameters.Add("$locationId", SqliteType.Text).Value = SqliteGuidText(Guid.NewGuid());
        await insert.ExecuteNonQueryAsync();
    }

    await using (var integrityConnection = new SqliteConnection(corruptEnabled))
    {
        await integrityConnection.OpenAsync();
        var integrityOptions = new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite(integrityConnection)
            .Options;
        await using var integrityDb = new DirectoryDb(integrityOptions);

        await ExpectInvalidOperationAsync(
            () => ProductionSchemaGate.ValidateSchemaAsync(integrityDb),
            "foreign-key integrity check found violations",
            "PRODUCTION_SCHEMA_GATE_DETECTS_LEGACY_ORPHANS");
    }

    Console.WriteLine("SQLITE_RUNTIME_INTEGRITY=PASS");
}
finally
{
    Directory.Delete(sandbox, recursive: true);
}
