using System.Net.Http.Json;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class ListeningHistoryApiClient
{
    private readonly HttpClient _httpClient;

    public ListeningHistoryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ListeningHistoryEntryDto>> GetListeningHistoryAsync(
        string? sortBy,
        string? period,
        CancellationToken cancellationToken = default)
    {
        var requestUri = BuildRequestUri(
            "api/analytics/listening-history",
            ("sortBy", sortBy),
            ("period", period));

        return await _httpClient.GetFromJsonAsync<List<ListeningHistoryEntryDto>>(requestUri, cancellationToken)
            ?? [];
    }

    public async Task<List<PoiListeningCountDto>> GetPoiRankingAsync(
        string? period,
        CancellationToken cancellationToken = default)
    {
        var requestUri = BuildRequestUri(
            "api/analytics/listening-history/ranking",
            ("period", period));

        return await _httpClient.GetFromJsonAsync<List<PoiListeningCountDto>>(requestUri, cancellationToken)
            ?? [];
    }

    private static string BuildRequestUri(string path, params (string Key, string? Value)[] queryParts)
    {
        var queryString = string.Join(
            "&",
            queryParts
                .Where(part => !string.IsNullOrWhiteSpace(part.Value))
                .Select(part => $"{Uri.EscapeDataString(part.Key)}={Uri.EscapeDataString(part.Value!)}"));

        return string.IsNullOrWhiteSpace(queryString)
            ? path
            : $"{path}?{queryString}";
    }
}
