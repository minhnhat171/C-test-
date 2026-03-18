using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public class LocationService : ILocationService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private CancellationTokenSource? _trackingCts;

    public event EventHandler<LocationDto>? LocationUpdated;

    public async Task StartListeningAsync()
    {
        if (_trackingCts is not null)
        {
            return;
        }

        var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (permissionStatus != PermissionStatus.Granted)
        {
            permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (permissionStatus != PermissionStatus.Granted)
        {
            throw new InvalidOperationException("Ung dung chua duoc cap quyen truy cap vi tri.");
        }

        _trackingCts = new CancellationTokenSource();
        _ = Task.Run(() => TrackLoopAsync(_trackingCts.Token), _trackingCts.Token);
    }

    public Task StopListeningAsync()
    {
        _trackingCts?.Cancel();
        _trackingCts?.Dispose();
        _trackingCts = null;
        return Task.CompletedTask;
    }

    private async Task TrackLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
                var location = await Geolocation.GetLocationAsync(request, cancellationToken)
                               ?? await Geolocation.GetLastKnownLocationAsync();

                if (location is not null)
                {
                    LocationUpdated?.Invoke(this, new LocationDto
                    {
                        Latitude = location.Latitude,
                        Longitude = location.Longitude,
                        AccuracyMeters = location.Accuracy,
                        TimestampUtc = DateTimeOffset.UtcNow
                    });
                }
            }
            catch (Exception)
            {
                // Keep loop alive; UI handles stale status.
            }

            try
            {
                await Task.Delay(_pollingInterval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
