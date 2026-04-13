namespace VinhKhanhGuide.Core.Contracts;

public sealed class AdminUserSummaryDto
{
    public Guid Id { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "offline";
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastActiveAtUtc { get; set; }
    public string LastPoiName { get; set; } = string.Empty;
    public string DevicePlatform { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int TotalListenSeconds { get; set; }

    public AdminUserSummaryDto Clone()
    {
        return new AdminUserSummaryDto
        {
            Id = Id,
            UserCode = UserCode,
            DisplayName = DisplayName,
            Email = Email,
            Role = Role,
            Status = Status,
            IsOnline = IsOnline,
            LastActiveAtUtc = LastActiveAtUtc,
            LastPoiName = LastPoiName,
            DevicePlatform = DevicePlatform,
            TotalSessions = TotalSessions,
            CompletedSessions = CompletedSessions,
            TotalListenSeconds = TotalListenSeconds
        };
    }
}

public sealed class AdminUserDetailDto
{
    public Guid Id { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "offline";
    public bool IsOnline { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastActiveAtUtc { get; set; }
    public DateTimeOffset? LastCompletedAtUtc { get; set; }
    public string PreferredLanguage { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DevicePlatform { get; set; } = string.Empty;
    public string LastTriggerType { get; set; } = string.Empty;
    public string LastSource { get; set; } = string.Empty;
    public string LastPoiName { get; set; } = string.Empty;
    public string LastPoiCode { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int TotalListenSeconds { get; set; }

    public AdminUserDetailDto Clone()
    {
        return new AdminUserDetailDto
        {
            Id = Id,
            UserCode = UserCode,
            DisplayName = DisplayName,
            Email = Email,
            Role = Role,
            Status = Status,
            IsOnline = IsOnline,
            CreatedAtUtc = CreatedAtUtc,
            LastActiveAtUtc = LastActiveAtUtc,
            LastCompletedAtUtc = LastCompletedAtUtc,
            PreferredLanguage = PreferredLanguage,
            PhoneNumber = PhoneNumber,
            DevicePlatform = DevicePlatform,
            LastTriggerType = LastTriggerType,
            LastSource = LastSource,
            LastPoiName = LastPoiName,
            LastPoiCode = LastPoiCode,
            TotalSessions = TotalSessions,
            CompletedSessions = CompletedSessions,
            TotalListenSeconds = TotalListenSeconds
        };
    }
}

public sealed class AdminUserLocationDto
{
    public Guid UserId { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string MapLink { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public AdminUserLocationDto Clone()
    {
        return new AdminUserLocationDto
        {
            UserId = UserId,
            UserCode = UserCode,
            DisplayName = DisplayName,
            PoiId = PoiId,
            PoiCode = PoiCode,
            PoiName = PoiName,
            Address = Address,
            Latitude = Latitude,
            Longitude = Longitude,
            MapLink = MapLink,
            UpdatedAtUtc = UpdatedAtUtc
        };
    }
}
