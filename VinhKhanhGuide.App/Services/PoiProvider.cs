using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Mappings;
using VinhKhanhGuide.Core.Models;
using VinhKhanhGuide.Core.Seed;

namespace VinhKhanhGuide.App.Services;

public class PoiProvider : IPoiProvider
{
    private static readonly IReadOnlyList<POI> FallbackPois = PoiSeedData.CreateDefaultDtos()
        .Where(dto => dto.IsActive)
        .Select(dto => dto.ToDomain())
        .ToList();

    private readonly HttpClient _httpClient;
    private readonly ILogger<PoiProvider> _logger;
    private readonly IPoiOfflineStore _poiOfflineStore;
    private readonly object _syncRoot = new();
    private IReadOnlyList<POI> _lastSuccessfulRemotePois = Array.Empty<POI>();
    private bool _hasSuccessfulRemoteFetch;

    public PoiProvider(
        HttpClient httpClient,
        ILogger<PoiProvider> logger,
        IPoiOfflineStore poiOfflineStore)
    {
        _httpClient = httpClient;
        _logger = logger;
        _poiOfflineStore = poiOfflineStore;
    }

    public async Task<IReadOnlyList<POI>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pois = await _httpClient.GetFromJsonAsync<List<PoiDto>>("api/pois", cancellationToken) ?? [];
            var mappedPois = pois
                .Where(dto => dto.IsActive)
                .Select(ToDomainWithResolvedImageSource)
                .ToList();

            CacheSuccessfulRemotePois(mappedPois);
            await PersistOfflineSnapshotAsync(mappedPois, cancellationToken);
            return ClonePois(mappedPois);
        }
        catch (Exception ex)
        {
            try
            {
                var offlinePois = await _poiOfflineStore.GetPoisAsync(cancellationToken);
                if (offlinePois.Count > 0)
                {
                    _logger.LogWarning(ex, "Failed to load POIs from API. Using SQLite offline snapshot.");
                    CacheSuccessfulRemotePois(offlinePois);
                    return ClonePois(offlinePois);
                }
            }
            catch (Exception offlineEx)
            {
                _logger.LogWarning(
                    offlineEx,
                    "Failed to read POIs from SQLite offline store after API failure. Falling back to in-memory or seed data.");
            }

            if (TryGetLastSuccessfulRemotePois(out var cachedPois))
            {
                _logger.LogWarning(ex, "Failed to load POIs from API. Using the last successful API snapshot.");
                return cachedPois;
            }

            _logger.LogWarning(ex, "Failed to load POIs from API. Falling back to local seed data.");
        }

        await EnsureSeedSnapshotAsync(cancellationToken);
        return ClonePois(FallbackPois);
    }

    public async Task<POI?> GetPoiByIdAsync(Guid poiId, CancellationToken cancellationToken = default)
    {
        if (poiId == Guid.Empty)
        {
            return null;
        }

        try
        {
            var dto = await _httpClient.GetFromJsonAsync<PoiDto>($"api/pois/{poiId}", cancellationToken);
            if (dto is not null)
            {
                return ToDomainWithResolvedImageSource(dto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load POI {PoiId} from API.", poiId);
        }

        try
        {
            var offlinePoi = await _poiOfflineStore.GetPoiByIdAsync(poiId, cancellationToken);
            if (offlinePoi is not null)
            {
                return offlinePoi;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load POI {PoiId} from SQLite offline store.", poiId);
        }

        if (TryGetLastSuccessfulRemotePois(out var cachedPois))
        {
            var cachedPoi = cachedPois.FirstOrDefault(item => item.Id == poiId);
            if (cachedPoi is not null)
            {
                return cachedPoi;
            }
        }

        return ClonePois(FallbackPois).FirstOrDefault(item => item.Id == poiId);
    }

    private void CacheSuccessfulRemotePois(IReadOnlyList<POI> pois)
    {
        lock (_syncRoot)
        {
            _lastSuccessfulRemotePois = ClonePois(pois);
            _hasSuccessfulRemoteFetch = true;
        }
    }

    private async Task PersistOfflineSnapshotAsync(
        IReadOnlyList<POI> pois,
        CancellationToken cancellationToken)
    {
        try
        {
            await _poiOfflineStore.ReplacePoisAsync(pois, DateTimeOffset.UtcNow, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist POIs into SQLite offline store.");
        }
    }

    private async Task EnsureSeedSnapshotAsync(CancellationToken cancellationToken)
    {
        try
        {
            var offlinePois = await _poiOfflineStore.GetPoisAsync(cancellationToken);
            if (offlinePois.Count > 0)
            {
                return;
            }

            await _poiOfflineStore.ReplacePoisAsync(FallbackPois, DateTimeOffset.UtcNow, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed SQLite offline store.");
        }
    }

    private bool TryGetLastSuccessfulRemotePois(out IReadOnlyList<POI> pois)
    {
        lock (_syncRoot)
        {
            if (!_hasSuccessfulRemoteFetch)
            {
                pois = Array.Empty<POI>();
                return false;
            }

            pois = ClonePois(_lastSuccessfulRemotePois);
            return true;
        }
    }

    private static IReadOnlyList<POI> ClonePois(IEnumerable<POI> pois)
    {
        return pois
            .Select(poi => poi.ToDto().ToDomain())
            .ToList();
    }

    private POI ToDomainWithResolvedImageSource(PoiDto dto)
    {
        var poi = dto.ToDomain();
        poi.ImageSource = ResolveImageSource(poi.ImageSource);
        return poi;
    }

    private string ResolveImageSource(string? imageSource)
    {
        var value = imageSource?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value) ||
            Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            return value;
        }

        if (value.StartsWith("/", StringComparison.Ordinal) &&
            _httpClient.BaseAddress is not null)
        {
            return new Uri(_httpClient.BaseAddress, value.TrimStart('/')).ToString();
        }

        return value;
    }
}
