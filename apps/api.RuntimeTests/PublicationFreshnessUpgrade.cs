using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YangSpace.Api;

/// <summary>
/// F1b contract test: migrate a disposable, populated EF initial-schema DB
/// to the generated publication-timestamp migration, and restore its
/// pre-upgrade online backup to a distinct disposable file.
/// No configuration, active DB, HTTP listener, or external infrastructure.
/// </summary>
internal static class PublicationFreshnessUpgrade
{
    private const string Initial = "20261110043149_InitialDirectorySchema";
    private const string Freshness = "20261111125709_PublicationFreshnessTimestamps";

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + label);
        Console.WriteLine(label + "=PASS");
    }

    private static string ConnectionString(string path, bool create) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = create ? SqliteOpenMode.ReadWriteCreate : SqliteOpenMode.ReadWrite,
            ForeignKeys = true,
            Pooling = false
        }.ConnectionString;

    private static async Task<long> CountAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static async Task<bool> HasColumnAsync(SqliteConnection connection, string table, string name)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"" + table + "\");";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            if (string.Equals(reader.GetString(1), name, StringComparison.Ordinal)) return true;
        return false;
    }

    private static async Task AssertIntegrityAsync(SqliteConnection connection)
    {
        Check(
            await CountAsync(connection, "SELECT COUNT(*) FROM pragma_foreign_key_check;") == 0,
            "F1B_FOREIGN_KEYS_OK");
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var result = Convert.ToString(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        Check(string.Equals(result, "ok", StringComparison.Ordinal), "F1B_UPGRADE_INTEGRITY");
    }

    private static async Task SeedLegacyAsync(SqliteConnection connection, Guid locationId)
    {
        // All values are deterministic fictional fixtures. Only GUID fields
        // are bound; no user input or external database is ever consulted.
        var location = locationId.ToString("D").ToUpperInvariant();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "Locations"
                ("Id","Slug","Name","City","Neighborhood","Category","Description",
                 "Address","IsPublished","IsVerified","UpdatedAtUtc")
            VALUES ($location,'f1b-legacy-fixture','F1b legacy location','Варна',
                    'fixture','fixture','Not real','Disposable test only',
                    1,0,'2026-11-10T00:00:00.0000000Z');

            INSERT INTO "Offers"
                ("Id","LocationId","Title","Description","StartsAtUtc","EndsAtUtc","IsPublished")
            VALUES ($offer,$location,'F1b legacy offer','Preserve description',
                    '2026-11-10T00:00:00.0000000Z','2026-11-30T00:00:00.0000000Z',1);

            INSERT INTO "Brochures"
                ("Id","LocationId","Title","Url","ValidFromUtc","ValidToUtc","IsPublished")
            VALUES ($brochure,$location,'F1b legacy brochure','https://example.org/f1b.pdf',
                    '2026-11-10T00:00:00.0000000Z','2026-11-30T00:00:00.0000000Z',1);

            INSERT INTO "Jobs"
                ("Id","LocationId","Title","Description","ApplyUrl","ExpiresAtUtc","IsPublished")
            VALUES ($job,$location,'F1b legacy job','Preserve description',
                    'https://example.org/f1b-job','2026-11-30T00:00:00.0000000Z',1);
            """;
        command.Parameters.AddWithValue("$location", location);
        command.Parameters.AddWithValue("$offer", Guid.NewGuid().ToString("D").ToUpperInvariant());
        command.Parameters.AddWithValue("$brochure", Guid.NewGuid().ToString("D").ToUpperInvariant());
        command.Parameters.AddWithValue("$job", Guid.NewGuid().ToString("D").ToUpperInvariant());
        await command.ExecuteNonQueryAsync();
    }

    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "yangspace-f1b-upgrade-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var oldPath = Path.Combine(root, "original.db");
            var backupPath = Path.Combine(root, "preupgrade.backup.db");
            var recoveredPath = Path.Combine(root, "preupgrade.restored.db");
            var locationId = Guid.NewGuid();

            await using (var conn = new SqliteConnection(ConnectionString(oldPath, create: true)))
            {
                await conn.OpenAsync();
                var options = new DbContextOptionsBuilder<DirectoryDb>()
                    .UseSqlite(conn).Options;
                await using var db = new DirectoryDb(options);
                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Initial);

                var oldHistory = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                Check(oldHistory.Length == 1 && oldHistory[0] == Initial,
                    "F1B_LEGACY_SCHEMA_CREATED");
                foreach (var table in new[] { "Offers", "Brochures", "Jobs" })
                    Check(!await HasColumnAsync(conn, table, "CreatedAtUtc"),
                        "F1B_OLD_SCHEMA_WITHOUT_TIMESTAMP_" + table.ToUpperInvariant());

                await SeedLegacyAsync(conn, locationId);

                // Backup must represent actual populated initial EF schema.
                await using (var backup = new SqliteConnection(ConnectionString(backupPath, create: true)))
                {
                    await backup.OpenAsync();
                    conn.BackupDatabase(backup);
                }
                Check(File.Exists(backupPath) && new FileInfo(backupPath).Length > 0,
                    "F1B_PREUPGRADE_BACKUP_CREATED");

                await migrator.MigrateAsync(Freshness);
                var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                Check(migrations.SequenceEqual(new[] { Initial, Freshness }),
                    "F1B_MIGRATION_HISTORY");

                foreach (var table in new[] { "Offers", "Brochures", "Jobs" })
                    foreach (var column in new[] { "CreatedAtUtc", "UpdatedAtUtc", "PublishedAtUtc" })
                        Check(await HasColumnAsync(conn, table, column),
                            "F1B_COLUMN_" + table.ToUpperInvariant() + "_" + column.ToUpperInvariant());

                Check(await CountAsync(conn, """
                    SELECT COUNT(*) FROM "Offers"
                    WHERE "Title" = 'F1b legacy offer'
                      AND "Description" = 'Preserve description'
                      AND "CreatedAtUtc" IS NULL AND "UpdatedAtUtc" IS NULL
                      AND "PublishedAtUtc" IS NULL;
                    """) == 1 &&
                    await CountAsync(conn, """
                    SELECT COUNT(*) FROM "Brochures"
                    WHERE "Title" = 'F1b legacy brochure'
                      AND "Url" = 'https://example.org/f1b.pdf'
                      AND "CreatedAtUtc" IS NULL AND "UpdatedAtUtc" IS NULL
                      AND "PublishedAtUtc" IS NULL;
                    """) == 1 &&
                    await CountAsync(conn, """
                    SELECT COUNT(*) FROM "Jobs"
                    WHERE "Title" = 'F1b legacy job'
                      AND "Description" = 'Preserve description'
                      AND "CreatedAtUtc" IS NULL AND "UpdatedAtUtc" IS NULL
                      AND "PublishedAtUtc" IS NULL;
                    """) == 1, "F1B_LEGACY_TIMESTAMPS_NULL");

                Check(await db.Locations.AsNoTracking().AnyAsync(x => x.Id == locationId),
                    "F1B_LEGACY_ROWS_PRESERVED");
                await AssertIntegrityAsync(conn);

                var now = DateTime.UtcNow;
                var offer = new LocalOffer
                {
                    LocationId = locationId,
                    Title = "Post-upgrade created offer",
                    Description = "F1b generated schema accepts timestamps",
                    StartsAtUtc = now.AddHours(-1),
                    EndsAtUtc = now.AddDays(3),
                    IsPublished = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    PublishedAtUtc = now
                };
                db.Offers.Add(offer);
                await db.SaveChangesAsync();
                Check(await db.Offers.AsNoTracking().AnyAsync(x => x.Id == offer.Id &&
                    x.CreatedAtUtc.HasValue && x.UpdatedAtUtc.HasValue &&
                    x.PublishedAtUtc.HasValue), "F1B_NEW_TIMESTAMP_WRITE");
            }

            // Restore pre-upgrade backup into an entirely separate file.
            await using (var backup = new SqliteConnection(ConnectionString(backupPath, create: false)))
            {
                await backup.OpenAsync();
                await using var restored = new SqliteConnection(ConnectionString(recoveredPath, create: true));
                await restored.OpenAsync();
                backup.BackupDatabase(restored);
            }
            await using (var restored = new SqliteConnection(ConnectionString(recoveredPath, create: false)))
            {
                await restored.OpenAsync();
                var options = new DbContextOptionsBuilder<DirectoryDb>()
                    .UseSqlite(restored).Options;
                await using var oldDb = new DirectoryDb(options);
                var history = (await oldDb.Database.GetAppliedMigrationsAsync()).ToArray();
                Check(history.Length == 1 && history[0] == Initial,
                    "F1B_PREUPGRADE_BACKUP_HISTORY");
                Check(!await HasColumnAsync(restored, "Offers", "CreatedAtUtc"),
                    "F1B_PREUPGRADE_BACKUP_SCHEMA");
                Check(await CountAsync(restored,
                    "SELECT COUNT(*) FROM \"Offers\" WHERE \"Title\"='F1b legacy offer';") == 1 &&
                    await CountAsync(restored,
                    "SELECT COUNT(*) FROM \"Brochures\" WHERE \"Title\"='F1b legacy brochure';") == 1 &&
                    await CountAsync(restored,
                    "SELECT COUNT(*) FROM \"Jobs\" WHERE \"Title\"='F1b legacy job';") == 1,
                    "F1B_PREUPGRADE_BACKUP_RESTORED");
                await AssertIntegrityAsync(restored);
            }
            Console.WriteLine("F1B_UPGRADE_REHEARSAL=PASS");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}
