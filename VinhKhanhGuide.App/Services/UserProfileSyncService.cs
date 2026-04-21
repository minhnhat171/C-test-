using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.App.Services;

public sealed class UserProfileSyncService : IUserProfileSyncService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly ILogger<UserProfileSyncService> _logger;

    public UserProfileSyncService(
        HttpClient httpClient,
        IAuthService authService,
        ILogger<UserProfileSyncService> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _logger = logger;
    }

    public async Task<bool> SyncCurrentUserAsync(
        string preferredLanguageCode,
        CancellationToken cancellationToken = default)
    {
        var session = _authService.CurrentSession;
        if (session is null)
        {
            return false;
        }

        try
        {
            var request = new AdminUserProfileUpsertRequest
            {
                Id = session.UserId == Guid.Empty ? null : session.UserId,
                UserCode = session.UserCode,
                DisplayName = string.IsNullOrWhiteSpace(session.FullName) ? "Khách tham quan" : session.FullName.Trim(),
                Email = session.Email?.Trim() ?? string.Empty,
                Role = string.IsNullOrWhiteSpace(session.Role) ? "Guest" : session.Role,
                PhoneNumber = session.PhoneNumber?.Trim() ?? string.Empty,
                PreferredLanguage = preferredLanguageCode,
                DevicePlatform = DeviceInfo.Current.Platform.ToString()
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/users/profile-sync",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not sync current user profile to API.");
            return false;
        }
    }
}
