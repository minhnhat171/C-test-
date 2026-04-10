using System.Net;
using System.Net.Http.Json;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class PoiApiClient
{
    private readonly HttpClient _httpClient;

    public PoiApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<PoiDto>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<PoiDto>>("api/pois", cancellationToken)
            ?? [];
    }

    public async Task<PoiDto?> GetPoiAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/pois/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PoiDto>(cancellationToken: cancellationToken);
    }

    public async Task<PoiDto> CreatePoiAsync(PoiDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/pois", dto, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PoiDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("VKFoodAPI returned an empty POI payload.");
    }

    public async Task<bool> UpdatePoiAsync(Guid id, PoiDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/pois/{id}", dto, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<bool> DeletePoiAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/pois/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
