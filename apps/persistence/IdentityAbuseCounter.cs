namespace YangSpace.Api;

/// <summary>
/// One fixed-window counter for a keyed Identity operation. KeyHash is an
/// HMAC-SHA256 digest of the normalized account identifier; raw email is never stored.
/// </summary>
public sealed class IdentityAbuseCounter
{
    public string Operation { get; set; } = "";
    public string KeyHash { get; set; } = "";
    public DateTime WindowStartUtc { get; set; }
    public int Count { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
