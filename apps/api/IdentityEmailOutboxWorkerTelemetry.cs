using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace YangSpace.Api;

internal sealed record IdentityEmailOutboxWorkerSnapshot(
    bool DispatchEnabled,
    DateTime? LastCycleStartedAtUtc,
    DateTime? LastCycleSucceededAtUtc,
    string? LastCycleFailureCategory,
    int ConsecutiveFailures,
    long TotalCycles,
    long SuccessfulCycles,
    long FailedCycles,
    long TotalClaimed,
    long TotalSent,
    long TotalRetried,
    long TotalDeadLettered,
    long TotalSuppressed,
    long TotalLeaseLost,
    long TotalClaimConflicts,
    long TotalTransportFailures,
    long TotalPurged,
    long Pending,
    long Due,
    long Leased,
    long DeadLetter);

/// <summary>
/// Process-local, low-cardinality worker telemetry. No user, outbox, email,
/// token, lease or exception values are accepted by this type.
/// </summary>
internal sealed class IdentityEmailOutboxWorkerTelemetry : IDisposable
{
    internal const string MeterName = "YangSpace.IdentityEmailOutbox.Worker";

    private readonly Meter _meter = new(MeterName, "1.0.0");
    private readonly Counter<long> _cycles;
    private readonly Counter<long> _messages;
    private readonly Histogram<double> _cycleDuration;
    private long _dispatchEnabled;
    private long _lastCycleStartedTicks;
    private long _lastCycleSucceededTicks;
    private long _totalCycles;
    private long _successfulCycles;
    private long _failedCycles;
    private long _totalClaimed;
    private long _totalSent;
    private long _totalRetried;
    private long _totalDeadLettered;
    private long _totalSuppressed;
    private long _totalLeaseLost;
    private long _totalClaimConflicts;
    private long _totalTransportFailures;
    private long _totalPurged;
    private long _pending;
    private long _due;
    private long _leased;
    private long _deadLetter;
    private int _consecutiveFailures;
    private string? _lastCycleFailureCategory;

    public IdentityEmailOutboxWorkerTelemetry()
    {
        _cycles = _meter.CreateCounter<long>(
            "yangspace.identity_email_outbox.worker.cycles", "{cycle}");
        _messages = _meter.CreateCounter<long>(
            "yangspace.identity_email_outbox.messages", "{message}");
        _cycleDuration = _meter.CreateHistogram<double>(
            "yangspace.identity_email_outbox.worker.cycle.duration", "ms");

        _meter.CreateObservableGauge<long>(
            "yangspace.identity_email_outbox.pending", () => Interlocked.Read(ref _pending), "{message}");
        _meter.CreateObservableGauge<long>(
            "yangspace.identity_email_outbox.due", () => Interlocked.Read(ref _due), "{message}");
        _meter.CreateObservableGauge<long>(
            "yangspace.identity_email_outbox.leased", () => Interlocked.Read(ref _leased), "{message}");
        _meter.CreateObservableGauge<long>(
            "yangspace.identity_email_outbox.dead_letter", () => Interlocked.Read(ref _deadLetter), "{message}");
        _meter.CreateObservableGauge<long>(
            "yangspace.identity_email_outbox.worker.consecutive_failures",
            () => Volatile.Read(ref _consecutiveFailures), "{failure}");
        _meter.CreateObservableGauge<long>(
            "yangspace.identity_email_outbox.worker.last_success_unix_seconds",
            () => Interlocked.Read(ref _lastCycleSucceededTicks) == 0
                ? 0
                : new DateTimeOffset(new DateTime(
                    Interlocked.Read(ref _lastCycleSucceededTicks), DateTimeKind.Utc)).ToUnixTimeSeconds(), "s");
    }

    internal void SetDispatchEnabled(bool enabled) =>
        Interlocked.Exchange(ref _dispatchEnabled, enabled ? 1 : 0);

    internal void RecordCycleStarted(DateTime nowUtc)
    {
        Interlocked.Increment(ref _totalCycles);
        Interlocked.Exchange(ref _lastCycleStartedTicks, nowUtc.ToUniversalTime().Ticks);
    }

