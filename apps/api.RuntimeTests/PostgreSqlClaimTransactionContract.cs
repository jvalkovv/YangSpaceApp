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
    private sealed record CompetingFixturePair(Fixture First, Fixture Second);

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

    private static async Task<CompetingFixturePair> SeedCompetingAsync(
        DirectoryDb db, string suffix, string firstApplicantId, string secondApplicantId)
    {
        var firstOrganization = new Organization
        {
            Name = "F2a3 competing org A " + suffix,
            Status = OrganizationStatus.PendingReview
        };
        var secondOrganization = new Organization
        {
            Name = "F2a3 competing org B " + suffix,
            Status = OrganizationStatus.PendingReview
        };
        var location = new BusinessLocation
        {
            Slug = "f2a3-pg-competing-" + suffix,
            Name = "F2a3 competing synthetic location",
            Category = "test",
            Neighborhood = "test",
            Address = "Disposable PostgreSQL only",
            OrganizationId = null
        };
        var firstClaim = new BusinessClaim
        {
            LocationId = location.Id,
            OrganizationId = firstOrganization.Id,
            ApplicantUserId = firstApplicantId,
            Status = BusinessClaimStatus.Pending
        };
        var secondClaim = new BusinessClaim
        {
            LocationId = location.Id,
            OrganizationId = secondOrganization.Id,
            ApplicantUserId = secondApplicantId,
            Status = BusinessClaimStatus.Pending
        };

        db.Organizations.AddRange(firstOrganization, secondOrganization);
        db.Locations.Add(location);
        db.BusinessClaims.AddRange(firstClaim, secondClaim);
        await db.SaveChangesAsync();

        return new CompetingFixturePair(
            new Fixture(firstClaim.Id, firstOrganization.Id, location.Id, firstApplicantId),
            new Fixture(secondClaim.Id, secondOrganization.Id, location.Id, secondApplicantId));
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

    private static async Task<bool> TryApproveCompetingWriterAsync(
        DbContextOptions<DirectoryDb> options,
        Fixture fixture,
        TaskCompletionSource<bool> release,
        Action signalLoaded)
    {
        await using var db = new DirectoryDb(options);
        await using var transaction = await db.Database.BeginTransactionAsync();

        var claim = await db.BusinessClaims.SingleAsync(x => x.Id == fixture.ClaimId);
        var org = await db.Organizations.SingleAsync(x => x.Id == fixture.OrganizationId);
        var location = await db.Locations.SingleAsync(x => x.Id == fixture.LocationId);

        if (claim.Status != BusinessClaimStatus.Pending ||
            org.Status != OrganizationStatus.PendingReview ||
            location.OrganizationId != null ||
            claim.Revision != 0 || org.Revision != 0 || location.Revision != 0)
            throw new InvalidOperationException("F2A3_PG_PARALLEL_PRECONDITION_FAILED");

        // Both independent DbContexts must observe the same original revision
        // before either writer is allowed to issue its first UPDATE.
        signalLoaded();
        if (!await release.Task.WaitAsync(TimeSpan.FromSeconds(30)))
            throw new InvalidOperationException("F2A3_PG_PARALLEL_WRITER_CANCELLED");

        claim.Status = BusinessClaimStatus.Approved;
        claim.ReviewedByUserId = "qa-reviewer";
        claim.ReviewedAtUtc = DateTime.UtcNow;
        claim.DecisionReason = "synthetic concurrent QA";
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

        try
        {
            // Commit neither phase independently: the first SaveChanges writes
            // claim/org/location/member under the same outer transaction.
            // Only after those concurrency-checked writes succeed do we insert
            // the four audit rows in a second SaveChanges.
            await db.SaveChangesAsync();
            db.ContentAuditEvents.AddRange(
                Audit(fixture, "BusinessClaim", claim.Id, 0, 1),
                Audit(fixture, "Organization", org.Id, 0, 1),
                Audit(fixture, "BusinessLocation", location.Id, 0, 1),
                Audit(fixture, "OrganizationMember", member.Id, -1, 0));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return false;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgres &&
            postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
            postgres.ConstraintName == "UX_BusinessClaims_ApprovedLocation")
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    private static async Task RunCompetingWritersAsync(
        DbContextOptions<DirectoryDb> options,
        CompetingFixturePair pair)
    {
        var bothLoaded = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;

        void SignalLoaded()
        {
            if (Interlocked.Increment(ref arrivals) == 2)
                bothLoaded.TrySetResult(true);
        }

        var firstTask = TryApproveCompetingWriterAsync(
            options, pair.First, release, SignalLoaded);
        var secondTask = TryApproveCompetingWriterAsync(
            options, pair.Second, release, SignalLoaded);

        bool[] outcomes;
        try
        {
            await bothLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Check(arrivals == 2, "F2A3_PG_PARALLEL_BOTH_READ_SAME_REVISION");
            release.TrySetResult(true);
            outcomes = await Task.WhenAll(firstTask, secondTask);
        }
        catch
        {
            release.TrySetResult(false);
            try { await Task.WhenAll(firstTask, secondTask); }
            catch { /* Preserve the original synchronization/test failure. */ }
            throw;
        }

        Check(outcomes.Count(x => x) == 1,
            "F2A3_PG_PARALLEL_EXACTLY_ONE_WINNER");

        await using var verify = new DirectoryDb(options);
        var firstClaim = await verify.BusinessClaims.AsNoTracking()
            .SingleAsync(x => x.Id == pair.First.ClaimId);
        var secondClaim = await verify.BusinessClaims.AsNoTracking()
            .SingleAsync(x => x.Id == pair.Second.ClaimId);
        var firstOrg = await verify.Organizations.AsNoTracking()
            .SingleAsync(x => x.Id == pair.First.OrganizationId);
        var secondOrg = await verify.Organizations.AsNoTracking()
            .SingleAsync(x => x.Id == pair.Second.OrganizationId);
        var location = await verify.Locations.AsNoTracking()
            .SingleAsync(x => x.Id == pair.First.LocationId);

        var firstWon = firstClaim.Status == BusinessClaimStatus.Approved;
        var secondWon = secondClaim.Status == BusinessClaimStatus.Approved;
        Check(firstWon != secondWon &&
              outcomes[0] == firstWon && outcomes[1] == secondWon,
            "F2A3_PG_PARALLEL_WINNER_MATCHES_PERSISTED_STATE");

        var winner = firstWon ? pair.First : pair.Second;
        var loser = firstWon ? pair.Second : pair.First;
        var winnerClaim = firstWon ? firstClaim : secondClaim;
        var loserClaim = firstWon ? secondClaim : firstClaim;
        var winnerOrg = firstWon ? firstOrg : secondOrg;
        var loserOrg = firstWon ? secondOrg : firstOrg;

        Check(winnerClaim.Status == BusinessClaimStatus.Approved &&
              winnerClaim.Revision == 1 &&
              winnerOrg.Status == OrganizationStatus.Active &&
              winnerOrg.Revision == 1 &&
              location.OrganizationId == winner.OrganizationId &&
              location.Revision == 1,
            "F2A3_PG_PARALLEL_WINNER_STATE_CONSISTENT");

        var ownerMembers = await verify.OrganizationMembers.AsNoTracking()
            .Where(x => (x.OrganizationId == pair.First.OrganizationId ||
                         x.OrganizationId == pair.Second.OrganizationId) &&
                        x.Role == OrganizationMemberRole.Owner &&
                        x.RevokedAtUtc == null)
            .ToArrayAsync();
        Check(ownerMembers.Length == 1 &&
              ownerMembers[0].OrganizationId == winner.OrganizationId &&
              ownerMembers[0].UserId == winner.ApplicantId,
            "F2A3_PG_PARALLEL_OWNER_UNIQUENESS");

        var auditEvents = await verify.ContentAuditEvents.AsNoTracking()
            .Where(x => x.LocationId == pair.First.LocationId)
            .ToArrayAsync();
        Check(auditEvents.Length == 4 &&
              auditEvents.All(x => x.OrganizationId == winner.OrganizationId) &&
              auditEvents.Select(x => x.EntityType).Distinct().Count() == 4,
            "F2A3_PG_PARALLEL_AUDIT_CARDINALITY");

        Check(loserClaim.Status == BusinessClaimStatus.Pending &&
              loserClaim.Revision == 0 &&
              loserOrg.Status == OrganizationStatus.PendingReview &&
              loserOrg.Revision == 0 &&
              await verify.OrganizationMembers.AsNoTracking().CountAsync(x =>
                  x.OrganizationId == loser.OrganizationId) == 0 &&
              !await verify.ContentAuditEvents.AsNoTracking().AnyAsync(x =>
                  x.OrganizationId == loser.OrganizationId),
            "F2A3_PG_PARALLEL_LOSER_ROLLBACK");

        Console.WriteLine("F2A3_PG_PARALLEL_WRITERS_PROVIDER=NPGSQL_REAL_POSTGRESQL");
        Console.WriteLine("F2A3_PG_PARALLEL_WRITERS_ISOLATED_ONLY=PASS");
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
        CompetingFixturePair competing;

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
                new IdentityUser { Id = "qa-applicant-b-" + token, UserName = "qa-applicant-b" },
                new IdentityUser { Id = "qa-applicant-c-" + token, UserName = "qa-applicant-c" },
                new IdentityUser { Id = "qa-applicant-d-" + token, UserName = "qa-applicant-d" });
            await db.SaveChangesAsync();

            approved = await SeedAsync(db, "approved-" + token, "qa-applicant-a-" + token);
            rollback = await SeedAsync(db, "rollback-" + token, "qa-applicant-b-" + token);
            competing = await SeedCompetingAsync(
                db, "parallel-" + token,
                "qa-applicant-c-" + token, "qa-applicant-d-" + token);
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

        await RunCompetingWritersAsync(options, competing);

        Console.WriteLine("F2A3_PG_APP_TX_PROVIDER=NPGSQL_REAL_POSTGRESQL");
        Console.WriteLine("F2A3_PG_REVIEWER_HTTP=NOT_TESTED_IN_TRANSACTION_MODE");
        Console.WriteLine("F2A3_PG_PARALLEL_WRITERS=PASS_DISPOSABLE_ONLY");
    }
}
