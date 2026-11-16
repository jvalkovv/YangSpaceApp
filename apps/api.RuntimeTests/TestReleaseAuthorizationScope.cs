using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

internal sealed class TestReleaseAuthorizationScope : IDisposable
{
    private readonly string? _previousCommit;
    private readonly ECDsa _signer;

    internal IConfiguration Configuration { get; }
    internal IHostEnvironment HostEnvironment { get; }

    internal TestReleaseAuthorizationScope(DateTimeOffset now, string sourceCommit)
    {
        if (sourceCommit.Length != 40 || sourceCommit.Any(c => !Uri.IsHexDigit(c)) ||
            sourceCommit.Any(char.IsUpper))
            throw new InvalidOperationException("TEST_RELEASE_SOURCE_SHA_REFUSED");

        _previousCommit = System.Environment.GetEnvironmentVariable("YANGSPACE_RELEASE_COMMIT_SHA");
        System.Environment.SetEnvironmentVariable("YANGSPACE_RELEASE_COMMIT_SHA", sourceCommit);

        _signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        const string keyId = "worker-contract";
        var payload = new YangSpace.Api.PublicAccountReleaseAuthorization.Payload(
            1,
            keyId,
            YangSpace.Api.PublicAccountReleaseAuthorization.RequiredScope,
            Environments.Production,
            YangSpace.Api.PublicAccountReleaseAuthorization.RequiredAudience,
            sourceCommit,
            "issue-44-worker-contract",
            now.AddMinutes(-1),
            now.AddMinutes(59));
        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var signature = _signer.SignData(
            payloadBytes,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicAccounts:Enabled"] = "true",
                ["PublicAccounts:Outbox:DispatchEnabled"] = "true",
                ["PublicAccounts:CanonicalOrigin"] = YangSpace.Api.PublicAccountReleaseAuthorization.RequiredAudience,
                ["PublicAccounts:ReleaseAuthorization:TrustedKeyId"] = keyId,
                ["PublicAccounts:ReleaseAuthorization:TrustedPublicKeyPem"] = _signer.ExportSubjectPublicKeyInfoPem(),
                ["PublicAccounts:ReleaseAuthorization:Payload"] = WebEncoders.Base64UrlEncode(payloadBytes),
                ["PublicAccounts:ReleaseAuthorization:Signature"] = WebEncoders.Base64UrlEncode(signature)
            })
            .Build();

        HostEnvironment = new TestProductionHostEnvironment();
    }

    public void Dispose()
    {
        System.Environment.SetEnvironmentVariable("YANGSPACE_RELEASE_COMMIT_SHA", _previousCommit);
        _signer.Dispose();
    }

    private sealed class TestProductionHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "YangSpace.Api.RuntimeTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
