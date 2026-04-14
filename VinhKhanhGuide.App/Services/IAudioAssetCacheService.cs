using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public interface IAudioAssetCacheService
{
    Task<string> ResolveAsync(string audioAssetPath, CancellationToken cancellationToken = default);

    Task<AudioCacheStatus> GetStatusAsync(
        IEnumerable<POI> pois,
        CancellationToken cancellationToken = default);

    Task<AudioCachePrefetchResult> PrefetchAsync(
        IEnumerable<POI> pois,
        CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
