using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

/// <summary>
/// Platform-admin-only publication lifecycle. The caller supplies BOTH the
/// location and record identifiers; a record belonging to another location
/// returns 404 rather than permitting cross-location updates.
/// No hard delete. Archiving only changes IsPublished.
/// </summary>
public static class AdminContentEndpoints
{
    public static void MapContentManagement(this RouteGroupBuilder admin)
    {
        admin.MapGet("/locations/{id:guid}/content", async (DirectoryDb db, Guid id, CancellationToken ct) =>
        {
            if (!await db.Locations.AnyAsync(x => x.Id == id, ct)) return Results.NotFound();
            var offers = await db.Offers.AsNoTracking().Where(x => x.LocationId == id)
                .OrderByDescending(x => x.StartsAtUtc).ThenBy(x => x.Id).ToListAsync(ct);
            var brochures = await db.Brochures.AsNoTracking().Where(x => x.LocationId == id)
                .OrderByDescending(x => x.ValidFromUtc).ThenBy(x => x.Id).ToListAsync(ct);
            var jobs = await db.Jobs.AsNoTracking().Where(x => x.LocationId == id)
                .OrderByDescending(x => x.ExpiresAtUtc).ThenBy(x => x.Id).ToListAsync(ct);
            return Results.Ok(new { offers, brochures, jobs });
        });

        admin.MapPut("/locations/{id:guid}/offers/{offerId:guid}",
            async (DirectoryDb db, Guid id, Guid offerId, OfferInput input, CancellationToken ct) =>
            {
                if (!ValidOffer(input)) return Results.BadRequest(new { error = "Invalid offer or period." });
                var item = await db.Offers.SingleOrDefaultAsync(x => x.LocationId == id && x.Id == offerId, ct);
                if (item is null) return Results.NotFound();
                var title = input.Title!.Trim();
                var description = input.Description!.Trim();
                var startsAtUtc = input.StartsAtUtc.ToUniversalTime();
                var endsAtUtc = input.EndsAtUtc.ToUniversalTime();
                if (item.Title == title && item.Description == description
                    && item.StartsAtUtc == startsAtUtc && item.EndsAtUtc == endsAtUtc
                    && item.IsPublished == input.IsPublished)
                    return Results.NoContent();
                var publishing = !item.IsPublished && input.IsPublished;
                var now = DateTime.UtcNow;
                item.Title = title;
                item.Description = description;
                item.StartsAtUtc = startsAtUtc;
                item.EndsAtUtc = endsAtUtc;
                item.IsPublished = input.IsPublished;
                item.UpdatedAtUtc = now;
                if (publishing) item.PublishedAtUtc = now;
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            });
        admin.MapPost("/locations/{id:guid}/offers/{offerId:guid}/hide",
            async (DirectoryDb db, Guid id, Guid offerId, CancellationToken ct) =>
            {
                var item = await db.Offers.SingleOrDefaultAsync(x => x.LocationId == id && x.Id == offerId, ct);
                if (item is null) return Results.NotFound();
                if (item.IsPublished)
                {
                    item.IsPublished = false;
                    item.UpdatedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
                return Results.NoContent();
            });
        admin.MapPost("/locations/{id:guid}/offers/{offerId:guid}/restore",
            async (DirectoryDb db, Guid id, Guid offerId, CancellationToken ct) =>
            {
                var item = await db.Offers.SingleOrDefaultAsync(x => x.LocationId == id && x.Id == offerId, ct);
                if (item is null) return Results.NotFound();
                if (item.EndsAtUtc <= DateTime.UtcNow)
                    return Results.Conflict(new { error = "Offer has expired. Edit its period before restoring." });
                if (!item.IsPublished)
                {
                    var now = DateTime.UtcNow;
                    item.IsPublished = true;
                    item.UpdatedAtUtc = now;
                    item.PublishedAtUtc = now;
                    await db.SaveChangesAsync(ct);
                }
                return Results.NoContent();
            });

        admin.MapPut("/locations/{id:guid}/brochures/{brochureId:guid}",
            async (DirectoryDb db, Guid id, Guid brochureId, BrochureInput input, CancellationToken ct) =>
            {
                if (!ValidBrochure(input)) return Results.BadRequest(new { error = "Invalid brochure or period; URL must use HTTPS." });
                var item = await db.Brochures.SingleOrDefaultAsync(x => x.LocationId == id && x.Id == brochureId, ct);
                if (item is null) return Results.NotFound();
                var title = input.Title!.Trim();
                var url = input.Url!.Trim();
                var validFromUtc = input.ValidFromUtc.ToUniversalTime();
                var validToUtc = input.ValidToUtc.ToUniversalTime();
                if (item.Title == title && item.Url == url
                    && item.ValidFromUtc == validFromUtc && item.ValidToUtc == validToUtc
                    && item.IsPublished == input.IsPublished)
                    return Results.NoContent();
                var publishing = !item.IsPublished && input.IsPublished;
                var now = DateTime.UtcNow;
                item.Title = title;
                item.Url = url;
                item.ValidFromUtc = validFromUtc;
                item.ValidToUtc = validToUtc;
                item.IsPublished = input.IsPublished;
                item.UpdatedAtUtc = now;
                if (publishing) item.PublishedAtUtc = now;
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            });
        admin.MapPost("/locations/{id:guid}/brochures/{brochureId:guid}/hide",
            async (DirectoryDb db, Guid id, Guid brochureId, CancellationToken ct) =>
            {
                var item = await db.Brochures.SingleOrDefaultAsync(x => x.LocationId == id && x.Id == brochureId, ct);
                if (item is null) return Results.NotFound();
                if (item.IsPublished)
                {
                    item.IsPublished = false;
                    item.UpdatedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
                return Results.NoContent();
            });
        admin.MapPost("/locations/{id:guid}/brochures/{brochureId:guid}/restore",
            async (DirectoryDb db, Guid id, Guid brochureId, CancellationToken ct) =>
            {
                var item = await db.Brochures.SingleOrDefaultAsync(x => x.LocationId == id && x.Id == brochureId, ct);
                if (item is null) return Results.NotFound();
                if (item.ValidToUtc <= DateTime.UtcNow)
                    return Results.Conflict(new { error = "Brochure has expired. Edit its validity before restoring." });
                if (!item.IsPublished)
                {
                    var now = DateTime.UtcNow;
                    item.IsPublished = true;
                    item.UpdatedAtUtc = now;
                    item.PublishedAtUtc = now;
                    await db.SaveChangesAsync(ct);
                }
                return Results.NoContent();
            });

        admin.MapPut("/locations/{id:guid}/jobs/{jobId:guid}",
            async (DirectoryDb db, Guid id, Guid jobId, JobInput input, CancellationToken ct) =>
            {
                if (!ValidJob(input)) return Results.BadRequest(new { error = "Invalid job or expiry; application URL must use HTTPS." });
                var item = await db.Jobs.SingleOrDefaultAsync(x => x.LocationId == id && x.Id == jobId, ct);
                if (item is null) return Results.NotFound();
                var title = input.Title!.Trim();
                var description = input.Description!.Trim();
                var applyUrl = input.ApplyUrl!.Trim();
                var expiresAtUtc = input.ExpiresAtUtc.ToUniversalTime();
                if (item.Title == title && item.Description == description
                    && item.ApplyUrl == applyUrl && item.ExpiresAtUtc == expiresAtUtc
                    && item.IsPublished == input.IsPublished)
                    return Results.NoContent();
                var publishing = !item.IsPublished && input.IsPublished;
                var now = DateTime.UtcNow;
                item.Title = title;
                item.Description = description;
                item.ApplyUrl = applyUrl;
                item.ExpiresAtUtc = expiresAtUtc;
                item.IsPublished = input.IsPublished;
                item.UpdatedAtUtc = now;
                if (publishing) item.PublishedAtUtc = now;
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            });
        admin.MapPost("/locations/{id:guid}/jobs/{jobId:guid}/hide",
            async (DirectoryDb db, Guid id, Guid jobId, CancellationToken ct) =>
            {
                var item = await db.Jobs.SingleOrDefaultAsync(x => x.LocationId == id && x.Id == jobId, ct);
                if (item is null) return Results.NotFound();
                if (item.IsPublished)
                {
                    item.IsPublished = false;
                    item.UpdatedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
                return Results.NoContent();
            });
        admin.MapPost("/locations/{id:guid}/jobs/{jobId:guid}/restore",
            async (DirectoryDb db, Guid id, Guid jobId, CancellationToken ct) =>
            {
                var item = await db.Jobs.SingleOrDefaultAsync(x => x.LocationId == id && x.Id == jobId, ct);
                if (item is null) return Results.NotFound();
                if (item.ExpiresAtUtc <= DateTime.UtcNow)
                    return Results.Conflict(new { error = "Job has expired. Edit its expiry before restoring." });
                if (!item.IsPublished)
                {
                    var now = DateTime.UtcNow;
                    item.IsPublished = true;
                    item.UpdatedAtUtc = now;
                    item.PublishedAtUtc = now;
                    await db.SaveChangesAsync(ct);
                }
                return Results.NoContent();
            });
    }

    private static bool ValidOffer(OfferInput input) =>
        InputChecks.ShortText(input.Title, 120, 3)
        && InputChecks.ShortText(input.Description, 2000, 0)
        && InputChecks.Dates(input.StartsAtUtc, input.EndsAtUtc)
        && (!input.IsPublished || input.EndsAtUtc > DateTime.UtcNow);

    private static bool ValidBrochure(BrochureInput input) =>
        InputChecks.ShortText(input.Title, 120, 3)
        && InputChecks.HttpsUrl(input.Url, false)
        && InputChecks.Dates(input.ValidFromUtc, input.ValidToUtc)
        && (!input.IsPublished || input.ValidToUtc > DateTime.UtcNow);

    private static bool ValidJob(JobInput input) =>
        InputChecks.ShortText(input.Title, 120, 3)
        && InputChecks.ShortText(input.Description, 2000, 0)
        && InputChecks.HttpsUrl(input.ApplyUrl, false)
        && input.ExpiresAtUtc > DateTime.UtcNow
        && input.ExpiresAtUtc <= DateTime.UtcNow.AddYears(1);
}
