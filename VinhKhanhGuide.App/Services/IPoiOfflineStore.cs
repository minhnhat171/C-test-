using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public interface IPoiOfflineStore
{
    Task<IReadOnlyList<POI>> GetPoisAsync(CancellationToken cancellationToken = default);

    Task<POI?> GetPoiByIdAsync(Guid poiId, CancellationToken cancellationToken = default);

    Task ReplacePoisAsync(
        IReadOnlyList<POI> pois,
        DateTimeOffset syncedAtUtc,
        CancellationToken cancellationToken = default);

    Task<DateTimeOffset?> GetLastSyncedAtAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
