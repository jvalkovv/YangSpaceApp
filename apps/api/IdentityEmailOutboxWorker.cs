using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
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

internal sealed class IdentityEmailOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdentityEmailOutboxWorker> _logger;
    private readonly IdentityEmailOutboxOptions _options;
    private readonly TimeProvider _clock;
    private readonly string _canonicalOrigin;

    internal IdentityEmailOutboxWorker(IServiceScopeFactory scopeFactory,
        ILogger<IdentityEmailOutboxWorker> logger, IOptions<IdentityEmailOutboxOptions> options,
        TimeProvider clock, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _options.Validate();
        _clock = clock;
        _canonicalOrigin = configuration["PublicAccounts:CanonicalOrigin"] ?? "";
        if (_canonicalOrigin != "https://yangspace.gndstore.eu")
            throw new InvalidOperationException("PUBLIC_ACCOUNT_CANONICAL_ORIGIN_REFUSED");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchOnceAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch { _logger.LogError("IDENTITY_EMAIL_OUTBOX_WORKER_CYCLE_FAILED"); }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), _clock, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    internal async Task<int> ProcessBatchOnceAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<DirectoryDb>();
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var transport = services.GetRequiredService<IIdentityEmailOutboxTransport>();

        await NormalizeAsync(db, now, cancellationToken);
        await PurgeAsync(db, now, cancellationToken);

        var candidates = await db.IdentityEmailOutbox.AsNoTracking()
            .Where(x => x.ExpiresAtUtc > now && x.AttemptCount < _options.MaxAttempts &&
                ((x.Status == IdentityEmailOutboxStatus.Pending && x.AvailableAtUtc <= now) ||
                 (x.Status == IdentityEmailOutboxStatus.Leased && x.LeasedUntilUtc != null && x.LeasedUntilUtc <= now)))
            .OrderBy(x => x.AvailableAtUtc).ThenBy(x => x.QueuedAtUtc).ThenBy(x => x.Id)
            .Select(x => new Candidate(x.Id, x.Revision))
            .Take(_options.BatchSize)
            .ToArrayAsync(cancellationToken);

        var claimed = 0;
        foreach (var item in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var claimNow = _clock.GetUtcNow().UtcDateTime;
            var leaseId = Guid.NewGuid();
            var leaseUntil = claimNow.AddSeconds(_options.LeaseSeconds);
            var count = await db.IdentityEmailOutbox.Where(x =>
                    x.Id == item.Id && x.Revision == item.Revision &&
                    x.ExpiresAtUtc > claimNow && x.AttemptCount < _options.MaxAttempts &&
                    ((x.Status == IdentityEmailOutboxStatus.Pending && x.AvailableAtUtc <= claimNow) ||
                     (x.Status == IdentityEmailOutboxStatus.Leased && x.LeasedUntilUtc != null && x.LeasedUntilUtc <= claimNow)))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Leased)
                    .SetProperty(x => x.LeaseId, (Guid?)leaseId)
                    .SetProperty(x => x.LeasedUntilUtc, (DateTime?)leaseUntil)
                    .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                    .SetProperty(x => x.Revision, x => x.Revision + 1), cancellationToken);
            if (count != 1) continue;
            claimed++;
            await DispatchAsync(db, users, transport, item.Id, leaseId, cancellationToken);
        }
        return claimed;
    }

    private async Task NormalizeAsync(DirectoryDb db, DateTime now, CancellationToken ct)
    {
        await db.IdentityEmailOutbox.Where(x =>
                x.ExpiresAtUtc <= now &&
                (x.Status == IdentityEmailOutboxStatus.Pending ||
                 (x.Status == IdentityEmailOutboxStatus.Leased && x.LeasedUntilUtc != null && x.LeasedUntilUtc <= now)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Suppressed)
                .SetProperty(x => x.ExpiresAtUtc, now)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastFailureCode, "intent_expired")
                .SetProperty(x => x.Revision, x => x.Revision + 1), ct);

        await db.IdentityEmailOutbox.Where(x =>
                x.ExpiresAtUtc > now && x.AttemptCount >= _options.MaxAttempts &&
                ((x.Status == IdentityEmailOutboxStatus.Pending && x.AvailableAtUtc <= now) ||
                 (x.Status == IdentityEmailOutboxStatus.Leased && x.LeasedUntilUtc != null && x.LeasedUntilUtc <= now)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, IdentityEmailOutboxStatus.DeadLetter)
                .SetProperty(x => x.DeadLetterAtUtc, (DateTime?)now)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastFailureCode, "attempt_limit")
                .SetProperty(x => x.Revision, x => x.Revision + 1), ct);
    }

    private async Task PurgeAsync(DirectoryDb db, DateTime now, CancellationToken ct)
    {
        var sentCutoff = now.AddDays(-_options.SentRetentionDays);
        var suppressedCutoff = now.AddDays(-_options.SuppressedRetentionDays);
        var deadCutoff = now.AddDays(-_options.DeadLetterRetentionDays);
        await db.IdentityEmailOutbox.Where(x =>
                (x.Status == IdentityEmailOutboxStatus.Sent && x.SentAtUtc != null && x.SentAtUtc <= sentCutoff) ||
                (x.Status == IdentityEmailOutboxStatus.Suppressed && x.ExpiresAtUtc <= suppressedCutoff) ||
                (x.Status == IdentityEmailOutboxStatus.DeadLetter && x.DeadLetterAtUtc != null && x.DeadLetterAtUtc <= deadCutoff))
            .ExecuteDeleteAsync(ct);
    }

    private async Task DispatchAsync(DirectoryDb db, UserManager<IdentityUser> users,
        IIdentityEmailOutboxTransport transport, Guid id, Guid leaseId, CancellationToken ct)
    {
        try
        {
            var item = await db.IdentityEmailOutbox.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == id && x.Status == IdentityEmailOutboxStatus.Leased && x.LeaseId == leaseId, ct);
            if (item is null) return;
            if (item.ExpiresAtUtc <= _clock.GetUtcNow().UtcDateTime)
            {
                await SuppressAsync(db,id,leaseId,"intent_expired",ct);
                return;
            }

            var user = await users.FindByIdAsync(item.UserId);
            if (user is null) { await SuppressAsync(db,id,leaseId,"user_missing",ct); return; }

            var email = user.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254 ||
                !new EmailAddressAttribute().IsValid(email))
            { await SuppressAsync(db,id,leaseId,"email_unavailable",ct); return; }

            if ((await users.GetRolesAsync(user)).Count != 0)
            { await SuppressAsync(db,id,leaseId,"staff_account",ct); return; }

            if (item.Purpose == IdentityEmailPurpose.ConfirmEmail && user.EmailConfirmed)
            { await SuppressAsync(db,id,leaseId,"already_confirmed",ct); return; }

            if (item.Purpose == IdentityEmailPurpose.ResetPassword && !user.EmailConfirmed)
            { await SuppressAsync(db,id,leaseId,"email_not_confirmed",ct); return; }

            if (item.Purpose is not IdentityEmailPurpose.ConfirmEmail and not IdentityEmailPurpose.ResetPassword)
            { await SuppressAsync(db,id,leaseId,"purpose_invalid",ct); return; }

            // Generate a fresh token from the current Identity record; never persist token or message payload.
            var rawToken = item.Purpose == IdentityEmailPurpose.ConfirmEmail
                ? await users.GenerateEmailConfirmationTokenAsync(user)
                : await users.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
            var dispatchNow = _clock.GetUtcNow().UtcDateTime;
            var stillOwnsLease = await db.IdentityEmailOutbox.AsNoTracking().AnyAsync(x =>
                x.Id == id && x.Status == IdentityEmailOutboxStatus.Leased &&
                x.LeaseId == leaseId && x.LeasedUntilUtc != null &&
                x.LeasedUntilUtc > dispatchNow && x.ExpiresAtUtc > dispatchNow, ct);
            if (!stillOwnsLease)
                return;

            if (item.Purpose == IdentityEmailPurpose.ConfirmEmail)
            {
                var link = _canonicalOrigin + "/api/public-account/confirm?userId=" +
                    Uri.EscapeDataString(user.Id) + "&code=" + Uri.EscapeDataString(code);
                await transport.SendConfirmationAsync(user,email,link,ct);
            }
            else
            {
                await transport.SendPasswordResetAsync(user,email,code,ct);
            }

            await MarkSentAsync(db,id,leaseId,_clock.GetUtcNow().UtcDateTime,ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Keep lease. A restarted process reclaims it after LeasedUntilUtc.
            throw;
        }
        catch
        {
            // SMTP uncertain ACK is at-least-once: retry can duplicate an accepted message.
            await FailAsync(db,id,leaseId,"dispatch_failed",_clock.GetUtcNow().UtcDateTime,ct);
        }
    }

    private Task<int> SuppressAsync(DirectoryDb db, Guid id, Guid leaseId, string code, CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        return db.IdentityEmailOutbox.Where(x => x.Id == id &&
                x.Status == IdentityEmailOutboxStatus.Leased && x.LeaseId == leaseId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Suppressed)
                .SetProperty(x => x.ExpiresAtUtc, now)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastFailureCode, code)
                .SetProperty(x => x.Revision, x => x.Revision + 1), ct);
    }

    private static Task<int> MarkSentAsync(DirectoryDb db, Guid id, Guid leaseId, DateTime now, CancellationToken ct) =>
        db.IdentityEmailOutbox.Where(x => x.Id == id &&
                x.Status == IdentityEmailOutboxStatus.Leased && x.LeaseId == leaseId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Sent)
                .SetProperty(x => x.SentAtUtc, (DateTime?)now)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastFailureCode, (string?)null)
                .SetProperty(x => x.Revision, x => x.Revision + 1), ct);

    private async Task FailAsync(DirectoryDb db, Guid id, Guid leaseId, string code, DateTime now, CancellationToken ct)
    {
        var attempts = await db.IdentityEmailOutbox.AsNoTracking()
            .Where(x => x.Id == id && x.Status == IdentityEmailOutboxStatus.Leased && x.LeaseId == leaseId)
            .Select(x => (int?)x.AttemptCount).SingleOrDefaultAsync(ct);
        if (attempts is null) return;

        var query = db.IdentityEmailOutbox.Where(x => x.Id == id &&
            x.Status == IdentityEmailOutboxStatus.Leased && x.LeaseId == leaseId);
        if (attempts.Value >= _options.MaxAttempts)
        {
            await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, IdentityEmailOutboxStatus.DeadLetter)
                .SetProperty(x => x.DeadLetterAtUtc, (DateTime?)now)
                .SetProperty(x => x.LeaseId, (Guid?)null)
                .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
                .SetProperty(x => x.LastFailureCode, code)
                .SetProperty(x => x.Revision, x => x.Revision + 1), ct);
            return;
        }

        await query.ExecuteUpdateAsync(s => s
            .SetProperty(x => x.Status, IdentityEmailOutboxStatus.Pending)
            .SetProperty(x => x.AvailableAtUtc, now + RetryDelay(attempts.Value))
            .SetProperty(x => x.LeaseId, (Guid?)null)
            .SetProperty(x => x.LeasedUntilUtc, (DateTime?)null)
            .SetProperty(x => x.LastFailureCode, code)
            .SetProperty(x => x.Revision, x => x.Revision + 1), ct);
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

internal static class IdentityEmailOutboxDispatchPolicy
{
    internal static bool DispatcherAvailable => true;
    internal static bool IsEnabled(IConfiguration configuration) =>
        DispatcherAvailable &&
        configuration.GetValue<bool>("PublicAccounts:Enabled") &&
        configuration.GetValue<bool>("PublicAccounts:Outbox:DispatchEnabled");
}
