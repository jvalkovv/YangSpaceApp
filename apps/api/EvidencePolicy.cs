using System.Text;

namespace YangSpace.Api;

public enum EvidenceFileRejectionReason
{
    None = 0,
    EmptyContent,
    InvalidFileName,
    UnsupportedExtension,
    UnsupportedContentType,
    SignatureMismatch,
    FileTooLarge,
    ClaimFileCountExceeded,
    ClaimTotalSizeExceeded,
    PasswordProtectedPdf,
    InvalidExistingClaimState
}

public sealed record EvidenceFileValidationRequest(
    string FileName,
    string ContentType,
    ReadOnlyMemory<byte> Content,
    int ExistingFileCount,
    long ExistingTotalBytes);

public sealed record EvidenceFileValidationResult(
    bool IsValid,
    EvidenceFileRejectionReason Reason)
{
    public static EvidenceFileValidationResult Accept() =>
        new(true, EvidenceFileRejectionReason.None);

    public static EvidenceFileValidationResult Reject(EvidenceFileRejectionReason reason) =>
        new(false, reason);
}

/// <summary>
/// Pure validation for the approved evidence allowlist. This class does not
/// read/write files, persist metadata, scan malware or expose an HTTP endpoint.
/// </summary>
public static class EvidenceFilePolicy
{
    public const int MaxFileBytes = 10 * 1024 * 1024;
    public const int MaxFilesPerClaim = 5;
    public const long MaxClaimBytes = 25L * 1024 * 1024;

    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static EvidenceFileValidationResult Validate(EvidenceFileValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ExistingFileCount < 0 || request.ExistingTotalBytes < 0)
            return EvidenceFileValidationResult.Reject(
                EvidenceFileRejectionReason.InvalidExistingClaimState);

        if (string.IsNullOrWhiteSpace(request.FileName) ||
            request.FileName.Length > 128 ||
            request.FileName.Contains('/') ||
            request.FileName.Contains('\\') ||
            request.FileName.IndexOfAny(['\0', '\r', '\n']) >= 0)
            return EvidenceFileValidationResult.Reject(
                EvidenceFileRejectionReason.InvalidFileName);

        var extension = Path.GetExtension(request.FileName);
        var expectedContentType = extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => null
        };

        if (expectedContentType is null)
            return EvidenceFileValidationResult.Reject(
                EvidenceFileRejectionReason.UnsupportedExtension);

        if (!string.Equals(
                request.ContentType?.Trim(),
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
            return EvidenceFileValidationResult.Reject(
                EvidenceFileRejectionReason.UnsupportedContentType);

        var content = request.Content.Span;
        if (content.IsEmpty)
            return EvidenceFileValidationResult.Reject(
                EvidenceFileRejectionReason.EmptyContent);

        if (content.Length > MaxFileBytes)
            return EvidenceFileValidationResult.Reject(
                EvidenceFileRejectionReason.FileTooLarge);

        if (request.ExistingFileCount >= MaxFilesPerClaim)
            return EvidenceFileValidationResult.Reject(
                EvidenceFileRejectionReason.ClaimFileCountExceeded);

        if (request.ExistingTotalBytes > MaxClaimBytes - content.Length)
            return EvidenceFileValidationResult.Reject(
                EvidenceFileRejectionReason.ClaimTotalSizeExceeded);

        var signatureMatches = extension.ToLowerInvariant() switch
        {
            ".pdf" => HasPdfSignature(content),
            ".jpg" or ".jpeg" => HasJpegSignature(content),
            ".png" => content.StartsWith(PngSignature),
            _ => false
        };

        if (!signatureMatches)
            return EvidenceFileValidationResult.Reject(
                EvidenceFileRejectionReason.SignatureMismatch);

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) &&
            Encoding.ASCII.GetString(content).Contains("/Encrypt", StringComparison.Ordinal))
            return EvidenceFileValidationResult.Reject(
                EvidenceFileRejectionReason.PasswordProtectedPdf);

        return EvidenceFileValidationResult.Accept();
    }

    private static bool HasPdfSignature(ReadOnlySpan<byte> content) =>
        content.Length >= 5 &&
        content[..5].SequenceEqual("%PDF-"u8);

    private static bool HasJpegSignature(ReadOnlySpan<byte> content) =>
        content.Length >= 3 &&
        content[0] == 0xFF &&
        content[1] == 0xD8 &&
        content[2] == 0xFF;
}

public enum EvidenceRetentionKind
{
    PendingClaimRaw = 0,
    RejectedClaimRaw,
    ApprovedClaimRaw,
    SupersededRaw,
    EvidenceAccessLog,
    ClaimDecisionMetadata,
    EncryptedBackupObject
}

