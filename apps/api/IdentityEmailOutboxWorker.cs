using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace YangSpace.Api;

internal interface IIdentityEmailOutboxTransport
{
    Task SendConfirmationAsync(IdentityUser user, string email, string confirmationLink, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(IdentityUser user, string email, string resetCode, CancellationToken cancellationToken);
}

internal sealed class IdentityEmailOutboxOptions
{
    public int PollIntervalSeconds { get; set; } = 5;
    public int LeaseSeconds { get; set; } = 120;
    public int BatchSize { get; set; } = 10;
    public int MaxAttempts { get; set; } = 5;
    public int SentRetentionDays { get; set; } = 30;
    public int SuppressedRetentionDays { get; set; } = 30;
    public int DeadLetterRetentionDays { get; set; } = 90;

    internal void Validate()
    {
        if (PollIntervalSeconds is < 1 or > 300 || LeaseSeconds is < 30 or > 900 ||
            BatchSize is < 1 or > 100 || MaxAttempts is < 1 or > 20 ||
            SentRetentionDays is < 1 or > 3650 ||
            SuppressedRetentionDays is < 1 or > 3650 ||
            DeadLetterRetentionDays is < 1 or > 3650)
            throw new InvalidOperationException("IDENTITY_EMAIL_OUTBOX_OPTIONS_REFUSED");
    }
}

internal enum IdentityEmailOutboxFailureTransition
{
    RetryScheduled,
    DeadLettered,
    LeaseLost
}

internal sealed class IdentityEmailOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdentityEmailOutboxWorker> _logger;
    private readonly IdentityEmailOutboxWorkerTelemetry _telemetry;
    private readonly IdentityEmailOutboxOptions _options;
    private readonly TimeProvider _clock;
    private readonly string _canonicalOrigin;
    private readonly bool _dispatchEnabled;

