using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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

// SQLite foreign-key enforcement is connection-scoped. Normalize the
// connection string once so every EF connection enables PRAGMA foreign_keys
// before it can execute application SQL.
var runtimeConnection = SqliteRuntimePolicy.EnableForeignKeys(connection);

// Production reads and CMS writes BOTH require the same migration-managed,
// pre-existing external DB. No accidental SQLite creation for public GETs.
if (builder.Environment.IsProduction())
    ProductionSqlitePolicy.RequirePreparedDatabase(runtimeConnection, builder.Environment.ContentRootPath);
builder.Services.AddDbContext<DirectoryDb>(options => options.UseSqlite(runtimeConnection));
builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<DirectoryDb>();

// Only the Identity bearer scheme may authorize CMS writes. Identity's
// default composite scheme also accepts cookies; that is not our CMS contract.
builder.Services.AddAuthorization(options => options.AddPolicy("PlatformAdmin", policy =>
    policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme)
          .RequireAuthenticatedUser()
          .RequireRole("PlatformAdmin")));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 12;
        limiter.QueueLimit = 0;
    });
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

if (preview || productionAdmin)
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
        else if (HttpMethods.IsPost(request.Method) &&
                 string.Equals(path, "/api/auth/register", StringComparison.OrdinalIgnoreCase) &&
                 !allowPreviewRegistration)
        {
            return Results.NotFound();
        }
        return await next(context);
    });
    auth.MapIdentityApi<IdentityUser>();

    // No change to the preview QA behavior. Production CMS requires BOTH
    // PlatformAdmin identity and an internal-gateway request gate.
    app.MapAdmin(gatewaySecret);
}
await PreviewBootstrap.Initialize(app);
app.Run();
