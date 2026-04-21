using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Seed;

namespace VinhKhanhGuide.App.Services;

public class TourProvider : ITourProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TourProvider> _logger;
    private readonly object _syncRoot = new();
    private IReadOnlyList<TourDto> _lastSuccessfulRemoteTours = Array.Empty<TourDto>();
    private bool _hasSuccessfulRemoteFetch;

    public TourProvider(
        HttpClient httpClient,
        ILogger<TourProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TourDto>> GetToursAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tours = await _httpClient.GetFromJsonAsync<List<TourDto>>("api/tours", cancellationToken) ?? [];
            var mappedTours = tours
                .Where(item => item.IsActive)
                .Select(item => item.Clone())
                .ToList();

            CacheSuccessfulRemoteTours(mappedTours);
            return CloneTours(mappedTours);
        }
        catch (Exception ex)
        {
            if (TryGetLastSuccessfulRemoteTours(out var cachedTours))
            {
                _logger.LogWarning(ex, "Failed to load tours from API. Using the last successful API snapshot.");
                return cachedTours;
            }

            var seedTours = TourSeedData.CreateDefaultDtos()
                .Where(item => item.IsActive)
                .Select(item => item.Clone())
                .ToList();

            _logger.LogWarning(ex, "Failed to load tours from API. Using bundled tour seed data.");
            CacheSuccessfulRemoteTours(seedTours);
            return CloneTours(seedTours);
        }
    }

    public async Task<TourDto?> GetTourByIdAsync(int tourId, CancellationToken cancellationToken = default)
    {
        if (tourId <= 0)
        {
            return null;
        }

        try
        {
            var dto = await _httpClient.GetFromJsonAsync<TourDto>($"api/tours/{tourId}", cancellationToken);
            if (dto is not null)
            {
                return dto.Clone();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load tour {TourId} from API.", tourId);
        }

        if (TryGetLastSuccessfulRemoteTours(out var cachedTours))
        {
            var cachedTour = cachedTours.FirstOrDefault(item => item.Id == tourId);
            if (cachedTour is not null)
            {
                return cachedTour;
            }
        }

        return TourSeedData.CreateDefaultDtos()
            .FirstOrDefault(item => item.Id == tourId && item.IsActive)
            ?.Clone();
    }

    private void CacheSuccessfulRemoteTours(IReadOnlyList<TourDto> tours)
    {
        lock (_syncRoot)
        {
            _lastSuccessfulRemoteTours = CloneTours(tours);
            _hasSuccessfulRemoteFetch = true;
        }
    }

    private bool TryGetLastSuccessfulRemoteTours(out IReadOnlyList<TourDto> tours)
    {
        lock (_syncRoot)
        {
            if (!_hasSuccessfulRemoteFetch)
            {
                tours = Array.Empty<TourDto>();
                return false;
            }

            tours = CloneTours(_lastSuccessfulRemoteTours);
            return true;
        }
    }

    private static IReadOnlyList<TourDto> CloneTours(IEnumerable<TourDto> tours)
    {
        return tours
            .Select(item => item.Clone())
            .ToList();
    }
}
