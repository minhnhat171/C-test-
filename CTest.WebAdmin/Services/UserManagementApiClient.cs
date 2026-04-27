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

    public async Task<IReadOnlyList<AdminUserSummaryDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<AdminUserSummaryDto>>(
            "api/admin/users",
            cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<AdminUserSummaryDto>> SearchUsersAsync(
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return await GetUsersAsync(cancellationToken);
        }

        var url = $"api/admin/users/search?keyword={Uri.EscapeDataString(keyword.Trim())}";
        return await _httpClient.GetFromJsonAsync<List<AdminUserSummaryDto>>(
            url,
            cancellationToken) ?? [];
    }

    public async Task<AdminUserDetailDto?> GetUserDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<AdminUserDetailDto>(
            $"api/admin/users/{id}",
            cancellationToken);
    }

    public async Task<AdminUserDetailDto> UpsertProfileAsync(
        AdminUserProfileUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/admin/users/profile-sync",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AdminUserDetailDto>(cancellationToken: cancellationToken)
            ?? new AdminUserDetailDto();
    }
}
