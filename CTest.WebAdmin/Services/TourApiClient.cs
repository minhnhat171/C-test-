using System.Net;
using System.Net.Http.Json;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class TourApiClient
{
    private readonly HttpClient _httpClient;

    public TourApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TourDto>> GetToursAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<TourDto>>("api/tours", cancellationToken)
            ?? [];
    }

    public async Task<TourDto?> GetTourAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/tours/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TourDto>(cancellationToken: cancellationToken);
    }

    public async Task<TourDto> CreateTourAsync(TourDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/tours", dto, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TourDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("VKFoodAPI returned an empty tour payload.");
    }

    public async Task<bool> UpdateTourAsync(int id, TourDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/tours/{id}", dto, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<bool> DeleteTourAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/tours/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
