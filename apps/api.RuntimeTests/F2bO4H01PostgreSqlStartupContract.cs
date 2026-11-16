using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using YangSpace.Api;

// H01 executes the real Program Production startup and ProductionSchemaGate via TestServer.
// PublicAccounts and production dispatch remain off; no fake production activation bypass.
internal static class F2bO4H01PostgreSqlStartupContract
{
    private const string DbName = "yangspace_f2b_o3_worker_disposable";

    private sealed class StartupEnvironmentScope : IDisposable
    {
        private readonly Dictionary<string,string?> previous = new(StringComparer.Ordinal);
        private static readonly string[] Names =
        [
            "ConnectionStrings__Main", "Database__Provider", "AllowedHosts",
            "Preview__Enabled", "Preview__AllowRegistration", "Admin__Enabled",
            "MerchantRead__Enabled", "ClaimReview__QaEnabled",
            "ClaimReviewPostgreSqlQa__Enabled", "PublicAccounts__Enabled",
            "PublicAccounts__Outbox__DispatchEnabled",
            "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT"
        ];

        internal StartupEnvironmentScope()
        {
            foreach (var name in Names)
                previous[name] = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        internal void SetStartup(string connection, bool publicAccounts=false,
            bool dispatch=false, bool production=true)
        {
            static void Set(string name,string value) =>
                Environment.SetEnvironmentVariable(name,value,EnvironmentVariableTarget.Process);

            Set("ConnectionStrings__Main",connection);
            Set("Database__Provider","PostgreSQL");
            Set("AllowedHosts","yangspace.gndstore.eu");
            Set("Preview__Enabled","false");
            Set("Preview__AllowRegistration","false");
            Set("Admin__Enabled","false");
            Set("MerchantRead__Enabled","false");
            Set("ClaimReview__QaEnabled","false");
            Set("ClaimReviewPostgreSqlQa__Enabled","false");
            Set("PublicAccounts__Enabled",publicAccounts ? "true" : "false");
            Set("PublicAccounts__Outbox__DispatchEnabled",dispatch ? "true" : "false");
            Set("ASPNETCORE_ENVIRONMENT",production ? "Production" : "Development");
            Set("DOTNET_ENVIRONMENT",production ? "Production" : "Development");
        }

        public void Dispose()
        {
            foreach (var (name,value) in previous)
                Environment.SetEnvironmentVariable(name,value,EnvironmentVariableTarget.Process);
        }
    }

    private sealed class Factory : WebApplicationFactory<ApiAssemblyMarker>
    {
        internal required string Connection { get; init; }
        internal bool PublicAccounts { get; init; }
        internal bool Dispatch { get; init; }
        internal bool Production { get; init; } = true;

        protected override IEnumerable<Assembly> GetTestAssemblies()
        {
            yield return typeof(F2bO4H01PostgreSqlStartupContract).Assembly;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var root = AppContext.BaseDirectory;
            if (Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) != "/qa")
                throw new InvalidOperationException("F2B_O4_H01_UNEXPECTED_CONTENT_ROOT");
            if (!File.Exists(Path.Combine(root, "appsettings.json")))
                throw new InvalidOperationException("F2B_O4_H01_APPSETTINGS_NOT_PUBLISHED");
            builder.UseContentRoot(root);
            builder.UseEnvironment(Production ? "Production" : "Development");
            builder.ConfigureAppConfiguration((_, settings) => settings.AddInMemoryCollection(
                new Dictionary<string,string?>
                {
                    ["ConnectionStrings:Main"] = Connection,
                    ["Database:Provider"] = "PostgreSQL",
                    ["AllowedHosts"] = "yangspace.gndstore.eu",
                    ["Preview:Enabled"] = "false",
                    ["Preview:AllowRegistration"] = "false",
                    ["Admin:Enabled"] = "false",
                    ["MerchantRead:Enabled"] = "false",
                    ["ClaimReview:QaEnabled"] = "false",
                    ["ClaimReviewPostgreSqlQa:Enabled"] = "false",
                    ["PublicAccounts:Enabled"] = PublicAccounts ? "true" : "false",
                    ["PublicAccounts:Outbox:DispatchEnabled"] = Dispatch ? "true" : "false"
                }));
        }
    }

