using System.Net.Http.Json;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public sealed class UserManagementApiClient
{
    private readonly HttpClient _httpClient;

    public UserManagementApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<AdminUserSummaryDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<AdminUserSummaryDto>>(
                   "api/admin/users",
                   cancellationToken)
               ?? [];
    }

    public async Task<List<AdminUserSummaryDto>> GetUsersAsync(
        string? keyword,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var normalizedStatus = NormalizeStatus(status);
        var hasKeyword = !string.IsNullOrWhiteSpace(keyword);
        var endpoint = hasKeyword
            ? $"api/admin/users/search?keyword={Uri.EscapeDataString(keyword!.Trim())}"
            : normalizedStatus == "all"
                ? "api/admin/users"
                : $"api/admin/users/by-status?status={Uri.EscapeDataString(normalizedStatus)}";

        var users = await _httpClient.GetFromJsonAsync<List<AdminUserSummaryDto>>(
            endpoint,
            cancellationToken) ?? [];

        if (hasKeyword && normalizedStatus != "all")
        {
            users = users
                .Where(user => string.Equals(
                    NormalizeStatus(user.Status),
                    normalizedStatus,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return users;
    }

    public async Task<AdminUserDetailDto?> GetUserDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AdminUserDetailDto>(
                $"api/admin/users/{id}",
                cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<AdminUserLocationDto?> GetUserLocationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AdminUserLocationDto>(
                $"api/admin/users/{id}/location",
                cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private static string NormalizeStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "online" => "online",
            "offline" => "offline",
            _ => "all"
        };
    }
}
