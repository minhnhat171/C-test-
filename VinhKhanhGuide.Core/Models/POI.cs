namespace VinhKhanhGuide.Core.Models;

public class POI
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NarrationText { get; set; } = string.Empty;
    public string MapLink { get; set; } = string.Empty;
    public int Priority { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double TriggerRadiusMeters { get; set; } = 50;
    public int CooldownMinutes { get; set; } = 5;
}
