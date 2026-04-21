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
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

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
            IsDeleted = IsDeleted,
            CreatedAtUtc = CreatedAtUtc,
            UpdatedAtUtc = UpdatedAtUtc,
            DeletedAtUtc = DeletedAtUtc
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
            IsDeleted = IsDeleted,
            CreatedAtUtc = CreatedAtUtc,
            UpdatedAtUtc = UpdatedAtUtc,
            DeletedAtUtc = DeletedAtUtc
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
            IsDeleted = dto.IsDeleted,
            CreatedAtUtc = dto.CreatedAtUtc,
            UpdatedAtUtc = dto.UpdatedAtUtc,
            DeletedAtUtc = dto.DeletedAtUtc
        };
    }
}
