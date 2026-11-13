using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace YangSpace.Api;

// Public accounts are NOT business owners. This endpoint group has no access
// to platform-admin routes, tenant membership grants or claim decisions.
// Enabled only by the explicit production + PostgreSQL + email readiness gate.
internal static class PublicAccountEndpoints
{
    private const string StaffRole = "PlatformAdmin";
    private const string ReviewerRole = "MerchantClaimReviewer";
    private const string ConfirmPath = "/api/public-account/confirm";

    internal static void ValidateConfiguration(IConfiguration configuration, IWebHostEnvironment environment,
        bool preview, bool merchantQa, bool reviewerQa, bool productionAdmin)
    {
        if (!configuration.GetValue<bool>("PublicAccounts:Enabled")) return;
        if (!environment.IsProduction() || preview || merchantQa || reviewerQa ||
            configuration.GetValue<bool>("Preview:AllowRegistration"))
            throw new InvalidOperationException("PUBLIC_ACCOUNT_SCOPE_REFUSED");
        if (!string.Equals(configuration["Database:Provider"], "PostgreSQL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PUBLIC_ACCOUNT_POSTGRESQL_REQUIRED");
        if (configuration["PublicAccounts:CanonicalOrigin"] != "https://yangspace.gndstore.eu")
            throw new InvalidOperationException("PUBLIC_ACCOUNT_CANONICAL_ORIGIN_REFUSED");
        var hosts = configuration["AllowedHosts"] ?? "";
        if (hosts.Split(';').Any(x => x.Trim() == "*") ||
            !hosts.Split(';').Any(x => string.Equals(x.Trim(), "yangspace.gndstore.eu", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("PUBLIC_ACCOUNT_HOST_POLICY_REFUSED");
        // Coexists with the production CMS, but never reuses its protected
        // /api/auth namespace or gateway secret.
        _ = productionAdmin;
        PublicAccountEmailSettings.RequireConfigured(configuration);
    }

    internal static RouteGroupBuilder MapPublicAccounts(this WebApplication app)
    {
        var group = app.MapGroup("/api/public-account").RequireRateLimiting("auth");

        group.MapPost("/register", async Task<IResult> (
            PublicRegisterRequest request,
            UserManager<IdentityUser> users,
            IEmailSender<IdentityUser> emails,
            IConfiguration configuration) =>
        {
            var email = NormalizeEmail(request.Email);
            if (email is null || string.IsNullOrEmpty(request.Password) || request.Password.Length > 256)
                return Results.BadRequest(new { error = "Invalid registration details." });

            // Duplicate and already-existing account outcomes must not
            // identify whether an email belongs to an existing user.
            if (await users.FindByEmailAsync(email) is not null)
                return Results.Accepted();

            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = false };
            var result = await users.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(x => x.Code.StartsWith("Password", StringComparison.Ordinal)))
                    return Results.BadRequest(new { error = "Password does not meet the account security policy." });
                return Results.Accepted();
            }

            await SendConfirmationAsync(user, users, emails, configuration);
            return Results.Accepted();
        });

        group.MapPost("/resend-confirmation", async Task<IResult> (
            PublicEmailRequest request, UserManager<IdentityUser> users,
            IEmailSender<IdentityUser> emails, IConfiguration configuration) =>
        {
            var email = NormalizeEmail(request.Email);
            if (email is null) return Results.Accepted();
            var user = await users.FindByEmailAsync(email);
            if (user is not null && !user.EmailConfirmed &&
                !await IsStaffAsync(users, user))
                await SendConfirmationAsync(user, users, emails, configuration);
            return Results.Accepted();
        });

        group.MapGet("/confirm", async Task<IResult> (
            string? userId, string? code, HttpContext context,
            UserManager<IdentityUser> users) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            var confirmed = false;
            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(code) &&
                userId.Length <= 256 && code.Length <= 4096)
            {
                var user = await users.FindByIdAsync(userId);
                if (user is not null && !await IsStaffAsync(users, user))
                {
                    try
                    {
                        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                        confirmed = (await users.ConfirmEmailAsync(user, token)).Succeeded;
                    }
                    catch (FormatException) { /* generic failure */ }
                }
            }
            return Results.Redirect(confirmed ? "/login?confirmed=1" : "/login?confirmed=0");
        });

