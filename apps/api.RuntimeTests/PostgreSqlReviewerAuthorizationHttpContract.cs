using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using YangSpace.Api;

// ASP.NET Core 10 WebApplicationFactory resolves content root BEFORE
// WithWebHostBuilder/UseContentRoot. In our no-repository Linux sidecar the
// published appsettings.json under /qa is the explicit content-root marker.
// The assembly-name key is supported by WebApplicationFactory 10. This
// attribute is inert for other runs without the /qa/appsettings.json marker.
[assembly: WebApplicationFactoryContentRoot("YangSpace.Api", "/qa", "appsettings.json", "0")]

// Real Identity bearer HTTP through TestServer backed by the disposable
// PostgreSQL database. This is synthetic QA only: no claim mutation route,
// real evidence, production credentials, CT access or deployment.
internal static class PostgreSqlReviewerAuthorizationHttpContract
{
    private const string Password = "OnlyForQa!Pass2026";
    private const string AccessPath = "/api/v1/staff/claim-review/access";
    private const string DatabaseName = "yangspace_f2a3_disposable";
    private const string ExpectedMigration = "20261111203833_InitialPostgreSqlSchema";

    private sealed class StartupEnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string?> previous = new(StringComparer.Ordinal);
        private static readonly string[] Names =
        [
            "ConnectionStrings__Main", "Database__Provider", "Preview__Enabled",
            "Preview__AllowRegistration", "Admin__Enabled", "MerchantRead__Enabled",
            "ClaimReview__QaEnabled", "ClaimReviewPostgreSqlQa__Enabled",
            "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT",
            "YANGSPACE_F2A3_PG_DISPOSABLE_ONLY", "YANGSPACE_F2A3_PG_TESTSERVER_ONLY",
            "YANGSPACE_F2A3_PG_CONNECTION", "YANGSPACE_F2A3_PG_TOKEN"
        ];

        internal StartupEnvironmentScope(string connectionString)
        {
            foreach (var name in Names)
                previous.Add(name, Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process));

