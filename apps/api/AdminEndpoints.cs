using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

public static class AdminEndpoints
{
    public static void MapAdmin(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin").RequireAuthorization("PlatformAdmin").WithTags("Admin (preview only)");
        admin.MapGet("/me", (System.Security.Claims.ClaimsPrincipal user) => Results.Ok(new { email = user.Identity?.Name, role = "PlatformAdmin" }));
        admin.MapContentManagement();
        admin.MapGet("/locations", async (DirectoryDb db, CancellationToken ct) =>
        {
            var items = await db.Locations.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
            return Results.Ok(items);
        });
        admin.MapPost("/locations", async (DirectoryDb db, LocationInput input, CancellationToken ct) =>
        {
            if (!InputChecks.Location(input)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["location"] = ["Invalid location data."] });
            if (await db.Locations.AnyAsync(x => x.Slug == input.Slug, ct)) return Results.Conflict(new { error = "Slug already exists." });
            var x = new BusinessLocation();
            InputChecks.Apply(input, x);
            db.Locations.Add(x);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/locations/{x.Slug}", new { x.Id, x.Slug });
        });
        admin.MapPut("/locations/{id:guid}", async (DirectoryDb db, Guid id, LocationInput input, CancellationToken ct) =>
        {
            if (!InputChecks.Location(input)) return Results.BadRequest(new { error = "Invalid location data." });
            var x = await db.Locations.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (x is null) return Results.NotFound();
            if (await db.Locations.AnyAsync(y => y.Id != id && y.Slug == input.Slug, ct)) return Results.Conflict(new { error = "Slug already exists." });
            InputChecks.Apply(input, x);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { x.Id, x.Slug });
        });
        // Non-destructive publication change; no public hard delete.
        // Restore is intentionally idempotent: returning to a published state
        // must not require resubmitting the entire location and risking stale edits.
        admin.MapPost("/locations/{id:guid}/restore", async (DirectoryDb db, Guid id, CancellationToken ct) =>
        {
            var x = await db.Locations.FindAsync(new object?[] { id }, ct);
            if (x is null) return Results.NotFound();
            if (!x.IsPublished)
            {
                x.IsPublished = true;
                x.UpdatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            return Results.NoContent();
        });
        admin.MapPost("/locations/{id:guid}/hide", async (DirectoryDb db, Guid id, CancellationToken ct) =>
        {
            var x = await db.Locations.FindAsync(new object?[] { id }, ct);
            if (x is null) return Results.NotFound();
            x.IsPublished = false;
            x.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
        admin.MapPost("/locations/{id:guid}/offers", async (DirectoryDb db, Guid id, OfferInput input, CancellationToken ct) =>
        {
            if (!InputChecks.ShortText(input.Title, 120, 3) || !InputChecks.ShortText(input.Description, 2000, 0)
                || !InputChecks.Dates(input.StartsAtUtc, input.EndsAtUtc))
                return Results.BadRequest(new { error = "Invalid offer or period." });
            if (!await db.Locations.AnyAsync(x => x.Id == id, ct)) return Results.NotFound();
            var x = new LocalOffer { LocationId = id, Title = input.Title!.Trim(), Description = (input.Description ?? "").Trim(),
                StartsAtUtc = input.StartsAtUtc.ToUniversalTime(), EndsAtUtc = input.EndsAtUtc.ToUniversalTime(), IsPublished = input.IsPublished };
            db.Offers.Add(x);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/locations/{id}/offers/{x.Id}", new { x.Id });
        });
        admin.MapPost("/locations/{id:guid}/brochures", async (DirectoryDb db, Guid id, BrochureInput input, CancellationToken ct) =>
        {
            if (!InputChecks.ShortText(input.Title, 120, 3) || !InputChecks.HttpsUrl(input.Url, false)
                || !InputChecks.Dates(input.ValidFromUtc, input.ValidToUtc))
                return Results.BadRequest(new { error = "Invalid brochure or period; URL must use HTTPS." });
            if (!await db.Locations.AnyAsync(x => x.Id == id, ct)) return Results.NotFound();
            var x = new LocalBrochure { LocationId = id, Title = input.Title!.Trim(), Url = input.Url!.Trim(),
                ValidFromUtc = input.ValidFromUtc.ToUniversalTime(), ValidToUtc = input.ValidToUtc.ToUniversalTime(), IsPublished = input.IsPublished };
            db.Brochures.Add(x);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/locations/{id}/brochures/{x.Id}", new { x.Id });
        });
        admin.MapPost("/locations/{id:guid}/jobs", async (DirectoryDb db, Guid id, JobInput input, CancellationToken ct) =>
        {
            if (!InputChecks.ShortText(input.Title, 120, 3) || !InputChecks.ShortText(input.Description, 2000, 0)
                || !InputChecks.HttpsUrl(input.ApplyUrl, false)
                || input.ExpiresAtUtc <= DateTime.UtcNow || input.ExpiresAtUtc > DateTime.UtcNow.AddYears(1))
                return Results.BadRequest(new { error = "Invalid job or expiry; application URL must use HTTPS." });
            if (!await db.Locations.AnyAsync(x => x.Id == id, ct)) return Results.NotFound();
            var x = new LocalJob { LocationId = id, Title = input.Title!.Trim(), Description = (input.Description ?? "").Trim(),
                ApplyUrl = input.ApplyUrl!.Trim(), ExpiresAtUtc = input.ExpiresAtUtc.ToUniversalTime(), IsPublished = input.IsPublished };
            db.Jobs.Add(x);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/locations/{id}/jobs/{x.Id}", new { x.Id });
        });
    }
}
