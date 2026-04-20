namespace VinhKhanhGuide.Core.Contracts;

public sealed class ListeningHistoryEntryDto
{
    public Guid Id { get; set; }
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public string PoiAddress { get; set; } = string.Empty;
    public string PoiDescription { get; set; } = string.Empty;
    public string PoiSpecialDish { get; set; } = string.Empty;
    public string PoiImageSource { get; set; } = string.Empty;
    public string PoiMapLink { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string PlaybackMode { get; set; } = "tts";
    public string NarrationSnapshot { get; set; } = string.Empty;
    public string AudioAssetPath { get; set; } = string.Empty;
    public string Source { get; set; } = "app";
    public string DevicePlatform { get; set; } = string.Empty;
    public bool AutoTriggered { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public int TtsQueuePosition { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public int ListenSeconds { get; set; }
    public bool Completed { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public ListeningHistoryEntryDto Clone()
    {
        return new ListeningHistoryEntryDto
        {
            Id = Id,
            PoiId = PoiId,
            PoiCode = PoiCode,
            PoiName = PoiName,
            PoiAddress = PoiAddress,
            PoiDescription = PoiDescription,
            PoiSpecialDish = PoiSpecialDish,
            PoiImageSource = PoiImageSource,
            PoiMapLink = PoiMapLink,
            UserCode = UserCode,
            UserDisplayName = UserDisplayName,
            UserEmail = UserEmail,
            TriggerType = TriggerType,
            Language = Language,
            PlaybackMode = PlaybackMode,
            NarrationSnapshot = NarrationSnapshot,
            AudioAssetPath = AudioAssetPath,
            Source = Source,
            DevicePlatform = DevicePlatform,
            AutoTriggered = AutoTriggered,
            StartedAtUtc = StartedAtUtc,
            ReceivedAtUtc = ReceivedAtUtc,
            TtsQueuePosition = TtsQueuePosition,
            CompletedAtUtc = CompletedAtUtc,
            ListenSeconds = ListenSeconds,
            Completed = Completed,
            ErrorMessage = ErrorMessage
        };
    }
}

public sealed class ListeningHistoryCreateRequest
{
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public string PoiAddress { get; set; } = string.Empty;
    public string PoiDescription { get; set; } = string.Empty;
    public string PoiSpecialDish { get; set; } = string.Empty;
    public string PoiImageSource { get; set; } = string.Empty;
    public string PoiMapLink { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string PlaybackMode { get; set; } = "tts";
    public string NarrationSnapshot { get; set; } = string.Empty;
    public string AudioAssetPath { get; set; } = string.Empty;
    public string Source { get; set; } = "app";
    public string DevicePlatform { get; set; } = string.Empty;
    public bool AutoTriggered { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ListeningHistoryUpdateRequest
{
    public int ListenSeconds { get; set; }
    public bool Completed { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class PoiListeningCountDto
{
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public int ListenCount { get; set; }
    public int CompletedCount { get; set; }
    public int TotalListenSeconds { get; set; }
    public DateTimeOffset? LastStartedAtUtc { get; set; }
}
