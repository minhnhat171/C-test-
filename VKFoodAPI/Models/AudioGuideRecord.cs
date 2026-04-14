using VinhKhanhGuide.Core.Contracts;

namespace VKFoodAPI.Models;

public class AudioGuideRecord
{
    public Guid Id { get; set; }
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "vi";
    public string VoiceType { get; set; } = "female";
    public string SourceType { get; set; } = "tts";
    public string Script { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; } = 30;
    public bool IsPublished { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public AudioGuideRecord Clone()
    {
        return new AudioGuideRecord
        {
            Id = Id,
            PoiId = PoiId,
            PoiCode = PoiCode,
            PoiName = PoiName,
            LanguageCode = LanguageCode,
            VoiceType = VoiceType,
            SourceType = SourceType,
            Script = Script,
            FilePath = FilePath,
            EstimatedSeconds = EstimatedSeconds,
            IsPublished = IsPublished,
            UpdatedAtUtc = UpdatedAtUtc
        };
    }

    public AudioGuideDto ToDto()
    {
        return new AudioGuideDto
        {
            Id = Id,
            PoiId = PoiId,
            PoiCode = PoiCode,
            PoiName = PoiName,
            LanguageCode = LanguageCode,
            VoiceType = VoiceType,
            SourceType = SourceType,
            Script = Script,
            FilePath = FilePath,
            EstimatedSeconds = EstimatedSeconds,
            IsPublished = IsPublished,
            UpdatedAtUtc = UpdatedAtUtc
        };
    }

    public static AudioGuideRecord FromDto(AudioGuideDto dto)
    {
        return new AudioGuideRecord
        {
            Id = dto.Id,
            PoiId = dto.PoiId,
            PoiCode = dto.PoiCode,
            PoiName = dto.PoiName,
            LanguageCode = dto.LanguageCode,
            VoiceType = dto.VoiceType,
            SourceType = dto.SourceType,
            Script = dto.Script,
            FilePath = dto.FilePath,
            EstimatedSeconds = dto.EstimatedSeconds,
            IsPublished = dto.IsPublished,
            UpdatedAtUtc = dto.UpdatedAtUtc
        };
    }
}
