namespace VinhKhanhGuide.App.Models;

public sealed class AccountProfileUpdateRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}

public sealed class AccountProfileValidationResult
{
    public IReadOnlyList<string> Errors { get; init; } = [];
    public bool IsValid => Errors.Count == 0;
    public string ErrorMessage => string.Join(Environment.NewLine, Errors);
}
