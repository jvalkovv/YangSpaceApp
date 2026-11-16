using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using YangSpace.Api;

internal static class IdentityAbuseThrottleContract
{
    private sealed class ManualTimeProvider(DateTimeOffset initial) : TimeProvider
    {
        private DateTimeOffset _now = initial;
        public override DateTimeOffset GetUtcNow() => _now;
        internal void Advance(TimeSpan value) => _now = _now.Add(value);
    }

    private static void Check(bool condition, string marker)
    {
        if (!condition)
            throw new InvalidOperationException("F2B_H03_ABUSE_FAILED=" + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private static IConfiguration BuildConfiguration(byte[] key) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [IdentityAbuseThrottle.HmacKeyConfigurationPath] = Convert.ToBase64String(key)
            })
            .Build();

    internal static async Task RunAsync()
    {
        var key = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        var configuration = BuildConfiguration(key);
        IdentityAbuseThrottle.ValidateConfiguration(configuration);
        Check(true, "F2B_H03_ABUSE_HMAC_KEY_VALIDATED");

        var missingKeyRejected = false;
        try
        {
            IdentityAbuseThrottle.ValidateConfiguration(new ConfigurationBuilder().Build());
        }
        catch (InvalidOperationException exception)
            when (exception.Message == "PUBLIC_ACCOUNT_ABUSE_HMAC_KEY_REQUIRED")
        {
            missingKeyRejected = true;
        }
        Check(missingKeyRejected, "F2B_H03_ABUSE_MISSING_HMAC_KEY_FAILS_CLOSED");

        var shortKeyRejected = false;
        try
        {
            IdentityAbuseThrottle.ValidateConfiguration(BuildConfiguration([1, 2, 3, 4]));
        }
        catch (InvalidOperationException exception)
            when (exception.Message == "PUBLIC_ACCOUNT_ABUSE_HMAC_KEY_TOO_SHORT")
        {
            shortKeyRejected = true;
        }
        Check(shortKeyRejected, "F2B_H03_ABUSE_SHORT_HMAC_KEY_FAILS_CLOSED");

        var databaseName = "yangspace-h03-abuse-" + Guid.NewGuid().ToString("N");
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseName,
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = 30
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<DirectoryDb>()
            .UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly("YangSpace.Api"))
            .Options;
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        await using (var migrationDb = new DirectoryDb(options))
            await migrationDb.Database.MigrateAsync();
        Check(true, "F2B_H03_ABUSE_SQLITE_MIGRATIONS_APPLIED");

        var clock = new ManualTimeProvider(
            new DateTimeOffset(2026, 11, 16, 20, 0, 0, TimeSpan.Zero));
        var normalizer = new UpperInvariantLookupNormalizer();

        async Task<bool> AcquireAsync(IdentityAbuseOperation operation, string email)
        {
            await using var db = new DirectoryDb(options);
            var throttle = new IdentityAbuseThrottle(
                db, normalizer, configuration, clock, NullLogger<IdentityAbuseThrottle>.Instance);
            return await throttle.TryAcquireAsync(operation, email, CancellationToken.None);
        }

        var email = "H03-Throttle-" + Guid.NewGuid().ToString("N") + "@Example.Invalid";
        Check(await AcquireAsync(IdentityAbuseOperation.ResendConfirmation, email),
            "F2B_H03_ABUSE_RESEND_FIRST_ALLOWED");
        Check(await AcquireAsync(IdentityAbuseOperation.ResendConfirmation, email.ToLowerInvariant()),
            "F2B_H03_ABUSE_NORMALIZED_EMAIL_SHARED_BUCKET");
        Check(await AcquireAsync(IdentityAbuseOperation.ResendConfirmation, email.ToUpperInvariant()),
            "F2B_H03_ABUSE_THIRD_ALLOWED");
        Check(!await AcquireAsync(IdentityAbuseOperation.ResendConfirmation, email),
            "F2B_H03_ABUSE_RESEND_LIMIT_ENFORCED");

        Check(await AcquireAsync(IdentityAbuseOperation.ForgotPassword, email),
            "F2B_H03_ABUSE_OPERATION_BUCKET_ISOLATION");
        Check(await AcquireAsync(IdentityAbuseOperation.ResendConfirmation, "other-" + email),
            "F2B_H03_ABUSE_ACCOUNT_BUCKET_ISOLATION");

        var normalizedEmail = normalizer.NormalizeEmail(email)!;
        var expectedHash = IdentityAbuseThrottle.ComputeKeyHash(key, normalizedEmail);
        await using (var readDb = new DirectoryDb(options))
        {
            var rows = await readDb.IdentityAbuseCounters.AsNoTracking()
                .Where(row => row.Operation == IdentityAbuseOperation.ResendConfirmation.ToString() &&
                              row.KeyHash == expectedHash)
                .OrderByDescending(row => row.WindowStartUtc)
                .ToArrayAsync();
            Check(rows.Length == 1 && rows[0].Count == 4,
                "F2B_H03_ABUSE_COUNTER_PERSISTED");
            Check(rows[0].KeyHash.Length == 64 &&
                  !rows[0].KeyHash.Contains(email, StringComparison.OrdinalIgnoreCase),
                "F2B_H03_ABUSE_NO_RAW_EMAIL_AT_REST");
        }

        clock.Advance(TimeSpan.FromHours(2));
        Check(await AcquireAsync(IdentityAbuseOperation.ResendConfirmation, email),
            "F2B_H03_ABUSE_WINDOW_EXPIRY_RECOVERY");

        var concurrentEmail = "H03-Concurrent-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var concurrent = await Task.WhenAll(Enumerable.Range(0, 24).Select(async _ =>
            await AcquireAsync(IdentityAbuseOperation.Register, concurrentEmail)));
        Check(concurrent.Count(allowed => allowed) == 3 &&
              concurrent.Count(allowed => !allowed) == 21,
            "F2B_H03_ABUSE_ATOMIC_CONCURRENT_BUDGET");

        clock.Advance(TimeSpan.FromDays(3));
        Check(await AcquireAsync(IdentityAbuseOperation.ResendConfirmation, email),
            "F2B_H03_ABUSE_RETENTION_NEW_WINDOW_ALLOWED");
        await using (var retentionDb = new DirectoryDb(options))
        {
            var oldRows = await retentionDb.IdentityAbuseCounters.AsNoTracking()
                .Where(row => row.KeyHash == expectedHash)
                .ToArrayAsync();
            Check(oldRows.Length == 1,
                "F2B_H03_ABUSE_EXPIRED_COUNTERS_PURGED");
        }

        Console.WriteLine("F2B_H03_ABUSE_SQLITE_RUNTIME=PASS");
        Console.WriteLine("F2B_H03_ABUSE_CROSS_INSTANCE_ATOMIC_UPSERT=PASS");
        Console.WriteLine("F2B_H03_ABUSE_RETENTION=PASS");
        Console.WriteLine("CT164_DB=NOT_ACCESSED");
        Console.WriteLine("SMTP=NOT_CONNECTED");
    }
}
