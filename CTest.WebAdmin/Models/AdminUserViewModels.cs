using System.ComponentModel.DataAnnotations;

namespace CTest.WebAdmin.Models;

public sealed class AdminUserIndexViewModel
{
    public List<AdminUserListItemViewModel> Items { get; set; } = [];
    public string Query { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int LockedCount { get; set; }
    public int AdminCount { get; set; }
    public int OwnerCount { get; set; }
    public string LoadWarningMessage { get; set; } = string.Empty;
}

public sealed class AdminUserListItemViewModel
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public string OwnerCode { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastLoginAtUtc { get; set; }
    public int OwnedPoiCount { get; set; }
}

public sealed class AdminUserFormViewModel
{
    public string OriginalUsername { get; set; } = string.Empty;

    [Display(Name = "Tên đăng nhập")]
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(80, ErrorMessage = "Tên đăng nhập tối đa 80 ký tự.")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Tên hiển thị")]
    [Required(ErrorMessage = "Vui lòng nhập tên hiển thị.")]
    [StringLength(160, ErrorMessage = "Tên hiển thị tối đa 160 ký tự.")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "Vai trò")]
    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public string Role { get; set; } = "Admin";

    [Display(Name = "Owner code")]
    [StringLength(80, ErrorMessage = "Owner code tối đa 80 ký tự.")]
    public string OwnerCode { get; set; } = string.Empty;

    [Display(Name = "Email chủ cửa hàng")]
    [EmailAddress(ErrorMessage = "Email chủ cửa hàng không hợp lệ.")]
    [StringLength(160, ErrorMessage = "Email tối đa 160 ký tự.")]
    public string OwnerEmail { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu")]
    [DataType(DataType.Password)]
    [StringLength(120, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Cho phép đăng nhập")]
    public bool IsActive { get; set; } = true;

    public DateTimeOffset? CreatedAtUtc { get; set; }
    public DateTimeOffset? LastLoginAtUtc { get; set; }
    public int OwnedPoiCount { get; set; }
    public bool IsEditMode => !string.IsNullOrWhiteSpace(OriginalUsername);
}

public sealed class AdminUserDeleteViewModel
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int OwnedPoiCount { get; set; }
}
