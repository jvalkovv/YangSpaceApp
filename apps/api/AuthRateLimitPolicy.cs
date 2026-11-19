using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace YangSpace.Api;

/// <summary>
/// Split the auth budget by transport peer IP. Do not trust raw forwarded
/// headers. Production ingress must independently configure sanitized and
/// trusted forwarding BEFORE these keys correspond to external client IPs.
/// </summary>
internal static class AuthRateLimitPolicy
{
    internal const int RequestsPerMinute = 12;

    internal static string PartitionKey(HttpContext context)
    {
        IPAddress? remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is null)
            return "unknown";

        if (remoteIp.IsIPv4MappedToIPv6)
            remoteIp = remoteIp.MapToIPv4();

        return remoteIp.ToString();
    }

    internal static RateLimitPartition<string> CreatePartition(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: PartitionKey(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = RequestsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
}
