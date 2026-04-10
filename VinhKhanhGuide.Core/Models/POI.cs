namespace VinhKhanhGuide.Core.Models;

public class POI
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Mã định danh ổn định toàn hệ thống
    public string Code { get; set; } = string.Empty;

    // Nội dung chính
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Ẩm thực";
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SpecialDish { get; set; } = string.Empty;

    // Media / map / narration
    public string ImageSource { get; set; } = string.Empty;
    public string MapLink { get; set; } = string.Empty;
    public string NarrationText { get; set; } = string.Empty;
    public string AudioAssetPath { get; set; } = string.Empty;

    // GPS / trigger
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double TriggerRadiusMeters { get; set; } = 50;
    public int Priority { get; set; } = 1;
    public int CooldownMinutes { get; set; } = 5;

    // Trạng thái
    public bool IsActive { get; set; } = true;

    // Đa ngôn ngữ
    public Dictionary<string, string> NarrationTranslations { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);
    public string GetNarrationText(string? languageCode = null)
    {
        if (!string.IsNullOrWhiteSpace(languageCode)
            && NarrationTranslations != null
            && NarrationTranslations.TryGetValue(languageCode, out var translated)
            && !string.IsNullOrWhiteSpace(translated))
        {
            return translated;
        }

        return NarrationText ?? string.Empty;
    }
}