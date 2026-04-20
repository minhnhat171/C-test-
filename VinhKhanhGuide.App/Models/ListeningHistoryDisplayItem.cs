namespace VinhKhanhGuide.App.Models;

public sealed class ListeningHistoryDisplayItem
{
    public Guid Id { get; init; }
    public Guid PoiId { get; init; }
    public string PoiCode { get; init; } = string.Empty;
    public string PoiName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string SpecialDish { get; init; } = string.Empty;
    public string ImageSource { get; init; } = string.Empty;
    public string MapLink { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string LanguageLabel { get; init; } = string.Empty;
    public string PlaybackMode { get; init; } = "tts";
    public string PlaybackModeLabel { get; init; } = "Giọng đọc";
    public string NarrationSnapshot { get; init; } = string.Empty;
    public string AudioAssetPath { get; init; } = string.Empty;
    public string NarrationPreview { get; init; } = string.Empty;
    public string Title => string.IsNullOrWhiteSpace(PoiCode) ? PoiName : $"{PoiName} ({PoiCode})";
    public string StartedAtLabel { get; init; } = string.Empty;
    public string StartedAtShortLabel { get; init; } = string.Empty;
    public string DetailLabel { get; init; } = string.Empty;
    public string DetailSummaryLabel { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusAccentColor { get; init; } = "#EA580C";
    public string ErrorMessage { get; init; } = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageSource);
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasAddress => !string.IsNullOrWhiteSpace(Address);
    public bool HasNarrationPreview => !string.IsNullOrWhiteSpace(NarrationPreview);
    public bool CanReplay => !string.IsNullOrWhiteSpace(NarrationSnapshot) || !string.IsNullOrWhiteSpace(AudioAssetPath);
    public string ReplayActionText => "Nghe lại";

    public override string ToString()
    {
        var lines = new List<string>
        {
            StartedAtLabel,
            Title,
            DetailSummaryLabel,
            DetailLabel,
            StatusLabel
        };

        if (HasError)
        {
            lines.Add(ErrorMessage);
        }

        return string.Join(Environment.NewLine, lines.Where(line => !string.IsNullOrWhiteSpace(line)));
    }
}

public sealed class ListeningHistoryRankingDisplayItem
{
    public int Rank { get; init; }
    public string PoiCode { get; init; } = string.Empty;
    public string PoiName { get; init; } = string.Empty;
    public string Title => string.IsNullOrWhiteSpace(PoiCode) ? PoiName : $"{PoiName} ({PoiCode})";
    public string SummaryLabel { get; init; } = string.Empty;
    public string LastStartedAtLabel { get; init; } = string.Empty;

    public override string ToString()
    {
        return $"Top #{Rank}: {Title}{Environment.NewLine}{SummaryLabel}{Environment.NewLine}{LastStartedAtLabel}";
    }
}
