using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace YangSpace.Api;

internal enum IdentityEmailOutboxReplayResult { Replayed, NotFound, NotDeadLetter, PendingExists }

internal static class IdentityEmailOutboxAdminEndpoints
{
    internal static void MapIdentityEmailOutboxAdmin(this RouteGroupBuilder admin)
    {
        admin.MapGet("/email-outbox/dead-letter", async (DirectoryDb db, CancellationToken ct) =>
        {
            var rows = await db.IdentityEmailOutbox.AsNoTracking()
                .Where(x => x.Status == IdentityEmailOutboxStatus.DeadLetter)
                .OrderBy(x => x.DeadLetterAtUtc).Take(100)
                .Select(x => new { x.Id, x.Purpose, x.QueuedAtUtc, x.AttemptCount, x.LastFailureCode, x.DeadLetterAtUtc })
                .ToArrayAsync(ct);
            return Results.Ok(rows); // no user IDs, addresses, tokens or payloads
        });

        admin.MapPost("/email-outbox/{id:guid}/replay", async (
            DirectoryDb db, Guid id, TimeProvider clock, CancellationToken ct) =>
        {
            var result = await ReplayDeadLetterAsync(db,id,clock,ct);
            return result switch
            {
                IdentityEmailOutboxReplayResult.Replayed => Results.Accepted(),
                IdentityEmailOutboxReplayResult.NotFound => Results.NotFound(),
                _ => Results.Conflict(new { error = "The outbox item cannot be replayed in its current state." })
            };
        });
    }

    internal static async Task<IdentityEmailOutboxReplayResult> ReplayDeadLetterAsync(
        DirectoryDb db, Guid id, TimeProvider clock, CancellationToken ct)
    {
        var row = await db.IdentityEmailOutbox.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id,ct);
        if (row is null) return IdentityEmailOutboxReplayResult.NotFound;
        if (row.Status != IdentityEmailOutboxStatus.DeadLetter) return IdentityEmailOutboxReplayResult.NotDeadLetter;
        if (await db.IdentityEmailOutbox.AnyAsync(x => x.UserId == row.UserId &&
            x.Purpose == row.Purpose && x.Status == IdentityEmailOutboxStatus.Pending,ct))
            return IdentityEmailOutboxReplayResult.PendingExists;

        var now = clock.GetUtcNow().UtcDateTime;
        var expiry = now.Add(row.Purpose == IdentityEmailPurpose.ResetPassword
            ? TimeSpan.FromHours(2) : TimeSpan.FromHours(24));
        try
        {
            var count = await db.IdentityEmailOutbox.Where(x =>
                    x.Id == id && x.Revision == row.Revision && x.Status == IdentityEmailOutboxStatus.DeadLetter)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Pending)
                    .SetProperty(x => x.QueuedAtUtc, now)
                    .SetProperty(x => x.AvailableAtUtc, now)
                    .SetProperty(x => x.ExpiresAtUtc, expiry)
                    .SetProperty(x => x.AttemptCount, 0)
                    .SetProperty(x => x.LeaseId, (Guid?)null)
                    .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                    .SetProperty(x => x.SentAtUtc, (DateTime?)null)
                    .SetProperty(x => x.DeadLetterAtUtc, (DateTime?)null)
                    .SetProperty(x => x.LastFailureCode, (string?)null)
                    .SetProperty(x => x.Revision, x => x.Revision + 1),ct);
            return count == 1 ? IdentityEmailOutboxReplayResult.Replayed : IdentityEmailOutboxReplayResult.NotDeadLetter;
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            return IdentityEmailOutboxReplayResult.PendingExists;
        }
    }

    private static bool IsUniqueViolation(Exception ex)
    {
        for (Exception? current=ex; current is not null; current=current.InnerException)
        {
            if (current is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation &&
                pg.ConstraintName == "UX_IdentityEmailOutbox_PendingUserPurpose") return true;
            if (current is SqliteException sqlite && sqlite.SqliteExtendedErrorCode == 2067) return true;
        }
        return false;
    }
}
