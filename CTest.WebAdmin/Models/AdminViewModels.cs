namespace CTest.WebAdmin.Models;

public class PoiManagementViewModel
{
    public string SearchTerm { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = "all";
    public string LoadErrorMessage { get; set; } = string.Empty;
    public List<PoiListItemViewModel> Items { get; set; } = new();
}

public class PoiListItemViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusInMeters { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public int RelatedAudioCount { get; set; }

    public string DisplayId =>
        Id == Guid.Empty
            ? "N/A"
            : AdminViewModelIds.BuildShortPoiId(Id);
}

public class PoiEditorViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = "Ẩm thực";
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusInMeters { get; set; } = 50;
    public int Priority { get; set; } = 1;
    public int CooldownMinutes { get; set; } = 5;
    public string Description { get; set; } = string.Empty;
    public string SpecialDish { get; set; } = string.Empty;
    public string ImageSource { get; set; } = string.Empty;
    public string MapLink { get; set; } = string.Empty;
    public string NarrationScript { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsEditMode { get; set; }
    public int RelatedAudioCount { get; set; }
    public int RelatedTranslationCount { get; set; }
    public bool IsQrEnabled { get; set; }

    public string DisplayId =>
        Id == Guid.Empty
            ? "Mới"
            : Id.ToString("N")[..8].ToUpperInvariant();
}

public class PoiLookupItemViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AudioManagementViewModel
{
    public List<AudioGuide> Items { get; set; } = new();
    public List<Poi> Pois { get; set; } = new();
    public AudioGuideEditorViewModel Editor { get; set; } = new();
}

public class AudioGuideEditorViewModel
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string Language { get; set; } = "vi-VN";
    public string VoiceType { get; set; } = "Female";
    public string SourceType { get; set; } = "TTS";
    public string Script { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int EstimatedSeconds { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class TranslationManagementViewModel
{
    public string LoadErrorMessage { get; set; } = string.Empty;
    public List<PoiLookupItemViewModel> Pois { get; set; } = new();
    public Guid SelectedPoiId { get; set; }
    public string SelectedPoiName { get; set; } = string.Empty;
    public List<TranslationLanguageEditorViewModel> Languages { get; set; } = new();
}

public class TranslationLanguageEditorViewModel
{
    public int TranslationId { get; set; }
    public Guid PoiId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NarrationScript { get; set; } = string.Empty;
    public bool IsTranslated { get; set; }
}

public class QrManagementViewModel
{
    public string LoadErrorMessage { get; set; } = string.Empty;
    public List<QrCodeItemViewModel> Items { get; set; } = new();
    public string SelectedTargetType { get; set; } = string.Empty;
    public string SelectedTargetId { get; set; } = string.Empty;
    public QrCodeItemViewModel? SelectedItem { get; set; }
}

public class QrCodeItemViewModel
{
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetCode { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string TargetKindLabel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ActivationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string QrSvgUrl { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    public string DetailActionLabel { get; set; } = string.Empty;
    public string NarrationText { get; set; } = string.Empty;
    public string MapLink { get; set; } = string.Empty;
    public string SpecialDish { get; set; } = string.Empty;
    public string RouteSummary { get; set; } = string.Empty;
    public string EstimatedDurationLabel { get; set; } = string.Empty;

    public string DisplayId =>
        string.IsNullOrWhiteSpace(TargetId)
            ? "N/A"
            : AdminViewModelIds.BuildShortQrTargetId(TargetType, TargetId);
}

public class QrScanViewModel
{
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetCode { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string TargetKindLabel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string NarrationText { get; set; } = string.Empty;
    public string MapLink { get; set; } = string.Empty;
    public string SpecialDish { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string AppLaunchUrl { get; set; } = string.Empty;
    public bool AutoOpenApp { get; set; }
    public string EstimatedDurationLabel { get; set; } = string.Empty;
    public IReadOnlyList<string> TourStops { get; set; } = Array.Empty<string>();
}

internal static class QrTargetTypes
{
    public const string Poi = "poi";
    public const string Tour = "tour";

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Tour => Tour,
            _ => Poi
        };
    }
}

internal static class AdminViewModelIds
{
    public static string BuildShortPoiId(Guid id)
    {
        var raw = id.ToString("N").ToUpperInvariant();
        var tail = raw.Length >= 6 ? raw[^6..] : raw;

        return $"POI-{tail}";
    }

    public static string BuildShortQrTargetId(string targetType, string targetId)
    {
        var typePrefix = QrTargetTypes.Normalize(targetType) switch
        {
            QrTargetTypes.Tour => "TOUR",
            _ => "POI"
        };

        var compactId = new string((targetId ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();

        var tail = compactId.Length >= 6 ? compactId[^6..] : compactId;

        return $"{typePrefix}-{tail}";
    }
}
