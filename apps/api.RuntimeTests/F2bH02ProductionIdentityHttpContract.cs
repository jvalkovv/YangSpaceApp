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
using Npgsql;
using YangSpace.Api;

internal static class F2bH02ProductionIdentityHttpContract
{
    private const string DatabaseName = "yangspace_f2b_o3_worker_disposable";
    private const string IdentityRole = "ys_h02_identity";

    private static void Check(bool ok, string marker)
    {
        if (!ok) throw new InvalidOperationException("F2B_H02_HTTP_FAILED=" + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException("F2B_H02_ENV_MISSING=" + name);

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
        private int _failBeforeAccept;

        internal IReadOnlyCollection<Delivery> Deliveries => _deliveries.ToArray();
        internal void FailBeforeAcceptOnce() => Interlocked.Exchange(ref _failBeforeAccept, 1);

        public Task SendConfirmationAsync(
            IdentityUser user, string email, string confirmationLink, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.Exchange(ref _failBeforeAccept, 0) == 1)
                throw new IOException("synthetic transport failure");
            _deliveries.Enqueue(new Delivery(IdentityEmailPurpose.ConfirmEmail, email, confirmationLink));
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(
            IdentityUser user, string email, string resetCode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        internal FakeTransport Fake { get; }

        internal Factory(string connection, IReadOnlyDictionary<string, string?> configuration, FakeTransport fake)
        {
            _connection = connection;
            _configuration = configuration;
            Fake = fake;
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
                values["Admin:Enabled"] = "false";
                values["MerchantRead:Enabled"] = "false";
                values["ClaimReview:QaEnabled"] = "false";
                values["ClaimReviewPostgreSqlQa:Enabled"] = "false";
                values["PublicAccounts:Enabled"] = "true";
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
                services.AddSingleton<IStartupFilter>(new TestPeerAddressStartupFilter());
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
        ValidateConnection(identityConnection, IdentityRole);
        Check(await VerifyQaScopeAsync(migrator, scopeToken),
            "F2B_H02_QA_SCOPE_BOUND");
        await GrantIdentityRoleAsync(migrator);
        Check(await VerifyIdentityRoleAsync(identityConnection),
            "F2B_H02_IDENTITY_RUNTIME_LEAST_PRIVILEGE");

        using var environment = new CommitEnvironmentScope(commit);
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

        var email = "h02-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        const string password = "StrongPassword!2026";
        fake.FailBeforeAcceptOnce();

        using var registration = await client.PostAsJsonAsync("/api/public-account/register",
            new { email, password });
        Check(registration.StatusCode == HttpStatusCode.Accepted,
            "F2B_H02_REGISTER_ACCEPTED_WITH_TRANSPORT_FAILURE");

        var userId = await WaitForUserIdAsync(identityConnection, email, TimeSpan.FromSeconds(15));
        var outboxId = await WaitForRetryAsync(identityConnection, userId, TimeSpan.FromSeconds(15));
        Check(outboxId != Guid.Empty, "F2B_H02_REGISTER_IDENTITY_AND_OUTBOX_ATOMIC");

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

        using var oldPassword = await client.PostAsJsonAsync("/api/public-account/login",
            new { email, password });
        Check(oldPassword.StatusCode == HttpStatusCode.Unauthorized,
            "F2B_H02_OLD_PASSWORD_REVOKED");
        _ = await LoginAsync(client, email, "ReplacementPassword!2026");

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

        Check(fake.Deliveries.Count(x => x.Email == email) == 2,
            "F2B_H02_FAKE_CONFIRMATION_AND_RESET_ONLY");
        Check(!client.DefaultRequestHeaders.Contains("Cookie"),
            "F2B_H02_NO_COOKIE_CLIENT_FALLBACK");
        Console.WriteLine("F2B_H02_NO_REAL_SMTP=PASS");
        Console.WriteLine("F2B_H02_NO_CT164_OR_PRODUCTION_DB=PASS");
        Console.WriteLine("F2B_H02_IDENTITY_HTTP_FAKE_TRANSPORT=PASS");
    }

    private static Dictionary<string, string?> CreateAuthorization(ECDsa signer, string commit)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new PublicAccountReleaseAuthorization.Payload(
            1, "h02-ephemeral-test-key", PublicAccountReleaseAuthorization.RequiredScope,
            Environments.Production, PublicAccountReleaseAuthorization.RequiredAudience,
            commit, "issue-44", now.AddMinutes(-1), now.AddHours(1));
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

    private static async Task GrantIdentityRoleAsync(string migrator)
    {
        await using var connection = new NpgsqlConnection(migrator);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            GRANT SELECT ON TABLE public."__EFMigrationsHistory" TO ys_h02_identity;
            GRANT SELECT, INSERT, UPDATE ON TABLE public."AspNetUsers" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetUserRoles" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetRoles" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetUserClaims" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetRoleClaims" TO ys_h02_identity;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public."IdentityEmailOutbox" TO ys_h02_identity;
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
            EmailConfirmed = emailConfirmed
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
        const string roleName = "H02Staff";
        if (!await roles.RoleExistsAsync(roleName))
        {
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
        var assigned = await users.AddToRoleAsync(user, roleName);
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

    private static async Task<bool> UserExistsAsync(string connection, string email)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """SELECT EXISTS(SELECT 1 FROM public."AspNetUsers" WHERE "Email"=@email)""";
        cmd.Parameters.AddWithValue("email", email);
        return (bool)(await cmd.ExecuteScalarAsync() ?? false);
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
                WHERE "UserId"=@user AND "Purpose"=@purpose AND "Status"=@pending AND "AttemptCount"=1
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
