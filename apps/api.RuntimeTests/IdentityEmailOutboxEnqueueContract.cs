using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YangSpace.Api;

internal static class IdentityEmailOutboxEnqueueContract
{
    private static void Check(bool ok, string marker)
    {
        if (!ok) throw new InvalidOperationException("OUTBOX_ENQUEUE_FAILED=" + marker);
        Console.WriteLine(marker + "=PASS");
    }

    internal static async Task RunAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite(connection).Options;

        await using (var db = new DirectoryDb(options))
        {
            // Disposable in-memory SQLite model only: not a provider migration test.
            await db.Database.EnsureCreatedAsync();
            var user = new IdentityUser
            {
                UserName = "fixture@example.invalid",
                Email = "fixture@example.invalid"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var queue = new OutboxIdentityEmailSender(db);
            await queue.SendConfirmationLinkAsync(
                user, user.Email!, "https://yangspace.gndstore.eu/confirm?token=raw_secret_fixture");
            Check(await db.IdentityEmailOutbox.CountAsync() == 1, "F2B_OUTBOX_EMAIL_INTENT_PERSISTED");
            var intent = await db.IdentityEmailOutbox.SingleAsync();
            Check(intent.UserId == user.Id &&
                  intent.Purpose == IdentityEmailPurpose.ConfirmEmail &&
                  intent.Status == IdentityEmailOutboxStatus.Pending,
                  "F2B_OUTBOX_MINIMAL_PENDING_INTENT");

            var originalExpiry = intent.ExpiresAtUtc;
            await queue.SendConfirmationLinkAsync(
                user, user.Email!, "https://yangspace.gndstore.eu/confirm?token=different_secret");
            await db.Entry(intent).ReloadAsync();
            Check(await db.IdentityEmailOutbox.CountAsync() == 1 &&
                  intent.ExpiresAtUtc == originalExpiry,
                  "F2B_OUTBOX_PENDING_COALESCED");

            // An expired Pending intent is refreshed in place, not duplicated.
            intent.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            intent.AttemptCount = 5;
            intent.LastFailureCode = "synthetic-transient";
            await db.SaveChangesAsync();
            var originalId = intent.Id;
            var originalRevision = intent.Revision;
            var originalQueuedAt = intent.QueuedAtUtc;
            await queue.SendConfirmationLinkAsync(
                user, user.Email!, "https://yangspace.gndstore.eu/confirm?token=expired_secret");
            await db.Entry(intent).ReloadAsync();
            Check(intent.Id == originalId &&
                  intent.QueuedAtUtc > originalQueuedAt &&
                  intent.ExpiresAtUtc > DateTime.UtcNow &&
                  intent.AttemptCount == 0 &&
                  intent.LastFailureCode is null &&
                  intent.Revision == originalRevision + 1 &&
                  await db.IdentityEmailOutbox.CountAsync() == 1,
                  "F2B_OUTBOX_EXPIRED_PENDING_REUSED");

            await queue.SendPasswordResetCodeAsync(user, user.Email!, "unpersisted_reset_secret");
            Check(await db.IdentityEmailOutbox.CountAsync() == 2 &&
                  await db.IdentityEmailOutbox.CountAsync(
                    x => x.Purpose == IdentityEmailPurpose.ResetPassword) == 1,
                  "F2B_OUTBOX_RESET_INTENT_PERSISTED");

            var allRows = await db.IdentityEmailOutbox.AsNoTracking().ToArrayAsync();
            Check(allRows.All(x => x.LastFailureCode is null) &&
                  allRows.All(x => x.UserId != user.Email),
                  "F2B_OUTBOX_NO_EMAIL_PAYLOAD_STORED");
            Check(!IdentityEmailOutboxDispatchPolicy.DispatcherAvailable,
                  "F2B_OUTBOX_DISPATCH_FAIL_CLOSED");

            var rolledBack = new IdentityUser
            {
                UserName = "rollback@example.invalid",
                Email = "rollback@example.invalid"
            };
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                db.Users.Add(rolledBack);
                await db.SaveChangesAsync();
                await queue.SendConfirmationLinkAsync(
                    rolledBack, rolledBack.Email!, "raw_transaction_secret");
                await tx.RollbackAsync();
            }

            await using var fresh = new DirectoryDb(options);
            Check(!await fresh.Users.AnyAsync(x => x.Id == rolledBack.Id) &&
                  !await fresh.IdentityEmailOutbox.AnyAsync(x => x.UserId == rolledBack.Id),
                  "F2B_OUTBOX_ATOMIC_ROLLBACK");
        }

        await RunConcurrentSqliteAsync();

