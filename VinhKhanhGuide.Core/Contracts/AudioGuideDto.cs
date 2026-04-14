namespace VinhKhanhGuide.Core.Contracts;

public class AudioGuideDto
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

    public AudioGuideDto Clone()
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
}
