namespace CTest.WebAdmin.Models;

public class ListeningHistoryPageViewModel
{
    public string SelectedSortBy { get; set; } = "time_desc";
    public string SelectedPeriod { get; set; } = "all";
    public string SearchTerm { get; set; } = string.Empty;
    public string LoadErrorMessage { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int TotalListenSeconds { get; set; }
    public double AverageListenSeconds { get; set; }
    public int DistinctPoiCount { get; set; }
    public string MostPlayedPoi { get; set; } = "Chưa có dữ liệu";
    public int MostPlayedPoiListenCount { get; set; }
    public List<ListeningHistoryItemViewModel> TimelineItems { get; set; } = new();
    public List<PoiListeningRankingItemViewModel> RankingItems { get; set; } = new();

    public int CompletionRate => TotalSessions == 0
        ? 0
        : (int)Math.Round(CompletedSessions * 100.0 / TotalSessions);

    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchTerm);

    public string AverageListenLabel => TotalSessions == 0
        ? "0 giây"
        : $"{AverageListenSeconds:0.#} giây";

    public string TotalListenLabel => $"{TotalListenSeconds} giây";
}

public class ListeningHistoryItemViewModel
{
    public Guid Id { get; set; }
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string DevicePlatform { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? StartedAtLocal { get; set; }
    public DateTimeOffset? CompletedAtLocal { get; set; }
    public int ListenSeconds { get; set; }
    public bool Completed { get; set; }
    public bool AutoTriggered { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int TtsQueuePosition { get; set; }

    public string StartedAtDisplay => (StartedAtLocal ?? StartedAtUtc.ToLocalTime()).ToString("dd/MM/yyyy HH:mm:ss");

    public string CompletedAtDisplay => CompletedAtUtc.HasValue
        ? (CompletedAtLocal ?? CompletedAtUtc.Value.ToLocalTime()).ToString("dd/MM/yyyy HH:mm:ss")
        : "--";

    public string DurationLabel => $"{ListenSeconds} giây";

    public string StatusLabel
    {
        get
        {
            if (Completed)
            {
                return "Hoàn tất";
            }

            return string.IsNullOrWhiteSpace(ErrorMessage)
                ? "Đang nghe / dừng sớm"
                : "Dừng vì lỗi";
        }
    }
}

public class PoiListeningRankingItemViewModel
{
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public int ListenCount { get; set; }
    public int CompletedCount { get; set; }
    public int TotalListenSeconds { get; set; }
    public DateTimeOffset? LastStartedAtUtc { get; set; }
    public DateTimeOffset? LastStartedAtLocal { get; set; }

    public double AverageListenSeconds => ListenCount == 0 ? 0 : TotalListenSeconds / (double)ListenCount;

    public string CompletionRateLabel => ListenCount == 0
        ? "0%"
        : $"{Math.Round(CompletedCount * 100.0 / ListenCount):0}%";

    public string TotalListenLabel => $"{TotalListenSeconds} giây";

    public string AverageListenLabel => ListenCount == 0
        ? "0 giây"
        : $"{AverageListenSeconds:0.#} giây";

    public string LastStartedAtDisplay => LastStartedAtUtc.HasValue
        ? (LastStartedAtLocal ?? LastStartedAtUtc.Value.ToLocalTime()).ToString("dd/MM/yyyy HH:mm")
        : "--";
}
