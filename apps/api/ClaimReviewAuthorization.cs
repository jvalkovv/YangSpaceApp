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
    public async Task<bool> CanReviewClaimAsync(
        Guid claimId,
        string reviewerUserId,
        CancellationToken cancellationToken = default)
    {
        // Resolve applicant and target organization from the persisted claim;
        // never accept either value from the HTTP request.
        var claim = await db.BusinessClaims.AsNoTracking()
            .Where(item => item.Id == claimId)
            .Select(item => new { item.OrganizationId, item.ApplicantUserId })
            .SingleOrDefaultAsync(cancellationToken);

        // Missing reviewer identity and unknown claims fail closed. The caller
        // maps this to a generic response without disclosing claim existence.
        if (string.IsNullOrWhiteSpace(reviewerUserId) || claim is null ||
            string.Equals(reviewerUserId, claim.ApplicantUserId, StringComparison.Ordinal))
            return false;

        var activeMember = await db.OrganizationMembers.AsNoTracking().AnyAsync(member =>
                member.OrganizationId == claim.OrganizationId &&
                member.UserId == reviewerUserId &&
                member.RevokedAtUtc == null,
            cancellationToken);
        return !activeMember;
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
