namespace YangSpace.Api;

// Draft F2a-1 persistence model. These types do not expose any HTTP routes;
// all old PlatformAdmin routes retain their current behavior until F2b.
// Creating these EF tables is not an approval to onboard real merchants.

public enum OrganizationStatus
{
    PendingReview = 0,
    Active = 1,
    Suspended = 2
}

public enum OrganizationMemberRole
{
    Owner = 0,
    Editor = 1
}

public enum BusinessClaimStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Withdrawn = 3
}

public sealed class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public OrganizationStatus Status { get; set; } = OrganizationStatus.PendingReview;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Application must explicitly increment on each state change.
    // No organization mutations exist yet in this source slice.
    public long Revision { get; set; }
}

public sealed class OrganizationMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string UserId { get; set; } = "";
    public OrganizationMemberRole Role { get; set; } = OrganizationMemberRole.Editor;
    public string GrantedByUserId { get; set; } = "";
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
    public string? RevokedByUserId { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public long Revision { get; set; }
}

public sealed class BusinessClaim
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LocationId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ApplicantUserId { get; set; } = "";
    public BusinessClaimStatus Status { get; set; } = BusinessClaimStatus.Pending;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? DecisionReason { get; set; }
    public long Revision { get; set; }
}

public sealed class ContentAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? OrganizationId { get; set; }
    public Guid LocationId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string Action { get; set; } = "";
    public string ActorUserId { get; set; } = "";
    public string ActorRole { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public long RevisionBefore { get; set; }
    public long RevisionAfter { get; set; }
    public string CorrelationId { get; set; } = "";

    // Must be populated from a per-entity field allowlist by future F2b.
    // No raw requests, credentials, contact secrets or free-form PII.
    public string ChangedFieldsJson { get; set; } = "{}";
    public string? Reason { get; set; }
}
