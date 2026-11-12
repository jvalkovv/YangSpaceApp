using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

/// <summary>
/// Fail-closed ownership query for future private, Identity-BEARER-only
/// endpoints. Callers MUST separately require bearer authentication; this
/// helper never grants global PlatformAdmin membership or creates claims.
/// </summary>
internal static class TenantScope
{
    // A shared query keeps internal checks and the merchant HTTP read
    // contract on the same current-membership predicate. One database query
    // resolves both membership and the resource, preventing a check-then-read
    // window and exposing no membership/actor/claim details.
    internal static IQueryable<BusinessLocation> ManagedLocations(
        DirectoryDb db, ClaimsPrincipal principal)
    {
        var userId = principal.Identity?.IsAuthenticated == true
            ? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;
        if (string.IsNullOrWhiteSpace(userId))
            return db.Locations.AsNoTracking().Where(_ => false);

        return
            from loc in db.Locations.AsNoTracking()
            join org in db.Organizations.AsNoTracking()
                on loc.OrganizationId equals (Guid?)org.Id
            join member in db.OrganizationMembers.AsNoTracking()
                on org.Id equals member.OrganizationId
            where org.Status == OrganizationStatus.Active
                && member.UserId == userId
                && member.RevokedAtUtc == null
                && (member.Role == OrganizationMemberRole.Owner
                    || member.Role == OrganizationMemberRole.Editor)
            select loc;
    }

    internal static Task<bool> CanManageLocationAsync(
        DirectoryDb db, ClaimsPrincipal principal, Guid locationId,
        CancellationToken cancellationToken = default)
    {
        if (locationId == Guid.Empty)
            return Task.FromResult(false);
        return ManagedLocations(db, principal)
            .AnyAsync(loc => loc.Id == locationId, cancellationToken);
    }
}
