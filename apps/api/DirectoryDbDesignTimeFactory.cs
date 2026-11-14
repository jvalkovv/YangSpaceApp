using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YangSpace.Api;

/// <summary>
/// Design-time scaffolding only: no app startup, no credentials, no live DB.
/// EF CLI commands deliberately use the same SQLite provider and DirectoryDb
/// model configuration as runtime. All operator invocations require opt-in.
/// </summary>
public sealed class DirectoryDbDesignTimeFactory : IDesignTimeDbContextFactory<DirectoryDb>
{
    public DirectoryDb CreateDbContext(string[] args)
    {
        if (Environment.GetEnvironmentVariable("YANGSPACE_EF_DESIGNTIME") != "CONFIRMED")
            throw new InvalidOperationException(
                "REFUSED: EF design-time execution requires explicit operator opt-in.");
        if (args.Length != 0)
            throw new InvalidOperationException(
                "REFUSED: EF design-time factory accepts no database or environment arguments.");

        // The factory never opens a connection or reads ConnectionStrings__Main.
        // EF scaffolds schema from the model without touching any database.
        var options = new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new DirectoryDb(options);
    }
}
