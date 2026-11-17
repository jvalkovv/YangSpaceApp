using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using YangSpace.Api;

internal static class F2bH02ReleaseAuthorizationContract
{
    private const string SourceSha = "b36f155c37a8f707849100fa12f3b0e82637119f";

    private static void Check(bool ok, string marker)
    {
        if (!ok) throw new InvalidOperationException("F2B_H02_AUTHORIZATION_FAILED=" + marker);
        Console.WriteLine(marker + "=PASS");
    }

    internal static void Run()
    {
        var now = DateTimeOffset.UtcNow;
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = signer.ExportSubjectPublicKeyInfoPem();
        var payload = new PublicAccountReleaseAuthorization.Payload(
            1, "yangspace-release-2026", PublicAccountReleaseAuthorization.RequiredScope,
            Environments.Production, PublicAccountReleaseAuthorization.RequiredAudience,
            SourceSha, "issue-44", now.AddMinutes(-1), now.AddMinutes(59));

        var configuration = BuildConfiguration(signer, publicKey, payload);
        Check(Valid(configuration, now), "F2B_H02_AUTH_VALID_SIGNATURE");
        Check(!Valid(BuildConfiguration(signer, publicKey, payload, includeAuthorization: false), now),
            "F2B_H02_AUTH_MISSING_REFUSED");
        Check(!Valid(BuildConfiguration(signer, publicKey, payload,
            signatureOverride: WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64))), now),
            "F2B_H02_AUTH_BAD_SIGNATURE_REFUSED");
        Check(!Valid(BuildConfiguration(signer, publicKey,
            payload with { ExpiresAtUtc = now.AddSeconds(-1) }), now),
            "F2B_H02_AUTH_EXPIRED_REFUSED");
        Check(!Valid(BuildConfiguration(signer, publicKey,
            payload with { IssuedAtUtc = now.AddMinutes(10) }), now),
            "F2B_H02_AUTH_FUTURE_ISSUED_REFUSED");
        Check(Valid(BuildConfiguration(signer, publicKey, payload), now),
            "F2B_H02_AUTH_ONE_HOUR_BOUNDARY_ACCEPTED");
        Check(!Valid(BuildConfiguration(signer, publicKey,
            payload with
            {
                ExpiresAtUtc = payload.IssuedAtUtc
                    .Add(PublicAccountReleaseAuthorization.MaxAuthorizationLifetime)
                    .AddTicks(1)
            }), now),
            "F2B_H02_AUTH_OVER_ONE_HOUR_REFUSED");
        Check(!Valid(BuildConfiguration(signer, publicKey,
            payload with { Audience = "https://evil.example" }), now),
            "F2B_H02_AUTH_WRONG_AUDIENCE_REFUSED");
        Check(!Valid(BuildConfiguration(signer, publicKey,
            payload with { Scope = "all-features" }), now),
            "F2B_H02_AUTH_WRONG_SCOPE_REFUSED");
        Check(!Valid(BuildConfiguration(signer, publicKey,
            payload with { Environment = Environments.Development }), now),
            "F2B_H02_AUTH_WRONG_ENVIRONMENT_REFUSED");
        Check(!Valid(BuildConfiguration(signer, publicKey,
            payload with { SourceCommit = new string('a', 40) }), now),
            "F2B_H02_AUTH_WRONG_COMMIT_REFUSED");
        Check(!Valid(BuildConfiguration(signer, publicKey,
            payload with { SourceCommit = null! }), now),
            "F2B_H02_AUTH_NULL_SOURCE_COMMIT_REFUSED");
        Check(!Valid(BuildConfiguration(signer, publicKey,
            payload with { ApprovalId = null! }), now),
            "F2B_H02_AUTH_NULL_APPROVAL_ID_REFUSED");
        Check(!Valid(BuildConfiguration(signer, publicKey,
            payload with { KeyId = "untrusted-key" }), now),
            "F2B_H02_AUTH_WRONG_KEY_ID_REFUSED");
        Check(!Valid(BuildConfiguration(signer, "not-a-public-key", payload), now),
            "F2B_H02_AUTH_BAD_PUBLIC_KEY_REFUSED");

        var wrongExpectedSha = PublicAccountReleaseAuthorization.Validate(
            configuration, Environments.Production, new string('c', 40), now);
        Check(!wrongExpectedSha.Authorized && wrongExpectedSha.FailureCode == "source_commit",
            "F2B_H02_AUTH_DEPLOYMENT_SHA_BINDING");
        var development = PublicAccountReleaseAuthorization.Validate(
            configuration, Environments.Development, SourceSha, now);
        Check(!development.Authorized && development.FailureCode == "environment",
            "F2B_H02_AUTH_PRODUCTION_ONLY");

        var defaultOff = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicAccounts:Enabled"] = "false",
            ["PublicAccounts:Outbox:DispatchEnabled"] = "false"
        }).Build();
        Check(!IdentityEmailOutboxDispatchPolicy.AreSwitchesEnabled(defaultOff),
            "F2B_H02_AUTH_DEFAULT_SWITCHES_OFF");
        Check(IdentityEmailOutboxDispatchPolicy.DispatcherAvailable,
            "F2B_H02_AUTH_IMPLEMENTATION_AVAILABLE_AFTER_O3");

        Console.WriteLine("F2B_H02_SIGNED_RELEASE_AUTHORIZATION_CONTRACT=PASS");
        Console.WriteLine("F2B_H02_PRIVATE_KEY_NOT_REQUIRED_BY_VERIFIER=PASS");
        Console.WriteLine("F2B_H02_PRODUCTION_ACTIVATION=NOT_AUTHORIZED");
    }

    private static bool Valid(IConfiguration configuration, DateTimeOffset now) =>
        PublicAccountReleaseAuthorization.Validate(
            configuration, Environments.Production, SourceSha, now).Authorized;

    private static IConfiguration BuildConfiguration(
        ECDsa signer,
        string publicKey,
        PublicAccountReleaseAuthorization.Payload payload,
        bool includeAuthorization = true,
        string? signatureOverride = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["PublicAccounts:CanonicalOrigin"] = PublicAccountReleaseAuthorization.RequiredAudience,
            ["PublicAccounts:Enabled"] = "true",
            ["PublicAccounts:Outbox:DispatchEnabled"] = "true",
            ["PublicAccounts:ReleaseAuthorization:TrustedKeyId"] = "yangspace-release-2026",
            ["PublicAccounts:ReleaseAuthorization:TrustedPublicKeyPem"] = publicKey
        };
        if (includeAuthorization)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            values["PublicAccounts:ReleaseAuthorization:Payload"] = WebEncoders.Base64UrlEncode(bytes);
            values["PublicAccounts:ReleaseAuthorization:Signature"] = signatureOverride ??
                WebEncoders.Base64UrlEncode(signer.SignData(
                    bytes, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
        }
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
