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
        string? view,
        CancellationToken cancellationToken = default)
    {
        var normalizedSortBy = NormalizeSortBy(sortBy);
        var normalizedPeriod = NormalizePeriod(period);
        var normalizedView = NormalizeView(view);

        try
        {
            var timelineTask = _apiClient.GetListeningHistoryAsync(normalizedSortBy, normalizedPeriod, cancellationToken);
            var rankingTask = _apiClient.GetPoiRankingAsync(normalizedPeriod, cancellationToken);

            await Task.WhenAll(timelineTask, rankingTask);

            var timelineItems = timelineTask.Result
                .Select(ToTimelineItem)
                .ToList();
            var rankingItems = rankingTask.Result
                .Select(ToRankingItem)
                .ToList();

            return new ListeningHistoryPageViewModel
            {
                SelectedSortBy = normalizedSortBy,
                SelectedPeriod = normalizedPeriod,
                SelectedView = normalizedView,
                TimelineItems = timelineItems,
                RankingItems = rankingItems,
                TotalSessions = timelineItems.Count,
                CompletedSessions = timelineItems.Count(item => item.Completed),
                TotalListenSeconds = timelineItems.Sum(item => item.ListenSeconds),
                MostPlayedPoi = rankingItems.FirstOrDefault()?.PoiName ?? "Chưa có dữ liệu"
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
                SelectedView = normalizedView,
                LoadErrorMessage = $"Không tải được lịch sử nghe từ API: {ex.Message}"
            };
        }
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

    private static PoiListeningRankingItemViewModel ToRankingItem(PoiListeningCountDto item)
    {
        return new PoiListeningRankingItemViewModel
        {
            PoiId = item.PoiId,
            PoiCode = item.PoiCode,
            PoiName = item.PoiName,
            ListenCount = item.ListenCount,
            CompletedCount = item.CompletedCount,
            TotalListenSeconds = item.TotalListenSeconds,
            LastStartedAtUtc = item.LastStartedAtUtc
        };
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

    private static string NormalizeView(string? view)
    {
        return view?.Trim().ToLowerInvariant() switch
        {
            "ranking" => "ranking",
            _ => "timeline"
        };
    }
}
