using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using YangSpace.Api;

internal static class F2bH02ProductionIdentityHttpContract
{
    private const string DatabaseName = "yangspace_f2b_o3_worker_disposable";
    private const string IdentityRole = "ys_h02_identity";
    private static readonly byte[] TestAbuseHmacKeyBytes =
        Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
    private static readonly string TestAbuseHmacKey =
        Convert.ToBase64String(TestAbuseHmacKeyBytes);

    private static void Check(bool ok, string marker)
    {
        if (!ok) throw new InvalidOperationException("F2B_H02_HTTP_FAILED=" + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException("F2B_H02_ENV_MISSING=" + name);

    private sealed class SyntheticKeyRingScope : IDisposable
    {
        internal string DirectoryPath { get; }

        internal SyntheticKeyRingScope()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(), "yangspace-h05-dp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
            if (OperatingSystem.IsLinux())
                File.SetUnixFileMode(DirectoryPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            var provider = DataProtectionProvider.Create(
                new DirectoryInfo(DirectoryPath),
                builder => builder.SetApplicationName(PublicAccountDataProtectionPolicy.ApplicationName));
            var sentinel = provider.CreateProtector(PublicAccountDataProtectionPolicy.SentinelPurpose)
                .Protect(PublicAccountDataProtectionPolicy.SentinelPlaintext);
            var sentinelPath = Path.Combine(DirectoryPath, PublicAccountDataProtectionPolicy.SentinelFileName);
            File.WriteAllText(sentinelPath, sentinel);
            if (OperatingSystem.IsLinux())
                File.SetUnixFileMode(sentinelPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            Check(PublicAccountDataProtectionPolicy.IsReady(new DirectoryInfo(DirectoryPath)),
                "F2B_H05_DATAPROTECTION_OFFLINE_PROVISIONED_KEYRING");
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, recursive: true);
        }
    }

    private sealed class CommitEnvironmentScope : IDisposable
    {
        private readonly string? _previous =
            Environment.GetEnvironmentVariable("YANGSPACE_RELEASE_COMMIT_SHA");

        internal CommitEnvironmentScope(string commit)
        {
            Environment.SetEnvironmentVariable("YANGSPACE_RELEASE_COMMIT_SHA", commit);
        }

        public void Dispose() =>
            Environment.SetEnvironmentVariable("YANGSPACE_RELEASE_COMMIT_SHA", _previous);
    }

    private sealed class StartupEnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string?> _previous = new(StringComparer.Ordinal);

        internal StartupEnvironmentScope(
            IReadOnlyDictionary<string, string?> configuration, string environment)
        {
            var values = configuration
                .Where(pair => pair.Value is not null)
                .ToDictionary(
                    pair => pair.Key.Replace(":", "__", StringComparison.Ordinal),
                    pair => pair.Value!,
                    StringComparer.Ordinal);
            values["ASPNETCORE_ENVIRONMENT"] = environment;
            values["DOTNET_ENVIRONMENT"] = environment;

            foreach (var (name, value) in values)
            {
                _previous[name] = Environment.GetEnvironmentVariable(
                    name, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable(
                    name, value, EnvironmentVariableTarget.Process);
            }
        }

        public void Dispose()
        {
            foreach (var (name, value) in _previous)
                Environment.SetEnvironmentVariable(
                    name, value, EnvironmentVariableTarget.Process);
        }
    }

    private sealed record Delivery(IdentityEmailPurpose Purpose, string Email, string Payload);

    private sealed class FakeTransport : IIdentityEmailOutboxTransport
    {
        private readonly ConcurrentQueue<Delivery> _deliveries = new();
        private int _failAlways;
        private long _failedSendAttempts;

        internal IReadOnlyCollection<Delivery> Deliveries => _deliveries.ToArray();
        internal long FailedSendAttempts => Interlocked.Read(ref _failedSendAttempts);
        internal bool FailAlways
        {
            get => Volatile.Read(ref _failAlways) == 1;
            set => Volatile.Write(ref _failAlways, value ? 1 : 0);
        }

        public Task SendConfirmationAsync(
            IdentityUser user, string email, string confirmationLink, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailAlways)
            {
                Interlocked.Increment(ref _failedSendAttempts);
                throw new IOException("synthetic transport failure");
            }
            _deliveries.Enqueue(new Delivery(IdentityEmailPurpose.ConfirmEmail, email, confirmationLink));
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(
            IdentityUser user, string email, string resetCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FailAlways)
            {
                Interlocked.Increment(ref _failedSendAttempts);
                throw new IOException("synthetic transport failure");
            }
            _deliveries.Enqueue(new Delivery(IdentityEmailPurpose.ResetPassword, email, resetCode));
            return Task.CompletedTask;
        }
    }

    // H02 exercises many auth endpoints in one TestServer process. TestServer
    // normally gives every request the same loopback peer, which exhausts the
    // real 12/minute production limiter before the lifecycle contract finishes.
    // This filter is registered only by this test factory and simulates distinct
    // transport peers; it does not trust forwarding headers or change production.
    private sealed class TestPeerAddressStartupFilter : IStartupFilter
    {
        internal const string RequestHeader = "X-YangSpace-Test-Peer";
        internal const string AppliedHeader = "X-YangSpace-Test-Peer-Applied";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (!context.Request.Headers.TryGetValue(RequestHeader, out var raw) ||
                        !IPAddress.TryParse(raw.ToString(), out var address))
                        throw new InvalidOperationException("F2B_H02_TEST_PEER_INVALID");

                    context.Connection.RemoteIpAddress = address;
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers[AppliedHeader] = address.ToString();
                        return Task.CompletedTask;
                    });
                    await nextMiddleware();
                });
                next(app);
            };
    }

    private static void SetTestPeer(HttpClient client, string address)
    {
        client.DefaultRequestHeaders.Remove(TestPeerAddressStartupFilter.RequestHeader);
        client.DefaultRequestHeaders.Add(TestPeerAddressStartupFilter.RequestHeader, address);
    }

    private sealed class Factory : WebApplicationFactory<ApiAssemblyMarker>
    {
        private readonly string _connection;
        private readonly IReadOnlyDictionary<string, string?> _configuration;
        private readonly NpgsqlDataSource _dataSource;
        internal NpgsqlDataSource DataSource => _dataSource;
        internal FakeTransport Fake { get; }
        private readonly bool _adminEnabled;

        internal Factory(string connection, IReadOnlyDictionary<string, string?> configuration,
            FakeTransport fake, bool adminEnabled = false)
        {
            _connection = connection;
            _configuration = configuration;
            _dataSource = NpgsqlDataSource.Create(connection);
            Fake = fake;
            _adminEnabled = adminEnabled;
        }

        protected override IEnumerable<System.Reflection.Assembly> GetTestAssemblies()
        {
            yield return typeof(F2bH02ProductionIdentityHttpContract).Assembly;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var root = AppContext.BaseDirectory;
            if (Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) != "/qa")
                throw new InvalidOperationException("F2B_H02_UNEXPECTED_CONTENT_ROOT");
            if (!File.Exists(Path.Combine(root, "appsettings.json")))
                throw new InvalidOperationException("F2B_H02_APPSETTINGS_NOT_PUBLISHED");

            builder.UseContentRoot(root);
            builder.UseEnvironment(Environments.Production);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var values = _configuration.ToDictionary(
                    pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
                values["ConnectionStrings:Main"] = _connection;
                values["Database:Provider"] = "PostgreSQL";
                values["AllowedHosts"] = "yangspace.gndstore.eu";
                values["Preview:Enabled"] = "false";
                values["Preview:AllowRegistration"] = "false";
                values["Admin:Enabled"] = _adminEnabled ? "true" : "false";
                values["MerchantRead:Enabled"] = "false";
                values["ClaimReview:QaEnabled"] = "false";
                values["ClaimReviewPostgreSqlQa:Enabled"] = "false";
                values["PublicAccounts:Enabled"] = "true";
                values["PublicAccounts:AbuseProtection:HmacKey"] = TestAbuseHmacKey;
                values["PublicAccounts:Outbox:DispatchEnabled"] = "true";
                values["PublicAccounts:CanonicalOrigin"] = PublicAccountReleaseAuthorization.RequiredAudience;
                values["PublicAccounts:Email:SmtpHost"] = "127.0.0.1";
                values["PublicAccounts:Email:Port"] = "587";
                values["PublicAccounts:Email:Username"] = "synthetic";
                values["PublicAccounts:Email:Password"] = "synthetic-never-used";
                values["PublicAccounts:Email:From"] = "no-reply@yangspace.gndstore.eu";
                configuration.AddInMemoryCollection(values);
            });
            builder.ConfigureTestServices(services =>
            {
                // Use an explicitly owned EF data source in this TestServer so
                // the recovery contract can clear the exact pool used by EF.
                // Production Program and the connection string remain unchanged.
                services.RemoveAll<DirectoryDb>();
                services.RemoveAll<DbContextOptions<DirectoryDb>>();
                services.RemoveAll<IDbContextOptionsConfiguration<DirectoryDb>>();
                services.AddDbContext<DirectoryDb>(options =>
                    options.UseNpgsql(_dataSource,
                        postgres => postgres.MigrationsAssembly("YangSpace.PostgreSql.Migrations")));

                services.AddSingleton<IStartupFilter>(new TestPeerAddressStartupFilter());
                if (_adminEnabled)
                {
                    // Keep the real HTTP production application and auth policies;
                    // suppress only background dispatch to make replay-state assertions deterministic.
                    foreach (var descriptor in services.Where(descriptor =>
                        descriptor.ServiceType == typeof(IHostedService) &&
                        descriptor.ImplementationType == typeof(IdentityEmailOutboxWorker)).ToArray())
                        services.Remove(descriptor);
                }
                services.RemoveAll<IIdentityEmailOutboxTransport>();
                services.AddSingleton<IIdentityEmailOutboxTransport>(Fake);
                services.Configure<IdentityEmailOutboxOptions>(options =>
                {
                    options.PollIntervalSeconds = 1;
                    options.LeaseSeconds = 30;
                    options.BatchSize = 10;
                    options.MaxAttempts = 3;
                    options.SentRetentionDays = 30;
                    options.SuppressedRetentionDays = 30;
                    options.DeadLetterRetentionDays = 90;
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                if (disposing)
                    _dataSource.Dispose();
            }
        }
    }

    internal static async Task RunAsync()
    {
        var migrator = Required("YANGSPACE_F2B_O3_PG_MIGRATOR_CONNECTION");
        var identityConnection = Required("YANGSPACE_F2B_H02_PG_IDENTITY_CONNECTION");
        var scopeToken = Required("YANGSPACE_F2B_O3_PG_SCOPE_TOKEN");
        var commit = Required("YANGSPACE_RELEASE_COMMIT_SHA");
        if (commit.Length != 40 || commit.Any(c => !Uri.IsHexDigit(c)) || commit != commit.ToLowerInvariant())
            throw new InvalidOperationException("F2B_H02_SOURCE_SHA_REFUSED");

        ValidateConnection(migrator, "ys_o3_migrator");
        Check(await VerifyQaScopeAsync(migrator, scopeToken),
            "F2B_H02_QA_SCOPE_BOUND");
        await GrantIdentityRoleAsync(migrator);
        ValidateConnection(identityConnection, IdentityRole);
        await VerifyAbuseStoragePrivilegeGateAsync(migrator, identityConnection);
        Check(await VerifyIdentityRoleAsync(identityConnection),
            "F2B_H02_IDENTITY_RUNTIME_LEAST_PRIVILEGE");

        using var environment = new CommitEnvironmentScope(commit);
        using var keyRing = new SyntheticKeyRingScope();
        var missingConfigurationRefused = false;
        try
        {
            _ = PublicAccountDataProtectionPolicy.RequireExistingKeyRing(
                new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?>()).Build());
        }
        catch (InvalidOperationException exception)
            when (exception.Message == "PUBLIC_ACCOUNT_KEYRING_ABSOLUTE_PATH_REQUIRED")
        {
            missingConfigurationRefused = true;
        }
        Check(missingConfigurationRefused,
            "F2B_H05_DATAPROTECTION_UNCONFIGURED_FAILS_CLOSED");

        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var authorization = CreateAuthorization(signer, commit);
        var startupConfiguration = authorization.ToDictionary(
            pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        startupConfiguration["ConnectionStrings:Main"] = identityConnection;
        startupConfiguration["Database:Provider"] = "PostgreSQL";
        startupConfiguration["AllowedHosts"] = "yangspace.gndstore.eu";
        startupConfiguration["Preview:Enabled"] = "false";
        startupConfiguration["Preview:AllowRegistration"] = "false";
        startupConfiguration["Admin:Enabled"] = "false";
        startupConfiguration["MerchantRead:Enabled"] = "false";
        startupConfiguration["ClaimReview:QaEnabled"] = "false";
        startupConfiguration["ClaimReviewPostgreSqlQa:Enabled"] = "false";
        startupConfiguration["PublicAccounts:AbuseProtection:HmacKey"] = TestAbuseHmacKey;
        startupConfiguration[PublicAccountDataProtectionPolicy.DirectorySetting] = keyRing.DirectoryPath;
        startupConfiguration["PublicAccounts:Email:SmtpHost"] = "127.0.0.1";
        startupConfiguration["PublicAccounts:Email:Port"] = "587";
        startupConfiguration["PublicAccounts:Email:Username"] = "synthetic";
        startupConfiguration["PublicAccounts:Email:Password"] = "synthetic-never-used";
        startupConfiguration["PublicAccounts:Email:From"] = "no-reply@yangspace.gndstore.eu";

        // WebApplicationFactory.ConfigureWebHost runs after minimal-host Program
        // has already created its builder and evaluated fail-closed startup guards.
        // Seed the same disposable values into process environment before CreateClient.
        using var startupEnvironment = new StartupEnvironmentScope(
            startupConfiguration, Environments.Production);
        Check(Environment.GetEnvironmentVariable("ConnectionStrings__Main") == identityConnection &&
              Environment.GetEnvironmentVariable("Database__Provider") == "PostgreSQL" &&
              Environment.GetEnvironmentVariable("PublicAccounts__Enabled") == "true" &&
              Environment.GetEnvironmentVariable("PublicAccounts__Outbox__DispatchEnabled") == "true" &&
              Environment.GetEnvironmentVariable("PublicAccounts__AbuseProtection__HmacKey") == TestAbuseHmacKey &&
              Environment.GetEnvironmentVariable("PublicAccounts__ReleaseAuthorization__Payload") ==
                  startupConfiguration["PublicAccounts:ReleaseAuthorization:Payload"],
            "F2B_H02_EARLY_STARTUP_CONFIGURATION");

        var fake = new FakeTransport();
        using var factory = new Factory(identityConnection, startupConfiguration, fake);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(PublicAccountReleaseAuthorization.RequiredAudience),
            AllowAutoRedirect = false
        });
        SetTestPeer(client, "192.0.2.10");

        Check(factory.Services.GetRequiredService<IWebHostEnvironment>().IsProduction(),
            "F2B_H02_REAL_PRODUCTION_ENVIRONMENT");
        Check(ReferenceEquals(factory.Services.GetRequiredService<IIdentityEmailOutboxTransport>(), fake),
            "F2B_H02_FAKE_TRANSPORT_ONLY");
        Check(PublicAccountReleaseAuthorization.IsAuthorized(
            new ConfigurationBuilder().AddInMemoryCollection(authorization).Build(),
            Environments.Production, commit, DateTimeOffset.UtcNow),
            "F2B_H02_RELEASE_AUTHORIZATION_VALID");

        using var live = await client.GetAsync("/health/live");
        using var ready = await client.GetAsync("/health/ready");
        Check(live.StatusCode == HttpStatusCode.OK && ready.StatusCode == HttpStatusCode.OK,
            "F2B_H02_PRODUCTION_LIVE_READY");
        Check(live.Headers.TryGetValues(TestPeerAddressStartupFilter.AppliedHeader, out var appliedPeer) &&
              appliedPeer.SingleOrDefault() == "192.0.2.10",
            "F2B_H02_TEST_PEER_PARTITIONING");

        var firstProtector = DataProtectionProvider.Create(
            new DirectoryInfo(keyRing.DirectoryPath),
            builder => builder.SetApplicationName(PublicAccountDataProtectionPolicy.ApplicationName))
            .CreateProtector("F2B_H05_KeyContinuity");
        var protectedPayload = firstProtector.Protect("synthetic-continuity-proof");
        var secondProtector = DataProtectionProvider.Create(
            new DirectoryInfo(keyRing.DirectoryPath),
            builder => builder.SetApplicationName(PublicAccountDataProtectionPolicy.ApplicationName))
            .CreateProtector("F2B_H05_KeyContinuity");
        Check(secondProtector.Unprotect(protectedPayload) == "synthetic-continuity-proof",
            "F2B_H05_DATAPROTECTION_CROSS_INSTANCE_UNPROTECT");

        using var isolatedKeyRing = new SyntheticKeyRingScope();
        var isolatedProtector = DataProtectionProvider.Create(
            new DirectoryInfo(isolatedKeyRing.DirectoryPath),
            builder => builder.SetApplicationName(PublicAccountDataProtectionPolicy.ApplicationName))
            .CreateProtector("F2B_H05_KeyContinuity");
        var crossRingTokenRejected = false;
        try { _ = isolatedProtector.Unprotect(protectedPayload); }
        catch (CryptographicException) { crossRingTokenRejected = true; }
        Check(crossRingTokenRejected,
            "F2B_H05_DATAPROTECTION_ISOLATED_RING_REJECTS_TOKEN");

        var sentinelPath = Path.Combine(
            keyRing.DirectoryPath, PublicAccountDataProtectionPolicy.SentinelFileName);
        var sentinelRemovedPath = sentinelPath + ".temporarily-unavailable";
        File.Move(sentinelPath, sentinelRemovedPath);
        try
        {
            using var unavailable = await client.GetAsync("/health/ready");
            using var alive = await client.GetAsync("/health/live");
            Check(unavailable.StatusCode == HttpStatusCode.ServiceUnavailable &&
                  alive.StatusCode == HttpStatusCode.OK,
                "F2B_H05_DATAPROTECTION_MISSING_SENTINEL_READINESS_503_LIVE_200");
        }
        finally
        {
            File.Move(sentinelRemovedPath, sentinelPath);
        }

        using (var restored = await client.GetAsync("/health/ready"))
        {
            Check(restored.StatusCode == HttpStatusCode.OK,
                "F2B_H05_DATAPROTECTION_SENTINEL_RESTORED_READY_200");
        }

        var originalSentinel = File.ReadAllText(sentinelPath);
        File.WriteAllText(sentinelPath, "corrupted-synthetic-proof");
        try
        {
            using var corrupted = await client.GetAsync("/health/ready");
            Check(corrupted.StatusCode == HttpStatusCode.ServiceUnavailable,
                "F2B_H05_DATAPROTECTION_CORRUPT_SENTINEL_READINESS_503");
        }
        finally
        {
            File.WriteAllText(sentinelPath, originalSentinel);
        }
        using (var restoredAfterCorruption = await client.GetAsync("/health/ready"))
        {
            Check(restoredAfterCorruption.StatusCode == HttpStatusCode.OK,
                "F2B_H05_DATAPROTECTION_CORRUPT_SENTINEL_RESTORED_READY_200");
        }

        await SetIdentityRoleConnectAsync(migrator, identityConnection, factory.DataSource, enabled: false);
        try
        {
            using var unavailable = await client.GetAsync("/health/ready");
            using var alive = await client.GetAsync("/health/live");
            Check(unavailable.StatusCode == HttpStatusCode.ServiceUnavailable &&
                  alive.StatusCode == HttpStatusCode.OK,
                "F2B_H05E_DB_UNAVAILABLE_READINESS_503_LIVE_200");
        }
        finally
        {
            await SetIdentityRoleConnectAsync(migrator, identityConnection, factory.DataSource, enabled: true);
        }
        using (var restoredDatabase = await client.GetAsync("/health/ready"))
        {
            Check(restoredDatabase.StatusCode == HttpStatusCode.OK,
                "F2B_H05E_DB_RESTORED_READINESS_200");
        }

        await RenameIdentityOutboxTableAsync(migrator, renamed: true);
        try
        {
            using var schemaUnavailable = await client.GetAsync("/health/ready");
            using var alive = await client.GetAsync("/health/live");
            Check(schemaUnavailable.StatusCode == HttpStatusCode.ServiceUnavailable &&
                  alive.StatusCode == HttpStatusCode.OK,
                "F2B_H05E_SCHEMA_UNAVAILABLE_READINESS_503_LIVE_200");
        }
        finally
        {
            await RenameIdentityOutboxTableAsync(migrator, renamed: false);
        }
        using (var restoredSchema = await client.GetAsync("/health/ready"))
        {
            Check(restoredSchema.StatusCode == HttpStatusCode.OK,
                "F2B_H05E_SCHEMA_RESTORED_READINESS_200");
        }

        var email = "h02-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        const string password = "StrongPassword!2026";

        SetTestPeer(client, "192.0.2.13");
        var outboxCountBeforeInvalidRegistrations = await ReadTotalOutboxCountAsync(migrator);

        var malformedEmail = "not-an-email";
        using var malformedRegistration = await client.PostAsJsonAsync(
            "/api/public-account/register", new { email = malformedEmail, password });
        var malformedRegistrationBody = await malformedRegistration.Content.ReadAsStringAsync();
        Check(malformedRegistration.StatusCode == HttpStatusCode.BadRequest &&
              !await UserExistsAsync(identityConnection, malformedEmail),
            "F2B_H04_REGISTER_INVALID_EMAIL_NO_ACCOUNT");

        var emptyEmail = string.Empty;
        using var emptyRegistration = await client.PostAsJsonAsync(
            "/api/public-account/register", new { email = emptyEmail, password });
        var emptyRegistrationBody = await emptyRegistration.Content.ReadAsStringAsync();
        Check(emptyRegistration.StatusCode == HttpStatusCode.BadRequest &&
              emptyRegistrationBody == malformedRegistrationBody &&
              !await UserExistsAsync(identityConnection, emptyEmail),
            "F2B_H04_REGISTER_EMPTY_EMAIL_NO_ACCOUNT");

        var oversizedEmail = string.Concat(
            new string('a', 64), "@",
            new string('b', 63), ".",
            new string('c', 63), ".",
            new string('d', 63), ".",
            new string('e', 60));
        Check(oversizedEmail.Length > 254 &&
              new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(oversizedEmail),
            "F2B_H04_OVERSIZED_EMAIL_FIXTURE_VALIDATED");
        using var oversizedRegistration = await client.PostAsJsonAsync(
            "/api/public-account/register", new { email = oversizedEmail, password });
        var oversizedRegistrationBody = await oversizedRegistration.Content.ReadAsStringAsync();
        Check(oversizedRegistration.StatusCode == HttpStatusCode.BadRequest &&
              oversizedRegistrationBody == malformedRegistrationBody &&
              !await UserExistsAsync(identityConnection, oversizedEmail),
            "F2B_H04_REGISTER_OVERSIZED_EMAIL_NO_ACCOUNT");

        var emptyPasswordEmail = "h04-empty-password-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        using var emptyPasswordRegistration = await client.PostAsJsonAsync(
            "/api/public-account/register", new { email = emptyPasswordEmail, password = string.Empty });
        Check(emptyPasswordRegistration.StatusCode == HttpStatusCode.BadRequest &&
              !await UserExistsAsync(identityConnection, emptyPasswordEmail),
            "F2B_H04_REGISTER_EMPTY_PASSWORD_NO_ACCOUNT");

        var weakPasswordEmail = "h04-weak-password-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        using var weakPasswordRegistration = await client.PostAsJsonAsync(
            "/api/public-account/register", new { email = weakPasswordEmail, password = "weak" });
        Check(weakPasswordRegistration.StatusCode == HttpStatusCode.BadRequest &&
              !await UserExistsAsync(identityConnection, weakPasswordEmail),
            "F2B_H04_REGISTER_WEAK_PASSWORD_NO_ACCOUNT");

        var outboxCountAfterInvalidRegistrations = await ReadTotalOutboxCountAsync(migrator);
        Check(outboxCountAfterInvalidRegistrations == outboxCountBeforeInvalidRegistrations,
            "F2B_H04_INVALID_REGISTRATIONS_NO_OUTBOX_INSERT");

        SetTestPeer(client, "192.0.2.10");
        var failedSendAttemptsBefore = fake.FailedSendAttempts;
        string registrationBody;
        string userId;
        Guid outboxId;
        fake.FailAlways = true;
        try
        {
            using var registration = await client.PostAsJsonAsync(
                "/api/public-account/register", new { email, password });
            Check(registration.StatusCode == HttpStatusCode.Accepted,
                "F2B_H02_REGISTER_ACCEPTED_WITH_TRANSPORT_FAILURE");
            registrationBody = await registration.Content.ReadAsStringAsync();

            userId = await WaitForUserIdAsync(
                identityConnection, email, TimeSpan.FromSeconds(15));
            Check(await ReadIdentityRoleAssignmentCountAsync(migrator, userId) == 0,
                "F2B_H04_PUBLIC_REGISTER_NO_IDENTITY_ROLE");
            Check(await ReadOrganizationMembershipCountAsync(migrator, userId) == 0,
                "F2B_H04_PUBLIC_REGISTER_NO_ORGANIZATION_MEMBERSHIP");

            outboxId = await WaitForRetryAsync(
                identityConnection, userId, TimeSpan.FromSeconds(15));
            Check(outboxId != Guid.Empty,
                "F2B_H02_REGISTER_IDENTITY_AND_OUTBOX_ATOMIC");
            Check(fake.FailedSendAttempts > failedSendAttemptsBefore,
                "F2B_H05E_SMTP_TRANSPORT_FAILURE_OBSERVED");
            Check(await ReadAttemptCountAsync(identityConnection, outboxId) >= 1,
                "F2B_H05E_SMTP_RETRY_PERSISTED");

            using var smtpUnavailable = await client.GetAsync("/health/ready");
            using var liveDuringSmtpFailure = await client.GetAsync("/health/live");
            Check(smtpUnavailable.StatusCode == HttpStatusCode.OK &&
                  liveDuringSmtpFailure.StatusCode == HttpStatusCode.OK,
                "F2B_H05E_SMTP_UNAVAILABLE_READINESS_200");
        }
        finally
        {
            fake.FailAlways = false;
        }

        var unknownResendEmail = "missing-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        using var unknownResend = await client.PostAsJsonAsync(
            "/api/public-account/resend-confirmation", new { email = unknownResendEmail });
        using var resend = await client.PostAsJsonAsync(
            "/api/public-account/resend-confirmation", new { email });
        Check(unknownResend.StatusCode == HttpStatusCode.Accepted &&
              resend.StatusCode == HttpStatusCode.Accepted &&
              await unknownResend.Content.ReadAsStringAsync() == await resend.Content.ReadAsStringAsync(),
            "F2B_H02_RESEND_UNKNOWN_GENERIC");
        Check(resend.StatusCode == HttpStatusCode.Accepted,
            "F2B_H02_RESEND_CONFIRMATION_ACCEPTED");

        var confirmation = await WaitForDeliveryAsync(fake, IdentityEmailPurpose.ConfirmEmail,
            email, TimeSpan.FromSeconds(20));
        await WaitForStatusAsync(identityConnection, outboxId, IdentityEmailOutboxStatus.Sent, TimeSpan.FromSeconds(10));
        Check(true, "F2B_H02_POSTGRES_ENUM_TEXT_MAPPING");
        Check(await ReadAttemptCountAsync(identityConnection, outboxId) >= 2,
            "F2B_H02_RESEND_CONFIRMATION_RELEASES_RETRY");
        Check(confirmation.Payload.StartsWith(
            PublicAccountReleaseAuthorization.RequiredAudience + "/api/public-account/confirm?",
            StringComparison.Ordinal), "F2B_H02_CONFIRMATION_LINK_CANONICAL_HTTPS");

        var confirmationUri = new Uri(confirmation.Payload);
        var query = QueryHelpers.ParseQuery(confirmationUri.Query);
        var userIdFromLink = query["userId"].ToString();
        var code = query["code"].ToString();
        Check(userIdFromLink == userId && code.Length > 0, "F2B_H02_CONFIRMATION_TOKEN_DELIVERED");

        using var confirmed = await client.GetAsync(confirmation.Payload);
        Check(confirmed.StatusCode == HttpStatusCode.Redirect &&
              confirmed.Headers.Location?.OriginalString.Contains("confirmed=1", StringComparison.Ordinal) == true,
            "F2B_H02_EMAIL_CONFIRMATION_HTTP");
        Check(confirmed.Headers.CacheControl?.NoStore == true &&
              confirmed.Headers.TryGetValues("Referrer-Policy", out var confirmationPolicies) &&
              confirmationPolicies.Contains("no-referrer", StringComparer.OrdinalIgnoreCase),
            "F2B_H04_CONFIRMATION_RESPONSE_SECURITY_HEADERS");

        SetTestPeer(client, "192.0.2.14");
        var malformedConfirmationEmail = "h04-malformed-confirm-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var malformedConfirmationUser = await CreatePublicUserAsync(
            factory.Services, malformedConfirmationEmail, password, emailConfirmed: false);
        using var malformedConfirmation = await client.GetAsync(
            "/api/public-account/confirm?userId=" + Uri.EscapeDataString(malformedConfirmationUser.Id) +
            "&code=" + Uri.EscapeDataString("%%%"));
        Check(malformedConfirmation.StatusCode == HttpStatusCode.Redirect &&
              malformedConfirmation.Headers.Location?.OriginalString.Contains("confirmed=0", StringComparison.Ordinal) == true &&
              !await ReadEmailConfirmedAsync(identityConnection, malformedConfirmationUser.Id),
            "F2B_H04_CONFIRM_MALFORMED_TOKEN_FAIL_CLOSED");

        var invalidConfirmationEmail = "h04-invalid-confirm-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var invalidConfirmationUser = await CreatePublicUserAsync(
            factory.Services, invalidConfirmationEmail, password, emailConfirmed: false);
        var invalidIdentityConfirmationCode = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes("synthetic-invalid-confirmation-token"));
        using var invalidConfirmation = await client.GetAsync(
            "/api/public-account/confirm?userId=" + Uri.EscapeDataString(invalidConfirmationUser.Id) +
            "&code=" + Uri.EscapeDataString(invalidIdentityConfirmationCode));
        Check(invalidConfirmation.StatusCode == HttpStatusCode.Redirect &&
              invalidConfirmation.Headers.Location?.OriginalString.Contains("confirmed=0", StringComparison.Ordinal) == true &&
              !await ReadEmailConfirmedAsync(identityConnection, invalidConfirmationUser.Id),
            "F2B_H04_CONFIRM_INVALID_TOKEN_FAIL_CLOSED");

        using var missingConfirmationUser = await client.GetAsync(
            "/api/public-account/confirm?code=" + Uri.EscapeDataString("%%%"));
        Check(missingConfirmationUser.StatusCode == HttpStatusCode.Redirect &&
              missingConfirmationUser.Headers.Location?.OriginalString.Contains("confirmed=0", StringComparison.Ordinal) == true,
            "F2B_H04_CONFIRM_MISSING_USER_FAIL_CLOSED");

        using var unknownConfirmation = await client.GetAsync(
            "/api/public-account/confirm?userId=" + Uri.EscapeDataString("missing-" + Guid.NewGuid().ToString("N")) +
            "&code=" + Uri.EscapeDataString("synthetic-invalid-code"));
        Check(unknownConfirmation.StatusCode == HttpStatusCode.Redirect &&
              unknownConfirmation.Headers.Location?.OriginalString.Contains("confirmed=0", StringComparison.Ordinal) == true,
            "F2B_H04_CONFIRM_UNKNOWN_USER_FAIL_CLOSED");

        SetTestPeer(client, "192.0.2.10");
        using var replay = await client.GetAsync(confirmation.Payload);
        Check(replay.StatusCode == HttpStatusCode.Redirect &&
              replay.Headers.Location?.OriginalString.Contains("confirmed=0", StringComparison.Ordinal) == true,
            "F2B_H02_CONFIRMATION_REPLAY_REJECTED");

        var raceEmail = "h02-confirm-race-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var raceUser = await CreatePublicUserAsync(factory.Services, raceEmail, password, emailConfirmed: false);
        var raceToken = await GenerateIdentityTokenAsync(factory.Services, raceUser.Id, passwordReset: false);
        var raceCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raceToken));
        var raceUrl = QueryHelpers.AddQueryString(
            QueryHelpers.AddQueryString("/api/public-account/confirm", "userId", raceUser.Id),
            "code", raceCode);
        var raceResponses = await Task.WhenAll(client.GetAsync(raceUrl), client.GetAsync(raceUrl));
        try
        {
            var winners = raceResponses.Count(response =>
                response.StatusCode == HttpStatusCode.Redirect &&
                response.Headers.Location?.OriginalString.Contains("confirmed=1", StringComparison.Ordinal) == true);
            var losers = raceResponses.Count(response =>
                response.StatusCode == HttpStatusCode.Redirect &&
                response.Headers.Location?.OriginalString.Contains("confirmed=0", StringComparison.Ordinal) == true);
            Check(winners == 1 && losers == 1 &&
                  await ReadEmailConfirmedAsync(identityConnection, raceUser.Id),
                "F2B_H02_CONCURRENT_CONFIRMATION_SINGLE_WINNER");
        }
        finally
        {
            foreach (var response in raceResponses) response.Dispose();
        }

        using var duplicate = await client.PostAsJsonAsync("/api/public-account/register",
            new { email, password });
        Check(duplicate.StatusCode == HttpStatusCode.Accepted,
            "F2B_H02_DUPLICATE_REGISTRATION_GENERIC");
        Check(await duplicate.Content.ReadAsStringAsync() == registrationBody,
            "F2B_H04_DUPLICATE_REGISTRATION_BODY_GENERIC");

        var accessToken = await LoginAsync(client, email, password);
        using (var unauthenticatedSession = await client.GetAsync("/api/public-account/session"))
            Check(unauthenticatedSession.StatusCode == HttpStatusCode.Unauthorized,
                "F2B_H02_SESSION_REQUIRES_BEARER");
        using (var sessionRequest = new HttpRequestMessage(HttpMethod.Get, "/api/public-account/session"))
        {
            sessionRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var session = await client.SendAsync(sessionRequest);
            Check(session.StatusCode == HttpStatusCode.OK, "F2B_H02_SESSION_BEARER_OK");
        }

        var staffUnconfirmedEmail = "h02-staff-unconfirmed-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var staffConfirmedEmail = "h02-staff-confirmed-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        IdentityUser staffUnconfirmed;
        IdentityUser staffConfirmed;
        await SetStaffRoleWritePermissionAsync(migrator, enabled: true);
        try
        {
            staffUnconfirmed = await CreateStaffUserAsync(
                factory.Services, staffUnconfirmedEmail, password, emailConfirmed: false);
            staffConfirmed = await CreateStaffUserAsync(
                factory.Services, staffConfirmedEmail, password, emailConfirmed: true);
        }
        finally
        {
            await SetStaffRoleWritePermissionAsync(migrator, enabled: false);
        }
        Check(true, "F2B_H02_STAFF_ROLE_FIXTURES_CREATED");
        using (var staffScope = factory.Services.CreateScope())
        {
            var staffUsers = staffScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            Check((await staffUsers.GetRolesAsync(staffConfirmed)).Count == 4,
                "F2B_H04_STAFF_MULTIPLE_ROLES_CREATED");
        }

        SetTestPeer(client, "192.0.2.11");
        var staffConfirmationToken = await GenerateIdentityTokenAsync(
            factory.Services, staffUnconfirmed.Id, passwordReset: false);
        var staffConfirmationCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(staffConfirmationToken));
        using var staffConfirmation = await client.GetAsync(
            "/api/public-account/confirm?userId=" + Uri.EscapeDataString(staffUnconfirmed.Id) +
            "&code=" + Uri.EscapeDataString(staffConfirmationCode));
        Check(staffConfirmation.StatusCode == HttpStatusCode.Redirect &&
              staffConfirmation.Headers.Location?.OriginalString.Contains("confirmed=0", StringComparison.Ordinal) == true &&
              !await ReadEmailConfirmedAsync(identityConnection, staffUnconfirmed.Id),
            "F2B_H02_STAFF_CONFIRMATION_DENIED");

        using var staffResend = await client.PostAsJsonAsync(
            "/api/public-account/resend-confirmation", new { email = staffUnconfirmedEmail });
        var staffResendOutboxCount = await ReadOutboxCountAsync(identityConnection, staffUnconfirmed.Id);
        var staffResendDelivered = fake.Deliveries.Any(x => x.Email == staffUnconfirmedEmail);
        Console.WriteLine($"F2B_H02_STAFF_RESEND_STATUS={(int)staffResend.StatusCode}");
        Console.WriteLine($"F2B_H02_STAFF_RESEND_OUTBOX_COUNT={staffResendOutboxCount}");
        Console.WriteLine($"F2B_H02_STAFF_RESEND_DELIVERY_PRESENT={staffResendDelivered}");
        Check(staffResend.StatusCode == HttpStatusCode.Accepted &&
              staffResendOutboxCount == 0 &&
              !staffResendDelivered,
            "F2B_H02_STAFF_RESEND_NO_OUTBOX");

        using var staffLogin = await client.PostAsJsonAsync(
            "/api/public-account/login", new { email = staffConfirmedEmail, password });
        Check(staffLogin.StatusCode == HttpStatusCode.Unauthorized &&
              !staffLogin.Headers.Contains("Set-Cookie"),
            "F2B_H02_STAFF_LOGIN_DENIED");

        using var staffForgot = await client.PostAsJsonAsync(
            "/api/public-account/forgot-password", new { email = staffConfirmedEmail });
        Check(staffForgot.StatusCode == HttpStatusCode.Accepted &&
              await ReadOutboxCountAsync(identityConnection, staffConfirmed.Id) == 0 &&
              !fake.Deliveries.Any(x => x.Email == staffConfirmedEmail),
            "F2B_H02_STAFF_FORGOT_NO_OUTBOX");

        var staffResetToken = await GenerateIdentityTokenAsync(
            factory.Services, staffConfirmed.Id, passwordReset: true);
        var staffResetCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(staffResetToken));
        using var staffReset = await client.PostAsJsonAsync("/api/public-account/reset-password",
            new { email = staffConfirmedEmail, resetCode = staffResetCode, newPassword = "StaffReplacement!2026" });
        Check(staffReset.StatusCode == HttpStatusCode.BadRequest &&
              await ReadOutboxCountAsync(identityConnection, staffConfirmed.Id) == 0,
            "F2B_H02_STAFF_RESET_DENIED");
        Check(staffConfirmation.StatusCode == HttpStatusCode.Redirect &&
              staffResend.StatusCode == HttpStatusCode.Accepted &&
              staffLogin.StatusCode == HttpStatusCode.Unauthorized &&
              staffForgot.StatusCode == HttpStatusCode.Accepted &&
              staffReset.StatusCode == HttpStatusCode.BadRequest,
            "F2B_H04_STAFF_ANY_ROLE_EXCLUDED_FROM_ALL_PUBLIC_ACTIONS");

        SetTestPeer(client, "192.0.2.12");
        using var forgot = await client.PostAsJsonAsync("/api/public-account/forgot-password", new { email });
        Check(forgot.StatusCode == HttpStatusCode.Accepted, "F2B_H02_FORGOT_PASSWORD_ACCEPTED");
        var unknownForgotEmail = "missing-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        using var unknownForgot = await client.PostAsJsonAsync(
            "/api/public-account/forgot-password", new { email = unknownForgotEmail });
        Check(unknownForgot.StatusCode == HttpStatusCode.Accepted &&
              await unknownForgot.Content.ReadAsStringAsync() == await forgot.Content.ReadAsStringAsync(),
            "F2B_H02_FORGOT_PASSWORD_UNKNOWN_GENERIC");
        var resetDelivery = await WaitForDeliveryAsync(fake, IdentityEmailPurpose.ResetPassword,
            email, TimeSpan.FromSeconds(20));
        using var reset = await client.PostAsJsonAsync("/api/public-account/reset-password",
            new { email, resetCode = resetDelivery.Payload, newPassword = "ReplacementPassword!2026" });
        Check(reset.StatusCode == HttpStatusCode.OK, "F2B_H02_PASSWORD_RESET_HTTP");

        using var resetReplay = await client.PostAsJsonAsync("/api/public-account/reset-password",
            new { email, resetCode = resetDelivery.Payload, newPassword = "ReplayReplacement!2026" });
        Check(resetReplay.StatusCode == HttpStatusCode.BadRequest,
            "F2B_H04_RESET_TOKEN_REPLAY_REJECTED");

        using var replayPasswordLogin = await client.PostAsJsonAsync("/api/public-account/login",
            new { email, password = "ReplayReplacement!2026" });
        Check(replayPasswordLogin.StatusCode == HttpStatusCode.Unauthorized,
            "F2B_H04_RESET_REPLAY_DOES_NOT_CHANGE_PASSWORD");

        using var oldPassword = await client.PostAsJsonAsync("/api/public-account/login",
            new { email, password });
        Check(oldPassword.StatusCode == HttpStatusCode.Unauthorized,
            "F2B_H02_OLD_PASSWORD_REVOKED");
        _ = await LoginAsync(client, email, "ReplacementPassword!2026");

        var invalidResetEmail = "h04-invalid-reset-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        _ = await CreatePublicUserAsync(factory.Services, invalidResetEmail, password, emailConfirmed: true);
        using var invalidReset = await client.PostAsJsonAsync("/api/public-account/reset-password",
            new { email = invalidResetEmail, resetCode = "%%%", newPassword = "InvalidReplacement!2026" });
        Check(invalidReset.StatusCode == HttpStatusCode.BadRequest,
            "F2B_H04_RESET_MALFORMED_TOKEN_REJECTED");
        _ = await LoginAsync(client, invalidResetEmail, password);
        Check(true, "F2B_H04_RESET_MALFORMED_TOKEN_PRESERVES_PASSWORD");

        var invalidResetTokenEmail = "h04-invalid-reset-token-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        _ = await CreatePublicUserAsync(factory.Services, invalidResetTokenEmail, password, emailConfirmed: true);
        var invalidIdentityResetCode = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes("synthetic-invalid-reset-token"));
        using var invalidResetToken = await client.PostAsJsonAsync("/api/public-account/reset-password",
            new { email = invalidResetTokenEmail, resetCode = invalidIdentityResetCode, newPassword = "InvalidTokenReplacement!2026" });
        Check(invalidResetToken.StatusCode == HttpStatusCode.BadRequest,
            "F2B_H04_RESET_INVALID_TOKEN_REJECTED");
        _ = await LoginAsync(client, invalidResetTokenEmail, password);
        Check(true, "F2B_H04_RESET_INVALID_TOKEN_PRESERVES_PASSWORD");

        SetTestPeer(client, "192.0.2.15");
        var expiredConfirmationEmail = "h02-expired-confirm-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var expiredConfirmationUser = await CreatePublicUserAsync(
            factory.Services, expiredConfirmationEmail, password, emailConfirmed: false);
        var expiredConfirmationToken = await GenerateExpiredIdentityTokenAsync(
            factory.Services, expiredConfirmationUser.Id, passwordReset: false);
        var expiredConfirmationCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(expiredConfirmationToken));
        using var expiredConfirmation = await client.GetAsync(
            "/api/public-account/confirm?userId=" + Uri.EscapeDataString(expiredConfirmationUser.Id) +
            "&code=" + Uri.EscapeDataString(expiredConfirmationCode));
        Check(expiredConfirmation.StatusCode == HttpStatusCode.Redirect &&
              expiredConfirmation.Headers.Location?.OriginalString.Contains("confirmed=0", StringComparison.Ordinal) == true &&
              !await ReadEmailConfirmedAsync(identityConnection, expiredConfirmationUser.Id),
            "F2B_H02_EXPIRED_CONFIRMATION_TOKEN_REJECTED");

        var expiredResetEmail = "h02-expired-reset-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var expiredResetUser = await CreatePublicUserAsync(
            factory.Services, expiredResetEmail, password, emailConfirmed: true);
        var expiredResetToken = await GenerateExpiredIdentityTokenAsync(
            factory.Services, expiredResetUser.Id, passwordReset: true);
        var expiredResetCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(expiredResetToken));
        using var expiredReset = await client.PostAsJsonAsync("/api/public-account/reset-password",
            new { email = expiredResetEmail, resetCode = expiredResetCode, newPassword = "ExpiredReplacement!2026" });
        Check(expiredReset.StatusCode == HttpStatusCode.BadRequest,
            "F2B_H02_EXPIRED_RESET_TOKEN_REJECTED");
        _ = await LoginAsync(client, expiredResetEmail, password);
        Check(true, "F2B_H02_EXPIRED_RESET_PRESERVES_PASSWORD");

        var rollbackEmail = "h02-rollback-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        await SetOutboxInsertPermissionAsync(migrator, enabled: false);
        try
        {
            using var rollbackRegistration = await client.PostAsJsonAsync(
                "/api/public-account/register", new { email = rollbackEmail, password });
            Check(rollbackRegistration.StatusCode == HttpStatusCode.InternalServerError,
                "F2B_H02_REGISTER_ENQUEUE_FAILURE_HTTP_500");
            Check(!await UserExistsAsync(identityConnection, rollbackEmail),
                "F2B_H02_REGISTER_ENQUEUE_FAILURE_ROLLBACK");
        }
        finally
        {
            await SetOutboxInsertPermissionAsync(migrator, enabled: true);
        }

        // H03 account-keyed abuse budgets are tested through the real Production Program
        // and disposable PostgreSQL. Responses remain generic when the account is unknown
        // or the account-keyed budget has been exhausted.
        SetTestPeer(client, "192.0.2.30");
        var abuseRegisterEmail = "h03-register-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var registerBodies = new List<string>();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/public-account/register", new { email = abuseRegisterEmail, password });
            Check(response.StatusCode == HttpStatusCode.Accepted,
                "F2B_H03_REGISTER_THROTTLE_GENERIC_ACCEPTED");
            registerBodies.Add(await response.Content.ReadAsStringAsync());
        }
        Check(registerBodies.Distinct(StringComparer.Ordinal).Count() == 1 &&
              await ReadAbuseCounterCountAsync(
                  identityConnection, IdentityAbuseOperation.Register, abuseRegisterEmail) == 4,
            "F2B_H03_REGISTER_ACCOUNT_BUDGET");

        SetTestPeer(client, "192.0.2.31");
        var abuseResendEmail = "h03-resend-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var resendBodies = new List<string>();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/public-account/resend-confirmation", new { email = abuseResendEmail });
            Check(response.StatusCode == HttpStatusCode.Accepted,
                "F2B_H03_RESEND_THROTTLE_GENERIC_ACCEPTED");
            resendBodies.Add(await response.Content.ReadAsStringAsync());
        }
        Check(resendBodies.Distinct(StringComparer.Ordinal).Count() == 1 &&
              await ReadAbuseCounterCountAsync(
                  identityConnection, IdentityAbuseOperation.ResendConfirmation, abuseResendEmail) == 4,
            "F2B_H03_RESEND_ACCOUNT_BUDGET");

        // Distinct transport peers race on one account key. The PostgreSQL
        // UPSERT must preserve every increment and enforce one shared budget.
        client.DefaultRequestHeaders.Remove(TestPeerAddressStartupFilter.RequestHeader);
        var concurrentAbuseEmail = "h03-pg-concurrent-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var concurrentRequests = Enumerable.Range(1, 16).Select(async index =>
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post, "/api/public-account/resend-confirmation")
            {
                Content = JsonContent.Create(new { email = concurrentAbuseEmail })
            };
            request.Headers.Add(
                TestPeerAddressStartupFilter.RequestHeader, $"198.51.100.{index}");
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }).ToArray();
        var concurrentResponses = await Task.WhenAll(concurrentRequests);
        Check(concurrentResponses.All(status => status == HttpStatusCode.Accepted) &&
              await ReadAbuseCounterCountAsync(
                  identityConnection, IdentityAbuseOperation.ResendConfirmation, concurrentAbuseEmail) == 16,
            "F2B_H03_POSTGRES_ATOMIC_CROSS_PEER_COUNTER");

        SetTestPeer(client, "192.0.2.32");
        var abuseForgotEmail = "h03-forgot-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var forgotBodies = new List<string>();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/public-account/forgot-password", new { email = abuseForgotEmail });
            Check(response.StatusCode == HttpStatusCode.Accepted,
                "F2B_H03_FORGOT_THROTTLE_GENERIC_ACCEPTED");
            forgotBodies.Add(await response.Content.ReadAsStringAsync());
        }
        Check(forgotBodies.Distinct(StringComparer.Ordinal).Count() == 1 &&
              await ReadAbuseCounterCountAsync(
                  identityConnection, IdentityAbuseOperation.ForgotPassword, abuseForgotEmail) == 4,
            "F2B_H03_FORGOT_ACCOUNT_BUDGET");

        SetTestPeer(client, "192.0.2.33");
        var abuseResetEmail = "h03-reset-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var resetBodies = new List<string>();
        var invalidResetCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("synthetic-invalid-reset-token"));
        for (var attempt = 0; attempt < 6; attempt++)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/public-account/reset-password",
                new { email = abuseResetEmail, resetCode = invalidResetCode, newPassword = "ReplacementPassword!2026" });
            Check(response.StatusCode == HttpStatusCode.BadRequest,
                "F2B_H03_RESET_THROTTLE_GENERIC_BAD_REQUEST");
            resetBodies.Add(await response.Content.ReadAsStringAsync());
        }
        Check(resetBodies.Distinct(StringComparer.Ordinal).Count() == 1 &&
              await ReadAbuseCounterCountAsync(
                  identityConnection, IdentityAbuseOperation.ResetPassword, abuseResetEmail) == 6,
            "F2B_H03_RESET_ACCOUNT_BUDGET");

        // Identity's persistent lockout is account-scoped and cannot be bypassed
        // by changing the synthetic source peer.
        SetTestPeer(client, "192.0.2.34");
        var lockoutEmail = "h03-lockout-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var lockoutUser = await CreatePublicUserAsync(
            factory.Services, lockoutEmail, password, emailConfirmed: true);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await client.PostAsJsonAsync(
                "/api/public-account/login", new { email = lockoutEmail, password = "WrongPassword!2026" });
            Check(failed.StatusCode == HttpStatusCode.Unauthorized,
                "F2B_H03_IDENTITY_FAILED_LOGIN_GENERIC");
        }
        Check(await IsUserLockedOutAsync(identityConnection, lockoutUser.Id),
            "F2B_H03_IDENTITY_FIVE_FAILURE_LOCKOUT");

        SetTestPeer(client, "192.0.2.35");
        using (var bypassAttempt = await client.PostAsJsonAsync(
                   "/api/public-account/login", new { email = lockoutEmail, password }))
            Check(bypassAttempt.StatusCode == HttpStatusCode.Unauthorized,
                "F2B_H03_IDENTITY_LOCKOUT_NOT_BYPASSED_BY_PEER");

        await ExpireUserLockoutAsync(identityConnection, lockoutUser.Id);
        _ = await LoginAsync(client, lockoutEmail, password);
        Check(true, "F2B_H03_IDENTITY_LOCKOUT_EXPIRY_RECOVERY");

        Check(fake.Deliveries.Count(x => x.Email == email) == 2,
            "F2B_H02_FAKE_CONFIRMATION_AND_RESET_ONLY");
        Check(!client.DefaultRequestHeaders.Contains("Cookie"),
            "F2B_H02_NO_COOKIE_CLIENT_FALLBACK");
        Console.WriteLine("F2B_H02_NO_REAL_SMTP=PASS");
        Console.WriteLine("F2B_H02_NO_CT164_OR_PRODUCTION_DB=PASS");
        Console.WriteLine("F2B_H02_IDENTITY_HTTP_FAKE_TRANSPORT=PASS");

        await RunH05RestartIdentityRecoveryAsync(
            factory, client, identityConnection, startupConfiguration, keyRing, password);
        // H05b explicitly disposes the first factory/worker before H05d:
        // otherwise a live background worker could race the replay assertions.
        await RunH05dAdminHttpAsync(
            identityConnection, startupConfiguration, staffConfirmedEmail, password);
    }


    // H05d: actual Production Program + PlatformAdmin bearer authorization and
    // gateway filter, with synthetic loopback peer. All accounts/outbox rows live
    // solely in the disposable PostgreSQL 17 fixture; no real SMTP is used.
    private static async Task RunH05dAdminHttpAsync(
        string connection, IReadOnlyDictionary<string, string?> configuration,
        string operatorEmail, string password)
    {
        var gateway = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var values = configuration.ToDictionary(
            pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        values["Admin:Enabled"] = "true";
        values["Admin:GatewaySecret"] = gateway;

        using var environment = new StartupEnvironmentScope(values, Environments.Production);
        using var adminFactory = new Factory(connection, values, new FakeTransport(), adminEnabled: true);
        using var client = adminFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(PublicAccountReleaseAuthorization.RequiredAudience),
            AllowAutoRedirect = false
        });
        SetTestPeer(client, "127.0.0.1");
        Check(adminFactory.Services.GetRequiredService<IWebHostEnvironment>().IsProduction(),
            "F2B_H05D_ADMIN_REAL_PRODUCTION_TESTSERVER");

        static async Task<string> GetBearerAsync(
            HttpClient client, string email, string pass, string gatewaySecret)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
            request.Headers.Add(ProductionAdminAccess.HeaderName, gatewaySecret);
            request.Content = JsonContent.Create(new { email, password = pass });
            using var response = await client.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.OK ||
                response.Headers.Contains("Set-Cookie"))
                throw new InvalidOperationException("F2B_H05D_BEARER_LOGIN_FAILED");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var bearer = json.RootElement.GetProperty("accessToken").GetString();
            return !string.IsNullOrWhiteSpace(bearer)
                ? bearer : throw new InvalidOperationException("F2B_H05D_BEARER_MISSING");
        }

        static async Task<HttpResponseMessage> RequestAsync(
            HttpClient client, HttpMethod method, string endpoint,
            string? bearer, string? gatewaySecret, bool sendCookie = false)
        {
            using var request = new HttpRequestMessage(method, endpoint);
            if (bearer is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            if (gatewaySecret is not null)
                request.Headers.Add(ProductionAdminAccess.HeaderName, gatewaySecret);
            if (sendCookie)
                request.Headers.Add("Cookie", "Identity.Application=synthetic-cookie-not-a-bearer");
            return await client.SendAsync(request);
        }

        var operatorBearer = await GetBearerAsync(client, operatorEmail, password, gateway);
        Check(true, "F2B_H05D_ADMIN_BEARER_LOGIN_NO_COOKIE");

        var publicEmail = "h05d-nonadmin-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        _ = await CreatePublicUserAsync(adminFactory.Services, publicEmail, password, emailConfirmed: true);
        var nonAdminBearer = await GetBearerAsync(client, publicEmail, password, gateway);

        var summaryUrl = "/api/v1/admin/email-outbox/summary";
        var workerStatusUrl = "/api/v1/admin/email-outbox/worker-status";
        using (var missingGateway = await RequestAsync(
            client, HttpMethod.Get, summaryUrl, operatorBearer, null))
            Check(missingGateway.StatusCode == HttpStatusCode.NotFound,
                "F2B_H05D_ADMIN_MISSING_GATEWAY_REFUSED");

        using (var badGateway = await RequestAsync(
            client, HttpMethod.Get, summaryUrl, operatorBearer,
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))))
            Check(badGateway.StatusCode == HttpStatusCode.NotFound,
                "F2B_H05D_ADMIN_INVALID_GATEWAY_REFUSED");

        SetTestPeer(client, "198.51.100.91");
        using (var nonLoopback = await RequestAsync(
            client, HttpMethod.Get, summaryUrl, operatorBearer, gateway))
            Check(nonLoopback.StatusCode == HttpStatusCode.NotFound,
                "F2B_H05D_ADMIN_NONLOOPBACK_GATEWAY_REFUSED");
        SetTestPeer(client, "127.0.0.1");

        using (var anonymous = await RequestAsync(
            client, HttpMethod.Get, summaryUrl, null, gateway))
            Check(anonymous.StatusCode == HttpStatusCode.Unauthorized,
                "F2B_H05D_ADMIN_ANONYMOUS_REFUSED");

        using (var cookieOnly = await RequestAsync(
            client, HttpMethod.Get, summaryUrl, null, gateway, sendCookie: true))
            Check(cookieOnly.StatusCode == HttpStatusCode.Unauthorized,
                "F2B_H05D_ADMIN_COOKIE_ONLY_REFUSED");

        using (var nonAdmin = await RequestAsync(
            client, HttpMethod.Get, summaryUrl, nonAdminBearer, gateway))
            Check(nonAdmin.StatusCode == HttpStatusCode.Forbidden,
                "F2B_H05D_ADMIN_NONADMIN_BEARER_REFUSED");

        using (var accepted = await RequestAsync(
            client, HttpMethod.Get, summaryUrl, operatorBearer, gateway))
        {
            var content = await accepted.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(content);
            Check(accepted.StatusCode == HttpStatusCode.OK &&
                  json.RootElement.TryGetProperty("pending", out _) &&
                  json.RootElement.TryGetProperty("deadLetter", out _) &&
                  !content.Contains("@", StringComparison.Ordinal) &&
                  !content.Contains("userId", StringComparison.OrdinalIgnoreCase) &&
                  !content.Contains("token", StringComparison.OrdinalIgnoreCase),
                "F2B_H05D_ADMIN_SUMMARY_HTTP_ALLOWED_NO_PII");
        }

        using (var anonymousWorkerStatus = await RequestAsync(
            client, HttpMethod.Get, workerStatusUrl, null, gateway))
            Check(anonymousWorkerStatus.StatusCode == HttpStatusCode.Unauthorized,
                "F2B_H05E_ADMIN_WORKER_STATUS_ANONYMOUS_REFUSED");

        using (var workerStatus = await RequestAsync(
            client, HttpMethod.Get, workerStatusUrl, operatorBearer, gateway))
        {
            var content = await workerStatus.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(content);
            Check(workerStatus.StatusCode == HttpStatusCode.OK &&
                  json.RootElement.TryGetProperty("worker", out var worker) &&
                  worker.TryGetProperty("lastCycleSucceededAtUtc", out _) &&
                  json.RootElement.TryGetProperty("backlog", out var backlog) &&
                  backlog.TryGetProperty("pending", out _) &&
                  !content.Contains("@", StringComparison.Ordinal) &&
                  !content.Contains("UserId", StringComparison.OrdinalIgnoreCase) &&
                  !content.Contains("Email", StringComparison.OrdinalIgnoreCase) &&
                  !content.Contains("Token", StringComparison.OrdinalIgnoreCase) &&
                  !content.Contains("Payload", StringComparison.OrdinalIgnoreCase) &&
                  !content.Contains("Secret", StringComparison.OrdinalIgnoreCase),
                "F2B_H05E_ADMIN_WORKER_STATUS_HTTP_NO_PII");
        }

        var deadUserEmail = "h05d-dead-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var deadUser = await CreatePublicUserAsync(
            adminFactory.Services, deadUserEmail, password, emailConfirmed: false);
        var deadId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var scope = adminFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DirectoryDb>();
            db.IdentityEmailOutbox.Add(new IdentityEmailOutboxMessage
            {
                Id = deadId, UserId = deadUser.Id, Purpose = IdentityEmailPurpose.ConfirmEmail,
                Status = IdentityEmailOutboxStatus.DeadLetter, AttemptCount = 3,
                Revision = 4, QueuedAtUtc = now.AddMinutes(-10),
                AvailableAtUtc = now.AddMinutes(-9),
                ExpiresAtUtc = now.AddHours(1), DeadLetterAtUtc = now.AddMinutes(-1),
                LastFailureCode = "dispatch_failed"
            });
            await db.SaveChangesAsync();
        }

        using (var deadList = await RequestAsync(
            client, HttpMethod.Get, "/api/v1/admin/email-outbox/dead-letter",
            operatorBearer, gateway))
        {
            var body = await deadList.Content.ReadAsStringAsync();
            Check(deadList.StatusCode == HttpStatusCode.OK &&
                  body.Contains(deadId.ToString(), StringComparison.OrdinalIgnoreCase) &&
                  !body.Contains(deadUserEmail, StringComparison.OrdinalIgnoreCase),
                "F2B_H05D_ADMIN_DEADLETTER_LIST_ALLOWED_NO_PII");
        }

        var replayPath = "/api/v1/admin/email-outbox/" + deadId + "/replay";
        using (var denied = await RequestAsync(
            client, HttpMethod.Post, replayPath, nonAdminBearer, gateway))
            Check(denied.StatusCode == HttpStatusCode.Forbidden,
                "F2B_H05D_ADMIN_REPLAY_NONADMIN_REFUSED");
        using (var missingGateway = await RequestAsync(
            client, HttpMethod.Post, replayPath, operatorBearer, null))
            Check(missingGateway.StatusCode == HttpStatusCode.NotFound,
                "F2B_H05D_ADMIN_REPLAY_GATEWAY_REFUSED");

        var competing = await Task.WhenAll(
            RequestAsync(client, HttpMethod.Post, replayPath, operatorBearer, gateway),
            RequestAsync(client, HttpMethod.Post, replayPath, operatorBearer, gateway));
        using (competing[0])
        using (competing[1])
        {
            Check(competing.Count(response =>
                response.StatusCode == HttpStatusCode.Accepted) == 1 &&
                  competing.Count(response =>
                response.StatusCode == HttpStatusCode.Conflict) == 1,
                "F2B_H05D_ADMIN_CONCURRENT_REPLAY_SINGLE_WINNER");
        }

        await using (var scope = adminFactory.Services.CreateAsyncScope())
        {
            var row = await scope.ServiceProvider.GetRequiredService<DirectoryDb>()
                .IdentityEmailOutbox.AsNoTracking().SingleAsync(item => item.Id == deadId);
            Check(row.Status == IdentityEmailOutboxStatus.Pending &&
                  row.AttemptCount == 0 && row.LeaseId is null &&
                  row.LeasedUntilUtc is null && row.DeadLetterAtUtc is null &&
                  row.LastFailureCode is null && row.Revision == 5,
                "F2B_H05D_ADMIN_REPLAY_STATE_RESET_ATOMIC");
        }
        using (var retry = await RequestAsync(
            client, HttpMethod.Post, replayPath, operatorBearer, gateway))
            Check(retry.StatusCode == HttpStatusCode.Conflict,
                "F2B_H05D_ADMIN_REPLAY_IDEMPOTENT_CONFLICT");

        using (var unknown = await RequestAsync(
            client, HttpMethod.Post,
            "/api/v1/admin/email-outbox/" + Guid.NewGuid() + "/replay",
            operatorBearer, gateway))
            Check(unknown.StatusCode == HttpStatusCode.NotFound,
                "F2B_H05D_ADMIN_REPLAY_UNKNOWN_404");
    }

    // H05b: exercise actual Identity tokens (not only generic DataProtection payloads)
    // across a stopped Production TestServer, a foreign key ring and an offline
    // restored copy of the original key ring. All data stays in disposable PG17.
    private static async Task RunH05RestartIdentityRecoveryAsync(
        Factory firstFactory, HttpClient firstClient, string identityConnection,
        IReadOnlyDictionary<string, string?> originalConfiguration,
        SyntheticKeyRingScope originalRing, string password)
    {
        var confirmationEmail =
            "h05b-restart-confirm-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var resetEmail =
            "h05b-restart-reset-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var confirmUser = await CreatePublicUserAsync(
            firstFactory.Services, confirmationEmail, password, emailConfirmed: false);
        var resetUser = await CreatePublicUserAsync(
            firstFactory.Services, resetEmail, password, emailConfirmed: true);
        var confirmationToken = await GenerateIdentityTokenAsync(
            firstFactory.Services, confirmUser.Id, passwordReset: false);
        var resetToken = await GenerateIdentityTokenAsync(
            firstFactory.Services, resetUser.Id, passwordReset: true);
        var encodedConfirmation = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(confirmationToken));
        var encodedReset = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
        var confirmationUrl = QueryHelpers.AddQueryString(
            QueryHelpers.AddQueryString("/api/public-account/confirm", "userId", confirmUser.Id),
            "code", encodedConfirmation);

        // Dispose both client and host before constructing either replacement.
        // The outer using declarations are idempotently disposed again at exit.
        firstClient.Dispose();
        firstFactory.Dispose();
        Check(true, "F2B_H05B_FIRST_PRODUCTION_TESTSERVER_DISPOSED");

        // An unrelated but properly provisioned key ring must NOT validate
        // previously minted Identity tokens, even against the same database.
        using (var foreignRing = new SyntheticKeyRingScope())
        {
            var foreignSettings = originalConfiguration.ToDictionary(
                pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            foreignSettings[PublicAccountDataProtectionPolicy.DirectorySetting] =
                foreignRing.DirectoryPath;
            using var foreignEnvironment = new StartupEnvironmentScope(
                foreignSettings, Environments.Production);
            using var foreignFactory = new Factory(
                identityConnection, foreignSettings, new FakeTransport());
            using var foreignClient = foreignFactory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri(PublicAccountReleaseAuthorization.RequiredAudience),
                    AllowAutoRedirect = false
                });
            SetTestPeer(foreignClient, "198.51.100.80");
            using var foreignConfirmation = await foreignClient.GetAsync(confirmationUrl);
            Check(foreignConfirmation.StatusCode == HttpStatusCode.Redirect &&
                  foreignConfirmation.Headers.Location?.OriginalString.Contains(
                      "confirmed=0", StringComparison.Ordinal) == true &&
                  !await ReadEmailConfirmedAsync(identityConnection, confirmUser.Id),
                "F2B_H05B_FOREIGN_RING_CONFIRMATION_TOKEN_REFUSED");

            using var foreignReset = await foreignClient.PostAsJsonAsync(
                "/api/public-account/reset-password",
                new { email = resetEmail, resetCode = encodedReset, newPassword = "H05bForeign!2026" });
            Check(foreignReset.StatusCode == HttpStatusCode.BadRequest,
                "F2B_H05B_FOREIGN_RING_RESET_TOKEN_REFUSED");
        }

        // Simulate an offline key-ring backup/restore into a new absolute path.
        // Tokens are never copied or persisted, only the original DP ring and
        // protected continuity sentinel. This is not an off-host disaster test.
        var restoredPath = Path.Combine(
            Path.GetTempPath(), "yangspace-h05b-restored-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(restoredPath);
        if (OperatingSystem.IsLinux())
            File.SetUnixFileMode(restoredPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            foreach (var sourcePath in Directory.GetFiles(originalRing.DirectoryPath))
            {
                var destination = Path.Combine(restoredPath, Path.GetFileName(sourcePath));
                File.Copy(sourcePath, destination);
                if (OperatingSystem.IsLinux())
                    File.SetUnixFileMode(destination,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            Check(PublicAccountDataProtectionPolicy.IsReady(new DirectoryInfo(restoredPath)),
                "F2B_H05B_OFFLINE_KEYRING_COPY_CONTINUITY_VALID");

            var recoveredSettings = originalConfiguration.ToDictionary(
                pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            recoveredSettings[PublicAccountDataProtectionPolicy.DirectorySetting] =
                restoredPath;
            using var recoveredEnvironment = new StartupEnvironmentScope(
                recoveredSettings, Environments.Production);
            using var recoveredFactory = new Factory(
                identityConnection, recoveredSettings, new FakeTransport());
            using var recoveredClient = recoveredFactory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri(PublicAccountReleaseAuthorization.RequiredAudience),
                    AllowAutoRedirect = false
                });
            SetTestPeer(recoveredClient, "198.51.100.81");

            using var recoveredConfirmation = await recoveredClient.GetAsync(confirmationUrl);
            Check(recoveredConfirmation.StatusCode == HttpStatusCode.Redirect &&
                  recoveredConfirmation.Headers.Location?.OriginalString.Contains(
                      "confirmed=1", StringComparison.Ordinal) == true &&
                  await ReadEmailConfirmedAsync(identityConnection, confirmUser.Id),
                "F2B_H05B_RESTORED_RING_CONFIRMATION_HTTP_SUCCESS");

            using var recoveredReset = await recoveredClient.PostAsJsonAsync(
                "/api/public-account/reset-password",
                new { email = resetEmail, resetCode = encodedReset, newPassword = "H05bRestored!2026" });
            Check(recoveredReset.StatusCode == HttpStatusCode.OK,
                "F2B_H05B_RESTORED_RING_RESET_HTTP_SUCCESS");

            using var oldPassword = await recoveredClient.PostAsJsonAsync(
                "/api/public-account/login", new { email = resetEmail, password });
            Check(oldPassword.StatusCode == HttpStatusCode.Unauthorized,
                "F2B_H05B_RESTORED_RESET_OLD_PASSWORD_REVOKED");
            _ = await LoginAsync(recoveredClient, resetEmail, "H05bRestored!2026");
            Check(true, "F2B_H05B_RESTORED_RESET_NEW_PASSWORD_VALID");
        }
        finally
        {
            if (Directory.Exists(restoredPath))
                Directory.Delete(restoredPath, recursive: true);
        }
        Check(!Directory.Exists(restoredPath),
            "F2B_H05B_RESTORED_KEYRING_CLEANED");
    }

    private static Dictionary<string, string?> CreateAuthorization(ECDsa signer, string commit)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new PublicAccountReleaseAuthorization.Payload(
            1, "h02-ephemeral-test-key", PublicAccountReleaseAuthorization.RequiredScope,
            Environments.Production, PublicAccountReleaseAuthorization.RequiredAudience,
            commit, "issue-44", now.AddMinutes(-1), now.AddMinutes(59));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new Dictionary<string, string?>
        {
            ["PublicAccounts:CanonicalOrigin"] = PublicAccountReleaseAuthorization.RequiredAudience,
            ["PublicAccounts:Enabled"] = "true",
            ["PublicAccounts:Outbox:DispatchEnabled"] = "true",
            ["PublicAccounts:ReleaseAuthorization:Payload"] = WebEncoders.Base64UrlEncode(bytes),
            ["PublicAccounts:ReleaseAuthorization:Signature"] = WebEncoders.Base64UrlEncode(
                signer.SignData(bytes, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)),
            ["PublicAccounts:ReleaseAuthorization:TrustedKeyId"] = "h02-ephemeral-test-key",
            ["PublicAccounts:ReleaseAuthorization:TrustedPublicKeyPem"] = signer.ExportSubjectPublicKeyInfoPem()
        };
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/public-account/login", new { email, password });
        Check(response.StatusCode == HttpStatusCode.OK, "F2B_H02_LOGIN_HTTP");
        Check(!response.Headers.Contains("Set-Cookie"), "F2B_H02_LOGIN_NO_SET_COOKIE");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("accessToken", out var tokenElement))
            throw new InvalidOperationException("F2B_H02_BEARER_TOKEN_RESPONSE_MISSING");
        var token = tokenElement.GetString();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("F2B_H02_BEARER_TOKEN_EMPTY");
        Check(true, "F2B_H02_LOGIN_BEARER_ONLY");
        return token;
    }

    private static void ValidateConnection(string connection, string role)
    {
        var cs = new NpgsqlConnectionStringBuilder(connection);
        if (cs.Host != "localhost" || cs.Port != 5432 ||
            cs.Database != DatabaseName || cs.Username != role ||
            cs.SslMode != SslMode.VerifyFull ||
            cs.RootCertificate != "/run/pgcerts/ca.crt" ||
            cs.Pooling || cs.IncludeErrorDetail)
            throw new InvalidOperationException("F2B_H02_CONNECTION_SCOPE_REFUSED");
    }

    private static async Task VerifyAbuseStoragePrivilegeGateAsync(
        string migrator, string identityConnection)
    {
        await using (var connection = new NpgsqlConnection(migrator))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """REVOKE DELETE ON TABLE public."IdentityAbuseCounters" FROM ys_h02_identity;""";
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<DirectoryDb>()
            .UseNpgsql(identityConnection, postgres =>
                postgres.MigrationsAssembly("YangSpace.PostgreSql.Migrations"))
            .Options;
        var rejected = false;
        try
        {
            await using var db = new DirectoryDb(options);
            await ProductionSchemaGate.ValidatePublicAccountAbuseStorageAsync(db);
        }
        catch (InvalidOperationException exception)
            when (exception.Message == "PUBLIC_ACCOUNT_ABUSE_STORAGE_PRIVILEGE_REQUIRED")
        {
            rejected = true;
        }
        finally
        {
            await using var connection = new NpgsqlConnection(migrator);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """GRANT DELETE ON TABLE public."IdentityAbuseCounters" TO ys_h02_identity;""";
            await command.ExecuteNonQueryAsync();
        }

        Check(rejected, "F2B_H03_ABUSE_STORAGE_PRIVILEGE_FAIL_CLOSED");
        await using (var db = new DirectoryDb(options))
            await ProductionSchemaGate.ValidatePublicAccountAbuseStorageAsync(db);
        Check(true, "F2B_H03_ABUSE_STORAGE_PRIVILEGES_RESTORED");
    }

    private static async Task GrantIdentityRoleAsync(string migrator)
    {
        await using var connection = new NpgsqlConnection(migrator);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            GRANT CONNECT ON DATABASE yangspace_f2b_o3_worker_disposable TO ys_h02_identity;
            GRANT SELECT ON TABLE public."__EFMigrationsHistory" TO ys_h02_identity;
            GRANT SELECT, INSERT, UPDATE ON TABLE public."AspNetUsers" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetUserRoles" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetRoles" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetUserClaims" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetRoleClaims" TO ys_h02_identity;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public."IdentityEmailOutbox" TO ys_h02_identity;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public."IdentityAbuseCounters" TO ys_h02_identity;
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> VerifyQaScopeAsync(string connection, string token)
    {
        // The disposable-database guard belongs to the QA/migrator boundary.
        // Never grant this test-only table to the Identity runtime principal.
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT current_database()=@db
              AND current_user='ys_o3_migrator'
              AND (SELECT scope_token FROM public.yangspace_f2b_o3_qa_guard LIMIT 1)=@token
              AND (SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid())
              AND current_setting('server_version_num')::integer BETWEEN 170000 AND 179999
            """;
        cmd.Parameters.AddWithValue("db", DatabaseName);
        cmd.Parameters.AddWithValue("token", token);
        return (bool)(await cmd.ExecuteScalarAsync() ?? false);
    }

    private static async Task<bool> VerifyIdentityRoleAsync(string connection)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT current_database()=@db
              AND current_user='ys_h02_identity'
              AND (SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid())
              AND current_setting('server_version_num')::integer BETWEEN 170000 AND 179999
              AND NOT (SELECT rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls
                       FROM pg_roles WHERE rolname=current_user)
              AND has_schema_privilege(current_user,'public','USAGE')
              AND NOT has_schema_privilege(current_user,'public','CREATE')
              AND has_table_privilege(current_user,'public."__EFMigrationsHistory"','SELECT')
              AND has_table_privilege(current_user,'public."AspNetUsers"','SELECT')
              AND has_table_privilege(current_user,'public."AspNetUsers"','INSERT')
              AND has_table_privilege(current_user,'public."AspNetUsers"','UPDATE')
              AND NOT has_table_privilege(current_user,'public."AspNetUsers"','DELETE')
              AND has_table_privilege(current_user,'public."AspNetUserRoles"','SELECT')
              AND has_table_privilege(current_user,'public."AspNetRoles"','SELECT')
              AND has_table_privilege(current_user,'public."AspNetUserClaims"','SELECT')
              AND has_table_privilege(current_user,'public."AspNetRoleClaims"','SELECT')
              AND has_table_privilege(current_user,'public."IdentityEmailOutbox"','SELECT')
              AND has_table_privilege(current_user,'public."IdentityEmailOutbox"','INSERT')
              AND has_table_privilege(current_user,'public."IdentityEmailOutbox"','UPDATE')
              AND has_table_privilege(current_user,'public."IdentityEmailOutbox"','DELETE')
              AND has_table_privilege(current_user,'public."IdentityAbuseCounters"','SELECT')
              AND has_table_privilege(current_user,'public."IdentityAbuseCounters"','INSERT')
              AND has_table_privilege(current_user,'public."IdentityAbuseCounters"','UPDATE')
              AND has_table_privilege(current_user,'public."IdentityAbuseCounters"','DELETE')
              AND NOT has_table_privilege(current_user,'public.yangspace_f2b_o3_qa_guard','SELECT')
              AND NOT has_table_privilege(current_user,'public."Organizations"','SELECT')
              AND NOT has_table_privilege(current_user,'public."Locations"','SELECT')
            """;
        cmd.Parameters.AddWithValue("db", DatabaseName);
        return (bool)(await cmd.ExecuteScalarAsync() ?? false);
    }

    private static async Task<string> WaitForUserIdAsync(string connection, string email, TimeSpan timeout)
    {
        var until = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < until)
        {
            await using var c = new NpgsqlConnection(connection);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = """SELECT "Id" FROM public."AspNetUsers" WHERE "Email"=@email""";
            cmd.Parameters.AddWithValue("email", email);
            var value = await cmd.ExecuteScalarAsync();
            if (value is string id) return id;
            await Task.Delay(100);
        }
        throw new TimeoutException("F2B_H02_USER_ROW_NOT_FOUND");
    }

    private static async Task SetStaffRoleWritePermissionAsync(string migrator, bool enabled)
    {
        await using var c = new NpgsqlConnection(migrator);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = enabled
            ? """GRANT INSERT ON TABLE public."AspNetRoles", public."AspNetUserRoles" TO ys_h02_identity;"""
            : """REVOKE INSERT ON TABLE public."AspNetRoles", public."AspNetUserRoles" FROM ys_h02_identity;""";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<IdentityUser> CreatePublicUserAsync(
        IServiceProvider services, string email, string password, bool emailConfirmed)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed,
            LockoutEnabled = true
        };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            throw new InvalidOperationException("F2B_H02_PUBLIC_TOKEN_FIXTURE_CREATE_FAILED");
        return user;
    }

    private static async Task<string> GenerateExpiredIdentityTokenAsync(
        IServiceProvider services, string userId, bool passwordReset)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("F2B_H02_EXPIRED_TOKEN_USER_MISSING");
        var token = passwordReset
            ? await users.GeneratePasswordResetTokenAsync(user)
            : await users.GenerateEmailConfirmationTokenAsync(user);

        var dataProtection = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value;
        var protector = dataProtection.CreateProtector(options.Name ?? "DataProtectorTokenProvider");
        var unprotected = protector.Unprotect(Convert.FromBase64String(token));

        string tokenUserId;
        string purpose;
        string stamp;
        using (var input = new BinaryReader(
            new MemoryStream(unprotected), new UTF8Encoding(false, true), leaveOpen: false))
        {
            _ = input.ReadInt64();
            tokenUserId = input.ReadString();
            purpose = input.ReadString();
            stamp = input.ReadString();
            if (input.BaseStream.Position != input.BaseStream.Length)
                throw new InvalidOperationException("F2B_H02_TOKEN_PAYLOAD_SHAPE_UNEXPECTED");
        }

        using var payload = new MemoryStream();
        using (var output = new BinaryWriter(payload, new UTF8Encoding(false, true), leaveOpen: true))
        {
            output.Write((DateTimeOffset.UtcNow - options.TokenLifespan - TimeSpan.FromMinutes(1)).UtcTicks);
            output.Write(tokenUserId);
            output.Write(purpose);
            output.Write(stamp);
        }

        return Convert.ToBase64String(protector.Protect(payload.ToArray()));
    }

    private static async Task<IdentityUser> CreateStaffUserAsync(
        IServiceProvider services, string email, string password, bool emailConfirmed)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roleNames = new[] { "H02Staff", "PlatformAdmin", "Moderator", "SupportAgent" };
        foreach (var roleName in roleNames)
        {
            if (await roles.RoleExistsAsync(roleName)) continue;
            var role = await roles.CreateAsync(new IdentityRole(roleName));
            if (!role.Succeeded)
                throw new InvalidOperationException("F2B_H02_STAFF_ROLE_CREATE_FAILED");
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed
        };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            throw new InvalidOperationException("F2B_H02_STAFF_USER_CREATE_FAILED");
        var assigned = await users.AddToRolesAsync(user, roleNames);
        if (!assigned.Succeeded)
            throw new InvalidOperationException("F2B_H02_STAFF_ROLE_ASSIGN_FAILED");
        return user;
    }

    private static async Task<string> GenerateIdentityTokenAsync(
        IServiceProvider services, string userId, bool passwordReset)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("F2B_H02_STAFF_TOKEN_USER_MISSING");
        return passwordReset
            ? await users.GeneratePasswordResetTokenAsync(user)
            : await users.GenerateEmailConfirmationTokenAsync(user);
    }

    private static async Task<bool> ReadEmailConfirmedAsync(string connection, string userId)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """SELECT "EmailConfirmed" FROM public."AspNetUsers" WHERE "Id"=@id""";
        cmd.Parameters.AddWithValue("id", userId);
        return (bool)(await cmd.ExecuteScalarAsync() ?? false);
    }

    private static async Task<int> ReadAbuseCounterCountAsync(
        string connection, IdentityAbuseOperation operation, string email)
    {
        var normalized = email.Trim().ToUpperInvariant();
        var digest = Convert.ToHexString(HMACSHA256.HashData(
            TestAbuseHmacKeyBytes, Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT "Count" FROM public."IdentityAbuseCounters"
            WHERE "Operation"=@operation AND "KeyHash"=@keyHash
            ORDER BY "WindowStartUtc" DESC LIMIT 1
            """;
        cmd.Parameters.AddWithValue("operation", operation.ToString());
        cmd.Parameters.AddWithValue("keyHash", digest);
        var value = await cmd.ExecuteScalarAsync();
        return value is null ? 0 : Convert.ToInt32(
            value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<bool> IsUserLockedOutAsync(string connection, string userId)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT "LockoutEnd" IS NOT NULL AND "LockoutEnd" > now()
            FROM public."AspNetUsers" WHERE "Id"=@id
            """;
        cmd.Parameters.AddWithValue("id", userId);
        return (bool)(await cmd.ExecuteScalarAsync() ?? false);
    }

    private static async Task ExpireUserLockoutAsync(string connection, string userId)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            UPDATE public."AspNetUsers"
            SET "LockoutEnd"=now() - interval '1 minute', "AccessFailedCount"=0
            WHERE "Id"=@id
            """;
        cmd.Parameters.AddWithValue("id", userId);
        if (await cmd.ExecuteNonQueryAsync() != 1)
            throw new InvalidOperationException("F2B_H03_LOCKOUT_EXPIRY_FIXTURE_FAILED");
    }

    private static async Task<int> ReadOutboxCountAsync(string connection, string userId)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """SELECT count(*) FROM public."IdentityEmailOutbox" WHERE "UserId"=@id""";
        cmd.Parameters.AddWithValue("id", userId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task SetOutboxInsertPermissionAsync(string migrator, bool enabled)
    {
        await using var c = new NpgsqlConnection(migrator);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = enabled
            ? """GRANT INSERT ON TABLE public."IdentityEmailOutbox" TO ys_h02_identity;"""
            : """REVOKE INSERT ON TABLE public."IdentityEmailOutbox" FROM ys_h02_identity;""";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SetIdentityRoleConnectAsync(
        string migrator, string identityConnection, NpgsqlDataSource dataSource, bool enabled)
    {
        // REVOKE CONNECT affects new sessions only. Existing PostgreSQL sessions
        // remain usable, and EF's NpgsqlDataSource owns the actual TestServer pool.
        // Clear that exact data source so readiness must establish a new connection.
        await using (var c = new NpgsqlConnection(migrator))
        {
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = enabled
                ? $"GRANT CONNECT ON DATABASE {DatabaseName} TO {IdentityRole};"
                : $"REVOKE CONNECT ON DATABASE {DatabaseName} FROM {IdentityRole};";
            await cmd.ExecuteNonQueryAsync();
        }

        if (!enabled)
        {
            Check(!await IdentityRoleHasDatabaseConnectAsync(migrator),
                "F2B_H05E_DB_UNAVAILABLE_CONNECT_PRIVILEGE_REVOKED");
        }

        dataSource.Clear();
        if (!enabled)
        {
            Check(true, "F2B_H05E_DB_UNAVAILABLE_EF_DATASOURCE_CLEARED");
            await WaitForNoIdentitySessionsAsync(migrator, TimeSpan.FromSeconds(5));
            Check(true, "F2B_H05E_DB_UNAVAILABLE_SESSIONS_DRAINED");
            Check(await IdentityConnectionDeniedAsync(identityConnection),
                "F2B_H05E_DB_UNAVAILABLE_NEW_CONNECTION_DENIED");
        }
    }

    private static async Task<bool> IdentityRoleHasDatabaseConnectAsync(string migrator)
    {
        await using var c = new NpgsqlConnection(migrator);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT has_database_privilege(@role, @database, 'CONNECT');";
        cmd.Parameters.AddWithValue("role", IdentityRole);
        cmd.Parameters.AddWithValue("database", DatabaseName);
        return Convert.ToBoolean(await cmd.ExecuteScalarAsync());
    }

    private static async Task WaitForNoIdentitySessionsAsync(
        string migrator, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (true)
        {
            var sessions = await ReadIdentitySessionCountAsync(migrator);
            if (sessions == 0)
                return;

            if (DateTimeOffset.UtcNow >= deadline)
                throw new InvalidOperationException(
                    "F2B_H05E_DB_UNAVAILABLE_SESSIONS_DID_NOT_DRAIN");

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }
    }

    private static async Task<int> ReadIdentitySessionCountAsync(string migrator)
    {
        await using var c = new NpgsqlConnection(migrator);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT count(*)
            FROM pg_stat_activity
            WHERE datname=@database
              AND usename=@role
              AND pid<>pg_backend_pid();
            """;
        cmd.Parameters.AddWithValue("database", DatabaseName);
        cmd.Parameters.AddWithValue("role", IdentityRole);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<bool> IdentityConnectionDeniedAsync(string identityConnection)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(identityConnection)
        {
            Pooling = false
        };

        await using var c = new NpgsqlConnection(connectionString.ConnectionString);
        try
        {
            await c.OpenAsync();
            return false;
        }
        catch (PostgresException exception) when (exception.SqlState == "42501")
        {
            return true;
        }
    }

    private static async Task RenameIdentityOutboxTableAsync(string migrator, bool renamed)
    {
        await using var c = new NpgsqlConnection(migrator);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = renamed
            ? """ALTER TABLE public."IdentityEmailOutbox" RENAME TO "IdentityEmailOutbox_H05E";"""
            : """ALTER TABLE public."IdentityEmailOutbox_H05E" RENAME TO "IdentityEmailOutbox";""";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<bool> UserExistsAsync(string connection, string email)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """SELECT EXISTS(SELECT 1 FROM public."AspNetUsers" WHERE "Email"=@email)""";
        cmd.Parameters.AddWithValue("email", email);
        return (bool)(await cmd.ExecuteScalarAsync() ?? false);
    }

    private static async Task<int> ReadTotalOutboxCountAsync(string connection)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """SELECT count(*) FROM public."IdentityEmailOutbox" """;
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<int> ReadIdentityRoleAssignmentCountAsync(string connection, string userId)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """SELECT count(*) FROM public."AspNetUserRoles" WHERE "UserId"=@userId""";
        cmd.Parameters.AddWithValue("userId", userId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<int> ReadOrganizationMembershipCountAsync(string connection, string userId)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """SELECT count(*) FROM public."OrganizationMembers" WHERE "UserId"=@userId""";
        cmd.Parameters.AddWithValue("userId", userId);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<int> ReadAttemptCountAsync(string connection, Guid id)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """SELECT "AttemptCount" FROM public."IdentityEmailOutbox" WHERE "Id"=@id""";
        cmd.Parameters.AddWithValue("id", id);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<Guid> WaitForRetryAsync(string connection, string userId, TimeSpan timeout)
    {
        var until = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < until)
        {
            await using var c = new NpgsqlConnection(connection);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = """
                SELECT "Id" FROM public."IdentityEmailOutbox"
                WHERE "UserId"=@user AND "Purpose"=@purpose AND "Status"=@pending AND "AttemptCount">=1
                ORDER BY "QueuedAtUtc" DESC LIMIT 1
                """;
            cmd.Parameters.AddWithValue("user", userId);
            cmd.Parameters.Add(new NpgsqlParameter("purpose", NpgsqlTypes.NpgsqlDbType.Varchar)
            {
                Value = IdentityEmailPurpose.ConfirmEmail.ToString()
            });
            cmd.Parameters.Add(new NpgsqlParameter("pending", NpgsqlTypes.NpgsqlDbType.Varchar)
            {
                Value = IdentityEmailOutboxStatus.Pending.ToString()
            });
            var value = await cmd.ExecuteScalarAsync();
            if (value is Guid id) return id;
            await Task.Delay(100);
        }
        throw new TimeoutException("F2B_H02_OUTBOX_RETRY_NOT_OBSERVED");
    }

    private static async Task WaitForStatusAsync(
        string connection, Guid id, IdentityEmailOutboxStatus status, TimeSpan timeout)
    {
        var until = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < until)
        {
            await using var c = new NpgsqlConnection(connection);
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = """SELECT "Status" FROM public."IdentityEmailOutbox" WHERE "Id"=@id""";
            cmd.Parameters.AddWithValue("id", id);
            var value = await cmd.ExecuteScalarAsync();
            if (value is string persistedStatus &&
                string.Equals(persistedStatus, status.ToString(), StringComparison.Ordinal))
                return;
            await Task.Delay(100);
        }
        throw new TimeoutException("F2B_H02_OUTBOX_STATUS_TIMEOUT");
    }

    private static async Task<Delivery> WaitForDeliveryAsync(
        FakeTransport fake, IdentityEmailPurpose purpose, string email, TimeSpan timeout)
    {
        var until = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < until)
        {
            var delivery = fake.Deliveries.FirstOrDefault(x => x.Purpose == purpose && x.Email == email);
            if (delivery is not null) return delivery;
            await Task.Delay(100);
        }
        throw new TimeoutException("F2B_H02_FAKE_DELIVERY_TIMEOUT");
    }
}
