using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YangSpace.Api;

/// <summary>
/// F2a-S2: exercises the generated SQLite organization/claim migration against
/// synthetic historic data and proves restore-based recovery at transaction
/// boundaries. Every database is created below the OS temp directory.
/// </summary>
internal static class F2aOrganizationClaimUpgrade
{
    private const string Initial = "20261110043149_InitialDirectorySchema";
    private const string Freshness = "20261111125709_PublicationFreshnessTimestamps";
    private const string F2a = "20261114183515_F2aOrganizationClaimFoundation";

    private sealed record FixtureIds(
        Guid Location,
        Guid Offer,
        Guid Brochure,
        Guid Job,
        Guid Organization,
        Guid Member,
        Guid Claim,
        Guid Audit);

    private sealed class InjectedMigrationFailureException(string marker)
        : Exception("Injected disposable migration failure at " + marker);

    private sealed class FailOnCommandInterceptor : DbCommandInterceptor
    {
        private readonly string _marker;

        public FailOnCommandInterceptor(string marker)
        {
            _marker = Normalize(marker);
        }

        public bool Triggered { get; private set; }

        private static string Normalize(string value) =>
            Regex.Replace(value, @"\s+", " ").Trim().ToUpperInvariant();

        private void ThrowIfMatched(string commandText)
        {
            if (Triggered || !Normalize(commandText).Contains(_marker, StringComparison.Ordinal))
                return;

            Triggered = true;
            throw new InjectedMigrationFailureException(_marker);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            ThrowIfMatched(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ThrowIfMatched(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private static void Check(bool condition, string marker)
    {
        if (!condition)
            throw new InvalidOperationException("FAIL: " + marker);

        Console.WriteLine(marker + "=PASS");
    }

    private static string GuidText(Guid value) =>
        value.ToString("D").ToUpperInvariant();

    private static string ConnectionString(string path, SqliteOpenMode mode) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            ForeignKeys = true,
            Pooling = false
        }.ConnectionString;

    private static DbContextOptions<DirectoryDb> Options(
        SqliteConnection connection,
        IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly("YangSpace.Api"));

        if (interceptor is not null)
            builder.AddInterceptors(interceptor);

        return builder.Options;
    }

    private static async Task<long> ScalarLongAsync(
        SqliteConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var parameter in parameters)
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);

        return Convert.ToInt64(
            await command.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture);
    }

    private static async Task<bool> HasColumnAsync(
        SqliteConnection connection,
        string table,
        string column)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"" + table + "\");";
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static async Task<HashSet<string>> ReadColumnsAsync(
        SqliteConnection connection,
        string table)
    {
        var columns = new HashSet<string>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"" + table + "\");";
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            columns.Add(reader.GetString(1));

        return columns;
    }

