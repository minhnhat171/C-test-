using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Platforms.Android.Services;

internal static class LocationForegroundServiceHub
{
    public static event EventHandler<LocationDto>? LocationUpdated;

    public static bool IsRunning { get; private set; }

    public static void Publish(LocationDto location)
    {
        LocationUpdated?.Invoke(null, location);
    }

    public static void SetRunning(bool isRunning)
    {
        IsRunning = isRunning;
    }
}
