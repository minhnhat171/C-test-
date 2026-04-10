namespace CTest.WebAdmin.Models;

public class TourPlan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
    public string PoiSequence { get; set; } = string.Empty;
    public bool IsQrEnabled { get; set; }
}
