namespace YangSpace.Api;

/// <summary>
/// Durable, minimal public Identity email intent. Never persist tokens,
/// recipient addresses, SMTP payloads, passwords or secrets here.
/// Actual tokens must be generated at dispatch after a fresh role/user check.
/// </summary>
public enum IdentityEmailPurpose
{
    ConfirmEmail = 1,
    ResetPassword = 2
}

public enum IdentityEmailOutboxStatus
{
    Pending = 1,
    Leased = 2,
    Sent = 3,
    DeadLetter = 4,
    Suppressed = 5
}

public sealed class IdentityEmailOutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public IdentityEmailPurpose Purpose { get; set; }
    public IdentityEmailOutboxStatus Status { get; set; } = IdentityEmailOutboxStatus.Pending;
    public DateTime QueuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime AvailableAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddHours(24);
    public int AttemptCount { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTime? LeasedUntilUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? DeadLetterAtUtc { get; set; }
    // Allowlisted operational codes only, no exception messages or content.
    public string? LastFailureCode { get; set; }
    public long Revision { get; set; }
}
