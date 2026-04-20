using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Maui.Storage;
using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public class AuthService : IAuthService
{
    private const string GuestRole = "guest";
    private const string UsersPreferenceKey = "vinhkhanh.auth.users.v2";
    private const string LegacyUsersPreferenceKey = "vinhkhanh.auth.users.v1";
    private const string SessionPreferenceKey = "vinhkhanh.auth.session.login.v2";
    private const string LegacySessionPreferenceKey = "vinhkhanh.auth.session.email.v1";
    private const string GuestProfilePreferenceKey = "vinhkhanh.auth.guest-profile.v1";
    private const string GuestUserCodePreferenceKey = "vinhkhanh.auth.guest-user-code.v1";
    private const string DefaultAdminLogin = "user";
    private const string DefaultAdminPassword = "12345678";
    private const string DefaultAdminDisplayName = "Quản trị viên Vĩnh Khánh";
    private const string DefaultAdminEmail = "user@local.vinhkhanh";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Regex UsernamePattern = new(
        "^[a-z0-9._-]{4,24}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public event EventHandler? SessionChanged;

    public AuthSession? CurrentSession { get; private set; }
    public bool IsAuthenticated => CurrentSession is not null;

    public AuthService()
    {
        EnsureDefaultAdminAccount();
        RestoreSession();
    }

    public Task<AuthResult> ContinueAsGuestAsync()
    {
        Preferences.Default.Remove(SessionPreferenceKey);
        Preferences.Default.Remove(LegacySessionPreferenceKey);

        CurrentSession = CreateGuestSession(LoadGuestProfile());
        SessionChanged?.Invoke(this, EventArgs.Empty);

        return Task.FromResult(AuthResult.Success(
            CurrentSession,
            "Đã vào ứng dụng ở chế độ khách tham quan."));
    }

    public Task<AuthResult> SignInAsync(string username, string password)
    {
        var normalizedLogin = NormalizeUsername(username);

        if (string.IsNullOrWhiteSpace(normalizedLogin) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(AuthResult.Failure("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu."));
        }

        var users = LoadUsers();
        var matchedUser = FindUserByLogin(users, normalizedLogin);

        if (matchedUser is null)
        {
            return Task.FromResult(AuthResult.Failure("Tài khoản không tồn tại."));
        }

        if (!VerifyPassword(password, matchedUser.PasswordSalt, matchedUser.PasswordHash))
        {
            return Task.FromResult(AuthResult.Failure("Sai mật khẩu."));
        }

        matchedUser.LastSignedInAtUtc = DateTimeOffset.UtcNow;
        SaveUsers(users);
        SetCurrentSession(matchedUser);

        return Task.FromResult(AuthResult.Success(
            CurrentSession!,
            $"Đăng nhập thành công. Xin chào, {matchedUser.FullName}."));
    }

    public Task<AuthResult> RegisterAsync(
        string fullName,
        string username,
        string password,
        string confirmPassword,
        string role)
    {
        fullName = fullName.Trim();
        var normalizedUsername = NormalizeUsername(username);
        var normalizedRole = NormalizeRole(role);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Task.FromResult(AuthResult.Failure("Vui lòng nhập họ tên của bạn."));
        }

        if (fullName.Length < 2)
        {
            return Task.FromResult(AuthResult.Failure("Họ tên cần có ít nhất 2 ký tự."));
        }

        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return Task.FromResult(AuthResult.Failure("Vui lòng nhập tên đăng nhập."));
        }

        if (!UsernamePattern.IsMatch(normalizedUsername))
        {
            return Task.FromResult(AuthResult.Failure(
                "Tên đăng nhập chỉ gồm chữ thường, số, dấu chấm, gạch ngang hoặc gạch dưới và dài 4-24 ký tự."));
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
        var accountExists = users.Any(user =>
            string.Equals(user.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase));

        if (accountExists)
        {
            return Task.FromResult(AuthResult.Failure("Tài khoản đã tồn tại."));
        }

        var passwordSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var passwordHash = ComputePasswordHash(password, passwordSalt);

        var newUser = new AuthUser
        {
            FullName = fullName,
            Username = normalizedUsername,
            Email = string.Empty,
            PasswordSalt = passwordSalt,
            PasswordHash = passwordHash,
            Role = normalizedRole,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        users.Add(newUser);
        SaveUsers(users);

        return Task.FromResult(AuthResult.Success(
            "Đăng ký thành công. Hãy đăng nhập bằng tên đăng nhập vừa tạo."));
    }

    public Task<AuthResult> UpdateCurrentUserProfileAsync(AccountProfileUpdateRequest request)
    {
        if (CurrentSession is null)
        {
            return Task.FromResult(AuthResult.Failure("Không tìm thấy phiên người dùng hiện tại."));
        }

        if (string.Equals(CurrentSession.Role, GuestRole, StringComparison.OrdinalIgnoreCase))
        {
            var guestProfile = new GuestProfileRecord
            {
                FullName = request.FullName.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email)
                    ? string.Empty
                    : NormalizeEmail(request.Email),
                PhoneNumber = NormalizePhoneNumber(request.PhoneNumber)
            };

            SaveGuestProfile(guestProfile);
            CurrentSession = CreateGuestSession(guestProfile);
            SessionChanged?.Invoke(this, EventArgs.Empty);

            return Task.FromResult(AuthResult.Success(
                CurrentSession,
                "Đã cập nhật thông tin khách tham quan trên thiết bị này."));
        }

        var users = LoadUsers();
        var currentUser = users.FirstOrDefault(user =>
            user.Id == CurrentSession.UserId ||
            string.Equals(user.Username, CurrentSession.Username, StringComparison.OrdinalIgnoreCase));

        if (currentUser is null)
        {
            return Task.FromResult(AuthResult.Failure("Không tìm thấy hồ sơ người dùng hiện tại."));
        }

        var normalizedEmail = string.IsNullOrWhiteSpace(request.Email)
            ? string.Empty
            : NormalizeEmail(request.Email);

        var emailExists = !string.IsNullOrWhiteSpace(normalizedEmail) &&
            users.Any(user =>
                user.Id != currentUser.Id &&
                string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase));

        if (emailExists)
        {
            return Task.FromResult(AuthResult.Failure("Email này đã được sử dụng bởi tài khoản khác."));
        }

        currentUser.FullName = request.FullName.Trim();
        currentUser.Email = normalizedEmail;
        currentUser.PhoneNumber = NormalizePhoneNumber(request.PhoneNumber);

        SaveUsers(users);
        SetCurrentSession(currentUser);

        return Task.FromResult(AuthResult.Success(
            CurrentSession!,
            "Cập nhật thông tin tài khoản thành công."));
    }

    public Task SignOutAsync()
    {
        CurrentSession = null;
        Preferences.Default.Remove(SessionPreferenceKey);
        Preferences.Default.Remove(LegacySessionPreferenceKey);
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private void RestoreSession()
    {
        var sessionLogin = Preferences.Default.Get(SessionPreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(sessionLogin))
        {
            sessionLogin = Preferences.Default.Get(LegacySessionPreferenceKey, string.Empty);
        }

        var normalizedLogin = NormalizeUsername(sessionLogin);
        if (string.IsNullOrWhiteSpace(normalizedLogin))
        {
            return;
        }

        var user = FindUserByLogin(LoadUsers(), normalizedLogin);

        if (user is null)
        {
            Preferences.Default.Remove(SessionPreferenceKey);
            Preferences.Default.Remove(LegacySessionPreferenceKey);
            return;
        }

        CurrentSession = ToSession(user);
    }

    private void SetCurrentSession(AuthUser user)
    {
        CurrentSession = ToSession(user);
        Preferences.Default.Set(SessionPreferenceKey, user.Username);
        Preferences.Default.Remove(LegacySessionPreferenceKey);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    private AuthSession CreateGuestSession(GuestProfileRecord? guestProfile = null)
    {
        var fullName = guestProfile?.FullName?.Trim() ?? string.Empty;
        return new AuthSession
        {
            UserId = Guid.Empty,
            UserCode = GetOrCreateGuestUserCode(),
            FullName = fullName,
            Username = "guest",
            Email = guestProfile?.Email ?? string.Empty,
            PhoneNumber = guestProfile?.PhoneNumber ?? string.Empty,
            Role = GuestRole
        };
    }

    private static AuthSession ToSession(AuthUser user)
    {
        return new AuthSession
        {
            UserId = user.Id,
            UserCode = string.IsNullOrWhiteSpace(user.Username)
                ? NormalizeEmail(user.Email)
                : user.Username,
            FullName = user.FullName,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role
        };
    }

    private static string GetOrCreateGuestUserCode()
    {
        var existing = Preferences.Default.Get(GuestUserCodePreferenceKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing.Trim().ToLowerInvariant();
        }

        var generated = $"guest-{Guid.NewGuid():N}"[..14];
        Preferences.Default.Set(GuestUserCodePreferenceKey, generated);
        return generated;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }

    private static string NormalizePhoneNumber(string? phoneNumber)
    {
        return phoneNumber?.Trim() ?? string.Empty;
    }

    private static string NormalizeRole(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "admin" => "admin",
            "poi_owner" => "poi_owner",
            _ => "user"
        };
    }

    private static bool LooksLikeEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
               email.Contains('@', StringComparison.Ordinal) &&
               email.Contains('.', StringComparison.Ordinal);
    }

    private static AuthUser? FindUserByLogin(IEnumerable<AuthUser> users, string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        return users.FirstOrDefault(user =>
            string.Equals(user.Username, login, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(user.Email) &&
             string.Equals(user.Email, login, StringComparison.OrdinalIgnoreCase)));
    }

    private GuestProfileRecord? LoadGuestProfile()
    {
        var json = Preferences.Default.Get(GuestProfilePreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<GuestProfileRecord>(json, JsonOptions);
        }
        catch
        {
            Preferences.Default.Remove(GuestProfilePreferenceKey);
            return null;
        }
    }

    private void SaveGuestProfile(GuestProfileRecord guestProfile)
    {
        var json = JsonSerializer.Serialize(guestProfile, JsonOptions);
        Preferences.Default.Set(GuestProfilePreferenceKey, json);
    }

    private List<AuthUser> LoadUsers()
    {
        var json = Preferences.Default.Get(UsersPreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            json = Preferences.Default.Get(LegacyUsersPreferenceKey, string.Empty);
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var users = JsonSerializer.Deserialize<List<AuthUser>>(json, JsonOptions) ?? [];
            var normalizedUsers = NormalizeUsers(users);
            SaveUsers(normalizedUsers);
            return normalizedUsers;
        }
        catch
        {
            Preferences.Default.Remove(UsersPreferenceKey);
            Preferences.Default.Remove(LegacyUsersPreferenceKey);
            return [];
        }
    }

    private void SaveUsers(List<AuthUser> users)
    {
        var normalizedUsers = NormalizeUsers(users);
        var json = JsonSerializer.Serialize(normalizedUsers, JsonOptions);
        Preferences.Default.Set(UsersPreferenceKey, json);
        Preferences.Default.Remove(LegacyUsersPreferenceKey);
    }

    private static List<AuthUser> NormalizeUsers(IEnumerable<AuthUser> users)
    {
        var normalizedUsers = new List<AuthUser>();
        var seenLogins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var user in users)
        {
            if (user is null)
            {
                continue;
            }

            var normalizedEmail = LooksLikeEmail(user.Email) ? NormalizeEmail(user.Email) : string.Empty;
            var normalizedUsername = NormalizeUsername(user.Username);

            if (string.IsNullOrWhiteSpace(normalizedUsername))
            {
                normalizedUsername = NormalizeUsername(user.Email);
            }

            if (string.IsNullOrWhiteSpace(normalizedUsername))
            {
                normalizedUsername = $"user-{Guid.NewGuid():N}"[..13];
            }

            var uniqueUsername = normalizedUsername;
            var suffix = 1;
            while (!seenLogins.Add(uniqueUsername))
            {
                uniqueUsername = $"{normalizedUsername}{suffix}";
                suffix++;
            }

            user.Id = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id;
            user.FullName = string.IsNullOrWhiteSpace(user.FullName) ? uniqueUsername : user.FullName.Trim();
            user.Username = uniqueUsername;
            user.Email = normalizedEmail;
            user.PhoneNumber = NormalizePhoneNumber(user.PhoneNumber);
            user.Role = NormalizeRole(user.Role);
            user.CreatedAtUtc = user.CreatedAtUtc == default ? DateTimeOffset.UtcNow : user.CreatedAtUtc;

            normalizedUsers.Add(user);
        }

        return normalizedUsers;
    }

    private void EnsureDefaultAdminAccount()
    {
        var users = LoadUsers();
        var adminUser = users.FirstOrDefault(user =>
            string.Equals(user.Username, DefaultAdminLogin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Email, DefaultAdminLogin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Email, DefaultAdminEmail, StringComparison.OrdinalIgnoreCase));

        var passwordSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var passwordHash = ComputePasswordHash(DefaultAdminPassword, passwordSalt);

        if (adminUser is null)
        {
            users.Add(new AuthUser
            {
                FullName = DefaultAdminDisplayName,
                Username = DefaultAdminLogin,
                Email = DefaultAdminEmail,
                PasswordSalt = passwordSalt,
                PasswordHash = passwordHash,
                Role = "admin",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        else
        {
            adminUser.FullName = DefaultAdminDisplayName;
            adminUser.Username = DefaultAdminLogin;
            adminUser.Email = DefaultAdminEmail;
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

    private sealed class GuestProfileRecord
    {
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
    }
}
