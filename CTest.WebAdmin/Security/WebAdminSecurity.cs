using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Security;

public static class WebAdminRoles
{
    public const string Admin = "Admin";
    public const string PoiOwner = "PoiOwner";
}

public static class WebAdminPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string OwnerArea = "OwnerArea";
}

public static class WebAdminClaimTypes
{
    public const string OwnerCode = "vkg.owner_code";
    public const string OwnerEmail = "vkg.owner_email";
    public const string DisplayName = "vkg.display_name";
}

public sealed class WebAdminAuthOptions
{
    public List<WebAdminAuthUserOptions> Users { get; set; } = new();
}

public sealed class WebAdminAuthUserOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string OwnerCode { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
}

public sealed class WebAdminAuthenticatedUser
{
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string OwnerCode { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
}

public interface IWebAdminAuthService
{
    WebAdminAuthenticatedUser? ValidateCredentials(string? username, string? password);
}

public sealed class WebAdminAuthService : IWebAdminAuthService
{
    private readonly IWebAdminAccountStore _accountStore;

    public WebAdminAuthService(IWebAdminAccountStore accountStore)
    {
        _accountStore = accountStore;
    }

    public WebAdminAuthenticatedUser? ValidateCredentials(string? username, string? password)
    {
        var normalizedUsername = NormalizeLookup(username);
        if (string.IsNullOrWhiteSpace(normalizedUsername) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var users = _accountStore.GetAll();
        var matchedUser = users.FirstOrDefault(user =>
            string.Equals(NormalizeLookup(user.Username), normalizedUsername, StringComparison.OrdinalIgnoreCase));

        if (matchedUser is null || !VerifyPassword(matchedUser, password))
        {
            return null;
        }

        var role = NormalizeRole(matchedUser.Role);
        var ownerCode = string.IsNullOrWhiteSpace(matchedUser.OwnerCode)
            ? matchedUser.Username.Trim()
            : matchedUser.OwnerCode.Trim();

        return new WebAdminAuthenticatedUser
        {
            Username = matchedUser.Username.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(matchedUser.DisplayName)
                ? matchedUser.Username.Trim()
                : matchedUser.DisplayName.Trim(),
            Role = role,
            OwnerCode = ownerCode,
            OwnerEmail = matchedUser.OwnerEmail?.Trim().ToLowerInvariant() ?? string.Empty
        };
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

    private static bool VerifyPassword(WebAdminAuthUserOptions user, string password)
    {
        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return WebAdminPasswordHasher.VerifyPassword(user.PasswordHash, password);
        }

        return !string.IsNullOrWhiteSpace(user.Password) &&
               string.Equals(user.Password, password, StringComparison.Ordinal);
    }
}

public static class WebAdminPasswordHasher
{
    private const int SaltByteCount = 16;
    private const int HashByteCount = 32;
    private const int IterationCount = 100000;
    private const string Prefix = "PBKDF2";

    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltByteCount);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            IterationCount,
            HashAlgorithmName.SHA256,
            HashByteCount);

        return $"{Prefix}${IterationCount}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string storedHash, string password)
    {
        if (string.IsNullOrWhiteSpace(storedHash) ||
            string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4 ||
            !string.Equals(parts[0], Prefix, StringComparison.Ordinal) ||
            !int.TryParse(parts[1], out var iterations) ||
            iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public interface IWebAdminCurrentUser
{
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsPoiOwner { get; }
    string Username { get; }
    string DisplayName { get; }
    string OwnerCode { get; }
    string OwnerEmail { get; }
    bool CanManage(PoiDto poi);
}

public sealed class WebAdminCurrentUser : IWebAdminCurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WebAdminCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;
    public bool IsAdmin => Principal.IsInRole(WebAdminRoles.Admin);
    public bool IsPoiOwner => Principal.IsInRole(WebAdminRoles.PoiOwner);
    public string Username => Principal.Identity?.Name ?? string.Empty;
    public string DisplayName => Principal.FindFirstValue(WebAdminClaimTypes.DisplayName) ?? Username;
    public string OwnerCode => Principal.FindFirstValue(WebAdminClaimTypes.OwnerCode) ?? Username;
    public string OwnerEmail => Principal.FindFirstValue(WebAdminClaimTypes.OwnerEmail) ?? string.Empty;

    private ClaimsPrincipal Principal =>
        _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    public bool CanManage(PoiDto poi)
    {
        if (IsAdmin)
        {
            return true;
        }

        if (!IsPoiOwner)
        {
            return false;
        }

        return MatchesOwner(poi);
    }

    private bool MatchesOwner(PoiDto poi)
    {
        var ownerCode = NormalizeLookup(OwnerCode);
        var ownerEmail = NormalizeLookup(OwnerEmail);
        var poiOwnerCode = NormalizeLookup(poi.OwnerUserCode);
        var poiOwnerEmail = NormalizeLookup(poi.OwnerEmail);

        return (!string.IsNullOrWhiteSpace(ownerCode) &&
                string.Equals(ownerCode, poiOwnerCode, StringComparison.OrdinalIgnoreCase)) ||
               (!string.IsNullOrWhiteSpace(ownerEmail) &&
                string.Equals(ownerEmail, poiOwnerEmail, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeLookup(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
