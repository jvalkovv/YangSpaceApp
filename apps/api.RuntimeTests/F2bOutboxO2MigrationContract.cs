using System.Security.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using YangSpace.Api;

internal static class F2bOutboxO2MigrationContract
{
    private const string SqliteBaseline = "20261114183515_F2aOrganizationClaimFoundation";
    private const string SqliteTarget = "20261115101409_F2bIdentityEmailOutbox";
    private const string PostgreSqlBaseline = "20261111203833_InitialPostgreSqlSchema";
    private const string PostgreSqlTarget = "20261115101421_F2bIdentityEmailOutbox";
    private const string PostgreSqlDatabase = "yangspace_f2b_o2_disposable";
    private const string PendingIndex = "UX_IdentityEmailOutbox_PendingUserPurpose";
    private const string MigratorRole = "ys_o2_migrator";
    private const string RuntimeRole = "ys_o2_runtime";

    private static readonly string[] ExpectedColumns =
    [
        "Id", "UserId", "Purpose", "Status", "QueuedAtUtc", "AvailableAtUtc",
        "ExpiresAtUtc", "AttemptCount", "LeaseId", "LeasedUntilUtc", "SentAtUtc",
        "DeadLetterAtUtc", "LastFailureCode", "Revision"
    ];

    private static void Check(bool condition, string marker)
    {
        if (!condition)
            throw new InvalidOperationException("F2B_O2_FAILED=" + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private static DbContextOptions<DirectoryDb> SqliteOptions(string connectionString) =>
        new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite(connectionString, options =>
                options.MigrationsAssembly("YangSpace.Api"))
            .Options;

    private static DbContextOptions<DirectoryDb> PostgreSqlOptions(string connectionString) =>
        new DbContextOptionsBuilder<DirectoryDb>()
            .UseNpgsql(connectionString, options =>
                options.MigrationsAssembly("YangSpace.PostgreSql.Migrations"))
            .Options;

    private static IdentityEmailOutboxMessage NewMessage(
        string userId,
        IdentityEmailPurpose purpose,
        IdentityEmailOutboxStatus status = IdentityEmailOutboxStatus.Pending)
    {
        var now = DateTime.UtcNow;
        return new IdentityEmailOutboxMessage
        {
            UserId = userId,
            Purpose = purpose,
            Status = status,
            QueuedAtUtc = now,
            AvailableAtUtc = now,
            ExpiresAtUtc = now.AddHours(24)
        };
    }

    private static IdentityUser NewIdentityFixture(string email) => new()
    {
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        EmailConfirmed = false
    };

    private static IdentityUser SenderIdentity(IdentityUser source) => new()
    {
        Id = source.Id,
        UserName = source.UserName,
        Email = source.Email
    };

    private static SqliteException? FindSqliteException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is SqliteException sqlite)
                return sqlite;
        }

