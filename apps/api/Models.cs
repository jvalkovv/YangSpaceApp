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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
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
