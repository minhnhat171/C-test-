using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public class ActiveDeviceTracker : IActiveDeviceTracker
{
    private const string DeviceIdPreferenceKey = "vinhkhanh.active_device.id.v1";
    private readonly string _clientInstanceId = Guid.NewGuid().ToString("N");
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan MaxCachedLocationAge = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan LocationRefreshInterval = TimeSpan.FromSeconds(20);

    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly ILocationService _locationService;
    private readonly ILogger<ActiveDeviceTracker> _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly object _syncRoot = new();
    private readonly object _locationSyncRoot = new();
    private CancellationTokenSource? _heartbeatCancellation;
    private Task? _heartbeatTask;
    private LocationDto? _lastKnownLocation;
    private DateTimeOffset _lastLocationRefreshAttemptUtc = DateTimeOffset.MinValue;

    public ActiveDeviceTracker(
        HttpClient httpClient,
        IAuthService authService,
        ILocationService locationService,
        ILogger<ActiveDeviceTracker> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _locationService = locationService;
        _logger = logger;
        _locationService.LocationUpdated += OnLocationUpdated;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            if (_heartbeatTask is { IsCompleted: false })
            {
                return SendHeartbeatAsync(cancellationToken);
            }

            _heartbeatCancellation = new CancellationTokenSource();
            _heartbeatTask = RunHeartbeatLoopAsync(_heartbeatCancellation.Token);
        }

        return SendHeartbeatAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource? heartbeatCancellation;

        lock (_syncRoot)
        {
            heartbeatCancellation = _heartbeatCancellation;
            _heartbeatCancellation = null;
            _heartbeatTask = null;
        }

        heartbeatCancellation?.Cancel();

        try
        {
            var request = new ActiveDeviceDisconnectRequest
            {
                DeviceId = GetOrCreateDeviceId(),
                ClientInstanceId = _clientInstanceId,
                DisconnectedAtUtc = DateTimeOffset.UtcNow
            };

            await _httpClient.PostAsJsonAsync(
                "api/analytics/active-devices/disconnect",
                request,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Could not mark this app device as offline.");
        }
        finally
        {
            heartbeatCancellation?.Dispose();
        }
    }

    public async Task SendHeartbeatAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await _sendLock.WaitAsync(0, cancellationToken))
            {
                return;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            var request = await BuildHeartbeatRequestAsync(cancellationToken);
            var response = await _httpClient.PostAsJsonAsync(
                "api/analytics/active-devices/heartbeat",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Active device heartbeat returned status {StatusCode}.",
                    response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Could not send active device heartbeat.");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await SendHeartbeatAsync(cancellationToken);

            try
            {
                await Task.Delay(HeartbeatInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task<ActiveDeviceHeartbeatRequest> BuildHeartbeatRequestAsync(CancellationToken cancellationToken)
    {
        var session = _authService.CurrentSession;
        var userCode = session?.LoginId;
        if (string.IsNullOrWhiteSpace(userCode))
        {
            userCode = "guest";
        }

        var displayName = session?.FullName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Khách tham quan";
        }

        var request = new ActiveDeviceHeartbeatRequest
        {
            DeviceId = GetOrCreateDeviceId(),
            ClientInstanceId = _clientInstanceId,
            UserCode = userCode,
            UserDisplayName = displayName,
            UserEmail = session?.Email ?? string.Empty,
            DevicePlatform = DeviceInfo.Current.Platform.ToString(),
            DeviceModel = BuildDeviceModel(),
            AppVersion = AppInfo.Current.VersionString,
            SentAtUtc = DateTimeOffset.UtcNow
        };

        var location = await GetLocationSnapshotAsync(cancellationToken);
        if (location is not null)
        {
            request.Latitude = location.Latitude;
            request.Longitude = location.Longitude;
            request.AccuracyMeters = location.AccuracyMeters;
            request.LocationTimestampUtc = location.TimestampUtc;
        }

        return request;
    }

    private static string BuildDeviceModel()
    {
        var manufacturer = DeviceInfo.Current.Manufacturer?.Trim();
        var model = DeviceInfo.Current.Model?.Trim();

        if (string.IsNullOrWhiteSpace(manufacturer))
        {
            return model ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            return manufacturer;
        }

        return $"{manufacturer} {model}";
    }

    private static string GetOrCreateDeviceId()
    {
        var deviceId = Preferences.Default.Get(DeviceIdPreferenceKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            return deviceId;
        }

        deviceId = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(DeviceIdPreferenceKey, deviceId);
        return deviceId;
    }

    private async Task<LocationDto?> GetLocationSnapshotAsync(CancellationToken cancellationToken)
    {
        var cachedLocation = GetFreshCachedLocation();
        if (cachedLocation is not null)
        {
            return cachedLocation;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        {
            lock (_locationSyncRoot)
            {
                if (nowUtc - _lastLocationRefreshAttemptUtc < LocationRefreshInterval)
                {
                    return null;
                }

                _lastLocationRefreshAttemptUtc = nowUtc;
            }
        }

        try
        {
            var hasPermission = await _locationService.EnsurePermissionAsync(requestIfNeeded: false);
            if (!hasPermission)
            {
                return null;
            }

            var location = await _locationService.GetCurrentLocationAsync(cancellationToken);
            if (location is null)
            {
                return null;
            }

            CacheLocation(location);
            return CloneLocation(location);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Could not capture device location for heartbeat.");
            return null;
        }
    }

    private LocationDto? GetFreshCachedLocation()
    {
        lock (_locationSyncRoot)
        {
            if (_lastKnownLocation is null)
            {
                return null;
            }

            if (DateTimeOffset.UtcNow - _lastKnownLocation.TimestampUtc > MaxCachedLocationAge)
            {
                return null;
            }

            return CloneLocation(_lastKnownLocation);
        }
    }

    private void OnLocationUpdated(object? sender, LocationDto location)
    {
        CacheLocation(location);
    }

    private void CacheLocation(LocationDto location)
    {
        lock (_locationSyncRoot)
        {
            _lastKnownLocation = CloneLocation(location);
        }
    }

    private static LocationDto CloneLocation(LocationDto location)
    {
        return new LocationDto
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            AccuracyMeters = location.AccuracyMeters,
            TimestampUtc = location.TimestampUtc
        };
    }
}
