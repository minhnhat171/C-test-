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
    private readonly object _syncRoot = new();
    private IReadOnlyList<POI> _lastSuccessfulRemotePois = Array.Empty<POI>();
    private bool _hasSuccessfulRemoteFetch;

    public PoiProvider(HttpClient httpClient, ILogger<PoiProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<POI>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pois = await _httpClient.GetFromJsonAsync<List<PoiDto>>("api/pois", cancellationToken) ?? [];
            var mappedPois = pois
                .Where(dto => dto.IsActive)
                .Select(dto => dto.ToDomain())
                .ToList();

            CacheSuccessfulRemotePois(mappedPois);
            return ClonePois(mappedPois);
        }
        catch (Exception ex)
        {
            if (TryGetLastSuccessfulRemotePois(out var cachedPois))
            {
                _logger.LogWarning(ex, "Failed to load POIs from API. Using the last successful API snapshot.");
                return cachedPois;
            }

            _logger.LogWarning(ex, "Failed to load POIs from API. Falling back to local seed data.");
        }

        return ClonePois(FallbackPois);
    }

    private void CacheSuccessfulRemotePois(IReadOnlyList<POI> pois)
    {
        lock (_syncRoot)
        {
            _lastSuccessfulRemotePois = ClonePois(pois);
            _hasSuccessfulRemoteFetch = true;
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
}
