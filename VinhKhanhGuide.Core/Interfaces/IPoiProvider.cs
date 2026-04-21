using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.Core.Interfaces;

public interface IPoiProvider
{
    PoiDataSource LastDataSource { get; }
    Task<IReadOnlyList<POI>> GetPoisAsync(CancellationToken cancellationToken = default);
    Task<POI?> GetPoiByIdAsync(Guid poiId, CancellationToken cancellationToken = default);
}
