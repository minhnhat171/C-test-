namespace VinhKhanhGuide.App.Models;

public sealed class ListeningHistoryDisplayItem
{
    public Guid Id { get; init; }
    public string PoiCode { get; init; } = string.Empty;
    public string PoiName { get; init; } = string.Empty;
    public string Title => string.IsNullOrWhiteSpace(PoiCode) ? PoiName : $"{PoiName} ({PoiCode})";
    public string StartedAtLabel { get; init; } = string.Empty;
    public string DetailLabel { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusAccentColor { get; init; } = "#EA580C";
    public string ErrorMessage { get; init; } = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public override string ToString()
    {
        var lines = new List<string>
        {
            StartedAtLabel,
            Title,
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
