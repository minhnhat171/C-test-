using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public interface IAuthService
{
    event EventHandler? SessionChanged;

    AuthSession? CurrentSession { get; }
    bool IsAuthenticated { get; }

    Task<AuthResult> ContinueAsGuestAsync();
    Task<AuthResult> SignInAsync(string username, string password);
    Task<AuthResult> RegisterAsync(string fullName, string username, string password, string confirmPassword, string role);
    Task<AuthResult> UpdateCurrentUserProfileAsync(AccountProfileUpdateRequest request);
    Task SignOutAsync();
}
