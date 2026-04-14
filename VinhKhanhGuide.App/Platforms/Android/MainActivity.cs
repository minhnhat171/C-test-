using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Webkit;
using VinhKhanhGuide.App.Services;

namespace VinhKhanhGuide.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "vinhkhanhguide")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        PublishQrRequest(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        PublishQrRequest(intent);
    }

    private static void PublishQrRequest(Intent? intent)
    {
        if (intent?.DataString is { Length: > 0 } dataString)
        {
            QrDeepLinkBroker.Publish(dataString);
        }
    }
}
