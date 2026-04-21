using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
public class AdminUsersController : Controller
{
    private readonly UserManagementApiClient _userManagementApiClient;

    public AdminUsersController(UserManagementApiClient userManagementApiClient)
    {
        _userManagementApiClient = userManagementApiClient;
    }

    public async Task<IActionResult> Index(
        string? keyword,
        string? status,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var model = new AdminUsersPageViewModel
        {
            SearchTerm = keyword?.Trim() ?? string.Empty,
            StatusFilter = NormalizeStatus(status),
            SelectedUserId = userId
        };

        try
        {
            model.Users = (await _userManagementApiClient.GetUsersAsync(
                    model.SearchTerm,
                    model.StatusFilter,
                    cancellationToken))
                .OrderByDescending(user => user.IsOnline)
                .ThenByDescending(user => user.LastActiveAtUtc ?? DateTimeOffset.MinValue)
                .ThenBy(user => user.DisplayName)
                .ToList();

            var selectedId = userId ?? model.Users.FirstOrDefault()?.Id;
            if (selectedId.HasValue && selectedId.Value != Guid.Empty)
            {
                model.SelectedUserId = selectedId;
                model.SelectedUser = await _userManagementApiClient.GetUserDetailsAsync(
                    selectedId.Value,
                    cancellationToken);
                model.SelectedLocation = await _userManagementApiClient.GetUserLocationAsync(
                    selectedId.Value,
                    cancellationToken);
            }
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            model.LoadErrorMessage = "Không tải được danh sách người dùng từ VKFoodAPI. Kiểm tra API và khóa Admin API.";
        }

        return View(model);
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
