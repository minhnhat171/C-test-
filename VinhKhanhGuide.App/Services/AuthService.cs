using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public class AuthService : IAuthService
{
    private const string UsersPreferenceKey = "vinhkhanh.auth.users.v1";
    private const string SessionPreferenceKey = "vinhkhanh.auth.session.email.v1";
    private const string DefaultAdminLogin = "user";
    private const string DefaultAdminPassword = "12345";
    private const string DefaultAdminDisplayName = "Quản trị viên";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public event EventHandler? SessionChanged;

    public AuthSession? CurrentSession { get; private set; }
    public bool IsAuthenticated => CurrentSession is not null;

    public AuthService()
    {
        EnsureDefaultAdminAccount();
        RestoreSession();
    }

    public Task<AuthResult> SignInAsync(string email, string password)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(AuthResult.Failure("Vui lòng nhập đầy đủ email và mật khẩu."));
        }

        var users = LoadUsers();
        var matchedUser = users.FirstOrDefault(user =>
            string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase));

        if (matchedUser is null || !VerifyPassword(password, matchedUser.PasswordSalt, matchedUser.PasswordHash))
        {
            return Task.FromResult(AuthResult.Failure("Email hoặc mật khẩu chưa đúng."));
        }

        matchedUser.LastSignedInAtUtc = DateTimeOffset.UtcNow;
        SaveUsers(users);
        SetCurrentSession(matchedUser);

        return Task.FromResult(AuthResult.Success(
            CurrentSession!,
            $"Chào mừng bạn quay lại, {matchedUser.FullName}."));
    }

    public Task<AuthResult> RegisterAsync(
        string fullName,
        string email,
        string password,
        string confirmPassword)
    {
        fullName = fullName.Trim();
        var normalizedEmail = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Task.FromResult(AuthResult.Failure("Vui lòng nhập họ tên của bạn."));
        }

        if (fullName.Length < 2)
        {
            return Task.FromResult(AuthResult.Failure("Họ tên cần có ít nhất 2 ký tự."));
        }

        if (!LooksLikeEmail(normalizedEmail))
        {
            return Task.FromResult(AuthResult.Failure("Email chưa đúng định dạng."));
        }

        if (password.Length < 8)
        {
            return Task.FromResult(AuthResult.Failure("Mật khẩu cần có ít nhất 8 ký tự."));
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthResult.Failure("Mật khẩu xác nhận chưa khớp."));
        }

        var users = LoadUsers();
        var emailExists = users.Any(user =>
            string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase));

        if (emailExists)
        {
            return Task.FromResult(AuthResult.Failure("Email này đã được đăng ký."));
        }

        var passwordSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var passwordHash = ComputePasswordHash(password, passwordSalt);

        var newUser = new AuthUser
        {
            FullName = fullName,
            Email = normalizedEmail,
            PasswordSalt = passwordSalt,
            PasswordHash = passwordHash,
            Role = "user",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSignedInAtUtc = DateTimeOffset.UtcNow
        };

        users.Add(newUser);
        SaveUsers(users);
        SetCurrentSession(newUser);

        return Task.FromResult(AuthResult.Success(
            CurrentSession!,
            $"Tạo tài khoản thành công. Chào mừng {newUser.FullName}."));
    }

    public Task SignOutAsync()
    {
        CurrentSession = null;
        Preferences.Default.Remove(SessionPreferenceKey);
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private void RestoreSession()
    {
        var sessionEmail = Preferences.Default.Get(SessionPreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(sessionEmail))
        {
            return;
        }

        var user = LoadUsers().FirstOrDefault(item =>
            string.Equals(item.Email, sessionEmail, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            Preferences.Default.Remove(SessionPreferenceKey);
            return;
        }

        CurrentSession = ToSession(user);
    }

    private void SetCurrentSession(AuthUser user)
    {
        CurrentSession = ToSession(user);
        Preferences.Default.Set(SessionPreferenceKey, user.Email);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static AuthSession ToSession(AuthUser user)
    {
        return new AuthSession
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }

    private static bool LooksLikeEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
               email.Contains('@', StringComparison.Ordinal) &&
               email.Contains('.', StringComparison.Ordinal);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private List<AuthUser> LoadUsers()
    {
        var json = Preferences.Default.Get(UsersPreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<AuthUser>>(json, JsonOptions) ?? [];
        }
        catch
        {
            Preferences.Default.Remove(UsersPreferenceKey);
            return [];
        }
    }

    private void SaveUsers(List<AuthUser> users)
    {
        var json = JsonSerializer.Serialize(users, JsonOptions);
        Preferences.Default.Set(UsersPreferenceKey, json);
    }

    private void EnsureDefaultAdminAccount()
    {
        var users = LoadUsers();
        var adminUser = users.FirstOrDefault(user =>
            string.Equals(user.Email, DefaultAdminLogin, StringComparison.OrdinalIgnoreCase));

        var passwordSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var passwordHash = ComputePasswordHash(DefaultAdminPassword, passwordSalt);

        if (adminUser is null)
        {
            users.Add(new AuthUser
            {
                FullName = DefaultAdminDisplayName,
                Email = DefaultAdminLogin,
                PasswordSalt = passwordSalt,
                PasswordHash = passwordHash,
                Role = "admin",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        else
        {
            adminUser.FullName = DefaultAdminDisplayName;
            adminUser.Email = DefaultAdminLogin;
            adminUser.PasswordSalt = passwordSalt;
            adminUser.PasswordHash = passwordHash;
            adminUser.Role = "admin";
        }

        SaveUsers(users);
    }

    private static string ComputePasswordHash(string password, string salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes($"{password}|{salt}");
        var hashBytes = SHA256.HashData(passwordBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static bool VerifyPassword(string password, string salt, string hash)
    {
        var computedHash = ComputePasswordHash(password, salt);
        return string.Equals(computedHash, hash, StringComparison.Ordinal);
    }
}
