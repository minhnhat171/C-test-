namespace VinhKhanhGuide.Core.Contracts;

public sealed class ActiveDeviceHeartbeatRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string DevicePlatform { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public DateTimeOffset SentAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public DateTimeOffset? LocationTimestampUtc { get; set; }
}

public sealed class ActiveDeviceDisconnectRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTimeOffset DisconnectedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ActiveDeviceSessionDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string DevicePlatform { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public DateTimeOffset ConnectedAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public int SecondsSinceLastSeen { get; set; }
    public bool IsActive { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public DateTimeOffset? LocationTimestampUtc { get; set; }

    public ActiveDeviceSessionDto Clone()
    {
        return new ActiveDeviceSessionDto
        {
            DeviceId = DeviceId,
            UserCode = UserCode,
            UserDisplayName = UserDisplayName,
            UserEmail = UserEmail,
            DevicePlatform = DevicePlatform,
            DeviceModel = DeviceModel,
            AppVersion = AppVersion,
            ConnectedAtUtc = ConnectedAtUtc,
            LastSeenAtUtc = LastSeenAtUtc,
            SecondsSinceLastSeen = SecondsSinceLastSeen,
            IsActive = IsActive,
            Latitude = Latitude,
            Longitude = Longitude,
            AccuracyMeters = AccuracyMeters,
            LocationTimestampUtc = LocationTimestampUtc
        };
    }
}

public sealed class ActiveDeviceStatsDto
{
    public int ActiveDeviceCount { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ActiveThresholdUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<ActiveDeviceSessionDto> Devices { get; set; } = [];

    public ActiveDeviceStatsDto Clone()
    {
        return new ActiveDeviceStatsDto
        {
            ActiveDeviceCount = ActiveDeviceCount,
            GeneratedAtUtc = GeneratedAtUtc,
            ActiveThresholdUtc = ActiveThresholdUtc,
            Devices = Devices.Select(device => device.Clone()).ToList()
        };
    }
}
