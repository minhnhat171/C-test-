using System.Text.Encodings.Web;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using VinhKhanhGuide.Core.Contracts;

namespace VKFoodAPI.Services;

public class ActiveDeviceRepository
{
    private static readonly TimeSpan ActiveTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan RouteRetention = TimeSpan.FromHours(12);
    private const int MaxRoutePoints = 1200;

    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly string _dataFilePath;
    private readonly string _routeDataFilePath;
    private readonly Dictionary<string, ActiveDeviceSessionDto> _devices;
    private readonly List<ActiveDeviceRoutePointDto> _routePoints;

    public ActiveDeviceRepository(IHostEnvironment environment)
    {
        var dataDirectory = AppDataPathResolver.GetDataDirectory(environment);
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "active-devices.json");
        _routeDataFilePath = Path.Combine(dataDirectory, "active-device-routes.json");
        _devices = LoadDevices()
            .Where(device => !string.IsNullOrWhiteSpace(device.DeviceId))
            .GroupBy(device => BuildSessionKey(device.DeviceId, device.ClientInstanceId), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => NormalizeStoredDevice(group.First()), StringComparer.OrdinalIgnoreCase);
        _routePoints = LoadRoutePoints();
    }

    public ActiveDeviceStatsDto GetStats()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        ActiveDeviceStatsDto stats;
        bool changed;

        lock (_syncRoot)
        {
            changed = PruneInactiveUnsafe(nowUtc) | PruneRoutePointsUnsafe(nowUtc);
            if (changed)
            {
                SaveUnsafe();
            }

            stats = BuildStatsUnsafe(nowUtc);
        }

        return stats;
    }

    public IReadOnlyList<ActiveDeviceSessionDto> GetRawSessions()
    {
        var nowUtc = DateTimeOffset.UtcNow;

        lock (_syncRoot)
        {
            PruneInactiveUnsafe(nowUtc);

            return _devices.Values
                .OrderByDescending(device => device.LastSeenAtUtc)
                .ThenBy(device => device.UserDisplayName)
                .ThenBy(device => device.SessionKey)
                .Select(device =>
                {
                    var snapshot = device.Clone();
                    snapshot.IsActive = snapshot.LastSeenAtUtc >= nowUtc.Subtract(ActiveTimeout);
                    snapshot.SecondsSinceLastSeen = Math.Max(0, (int)Math.Round((nowUtc - snapshot.LastSeenAtUtc).TotalSeconds));
                    return snapshot;
                })
                .ToList();
        }
    }

    public ActiveDeviceStatsDto RegisterHeartbeat(ActiveDeviceHeartbeatRequest request)
    {
        var deviceId = NormalizeDeviceId(request.DeviceId);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(request));
        }

        var clientInstanceId = NormalizeClientInstanceId(request.ClientInstanceId);
        var sessionKey = BuildSessionKey(deviceId, clientInstanceId);
        var nowUtc = DateTimeOffset.UtcNow;
        ActiveDeviceStatsDto stats;

        lock (_syncRoot)
        {
            PruneInactiveUnsafe(nowUtc);
            PruneRoutePointsUnsafe(nowUtc);

            if (!_devices.TryGetValue(sessionKey, out var device))
            {
                device = new ActiveDeviceSessionDto
                {
                    DeviceId = deviceId,
                    ClientInstanceId = clientInstanceId,
                    SessionKey = sessionKey,
                    ConnectedAtUtc = nowUtc
                };
                _devices[sessionKey] = device;
            }

            device.DeviceId = deviceId;
            device.ClientInstanceId = clientInstanceId;
            device.SessionKey = sessionKey;
            device.UserCode = NormalizeUserCode(request.UserCode);
            device.UserDisplayName = NormalizeDisplayName(request.UserDisplayName, device.UserCode);
            device.UserEmail = request.UserEmail?.Trim() ?? string.Empty;
            device.DevicePlatform = request.DevicePlatform?.Trim() ?? string.Empty;
            device.DeviceModel = request.DeviceModel?.Trim() ?? string.Empty;
            device.AppVersion = request.AppVersion?.Trim() ?? string.Empty;
            device.LastSeenAtUtc = nowUtc;
            device.IsActive = true;
            device.SecondsSinceLastSeen = 0;
            UpdateLocationUnsafe(device, request);
            TryAppendRoutePointUnsafe(sessionKey, request, nowUtc);

            SaveUnsafe();
            stats = BuildStatsUnsafe(nowUtc);
        }

        return stats;
    }

    public ActiveDeviceStatsDto Disconnect(ActiveDeviceDisconnectRequest request)
    {
        var deviceId = NormalizeDeviceId(request.DeviceId);
        var clientInstanceId = NormalizeClientInstanceId(request.ClientInstanceId);
        var nowUtc = DateTimeOffset.UtcNow;
        ActiveDeviceStatsDto stats;

        lock (_syncRoot)
        {
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                if (!string.IsNullOrWhiteSpace(clientInstanceId))
                {
                    _devices.Remove(BuildSessionKey(deviceId, clientInstanceId));
                }
                else
                {
                    var matchingKeys = _devices
                        .Where(pair => string.Equals(pair.Value.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
                        .Select(pair => pair.Key)
                        .ToList();

                    foreach (var key in matchingKeys)
                    {
                        _devices.Remove(key);
                    }
                }
            }

            PruneInactiveUnsafe(nowUtc);
            PruneRoutePointsUnsafe(nowUtc);
            SaveUnsafe();
            stats = BuildStatsUnsafe(nowUtc);
        }

        return stats;
    }

    public bool PruneInactive()
    {
        var nowUtc = DateTimeOffset.UtcNow;

        lock (_syncRoot)
        {
            var changed = PruneInactiveUnsafe(nowUtc) | PruneRoutePointsUnsafe(nowUtc);
            if (changed)
            {
                SaveUnsafe();
            }

            return changed;
        }
    }

    private ActiveDeviceStatsDto BuildStatsUnsafe(DateTimeOffset nowUtc)
    {
        var thresholdUtc = nowUtc.Subtract(ActiveTimeout);
        var activeDevices = _devices.Values
            .Where(device => device.LastSeenAtUtc >= thresholdUtc)
            .Select(device =>
            {
                var snapshot = device.Clone();
                snapshot.IsActive = true;
                snapshot.SecondsSinceLastSeen = Math.Max(0, (int)Math.Round((nowUtc - snapshot.LastSeenAtUtc).TotalSeconds));
                return snapshot;
            })
            .OrderByDescending(device => device.LastSeenAtUtc)
            .ThenBy(device => device.UserDisplayName)
            .ThenBy(device => device.DeviceId)
            .ToList();

        return new ActiveDeviceStatsDto
        {
            ActiveDeviceCount = activeDevices.Count,
            GeneratedAtUtc = nowUtc,
            ActiveThresholdUtc = thresholdUtc,
            Devices = activeDevices,
            RoutePoints = _routePoints
                .OrderBy(point => point.RecordedAtUtc)
                .Select(point => point.Clone())
                .ToList()
        };
    }

    private bool PruneInactiveUnsafe(DateTimeOffset nowUtc)
    {
        var thresholdUtc = nowUtc.Subtract(ActiveTimeout);
        var inactiveDeviceIds = _devices
            .Where(pair => pair.Value.LastSeenAtUtc < thresholdUtc)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var deviceId in inactiveDeviceIds)
        {
            _devices.Remove(deviceId);
        }

        return inactiveDeviceIds.Count > 0;
    }

    private bool PruneRoutePointsUnsafe(DateTimeOffset nowUtc)
    {
        var thresholdUtc = nowUtc.Subtract(RouteRetention);
        var beforeCount = _routePoints.Count;

        _routePoints.RemoveAll(point =>
            point.RecordedAtUtc < thresholdUtc ||
            !HasValidLocation(point.Latitude, point.Longitude));

        if (_routePoints.Count > MaxRoutePoints)
        {
            var overflow = _routePoints
                .OrderBy(point => point.RecordedAtUtc)
                .Take(_routePoints.Count - MaxRoutePoints)
                .ToHashSet();

            _routePoints.RemoveAll(overflow.Contains);
        }

        return _routePoints.Count != beforeCount;
    }

    private List<ActiveDeviceSessionDto> LoadDevices()
    {
        if (!File.Exists(_dataFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_dataFilePath);
            return JsonSerializer.Deserialize<List<ActiveDeviceSessionDto>>(json, _jsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private List<ActiveDeviceRoutePointDto> LoadRoutePoints()
    {
        if (!File.Exists(_routeDataFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_routeDataFilePath);
            return (JsonSerializer.Deserialize<List<ActiveDeviceRoutePointDto>>(json, _jsonOptions) ?? [])
                .Select(NormalizeStoredRoutePoint)
                .Where(point => !string.IsNullOrWhiteSpace(point.AnonymousRouteId))
                .Where(point => HasValidLocation(point.Latitude, point.Longitude))
                .OrderBy(point => point.RecordedAtUtc)
                .TakeLast(MaxRoutePoints)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private void SaveUnsafe()
    {
        var json = JsonSerializer.Serialize(_devices.Values.OrderBy(device => device.SessionKey).ToList(), _jsonOptions);
        File.WriteAllText(_dataFilePath, json);

        var routeJson = JsonSerializer.Serialize(
            _routePoints.OrderBy(point => point.RecordedAtUtc).ToList(),
            _jsonOptions);
        File.WriteAllText(_routeDataFilePath, routeJson);
    }

    private static ActiveDeviceSessionDto NormalizeStoredDevice(ActiveDeviceSessionDto device)
    {
        var normalized = device.Clone();
        normalized.DeviceId = NormalizeDeviceId(normalized.DeviceId);
        normalized.ClientInstanceId = NormalizeClientInstanceId(normalized.ClientInstanceId);
        normalized.SessionKey = BuildSessionKey(normalized.DeviceId, normalized.ClientInstanceId);
        normalized.UserCode = NormalizeUserCode(normalized.UserCode);
        normalized.UserDisplayName = NormalizeDisplayName(normalized.UserDisplayName, normalized.UserCode);
        normalized.UserEmail ??= string.Empty;
        normalized.DevicePlatform ??= string.Empty;
        normalized.DeviceModel ??= string.Empty;
        normalized.AppVersion ??= string.Empty;
        normalized.ConnectedAtUtc = normalized.ConnectedAtUtc == default
            ? normalized.LastSeenAtUtc
            : normalized.ConnectedAtUtc;
        normalized.LastSeenAtUtc = normalized.LastSeenAtUtc == default
            ? DateTimeOffset.MinValue
            : normalized.LastSeenAtUtc;
        normalized.LocationTimestampUtc = normalized.LocationTimestampUtc == default
            ? null
            : normalized.LocationTimestampUtc;
        return normalized;
    }

    private static ActiveDeviceRoutePointDto NormalizeStoredRoutePoint(ActiveDeviceRoutePointDto point)
    {
        var normalized = point.Clone();
        normalized.AnonymousRouteId = normalized.AnonymousRouteId?.Trim() ?? string.Empty;
        normalized.AccuracyMeters = normalized.AccuracyMeters is > 0 ? normalized.AccuracyMeters : null;
        normalized.RecordedAtUtc = normalized.RecordedAtUtc == default
            ? DateTimeOffset.UtcNow
            : normalized.RecordedAtUtc;
        return normalized;
    }

    private static void UpdateLocationUnsafe(ActiveDeviceSessionDto device, ActiveDeviceHeartbeatRequest request)
    {
        if (!HasValidLocation(request.Latitude, request.Longitude))
        {
            return;
        }

        device.Latitude = request.Latitude;
        device.Longitude = request.Longitude;
        device.AccuracyMeters = request.AccuracyMeters is > 0 ? request.AccuracyMeters : null;
        device.LocationTimestampUtc = request.LocationTimestampUtc ?? request.SentAtUtc;
    }

    private void TryAppendRoutePointUnsafe(
        string deviceId,
        ActiveDeviceHeartbeatRequest request,
        DateTimeOffset fallbackUtc)
    {
        if (!HasValidLocation(request.Latitude, request.Longitude) ||
            !request.Latitude.HasValue ||
            !request.Longitude.HasValue)
        {
            return;
        }

        var latitude = request.Latitude.Value;
        var longitude = request.Longitude.Value;
        var anonymousRouteId = BuildAnonymousRouteId(deviceId);
        var recordedAtUtc = request.LocationTimestampUtc ?? request.SentAtUtc;
        if (recordedAtUtc == default)
        {
            recordedAtUtc = fallbackUtc;
        }

        var previousPoint = _routePoints
            .Where(point => string.Equals(point.AnonymousRouteId, anonymousRouteId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(point => point.RecordedAtUtc)
            .FirstOrDefault();

        if (previousPoint is not null &&
            Math.Abs(previousPoint.Latitude - latitude) < 0.00001 &&
            Math.Abs(previousPoint.Longitude - longitude) < 0.00001 &&
            Math.Abs((recordedAtUtc - previousPoint.RecordedAtUtc).TotalSeconds) < 15)
        {
            return;
        }

        _routePoints.Add(new ActiveDeviceRoutePointDto
        {
            AnonymousRouteId = anonymousRouteId,
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = request.AccuracyMeters is > 0 ? request.AccuracyMeters : null,
            RecordedAtUtc = recordedAtUtc
        });

        PruneRoutePointsUnsafe(fallbackUtc);
    }

    private static bool HasValidLocation(double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return false;
        }

        if (latitude.Value is < -90 or > 90 ||
            longitude.Value is < -180 or > 180)
        {
            return false;
        }

        return Math.Abs(latitude.Value) > 0.000001 ||
               Math.Abs(longitude.Value) > 0.000001;
    }

    private static string BuildAnonymousRouteId(string deviceId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(deviceId));
        return $"ANON-{Convert.ToHexString(hash.AsSpan(0, 3))}";
    }

    private static string BuildSessionKey(string? deviceId, string? clientInstanceId)
    {
        var normalizedDeviceId = NormalizeDeviceId(deviceId);
        var normalizedClientInstanceId = NormalizeClientInstanceId(clientInstanceId);

        return string.IsNullOrWhiteSpace(normalizedClientInstanceId)
            ? normalizedDeviceId
            : $"{normalizedDeviceId}:{normalizedClientInstanceId}";
    }

    private static string NormalizeDeviceId(string? deviceId)
    {
        return deviceId?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string NormalizeClientInstanceId(string? clientInstanceId)
    {
        return clientInstanceId?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string NormalizeUserCode(string? userCode)
    {
        return string.IsNullOrWhiteSpace(userCode)
            ? "guest"
            : userCode.Trim();
    }

    private static string NormalizeDisplayName(string? displayName, string userCode)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        return string.IsNullOrWhiteSpace(userCode) ? "Khách tham quan" : userCode;
    }
}
