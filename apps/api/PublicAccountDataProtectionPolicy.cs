using Microsoft.AspNetCore.DataProtection;

namespace YangSpace.Api;

/// <summary>
/// Production PublicAccounts key-ring contract. Never create a replacement ring
/// during application startup: an apparently healthy app with a new ring would
/// invalidate previously issued confirmation/reset tokens after restart.
/// </summary>
internal static class PublicAccountDataProtectionPolicy
{
    internal const string ApplicationName = "YangSpace.PublicAccounts.v1";
    internal const string DirectorySetting = "PublicAccounts:DataProtection:KeyRingDirectory";
    internal const string SentinelFileName = "keyring.sentinel";
    internal const string SentinelPurpose = "YangSpace.PublicAccounts.KeyRingContinuity.v1";
    internal const string SentinelPlaintext = "YangSpace.PublicAccounts.KeyRingReady.v1";

    internal static DirectoryInfo RequireExistingKeyRing(IConfiguration configuration)
    {
        var configured = configuration[DirectorySetting];
        if (string.IsNullOrWhiteSpace(configured) ||
            !Path.IsPathFullyQualified(configured))
            throw new InvalidOperationException("PUBLIC_ACCOUNT_KEYRING_ABSOLUTE_PATH_REQUIRED");

        var directory = new DirectoryInfo(Path.GetFullPath(configured));
        if (!directory.Exists || (directory.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("PUBLIC_ACCOUNT_KEYRING_DIRECTORY_UNAVAILABLE");

        // Linux public-account service and operator backup should not rely on
        // group/world traversal of an unencrypted DataProtection key directory.
        if (OperatingSystem.IsLinux())
        {
            var mode = File.GetUnixFileMode(directory.FullName);
            const UnixFileMode forbidden =
                UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;
            if ((mode & forbidden) != 0)
                throw new InvalidOperationException("PUBLIC_ACCOUNT_KEYRING_PERMISSIONS_REFUSED");
        }

        // Provisioning is an explicit offline operation. If an existing ring
        // goes missing, fail closed instead of silently creating a new identity.
        if (!directory.EnumerateFiles("key-*.xml", SearchOption.TopDirectoryOnly).Any())
            throw new InvalidOperationException("PUBLIC_ACCOUNT_KEYRING_KEYS_MISSING");

        VerifyContinuity(directory);
        return directory;
    }

    internal static bool IsReady(DirectoryInfo keyRing)
    {
        try
        {
            _ = RequireExistingKeyRing(
                new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?> { [DirectorySetting] = keyRing.FullName }).Build());
            return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (System.Security.Cryptography.CryptographicException) { return false; }
    }

    private static void VerifyContinuity(DirectoryInfo directory)
    {
        var sentinelPath = Path.Combine(directory.FullName, SentinelFileName);
        var sentinel = new FileInfo(sentinelPath);
        if (!sentinel.Exists || (sentinel.Attributes & FileAttributes.ReparsePoint) != 0 ||
            sentinel.Length == 0 || sentinel.Length > 8192)
            throw new InvalidOperationException("PUBLIC_ACCOUNT_KEYRING_SENTINEL_MISSING");

        try
        {
            var provider = DataProtectionProvider.Create(
                directory, builder => builder.SetApplicationName(ApplicationName));
            var plaintext = provider.CreateProtector(SentinelPurpose)
                .Unprotect(File.ReadAllText(sentinel.FullName));
            if (!string.Equals(plaintext, SentinelPlaintext, StringComparison.Ordinal))
                throw new InvalidOperationException("PUBLIC_ACCOUNT_KEYRING_SENTINEL_MISMATCH");
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            throw new InvalidOperationException("PUBLIC_ACCOUNT_KEYRING_CONTINUITY_FAILED");
        }
        catch (System.Xml.XmlException)
        {
            throw new InvalidOperationException("PUBLIC_ACCOUNT_KEYRING_XML_INVALID");
        }
    }

    internal static void Configure(IServiceCollection services, DirectoryInfo directory)
    {
        services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToFileSystem(directory);
    }
}
