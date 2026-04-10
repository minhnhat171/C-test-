namespace VinhKhanhGuide.App.Services;

public enum UsageHistoryCategory
{
    Activity,
    Listening,
    Viewing
}

public interface IUsageHistoryService
{
    IReadOnlyList<string> LoadEntries(UsageHistoryCategory category);
    void AppendEntry(UsageHistoryCategory category, string message);
    void ClearEntries(UsageHistoryCategory category);
}
