using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Models;

public sealed class SystemAdminViewModel
{
    public List<WebAdminAccountViewModel> WebAdminAccounts { get; set; } = [];
    public WebAdminAccountFormViewModel AccountForm { get; set; } = new();
    public List<AdminUserSummaryDto> AppUsers { get; set; } = [];
    public AppUserFormViewModel AppUserForm { get; set; } = new();
    public string AppUserKeyword { get; set; } = string.Empty;
    public string AppUserLoadErrorMessage { get; set; } = string.Empty;
}

public sealed class WebAdminAccountViewModel
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string OwnerCode { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
    public string RoleLabel => IsAdmin ? "Admin" : "Chủ cửa hàng";
}

public sealed class WebAdminAccountFormViewModel
{
    public string OriginalUsername { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "PoiOwner";
    public string OwnerCode { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool IsEditMode => !string.IsNullOrWhiteSpace(OriginalUsername);
}

public sealed class AppUserFormViewModel
{
    public Guid? Id { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string PhoneNumber { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "vi-VN";
    public string DevicePlatform { get; set; } = string.Empty;
    public bool IsEditMode => Id.HasValue && Id.Value != Guid.Empty;
}
