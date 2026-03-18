namespace VinhKhanhGuide.App.Models;

public class PoiStatusItem
{
    public string Name { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    public double TriggerRadiusMeters { get; set; }
    public bool IsInsideRadius { get; set; }
    public int Priority { get; set; }

    public string PriorityLabel => $"P{Priority}";

    public string DistanceLabel =>
        double.IsNaN(DistanceMeters)
            ? $"Khoang cach: N/A | Ban kinh: {TriggerRadiusMeters:F0}m"
            : $"Khoang cach: {DistanceMeters:F0}m | Ban kinh: {TriggerRadiusMeters:F0}m";
}
