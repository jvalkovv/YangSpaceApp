using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YangSpace.Api;

/// <summary>
/// F2a-1 authorization contract. All rows exist only in an isolated SQLite
/// in-memory database; no HTTP endpoint, app connection string or CT is used.
/// This is not a substitute for future real-bearer cross-tenant API tests.
/// </summary>
internal static class TenantScopeContract
{
    private static void Check(bool condition, string marker)
    {
        if (!condition)
            throw new InvalidOperationException("FAIL: " + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private static async Task CheckUniqueClaimRejectedAsync(
        DirectoryDb db, BusinessClaim attempted, string marker)
    {
        db.BusinessClaims.Add(attempted);
        try
        {
            await db.SaveChangesAsync();
            throw new InvalidOperationException("FAIL: " + marker + " (duplicate accepted)");
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqliteException {
                SqliteErrorCode: 19,
                SqliteExtendedErrorCode: 2067
            })
        {
            Console.WriteLine(marker + "=PASS");
        }
        finally
        {
            // Failed inserts stay tracked as Added; otherwise they contaminate
            // subsequent SaveChanges and turn an expected rejection into noise.
            db.Entry(attempted).State = EntityState.Detached;
        }
    }

    private static ClaimsPrincipal Actor(string userId, string? role = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        if (role is not null) claims.Add(new Claim(ClaimTypes.Role, role));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "isolated-contract-test"));
    }

    public static async Task RunAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True;Pooling=False");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite(connection).Options;
        await using var db = new DirectoryDb(options);

        // Explicitly isolated, disposable in-memory model only.
        await db.Database.EnsureCreatedAsync();

        db.Users.AddRange(
            new IdentityUser { Id = "actor-a", UserName = "a" },
            new IdentityUser { Id = "actor-b", UserName = "b" },
            new IdentityUser { Id = "actor-revoked", UserName = "revoked" },
            new IdentityUser { Id = "actor-applicant", UserName = "applicant" },
            new IdentityUser { Id = "actor-staff", UserName = "staff" });
        await db.SaveChangesAsync();

        var orgA = new Organization { Name = "Fictional A", Status = OrganizationStatus.Active };
        var orgB = new Organization { Name = "Fictional B", Status = OrganizationStatus.Active };
        var orgSuspended = new Organization { Name = "Fictional suspended", Status = OrganizationStatus.Suspended };
        db.Organizations.AddRange(orgA, orgB, orgSuspended);
        await db.SaveChangesAsync();

        var locA = new BusinessLocation { Slug = "tenant-contract-a", Name = "A",
            OrganizationId = orgA.Id, Category = "test", Neighborhood = "test", Address = "disposable" };
        var locB = new BusinessLocation { Slug = "tenant-contract-b", Name = "B",
            OrganizationId = orgB.Id, Category = "test", Neighborhood = "test", Address = "disposable" };
        var locSuspended = new BusinessLocation { Slug = "tenant-contract-suspended", Name = "C",
            OrganizationId = orgSuspended.Id, Category = "test", Neighborhood = "test", Address = "disposable" };
        var locLegacy = new BusinessLocation { Slug = "tenant-contract-legacy", Name = "Legacy",
            OrganizationId = null, Category = "test", Neighborhood = "test", Address = "disposable" };
        db.Locations.AddRange(locA, locB, locSuspended, locLegacy);

        var memberA = new OrganizationMember { OrganizationId = orgA.Id,
            UserId = "actor-a", Role = OrganizationMemberRole.Owner,
            GrantedByUserId = "actor-staff" };
        var memberB = new OrganizationMember { OrganizationId = orgB.Id,
            UserId = "actor-b", Role = OrganizationMemberRole.Editor,
            GrantedByUserId = "actor-staff" };
        var revoked = new OrganizationMember { OrganizationId = orgA.Id,
            UserId = "actor-revoked", Role = OrganizationMemberRole.Owner,
            GrantedByUserId = "actor-staff", RevokedAtUtc = DateTime.UtcNow,
            RevokedByUserId = "actor-staff" };
        var suspendedMember = new OrganizationMember { OrganizationId = orgSuspended.Id,
            UserId = "actor-applicant", Role = OrganizationMemberRole.Owner,
            GrantedByUserId = "actor-staff" };
        db.OrganizationMembers.AddRange(memberA, memberB, revoked, suspendedMember);

        db.BusinessClaims.Add(new BusinessClaim
        {
            LocationId = locA.Id, OrganizationId = orgA.Id,
            ApplicantUserId = "actor-applicant",
            Status = BusinessClaimStatus.Pending
        });
        await db.SaveChangesAsync();

