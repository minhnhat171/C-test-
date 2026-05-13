using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CTest.WebAdmin.Security;

public interface IWebAdminAccountStore
{
    IReadOnlyList<WebAdminAuthUserOptions> GetAll();
    WebAdminAuthUserOptions? GetByUsername(string? username);
    WebAdminAuthUserOptions Upsert(
        WebAdminAuthUserOptions request,
        string? originalUsername,
        string? plainPassword);
    bool ResetPassword(string? username, string plainPassword);
    bool RecordLogin(string? username);
    bool Delete(string? username);
}

public sealed class WebAdminAccountStore : IWebAdminAccountStore
{
    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly WebAdminAuthOptions _options;
    private readonly string _dataFilePath;
    private List<WebAdminAuthUserOptions> _users;

    public WebAdminAccountStore(
        IHostEnvironment environment,
        IOptions<WebAdminAuthOptions> options)
    {
        _options = options.Value;
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _dataFilePath = Path.Combine(dataDirectory, "web-admin-users.json");
        _users = LoadUsers();
    }

    public IReadOnlyList<WebAdminAuthUserOptions> GetAll()
    {
        lock (_syncRoot)
        {
            return _users
                .OrderBy(user => string.Equals(user.Role, WebAdminRoles.Admin, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(user => user.Username)
                .Select(Clone)
                .ToList();
        }
    }

    public WebAdminAuthUserOptions? GetByUsername(string? username)
    {
        var normalizedUsername = NormalizeLookup(username);
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return null;
        }

        lock (_syncRoot)
        {
            return _users.FirstOrDefault(user =>
                    string.Equals(NormalizeLookup(user.Username), normalizedUsername, StringComparison.OrdinalIgnoreCase))
                is { } match
                    ? Clone(match)
                    : null;
        }
    }

    public WebAdminAuthUserOptions Upsert(
        WebAdminAuthUserOptions request,
        string? originalUsername,
        string? plainPassword)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_syncRoot)
        {
            var normalizedOriginalUsername = NormalizeLookup(originalUsername);
            var normalized = NormalizeUser(request);
            var index = _users.FindIndex(user =>
                !string.IsNullOrWhiteSpace(normalizedOriginalUsername)
                    ? string.Equals(NormalizeLookup(user.Username), normalizedOriginalUsername, StringComparison.OrdinalIgnoreCase)
                    : string.Equals(NormalizeLookup(user.Username), NormalizeLookup(normalized.Username), StringComparison.OrdinalIgnoreCase));
            var existing = index >= 0 ? _users[index] : null;

            ValidateForSave(normalized, existing, plainPassword);

            normalized.CreatedAtUtc = existing?.CreatedAtUtc ?? DateTimeOffset.UtcNow;
            normalized.LastLoginAtUtc = existing?.LastLoginAtUtc;

            if (!string.IsNullOrWhiteSpace(plainPassword))
            {
                normalized.PasswordHash = WebAdminPasswordHasher.HashPassword(plainPassword.Trim());
            }
            else if (existing is not null)
            {
                normalized.PasswordHash = existing.PasswordHash;
            }

            normalized.Password = string.Empty;

            if (index >= 0)
            {
                _users[index] = normalized;
            }
            else
            {
                _users.Add(normalized);
            }

            SaveUnsafe();
            return Clone(normalized);
        }
    }

    public bool ResetPassword(string? username, string plainPassword)
    {
        var normalizedUsername = NormalizeLookup(username);
        if (string.IsNullOrWhiteSpace(normalizedUsername) ||
            string.IsNullOrWhiteSpace(plainPassword))
        {
            return false;
        }

        lock (_syncRoot)
        {
            var user = _users.FirstOrDefault(item =>
                string.Equals(NormalizeLookup(item.Username), normalizedUsername, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                return false;
            }

            if (plainPassword.Trim().Length < 6)
            {
                throw new ArgumentException("Mật khẩu phải có ít nhất 6 ký tự.", nameof(plainPassword));
            }

            user.PasswordHash = WebAdminPasswordHasher.HashPassword(plainPassword.Trim());
            user.Password = string.Empty;
            SaveUnsafe();
            return true;
        }
    }

    public bool RecordLogin(string? username)
    {
        var normalizedUsername = NormalizeLookup(username);
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return false;
        }

        lock (_syncRoot)
        {
            var user = _users.FirstOrDefault(item =>
                string.Equals(NormalizeLookup(item.Username), normalizedUsername, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                return false;
            }

            user.LastLoginAtUtc = DateTimeOffset.UtcNow;
            SaveUnsafe();
            return true;
        }
    }

    public bool Delete(string? username)
    {
        var normalizedUsername = NormalizeLookup(username);
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            return false;
        }

        lock (_syncRoot)
        {
            var index = _users.FindIndex(user =>
                string.Equals(NormalizeLookup(user.Username), normalizedUsername, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return false;
            }

            var user = _users[index];
            var wouldHaveActiveAdmin = _users
                .Where((_, itemIndex) => itemIndex != index)
                .Any(item =>
                    item.IsActive &&
                    string.Equals(item.Role, WebAdminRoles.Admin, StringComparison.OrdinalIgnoreCase));

            if (!wouldHaveActiveAdmin)
            {
                return false;
            }

            _users.RemoveAt(index);
            SaveUnsafe();
            return true;
        }
    }

    private List<WebAdminAuthUserOptions> LoadUsers()
    {
        if (File.Exists(_dataFilePath))
        {
            try
            {
                var json = File.ReadAllText(_dataFilePath);
                var users = JsonSerializer.Deserialize<List<WebAdminAuthUserOptions>>(json, _jsonOptions);
                if (users is not null)
                {
                    var normalizedUsers = users
                        .Select(NormalizeUser)
                        .Where(user => !string.IsNullOrWhiteSpace(user.Username))
                        .GroupBy(user => NormalizeLookup(user.Username), StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.First())
                        .ToList();

                    _users = normalizedUsers;
                    SaveUnsafe();
                    return normalizedUsers;
                }
            }
            catch
            {
                // Fall through to configured/default users.
            }
        }

        var seeded = GetSeedUsers()
            .Select(NormalizeUser)
            .GroupBy(user => NormalizeLookup(user.Username), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        _users = seeded;
        SaveUnsafe();
        return seeded;
    }

    private IEnumerable<WebAdminAuthUserOptions> GetSeedUsers()
    {
        if (_options.Users.Count > 0)
        {
            return _options.Users.Select(Clone);
        }

        return
        [
            new WebAdminAuthUserOptions
            {
                Username = "user",
                PasswordHash = WebAdminPasswordHasher.HashPassword("12345678"),
                DisplayName = "Admin",
                Role = WebAdminRoles.Admin
            },
            new WebAdminAuthUserOptions
            {
                Username = "owner",
                PasswordHash = WebAdminPasswordHasher.HashPassword("12345678"),
                DisplayName = "Chủ nhà hàng",
                Role = WebAdminRoles.PoiOwner,
                OwnerCode = "owner",
                OwnerEmail = "owner@local.vinhkhanh"
            }
        ];
    }

    private void ValidateForSave(
        WebAdminAuthUserOptions user,
        WebAdminAuthUserOptions? existing,
        string? plainPassword)
    {
        if (string.IsNullOrWhiteSpace(user.Username))
        {
            throw new ArgumentException("Tên đăng nhập là bắt buộc.", nameof(user));
        }

        if (existing is null && string.IsNullOrWhiteSpace(plainPassword) && string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new ArgumentException("Mật khẩu là bắt buộc khi tạo tài khoản.", nameof(user));
        }

        if (!string.IsNullOrWhiteSpace(plainPassword) && plainPassword.Trim().Length < 6)
        {
            throw new ArgumentException("Mật khẩu phải có ít nhất 6 ký tự.", nameof(user));
        }

        if (!string.IsNullOrWhiteSpace(user.OwnerEmail) &&
            !user.OwnerEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Email chủ cửa hàng không hợp lệ.", nameof(user));
        }

        var duplicate = _users.Any(item =>
            !ReferenceEquals(item, existing) &&
            string.Equals(NormalizeLookup(item.Username), NormalizeLookup(user.Username), StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            throw new InvalidOperationException($"Tài khoản '{user.Username}' đã tồn tại.");
        }

        var wouldHaveActiveAdmin = _users
            .Where(item => !ReferenceEquals(item, existing))
            .Append(user)
            .Any(item =>
                item.IsActive &&
                string.Equals(item.Role, WebAdminRoles.Admin, StringComparison.OrdinalIgnoreCase));

        if (!wouldHaveActiveAdmin)
        {
            throw new InvalidOperationException("Cần giữ ít nhất một tài khoản Admin đang hoạt động.");
        }
    }

    private void SaveUnsafe()
    {
        var json = JsonSerializer.Serialize(_users.Select(Clone).ToList(), _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private static WebAdminAuthUserOptions NormalizeUser(WebAdminAuthUserOptions user)
    {
        var normalized = Clone(user);
        normalized.Username = normalized.Username?.Trim() ?? string.Empty;
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName)
            ? normalized.Username
            : normalized.DisplayName.Trim();
        normalized.Role = NormalizeRole(normalized.Role);
        normalized.OwnerCode = string.Equals(normalized.Role, WebAdminRoles.PoiOwner, StringComparison.OrdinalIgnoreCase)
            ? string.IsNullOrWhiteSpace(normalized.OwnerCode) ? normalized.Username : normalized.OwnerCode.Trim()
            : string.Empty;
        normalized.OwnerEmail = string.Equals(normalized.Role, WebAdminRoles.PoiOwner, StringComparison.OrdinalIgnoreCase)
            ? normalized.OwnerEmail?.Trim().ToLowerInvariant() ?? string.Empty
            : string.Empty;
        normalized.CreatedAtUtc = normalized.CreatedAtUtc == default
            ? DateTimeOffset.UtcNow
            : normalized.CreatedAtUtc.ToUniversalTime();
        normalized.LastLoginAtUtc = normalized.LastLoginAtUtc?.ToUniversalTime();

        if (string.IsNullOrWhiteSpace(normalized.PasswordHash) &&
            !string.IsNullOrWhiteSpace(normalized.Password))
        {
            normalized.PasswordHash = WebAdminPasswordHasher.HashPassword(normalized.Password.Trim());
        }

        normalized.Password = string.Empty;
        return normalized;
    }

    private static string NormalizeRole(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "admin" => WebAdminRoles.Admin,
            "poi_owner" => WebAdminRoles.PoiOwner,
            "poiowner" => WebAdminRoles.PoiOwner,
            "owner" => WebAdminRoles.PoiOwner,
            _ => WebAdminRoles.PoiOwner
        };
    }

    private static string NormalizeLookup(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static WebAdminAuthUserOptions Clone(WebAdminAuthUserOptions user)
    {
        return new WebAdminAuthUserOptions
        {
            Username = user.Username,
            Password = string.Empty,
            PasswordHash = user.PasswordHash,
            DisplayName = user.DisplayName,
            Role = user.Role,
            OwnerCode = user.OwnerCode,
            OwnerEmail = user.OwnerEmail,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }
}
