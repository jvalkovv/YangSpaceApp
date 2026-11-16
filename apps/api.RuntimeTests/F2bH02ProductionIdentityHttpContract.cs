using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        await GrantIdentityRoleAsync(migrator);
        Check(await VerifyIdentityRoleAsync(identityConnection, scopeToken),
            "F2B_H02_IDENTITY_RUNTIME_LEAST_PRIVILEGE");

        using var environment = new CommitEnvironmentScope(commit);
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var authorization = CreateAuthorization(signer, commit);
        var fake = new FakeTransport();
        using var factory = new Factory(identityConnection, authorization, fake);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(PublicAccountReleaseAuthorization.RequiredAudience),
            AllowAutoRedirect = false
        });

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
            GRANT SELECT ON TABLE public.yangspace_f2b_o3_qa_guard TO ys_h02_identity;
            GRANT SELECT, INSERT, UPDATE ON TABLE public."AspNetUsers" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetUserRoles" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetRoles" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetUserClaims" TO ys_h02_identity;
            GRANT SELECT ON TABLE public."AspNetRoleClaims" TO ys_h02_identity;
            GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public."IdentityEmailOutbox" TO ys_h02_identity;
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> VerifyIdentityRoleAsync(string connection, string token)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT current_database()=@db
              AND current_user='ys_h02_identity'
              AND (SELECT scope_token FROM public.yangspace_f2b_o3_qa_guard LIMIT 1)=@token
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
              AND NOT has_table_privilege(current_user,'public."Organizations"','SELECT')
              AND NOT has_table_privilege(current_user,'public."Locations"','SELECT')
            """;
        cmd.Parameters.AddWithValue("db", DatabaseName);
        cmd.Parameters.AddWithValue("token", token);
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
            cmd.Parameters.AddWithValue("purpose", (int)IdentityEmailPurpose.ConfirmEmail);
            cmd.Parameters.AddWithValue("pending", (int)IdentityEmailOutboxStatus.Pending);
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
            if (value is not null && Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) == (int)status)
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
