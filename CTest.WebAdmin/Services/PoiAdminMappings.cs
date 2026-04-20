using System.Globalization;
using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

internal static class PoiAdminMappings
{
    public static PoiListItemViewModel ToListItem(this PoiDto dto)
    {
        return new PoiListItemViewModel
        {
            Id = dto.Id,
            Code = dto.Code,
            Name = dto.Name,
            Address = dto.Address,
            Description = dto.Description,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            RadiusInMeters = dto.TriggerRadiusMeters,
            Priority = dto.Priority,
            IsActive = dto.IsActive
        };
    }

    public static PoiLookupItemViewModel ToLookupItem(this PoiDto dto)
    {
        return new PoiLookupItemViewModel
        {
            Id = dto.Id,
            Code = dto.Code,
            Name = dto.Name,
            Address = dto.Address,
            Description = dto.Description,
            NarrationText = dto.NarrationText,
            NarrationTranslations = new Dictionary<string, string>(
                dto.NarrationTranslations ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase),
            IsActive = dto.IsActive
        };
    }

    public static PoiEditorViewModel ToEditorViewModel(
        this PoiDto dto,
        int relatedAudioCount = 0)
    {
        var narrationScript = dto.NarrationTranslations is not null &&
                              dto.NarrationTranslations.TryGetValue("vi", out var vietnameseNarration) &&
                              !string.IsNullOrWhiteSpace(vietnameseNarration)
            ? vietnameseNarration
            : dto.NarrationText;

        return new PoiEditorViewModel
        {
            Id = dto.Id,
            Code = dto.Code,
            Category = dto.Category,
            Name = dto.Name,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            RadiusInMeters = dto.TriggerRadiusMeters,
            Priority = dto.Priority,
            CooldownMinutes = dto.CooldownMinutes,
            Description = dto.Description,
            SpecialDish = dto.SpecialDish,
            ImageSource = dto.ImageSource,
            MapLink = dto.MapLink,
            NarrationScript = narrationScript ?? string.Empty,
            IsActive = dto.IsActive,
            IsEditMode = true,
            RelatedAudioCount = relatedAudioCount,
            RelatedTranslationCount = dto.NarrationTranslations?.Count(x => !string.IsNullOrWhiteSpace(x.Value)) ?? 0,
            IsQrEnabled = ContainsQr(dto.Description, narrationScript)
        };
    }

    public static void ApplyEditorValues(this PoiDto dto, PoiEditorViewModel model)
    {
        dto.Code = string.IsNullOrWhiteSpace(model.Code)
            ? $"VK-POI-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
            : model.Code.Trim();
        dto.Category = string.IsNullOrWhiteSpace(model.Category) ? "Ẩm thực" : model.Category.Trim();
        dto.Name = model.Name.Trim();
        dto.Address = model.Address.Trim();
        dto.Latitude = model.Latitude;
        dto.Longitude = model.Longitude;
        dto.TriggerRadiusMeters = model.RadiusInMeters <= 0 ? 50 : model.RadiusInMeters;
        dto.Priority = model.Priority <= 0 ? 1 : model.Priority;
        dto.CooldownMinutes = model.CooldownMinutes <= 0 ? 5 : model.CooldownMinutes;
        dto.Description = model.Description.Trim();
        dto.SpecialDish = model.SpecialDish.Trim();
        dto.ImageSource = model.ImageSource.Trim();
        dto.MapLink = string.IsNullOrWhiteSpace(model.MapLink)
            ? BuildMapLink(model.Latitude, model.Longitude)
            : model.MapLink.Trim();
        dto.NarrationText = model.NarrationScript.Trim();
        dto.IsActive = model.IsActive;
        dto.NarrationTranslations ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        dto.NarrationTranslations["vi"] = dto.NarrationText;
    }

    public static bool ContainsQr(string? description, string? narrationScript)
    {
        return (description?.Contains("QR", StringComparison.OrdinalIgnoreCase) ?? false)
               || (narrationScript?.Contains("QR", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string BuildMapLink(double latitude, double longitude)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "https://maps.google.com/?q={0},{1}",
            latitude,
            longitude);
    }
}
