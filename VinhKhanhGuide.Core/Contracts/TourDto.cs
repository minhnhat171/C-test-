namespace VinhKhanhGuide.Core.Contracts;

public class TourDto
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int EstimatedMinutes { get; set; } = 45;
    public bool IsActive { get; set; } = true;
    public bool IsQrEnabled { get; set; } = true;

    public List<Guid> PoiIds { get; set; } = [];
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    public TourDto Clone()
    {
        return new TourDto
        {
            Id = Id,
            Code = Code,
            Name = Name,
            Description = Description,
            EstimatedMinutes = EstimatedMinutes,
            IsActive = IsActive,
            IsQrEnabled = IsQrEnabled,
            PoiIds = PoiIds
                .Where(poiId => poiId != Guid.Empty)
                .ToList(),
            IsDeleted = IsDeleted,
            CreatedAtUtc = CreatedAtUtc,
            UpdatedAtUtc = UpdatedAtUtc,
            DeletedAtUtc = DeletedAtUtc
        };
    }
}
