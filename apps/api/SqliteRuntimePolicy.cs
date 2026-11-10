using Microsoft.Data.Sqlite;

namespace YangSpace.Api;

/// <summary>
/// Applies SQLite connection-level integrity defaults before EF opens any
/// connection. Foreign-key enforcement is disabled unless explicitly enabled
/// in the connection string, so the application must opt in centrally.
/// </summary>
internal static class SqliteRuntimePolicy
{
    internal static string EnableForeignKeys(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("SQLite connection string is required.");

        try
        {
            var parsed = new SqliteConnectionStringBuilder(connectionString)
            {
                ForeignKeys = true
            };
            return parsed.ConnectionString;
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("SQLite connection string is invalid.", exception);
        }
    }
}