    public IdentityEmailOutboxWorker(IServiceScopeFactory scopeFactory,
        ILogger<IdentityEmailOutboxWorker> logger,
        IdentityEmailOutboxWorkerTelemetry telemetry,
        IOptions<IdentityEmailOutboxOptions> options,
        TimeProvider clock, IConfiguration configuration,
        IHostEnvironment environment)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _telemetry = telemetry;
        _options = options.Value;
        _options.Validate();
        _clock = clock;
        _canonicalOrigin = configuration["PublicAccounts:CanonicalOrigin"] ?? "";
        _dispatchEnabled = IdentityEmailOutboxDispatchPolicy.IsEnabled(configuration, environment, _clock);
        if (_canonicalOrigin != "https://yangspace.gndstore.eu")
            throw new InvalidOperationException("PUBLIC_ACCOUNT_CANONICAL_ORIGIN_REFUSED");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _telemetry.SetDispatchEnabled(_dispatchEnabled);
        if (!_dispatchEnabled)
        {
            _logger.LogInformation("IDENTITY_EMAIL_OUTBOX_DISPATCH_DISABLED");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                // ProcessBatchOnceAsync records and emits the stable, redacted failure event.
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), _clock, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    internal async Task<int> ProcessBatchOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!_dispatchEnabled) return 0;

        var cycleStartedUtc = _clock.GetUtcNow().UtcDateTime;
        var stopwatchStarted = Stopwatch.GetTimestamp();
        _telemetry.RecordCycleStarted(cycleStartedUtc);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<DirectoryDb>();
            var users = services.GetRequiredService<UserManager<IdentityUser>>();
            var transport = services.GetRequiredService<IIdentityEmailOutboxTransport>();

            await NormalizeAsync(db, cycleStartedUtc, cancellationToken);
            await PurgeAsync(db, cycleStartedUtc, cancellationToken);

            var candidates = await db.IdentityEmailOutbox.AsNoTracking()
                .Where(x => x.ExpiresAtUtc > cycleStartedUtc && x.AttemptCount < _options.MaxAttempts &&
                    ((x.Status == IdentityEmailOutboxStatus.Pending && x.AvailableAtUtc <= cycleStartedUtc) ||
                     (x.Status == IdentityEmailOutboxStatus.Leased &&
                      (x.LeasedUntilUtc == null || x.LeasedUntilUtc <= cycleStartedUtc))))
                .OrderBy(x => x.AvailableAtUtc).ThenBy(x => x.QueuedAtUtc).ThenBy(x => x.Id)
                .Select(x => new Candidate(x.Id, x.Revision))
                .Take(_options.BatchSize)
                .ToArrayAsync(cancellationToken);

            var claimed = 0;
            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var claimNow = _clock.GetUtcNow().UtcDateTime;
                var leaseId = Guid.NewGuid();
                var leaseUntil = claimNow.AddSeconds(_options.LeaseSeconds);
                var count = await db.IdentityEmailOutbox.Where(x =>
                        x.Id == candidate.Id && x.Revision == candidate.Revision &&
                        x.ExpiresAtUtc > claimNow && x.AttemptCount < _options.MaxAttempts &&
                        ((x.Status == IdentityEmailOutboxStatus.Pending && x.AvailableAtUtc <= claimNow) ||
                         (x.Status == IdentityEmailOutboxStatus.Leased &&
                          (x.LeasedUntilUtc == null || x.LeasedUntilUtc <= claimNow))))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Leased)
                        .SetProperty(x => x.LeaseId, (Guid?)leaseId)
                        .SetProperty(x => x.LeasedUntilUtc, (DateTime?)leaseUntil)
                        .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                        .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);

                if (count != 1)
                {
                    _telemetry.RecordClaimConflicts(1);
                    continue;
                }

                claimed++;
                _telemetry.RecordClaimed(1);
                await DispatchAsync(db, users, transport, candidate.Id, leaseId, cancellationToken);
            }

            await RefreshBacklogAsync(db, _clock.GetUtcNow().UtcDateTime, cancellationToken);
            _telemetry.RecordCycleSucceeded(_clock.GetUtcNow().UtcDateTime);
            _logger.LogDebug(
                "IDENTITY_EMAIL_OUTBOX_WORKER_CYCLE_COMPLETED Claimed={Claimed}", claimed);
            return claimed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _telemetry.RecordCycleFailed(FailureCategory(exception));
            _logger.LogError(
                "IDENTITY_EMAIL_OUTBOX_WORKER_CYCLE_FAILED FailureCategory={FailureCategory}",
                FailureCategory(exception));
            throw;
        }
        finally
        {
            _telemetry.RecordCycleDuration(Stopwatch.GetElapsedTime(stopwatchStarted));
        }
    }

    private async Task NormalizeAsync(DirectoryDb db, DateTime now, CancellationToken ct)
    {
        var suppressed = await db.IdentityEmailOutbox.Where(x =>
                x.ExpiresAtUtc <= now &&
                (x.Status == IdentityEmailOutboxStatus.Pending ||
                 (x.Status == IdentityEmailOutboxStatus.Leased &&
                  (x.LeasedUntilUtc == null || x.LeasedUntilUtc <= now))))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Suppressed)
                .SetProperty(x => x.ExpiresAtUtc, now)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastFailureCode, "intent_expired")
                .SetProperty(x => x.Revision, x => x.Revision + 1), ct);
        _telemetry.RecordSuppressed(suppressed);

        var deadLettered = await db.IdentityEmailOutbox.Where(x =>
                x.ExpiresAtUtc > now && x.AttemptCount >= _options.MaxAttempts &&
                ((x.Status == IdentityEmailOutboxStatus.Pending && x.AvailableAtUtc <= now) ||
                 (x.Status == IdentityEmailOutboxStatus.Leased &&
                  (x.LeasedUntilUtc == null || x.LeasedUntilUtc <= now))))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, IdentityEmailOutboxStatus.DeadLetter)
                .SetProperty(x => x.DeadLetterAtUtc, (DateTime?)now)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastFailureCode, "attempt_limit")
                .SetProperty(x => x.Revision, x => x.Revision + 1), ct);
        _telemetry.RecordDeadLettered(deadLettered);
    }

    private async Task PurgeAsync(DirectoryDb db, DateTime now, CancellationToken ct)
    {
        var sentCutoff = now.AddDays(-_options.SentRetentionDays);
        var suppressedCutoff = now.AddDays(-_options.SuppressedRetentionDays);
        var deadCutoff = now.AddDays(-_options.DeadLetterRetentionDays);
        var purged = await db.IdentityEmailOutbox.Where(x =>
                (x.Status == IdentityEmailOutboxStatus.Sent && x.SentAtUtc != null && x.SentAtUtc <= sentCutoff) ||
                (x.Status == IdentityEmailOutboxStatus.Suppressed && x.ExpiresAtUtc <= suppressedCutoff) ||
                (x.Status == IdentityEmailOutboxStatus.DeadLetter &&
                 x.DeadLetterAtUtc != null && x.DeadLetterAtUtc <= deadCutoff))
            .ExecuteDeleteAsync(ct);
        _telemetry.RecordPurged(purged);
    }

    private async Task RefreshBacklogAsync(DirectoryDb db, DateTime now, CancellationToken ct)
    {
        var counts = await db.IdentityEmailOutbox.AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.LongCount() })
            .ToArrayAsync(ct);
        long Count(IdentityEmailOutboxStatus status) =>
            counts.Where(x => x.Status == status).Sum(x => x.Count);
        var due = await db.IdentityEmailOutbox.AsNoTracking().LongCountAsync(x =>
            x.Status == IdentityEmailOutboxStatus.Pending &&
            x.AvailableAtUtc <= now && x.ExpiresAtUtc > now, ct);

        _telemetry.SetBacklog(
            Count(IdentityEmailOutboxStatus.Pending),
            due,
            Count(IdentityEmailOutboxStatus.Leased),
            Count(IdentityEmailOutboxStatus.DeadLetter));
    }

    private async Task DispatchAsync(DirectoryDb db, UserManager<IdentityUser> users,
        IIdentityEmailOutboxTransport transport, Guid id, Guid leaseId, CancellationToken ct)
    {
        var item = await db.IdentityEmailOutbox.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == id && x.Status == IdentityEmailOutboxStatus.Leased &&
                 x.LeaseId == leaseId, ct);
        if (item is null)
        {
            _telemetry.RecordLeaseLost(1);
            _logger.LogWarning("IDENTITY_EMAIL_OUTBOX_LEASE_LOST Transition={Transition}", "load");
            return;
        }

        if (item.ExpiresAtUtc <= _clock.GetUtcNow().UtcDateTime)
        {
            await SuppressAsync(db, id, leaseId, item.Revision, "intent_expired", ct);
            return;
        }

        // Read only the Identity fields required for dispatch and token generation.
        // The worker must not select PasswordHash or unrelated credential columns.
        var identity = await db.Users.AsNoTracking()
            .Where(candidate => candidate.Id == item.UserId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Email,
                candidate.EmailConfirmed,
                candidate.SecurityStamp
            })
            .SingleOrDefaultAsync(ct);

        if (identity is null)
        {
            await SuppressAsync(db, id, leaseId, item.Revision, "user_missing", ct);
            return;
        }

        var user = new IdentityUser
        {
            Id = identity.Id,
            Email = identity.Email,
            EmailConfirmed = identity.EmailConfirmed,
            SecurityStamp = identity.SecurityStamp
        };
        var email = user.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 ||
            !new EmailAddressAttribute().IsValid(email))
        {
            await SuppressAsync(db, id, leaseId, item.Revision, "email_unavailable", ct);
            return;
        }

        if ((await users.GetRolesAsync(user)).Count != 0)
        {
            await SuppressAsync(db, id, leaseId, item.Revision, "staff_account", ct);
            return;
        }

        if (item.Purpose == IdentityEmailPurpose.ConfirmEmail && user.EmailConfirmed)
        {
            await SuppressAsync(db, id, leaseId, item.Revision, "already_confirmed", ct);
            return;
        }

        if (item.Purpose == IdentityEmailPurpose.ResetPassword && !user.EmailConfirmed)
        {
            await SuppressAsync(db, id, leaseId, item.Revision, "email_not_confirmed", ct);
            return;
        }

        if (item.Purpose is not IdentityEmailPurpose.ConfirmEmail and not IdentityEmailPurpose.ResetPassword)
        {
            await SuppressAsync(db, id, leaseId, item.Revision, "purpose_invalid", ct);
            return;
        }

        // Generate a fresh token from the current Identity record; never persist token or message payload.
        var rawToken = item.Purpose == IdentityEmailPurpose.ConfirmEmail
            ? await users.GenerateEmailConfirmationTokenAsync(user)
            : await users.GeneratePasswordResetTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var dispatchNow = _clock.GetUtcNow().UtcDateTime;
        var stillOwnsLease = await db.IdentityEmailOutbox.AsNoTracking().AnyAsync(x =>
            x.Id == id && x.Status == IdentityEmailOutboxStatus.Leased &&
            x.LeaseId == leaseId && x.Revision == item.Revision &&
            x.LeasedUntilUtc != null && x.LeasedUntilUtc > dispatchNow &&
            x.ExpiresAtUtc > dispatchNow, ct);
        if (!stillOwnsLease)
        {
            _telemetry.RecordLeaseLost(1);
            _logger.LogWarning("IDENTITY_EMAIL_OUTBOX_LEASE_LOST Transition={Transition}", "pre_send");
            return;
        }

        try
        {
            if (item.Purpose == IdentityEmailPurpose.ConfirmEmail)
            {
                var link = _canonicalOrigin + "/api/public-account/confirm?userId=" +
                    Uri.EscapeDataString(user.Id) + "&code=" + Uri.EscapeDataString(code);
                await transport.SendConfirmationAsync(user, email, link, ct);
            }
            else
            {
                await transport.SendPasswordResetAsync(user, email, code, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Keep the lease. A restarted process reclaims it after LeasedUntilUtc.
            throw;
        }
        catch (Exception)
        {
            // A transport ACK can be uncertain. Retry is explicitly at-least-once.
            _telemetry.RecordTransportFailures(1);
            var transition = await FailAsync(
                db, id, leaseId, item.Revision, "dispatch_failed",
                _clock.GetUtcNow().UtcDateTime, ct);
            switch (transition)
            {
                case IdentityEmailOutboxFailureTransition.RetryScheduled:
                    _telemetry.RecordRetried(1);
                    _logger.LogWarning(
                        "IDENTITY_EMAIL_OUTBOX_TRANSPORT_FAILED Outcome={Outcome}", "retry");
                    break;
                case IdentityEmailOutboxFailureTransition.DeadLettered:
                    _telemetry.RecordDeadLettered(1);
                    _logger.LogWarning(
                        "IDENTITY_EMAIL_OUTBOX_TRANSPORT_FAILED Outcome={Outcome}", "dead_letter");
                    break;
                default:
                    _telemetry.RecordLeaseLost(1);
                    _logger.LogWarning(
                        "IDENTITY_EMAIL_OUTBOX_TRANSPORT_FAILED Outcome={Outcome}", "lease_lost");
                    break;
            }
            return;
        }

        var sent = await MarkSentAsync(
            db, id, leaseId, item.Revision, _clock.GetUtcNow().UtcDateTime, ct);
        if (sent == 1)
        {
            _telemetry.RecordSent(1);
            return;
        }

        // A stale or expired lease owner must never overwrite a newer owner's state.
        _telemetry.RecordLeaseLost(1);
        _logger.LogWarning("IDENTITY_EMAIL_OUTBOX_LEASE_LOST Transition={Transition}", "mark_sent");
    }

    private async Task SuppressAsync(
        DirectoryDb db, Guid id, Guid leaseId, long revision, string code, CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var count = await db.IdentityEmailOutbox.Where(x =>
                x.Id == id && x.Revision == revision &&
                x.Status == IdentityEmailOutboxStatus.Leased && x.LeaseId == leaseId &&
                x.LeasedUntilUtc != null && x.LeasedUntilUtc > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Suppressed)
                .SetProperty(x => x.ExpiresAtUtc, now)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastFailureCode, code)
                .SetProperty(x => x.Revision, x => x.Revision + 1), ct);

        if (count == 1)
        {
            _telemetry.RecordSuppressed(1);
            return;
        }

        _telemetry.RecordLeaseLost(1);
        _logger.LogWarning("IDENTITY_EMAIL_OUTBOX_LEASE_LOST Transition={Transition}", "suppress");
    }

    private static Task<int> MarkSentAsync(
        DirectoryDb db, Guid id, Guid leaseId, long revision, DateTime now, CancellationToken ct) =>
        db.IdentityEmailOutbox.Where(x =>
                x.Id == id && x.Revision == revision &&
                x.Status == IdentityEmailOutboxStatus.Leased && x.LeaseId == leaseId &&
                x.LeasedUntilUtc != null && x.LeasedUntilUtc > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Sent)
                .SetProperty(x => x.SentAtUtc, (DateTime?)now)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastFailureCode, (string?)null)
                .SetProperty(x => x.Revision, x => x.Revision + 1), ct);

    private async Task<IdentityEmailOutboxFailureTransition> FailAsync(
        DirectoryDb db, Guid id, Guid leaseId, long revision,
        string code, DateTime now, CancellationToken ct)
    {
        var attempts = await db.IdentityEmailOutbox.AsNoTracking()
            .Where(x => x.Id == id && x.Revision == revision &&
                x.Status == IdentityEmailOutboxStatus.Leased && x.LeaseId == leaseId &&
                x.LeasedUntilUtc != null && x.LeasedUntilUtc > now)
            .Select(x => (int?)x.AttemptCount)
            .SingleOrDefaultAsync(ct);
        if (attempts is null) return IdentityEmailOutboxFailureTransition.LeaseLost;

        var query = db.IdentityEmailOutbox.Where(x =>
            x.Id == id && x.Revision == revision &&
            x.Status == IdentityEmailOutboxStatus.Leased && x.LeaseId == leaseId &&
            x.LeasedUntilUtc != null && x.LeasedUntilUtc > now);

        if (attempts.Value >= _options.MaxAttempts)
        {
            var deadLettered = await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, IdentityEmailOutboxStatus.DeadLetter)
                .SetProperty(x => x.DeadLetterAtUtc, (DateTime?)now)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastFailureCode, code)
                .SetProperty(x => x.Revision, x => x.Revision + 1), ct);
            return deadLettered == 1
                ? IdentityEmailOutboxFailureTransition.DeadLettered
                : IdentityEmailOutboxFailureTransition.LeaseLost;
        }

        var retried = await query.ExecuteUpdateAsync(s => s
            .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Pending)
            .SetProperty(x => x.AvailableAtUtc, now + RetryDelay(attempts.Value))
            .SetProperty(x => x.LeaseId, (Guid?)null)
            .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
            .SetProperty(x => x.LastFailureCode, code)
            .SetProperty(x => x.Revision, x => x.Revision + 1), ct);
        return retried == 1
            ? IdentityEmailOutboxFailureTransition.RetryScheduled
            : IdentityEmailOutboxFailureTransition.LeaseLost;
    }

    private static string FailureCategory(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException or DbUpdateException)
                return "database";
        }
        return "worker";
    }

    private static TimeSpan RetryDelay(int attempts) => attempts switch
    {
        <= 1 => TimeSpan.FromSeconds(30),
        2 => TimeSpan.FromMinutes(2),
        3 => TimeSpan.FromMinutes(10),
        4 => TimeSpan.FromMinutes(30),
        _ => TimeSpan.FromHours(1)
    };

    private sealed record Candidate(Guid Id, long Revision);
}
