namespace VinhKhanhGuide.Core.Contracts;

public class PoiDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Ẩm thực";

    public string ImageSource { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SpecialDish { get; set; } = string.Empty;

    public string NarrationText { get; set; } = string.Empty;
    public string MapLink { get; set; } = string.Empty;
    public string AudioAssetPath { get; set; } = string.Empty;

    public int Priority { get; set; } = 1;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double TriggerRadiusMeters { get; set; } = 50;

    public int CooldownMinutes { get; set; } = 5;
    public bool IsActive { get; set; } = true;

    public Dictionary<string, string> NarrationTranslations { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public PoiDto Clone()
    {
        return new PoiDto
        {
            Id = Id,
            Code = Code,
            Name = Name,
            Category = Category,
            ImageSource = ImageSource,
            Address = Address,
            Description = Description,
            SpecialDish = SpecialDish,
            NarrationText = NarrationText,
            MapLink = MapLink,
            AudioAssetPath = AudioAssetPath,
            Priority = Priority,
            Latitude = Latitude,
            Longitude = Longitude,
            TriggerRadiusMeters = TriggerRadiusMeters,
            CooldownMinutes = CooldownMinutes,
            IsActive = IsActive,
            NarrationTranslations = new Dictionary<string, string>(
                NarrationTranslations ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase)
        };
    }
}
