using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

public class ListeningHistoryService
{
    private readonly ListeningHistoryApiClient _apiClient;

    public ListeningHistoryService(ListeningHistoryApiClient apiClient)
    {
        _apiClient = apiClient;
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

    private static List<PoiListeningRankingItemViewModel> BuildRanking(IEnumerable<ListeningHistoryEntryDto> entries)
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
                LastStartedAtUtc = group.Max(item => (DateTimeOffset?)item.StartedAtUtc)
            })
            .OrderByDescending(item => item.ListenCount)
            .ThenByDescending(item => item.TotalListenSeconds)
            .ThenBy(item => item.PoiName)
            .ToList();
    }

    private static ListeningHistoryItemViewModel ToTimelineItem(ListeningHistoryEntryDto item)
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
            ListenSeconds = item.ListenSeconds,
            Completed = item.Completed,
            AutoTriggered = item.AutoTriggered,
            ErrorMessage = item.ErrorMessage
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