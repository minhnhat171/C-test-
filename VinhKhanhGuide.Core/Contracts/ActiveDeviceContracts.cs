namespace VinhKhanhGuide.Core.Contracts;

public sealed class ActiveDeviceHeartbeatRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string ClientInstanceId { get; set; } = string.Empty;
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
    public string ClientInstanceId { get; set; } = string.Empty;
    public DateTimeOffset DisconnectedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ActiveDeviceSessionDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string ClientInstanceId { get; set; } = string.Empty;
    public string SessionKey { get; set; } = string.Empty;
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
            ClientInstanceId = ClientInstanceId,
            SessionKey = SessionKey,
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

public sealed class ActiveDeviceRoutePointDto
{
    public string AnonymousRouteId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }

    public ActiveDeviceRoutePointDto Clone()
    {
        return new ActiveDeviceRoutePointDto
        {
            AnonymousRouteId = AnonymousRouteId,
            Latitude = Latitude,
            Longitude = Longitude,
            AccuracyMeters = AccuracyMeters,
            RecordedAtUtc = RecordedAtUtc
        };
    }
}

public sealed class ActiveDeviceStatsDto
{
    public int ActiveDeviceCount { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ActiveThresholdUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<ActiveDeviceSessionDto> Devices { get; set; } = [];
    public List<ActiveDeviceRoutePointDto> RoutePoints { get; set; } = [];

    public ActiveDeviceStatsDto Clone()
    {
        return new ActiveDeviceStatsDto
        {
            ActiveDeviceCount = ActiveDeviceCount,
            GeneratedAtUtc = GeneratedAtUtc,
            ActiveThresholdUtc = ActiveThresholdUtc,
            Devices = Devices.Select(device => device.Clone()).ToList(),
            RoutePoints = RoutePoints.Select(point => point.Clone()).ToList()
        };
    }
}
