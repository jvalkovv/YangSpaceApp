using System.Net;
using System.Security.Cryptography;

namespace YangSpace.Api;

/// <summary>
/// Defense-in-depth for the operator-authenticated internal reverse-proxy route.
/// Nginx must remove any client-supplied gateway header, verify operator access,
/// and insert the protected value only on its approved admin upstream routes.
/// The header is NOT a substitute for identity roles or Cloudflare/edge access.
/// </summary>
internal static class ProductionAdminAccess
{
    internal const string HeaderName = "X-YangSpace-Internal-Gateway";
    internal const string SecretEnvironmentName = "Admin__GatewaySecret";
    private const int SecretLength = 32;

    internal static byte[] RequireSecret()
    {
        // Refuse a checked-in appsettings.json value: only the protected process
        // environment may provide this material to the application.
        var raw = Environment.GetEnvironmentVariable(SecretEnvironmentName);
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Production CMS requires a protected gateway secret in the process environment.");

        byte[] secret;
        try { secret = Convert.FromBase64String(raw); }
        catch (FormatException)
        {
            throw new InvalidOperationException("Production CMS gateway secret must be valid Base64.");
        }
        if (secret.Length != SecretLength)
            throw new InvalidOperationException("Production CMS gateway secret must contain exactly 32 random bytes.");

        return secret;
    }

    internal static bool Accept(HttpContext context, byte[] expected)
    {
        // Do not trust X-Forwarded-For / CF-Connecting-IP here. The actual TCP
        // peer must be a loopback proxy in the SAME application container.
        var peer = context.Connection.RemoteIpAddress;
        if (peer is null) return false;
        if (peer.IsIPv4MappedToIPv6) peer = peer.MapToIPv4();
        if (!IPAddress.IsLoopback(peer)) return false;

        if (!context.Request.Headers.TryGetValue(HeaderName, out var values) || values.Count != 1)
            return false;
        var raw = values[0];
        if (raw is null || raw.Length != 44) return false; // exact Base64 length for 32 bytes
        try
        {
            var supplied = Convert.FromBase64String(raw);
            return supplied.Length == expected.Length &&
                CryptographicOperations.FixedTimeEquals(supplied, expected);
        }
        catch (FormatException) { return false; }
    }

    internal static async ValueTask<object?> RequireGateway(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next, byte[] expected)
    {
        if (!Accept(context.HttpContext, expected))
            return Results.NotFound();
        return await next(context);
    }
}