    private static void Check(bool ok, string marker)
    {
        if (!ok) throw new InvalidOperationException("F2B_O4_H01_FAILED=" + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException("F2B_O4_H01_ENV_MISSING=" + name);

    private static NpgsqlConnectionStringBuilder Scoped(string raw, string role)
    {
        var cs = new NpgsqlConnectionStringBuilder(raw);
        if (cs.Host != "localhost" || cs.Port != 5432 ||
            cs.Database != DbName || cs.Username != role ||
            cs.SslMode != SslMode.VerifyFull || cs.Pooling || cs.IncludeErrorDetail ||
            cs.RootCertificate != "/run/pgcerts/ca.crt")
            throw new InvalidOperationException("F2B_O4_H01_CONNECTION_NOT_DISPOSABLE");
        return cs;
    }

    private static async Task VerifyRuntimeScope(string connection, string token)
    {
        await using var c = new NpgsqlConnection(connection);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            SELECT current_database()=@db
              AND current_user='ys_o3_runtime'
              AND (SELECT scope_token FROM public.yangspace_f2b_o3_qa_guard LIMIT 1)=@token
              AND (SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid())
              AND current_setting('server_version_num')::integer BETWEEN 170000 AND 179999
              AND NOT (SELECT rolsuper FROM pg_roles WHERE rolname=current_user)
              AND has_schema_privilege(current_user,'public','USAGE')
              AND NOT has_schema_privilege(current_user,'public','CREATE')
            """;
        cmd.Parameters.AddWithValue("db", DbName);
        cmd.Parameters.AddWithValue("token", token);
        Check((bool)(await cmd.ExecuteScalarAsync() ?? false),
            "F2B_O4_H01_RUNTIME_TLS_SCOPE_NON_ELEVATED");
    }

    private static async Task GrantHistoryOnly(string migrator)
    {
        await using var c = new NpgsqlConnection(migrator);
        await c.OpenAsync();
        await using var cmd = c.CreateCommand();
        cmd.CommandText = """
            GRANT SELECT ON TABLE public."__EFMigrationsHistory" TO ys_o3_runtime;
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    private static bool HasMessage(Exception exception, string expected)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current.Message.Contains(expected, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool HasAuthenticationFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is AuthenticationException) return true;
        return false;
    }

    private static async Task RejectStartup(Factory factory, string error, string marker)
    {
        var refused = false;
        try
        {
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://yangspace.gndstore.eu")
            });
            using var response = await client.GetAsync("/health/live");
        }
        catch (Exception ex) when (HasMessage(ex, error)) { refused = true; }
        Check(refused, marker);
    }

    internal static async Task RunAsync()
    {
        var migrator = Required("YANGSPACE_F2B_O3_PG_MIGRATOR_CONNECTION");
        var runtime = Required("YANGSPACE_F2B_O3_PG_RUNTIME_CONNECTION");
        var scopeToken = Required("YANGSPACE_F2B_O3_PG_SCOPE_TOKEN");

        using var startupEnvironment = new StartupEnvironmentScope();
        Scoped(migrator, "ys_o3_migrator");
        Scoped(runtime, "ys_o3_runtime");
        await VerifyRuntimeScope(runtime,scopeToken);
        await GrantHistoryOnly(migrator);

        await using (var db = new NpgsqlConnection(runtime))
        {
            await db.OpenAsync();
            await using var cmd = db.CreateCommand();
            cmd.CommandText = """
                SELECT has_table_privilege(current_user,'public."__EFMigrationsHistory"','SELECT')
                  AND NOT has_schema_privilege(current_user,'public','CREATE')
                  AND NOT has_column_privilege(current_user,'public."AspNetUsers"','PasswordHash','SELECT')
                  AND NOT has_table_privilege(current_user,'public."Organizations"','SELECT')
                """;
            Check((bool)(await cmd.ExecuteScalarAsync() ?? false),
                "F2B_O4_H01_RUNTIME_METADATA_READ_ONLY");
        }

        // This is the SAME policy used by Program for its Production PostgreSQL startup.
        ProductionPostgreSqlPolicy.RequirePreparedConnection(runtime);
        Check(true, "F2B_O4_H01_PRODUCTION_PG_POLICY");

        startupEnvironment.SetStartup(runtime);
        using (var factory = new Factory { Connection = runtime })
        {
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://yangspace.gndstore.eu"),
                AllowAutoRedirect = false
            });
            Check(factory.Services.GetRequiredService<IWebHostEnvironment>().IsProduction(),
                "F2B_O4_H01_REAL_PRODUCTION_ENVIRONMENT");
            using var live = await client.GetAsync("/health/live");
            Check(live.StatusCode == HttpStatusCode.OK,
                "F2B_O4_H01_PRODUCTION_HTTP_LIVE");
            using var ready = await client.GetAsync("/health/ready");
            Check(ready.StatusCode == HttpStatusCode.OK,
                "F2B_O4_H01_PRODUCTION_HTTP_READY");

