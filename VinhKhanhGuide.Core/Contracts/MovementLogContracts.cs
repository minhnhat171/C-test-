namespace VinhKhanhGuide.Core.Contracts;

public sealed class MovementLogCreateRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string DevicePlatform { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public double? SpeedMetersPerSecond { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class MovementLogDto
{
    public Guid Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string DevicePlatform { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public double? SpeedMetersPerSecond { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }

    public MovementLogDto Clone()
    {
        return new MovementLogDto
        {
            Id = Id,
            DeviceId = DeviceId,
            UserCode = UserCode,
            UserDisplayName = UserDisplayName,
            UserEmail = UserEmail,
            DevicePlatform = DevicePlatform,
            DeviceModel = DeviceModel,
            AppVersion = AppVersion,
            Latitude = Latitude,
            Longitude = Longitude,
            AccuracyMeters = AccuracyMeters,
            SpeedMetersPerSecond = SpeedMetersPerSecond,
            RecordedAtUtc = RecordedAtUtc,
            ReceivedAtUtc = ReceivedAtUtc
        };
    }
}
