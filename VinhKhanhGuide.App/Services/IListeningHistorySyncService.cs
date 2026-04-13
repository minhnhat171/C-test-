using VinhKhanhGuide.Core.Models;
using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.App.Services;

public interface IListeningHistorySyncService
{
    Task<Guid?> BeginAsync(
        POI poi,
        string? language,
        string? playbackMode,
        bool autoTriggered,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid historyId,
        int listenSeconds,
        bool completed,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListeningHistoryEntryDto>> GetCurrentUserHistoryAsync(
        string? sortBy,
        string? period,
        int limit = 15,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PoiListeningCountDto>> GetCurrentUserRankingAsync(
        string? period,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid historyId,
        CancellationToken cancellationToken = default);
}
