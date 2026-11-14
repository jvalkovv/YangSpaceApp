using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

/// <summary>
/// Production DB readiness check. Never modifies schema or seeds users/data.
/// Reviewed offline migrations and first PlatformAdmin enrollment are mandatory.
/// </summary>
internal static class ProductionSchemaGate
{
    internal static async Task ValidateSchemaAsync(DirectoryDb db)
    {
        if (!db.Database.IsSqlite())
            throw new InvalidOperationException("Production CMS requires the reviewed SQLite persistence provider.");

        var knownMigrations = db.Database.GetMigrations().ToArray();
        if (knownMigrations.Length == 0)
            throw new InvalidOperationException(
                "Production CMS blocked: no reviewed EF Core migration has been committed.");

        if (!await db.Database.CanConnectAsync())
            throw new InvalidOperationException(
                "Production CMS blocked: configured database is not reachable.");

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
        if (pending.Length != 0)
            throw new InvalidOperationException(
                "Production CMS blocked: database migrations must be applied offline before startup.");

        // An updated EF model without a reviewed migration is also a NO-GO,
        // even if the DB has applied every *existing* migration identifier.
        if (db.Database.HasPendingModelChanges())
            throw new InvalidOperationException(
                "Production CMS blocked: EF model differs from the reviewed migration snapshot.");

        await ValidateForeignKeyIntegrityAsync(db);
    }

    private static async Task ValidateForeignKeyIntegrityAsync(DirectoryDb db)
    {
        // PRAGMA foreign_keys is per-connection. Inspect the actual EF
        // connection, not only the configured connection-string text.
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

            // Detect orphan rows created by older connections that may have
            // run with enforcement disabled. This check is read-only.
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

    internal static async Task ValidateAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
        await ValidateSchemaAsync(db);

        // No startup-time admin grants. Operator enrollment is interactive
        // and deliberately separate from the web server entry point.
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
