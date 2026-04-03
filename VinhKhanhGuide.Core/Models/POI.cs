namespace VinhKhanhGuide.Core.Models;

public class POI
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImageSource { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SpecialDish { get; set; } = string.Empty;
    public string NarrationText { get; set; } = string.Empty;
    public string MapLink { get; set; } = string.Empty;
    public string AudioAssetPath { get; set; } = string.Empty;

    public int Priority { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double TriggerRadiusMeters { get; set; } = 50;
    public int CooldownMinutes { get; set; } = 5;
    public Dictionary<string, string> NarrationTranslations { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public string GetNarrationText(string? languageCode = null)
    {
        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            if (NarrationTranslations.TryGetValue(languageCode, out var directMatch) &&
                !string.IsNullOrWhiteSpace(directMatch))
            {
                return directMatch;
            }

            var normalizedLanguage = languageCode.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];

            if (NarrationTranslations.TryGetValue(normalizedLanguage, out var normalizedMatch) &&
                !string.IsNullOrWhiteSpace(normalizedMatch))
            {
                return normalizedMatch;
            }
        }

        return string.IsNullOrWhiteSpace(NarrationText)
            ? $"Bạn đang ở gần {Name}."
            : NarrationText;
    }
}
