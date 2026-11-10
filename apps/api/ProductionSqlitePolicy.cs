using Microsoft.Data.Sqlite;

namespace YangSpace.Api;

/// <summary>
/// Prevent accidental file creation or use of the disposable preview DB.
/// The active database lives outside the immutable release/content directory.
/// </summary>
internal static class ProductionSqlitePolicy
{
    internal static void RequirePreparedDatabase(string connectionString, string contentRoot)
    {
        SqliteConnectionStringBuilder parsed;
        try { parsed = new SqliteConnectionStringBuilder(connectionString); }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Production SQLite connection string is invalid.");
        }

        if (parsed.Mode != SqliteOpenMode.ReadWrite)
            throw new InvalidOperationException(
                "Production SQLite must use Mode=ReadWrite, not ReadWriteCreate/Memory.");

        if (!parsed.ForeignKeys)
            throw new InvalidOperationException(
                "Production SQLite must use ForeignKeys=True; referential integrity is mandatory.");

        var source = parsed.DataSource;
        if (string.IsNullOrWhiteSpace(source) || !Path.IsPathFullyQualified(source))
            throw new InvalidOperationException(
                "Production SQLite Data Source must be an absolute filesystem path.");

        var databasePath = Path.GetFullPath(source);
        var releaseRoot = Path.GetFullPath(contentRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (databasePath.StartsWith(releaseRoot, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Production SQLite database must not live inside the immutable application release.");
        if (!File.Exists(databasePath))
            throw new InvalidOperationException(
                "Production SQLite file is missing; create schema offline with reviewed migrations.");
    }
}
