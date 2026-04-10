namespace CTest.WebAdmin.Models;

public class UsageLog
{
    public int Id { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty; // GPS or QR
    public string PoiName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public int ListenSeconds { get; set; }
    public bool Completed { get; set; }
}