            using var blocked = await client.PostAsJsonAsync("/api/public-account/register",
                new { email = "qa-none@example.invalid", password = "QaOnlyPassword!2026" });
            Check(blocked.StatusCode == HttpStatusCode.NotFound,
                "F2B_O4_H01_DISABLED_PUBLIC_REGISTER_404");

            using var legacy = await client.PostAsJsonAsync("/api/auth/register",
                new { email = "qa-none@example.invalid", password = "QaOnlyPassword!2026" });
            Check(legacy.StatusCode == HttpStatusCode.NotFound,
                "F2B_O4_H01_DISABLED_LEGACY_REGISTER_404");
        }

        var invalidTlsConnection = new NpgsqlConnectionStringBuilder(runtime) {
            SslMode = SslMode.Disable
        }.ConnectionString;
        startupEnvironment.SetStartup(invalidTlsConnection);
        using (var invalidTls = new Factory { Connection = invalidTlsConnection })
            await RejectStartup(invalidTls, "SslMode=VerifyFull",
                "F2B_O4_H01_REJECTS_DISABLED_TLS");

        // Hostname verification must fail when the SAN is localhost, not 127.0.0.1.
        var wrongHost = new NpgsqlConnectionStringBuilder(runtime) {
            Host = "127.0.0.1"
        }.ConnectionString;
        var wrongHostRefused = false;
        try
        {
            await using var c = new NpgsqlConnection(wrongHost);
            await c.OpenAsync();
        }
        catch (Exception ex) when (HasAuthenticationFailure(ex))
        { wrongHostRefused = true; }
        Check(wrongHostRefused,"F2B_O4_H01_REJECTS_TLS_HOSTNAME_MISMATCH");

        startupEnvironment.SetStartup(migrator);
        using (var elevated = new Factory { Connection = migrator })
            await RejectStartup(elevated,
                "PostgreSQL runtime role must be non-elevated",
                "F2B_O4_H01_REJECTS_MIGRATOR_AS_RUNTIME");

        // The production feature is intentionally compile-time disabled pending O4/O5.
        startupEnvironment.SetStartup(runtime,publicAccounts:true,dispatch:true);
        using (var enabled = new Factory { Connection = runtime, PublicAccounts = true, Dispatch = true })
            await RejectStartup(enabled, "PUBLIC_ACCOUNT_EMAIL_DISPATCH_NOT_READY",
                "F2B_O4_H01_REJECTS_PREMATURE_PUBLIC_ENABLE");

        startupEnvironment.SetStartup(runtime,publicAccounts:true,dispatch:false);
        using (var noDispatch = new Factory { Connection = runtime, PublicAccounts = true })
            await RejectStartup(noDispatch, "PUBLIC_ACCOUNT_EMAIL_DISPATCH_NOT_READY",
                "F2B_O4_H01_REJECTS_PUBLIC_WITH_DISPATCH_OFF");

        startupEnvironment.SetStartup(runtime,publicAccounts:true,dispatch:true,production:false);
        using (var notProduction = new Factory {
            Connection = runtime, PublicAccounts = true, Dispatch = true, Production = false
        })
            await RejectStartup(notProduction, "PUBLIC_ACCOUNT_EMAIL_DISPATCH_NOT_READY",
                "F2B_O4_H01_REJECTS_NONPRODUCTION_PUBLIC_ENABLE");

        Console.WriteLine("F2B_O4_H01_REAL_TESTSERVER_DEFAULT_OFF=PASS");
        Console.WriteLine("F2B_O4_H01_DISPOSABLE_PG_TLS_VERIFYFULL=PASS");
        Console.WriteLine("F2B_O4_H01_PRODUCTION_GATES_FAIL_CLOSED=PASS");
        Console.WriteLine("F2B_O4_H01_SMTP=NOT_CONNECTED");
        Console.WriteLine("F2B_O4_H01_ENABLED_PUBLIC_HTTP=BLOCKED_BY_RELEASE_POLICY");
        Console.WriteLine("F2B_O4_H01_CT164=NOT_ACCESSED");
    }
}
