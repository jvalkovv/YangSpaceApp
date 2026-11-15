using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

/// <summary>
/// Store only a minimal email intent; worker generates fresh Identity tokens
/// and fetches the current recipient at dispatch. No external I/O here.
/// </summary>
internal sealed class OutboxIdentityEmailSender(DirectoryDb db) : IEmailSender<IdentityUser>
{
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
        var alreadyPending = await db.IdentityEmailOutbox.AnyAsync(message =>
            message.UserId == user.Id &&
            message.Purpose == purpose &&
            message.Status == IdentityEmailOutboxStatus.Pending &&
            message.ExpiresAtUtc > now);
        if (alreadyPending)
            return;

        db.IdentityEmailOutbox.Add(new IdentityEmailOutboxMessage
        {
            UserId = user.Id,
            Purpose = purpose,
            QueuedAtUtc = now,
            AvailableAtUtc = now,
            ExpiresAtUtc = now.Add(purpose == IdentityEmailPurpose.ResetPassword
                ? TimeSpan.FromHours(2)
                : TimeSpan.FromHours(24))
        });
        // Participates in the caller's EF transaction during registration.
        await db.SaveChangesAsync();
    }
}

// Deliberate compile-time startup HOLD until the reviewed migration, worker,
// outbox leases and restart recovery have all passed, independent of config.
internal static class IdentityEmailOutboxDispatchPolicy
{
    internal static bool DispatcherAvailable => false;
}
