using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.Core.Interfaces;

public interface IAutoPoiSelectionService
{
    IReadOnlyList<AutoNarrationCandidate> CreateCandidates(
        IEnumerable<AutoNarrationPoiEvaluation> evaluations,
        string? languageCode = null);

    AutoNarrationDecisionResult Decide(AutoNarrationDecisionInput input);
}
