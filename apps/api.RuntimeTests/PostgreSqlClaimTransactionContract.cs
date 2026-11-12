using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using YangSpace.Api;

// Disposable, test-only real-Npgsql EF transaction contract.
// The opt-in token is created inside an ephemeral PostgreSQL container.
// No API endpoint, reviewer authorization, claim workflow or live DB is touched.
internal static class PostgreSqlClaimTransactionContract
{
    private const string DatabaseName = "yangspace_f2a3_disposable";
    private const string ExpectedMigration = "20261111203833_InitialPostgreSqlSchema";

    private sealed record Fixture(
        Guid ClaimId, Guid OrganizationId, Guid LocationId, string ApplicantId);

    private static void Check(bool condition, string marker)
    {
        if (!condition)
            throw new InvalidOperationException("FAIL: " + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private static string RequireIsolatedConnection()
    {
        if (Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_DISPOSABLE_ONLY") !=
            "CONFIRMED_CONTAINER_NETNS_ONLY")
            throw new InvalidOperationException("F2A3_PG_SCOPE_NOT_CONFIRMED");

        var raw = Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_CONNECTION");
        var token = Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_TOKEN");
        if (string.IsNullOrWhiteSpace(raw) ||
            !Guid.TryParseExact(token, "N", out _))
            throw new InvalidOperationException("F2A3_PG_QA_CONNECTION_OR_TOKEN_MISSING");

        NpgsqlConnectionStringBuilder parsed;
        try
        {
            parsed = new NpgsqlConnectionStringBuilder(raw);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("F2A3_PG_QA_CONNECTION_INVALID");
        }

        // Loopback in the private, shared container network namespace only.
        // Reject arbitrary hosts, other databases, credentials and SSL bypass
        // flags in any application/production context.
        if (parsed.Host != "127.0.0.1" ||
            parsed.Port != 5432 ||
            parsed.Database != DatabaseName ||
            parsed.Username != "postgres" ||
            parsed.SslMode != SslMode.Disable ||
            !string.IsNullOrEmpty(parsed.Password) ||
            parsed.Pooling ||
            parsed.IncludeErrorDetail)
            throw new InvalidOperationException("F2A3_PG_QA_CONNECTION_SCOPE_REFUSED");

        return raw;
    }

    private static async Task VerifyDisposableSentinelAsync(
        string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT scope_token FROM yangspace_f2a3_qa_guard LIMIT 1";
        var actualToken = (string?)await command.ExecuteScalarAsync();
        var expectedToken = Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_TOKEN");
        Check(
            actualToken != null &&
            string.Equals(actualToken, expectedToken, StringComparison.Ordinal),
            "F2A3_PG_SENTINEL_MATCHED");
    }

    private static DbContextOptions<DirectoryDb> Options(string connectionString) =>
        new DbContextOptionsBuilder<DirectoryDb>()
            .UseNpgsql(connectionString, options =>
                options.MigrationsAssembly("YangSpace.PostgreSql.Migrations"))
            .Options;

    private static async Task<Fixture> SeedAsync(
        DirectoryDb db, string suffix, string applicantId)
    {
        var organization = new Organization
        {
            Name = "F2a3 synthetic org " + suffix,
            Status = OrganizationStatus.PendingReview
        };
        var location = new BusinessLocation
        {
            Slug = "f2a3-pg-" + suffix,
            Name = "F2a3 synthetic unclaimed location",
            Category = "test",
            Neighborhood = "test",
            Address = "Disposable PostgreSQL only",
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

    private static ContentAuditEvent Audit(
        Fixture fixture,
        string entityType,
        Guid entityId,
        long before,
        long after) => new()
    {
        OrganizationId = fixture.OrganizationId,
        LocationId = fixture.LocationId,
        EntityType = entityType,
        EntityId = entityId,
        Action = "ClaimApprovalQa",
        ActorUserId = "qa-reviewer",
        ActorRole = "SyntheticReviewerOnly",
        RevisionBefore = before,
        RevisionAfter = after,
        CorrelationId = "f2a3-pg-" + fixture.ClaimId.ToString("N"),
        ChangedFieldsJson = "{\"changed\":true}",
        Reason = "synthetic test; not verified ownership"
    };

    private static async Task ApplySyntheticApprovalAsync(
        DirectoryDb db, Fixture fixture, bool injectAuditFailure)
    {
        var claim = await db.BusinessClaims.SingleAsync(x => x.Id == fixture.ClaimId);
        var org = await db.Organizations.SingleAsync(x => x.Id == fixture.OrganizationId);
        var location = await db.Locations.SingleAsync(x => x.Id == fixture.LocationId);

        if (claim.Status != BusinessClaimStatus.Pending ||
            org.Status != OrganizationStatus.PendingReview ||
            location.OrganizationId != null ||
            claim.Revision != 0 || org.Revision != 0 || location.Revision != 0)
            throw new InvalidOperationException("F2A3_PG_SYNTHETIC_PRECONDITION_FAILED");

        claim.Status = BusinessClaimStatus.Approved;
        claim.ReviewedByUserId = "qa-reviewer";
        claim.ReviewedAtUtc = DateTime.UtcNow;
        claim.DecisionReason = "synthetic QA";
        claim.Revision++;

        org.Status = OrganizationStatus.Active;
        org.Revision++;

        location.OrganizationId = org.Id;
        location.UpdatedAtUtc = DateTime.UtcNow;
        location.Revision++;

        var member = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = fixture.ApplicantId,
            Role = OrganizationMemberRole.Owner,
            GrantedByUserId = "qa-reviewer"
        };
        db.OrganizationMembers.Add(member);
        db.ContentAuditEvents.AddRange(
            Audit(fixture, "BusinessClaim", claim.Id, 0, 1),
            Audit(fixture, "Organization", org.Id, 0, 1),
            Audit(fixture, "BusinessLocation", location.Id, 0, 1),
            Audit(fixture, "OrganizationMember", member.Id, -1, 0));

        // First save really writes all entities to PostgreSQL, but does NOT
        // commit the explicit outer transaction.
        await db.SaveChangesAsync();

        if (injectAuditFailure)
        {
            // Second SaveChanges forces the real PostgreSQL unique audit index
            // to reject the duplicate. Caller must rollback the first save too.
            db.ContentAuditEvents.Add(
                Audit(fixture, "BusinessClaim", claim.Id, 0, 1));
            await db.SaveChangesAsync();
        }
    }

    public static async Task RunAsync()
    {
        var connectionString = RequireIsolatedConnection();
        await VerifyDisposableSentinelAsync(connectionString);
        Console.WriteLine("F2A3_PG_LOOPBACK_DISPOSABLE_SCOPE=PASS");

        var options = Options(connectionString);
        var token = Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_TOKEN")!;
        Fixture approved;
        Fixture rollback;

        await using (var db = new DirectoryDb(options))
        {
            // The schema must already be applied from the pinned reviewed SQL.
            // No EnsureCreated or Migrate is allowed in this test process.
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            Check(applied.Contains(ExpectedMigration),
                "F2A3_PG_REVIEWED_MIGRATION_PRESENT");

            Check(!await db.BusinessClaims.AnyAsync() &&
                  !await db.OrganizationMembers.AnyAsync() &&
                  !await db.ContentAuditEvents.AnyAsync(),
                "F2A3_PG_FRESH_SYNTHETIC_STATE");

            db.Users.AddRange(
                new IdentityUser { Id = "qa-reviewer-" + token, UserName = "qa-reviewer" },
                new IdentityUser { Id = "qa-applicant-a-" + token, UserName = "qa-applicant-a" },
                new IdentityUser { Id = "qa-applicant-b-" + token, UserName = "qa-applicant-b" });
            await db.SaveChangesAsync();

            approved = await SeedAsync(db, "approved-" + token, "qa-applicant-a-" + token);
            rollback = await SeedAsync(db, "rollback-" + token, "qa-applicant-b-" + token);
        }

        await using (var db = new DirectoryDb(options))
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await ApplySyntheticApprovalAsync(db, approved, injectAuditFailure: false);
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

            Check(claim.Status == BusinessClaimStatus.Approved &&
                  claim.Revision == 1 &&
                  claim.ReviewedByUserId != null &&
                  org.Status == OrganizationStatus.Active &&
                  org.Revision == 1 &&
                  location.OrganizationId == org.Id &&
                  location.Revision == 1 &&
                  await verify.OrganizationMembers.AsNoTracking().CountAsync(x =>
                      x.OrganizationId == org.Id &&
                      x.UserId == approved.ApplicantId &&
                      x.Role == OrganizationMemberRole.Owner &&
                      x.RevokedAtUtc == null) == 1,
                "F2A3_PG_APP_TX_SUCCESS_PERSISTED");

            Check(await verify.ContentAuditEvents.AsNoTracking().CountAsync(x =>
                    x.LocationId == approved.LocationId) == 4,
                "F2A3_PG_APP_TX_AUDIT_CARDINALITY");
        }

        var rejected = false;
        await using (var db = new DirectoryDb(options))
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                await ApplySyntheticApprovalAsync(db, rollback, injectAuditFailure: true);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is PostgresException pg &&
                pg.SqlState == PostgresErrorCodes.UniqueViolation &&
                pg.ConstraintName == "IX_ContentAuditEvents_EntityType_EntityId_RevisionAfter")
            {
                rejected = true;
            }

            await transaction.RollbackAsync();
        }
        Check(rejected, "F2A3_PG_APP_TX_SECOND_SAVE_AUDIT_REJECTED");

        await using (var verify = new DirectoryDb(options))
        {
            var claim = await verify.BusinessClaims.AsNoTracking()
                .SingleAsync(x => x.Id == rollback.ClaimId);
            var org = await verify.Organizations.AsNoTracking()
                .SingleAsync(x => x.Id == rollback.OrganizationId);
            var location = await verify.Locations.AsNoTracking()
                .SingleAsync(x => x.Id == rollback.LocationId);

            Check(claim.Status == BusinessClaimStatus.Pending &&
                  claim.Revision == 0 &&
                  claim.ReviewedByUserId == null &&
                  org.Status == OrganizationStatus.PendingReview &&
                  org.Revision == 0 &&
                  location.OrganizationId == null &&
                  location.Revision == 0 &&
                  await verify.OrganizationMembers.AsNoTracking().CountAsync(x =>
                      x.OrganizationId == rollback.OrganizationId) == 0 &&
                  await verify.ContentAuditEvents.AsNoTracking().CountAsync(x =>
                      x.LocationId == rollback.LocationId) == 0,
                "F2A3_PG_APP_TX_OUTER_ROLLBACK_ALL_ROWS");
        }

        Console.WriteLine("F2A3_PG_APP_TX_PROVIDER=NPGSQL_REAL_POSTGRESQL");
        Console.WriteLine("F2A3_PG_REVIEWER_HTTP=NOT_TESTED");
        Console.WriteLine("F2A3_PG_PARALLEL_WRITERS=NOT_TESTED");
    }
}
