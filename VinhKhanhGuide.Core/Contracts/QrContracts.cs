namespace VinhKhanhGuide.Core.Contracts;

public static class QrTargetKinds
{
    public const string Poi = "poi";
    public const string Tour = "tour";

    public static bool IsSupported(string? value)
    {
        return value?.Trim().ToLowerInvariant() is Poi or Tour;
    }

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Tour => Tour,
            _ => Poi
        };
    }
}

public sealed class QrCodeItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string TargetType { get; set; } = QrTargetKinds.Poi;
    public string TargetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAtUtc { get; set; }

    public QrCodeItemDto Clone()
    {
        return new QrCodeItemDto
        {
            Id = Id,
            Code = Code,
            TargetType = TargetType,
            TargetId = TargetId,
            DisplayName = DisplayName,
            Description = Description,
            IsActive = IsActive,
            IsDeleted = IsDeleted,
            CreatedAtUtc = CreatedAtUtc,
            UpdatedAtUtc = UpdatedAtUtc,
            DeletedAtUtc = DeletedAtUtc
        };
    }
}

public sealed class QrCodeItemSaveRequest
{
    public string Code { get; set; } = string.Empty;
    public string TargetType { get; set; } = QrTargetKinds.Poi;
    public string TargetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ResolveQrResponseDto
{
    public bool Resolved { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string DeepLink { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public PoiDto? Poi { get; set; }
    public TourDto? Tour { get; set; }

    public static ResolveQrResponseDto NotFound(string code)
    {
        return new ResolveQrResponseDto
        {
            Resolved = false,
            Code = code,
            Message = "QR code was not linked to an active POI or tour."
        };
    }
}
