using Microsoft.Extensions.Logging;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.App.ViewModels;
using VinhKhanhGuide.Core.Interfaces;
using Microsoft.Maui.Controls.Maps;

namespace VinhKhanhGuide.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
        .UseMauiApp<App>()
        .UseMauiMaps()
        .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IPoiProvider, PoiProvider>();
        builder.Services.AddSingleton<INarrationService, NarrationService>();
        builder.Services.AddSingleton<GeofenceEngine>();
        builder.Services.AddSingleton<RestaurantService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<AppShell>();
        builder.UseMauiMaps();
        return builder.Build();
    }
}
