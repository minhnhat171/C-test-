using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.App.Views;

namespace VinhKhanhGuide.App;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthService _authService;

    public App(IServiceProvider serviceProvider, IAuthService authService)
    {
        _serviceProvider = serviceProvider;
        _authService = authService;
        _authService.SessionChanged += OnSessionChanged;

        MainPage = CreateAuthRootPage();
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdateRootPage);
    }

    private void UpdateRootPage()
    {
        MainPage = _authService.IsAuthenticated
            ? _serviceProvider.GetRequiredService<AppShell>()
            : CreateAuthRootPage();
    }

    private NavigationPage CreateAuthRootPage()
    {
        return new NavigationPage(_serviceProvider.GetRequiredService<AuthPage>())
        {
            BarBackgroundColor = Color.FromArgb("#215C57"),
            BarTextColor = Colors.White
        };
    }
}
