using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public interface IPoiRepository
{
    Task<IReadOnlyList<POI>> GetPoisAsync(CancellationToken cancellationToken = default);

    Task<POI?> GetPoiByIdAsync(Guid poiId, CancellationToken cancellationToken = default);

    IReadOnlyList<POI> GetCachedPois();

    IReadOnlyList<POI> SearchCachedPois(string? keyword);

    void StoreSnapshot(IEnumerable<POI> pois);
}
