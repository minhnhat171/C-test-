using Mapsui.UI.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.App.ViewModels;
using VinhKhanhGuide.App.Views;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Services;

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

        builder.Services.AddSingleton(_ =>
        {
            var client = new HttpClient
            {
                BaseAddress = PoiApiEndpoint.CreateBaseUri(),
                Timeout = TimeSpan.FromSeconds(8)
            };

            if (client.BaseAddress.Host.EndsWith(".ngrok-free.dev", StringComparison.OrdinalIgnoreCase))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
            }

            return client;
        });

        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IUserProfileSyncService, UserProfileSyncService>();
        builder.Services.AddSingleton<IAudioSettingsService, AudioSettingsService>();
        builder.Services.AddSingleton<IAudioAssetCacheService, AudioAssetCacheService>();
        builder.Services.AddSingleton<IAccountProfileValidationService, AccountProfileValidationService>();
        builder.Services.AddSingleton<IUsageHistoryService, UsageHistoryService>();
        builder.Services.AddSingleton<IListeningHistorySyncService, ListeningHistorySyncService>();
        builder.Services.AddSingleton<IActiveDeviceTracker, ActiveDeviceTracker>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IMapOfflineTileService, MapOfflineTileService>();
        builder.Services.AddSingleton<IPoiOfflineStore, PoiOfflineStore>();
        builder.Services.AddSingleton<IAutoPoiSelectionService, PoiAutoNarrationDecisionService>();
        builder.Services.AddSingleton<IPoiProvider, PoiProvider>();
        builder.Services.AddSingleton<IPoiRepository, PoiRepository>();
        builder.Services.AddSingleton<ITourProvider, TourProvider>();
        builder.Services.AddSingleton<ITourRepository, TourRepository>();
        builder.Services.AddSingleton<ISearchService, SearchService>();
        builder.Services.AddSingleton<INarrationService, NarrationService>();
        builder.Services.AddSingleton<GeofenceEngine>();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<AppShell>();
        builder.Services.AddTransient<AuthPageViewModel>();
        builder.Services.AddTransient<Views.AuthPage>();
        builder.Services.AddTransient<Views.AccountPage>();
        builder.Services.AddTransient<Views.PoiDetailPage>();
        builder.Services.AddTransient<Views.FeaturedDishCategoryPage>();
        builder.Services.AddTransient<Views.PoiBrowsePage>();
        builder.Services.AddTransient<Views.TourPage>();
        builder.Services.AddTransient<Views.ActiveTourPage>();
        builder.Services.AddTransient<Views.ListeningHistoryPage>();

        var app = builder.Build();
        MapTileHttpClientFactory.ConfigureOfflineTileService(
            app.Services.GetRequiredService<IMapOfflineTileService>());
        return app;
    }
}