            Set("ConnectionStrings__Main", connectionString);
            Set("YANGSPACE_F2A3_PG_CONNECTION", connectionString);
            Set("Preview__Enabled", "false");
            Set("Preview__AllowRegistration", "false");
            Set("Admin__Enabled", "false");
            Set("MerchantRead__Enabled", "false");
            Set("ClaimReview__QaEnabled", "false");
            Configure(true, "YangSpaceQa", "PostgreSQL");
        }

        private static void Set(string name, string value) =>
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);

        internal void Configure(
            bool enable,
            string environment,
            string provider = "PostgreSQL",
            bool sqliteReviewerQa = false)
        {
            Set("ASPNETCORE_ENVIRONMENT", environment);
            Set("DOTNET_ENVIRONMENT", environment);
            Set("ClaimReviewPostgreSqlQa__Enabled", enable ? "true" : "false");
            Set("ClaimReview__QaEnabled", sqliteReviewerQa ? "true" : "false");
            Set("YANGSPACE_F2A3_PG_TESTSERVER_ONLY",
                enable ? "CONFIRMED_WEBAPPLICATIONFACTORY_ONLY" : "");
            Set("Database__Provider", provider);
        }

        internal void ConfigureDisposableScope(bool enable) =>
            Set("YANGSPACE_F2A3_PG_DISPOSABLE_ONLY",
                enable ? "CONFIRMED_CONTAINER_NETNS_ONLY" : "");

        internal void ConfigureTestServerScope(bool enable) =>
            Set("YANGSPACE_F2A3_PG_TESTSERVER_ONLY",
                enable ? "CONFIRMED_WEBAPPLICATIONFACTORY_ONLY" : "");

        internal void SetExpectedToken(string token) =>
            Set("YANGSPACE_F2A3_PG_TOKEN", token);

        internal void SetScopedConnection(string connectionString) =>
            Set("YANGSPACE_F2A3_PG_CONNECTION", connectionString);

        public void Dispose()
        {
            foreach (var (name, value) in previous)
                Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
        }
    }

    private static void Check(bool condition, string marker)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private static string RequireIsolatedConnection()
    {
        if (Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_DISPOSABLE_ONLY") !=
            "CONFIRMED_CONTAINER_NETNS_ONLY")
            throw new InvalidOperationException("F2A3_PG_REVIEWER_SCOPE_NOT_CONFIRMED");

        var raw = Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_CONNECTION");
        var token = Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_TOKEN");
        if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParseExact(token, "N", out _))
            throw new InvalidOperationException("F2A3_PG_REVIEWER_CONNECTION_OR_TOKEN_MISSING");

        NpgsqlConnectionStringBuilder parsed;
        try
        {
            parsed = new NpgsqlConnectionStringBuilder(raw);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("F2A3_PG_REVIEWER_CONNECTION_INVALID");
        }

        if (parsed.Host != "127.0.0.1" ||
            parsed.Port != 5432 ||
            parsed.Database != DatabaseName ||
            parsed.Username != "postgres" ||
            parsed.SslMode != SslMode.Disable ||
            !string.IsNullOrEmpty(parsed.Password) ||
            parsed.Pooling ||
            parsed.IncludeErrorDetail)
            throw new InvalidOperationException("F2A3_PG_REVIEWER_CONNECTION_SCOPE_REFUSED");

        return raw;
    }

    private static async Task VerifyDisposableSentinelAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT scope_token FROM yangspace_f2a3_qa_guard LIMIT 1";
        var actual = (string?)await command.ExecuteScalarAsync();
        var expected = Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_TOKEN");
        Check(actual is not null && string.Equals(actual, expected, StringComparison.Ordinal),
            "F2A3_PG_REVIEWER_HTTP_SENTINEL_MATCHED");
    }

    private static WebApplicationFactory<ApiAssemblyMarker> Factory(
        string connectionString,
        bool enable,
        string environment = "YangSpaceQa",
        string provider = "PostgreSQL",
        bool sqliteReviewerQa = false)
    {
        var contentRoot = AppContext.BaseDirectory;
        if (!File.Exists(Path.Combine(contentRoot, "appsettings.json")))
            contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "apps", "api");
        if (!File.Exists(Path.Combine(contentRoot, "appsettings.json")))
            throw new InvalidOperationException("F2A3_PG_REVIEWER_API_CONTENT_ROOT_NOT_FOUND");

        return new WebApplicationFactory<ApiAssemblyMarker>().WithWebHostBuilder(host =>
        {
            host.UseContentRoot(contentRoot);
            host.UseEnvironment(environment);
            host.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Preview:Enabled"] = "false",
                    ["Preview:AllowRegistration"] = "false",
                    ["Admin:Enabled"] = "false",
                    ["MerchantRead:Enabled"] = "false",
                    ["ClaimReview:QaEnabled"] = sqliteReviewerQa ? "true" : "false",
                    ["ClaimReviewPostgreSqlQa:Enabled"] = enable ? "true" : "false",
                    ["ConnectionStrings:Main"] = connectionString,
                    ["Database:Provider"] = provider
                }));
        });
    }

    private static async Task<IdentityUser> NewUserAsync(
        UserManager<IdentityUser> users, string email)
    {
        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        var result = await users.CreateAsync(user, Password);
        Check(result.Succeeded, "F2A3_PG_REVIEWER_IDENTITY_SEED_" +
            email.Split('@')[0].ToUpperInvariant().Replace('-', '_'));
        return user;
    }

    private static async Task<string> RealLoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = Password });
        Check(response.StatusCode == HttpStatusCode.OK,
            "F2A3_PG_REVIEWER_LOGIN_" + email.Split('@')[0].ToUpperInvariant().Replace('-', '_'));
        Check(!response.Headers.Contains("Set-Cookie"),
            "F2A3_PG_REVIEWER_BEARER_LOGIN_NO_COOKIE");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        Check(!string.IsNullOrWhiteSpace(token), "F2A3_PG_REVIEWER_REAL_BEARER_ISSUED");
        return token!;
    }

    private static async Task<(HttpStatusCode Status, string Body)> RequestAsync(
        HttpClient client, string path, string? bearer = null, string? cookieHeader = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (cookieHeader is not null)
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        using var response = await client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static bool HasExpectedExceptionMessage(Exception exception, string expectedMessage)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains(expectedMessage, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static void ExpectStartupRefused(
        WebApplicationFactory<ApiAssemblyMarker> factory,
        string expectedMessage,
        string marker)
    {
        var refused = false;
        try
        {
            using var client = factory.CreateClient();
            using var response = client.GetAsync("/health/live").GetAwaiter().GetResult();
        }
        catch (Exception exception) when (
            HasExpectedExceptionMessage(exception, expectedMessage))
        {
            refused = true;
        }
        Check(refused, marker);
    }

    internal static async Task RunAsync()
    {
        var connectionString = RequireIsolatedConnection();
        await VerifyDisposableSentinelAsync(connectionString);
        Console.WriteLine("F2A3_PG_REVIEWER_HTTP_PROVIDER=NPGSQL_REAL_POSTGRESQL");

        var token = Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_TOKEN")!;
        var suffix = token[..8];

        // Fail with a useful diagnostic instead of the framework's opaque
        // UseSolutionRelativeContentRoot exception if the publish is incomplete.
        var expectedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath("/qa"));
        var actualRoot = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(AppContext.BaseDirectory));
        Check(OperatingSystem.IsLinux() &&
              string.Equals(actualRoot, expectedRoot, StringComparison.Ordinal) &&
              File.Exists(Path.Combine(expectedRoot, "appsettings.json")),
              "F2A3_PG_REVIEWER_WAF_CONTENT_ROOT_MARKER");

        using var startup = new StartupEnvironmentScope(connectionString);

        // Program itself must refuse the PostgreSQL QA flag without the
        // per-run disposable-container scope marker, even in YangSpaceQa.
        startup.Configure(true, "YangSpaceQa");
        startup.ConfigureDisposableScope(false);
        using (var invalidScope = Factory(connectionString, true))
        {
            ExpectStartupRefused(invalidScope,
                "require explicit disposable-container and WebApplicationFactory TestServer scopes",
                "F2A3_PG_REVIEWER_DISPOSABLE_SCOPE_REQUIRED");
        }
        startup.ConfigureDisposableScope(true);

        // The PostgreSQL QA flag is only allowed inside the in-process
        // WebApplicationFactory TestServer, never a standalone web host.
        startup.ConfigureTestServerScope(false);
        using (var invalidTestServerScope = Factory(connectionString, true))
        {
            ExpectStartupRefused(invalidTestServerScope,
                "require explicit disposable-container and WebApplicationFactory TestServer scopes",
                "F2A3_PG_REVIEWER_TESTSERVER_ONLY_REQUIRED");
        }
        startup.ConfigureTestServerScope(true);

        // The app must bind the configured connection to the runner-scoped
        // connection value, not accept a different connection from config.
        startup.SetScopedConnection(connectionString + ";Application Name=wrong-scope");
        using (var invalidConnection = Factory(connectionString, true))
        {
            ExpectStartupRefused(invalidConnection,
                "require an exact scoped connection match",
                "F2A3_PG_REVIEWER_CONNECTION_ENV_MATCH_REQUIRED");
        }
        startup.SetScopedConnection(connectionString);

        // A syntactically valid but incorrect token must not unlock the QA route.
        startup.SetExpectedToken("00000000000000000000000000000000");
        using (var invalidSentinel = Factory(connectionString, true))
        {
            ExpectStartupRefused(invalidSentinel,
                "require a matching disposable PostgreSQL sentinel",
                "F2A3_PG_REVIEWER_SENTINEL_REQUIRED");
        }
        startup.SetExpectedToken(token);

        // Default-off must leave both the reviewer probe and Identity login absent.
        startup.Configure(false, "YangSpaceQa");
        using (var disabled = Factory(connectionString, false))
        using (var client = disabled.CreateClient(new WebApplicationFactoryClientOptions
               { HandleCookies = false }))
        {
            var route = await RequestAsync(client, AccessPath);
            Check(route.Status == HttpStatusCode.NotFound,
                "F2A3_PG_REVIEWER_HTTP_DEFAULT_OFF");
            using var login = await client.PostAsJsonAsync("/api/auth/login",
                new { email = "nobody@invalid.example", password = Password });
            Check(login.StatusCode == HttpStatusCode.NotFound,
                "F2A3_PG_REVIEWER_AUTH_DEFAULT_OFF");
        }

        foreach (var environment in new[] { "Production", "Development" })
        {
            startup.Configure(true, environment);
            using var invalid = Factory(connectionString, true, environment);
            ExpectStartupRefused(invalid,
                "Claim review PostgreSQL QA endpoints require explicit YangSpaceQa",
                "F2A3_PG_REVIEWER_ENVIRONMENT_FAIL_CLOSED_" + environment.ToUpperInvariant());
        }

        startup.Configure(true, "YangSpaceQa", "SQLite");
        using (var invalidProvider = Factory(connectionString, true, "YangSpaceQa", "SQLite"))
        {
            ExpectStartupRefused(invalidProvider,
                "Claim review PostgreSQL QA endpoints require PostgreSQL persistence",
                "F2A3_PG_REVIEWER_PROVIDER_FAIL_CLOSED");
        }

        startup.Configure(true, "YangSpaceQa", "PostgreSQL", sqliteReviewerQa: true);
        using (var conflictingQa = Factory(connectionString, true, "YangSpaceQa",
                   "PostgreSQL", sqliteReviewerQa: true))
        {
            ExpectStartupRefused(conflictingQa,
                "Claim review PostgreSQL QA endpoints require explicit YangSpaceQa",
                "F2A3_PG_REVIEWER_QA_SURFACE_EXCLUSION");
        }

        startup.Configure(true, "YangSpaceQa", "PostgreSQL");
        using var factory = Factory(connectionString, true);
        using var http = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        IdentityUser reviewer;
        IdentityUser platformAdmin;
        IdentityUser applicant;
        Guid activeOrganizationId;
        Guid revokedOrganizationId;
        Guid activeClaimId;
        Guid revokedClaimId;
        Guid activeLocationId;
        Guid revokedLocationId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            Check(applied.Contains(ExpectedMigration),
                "F2A3_PG_REVIEWER_HTTP_REVIEWED_MIGRATION_PRESENT");

            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync(ClaimReviewRoles.Reviewer))
                Check((await roles.CreateAsync(new IdentityRole(ClaimReviewRoles.Reviewer))).Succeeded,
                    "F2A3_PG_REVIEWER_ROLE_CREATED");
            else
                Check(true, "F2A3_PG_REVIEWER_ROLE_EXISTS");
            if (!await roles.RoleExistsAsync("PlatformAdmin"))
                Check((await roles.CreateAsync(new IdentityRole("PlatformAdmin"))).Succeeded,
                    "F2A3_PG_PLATFORM_ADMIN_ROLE_CREATED");
            else
                Check(true, "F2A3_PG_PLATFORM_ADMIN_ROLE_EXISTS");

            reviewer = await NewUserAsync(users, $"reviewer-{suffix}@invalid.example");
            platformAdmin = await NewUserAsync(users, $"platform-admin-{suffix}@invalid.example");
            applicant = await NewUserAsync(users, $"applicant-{suffix}@invalid.example");

            Check((await users.AddToRoleAsync(reviewer, ClaimReviewRoles.Reviewer)).Succeeded,
                "F2A3_PG_REVIEWER_ROLE_ASSIGNED");
            Check((await users.AddToRoleAsync(platformAdmin, "PlatformAdmin")).Succeeded,
                "F2A3_PG_PLATFORM_ADMIN_ROLE_ASSIGNED");

            var activeOrganization = new Organization
            {
                Name = "Disposable active member " + suffix,
                Status = OrganizationStatus.Active
            };
            var revokedOrganization = new Organization
            {
                Name = "Disposable revoked member " + suffix,
                Status = OrganizationStatus.Active
            };
            activeOrganizationId = activeOrganization.Id;
            revokedOrganizationId = revokedOrganization.Id;

            var activeLocation = new BusinessLocation
            {
                Slug = "review-active-" + suffix,
                Name = "Disposable active claim target",
                Category = "test",
                Neighborhood = "test",
                Description = "QA only",
                Address = "Disposable PostgreSQL"
            };
            var revokedLocation = new BusinessLocation
            {
                Slug = "review-revoked-" + suffix,
                Name = "Disposable revoked claim target",
                Category = "test",
                Neighborhood = "test",
                Description = "QA only",
                Address = "Disposable PostgreSQL"
            };
            activeLocationId = activeLocation.Id;
            revokedLocationId = revokedLocation.Id;

            var activeClaim = new BusinessClaim
            {
                LocationId = activeLocation.Id,
                OrganizationId = activeOrganizationId,
                ApplicantUserId = applicant.Id,
                Status = BusinessClaimStatus.Pending
            };
            var revokedClaim = new BusinessClaim
            {
                LocationId = revokedLocation.Id,
                OrganizationId = revokedOrganizationId,
                ApplicantUserId = applicant.Id,
                Status = BusinessClaimStatus.Pending
            };
            activeClaimId = activeClaim.Id;
            revokedClaimId = revokedClaim.Id;

            db.Organizations.AddRange(activeOrganization, revokedOrganization);
            db.Locations.AddRange(activeLocation, revokedLocation);
            db.BusinessClaims.AddRange(activeClaim, revokedClaim);
            db.OrganizationMembers.AddRange(
                new OrganizationMember
                {
                    OrganizationId = activeOrganizationId,
                    UserId = reviewer.Id,
                    Role = OrganizationMemberRole.Editor,
                    GrantedByUserId = platformAdmin.Id
                },
                new OrganizationMember
                {
                    OrganizationId = revokedOrganizationId,
                    UserId = reviewer.Id,
                    Role = OrganizationMemberRole.Owner,
                    GrantedByUserId = platformAdmin.Id,
                    RevokedByUserId = platformAdmin.Id,
                    RevokedAtUtc = DateTime.UtcNow
                });
            await db.SaveChangesAsync();

            var conflicts = scope.ServiceProvider.GetRequiredService<ClaimReviewConflictPolicy>();
            Check(!await conflicts.CanReviewClaimAsync(activeClaimId, reviewer.Id),
                "F2A3_PG_REVIEWER_ACTIVE_MEMBER_CONFLICT");
            Check(!await conflicts.CanReviewClaimAsync(activeClaimId, applicant.Id),
                "F2A3_PG_REVIEWER_SELF_REVIEW_CONFLICT");
            Check(await conflicts.CanReviewClaimAsync(revokedClaimId, reviewer.Id),
                "F2A3_PG_REVIEWER_REVOKED_MEMBER_NOT_ACTIVE_CONFLICT");
            Check(await conflicts.CanReviewClaimAsync(revokedClaimId, platformAdmin.Id),
                "F2A3_PG_REVIEWER_UNRELATED_REVIEWER_NO_CONFLICT");
            Check(!await conflicts.CanReviewClaimAsync(Guid.NewGuid(), reviewer.Id),
                "F2A3_PG_REVIEWER_UNKNOWN_CLAIM_FAIL_CLOSED");
            Check(!await conflicts.CanReviewClaimAsync(activeClaimId, string.Empty),
                "F2A3_PG_REVIEWER_EMPTY_SUBJECT_FAIL_CLOSED");
        }

        var reviewerEmail = $"reviewer-{suffix}@invalid.example";
        var adminEmail = $"platform-admin-{suffix}@invalid.example";
        var applicantEmail = $"applicant-{suffix}@invalid.example";
        var reviewerToken = await RealLoginAsync(http, reviewerEmail);
        var adminToken = await RealLoginAsync(http, adminEmail);
        var applicantToken = await RealLoginAsync(http, applicantEmail);

        Check((await RequestAsync(http, AccessPath)).Status == HttpStatusCode.Unauthorized,
            "F2A3_PG_REVIEWER_ANONYMOUS_401");
        Check((await RequestAsync(http, AccessPath, "invalid-token")).Status == HttpStatusCode.Unauthorized,
            "F2A3_PG_REVIEWER_INVALID_BEARER_401");
        Check((await RequestAsync(http, AccessPath,
            cookieHeader: ".AspNetCore.Identity.Application=fake-cookie")).Status ==
            HttpStatusCode.Unauthorized,
            "F2A3_PG_REVIEWER_COOKIE_ONLY_401");

        var reviewerResponse = await RequestAsync(http, AccessPath, reviewerToken);
        Check(reviewerResponse.Status == HttpStatusCode.OK,
            "F2A3_PG_REVIEWER_DEDICATED_ROLE_200");
        using (var payload = JsonDocument.Parse(reviewerResponse.Body))
        {
            var names = payload.RootElement.EnumerateObject()
                .Select(property => property.Name).ToArray();
            Check(names.SequenceEqual(new[] { "authorized" }) &&
                  payload.RootElement.GetProperty("authorized").GetBoolean(),
                "F2A3_PG_REVIEWER_AUTH_RESPONSE_MINIMAL");
        }

        Check((await RequestAsync(http, AccessPath, adminToken)).Status == HttpStatusCode.Forbidden,
            "F2A3_PG_REVIEWER_PLATFORM_ADMIN_ONLY_403");
        Check((await RequestAsync(http, AccessPath, applicantToken)).Status == HttpStatusCode.Forbidden,
            "F2A3_PG_REVIEWER_NON_REVIEWER_403");

        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var reviewerForRevocation = await users.FindByIdAsync(reviewer.Id);
            Check(reviewerForRevocation is not null,
                "F2A3_PG_REVIEWER_RELOADED_IN_REVOCATION_SCOPE");
            Check(await users.IsInRoleAsync(reviewerForRevocation!, ClaimReviewRoles.Reviewer),
                "F2A3_PG_REVIEWER_ROLE_PRESENT_BEFORE_REVOCATION");
            Check((await users.RemoveFromRoleAsync(
                reviewerForRevocation!, ClaimReviewRoles.Reviewer)).Succeeded,
                "F2A3_PG_REVIEWER_ROLE_REVOKED");
        }
        Check((await RequestAsync(http, AccessPath, reviewerToken)).Status == HttpStatusCode.Forbidden,
            "F2A3_PG_REVIEWER_SAME_TOKEN_ROLE_REVOCATION_403");

        using (var register = await http.PostAsJsonAsync("/api/auth/register",
            new { email = $"attacker-{suffix}@invalid.example", password = Password }))
            Check(register.StatusCode == HttpStatusCode.NotFound,
                "F2A3_PG_REVIEWER_REGISTRATION_NOT_EXPOSED");
        using (var cookieLogin = await http.PostAsJsonAsync(
            "/api/auth/login?useCookies=true",
            new { email = reviewerEmail, password = Password }))
            Check(cookieLogin.StatusCode == HttpStatusCode.BadRequest,
                "F2A3_PG_REVIEWER_COOKIE_LOGIN_REJECTED");

        using (var decision = new HttpRequestMessage(
            HttpMethod.Post, "/api/v1/staff/claims/" + Guid.NewGuid() + "/decision"))
        {
            decision.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", reviewerToken);
            using var response = await http.SendAsync(decision);
            Check(response.StatusCode == HttpStatusCode.NotFound,
                "F2A3_PG_REVIEWER_DECISION_ROUTE_NOT_EXPOSED");
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
            var claims = await db.BusinessClaims.AsNoTracking()
                .Where(x => x.Id == activeClaimId || x.Id == revokedClaimId).ToArrayAsync();
            var organizations = await db.Organizations.AsNoTracking()
                .Where(x => x.Id == activeOrganizationId || x.Id == revokedOrganizationId).ToArrayAsync();
            var locations = await db.Locations.AsNoTracking()
                .Where(x => x.Id == activeLocationId || x.Id == revokedLocationId).ToArrayAsync();
            var members = await db.OrganizationMembers.AsNoTracking()
                .Where(x => x.OrganizationId == activeOrganizationId ||
                            x.OrganizationId == revokedOrganizationId).ToArrayAsync();
            var audits = await db.ContentAuditEvents.AsNoTracking()
                .Where(x => x.LocationId == activeLocationId ||
                            x.LocationId == revokedLocationId).CountAsync();

            Check(claims.Length == 2 &&
                  claims.All(x => x.Status == BusinessClaimStatus.Pending && x.Revision == 0) &&
                  organizations.Length == 2 &&
                  organizations.All(x => x.Status == OrganizationStatus.Active && x.Revision == 0) &&
                  locations.Length == 2 && locations.All(x => x.OrganizationId is null && x.Revision == 0) &&
                  members.Length == 2 && audits == 0,
                "F2A3_PG_REVIEWER_HTTP_NO_BUSINESS_SIDE_EFFECTS");
        }

        Console.WriteLine("F2A3_PG_REVIEWER_IDENTITY_HTTP=PASS");
        Console.WriteLine("F2A3_PG_REVIEWER_HTTP_ISOLATED_ONLY=PASS");
    }
}
