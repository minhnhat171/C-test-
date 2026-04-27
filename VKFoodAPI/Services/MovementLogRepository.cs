using System.Text.Encodings.Web;
using System.Text.Json;
using VinhKhanhGuide.Core.Contracts;

namespace VKFoodAPI.Services;

public sealed class MovementLogRepository
{
    private const int MaxStoredLogs = 10000;

    private readonly object _syncRoot = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly string _dataFilePath;
    private List<MovementLogDto> _items;

    public MovementLogRepository(IHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        _dataFilePath = Path.Combine(dataDirectory, "movement-logs.json");
        _items = LoadItems();
    }

    public IReadOnlyList<MovementLogDto> GetLogs(
        string? userCode = null,
        string? deviceId = null,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        int? limit = null)
    {
        lock (_syncRoot)
        {
            var normalizedUserCode = NormalizeLookup(userCode);
            var normalizedDeviceId = NormalizeLookup(deviceId);
            var query = _items.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(normalizedUserCode))
            {
                query = query.Where(item =>
                    string.Equals(NormalizeLookup(item.UserCode), normalizedUserCode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(NormalizeLookup(item.UserEmail), normalizedUserCode, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(normalizedDeviceId))
            {
                query = query.Where(item =>
                    string.Equals(NormalizeLookup(item.DeviceId), normalizedDeviceId, StringComparison.OrdinalIgnoreCase));
            }

            if (fromUtc.HasValue)
            {
                query = query.Where(item => item.RecordedAtUtc >= fromUtc.Value.ToUniversalTime());
            }

            if (toUtc.HasValue)
            {
                query = query.Where(item => item.RecordedAtUtc <= toUtc.Value.ToUniversalTime());
            }

            query = query
                .OrderByDescending(item => item.RecordedAtUtc)
                .ThenByDescending(item => item.ReceivedAtUtc);

            if (limit is > 0)
            {
                query = query.Take(Math.Min(limit.Value, MaxStoredLogs));
            }

            return query.Select(item => item.Clone()).ToList();
        }
    }

    public MovementLogDto Create(MovementLogCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!HasValidLocation(request.Latitude, request.Longitude))
        {
            throw new ArgumentException("Movement log location is invalid.", nameof(request));
        }

        lock (_syncRoot)
        {
            var created = Normalize(new MovementLogDto
            {
                Id = Guid.NewGuid(),
                DeviceId = request.DeviceId,
                UserCode = request.UserCode,
                UserDisplayName = request.UserDisplayName,
                UserEmail = request.UserEmail,
                DevicePlatform = request.DevicePlatform,
                DeviceModel = request.DeviceModel,
                AppVersion = request.AppVersion,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                AccuracyMeters = request.AccuracyMeters,
                SpeedMetersPerSecond = request.SpeedMetersPerSecond,
                RecordedAtUtc = request.RecordedAtUtc,
                ReceivedAtUtc = DateTimeOffset.UtcNow
            });

            _items.Add(created);
            TrimUnsafe();
            SaveUnsafe();
            return created.Clone();
        }
    }

    private List<MovementLogDto> LoadItems()
    {
        if (!File.Exists(_dataFilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_dataFilePath);
            return (JsonSerializer.Deserialize<List<MovementLogDto>>(json, _jsonOptions) ?? [])
                .Select(Normalize)
                .Where(item => HasValidLocation(item.Latitude, item.Longitude))
                .OrderByDescending(item => item.RecordedAtUtc)
                .Take(MaxStoredLogs)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private void TrimUnsafe()
    {
        if (_items.Count <= MaxStoredLogs)
        {
            return;
        }

        _items = _items
            .OrderByDescending(item => item.RecordedAtUtc)
            .ThenByDescending(item => item.ReceivedAtUtc)
            .Take(MaxStoredLogs)
            .ToList();
    }

    private void SaveUnsafe()
    {
        var json = JsonSerializer.Serialize(_items, _jsonOptions);
        File.WriteAllText(_dataFilePath, json);
    }

    private static MovementLogDto Normalize(MovementLogDto item)
    {
        var normalized = item.Clone();
        normalized.Id = normalized.Id == Guid.Empty ? Guid.NewGuid() : normalized.Id;
        normalized.DeviceId = normalized.DeviceId?.Trim() ?? string.Empty;
        normalized.UserCode = string.IsNullOrWhiteSpace(normalized.UserCode) ? "guest" : normalized.UserCode.Trim();
        normalized.UserDisplayName = normalized.UserDisplayName?.Trim() ?? string.Empty;
        normalized.UserEmail = normalized.UserEmail?.Trim().ToLowerInvariant() ?? string.Empty;
        normalized.DevicePlatform = normalized.DevicePlatform?.Trim() ?? string.Empty;
        normalized.DeviceModel = normalized.DeviceModel?.Trim() ?? string.Empty;
        normalized.AppVersion = normalized.AppVersion?.Trim() ?? string.Empty;
        normalized.AccuracyMeters = normalized.AccuracyMeters is > 0 ? normalized.AccuracyMeters : null;
        normalized.SpeedMetersPerSecond = normalized.SpeedMetersPerSecond is >= 0 ? normalized.SpeedMetersPerSecond : null;
        normalized.RecordedAtUtc = normalized.RecordedAtUtc == default
            ? DateTimeOffset.UtcNow
            : normalized.RecordedAtUtc.ToUniversalTime();
        normalized.ReceivedAtUtc = normalized.ReceivedAtUtc == default
            ? DateTimeOffset.UtcNow
            : normalized.ReceivedAtUtc.ToUniversalTime();
        return normalized;
    }

    private static bool HasValidLocation(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90 &&
               longitude is >= -180 and <= 180;
    }

    private static string NormalizeLookup(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
