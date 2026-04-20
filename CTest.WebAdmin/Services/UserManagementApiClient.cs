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
}
