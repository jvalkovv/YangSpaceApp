using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using YangSpace.Api;

var builder = WebApplication.CreateBuilder(args);
var preview = builder.Configuration.GetValue<bool>("Preview:Enabled");
if (preview && builder.Environment.IsProduction())
    throw new InvalidOperationException("Preview mode must not run in Production.");

var connection = builder.Configuration.GetConnectionString("Main");
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("ConnectionStrings:Main must be supplied from environment or protected config.");

builder.Services.AddDbContext<DirectoryDb>(options => options.UseSqlite(connection));
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

builder.Services.AddAuthorization(options => options.AddPolicy("PlatformAdmin", policy => policy.RequireRole("PlatformAdmin")));
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
// All new write endpoints are disabled outside explicit preview mode until the production
// identity/ownership model, migrations and audit logs have passed the M0 security gate.
if (preview)
{
    app.MapGroup("/api/auth").MapIdentityApi<IdentityUser>().RequireRateLimiting("auth");
    app.MapAdmin();
}
await PreviewBootstrap.Initialize(app);
app.Run();
