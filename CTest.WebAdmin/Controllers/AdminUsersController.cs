using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
public sealed class AdminUsersController : Controller
{
    private readonly IWebAdminAccountStore _accountStore;

    public AdminUsersController(IWebAdminAccountStore accountStore)
    {
        _accountStore = accountStore;
    }

    [HttpGet]
    public IActionResult Index(string? editAdmin = null)
    {
        return View(BuildModel(editAdmin));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveAccount(AdminUsersViewModel model)
    {
        var form = model.AccountForm;
        if (!string.Equals(form.Password, form.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(
                $"{nameof(AdminUsersViewModel.AccountForm)}.{nameof(WebAdminAccountFormViewModel.ConfirmPassword)}",
                "Mật khẩu xác nhận không khớp.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = BuildModel(null);
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

            TempData["AdminUsersMessage"] = form.IsEditMode
                ? "Đã cập nhật tài khoản quản trị."
                : "Đã tạo tài khoản quản trị.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var invalidModel = BuildModel(null);
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
            TempData["AdminUsersError"] = "Không thể xóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        var existedBeforeDelete = _accountStore.GetByUsername(username) is not null;
        var deleted = _accountStore.Delete(username);
        TempData[deleted ? "AdminUsersMessage" : "AdminUsersError"] = deleted
            ? "Đã xóa tài khoản quản trị."
            : existedBeforeDelete ? "Không thể xóa admin cuối cùng." : "Không tìm thấy tài khoản.";

        return RedirectToAction(nameof(Index));
    }

    private AdminUsersViewModel BuildModel(string? editAdmin)
    {
        var model = new AdminUsersViewModel
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
                .ToList()
        };

        var accountToEdit = _accountStore.GetByUsername(editAdmin);
        if (accountToEdit is null)
        {
            return model;
        }

        model.AccountForm = new WebAdminAccountFormViewModel
        {
            OriginalUsername = accountToEdit.Username,
            Username = accountToEdit.Username,
            DisplayName = accountToEdit.DisplayName,
            Role = accountToEdit.Role,
            OwnerCode = accountToEdit.OwnerCode,
            OwnerEmail = accountToEdit.OwnerEmail
        };

        return model;
    }
}
