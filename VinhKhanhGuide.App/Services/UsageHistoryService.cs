using System.Text.Json;

namespace VinhKhanhGuide.App.Services;

public class UsageHistoryService : IUsageHistoryService
{
    private const int MaxPersistedLogs = 40;
    private const string PreferenceKeyPrefix = "vinhkhanh.usage.entries.v2";

    private readonly IAuthService _authService;

    public UsageHistoryService(IAuthService authService)
    {
        _authService = authService;
    }

    public IReadOnlyList<string> LoadEntries(UsageHistoryCategory category)
    {
        var json = Preferences.Default.Get(GetCurrentPreferenceKey(category), string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            Preferences.Default.Remove(GetCurrentPreferenceKey(category));
            return [];
        }
    }

    public void AppendEntry(UsageHistoryCategory category, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var logs = LoadEntries(category).ToList();
        logs.Insert(0, message);

        while (logs.Count > MaxPersistedLogs)
        {
            logs.RemoveAt(logs.Count - 1);
        }

        SaveLogs(category, logs);
    }

    public void ClearEntries(UsageHistoryCategory category)
    {
        Preferences.Default.Remove(GetCurrentPreferenceKey(category));
    }

    private void SaveLogs(UsageHistoryCategory category, List<string> logs)
    {
        var json = JsonSerializer.Serialize(logs);
        Preferences.Default.Set(GetCurrentPreferenceKey(category), json);
    }

    private string GetCurrentPreferenceKey(UsageHistoryCategory category)
    {
        var scope = _authService.CurrentSession?.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(scope))
        {
            scope = "guest";
        }

        return $"{PreferenceKeyPrefix}.{scope}.{category.ToString().ToLowerInvariant()}";
    }
}
