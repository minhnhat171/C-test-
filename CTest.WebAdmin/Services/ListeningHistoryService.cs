using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class ListeningHistoryService
{
    private readonly ListeningHistoryApiClient _apiClient;
    private readonly WebDisplayClock _clock;

    public ListeningHistoryService(
        ListeningHistoryApiClient apiClient,
        WebDisplayClock clock)
    {
        _apiClient = apiClient;
        _clock = clock;
    }

    public async Task<ListeningHistoryPageViewModel> LoadPageAsync(
        string? sortBy,
        string? period,
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        var normalizedSortBy = NormalizeSortBy(sortBy);
        var normalizedPeriod = NormalizePeriod(period);
        var normalizedKeyword = keyword?.Trim() ?? string.Empty;

        try
        {
            var historyEntries = await _apiClient.GetListeningHistoryAsync(normalizedSortBy, normalizedPeriod, cancellationToken);

            var filteredEntries = ApplyKeywordFilter(historyEntries, normalizedKeyword)
                .ToList();

            filteredEntries = normalizedSortBy == "time_asc"
                ? filteredEntries.OrderBy(item => item.StartedAtUtc).ToList()
                : filteredEntries.OrderByDescending(item => item.StartedAtUtc).ToList();

            var rankingItems = BuildRanking(filteredEntries);
            var timelineItems = filteredEntries
                .Select(ToTimelineItem)
                .ToList();

            return new ListeningHistoryPageViewModel
            {
                SelectedSortBy = normalizedSortBy,
                SelectedPeriod = normalizedPeriod,
                SearchTerm = normalizedKeyword,
                TimelineItems = timelineItems,
                RankingItems = rankingItems,
                TotalSessions = filteredEntries.Count,
                CompletedSessions = filteredEntries.Count(item => item.Completed),
                TotalListenSeconds = filteredEntries.Sum(item => item.ListenSeconds),
                AverageListenSeconds = filteredEntries.Count == 0 ? 0 : filteredEntries.Average(item => item.ListenSeconds),
                DistinctPoiCount = rankingItems.Count,
                MostPlayedPoi = rankingItems.FirstOrDefault()?.PoiName ?? "Chưa có dữ liệu",
                MostPlayedPoiListenCount = rankingItems.FirstOrDefault()?.ListenCount ?? 0
            };
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            return new ListeningHistoryPageViewModel
            {
                SelectedSortBy = normalizedSortBy,
                SelectedPeriod = normalizedPeriod,
                SearchTerm = normalizedKeyword,
                LoadErrorMessage = $"Không tải được lịch sử sử dụng từ API: {ex.Message}"
            };
        }
    }

    public async Task<ListeningHistoryPageViewModel> LoadPageForPoisAsync(
        IEnumerable<Guid> allowedPoiIds,
        string? sortBy,
        string? period,
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        var allowed = allowedPoiIds
            .Where(id => id != Guid.Empty)
            .ToHashSet();

        var page = await LoadPageAsync(sortBy, period, keyword, cancellationToken);
        if (allowed.Count == 0)
        {
            page.TimelineItems = [];
            page.RankingItems = [];
            page.TotalSessions = 0;
            page.CompletedSessions = 0;
            page.TotalListenSeconds = 0;
            page.AverageListenSeconds = 0;
            page.DistinctPoiCount = 0;
            page.MostPlayedPoi = "Chua co du lieu";
            page.MostPlayedPoiListenCount = 0;
            return page;
        }

        var filtered = page.TimelineItems
            .Where(item => allowed.Contains(item.PoiId))
            .ToList();

        page.TimelineItems = filtered;
        page.RankingItems = BuildRanking(filtered.Select(ToDtoFromTimeline));
        page.TotalSessions = filtered.Count;
        page.CompletedSessions = filtered.Count(item => item.Completed);
        page.TotalListenSeconds = filtered.Sum(item => item.ListenSeconds);
        page.AverageListenSeconds = filtered.Count == 0 ? 0 : filtered.Average(item => item.ListenSeconds);
        page.DistinctPoiCount = page.RankingItems.Count;
        page.MostPlayedPoi = page.RankingItems.FirstOrDefault()?.PoiName ?? "Chua co du lieu";
        page.MostPlayedPoiListenCount = page.RankingItems.FirstOrDefault()?.ListenCount ?? 0;
        return page;
    }

    private static IEnumerable<ListeningHistoryEntryDto> ApplyKeywordFilter(
        IEnumerable<ListeningHistoryEntryDto> items,
        string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return items;
        }

        return items.Where(item =>
            ContainsKeyword(item.PoiName, keyword) ||
            ContainsKeyword(item.PoiCode, keyword) ||
            ContainsKeyword(item.UserCode, keyword) ||
            ContainsKeyword(item.UserDisplayName, keyword) ||
            ContainsKeyword(item.TriggerType, keyword) ||
            ContainsKeyword(item.Language, keyword) ||
            ContainsKeyword(item.Source, keyword) ||
            ContainsKeyword(item.DevicePlatform, keyword));
    }

    private List<PoiListeningRankingItemViewModel> BuildRanking(IEnumerable<ListeningHistoryEntryDto> entries)
    {
        return entries
            .GroupBy(item => new { item.PoiId, item.PoiCode, item.PoiName })
            .Select(group => new PoiListeningRankingItemViewModel
            {
                PoiId = group.Key.PoiId,
                PoiCode = group.Key.PoiCode,
                PoiName = group.Key.PoiName,
                ListenCount = group.Count(),
                CompletedCount = group.Count(item => item.Completed),
                TotalListenSeconds = group.Sum(item => item.ListenSeconds),
                LastStartedAtUtc = group.Max(item => (DateTimeOffset?)item.StartedAtUtc),
                LastStartedAtLocal = group
                    .Select(item => (DateTimeOffset?)_clock.ToDisplayTime(item.StartedAtUtc))
                    .Max()
            })
            .OrderByDescending(item => item.ListenCount)
            .ThenByDescending(item => item.TotalListenSeconds)
            .ThenBy(item => item.PoiName)
            .ToList();
    }

    private ListeningHistoryItemViewModel ToTimelineItem(ListeningHistoryEntryDto item)
    {
        return new ListeningHistoryItemViewModel
        {
            Id = item.Id,
            PoiId = item.PoiId,
            PoiCode = item.PoiCode,
            PoiName = item.PoiName,
            UserCode = item.UserCode,
            UserDisplayName = item.UserDisplayName,
            TriggerType = item.TriggerType,
            Language = item.Language,
            Source = item.Source,
            DevicePlatform = item.DevicePlatform,
            StartedAtUtc = item.StartedAtUtc,
            CompletedAtUtc = item.CompletedAtUtc,
            StartedAtLocal = _clock.ToDisplayTime(item.StartedAtUtc),
            CompletedAtLocal = item.CompletedAtUtc.HasValue
                ? _clock.ToDisplayTime(item.CompletedAtUtc.Value)
                : null,
            ListenSeconds = item.ListenSeconds,
            Completed = item.Completed,
            AutoTriggered = item.AutoTriggered,
            ErrorMessage = item.ErrorMessage,
            TtsQueuePosition = item.TtsQueuePosition
        };
    }

    private static ListeningHistoryEntryDto ToDtoFromTimeline(ListeningHistoryItemViewModel item)
    {
        return new ListeningHistoryEntryDto
        {
            Id = item.Id,
            PoiId = item.PoiId,
            PoiCode = item.PoiCode,
            PoiName = item.PoiName,
            UserCode = item.UserCode,
            UserDisplayName = item.UserDisplayName,
            TriggerType = item.TriggerType,
            Language = item.Language,
            Source = item.Source,
            DevicePlatform = item.DevicePlatform,
            StartedAtUtc = item.StartedAtUtc,
            CompletedAtUtc = item.CompletedAtUtc,
            ListenSeconds = item.ListenSeconds,
            Completed = item.Completed,
            AutoTriggered = item.AutoTriggered,
            ErrorMessage = item.ErrorMessage,
            TtsQueuePosition = item.TtsQueuePosition
        };
    }

    private static bool ContainsKeyword(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "time_asc" => "time_asc",
            _ => "time_desc"
        };
    }

    private static string NormalizePeriod(string? period)
    {
        return period?.Trim().ToLowerInvariant() switch
        {
            "day" => "day",
            "week" => "week",
            "month" => "month",
            _ => "all"
        };
    }
}
