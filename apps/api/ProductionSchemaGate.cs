using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

/// <summary>
/// Read-only production DB readiness gate. It never migrates or seeds.
/// Provider-specific migration assembly must be selected explicitly at DI setup.
/// </summary>
internal static class ProductionSchemaGate
{
    private const string SqliteProvider = "Microsoft.EntityFrameworkCore.Sqlite";
    private const string PostgreSqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";

    internal static async Task ValidateSchemaAsync(DirectoryDb db)
    {
        var provider = db.Database.ProviderName;
        if (provider is not SqliteProvider and not PostgreSqlProvider)
            throw new InvalidOperationException(
                "Production CMS blocked: unsupported or ambiguous EF persistence provider.");

        var knownMigrations = db.Database.GetMigrations().ToArray();
        if (knownMigrations.Length == 0)
            throw new InvalidOperationException(
                "Production CMS blocked: no reviewed provider-specific EF migration has been committed.");

        if (!await db.Database.CanConnectAsync())
            throw new InvalidOperationException(
                "Production CMS blocked: configured database is not reachable.");

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
        if (pending.Length != 0)
            throw new InvalidOperationException(
                "Production CMS blocked: database migrations must be applied offline before startup.");

        if (db.Database.HasPendingModelChanges())
            throw new InvalidOperationException(
                "Production CMS blocked: EF model differs from the selected provider's reviewed migration snapshot.");

        if (provider == SqliteProvider)
            await ValidateSqliteIntegrityAsync(db);
        else
            await ValidatePostgreSqlIntegrityAsync(db);
    }

    private static async Task ValidateSqliteIntegrityAsync(DirectoryDb db)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            var connection = db.Database.GetDbConnection();
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_keys;";
                var value = await command.ExecuteScalarAsync();
                if (Convert.ToInt32(value, CultureInfo.InvariantCulture) != 1)
                    throw new InvalidOperationException(
                        "Production CMS blocked: SQLite foreign-key enforcement is disabled.");
            }

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_key_check;";
                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                    throw new InvalidOperationException(
                        "Production CMS blocked: SQLite foreign-key integrity check found violations.");
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task ValidatePostgreSqlIntegrityAsync(DirectoryDb db)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            var connection = db.Database.GetDbConnection();

            // The web process uses a least-privileged runtime role, never the
            // schema owner/migration role or a PostgreSQL superuser.
            await using (var role = connection.CreateCommand())
            {
                role.CommandText = """
                    SELECT r.rolsuper, r.rolcreatedb, r.rolcreaterole,
                           r.rolreplication, r.rolbypassrls,
                           has_schema_privilege(current_user, 'public', 'USAGE'),
                           has_schema_privilege(current_user, 'public', 'CREATE')
                    FROM pg_roles AS r
                    WHERE r.rolname = current_user;
                    """;
                await using var reader = await role.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    throw new InvalidOperationException(
                        "Production CMS blocked: PostgreSQL runtime role could not be verified.");

                var elevated = reader.GetBoolean(0) || reader.GetBoolean(1) ||
                    reader.GetBoolean(2) || reader.GetBoolean(3) || reader.GetBoolean(4);
                var canUseSchema = reader.GetBoolean(5);
                var canCreateSchemaObjects = reader.GetBoolean(6);
                if (elevated || !canUseSchema || canCreateSchemaObjects)
                    throw new InvalidOperationException(
                        "Production CMS blocked: PostgreSQL runtime role must be non-elevated, have public schema USAGE, and have no schema CREATE privilege.");
            }

            await using (var foreignKeys = connection.CreateCommand())
            {
                foreignKeys.CommandText = """
                    SELECT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE connamespace = 'public'::regnamespace
                          AND contype = 'f'
                          AND NOT convalidated
                    );
                    """;
                if (Convert.ToBoolean(await foreignKeys.ExecuteScalarAsync(), CultureInfo.InvariantCulture))
                    throw new InvalidOperationException(
                        "Production CMS blocked: PostgreSQL has unvalidated foreign-key constraints.");
            }

            await using (var indexes = connection.CreateCommand())
            {
                indexes.CommandText = """
                    SELECT EXISTS (
                        SELECT 1
                        FROM pg_index AS i
                        JOIN pg_class AS t ON t.oid = i.indrelid
                        JOIN pg_namespace AS n ON n.oid = t.relnamespace
                        WHERE n.nspname = 'public'
                          AND (NOT i.indisvalid OR NOT i.indisready)
                    );
                    """;
                if (Convert.ToBoolean(await indexes.ExecuteScalarAsync(), CultureInfo.InvariantCulture))
                    throw new InvalidOperationException(
                        "Production CMS blocked: PostgreSQL has invalid or not-ready indexes.");
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    internal static async Task ValidateAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
        await ValidateSchemaAsync(db);

        var hasAdmin = await (
            from membership in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on membership.RoleId equals role.Id
            join user in db.Users.AsNoTracking() on membership.UserId equals user.Id
            where role.Name == "PlatformAdmin" && user.EmailConfirmed
            select user.Id
        ).AnyAsync();

        if (!hasAdmin)
            throw new InvalidOperationException(
                "Production CMS blocked: no confirmed PlatformAdmin account has been provisioned offline.");
    }
}
