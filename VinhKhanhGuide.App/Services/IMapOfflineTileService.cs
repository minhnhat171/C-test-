using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public interface IMapOfflineTileService
{
    Task<OfflineMapStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<OfflineMapPrefetchResult> PrefetchAsync(CancellationToken cancellationToken = default);

    Task<byte[]?> TryGetCachedTileAsync(Uri? requestUri, CancellationToken cancellationToken = default);

    Task StoreTileAsync(Uri? requestUri, byte[] tileBytes, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
