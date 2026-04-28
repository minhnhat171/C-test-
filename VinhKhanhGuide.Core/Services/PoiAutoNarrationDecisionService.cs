using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.Core.Services;

public sealed class PoiAutoNarrationDecisionService : IAutoPoiSelectionService
{
    public IReadOnlyList<AutoNarrationCandidate> CreateCandidates(
        IEnumerable<AutoNarrationPoiEvaluation> evaluations,
        string? languageCode = null)
    {
        return evaluations
            .Where(evaluation => IsValidCandidate(evaluation, languageCode))
            .Select(evaluation => new AutoNarrationCandidate(
                evaluation.Poi,
                evaluation.DistanceMeters))
            .OrderByDescending(candidate => candidate.Poi.Priority)
            .ThenBy(candidate => candidate.DistanceMeters)
            .ThenBy(candidate => candidate.Poi.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Poi.Id)
            .ToList();
    }

    public AutoNarrationDecisionResult Decide(AutoNarrationDecisionInput input)
    {
        var options = input.Options ?? AutoNarrationDecisionOptions.Default;

        if (IsDebounced(input, options))
        {
            return AutoNarrationDecisionResult.None(AutoNarrationDecisionReason.Debounced);
        }

        var candidates = NormalizeCandidates(input.Candidates);
        if (candidates.Count == 0)
        {
            return AutoNarrationDecisionResult.None(AutoNarrationDecisionReason.NoCandidate);
        }

        if (input.IsNarrationInProgress)
        {
            var activeCandidate = FindCandidate(candidates, input.ActiveNarrationPoiId);
            return activeCandidate is null
                ? AutoNarrationDecisionResult.None(AutoNarrationDecisionReason.CurrentNarrationInProgress)
                : AutoNarrationDecisionResult.ForCandidate(
                    activeCandidate,
                    AutoNarrationDecisionReason.CurrentNarrationInProgress,
                    shouldNarrate: false);
        }

        var selected = SelectStableCandidate(candidates, input, options);
        if (selected is null)
        {
            return AutoNarrationDecisionResult.None(AutoNarrationDecisionReason.NoCandidate);
        }

        var isNewEntry = !input.PreviousInsidePoiIds.Contains(selected.Poi.Id);
        var candidateChanged = input.LastAutoNarratedPoiId != selected.Poi.Id;
        if (!isNewEntry && !candidateChanged)
        {
            return AutoNarrationDecisionResult.ForCandidate(
                selected,
                AutoNarrationDecisionReason.StableInsideCurrentPoi,
                shouldNarrate: false);
        }

        if (IsInCooldown(selected.Poi, input))
        {
            return AutoNarrationDecisionResult.ForCandidate(
                selected,
                AutoNarrationDecisionReason.CooldownActive,
                shouldNarrate: false);
        }

        return AutoNarrationDecisionResult.ForCandidate(
            selected,
            AutoNarrationDecisionReason.Selected,
            shouldNarrate: true);
    }

    private static bool IsDebounced(
        AutoNarrationDecisionInput input,
        AutoNarrationDecisionOptions options)
    {
        return input.LastEvaluationAtUtc.HasValue &&
               input.NowUtc - input.LastEvaluationAtUtc.Value < options.DebounceInterval;
    }

    private static IReadOnlyList<AutoNarrationCandidate> NormalizeCandidates(
        IEnumerable<AutoNarrationCandidate> candidates)
    {
        return candidates
            .Where(candidate =>
                candidate.Poi.IsActive &&
                !double.IsNaN(candidate.DistanceMeters) &&
                !double.IsInfinity(candidate.DistanceMeters))
            .OrderByDescending(candidate => candidate.Poi.Priority)
            .ThenBy(candidate => candidate.DistanceMeters)
            .ThenBy(candidate => candidate.Poi.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Poi.Id)
            .ToList();
    }

    private static AutoNarrationCandidate? SelectStableCandidate(
        IReadOnlyList<AutoNarrationCandidate> candidates,
        AutoNarrationDecisionInput input,
        AutoNarrationDecisionOptions options)
    {
        var best = candidates.FirstOrDefault();
        if (best is null)
        {
            return null;
        }

        var currentCandidate = FindCandidate(
            candidates,
            input.ActiveNarrationPoiId ??
            input.LastAutoNarratedPoiId ??
            input.CurrentPoiId);
        if (currentCandidate is null ||
            currentCandidate.Poi.Id == best.Poi.Id ||
            currentCandidate.Poi.Priority != best.Poi.Priority)
        {
            return best;
        }

        var distanceImprovementMeters = currentCandidate.DistanceMeters - best.DistanceMeters;
        return distanceImprovementMeters <= options.SamePrioritySwitchThresholdMeters
            ? currentCandidate
            : best;
    }

    private static AutoNarrationCandidate? FindCandidate(
        IEnumerable<AutoNarrationCandidate> candidates,
        Guid? poiId)
    {
        return !poiId.HasValue
            ? null
            : candidates.FirstOrDefault(candidate => candidate.Poi.Id == poiId.Value);
    }

    private static bool IsInCooldown(POI poi, AutoNarrationDecisionInput input)
    {
        if (!input.LastNarratedAtUtc.TryGetValue(poi.Id, out var lastNarratedAtUtc))
        {
            return false;
        }

        return input.NowUtc - lastNarratedAtUtc < TimeSpan.FromMinutes(poi.CooldownMinutes);
    }

    private static bool IsValidCandidate(
        AutoNarrationPoiEvaluation evaluation,
        string? languageCode)
    {
        return evaluation.Poi.IsActive &&
               evaluation.Poi.TriggerRadiusMeters > 0 &&
               evaluation.IsInsideTriggerRadius &&
               evaluation.DistanceMeters >= 0 &&
               evaluation.DistanceMeters <= evaluation.Poi.TriggerRadiusMeters &&
               HasNarrationContent(evaluation.Poi, languageCode);
    }

    private static bool HasNarrationContent(POI poi, string? languageCode)
    {
        return !string.IsNullOrWhiteSpace(poi.GetNarrationText(languageCode)) ||
               !string.IsNullOrWhiteSpace(poi.GetAudioAssetPath(languageCode));
    }
}
