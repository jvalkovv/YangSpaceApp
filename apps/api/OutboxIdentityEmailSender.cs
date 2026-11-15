using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace YangSpace.Api;

/// <summary>
/// Store only a minimal email intent; worker generates fresh Identity tokens
/// and fetches the current recipient at dispatch. No external I/O here.
/// </summary>
internal sealed class OutboxIdentityEmailSender(DirectoryDb db) : IEmailSender<IdentityUser>
{
    private const string PendingUniqueIndex = "UX_IdentityEmailOutbox_PendingUserPurpose";
    private const int MaxCoalescingAttempts = 4;

    public Task SendConfirmationLinkAsync(
        IdentityUser user, string email, string confirmationLink) =>
        QueueAsync(user, email, IdentityEmailPurpose.ConfirmEmail);

    public Task SendPasswordResetLinkAsync(
        IdentityUser user, string email, string resetLink) =>
        QueueAsync(user, email, IdentityEmailPurpose.ResetPassword);

    public Task SendPasswordResetCodeAsync(
        IdentityUser user, string email, string resetCode) =>
        QueueAsync(user, email, IdentityEmailPurpose.ResetPassword);

    private async Task QueueAsync(
        IdentityUser user, string recipient, IdentityEmailPurpose purpose)
    {
        if (string.IsNullOrWhiteSpace(user.Id) ||
            !string.Equals(user.Email, recipient, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("OUTBOX_IDENTITY_RECIPIENT_MISMATCH");

        var now = DateTime.UtcNow;
        var expiresAt = now.Add(purpose == IdentityEmailPurpose.ResetPassword
            ? TimeSpan.FromHours(2)
            : TimeSpan.FromHours(24));

        // The partial unique index is the final concurrency authority. This
        // loop handles the winner/loser race and retries a lease transition
        // without ever allowing two Pending rows for the same intent.
        for (var attempt = 0; attempt < MaxCoalescingAttempts; attempt++)
        {
            var pending = await db.IdentityEmailOutbox
                .AsNoTracking()
                .SingleOrDefaultAsync(message =>
                    message.UserId == user.Id &&
                    message.Purpose == purpose &&
                    message.Status == IdentityEmailOutboxStatus.Pending);

            if (pending is not null)
            {
                if (pending.ExpiresAtUtc > now)
                    return;

                // Reuse an expired Pending row rather than inserting a second
                // row. Revision + status predicates prevent overwriting a
                // concurrent worker lease or another request's refresh.
                var refreshed = await db.IdentityEmailOutbox
                    .Where(message =>
                        message.Id == pending.Id &&
                        message.Status == IdentityEmailOutboxStatus.Pending &&
                        message.Revision == pending.Revision &&
                        message.ExpiresAtUtc <= now)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(message => message.QueuedAtUtc, now)
                        .SetProperty(message => message.AvailableAtUtc, now)
                        .SetProperty(message => message.ExpiresAtUtc, expiresAt)
                        .SetProperty(message => message.AttemptCount, 0)
                        .SetProperty(message => message.LeaseId, (Guid?)null)
                        .SetProperty(message => message.LeasedUntilUtc, (DateTime?)null)
                        .SetProperty(message => message.SentAtUtc, (DateTime?)null)
                        .SetProperty(message => message.DeadLetterAtUtc, (DateTime?)null)
                        .SetProperty(message => message.LastFailureCode, (string?)null)
                        .SetProperty(message => message.Revision, message => message.Revision + 1));

                if (refreshed == 1)
                    return;

                // A worker or another request changed the row after our read.
                continue;
            }

            var message = new IdentityEmailOutboxMessage
            {
                UserId = user.Id,
                Purpose = purpose,
                QueuedAtUtc = now,
                AvailableAtUtc = now,
                ExpiresAtUtc = expiresAt
            };
            db.IdentityEmailOutbox.Add(message);

            try
            {
                // Participates in the caller's EF transaction during registration.
                await db.SaveChangesAsync();
                return;
            }
            catch (DbUpdateException exception)
            {
                // Never swallow a constraint error inside a caller-owned
                // transaction: PostgreSQL marks that transaction aborted.
                // Registration uses an explicit transaction and must roll back.
                if (db.Database.CurrentTransaction is not null ||
                    System.Transactions.Transaction.Current is not null ||
                    !IsPendingUniqueConflict(exception))
                    throw;

                db.Entry(message).State = EntityState.Detached;
                // Another request won the insert. Re-read and coalesce. If a
                // worker already leased it, the next pass may enqueue a new
                // Pending intent because the partial unique predicate no
                // longer includes the leased row.
            }
        }

        throw new InvalidOperationException("OUTBOX_PENDING_COALESCING_RETRY_EXHAUSTED");
    }

    private static bool IsPendingUniqueConflict(DbUpdateException exception)
    {
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is PostgresException postgres &&
                postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                string.Equals(postgres.ConstraintName, PendingUniqueIndex, StringComparison.Ordinal))
                return true;

            if (inner is SqliteException sqlite &&
                sqlite.SqliteExtendedErrorCode == 2067 &&
                sqlite.Message.Contains("IdentityEmailOutbox.UserId", StringComparison.Ordinal) &&
                sqlite.Message.Contains("IdentityEmailOutbox.Purpose", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}

// Implementation availability is separate from the explicit runtime switches.
// Both PublicAccounts and Outbox.DispatchEnabled remain default-OFF.
internal static class IdentityEmailOutboxDispatchPolicy
{
    internal static bool DispatcherAvailable => true;

    internal static bool IsEnabled(IConfiguration configuration) =>
        DispatcherAvailable &&
        configuration.GetValue<bool>("PublicAccounts:Enabled") &&
        configuration.GetValue<bool>("PublicAccounts:Outbox:DispatchEnabled");
}
