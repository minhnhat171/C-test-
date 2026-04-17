namespace VinhKhanhGuide.App.Services;

public static class UserPreferenceScope
{
    public const string PreferenceKeyPrefix = "vinhkhanh.user.preferences.v1";
    public const string GuestScopeKey = "guest";

    public static string BuildAudioSettingsPrefix(string? scopeKey)
    {
        var normalizedScope = string.IsNullOrWhiteSpace(scopeKey)
            ? GuestScopeKey
            : scopeKey.Trim().ToLowerInvariant();

        return $"{PreferenceKeyPrefix}.{normalizedScope}";
    }
}