    internal void RecordCycleSucceeded(DateTime nowUtc)
    {
        Interlocked.Increment(ref _successfulCycles);
        Interlocked.Exchange(ref _lastCycleSucceededTicks, nowUtc.ToUniversalTime().Ticks);
        Interlocked.Exchange(ref _consecutiveFailures, 0);
        Volatile.Write(ref _lastCycleFailureCategory, null);
        var tags = new TagList { { "outcome", "success" } };
        _cycles.Add(1, tags);
    }

    internal void RecordCycleFailed(string category)
    {
        Interlocked.Increment(ref _failedCycles);
        Interlocked.Increment(ref _consecutiveFailures);
        Volatile.Write(ref _lastCycleFailureCategory, category);
        var tags = new TagList
        {
            { "outcome", "failure" },
            { "failure_category", category }
        };
        _cycles.Add(1, tags);
    }

    internal void RecordCycleDuration(TimeSpan duration) =>
        _cycleDuration.Record(duration.TotalMilliseconds);

    internal void RecordClaimed(long count) => AddMessage(count, "claimed", ref _totalClaimed);
    internal void RecordSent(long count) => AddMessage(count, "sent", ref _totalSent);
    internal void RecordRetried(long count) => AddMessage(count, "retry", ref _totalRetried);
    internal void RecordDeadLettered(long count) => AddMessage(count, "dead_letter", ref _totalDeadLettered);
    internal void RecordSuppressed(long count) => AddMessage(count, "suppressed", ref _totalSuppressed);
    internal void RecordLeaseLost(long count) => AddMessage(count, "lease_lost", ref _totalLeaseLost);
    internal void RecordClaimConflicts(long count) => AddMessage(count, "claim_conflict", ref _totalClaimConflicts);
    internal void RecordTransportFailures(long count) => AddMessage(count, "transport_failure", ref _totalTransportFailures);
    internal void RecordPurged(long count) => AddMessage(count, "purged", ref _totalPurged);

    internal void SetBacklog(long pending, long due, long leased, long deadLetter)
    {
        Interlocked.Exchange(ref _pending, pending);
        Interlocked.Exchange(ref _due, due);
        Interlocked.Exchange(ref _leased, leased);
        Interlocked.Exchange(ref _deadLetter, deadLetter);
    }

    internal IdentityEmailOutboxWorkerSnapshot Snapshot() => new(
        Interlocked.Read(ref _dispatchEnabled) == 1,
        ReadUtc(Interlocked.Read(ref _lastCycleStartedTicks)),
        ReadUtc(Interlocked.Read(ref _lastCycleSucceededTicks)),
        Volatile.Read(ref _lastCycleFailureCategory),
        Volatile.Read(ref _consecutiveFailures),
        Interlocked.Read(ref _totalCycles),
        Interlocked.Read(ref _successfulCycles),
        Interlocked.Read(ref _failedCycles),
        Interlocked.Read(ref _totalClaimed),
        Interlocked.Read(ref _totalSent),
        Interlocked.Read(ref _totalRetried),
        Interlocked.Read(ref _totalDeadLettered),
        Interlocked.Read(ref _totalSuppressed),
        Interlocked.Read(ref _totalLeaseLost),
        Interlocked.Read(ref _totalClaimConflicts),
        Interlocked.Read(ref _totalTransportFailures),
        Interlocked.Read(ref _totalPurged),
        Interlocked.Read(ref _pending),
        Interlocked.Read(ref _due),
        Interlocked.Read(ref _leased),
        Interlocked.Read(ref _deadLetter));

    private void AddMessage(long count, string outcome, ref long total)
    {
        if (count <= 0) return;
        Interlocked.Add(ref total, count);
        var tags = new TagList { { "outcome", outcome } };
        _messages.Add(count, tags);
    }

    private static DateTime? ReadUtc(long ticks) =>
        ticks <= 0 ? null : new DateTime(ticks, DateTimeKind.Utc);

    public void Dispose() => _meter.Dispose();
}