public sealed record EvidenceRetentionFacts(
    DateTimeOffset? LastMeaningfulActivityAtUtc = null,
    DateTimeOffset? FinalDecisionAtUtc = null,
    DateTimeOffset? ApprovedAtUtc = null,
    DateTimeOffset? LastReverifiedAtUtc = null,
    DateTimeOffset? ReplacementAcceptedAtUtc = null,
    DateTimeOffset? AccessEventAtUtc = null,
    DateTimeOffset? ClaimClosedAtUtc = null,
    DateTimeOffset? PrimaryObjectDeletedAtUtc = null,
    bool ClaimIsActive = false);

public sealed record EvidenceRetentionSchedule(
    DateTimeOffset DeleteEligibleAtUtc,
    DateTimeOffset DeleteByUtc,
    DateTimeOffset? VerificationStaleAtUtc = null);

/// <summary>
/// Deterministic retention calculator. It computes deadlines only; it does not
/// schedule deletion, access storage, create legal holds or mutate records.
/// All timestamps supplied to it must be UTC.
/// </summary>
public static class EvidenceRetentionPolicy
{
    public static EvidenceRetentionSchedule? Calculate(
        EvidenceRetentionKind kind,
        EvidenceRetentionFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        return kind switch
        {
            EvidenceRetentionKind.PendingClaimRaw =>
                Schedule(Require(facts.LastMeaningfulActivityAtUtc, nameof(facts.LastMeaningfulActivityAtUtc))
                    .AddDays(90), deletionGraceDays: 30),

            EvidenceRetentionKind.RejectedClaimRaw =>
                Schedule(Require(facts.FinalDecisionAtUtc, nameof(facts.FinalDecisionAtUtc))
                    .AddDays(180), deletionGraceDays: 30),

            EvidenceRetentionKind.ApprovedClaimRaw =>
                ApprovedSchedule(facts),

            EvidenceRetentionKind.SupersededRaw =>
                Schedule(Require(facts.ReplacementAcceptedAtUtc, nameof(facts.ReplacementAcceptedAtUtc))
                    .AddDays(30), deletionGraceDays: 0),

            EvidenceRetentionKind.EvidenceAccessLog =>
                Schedule(Require(facts.AccessEventAtUtc, nameof(facts.AccessEventAtUtc))
                    .AddMonths(12), deletionGraceDays: 0),

            EvidenceRetentionKind.ClaimDecisionMetadata =>
                facts.ClaimIsActive
                    ? null
                    : Schedule(Require(facts.ClaimClosedAtUtc, nameof(facts.ClaimClosedAtUtc))
                        .AddMonths(24), deletionGraceDays: 0),

            EvidenceRetentionKind.EncryptedBackupObject =>
                Schedule(Require(facts.PrimaryObjectDeletedAtUtc, nameof(facts.PrimaryObjectDeletedAtUtc))
                    .AddDays(35), deletionGraceDays: 0),

            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown evidence retention kind.")
        };
    }

    private static EvidenceRetentionSchedule ApprovedSchedule(EvidenceRetentionFacts facts)
    {
        var approvedAt = Require(facts.ApprovedAtUtc, nameof(facts.ApprovedAtUtc));
        var reverifiedAt = facts.LastReverifiedAtUtc is { } suppliedReverification
            ? Require(suppliedReverification, nameof(facts.LastReverifiedAtUtc))
            : approvedAt;
        var baseline = reverifiedAt > approvedAt ? reverifiedAt : approvedAt;
        var staleAt = baseline.AddMonths(12);
        var schedule = Schedule(staleAt, deletionGraceDays: 30);
        return schedule with { VerificationStaleAtUtc = staleAt };
    }

    private static EvidenceRetentionSchedule Schedule(
        DateTimeOffset eligibleAt,
        int deletionGraceDays) =>
        new(eligibleAt, eligibleAt.AddDays(deletionGraceDays));

    private static DateTimeOffset Require(DateTimeOffset? value, string name)
    {
        if (value is not { } timestamp)
            throw new ArgumentException($"Required UTC timestamp is missing: {name}.");

        if (timestamp.Offset != TimeSpan.Zero)
            throw new ArgumentException($"Timestamp must use UTC offset zero: {name}.");

        return timestamp;
    }
}

/// <summary>
/// Contract only. No scanner implementation or external processor is wired.
/// A future intake service must refuse to proceed if no approved scanner exists.
/// </summary>
public interface IEvidenceMalwareScanner
{
    Task<EvidenceMalwareScanResult> ScanAsync(
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default);
}

public enum EvidenceMalwareScanResult
{
    Clean = 0,
    ThreatDetected,
    Indeterminate
}

public static class EvidenceMalwareScannerGuard
{
    public static IEvidenceMalwareScanner RequireConfigured(IEvidenceMalwareScanner? scanner) =>
        scanner ?? throw new InvalidOperationException("EVIDENCE_MALWARE_SCANNER_NOT_CONFIGURED");
}
