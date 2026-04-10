namespace CTest.WebAdmin.Models;

public class TranslationEntry
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string PoiName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string NarrationScript { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
