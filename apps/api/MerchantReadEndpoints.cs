using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

// Explicit minimal private DTO. Never serialize EF entities (phone, owner,
// member, claims, actor, evidence or audit fields) to this endpoint.
public sealed record MerchantLocationReadDto(
    Guid Id, string Slug, string Name, bool IsPublished, long Revision);

public static class MerchantReadEndpoints
{
    public static void MapMerchantRead(this WebApplication app)
    {
        var merchant = app.MapGroup("/api/v1/merchant")
            .WithTags("Merchant read QA")
            .RequireAuthorization("MerchantBearerRead");

        merchant.MapGet("/locations/{id:guid}", async (
            Guid id, DirectoryDb db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            // One joined SQL query returns null for both nonexistent and
            // out-of-scope locations. No existence oracle for foreign tenants.
            var location = await TenantScope.ManagedLocations(db, user)
                .Where(loc => loc.Id == id)
                .Select(loc => new MerchantLocationReadDto(
                    loc.Id, loc.Slug, loc.Name, loc.IsPublished, loc.Revision))
                .SingleOrDefaultAsync(ct);

            return location is null ? Results.NotFound() : Results.Ok(location);
        });
    }
}

// WebApplicationFactory locates the actual API entry point through this assembly
// marker; the runtime-test executable has its own generated Program class.
public sealed class ApiAssemblyMarker { }
