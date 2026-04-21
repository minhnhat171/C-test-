using VinhKhanhGuide.Core.Models;
using VinhKhanhGuide.Core.Services;
using Xunit;

namespace VinhKhanhGuide.Core.Tests;

public class PoiAutoNarrationDecisionServiceTests
{
    private static readonly DateTimeOffset NowUtc =
        new(2026, 4, 21, 10, 0, 0, TimeSpan.Zero);

    private static readonly AutoNarrationDecisionOptions TestOptions = new()
    {
        DebounceInterval = TimeSpan.FromSeconds(2),
        SamePrioritySwitchThresholdMeters = 8d
    };

    private readonly PoiAutoNarrationDecisionService _service = new();

    [Fact]
    public void NoCandidate_ReturnsNone()
    {
        var poi = CreatePoi(narrationText: string.Empty);
        var candidates = _service.CreateCandidates(
            [CreateEvaluation(poi, distanceMeters: 10)],
            languageCode: "vi");

        var result = Decide(candidates);

        Assert.Empty(candidates);
        Assert.Null(result.Poi);
        Assert.False(result.ShouldNarrate);
        Assert.Equal(AutoNarrationDecisionReason.NoCandidate, result.Reason);
    }

    [Fact]
    public void OneCandidate_SelectsThatPoi()
    {
        var poi = CreatePoi(name: "Quan A");
        var candidates = _service.CreateCandidates(
            [CreateEvaluation(poi, distanceMeters: 10)],
            languageCode: "vi");

        var result = Decide(candidates);

        Assert.Equal(poi.Id, result.Poi?.Id);
        Assert.True(result.ShouldNarrate);
        Assert.Equal(10, result.DistanceMeters);
        Assert.Equal(AutoNarrationDecisionReason.Selected, result.Reason);
    }

    [Fact]
    public void HigherPriorityPoi_Wins()
    {
        var lowPriority = CreatePoi(name: "Quan gan hon", priority: 1);
        var highPriority = CreatePoi(name: "Quan uu tien", priority: 3);
        var candidates = _service.CreateCandidates(
            [
                CreateEvaluation(lowPriority, distanceMeters: 8),
                CreateEvaluation(highPriority, distanceMeters: 30)
            ],
            languageCode: "vi");

        var result = Decide(candidates);

        Assert.Equal(highPriority.Id, result.Poi?.Id);
        Assert.True(result.ShouldNarrate);
    }

    [Fact]
    public void SamePriority_NearestPoiWins()
    {
        var farther = CreatePoi(name: "Quan xa", priority: 2);
        var nearest = CreatePoi(name: "Quan gan", priority: 2);
        var candidates = _service.CreateCandidates(
            [
                CreateEvaluation(farther, distanceMeters: 25),
                CreateEvaluation(nearest, distanceMeters: 12)
            ],
            languageCode: "vi");

        var result = Decide(candidates);

        Assert.Equal(nearest.Id, result.Poi?.Id);
        Assert.True(result.ShouldNarrate);
    }

    [Fact]
    public void TinyDistanceDifference_KeepsCurrentPoi()
    {
        var current = CreatePoi(name: "Quan hien tai", priority: 2);
        var slightlyCloser = CreatePoi(name: "Quan lech GPS", priority: 2);
        var candidates = _service.CreateCandidates(
            [
                CreateEvaluation(current, distanceMeters: 20),
                CreateEvaluation(slightlyCloser, distanceMeters: 15)
            ],
            languageCode: "vi");

        var result = Decide(
            candidates,
            previousInsidePoiIds: [],
            lastAutoNarratedPoiId: current.Id,
            lastNarratedAtUtc: new Dictionary<Guid, DateTimeOffset>
            {
                [current.Id] = NowUtc.AddMinutes(-10)
            });

        Assert.Equal(current.Id, result.Poi?.Id);
        Assert.True(result.ShouldNarrate);
        Assert.Equal(AutoNarrationDecisionReason.Selected, result.Reason);
    }

    [Fact]
    public void Cooldown_PreventsImmediateReplay()
    {
        var poi = CreatePoi(name: "Quan vua phat", cooldownMinutes: 5);
        var candidates = _service.CreateCandidates(
            [CreateEvaluation(poi, distanceMeters: 10)],
            languageCode: "vi");

        var result = Decide(
            candidates,
            lastNarratedAtUtc: new Dictionary<Guid, DateTimeOffset>
            {
                [poi.Id] = NowUtc.AddMinutes(-1)
            });

        Assert.Equal(poi.Id, result.Poi?.Id);
        Assert.False(result.ShouldNarrate);
        Assert.Equal(AutoNarrationDecisionReason.CooldownActive, result.Reason);
    }

