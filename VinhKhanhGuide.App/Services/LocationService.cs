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

    public async Task StartListeningAsync()
    {
        if (_listeningTask is { IsCompleted: false })
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
        var lastKnownLocation = await Geolocation.Default.GetLastKnownLocationAsync();
        PublishLocation(lastKnownLocation);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, RequestTimeout);
                var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken)
                    ?? await Geolocation.Default.GetLastKnownLocationAsync();

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
        if (location is null)
        {
            return;
        }

        LocationUpdated?.Invoke(this, new LocationDto
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            AccuracyMeters = location.Accuracy,
            TimestampUtc = DateTimeOffset.UtcNow
        });
    }
}
