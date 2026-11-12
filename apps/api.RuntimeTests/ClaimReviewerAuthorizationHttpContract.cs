using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YangSpace.Api;

// Real Identity bearer HTTP on TestServer + disposable SQLite. Not PostgreSQL,
// claim mutation or production proof.
internal static class ClaimReviewerAuthorizationHttpContract
{
    private const string Password = "OnlyForQa!Pass2026";
    private const string AccessPath = "/api/v1/staff/claim-review/access";

    private sealed class StartupEnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string?> previous = new(StringComparer.Ordinal);
        private static readonly string[] Names =
        [
            "ConnectionStrings__Main", "Database__Provider", "Preview__Enabled",
            "Preview__AllowRegistration", "Admin__Enabled", "MerchantRead__Enabled",
            "ClaimReview__QaEnabled", "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT"
        ];

        internal StartupEnvironmentScope(string sqlitePath)
        {
            foreach (var name in Names)
                previous.Add(name, Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process));
            var connection = new SqliteConnectionStringBuilder
            {
                DataSource = sqlitePath, Mode = SqliteOpenMode.ReadWriteCreate,
                ForeignKeys = true, Pooling = false
            }.ConnectionString;
            Set("ConnectionStrings__Main", connection);
            Set("Preview__Enabled", "false");
            Set("Preview__AllowRegistration", "false");
            Set("Admin__Enabled", "false");
            Set("MerchantRead__Enabled", "false");
            Configure(false, "YangSpaceQa");
        }

        private static void Set(string name, string value) =>
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);

        internal void Configure(bool enable, string environment, string provider = "SQLite")
        {
            Set("ASPNETCORE_ENVIRONMENT", environment);
            Set("DOTNET_ENVIRONMENT", environment);
            Set("ClaimReview__QaEnabled", enable ? "true" : "false");
            Set("Database__Provider", provider);
        }

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

    private static WebApplicationFactory<ApiAssemblyMarker> Factory(
        string filePath, bool enable, string environment = "YangSpaceQa", string provider = "SQLite")
    {
        var apiContentRoot = Path.Combine(Directory.GetCurrentDirectory(), "apps", "api");
        if (!File.Exists(Path.Combine(apiContentRoot, "appsettings.json")))
            throw new InvalidOperationException("F2A3_REVIEWER_REPO_ROOT_NOT_FOUND");

        return new WebApplicationFactory<ApiAssemblyMarker>().WithWebHostBuilder(host =>
        {
            host.UseContentRoot(apiContentRoot);
            host.UseEnvironment(environment);
            host.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Preview:Enabled"] = "false",
                    ["Preview:AllowRegistration"] = "false",
                    ["Admin:Enabled"] = "false",
                    ["MerchantRead:Enabled"] = "false",
                    ["ClaimReview:QaEnabled"] = enable ? "true" : "false",
                    ["ConnectionStrings:Main"] = new SqliteConnectionStringBuilder
                    {
                        DataSource = filePath, Mode = SqliteOpenMode.ReadWriteCreate,
                        ForeignKeys = true, Pooling = false
                    }.ConnectionString,
                    ["Database:Provider"] = provider
                }));
        });
    }

    private static async Task<IdentityUser> NewUserAsync(UserManager<IdentityUser> users, string email)
    {
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, Password);
        Check(result.Succeeded, "CLAIM_REVIEW_IDENTITY_SEED_" + email.Split('@')[0].ToUpperInvariant());
        return user;
    }

    private static async Task<string> RealLoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Check(response.StatusCode == HttpStatusCode.OK, "CLAIM_REVIEW_LOGIN_" + email.Split('@')[0].ToUpperInvariant());
        Check(!response.Headers.Contains("Set-Cookie"), "CLAIM_REVIEW_BEARER_LOGIN_NO_COOKIE");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        Check(!string.IsNullOrWhiteSpace(token), "CLAIM_REVIEW_REAL_BEARER_ISSUED");
        return token!;
    }

    private static async Task<(HttpStatusCode Status, string Body)> RequestAsync(
        HttpClient client, string path, string? bearer = null, string? cookieHeader = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (cookieHeader is not null) request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        using var response = await client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "yangspace-f2a3-reviewer-auth-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var dbPath = Path.Combine(root, "reviewer-auth-qa.db");
            using var startup = new StartupEnvironmentScope(dbPath);
            startup.Configure(false, "YangSpaceQa");

            using (var disabled = Factory(dbPath, false))
            using (var client = disabled.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false }))
            {
                var route = await RequestAsync(client, AccessPath);
                Check(route.Status == HttpStatusCode.NotFound, "CLAIM_REVIEW_HTTP_DEFAULT_OFF");
                using var login = await client.PostAsJsonAsync("/api/auth/login",
                    new { email = "nobody@invalid.example", password = Password });
                Check(login.StatusCode == HttpStatusCode.NotFound, "CLAIM_REVIEW_AUTH_DEFAULT_OFF");
            }

            foreach (var environment in new[] { "Production", "Development" })
            {
                startup.Configure(true, environment);
                using var invalid = Factory(dbPath, true, environment);
                var refused = false;
                try
                {
                    using var invalidClient = invalid.CreateClient();
                    using var response = await invalidClient.GetAsync("/health/live");
                }
                catch (InvalidOperationException exception) when (exception.Message.Contains(
                    "Claim review QA endpoints require explicit YangSpaceQa", StringComparison.Ordinal))
                { refused = true; }
                Check(refused, "CLAIM_REVIEW_ENVIRONMENT_FAIL_CLOSED_" + environment.ToUpperInvariant());
            }

            startup.Configure(true, "YangSpaceQa", "PostgreSQL");
            using (var invalidProvider = Factory(dbPath, true, "YangSpaceQa", "PostgreSQL"))
            {
                var refused = false;
                try
                {
                    using var invalidClient = invalidProvider.CreateClient();
                    using var response = await invalidClient.GetAsync("/health/live");
                }
                catch (InvalidOperationException exception) when (exception.Message.Contains(
                    "Claim review QA endpoints require isolated SQLite persistence", StringComparison.Ordinal))
                { refused = true; }
                Check(refused, "CLAIM_REVIEW_DATABASE_PROVIDER_FAIL_CLOSED");
            }

            startup.Configure(true, "YangSpaceQa");
            using var factory = Factory(dbPath, true);
            using var http = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

            IdentityUser reviewer;
            IdentityUser platformAdmin;
            IdentityUser applicant;
            Guid activeOrganizationId;
            Guid revokedOrganizationId;

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
                await db.Database.EnsureCreatedAsync();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                Check((await roles.CreateAsync(new IdentityRole(ClaimReviewRoles.Reviewer))).Succeeded,
                    "CLAIM_REVIEW_REVIEWER_ROLE_SEEDED");
                Check((await roles.CreateAsync(new IdentityRole("PlatformAdmin"))).Succeeded,
                    "CLAIM_REVIEW_PLATFORM_ADMIN_ROLE_SEEDED");

                reviewer = await NewUserAsync(users, "reviewer@invalid.example");
                platformAdmin = await NewUserAsync(users, "platform-admin@invalid.example");
                applicant = await NewUserAsync(users, "applicant@invalid.example");
                Check((await users.AddToRoleAsync(reviewer, ClaimReviewRoles.Reviewer)).Succeeded,
                    "CLAIM_REVIEW_REVIEWER_ROLE_ASSIGNED");
                Check((await users.AddToRoleAsync(platformAdmin, "PlatformAdmin")).Succeeded,
                    "CLAIM_REVIEW_PLATFORM_ADMIN_ROLE_ASSIGNED");

                var activeOrganization = new Organization
                { Name = "Disposable active-member conflict", Status = OrganizationStatus.Active };
                var revokedOrganization = new Organization
                { Name = "Disposable revoked-member control", Status = OrganizationStatus.Active };
                activeOrganizationId = activeOrganization.Id;
                revokedOrganizationId = revokedOrganization.Id;
                db.Organizations.AddRange(activeOrganization, revokedOrganization);
                db.OrganizationMembers.AddRange(
                    new OrganizationMember
                    {
                        OrganizationId = activeOrganizationId, UserId = reviewer.Id,
                        Role = OrganizationMemberRole.Editor, GrantedByUserId = platformAdmin.Id
                    },
                    new OrganizationMember
                    {
                        OrganizationId = revokedOrganizationId, UserId = reviewer.Id,
                        Role = OrganizationMemberRole.Owner, GrantedByUserId = platformAdmin.Id,
                        RevokedByUserId = platformAdmin.Id, RevokedAtUtc = DateTime.UtcNow
                    });
                await db.SaveChangesAsync();

                var conflicts = scope.ServiceProvider.GetRequiredService<ClaimReviewConflictPolicy>();
                Check(await conflicts.IsConflictAsync(activeOrganizationId, reviewer.Id, applicant.Id),
                    "CLAIM_REVIEW_ACTIVE_MEMBER_CONFLICT");
                Check(await conflicts.IsConflictAsync(activeOrganizationId, reviewer.Id, reviewer.Id),
                    "CLAIM_REVIEW_SELF_REVIEW_CONFLICT");
                Check(!await conflicts.IsConflictAsync(revokedOrganizationId, reviewer.Id, applicant.Id),
                    "CLAIM_REVIEW_REVOKED_MEMBER_NOT_ACTIVE_CONFLICT");
                Check(!await conflicts.IsConflictAsync(revokedOrganizationId, platformAdmin.Id, applicant.Id),
                    "CLAIM_REVIEW_UNRELATED_REVIEWER_NO_CONFLICT");
            }

            var reviewerToken = await RealLoginAsync(http, "reviewer@invalid.example");
            var adminToken = await RealLoginAsync(http, "platform-admin@invalid.example");
            var applicantToken = await RealLoginAsync(http, "applicant@invalid.example");

            Check((await RequestAsync(http, AccessPath)).Status == HttpStatusCode.Unauthorized,
                "CLAIM_REVIEW_ANONYMOUS_401");
            Check((await RequestAsync(http, AccessPath, "invalid-token")).Status == HttpStatusCode.Unauthorized,
                "CLAIM_REVIEW_INVALID_BEARER_401");
            Check((await RequestAsync(http, AccessPath, cookieHeader: ".AspNetCore.Identity.Application=fake-cookie")).Status
                == HttpStatusCode.Unauthorized, "CLAIM_REVIEW_COOKIE_ONLY_401");

            var reviewerResponse = await RequestAsync(http, AccessPath, reviewerToken);
            Check(reviewerResponse.Status == HttpStatusCode.OK, "CLAIM_REVIEW_DEDICATED_ROLE_200");
            using (var payload = JsonDocument.Parse(reviewerResponse.Body))
            {
                var names = payload.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
                Check(names.SequenceEqual(new[] { "authorized" }) &&
                    payload.RootElement.GetProperty("authorized").GetBoolean(),
                    "CLAIM_REVIEW_AUTH_RESPONSE_MINIMAL");
            }

            Check((await RequestAsync(http, AccessPath, adminToken)).Status == HttpStatusCode.Forbidden,
                "CLAIM_REVIEW_PLATFORM_ADMIN_ONLY_403");
            Check((await RequestAsync(http, AccessPath, applicantToken)).Status == HttpStatusCode.Forbidden,
                "CLAIM_REVIEW_NON_REVIEWER_403");

            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                Check((await users.RemoveFromRoleAsync(reviewer, ClaimReviewRoles.Reviewer)).Succeeded,
                    "CLAIM_REVIEW_REVIEWER_ROLE_REVOKED");
            }
            Check((await RequestAsync(http, AccessPath, reviewerToken)).Status == HttpStatusCode.Forbidden,
                "CLAIM_REVIEW_SAME_TOKEN_ROLE_REVOCATION_403");

            using (var register = await http.PostAsJsonAsync("/api/auth/register",
                new { email = "attacker@invalid.example", password = Password }))
                Check(register.StatusCode == HttpStatusCode.NotFound, "CLAIM_REVIEW_REGISTRATION_NOT_EXPOSED");
            using (var cookieLogin = await http.PostAsJsonAsync("/api/auth/login?useCookies=true",
                new { email = "reviewer@invalid.example", password = Password }))
                Check(cookieLogin.StatusCode == HttpStatusCode.BadRequest, "CLAIM_REVIEW_COOKIE_LOGIN_REJECTED");

            using (var decision = new HttpRequestMessage(HttpMethod.Post,
                "/api/v1/staff/claims/" + Guid.NewGuid() + "/decision"))
            {
                decision.Headers.Authorization = new AuthenticationHeaderValue("Bearer", reviewerToken);
                using var response = await http.SendAsync(decision);
                Check(response.StatusCode == HttpStatusCode.NotFound,
                    "CLAIM_REVIEW_DECISION_ROUTE_NOT_EXPOSED");
            }

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
                Check(await db.Organizations.CountAsync() == 2 &&
                    await db.OrganizationMembers.CountAsync() == 2 &&
                    await db.Locations.CountAsync() == 0 &&
                    !await db.BusinessClaims.AnyAsync() &&
                    !await db.ContentAuditEvents.AnyAsync(),
                    "CLAIM_REVIEW_NO_BUSINESS_SIDE_EFFECTS");
            }

            Console.WriteLine("F2A3_REVIEWER_AUTHORIZATION_HTTP_CONTRACT=PASS");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
