using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Models;

public class DashboardViewModel
{
    public int TotalPois { get; set; }
    public int TotalAudioGuides { get; set; }
    public int MappedPoiCount { get; set; }
    public int TotalTranslations
    {
        get => MappedPoiCount;
        set => MappedPoiCount = value;
    }
    public int TotalQrCodes { get; set; }
    public int TodayListenCount { get; set; }
    public int TotalTours { get; set; }
    public int TotalUsageLogs { get; set; }
    public string MostPlayedPoi { get; set; } = string.Empty;
    public double AverageListenSeconds { get; set; }
    public int CompletionRate { get; set; }
    public int QrListenRate { get; set; }
    public int PublishedAudioCount { get; set; }
    public int ActivePoiCount { get; set; }
    public int ActiveDeviceCount { get; set; }
    public ActiveDeviceStatsDto ActiveDeviceStats { get; set; } = new();
    public bool IsSyncOnline { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string DataSourceLabel { get; set; } = string.Empty;
    public string DataSourceDescription { get; set; } = string.Empty;
    public string LoadErrorMessage { get; set; } = string.Empty;
    public List<DashboardDailyListenPoint> DailyListenPoints { get; set; } = new();
    public List<DashboardTopPoiItem> TopPois { get; set; } = new();
    public List<UsageLog> RecentLogs { get; set; } = new();
    public List<DashboardRecentUserItem> RecentUsers { get; set; } = new();
}

public class DashboardUsageSnapshotViewModel
{
    public int TodayListenCount { get; set; }
    public string MostPlayedPoi { get; set; } = string.Empty;
    public List<UsageLog> RecentLogs { get; set; } = new();
}

public class DashboardDailyListenPoint
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardTopPoiItem
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardRecentUserItem
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? LastActiveAtUtc { get; set; }
}
