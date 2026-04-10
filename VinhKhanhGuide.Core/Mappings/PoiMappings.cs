using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.Core.Mappings;

public static class PoiMappings
{
    public static POI ToDomain(this PoiDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new POI
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            Code = dto.Code,
            Name = dto.Name,
            Category = dto.Category,
            ImageSource = dto.ImageSource,
            Address = dto.Address,
            Description = dto.Description,
            SpecialDish = dto.SpecialDish,
            NarrationText = dto.NarrationText,
            MapLink = dto.MapLink,
            AudioAssetPath = dto.AudioAssetPath,
            Priority = dto.Priority,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            TriggerRadiusMeters = dto.TriggerRadiusMeters,
            CooldownMinutes = dto.CooldownMinutes,
            IsActive = dto.IsActive,
            NarrationTranslations = new Dictionary<string, string>(
                dto.NarrationTranslations ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase)
        };
    }

    public static PoiDto ToDto(this POI poi)
    {
        ArgumentNullException.ThrowIfNull(poi);

        return new PoiDto
        {
            Id = poi.Id,
            Code = poi.Code,
            Name = poi.Name,
            Category = poi.Category,
            ImageSource = poi.ImageSource,
            Address = poi.Address,
            Description = poi.Description,
            SpecialDish = poi.SpecialDish,
            NarrationText = poi.NarrationText,
            MapLink = poi.MapLink,
            AudioAssetPath = poi.AudioAssetPath,
            Priority = poi.Priority,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            TriggerRadiusMeters = poi.TriggerRadiusMeters,
            CooldownMinutes = poi.CooldownMinutes,
            IsActive = poi.IsActive,
            NarrationTranslations = new Dictionary<string, string>(
                poi.NarrationTranslations ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase)
        };
    }
}
