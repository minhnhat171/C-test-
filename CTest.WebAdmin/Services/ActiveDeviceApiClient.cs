using System.Net.Http.Json;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class ActiveDeviceApiClient
{
    private readonly HttpClient _httpClient;

    public ActiveDeviceApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ActiveDeviceStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ActiveDeviceStatsDto>(
            "api/analytics/active-devices",
            cancellationToken) ?? new ActiveDeviceStatsDto();
    }
}
