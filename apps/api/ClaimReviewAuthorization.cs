using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

public static class ClaimReviewRoles
{
    public const string Reviewer = "MerchantClaimReviewer";
}

public sealed record LiveIdentityRoleRequirement(string RoleName) : IAuthorizationRequirement;

/// <summary>
/// Checks persisted Identity membership on every authorized request. A role
/// claim in an already-issued bearer token is not sufficient after revocation.
/// </summary>
public sealed class LiveIdentityRoleAuthorizationHandler(UserManager<IdentityUser> users)
    : AuthorizationHandler<LiveIdentityRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        LiveIdentityRoleRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var user = await users.FindByIdAsync(userId);
        if (user is not null && await users.IsInRoleAsync(user, requirement.RoleName))
            context.Succeed(requirement);
    }
}

/// <summary>
/// Claim-specific conflict check. Callers must supply the bearer subject and
/// DB-loaded claim applicant/organization, never caller-controlled fields.
/// </summary>
public sealed class ClaimReviewConflictPolicy(DirectoryDb db)
{
    public async Task<bool> IsConflictAsync(
        Guid targetOrganizationId,
        string reviewerUserId,
        string applicantUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(reviewerUserId, applicantUserId, StringComparison.Ordinal))
            return true;

        return await db.OrganizationMembers.AsNoTracking().AnyAsync(member =>
                member.OrganizationId == targetOrganizationId &&
                member.UserId == reviewerUserId &&
                member.RevokedAtUtc == null,
            cancellationToken);
    }
}

public static class ClaimReviewQaEndpoints
{
    // Authorization probe only: no claim lookup and no business-state writes.
    public static void MapClaimReviewQa(this WebApplication app)
    {
        app.MapGet("/api/v1/staff/claim-review/access",
                () => Results.Ok(new { authorized = true }))
            .WithTags("Claim review QA")
            .RequireAuthorization("MerchantClaimReviewer");
    }
}
