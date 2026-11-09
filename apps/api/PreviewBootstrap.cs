using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

internal static class PreviewBootstrap
{
    public static async Task Initialize(WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Preview:Enabled")) return;
        if (app.Environment.IsProduction())
            throw new InvalidOperationException("Preview mode must not run in Production.");

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
        // EnsureCreated is for an isolated preview DB only. NOT a production migration strategy.
        await db.Database.EnsureCreatedAsync();
        if (!await db.Locations.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var samples = new[]
            {
                ("pekarna-demo", "Пекарна „Сутрин“", "Пекарни", "Цветен квартал", "Топъл хляб и закуски през целия ден."),
                ("studio-demo", "Студио „Линия“", "Красота", "Цветен квартал", "Подстригване, грижа и услуги с предварително записване."),
                ("market-demo", "Магазин „При нас“", "Хранителни магазини", "Цветен квартал", "Квартален магазин с продукти за дома."),
                ("cafe-demo", "Кафене „Ъгълът“", "Заведения", "Левски", "Кафе, десерти и спокойна атмосфера."),
                ("bike-demo", "Вело сервиз „Педал“", "Услуги", "Левски", "Сервиз и поддръжка на велосипеди."),
                ("pet-demo", "Зоомагазин „Мустаци“", "Магазини", "Чайка", "Храна и аксесоари за домашни любимци.")
            };
            foreach (var (slug, name, category, neighborhood, description) in samples)
            {
                var x = new BusinessLocation {
                    Slug = slug, Name = name, Category = category, Neighborhood = neighborhood,
                    Description = description, Address = "Демо обект — адресът предстои",
                    OpeningHours = "Примерно работно време — непотвърдено", IsPublished = true, IsVerified = false
                };
                x.Offers.Add(new LocalOffer {
                    Title = "Примерно предложение", Description = "Демонстрационно съдържание, не е реална промоция.",
                    StartsAtUtc = now.AddDays(-1), EndsAtUtc = now.AddDays(30), IsPublished = true
                });
                if (slug == "market-demo") x.Brochures.Add(new LocalBrochure {
                    Title = "Примерна седмична брошура", Url = "https://example.org/catalogue.pdf",
                    ValidFromUtc = now.AddDays(-1), ValidToUtc = now.AddDays(7), IsPublished = true
                });
                if (slug == "cafe-demo") x.Jobs.Add(new LocalJob {
                    Title = "Примерна обява: бариста", Description = "Демо позиция — не кандидатствайте.",
                    ApplyUrl = "https://example.org/jobs", ExpiresAtUtc = now.AddDays(30), IsPublished = true
                });
                db.Locations.Add(x);
            }
            await db.SaveChangesAsync();
        }

        var email = app.Configuration["Bootstrap:AdminEmail"];
        var password = app.Configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password)) return;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Both bootstrap admin email and password are required.");
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("PlatformAdmin"))
            Check(await roles.CreateAsync(new IdentityRole("PlatformAdmin")));
        var user = await users.FindByEmailAsync(email);
        var createdByBootstrap = user is null;
        if (user is null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            Check(await users.CreateAsync(user, password));
        }
        // Bootstrap is not an upgrade path for accounts created by self-registration.
        // Fail closed if the configured identity already exists without the admin role.
        // This prevents turning an arbitrary ordinary account into PlatformAdmin.
        if (!await users.IsInRoleAsync(user, "PlatformAdmin"))
        {
            if (!createdByBootstrap)
                throw new InvalidOperationException(
                    "Existing non-admin account cannot be promoted through bootstrap.");
            Check(await users.AddToRoleAsync(user, "PlatformAdmin"));
        }
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(
            "Identity bootstrap failed: " + string.Join("; ", result.Errors.Select(e => e.Code)));
    }
}