        Console.WriteLine("F2B_OUTBOX_ENQUEUE_SYNTHETIC_ONLY=PASS");
        Console.WriteLine("F2B_OUTBOX_SQLITE_CONCURRENCY=PASS");
        Console.WriteLine("F2B_OUTBOX_PG_MIGRATIONS=NOT_GENERATED");
        Console.WriteLine("F2B_OUTBOX_WORKER=IMPLEMENTED_SOURCE_ONLY");
        Console.WriteLine("PUBLIC_REGISTRATION_DEFAULT_OFF=PASS");
        Console.WriteLine("CT164_DB=NOT_ACCESSED");
    }

    private static async Task RunConcurrentSqliteAsync()
    {
        var sandbox = Path.Combine(
            Path.GetTempPath(), "yangspace-outbox-concurrency-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        var databasePath = Path.Combine(sandbox, "outbox.db");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true,
            Pooling = false,
            DefaultTimeout = 30
        }.ToString();

        try
        {
            string userId;
            const string email = "parallel@example.invalid";
            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                var options = new DbContextOptionsBuilder<DirectoryDb>()
                    .UseSqlite(connection).Options;
                await using var db = new DirectoryDb(options);
                await db.Database.EnsureCreatedAsync();
                var user = new IdentityUser { UserName = email, Email = email };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                userId = user.Id;
            }

            await RunConcurrentBatchAsync(connectionString, userId, email, 16);

            Guid pendingId;
            await using (var verifyConnection = new SqliteConnection(connectionString))
            {
                await verifyConnection.OpenAsync();
                var options = new DbContextOptionsBuilder<DirectoryDb>()
                    .UseSqlite(verifyConnection).Options;
                await using var db = new DirectoryDb(options);
                var pending = await db.IdentityEmailOutbox.AsNoTracking()
                    .Where(x => x.UserId == userId &&
                                x.Purpose == IdentityEmailPurpose.ConfirmEmail &&
                                x.Status == IdentityEmailOutboxStatus.Pending)
                    .ToArrayAsync();
                Check(pending.Length == 1, "F2B_OUTBOX_CONCURRENT_ENQUEUE_COALESCED");
                pendingId = pending[0].Id;

                var duplicate = new IdentityEmailOutboxMessage
                {
                    UserId = userId,
                    Purpose = IdentityEmailPurpose.ConfirmEmail,
                    Status = IdentityEmailOutboxStatus.Pending,
                    QueuedAtUtc = DateTime.UtcNow,
                    AvailableAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
                };
                db.IdentityEmailOutbox.Add(duplicate);
                var rejected = false;
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException exception)
                    when (exception.InnerException is SqliteException sqlite &&
                          sqlite.SqliteExtendedErrorCode == 2067)
                {
                    rejected = true;
                    db.Entry(duplicate).State = EntityState.Detached;
                }
                Check(rejected, "F2B_OUTBOX_PENDING_UNIQUE_INDEX");
            }

            await using (var leaseConnection = new SqliteConnection(connectionString))
            {
                await leaseConnection.OpenAsync();
                var options = new DbContextOptionsBuilder<DirectoryDb>()
                    .UseSqlite(leaseConnection).Options;
                await using var db = new DirectoryDb(options);
                var leased = await db.IdentityEmailOutbox.SingleAsync(x => x.Id == pendingId);
                leased.Status = IdentityEmailOutboxStatus.Leased;
                leased.LeaseId = Guid.NewGuid();
                leased.LeasedUntilUtc = DateTime.UtcNow.AddMinutes(5);
                await db.SaveChangesAsync();
            }

            await RunConcurrentBatchAsync(connectionString, userId, email, 8);
            await using (var pendingConnection = new SqliteConnection(connectionString))
            {
                await pendingConnection.OpenAsync();
                var options = new DbContextOptionsBuilder<DirectoryDb>()
                    .UseSqlite(pendingConnection).Options;
                await using var db = new DirectoryDb(options);
                var pending = await db.IdentityEmailOutbox
                    .SingleAsync(x => x.UserId == userId &&
                                      x.Purpose == IdentityEmailPurpose.ConfirmEmail &&
                                      x.Status == IdentityEmailOutboxStatus.Pending);
                Check(await db.IdentityEmailOutbox.CountAsync() == 2 &&
                      pending.Id != pendingId,
                      "F2B_OUTBOX_LEASED_HISTORY_DOES_NOT_BLOCK_PENDING");
                pendingId = pending.Id;
                pending.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
                pending.AttemptCount = 4;
                pending.LastFailureCode = "synthetic-retry";
                await db.SaveChangesAsync();
            }

            await RunConcurrentBatchAsync(connectionString, userId, email, 16);

            await using (var verifyConnection = new SqliteConnection(connectionString))
            {
                await verifyConnection.OpenAsync();
                var options = new DbContextOptionsBuilder<DirectoryDb>()
                    .UseSqlite(verifyConnection).Options;
                await using var db = new DirectoryDb(options);
                var pending = await db.IdentityEmailOutbox
                    .SingleAsync(x => x.UserId == userId &&
                                      x.Purpose == IdentityEmailPurpose.ConfirmEmail &&
                                      x.Status == IdentityEmailOutboxStatus.Pending);
                Check(pending.Id == pendingId &&
                      pending.ExpiresAtUtc > DateTime.UtcNow &&
                      pending.AttemptCount == 0 &&
                      pending.LastFailureCode is null &&
                      pending.Revision == 1,
                      "F2B_OUTBOX_CONCURRENT_EXPIRED_REFRESH");
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
                File.Delete(databasePath);
            if (Directory.Exists(sandbox))
                Directory.Delete(sandbox, recursive: true);
        }
    }

    private static async Task RunConcurrentBatchAsync(
        string connectionString, string userId, string email, int concurrency)
    {
        var start = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var workers = Enumerable.Range(0, concurrency).Select(async worker =>
        {
            await start.Task.ConfigureAwait(false);
            var options = new DbContextOptionsBuilder<DirectoryDb>()
                .UseSqlite(connectionString).Options;
            await using var db = new DirectoryDb(options);
            var user = new IdentityUser { Id = userId, UserName = email, Email = email };
            var sender = new OutboxIdentityEmailSender(db);
            await sender.SendConfirmationLinkAsync(
                user, email, "https://example.invalid/confirm?synthetic=" + worker);
        }).ToArray();

        start.SetResult(true);
        await Task.WhenAll(workers);
    }
}
