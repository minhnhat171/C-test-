namespace CTest.WebAdmin.Models;

public class AudioGuide
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string PoiName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string VoiceType { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty; // TTS or File
    public string ContentOrFile { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
