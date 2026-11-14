using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

/// <summary>
/// Explicit, interactive, no-listener, no-migration operator enrollment.
/// Only the FIRST PlatformAdmin can be created this way. Never invoked from
/// HTTP, startup, preview seeding or checked-in configuration.
/// </summary>
internal static class ProductionAdminProvisioner
{
    internal static async Task RunAsync(WebApplication app)
    {
        if (Environment.GetEnvironmentVariable("YANGSPACE_OFFLINE_ADMIN_PROVISION") != "CONFIRMED")
            throw new InvalidOperationException("Offline provisioning requires explicit operator consent.");

        if (Console.IsInputRedirected || Console.IsOutputRedirected)
            throw new InvalidOperationException("Offline provisioning requires an interactive console, not redirected input.");

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
        await ProductionSchemaGate.ValidateSchemaAsync(db);

        // Verify there is no PlatformAdmin *before* asking for credentials.
        var existingAdmin = await (
            from membership in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on membership.RoleId equals role.Id
            where role.Name == "PlatformAdmin"
            select membership.UserId
        ).AnyAsync();
        if (existingAdmin)
            throw new InvalidOperationException(
                "A PlatformAdmin already exists. Subsequent grants require a separate audited operator workflow.");

        Console.Write("New operator email: ");
        var email = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(email) || email.Length > 254 || !email.Contains('@'))
            throw new InvalidOperationException("A valid operator email is required.");

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (await userManager.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("An existing account may not be promoted by offline provisioning.");

        Console.Write("New password (hidden): ");
        var password = ReadHidden();
        Console.Write("Confirm password (hidden): ");
        var confirmation = ReadHidden();
        if (password != confirmation)
            throw new InvalidOperationException("Password confirmation does not match.");

        // Same shared EF scope under one transaction. A partially created
        // identity/role is not left behind if any Identity operation fails.
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Re-check inside the write transaction: two simultaneous operator
        // sessions must not both pass the initial read-side preflight.
        var alreadyGranted = await (
            from membership in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on membership.RoleId equals role.Id
            where role.Name == "PlatformAdmin"
            select membership.UserId
        ).AnyAsync();
        if (alreadyGranted)
            throw new InvalidOperationException("PlatformAdmin enrollment was completed by another operator.");

        if (!await roleManager.RoleExistsAsync("PlatformAdmin"))
            Require(await roleManager.CreateAsync(new IdentityRole("PlatformAdmin")));

        var user = new IdentityUser
        {
            UserName = email, Email = email, EmailConfirmed = true
        };
        Require(await userManager.CreateAsync(user, password));
        Require(await userManager.AddToRoleAsync(user, "PlatformAdmin"));
        await transaction.CommitAsync();

        Console.WriteLine("OFFLINE_ADMIN_PROVISION=PASS");
    }

    private static string ReadHidden()
    {
        var buffer = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return buffer.ToString();
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0) buffer.Length--;
                continue;
            }
            if (!char.IsControl(key.KeyChar) && buffer.Length < 256)
                buffer.Append(key.KeyChar);
        }
    }

    private static void Require(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Operator enrollment failed: " +
                string.Join(",", result.Errors.Select(x => x.Code)));
    }
}