    private static async Task<bool> HasTableAsync(
        SqliteConnection connection,
        string table)
    {
        return await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name;",
            ("$name", table)) == 1;
    }

    private static async Task AssertIntegrityAsync(
        SqliteConnection connection,
        string prefix)
    {
        var foreignKeys = await ScalarLongAsync(connection, "PRAGMA foreign_keys;");
        Check(foreignKeys == 1, prefix + "_FOREIGN_KEYS_ENABLED");

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA integrity_check;";
            var result = Convert.ToString(
                await command.ExecuteScalarAsync(),
                CultureInfo.InvariantCulture);
            Check(string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase),
                prefix + "_SQLITE_INTEGRITY");
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await command.ExecuteReaderAsync();
            Check(!await reader.ReadAsync(), prefix + "_FOREIGN_KEY_CHECK");
        }
    }

    private static async Task AssertHistoryAsync(
        SqliteConnection connection,
        string[] expected,
        string marker)
    {
        var actual = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT MigrationId FROM \"__EFMigrationsHistory\" ORDER BY MigrationId;";
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            actual.Add(reader.GetString(0));

        Check(actual.SequenceEqual(expected, StringComparer.Ordinal), marker);
    }

    private static async Task SeedLegacyAsync(
        SqliteConnection connection,
        FixtureIds ids)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "Locations"
                ("Id","Slug","Name","City","Neighborhood","Category","Description",
                 "Address","Phone","Website","ImageUrl","OpeningHours",
                 "IsPublished","IsVerified","UpdatedAtUtc")
            VALUES
                ($locationId,'f2a-s2-legacy-location','Legacy location','Варна',
                 'Център','health','Legacy description','Legacy address',
                 '+359000000000','https://example.org','https://example.org/image.png',
                 '09:00-18:00',1,1,'2026-11-14T10:00:00.0000000Z');

            INSERT INTO "Offers"
                ("Id","LocationId","Title","Description","StartsAtUtc","EndsAtUtc","IsPublished")
            VALUES
                ($offerId,$locationId,'Legacy offer','Offer description',
                 '2026-11-14T00:00:00.0000000Z','2026-12-14T00:00:00.0000000Z',1);

            INSERT INTO "Brochures"
                ("Id","LocationId","Title","Url","ValidFromUtc","ValidToUtc","IsPublished")
            VALUES
                ($brochureId,$locationId,'Legacy brochure','https://example.org/brochure.pdf',
                 '2026-11-14T00:00:00.0000000Z','2026-12-14T00:00:00.0000000Z',1);

            INSERT INTO "Jobs"
                ("Id","LocationId","Title","Description","ApplyUrl","ExpiresAtUtc","IsPublished")
            VALUES
                ($jobId,$locationId,'Legacy job','Job description',
                 'https://example.org/apply','2026-12-14T00:00:00.0000000Z',1);

            INSERT INTO "AspNetRoles"
                ("Id","Name","NormalizedName","ConcurrencyStamp")
            VALUES
                ('fixture-role-1','PlatformAdmin','PLATFORMADMIN','role-stamp-1');

            INSERT INTO "AspNetUsers"
                ("Id","UserName","NormalizedUserName","Email","NormalizedEmail",
                 "EmailConfirmed","PasswordHash","SecurityStamp","ConcurrencyStamp",
                 "PhoneNumber","PhoneNumberConfirmed","TwoFactorEnabled",
                 "LockoutEnd","LockoutEnabled","AccessFailedCount")
            VALUES
                ('fixture-user-1','fixture-admin','FIXTURE-ADMIN',
                 'fixture@example.org','FIXTURE@EXAMPLE.ORG',1,'hash-1',
                 'security-1','concurrency-1','+359000000001',1,0,
                 NULL,1,0);

            INSERT INTO "AspNetUserRoles" ("UserId","RoleId")
            VALUES ('fixture-user-1','fixture-role-1');

            INSERT INTO "AspNetUserClaims" ("Id","UserId","ClaimType","ClaimValue")
            VALUES (71,'fixture-user-1','fixture-type','fixture-value');

            INSERT INTO "AspNetRoleClaims" ("Id","RoleId","ClaimType","ClaimValue")
            VALUES (81,'fixture-role-1','fixture-role-type','fixture-role-value');

            INSERT INTO "AspNetUserLogins"
                ("LoginProvider","ProviderKey","ProviderDisplayName","UserId")
            VALUES ('fixture-provider','fixture-key','Fixture login','fixture-user-1');

            INSERT INTO "AspNetUserTokens" ("UserId","LoginProvider","Name","Value")
            VALUES ('fixture-user-1','fixture-provider','fixture-token','fixture-token-value');
            """;

        command.Parameters.AddWithValue("$locationId", GuidText(ids.Location));
        command.Parameters.AddWithValue("$offerId", GuidText(ids.Offer));
        command.Parameters.AddWithValue("$brochureId", GuidText(ids.Brochure));
        command.Parameters.AddWithValue("$jobId", GuidText(ids.Job));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertLegacyStateAsync(
        string path,
        FixtureIds ids,
        string prefix)
    {
        await using var connection = new SqliteConnection(
            ConnectionString(path, SqliteOpenMode.ReadWrite));
        await connection.OpenAsync();

        var columns = await ReadColumnsAsync(connection, "Locations");
        var expectedColumns = new[]
        {
            "Id", "Slug", "Name", "City", "Neighborhood", "Category",
            "Description", "Address", "Phone", "Website", "ImageUrl",
            "OpeningHours", "IsPublished", "IsVerified", "UpdatedAtUtc"
        };
        Check(columns.SetEquals(expectedColumns), prefix + "_LEGACY_LOCATIONS_COLUMNS");
        Check(!columns.Contains("OrganizationId") && !columns.Contains("Revision"),
            prefix + "_NO_F2A_COLUMNS");

        Check(await ScalarLongAsync(connection, """
            SELECT COUNT(*) FROM "Locations"
            WHERE "Id"=$id AND "Slug"='f2a-s2-legacy-location'
              AND "Name"='Legacy location' AND "City"='Варна'
              AND "Neighborhood"='Център' AND "Category"='health'
              AND "Description"='Legacy description' AND "Address"='Legacy address'
              AND "Phone"='+359000000000' AND "Website"='https://example.org'
              AND "ImageUrl"='https://example.org/image.png'
              AND "OpeningHours"='09:00-18:00'
              AND "IsPublished"=1 AND "IsVerified"=1
              AND "UpdatedAtUtc"='2026-11-14T10:00:00.0000000Z';
            """, ("$id", GuidText(ids.Location))) == 1,
            prefix + "_LOCATION_DATA");

        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"Offers\" WHERE \"Id\"=$id AND \"LocationId\"=$locationId AND \"Title\"='Legacy offer' AND \"Description\"='Offer description' AND \"StartsAtUtc\"='2026-11-14T00:00:00.0000000Z' AND \"EndsAtUtc\"='2026-12-14T00:00:00.0000000Z' AND \"IsPublished\"=1;",
            ("$id", GuidText(ids.Offer)), ("$locationId", GuidText(ids.Location))) == 1, prefix + "_OFFER_DATA");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"Brochures\" WHERE \"Id\"=$id AND \"LocationId\"=$locationId AND \"Title\"='Legacy brochure' AND \"Url\"='https://example.org/brochure.pdf' AND \"ValidFromUtc\"='2026-11-14T00:00:00.0000000Z' AND \"ValidToUtc\"='2026-12-14T00:00:00.0000000Z' AND \"IsPublished\"=1;",
            ("$id", GuidText(ids.Brochure)), ("$locationId", GuidText(ids.Location))) == 1, prefix + "_BROCHURE_DATA");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"Jobs\" WHERE \"Id\"=$id AND \"LocationId\"=$locationId AND \"Title\"='Legacy job' AND \"Description\"='Job description' AND \"ApplyUrl\"='https://example.org/apply' AND \"ExpiresAtUtc\"='2026-12-14T00:00:00.0000000Z' AND \"IsPublished\"=1;",
            ("$id", GuidText(ids.Job)), ("$locationId", GuidText(ids.Location))) == 1, prefix + "_JOB_DATA");

        await AssertIdentityDataAsync(connection, prefix);
        await AssertHistoryAsync(connection, new[] { Initial, Freshness },
            prefix + "_MIGRATION_HISTORY");
        await AssertIntegrityAsync(connection, prefix);
    }

    private static async Task AssertIdentityDataAsync(
        SqliteConnection connection,
        string prefix)
    {
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"AspNetUsers\" WHERE \"Id\"='fixture-user-1' AND \"UserName\"='fixture-admin' AND \"NormalizedUserName\"='FIXTURE-ADMIN' AND \"Email\"='fixture@example.org' AND \"NormalizedEmail\"='FIXTURE@EXAMPLE.ORG' AND \"EmailConfirmed\"=1 AND \"PasswordHash\"='hash-1' AND \"SecurityStamp\"='security-1' AND \"ConcurrencyStamp\"='concurrency-1' AND \"PhoneNumber\"='+359000000001' AND \"PhoneNumberConfirmed\"=1 AND \"TwoFactorEnabled\"=0 AND \"LockoutEnabled\"=1 AND \"AccessFailedCount\"=0;") == 1,
            prefix + "_IDENTITY_USER");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"AspNetRoles\" WHERE \"Id\"='fixture-role-1' AND \"NormalizedName\"='PLATFORMADMIN';") == 1,
            prefix + "_IDENTITY_ROLE");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"AspNetUserRoles\" WHERE \"UserId\"='fixture-user-1' AND \"RoleId\"='fixture-role-1';") == 1,
            prefix + "_IDENTITY_USER_ROLE");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"AspNetUserClaims\" WHERE \"Id\"=71 AND \"UserId\"='fixture-user-1';") == 1,
            prefix + "_IDENTITY_USER_CLAIM");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"AspNetRoleClaims\" WHERE \"Id\"=81 AND \"RoleId\"='fixture-role-1';") == 1,
            prefix + "_IDENTITY_ROLE_CLAIM");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"AspNetUserLogins\" WHERE \"LoginProvider\"='fixture-provider' AND \"ProviderKey\"='fixture-key' AND \"UserId\"='fixture-user-1';") == 1,
            prefix + "_IDENTITY_LOGIN");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"AspNetUserTokens\" WHERE \"UserId\"='fixture-user-1' AND \"Name\"='fixture-token' AND \"Value\"='fixture-token-value';") == 1,
            prefix + "_IDENTITY_TOKEN");
    }

    private static async Task AssertUpgradedStateAsync(
        string path,
        FixtureIds ids)
    {
        await using var connection = new SqliteConnection(
            ConnectionString(path, SqliteOpenMode.ReadWrite));
        await connection.OpenAsync();

        var columns = await ReadColumnsAsync(connection, "Locations");
        var expectedColumns = new[]
        {
            "Id", "Slug", "Name", "City", "Neighborhood", "Category",
            "Description", "Address", "Phone", "Website", "ImageUrl",
            "OpeningHours", "IsPublished", "IsVerified", "UpdatedAtUtc",
            "OrganizationId", "Revision"
        };
        Check(columns.SetEquals(expectedColumns), "F2A_S2_LOCATIONS_COLUMNS");

        Check(await ScalarLongAsync(connection, """
            SELECT COUNT(*) FROM "Locations"
            WHERE "Id"=$id AND "Slug"='f2a-s2-legacy-location'
              AND "Name"='Legacy location' AND "City"='Варна'
              AND "Neighborhood"='Център' AND "Category"='health'
              AND "Description"='Legacy description' AND "Address"='Legacy address'
              AND "Phone"='+359000000000' AND "Website"='https://example.org'
              AND "ImageUrl"='https://example.org/image.png'
              AND "OpeningHours"='09:00-18:00'
              AND "IsPublished"=1 AND "IsVerified"=1
              AND "UpdatedAtUtc"='2026-11-14T10:00:00.0000000Z'
              AND "OrganizationId" IS NULL AND "Revision"=0;
            """, ("$id", GuidText(ids.Location))) == 1,
            "F2A_S2_LOCATION_ALL_FIELDS_PRESERVED");

        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"Offers\" WHERE \"Id\"=$id AND \"LocationId\"=$locationId AND \"Title\"='Legacy offer' AND \"Description\"='Offer description' AND \"StartsAtUtc\"='2026-11-14T00:00:00.0000000Z' AND \"EndsAtUtc\"='2026-12-14T00:00:00.0000000Z' AND \"IsPublished\"=1 AND \"CreatedAtUtc\" IS NULL AND \"UpdatedAtUtc\" IS NULL AND \"PublishedAtUtc\" IS NULL;",
            ("$id", GuidText(ids.Offer)), ("$locationId", GuidText(ids.Location))) == 1, "F2A_S2_OFFERS_PRESERVED");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"Brochures\" WHERE \"Id\"=$id AND \"LocationId\"=$locationId AND \"Title\"='Legacy brochure' AND \"Url\"='https://example.org/brochure.pdf' AND \"ValidFromUtc\"='2026-11-14T00:00:00.0000000Z' AND \"ValidToUtc\"='2026-12-14T00:00:00.0000000Z' AND \"IsPublished\"=1 AND \"CreatedAtUtc\" IS NULL AND \"UpdatedAtUtc\" IS NULL AND \"PublishedAtUtc\" IS NULL;",
            ("$id", GuidText(ids.Brochure)), ("$locationId", GuidText(ids.Location))) == 1, "F2A_S2_BROCHURES_PRESERVED");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"Jobs\" WHERE \"Id\"=$id AND \"LocationId\"=$locationId AND \"Title\"='Legacy job' AND \"Description\"='Job description' AND \"ApplyUrl\"='https://example.org/apply' AND \"ExpiresAtUtc\"='2026-12-14T00:00:00.0000000Z' AND \"IsPublished\"=1 AND \"CreatedAtUtc\" IS NULL AND \"UpdatedAtUtc\" IS NULL AND \"PublishedAtUtc\" IS NULL;",
            ("$id", GuidText(ids.Job)), ("$locationId", GuidText(ids.Location))) == 1, "F2A_S2_JOBS_PRESERVED");

        await AssertIdentityDataAsync(connection, "F2A_S2");
        await AssertHistoryAsync(connection, new[] { Initial, Freshness, F2a },
            "F2A_S2_MIGRATION_HISTORY");

        foreach (var table in new[]
        {
            "Organizations", "OrganizationMembers", "BusinessClaims", "ContentAuditEvents"
        })
            Check(await HasTableAsync(connection, table), "F2A_S2_TABLE_" + table.ToUpperInvariant());

        Check(!await HasTableAsync(connection, "ef_temp_Locations"),
            "F2A_S2_TEMP_TABLE_REMOVED");
        Check(!await HasTableAsync(connection, "IdentityEmailOutbox"),
            "F2A_S2_NO_OUTBOX_TABLE");

        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name IN ('IX_Locations_City_Neighborhood_Category','IX_Locations_Slug','IX_Locations_OrganizationId');") == 3,
            "F2A_S2_LOCATIONS_INDEXES");

        await AssertIntegrityAsync(connection, "F2A_S2");

        await ExerciseNewSchemaAsync(connection, ids);
        await AssertIntegrityAsync(connection, "F2A_S2_AFTER_NEW_RELATIONSHIPS");

        Console.WriteLine("F2A_S2_DATA_PRESERVATION=PASS");
        Console.WriteLine("F2A_S2_IDENTITY_PRESERVATION=PASS");
        Console.WriteLine("F2A_S2_NEW_SCHEMA_RELATIONSHIPS=PASS");
    }

    private static async Task ExerciseNewSchemaAsync(
        SqliteConnection connection,
        FixtureIds ids)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "Organizations"
                ("Id","Name","Status","CreatedAtUtc","Revision")
            VALUES ($organizationId,'Synthetic Organization','Active',
                    '2026-11-14T10:00:00.0000000Z',0);

            UPDATE "Locations" SET "OrganizationId"=$organizationId WHERE "Id"=$locationId;

            INSERT INTO "OrganizationMembers"
                ("Id","OrganizationId","UserId","Role","GrantedByUserId",
                 "GrantedAtUtc","RevokedByUserId","RevokedAtUtc","Revision")
            VALUES ($memberId,$organizationId,'fixture-user-1','Owner',
                    'fixture-user-1','2026-11-14T10:00:00.0000000Z',NULL,NULL,0);

            INSERT INTO "BusinessClaims"
                ("Id","LocationId","OrganizationId","ApplicantUserId","Status",
                 "SubmittedAtUtc","ReviewedByUserId","ReviewedAtUtc","DecisionReason","Revision")
            VALUES ($claimId,$locationId,$organizationId,'fixture-user-1','Pending',
                    '2026-11-14T10:00:00.0000000Z',NULL,NULL,NULL,0);

            INSERT INTO "ContentAuditEvents"
                ("Id","OrganizationId","LocationId","EntityType","EntityId","Action",
                 "ActorUserId","ActorRole","OccurredAtUtc","RevisionBefore","RevisionAfter",
                 "CorrelationId","ChangedFieldsJson","Reason")
            VALUES ($auditId,$organizationId,$locationId,'Location',$locationId,'Updated',
                    'fixture-user-1','Owner','2026-11-14T10:00:00.0000000Z',
                    0,1,'fixture-correlation','{}','Synthetic fixture');
            """;

        command.Parameters.AddWithValue("$organizationId", GuidText(ids.Organization));
        command.Parameters.AddWithValue("$locationId", GuidText(ids.Location));
        command.Parameters.AddWithValue("$memberId", GuidText(ids.Member));
        command.Parameters.AddWithValue("$claimId", GuidText(ids.Claim));
        command.Parameters.AddWithValue("$auditId", GuidText(ids.Audit));
        await command.ExecuteNonQueryAsync();

        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"Locations\" WHERE \"Id\"=$id AND \"OrganizationId\"=$organizationId;",
            ("$id", GuidText(ids.Location)),
            ("$organizationId", GuidText(ids.Organization))) == 1,
            "F2A_S2_LOCATION_ORGANIZATION_FK");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"OrganizationMembers\" WHERE \"Id\"=$id AND \"UserId\"='fixture-user-1';",
            ("$id", GuidText(ids.Member))) == 1,
            "F2A_S2_ORGANIZATION_MEMBER_FK");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"BusinessClaims\" WHERE \"Id\"=$id AND \"ApplicantUserId\"='fixture-user-1';",
            ("$id", GuidText(ids.Claim))) == 1,
            "F2A_S2_BUSINESS_CLAIM_FK");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"ContentAuditEvents\" WHERE \"Id\"=$id AND \"OrganizationId\"=$organizationId;",
            ("$id", GuidText(ids.Audit)),
            ("$organizationId", GuidText(ids.Organization))) == 1,
            "F2A_S2_CONTENT_AUDIT_FK");
    }

    private static async Task CopyDatabaseAsync(
        string sourcePath,
        string destinationPath)
    {
        await using var source = new SqliteConnection(
            ConnectionString(sourcePath, SqliteOpenMode.ReadOnly));
        await source.OpenAsync();

        await using var destination = new SqliteConnection(
            ConnectionString(destinationPath, SqliteOpenMode.ReadWriteCreate));
        await destination.OpenAsync();

        source.BackupDatabase(destination);
    }

    private static async Task ApplyF2aAsync(
        string path,
        IInterceptor? interceptor = null)
    {
        await using var connection = new SqliteConnection(
            ConnectionString(path, SqliteOpenMode.ReadWrite));
        await connection.OpenAsync();

        await using var db = new DirectoryDb(Options(connection, interceptor));
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(F2a);
    }

    private static async Task AssertInterruptedStateAsync(
        string path,
        FixtureIds ids,
        string label,
        bool expectLocationColumns,
        bool expectTempTable)
    {
        await using var connection = new SqliteConnection(
            ConnectionString(path, SqliteOpenMode.ReadWrite));
        await connection.OpenAsync();

        var hasOrganizationId = await HasColumnAsync(connection, "Locations", "OrganizationId");
        var hasRevision = await HasColumnAsync(connection, "Locations", "Revision");
        Check(hasOrganizationId == expectLocationColumns,
            label + "_ORGANIZATION_COLUMN_STATE");
        Check(hasRevision == expectLocationColumns,
            label + "_REVISION_COLUMN_STATE");
        Check(await HasTableAsync(connection, "Organizations"),
            label + "_F2A_TABLE_EXISTS");
        Check(await HasTableAsync(connection, "ef_temp_Locations") == expectTempTable,
            label + "_TEMP_TABLE_STATE");
        Check(await ScalarLongAsync(connection,
            "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\"=$id;",
            ("$id", F2a)) == 0, label + "_HISTORY_NOT_ADVANCED");

        var locationSql = expectLocationColumns
            ? """
                SELECT COUNT(*) FROM "Locations"
                WHERE "Id"=$id AND "Slug"='f2a-s2-legacy-location'
                  AND "Name"='Legacy location' AND "OrganizationId" IS NULL
                  AND "Revision"=0;
                """
            : """
                SELECT COUNT(*) FROM "Locations"
                WHERE "Id"=$id AND "Slug"='f2a-s2-legacy-location'
                  AND "Name"='Legacy location' AND "City"='Варна'
                  AND "UpdatedAtUtc"='2026-11-14T10:00:00.0000000Z';
                """;

        Check(await ScalarLongAsync(connection, locationSql,
            ("$id", GuidText(ids.Location))) == 1,
            label + "_LOCATION_ROW_SURVIVES");
        await AssertIntegrityAsync(connection, label);
    }

    private static async Task RunFaultRecoveryAsync(
        string root,
        string backupPath,
        FixtureIds ids,
        string label,
        string commandMarker,
        bool expectLocationColumns,
        bool expectTempTable)
    {
        var interruptedPath = Path.Combine(root, label + ".interrupted.db");
        var recoveredPath = Path.Combine(root, label + ".recovered.db");

        await CopyDatabaseAsync(backupPath, interruptedPath);

        var interceptor = new FailOnCommandInterceptor(commandMarker);
        var injected = false;

        try
        {
            await ApplyF2aAsync(interruptedPath, interceptor);
        }
        catch (InjectedMigrationFailureException)
        {
            injected = true;
        }

        Check(injected && interceptor.Triggered, label + "_FAILURE_INJECTED");
        await AssertInterruptedStateAsync(
            interruptedPath, ids, label.ToUpperInvariant(),
            expectLocationColumns, expectTempTable);

        // Recovery contract is deliberately restore-only. Never blindly rerun
        // a migration on a database whose command history is uncertain.
        await CopyDatabaseAsync(backupPath, recoveredPath);
        await AssertLegacyStateAsync(recoveredPath, ids, label.ToUpperInvariant());

        Console.WriteLine(label.ToUpperInvariant() + "_RESTORE_RECOVERY=PASS");
    }

    public static async Task RunAsync()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "yangspace-f2a-s2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var ids = new FixtureIds(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        try
        {
            var baselinePath = Path.Combine(root, "legacy-baseline.db");
            var backupPath = Path.Combine(root, "pre-f2a.backup.db");
            var successPath = Path.Combine(root, "successful-upgrade.db");

            await using (var connection = new SqliteConnection(
                ConnectionString(baselinePath, SqliteOpenMode.ReadWriteCreate)))
            {
                await connection.OpenAsync();
                await using var db = new DirectoryDb(Options(connection));
                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(Freshness);
                await SeedLegacyAsync(connection, ids);
            }

            await AssertLegacyStateAsync(baselinePath, ids, "F2A_S2_BASELINE");
            Console.WriteLine("F2A_S2_LEGACY_BASELINE=PASS");

            await CopyDatabaseAsync(baselinePath, backupPath);
            Check(File.Exists(backupPath) && new FileInfo(backupPath).Length > 0,
                "F2A_S2_PREUPGRADE_BACKUP_CREATED");

            await CopyDatabaseAsync(backupPath, successPath);
            await ApplyF2aAsync(successPath);
            await AssertUpgradedStateAsync(successPath, ids);
            Console.WriteLine("F2A_S2_SUCCESSFUL_UPGRADE=PASS");

            await RunFaultRecoveryAsync(
                root, backupPath, ids,
                "fault-after-f2a-ddl",
                "PRAGMA foreign_keys = 0",
                expectLocationColumns: false,
                expectTempTable: false);

            await RunFaultRecoveryAsync(
                root, backupPath, ids,
                "fault-after-locations-rebuild",
                "PRAGMA foreign_keys = 1",
                expectLocationColumns: true,
                expectTempTable: false);

            await RunFaultRecoveryAsync(
                root, backupPath, ids,
                "fault-before-migration-history",
                "INSERT INTO \"__EFMigrationsHistory\"",
                expectLocationColumns: true,
                expectTempTable: false);

            Console.WriteLine("F2A_S2_RECOVERY_ALL_BOUNDARIES=PASS");
            Console.WriteLine("F2A_S2_DISPOSABLE_UPGRADE_RECOVERY=PASS");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}
