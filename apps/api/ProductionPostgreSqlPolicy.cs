using Npgsql;

namespace YangSpace.Api;

/// <summary>
/// Production-only connection policy for the future PostgreSQL target.
/// No network connection is opened here; credentials remain in protected config.
/// </summary>
internal static class ProductionPostgreSqlPolicy
{
    internal static void RequirePreparedConnection(string connectionString)
    {
        NpgsqlConnectionStringBuilder parsed;
        try
        {
            parsed = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Production PostgreSQL connection string is invalid.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Host) ||
            string.IsNullOrWhiteSpace(parsed.Database) ||
            string.IsNullOrWhiteSpace(parsed.Username))
        {
            throw new InvalidOperationException(
                "Production PostgreSQL requires explicit Host, Database and Username.");
        }

        if (parsed.SslMode != SslMode.VerifyFull)
            throw new InvalidOperationException(
                "Production PostgreSQL requires SslMode=VerifyFull; encryption without server identity validation is insufficient.");

        if (!string.IsNullOrWhiteSpace(parsed.RootCertificate) &&
            (!Path.IsPathFullyQualified(parsed.RootCertificate) ||
             !File.Exists(parsed.RootCertificate)))
        {
            throw new InvalidOperationException(
                "Production PostgreSQL Root Certificate must be an existing absolute path when configured.");
        }

        if (parsed.IncludeErrorDetail)
            throw new InvalidOperationException(
                "Production PostgreSQL must not expose server error details that may contain sensitive data.");
    }
}
