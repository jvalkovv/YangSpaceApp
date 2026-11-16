using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace YangSpace.Api;

/// <summary>
/// Verifies an operator-signed, short-lived authorization for the exact
/// public Identity/email-dispatch release scope. The private signing key is
/// external to this process and must never be stored with application config.
/// </summary>
internal static class PublicAccountReleaseAuthorization
{
    internal const string RequiredScope = "public-identity-email-v1";
    internal const string RequiredAudience = "https://yangspace.gndstore.eu";
    private static readonly Regex ShaPattern = new("^[a-f0-9]{40}$", RegexOptions.CultureInvariant);
    private static readonly Regex KeyIdPattern = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.CultureInvariant);
    private static readonly Regex ApprovalPattern = new("^[A-Za-z0-9][A-Za-z0-9._:/#-]{2,127}$", RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    internal sealed record Payload(
        int Version,
        string KeyId,
        string Scope,
        string Environment,
        string Audience,
        string SourceCommit,
        string ApprovalId,
        DateTimeOffset IssuedAtUtc,
        DateTimeOffset ExpiresAtUtc);

    internal readonly record struct ValidationResult(bool Authorized, string FailureCode);

    internal static bool IsAuthorized(
        IConfiguration configuration,
        string environmentName,
        string? expectedSourceCommit,
        DateTimeOffset now) =>
        Validate(configuration, environmentName, expectedSourceCommit, now).Authorized;

    internal static ValidationResult Validate(
        IConfiguration configuration,
        string environmentName,
        string? expectedSourceCommit,
        DateTimeOffset now)
    {
        if (!string.Equals(environmentName, Environments.Production, StringComparison.Ordinal))
            return new(false, "environment");

        var payloadText = configuration["PublicAccounts:ReleaseAuthorization:Payload"];
        var signatureText = configuration["PublicAccounts:ReleaseAuthorization:Signature"];
        var publicKeyPem = configuration["PublicAccounts:ReleaseAuthorization:TrustedPublicKeyPem"];
        var configuredKeyId = configuration["PublicAccounts:ReleaseAuthorization:TrustedKeyId"];
        if (string.IsNullOrWhiteSpace(payloadText) ||
            string.IsNullOrWhiteSpace(signatureText) ||
            string.IsNullOrWhiteSpace(publicKeyPem) ||
            string.IsNullOrWhiteSpace(configuredKeyId))
            return new(false, "missing");

        if (payloadText.Length > 8192 || signatureText.Length > 1024 ||
            publicKeyPem.Length > 4096 || !KeyIdPattern.IsMatch(configuredKeyId))
            return new(false, "size_or_key_id");

        byte[] payloadBytes;
        byte[] signatureBytes;
        Payload? payload;
        try
        {
            payloadBytes = WebEncoders.Base64UrlDecode(payloadText);
            signatureBytes = WebEncoders.Base64UrlDecode(signatureText);
            payload = JsonSerializer.Deserialize<Payload>(payloadBytes, JsonOptions);
        }
        catch (Exception exception) when (
            exception is FormatException or JsonException or ArgumentException)
        {
            return new(false, "encoding_or_json");
        }

        if (payload is null || payload.Version != 1 ||
            !string.Equals(payload.KeyId, configuredKeyId, StringComparison.Ordinal) ||
            !string.Equals(payload.Scope, RequiredScope, StringComparison.Ordinal) ||
            !string.Equals(payload.Environment, Environments.Production, StringComparison.Ordinal) ||
            !string.Equals(payload.Audience, RequiredAudience, StringComparison.Ordinal) ||
            !string.Equals(configuration["PublicAccounts:CanonicalOrigin"], RequiredAudience, StringComparison.Ordinal))
            return new(false, "scope_or_audience");

        if (!ShaPattern.IsMatch(expectedSourceCommit ?? string.Empty) ||
            !string.Equals(payload.SourceCommit, expectedSourceCommit, StringComparison.Ordinal))
            return new(false, "source_commit");

        if (!ApprovalPattern.IsMatch(payload.ApprovalId) ||
            payload.IssuedAtUtc.Offset != TimeSpan.Zero ||
            payload.ExpiresAtUtc.Offset != TimeSpan.Zero ||
            payload.ExpiresAtUtc <= payload.IssuedAtUtc ||
            payload.IssuedAtUtc > now.AddMinutes(5) ||
            payload.IssuedAtUtc < now.AddHours(-24) ||
            payload.ExpiresAtUtc <= now ||
            payload.ExpiresAtUtc > payload.IssuedAtUtc.AddHours(24))
            return new(false, "time_window");

        if (!publicKeyPem.StartsWith("-----BEGIN PUBLIC KEY-----", StringComparison.Ordinal) ||
            publicKeyPem.Contains("PRIVATE KEY", StringComparison.Ordinal))
            return new(false, "public_key_format");

        try
        {
            using var verifier = ECDsa.Create();
            verifier.ImportFromPem(publicKeyPem);
            if (verifier.KeySize != 256 ||
                !verifier.VerifyData(payloadBytes, signatureBytes,
                    HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
                return new(false, "signature");
        }
        catch (CryptographicException)
        {
            return new(false, "public_key_or_signature");
        }
        catch (ArgumentException)
        {
            return new(false, "public_key_or_signature");
        }

        return new(true, string.Empty);
    }
}
