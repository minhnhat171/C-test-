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
    public string PriceRange { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = string.Empty;
    public string FirstDishSuggestion { get; set; } = string.Empty;
    public List<string> FeaturedCategories { get; set; } = [];

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
            PriceRange = PriceRange,
            OpeningHours = OpeningHours,
            FirstDishSuggestion = FirstDishSuggestion,
            FeaturedCategories = FeaturedCategories
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
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
