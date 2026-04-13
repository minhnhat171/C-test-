using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.Core.Interfaces;

public interface IPoiProvider
{
    Task<IReadOnlyList<POI>> GetPoisAsync(CancellationToken cancellationToken = default);
    Task<POI?> GetPoiByIdAsync(Guid poiId, CancellationToken cancellationToken = default);
}
