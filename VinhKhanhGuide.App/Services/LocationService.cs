using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;
#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using VinhKhanhGuide.App.Platforms.Android.Services;
#endif

namespace VinhKhanhGuide.App.Services;

public class LocationService : ILocationService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaxLastKnownLocationAge = TimeSpan.FromMinutes(5);
    private const double MaxAcceptedAccuracyMeters = 5000;

    public event EventHandler<LocationDto>? LocationUpdated;

#if !ANDROID
    private CancellationTokenSource? _cts;
    private Task? _listeningTask;
#endif

    public LocationService()
    {
#if ANDROID
        LocationForegroundServiceHub.LocationUpdated += OnAndroidForegroundLocationUpdated;
#endif
    }

    public async Task<bool> EnsurePermissionAsync(bool requestIfNeeded = true)
    {
        var locationWhenInUseStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (locationWhenInUseStatus != PermissionStatus.Granted && requestIfNeeded)
        {
            locationWhenInUseStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (locationWhenInUseStatus != PermissionStatus.Granted)
        {
            return false;
        }

#if ANDROID
        if (requestIfNeeded)
        {
            try
            {
                await Permissions.RequestAsync<Permissions.LocationAlways>();
            }
            catch
            {
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                try
                {
                    await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
                catch
                {
                }
            }
        }
#endif

        return true;
    }

    public async Task<LocationDto?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, RequestTimeout);
            var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken);
            if (IsUsableLocation(location))
            {
                return ToDto(location);
            }

            var lastKnownLocation = await Geolocation.Default.GetLastKnownLocationAsync();
            if (lastKnownLocation is not null &&
                IsUsableLocation(lastKnownLocation) &&
                IsRecentLocation(lastKnownLocation, MaxLastKnownLocationAge))
            {
                return ToDto(lastKnownLocation);
            }

            return null;
        }
        catch (System.OperationCanceledException)
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
        var hasPermission = await EnsurePermissionAsync();
        if (!hasPermission)
        {
            throw new InvalidOperationException("Chưa được cấp quyền truy cập vị trí.");
        }

#if ANDROID
        if (!LocationForegroundServiceHub.IsRunning)
        {
            var context = global::Android.App.Application.Context;
            var intent = LocationForegroundService.CreateStartIntent(context);

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        var firstLocation = await GetCurrentLocationAsync();
        PublishLocation(firstLocation);
        return;
#else
        if (_listeningTask is { IsCompleted: false })
        {
            return;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _listeningTask = ListenLoopAsync(_cts.Token);
#endif
    }

    public async Task StopListeningAsync()
    {
#if ANDROID
        if (!LocationForegroundServiceHub.IsRunning)
        {
            return;
        }

        var context = global::Android.App.Application.Context;
        context.StartService(LocationForegroundService.CreateStopIntent(context));
        await Task.CompletedTask;
        return;
#else
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
#endif
    }

#if !ANDROID
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
#endif

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

#if ANDROID
    private void OnAndroidForegroundLocationUpdated(object? sender, LocationDto location)
    {
        PublishLocation(location);
    }
#endif

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
            TimestampUtc = location.Timestamp.ToUniversalTime()
        };
    }

    private static bool IsUsableLocation(Location? location)
    {
        return location is not null &&
               IsValidCoordinate(location.Latitude, location.Longitude) &&
               (location.Accuracy is null || location.Accuracy <= MaxAcceptedAccuracyMeters);
    }

    public static bool IsValidCoordinate(double latitude, double longitude)
    {
        return !double.IsNaN(latitude) &&
               !double.IsNaN(longitude) &&
               !double.IsInfinity(latitude) &&
               !double.IsInfinity(longitude) &&
               latitude is >= -90 and <= 90 &&
               longitude is >= -180 and <= 180 &&
               (Math.Abs(latitude) > 0.000001 || Math.Abs(longitude) > 0.000001);
    }

    private static bool IsRecentLocation(Location location, TimeSpan maxAge)
    {
        return DateTimeOffset.UtcNow - location.Timestamp.ToUniversalTime() <= maxAge;
    }
}
