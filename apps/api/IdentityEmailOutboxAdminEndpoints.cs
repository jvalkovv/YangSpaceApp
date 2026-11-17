using System.Security.Claims;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace YangSpace.Api;

internal enum IdentityEmailOutboxReplayResult { Replayed, NotFound, NotDeadLetter, PendingExists }

internal sealed record IdentityEmailOutboxOperationalSummary(
    DateTime ObservedAtUtc, long Pending, long Due, long RetryPending,
    long Leased, long Sent, long Suppressed, long DeadLetter,
    DateTime? OldestPendingUtc, DateTime? LatestSentUtc);

internal static class IdentityEmailOutboxAdminEndpoints
{
    internal static void MapIdentityEmailOutboxAdmin(this RouteGroupBuilder admin)
    {
        // Only mapped beneath the existing PlatformAdmin bearer + gateway filtered group.
        // Count-based operational data; deliberately no user IDs, emails, tokens or payloads.
        admin.MapGet("/email-outbox/summary", async (
            DirectoryDb db, TimeProvider clock, CancellationToken ct) =>
            Results.Ok(await ReadSummaryAsync(db, clock, ct)));

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
            DirectoryDb db, Guid id, TimeProvider clock, ClaimsPrincipal principal,
            ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var result = await ReplayDeadLetterAsync(db,id,clock,ct);
            if (result == IdentityEmailOutboxReplayResult.Replayed)
            {
                var actorId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
                var logger = loggerFactory.CreateLogger("YangSpace.IdentityEmailOutboxAdmin");
                logger.LogInformation(
                    "IDENTITY_EMAIL_OUTBOX_OPERATOR_REPLAYED OutboxId={OutboxId} ActorId={ActorId}",
                    id, actorId);
            }
            return result switch
            {
                IdentityEmailOutboxReplayResult.Replayed => Results.Accepted(),
                IdentityEmailOutboxReplayResult.NotFound => Results.NotFound(),
                _ => Results.Conflict(new { error = "The outbox item cannot be replayed in its current state." })
            };
        });
    }

    internal static async Task<IdentityEmailOutboxOperationalSummary> ReadSummaryAsync(
        DirectoryDb db, TimeProvider clock, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var rows = await db.IdentityEmailOutbox.AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.LongCount() })
            .ToArrayAsync(ct);
        long Count(IdentityEmailOutboxStatus status) =>
            rows.Where(x => x.Status == status).Sum(x => x.Count);

        var due = await db.IdentityEmailOutbox.AsNoTracking().LongCountAsync(x =>
            x.Status == IdentityEmailOutboxStatus.Pending &&
            x.AvailableAtUtc <= now && x.ExpiresAtUtc > now, ct);
        var retryPending = await db.IdentityEmailOutbox.AsNoTracking().LongCountAsync(x =>
            x.Status == IdentityEmailOutboxStatus.Pending && x.AttemptCount > 0, ct);
        var oldestPending = await db.IdentityEmailOutbox.AsNoTracking()
            .Where(x => x.Status == IdentityEmailOutboxStatus.Pending)
            .MinAsync(x => (DateTime?)x.QueuedAtUtc, ct);
        var latestSent = await db.IdentityEmailOutbox.AsNoTracking()
            .Where(x => x.Status == IdentityEmailOutboxStatus.Sent)
            .MaxAsync(x => x.SentAtUtc, ct);

        return new IdentityEmailOutboxOperationalSummary(
            now,
            Count(IdentityEmailOutboxStatus.Pending),
            due, retryPending,
            Count(IdentityEmailOutboxStatus.Leased),
            Count(IdentityEmailOutboxStatus.Sent),
            Count(IdentityEmailOutboxStatus.Suppressed),
            Count(IdentityEmailOutboxStatus.DeadLetter),
            oldestPending, latestSent);
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