        group.MapPost("/login", async Task<IResult> (
            PublicLoginRequest request, UserManager<IdentityUser> users,
            SignInManager<IdentityUser> signIn) =>
        {
            var email = NormalizeEmail(request.Email);
            if (email is null || string.IsNullOrEmpty(request.Password))
                return Results.Unauthorized();
            var user = await users.FindByEmailAsync(email);
            if (user is null || !user.EmailConfirmed || await IsStaffAsync(users, user))
                return Results.Unauthorized();

            // The public endpoint never emits auth cookies. A successful
            // Identity Bearer sign-in writes AccessTokenResponse to the body.
            signIn.AuthenticationScheme = IdentityConstants.BearerScheme;
            var result = await signIn.PasswordSignInAsync(
                user, request.Password, isPersistent: false, lockoutOnFailure: true);
            return result.Succeeded ? TypedResults.Empty : Results.Unauthorized();
        });

        group.MapPost("/forgot-password", async Task<IResult> (
            PublicEmailRequest request, UserManager<IdentityUser> users,
            IEmailSender<IdentityUser> emails) =>
        {
            var email = NormalizeEmail(request.Email);
            if (email is not null)
            {
                var user = await users.FindByEmailAsync(email);
                if (user is not null && user.EmailConfirmed && !await IsStaffAsync(users, user))
                {
                    var token = await users.GeneratePasswordResetTokenAsync(user);
                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    await emails.SendPasswordResetCodeAsync(user, email, code);
                }
            }
            return Results.Accepted();
        });

        group.MapPost("/reset-password", async Task<IResult> (
            PublicResetRequest request, UserManager<IdentityUser> users) =>
        {
            var email = NormalizeEmail(request.Email);
            if (email is null || string.IsNullOrWhiteSpace(request.ResetCode) ||
                request.ResetCode.Length > 4096 ||
                string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length > 256)
                return Results.BadRequest(new { error = "Invalid reset details." });

            var user = await users.FindByEmailAsync(email);
            if (user is null || !user.EmailConfirmed || await IsStaffAsync(users, user))
                return Results.BadRequest(new { error = "Invalid or expired reset details." });
            try
            {
                var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.ResetCode));
                var reset = await users.ResetPasswordAsync(user, code, request.NewPassword);
                return reset.Succeeded
                    ? Results.Ok()
                    : Results.BadRequest(new { error = "Invalid or expired reset details." });
            }
            catch (FormatException)
            {
                return Results.BadRequest(new { error = "Invalid or expired reset details." });
            }
        });

        group.MapGet("/session", async Task<IResult> (
            System.Security.Claims.ClaimsPrincipal principal, UserManager<IdentityUser> users) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null || !user.EmailConfirmed || await IsStaffAsync(users, user))
                return Results.Unauthorized();
            return Results.Ok(new { email = user.Email });
        }).RequireAuthorization(new AuthorizeAttribute
        {
            AuthenticationSchemes = IdentityConstants.BearerScheme
        });

        return group;
    }

    private static async Task SendConfirmationAsync(IdentityUser user,
        UserManager<IdentityUser> users, IEmailSender<IdentityUser> emails,
        IConfiguration configuration)
    {
        var origin = configuration["PublicAccounts:CanonicalOrigin"]!;
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = origin + ConfirmPath +
            "?userId=" + Uri.EscapeDataString(user.Id) +
            "&code=" + Uri.EscapeDataString(code);
        await emails.SendConfirmationLinkAsync(user, user.Email!, link);
    }

    private static string? NormalizeEmail(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var email = raw.Trim();
        return email.Length is > 0 and <= 254 && new EmailAddressAttribute().IsValid(email)
            ? email : null;
    }

    private static async Task<bool> IsStaffAsync(UserManager<IdentityUser> manager, IdentityUser user) =>
        await manager.IsInRoleAsync(user, StaffRole) || await manager.IsInRoleAsync(user, ReviewerRole);
}

internal sealed record PublicRegisterRequest(string Email, string Password);
internal sealed record PublicLoginRequest(string Email, string Password);
internal sealed record PublicEmailRequest(string Email);
internal sealed record PublicResetRequest(string Email, string ResetCode, string NewPassword);
