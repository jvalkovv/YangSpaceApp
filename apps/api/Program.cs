using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using YangSpace.Api;

// Explicit offline enrollment cannot be triggered by an HTTP request, nor
// accidentally interpreted as an ASP.NET command-line configuration flag.
var offlineProvision = args.Length == 1 &&
    string.Equals(args[0], "--yangspace-provision-admin", StringComparison.Ordinal);
if (args.Any(x => string.Equals(x, "--yangspace-provision-admin", StringComparison.Ordinal)) && !offlineProvision)
    throw new InvalidOperationException("Offline admin provisioning cannot be combined with other arguments.");
var builder = WebApplication.CreateBuilder(offlineProvision ? Array.Empty<string>() : args);
var preview = builder.Configuration.GetValue<bool>("Preview:Enabled");
var allowPreviewRegistration = builder.Configuration.GetValue<bool>("Preview:AllowRegistration");
var productionAdmin = builder.Configuration.GetValue<bool>("Admin:Enabled");
var merchantRead = builder.Configuration.GetValue<bool>("MerchantRead:Enabled");
var claimReviewQa = builder.Configuration.GetValue<bool>("ClaimReview:QaEnabled");
var claimReviewPostgreSqlQa = builder.Configuration.GetValue<bool>("ClaimReviewPostgreSqlQa:Enabled");
var publicAccounts = builder.Configuration.GetValue<bool>("PublicAccounts:Enabled");
// F2a-2's first HTTP slice is a QA-only test surface, not a public merchant release.
// It is impossible to enable accidentally in Development, Preview, or Production.
if (merchantRead && (preview || productionAdmin || allowPreviewRegistration ||
                     !builder.Environment.IsEnvironment("YangSpaceQa")))
    throw new InvalidOperationException(
        "Merchant read QA endpoints require explicit YangSpaceQa environment with Preview/Admin/Registration disabled.");
// Reviewer identity QA is a separate, read-only probe and cannot coexist with other QA surfaces.
if (claimReviewQa && (preview || productionAdmin || allowPreviewRegistration || merchantRead ||
                      !builder.Environment.IsEnvironment("YangSpaceQa")))
    throw new InvalidOperationException(
        "Claim review QA endpoints require explicit YangSpaceQa environment with Preview/Admin/Registration/MerchantRead disabled.");
// PostgreSQL reviewer HTTP is a distinct opt-in QA surface. It never relaxes
// the existing SQLite-only reviewer QA contract and cannot coexist with any
// other preview/admin/merchant/SQLite-reviewer QA surface.
if (claimReviewPostgreSqlQa && (preview || productionAdmin || allowPreviewRegistration ||
                                merchantRead || claimReviewQa ||
                                !builder.Environment.IsEnvironment("YangSpaceQa")))
    throw new InvalidOperationException(
        "Claim review PostgreSQL QA endpoints require explicit YangSpaceQa with Preview/Admin/Registration/MerchantRead/SQLite reviewer QA disabled.");
// Never enable the CMS implicitly from a DNS record or a general Production deployment.
if (productionAdmin && (preview || !builder.Environment.IsProduction()))
    throw new InvalidOperationException("Production CMS requires Production environment and Preview:Enabled=false.");
if (productionAdmin && allowPreviewRegistration)
    throw new InvalidOperationException("Production CMS never permits self-registration.");
// Fail closed before any HTTP server startup. Nginx supplies the value only
// after operator access checks; the secret is NEVER sent to the browser.
var gatewaySecret = productionAdmin && !offlineProvision
    ? ProductionAdminAccess.RequireSecret() : null;
if (productionAdmin)
{
    var hosts = builder.Configuration["AllowedHosts"];
    if (string.IsNullOrWhiteSpace(hosts) || hosts.Split(';').Any(x => x.Trim() == "*") ||
        !hosts.Split(';').Any(x => string.Equals(x.Trim(), "yangspace.gndstore.eu", StringComparison.OrdinalIgnoreCase)))
        throw new InvalidOperationException("Production CMS requires an explicit canonical AllowedHosts configuration.");
}
if (allowPreviewRegistration && !preview)
    throw new InvalidOperationException("Preview self-registration cannot be enabled outside preview mode.");
if (preview && builder.Environment.IsProduction())
    throw new InvalidOperationException("Preview mode must not run in Production.");

