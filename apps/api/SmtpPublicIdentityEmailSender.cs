using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace YangSpace.Api;

// Transport is configured from deployment secrets, not source-controlled files.
// Disabled by default. Send errors propagate (never claim that an email was sent).
internal sealed class PublicAccountEmailSettings
{
    internal required string Host { get; init; }
    internal required int Port { get; init; }
    internal required string Username { get; init; }
    internal required string Password { get; init; }
    internal required string From { get; init; }

    internal static PublicAccountEmailSettings RequireConfigured(IConfiguration configuration)
    {
        var host = configuration["PublicAccounts:Email:SmtpHost"];
        var username = configuration["PublicAccounts:Email:Username"];
        var password = configuration["PublicAccounts:Email:Password"];
        var from = configuration["PublicAccounts:Email:From"];
        var portRaw = configuration["PublicAccounts:Email:Port"];

        if (string.IsNullOrWhiteSpace(host) || host.Length > 253 ||
            host.Any(char.IsWhiteSpace) || host.Contains('/') ||
            !int.TryParse(portRaw, out var port) || port != 587 ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("PUBLIC_ACCOUNT_SMTP_CONFIG_REFUSED");

        try { _ = new MailAddress(from); }
        catch (FormatException) { throw new InvalidOperationException("PUBLIC_ACCOUNT_SMTP_FROM_REFUSED"); }

        return new PublicAccountEmailSettings
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            From = from
        };
    }
}

internal sealed class SmtpPublicIdentityEmailSender(
    IConfiguration configuration) : IEmailSender<IdentityUser>
{
    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink)
    {
        var uri = new Uri(WebUtility.HtmlDecode(confirmationLink), UriKind.Absolute);
        if (uri.Scheme != Uri.UriSchemeHttps ||
            uri.Host != "yangspace.gndstore.eu" ||
            uri.AbsolutePath != "/api/public-account/confirm")
            throw new InvalidOperationException("PUBLIC_ACCOUNT_EMAIL_LINK_SCOPE_REFUSED");
        return SendAsync(email, "Потвърди имейла си в YangSpace",
            "За да потвърдиш имейла си, отвори този адрес:\n\n" + uri.AbsoluteUri +
            "\n\nАко не си създавал акаунт, игнорирай съобщението.");
    }

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink) =>
        SendAsync(email, "Възстановяване на достъп до YangSpace",
            "Отвори следния адрес, ако си поискал възстановяване:\n\n" +
            WebUtility.HtmlDecode(resetLink));

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode) =>
        SendAsync(email, "Код за възстановяване на YangSpace",
            "Отвори https://yangspace.gndstore.eu/reset и въведи този код:\n\n" +
            WebUtility.HtmlDecode(resetCode) +
            "\n\nАко не си поискал това, игнорирай съобщението.");

    private async Task SendAsync(string to, string subject, string body)
    {
        var settings = PublicAccountEmailSettings.RequireConfigured(configuration);
        using var message = new MailMessage(
            new MailAddress(settings.From, "YangSpace"), new MailAddress(to))
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = true, // Require STARTTLS; never downgrade to cleartext.
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(settings.Username, settings.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15000
        };
        await client.SendMailAsync(message).WaitAsync(TimeSpan.FromSeconds(20));
    }
}
