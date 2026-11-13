using System.Security.Cryptography;
using System.Text;
using YangSpace.Api;

internal static class EvidencePolicyContract
{
    private static void Check(bool condition, string marker)
    {
        if (!condition)
            throw new InvalidOperationException("FAIL: " + marker);
        Console.WriteLine(marker + "=PASS");
    }

    private static void ExpectInvalidOperation(Action action, string expected, string marker)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException exception)
            when (exception.Message.Contains(expected, StringComparison.Ordinal))
        {
            Console.WriteLine(marker + "=PASS");
            return;
        }

        throw new InvalidOperationException("FAIL: " + marker);
    }

    private static void ExpectArgumentException(Action action, string expected, string marker)
    {
        try
        {
            action();
        }
        catch (ArgumentException exception)
            when (exception.Message.Contains(expected, StringComparison.Ordinal))
        {
            Console.WriteLine(marker + "=PASS");
            return;
        }

        throw new InvalidOperationException("FAIL: " + marker);
    }

    private sealed class SyntheticEvidenceStore : IDisposable
    {
        private readonly Dictionary<Guid, byte[]> objects = [];

        internal Guid Put(ReadOnlySpan<byte> content)
        {
            if (content.IsEmpty)
                throw new ArgumentException("Synthetic fixture must not be empty.", nameof(content));

            var id = Guid.NewGuid();
            objects.Add(id, content.ToArray());
            return id;
        }

        internal bool TryRead(Guid id, out byte[] content)
        {
            if (objects.TryGetValue(id, out var stored))
            {
                content = stored.ToArray();
                return true;
            }

            content = [];
            return false;
        }

        internal bool Delete(Guid id)
        {
            if (!objects.Remove(id, out var stored))
                return false;

            CryptographicOperations.ZeroMemory(stored);
            return true;
        }

        public void Dispose()
        {
            foreach (var stored in objects.Values)
                CryptographicOperations.ZeroMemory(stored);
            objects.Clear();
        }
    }

    internal static Task RunAsync()
    {
        ValidateFilePolicy();
        ValidateRetentionPolicy();
        ValidateFailClosedScannerGuard();
        ValidateSyntheticStore();
        Console.WriteLine("F2A3_EVIDENCE_POLICY_CONTRACT=PASS");
        return Task.CompletedTask;
    }

    private static void ValidateFilePolicy()
    {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\nsynthetic fixture");
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        Check(
            EvidenceFilePolicy.Validate(new(
                "synthetic.pdf", "application/pdf", pdf, 0, 0)).IsValid,
            "EVIDENCE_PDF_ACCEPTED");

        Check(
            EvidenceFilePolicy.Validate(new(
                "synthetic.jpg", "image/jpeg", jpeg, 0, 0)).IsValid,
            "EVIDENCE_JPEG_ACCEPTED");

        Check(
            EvidenceFilePolicy.Validate(new(
                "synthetic.png", "image/png", png, 0, 0)).IsValid,
            "EVIDENCE_PNG_ACCEPTED");

        Check(
            EvidenceFilePolicy.Validate(new(
                "synthetic.pdf", "application/pdf", jpeg, 0, 0)).Reason ==
                EvidenceFileRejectionReason.SignatureMismatch,
            "EVIDENCE_EXTENSION_SIGNATURE_MISMATCH_REJECTED");

        Check(
            EvidenceFilePolicy.Validate(new(
                "synthetic.pdf", "image/jpeg", pdf, 0, 0)).Reason ==
                EvidenceFileRejectionReason.UnsupportedContentType,
            "EVIDENCE_MIME_MISMATCH_REJECTED");

        Check(
            EvidenceFilePolicy.Validate(new(
                "synthetic.exe", "application/octet-stream", pdf, 0, 0)).Reason ==
                EvidenceFileRejectionReason.UnsupportedExtension,
            "EVIDENCE_EXECUTABLE_EXTENSION_REJECTED");

        Check(
            EvidenceFilePolicy.Validate(new(
                "../synthetic.pdf", "application/pdf", pdf, 0, 0)).Reason ==
                EvidenceFileRejectionReason.InvalidFileName,
            "EVIDENCE_PATH_TRAVERSAL_NAME_REJECTED");

        Check(
            EvidenceFilePolicy.Validate(new(
                "synthetic.pdf", "application/pdf", ReadOnlyMemory<byte>.Empty, 0, 0)).Reason ==
                EvidenceFileRejectionReason.EmptyContent,
            "EVIDENCE_EMPTY_FILE_REJECTED");

        var encryptedPdf = Encoding.ASCII.GetBytes("%PDF-1.7\ntrailer << /Encrypt 4 0 R >>");
        Check(
            EvidenceFilePolicy.Validate(new(
                "synthetic.pdf", "application/pdf", encryptedPdf, 0, 0)).Reason ==
                EvidenceFileRejectionReason.PasswordProtectedPdf,
            "EVIDENCE_ENCRYPTED_PDF_MARKER_REJECTED");

        var oversized = new byte[EvidenceFilePolicy.MaxFileBytes + 1];
        Encoding.ASCII.GetBytes("%PDF-").CopyTo(oversized, 0);
        Check(
            EvidenceFilePolicy.Validate(new(
                "oversized.pdf", "application/pdf", oversized, 0, 0)).Reason ==
                EvidenceFileRejectionReason.FileTooLarge,
            "EVIDENCE_PER_FILE_LIMIT_ENFORCED");

        Check(
            EvidenceFilePolicy.Validate(new(
                "sixth.pdf", "application/pdf", pdf,
                EvidenceFilePolicy.MaxFilesPerClaim, 0)).Reason ==
                EvidenceFileRejectionReason.ClaimFileCountExceeded,
            "EVIDENCE_CLAIM_FILE_COUNT_LIMIT_ENFORCED");

        Check(
            EvidenceFilePolicy.Validate(new(
                "aggregate.pdf", "application/pdf", pdf,
                1, EvidenceFilePolicy.MaxClaimBytes - pdf.Length + 1)).Reason ==
                EvidenceFileRejectionReason.ClaimTotalSizeExceeded,
            "EVIDENCE_CLAIM_TOTAL_SIZE_LIMIT_ENFORCED");

        Check(
            EvidenceFilePolicy.Validate(new(
                "invalid-state.pdf", "application/pdf", pdf, -1, 0)).Reason ==
                EvidenceFileRejectionReason.InvalidExistingClaimState,
            "EVIDENCE_INVALID_EXISTING_STATE_REJECTED");

        var exactFileLimit = new byte[EvidenceFilePolicy.MaxFileBytes];
        Encoding.ASCII.GetBytes("%PDF-").CopyTo(exactFileLimit, 0);
        Check(
            EvidenceFilePolicy.Validate(new(
                "exact-limit.pdf", "application/pdf", exactFileLimit, 0, 0)).IsValid,
            "EVIDENCE_EXACT_FILE_LIMIT_ACCEPTED");
    }

    private static void ValidateRetentionPolicy()
    {
        var baseline = new DateTimeOffset(2026, 11, 13, 0, 0, 0, TimeSpan.Zero);

        var pending = EvidenceRetentionPolicy.Calculate(
            EvidenceRetentionKind.PendingClaimRaw,
            new(LastMeaningfulActivityAtUtc: baseline));
        Check(
            pending is not null &&
            pending.DeleteEligibleAtUtc == baseline.AddDays(90) &&
            pending.DeleteByUtc == baseline.AddDays(120),
            "EVIDENCE_PENDING_90_PLUS_30_DAYS");

        var rejected = EvidenceRetentionPolicy.Calculate(
            EvidenceRetentionKind.RejectedClaimRaw,
            new(FinalDecisionAtUtc: baseline));
        Check(
            rejected is not null &&
            rejected.DeleteEligibleAtUtc == baseline.AddDays(180) &&
            rejected.DeleteByUtc == baseline.AddDays(210),
            "EVIDENCE_REJECTED_180_PLUS_30_DAYS");

        var approved = EvidenceRetentionPolicy.Calculate(
            EvidenceRetentionKind.ApprovedClaimRaw,
            new(
                ApprovedAtUtc: baseline,
                LastReverifiedAtUtc: baseline.AddMonths(3)));
        var approvedStaleAt = baseline.AddMonths(15);
        Check(
            approved is not null &&
            approved.VerificationStaleAtUtc == approvedStaleAt &&
            approved.DeleteEligibleAtUtc == approvedStaleAt &&
            approved.DeleteByUtc == approvedStaleAt.AddDays(30),
            "EVIDENCE_APPROVED_REVERIFY_12_MONTHS_PLUS_30_DAYS");

        var superseded = EvidenceRetentionPolicy.Calculate(
            EvidenceRetentionKind.SupersededRaw,
            new(ReplacementAcceptedAtUtc: baseline));
        Check(
            superseded is not null &&
            superseded.DeleteEligibleAtUtc == baseline.AddDays(30) &&
            superseded.DeleteByUtc == baseline.AddDays(30),
            "EVIDENCE_SUPERSEDED_30_DAYS");

        var accessLog = EvidenceRetentionPolicy.Calculate(
            EvidenceRetentionKind.EvidenceAccessLog,
            new(AccessEventAtUtc: baseline));
        Check(
            accessLog is not null &&
            accessLog.DeleteEligibleAtUtc == baseline.AddMonths(12) &&
            accessLog.DeleteByUtc == baseline.AddMonths(12),
            "EVIDENCE_ACCESS_LOG_12_MONTHS");

        Check(
            EvidenceRetentionPolicy.Calculate(
                EvidenceRetentionKind.ClaimDecisionMetadata,
                new(ClaimIsActive: true)) is null,
            "EVIDENCE_ACTIVE_CLAIM_METADATA_RETAINED");

        var metadata = EvidenceRetentionPolicy.Calculate(
            EvidenceRetentionKind.ClaimDecisionMetadata,
            new(ClaimClosedAtUtc: baseline, ClaimIsActive: false));
        Check(
            metadata is not null &&
            metadata.DeleteEligibleAtUtc == baseline.AddMonths(24) &&
            metadata.DeleteByUtc == baseline.AddMonths(24),
            "EVIDENCE_DECISION_METADATA_24_MONTHS");

        var backup = EvidenceRetentionPolicy.Calculate(
            EvidenceRetentionKind.EncryptedBackupObject,
            new(PrimaryObjectDeletedAtUtc: baseline));
        Check(
            backup is not null &&
            backup.DeleteEligibleAtUtc == baseline.AddDays(35) &&
            backup.DeleteByUtc == baseline.AddDays(35),
            "EVIDENCE_BACKUP_MAX_35_DAYS");

        ExpectArgumentException(
            () => EvidenceRetentionPolicy.Calculate(
                EvidenceRetentionKind.PendingClaimRaw,
                new()),
            "LastMeaningfulActivityAtUtc",
            "EVIDENCE_MISSING_RETENTION_ANCHOR_FAILS");

        ExpectArgumentException(
            () => EvidenceRetentionPolicy.Calculate(
                EvidenceRetentionKind.PendingClaimRaw,
                new(LastMeaningfulActivityAtUtc:
                    new DateTimeOffset(2026, 11, 13, 0, 0, 0, TimeSpan.FromHours(2)))),
            "must use UTC offset zero",
            "EVIDENCE_NON_UTC_RETENTION_ANCHOR_REJECTED");
    }

    private static void ValidateFailClosedScannerGuard()
    {
        ExpectInvalidOperation(
            () => EvidenceMalwareScannerGuard.RequireConfigured(null),
            "EVIDENCE_MALWARE_SCANNER_NOT_CONFIGURED",
            "EVIDENCE_SCANNER_MISSING_FAILS_CLOSED");

        EvidenceMalwareScannerGuard.RequireClean(EvidenceMalwareScanResult.Clean);
        Console.WriteLine("EVIDENCE_SCANNER_CLEAN_ACCEPTED=PASS");
        ExpectInvalidOperation(
            () => EvidenceMalwareScannerGuard.RequireClean(EvidenceMalwareScanResult.ThreatDetected),
            "EVIDENCE_MALWARE_SCAN_NOT_CLEAN",
            "EVIDENCE_SCANNER_THREAT_FAILS_CLOSED");
        ExpectInvalidOperation(
            () => EvidenceMalwareScannerGuard.RequireClean(EvidenceMalwareScanResult.Indeterminate),
            "EVIDENCE_MALWARE_SCAN_NOT_CLEAN",
            "EVIDENCE_SCANNER_INDETERMINATE_FAILS_CLOSED");
    }

    private static void ValidateSyntheticStore()
    {
        using var store = new SyntheticEvidenceStore();
        var fixture = Encoding.ASCII.GetBytes("%PDF-1.7\nsynthetic-only");
        var id = store.Put(fixture);

        Check(store.TryRead(id, out var loaded) && loaded.SequenceEqual(fixture),
            "EVIDENCE_SYNTHETIC_STORE_ROUNDTRIP");
        Check(store.Delete(id), "EVIDENCE_SYNTHETIC_STORE_DELETE");
        Check(!store.TryRead(id, out _), "EVIDENCE_SYNTHETIC_STORE_NOT_FOUND_AFTER_DELETE");
    }
}
