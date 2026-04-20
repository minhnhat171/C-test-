using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class DashboardService
{
    private readonly PoiApiClient _poiApiClient;
    private readonly TourApiClient _tourApiClient;
    private readonly ListeningHistoryApiClient _listeningHistoryApiClient;
    private readonly UserManagementApiClient _userManagementApiClient;
    private readonly AppDataService _fallbackData;

    public DashboardService(
        PoiApiClient poiApiClient,
        TourApiClient tourApiClient,
        ListeningHistoryApiClient listeningHistoryApiClient,
        UserManagementApiClient userManagementApiClient,
        AppDataService fallbackData)
    {
        _poiApiClient = poiApiClient;
        _tourApiClient = tourApiClient;
        _listeningHistoryApiClient = listeningHistoryApiClient;
        _userManagementApiClient = userManagementApiClient;
        _fallbackData = fallbackData;
    }

    public async Task<DashboardViewModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var poisTask = _poiApiClient.GetPoisAsync(cancellationToken);
            var toursTask = _tourApiClient.GetToursAsync(cancellationToken);
            var historyTask = _listeningHistoryApiClient.GetListeningHistoryAsync(
                sortBy: "time_desc",
                period: "all",
                cancellationToken: cancellationToken);
            var usersTask = _userManagementApiClient.GetUsersAsync(cancellationToken);

            await Task.WhenAll(poisTask, toursTask, historyTask, usersTask);

            return BuildFromSharedData(
                poisTask.Result,
                historyTask.Result,
                toursTask.Result.Count,
                usersTask.Result);
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            var fallback = BuildFromSampleData();
            fallback.LoadErrorMessage = "Khong the ket noi VKFoodAPI. Dashboard dang tam hien thi du lieu mau cua WebAdmin.";
            fallback.DataSourceLabel = "Du lieu mau";
            fallback.DataSourceDescription = "Chua ket noi duoc nguon du lieu dung chung nen dashboard chua phan anh dung thong tin dang co trong app.";
            return fallback;
        }
    }

    private static DashboardViewModel BuildFromSharedData(
        IReadOnlyList<PoiDto> pois,
        IReadOnlyList<ListeningHistoryEntryDto> history,
        int totalTours,
        IReadOnlyList<AdminUserSummaryDto> users)
    {
        var dashboardGeneratedAt = DateTime.Now;
        var today = dashboardGeneratedAt.Date;

        var localizedHistory = history
            .Select(item => new
            {
                Item = item,
                LocalStartedAt = item.StartedAtUtc.ToLocalTime()
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

        return new DashboardViewModel
        {
            TotalPois = pois.Count,
            TotalAudioGuides = pois.Sum(CountAudioVariants),
            MappedPoiCount = pois.Count(HasMapData),
            TotalQrCodes = pois.Count(poi => poi.IsActive && !string.IsNullOrWhiteSpace(poi.Code)),
            TodayListenCount = localizedHistory.Count(x => x.LocalStartedAt.Date == today),
            TotalTours = totalTours,
            TotalUsageLogs = totalUsageLogs,
            MostPlayedPoi = topPois.FirstOrDefault()?.Name ?? "Chua co du lieu",
            AverageListenSeconds = totalUsageLogs == 0 ? 0 : localizedHistory.Average(x => x.Item.ListenSeconds),
            CompletionRate = totalUsageLogs == 0 ? 0 : (int)Math.Round(localizedHistory.Count(x => x.Item.Completed) * 100.0 / totalUsageLogs),
            QrListenRate = totalUsageLogs == 0 ? 0 : (int)Math.Round(localizedHistory.Count(x => IsQrTrigger(x.Item.TriggerType)) * 100.0 / totalUsageLogs),
            PublishedAudioCount = pois.Count(poi => poi.IsActive && CountAudioVariants(poi) > 0),
            ActivePoiCount = pois.Count(poi => poi.IsActive),
            IsSyncOnline = true,
            LastSyncedAt = dashboardGeneratedAt,
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

    private DashboardViewModel BuildFromSampleData()
    {
        var today = DateTime.Today;
        var lastSyncedAt = _fallbackData.UsageLogs
            .OrderByDescending(x => x.StartedAt)
            .Select(x => (DateTime?)x.StartedAt)
            .FirstOrDefault();

        var dailyListenPoints = Enumerable.Range(0, 7)
            .Select(offset => today.AddDays(offset - 6))
            .Select(date => new DashboardDailyListenPoint
            {
                Date = date,
                Label = date.ToString("dd/MM"),
                Count = _fallbackData.UsageLogs.Count(x => x.StartedAt.Date == date)
            })
            .ToList();

        var topPois = _fallbackData.UsageLogs
            .GroupBy(x => x.PoiName)
            .Select(group => new DashboardTopPoiItem
            {
                Name = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .Take(5)
            .ToList();

        var totalUsageLogs = _fallbackData.UsageLogs.Count;
        var totalQrCodes = _fallbackData.Pois.Count(x =>
            x.Description.Contains("QR", StringComparison.OrdinalIgnoreCase) ||
            x.NarrationScript.Contains("QR", StringComparison.OrdinalIgnoreCase));

        var mostPlayed = _fallbackData.UsageLogs
            .GroupBy(x => x.PoiName)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Chua co du lieu";

        return new DashboardViewModel
        {
            TotalPois = _fallbackData.Pois.Count,
            TotalAudioGuides = _fallbackData.AudioGuides.Count,
            MappedPoiCount = _fallbackData.Pois.Count(HasMapData),
            TotalQrCodes = totalQrCodes,
            TodayListenCount = _fallbackData.UsageLogs.Count(x => x.StartedAt.Date == today),
            TotalTours = _fallbackData.Tours.Count,
            TotalUsageLogs = totalUsageLogs,
            MostPlayedPoi = mostPlayed,
            AverageListenSeconds = totalUsageLogs == 0 ? 0 : _fallbackData.UsageLogs.Average(x => x.ListenSeconds),
            CompletionRate = totalUsageLogs == 0 ? 0 : (int)Math.Round(_fallbackData.UsageLogs.Count(x => x.Completed) * 100.0 / totalUsageLogs),
            QrListenRate = totalUsageLogs == 0 ? 0 : (int)Math.Round(_fallbackData.UsageLogs.Count(x => x.TriggerType == "QR") * 100.0 / totalUsageLogs),
            PublishedAudioCount = _fallbackData.AudioGuides.Count(x => x.IsPublished),
            ActivePoiCount = _fallbackData.Pois.Count(x => x.IsActive),
            IsSyncOnline = false,
            LastSyncedAt = lastSyncedAt,
            DataSourceLabel = "Du lieu mau",
            DataSourceDescription = "Dashboard dang dung du lieu noi bo cua WebAdmin nen co the chua giong voi du lieu dang hien co trong app.",
            DailyListenPoints = dailyListenPoints,
            TopPois = topPois,
            RecentUsers = new List<DashboardRecentUserItem>(),
            RecentLogs = _fallbackData.UsageLogs.OrderByDescending(x => x.StartedAt).Take(8).ToList()
        };
    }

    private static int CountAudioVariants(PoiDto poi)
    {
        var translationCount = CountNarrations(poi, includeVietnamese: true);
        var fallbackNarrationCount = translationCount > 0
            ? 0
            : string.IsNullOrWhiteSpace(poi.NarrationText) ? 0 : 1;
        var audioFileCount = string.IsNullOrWhiteSpace(poi.AudioAssetPath) ? 0 : 1;

        return translationCount + fallbackNarrationCount + audioFileCount;
    }

    private static int CountNarrations(PoiDto poi, bool includeVietnamese)
    {
        if (poi.NarrationTranslations is null || poi.NarrationTranslations.Count == 0)
        {
            return 0;
        }

        return poi.NarrationTranslations.Count(entry =>
            !string.IsNullOrWhiteSpace(entry.Value) &&
            (includeVietnamese || !string.Equals(entry.Key, "vi", StringComparison.OrdinalIgnoreCase)));
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

    private static bool HasMapData(Poi poi)
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
