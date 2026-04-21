namespace VinhKhanhGuide.Core.Models;

public enum AutoNarrationDecisionReason
{
    Selected,
    NoCandidate,
    Debounced,
    CurrentNarrationInProgress,
    StableInsideCurrentPoi,
    CooldownActive
}

public sealed record AutoNarrationPoiEvaluation(
    POI Poi,
    double DistanceMeters,
    bool IsInsideTriggerRadius);

public sealed record AutoNarrationCandidate(
    POI Poi,
    double DistanceMeters);

public sealed record AutoNarrationDecisionOptions
{
    public static AutoNarrationDecisionOptions Default { get; } = new();

    public TimeSpan DebounceInterval { get; init; } = TimeSpan.FromSeconds(2);
    public double SamePrioritySwitchThresholdMeters { get; init; } = 8d;
}

public sealed record AutoNarrationDecisionInput
{
    public IReadOnlyList<AutoNarrationCandidate> Candidates { get; init; } =
        Array.Empty<AutoNarrationCandidate>();

    public Guid? CurrentPoiId { get; init; }
    public Guid? ActiveNarrationPoiId { get; init; }
    public Guid? LastAutoNarratedPoiId { get; init; }
    public bool IsNarrationInProgress { get; init; }
    public DateTimeOffset? LastEvaluationAtUtc { get; init; }
    public DateTimeOffset NowUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyCollection<Guid> PreviousInsidePoiIds { get; init; } =
        Array.Empty<Guid>();

    public IReadOnlyDictionary<Guid, DateTimeOffset> LastNarratedAtUtc { get; init; } =
        new Dictionary<Guid, DateTimeOffset>();

    public AutoNarrationDecisionOptions Options { get; init; } =
        AutoNarrationDecisionOptions.Default;
}

public sealed record AutoNarrationDecisionResult
{
    public POI? Poi { get; init; }
    public double? DistanceMeters { get; init; }
    public AutoNarrationDecisionReason Reason { get; init; }
    public bool ShouldNarrate { get; init; }
    public bool ShouldUpdateEvaluationTimestamp =>
        Reason != AutoNarrationDecisionReason.Debounced;

    public static AutoNarrationDecisionResult None(AutoNarrationDecisionReason reason)
    {
        return new AutoNarrationDecisionResult
        {
            Reason = reason,
            ShouldNarrate = false
        };
    }

    public static AutoNarrationDecisionResult ForCandidate(
        AutoNarrationCandidate candidate,
        AutoNarrationDecisionReason reason,
        bool shouldNarrate)
    {
        return new AutoNarrationDecisionResult
        {
            Poi = candidate.Poi,
            DistanceMeters = candidate.DistanceMeters,
            Reason = reason,
            ShouldNarrate = shouldNarrate
        };
    }
}
