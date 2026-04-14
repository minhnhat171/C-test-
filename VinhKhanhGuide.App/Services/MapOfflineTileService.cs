using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Maui.Storage;
using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public sealed class MapOfflineTileService : IMapOfflineTileService
{
    private const string LastPreparedPreferenceKey = "vinhkhanh.offline.map.lastPreparedUtc";
    private const int MinZoomLevel = 15;
    private const int MaxZoomLevel = 18;
    private const double MinLatitude = 10.7592;
    private const double MaxLatitude = 10.7642;
    private const double MinLongitude = 106.6994;
    private const double MaxLongitude = 106.7069;

    private static readonly Regex TilePathPattern = new(
        @"/(?<z>\d+)/(?<x>\d+)/(?<y>\d+)\.(?<ext>png|jpg|jpeg|webp)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly Lazy<IReadOnlyList<MapTileCoordinate>> _plannedTiles;
    private readonly HttpClient _httpClient;

    public MapOfflineTileService()
    {
        _plannedTiles = new Lazy<IReadOnlyList<MapTileCoordinate>>(BuildPlannedTiles);

        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        };

        _httpClient = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(MapTileHttpClientFactory.UserAgent);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*", 0.9));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.5));
    }

    public Task<OfflineMapStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cachedTileCount = 0;
            long cachedBytes = 0;

            foreach (var tile in _plannedTiles.Value)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tilePath = GetTilePath(tile);
                if (!File.Exists(tilePath))
                {
                    continue;
                }

                cachedTileCount++;
                cachedBytes += new FileInfo(tilePath).Length;
            }

            return new OfflineMapStatus
            {
                PlannedTileCount = _plannedTiles.Value.Count,
                CachedTileCount = cachedTileCount,
                CachedBytes = cachedBytes,
                LastPreparedAtUtc = ReadTimestampPreference(LastPreparedPreferenceKey)
            };
        }, cancellationToken);
    }

    public async Task<OfflineMapPrefetchResult> PrefetchAsync(CancellationToken cancellationToken = default)
    {
        var downloadedTileCount = 0;
        var failedTileCount = 0;

        foreach (var tile in _plannedTiles.Value)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tilePath = GetTilePath(tile);
            if (File.Exists(tilePath))
            {
                continue;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, GetTileUri(tile));
                MapTileHttpClientFactory.ConfigureRequest(request);

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                response.EnsureSuccessStatusCode();

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                await WriteTileAsync(tilePath, bytes, cancellationToken);
                downloadedTileCount++;
            }
            catch
            {
                failedTileCount++;
            }
        }

        var status = await GetStatusAsync(cancellationToken);
        if (status.HasCachedTiles)
        {
            Preferences.Default.Set(LastPreparedPreferenceKey, DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            status = await GetStatusAsync(cancellationToken);
        }

        return new OfflineMapPrefetchResult
        {
            PlannedTileCount = status.PlannedTileCount,
            CachedTileCount = status.CachedTileCount,
            DownloadedTileCount = downloadedTileCount,
            FailedTileCount = failedTileCount,
            CachedBytes = status.CachedBytes
        };
    }

    public async Task<byte[]?> TryGetCachedTileAsync(Uri? requestUri, CancellationToken cancellationToken = default)
    {
        var tile = ParseTileCoordinate(requestUri);
        if (tile is null)
        {
            return null;
        }

        var tilePath = GetTilePath(tile.Value);
        if (!File.Exists(tilePath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(tilePath, cancellationToken);
    }

    public async Task StoreTileAsync(Uri? requestUri, byte[] tileBytes, CancellationToken cancellationToken = default)
    {
        if (tileBytes.Length == 0)
        {
            return;
        }

        var tile = ParseTileCoordinate(requestUri);
        if (tile is null)
        {
            return;
        }

        var tilePath = GetTilePath(tile.Value);
        if (File.Exists(tilePath))
        {
            return;
        }

        await WriteTileAsync(tilePath, tileBytes, cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken);

        try
        {
            var cacheRoot = GetCacheRootDirectory();
            if (Directory.Exists(cacheRoot))
            {
                Directory.Delete(cacheRoot, recursive: true);
            }

            Preferences.Default.Remove(LastPreparedPreferenceKey);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private async Task WriteTileAsync(string tilePath, byte[] tileBytes, CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken);

        try
        {
            if (File.Exists(tilePath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(tilePath)!);

            var tempFilePath = $"{tilePath}.tmp";
            await File.WriteAllBytesAsync(tempFilePath, tileBytes, cancellationToken);

            if (File.Exists(tilePath))
            {
                File.Delete(tilePath);
            }

            File.Move(tempFilePath, tilePath);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private IReadOnlyList<MapTileCoordinate> BuildPlannedTiles()
    {
        var tiles = new HashSet<MapTileCoordinate>();

        for (var zoom = MinZoomLevel; zoom <= MaxZoomLevel; zoom++)
        {
            var minX = LongitudeToTileX(MinLongitude, zoom);
            var maxX = LongitudeToTileX(MaxLongitude, zoom);
            var minY = LatitudeToTileY(MaxLatitude, zoom);
            var maxY = LatitudeToTileY(MinLatitude, zoom);

            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    tiles.Add(new MapTileCoordinate(zoom, x, y));
                }
            }
        }

        return tiles
            .OrderBy(item => item.Zoom)
            .ThenBy(item => item.X)
            .ThenBy(item => item.Y)
            .ToList();
    }

    private static Uri GetTileUri(MapTileCoordinate tile)
    {
        return new Uri(
            $"https://tile.openstreetmap.org/{tile.Zoom}/{tile.X}/{tile.Y}.png",
            UriKind.Absolute);
    }

    private static string GetTilePath(MapTileCoordinate tile)
    {
        return Path.Combine(
            GetCacheRootDirectory(),
            tile.Zoom.ToString(CultureInfo.InvariantCulture),
            tile.X.ToString(CultureInfo.InvariantCulture),
            $"{tile.Y}.png");
    }

    private static string GetCacheRootDirectory()
    {
        return Path.Combine(FileSystem.AppDataDirectory, "offline-cache", "map-tiles");
    }

    private static MapTileCoordinate? ParseTileCoordinate(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return null;
        }

        var match = TilePathPattern.Match(requestUri.AbsolutePath);
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups["z"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var zoom) ||
            !int.TryParse(match.Groups["x"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(match.Groups["y"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var y))
        {
            return null;
        }

        return new MapTileCoordinate(zoom, x, y);
    }

    private static int LongitudeToTileX(double longitude, int zoom)
    {
        var tilesPerAxis = 1 << zoom;
        var normalizedLongitude = (longitude + 180d) / 360d;
        return (int)Math.Floor(normalizedLongitude * tilesPerAxis);
    }

    private static int LatitudeToTileY(double latitude, int zoom)
    {
        var latitudeRadians = Math.Clamp(latitude, -85.05112878, 85.05112878) * Math.PI / 180d;
        var tilesPerAxis = 1 << zoom;
        var mercatorProjection = Math.Log(Math.Tan(latitudeRadians) + (1d / Math.Cos(latitudeRadians)));
        return (int)Math.Floor((1d - (mercatorProjection / Math.PI)) / 2d * tilesPerAxis);
    }

    private static DateTimeOffset? ReadTimestampPreference(string preferenceKey)
    {
        var rawValue = Preferences.Default.Get(preferenceKey, string.Empty);
        return DateTimeOffset.TryParse(rawValue, out var parsedValue)
            ? parsedValue
            : null;
    }

    private readonly record struct MapTileCoordinate(int Zoom, int X, int Y);
}