        return null;
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException postgres)
                return postgres;
        }

        return null;
    }

    private static async Task<bool> SqliteTableExistsAsync(string connectionString)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'IdentityEmailOutbox';";
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task VerifySqliteSchemaAsync(string connectionString)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_keys;";
            Check(Convert.ToInt32(await command.ExecuteScalarAsync()) == 1,
                "F2B_O2_SQLITE_FOREIGN_KEYS_ENABLED");
        }

        var columns = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info('IdentityEmailOutbox');";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                columns.Add(reader.GetString(1));
        }

        Check(columns.SequenceEqual(ExpectedColumns, StringComparer.Ordinal),
            "F2B_O2_SQLITE_EXACT_OUTBOX_COLUMNS");

        var indexFound = false;
        var indexNames = new HashSet<string>(StringComparer.Ordinal);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA index_list('IdentityEmailOutbox');";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var indexName = reader.GetString(1);
                indexNames.Add(indexName);
                if (!string.Equals(indexName, PendingIndex, StringComparison.Ordinal))
                    continue;

                indexFound = reader.GetInt32(2) == 1 && reader.GetInt32(4) == 1;
            }
        }

        Check(indexFound, "F2B_O2_SQLITE_PENDING_INDEX_UNIQUE_PARTIAL");
        Check(indexNames.Contains("IX_IdentityEmailOutbox_Status_AvailableAtUtc") &&
              indexNames.Contains("IX_IdentityEmailOutbox_UserId_Purpose_Status"),
            "F2B_O2_SQLITE_ORDINARY_INDEXES");

        var indexColumns = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA index_info('{PendingIndex}');";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                indexColumns.Add(reader.GetString(2));
        }

        Check(indexColumns.SequenceEqual(["UserId", "Purpose"], StringComparer.Ordinal),
            "F2B_O2_SQLITE_PENDING_INDEX_COLUMNS");

        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = $name;";
            command.Parameters.AddWithValue("$name", PendingIndex);
            var sql = Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
            Check(sql.Contains("WHERE \"Status\" = 'Pending'", StringComparison.OrdinalIgnoreCase),
                "F2B_O2_SQLITE_PENDING_INDEX_FILTER");
        }

        var foreignKeyFound = false;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_key_list('IdentityEmailOutbox');";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                foreignKeyFound |=
                    reader.GetString(2) == "AspNetUsers" &&
                    reader.GetString(3) == "UserId" &&
                    reader.GetString(4) == "Id" &&
                    reader.GetString(6).Equals("CASCADE", StringComparison.OrdinalIgnoreCase);
            }
        }

        Check(foreignKeyFound, "F2B_O2_SQLITE_USER_FK_CASCADE");

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA integrity_check;";
            Check(string.Equals(
                    Convert.ToString(await command.ExecuteScalarAsync()),
                    "ok",
                    StringComparison.OrdinalIgnoreCase),
                "F2B_O2_SQLITE_INTEGRITY_CHECK");
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await command.ExecuteReaderAsync();
            Check(!await reader.ReadAsync(), "F2B_O2_SQLITE_FOREIGN_KEY_CHECK");
        }
    }

    private static async Task VerifySqliteConstraintBehaviorAsync(
        string connectionString,
        string userId)
    {
        await using var db = new DirectoryDb(SqliteOptions(connectionString));

        var pending = NewMessage(userId, IdentityEmailPurpose.ConfirmEmail);
        db.IdentityEmailOutbox.Add(pending);
        await db.SaveChangesAsync();

        var duplicate = NewMessage(userId, IdentityEmailPurpose.ConfirmEmail);
        db.IdentityEmailOutbox.Add(duplicate);
        var duplicateRejected = false;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (FindSqliteException(exception)?.SqliteExtendedErrorCode == 2067)
        {
            duplicateRejected = true;
            db.Entry(duplicate).State = EntityState.Detached;
        }

        Check(duplicateRejected, "F2B_O2_SQLITE_DUPLICATE_PENDING_REJECTED");

        var leasedHistory = NewMessage(
            userId, IdentityEmailPurpose.ConfirmEmail, IdentityEmailOutboxStatus.Leased);
        leasedHistory.LeaseId = Guid.NewGuid();
        leasedHistory.LeasedUntilUtc = DateTime.UtcNow.AddMinutes(5);
        db.IdentityEmailOutbox.Add(leasedHistory);
        await db.SaveChangesAsync();

        var reset = NewMessage(userId, IdentityEmailPurpose.ResetPassword);
        db.IdentityEmailOutbox.Add(reset);
        await db.SaveChangesAsync();

        Check(await db.IdentityEmailOutbox.CountAsync() == 3,
            "F2B_O2_SQLITE_LEASED_HISTORY_AND_DISTINCT_PURPOSE_ALLOWED");

        var orphan = NewMessage("missing-user-" + Guid.NewGuid().ToString("N"),
            IdentityEmailPurpose.ConfirmEmail);
        db.IdentityEmailOutbox.Add(orphan);
        var orphanRejected = false;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (FindSqliteException(exception)?.SqliteExtendedErrorCode == 787)
        {
            orphanRejected = true;
            db.Entry(orphan).State = EntityState.Detached;
        }

        Check(orphanRejected, "F2B_O2_SQLITE_ORPHAN_FK_REJECTED");
        Check(!IdentityEmailOutboxDispatchPolicy.DispatcherAvailable,
            "F2B_O2_DISPATCHER_FAIL_CLOSED");
    }

    private static async Task<IdentityUser> CreateSqliteUserAsync(
        DbContextOptions<DirectoryDb> options)
    {
        var email = "f2b-o2-sqlite-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        await using var db = new DirectoryDb(options);
        var user = NewIdentityFixture(email);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return SenderIdentity(user);
    }

    private static async Task RunConcurrentBatchAsync(
        DbContextOptions<DirectoryDb> options,
        IdentityUser user,
        int concurrency)
    {
        var start = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var workers = Enumerable.Range(0, concurrency).Select(async worker =>
        {
            await start.Task.ConfigureAwait(false);
            await using var db = new DirectoryDb(options);
            var sender = new OutboxIdentityEmailSender(db);
            var senderUser = SenderIdentity(user);
            await sender.SendConfirmationLinkAsync(
                senderUser,
                user.Email!,
                "https://example.invalid/f2b-o2/synthetic?request=" + worker);
        }).ToArray();

        start.SetResult(true);
        await Task.WhenAll(workers);
    }

    private static async Task RunConcurrencyContractAsync(
        DbContextOptions<DirectoryDb> options,
        IdentityUser user,
        string provider)
    {
        await RunConcurrentBatchAsync(options, user, 16);

        Guid firstPendingId;
        await using (var db = new DirectoryDb(options))
        {
            var pending = await db.IdentityEmailOutbox.AsNoTracking()
                .Where(message =>
                    message.UserId == user.Id &&
                    message.Purpose == IdentityEmailPurpose.ConfirmEmail &&
                    message.Status == IdentityEmailOutboxStatus.Pending)
                .ToArrayAsync();

            Check(pending.Length == 1, provider + "_CONCURRENT_ENQUEUE_SINGLE_PENDING");
            firstPendingId = pending[0].Id;
        }

        await using (var db = new DirectoryDb(options))
        {
            var pending = await db.IdentityEmailOutbox.SingleAsync(message =>
                message.Id == firstPendingId);
            pending.Status = IdentityEmailOutboxStatus.Leased;
            pending.LeaseId = Guid.NewGuid();
            pending.LeasedUntilUtc = DateTime.UtcNow.AddMinutes(5);
            await db.SaveChangesAsync();
        }

        await RunConcurrentBatchAsync(options, user, 8);

        Guid refreshedPendingId;
        await using (var db = new DirectoryDb(options))
        {
            var rows = await db.IdentityEmailOutbox.AsNoTracking()
                .Where(message =>
                    message.UserId == user.Id &&
                    message.Purpose == IdentityEmailPurpose.ConfirmEmail)
                .ToArrayAsync();

            var pending = rows.Single(message =>
                message.Status == IdentityEmailOutboxStatus.Pending);
            var leased = rows.Single(message =>
                message.Status == IdentityEmailOutboxStatus.Leased);

            Check(rows.Length == 2 && leased.Id == firstPendingId,
                provider + "_LEASED_HISTORY_PRESERVED");
            refreshedPendingId = pending.Id;
        }

        long revisionBeforeRefresh;
        DateTime queuedBeforeRefresh;
        await using (var db = new DirectoryDb(options))
        {
            var pending = await db.IdentityEmailOutbox.SingleAsync(message =>
                message.Id == refreshedPendingId);
            pending.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            pending.AttemptCount = 5;
            pending.LastFailureCode = "synthetic-retry";
            revisionBeforeRefresh = pending.Revision;
            queuedBeforeRefresh = pending.QueuedAtUtc;
            await db.SaveChangesAsync();
        }

        await RunConcurrentBatchAsync(options, user, 16);

        await using (var db = new DirectoryDb(options))
        {
            var pending = await db.IdentityEmailOutbox.SingleAsync(message =>
                message.Id == refreshedPendingId);

            Check(pending.ExpiresAtUtc > DateTime.UtcNow &&
                  pending.AttemptCount == 0 &&
                  pending.LastFailureCode is null &&
                  pending.Revision == revisionBeforeRefresh + 1 &&
                  pending.QueuedAtUtc > queuedBeforeRefresh,
                provider + "_CONCURRENT_EXPIRED_PENDING_REFRESH_ONCE");

            Check(await db.IdentityEmailOutbox.CountAsync(message =>
                    message.UserId == user.Id &&
                    message.Purpose == IdentityEmailPurpose.ConfirmEmail &&
                    message.Status == IdentityEmailOutboxStatus.Pending) == 1,
                provider + "_CONCURRENT_PENDING_CARDINALITY");
        }
    }

    public static async Task RunSqliteAsync()
    {
        if (Environment.GetEnvironmentVariable("YANGSPACE_F2B_O2_SQLITE_ONLY") !=
            "CONFIRMED_DISPOSABLE_ONLY")
            throw new InvalidOperationException("F2B_O2_SQLITE_SCOPE_NOT_CONFIRMED");

        var suppliedPath = Environment.GetEnvironmentVariable("YANGSPACE_F2B_O2_SQLITE_PATH");
        if (string.IsNullOrWhiteSpace(suppliedPath) || !Path.IsPathFullyQualified(suppliedPath))
            throw new InvalidOperationException("F2B_O2_SQLITE_PATH_NOT_ABSOLUTE");

        var databasePath = Path.GetFullPath(suppliedPath);
        var evidenceRoot = Path.GetFullPath(Path.Combine(
            Environment.CurrentDirectory, "logs", "yangspace", "f2b-o2")) +
            Path.DirectorySeparatorChar;
        if (!databasePath.StartsWith(evidenceRoot, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(databasePath).StartsWith("yangspace-f2b-o2-", StringComparison.Ordinal) ||
            File.Exists(databasePath))
            throw new InvalidOperationException("F2B_O2_SQLITE_PATH_NOT_FRESH_DISPOSABLE");
        Console.WriteLine("F2B_O2_SQLITE_PATH_EVIDENCE_SCOPED=PASS");

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Pooling = false,
            DefaultTimeout = 60
        }.ToString();

        try
        {
            var options = SqliteOptions(connectionString);
            string userId;
            await using (var db = new DirectoryDb(options))
            {
                var migrations = db.Database.GetMigrations().ToArray();
                Check(migrations.Contains(SqliteBaseline) && migrations.Contains(SqliteTarget),
                    "F2B_O2_SQLITE_MIGRATION_IDS_PRESENT");

                var migrator = db.GetService<IMigrator>();
                await migrator.MigrateAsync(SqliteBaseline);

                var baselineApplied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                Check(baselineApplied.Contains(SqliteBaseline) &&
                      !baselineApplied.Contains(SqliteTarget),
                    "F2B_O2_SQLITE_BASELINE_APPLIED");
                Check(!await SqliteTableExistsAsync(connectionString),
                    "F2B_O2_SQLITE_OUTBOX_ABSENT_AT_BASELINE");

                var email = "f2b-o2-sqlite-migration-" +
                            Guid.NewGuid().ToString("N") + "@example.invalid";
                var user = NewIdentityFixture(email);
                db.Users.Add(user);
                await db.SaveChangesAsync();
                userId = user.Id;

                await migrator.MigrateAsync(SqliteTarget);
                var firstApplied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                Check(firstApplied.Contains(SqliteTarget),
                    "F2B_O2_SQLITE_MIGRATION_APPLY");
                Check(await SqliteTableExistsAsync(connectionString),
                    "F2B_O2_SQLITE_OUTBOX_CREATED");

                await VerifySqliteSchemaAsync(connectionString);

                db.IdentityEmailOutbox.Add(NewMessage(userId, IdentityEmailPurpose.ConfirmEmail));
                await db.SaveChangesAsync();
                Check(await db.IdentityEmailOutbox.AnyAsync(message => message.UserId == userId),
                    "F2B_O2_SQLITE_SYNTHETIC_ROW_INSERTED_FOR_ROLLBACK");

                db.ChangeTracker.Clear();
                await migrator.MigrateAsync(SqliteBaseline);

                var rolledBack = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                Check(rolledBack.Contains(SqliteBaseline) &&
                      !rolledBack.Contains(SqliteTarget),
                    "F2B_O2_SQLITE_ROLLBACK_TO_PINNED_BASELINE");
                Check(!await SqliteTableExistsAsync(connectionString),
                    "F2B_O2_SQLITE_DOWN_REMOVED_ONLY_OUTBOX_TABLE");
                Check(await db.Users.AnyAsync(identity => identity.Id == userId),
                    "F2B_O2_SQLITE_BASELINE_IDENTITY_PRESERVED");

                await migrator.MigrateAsync(SqliteTarget);
                var reapplied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                Check(reapplied.Contains(SqliteTarget) &&
                      await SqliteTableExistsAsync(connectionString),
                    "F2B_O2_SQLITE_REAPPLY");

                db.ChangeTracker.Clear();
                await VerifySqliteSchemaAsync(connectionString);
            }

            await VerifySqliteConstraintBehaviorAsync(connectionString, userId);
            var concurrencyUser = await CreateSqliteUserAsync(options);
            await RunConcurrencyContractAsync(options, concurrencyUser, "F2B_O2_SQLITE");
            await VerifySqliteSchemaAsync(connectionString);

            Console.WriteLine("F2B_O2_SQLITE_APPLY_ROLLBACK_REAPPLY=PASS");
            Console.WriteLine("F2B_O2_SQLITE_INTEGRITY=PASS");
            Console.WriteLine("F2B_O2_SQLITE_CONCURRENCY=PASS");
            Console.WriteLine("F2B_O2_SQLITE_DISPOSABLE_ONLY=PASS");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var path = databasePath + suffix;
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }

    private static async Task VerifyPostgreSqlTlsAsync(string connectionString, string marker)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT ssl, version FROM pg_stat_ssl WHERE pid = pg_backend_pid();";
        await using var reader = await command.ExecuteReaderAsync();
        Check(await reader.ReadAsync() && reader.GetBoolean(0) &&
              !string.IsNullOrWhiteSpace(reader.GetString(1)), marker);
    }

    private static bool IsTlsValidationFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is AuthenticationException)
                return true;

            if (current.Message.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("TLS", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("RemoteCertificate", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static async Task ExpectTlsFailureAsync(
        string connectionString,
        string marker)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
        }
        catch (Exception exception) when (IsTlsValidationFailure(exception))
        {
            Check(true, marker);
            return;
        }

        throw new InvalidOperationException("F2B_O2_TLS_VALIDATION_NOT_ENFORCED=" + marker);
    }

    private static async Task VerifyPostgreSqlScopeTokenAsync(
        NpgsqlConnection connection,
        string token)
    {
        await using (var identityCommand = connection.CreateCommand())
        {
            identityCommand.CommandText =
                "SELECT current_database(), current_user, current_setting('server_version_num')::integer;";
            await using var reader = await identityCommand.ExecuteReaderAsync();
            Check(await reader.ReadAsync() &&
                  reader.GetString(0) == PostgreSqlDatabase &&
                  reader.GetString(1) == MigratorRole &&
                  reader.GetInt32(2) >= 170000 &&
                  reader.GetInt32(2) < 180000,
                "F2B_O2_PG_SERVER_VERSION_17_AND_DB_SCOPE");
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT scope_token FROM public.yangspace_f2b_o2_qa_guard LIMIT 1;";
        var actual = Convert.ToString(await command.ExecuteScalarAsync());
        Check(string.Equals(actual, token, StringComparison.Ordinal),
            "F2B_O2_PG_DISPOSABLE_SENTINEL_MATCHED");
    }

    private static async Task VerifyPostgreSqlRolesAsync(
        string migratorConnectionString,
        string migratorName,
        string runtimeName)
    {
        await using var connection = new NpgsqlConnection(migratorConnectionString);
        await connection.OpenAsync();

        foreach (var roleName in new[] { migratorName, runtimeName })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT rolsuper, rolcreatedb, rolcreaterole, rolreplication, rolbypassrls, rolcanlogin
FROM pg_roles
WHERE rolname = @role;";
            command.Parameters.AddWithValue("role", roleName);
            await using var reader = await command.ExecuteReaderAsync();
            Check(await reader.ReadAsync() &&
                  !reader.GetBoolean(0) &&
                  !reader.GetBoolean(1) &&
                  !reader.GetBoolean(2) &&
                  !reader.GetBoolean(3) &&
                  !reader.GetBoolean(4) &&
                  reader.GetBoolean(5),
                roleName == migratorName
                    ? "F2B_O2_PG_MIGRATOR_NON_SUPERUSER"
                    : "F2B_O2_PG_RUNTIME_NON_SUPERUSER");
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT
    has_database_privilege(@runtime, current_database(), 'CREATE'),
    has_database_privilege(@runtime, current_database(), 'TEMP'),
    has_schema_privilege(@runtime, 'public', 'CREATE'),
    has_table_privilege(@runtime, 'public.""IdentityEmailOutbox""', 'SELECT'),
    has_table_privilege(@runtime, 'public.""IdentityEmailOutbox""', 'INSERT'),
    has_table_privilege(@runtime, 'public.""IdentityEmailOutbox""', 'UPDATE'),
    has_table_privilege(@runtime, 'public.""IdentityEmailOutbox""', 'DELETE'),
    has_table_privilege(@runtime, 'public.""IdentityEmailOutbox""', 'TRUNCATE'),
    has_table_privilege(@runtime, 'public.""IdentityEmailOutbox""', 'REFERENCES'),
    has_table_privilege(@runtime, 'public.""IdentityEmailOutbox""', 'TRIGGER'),
    has_table_privilege(@runtime, 'public.""AspNetUsers""', 'SELECT');";
            command.Parameters.AddWithValue("runtime", runtimeName);
            await using var reader = await command.ExecuteReaderAsync();
            Check(await reader.ReadAsync() &&
                  !reader.GetBoolean(0) &&
                  !reader.GetBoolean(1) &&
                  !reader.GetBoolean(2) &&
                  reader.GetBoolean(3) &&
                  reader.GetBoolean(4) &&
                  reader.GetBoolean(5) &&
                  reader.GetBoolean(6) &&
                  !reader.GetBoolean(7) &&
                  !reader.GetBoolean(8) &&
                  !reader.GetBoolean(9) &&
                  !reader.GetBoolean(10),
                "F2B_O2_PG_RUNTIME_DML_ONLY_OUTBOX");
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                "SELECT has_schema_privilege(@migrator, 'public', 'CREATE');";
            command.Parameters.AddWithValue("migrator", migratorName);
            Check(Convert.ToBoolean(await command.ExecuteScalarAsync()),
                "F2B_O2_PG_MIGRATOR_SCHEMA_CREATE_ONLY");
        }
    }

    private static async Task ExpectRuntimePrivilegeDeniedAsync(
        string runtimeConnectionString,
        string sql,
        string marker)
    {
        await using var connection = new NpgsqlConnection(runtimeConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var denied = false;
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        {
            denied = true;
        }

        Check(denied, marker);
    }

    private static async Task VerifyPostgreSqlSchemaAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var columns = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'IdentityEmailOutbox'
ORDER BY ordinal_position;";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                columns.Add(reader.GetString(0));
        }

        Check(columns.SequenceEqual(ExpectedColumns, StringComparer.Ordinal),
            "F2B_O2_PG_EXACT_OUTBOX_COLUMNS");

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT i.indisunique, i.indisvalid,
       COALESCE(pg_get_expr(i.indpred, i.indrelid), ''),
       pg_get_indexdef(i.indexrelid)
FROM pg_index i
JOIN pg_class c ON c.oid = i.indexrelid
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'public' AND c.relname = @index;";
            command.Parameters.AddWithValue("index", PendingIndex);
            await using var reader = await command.ExecuteReaderAsync();
            Check(await reader.ReadAsync() &&
                  reader.GetBoolean(0) &&
                  reader.GetBoolean(1) &&
                  reader.GetString(2).Contains("Pending", StringComparison.Ordinal) &&
                  reader.GetString(3).Contains("\"UserId\", \"Purpose\"", StringComparison.Ordinal),
                "F2B_O2_PG_PENDING_INDEX_UNIQUE_PARTIAL");
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT COUNT(*)
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename = 'IdentityEmailOutbox'
  AND indexname IN (
      'IX_IdentityEmailOutbox_Status_AvailableAtUtc',
      'IX_IdentityEmailOutbox_UserId_Purpose_Status'
  );";
            Check(Convert.ToInt32(await command.ExecuteScalarAsync()) == 2,
                "F2B_O2_PG_ORDINARY_INDEXES");
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT convalidated, confdeltype::text, pg_get_constraintdef(oid)
FROM pg_constraint
WHERE conrelid = 'public.""IdentityEmailOutbox""'::regclass
  AND conname = 'FK_IdentityEmailOutbox_AspNetUsers_UserId';";
            await using var reader = await command.ExecuteReaderAsync();
            Check(await reader.ReadAsync() &&
                  reader.GetBoolean(0) &&
                  reader.GetString(1) == "c" &&
                  reader.GetString(2).Contains("FOREIGN KEY (\"UserId\")", StringComparison.Ordinal) &&
                  reader.GetString(2).Contains("\"AspNetUsers\"(\"Id\")", StringComparison.Ordinal) &&
                  reader.GetString(2).Contains("ON DELETE CASCADE",
                      StringComparison.Ordinal),
                "F2B_O2_PG_USER_FK_CASCADE_VALIDATED");
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT COUNT(*)
FROM pg_constraint
WHERE conrelid = 'public.""IdentityEmailOutbox""'::regclass
  AND NOT convalidated;";
            Check(Convert.ToInt32(await command.ExecuteScalarAsync()) == 0,
                "F2B_O2_PG_NO_UNVALIDATED_CONSTRAINTS");
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT COUNT(*)
FROM public.""IdentityEmailOutbox"" o
LEFT JOIN public.""AspNetUsers"" u ON u.""Id"" = o.""UserId""
WHERE u.""Id"" IS NULL;";
            Check(Convert.ToInt32(await command.ExecuteScalarAsync()) == 0,
                "F2B_O2_PG_NO_ORPHAN_OUTBOX_ROWS");
        }
    }

    private static async Task VerifyPostgreSqlConstraintBehaviorAsync(
        string connectionString,
        string userId)
    {
        await using var db = new DirectoryDb(PostgreSqlOptions(connectionString));

        var pending = NewMessage(userId, IdentityEmailPurpose.ConfirmEmail);
        db.IdentityEmailOutbox.Add(pending);
        await db.SaveChangesAsync();

        var duplicate = NewMessage(userId, IdentityEmailPurpose.ConfirmEmail);
        db.IdentityEmailOutbox.Add(duplicate);
        var duplicateRejected = false;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (FindPostgresException(exception) is
                  { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: PendingIndex })
        {
            duplicateRejected = true;
            db.Entry(duplicate).State = EntityState.Detached;
        }

        Check(duplicateRejected, "F2B_O2_PG_DUPLICATE_PENDING_REJECTED");

        var leased = NewMessage(
            userId, IdentityEmailPurpose.ConfirmEmail, IdentityEmailOutboxStatus.Leased);
        leased.LeaseId = Guid.NewGuid();
        leased.LeasedUntilUtc = DateTime.UtcNow.AddMinutes(5);
        db.IdentityEmailOutbox.Add(leased);
        await db.SaveChangesAsync();

        db.IdentityEmailOutbox.Add(NewMessage(userId, IdentityEmailPurpose.ResetPassword));
        await db.SaveChangesAsync();

        Check(await db.IdentityEmailOutbox.CountAsync() == 3,
            "F2B_O2_PG_LEASED_HISTORY_AND_DISTINCT_PURPOSE_ALLOWED");

        var orphan = NewMessage("missing-user-" + Guid.NewGuid().ToString("N"),
            IdentityEmailPurpose.ConfirmEmail);
        db.IdentityEmailOutbox.Add(orphan);
        var orphanRejected = false;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (FindPostgresException(exception)?.SqlState ==
                  PostgresErrorCodes.ForeignKeyViolation)
        {
            orphanRejected = true;
            db.Entry(orphan).State = EntityState.Detached;
        }

        Check(orphanRejected, "F2B_O2_PG_ORPHAN_FK_REJECTED");
    }

    private static async Task<IdentityUser> CreatePostgreSqlUserAsync(
        DbContextOptions<DirectoryDb> options)
    {
        var email = "f2b-o2-pg-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        await using var db = new DirectoryDb(options);
        var user = NewIdentityFixture(email);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return SenderIdentity(user);
    }

    private static async Task GrantRuntimeOutboxDmlAsync(string migratorConnectionString)
    {
        await using var connection = new NpgsqlConnection(migratorConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.\"IdentityEmailOutbox\" TO ys_o2_runtime;";
        await command.ExecuteNonQueryAsync();
    }

    public static async Task RunPostgreSqlAsync()
    {
        if (Environment.GetEnvironmentVariable("YANGSPACE_F2B_O2_PG_ONLY") !=
            "CONFIRMED_DISPOSABLE_ONLY")
            throw new InvalidOperationException("F2B_O2_PG_SCOPE_NOT_CONFIRMED");

        var migratorConnectionString =
            Environment.GetEnvironmentVariable("YANGSPACE_F2B_O2_PG_MIGRATOR_CONNECTION");
        var runtimeConnectionString =
            Environment.GetEnvironmentVariable("YANGSPACE_F2B_O2_PG_RUNTIME_CONNECTION");
        var scopeToken = Environment.GetEnvironmentVariable("YANGSPACE_F2B_O2_PG_SCOPE_TOKEN");
        var wrongCaPath = Environment.GetEnvironmentVariable("YANGSPACE_F2B_O2_PG_WRONG_CA_PATH");

        if (string.IsNullOrWhiteSpace(migratorConnectionString) ||
            string.IsNullOrWhiteSpace(runtimeConnectionString) ||
            string.IsNullOrWhiteSpace(scopeToken) ||
            string.IsNullOrWhiteSpace(wrongCaPath) ||
            !Path.IsPathFullyQualified(wrongCaPath) ||
            !File.Exists(wrongCaPath))
            throw new InvalidOperationException("F2B_O2_PG_TEST_INPUTS_MISSING");

        var migratorBuilder = new NpgsqlConnectionStringBuilder(migratorConnectionString);
        var runtimeBuilder = new NpgsqlConnectionStringBuilder(runtimeConnectionString);

        Check(migratorBuilder.Host == "localhost" &&
              migratorBuilder.Database == "yangspace_f2b_o2_disposable" &&
              migratorBuilder.Username == MigratorRole &&
              migratorBuilder.SslMode == SslMode.VerifyFull &&
              migratorBuilder.RootCertificate is not null &&
              Path.IsPathFullyQualified(migratorBuilder.RootCertificate) &&
              File.Exists(migratorBuilder.RootCertificate) &&
              !string.IsNullOrWhiteSpace(migratorBuilder.Password) &&
              !migratorBuilder.Pooling &&
              !migratorBuilder.IncludeErrorDetail,
            "F2B_O2_PG_MIGRATOR_VERIFYFULL_CONNECTION_POLICY");

        Check(runtimeBuilder.Host == "localhost" &&
              runtimeBuilder.Database == migratorBuilder.Database &&
              runtimeBuilder.Port == migratorBuilder.Port &&
              runtimeBuilder.Username == RuntimeRole &&
              runtimeBuilder.SslMode == SslMode.VerifyFull &&
              runtimeBuilder.RootCertificate == migratorBuilder.RootCertificate &&
              !string.IsNullOrWhiteSpace(runtimeBuilder.Password) &&
              !runtimeBuilder.Pooling &&
              !runtimeBuilder.IncludeErrorDetail,
            "F2B_O2_PG_RUNTIME_VERIFYFULL_CONNECTION_POLICY");

        ProductionPostgreSqlPolicy.RequirePreparedConnection(migratorConnectionString);
        ProductionPostgreSqlPolicy.RequirePreparedConnection(runtimeConnectionString);

        await VerifyPostgreSqlTlsAsync(
            migratorConnectionString, "F2B_O2_PG_MIGRATOR_TLS_ACTIVE");
        await VerifyPostgreSqlTlsAsync(
            runtimeConnectionString, "F2B_O2_PG_RUNTIME_TLS_ACTIVE");

        var wrongHost = new NpgsqlConnectionStringBuilder(migratorConnectionString)
        {
            Host = "127.0.0.1"
        };
        await ExpectTlsFailureAsync(
            wrongHost.ConnectionString, "F2B_O2_PG_VERIFYFULL_HOSTNAME_MISMATCH_REJECTED");

        var wrongCa = new NpgsqlConnectionStringBuilder(migratorConnectionString)
        {
            RootCertificate = wrongCaPath
        };
        await ExpectTlsFailureAsync(
            wrongCa.ConnectionString, "F2B_O2_PG_VERIFYFULL_UNTRUSTED_CA_REJECTED");

        await using (var sentinelConnection = new NpgsqlConnection(migratorConnectionString))
        {
            await sentinelConnection.OpenAsync();
            await VerifyPostgreSqlScopeTokenAsync(sentinelConnection, scopeToken);
        }

        var options = PostgreSqlOptions(migratorConnectionString);
        string userId;

        await using (var db = new DirectoryDb(options))
        {
            var migrations = db.Database.GetMigrations().ToArray();
            Check(migrations.Contains(PostgreSqlBaseline) &&
                  migrations.Contains(PostgreSqlTarget),
                "F2B_O2_PG_MIGRATION_IDS_PRESENT");

            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(PostgreSqlBaseline);

            var baselineApplied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            Check(baselineApplied.Contains(PostgreSqlBaseline) &&
                  !baselineApplied.Contains(PostgreSqlTarget),
                "F2B_O2_PG_BASELINE_APPLIED");

            await using (var baselineConnection = new NpgsqlConnection(migratorConnectionString))
            {
                await baselineConnection.OpenAsync();
                await using var baselineCommand = baselineConnection.CreateCommand();
                baselineCommand.CommandText =
                    "SELECT to_regclass('public.\"IdentityEmailOutbox\"') IS NULL;";
                Check(Convert.ToBoolean(await baselineCommand.ExecuteScalarAsync()),
                    "F2B_O2_PG_OUTBOX_ABSENT_AT_BASELINE");
            }

            var email = "f2b-o2-pg-migration-" +
                        Guid.NewGuid().ToString("N") + "@example.invalid";
            var user = NewIdentityFixture(email);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;

            await migrator.MigrateAsync(PostgreSqlTarget);
            var firstApplied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            Check(firstApplied.Contains(PostgreSqlTarget),
                "F2B_O2_PG_MIGRATION_APPLY");

            await VerifyPostgreSqlSchemaAsync(migratorConnectionString);
            await VerifyPostgreSqlConstraintBehaviorAsync(migratorConnectionString, userId);

            db.ChangeTracker.Clear();
            await migrator.MigrateAsync(PostgreSqlBaseline);

            var rolledBack = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            Check(rolledBack.Contains(PostgreSqlBaseline) &&
                  !rolledBack.Contains(PostgreSqlTarget),
                "F2B_O2_PG_ROLLBACK_TO_PINNED_BASELINE");

            await using (var connection = new NpgsqlConnection(migratorConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT to_regclass('public.\"IdentityEmailOutbox\"') IS NULL;";
                Check(Convert.ToBoolean(await command.ExecuteScalarAsync()),
                    "F2B_O2_PG_DOWN_REMOVED_ONLY_OUTBOX_TABLE");
            }

            Check(await db.Users.AnyAsync(identity => identity.Id == userId),
                "F2B_O2_PG_BASELINE_IDENTITY_PRESERVED");

            await migrator.MigrateAsync(PostgreSqlTarget);
            var reapplied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            Check(reapplied.Contains(PostgreSqlTarget),
                "F2B_O2_PG_REAPPLY");
        }

        await VerifyPostgreSqlSchemaAsync(migratorConnectionString);
        await VerifyPostgreSqlConstraintBehaviorAsync(migratorConnectionString, userId);
        await GrantRuntimeOutboxDmlAsync(migratorConnectionString);
        await VerifyPostgreSqlRolesAsync(
            migratorConnectionString, MigratorRole, RuntimeRole);

        await ExpectRuntimePrivilegeDeniedAsync(
            runtimeConnectionString,
            "CREATE TABLE public.f2b_o2_runtime_ddl_probe(id integer);",
            "F2B_O2_PG_RUNTIME_CREATE_DENIED");

        await ExpectRuntimePrivilegeDeniedAsync(
            runtimeConnectionString,
            "SELECT COUNT(*) FROM public.\"AspNetUsers\";",
            "F2B_O2_PG_RUNTIME_IDENTITY_READ_DENIED");

        await ExpectRuntimePrivilegeDeniedAsync(
            runtimeConnectionString,
            "TRUNCATE TABLE public.\"IdentityEmailOutbox\";",
            "F2B_O2_PG_RUNTIME_TRUNCATE_DENIED");

        var runtimeOptions = PostgreSqlOptions(runtimeConnectionString);
        var concurrencyUser = await CreatePostgreSqlUserAsync(options);
        await RunConcurrencyContractAsync(
            runtimeOptions, concurrencyUser, "F2B_O2_PG");

        await using (var runtimeDb = new DirectoryDb(runtimeOptions))
        {
            var sender = new OutboxIdentityEmailSender(runtimeDb);
            await sender.SendPasswordResetCodeAsync(
                SenderIdentity(concurrencyUser),
                concurrencyUser.Email!,
                "synthetic-reset-code-not-persisted");

            var deleted = await runtimeDb.IdentityEmailOutbox
                .Where(message =>
                    message.UserId == concurrencyUser.Id &&
                    message.Purpose == IdentityEmailPurpose.ResetPassword)
                .ExecuteDeleteAsync();

            Check(deleted == 1, "F2B_O2_PG_RUNTIME_DELETE_ALLOWED");
        }

        await VerifyPostgreSqlSchemaAsync(migratorConnectionString);

        await using (var connection = new NpgsqlConnection(migratorConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT COUNT(*)
FROM pg_constraint
WHERE conrelid = 'public.""IdentityEmailOutbox""'::regclass
  AND NOT convalidated;";
            Check(Convert.ToInt32(await command.ExecuteScalarAsync()) == 0,
                "F2B_O2_PG_FINAL_CONSTRAINT_INTEGRITY");
        }

        Check(!IdentityEmailOutboxDispatchPolicy.DispatcherAvailable,
            "F2B_O2_DISPATCHER_FAIL_CLOSED");
        Console.WriteLine("F2B_O2_PG_APPLY_ROLLBACK_REAPPLY=PASS");
        Console.WriteLine("F2B_O2_PG_INTEGRITY=PASS");
        Console.WriteLine("F2B_O2_PG_CONCURRENCY=PASS");
        Console.WriteLine("F2B_O2_PG_TLS_VERIFYFULL=PASS");
        Console.WriteLine("F2B_O2_PG_LEAST_PRIVILEGE=PASS");
        Console.WriteLine("F2B_O2_PG_DISPOSABLE_ONLY=PASS");
    }
}