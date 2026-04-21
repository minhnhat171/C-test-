using CTest.WebAdmin.Models;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Services;

internal static class AudioGuideAdminMappings
{
    public static AudioGuideListItemViewModel ToListItem(this AudioGuideDto dto, WebDisplayClock clock, bool isSelected = false)
    {
        return new AudioGuideListItemViewModel
        {
            Id = dto.Id,
            PoiId = dto.PoiId,
            PoiCode = dto.PoiCode,
            PoiName = dto.PoiName,
            LanguageCode = NormalizeLanguageCode(dto.LanguageCode),
            LanguageLabel = GetLanguageLabel(dto.LanguageCode),
            VoiceType = NormalizeVoiceType(dto.VoiceType),
            VoiceLabel = GetVoiceLabel(dto.VoiceType),
            SourceType = NormalizeSourceType(dto.SourceType),
            SourceLabel = GetSourceLabel(dto.SourceType),
            Script = dto.Script ?? string.Empty,
            FilePath = dto.FilePath ?? string.Empty,
            EstimatedSeconds = dto.EstimatedSeconds,
            IsPublished = dto.IsPublished,
            UpdatedAtLocal = dto.UpdatedAtUtc == default
                ? clock.Now.DateTime
                : clock.ToDisplayTime(dto.UpdatedAtUtc).DateTime,
            IsSelected = isSelected
        };
    }

    public static AudioGuideEditorFormViewModel ToEditorViewModel(this AudioGuideDto dto)
    {
        return new AudioGuideEditorFormViewModel
        {
            Id = dto.Id,
            PoiId = dto.PoiId,
            LanguageCode = NormalizeLanguageCode(dto.LanguageCode),
            VoiceType = NormalizeVoiceType(dto.VoiceType),
            SourceType = NormalizeSourceType(dto.SourceType),
            Script = dto.Script ?? string.Empty,
            FilePath = dto.FilePath ?? string.Empty,
            EstimatedSeconds = dto.EstimatedSeconds,
            IsPublished = dto.IsPublished,
            IsEditMode = dto.Id != Guid.Empty
        };
    }

    public static AudioGuideDto ToDto(
        this AudioGuideEditorFormViewModel model,
        IReadOnlyDictionary<Guid, PoiLookupItemViewModel> poiLookup)
    {
        poiLookup.TryGetValue(model.PoiId, out var poi);

        return new AudioGuideDto
        {
            Id = model.Id,
            PoiId = model.PoiId,
            PoiCode = poi?.Code ?? string.Empty,
            PoiName = poi?.Name ?? string.Empty,
            LanguageCode = NormalizeLanguageCode(model.LanguageCode),
            VoiceType = NormalizeVoiceType(model.VoiceType),
            SourceType = NormalizeSourceType(model.SourceType),
            Script = model.Script?.Trim() ?? string.Empty,
            FilePath = model.FilePath?.Trim() ?? string.Empty,
            EstimatedSeconds = model.EstimatedSeconds,
            IsPublished = model.IsPublished
        };
    }

    public static string NormalizeLanguageCode(string? languageCode)
    {
        var normalized = (languageCode ?? string.Empty).Trim().ToLowerInvariant();

        if (normalized.StartsWith("vi", StringComparison.Ordinal))
        {
            return "vi";
        }

        if (normalized.StartsWith("en", StringComparison.Ordinal))
        {
            return "en";
        }

        if (normalized.StartsWith("zh", StringComparison.Ordinal))
        {
            return "zh";
        }

        if (normalized.StartsWith("ko", StringComparison.Ordinal))
        {
            return "ko";
        }

        if (normalized.StartsWith("fr", StringComparison.Ordinal))
        {
            return "fr";
        }

        return string.IsNullOrWhiteSpace(normalized) ? "vi" : normalized;
    }

    public static string NormalizeVoiceType(string? voiceType)
    {
        var normalized = (voiceType ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "male" => "male",
            "neutral" => "neutral",
            _ => "female"
        };
    }

    public static string NormalizeSourceType(string? sourceType)
    {
        return string.Equals(sourceType?.Trim(), "file", StringComparison.OrdinalIgnoreCase)
            ? "file"
            : "tts";
    }

    public static string GetLanguageLabel(string? languageCode)
    {
        return NormalizeLanguageCode(languageCode) switch
        {
            "vi" => "Tiếng Việt",
            "en" => "Tiếng Anh",
            "zh" => "Tiếng Trung",
            "ko" => "Tiếng Hàn",
            "fr" => "Tiếng Pháp",
            _ => "Khác"
        };
    }

    public static string GetVoiceLabel(string? voiceType)
    {
        return NormalizeVoiceType(voiceType) switch
        {
            "male" => "Nam",
            "neutral" => "Trung tính",
            _ => "Nữ"
        };
    }

    public static string GetSourceLabel(string? sourceType)
    {
        return NormalizeSourceType(sourceType) switch
        {
            "file" => "File audio",
            _ => "TTS"
        };
    }
}
