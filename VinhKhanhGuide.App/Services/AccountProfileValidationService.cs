using System.Text.RegularExpressions;
using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public sealed class AccountProfileValidationService : IAccountProfileValidationService
{
    private static readonly Regex EmailPattern = new(
        "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex PhonePattern = new(
        "^[0-9+\\-\\s]{9,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public AccountProfileValidationResult Validate(AccountProfileUpdateRequest request)
    {
        var errors = new List<string>();
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            if (fullName.Length < 2)
            {
                errors.Add("Họ và tên cần có ít nhất 2 ký tự.");
            }
            else if (fullName.Length > 80)
            {
                errors.Add("Họ và tên không được vượt quá 80 ký tự.");
            }
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            if (email.Length > 120)
            {
                errors.Add("Email không được vượt quá 120 ký tự.");
            }
            else if (!EmailPattern.IsMatch(email))
            {
                errors.Add("Email chưa đúng định dạng.");
            }
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            if (!PhonePattern.IsMatch(phoneNumber))
            {
                errors.Add("Số điện thoại chỉ nên gồm số và dài từ 9 đến 20 ký tự.");
            }
        }

        return new AccountProfileValidationResult
        {
            Errors = errors
        };
    }
}
