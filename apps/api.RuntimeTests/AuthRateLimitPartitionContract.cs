using System.Net;
using Microsoft.AspNetCore.Http;
using YangSpace.Api;

internal static class AuthRateLimitPartitionContract
{
    private static void Check(bool ok, string label)
    {
        if (!ok)
            throw new InvalidOperationException("F2B_AUTH_RATE_FAILED=" + label);
        Console.WriteLine(label + "=PASS");
    }

    private static HttpContext Context(string? remoteIp, string? forgedForwardedFor = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress =
            remoteIp is null ? null : IPAddress.Parse(remoteIp);
        if (forgedForwardedFor is not null)
            context.Request.Headers["X-Forwarded-For"] = forgedForwardedFor;
        return context;
    }

    internal static void Run()
    {
        var clientA = Context("192.0.2.10");
        var clientB = Context("192.0.2.11");
        var forged = Context("192.0.2.10", "198.51.100.99");
        var mapped = Context("::ffff:192.0.2.10");
        var ipv6 = Context("2001:db8::10");
        var unknown = Context(null, "198.51.100.99");

        Check(AuthRateLimitPolicy.PartitionKey(clientA) !=
              AuthRateLimitPolicy.PartitionKey(clientB),
              "F2B_AUTH_RATE_CLIENT_ISOLATION");
        Check(AuthRateLimitPolicy.PartitionKey(clientA) ==
              AuthRateLimitPolicy.PartitionKey(mapped),
              "F2B_AUTH_RATE_MAPPED_IPV4_NORMALIZED");
        Check(AuthRateLimitPolicy.PartitionKey(clientA) ==
              AuthRateLimitPolicy.PartitionKey(forged),
              "F2B_AUTH_RATE_FORGED_FORWARD_HEADER_IGNORED");
        Check(AuthRateLimitPolicy.PartitionKey(unknown) == "unknown",
              "F2B_AUTH_RATE_UNKNOWN_PEER_FAIL_CLOSED");
        Check(AuthRateLimitPolicy.PartitionKey(ipv6) == "2001:db8::10",
              "F2B_AUTH_RATE_IPV6_KEY");
        Check(AuthRateLimitPolicy.RequestsPerMinute == 12,
              "F2B_AUTH_RATE_LIMIT_PRESERVED");
        Check(AuthRateLimitPolicy.CreatePartition(clientA).PartitionKey ==
              AuthRateLimitPolicy.CreatePartition(mapped).PartitionKey,
              "F2B_AUTH_RATE_PARTITION_KEY_STABLE");

        Console.WriteLine("F2B_AUTH_RATE_PARTITION_CONTRACT=PASS");
        Console.WriteLine("F2B_AUTH_RATE_PROXY_FORWARDING=NOT_TESTED");
        Console.WriteLine("F2B_AUTH_RATE_EXTERNAL_ABUSE=NOT_TESTED");
        Console.WriteLine("CT164_DB=NOT_ACCESSED");
    }
}
