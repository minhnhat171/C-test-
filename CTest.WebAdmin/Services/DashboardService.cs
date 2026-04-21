using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class DashboardService
{
    private readonly PoiApiClient _poiApiClient;
    private readonly TourApiClient _tourApiClient;
    private readonly AudioGuideApiClient _audioGuideApiClient;
    private readonly ListeningHistoryApiClient _listeningHistoryApiClient;
    private readonly UserManagementApiClient _userManagementApiClient;
    private readonly ActiveDeviceApiClient _activeDeviceApiClient;
    private readonly WebDisplayClock _clock;

    public DashboardService(
        PoiApiClient poiApiClient,
        TourApiClient tourApiClient,
        AudioGuideApiClient audioGuideApiClient,
        ListeningHistoryApiClient listeningHistoryApiClient,
        UserManagementApiClient userManagementApiClient,
        ActiveDeviceApiClient activeDeviceApiClient,
        WebDisplayClock clock)
    {
        _poiApiClient = poiApiClient;
        _tourApiClient = tourApiClient;
        _audioGuideApiClient = audioGuideApiClient;
        _listeningHistoryApiClient = listeningHistoryApiClient;
        _userManagementApiClient = userManagementApiClient;
        _activeDeviceApiClient = activeDeviceApiClient;
        _clock = clock;
    }

    public async Task<DashboardViewModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var poisTask = _poiApiClient.GetPoisAsync(cancellationToken);
            var toursTask = _tourApiClient.GetToursAsync(cancellationToken);
            var audioGuidesTask = _audioGuideApiClient.GetAudioGuidesAsync(cancellationToken);
            var historyTask = _listeningHistoryApiClient.GetListeningHistoryAsync(
                sortBy: "time_desc",
                period: "all",
                cancellationToken: cancellationToken);
            var usersTask = _userManagementApiClient.GetUsersAsync(cancellationToken);
            var activeDevicesTask = GetActiveDeviceStatsAsync(cancellationToken);

            await Task.WhenAll(poisTask, toursTask, audioGuidesTask, historyTask, usersTask, activeDevicesTask);

            return BuildFromSharedData(
                poisTask.Result,
                audioGuidesTask.Result,
                historyTask.Result,
                toursTask.Result.Count,
                usersTask.Result,
                activeDevicesTask.Result);
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            return BuildUnavailableDashboard(
                "Khong the ket noi VKFoodAPI. Dashboard chi doc du lieu tu API nen tam thoi de trong so lieu.");
        }
    }

    public async Task<ActiveDeviceStatsDto> GetActiveDeviceStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _activeDeviceApiClient.GetStatsAsync(cancellationToken);
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            return new ActiveDeviceStatsDto
            {
                ActiveDeviceCount = 0,
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                ActiveThresholdUtc = DateTimeOffset.UtcNow,
                Devices = []
            };
        }
    }

    public async Task<DashboardUsageSnapshotViewModel> GetUsageSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _listeningHistoryApiClient.GetListeningHistoryAsync(
                sortBy: "time_desc",
                period: "all",
                cancellationToken: cancellationToken);

            var localizedHistory = history
                .Select(item => new
                {
                    Item = item,
                    LocalStartedAt = _clock.ToDisplayTime(item.StartedAtUtc)
                })
                .OrderByDescending(x => x.LocalStartedAt)
                .ToList();

            var today = _clock.Now.Date;
            var mostPlayedPoi = localizedHistory
                .GroupBy(x => ResolvePoiName(x.Item))
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key)
                .Select(group => group.Key)
                .FirstOrDefault() ?? "Chua co du lieu";

            return new DashboardUsageSnapshotViewModel
            {
                TodayListenCount = localizedHistory.Count(x => x.LocalStartedAt.Date == today),
                MostPlayedPoi = mostPlayedPoi,
                RecentLogs = localizedHistory
                    .Take(8)
                    .Select((entry, index) => new UsageLog
                    {
                        Id = index + 1,
                        UserCode = ResolveUserLabel(entry.Item),
                        TriggerType = NormalizeTriggerType(entry.Item.TriggerType),
                        PoiName = ResolvePoiName(entry.Item),
                        Language = string.IsNullOrWhiteSpace(entry.Item.Language) ? "vi" : entry.Item.Language,
                        StartedAt = entry.LocalStartedAt.DateTime,
                        ListenSeconds = entry.Item.ListenSeconds,
                        Completed = entry.Item.Completed
                    })
                    .ToList()
            };
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            return new DashboardUsageSnapshotViewModel
            {
                TodayListenCount = 0,
                MostPlayedPoi = "Chua co du lieu",
                RecentLogs = new List<UsageLog>()
            };
        }
    }

    private DashboardViewModel BuildFromSharedData(
        IReadOnlyList<PoiDto> pois,
        IReadOnlyList<AudioGuideDto> audioGuides,
        IReadOnlyList<ListeningHistoryEntryDto> history,
        int totalTours,
        IReadOnlyList<AdminUserSummaryDto> users,
        ActiveDeviceStatsDto activeDevices)
    {
        var dashboardGeneratedAt = _clock.Now;
        var today = dashboardGeneratedAt.Date;

        var localizedHistory = history
            .Select(item => new
            {
                Item = item,
                LocalStartedAt = _clock.ToDisplayTime(item.StartedAtUtc)
            })
            .OrderByDescending(x => x.LocalStartedAt)
            .ToList();

        var topPois = localizedHistory
            .GroupBy(x => new { x.Item.PoiId, Name = ResolvePoiName(x.Item) })
            .Select(group => new DashboardTopPoiItem
            {
                Name = group.Key.Name,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .Take(5)
            .ToList();

        var totalUsageLogs = localizedHistory.Count;
        var poiIdsWithPublishedAudio = audioGuides
            .Where(item => item.IsPublished)
            .Select(item => item.PoiId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet();

        return new DashboardViewModel
        {
            TotalPois = pois.Count,
            TotalAudioGuides = audioGuides.Count,
            MappedPoiCount = pois.Count(HasMapData),
            TotalQrCodes = pois.Count(poi => poi.IsActive && !string.IsNullOrWhiteSpace(poi.Code)),
            TodayListenCount = localizedHistory.Count(x => x.LocalStartedAt.Date == today),
            TotalTours = totalTours,
            TotalUsageLogs = totalUsageLogs,
            MostPlayedPoi = topPois.FirstOrDefault()?.Name ?? "Chua co du lieu",
            AverageListenSeconds = totalUsageLogs == 0 ? 0 : localizedHistory.Average(x => x.Item.ListenSeconds),
            CompletionRate = totalUsageLogs == 0 ? 0 : (int)Math.Round(localizedHistory.Count(x => x.Item.Completed) * 100.0 / totalUsageLogs),
            QrListenRate = totalUsageLogs == 0 ? 0 : (int)Math.Round(localizedHistory.Count(x => IsQrTrigger(x.Item.TriggerType)) * 100.0 / totalUsageLogs),
            PublishedAudioCount = pois.Count(poi => poiIdsWithPublishedAudio.Contains(poi.Id)),
            ActivePoiCount = pois.Count(poi => poi.IsActive),
            ActiveDeviceStats = activeDevices.Clone(),
            ActiveDeviceCount = activeDevices.ActiveDeviceCount,
            IsSyncOnline = true,
            LastSyncedAt = dashboardGeneratedAt.DateTime,
            DataSourceLabel = "VKFoodAPI",
            DataSourceDescription = "Dashboard dang doc cung nguon POI va lich su nghe voi app MAUI qua VKFoodAPI.",
            DailyListenPoints = Enumerable.Range(0, 7)
                .Select(offset => today.AddDays(offset - 6))
                .Select(date => new DashboardDailyListenPoint
                {
                    Date = date,
                    Label = date.ToString("dd/MM"),
                    Count = localizedHistory.Count(x => x.LocalStartedAt.Date == date)
                })
                .ToList(),
            TopPois = topPois,
            RecentUsers = users
                .OrderByDescending(item => item.LastActiveAtUtc ?? DateTimeOffset.MinValue)
                .ThenBy(item => item.DisplayName)
                .Take(6)
                .Select(item => new DashboardRecentUserItem
                {
                    DisplayName = item.DisplayName,
                    Email = item.Email,
                    PhoneNumber = item.PhoneNumber,
                    PreferredLanguage = item.PreferredLanguage,
                    Role = item.Role,
                    Status = item.Status,
                    LastActiveAtUtc = item.LastActiveAtUtc
                })
                .ToList(),
            RecentLogs = localizedHistory
                .Take(8)
                .Select((entry, index) => new UsageLog
                {
                    Id = index + 1,
                    UserCode = ResolveUserLabel(entry.Item),
                    TriggerType = NormalizeTriggerType(entry.Item.TriggerType),
                    PoiName = ResolvePoiName(entry.Item),
                    Language = string.IsNullOrWhiteSpace(entry.Item.Language) ? "vi" : entry.Item.Language,
                    StartedAt = entry.LocalStartedAt.DateTime,
                    ListenSeconds = entry.Item.ListenSeconds,
                    Completed = entry.Item.Completed
                })
                .ToList()
        };
    }

    private DashboardViewModel BuildUnavailableDashboard(string loadErrorMessage)
    {
        var today = _clock.Now.Date;

        return new DashboardViewModel
        {
            MostPlayedPoi = "Chua co du lieu",
            ActiveDeviceStats = new ActiveDeviceStatsDto
            {
                ActiveDeviceCount = 0,
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                ActiveThresholdUtc = DateTimeOffset.UtcNow,
                Devices = []
            },
            ActiveDeviceCount = 0,
            IsSyncOnline = false,
            LastSyncedAt = null,
            DataSourceLabel = "VKFoodAPI",
            DataSourceDescription = "Dashboard chi doc tu VKFoodAPI; khi API offline, WebAdmin giu trang thai trong de tranh lech du lieu voi app.",
            LoadErrorMessage = loadErrorMessage,
            DailyListenPoints = Enumerable.Range(0, 7)
                .Select(offset => today.AddDays(offset - 6))
                .Select(date => new DashboardDailyListenPoint
                {
                    Date = date,
                    Label = date.ToString("dd/MM"),
                    Count = 0
                })
                .ToList()
        };
    }

    private static string ResolvePoiName(ListeningHistoryEntryDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.PoiName))
        {
            return item.PoiName;
        }

        if (!string.IsNullOrWhiteSpace(item.PoiCode))
        {
            return item.PoiCode;
        }

        return "POI khong xac dinh";
    }

    private static string ResolveUserLabel(ListeningHistoryEntryDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.UserDisplayName))
        {
            return item.UserDisplayName;
        }

        if (!string.IsNullOrWhiteSpace(item.UserCode))
        {
            return item.UserCode;
        }

        return "Khach tham quan";
    }

    private static string NormalizeTriggerType(string? triggerType)
    {
        return string.IsNullOrWhiteSpace(triggerType)
            ? "APP"
            : triggerType.Trim().ToUpperInvariant();
    }

    private static bool IsQrTrigger(string? triggerType)
    {
        return string.Equals(triggerType, "QR", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasMapData(PoiDto poi)
    {
        return HasMapData(poi.Latitude, poi.Longitude, poi.MapLink);
    }

    private static bool HasMapData(double latitude, double longitude, string? mapLink)
    {
        return Math.Abs(latitude) > 0.000001 ||
               Math.Abs(longitude) > 0.000001 ||
               !string.IsNullOrWhiteSpace(mapLink);
    }
}
