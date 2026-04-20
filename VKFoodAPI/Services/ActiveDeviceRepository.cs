using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;

namespace VKFoodAPI.Services;

public class ActiveDeviceRepository
{
    private static readonly TimeSpan ActiveTimeout = TimeSpan.FromSeconds(30);

    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly string _dataFilePath;
    private readonly Dictionary<string, ActiveDeviceSessionDto> _devices;

    public ActiveDeviceRepository(IHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "active-devices.json");
        _devices = LoadDevices()
            .Where(device => !string.IsNullOrWhiteSpace(device.DeviceId))
            .GroupBy(device => NormalizeDeviceId(device.DeviceId), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => NormalizeStoredDevice(group.First()), StringComparer.OrdinalIgnoreCase);
    }

    public ActiveDeviceStatsDto GetStats()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        ActiveDeviceStatsDto stats;
        bool changed;

        lock (_syncRoot)
        {
            changed = PruneInactiveUnsafe(nowUtc);
            if (changed)
            {
                SaveUnsafe();
            }

            stats = BuildStatsUnsafe(nowUtc);
        }

        return stats;
    }

    public ActiveDeviceStatsDto RegisterHeartbeat(ActiveDeviceHeartbeatRequest request)
    {
        var deviceId = NormalizeDeviceId(request.DeviceId);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(request));
        }

        var nowUtc = DateTimeOffset.UtcNow;
        ActiveDeviceStatsDto stats;

        lock (_syncRoot)
        {
            PruneInactiveUnsafe(nowUtc);

            if (!_devices.TryGetValue(deviceId, out var device))
            {
                device = new ActiveDeviceSessionDto
                {
                    DeviceId = deviceId,
                    ConnectedAtUtc = nowUtc
                };
                _devices[deviceId] = device;
            }

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

            SaveUnsafe();
            stats = BuildStatsUnsafe(nowUtc);
        }

        return stats;
    }

    public ActiveDeviceStatsDto Disconnect(ActiveDeviceDisconnectRequest request)
    {
        var deviceId = NormalizeDeviceId(request.DeviceId);
        var nowUtc = DateTimeOffset.UtcNow;
        ActiveDeviceStatsDto stats;

        lock (_syncRoot)
        {
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                _devices.Remove(deviceId);
            }

            PruneInactiveUnsafe(nowUtc);
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
            var changed = PruneInactiveUnsafe(nowUtc);
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
            Devices = activeDevices
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

    private void SaveUnsafe()
    {
        var json = JsonSerializer.Serialize(_devices.Values.OrderBy(device => device.DeviceId).ToList(), _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private static ActiveDeviceSessionDto NormalizeStoredDevice(ActiveDeviceSessionDto device)
    {
        var normalized = device.Clone();
        normalized.DeviceId = NormalizeDeviceId(normalized.DeviceId);
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

    private static bool HasValidLocation(double? latitude, double? longitude)
    {
        return latitude is >= -90 and <= 90 &&
               longitude is >= -180 and <= 180;
    }

    private static string NormalizeDeviceId(string? deviceId)
    {
        return deviceId?.Trim().ToLowerInvariant() ?? string.Empty;
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

        return string.IsNullOrWhiteSpace(userCode) ? "Khach tham quan" : userCode;
    }
}