        Check(await TenantScope.CanManageLocationAsync(db, Actor("actor-a"), locA.Id),
            "F2A_OWNER_OWN_LOCATION_ALLOWED");
        Check(await TenantScope.CanManageLocationAsync(db, Actor("actor-b"), locB.Id),
            "F2A_EDITOR_OWN_LOCATION_ALLOWED");
        Check(!await TenantScope.CanManageLocationAsync(db, Actor("actor-a"), locB.Id) &&
              !await TenantScope.CanManageLocationAsync(db, Actor("actor-b"), locA.Id),
            "F2A_CROSS_TENANT_LOCATION_DENIED");
        Check(!await TenantScope.CanManageLocationAsync(db, Actor("actor-applicant"), locA.Id),
            "F2A_PENDING_CLAIM_NO_GRANT");
        Check(!await TenantScope.CanManageLocationAsync(db, Actor("actor-revoked"), locA.Id),
            "F2A_REVOKED_MEMBERSHIP_DENIED");
        Check(!await TenantScope.CanManageLocationAsync(db, Actor("actor-applicant"), locSuspended.Id),
            "F2A_SUSPENDED_ORGANIZATION_DENIED");
        Check(!await TenantScope.CanManageLocationAsync(db, Actor("actor-a"), locLegacy.Id),
            "F2A_UNCLAIMED_LEGACY_DENIED");
        Check(!await TenantScope.CanManageLocationAsync(db, Actor("actor-staff", "PlatformAdmin"), locA.Id),
            "F2A_PLATFORM_ADMIN_NOT_IMPLICIT_OWNER");
        Check(!await TenantScope.CanManageLocationAsync(db, new ClaimsPrincipal(), locA.Id) &&
              !await TenantScope.CanManageLocationAsync(db,
                  new ClaimsPrincipal(new ClaimsIdentity(new[]
                  {
                      new Claim(ClaimTypes.NameIdentifier, "actor-a")
                  })), locA.Id),
            "F2A_ANONYMOUS_AND_UNAUTHENTICATED_DENIED");

        memberA.RevokedAtUtc = DateTime.UtcNow;
        memberA.RevokedByUserId = "actor-staff";
        memberA.Revision++;
        await db.SaveChangesAsync();
        Check(!await TenantScope.CanManageLocationAsync(db, Actor("actor-a"), locA.Id),
            "F2A_MEMBERSHIP_REVOCATION_IMMEDIATE");

        // F2a-1 database invariants: distinct organizations may have their
        // own pending applications, but duplicate pending requests from the
        // same organization/location (even from another applicant) are denied.
        db.BusinessClaims.Add(new BusinessClaim
        {
            LocationId = locA.Id,
            OrganizationId = orgB.Id,
            ApplicantUserId = "actor-b",
            Status = BusinessClaimStatus.Pending
        });
        await db.SaveChangesAsync();
        Check(await db.BusinessClaims.CountAsync(x =>
                x.LocationId == locA.Id && x.Status == BusinessClaimStatus.Pending) == 2,
            "F2A_COMPETING_ORGANIZATION_PENDING_ALLOWED");

        await CheckUniqueClaimRejectedAsync(db, new BusinessClaim
        {
            LocationId = locA.Id,
            OrganizationId = orgA.Id,
            ApplicantUserId = "actor-staff",
            Status = BusinessClaimStatus.Pending
        }, "F2A_DUPLICATE_PENDING_ORGANIZATION_DENIED");

        db.BusinessClaims.Add(new BusinessClaim
        {
            LocationId = locA.Id,
            OrganizationId = orgA.Id,
            ApplicantUserId = "actor-applicant",
            Status = BusinessClaimStatus.Approved
        });
        await db.SaveChangesAsync();

        await CheckUniqueClaimRejectedAsync(db, new BusinessClaim
        {
            LocationId = locA.Id,
            OrganizationId = orgB.Id,
            ApplicantUserId = "actor-b",
            Status = BusinessClaimStatus.Approved
        }, "F2A_SECOND_APPROVED_LOCATION_DENIED");

        // Closed claims may be followed by a new application; only an
        // active Pending claim for this same organization/location is unique.
        var priorPending = await db.BusinessClaims.SingleAsync(x =>
            x.LocationId == locA.Id && x.OrganizationId == orgA.Id &&
            x.Status == BusinessClaimStatus.Pending);
        priorPending.Status = BusinessClaimStatus.Withdrawn;
        priorPending.Revision++;
        db.BusinessClaims.Add(new BusinessClaim
        {
            LocationId = locA.Id,
            OrganizationId = orgA.Id,
            ApplicantUserId = "actor-applicant",
            Status = BusinessClaimStatus.Pending
        });
        await db.SaveChangesAsync();
        Check(await db.BusinessClaims.CountAsync(x =>
                x.LocationId == locA.Id && x.OrganizationId == orgA.Id &&
                x.Status == BusinessClaimStatus.Pending) == 1,
            "F2A_REAPPLY_AFTER_WITHDRAWN_ALLOWED");

        // Two independent EF contexts start with the same revision. EF's
        // configured concurrency token rejects the second stale update.
        await using var firstWriter = new DirectoryDb(options);
        await using var secondWriter = new DirectoryDb(options);
        var firstVersion = await firstWriter.Locations.SingleAsync(x => x.Id == locB.Id);
        var staleVersion = await secondWriter.Locations.SingleAsync(x => x.Id == locB.Id);
        Check(firstVersion.Revision == staleVersion.Revision,
            "F2A_LOCATION_REVISION_BASELINE_MATCHES");

        firstVersion.Name = "B updated once";
        firstVersion.Revision++;
        await firstWriter.SaveChangesAsync();

        staleVersion.Name = "B stale overwrite";
        staleVersion.Revision++;
        try
        {
            await secondWriter.SaveChangesAsync();
            throw new InvalidOperationException(
                "FAIL: F2A_LOCATION_STALE_REVISION_DENIED (stale write accepted)");
        }
        catch (DbUpdateConcurrencyException)
        {
            Console.WriteLine("F2A_LOCATION_STALE_REVISION_DENIED=PASS");
        }

        await using var finalCheck = new DirectoryDb(options);
        var persisted = await finalCheck.Locations.AsNoTracking()
            .SingleAsync(x => x.Id == locB.Id);
        Check(persisted.Name == "B updated once" && persisted.Revision == firstVersion.Revision,
            "F2A_LOCATION_REVISION_WINNER_PRESERVED");

        Console.WriteLine("F2A_TENANT_SCOPE_CONTRACT=PASS");
    }
}
