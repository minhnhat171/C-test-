using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public interface IListeningHistorySyncService
{
    Task<Guid?> BeginAsync(
        POI poi,
        string? language,
        bool autoTriggered,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid historyId,
        int listenSeconds,
        bool completed,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}
