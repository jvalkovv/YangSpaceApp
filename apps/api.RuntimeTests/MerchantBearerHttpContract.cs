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

// Real HTTP requests + actual Identity login-issued bearer tokens. Isolated
// ASP.NET Core TestServer and disposable SQLite schema; no mock identity or
// access to the running preview/production site.
internal static class MerchantBearerHttpContract
{
    private const string Password = "OnlyForQa!Pass2026";

    private static void Check(bool ok, string marker)
    {
        if (!ok)
            throw new InvalidOperationException("FAIL: " + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private static WebApplicationFactory<ApiAssemblyMarker> Factory(
        string filePath, bool enable, string environment = "YangSpaceQa")
    {
        return new WebApplicationFactory<ApiAssemblyMarker>().WithWebHostBuilder(host =>
        {
            host.UseEnvironment(environment);
            host.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Preview:Enabled"] = "false",
                    ["Preview:AllowRegistration"] = "false",
                    ["Admin:Enabled"] = "false",
                    ["MerchantRead:Enabled"] = enable ? "true" : "false",
                    ["ConnectionStrings:Main"] = new SqliteConnectionStringBuilder
                    {
                        DataSource = filePath,
                        Mode = SqliteOpenMode.ReadWriteCreate,
                        ForeignKeys = true,
                        Pooling = false
                    }.ConnectionString,
                    ["Database:Provider"] = "SQLite"
                });
            });
        });
    }

    private static async Task<IdentityUser> NewUserAsync(
        UserManager<IdentityUser> users, string email)
    {
        var user = new IdentityUser
        {
            UserName = email, Email = email, EmailConfirmed = true
        };
        var result = await users.CreateAsync(user, Password);
        Check(result.Succeeded, "MERCHANT_QA_IDENTITY_SEED_" + email.Split('@')[0].ToUpperInvariant());
        return user;
    }

    private static async Task<string> RealLoginAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = Password });
        Check(response.StatusCode == HttpStatusCode.OK,
            "MERCHANT_QA_LOGIN_" + email.Split('@')[0].ToUpperInvariant());

        Check(!response.Headers.Contains("Set-Cookie"), "MERCHANT_HTTP_LOGIN_NO_COOKIE");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        Check(!string.IsNullOrWhiteSpace(token), "MERCHANT_HTTP_ISSUED_REAL_BEARER");
        return token!;
    }

    private static async Task<(HttpStatusCode Status, string Body)> RequestAsync(
        HttpClient client, string path, string? bearer = null,
        string? actorHeader = null, string? cookieHeader = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (actorHeader is not null)
            request.Headers.TryAddWithoutValidation("X-Actor-UserId", actorHeader);
        if (cookieHeader is not null)
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        using var response = await client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "yangspace-f2a2-http-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var dbPath = Path.Combine(root, "isolated-merchant-qa.db");

            // Default-OFF means neither private data route nor login appears.
            using (var disabled = Factory(dbPath, enable: false))
            using (var disabledClient = disabled.CreateClient())
            {
                var notMapped = await RequestAsync(
                    disabledClient, "/api/v1/merchant/locations/" + Guid.NewGuid());
                Check(notMapped.Status == HttpStatusCode.NotFound,
                    "MERCHANT_HTTP_DEFAULT_OFF");
                using var loginNotMapped = await disabledClient.PostAsJsonAsync(
                    "/api/auth/login", new { email = "nobody@invalid.example", password = Password });
                Check(loginNotMapped.StatusCode == HttpStatusCode.NotFound,
                    "MERCHANT_HTTP_AUTH_DEFAULT_OFF");
            }

            // Ensure a QA flag cannot be accidentally enabled in a production
            // or ordinary development process, even with an explicit true value.
            foreach (var environment in new[] { "Production", "Development" })
            {
                using var invalid = Factory(dbPath, enable: true, environment);
                var refused = false;
                try
                {
                    using var invalidClient = invalid.CreateClient();
                    using var invalidResponse = await invalidClient.GetAsync("/health/live");
                }
                catch (InvalidOperationException ex)
                    when (ex.Message.Contains("Merchant read QA endpoints require explicit",
                        StringComparison.Ordinal))
                {
                    refused = true;
                }
                Check(refused, "MERCHANT_HTTP_ENVIRONMENT_FAIL_CLOSED_" +
                    environment.ToUpperInvariant());
            }

            using var factory = Factory(dbPath, enable: true);
            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions { HandleCookies = false });

            var orgAId = Guid.NewGuid();
            var orgBId = Guid.NewGuid();
            var orgSuspendedId = Guid.NewGuid();
            var locationAId = Guid.NewGuid();
            var locationBId = Guid.NewGuid();
            var unclaimedId = Guid.NewGuid();
            var suspendedId = Guid.NewGuid();
            string ownerId;
            string editorId;
            string staffId;
            Guid editorMemberId;

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
                await db.Database.EnsureCreatedAsync();

                var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                var owner = await NewUserAsync(users, "owner-a@invalid.example");
                var editor = await NewUserAsync(users, "editor-b@invalid.example");
                var staff = await NewUserAsync(users, "staff@invalid.example");
                ownerId = owner.Id;
                editorId = editor.Id;
                staffId = staff.Id;

                var roleResult = await roles.CreateAsync(new IdentityRole("PlatformAdmin"));
                Check(roleResult.Succeeded, "MERCHANT_HTTP_STAFF_ROLE_SEEDED");
                Check((await users.AddToRoleAsync(staff, "PlatformAdmin")).Succeeded,
                    "MERCHANT_HTTP_STAFF_ROLE_ASSIGNED");

                db.Organizations.AddRange(
                    new Organization { Id = orgAId, Name = "QA A", Status = OrganizationStatus.Active },
                    new Organization { Id = orgBId, Name = "QA B", Status = OrganizationStatus.Active },
                    new Organization { Id = orgSuspendedId, Name = "QA Suspended",
                        Status = OrganizationStatus.Suspended });

                editorMemberId = Guid.NewGuid();
                db.OrganizationMembers.AddRange(
                    new OrganizationMember { OrganizationId = orgAId, UserId = ownerId,
                        Role = OrganizationMemberRole.Owner, GrantedByUserId = staffId },
                    new OrganizationMember { Id = editorMemberId, OrganizationId = orgBId,
                        UserId = editorId, Role = OrganizationMemberRole.Editor,
                        GrantedByUserId = staffId });

                db.Locations.AddRange(
                    new BusinessLocation { Id = locationAId, OrganizationId = orgAId,
                        Name = "Private QA A", Slug = "qa-private-a", IsPublished = false,
                        Phone = "NEVER-EXPOSE-PRIVATE", Revision = 3 },
                    new BusinessLocation { Id = locationBId, OrganizationId = orgBId,
                        Name = "Private QA B", Slug = "qa-private-b", IsPublished = false,
                        Revision = 7 },
                    new BusinessLocation { Id = unclaimedId, OrganizationId = null,
                        Name = "Unclaimed", Slug = "qa-unclaimed", Revision = 0 },
                    new BusinessLocation { Id = suspendedId, OrganizationId = orgSuspendedId,
                        Name = "Suspended", Slug = "qa-suspended", Revision = 0 });
                await db.SaveChangesAsync();
            }
            Console.WriteLine("MERCHANT_HTTP_SYNTHETIC_DB_SEEDED=PASS");

            var ownerToken = await RealLoginAsync(client, "owner-a@invalid.example");
            var editorToken = await RealLoginAsync(client, "editor-b@invalid.example");
            var staffToken = await RealLoginAsync(client, "staff@invalid.example");

            string PathFor(Guid id) => "/api/v1/merchant/locations/" + id.ToString("D");

            var anonymous = await RequestAsync(client, PathFor(locationAId));
            Check(anonymous.Status == HttpStatusCode.Unauthorized, "MERCHANT_HTTP_ANONYMOUS_401");

            var invalidBearer = await RequestAsync(client, PathFor(locationAId), "invalid-token");
            Check(invalidBearer.Status == HttpStatusCode.Unauthorized,
                "MERCHANT_HTTP_INVALID_BEARER_401");

            var cookieOnly = await RequestAsync(client, PathFor(locationAId),
                cookieHeader: ".AspNetCore.Identity.Application=fake-cookie");
            Check(cookieOnly.Status == HttpStatusCode.Unauthorized,
                "MERCHANT_HTTP_COOKIE_ONLY_401");

            var owner = await RequestAsync(client, PathFor(locationAId), ownerToken);
            Check(owner.Status == HttpStatusCode.OK, "MERCHANT_HTTP_OWNER_A_200");
            using (var dto = JsonDocument.Parse(owner.Body))
            {
                var keys = dto.RootElement.EnumerateObject()
                    .Select(x => x.Name).OrderBy(x => x).ToArray();
                var allowed = new[] { "id", "isPublished", "name", "revision", "slug" };
                Check(keys.SequenceEqual(allowed) &&
                      dto.RootElement.GetProperty("revision").GetInt64() == 3 &&
                      owner.Body.IndexOf("NEVER-EXPOSE-PRIVATE", StringComparison.Ordinal) < 0,
                    "MERCHANT_HTTP_PRIVATE_DTO_ALLOWLIST");
            }

            var editor = await RequestAsync(client, PathFor(locationBId), editorToken);
            Check(editor.Status == HttpStatusCode.OK, "MERCHANT_HTTP_EDITOR_B_200");

            var crossA = await RequestAsync(client, PathFor(locationBId), ownerToken);
            var crossB = await RequestAsync(client, PathFor(locationAId), editorToken);
            Check(crossA.Status == HttpStatusCode.NotFound &&
                  crossB.Status == HttpStatusCode.NotFound,
                "MERCHANT_HTTP_CROSS_TENANT_404");

            var unknown = await RequestAsync(client, PathFor(Guid.NewGuid()), ownerToken);
            var unclaimed = await RequestAsync(client, PathFor(unclaimedId), ownerToken);
            var suspended = await RequestAsync(client, PathFor(suspendedId), ownerToken);
            Check(unknown.Status == HttpStatusCode.NotFound &&
                  unclaimed.Status == HttpStatusCode.NotFound &&
                  suspended.Status == HttpStatusCode.NotFound,
                "MERCHANT_HTTP_UNKNOWN_UNCLAIMED_SUSPENDED_404");

            var forged = await RequestAsync(client, PathFor(locationBId), ownerToken,
                actorHeader: editorId);
            Check(forged.Status == HttpStatusCode.NotFound,
                "MERCHANT_HTTP_FORGED_ACTOR_DENIED");

            var admin = await RequestAsync(client, PathFor(locationAId), staffToken);
            Check(admin.Status == HttpStatusCode.NotFound,
                "MERCHANT_HTTP_PLATFORM_ADMIN_NOT_OWNER");

            using (var register = await client.PostAsJsonAsync("/api/auth/register",
                new { email = "attacker@invalid.example", password = Password }))
                Check(register.StatusCode == HttpStatusCode.NotFound,
                    "MERCHANT_HTTP_REGISTRATION_NOT_EXPOSED");

            using (var cookieLogin = await client.PostAsJsonAsync(
                "/api/auth/login?useCookies=true",
                new { email = "owner-a@invalid.example", password = Password }))
                Check(cookieLogin.StatusCode == HttpStatusCode.BadRequest,
                    "MERCHANT_HTTP_COOKIE_LOGIN_REJECTED");

            using (var put = new HttpRequestMessage(HttpMethod.Put, PathFor(locationAId)))
            {
                put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
                using var result = await client.SendAsync(put);
                Check(result.StatusCode == HttpStatusCode.MethodNotAllowed,
                    "MERCHANT_HTTP_NO_MUTATION_ROUTE");
            }

            // Membership and organization changes happen AFTER real tokens
            // are issued; the same bearer must immediately stop authorizing.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
                var member = await db.OrganizationMembers.SingleAsync(x => x.Id == editorMemberId);
                member.RevokedAtUtc = DateTime.UtcNow;
                member.RevokedByUserId = staffId;
                member.Revision++;
                await db.SaveChangesAsync();
            }
            var revoked = await RequestAsync(client, PathFor(locationBId), editorToken);
            Check(revoked.Status == HttpStatusCode.NotFound,
                "MERCHANT_HTTP_SAME_TOKEN_REVOKED_IMMEDIATELY");

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
                var org = await db.Organizations.SingleAsync(x => x.Id == orgAId);
                org.Status = OrganizationStatus.Suspended;
                org.Revision++;
                await db.SaveChangesAsync();
            }
            var suspendedAfterToken = await RequestAsync(client, PathFor(locationAId), ownerToken);
            Check(suspendedAfterToken.Status == HttpStatusCode.NotFound,
                "MERCHANT_HTTP_SAME_TOKEN_ORGANIZATION_SUSPENDED");

            // Read-only HTTP traffic may never mutate location/revision, claims
            // or audit. Test fixtures' explicit membership and org revocation
            // above are the ONLY allowed data mutations after initial seeding.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
                Check(await db.Locations.CountAsync() == 4 &&
                      await db.Locations.AnyAsync(x => x.Id == locationAId && x.Revision == 3) &&
                      await db.Locations.AnyAsync(x => x.Id == locationBId && x.Revision == 7) &&
                      !await db.BusinessClaims.AnyAsync() &&
                      !await db.ContentAuditEvents.AnyAsync(),
                    "MERCHANT_HTTP_NO_BUSINESS_SIDE_EFFECTS");
            }

            Console.WriteLine("F2A_MERCHANT_BEARER_HTTP_CONTRACT=PASS");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }
}
