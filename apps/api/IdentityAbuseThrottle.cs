using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace YangSpace.Api;

internal enum IdentityAbuseOperation
{
    Register = 1,
    ResendConfirmation = 2,
    ForgotPassword = 3,
    ResetPassword = 4
}

internal interface IIdentityAbuseThrottle
{
    Task<bool> TryAcquireAsync(
        IdentityAbuseOperation operation,
        string email,
        CancellationToken cancellationToken);
}

/// <summary>
/// Cross-instance, account-keyed fixed-window throttling for public Identity
/// operations. A keyed digest is persisted instead of the account identifier.
/// Counter increments use one atomic provider-native UPSERT statement.
/// </summary>
internal sealed class IdentityAbuseThrottle : IIdentityAbuseThrottle
{
    internal const string HmacKeyConfigurationPath = "PublicAccounts:AbuseProtection:HmacKey";
    private static readonly TimeSpan Retention = TimeSpan.FromDays(2);
    private static readonly long PurgeIntervalSeconds = (long)TimeSpan.FromHours(6).TotalSeconds;
    private static long _nextPurgeUnixSeconds;

    private readonly DirectoryDb _db;
    private readonly byte[] _hmacKey;
    private readonly ILookupNormalizer _normalizer;
    private readonly TimeProvider _clock;
    private readonly ILogger<IdentityAbuseThrottle> _logger;

    public IdentityAbuseThrottle(
        DirectoryDb db,
        ILookupNormalizer normalizer,
        IConfiguration configuration,
        TimeProvider clock,
        ILogger<IdentityAbuseThrottle> logger)
    {
        _db = db;
        _normalizer = normalizer;
        _hmacKey = ReadHmacKey(configuration);
        _clock = clock;
        _logger = logger;
    }

    internal static void ValidateConfiguration(IConfiguration configuration) =>
        _ = ReadHmacKey(configuration);

    internal static string ComputeKeyHash(byte[] key, string normalizedEmail) =>
        Convert.ToHexString(HMACSHA256.HashData(
            key, Encoding.UTF8.GetBytes(normalizedEmail))).ToLowerInvariant();

    internal static (int PermitLimit, TimeSpan Window) GetPolicy(IdentityAbuseOperation operation) =>
        operation switch
        {
            IdentityAbuseOperation.Register => (3, TimeSpan.FromDays(1)),
            IdentityAbuseOperation.ResendConfirmation => (3, TimeSpan.FromHours(1)),
            IdentityAbuseOperation.ForgotPassword => (3, TimeSpan.FromHours(1)),
            IdentityAbuseOperation.ResetPassword => (5, TimeSpan.FromMinutes(15)),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

    public async Task<bool> TryAcquireAsync(
        IdentityAbuseOperation operation,
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = _normalizer.NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            throw new ArgumentException("A normalized account identifier is required.", nameof(email));

        var policy = GetPolicy(operation);
        var now = _clock.GetUtcNow();
        var windowSeconds = checked((long)policy.Window.TotalSeconds);
        var unixSeconds = now.ToUnixTimeSeconds();
        var windowStartSeconds = unixSeconds - unixSeconds % windowSeconds;
        var windowStartUtc = DateTimeOffset.FromUnixTimeSeconds(windowStartSeconds).UtcDateTime;
        var keyHash = ComputeKeyHash(_hmacKey, normalizedEmail);

        var count = await IncrementAsync(
            operation.ToString(), keyHash, windowStartUtc, now.UtcDateTime, cancellationToken);

        await TryPurgeExpiredAsync(now, cancellationToken);
        return count <= policy.PermitLimit;
    }

    private static byte[] ReadHmacKey(IConfiguration configuration)
    {
        var encoded = configuration[HmacKeyConfigurationPath];
        if (string.IsNullOrWhiteSpace(encoded))
            throw new InvalidOperationException("PUBLIC_ACCOUNT_ABUSE_HMAC_KEY_REQUIRED");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(encoded);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("PUBLIC_ACCOUNT_ABUSE_HMAC_KEY_INVALID");
        }

        if (key.Length < 32)
            throw new InvalidOperationException("PUBLIC_ACCOUNT_ABUSE_HMAC_KEY_TOO_SHORT");
        return key;
    }

    private async Task<int> IncrementAsync(
        string operation,
        string keyHash,
        DateTime windowStartUtc,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var provider = _db.Database.ProviderName;
        var sql = provider switch
        {
            "Microsoft.EntityFrameworkCore.Sqlite" => """
                INSERT INTO "IdentityAbuseCounters"
                    ("Operation", "KeyHash", "WindowStartUtc", "Count", "UpdatedAtUtc")
                VALUES (@operation, @keyHash, @windowStartUtc, 1, @updatedAtUtc)
                ON CONFLICT ("Operation", "KeyHash", "WindowStartUtc") DO UPDATE SET
                    "Count" = CASE WHEN "Count" < 2147483647 THEN "Count" + 1 ELSE 2147483647 END,
                    "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc"
                RETURNING "Count";
                """,
            "Npgsql.EntityFrameworkCore.PostgreSQL" => """
                INSERT INTO "IdentityAbuseCounters"
                    ("Operation", "KeyHash", "WindowStartUtc", "Count", "UpdatedAtUtc")
                VALUES (@operation, @keyHash, @windowStartUtc, 1, @updatedAtUtc)
                ON CONFLICT ("Operation", "KeyHash", "WindowStartUtc") DO UPDATE SET
                    "Count" = CASE WHEN "IdentityAbuseCounters"."Count" < 2147483647
                                   THEN "IdentityAbuseCounters"."Count" + 1 ELSE 2147483647 END,
                    "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc"
                RETURNING "Count";
                """,
            _ => throw new InvalidOperationException("PUBLIC_ACCOUNT_ABUSE_DATABASE_PROVIDER_UNSUPPORTED")
        };

        var connection = _db.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@operation", operation);
            AddParameter(command, "@keyHash", keyHash);
            AddParameter(command, "@windowStartUtc", windowStartUtc);
            AddParameter(command, "@updatedAtUtc", updatedAtUtc);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result is DBNull)
                throw new InvalidOperationException("PUBLIC_ACCOUNT_ABUSE_COUNTER_UPSERT_FAILED");
            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
        }
    }

    private async Task TryPurgeExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var nowSeconds = now.ToUnixTimeSeconds();
        var scheduled = Volatile.Read(ref _nextPurgeUnixSeconds);
        if (nowSeconds < scheduled ||
            Interlocked.CompareExchange(
                ref _nextPurgeUnixSeconds,
                nowSeconds + PurgeIntervalSeconds,
                scheduled) != scheduled)
            return;

        var connection = _db.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        try
        {
            if (closeConnection)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                DELETE FROM "IdentityAbuseCounters"
                WHERE "WindowStartUtc" < @cutoffUtc;
                """;
            AddParameter(command, "@cutoffUtc", now.UtcDateTime.Subtract(Retention));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbException or InvalidOperationException)
        {
            // Cleanup is opportunistic; the counter UPSERT has already committed.
            // Do not log account identifiers, digests, SQL parameters or exception text.
            _logger.LogWarning("IDENTITY_ABUSE_COUNTER_CLEANUP_FAILED");
            Interlocked.Exchange(ref _nextPurgeUnixSeconds, nowSeconds + 300);
        }
        finally
        {
            if (closeConnection && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
