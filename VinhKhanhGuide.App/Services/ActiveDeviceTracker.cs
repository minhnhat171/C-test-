using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.App.Services;

public class ActiveDeviceTracker : IActiveDeviceTracker
{
    private const string DeviceIdPreferenceKey = "vinhkhanh.active_device.id.v1";
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(8);

    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly ILogger<ActiveDeviceTracker> _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly object _syncRoot = new();
    private CancellationTokenSource? _heartbeatCancellation;
    private Task? _heartbeatTask;

    public ActiveDeviceTracker(
        HttpClient httpClient,
        IAuthService authService,
        ILogger<ActiveDeviceTracker> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _logger = logger;
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
            var request = BuildHeartbeatRequest();
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

    private ActiveDeviceHeartbeatRequest BuildHeartbeatRequest()
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
            displayName = "Khach tham quan";
        }

        return new ActiveDeviceHeartbeatRequest
        {
            DeviceId = GetOrCreateDeviceId(),
            UserCode = userCode,
            UserDisplayName = displayName,
            UserEmail = session?.Email ?? string.Empty,
            DevicePlatform = DeviceInfo.Current.Platform.ToString(),
            DeviceModel = BuildDeviceModel(),
            AppVersion = AppInfo.Current.VersionString,
            SentAtUtc = DateTimeOffset.UtcNow
        };
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
}
