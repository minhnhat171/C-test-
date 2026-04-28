using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Models;

public sealed class SystemAdminViewModel
{
    public List<AdminUserSummaryDto> AppUsers { get; set; } = [];
    public AppUserFormViewModel AppUserForm { get; set; } = new();
    public string AppUserKeyword { get; set; } = string.Empty;
    public string AppUserLoadErrorMessage { get; set; } = string.Empty;
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
