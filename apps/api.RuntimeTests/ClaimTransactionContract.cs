using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YangSpace.Api;

// Test-only transactional feasibility check for the proposed F2a-3 claim
// lifecycle. No HTTP routes, Identity role grants, merchant writes, PVE
// connections or production feature flags are created by this contract.
// SQLite is intentionally NOT presented as PostgreSQL transaction acceptance.
internal static class ClaimTransactionContract
{
    private static void Check(bool condition, string marker)
    {
        if (!condition)
            throw new InvalidOperationException("FAIL: " + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private sealed record Fixture(
        Guid ClaimId, Guid OrganizationId, Guid LocationId, string ApplicantId);

    private static ContentAuditEvent Audit(
        Fixture fixture, string entityType, Guid entityId,
        long before, long after, string actor, string correlationId) => new()
    {
        LocationId = fixture.LocationId,
        OrganizationId = fixture.OrganizationId,
        EntityType = entityType,
        EntityId = entityId,
        Action = "ClaimApprovalQa",
        ActorUserId = actor,
        ActorRole = "SyntheticReviewerOnly",
        RevisionBefore = before,
        RevisionAfter = after,
        CorrelationId = correlationId,
        ChangedFieldsJson = "{\"changed\":true}",
        Reason = "synthetic fixture — not a verified claim"
    };

    private static async Task<Fixture> SeedAsync(
        DirectoryDb db, string suffix, string applicantId)
    {
        var organization = new Organization
        {
            Name = "Synthetic organization " + suffix,
            Status = OrganizationStatus.PendingReview
        };
        var location = new BusinessLocation
        {
            Slug = "f2a3-claim-qa-" + suffix,
            Name = "Synthetic unclaimed location " + suffix,
            Category = "test",
            Neighborhood = "test",
            Address = "Disposable database only",
            OrganizationId = null
        };
        var claim = new BusinessClaim
        {
            LocationId = location.Id,
            OrganizationId = organization.Id,
            ApplicantUserId = applicantId,
            Status = BusinessClaimStatus.Pending
        };
        db.Organizations.Add(organization);
        db.Locations.Add(location);
        db.BusinessClaims.Add(claim);
        await db.SaveChangesAsync();
        return new Fixture(claim.Id, organization.Id, location.Id, applicantId);
    }

    private static async Task ApplySyntheticApprovalAsync(
        DirectoryDb db, Fixture fixture, bool forceAuditFailure)
    {
        var claim = await db.BusinessClaims.SingleAsync(x => x.Id == fixture.ClaimId);
        var organization = await db.Organizations.SingleAsync(
            x => x.Id == fixture.OrganizationId);
        var location = await db.Locations.SingleAsync(x => x.Id == fixture.LocationId);

        if (claim.Status != BusinessClaimStatus.Pending ||
            organization.Status != OrganizationStatus.PendingReview ||
            location.OrganizationId is not null ||
            claim.Revision != 0 || organization.Revision != 0 ||
            location.Revision != 0)
            throw new InvalidOperationException("Synthetic preconditions violated.");

        const string reviewer = "qa-reviewer";
        var correlation = "f2a3-claim-qa-" + Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        claim.Status = BusinessClaimStatus.Approved;
        claim.ReviewedByUserId = reviewer;
        claim.ReviewedAtUtc = now;
        claim.DecisionReason = "synthetic test decision";
        claim.Revision++;

        organization.Status = OrganizationStatus.Active;
        organization.Revision++;

        location.OrganizationId = organization.Id;
        location.UpdatedAtUtc = now;
        location.Revision++;

        var member = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = fixture.ApplicantId,
            Role = OrganizationMemberRole.Owner,
            GrantedByUserId = reviewer,
            GrantedAtUtc = now
        };
        db.OrganizationMembers.Add(member);

        var claimAudit = Audit(fixture, "BusinessClaim", claim.Id,
            before: 0, after: 1, reviewer, correlation);
        db.ContentAuditEvents.AddRange(
            claimAudit,
            Audit(fixture, "Organization", organization.Id,
                before: 0, after: 1, reviewer, correlation),
            Audit(fixture, "BusinessLocation", location.Id,
                before: 0, after: 1, reviewer, correlation),
            Audit(fixture, "OrganizationMember", member.Id,
                before: -1, after: 0, reviewer, correlation));

        if (forceAuditFailure)
        {
            // Identical (EntityType, EntityId, RevisionAfter) violates the
            // real EF unique audit index, even in a synthetic transaction.
            // The entire domain + membership + audit batch MUST roll back.
            db.ContentAuditEvents.Add(Audit(
                fixture, "BusinessClaim", claim.Id,
                before: 0, after: 1, reviewer, correlation));
        }

        await db.SaveChangesAsync();
    }

    public static async Task RunAsync()
    {
        await using var connection = new SqliteConnection(
            "Data Source=:memory:;Foreign Keys=True;Pooling=False");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite(connection).Options;

        Fixture approved;
        Fixture rolledBack;

        await using (var db = new DirectoryDb(options))
        {
            await db.Database.EnsureCreatedAsync();
            db.Users.AddRange(
                new IdentityUser { Id = "qa-reviewer", UserName = "qa-reviewer" },
                new IdentityUser { Id = "qa-applicant-1", UserName = "qa-applicant-1" },
                new IdentityUser { Id = "qa-applicant-2", UserName = "qa-applicant-2" });
            await db.SaveChangesAsync();
            approved = await SeedAsync(db, "success", "qa-applicant-1");
            rolledBack = await SeedAsync(db, "rollback", "qa-applicant-2");
        }
        Console.WriteLine("CLAIM_TX_DISPOSABLE_SQLITE=PASS");

        await using (var db = new DirectoryDb(options))
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await ApplySyntheticApprovalAsync(db, approved, forceAuditFailure: false);
            await transaction.CommitAsync();
        }

        await using (var verify = new DirectoryDb(options))
        {
            var claim = await verify.BusinessClaims.AsNoTracking()
                .SingleAsync(x => x.Id == approved.ClaimId);
            var org = await verify.Organizations.AsNoTracking()
                .SingleAsync(x => x.Id == approved.OrganizationId);
            var location = await verify.Locations.AsNoTracking()
                .SingleAsync(x => x.Id == approved.LocationId);
            Check(
                claim.Status == BusinessClaimStatus.Approved &&
                claim.Revision == 1 &&
                claim.ReviewedByUserId == "qa-reviewer" &&
                org.Status == OrganizationStatus.Active &&
                org.Revision == 1 &&
                location.OrganizationId == approved.OrganizationId &&
                location.Revision == 1 &&
                await verify.OrganizationMembers.AsNoTracking().CountAsync(x =>
                    x.OrganizationId == approved.OrganizationId &&
                    x.UserId == approved.ApplicantId &&
                    x.Role == OrganizationMemberRole.Owner &&
                    x.RevokedAtUtc == null) == 1,
                "CLAIM_TX_APPROVAL_PERSISTED_ATOMICALLY");
            Check(
                await verify.ContentAuditEvents.AsNoTracking().CountAsync(x =>
                    x.LocationId == approved.LocationId) == 4,
                "CLAIM_TX_AUDIT_CARDINALITY");
        }

        var duplicateAuditRejected = false;
        await using (var db = new DirectoryDb(options))
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                await ApplySyntheticApprovalAsync(
                    db, rolledBack, forceAuditFailure: true);
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is SqliteException
                {
                    SqliteErrorCode: 19,
                    SqliteExtendedErrorCode: 2067
                })
            {
                duplicateAuditRejected = true;
            }

