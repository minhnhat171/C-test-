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

    public PoiProvider(HttpClient httpClient, ILogger<PoiProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<POI>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pois = await _httpClient.GetFromJsonAsync<List<PoiDto>>("api/pois", cancellationToken);
            if (pois is { Count: > 0 })
            {
                return pois
                    .Where(dto => dto.IsActive)
                    .Select(dto => dto.ToDomain())
                    .ToList();
            }

            _logger.LogWarning("POI API returned no data. Falling back to local seed data.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load POIs from API. Falling back to local seed data.");
        }

        return FallbackPois;
    }
}