    [Fact]
    public void CurrentNarration_NotInterruptedUnnecessarily()
    {
        var activePoiId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var nextCandidate = CreatePoi(name: "Quan khac", priority: 5);
        var candidates = _service.CreateCandidates(
            [CreateEvaluation(nextCandidate, distanceMeters: 5)],
            languageCode: "vi");

        var result = Decide(
            candidates,
            isNarrationInProgress: true,
            activeNarrationPoiId: activePoiId);

        Assert.Null(result.Poi);
        Assert.False(result.ShouldNarrate);
        Assert.Equal(AutoNarrationDecisionReason.CurrentNarrationInProgress, result.Reason);
    }

    [Fact]
    public void NoDuplicateSimultaneousPlayback()
    {
        var active = CreatePoi(name: "Quan dang phat");
        var candidates = _service.CreateCandidates(
            [CreateEvaluation(active, distanceMeters: 10)],
            languageCode: "vi");

        var result = Decide(
            candidates,
            isNarrationInProgress: true,
            activeNarrationPoiId: active.Id);

        Assert.Equal(active.Id, result.Poi?.Id);
        Assert.False(result.ShouldNarrate);
        Assert.Equal(AutoNarrationDecisionReason.CurrentNarrationInProgress, result.Reason);
    }

    [Fact]
    public void GPSJitter_DoesNotCauseOscillation()
    {
        var stable = CreatePoi(name: "Quan on dinh", priority: 2);
        var jitter = CreatePoi(name: "Quan rung GPS", priority: 2);
        var previousInsidePoiIds = new[] { stable.Id, jitter.Id };
        var lastNarratedAtUtc = new Dictionary<Guid, DateTimeOffset>
        {
            [stable.Id] = NowUtc.AddMinutes(-10)
        };

        var jitterFrames = new[]
        {
            (StableDistance: 20d, JitterDistance: 16d),
            (StableDistance: 21d, JitterDistance: 15d),
            (StableDistance: 19d, JitterDistance: 14d)
        };

        foreach (var frame in jitterFrames)
        {
            var candidates = _service.CreateCandidates(
                [
                    CreateEvaluation(stable, frame.StableDistance),
                    CreateEvaluation(jitter, frame.JitterDistance)
                ],
                languageCode: "vi");

            var result = Decide(
                candidates,
                previousInsidePoiIds: previousInsidePoiIds,
                lastAutoNarratedPoiId: stable.Id,
                lastNarratedAtUtc: lastNarratedAtUtc);

            Assert.Equal(stable.Id, result.Poi?.Id);
            Assert.False(result.ShouldNarrate);
            Assert.Equal(AutoNarrationDecisionReason.StableInsideCurrentPoi, result.Reason);
        }
    }

    private AutoNarrationDecisionResult Decide(
        IReadOnlyList<AutoNarrationCandidate> candidates,
        IReadOnlyCollection<Guid>? previousInsidePoiIds = null,
        Guid? lastAutoNarratedPoiId = null,
        Guid? activeNarrationPoiId = null,
        bool isNarrationInProgress = false,
        IReadOnlyDictionary<Guid, DateTimeOffset>? lastNarratedAtUtc = null)
    {
        return _service.Decide(new AutoNarrationDecisionInput
        {
            Candidates = candidates,
            PreviousInsidePoiIds = previousInsidePoiIds ?? Array.Empty<Guid>(),
            CurrentPoiId = lastAutoNarratedPoiId,
            LastAutoNarratedPoiId = lastAutoNarratedPoiId,
            ActiveNarrationPoiId = activeNarrationPoiId,
            IsNarrationInProgress = isNarrationInProgress,
            LastEvaluationAtUtc = NowUtc.AddSeconds(-10),
            NowUtc = NowUtc,
            LastNarratedAtUtc = lastNarratedAtUtc ?? new Dictionary<Guid, DateTimeOffset>(),
            Options = TestOptions
        });
    }

    private static AutoNarrationPoiEvaluation CreateEvaluation(
        POI poi,
        double distanceMeters,
        bool isInside = true)
    {
        return new AutoNarrationPoiEvaluation(poi, distanceMeters, isInside);
    }

    private static POI CreatePoi(
        string name = "Quan test",
        int priority = 1,
        double radiusMeters = 50,
        int cooldownMinutes = 5,
        bool isActive = true,
        string narrationText = "Noi dung thuyet minh")
    {
        return new POI
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = name.Replace(' ', '-').ToLowerInvariant(),
            IsActive = isActive,
            Priority = priority,
            TriggerRadiusMeters = radiusMeters,
            CooldownMinutes = cooldownMinutes,
            NarrationText = narrationText,
            Latitude = 10.7614,
            Longitude = 106.7028
        };
    }
}
