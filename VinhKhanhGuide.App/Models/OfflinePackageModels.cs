namespace VinhKhanhGuide.App.Models;

public sealed class OfflineContentStatus
{
    public int PoiCount { get; init; }
    public DateTimeOffset? LastSyncedAtUtc { get; init; }
    public bool HasSnapshot => PoiCount > 0;
}

public sealed class OfflineMapStatus
{
    public int PlannedTileCount { get; init; }
    public int CachedTileCount { get; init; }
    public long CachedBytes { get; init; }
    public DateTimeOffset? LastPreparedAtUtc { get; init; }
    public bool HasCachedTiles => CachedTileCount > 0;
    public bool IsReady => PlannedTileCount > 0 && CachedTileCount >= PlannedTileCount;
}

public sealed class OfflineMapPrefetchResult
{
    public int PlannedTileCount { get; init; }
    public int CachedTileCount { get; init; }
    public int DownloadedTileCount { get; init; }
    public int FailedTileCount { get; init; }
    public long CachedBytes { get; init; }
}

public sealed class AudioCacheStatus
{
    public int AvailableAssetCount { get; init; }
    public int CachedAssetCount { get; init; }
    public long CachedBytes { get; init; }
    public DateTimeOffset? LastPreparedAtUtc { get; init; }
    public bool HasPublishedAssets => AvailableAssetCount > 0;
    public bool HasCachedAssets => CachedAssetCount > 0;
    public bool IsReady => AvailableAssetCount > 0 && CachedAssetCount >= AvailableAssetCount;
}

public sealed class AudioCachePrefetchResult
{
    public int AvailableAssetCount { get; init; }
    public int CachedAssetCount { get; init; }
    public int DownloadedAssetCount { get; init; }
    public int FailedAssetCount { get; init; }
    public long CachedBytes { get; init; }
}
