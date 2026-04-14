using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.OS;
using VinhKhanhGuide.Core.Models;
using AndroidLocation = Android.Locations.Location;

namespace VinhKhanhGuide.App.Platforms.Android.Services;

[Service(
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeLocation)]
public sealed class LocationForegroundService : Service
{
    private const string StartAction = "VinhKhanhGuide.App.action.START_LOCATION_TRACKING";
    private const string StopAction = "VinhKhanhGuide.App.action.STOP_LOCATION_TRACKING";
    private const string ChannelId = "vinhkhanh.location.tracking";
    private const int NotificationId = 41021;
    private const long GpsMinTimeMs = 3_000;
    private const long NetworkMinTimeMs = 5_000;
    private const float GpsMinDistanceMeters = 5f;
    private const float NetworkMinDistanceMeters = 10f;

    private LocationManager? _locationManager;
    private BackgroundLocationListener? _gpsListener;
    private BackgroundLocationListener? _networkListener;

    public static Intent CreateStartIntent(Context context)
    {
        var intent = new Intent(context, typeof(LocationForegroundService));
        intent.SetAction(StartAction);
        return intent;
    }

    public static Intent CreateStopIntent(Context context)
    {
        var intent = new Intent(context, typeof(LocationForegroundService));
        intent.SetAction(StopAction);
        return intent;
    }

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (string.Equals(intent?.Action, StopAction, StringComparison.Ordinal))
        {
            StopTracking();
            if (OperatingSystem.IsAndroidVersionAtLeast(24))
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
#pragma warning disable CA1422
                StopForeground(true);
#pragma warning restore CA1422
            }
            StopSelf();
            LocationForegroundServiceHub.SetRunning(false);
            return StartCommandResult.NotSticky;
        }

        CreateNotificationChannelIfNeeded();
        StartForeground(NotificationId, BuildNotification(null));
        StartTracking();
        LocationForegroundServiceHub.SetRunning(true);

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        StopTracking();
        LocationForegroundServiceHub.SetRunning(false);
        base.OnDestroy();
    }

    private void StartTracking()
    {
        if (_locationManager is not null)
        {
            return;
        }

        _locationManager = GetSystemService(LocationService) as LocationManager;
        if (_locationManager is null)
        {
            return;
        }

        _gpsListener ??= new BackgroundLocationListener(HandleLocationChanged);
        _networkListener ??= new BackgroundLocationListener(HandleLocationChanged);

        try
        {
            if (_locationManager.IsProviderEnabled(LocationManager.GpsProvider))
            {
                _locationManager.RequestLocationUpdates(
                    LocationManager.GpsProvider,
                    GpsMinTimeMs,
                    GpsMinDistanceMeters,
                    _gpsListener,
                    Looper.MainLooper);
            }

            if (_locationManager.IsProviderEnabled(LocationManager.NetworkProvider))
            {
                _locationManager.RequestLocationUpdates(
                    LocationManager.NetworkProvider,
                    NetworkMinTimeMs,
                    NetworkMinDistanceMeters,
                    _networkListener,
                    Looper.MainLooper);
            }

            PublishLastKnownLocation();
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Warn("VKGuide.LocationService", $"Cannot start background tracking: {ex}");
        }
    }

    private void StopTracking()
    {
        if (_locationManager is null)
        {
            return;
        }

        try
        {
            if (_gpsListener is not null)
            {
                _locationManager.RemoveUpdates(_gpsListener);
            }

            if (_networkListener is not null)
            {
                _locationManager.RemoveUpdates(_networkListener);
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Warn("VKGuide.LocationService", $"Cannot stop background tracking cleanly: {ex}");
        }
        finally
        {
            _locationManager = null;
        }
    }

    private void PublishLastKnownLocation()
    {
        if (_locationManager is null)
        {
            return;
        }

        try
        {
            AndroidLocation? lastKnownLocation = null;

            if (_locationManager.IsProviderEnabled(LocationManager.GpsProvider))
            {
                lastKnownLocation = _locationManager.GetLastKnownLocation(LocationManager.GpsProvider);
            }

            if (lastKnownLocation is null && _locationManager.IsProviderEnabled(LocationManager.NetworkProvider))
            {
                lastKnownLocation = _locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
            }

            if (lastKnownLocation is not null)
            {
                HandleLocationChanged(lastKnownLocation);
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Warn("VKGuide.LocationService", $"Cannot publish last known background location: {ex}");
        }
    }

    private void HandleLocationChanged(AndroidLocation location)
    {
        var dto = new LocationDto
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            AccuracyMeters = location.HasAccuracy ? location.Accuracy : null,
            TimestampUtc = DateTimeOffset.UtcNow
        };

        LocationForegroundServiceHub.Publish(dto);
        UpdateNotification(dto);
    }

    private void UpdateNotification(LocationDto location)
    {
        var notificationManager = GetSystemService(NotificationService) as NotificationManager;
        notificationManager?.Notify(NotificationId, BuildNotification(location));
    }

    private Notification BuildNotification(LocationDto? location)
    {
        var tapIntent = new Intent(this, typeof(MainActivity));
        tapIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

        var pendingIntent = PendingIntent.GetActivity(
            this,
            0,
            tapIntent,
            OperatingSystem.IsAndroidVersionAtLeast(23)
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                : PendingIntentFlags.UpdateCurrent);

        var contentText = location is null
            ? "Đang theo dõi vị trí để kích hoạt thuyết minh tự động."
            : $"Lat {location.Latitude:F5}, Lng {location.Longitude:F5}";

        Notification.Builder builder;
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            builder = new Notification.Builder(this, ChannelId);
        }
        else
        {
#pragma warning disable CA1422
            builder = new Notification.Builder(this);
#pragma warning restore CA1422
        }

        builder
            .SetContentTitle("Vinh Khanh Guide đang tracking nền")
            .SetContentText(contentText)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetContentIntent(pendingIntent);

        return builder.Build();
    }

    private void CreateNotificationChannelIfNeeded()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var notificationManager = GetSystemService(NotificationService) as NotificationManager;
        if (notificationManager is null)
        {
            return;
        }

        var channel = new NotificationChannel(
            ChannelId,
            "Tracking vị trí nền",
            NotificationImportance.Low)
        {
            Description = "Giữ foreground service để tiếp tục theo dõi vị trí và geofence khi app chạy nền."
        };

        notificationManager.CreateNotificationChannel(channel);
    }

    private sealed class BackgroundLocationListener(Action<AndroidLocation> onLocationChanged)
        : Java.Lang.Object, ILocationListener
    {
        public void OnLocationChanged(AndroidLocation location)
        {
            onLocationChanged(location);
        }

#pragma warning disable CS0618
        public void OnProviderDisabled(string? provider)
        {
        }

        public void OnProviderEnabled(string? provider)
        {
        }

        public void OnStatusChanged(string? provider, Availability status, Bundle? extras)
        {
        }
#pragma warning restore CS0618
    }
}
