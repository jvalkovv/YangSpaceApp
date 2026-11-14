using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

public sealed record PublicDirectoryMeta(string[] Cities, string[] Neighborhoods, string[] Categories);
public sealed record PublicHighlight(string Slug, string LocationName, string Neighborhood, string Category, string Title, DateTime ExpiresAtUtc);
public sealed record HomepagePayload(
    PublicDirectoryMeta Meta, Paged<LocationCard> Directory,
    IReadOnlyList<PublicHighlight> Offers, IReadOnlyList<PublicHighlight> Brochures, IReadOnlyList<PublicHighlight> Jobs);

public static class DirectoryEndpoints
{
    private static IQueryable<BusinessLocation> Published(DirectoryDb db) =>
        db.Locations.AsNoTracking().Where(x => x.IsPublished);

    private static IQueryable<LocationCard> ToCards(IQueryable<BusinessLocation> query, DateTime now) =>
        query.Select(x => new LocationCard(
            x.Id, x.Slug, x.Name, x.Neighborhood, x.Category,
            x.Description, x.Address, x.ImageUrl, x.IsVerified,
            x.Offers.Count(o => o.IsPublished && o.StartsAtUtc <= now && o.EndsAtUtc > now),
            x.Jobs.Count(j => j.IsPublished && j.ExpiresAtUtc > now)));

    // The homepage receives its catalog, filter options and highlights through ONE HTTP request.
    // Database queries remain individually observable; no premature full-table entity loading.
    public static void MapDirectory(this WebApplication app)
    {
        var publicApi = app.MapGroup("/api/v1").WithTags("Public directory");

        publicApi.MapGet("/homepage", async (DirectoryDb db, CancellationToken ct) =>
        {
            var now = DateTime.UtcNow;
            var locations = Published(db);
            var facets = await locations
                .Select(x => new { x.City, x.Neighborhood, x.Category })
                .Distinct().ToListAsync(ct);
            var meta = new PublicDirectoryMeta(
                facets.Select(x => x.City).Distinct().OrderBy(x => x).ToArray(),
                facets.Select(x => x.Neighborhood).Distinct().OrderBy(x => x).ToArray(),
                facets.Select(x => x.Category).Distinct().OrderBy(x => x).ToArray());
            var total = await locations.CountAsync(ct);
            var featured = await ToCards(locations.OrderBy(x => x.Name).ThenBy(x => x.Id), now)
                .Take(12).ToListAsync(ct);

            var offers = await (
                from offer in db.Offers.AsNoTracking()
                join loc in locations on offer.LocationId equals loc.Id
                where offer.IsPublished && offer.StartsAtUtc <= now && offer.EndsAtUtc > now
                orderby offer.EndsAtUtc, loc.Name
                select new PublicHighlight(loc.Slug, loc.Name, loc.Neighborhood, loc.Category, offer.Title, offer.EndsAtUtc)
            ).Take(3).ToListAsync(ct);

            var brochures = await (
                from brochure in db.Brochures.AsNoTracking()
                join loc in locations on brochure.LocationId equals loc.Id
                where brochure.IsPublished && brochure.ValidFromUtc <= now && brochure.ValidToUtc > now
                orderby brochure.ValidToUtc, loc.Name
                select new PublicHighlight(loc.Slug, loc.Name, loc.Neighborhood, loc.Category, brochure.Title, brochure.ValidToUtc)
            ).Take(3).ToListAsync(ct);

            var jobs = await (
                from job in db.Jobs.AsNoTracking()
                join loc in locations on job.LocationId equals loc.Id
                where job.IsPublished && job.ExpiresAtUtc > now
                orderby job.ExpiresAtUtc, loc.Name
                select new PublicHighlight(loc.Slug, loc.Name, loc.Neighborhood, loc.Category, job.Title, job.ExpiresAtUtc)
            ).Take(3).ToListAsync(ct);

            return Results.Ok(new HomepagePayload(meta, new Paged<LocationCard>(featured, 1, 12, total), offers, brochures, jobs));
        });

        publicApi.MapGet("/meta", async (DirectoryDb db, CancellationToken ct) =>
        {
            var facets = await Published(db).Select(x => new { x.City, x.Neighborhood, x.Category }).Distinct().ToListAsync(ct);
            return Results.Ok(new PublicDirectoryMeta(
                facets.Select(x => x.City).Distinct().OrderBy(x => x).ToArray(),
                facets.Select(x => x.Neighborhood).Distinct().OrderBy(x => x).ToArray(),
                facets.Select(x => x.Category).Distinct().OrderBy(x => x).ToArray()));
        });

        publicApi.MapGet("/locations", async (
            DirectoryDb db, string? neighborhood, string? category, string? q,
            int? page, int? pageSize, CancellationToken ct) =>
        {
            var n = Math.Clamp(page ?? 1, 1, 100_000);
            var size = Math.Clamp(pageSize ?? 12, 1, 48);
            var now = DateTime.UtcNow;
            var query = Published(db);
            if (!string.IsNullOrWhiteSpace(neighborhood)) query = query.Where(x => x.Neighborhood == neighborhood);
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length > 100) return Results.BadRequest(new { error = "Search query is too long." });
                query = query.Where(x => x.Name.Contains(term) || x.Description.Contains(term) || x.Address.Contains(term));
            }
            var count = await query.CountAsync(ct);
            var items = await ToCards(query.OrderBy(x => x.Name).ThenBy(x => x.Id), now)
                .Skip((n - 1) * size).Take(size).ToListAsync(ct);
            return Results.Ok(new Paged<LocationCard>(items, n, size, count));
        });

        publicApi.MapGet("/locations/{slug}", async (DirectoryDb db, string slug, CancellationToken ct) =>
        {
            // SplitQuery avoids multiplicative row growth as offers/jobs/brochures increase.
            var x = await Published(db).AsSplitQuery()
                .Include(x => x.Offers).Include(x => x.Brochures).Include(x => x.Jobs)
                .SingleOrDefaultAsync(x => x.Slug == slug, ct);
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
