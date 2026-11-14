using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using YangSpace.Api;

namespace YangSpace.PostgreSql.Migrations;

/// <summary>
/// PostgreSQL-only migration scaffolding. The dummy connection is never opened.
/// No API startup, credentials, environment config or live database are read.
/// </summary>
public sealed class PostgreSqlDirectoryDbDesignTimeFactory
    : IDesignTimeDbContextFactory<DirectoryDb>
{
    public DirectoryDb CreateDbContext(string[] args)
    {
        if (Environment.GetEnvironmentVariable("YANGSPACE_EF_POSTGRESQL_DESIGNTIME") != "CONFIRMED")
            throw new InvalidOperationException(
                "REFUSED: PostgreSQL EF design-time execution requires explicit operator opt-in.");
        if (args.Length != 0)
            throw new InvalidOperationException(
                "REFUSED: PostgreSQL EF design-time factory accepts no connection or environment arguments.");

        const string noConnectDesignTime =
            "Host=127.0.0.1;Port=5432;Database=yangspace_ef_design_time;" +
            "Username=ef_design_time;Password=not-a-secret;SSL Mode=Disable;Timeout=3;Command Timeout=3";

        var options = new DbContextOptionsBuilder<DirectoryDb>()
            .UseNpgsql(noConnectDesignTime, npgsql =>
                npgsql.MigrationsAssembly("YangSpace.PostgreSql.Migrations"))
            .Options;

        return new DirectoryDb(options);
    }
}
