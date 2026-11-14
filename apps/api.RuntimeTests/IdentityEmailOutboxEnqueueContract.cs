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

        // Disposable in-memory SQLite model only: not a provider migration test.
        await using (var db = new DirectoryDb(options))
        {
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

            await queue.SendConfirmationLinkAsync(
                user, user.Email!, "https://yangspace.gndstore.eu/confirm?token=different_secret");
            Check(await db.IdentityEmailOutbox.CountAsync() == 1,
                  "F2B_OUTBOX_PENDING_COALESCED");

            await queue.SendPasswordResetCodeAsync(user, user.Email!, "unpersisted_reset_secret");
            Check(await db.IdentityEmailOutbox.CountAsync() == 2 &&
                  await db.IdentityEmailOutbox.CountAsync(
                    x => x.Purpose == IdentityEmailPurpose.ResetPassword) == 1,
                  "F2B_OUTBOX_RESET_INTENT_PERSISTED");

            var allRows = await db.IdentityEmailOutbox.AsNoTracking().ToArrayAsync();
            Check(allRows.All(x => x.LastFailureCode is null &&
                                x.QueuedAtUtc > DateTime.MinValue &&
                                x.ExpiresAtUtc > x.QueuedAtUtc),
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

        Console.WriteLine("F2B_OUTBOX_ENQUEUE_SYNTHETIC_ONLY=PASS");
        Console.WriteLine("F2B_OUTBOX_PG_MIGRATIONS=NOT_GENERATED");
        Console.WriteLine("F2B_OUTBOX_WORKER=NOT_IMPLEMENTED");
        Console.WriteLine("PUBLIC_REGISTRATION_DEFAULT_OFF=PASS");
        Console.WriteLine("CT164_DB=NOT_ACCESSED");
    }
}
