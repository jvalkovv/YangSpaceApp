using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

public static class DirectoryEndpoints
{
    public static void MapDirectory(this WebApplication app)
    {
        var publicApi = app.MapGroup("/api/v1").WithTags("Public directory");
        publicApi.MapGet("/meta", async (DirectoryDb db, CancellationToken ct) =>
        {
            var locations = await db.Locations.AsNoTracking().Where(x => x.IsPublished).ToListAsync(ct);
            return Results.Ok(new
            {
                Cities = locations.Select(x => x.City).Distinct().OrderBy(x => x).ToArray(),
                Neighborhoods = locations.Select(x => x.Neighborhood).Distinct().OrderBy(x => x).ToArray(),
                Categories = locations.Select(x => x.Category).Distinct().OrderBy(x => x).ToArray()
            });
        });

        publicApi.MapGet("/locations", async (
            DirectoryDb db, string? neighborhood, string? category, string? q,
            int? page, int? pageSize, CancellationToken ct) =>
        {
            var n = Math.Clamp(page ?? 1, 1, 100_000);
            var size = Math.Clamp(pageSize ?? 12, 1, 48);
            var now = DateTime.UtcNow;
            var query = db.Locations.AsNoTracking().Where(x => x.IsPublished);
            if (!string.IsNullOrWhiteSpace(neighborhood)) query = query.Where(x => x.Neighborhood == neighborhood);
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 100) return Results.BadRequest(new { error = "Search query is too long." });
                query = query.Where(x => x.Name.Contains(term) || x.Description.Contains(term) || x.Address.Contains(term));
            }
            var count = await query.CountAsync(ct);
            var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id)
                .Skip((n - 1) * size).Take(size)
                .Select(x => new LocationCard(x.Id, x.Slug, x.Name, x.Neighborhood, x.Category,
                    x.Description, x.Address, x.ImageUrl, x.IsVerified,
                    x.Offers.Count(o => o.IsPublished && o.StartsAtUtc <= now && o.EndsAtUtc > now),
                    x.Jobs.Count(j => j.IsPublished && j.ExpiresAtUtc > now)))
                .ToListAsync(ct);
            return Results.Ok(new Paged<LocationCard>(items, n, size, count));
        });

        publicApi.MapGet("/locations/{slug}", async (DirectoryDb db, string slug, CancellationToken ct) =>
        {
            var x = await db.Locations.AsNoTracking().Include(x => x.Offers)
                .Include(x => x.Brochures).Include(x => x.Jobs)
                .SingleOrDefaultAsync(x => x.Slug == slug && x.IsPublished, ct);
            if (x is null) return Results.NotFound();
            var now = DateTime.UtcNow;
            return Results.Ok(new LocationDetail(x.Id, x.Slug, x.Name, x.City, x.Neighborhood,
                x.Category, x.Description, x.Address, x.Phone, x.Website, x.ImageUrl,
                x.OpeningHours, x.IsVerified, x.UpdatedAtUtc,
                x.Offers.Where(o => o.IsPublished && o.StartsAtUtc <= now && o.EndsAtUtc > now).OrderBy(o => o.EndsAtUtc).ToList(),
                x.Brochures.Where(b => b.IsPublished && b.ValidFromUtc <= now && b.ValidToUtc > now).OrderBy(b => b.ValidToUtc).ToList(),
                x.Jobs.Where(j => j.IsPublished && j.ExpiresAtUtc > now).OrderBy(j => j.ExpiresAtUtc).ToList()));
        });
    }
}
