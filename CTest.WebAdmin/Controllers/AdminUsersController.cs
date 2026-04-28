using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
public sealed class AdminUsersController : Controller
{
    private const string DefaultResetPassword = "123456";

    private readonly IWebAdminAccountStore _accountStore;
    private readonly PoiApiClient _poiApiClient;

    public AdminUsersController(
        IWebAdminAccountStore accountStore,
        PoiApiClient poiApiClient)
    {
        _accountStore = accountStore;
        _poiApiClient = poiApiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? query = null,
        string? role = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var model = await BuildIndexModelAsync(query, role, status, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new AdminUserFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(AdminUserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Vui lòng nhập mật khẩu cho tài khoản mới.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            _accountStore.Upsert(ToAccountOptions(model), null, model.Password);
            TempData["AdminUsersMessage"] = "Đã tạo tài khoản mới.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Edit(string username)
    {
        var account = _accountStore.GetByUsername(username);
        if (account is null)
        {
            return NotFound();
        }

        return View(ToForm(account));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(string username, AdminUserFormViewModel model)
    {
        if (!string.Equals(username, model.OriginalUsername, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            _accountStore.Upsert(ToAccountOptions(model), model.OriginalUsername, model.Password);
            TempData["AdminUsersMessage"] = "Đã cập nhật tài khoản.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(string username)
    {
        try
        {
            var ok = _accountStore.ResetPassword(username, DefaultResetPassword);
            TempData[ok ? "AdminUsersMessage" : "AdminUsersError"] = ok
                ? $"Đã reset mật khẩu về {DefaultResetPassword}."
                : "Không tìm thấy tài khoản để reset mật khẩu.";
        }
        catch (ArgumentException ex)
        {
            TempData["AdminUsersError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string username, CancellationToken cancellationToken = default)
    {
        var account = _accountStore.GetByUsername(username);
        if (account is null)
        {
            return NotFound();
        }

        var (ownedPoiCounts, _) = await LoadOwnedPoiCountsAsync([account], cancellationToken);
        return View(new AdminUserDeleteViewModel
        {
            Username = account.Username,
            DisplayName = account.DisplayName,
            RoleLabel = RoleLabel(account.Role),
            IsActive = account.IsActive,
            OwnedPoiCount = ownedPoiCounts.GetValueOrDefault(account.Username)
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(string username)
    {
        if (string.Equals(username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["AdminUsersError"] = "Không thể xóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        var deleted = _accountStore.Delete(username);
        TempData[deleted ? "AdminUsersMessage" : "AdminUsersError"] = deleted
            ? "Đã xóa tài khoản."
            : "Không thể xóa tài khoản. Cần giữ ít nhất một tài khoản Admin.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<AdminUserIndexViewModel> BuildIndexModelAsync(
        string? query,
        string? role,
        string? status,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = NormalizeLookup(query);
        var normalizedRole = NormalizeRoleFilter(role);
        var normalizedStatus = NormalizeStatus(status);
        var accounts = _accountStore.GetAll();
        var (ownedPoiCounts, loadWarning) = await LoadOwnedPoiCountsAsync(accounts, cancellationToken);

        var filtered = accounts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            filtered = filtered.Where(account =>
                NormalizeLookup(account.Username).Contains(normalizedQuery, StringComparison.Ordinal) ||
                NormalizeLookup(account.DisplayName).Contains(normalizedQuery, StringComparison.Ordinal) ||
                NormalizeLookup(account.OwnerCode).Contains(normalizedQuery, StringComparison.Ordinal) ||
                NormalizeLookup(account.OwnerEmail).Contains(normalizedQuery, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(normalizedRole))
        {
            filtered = filtered.Where(account =>
                string.Equals(NormalizeRole(account.Role), normalizedRole, StringComparison.OrdinalIgnoreCase));
        }

        if (normalizedStatus == "active")
        {
            filtered = filtered.Where(account => account.IsActive);
        }
        else if (normalizedStatus == "locked")
        {
            filtered = filtered.Where(account => !account.IsActive);
        }

        return new AdminUserIndexViewModel
        {
            Query = query?.Trim() ?? string.Empty,
            Role = normalizedRole,
            Status = normalizedStatus,
            TotalCount = accounts.Count,
            ActiveCount = accounts.Count(account => account.IsActive),
            LockedCount = accounts.Count(account => !account.IsActive),
            AdminCount = accounts.Count(account =>
                string.Equals(NormalizeRole(account.Role), WebAdminRoles.Admin, StringComparison.OrdinalIgnoreCase)),
            OwnerCount = accounts.Count(account =>
                string.Equals(NormalizeRole(account.Role), WebAdminRoles.PoiOwner, StringComparison.OrdinalIgnoreCase)),
            LoadWarningMessage = loadWarning,
            Items = filtered
                .OrderByDescending(account => account.IsActive)
                .ThenBy(account => string.Equals(NormalizeRole(account.Role), WebAdminRoles.Admin, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(account => account.Username)
                .Select(account => new AdminUserListItemViewModel
                {
                    Username = account.Username,
                    DisplayName = account.DisplayName,
                    Role = NormalizeRole(account.Role),
                    RoleLabel = RoleLabel(account.Role),
                    OwnerCode = account.OwnerCode,
                    OwnerEmail = account.OwnerEmail,
                    IsActive = account.IsActive,
                    CreatedAtUtc = account.CreatedAtUtc,
                    LastLoginAtUtc = account.LastLoginAtUtc,
                    OwnedPoiCount = ownedPoiCounts.GetValueOrDefault(account.Username)
                })
                .ToList()
        };
    }

    private async Task<(Dictionary<string, int> Counts, string Warning)> LoadOwnedPoiCountsAsync(
        IReadOnlyList<WebAdminAuthUserOptions> accounts,
        CancellationToken cancellationToken)
    {
        var counts = accounts.ToDictionary(
            account => account.Username,
            _ => 0,
            StringComparer.OrdinalIgnoreCase);

        if (!accounts.Any(account =>
                string.Equals(NormalizeRole(account.Role), WebAdminRoles.PoiOwner, StringComparison.OrdinalIgnoreCase)))
        {
            return (counts, string.Empty);
        }

        List<PoiDto> pois;
        try
        {
            pois = await _poiApiClient.GetPoisAsync(cancellationToken);
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is InvalidOperationException ||
            ex is TaskCanceledException)
        {
            return (counts, "Không tải được số POI sở hữu từ VKFoodAPI.");
        }

        foreach (var account in accounts)
        {
            if (!string.Equals(NormalizeRole(account.Role), WebAdminRoles.PoiOwner, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var ownerCode = NormalizeLookup(string.IsNullOrWhiteSpace(account.OwnerCode)
                ? account.Username
                : account.OwnerCode);
            var ownerEmail = NormalizeLookup(account.OwnerEmail);

            counts[account.Username] = pois.Count(poi =>
                (!string.IsNullOrWhiteSpace(ownerCode) &&
                 string.Equals(NormalizeLookup(poi.OwnerUserCode), ownerCode, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(ownerEmail) &&
                 string.Equals(NormalizeLookup(poi.OwnerEmail), ownerEmail, StringComparison.OrdinalIgnoreCase)));
        }

        return (counts, string.Empty);
    }

    private static AdminUserFormViewModel ToForm(WebAdminAuthUserOptions account)
    {
        return new AdminUserFormViewModel
        {
            OriginalUsername = account.Username,
            Username = account.Username,
            DisplayName = account.DisplayName,
            Role = NormalizeRole(account.Role),
            OwnerCode = account.OwnerCode,
            OwnerEmail = account.OwnerEmail,
            IsActive = account.IsActive,
            CreatedAtUtc = account.CreatedAtUtc,
            LastLoginAtUtc = account.LastLoginAtUtc
        };
    }

    private static WebAdminAuthUserOptions ToAccountOptions(AdminUserFormViewModel model)
    {
        return new WebAdminAuthUserOptions
        {
            Username = model.Username,
            DisplayName = model.DisplayName,
            Role = NormalizeRole(model.Role),
            OwnerCode = model.OwnerCode,
            OwnerEmail = model.OwnerEmail,
            IsActive = model.IsActive,
            CreatedAtUtc = model.CreatedAtUtc ?? DateTimeOffset.UtcNow,
            LastLoginAtUtc = model.LastLoginAtUtc
        };
    }

    private static string RoleLabel(string? role)
    {
        return string.Equals(NormalizeRole(role), WebAdminRoles.Admin, StringComparison.OrdinalIgnoreCase)
            ? "Admin"
            : "Chủ cửa hàng";
    }

    private static string NormalizeRoleFilter(string? role)
    {
        var normalized = NormalizeRole(role);
        return string.IsNullOrWhiteSpace(role) ? string.Empty : normalized;
    }

    private static string NormalizeRole(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "admin" => WebAdminRoles.Admin,
            "restaurantowner" => WebAdminRoles.PoiOwner,
            "poi_owner" => WebAdminRoles.PoiOwner,
            "poiowner" => WebAdminRoles.PoiOwner,
            "owner" => WebAdminRoles.PoiOwner,
            _ => WebAdminRoles.PoiOwner
        };
    }

    private static string NormalizeStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "active" => "active",
            "locked" => "locked",
            _ => string.Empty
        };
    }

    private static string NormalizeLookup(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
