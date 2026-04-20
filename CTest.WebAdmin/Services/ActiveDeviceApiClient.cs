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

    public async Task<ActiveDeviceStatsDto> HeartbeatAsync(
        ActiveDeviceHeartbeatRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/analytics/active-devices/heartbeat",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ActiveDeviceStatsDto>(cancellationToken: cancellationToken)
            ?? new ActiveDeviceStatsDto();
    }

    public async Task<ActiveDeviceStatsDto> DisconnectAsync(
        ActiveDeviceDisconnectRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/analytics/active-devices/disconnect",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ActiveDeviceStatsDto>(cancellationToken: cancellationToken)
            ?? new ActiveDeviceStatsDto();
    }
}