var connection = builder.Configuration.GetConnectionString("Main");
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("ConnectionStrings:Main must be supplied from environment or protected config.");
// The PostgreSQL reviewer HTTP QA surface is only valid inside the disposable
// container network namespace created by its exact-SHA operator runner.
if (claimReviewPostgreSqlQa)
{
    if (Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_DISPOSABLE_ONLY") !=
            "CONFIRMED_CONTAINER_NETNS_ONLY" ||
        Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_TESTSERVER_ONLY") !=
            "CONFIRMED_WEBAPPLICATIONFACTORY_ONLY" ||
        !Guid.TryParseExact(
            Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_TOKEN"), "N", out _))
        throw new InvalidOperationException(
            "Claim review PostgreSQL QA endpoints require explicit disposable-container and WebApplicationFactory TestServer scopes.");

    NpgsqlConnectionStringBuilder scoped;
    try
    {
        scoped = new NpgsqlConnectionStringBuilder(connection);
    }
    catch (ArgumentException)
    {
        throw new InvalidOperationException(
            "Claim review PostgreSQL QA endpoints require a valid disposable PostgreSQL connection.");
    }

    var scopedConnection = Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_CONNECTION");
    if (string.IsNullOrWhiteSpace(scopedConnection) ||
        !string.Equals(connection, scopedConnection, StringComparison.Ordinal))
        throw new InvalidOperationException(
            "Claim review PostgreSQL QA endpoints require an exact scoped connection match.");

    if (scoped.Host != "127.0.0.1" ||
        scoped.Port != 5432 ||
        scoped.Database != "yangspace_f2a3_disposable" ||
        scoped.Username != "postgres" ||
        scoped.SslMode != SslMode.Disable ||
        !string.IsNullOrEmpty(scoped.Password) ||
        scoped.Pooling ||
        scoped.IncludeErrorDetail)
        throw new InvalidOperationException(
            "Claim review PostgreSQL QA endpoints refuse non-disposable PostgreSQL connection settings.");
}

// Provider choice is explicit in appsettings/configuration. Preview always
// stays on SQLite; PostgreSQL is a separate target with its own migrations assembly.
var databaseProvider = builder.Configuration["Database:Provider"];
if (string.IsNullOrWhiteSpace(databaseProvider))
    throw new InvalidOperationException("Database:Provider must explicitly select SQLite or PostgreSQL.");
PublicAccountEndpoints.ValidateConfiguration(builder.Configuration, builder.Environment,
    preview, merchantRead, claimReviewQa, productionAdmin);
// QA-only: an isolated SQLite model created by the test harness; no production
// PostgreSQL role or database is implicitly approved through this feature.
if (merchantRead && !string.Equals(databaseProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Merchant read QA endpoints require isolated SQLite persistence.");
if (claimReviewQa && !string.Equals(databaseProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Claim review QA endpoints require isolated SQLite persistence.");
if (claimReviewPostgreSqlQa && !string.Equals(databaseProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Claim review PostgreSQL QA endpoints require PostgreSQL persistence.");

// Do not expose even the QA probe unless the database itself proves possession
// of this runner's one-time sentinel. This check is PostgreSQL-QA-only.
if (claimReviewPostgreSqlQa)
{
    await using var qaConnection = new NpgsqlConnection(connection);
    await qaConnection.OpenAsync();
    await using var sentinelCommand = qaConnection.CreateCommand();
    sentinelCommand.CommandText =
        "SELECT scope_token FROM yangspace_f2a3_qa_guard LIMIT 1";
    var actualToken = (string?)await sentinelCommand.ExecuteScalarAsync();
    var expectedToken = Environment.GetEnvironmentVariable("YANGSPACE_F2A3_PG_TOKEN");
    if (actualToken is null ||
        !string.Equals(actualToken, expectedToken, StringComparison.Ordinal))
        throw new InvalidOperationException(
            "Claim review PostgreSQL QA endpoints require a matching disposable PostgreSQL sentinel.");
}

if (preview && !string.Equals(databaseProvider, "SQLite", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Preview mode is permanently pinned to its isolated SQLite provider.");

switch (databaseProvider.Trim().ToLowerInvariant())
{
    case "sqlite":
    {
        var runtimeConnection = SqliteRuntimePolicy.EnableForeignKeys(connection);
        if (builder.Environment.IsProduction())
            ProductionSqlitePolicy.RequirePreparedDatabase(runtimeConnection, builder.Environment.ContentRootPath);

        builder.Services.AddDbContext<DirectoryDb>(options =>
            options.UseSqlite(runtimeConnection,
                sqlite => sqlite.MigrationsAssembly("YangSpace.Api")));
        break;
    }
    case "postgresql":
    {
        if (builder.Environment.IsProduction())
            ProductionPostgreSqlPolicy.RequirePreparedConnection(connection);

        builder.Services.AddDbContext<DirectoryDb>(options =>
            options.UseNpgsql(connection,
                postgres => postgres.MigrationsAssembly("YangSpace.PostgreSql.Migrations")));
        break;
    }
    default:
        throw new InvalidOperationException(
            "Database:Provider must be exactly SQLite or PostgreSQL; implicit provider fallback is forbidden.");
}
builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    // Public accounts require verified email before login.
    options.SignIn.RequireConfirmedEmail = publicAccounts;
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<DirectoryDb>();
if (publicAccounts)
    builder.Services.AddTransient<IEmailSender<IdentityUser>, SmtpPublicIdentityEmailSender>();
builder.Services.AddScoped<IAuthorizationHandler, LiveIdentityRoleAuthorizationHandler>();
builder.Services.AddScoped<ClaimReviewConflictPolicy>();

// Only the Identity bearer scheme may authorize CMS writes. Identity's
// default composite scheme also accepts cookies; that is not our CMS contract.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PlatformAdmin", policy =>
        policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme)
              .RequireAuthenticatedUser()
              .RequireRole("PlatformAdmin"));

    // Never use Identity's composite bearer+cookie scheme for private tenant data.
    // The per-request membership JOIN below is the source of tenant authority.
    options.AddPolicy("MerchantBearerRead", policy =>
        policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme)
              .RequireAuthenticatedUser());

    // The persistent Identity role is checked per request; stale bearer role claims cannot survive revocation.
    options.AddPolicy("MerchantClaimReviewer", policy =>
        policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme)
              .RequireAuthenticatedUser()
              .AddRequirements(new LiveIdentityRoleRequirement(ClaimReviewRoles.Reviewer)));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // A shared limiter allows one client to exhaust everyone's auth budget.
    // Trust only the transport peer here; never parse untrusted forwarding headers.
    options.AddPolicy("auth", AuthRateLimitPolicy.CreatePartition);
});
builder.Services.AddProblemDetails();

var app = builder.Build();
if (offlineProvision)
{
    if (!productionAdmin || preview || !app.Environment.IsProduction())
        throw new InvalidOperationException("Offline admin provisioning requires the explicit Production CMS configuration.");
    await ProductionAdminProvisioner.RunAsync(app);
    return;
}
app.UseExceptionHandler();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/health/ready", async (DirectoryDb db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.MapDirectory();
// The production schema is created and migrated only by a reviewed OFFLINE
// operator action. No EnsureCreated(), Migrate() or demo seed on service startup.
if (app.Environment.IsProduction())
{
    if (productionAdmin)
    {
        await ProductionSchemaGate.ValidateAsync(app);
    }
    else
    {
        using var scope = app.Services.CreateScope();
        await ProductionSchemaGate.ValidateSchemaAsync(
            scope.ServiceProvider.GetRequiredService<DirectoryDb>());
    }
}

if (preview || productionAdmin || merchantRead || claimReviewQa || claimReviewPostgreSqlQa)
{
    var auth = app.MapGroup("/api/auth").RequireRateLimiting("auth");
    auth.AddEndpointFilter(async (context, next) =>
    {
        var request = context.HttpContext.Request;
        var path = request.Path.Value;
        if (productionAdmin)
        {
            if (!ProductionAdminAccess.Accept(context.HttpContext, gatewaySecret!))
                return Results.NotFound();

            // MapIdentityApi includes enrollment, recovery and user-management
            // endpoints. Do NOT expose them in Production. We only need a
            // bearer token login + refresh for an offline-provisioned admin.
            var supported = HttpMethods.IsPost(request.Method) &&
                (string.Equals(path, "/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(path, "/api/auth/refresh", StringComparison.OrdinalIgnoreCase));
            if (!supported) return Results.NotFound();

            // MapIdentityApi can also issue auth cookies when asked. The
            // CMS is a memory-only bearer client; forbid cookies in Production.
            if (request.Query.ContainsKey("useCookies") ||
                request.Query.ContainsKey("useSessionCookies"))
                return Results.BadRequest(new { error = "Cookie authentication is disabled for the CMS." });
        }
        else if (merchantRead || claimReviewQa || claimReviewPostgreSqlQa)
        {
            // For the isolated QA harness, enable only credential login and
            // bearer refresh. All enrollment, recovery and account endpoints
            // remain unmapped to callers via fail-closed endpoint filtering.
            var supported = HttpMethods.IsPost(request.Method) &&
                (string.Equals(path, "/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(path, "/api/auth/refresh", StringComparison.OrdinalIgnoreCase));
            if (!supported) return Results.NotFound();
            if (request.Query.ContainsKey("useCookies") ||
                request.Query.ContainsKey("useSessionCookies"))
                return Results.BadRequest(new { error = "Cookie authentication is disabled." });
        }
        else if (HttpMethods.IsPost(request.Method) &&
                 string.Equals(path, "/api/auth/register", StringComparison.OrdinalIgnoreCase) &&
                 !allowPreviewRegistration)
        {
            return Results.NotFound();
        }
        return await next(context);
    });
    auth.MapIdentityApi<IdentityUser>();

    // Preserve existing preview/admin routing and its secure gateway boundary.
    if (preview || productionAdmin)
        app.MapAdmin(gatewaySecret);
}
if (merchantRead)
    app.MapMerchantRead();
if (publicAccounts)
    app.MapPublicAccounts();
if (claimReviewQa || claimReviewPostgreSqlQa)
    app.MapClaimReviewQa();
await PreviewBootstrap.Initialize(app);
app.Run();

// Exposes an entry point to the isolated WebApplicationFactory HTTP harness.
public partial class Program { }
