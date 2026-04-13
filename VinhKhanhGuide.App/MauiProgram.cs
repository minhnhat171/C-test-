using Mapsui.UI.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.App.ViewModels;
using VinhKhanhGuide.Core.Interfaces;

namespace VinhKhanhGuide.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

#if ANDROID
        // Avoid GLTextureView/EGL rendering failures that can leave the base map blank on Android.
        MapControl.UseGPU = false;
#endif

        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton(_ => new HttpClient
        {
            BaseAddress = PoiApiEndpoint.CreateBaseUri(),
            Timeout = TimeSpan.FromSeconds(8)
        });

        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IAudioSettingsService, AudioSettingsService>();
        builder.Services.AddSingleton<IAccountProfileValidationService, AccountProfileValidationService>();
        builder.Services.AddSingleton<IUsageHistoryService, UsageHistoryService>();
        builder.Services.AddSingleton<IListeningHistorySyncService, ListeningHistorySyncService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IPoiProvider, PoiProvider>();
        builder.Services.AddSingleton<IPoiRepository, PoiRepository>();
        builder.Services.AddSingleton<ISearchService, SearchService>();
        builder.Services.AddSingleton<INarrationService, NarrationService>();
        builder.Services.AddSingleton<GeofenceEngine>();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<AuthPageViewModel>();
        builder.Services.AddTransient<Views.AuthPage>();
        builder.Services.AddTransient<Views.PoiDetailPage>();

        return builder.Build();
    }
}
