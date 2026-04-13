using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class UserManagementService
{
    private readonly UserManagementApiClient _apiClient;

    public UserManagementService(UserManagementApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<UserManagementPageViewModel> LoadPageAsync(
        string? status,
        string? keyword,
        Guid? selectedUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedStatus = NormalizeStatus(status);
        var normalizedKeyword = keyword?.Trim() ?? string.Empty;

        try
        {
            var allUsersTask = _apiClient.GetAllUsersAsync(cancellationToken);
            var filteredUsersTask = SelectListTask(normalizedStatus, normalizedKeyword, allUsersTask, cancellationToken);

            await Task.WhenAll(allUsersTask, filteredUsersTask);

            var allUsers = allUsersTask.Result;
            var filteredUsers = filteredUsersTask.Result;
            var effectiveSelectedUserId = ResolveSelectedUserId(selectedUserId, filteredUsers);

            AdminUserDetailDto? detail = null;
            AdminUserLocationDto? location = null;

            if (effectiveSelectedUserId.HasValue)
            {
                var detailTask = _apiClient.GetUserDetailsAsync(effectiveSelectedUserId.Value, cancellationToken);
                var locationTask = _apiClient.GetUserLocationAsync(effectiveSelectedUserId.Value, cancellationToken);

                await Task.WhenAll(detailTask, locationTask);

                detail = detailTask.Result;
                location = locationTask.Result;
            }

            return new UserManagementPageViewModel
            {
                SelectedStatus = normalizedStatus,
                SearchTerm = normalizedKeyword,
                SelectedUserId = effectiveSelectedUserId,
                TotalUsers = allUsers.Count,
                OnlineUsers = allUsers.Count(user => user.IsOnline),
                OfflineUsers = allUsers.Count(user => !user.IsOnline),
                FilteredUsers = filteredUsers.Count,
                Items = filteredUsers.Select(ToSummaryViewModel).ToList(),
                SelectedUser = detail is null ? null : ToDetailViewModel(detail),
                SelectedLocation = location is null ? null : ToLocationViewModel(location)
            };
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            return new UserManagementPageViewModel
            {
                SelectedStatus = normalizedStatus,
                SearchTerm = normalizedKeyword,
                SelectedUserId = selectedUserId,
                LoadErrorMessage = $"Khong tai duoc du lieu quan ly nguoi dung tu API: {ex.Message}"
            };
        }
    }

    private Task<List<AdminUserSummaryDto>> SelectListTask(
        string normalizedStatus,
        string normalizedKeyword,
        Task<List<AdminUserSummaryDto>> allUsersTask,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return _apiClient.SearchUsersAsync(normalizedKeyword, cancellationToken);
        }

        if (!string.Equals(normalizedStatus, "all", StringComparison.OrdinalIgnoreCase))
        {
            return _apiClient.GetUsersByStatusAsync(normalizedStatus, cancellationToken);
        }

        return allUsersTask;
    }

    private static Guid? ResolveSelectedUserId(Guid? selectedUserId, List<AdminUserSummaryDto> filteredUsers)
    {
        if (selectedUserId.HasValue && filteredUsers.Any(user => user.Id == selectedUserId.Value))
        {
            return selectedUserId.Value;
        }

        return filteredUsers.FirstOrDefault()?.Id;
    }

    private static UserSummaryViewModel ToSummaryViewModel(AdminUserSummaryDto dto)
    {
        return new UserSummaryViewModel
        {
            Id = dto.Id,
            UserCode = dto.UserCode,
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            Role = dto.Role,
            Status = dto.Status,
            IsOnline = dto.IsOnline,
            LastActiveAtUtc = dto.LastActiveAtUtc,
            LastPoiName = dto.LastPoiName,
            DevicePlatform = dto.DevicePlatform,
            TotalSessions = dto.TotalSessions,
            CompletedSessions = dto.CompletedSessions,
            TotalListenSeconds = dto.TotalListenSeconds
        };
    }

    private static UserDetailViewModel ToDetailViewModel(AdminUserDetailDto dto)
    {
        return new UserDetailViewModel
        {
            Id = dto.Id,
            UserCode = dto.UserCode,
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            Role = dto.Role,
            Status = dto.Status,
            IsOnline = dto.IsOnline,
            CreatedAtUtc = dto.CreatedAtUtc,
            LastActiveAtUtc = dto.LastActiveAtUtc,
            LastCompletedAtUtc = dto.LastCompletedAtUtc,
            PreferredLanguage = dto.PreferredLanguage,
            PhoneNumber = dto.PhoneNumber,
            DevicePlatform = dto.DevicePlatform,
            LastTriggerType = dto.LastTriggerType,
            LastSource = dto.LastSource,
            LastPoiName = dto.LastPoiName,
            LastPoiCode = dto.LastPoiCode,
            TotalSessions = dto.TotalSessions,
            CompletedSessions = dto.CompletedSessions,
            TotalListenSeconds = dto.TotalListenSeconds
        };
    }

    private static UserLocationViewModel ToLocationViewModel(AdminUserLocationDto dto)
    {
        return new UserLocationViewModel
        {
            UserId = dto.UserId,
            UserCode = dto.UserCode,
            DisplayName = dto.DisplayName,
            PoiId = dto.PoiId,
            PoiCode = dto.PoiCode,
            PoiName = dto.PoiName,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            MapLink = dto.MapLink,
            UpdatedAtUtc = dto.UpdatedAtUtc
        };
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
