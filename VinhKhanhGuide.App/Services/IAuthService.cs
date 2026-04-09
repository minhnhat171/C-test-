using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public interface IAuthService
{
    event EventHandler? SessionChanged;

    AuthSession? CurrentSession { get; }
    bool IsAuthenticated { get; }

    Task<AuthResult> SignInAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string fullName, string email, string password, string confirmPassword);
    Task SignOutAsync();
}
