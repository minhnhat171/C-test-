using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public class LocationService : ILocationService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    public event EventHandler<LocationDto>? LocationUpdated;

    private CancellationTokenSource? _cts;
    private Task? _listeningTask;

    public async Task<bool> EnsurePermissionAsync(bool requestIfNeeded = true)
    {
        var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (permissionStatus != PermissionStatus.Granted && requestIfNeeded)
        {
            permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        return permissionStatus == PermissionStatus.Granted;
    }

    public async Task<LocationDto?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, RequestTimeout);
            var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken)
                ?? await Geolocation.Default.GetLastKnownLocationAsync();

            return ToDto(location);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPS Snapshot Error: {ex.Message}");
            return null;
        }
    }

    public async Task StartListeningAsync()
    {
        if (_listeningTask is { IsCompleted: false })
        {
            return;
        }

        var hasPermission = await EnsurePermissionAsync();
        if (!hasPermission)
        {
            throw new InvalidOperationException("Chưa được cấp quyền truy cập vị trí.");
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _listeningTask = ListenLoopAsync(_cts.Token);
    }

    public async Task StopListeningAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();

        if (_listeningTask is not null)
        {
            try
            {
                await _listeningTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts.Dispose();
        _cts = null;
        _listeningTask = null;
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        var firstLocation = await GetCurrentLocationAsync(cancellationToken);
        PublishLocation(firstLocation);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var location = await GetCurrentLocationAsync(cancellationToken);
                PublishLocation(location);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPS Error: {ex.Message}");
            }

            try
            {
                await Task.Delay(PollInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void PublishLocation(Location? location)
    {
        var dto = ToDto(location);
        if (dto is null)
        {
            return;
        }

        LocationUpdated?.Invoke(this, dto);
    }

    private void PublishLocation(LocationDto? location)
    {
        if (location is null)
        {
            return;
        }

        LocationUpdated?.Invoke(this, location);
    }

    private static LocationDto? ToDto(Location? location)
    {
        if (location is null)
        {
            return null;
        }

        return new LocationDto
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            AccuracyMeters = location.Accuracy,
            TimestampUtc = DateTimeOffset.UtcNow
        };
    }
}
