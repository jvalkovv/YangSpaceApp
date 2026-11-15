using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

public sealed class DirectoryDb(DbContextOptions<DirectoryDb> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<BusinessLocation> Locations => Set<BusinessLocation>();
    public DbSet<LocalOffer> Offers => Set<LocalOffer>();
    public DbSet<LocalBrochure> Brochures => Set<LocalBrochure>();
    public DbSet<LocalJob> Jobs => Set<LocalJob>();

    // F2a-1: schema foundation only. No public merchant write capability.
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<BusinessClaim> BusinessClaims => Set<BusinessClaim>();
    public DbSet<ContentAuditEvent> ContentAuditEvents => Set<ContentAuditEvent>();
    // F2b email intent: protected by FK; no token or message body persisted.
    public DbSet<IdentityEmailOutboxMessage> IdentityEmailOutbox => Set<IdentityEmailOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Legacy/demo locations have no verified owner. The nullable FK is
        // intentional; no owner is fabricated from historic records.
        builder.Entity<BusinessLocation>()
            .HasOne<Organization>().WithMany()
            .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<BusinessLocation>().HasIndex(x => x.OrganizationId);
        // Application-managed revision. A stale expected revision fails EF's
        // UPDATE ... WHERE "Revision" = @original concurrency predicate.
        // HTTP 409 mapping and transactional audit are later F2b gates.
        builder.Entity<BusinessLocation>()
            .Property(x => x.Revision).IsConcurrencyToken();

        builder.Entity<Organization>(org =>
        {
            org.Property(x => x.Name).HasMaxLength(120).IsRequired();
            org.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            org.Property(x => x.Revision).IsConcurrencyToken();
            org.HasIndex(x => x.Status);
        });

        builder.Entity<OrganizationMember>(member =>
        {
            member.HasOne<Organization>().WithMany()
                .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            member.HasOne<IdentityUser>().WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            member.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
            member.Property(x => x.UserId).IsRequired();
            member.Property(x => x.GrantedByUserId).IsRequired();
            member.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
            member.Property(x => x.Revision).IsConcurrencyToken();
        });

        builder.Entity<BusinessClaim>(claim =>
        {
            claim.HasOne<Organization>().WithMany()
                .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            claim.HasOne<BusinessLocation>().WithMany()
                .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            claim.HasOne<IdentityUser>().WithMany()
                .HasForeignKey(x => x.ApplicantUserId).OnDelete(DeleteBehavior.Restrict);
            claim.HasIndex(x => new { x.LocationId, x.Status });
            claim.HasIndex(x => new { x.OrganizationId, x.ApplicantUserId });

            // Review policy: multiple distinct organizations may submit
            // competing pending claims for a location. Each organization
            // may have at most one Pending claim per location; a resolved
            // (Rejected/Withdrawn) claim permits a new application.
            // Only one Approved claim may exist for the location, globally.
            // Status is persisted as a string for both SQLite and PostgreSQL.
            claim.HasIndex(x => x.LocationId, "UX_BusinessClaims_ApprovedLocation")
                .IsUnique()
                .HasFilter("\"Status\" = 'Approved'");
            claim.HasIndex(x => new { x.LocationId, x.OrganizationId },
                    "UX_BusinessClaims_PendingLocationOrganization")
                .IsUnique()
                .HasFilter("\"Status\" = 'Pending'");
            claim.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            claim.Property(x => x.ApplicantUserId).IsRequired();
            claim.Property(x => x.DecisionReason).HasMaxLength(500);
            claim.Property(x => x.Revision).IsConcurrencyToken();
        });

        // Schema only: event writes are forbidden until F2b implements
        // atomic business mutation + allowlisted audit in one transaction.
        builder.Entity<ContentAuditEvent>(audit =>
        {
            audit.HasOne<BusinessLocation>().WithMany()
                .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            audit.HasOne<Organization>().WithMany()
                .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            audit.Property(x => x.ActorUserId).IsRequired();
            audit.Property(x => x.ActorRole).HasMaxLength(48).IsRequired();
            audit.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
            audit.Property(x => x.Action).HasMaxLength(40).IsRequired();
            audit.Property(x => x.CorrelationId).HasMaxLength(96).IsRequired();
            audit.Property(x => x.ChangedFieldsJson).IsRequired();
            audit.Property(x => x.Reason).HasMaxLength(500);
            audit.HasIndex(x => new { x.EntityType, x.EntityId, x.RevisionAfter }).IsUnique();
            audit.HasIndex(x => new { x.LocationId, x.OccurredAtUtc });
        });

        builder.Entity<IdentityEmailOutboxMessage>(message =>
        {
            message.HasOne<IdentityUser>().WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            message.Property(x => x.UserId).IsRequired();
            message.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(32);
            message.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            message.Property(x => x.Revision).IsConcurrencyToken();
            message.Property(x => x.LastFailureCode).HasMaxLength(64);
            message.HasIndex(x => new { x.Status, x.AvailableAtUtc });
            message.HasIndex(x => new { x.UserId, x.Purpose, x.Status });
        });

        builder.Entity<BusinessLocation>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<BusinessLocation>().HasIndex(x => new { x.City, x.Neighborhood, x.Category });
        builder.Entity<LocalOffer>().HasIndex(x => new { x.LocationId, x.IsPublished, x.EndsAtUtc });
        builder.Entity<LocalBrochure>().HasIndex(x => new { x.LocationId, x.IsPublished, x.ValidToUtc });
        builder.Entity<LocalJob>().HasIndex(x => new { x.LocationId, x.IsPublished, x.ExpiresAtUtc });
        builder.Entity<BusinessLocation>().HasMany(x => x.Offers).WithOne().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<BusinessLocation>().HasMany(x => x.Brochures).WithOne().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<BusinessLocation>().HasMany(x => x.Jobs).WithOne().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BusinessLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string City { get; set; } = "Варна";
    public string Neighborhood { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? ImageUrl { get; set; }
    public string? OpeningHours { get; set; }
    public bool IsPublished { get; set; }
    public bool IsVerified { get; set; }
    // Null = unclaimed legacy/demo location, never an implicit owner grant.
    public Guid? OrganizationId { get; set; }
    // Application-managed optimistic concurrency token. Defaults to 0
    // for new and historical rows; callers must increment on changes.
    public long Revision { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<LocalOffer> Offers { get; set; } = [];
    public List<LocalBrochure> Brochures { get; set; } = [];
    public List<LocalJob> Jobs { get; set; } = [];
}
public sealed class LocalOffer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LocationId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsPublished { get; set; }
    // Null for legacy rows: dates must not be fabricated during migration.
    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
public sealed class LocalBrochure
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LocationId { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime ValidFromUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
    public bool IsPublished { get; set; }
    // Null for legacy rows: dates must not be fabricated during migration.
    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
public sealed class LocalJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LocationId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ApplyUrl { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsPublished { get; set; }
    // Null for legacy rows: dates must not be fabricated during migration.
    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
