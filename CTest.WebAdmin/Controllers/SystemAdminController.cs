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
    private readonly IWebAdminAccountStore _accountStore;
    private readonly UserManagementApiClient _userManagementApiClient;

    public SystemAdminController(
        IWebAdminAccountStore accountStore,
        UserManagementApiClient userManagementApiClient)
    {
        _accountStore = accountStore;
        _userManagementApiClient = userManagementApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword = null,
        string? editAdmin = null,
        Guid? editAppUser = null,
        CancellationToken cancellationToken = default)
    {
        var model = await BuildModelAsync(keyword, editAdmin, editAppUser, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAccount(
        SystemAdminViewModel model,
        CancellationToken cancellationToken = default)
    {
        var form = model.AccountForm;
        if (!string.Equals(form.Password, form.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(
                $"{nameof(SystemAdminViewModel.AccountForm)}.{nameof(WebAdminAccountFormViewModel.ConfirmPassword)}",
                "Mật khẩu xác nhận không khớp.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildModelAsync(model.AppUserKeyword, null, null, cancellationToken);
            invalidModel.AccountForm = form;
            return View("Index", invalidModel);
        }

        try
        {
            _accountStore.Upsert(
                new WebAdminAuthUserOptions
                {
                    Username = form.Username,
                    DisplayName = form.DisplayName,
                    Role = form.Role,
                    OwnerCode = form.OwnerCode,
                    OwnerEmail = form.OwnerEmail
                },
                form.OriginalUsername,
                form.Password);

            TempData["SystemAdminMessage"] = form.IsEditMode
                ? "Đã cập nhật tài khoản quản trị."
                : "Đã tạo tài khoản quản trị.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var invalidModel = await BuildModelAsync(model.AppUserKeyword, null, null, cancellationToken);
            invalidModel.AccountForm = form;
            return View("Index", invalidModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAccount(string username)
    {
        if (string.Equals(username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["SystemAdminError"] = "Không thể xóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        var existedBeforeDelete = _accountStore.GetByUsername(username) is not null;
        var deleted = _accountStore.Delete(username);
        TempData[deleted ? "SystemAdminMessage" : "SystemAdminError"] = deleted
            ? "Đã xóa tài khoản quản trị."
            : existedBeforeDelete ? "Không thể xóa admin cuối cùng." : "Không tìm thấy tài khoản.";

        return RedirectToAction(nameof(Index));
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
            var invalidModel = await BuildModelAsync(model.AppUserKeyword, null, null, cancellationToken);
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
            var invalidModel = await BuildModelAsync(model.AppUserKeyword, null, null, cancellationToken);
            invalidModel.AppUserForm = form;
            return View("Index", invalidModel);
        }
    }

    private async Task<SystemAdminViewModel> BuildModelAsync(
        string? keyword,
        string? editAdmin,
        Guid? editAppUser,
        CancellationToken cancellationToken)
    {
        var model = new SystemAdminViewModel
        {
            WebAdminAccounts = _accountStore.GetAll()
                .Select(user => new WebAdminAccountViewModel
                {
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Role = user.Role,
                    OwnerCode = user.OwnerCode,
                    OwnerEmail = user.OwnerEmail
                })
                .ToList(),
            AppUserKeyword = keyword?.Trim() ?? string.Empty
        };

        var accountToEdit = _accountStore.GetByUsername(editAdmin);
        if (accountToEdit is not null)
        {
            model.AccountForm = new WebAdminAccountFormViewModel
            {
                OriginalUsername = accountToEdit.Username,
                Username = accountToEdit.Username,
                DisplayName = accountToEdit.DisplayName,
                Role = accountToEdit.Role,
                OwnerCode = accountToEdit.OwnerCode,
                OwnerEmail = accountToEdit.OwnerEmail
            };
        }

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
