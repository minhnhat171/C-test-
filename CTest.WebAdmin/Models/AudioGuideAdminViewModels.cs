namespace CTest.WebAdmin.Models;

public class AudioGuideManagementPageViewModel
{
    public string LoadErrorMessage { get; set; } = string.Empty;
    public Guid? ScopePoiId { get; set; }
    public string ScopePoiName { get; set; } = string.Empty;
    public string ScopePoiCode { get; set; } = string.Empty;
    public List<AudioGuideListItemViewModel> Items { get; set; } = new();
    public List<AudioGuidePoiCoverageItemViewModel> PoiAudioItems { get; set; } = new();
    public List<AudioGuideLanguageSlotViewModel> LanguageSlots { get; set; } = new();
    public List<PoiLookupItemViewModel> Pois { get; set; } = new();
    public AudioGuideEditorFormViewModel Editor { get; set; } = new();

    public bool HasPoiScope => ScopePoiId.HasValue;
    public int TotalPoiCount => PoiAudioItems.Count;
    public int PoiWithSelectedLanguageAudioCount => PoiAudioItems.Count(item => item.HasSelectedLanguageAudio);
    public int PublishedPoiAudioCount => PoiAudioItems.Count(item => item.HasPublishedAudio);
    public int MissingSelectedLanguageAudioCount => Math.Max(0, TotalPoiCount - PoiWithSelectedLanguageAudioCount);
}

public class AudioGuideListItemViewModel
{
    public Guid Id { get; set; }
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageLabel { get; set; } = string.Empty;
    public string VoiceType { get; set; } = string.Empty;
    public string VoiceLabel { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceLabel { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; }
    public bool IsPublished { get; set; }
    public DateTime UpdatedAtLocal { get; set; }
    public bool IsSelected { get; set; }

    public string DisplayId =>
        Id == Guid.Empty
            ? "N/A"
            : $"AUD-{Id.ToString("N")[^6..].ToUpperInvariant()}";
}

public class AudioGuidePoiCoverageItemViewModel
{
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public string PoiAddress { get; set; } = string.Empty;
    public bool IsActivePoi { get; set; }
    public Guid? AudioId { get; set; }
    public string LanguageCode { get; set; } = "vi";
    public string LanguageLabel { get; set; } = string.Empty;
    public string VoiceLabel { get; set; } = string.Empty;
    public string SourceType { get; set; } = "tts";
    public string SourceLabel { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public int AudioCount { get; set; }
    public string AvailableLanguageLabels { get; set; } = string.Empty;
    public bool HasSelectedLanguageAudio { get; set; }
    public bool HasPublishedAudio { get; set; }
    public bool IsSelected { get; set; }
    public DateTime? UpdatedAtLocal { get; set; }

    public string DisplayId =>
        AudioId.HasValue
            ? $"AUD-{AudioId.Value.ToString("N")[^6..].ToUpperInvariant()}"
            : "Chua co";

    public string StatusLabel =>
        !HasSelectedLanguageAudio
            ? "Can tao TTS"
            : HasPublishedAudio
                ? "Dang publish"
                : "Ban nhap";
}

public class AudioGuideEditorFormViewModel
{
    public Guid Id { get; set; }
    public Guid PoiId { get; set; }
    public string LanguageCode { get; set; } = "vi";
    public string VoiceType { get; set; } = "female";
    public string SourceType { get; set; } = "tts";
    public string Script { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; } = 30;
    public bool IsPublished { get; set; } = true;
    public bool IsEditMode { get; set; }
}

public class AudioGuideLanguageSlotViewModel
{
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageLabel { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = string.Empty;
    public string SourceLabel { get; set; } = string.Empty;
    public string UpdatedLabel { get; set; } = string.Empty;
    public Guid? AudioId { get; set; }
    public bool HasAudio { get; set; }
    public bool IsPublished { get; set; }
    public bool IsSelected { get; set; }
}
