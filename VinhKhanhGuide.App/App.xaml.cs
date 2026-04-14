using System.Diagnostics;
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
        if (!_authService.IsAuthenticated)
        {
            MainPage = CreateAuthRootPage();
            return;
        }

        try
        {
            MainPage = _serviceProvider.GetRequiredService<AppShell>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Failed to create AppShell: {ex}");

            var fallbackRoot = CreateAuthRootPage();
            MainPage = fallbackRoot;

            _ = fallbackRoot.CurrentPage.DisplayAlert(
                "Không thể mở ứng dụng",
                $"Có lỗi khi khởi tạo giao diện chính: {ex.Message}",
                "OK");
        }
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
