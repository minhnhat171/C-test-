namespace VinhKhanhGuide.App.Models;

public sealed class TourPackageItem
{
    public int TourId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
    public int StopCount { get; set; }
    public string StopsSummary { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public bool IsCompleted { get; set; }

    public string MetaLabel => $"{StopCount} điểm dừng • {EstimatedMinutes} phút";
    public string StatusLabel => IsSelected
        ? (IsCompleted ? "Đã hoàn tất" : "Đang chạy")
        : "Sẵn sàng";
    public string ActionLabel => IsSelected ? "Đang chọn" : "Bắt đầu";
}
