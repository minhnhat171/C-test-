using System.Net;
using System.Net.Http.Json;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class AudioGuideApiClient
{
    private readonly HttpClient _httpClient;

    public AudioGuideApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<AudioGuideDto>> GetAudioGuidesAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<AudioGuideDto>>("api/audioguides", cancellationToken)
            ?? [];
    }

    public async Task<AudioGuideDto?> GetAudioGuideAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/audioguides/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AudioGuideDto>(cancellationToken: cancellationToken);
    }

    public async Task<AudioGuideDto> CreateAudioGuideAsync(AudioGuideDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/audioguides", dto, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AudioGuideDto>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("VKFoodAPI returned an empty audio payload.");
    }

    public async Task<bool> UpdateAudioGuideAsync(Guid id, AudioGuideDto dto, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/audioguides/{id}", dto, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<bool> DeleteAudioGuideAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/audioguides/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
