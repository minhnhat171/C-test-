namespace VinhKhanhGuide.App.Models;

public sealed class TourStopProgressItem
{
    public int Order { get; set; }
    public Guid PoiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsUpcoming { get; set; }
    public string OrderLabel { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
}
