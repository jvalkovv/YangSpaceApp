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
    internal static Task<bool> CanManageLocationAsync(
        DirectoryDb db, ClaimsPrincipal principal, Guid locationId,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true || locationId == Guid.Empty)
            return Task.FromResult(false);

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(false);

        // A claimed location must belong to an ACTIVE organization, and the
        // caller must have an unrevoked scoped membership in that organization.
        // PlatformAdmin without membership is not silently treated as owner.
        return (
            from loc in db.Locations.AsNoTracking()
            join org in db.Organizations.AsNoTracking()
                on loc.OrganizationId equals (Guid?)org.Id
            join member in db.OrganizationMembers.AsNoTracking()
                on org.Id equals member.OrganizationId
            where loc.Id == locationId
                && org.Status == OrganizationStatus.Active
                && member.UserId == userId
                && member.RevokedAtUtc == null
                && (member.Role == OrganizationMemberRole.Owner
                    || member.Role == OrganizationMemberRole.Editor)
            select member.Id
        ).AnyAsync(cancellationToken);
    }
}
