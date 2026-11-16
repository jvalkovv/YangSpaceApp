using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using YangSpace.Api;

internal static class AuthRateLimitHttpContract
{
    private const string PeerHeader = "X-YangSpace-Test-Transport-Peer";

    private static void Check(bool condition, string marker)
    {
        if (!condition)
            throw new InvalidOperationException("F2B_H03_AUTH_RATE_HTTP_FAILED=" + marker);
        Console.WriteLine(marker + "=PASS");
    }

    internal static async Task RunAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy<string>("auth", AuthRateLimitPolicy.CreatePartition);
        });

        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            var rawPeer = context.Request.Headers[PeerHeader].ToString();
            if (string.Equals(rawPeer, "unknown", StringComparison.Ordinal))
                context.Connection.RemoteIpAddress = null;
            else if (IPAddress.TryParse(rawPeer, out var address))
                context.Connection.RemoteIpAddress = address;
            else
                throw new InvalidOperationException("F2B_H03_TEST_PEER_INVALID");

            await next();
        });
        app.UseRouting();
        app.UseRateLimiter();
        app.MapGet("/auth", () => Results.Ok()).RequireRateLimiting("auth");

        await app.StartAsync();
        using var client = app.GetTestClient();

        static async Task<HttpStatusCode> SendAsync(
            HttpClient client, string peer, string? forgedForwardedFor = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/auth");
            request.Headers.Add(PeerHeader, peer);
            if (forgedForwardedFor is not null)
                request.Headers.TryAddWithoutValidation("X-Forwarded-For", forgedForwardedFor);
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        for (var i = 0; i < AuthRateLimitPolicy.RequestsPerMinute; i++)
            Check(await SendAsync(client, "192.0.2.10", "198.51.100.99") == HttpStatusCode.OK,
                "F2B_H03_AUTH_RATE_CLIENT_A_WITH_FORGED_XFF");
        Check(await SendAsync(client, "192.0.2.10", "198.51.100.99") ==
              HttpStatusCode.TooManyRequests,
            "F2B_H03_AUTH_RATE_HTTP_429_AFTER_TWELVE");

        Check(await SendAsync(client, "192.0.2.11", "192.0.2.10") == HttpStatusCode.OK,
            "F2B_H03_AUTH_RATE_CLIENT_B_INDEPENDENT_BUDGET");

        Check(await SendAsync(client, "::ffff:192.0.2.10", "192.0.2.11") ==
              HttpStatusCode.TooManyRequests,
            "F2B_H03_AUTH_RATE_MAPPED_IPV4_SAME_BUCKET");

        for (var i = 0; i < AuthRateLimitPolicy.RequestsPerMinute; i++)
            Check(await SendAsync(client, "unknown", "203.0.113.55") == HttpStatusCode.OK,
                "F2B_H03_AUTH_RATE_UNKNOWN_PEER_FAIL_CLOSED_BUCKET");
        Check(await SendAsync(client, "unknown", "203.0.113.55") ==
              HttpStatusCode.TooManyRequests,
            "F2B_H03_AUTH_RATE_UNKNOWN_PEER_LIMITED");

        Console.WriteLine("F2B_H03_AUTH_RATE_REAL_MIDDLEWARE=PASS");
        Console.WriteLine("F2B_H03_AUTH_RATE_FORWARDED_HEADER_NOT_TRUSTED=PASS");
        Console.WriteLine("F2B_H03_AUTH_RATE_TESTSERVER_ONLY=PASS");
    }
}
