using Microsoft.Maui.Controls;
using VinhKhanhGuide.App.Services;

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
    public string PriorityLabel { get; set; } = string.Empty;
    public string CodeLabel { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string NearestLabel { get; set; } = string.Empty;
    public string InRadiusBadge { get; set; } = string.Empty;
    public bool HasNarrationPreview => !string.IsNullOrWhiteSpace(NarrationPreview);
    public bool HasSpecialDish => !string.IsNullOrWhiteSpace(SpecialDish);
    public string SpecialDishLabel { get; set; } = string.Empty;
    public string NarrationActionText { get; set; } = string.Empty;
    public string NarrationStateText { get; set; } = string.Empty;
    public string NarrationGuideText { get; set; } = string.Empty;
    public bool HasTourBadge => IsActiveTourStop || IsCompletedTourStop;
    public string TourBadgeText { get; set; } = string.Empty;
    public string DistanceLabel { get; set; } = string.Empty;
    public ImageSource? ResolvedImageSource => AppImageSourceResolver.Resolve(ImageSource);
}
