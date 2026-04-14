namespace VinhKhanhGuide.App.Models;

public class PoiStatusItem
{
    public Guid PoiId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ImageSource { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SpecialDish { get; set; } = string.Empty;
    public string NarrationPreview { get; set; } = string.Empty;
    public string MapLink { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMeters { get; set; }
    public double TriggerRadiusMeters { get; set; }
    public bool IsInsideRadius { get; set; }
    public bool IsNearest { get; set; }
    public bool IsNarrationActive { get; set; }
    public bool IsActiveTourStop { get; set; }
    public bool IsCompletedTourStop { get; set; }
    public int? TourOrder { get; set; }
    public int Priority { get; set; }

    public string PriorityLabel => $"P{Priority}";
    public string CodeLabel => string.IsNullOrWhiteSpace(Code) ? "Chưa có mã" : $"Mã: {Code}";
    public string StatusLabel => IsInsideRadius ? "Trong vùng geofence" : "Ngoài vùng";
    public string NearestLabel => IsNearest ? "POI gần nhất" : string.Empty;
    public string InRadiusBadge => IsInsideRadius ? "Đang ở gần" : string.Empty;
    public bool HasNarrationPreview => !string.IsNullOrWhiteSpace(NarrationPreview);
    public string SpecialDishLabel => $"Món nổi bật: {SpecialDish}";
    public string NarrationActionText => IsNarrationActive ? "Dừng thuyết minh" : "Nghe thuyết minh";
    public string NarrationStateText => IsNarrationActive ? "Đang phát thuyết minh" : "Sẵn sàng nghe";
    public string NarrationGuideText => IsNarrationActive
        ? "Phần Talk to Speech của quán này đang phát."
        : "Bấm để nghe phần Talk to Speech ngay tại trang chủ.";
    public bool HasTourBadge => IsActiveTourStop || IsCompletedTourStop;
    public string TourBadgeText => !HasTourBadge
        ? string.Empty
        : IsCompletedTourStop
            ? $"Chặng {TourOrder ?? 0} đã xong"
            : $"Chặng {TourOrder ?? 0} đang dẫn";

    public string DistanceLabel =>
        double.IsNaN(DistanceMeters)
            ? $"Khoảng cách: N/A | Bán kính: {TriggerRadiusMeters:F0}m"
            : $"Khoảng cách: {DistanceMeters:F0}m | Bán kính: {TriggerRadiusMeters:F0}m";
}
