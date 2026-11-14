using System.Text.RegularExpressions;

namespace YangSpace.Api;

public sealed record LocationInput(
    string? Slug,
    string? Name,
    string? Neighborhood,
    string? Category,
    string? Address,
    string? Description,
    string? Phone,
    string? Website,
    string? ImageUrl,
    string? OpeningHours,
    bool IsPublished);

public sealed record OfferInput(string? Title, string? Description, DateTime StartsAtUtc, DateTime EndsAtUtc, bool IsPublished);
public sealed record BrochureInput(string? Title, string? Url, DateTime ValidFromUtc, DateTime ValidToUtc, bool IsPublished);
public sealed record JobInput(string? Title, string? Description, string? ApplyUrl, DateTime ExpiresAtUtc, bool IsPublished);

public sealed record LocationCard(
    Guid Id, string Slug, string Name, string Neighborhood, string Category,
    string Description, string Address, string? ImageUrl, bool IsVerified, int ActiveOffers, int ActiveJobs);

public sealed record LocationDetail(
    Guid Id, string Slug, string Name, string City, string Neighborhood, string Category,
    string Description, string Address, string? Phone, string? Website, string? ImageUrl,
    string? OpeningHours, bool IsVerified, DateTime UpdatedAtUtc,
    IReadOnlyList<LocalOffer> Offers, IReadOnlyList<LocalBrochure> Brochures, IReadOnlyList<LocalJob> Jobs);

public sealed record Paged<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

internal static class InputChecks
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public static bool ShortText(string? value, int max, int min = 1) => value is not null && value.Trim().Length >= min && value.Trim().Length <= max;
    public static bool OptionalText(string? value, int max) => value is null || value.Trim().Length <= max;
    public static bool Slug(string? value) => value is not null && value.Length <= 90 && SlugPattern.IsMatch(value);
    public static bool HttpsUrl(string? value, bool allowEmpty = true)
        => string.IsNullOrWhiteSpace(value) ? allowEmpty : Uri.TryCreate(value, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps && url.UserInfo.Length == 0;
    public static bool ImageUrl(string? value) => string.IsNullOrWhiteSpace(value) || (value.StartsWith("/assets/", StringComparison.Ordinal) && !value.Contains("..", StringComparison.Ordinal)) || HttpsUrl(value);
    public static bool Dates(DateTime start, DateTime end) => start.Year >= 2020 && start.Year <= 9990 && start < end && end <= start.AddYears(2);
    public static bool Location(LocationInput x) => Slug(x.Slug)
        && ShortText(x.Name, 120, 2) && ShortText(x.Neighborhood, 90)
        && ShortText(x.Category, 80) && ShortText(x.Address, 240)
        && OptionalText(x.Description, 3000) && OptionalText(x.Phone, 40)
        && HttpsUrl(x.Website) && ImageUrl(x.ImageUrl) && OptionalText(x.OpeningHours, 250);
    public static void Apply(LocationInput x, BusinessLocation target)
    {
        target.Slug = x.Slug!.Trim();
        target.Name = x.Name!.Trim();
        target.Neighborhood = x.Neighborhood!.Trim();
        target.Category = x.Category!.Trim();
        target.Address = x.Address!.Trim();
        target.Description = x.Description?.Trim() ?? "";
        target.Phone = x.Phone?.Trim();
        target.Website = x.Website?.Trim();
        target.ImageUrl = x.ImageUrl?.Trim();
        target.OpeningHours = x.OpeningHours?.Trim();
        target.IsPublished = x.IsPublished;
        target.UpdatedAtUtc = DateTime.UtcNow;
    }
}
