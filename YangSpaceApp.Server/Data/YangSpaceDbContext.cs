using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YangSpaceApp.Server.Data.Models;

namespace YangSpaceApp.Server.Data
{
    public class YangSpaceDbContext : IdentityDbContext

    {
        public YangSpaceDbContext(DbContextOptions<YangSpaceDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User-Provider Relationships
            modelBuilder.Entity<User>()
                .HasMany(u => u.Services)
                .WithOne(s => s.Provider)
                .HasForeignKey(s => s.ProviderId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Bookings)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Reviews)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId);

            // Category-Service Relationship
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Services)
                .WithOne(s => s.Category)
                .HasForeignKey(s => s.CategoryId);

            // Service-Review Relationship
            modelBuilder.Entity<Service>()
                .HasMany(s => s.Reviews)
                .WithOne(r => r.Service)
                .HasForeignKey(r => r.ServiceId);
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Availability> Availabilities { get; set; }

        public DbSet<ServiceImage> ServiceImages { get; set; }
    }
}
