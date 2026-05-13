using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
public sealed class SystemAdminController : Controller
{
    private readonly UserManagementApiClient _userManagementApiClient;

    public SystemAdminController(UserManagementApiClient userManagementApiClient)
    {
        _userManagementApiClient = userManagementApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword = null,
        Guid? editAppUser = null,
        CancellationToken cancellationToken = default)
    {
        var model = await BuildModelAsync(keyword, editAppUser, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAppUser(
        SystemAdminViewModel model,
        CancellationToken cancellationToken = default)
    {
        var form = model.AppUserForm;
        if (string.IsNullOrWhiteSpace(form.UserCode) &&
            string.IsNullOrWhiteSpace(form.Email))
        {
            ModelState.AddModelError(
                $"{nameof(SystemAdminViewModel.AppUserForm)}.{nameof(AppUserFormViewModel.UserCode)}",
                "Cần nhập mã người dùng hoặc email.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildModelAsync(model.AppUserKeyword, null, cancellationToken);
            invalidModel.AppUserForm = form;
            return View("Index", invalidModel);
        }

        try
        {
            await _userManagementApiClient.UpsertProfileAsync(
                new AdminUserProfileUpsertRequest
                {
                    Id = form.Id,
                    UserCode = form.UserCode,
                    DisplayName = form.DisplayName,
                    Email = form.Email,
                    Role = form.Role,
                    PhoneNumber = form.PhoneNumber,
                    PreferredLanguage = form.PreferredLanguage,
                    DevicePlatform = form.DevicePlatform
                },
                cancellationToken);

            TempData["SystemAdminMessage"] = form.IsEditMode
                ? "Đã cập nhật người dùng app."
                : "Đã thêm người dùng app.";
            return RedirectToAction(nameof(Index), new { keyword = model.AppUserKeyword });
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is InvalidOperationException ||
            ex is TaskCanceledException)
        {
            ModelState.AddModelError(string.Empty, "Chưa cập nhật được người dùng app. Kiểm tra VKFoodAPI và API key admin.");
            var invalidModel = await BuildModelAsync(model.AppUserKeyword, null, cancellationToken);
            invalidModel.AppUserForm = form;
            return View("Index", invalidModel);
        }
    }

    private async Task<SystemAdminViewModel> BuildModelAsync(
        string? keyword,
        Guid? editAppUser,
        CancellationToken cancellationToken)
    {
        var model = new SystemAdminViewModel
        {
            AppUserKeyword = keyword?.Trim() ?? string.Empty
        };

        try
        {
            model.AppUsers = (await _userManagementApiClient.SearchUsersAsync(
                    model.AppUserKeyword,
                    cancellationToken))
                .ToList();

            if (editAppUser.HasValue)
            {
                var detail = await _userManagementApiClient.GetUserDetailsAsync(editAppUser.Value, cancellationToken);
                if (detail is not null)
                {
                    model.AppUserForm = new AppUserFormViewModel
                    {
                        Id = detail.Id,
                        UserCode = detail.UserCode,
                        DisplayName = detail.DisplayName,
                        Email = detail.Email,
                        Role = detail.Role,
                        PhoneNumber = detail.PhoneNumber,
                        PreferredLanguage = detail.PreferredLanguage,
                        DevicePlatform = detail.DevicePlatform
                    };
                }
            }
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is InvalidOperationException ||
            ex is TaskCanceledException)
        {
            model.AppUserLoadErrorMessage = "Không tải được danh sách người dùng app từ VKFoodAPI.";
        }

        return model;
    }
}