            // Explicit rollback is required even when EF created a savepoint.
            await transaction.RollbackAsync();
        }
        Check(duplicateAuditRejected, "CLAIM_TX_DUPLICATE_AUDIT_FORCED_FAILURE");

        await using (var verify = new DirectoryDb(options))
        {
            var claim = await verify.BusinessClaims.AsNoTracking()
                .SingleAsync(x => x.Id == rolledBack.ClaimId);
            var org = await verify.Organizations.AsNoTracking()
                .SingleAsync(x => x.Id == rolledBack.OrganizationId);
            var location = await verify.Locations.AsNoTracking()
                .SingleAsync(x => x.Id == rolledBack.LocationId);
            Check(
                claim.Status == BusinessClaimStatus.Pending &&
                claim.Revision == 0 &&
                claim.ReviewedByUserId is null &&
                org.Status == OrganizationStatus.PendingReview &&
                org.Revision == 0 &&
                location.OrganizationId is null &&
                location.Revision == 0 &&
                await verify.OrganizationMembers.AsNoTracking().CountAsync(x =>
                    x.OrganizationId == rolledBack.OrganizationId) == 0 &&
                await verify.ContentAuditEvents.AsNoTracking().CountAsync(x =>
                    x.LocationId == rolledBack.LocationId) == 0,
                "CLAIM_TX_FAILURE_ROLLBACK_ALL_ROWS");
        }

        // This contract deliberately does not make a real reviewer decision.
        // No reviewer HTTP API, real PG app connection or membership grant is
        // added to the application by this test-only source.
        Console.WriteLine("CLAIM_TX_NO_HTTP_ROUTE_OR_REAL_REVIEWER=PASS");
    }
}
