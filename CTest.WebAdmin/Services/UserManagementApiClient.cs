using System.Net;
using System.Net.Http.Json;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class UserManagementApiClient
{
    private readonly HttpClient _httpClient;

    public UserManagementApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<AdminUserSummaryDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<AdminUserSummaryDto>>(
                   "api/admin/users",
                   cancellationToken)
               ?? [];
    }

    public async Task<List<AdminUserSummaryDto>> GetUsersByStatusAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var requestUri = BuildRequestUri(
            "api/admin/users/by-status",
            ("status", status));

        return await _httpClient.GetFromJsonAsync<List<AdminUserSummaryDto>>(requestUri, cancellationToken)
               ?? [];
    }

    public async Task<List<AdminUserSummaryDto>> SearchUsersAsync(
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        var requestUri = BuildRequestUri(
            "api/admin/users/search",
            ("keyword", keyword));

        return await _httpClient.GetFromJsonAsync<List<AdminUserSummaryDto>>(requestUri, cancellationToken)
               ?? [];
    }

    public Task<AdminUserDetailDto?> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return GetOptionalAsync<AdminUserDetailDto>($"api/admin/users/{userId:D}", cancellationToken);
    }

    public Task<AdminUserLocationDto?> GetUserLocationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return GetOptionalAsync<AdminUserLocationDto>($"api/admin/users/{userId:D}/location", cancellationToken);
    }

    private async Task<T?> GetOptionalAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
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
